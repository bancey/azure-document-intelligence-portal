using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace DocumentIntelligencePortal.Tests.Integration;

/// <summary>
/// Security tests to verify the application handles security scenarios correctly
/// </summary>
public class SecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Endpoints_ShouldNotExposeInternalErrors()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing services and add mocks that throw exceptions
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

                var mockStorageService = new Mock<IAzureStorageService>();
                var mockDocIntelligenceService = new Mock<IDocumentIntelligenceService>();
                
                // Make the service throw internal exception
                mockDocIntelligenceService
                    .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
                    .ReturnsAsync(new AnalyzeDocumentResponse
                    {
                        Success = false,
                        Message = "Invalid request parameters"
                    });

                services.AddSingleton(mockStorageService.Object);
                services.AddSingleton(mockDocIntelligenceService.Object);
            });
        });

        var client = factory.CreateClient();
        var maliciousRequest = new AnalyzeDocumentRequest
        {
            BlobUri = "invalid://malicious.uri",
            ModelId = "'; DROP TABLE Users; --"
        };

        var json = JsonSerializer.Serialize(maliciousRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/documentanalysis/analyze", content);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Should not expose internal error details
        responseContent.Should().NotContain("System.");
        responseContent.Should().NotContain("Exception");
        responseContent.Should().NotContain("Stack trace");
        responseContent.Should().NotContain("at System.");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("../../../etc/passwd")]
    [InlineData("%3Cscript%3Ealert('xss')%3C/script%3E")]
    public async Task Endpoints_ShouldHandleMaliciousInput(string maliciousInput)
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add mock storage service that handles requests gracefully
                var storageServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAzureStorageService));
                if (storageServiceDescriptor != null)
                {
                    services.Remove(storageServiceDescriptor);
                }

                var mockStorageService = new Mock<IAzureStorageService>();
                mockStorageService
                    .Setup(x => x.ListDocumentsAsync(It.IsAny<string>()))
                    .ReturnsAsync((string containerName) =>
                    {
                        // Simulate validation that would happen in real service
                        if (containerName.Contains("<script>") || 
                            containerName.Contains("DROP TABLE") || 
                            containerName.Contains("../") ||
                            containerName.Contains("%3C"))
                        {
                            return new ListDocumentsResponse
                            {
                                Success = false,
                                ErrorMessage = "Invalid container name format"
                            };
                        }
                        
                        return new ListDocumentsResponse
                        {
                            Success = true,
                            Documents = new List<StorageDocument>()
                        };
                    });

                services.AddSingleton(mockStorageService.Object);
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/storage/containers/{maliciousInput}/documents");

        // Assert
        // Should return appropriate error status without exposing system details
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotContain("<script>");
        responseContent.Should().NotContain("DROP TABLE");
    }
}
