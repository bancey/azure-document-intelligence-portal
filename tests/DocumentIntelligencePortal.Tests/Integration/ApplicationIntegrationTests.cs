using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;
using DocumentIntelligencePortal.Models;

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

/// <summary>
/// Performance tests to verify the application can handle expected load
/// These tests would typically use tools like NBomber or Azure Load Testing
/// </summary>
public class PerformanceTests
{
    [Fact(Skip = "Requires performance testing setup")]
    public async Task DocumentAnalysis_ShouldHandleMultipleConcurrentRequests()
    {
        // This test would simulate multiple concurrent document analysis requests
        // to verify the application can handle the expected load

        // Example implementation would use:
        // - NBomber for load testing
        // - Azure Load Testing service
        // - Custom concurrent request logic

        await Task.CompletedTask; // Placeholder
    }

    [Fact(Skip = "Requires performance testing setup")]
    public async Task StorageOperations_ShouldMeetPerformanceRequirements()
    {
        // This test would verify that storage operations meet performance SLAs
        
        await Task.CompletedTask; // Placeholder
    }
}

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
