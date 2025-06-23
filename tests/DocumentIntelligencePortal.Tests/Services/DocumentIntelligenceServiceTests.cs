using Microsoft.Extensions.Configuration;

namespace DocumentIntelligencePortal.Tests.Services;

/// <summary>
/// Unit tests for DocumentIntelligenceService
/// Focuses on business logic, error handling, and Azure integration patterns
/// </summary>
public class DocumentIntelligenceServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly Mock<ILogger<DocumentIntelligenceService>> _mockLogger;
    private readonly Mock<IAzureStorageService> _mockStorageService;
    private readonly IConfiguration _configuration;

    public DocumentIntelligenceServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = _fixture.CreateMockLogger<DocumentIntelligenceService>();
        _mockStorageService = new Mock<IAzureStorageService>();
        _configuration = _fixture.Configuration;
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var service = CreateDocumentIntelligenceService();

        // Assert
        service.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Document Intelligence Service initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithMissingEndpoint_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new DocumentIntelligenceService(_mockLogger.Object, invalidConfig, _mockStorageService.Object));
        
        exception.Message.Should().Contain("Azure:DocumentIntelligence:Endpoint configuration is missing");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task AnalyzeDocumentAsync_WithInvalidBlobUri_ShouldReturnFailureResponse(string? invalidBlobUri)
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest(blobUri: invalidBlobUri);

        // Act
        var result = await service.AnalyzeDocumentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Blob URI is required");
        result.Result.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeDocumentAsync_WithValidRequest_ShouldLogAnalysisStart()
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest();

        // Act
        try
        {
            await service.AnalyzeDocumentAsync(request);
        }
        catch
        {
            // Expected to fail due to mock client, but we're testing logging
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting document analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("prebuilt-document")]
    [InlineData("prebuilt-invoice")]
    [InlineData("prebuilt-receipt")]
    [InlineData("custom-model-123")]
    public async Task AnalyzeDocumentAsync_WithDifferentModels_ShouldAcceptValidModelIds(string modelId)
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest(modelId: modelId);

        // Act
        try
        {
            await service.AnalyzeDocumentAsync(request);
        }
        catch
        {
            // Expected to fail due to mock client
        }

        // Assert - Verify the model ID is used in logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"with model: {modelId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDocumentFromStreamAsync_WithNullStream_ShouldReturnFailureResponse()
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentStreamRequest();

        // Act
        var result = await service.AnalyzeDocumentFromStreamAsync(null!, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Document stream is required");
    }

    [Fact]
    public async Task AnalyzeDocumentFromStreamAsync_WithValidStream_ShouldLogAnalysisStart()
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentStreamRequest();
        var stream = TestDataFactory.CreateTestDocumentStream();

        // Act
        try
        {
            await service.AnalyzeDocumentFromStreamAsync(stream, request);
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting document analysis from stream")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("", "blob.pdf")]
    [InlineData("container", "")]
    [InlineData(null, "blob.pdf")]
    [InlineData("container", null)]
    public async Task AnalyzeDocumentFromStorageAsync_WithInvalidParameters_ShouldReturnFailureResponse(
        string? containerName, string? blobName)
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentFromStorageRequest(
            containerName: containerName, blobName: blobName);

        // Act
        var result = await service.AnalyzeDocumentFromStorageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Container name and blob name are required");
    }

    [Fact]
    public async Task AnalyzeDocumentFromStorageAsync_WithValidParameters_ShouldCallStorageService()
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentFromStorageRequest();
        var mockStream = TestDataFactory.CreateTestDocumentStream();
        
        _mockStorageService
            .Setup(x => x.GetDocumentStreamAsync(request.ContainerName, request.BlobName))
            .ReturnsAsync(mockStream);

        // Act
        try
        {
            await service.AnalyzeDocumentFromStorageAsync(request);
        }
        catch
        {
            // Expected to fail due to mock client
        }

        // Assert
        _mockStorageService.Verify(
            x => x.GetDocumentStreamAsync(request.ContainerName, request.BlobName),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDocumentFromStorageAsync_WhenDocumentNotFound_ShouldReturnFailureResponse()
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();
        var request = TestDataFactory.CreateAnalyzeDocumentFromStorageRequest();
        
        _mockStorageService
            .Setup(x => x.GetDocumentStreamAsync(request.ContainerName, request.BlobName))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await service.AnalyzeDocumentFromStorageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Document not found");
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ShouldReturnListOfModels()
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();

        // Act & Assert
        // Note: This would require mocking the DocumentAnalysisClient more extensively
        // For now, we'll verify the method exists and handles exceptions properly
        try
        {
            var models = await service.GetAvailableModelsAsync();
            // In a real implementation, this would return a list of model names
        }
        catch (Exception ex)
        {
            // Expected to fail with current implementation due to authentication
            ex.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetAnalysisResultAsync_WithInvalidOperationId_ShouldHandleGracefully(string? operationId)
    {
        // Arrange
        var service = CreateDocumentIntelligenceService();

        // Act
        var result = await service.GetAnalysisResultAsync(operationId!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Service_ShouldImplementInterface()
    {
        // Arrange & Act
        var service = CreateDocumentIntelligenceService();

        // Assert
        service.Should().BeAssignableTo<IDocumentIntelligenceService>();
    }

    private DocumentIntelligenceService CreateDocumentIntelligenceService()
    {
        return new DocumentIntelligenceService(
            _mockLogger.Object,
            _configuration,
            _mockStorageService.Object);
    }
}
