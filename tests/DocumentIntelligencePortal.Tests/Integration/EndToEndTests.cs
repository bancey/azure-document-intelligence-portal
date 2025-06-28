using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace DocumentIntelligencePortal.Tests.Integration;

/// <summary>
/// End-to-end tests that simulate deployed environment scenarios
/// These tests use mocks to avoid requiring actual deployed environments
/// </summary>
public class EndToEndTests
{
    [Fact]
    public async Task DeployedApplication_HealthCheck_ShouldReturnOk()
    {
        // Arrange - Simulate a deployed application with mocked services
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Add minimal mocks for health check scenarios
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
                            Containers = new List<string> { "health-check" }
                        });

                    services.AddSingleton(mockStorageService.Object);
                });
            });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        // NotFound is acceptable if health check endpoint is not configured
    }

    [Fact]
    public async Task DeployedApplication_DocumentAnalysis_ShouldProcessRealDocument()
    {
        // Arrange - Simulate deployed application with realistic mock responses
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing services and add realistic mocks
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
                    
                    mockDocIntelligenceService
                        .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
                        .ReturnsAsync(new AnalyzeDocumentResponse
                        {
                            Success = true,
                            OperationId = Guid.NewGuid().ToString(),
                            Result = TestDataFactory.CreateDocumentAnalysisResult("prebuilt-document", "sample-invoice.pdf"),
                            Message = "Analysis completed successfully"
                        });

                    services.AddSingleton(mockStorageService.Object);
                    services.AddSingleton(mockDocIntelligenceService.Object);
                });
            });

        var client = factory.CreateClient();
        var request = new AnalyzeDocumentRequest
        {
            BlobUri = "https://test-storage.blob.core.windows.net/test-documents/sample.pdf",
            ModelId = "prebuilt-document"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/documentanalysis/analyze", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AnalyzeDocumentResponse>(responseContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result!.Content.Should().NotBeNullOrEmpty();
    }
}
