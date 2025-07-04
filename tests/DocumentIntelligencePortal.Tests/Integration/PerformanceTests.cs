using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace DocumentIntelligencePortal.Tests.Integration;

/// <summary>
/// Performance tests to verify the application can handle expected load
/// These tests use mock services to simulate load without requiring actual Azure services
/// </summary>
public class PerformanceTests
{
    [Fact]
    public async Task DocumentAnalysis_ShouldHandleMultipleConcurrentRequests()
    {
        // Arrange
        const int concurrentRequests = 10;
        var mockDocIntelligenceService = new Mock<IDocumentIntelligenceService>();
        mockDocIntelligenceService
            .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
            .ReturnsAsync(() =>
            {
                // Add small delay to simulate processing
                Thread.Sleep(50);
                return new AnalyzeDocumentResponse
                {
                    Success = true,
                    OperationId = Guid.NewGuid().ToString(),
                    Result = TestDataFactory.CreateDocumentAnalysisResult(),
                    Message = "Analysis completed"
                };
            });

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing services and add fast mock
                    var docIntelligenceServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDocumentIntelligenceService));
                    if (docIntelligenceServiceDescriptor != null)
                    {
                        services.Remove(docIntelligenceServiceDescriptor);
                    }

                    services.AddSingleton(mockDocIntelligenceService.Object);
                });
            });

        var client = factory.CreateClient();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async _ =>
            {
                var response = await client.PostAsJsonAsync("/api/documentanalysis/analyze", request);
                return response.IsSuccessStatusCode;
            });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllBeEquivalentTo(true, "All requests should succeed");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "All requests should complete within 5 seconds");
    }

    [Fact]
    public async Task StorageOperations_ShouldMeetPerformanceRequirements()
    {
        // Arrange
        var mockStorageService = new Mock<IAzureStorageService>();
        mockStorageService
            .Setup(x => x.ListContainersAsync())
            .ReturnsAsync(() =>
            {
                // Add small delay to simulate network call
                Thread.Sleep(25);
                return new ListContainersResponse
                {
                    Success = true,
                    Containers = Enumerable.Range(1, 100).Select(i => $"container-{i}").ToList()
                };
            });

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing storage service and add fast mock
                    var storageServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAzureStorageService));
                    if (storageServiceDescriptor != null)
                    {
                        services.Remove(storageServiceDescriptor);
                    }

                    services.AddSingleton(mockStorageService.Object);
                });
            });

        var client = factory.CreateClient();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.GetAsync("/api/storage/containers");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Storage operations should complete within 1 second");
        
        var result = await response.Content.ReadFromJsonAsync<ListContainersResponse>();
        result.Should().NotBeNull();
        result!.Containers.Should().HaveCount(100);
    }
}
