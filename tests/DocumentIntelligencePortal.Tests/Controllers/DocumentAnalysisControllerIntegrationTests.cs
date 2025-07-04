using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace DocumentIntelligencePortal.Tests.Controllers;

/// <summary>
/// Integration tests for DocumentAnalysisController
/// These tests verify the complete request pipeline
/// </summary>
public class DocumentAnalysisControllerIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public DocumentAnalysisControllerIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AnalyzeDocument_EndToEnd_ShouldProcessSuccessfully()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
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
        var request = TestDataFactory.CreateAnalyzeDocumentRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/documentanalysis/analyze", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AnalyzeDocumentResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }
}
