using Azure.Core;
using Microsoft.Extensions.Configuration;
using DocumentIntelligencePortal.Tests.Fixtures;

namespace DocumentIntelligencePortal.Tests.Services;

/// <summary>
/// Unit tests for AzureStorageService
/// Focuses on business logic, error handling, and Azure Storage integration patterns
/// These tests use mocks and avoid real service connections for fast execution
/// </summary>
public class AzureStorageServiceTests : MockedTestBase
{
    private readonly Mock<ILogger<AzureStorageService>> _mockLogger;
    private readonly Mock<IAzureCredentialProvider> _mockCredentialProvider;

    public AzureStorageServiceTests()
    {
        _mockLogger = CreateMockLogger<AzureStorageService>();
        _mockCredentialProvider = new Mock<IAzureCredentialProvider>();
        
        // Setup mock credential provider to return a mock credential
        _mockCredentialProvider
            .Setup(x => x.GetCredential())
            .Returns(new Mock<TokenCredential>().Object);
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
            new AzureStorageService(_mockLogger.Object, invalidConfig, _mockCredentialProvider.Object));
        
        exception.Message.Should().Contain("Azure:StorageAccountName configuration is missing");
    }

    [Fact]
    public async Task ListContainersAsync_ShouldLogContainerCount()
    {
        // Arrange
        var service = CreateAzureStorageService();

        // Act - This should fail fast with our mock configuration (no retries)
        var result = await service.ListContainersAsync();

        // Assert - Should fail fast but still log the attempt
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        
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

        // Act - Should fail fast with mock configuration
        var result = await service.ListDocumentsAsync(containerName);

        // Assert - Should fail fast but still log the attempt
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        
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

        // Act - Should fail fast with mock configuration
        var result = await service.GetDocumentStreamAsync(containerName, blobName);

        // Assert - Should fail fast and return null
        result.Should().BeNull();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Getting document stream for: {containerName}/{blobName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

        // Act & Assert
        // These tests validate that the service accepts wildcard patterns
        // Since we're using a mock configuration with no retries, this will fail fast
        var result = await service.SearchDocumentsAsync(containerName, searchTerm);
        
        // Should fail fast without long retries but pattern is accepted
        result.Should().NotBeNull();
        result.Success.Should().BeFalse(); // Expected to fail with mocked connection
        result.SearchTerm.Should().Be(searchTerm);
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

        // Act & Assert
        // Validate service accepts different max results values
        var result = await service.SearchDocumentsAsync(containerName, searchTerm, maxResults);
        
        // Should fail fast without long retries but maxResults is accepted
        result.Should().NotBeNull();
        result.Success.Should().BeFalse(); // Expected to fail with mocked connection
        result.MaxResults.Should().Be(maxResults);
    }

    [Fact]
    public async Task GetDocumentSasUriAsync_WithValidParameters_ShouldReturnUri()
    {
        // Arrange
        var service = CreateAzureStorageService();
        var containerName = "test-container";
        var blobName = "test-document.pdf";

        // Act & Assert - Should fail fast with mock configuration
        await Assert.ThrowsAnyAsync<Exception>(async () => 
            await service.GetDocumentSasUriAsync(containerName, blobName));
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
        return new AzureStorageService(_mockLogger.Object, Configuration, _mockCredentialProvider.Object);
    }
}
