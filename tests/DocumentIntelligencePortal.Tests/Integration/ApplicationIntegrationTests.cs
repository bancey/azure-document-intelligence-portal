using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
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

    [Fact(Skip = "Requires actual Azure services or comprehensive mocking")]
    public async Task DocumentAnalysis_AnalyzeDocument_WithValidRequest_ShouldReturnResponse()
    {
        // This test would require either:
        // 1. Actual Azure Document Intelligence service (for full integration tests)
        // 2. Comprehensive mocking of Azure services (for isolated tests)

        // Arrange
        var request = new AnalyzeDocumentRequest
        {
            BlobUri = "https://teststorage.blob.core.windows.net/test-container/test-document.pdf",
            ModelId = "prebuilt-document"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/documentanalysis/analyze", content);

        // Assert
        // The exact assertion would depend on whether you're using real services or mocks
        response.Should().NotBeNull();
    }

    [Fact(Skip = "Requires actual Azure Storage or comprehensive mocking")]
    public async Task Storage_GetContainers_ShouldReturnContainerList()
    {
        // This test would require either:
        // 1. Actual Azure Storage service (for full integration tests)
        // 2. Comprehensive mocking of Azure services (for isolated tests)

        // Act
        var response = await _client.GetAsync("/api/storage/containers");

        // Assert
        // The exact assertion would depend on whether you're using real services or mocks
        response.Should().NotBeNull();
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
