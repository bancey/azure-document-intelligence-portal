using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DocumentIntelligencePortal.Controllers;
using DocumentIntelligencePortal.Services;
using DocumentIntelligencePortal.Models;
using DocumentIntelligencePortal.Tests.Fixtures;

namespace DocumentIntelligencePortal.Tests.Controllers;

/// <summary>
/// Unit tests for StorageController
/// Focuses on HTTP handling, validation, and controller-specific logic
/// </summary>
public class StorageControllerTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly Mock<IAzureStorageService> _mockStorageService;
    private readonly Mock<ILogger<StorageController>> _mockLogger;

    public StorageControllerTests(TestFixture fixture)
    {
        _fixture = fixture;
        _mockStorageService = new Mock<IAzureStorageService>();
        _mockLogger = _fixture.CreateMockLogger<StorageController>();
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
    public async Task GetContainers_WhenServiceReturnsSuccess_ShouldReturnOkResult()
    {
        // Arrange
        var controller = CreateController();
        var expectedResponse = new ListContainersResponse
        {
            Success = true,
            Containers = new List<string> { "container1", "container2", "documents" }
        };

        _mockStorageService
            .Setup(x => x.ListContainersAsync())
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.GetContainers();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ListContainersResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Containers.Should().HaveCount(3);
        response.Containers.Should().Contain("container1");

        _mockStorageService.Verify(x => x.ListContainersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetContainers_WhenServiceReturnsFailure_ShouldReturnBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var expectedResponse = new ListContainersResponse
        {
            Success = false,
            ErrorMessage = "Failed to list containers"
        };

        _mockStorageService
            .Setup(x => x.ListContainersAsync())
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.GetContainers();

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ListContainersResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetContainers_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var controller = CreateController();

        _mockStorageService
            .Setup(x => x.ListContainersAsync())
            .ThrowsAsync(new InvalidOperationException("Storage service error"));

        // Act
        var result = await controller.GetContainers();

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var response = statusCodeResult.Value.Should().BeOfType<ListContainersResponse>().Subject;
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Internal server error");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetDocuments_WithInvalidContainerName_ShouldReturnBadRequest(string? containerName)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetDocuments(containerName!);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ListDocumentsResponse>().Subject;
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Contain("Container name is required");
    }

    [Fact]
    public async Task GetDocuments_WithValidContainerName_ShouldCallStorageService()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var expectedResponse = new ListDocumentsResponse
        {
            Success = true,
            Documents = new List<StorageDocument>
            {
                TestDataFactory.CreateStorageDocument("document1.pdf"),
                TestDataFactory.CreateStorageDocument("document2.pdf")
            }
        };

        _mockStorageService
            .Setup(x => x.ListDocumentsAsync(containerName))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.GetDocuments(containerName);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ListDocumentsResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Documents.Should().HaveCount(2);

        _mockStorageService.Verify(x => x.ListDocumentsAsync(containerName), Times.Once);
    }

    [Theory]
    [InlineData("", "document.pdf")]
    [InlineData("container", "")]
    [InlineData(null, "document.pdf")]
    [InlineData("container", null)]
    public async Task DownloadDocument_WithInvalidParameters_ShouldReturnBadRequest(
        string? containerName, string? blobName)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.DownloadDocument(containerName!, blobName!);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Container name and blob name are required");
    }

    [Fact]
    public async Task DownloadDocument_WithValidParameters_ShouldReturnFileStream()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var blobName = "test-document.pdf";
        var testStream = TestDataFactory.CreateTestDocumentStream("Test PDF content");

        _mockStorageService
            .Setup(x => x.GetDocumentStreamAsync(containerName, blobName))
            .ReturnsAsync(testStream);

        // Act
        var result = await controller.DownloadDocument(containerName, blobName);

        // Assert
        result.Should().NotBeNull();
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be(blobName);

        _mockStorageService.Verify(
            x => x.GetDocumentStreamAsync(containerName, blobName),
            Times.Once);
    }

    [Fact]
    public async Task DownloadDocument_WhenDocumentNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var blobName = "nonexistent.pdf";

        _mockStorageService
            .Setup(x => x.GetDocumentStreamAsync(containerName, blobName))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await controller.DownloadDocument(containerName, blobName);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Document not found: {containerName}/{blobName}");
    }

    [Fact]
    public async Task DownloadDocument_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var blobName = "test-document.pdf";

        _mockStorageService
            .Setup(x => x.GetDocumentStreamAsync(containerName, blobName))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        var result = await controller.DownloadDocument(containerName, blobName);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("Internal server error");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SearchDocuments_WithInvalidContainerName_ShouldReturnBadRequest(string? containerName)
    {
        // Arrange
        var controller = CreateController();
        var searchTerm = "*.pdf";

        // Act
        var result = await controller.SearchDocuments(containerName!, searchTerm);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<SearchDocumentsResponse>().Subject;
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Contain("Container name is required");
    }

    [Fact]
    public async Task SearchDocuments_WithValidParameters_ShouldCallStorageService()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var searchTerm = "*.pdf";
        var maxResults = 50;
        var expectedResponse = new SearchDocumentsResponse
        {
            Success = true,
            Documents = new List<StorageDocument>
            {
                TestDataFactory.CreateStorageDocument("document1.pdf"),
                TestDataFactory.CreateStorageDocument("document2.pdf")
            },
            SearchTerm = searchTerm,
            TotalMatches = 2,
            MaxResults = maxResults,
            HasMoreResults = false
        };

        _mockStorageService
            .Setup(x => x.SearchDocumentsAsync(containerName, searchTerm, maxResults))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.SearchDocuments(containerName, searchTerm, maxResults);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SearchDocumentsResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Documents.Should().HaveCount(2);
        response.SearchTerm.Should().Be(searchTerm);
        response.MaxResults.Should().Be(maxResults);

        _mockStorageService.Verify(
            x => x.SearchDocumentsAsync(containerName, searchTerm, maxResults),
            Times.Once);
    }

    [Fact]
    public async Task SearchDocuments_WithDefaultMaxResults_ShouldUse100()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var searchTerm = "*.pdf";
        var expectedResponse = new SearchDocumentsResponse
        {
            Success = true,
            Documents = new List<StorageDocument>(),
            SearchTerm = searchTerm,
            TotalMatches = 0,
            MaxResults = 100,
            HasMoreResults = false
        };

        _mockStorageService
            .Setup(x => x.SearchDocumentsAsync(containerName, searchTerm, 100))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.SearchDocuments(containerName, searchTerm);

        // Assert
        _mockStorageService.Verify(
            x => x.SearchDocumentsAsync(containerName, searchTerm, 100),
            Times.Once);
    }

    [Theory]
    [InlineData("*.pdf")]
    [InlineData("invoice*")]
    [InlineData("*2023*")]
    [InlineData("test?.pdf")]
    public async Task SearchDocuments_WithWildcardPatterns_ShouldAcceptPatterns(string searchTerm)
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var expectedResponse = new SearchDocumentsResponse
        {
            Success = true,
            SearchTerm = searchTerm
        };

        _mockStorageService
            .Setup(x => x.SearchDocumentsAsync(containerName, searchTerm, 100))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await controller.SearchDocuments(containerName, searchTerm);

        // Assert
        _mockStorageService.Verify(
            x => x.SearchDocumentsAsync(containerName, searchTerm, 100),
            Times.Once);
    }

    [Fact]
    public async Task SearchDocuments_ShouldLogSearchOperation()
    {
        // Arrange
        var controller = CreateController();
        var containerName = "test-container";
        var searchTerm = "*.pdf";

        _mockStorageService
            .Setup(x => x.SearchDocumentsAsync(containerName, searchTerm, 100))
            .ThrowsAsync(new Exception("Search error"));

        // Act
        try
        {
            await controller.SearchDocuments(containerName, searchTerm);
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Searching documents in container: {containerName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private StorageController CreateController()
    {
        return new StorageController(_mockStorageService.Object, _mockLogger.Object);
    }
}

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
        // using var factory = new WebApplicationFactory<Program>();
        // var client = factory.CreateClient();

        // Act
        // var response = await client.GetAsync("/api/storage/containers");

        // Assert
        // response.StatusCode.Should().Be(HttpStatusCode.OK);
        // var result = await response.Content.ReadFromJsonAsync<ListContainersResponse>();
        // result.Should().NotBeNull();
        // result!.Success.Should().BeTrue();
        // result.Containers.Should().NotBeNull();
    }

    [Fact(Skip = "Requires actual Azure Storage")]
    public async Task DownloadDocument_EndToEnd_ShouldReturnFileContent()
    {
        // This test would verify file download functionality
        
        // Arrange
        // using var factory = new WebApplicationFactory<Program>();
        // var client = factory.CreateClient();
        // var containerName = "test-documents";
        // var blobName = "sample.pdf";

        // Act
        // var response = await client.GetAsync($"/api/storage/containers/{containerName}/documents/{blobName}/download");

        // Assert
        // response.StatusCode.Should().Be(HttpStatusCode.OK);
        // response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        // var content = await response.Content.ReadAsByteArrayAsync();
        // content.Should().NotBeEmpty();
    }
}
