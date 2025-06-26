using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentIntelligencePortal.Tests.Fixtures;

/// <summary>
/// Base test fixture providing common test setup and utilities
/// </summary>
public class TestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }
    public Mock<ILogger<T>> CreateMockLogger<T>() => new Mock<ILogger<T>>();

    public TestFixture()
    {
        var services = new ServiceCollection();
        
        // Configure test configuration
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:DocumentIntelligence:Endpoint"] = "https://test-document-intelligence.cognitiveservices.azure.com/",
                ["Azure:StorageAccountName"] = "teststorageaccount",
                ["Logging:LogLevel:Default"] = "Debug"
            });
        
        Configuration = configurationBuilder.Build();
        services.AddSingleton(Configuration);
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Test data factory for creating test objects
/// </summary>
public static class TestDataFactory
{
    public static AnalyzeDocumentRequest CreateAnalyzeDocumentRequest(
        string? blobUri = "UNSET",
        string? modelId = null)
    {
        return new AnalyzeDocumentRequest
        {
            BlobUri = blobUri == "UNSET" ? "https://teststorage.blob.core.windows.net/test-container/test-document.pdf" : blobUri!,
            ModelId = modelId ?? "prebuilt-document",
            IncludeFieldElements = true
        };
    }

    public static AnalyzeDocumentStreamRequest CreateAnalyzeDocumentStreamRequest(
        string? fileName = null,
        string? modelId = null)
    {
        return new AnalyzeDocumentStreamRequest
        {
            FileName = fileName ?? "test-document.pdf",
            ModelId = modelId ?? "prebuilt-document",
            IncludeFieldElements = true,
            ContentType = "application/pdf"
        };
    }

    public static AnalyzeDocumentFromStorageRequest CreateAnalyzeDocumentFromStorageRequest(
        string? containerName = "UNSET",
        string? blobName = "UNSET",
        string? modelId = null)
    {
        return new AnalyzeDocumentFromStorageRequest
        {
            ContainerName = containerName == "UNSET" ? "test-container" : containerName!,
            BlobName = blobName == "UNSET" ? "test-document.pdf" : blobName!,
            ModelId = modelId ?? "prebuilt-document",
            IncludeFieldElements = true
        };
    }

    public static StorageDocument CreateStorageDocument(
        string? name = null,
        string? blobUri = null,
        long? size = null)
    {
        return new StorageDocument
        {
            Name = name ?? "test-document.pdf",
            BlobUri = blobUri ?? "https://teststorage.blob.core.windows.net/test-container/test-document.pdf",
            Size = size ?? 1024,
            ContentType = "application/pdf",
            LastModified = DateTimeOffset.UtcNow,
            Container = "test-container"
        };
    }

    public static DocumentAnalysisResult CreateDocumentAnalysisResult(
        string? modelId = null,
        string? documentUri = null)
    {
        return new DocumentAnalysisResult
        {
            ModelId = modelId ?? "prebuilt-document",
            Content = "Test document content",
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
                            Content = "Test line content",
                            BoundingBox = new BoundingBox 
                            { 
                                Points = new List<float> { 100, 100, 200, 100, 200, 120, 100, 120 } 
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
                                Points = new List<float> { 100, 100, 200, 100, 200, 120, 100, 120 } 
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
                    Value = "INV-12345",
                    Confidence = 0.95f
                }
            }
        };
    }

    public static Stream CreateTestDocumentStream(string content = "Test PDF content")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}
