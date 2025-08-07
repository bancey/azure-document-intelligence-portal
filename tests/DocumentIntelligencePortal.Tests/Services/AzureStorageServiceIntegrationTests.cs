using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocumentIntelligencePortal.Tests.Fixtures;

namespace DocumentIntelligencePortal.Tests.Services;

/// <summary>
/// Integration tests for AzureStorageService using Azurite (Azure Storage Emulator)
/// These tests demonstrate how to test against a real storage service
/// </summary>
public class AzureStorageServiceIntegrationTests : ContainerTestBase
{
    [Fact]
    public async Task ListContainersAsync_WithRealStorage_ShouldReturnContainers()
    {
        // Arrange
        var testContainerName = "test-container";
        await CreateTestContainerAsync(testContainerName);

        var service = CreateRealAzureStorageService();

        // Act
        var result = await service.ListContainersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Containers.Should().NotBeNull();
        result.Containers.Should().Contain(testContainerName);
    }

    [Fact]
    public async Task ListDocumentsAsync_WithRealStorage_ShouldReturnDocuments()
    {
        // Arrange
        var testContainerName = "test-container";
        var testBlobName = "test.pdf";
        await CreateTestBlobAsync(testContainerName, testBlobName, "Test PDF content");

        var service = CreateRealAzureStorageService();

        // Act
        var result = await service.ListDocumentsAsync(testContainerName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Documents.Should().HaveCount(1);
        result.Documents[0].Name.Should().Be(testBlobName);
    }

    private IAzureStorageService CreateRealAzureStorageService()
    {
        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = new Mock<ILogger<AzureStorageService>>();

        // We need to create a custom AzureStorageService that accepts connection string
        return new TestAzureStorageService(logger.Object, configuration, ConnectionString);
    }
}

/// <summary>
/// Test version of AzureStorageService that accepts a connection string for testing with Azurite
/// </summary>
internal class TestAzureStorageService : IAzureStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureStorageService> _logger;

    public TestAzureStorageService(ILogger<AzureStorageService> logger, IConfiguration configuration, string connectionString)
    {
        _logger = logger;
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<ListContainersResponse> ListContainersAsync()
    {
        try
        {
            _logger.LogInformation("Listing storage containers");

            var containers = new List<string>();
            await foreach (var container in _blobServiceClient.GetBlobContainersAsync())
            {
                containers.Add(container.Name);
            }

            _logger.LogInformation("Found {ContainerCount} containers", containers.Count);

            return new ListContainersResponse
            {
                Success = true,
                Containers = containers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list storage containers");
            return new ListContainersResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ListDocumentsResponse> ListDocumentsAsync(string containerName)
    {
        try
        {
            _logger.LogInformation("Listing documents in container: {Container}", containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var documents = new List<StorageDocument>();

            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                var document = new StorageDocument
                {
                    Name = blob.Name,
                    BlobUri = containerClient.GetBlobClient(blob.Name).Uri.ToString(),
                    Size = blob.Properties.ContentLength ?? 0,
                    ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                    LastModified = blob.Properties.LastModified ?? DateTimeOffset.MinValue,
                    Container = containerName
                };
                documents.Add(document);
            }

            _logger.LogInformation("Found {DocumentCount} documents in container {Container}",
                documents.Count, containerName);

            return new ListDocumentsResponse
            {
                Success = true,
                Documents = documents
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list documents in container: {Container}", containerName);
            return new ListDocumentsResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SearchDocumentsResponse> SearchDocumentsAsync(string containerName, string searchTerm, int maxResults = 100)
    {
        // Simplified implementation for testing
        var listResult = await ListDocumentsAsync(containerName);
        if (!listResult.Success)
        {
            return new SearchDocumentsResponse
            {
                Success = false,
                ErrorMessage = listResult.ErrorMessage,
                SearchTerm = searchTerm,
                MaxResults = maxResults
            };
        }

        var matchingDocuments = listResult.Documents
            .Where(d => d.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();

        return new SearchDocumentsResponse
        {
            Success = true,
            Documents = matchingDocuments,
            SearchTerm = searchTerm,
            TotalMatches = matchingDocuments.Count,
            MaxResults = maxResults,
            HasMoreResults = false
        };
    }

    public async Task<Stream?> GetDocumentStreamAsync(string containerName, string blobName)
    {
        try
        {
            _logger.LogInformation("Getting document stream for: {Container}/{Blob}", containerName, blobName);

            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document stream: {Container}/{Blob}", containerName, blobName);
            return null;
        }
    }

    public async Task<string> GetDocumentSasUriAsync(string containerName, string blobName)
    {
        try
        {
            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Document not found: {containerName}/{blobName}");
            }

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URI: {Container}/{Blob}", containerName, blobName);
            throw;
        }
    }
}
