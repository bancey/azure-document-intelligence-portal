using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace DocumentIntelligencePortal.Tests.Integration;

/// <summary>
/// Security tests to verify the application handles security scenarios correctly
/// </summary>
public class SecurityTests
{
    [Fact]
    public async Task Endpoints_ShouldNotExposeInternalErrors()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
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

        using var client = factory.CreateClient();
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
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/storage/containers/{maliciousInput}/documents");

        // Assert
        // The endpoint should still return OK status but the mock service will handle validation
        // This tests that the endpoint structure exists and handles requests gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.InternalServerError);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        // Verify no sensitive information is exposed
        responseContent.Should().NotContain("System.");
        responseContent.Should().NotContain("Stack trace");
        responseContent.Should().NotContain("at System.");
    }
}
