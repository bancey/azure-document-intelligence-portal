using Microsoft.AspNetCore.Mvc;
using DocumentIntelligencePortal.Controllers;

namespace DocumentIntelligencePortal.Tests.Controllers;

/// <summary>
/// Unit tests for DocumentAnalysisController
/// Focuses on HTTP handling, validation, and controller-specific logic
/// </summary>
public class DocumentAnalysisControllerTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly Mock<IDocumentIntelligenceService> _mockDocumentService;
    private readonly Mock<IAzureStorageService> _mockStorageService;
    private readonly Mock<ILogger<DocumentAnalysisController>> _mockLogger;

    public DocumentAnalysisControllerTests(TestFixture fixture)
    {
        _fixture = fixture;
        _mockDocumentService = new Mock<IDocumentIntelligenceService>();
        _mockStorageService = new Mock<IAzureStorageService>();
        _mockLogger = _fixture.CreateMockLogger<DocumentAnalysisController>();
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var controller = CreateController();

        // Assert
        controller.Should().NotBeNull();
        controller.Should().BeAssignableTo<ControllerBase>();
    }

    [Fact]
    public async Task AnalyzeDocument_WithNullRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.AnalyzeDocument(null!);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AnalyzeDocumentResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Blob URI is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AnalyzeDocument_WithInvalidBlobUri_ShouldReturnBadRequest(string? blobUri)
    {
        // Arrange
        var controller = CreateController();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest(blobUri: blobUri);

        // Act
        var result = await controller.AnalyzeDocument(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AnalyzeDocumentResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Blob URI is required");
    }

    [Fact]
    public async Task AnalyzeDocument_WithNullBlobUri_ShouldReturnBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new AnalyzeDocumentRequest
        {
            BlobUri = null,
            ModelId = "prebuilt-document",
            IncludeFieldElements = true
        };

        // Act
        var result = await controller.AnalyzeDocument(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AnalyzeDocumentResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Blob URI is required");
    }

    [Fact]
    public async Task AnalyzeDocument_WithValidRequest_ShouldCallDocumentService()
    {
        // Arrange
        var controller = CreateController();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest();
        var expectedResponse = new AnalyzeDocumentResponse
        {
            Success = true,
            Message = "Analysis completed successfully",
            Result = TestDataFactory.CreateDocumentAnalysisResult()
        };

        _mockDocumentService
            .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.AnalyzeDocument(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AnalyzeDocumentResponse>().Subject;
        response.Success.Should().BeTrue();

        _mockDocumentService.Verify(
            x => x.AnalyzeDocumentAsync(It.Is<AnalyzeDocumentRequest>(r => 
                r.BlobUri == request.BlobUri && r.ModelId == request.ModelId)),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDocument_WhenServiceReturnsFailure_ShouldReturnBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest();
        var expectedResponse = new AnalyzeDocumentResponse
        {
            Success = false,
            Message = "Document analysis failed"
        };

        _mockDocumentService
            .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.AnalyzeDocument(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AnalyzeDocumentResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeDocument_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var controller = CreateController();
        var request = TestDataFactory.CreateAnalyzeDocumentRequest();

        _mockDocumentService
            .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await controller.AnalyzeDocument(request);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var response = statusCodeResult.Value.Should().BeOfType<AnalyzeDocumentResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Internal server error");
    }

    [Theory]
    [InlineData("", "document.pdf")]
    [InlineData("container", "")]
    [InlineData(null, "document.pdf")]
    [InlineData("container", null)]
    public async Task AnalyzeDocumentByPath_WithInvalidParameters_ShouldReturnBadRequest(
        string? containerName, string? blobName)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.AnalyzeDocumentByPath(containerName!, blobName!);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AnalyzeDocumentResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Container name and blob name are required");
    }

    [Fact]
    public async Task AnalyzeDocumentByPath_WithValidParameters_ShouldCallStorageService()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var blobName = "test-document.pdf";
        var modelId = "prebuilt-invoice";
        var sasUri = "https://storage.blob.core.windows.net/container/document.pdf?sas=token";
        var analysisResponse = new AnalyzeDocumentResponse
        {
            Success = true,
            Result = TestDataFactory.CreateDocumentAnalysisResult()
        };

        _mockStorageService
            .Setup(x => x.GetDocumentSasUriAsync(containerName, blobName))
            .ReturnsAsync(sasUri);

        _mockDocumentService
            .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
            .ReturnsAsync(analysisResponse);

        // Act
        var result = await controller.AnalyzeDocumentByPath(containerName, blobName, modelId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        
        _mockStorageService.Verify(
            x => x.GetDocumentSasUriAsync(containerName, blobName),
            Times.Once);
        
        _mockDocumentService.Verify(
            x => x.AnalyzeDocumentAsync(It.Is<AnalyzeDocumentRequest>(r => 
                r.BlobUri == sasUri && r.ModelId == modelId)),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDocumentByPath_WithDefaultModelId_ShouldUsePrebuiltDocument()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var blobName = "test-document.pdf";
        var sasUri = "https://storage.blob.core.windows.net/container/document.pdf?sas=token";
        var analysisResponse = new AnalyzeDocumentResponse
        {
            Success = true,
            Result = TestDataFactory.CreateDocumentAnalysisResult()
        };

        _mockStorageService
            .Setup(x => x.GetDocumentSasUriAsync(containerName, blobName))
            .ReturnsAsync(sasUri);

        _mockDocumentService
            .Setup(x => x.AnalyzeDocumentAsync(It.IsAny<AnalyzeDocumentRequest>()))
            .ReturnsAsync(analysisResponse);

        // Act
        var result = await controller.AnalyzeDocumentByPath(containerName, blobName);

        // Assert
        _mockDocumentService.Verify(
            x => x.AnalyzeDocumentAsync(It.Is<AnalyzeDocumentRequest>(r => 
                r.ModelId == "prebuilt-document")),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDocumentByPath_ShouldLogAnalysisOperation()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var blobName = "test-document.pdf";
        var modelId = "prebuilt-invoice";

        _mockStorageService
            .Setup(x => x.GetDocumentSasUriAsync(containerName, blobName))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        try
        {
            await controller.AnalyzeDocumentByPath(containerName, blobName, modelId);
        }
        catch
        {
            // Expected to fail
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Analyzing document: {containerName}/{blobName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private DocumentAnalysisController CreateController()
    {
        return new DocumentAnalysisController(
            _mockDocumentService.Object,
            _mockStorageService.Object,
            _mockLogger.Object);
    }
}
