namespace DocumentIntelligencePortal.Tests.Models;

/// <summary>
/// Unit tests for API model classes
/// Verifies model properties, validation, and serialization behavior
/// </summary>
public class ApiModelsTests
{
    [Fact]
    public void AnalyzeDocumentRequest_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var request = new AnalyzeDocumentRequest();

        // Assert
        request.BlobUri.Should().Be(string.Empty);
        request.ModelId.Should().Be("prebuilt-document");
        request.IncludeFieldElements.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeDocumentRequest_PropertyAssignment_ShouldWork()
    {
        // Arrange
        var blobUri = "https://storage.blob.core.windows.net/container/document.pdf";
        var modelId = "prebuilt-invoice";

        // Act
        var request = new AnalyzeDocumentRequest
        {
            BlobUri = blobUri,
            ModelId = modelId,
            IncludeFieldElements = false
        };

        // Assert
        request.BlobUri.Should().Be(blobUri);
        request.ModelId.Should().Be(modelId);
        request.IncludeFieldElements.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeDocumentStreamRequest_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var request = new AnalyzeDocumentStreamRequest();

        // Assert
        request.FileName.Should().Be(string.Empty);
        request.ModelId.Should().Be("prebuilt-document");
        request.IncludeFieldElements.Should().BeTrue();
        request.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public void AnalyzeDocumentFromStorageRequest_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var request = new AnalyzeDocumentFromStorageRequest();

        // Assert
        request.ContainerName.Should().Be(string.Empty);
        request.BlobName.Should().Be(string.Empty);
        request.ModelId.Should().Be("prebuilt-document");
        request.IncludeFieldElements.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeDocumentResponse_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var response = new AnalyzeDocumentResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.OperationId.Should().BeNull();
        response.Message.Should().BeNull();
        response.Result.Should().BeNull();
    }

    [Fact]
    public void AnalyzeDocumentResponse_SuccessfulResponse_ShouldContainResult()
    {
        // Arrange
        var operationId = "operation-123";
        var message = "Analysis completed successfully";
        var result = TestDataFactory.CreateDocumentAnalysisResult();

        // Act
        var response = new AnalyzeDocumentResponse
        {
            Success = true,
            OperationId = operationId,
            Message = message,
            Result = result
        };

        // Assert
        response.Success.Should().BeTrue();
        response.OperationId.Should().Be(operationId);
        response.Message.Should().Be(message);
        response.Result.Should().NotBeNull();
        response.Result.Should().Be(result);
    }

    [Fact]
    public void ListContainersResponse_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var response = new ListContainersResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.Containers.Should().NotBeNull();
        response.Containers.Should().BeEmpty();
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ListContainersResponse_WithContainers_ShouldContainAllContainers()
    {
        // Arrange
        var containers = new List<string> { "container1", "container2", "documents" };

        // Act
        var response = new ListContainersResponse
        {
            Success = true,
            Containers = containers
        };

        // Assert
        response.Success.Should().BeTrue();
        response.Containers.Should().HaveCount(3);
        response.Containers.Should().BeEquivalentTo(containers);
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ListDocumentsResponse_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var response = new ListDocumentsResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.Documents.Should().NotBeNull();
        response.Documents.Should().BeEmpty();
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ListDocumentsResponse_WithDocuments_ShouldContainAllDocuments()
    {
        // Arrange
        var documents = new List<StorageDocument>
        {
            TestDataFactory.CreateStorageDocument("document1.pdf"),
            TestDataFactory.CreateStorageDocument("document2.pdf")
        };

        // Act
        var response = new ListDocumentsResponse
        {
            Success = true,
            Documents = documents
        };

        // Assert
        response.Success.Should().BeTrue();
        response.Documents.Should().HaveCount(2);
        response.Documents.Should().BeEquivalentTo(documents);
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SearchDocumentsResponse_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var response = new SearchDocumentsResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.Documents.Should().NotBeNull();
        response.Documents.Should().BeEmpty();
        response.SearchTerm.Should().Be(string.Empty);
        response.TotalMatches.Should().Be(0);
        response.MaxResults.Should().Be(0);
        response.HasMoreResults.Should().BeFalse();
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SearchDocumentsResponse_WithSearchResults_ShouldContainAllProperties()
    {
        // Arrange
        var documents = new List<StorageDocument>
        {
            TestDataFactory.CreateStorageDocument("invoice1.pdf"),
            TestDataFactory.CreateStorageDocument("invoice2.pdf")
        };
        var searchTerm = "invoice*";

        // Act
        var response = new SearchDocumentsResponse
        {
            Success = true,
            Documents = documents,
            SearchTerm = searchTerm,
            TotalMatches = 5,
            MaxResults = 2,
            HasMoreResults = true
        };

        // Assert
        response.Success.Should().BeTrue();
        response.Documents.Should().HaveCount(2);
        response.SearchTerm.Should().Be(searchTerm);
        response.TotalMatches.Should().Be(5);
        response.MaxResults.Should().Be(2);
        response.HasMoreResults.Should().BeTrue();
        response.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("prebuilt-document")]
    [InlineData("prebuilt-invoice")]
    [InlineData("prebuilt-receipt")]
    [InlineData("custom-model-123")]
    public void AnalyzeDocumentRequest_ModelIdVariations_ShouldBeAccepted(string modelId)
    {
        // Arrange & Act
        var request = new AnalyzeDocumentRequest
        {
            ModelId = modelId
        };

        // Assert
        request.ModelId.Should().Be(modelId);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/tiff")]
    public void AnalyzeDocumentStreamRequest_ContentTypeVariations_ShouldBeAccepted(string contentType)
    {
        // Arrange & Act
        var request = new AnalyzeDocumentStreamRequest
        {
            ContentType = contentType
        };

        // Assert
        request.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void ApiModels_ShouldSupportNullableProperties()
    {
        // Arrange & Act
        var response = new AnalyzeDocumentResponse
        {
            OperationId = null,
            Message = null,
            Result = null
        };

        // Assert
        response.OperationId.Should().BeNull();
        response.Message.Should().BeNull();
        response.Result.Should().BeNull();
    }

    [Fact]
    public void ListContainersResponse_ErrorScenario_ShouldHandleErrorMessage()
    {
        // Arrange
        var errorMessage = "Failed to connect to storage account";

        // Act
        var response = new ListContainersResponse
        {
            Success = false,
            ErrorMessage = errorMessage
        };

        // Assert
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be(errorMessage);
        response.Containers.Should().BeEmpty();
    }
}
