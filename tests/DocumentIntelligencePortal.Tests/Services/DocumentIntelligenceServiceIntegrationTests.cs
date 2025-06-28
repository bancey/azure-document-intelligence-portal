using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentIntelligencePortal.Tests.Services;

/// <summary>
/// Integration tests for DocumentIntelligenceService
/// These tests verify the integration with mocked Azure Document Intelligence service
/// </summary>
public class DocumentIntelligenceServiceIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public DocumentIntelligenceServiceIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AnalyzeDocumentAsync_WithMockedService_ShouldAnalyzeDocument()
    {
        // Arrange
        var mockStorageService = new Mock<IAzureStorageService>();
        var service = CreateMockedDocumentIntelligenceService(mockStorageService.Object);
        var request = TestDataFactory.CreateAnalyzeDocumentRequest();

        // Act
        var result = await service.AnalyzeDocumentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result!.ModelId.Should().Be(request.ModelId);
        result.Result.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WithMockedService_ShouldReturnModels()
    {
        // Arrange
        var mockStorageService = new Mock<IAzureStorageService>();
        var service = CreateMockedDocumentIntelligenceService(mockStorageService.Object);

        // Act
        var models = await service.GetAvailableModelsAsync();

        // Assert
        models.Should().NotBeNull();
        models.Should().NotBeEmpty();
        models.Should().Contain("prebuilt-document");
        models.Should().Contain("prebuilt-layout");
        models.Should().Contain("prebuilt-invoice");
    }

    [Fact]
    public async Task AnalyzeDocumentFromStreamAsync_WithValidStream_ShouldReturnSuccess()
    {
        // Arrange
        var mockStorageService = new Mock<IAzureStorageService>();
        var service = CreateMockedDocumentIntelligenceService(mockStorageService.Object);
        var request = TestDataFactory.CreateAnalyzeDocumentStreamRequest();
        
        using var stream = TestDataFactory.CreateTestDocumentStream();

        // Act
        var result = await service.AnalyzeDocumentFromStreamAsync(stream, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result!.ModelId.Should().Be(request.ModelId);
    }

    [Fact]
    public async Task AnalyzeDocumentFromStorageAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var mockStorageService = new Mock<IAzureStorageService>();
        var testStream = TestDataFactory.CreateTestDocumentStream("Mock PDF content for testing");
        
        mockStorageService
            .Setup(x => x.GetDocumentStreamAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testStream);

        var service = CreateMockedDocumentIntelligenceService(mockStorageService.Object);
        var request = TestDataFactory.CreateAnalyzeDocumentFromStorageRequest();

        // Act
        var result = await service.AnalyzeDocumentFromStorageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result!.ModelId.Should().Be(request.ModelId);
        
        mockStorageService.Verify(x => x.GetDocumentStreamAsync(request.ContainerName, request.BlobName), Times.Once);
    }

    [Fact]
    public async Task AnalyzeDocumentFromStorageAsync_WithMissingDocument_ShouldReturnFailure()
    {
        // Arrange
        var mockStorageService = new Mock<IAzureStorageService>();
        
        mockStorageService
            .Setup(x => x.GetDocumentStreamAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream?)null);

        var service = CreateMockedDocumentIntelligenceService(mockStorageService.Object);
        var request = TestDataFactory.CreateAnalyzeDocumentFromStorageRequest();

        // Act
        var result = await service.AnalyzeDocumentFromStorageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Document not found");
    }

    private IDocumentIntelligenceService CreateMockedDocumentIntelligenceService(IAzureStorageService storageService)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:DocumentIntelligence:Endpoint"] = "https://test-document-intelligence.cognitiveservices.azure.com/"
            })
            .Build();

        var logger = new Mock<ILogger<DocumentIntelligenceService>>();
        
        // Return a mock service that doesn't require real Azure credentials
        return new MockDocumentIntelligenceService(logger.Object, configuration, storageService);
    }
}

