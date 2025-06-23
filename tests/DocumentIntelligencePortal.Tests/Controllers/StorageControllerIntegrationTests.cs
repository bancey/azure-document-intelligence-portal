using Microsoft.AspNetCore.Mvc.Testing;
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

    [Fact(Skip = "Requires actual Azure Storage")]
    public async Task GetContainers_EndToEnd_ShouldReturnContainerList()
    {
        // This test would use TestServer to verify the complete pipeline
        
        // Arrange
        using var factory = new WebApplicationFactory<Program>();
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

    [Fact(Skip = "Requires actual Azure Storage")]
    public async Task DownloadDocument_EndToEnd_ShouldReturnFileContent()
    {
        // This test would verify file download functionality
        
        // Arrange
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var containerName = "test-documents";
        var blobName = "sample.pdf";

        // Act
        var response = await client.GetAsync($"/api/storage/containers/{containerName}/documents/{blobName}/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().NotBeEmpty();
    }
}
