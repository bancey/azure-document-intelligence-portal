using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace DocumentIntelligencePortal.Tests.Controllers;

/// <summary>
/// Integration tests for StorageController
/// These tests verify the complete request pipeline
/// </summary>
public class StorageControllerIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public StorageControllerIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetContainers_EndToEnd_ShouldReturnContainerList()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing storage service and add mock
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
                            Containers = new List<string> { "test-container", "documents" }
                        });

                    services.AddSingleton(mockStorageService.Object);
                });
            });
        
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/storage/containers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListContainersResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Containers.Should().NotBeNull();
    }

    [Fact]
    public async Task DownloadDocument_EndToEnd_ShouldReturnFileContent()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing storage service and add mock
                    var storageServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAzureStorageService));
                    if (storageServiceDescriptor != null)
                    {
                        services.Remove(storageServiceDescriptor);
                    }

                    var mockStorageService = new Mock<IAzureStorageService>();
                    var testContent = System.Text.Encoding.UTF8.GetBytes("Test PDF content");
                    var testStream = new MemoryStream(testContent);
                    
                    mockStorageService
                        .Setup(x => x.GetDocumentStreamAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(testStream);

                    services.AddSingleton(mockStorageService.Object);
                });
            });
        
        var client = factory.CreateClient();
        var containerName = "test-documents";
        var blobName = "sample.pdf";

        // Act
        var response = await client.GetAsync($"/api/storage/containers/{containerName}/documents/{blobName}/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().NotBeEmpty();
        System.Text.Encoding.UTF8.GetString(content).Should().Contain("Test PDF content");
    }
}
