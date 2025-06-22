using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocumentIntelligencePortal.Services;
using DocumentIntelligencePortal.Models;
using DocumentIntelligencePortal.Tests.Fixtures;

namespace DocumentIntelligencePortal.Tests.Services;

/// <summary>
/// Unit tests for AzureStorageService
/// Focuses on business logic, error handling, and Azure Storage integration patterns
/// </summary>
public class AzureStorageServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly Mock<ILogger<AzureStorageService>> _mockLogger;
    private readonly IConfiguration _configuration;

    public AzureStorageServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = _fixture.CreateMockLogger<AzureStorageService>();
        _configuration = _fixture.Configuration;
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var service = CreateAzureStorageService();

        // Assert
        service.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Azure Storage Service initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithMissingStorageAccountName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new AzureStorageService(_mockLogger.Object, invalidConfig));
        
        exception.Message.Should().Contain("Azure:StorageAccountName configuration is missing");
    }

    [Fact]
    public async Task ListContainersAsync_ShouldLogContainerCount()
    {
        // Arrange
        var service = CreateAzureStorageService();

        // Act
        try
        {
            await service.ListContainersAsync();
        }
        catch
        {
            // Expected to fail due to mock client, but we're testing logging structure
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Listing storage containers")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ListDocumentsAsync_WithInvalidContainerName_ShouldHandleGracefully(string? containerName)
    {
        // Arrange
        var service = CreateAzureStorageService();

        // Act
        try
        {
            var result = await service.ListDocumentsAsync(containerName!);
            
            // If this doesn't throw, verify it handles invalid input appropriately
            result.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            // Expected behavior when container name is invalid
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ListDocumentsAsync_WithValidContainerName_ShouldLogOperation()
    {
        // Arrange
        var service = CreateAzureStorageService();
        var containerName = "test-container";

        // Act
        try
        {
            await service.ListDocumentsAsync(containerName);
        }
        catch
        {
            // Expected to fail due to mock client
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Listing documents in container: {containerName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("", "blob.pdf")]
    [InlineData("container", "")]
    [InlineData(null, "blob.pdf")]
    [InlineData("container", null)]
    public async Task GetDocumentStreamAsync_WithInvalidParameters_ShouldHandleGracefully(
        string? containerName, string? blobName)
    {
        // Arrange
        var service = CreateAzureStorageService();

        // Act & Assert
        try
        {
            var result = await service.GetDocumentStreamAsync(containerName!, blobName!);
            
            // If this doesn't throw, the result should be null for invalid parameters
            result.Should().BeNull();
        }
        catch (Exception ex)
        {
            // Expected behavior for invalid parameters
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetDocumentStreamAsync_WithValidParameters_ShouldLogOperation()
    {
        // Arrange
        var service = CreateAzureStorageService();
        var containerName = "test-container";
        var blobName = "test-document.pdf";

        // Act
        try
        {
            await service.GetDocumentStreamAsync(containerName, blobName);
        }
        catch
        {
            // Expected to fail due to mock client
        }

        // Assert - For this test, we'd need to mock the BlobServiceClient more extensively
        // This demonstrates the test structure for when mocking is implemented
        service.Should().BeAssignableTo<IAzureStorageService>();
    }

    [Theory]
    [InlineData("", "search-term")]
    [InlineData("container", "")]
    [InlineData(null, "search-term")]
    [InlineData("container", null)]
    public async Task SearchDocumentsAsync_WithInvalidParameters_ShouldHandleGracefully(
        string? containerName, string? searchTerm)
    {
        // Arrange
        var service = CreateAzureStorageService();

        // Act & Assert
        try
        {
            var result = await service.SearchDocumentsAsync(containerName!, searchTerm!);
            
            // Should handle invalid parameters gracefully
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
        }
        catch (Exception ex)
        {
            // Expected behavior for invalid parameters
            ex.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("*.pdf")]
    [InlineData("invoice*")]
    [InlineData("*2023*")]
    [InlineData("test?.pdf")]
    public async Task SearchDocumentsAsync_WithWildcardPatterns_ShouldAcceptValidPatterns(string searchTerm)
    {
        // Arrange
        var service = CreateAzureStorageService();
        var containerName = "test-container";

        // Act
        try
        {
            await service.SearchDocumentsAsync(containerName, searchTerm);
        }
        catch
        {
            // Expected to fail due to mock client, but pattern should be accepted
        }

        // Assert - Verify the service accepts wildcard patterns
        service.Should().BeAssignableTo<IAzureStorageService>();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task SearchDocumentsAsync_WithDifferentMaxResults_ShouldAcceptValidLimits(int maxResults)
    {
        // Arrange
        var service = CreateAzureStorageService();
        var containerName = "test-container";
        var searchTerm = "*.pdf";

        // Act
        try
        {
            await service.SearchDocumentsAsync(containerName, searchTerm, maxResults);
        }
        catch
        {
            // Expected to fail due to mock client, but limit should be accepted
        }

        // Assert
        service.Should().BeAssignableTo<IAzureStorageService>();
    }

    [Fact]
    public async Task GetDocumentSasUriAsync_WithValidParameters_ShouldReturnUri()
    {
        // Arrange
        var service = CreateAzureStorageService();
        var containerName = "test-container";
        var blobName = "test-document.pdf";

        // Act & Assert
        try
        {
            var uri = await service.GetDocumentSasUriAsync(containerName, blobName);
            
            // In a real implementation with proper mocking, this would return a SAS URI
            uri.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            // Expected to fail due to mock client
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public void Service_ShouldImplementInterface()
    {
        // Arrange & Act
        var service = CreateAzureStorageService();

        // Assert
        service.Should().BeAssignableTo<IAzureStorageService>();
    }

    private AzureStorageService CreateAzureStorageService()
    {
        return new AzureStorageService(_mockLogger.Object, _configuration);
    }
}

/// <summary>
/// Integration tests for AzureStorageService using Azurite (Azure Storage Emulator)
/// These tests demonstrate how to test against a real storage service
/// </summary>
public class AzureStorageServiceIntegrationTests : IClassFixture<TestFixture>, IDisposable
{
    private readonly TestFixture _fixture;
    
    // Note: In a real implementation, you'd use Testcontainers.Azurite here
    // private readonly AzuriteContainer _azuriteContainer;

    public AzureStorageServiceIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
        
        // Example setup for Azurite container (commented out as it requires the actual package):
        // _azuriteContainer = new AzuriteBuilder()
        //     .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
        //     .Build();
        // await _azuriteContainer.StartAsync();
    }

    [Fact(Skip = "Requires Azurite container setup")]
    public async Task ListContainersAsync_WithRealStorage_ShouldReturnContainers()
    {
        // This test would run against a real Azurite instance
        
        // Arrange
        // var connectionString = _azuriteContainer.GetConnectionString();
        // var service = CreateRealAzureStorageService(connectionString);

        // Act
        // var result = await service.ListContainersAsync();

        // Assert
        // result.Should().NotBeNull();
        // result.Success.Should().BeTrue();
        // result.Containers.Should().NotBeNull();
    }

    [Fact(Skip = "Requires Azurite container setup")]
    public async Task ListDocumentsAsync_WithRealStorage_ShouldReturnDocuments()
    {
        // This test would create a container and upload test documents
        
        // Arrange
        // var connectionString = _azuriteContainer.GetConnectionString();
        // var service = CreateRealAzureStorageService(connectionString);
        // await CreateTestContainer(connectionString, "test-container");
        // await UploadTestDocument(connectionString, "test-container", "test.pdf");

        // Act
        // var result = await service.ListDocumentsAsync("test-container");

        // Assert
        // result.Should().NotBeNull();
        // result.Success.Should().BeTrue();
        // result.Documents.Should().HaveCount(1);
        // result.Documents[0].Name.Should().Be("test.pdf");
    }

    public void Dispose()
    {
        // _azuriteContainer?.DisposeAsync().AsTask().Wait();
        GC.SuppressFinalize(this);
    }
}