/// <summary>
/// Mock implementation of DocumentIntelligenceService for testing purposes
/// This avoids the need for actual Azure Document Intelligence service during tests
/// </summary>
internal class MockDocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly ILogger<DocumentIntelligenceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAzureStorageService _storageService;

    public MockDocumentIntelligenceService(
        ILogger<DocumentIntelligenceService> logger,
        IConfiguration configuration,
        IAzureStorageService storageService)
    {
        _logger = logger;
        _configuration = configuration;
        _storageService = storageService;
    }

    public Task<AnalyzeDocumentResponse> AnalyzeDocumentAsync(AnalyzeDocumentRequest request)
    {
        _logger.LogInformation("Mock analysis for document: {BlobUri}", request.BlobUri);
        
        if (string.IsNullOrWhiteSpace(request.BlobUri))
        {
            return Task.FromResult(new AnalyzeDocumentResponse
            {
                Success = false,
                Message = "Blob URI is required"
            });
        }

        var result = CreateMockAnalysisResult(request.BlobUri, request.ModelId);
        
        return Task.FromResult(new AnalyzeDocumentResponse
        {
            Success = true,
            OperationId = Guid.NewGuid().ToString(),
            Result = result,
            Message = "Mock analysis completed successfully"
        });
    }

    public Task<AnalyzeDocumentResponse> AnalyzeDocumentFromStreamAsync(Stream documentStream, AnalyzeDocumentStreamRequest request)
    {
        _logger.LogInformation("Mock analysis from stream for file: {FileName}", request.FileName);
        
        if (documentStream == null || documentStream.Length == 0)
        {
            return Task.FromResult(new AnalyzeDocumentResponse
            {
                Success = false,
                Message = "Document stream is required"
            });
        }

        var result = CreateMockAnalysisResult(request.FileName, request.ModelId);
        
        return Task.FromResult(new AnalyzeDocumentResponse
        {
            Success = true,
            OperationId = Guid.NewGuid().ToString(),
            Result = result,
            Message = "Mock analysis completed successfully"
        });
    }

    public async Task<AnalyzeDocumentResponse> AnalyzeDocumentFromStorageAsync(AnalyzeDocumentFromStorageRequest request)
    {
        _logger.LogInformation("Mock analysis from storage: {ContainerName}/{BlobName}", 
            request.ContainerName, request.BlobName);
        
        if (string.IsNullOrWhiteSpace(request.ContainerName) || string.IsNullOrWhiteSpace(request.BlobName))
        {
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = "Container name and blob name are required"
            };
        }

        // Use the storage service to check if document exists
        using var documentStream = await _storageService.GetDocumentStreamAsync(request.ContainerName, request.BlobName);
        
        if (documentStream == null)
        {
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = $"Document not found: {request.ContainerName}/{request.BlobName}"
            };
        }

        var result = CreateMockAnalysisResult($"{request.ContainerName}/{request.BlobName}", request.ModelId);
        
        return new AnalyzeDocumentResponse
        {
            Success = true,
            OperationId = Guid.NewGuid().ToString(),
            Result = result,
            Message = "Mock analysis completed successfully"
        };
    }

    public Task<DocumentAnalysisResult?> GetAnalysisResultAsync(string operationId)
    {
        _logger.LogInformation("Mock get analysis result for operation: {OperationId}", operationId);
        // Return null as operations are completed synchronously in mock
        return Task.FromResult<DocumentAnalysisResult?>(null);
    }

    public Task<List<string>> GetAvailableModelsAsync()
    {
        var models = new List<string>
        {
            "prebuilt-document",
            "prebuilt-read",
            "prebuilt-layout",
            "prebuilt-invoice",
            "prebuilt-receipt",
            "prebuilt-idDocument",
            "prebuilt-businessCard",
            "prebuilt-tax.us.w2"
        };

        _logger.LogInformation("Mock retrieved {ModelCount} available models", models.Count);
        return Task.FromResult(models);
    }

    private static DocumentAnalysisResult CreateMockAnalysisResult(string documentId, string modelId)
    {
        return new DocumentAnalysisResult
        {
            DocumentId = documentId,
            ModelId = modelId,
            Status = "Completed",
            AnalyzedAt = DateTime.UtcNow,
            Content = "Mock analyzed content from the document. This is sample text extracted by the mock Document Intelligence service.",
            Pages = new List<DocumentIntelligencePortal.Models.DocumentPage>
            {
                new()
                {
                    PageNumber = 1,
                    Width = 612,
                    Height = 792,
                    Unit = "pixel",
                    Angle = 0,
                    Lines = new List<DocumentIntelligencePortal.Models.DocumentLine>
                    {
                        new()
                        {
                            Content = "Mock line 1 content",
                            BoundingBox = new BoundingBox 
                            { 
                                Points = new List<float> { 100, 100, 300, 100, 300, 120, 100, 120 } 
                            }
                        },
                        new()
                        {
                            Content = "Mock line 2 content",
                            BoundingBox = new BoundingBox 
                            { 
                                Points = new List<float> { 100, 130, 280, 130, 280, 150, 100, 150 } 
                            }
                        }
                    }
                }
            },
            Tables = new List<DocumentIntelligencePortal.Models.DocumentTable>
            {
                new()
                {
                    RowCount = 2,
                    ColumnCount = 2,
                    Cells = new List<DocumentIntelligencePortal.Models.DocumentTableCell>
                    {
                        new()
                        {
                            Content = "Header 1",
                            RowIndex = 0,
                            ColumnIndex = 0,
                            RowSpan = 1,
                            ColumnSpan = 1,
                            BoundingBox = new BoundingBox 
                            { 
                                Points = new List<float> { 100, 200, 200, 200, 200, 220, 100, 220 } 
                            }
                        },
                        new()
                        {
                            Content = "Header 2",
                            RowIndex = 0,
                            ColumnIndex = 1,
                            RowSpan = 1,
                            ColumnSpan = 1,
                            BoundingBox = new BoundingBox 
                            { 
                                Points = new List<float> { 200, 200, 300, 200, 300, 220, 200, 220 } 
                            }
                        }
                    }
                }
            },
            KeyValuePairs = new List<DocumentIntelligencePortal.Models.DocumentKeyValuePair>
            {
                new()
                {
                    Key = "Invoice Number",
                    Value = "MOCK-12345",
                    Confidence = 0.95f
                },
                new()
                {
                    Key = "Date",
                    Value = DateTime.Now.ToString("yyyy-MM-dd"),
                    Confidence = 0.88f
                }
            }
        };
    }
}
