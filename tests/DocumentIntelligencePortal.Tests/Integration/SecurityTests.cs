using Microsoft.AspNetCore.Mvc.Testing;
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
        var maliciousRequest = new AnalyzeDocumentRequest
        {
            BlobUri = "invalid://malicious.uri",
            ModelId = "'; DROP TABLE Users; --"
        };

        var json = JsonSerializer.Serialize(maliciousRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/documentanalysis/analyze", content);

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
        // Act
        var response = await _client.GetAsync($"/api/storage/containers/{maliciousInput}/documents");

        // Assert
        // Should return appropriate error status without exposing system details
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotContain("<script>");
        responseContent.Should().NotContain("DROP TABLE");
    }
}
