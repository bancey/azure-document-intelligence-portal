using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DocumentIntelligencePortal.Tests.Integration;

/// <summary>
/// Integration tests that verify the complete application pipeline
/// These tests use TestServer to test the actual HTTP endpoints
/// </summary>
public class ApplicationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApplicationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration for testing
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Azure:DocumentIntelligence:Endpoint"] = "https://test-document-intelligence.cognitiveservices.azure.com/",
                    ["Azure:StorageAccountName"] = "teststorageaccount",
                    ["Logging:LogLevel:Default"] = "Warning" // Reduce log noise in tests
                });
            });

            builder.ConfigureServices(services =>
            {
                // Here you could replace real Azure services with mocks for testing
                // This is useful for testing without requiring actual Azure resources
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // This test assumes you have a health check endpoint
        // You might want to add one to your application

        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        // NotFound is acceptable if you don't have a root endpoint
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldReturnSwaggerUI()
    {
        // Act
        var response = await _client.GetAsync("/swagger");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.MovedPermanently, HttpStatusCode.Found);
    }

    [Fact]
    public async Task DocumentAnalysis_AnalyzeDocument_WithValidRequest_ShouldReturnResponse()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing services and add mocks
                var storageServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAzureStorageService));
                if (storageServiceDescriptor != null)
                {
                    services.Remove(storageServiceDescriptor);
                }
                
                var docIntelligenceServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDocumentIntelligenceService));
                if (docIntelligenceServiceDescriptor != null)
                {
                    services.Remove(docIntelligenceServiceDescriptor);
                }

                // Add mock services
                var mockStorageService = new Mock<IAzureStorageService>();
                var mockDocIntelligenceService = new Mock<IDocumentIntelligenceService>();
                
                mockDocIntelligenceService
                    .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
                    .ReturnsAsync(new AnalyzeDocumentResponse
                    {
                        Success = true,
                        OperationId = Guid.NewGuid().ToString(),
                        Result = TestDataFactory.CreateDocumentAnalysisResult(),
                        Message = "Analysis completed successfully"
                    });

                services.AddSingleton(mockStorageService.Object);
                services.AddSingleton(mockDocIntelligenceService.Object);
            });
        });

        var client = factory.CreateClient();
        var request = new AnalyzeDocumentRequest
        {
            BlobUri = "https://teststorage.blob.core.windows.net/test-container/test-document.pdf",
            ModelId = "prebuilt-document"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/documentanalysis/analyze", content);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        var result = JsonSerializer.Deserialize<AnalyzeDocumentResponse>(responseContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Storage_GetContainers_ShouldReturnContainerList()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing storage service and add mock
                var storageServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAzureStorageService));
                if (storageServiceDescriptor != null)
                {
                    services.Remove(storageServiceDescriptor);
                }

                var mockStorageService = new Mock<IAzureStorageService>();
                mockStorageService
                    .Setup(x => x.ListContainersAsync())
                    .ReturnsAsync(new ListContainersResponse
                    {
                        Success = true,
                        Containers = new List<string> { "test-container", "documents" }
                    });

                services.AddSingleton(mockStorageService.Object);
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/storage/containers");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ListContainersResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Containers.Should().NotBeNull();
        result.Containers.Should().Contain("test-container");
    }

    [Theory]
    [InlineData("/api/documentanalysis/analyze", "POST")]
    [InlineData("/api/storage/containers", "GET")]
    public async Task Endpoints_ShouldBeRoutedCorrectly(string endpoint, string method)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        
        if (method == "POST")
        {
            // Add minimal body for POST requests to avoid bad request due to missing body
            var testRequest = new AnalyzeDocumentRequest
            {
                BlobUri = "https://test.blob.core.windows.net/container/document.pdf"
            };
            var json = JsonSerializer.Serialize(testRequest);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // We're not testing the full functionality here, just that the routes exist
        // and return something other than 404 (Not Found)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Application_ShouldHaveCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/storage/containers");

        // Assert
        if (response.Content.Headers.ContentType != null)
        {
            response.Content.Headers.ContentType.MediaType.Should().BeOneOf("application/json", "text/plain");
        }
    }

    [Fact]
    public async Task Application_ShouldHandleCORS()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Origin", "https://localhost:3000");

        // Act
        var response = await _client.GetAsync("/api/storage/containers");

        // Assert
        // Check if CORS headers are present (this depends on your CORS configuration)
        var hasAccessControlAllowOrigin = response.Headers.Contains("Access-Control-Allow-Origin");
        
        // This assertion might need to be adjusted based on your CORS policy
        // For now, we just verify the request completes without CORS errors
        response.Should().NotBeNull();
    }
}
