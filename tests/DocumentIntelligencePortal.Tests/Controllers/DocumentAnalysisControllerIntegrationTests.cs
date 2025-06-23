using Microsoft.AspNetCore.Mvc.Testing;
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

    [Fact(Skip = "Requires actual Azure services")]
    public async Task AnalyzeDocument_EndToEnd_ShouldProcessSuccessfully()
    {
        // This test would use TestServer to verify the complete pipeline
        // including middleware, authentication, and Azure service integration
        
        // Arrange
        using var factory = new WebApplicationFactory<Program>();
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
