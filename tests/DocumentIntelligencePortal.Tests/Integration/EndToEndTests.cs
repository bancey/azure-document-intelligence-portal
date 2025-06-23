using System.Net;
using System.Text.Json;

namespace DocumentIntelligencePortal.Tests.Integration;

/// <summary>
/// End-to-end tests that would run against a fully deployed environment
/// These are typically run in a staging environment before production deployment
/// </summary>
public class EndToEndTests
{
    [Fact(Skip = "Requires deployed environment")]
    public async Task DeployedApplication_HealthCheck_ShouldReturnOk()
    {
        // This test would run against an actual deployed instance
        // It would be configured with the URL of your staging/production environment

        // Arrange
        var baseUrl = Environment.GetEnvironmentVariable("E2E_TEST_BASE_URL") ?? "https://your-app.azurewebsites.net";
        using var client = new HttpClient();

        // Act
        var response = await client.GetAsync($"{baseUrl}/api/storage/containers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = "Requires deployed environment with test data")]
    public async Task DeployedApplication_DocumentAnalysis_ShouldProcessRealDocument()
    {
        // This test would use actual Azure services with real test documents
        
        // Arrange
        var baseUrl = Environment.GetEnvironmentVariable("E2E_TEST_BASE_URL") ?? "https://your-app.azurewebsites.net";
        var testDocumentUri = Environment.GetEnvironmentVariable("E2E_TEST_DOCUMENT_URI") ?? "https://test-storage.blob.core.windows.net/test-documents/sample.pdf";
        
        using var client = new HttpClient();
        var request = new AnalyzeDocumentRequest
        {
            BlobUri = testDocumentUri,
            ModelId = "prebuilt-document"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync($"{baseUrl}/api/documentanalysis/analyze", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AnalyzeDocumentResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
    }
}
