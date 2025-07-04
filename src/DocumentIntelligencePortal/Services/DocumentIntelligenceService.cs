using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using DocumentIntelligencePortal.Models;
using DocumentAnalysisResult = DocumentIntelligencePortal.Models.DocumentAnalysisResult;
using DocumentPage = DocumentIntelligencePortal.Models.DocumentPage;
using DocumentLine = DocumentIntelligencePortal.Models.DocumentLine;
using DocumentTable = DocumentIntelligencePortal.Models.DocumentTable;
using DocumentTableCell = DocumentIntelligencePortal.Models.DocumentTableCell;
using DocumentKeyValuePair = DocumentIntelligencePortal.Models.DocumentKeyValuePair;

namespace DocumentIntelligencePortal.Services;

/// <summary>
/// Service for interacting with Azure Document Intelligence using managed identity
/// </summary>
public interface IDocumentIntelligenceService
{
    Task<AnalyzeDocumentResponse> AnalyzeDocumentAsync(AnalyzeDocumentRequest request);
    Task<AnalyzeDocumentResponse> AnalyzeDocumentFromStreamAsync(Stream documentStream, AnalyzeDocumentStreamRequest request);
    Task<AnalyzeDocumentResponse> AnalyzeDocumentFromStorageAsync(AnalyzeDocumentFromStorageRequest request);
    Task<DocumentAnalysisResult?> GetAnalysisResultAsync(string operationId);
    Task<List<string>> GetAvailableModelsAsync();
}

public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly DocumentAnalysisClient _documentAnalysisClient;
    private readonly ILogger<DocumentIntelligenceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAzureStorageService _storageService;

    public DocumentIntelligenceService(
        ILogger<DocumentIntelligenceService> logger, 
        IConfiguration configuration,
        IAzureStorageService storageService,
        IAzureCredentialProvider credentialProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _storageService = storageService;

        // Get the Document Intelligence endpoint from configuration
        var endpoint = _configuration["Azure:DocumentIntelligence:Endpoint"];
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("Azure:DocumentIntelligence:Endpoint configuration is missing");
        }

        // Use credential provider for authentication
        var credential = credentialProvider.GetCredential();
        _documentAnalysisClient = new DocumentAnalysisClient(new Uri(endpoint), credential);

        _logger.LogInformation("Document Intelligence Service initialized with endpoint: {Endpoint}", endpoint);
    }

    /// <summary>
    /// Analyzes a document using Azure Document Intelligence
    /// </summary>
    public async Task<AnalyzeDocumentResponse> AnalyzeDocumentAsync(AnalyzeDocumentRequest request)
    {
        try
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(request.BlobUri))
            {
                return new AnalyzeDocumentResponse
                {
                    Success = false,
                    Message = "Blob URI is required"
                };
            }

            _logger.LogInformation("Starting document analysis for: {BlobUri} with model: {ModelId}", 
                request.BlobUri, request.ModelId);

            // Start the analysis operation
            var operation = await _documentAnalysisClient.AnalyzeDocumentFromUriAsync(
                WaitUntil.Completed,
                request.ModelId,
                new Uri(request.BlobUri));

            if (operation.HasCompleted && operation.HasValue)
            {
                var result = operation.Value;
                var analysisResult = MapToDocumentAnalysisResult(result, request.BlobUri, request.ModelId);

                _logger.LogInformation("Document analysis completed successfully for: {BlobUri}", request.BlobUri);

                return new AnalyzeDocumentResponse
                {
                    Success = true,
                    OperationId = operation.Id,
                    Result = analysisResult,
                    Message = "Analysis completed successfully"
                };
            }
            else
            {
                _logger.LogWarning("Document analysis did not complete for: {BlobUri}", request.BlobUri);
                return new AnalyzeDocumentResponse
                {
                    Success = false,
                    Message = "Analysis did not complete successfully"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze document: {BlobUri}", request.BlobUri);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Analyzes a document from a stream using Azure Document Intelligence
    /// </summary>
    public async Task<AnalyzeDocumentResponse> AnalyzeDocumentFromStreamAsync(Stream documentStream, AnalyzeDocumentStreamRequest request)
    {
        try
        {
            _logger.LogInformation("Starting document analysis from stream for file: {FileName} with model: {ModelId}", 
                request.FileName, request.ModelId);

            if (documentStream == null || documentStream.Length == 0)
            {
                return new AnalyzeDocumentResponse
                {
                    Success = false,
                    Message = "Document stream is required"
                };
            }

            // Azure Document Intelligence requires a seekable stream
            // If the stream is not seekable, copy it to a MemoryStream
            Stream workingStream;
            bool shouldDisposeWorkingStream = false;

            if (documentStream.CanSeek)
            {
                // Stream is already seekable, use it directly
                workingStream = documentStream;
                workingStream.Position = 0;
            }
            else
            {
                // Stream is not seekable, copy to MemoryStream
                _logger.LogInformation("Stream is not seekable, copying to memory for analysis: {FileName}", request.FileName);
                
                workingStream = new MemoryStream();
                shouldDisposeWorkingStream = true;
                
                try
                {
                    await documentStream.CopyToAsync(workingStream);
                    workingStream.Position = 0;
                    
                    _logger.LogInformation("Stream copied to memory successfully. Size: {StreamSize} bytes", workingStream.Length);
                }
                catch (Exception ex)
                {
                    workingStream.Dispose();
                    _logger.LogError(ex, "Failed to copy stream to memory for: {FileName}", request.FileName);
                    throw;
                }
            }

            try
            {
                // Configure retry options for resilient operation
                var analyzeOptions = new AnalyzeDocumentOptions
                {
                    Features = { DocumentAnalysisFeature.OcrHighResolution }
                };

                // Start the analysis operation with retry logic
                Operation<AnalyzeResult>? operation = null;
                int maxRetries = 3;
                int retryCount = 0;
                TimeSpan delay = TimeSpan.FromSeconds(1);

                while (retryCount < maxRetries)
                {
                    try
                    {
                        // Reset stream position for each attempt
                        workingStream.Position = 0;

                        operation = await _documentAnalysisClient.AnalyzeDocumentAsync(
                            WaitUntil.Completed,
                            request.ModelId,
                            workingStream,
                            options: analyzeOptions);
                        break;
                    }
                    catch (RequestFailedException ex) when (ex.Status == 429 && retryCount < maxRetries - 1)
                    {
                        _logger.LogWarning("Rate limit hit, retrying in {Delay}ms. Attempt {RetryCount}/{MaxRetries}", 
                            delay.TotalMilliseconds, retryCount + 1, maxRetries);
                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                        retryCount++;
                    }
                    catch
                    {
                        throw; // Re-throw non-retryable exceptions
                    }
                }

                // This should not happen if retry logic works correctly
                if (operation == null)
                {
                    throw new InvalidOperationException("Failed to initialize document analysis operation");
                }

                if (operation.HasCompleted && operation.HasValue)
                {
                    var result = operation.Value;
                    var analysisResult = MapToDocumentAnalysisResult(result, request.FileName, request.ModelId);

                    _logger.LogInformation("Document analysis completed successfully for file: {FileName}", request.FileName);

                    return new AnalyzeDocumentResponse
                    {
                        Success = true,
                        OperationId = operation.Id,
                        Result = analysisResult,
                        Message = "Analysis completed successfully"
                    };
                }
                else
                {
                    _logger.LogWarning("Document analysis did not complete for file: {FileName}", request.FileName);
                    return new AnalyzeDocumentResponse
                    {
                        Success = false,
                        Message = "Analysis did not complete successfully"
                    };
                }
            }
            finally
            {
                // Dispose the working stream if we created a copy
                if (shouldDisposeWorkingStream && workingStream != null)
                {
                    workingStream.Dispose();
                }
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 400)
        {
            _logger.LogError(ex, "Bad request when analyzing document: {FileName}. Check file format and model compatibility", request.FileName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = $"Invalid document format or model incompatibility: {ex.Message}"
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            _logger.LogWarning(ex, "Rate limit exceeded when analyzing document: {FileName}", request.FileName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = "Service is currently busy. Please try again later."
            };
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "Document too large to process in memory: {FileName}", request.FileName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = "Document is too large to process. Please use a smaller file."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze document from stream: {FileName}", request.FileName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = $"Analysis failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets the analysis result for a specific operation
    /// </summary>
    public Task<DocumentAnalysisResult?> GetAnalysisResultAsync(string operationId)
    {
        // TODO: Implement operation result tracking
        // This would require storing operation IDs and their status
        // For now, return null as operations are completed synchronously
        _logger.LogWarning("GetAnalysisResultAsync not fully implemented - operation tracking needed");
        return Task.FromResult<DocumentAnalysisResult?>(null);
    }

    /// <summary>
    /// Gets a list of available Document Intelligence models
    /// </summary>
    public Task<List<string>> GetAvailableModelsAsync()
    {
        // Return common prebuilt models - in a real implementation,
        // you might want to call the Document Intelligence service to get available models
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

        _logger.LogInformation("Retrieved {ModelCount} available models", models.Count);
        return Task.FromResult(models);
    }

    /// <summary>
    /// Maps Azure SDK result to our domain model
    /// </summary>
    private static DocumentAnalysisResult MapToDocumentAnalysisResult(
        AnalyzeResult azureResult, 
        string documentId, 
        string modelId)
    {
        var result = new DocumentAnalysisResult
        {
            DocumentId = documentId,
            ModelId = modelId,
            Status = "Completed",
            AnalyzedAt = DateTime.UtcNow,
            Content = azureResult.Content
        };

        // Map pages
        foreach (var page in azureResult.Pages)
        {
            var documentPage = new DocumentPage
            {
                PageNumber = page.PageNumber,
                Width = page.Width ?? 0,
                Height = page.Height ?? 0,
                Unit = page.Unit?.ToString() ?? "pixel",
                Angle = page.Angle ?? 0
            };

            // Map lines
            if (page.Lines != null)
            {
                foreach (var line in page.Lines)
                {
                    var documentLine = new DocumentLine
                    {
                        Content = line.Content,
                        BoundingBox = new BoundingBox
                        {
                            Points = new List<float>() // Simplified - would need proper bounding box extraction
                        }
                    };
                    documentPage.Lines.Add(documentLine);
                }
            }

            result.Pages.Add(documentPage);
        }

        // Map tables
        if (azureResult.Tables != null)
        {
            foreach (var table in azureResult.Tables)
            {
                var documentTable = new DocumentTable
                {
                    RowCount = table.RowCount,
                    ColumnCount = table.ColumnCount
                };

                foreach (var cell in table.Cells)
                {
                    var documentCell = new DocumentTableCell
                    {
                        RowIndex = cell.RowIndex,
                        ColumnIndex = cell.ColumnIndex,
                        RowSpan = cell.RowSpan,
                        ColumnSpan = cell.ColumnSpan,
                        Content = cell.Content,
                        BoundingBox = new BoundingBox
                        {
                            Points = new List<float>() // Simplified - would need proper bounding box extraction
                        }
                    };
                    documentTable.Cells.Add(documentCell);
                }

                result.Tables.Add(documentTable);
            }
        }

        // Map key-value pairs
        if (azureResult.KeyValuePairs != null)
        {
            foreach (var kvp in azureResult.KeyValuePairs)
            {
                var documentKvp = new DocumentKeyValuePair
                {
                    Key = kvp.Key?.Content ?? "",
                    Value = kvp.Value?.Content ?? "",
                    Confidence = kvp.Confidence
                };
                result.KeyValuePairs.Add(documentKvp);
            }
        }

        // Note: Entities extraction would require specific models that support entity extraction
        // This is commented out as not all models support entities
        /*
        if (azureResult.Entities != null)
        {
            foreach (var entity in azureResult.Entities)
            {
                var documentEntity = new DocumentEntity
                {
                    Category = entity.Category?.ToString() ?? "",
                    SubCategory = entity.SubCategory ?? "",
                    Content = entity.Content,
                    Confidence = entity.Confidence,
                    BoundingBox = new BoundingBox
                    {
                        Points = new List<float>()
                    }
                };
                result.Entities.Add(documentEntity);
            }
        }
        */

        return result;
    }

    /// <summary>
    /// Analyzes a document from Azure Storage by streaming the content
    /// </summary>
    public async Task<AnalyzeDocumentResponse> AnalyzeDocumentFromStorageAsync(AnalyzeDocumentFromStorageRequest request)
    {
        try
        {
            _logger.LogInformation("Starting document analysis from storage for: {ContainerName}/{BlobName} with model: {ModelId}", 
                request.ContainerName, request.BlobName, request.ModelId);

            // Validate input parameters
            if (string.IsNullOrWhiteSpace(request.ContainerName) || string.IsNullOrWhiteSpace(request.BlobName))
            {
                return new AnalyzeDocumentResponse
                {
                    Success = false,
                    Message = "Container name and blob name are required"
                };
            }

            // Get the document stream from storage (already seekable MemoryStream)
            using var documentStream = await _storageService.GetDocumentStreamAsync(request.ContainerName, request.BlobName);
            
            if (documentStream == null)
            {
                _logger.LogWarning("Document not found in storage: {ContainerName}/{BlobName}", 
                    request.ContainerName, request.BlobName);
                return new AnalyzeDocumentResponse
                {
                    Success = false,
                    Message = $"Document not found: {request.ContainerName}/{request.BlobName}"
                };
            }

            _logger.LogInformation("Document stream obtained successfully. Size: {StreamSize} bytes", documentStream.Length);

            // Implement retry logic for Azure Document Intelligence
            const int maxRetries = 3;
            var retryCount = 0;
            Operation<AnalyzeResult>? operation = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation("Attempting document analysis (attempt {RetryCount}/{MaxRetries}) for: {ContainerName}/{BlobName}", 
                        retryCount + 1, maxRetries, request.ContainerName, request.BlobName);

                    // Reset stream position for retry
                    documentStream.Position = 0;

                    // Create analyze options
                    var analyzeOptions = new AnalyzeDocumentOptions();
                    if (request.IncludeFieldElements)
                    {
                        analyzeOptions.Features.Add(DocumentAnalysisFeature.OcrHighResolution);
                    }

                    // Start the analysis operation
                    operation = await _documentAnalysisClient.AnalyzeDocumentAsync(
                        WaitUntil.Completed,
                        request.ModelId,
                        documentStream,
                        options: analyzeOptions);

                    break; // Success, exit retry loop
                }
                catch (RequestFailedException ex) when (ex.Status == 429 && retryCount < maxRetries - 1)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
                    _logger.LogWarning(ex, "Rate limit hit, retrying in {Delay} seconds (attempt {RetryCount}/{MaxRetries})", 
                        delay.TotalSeconds, retryCount, maxRetries);
                    await Task.Delay(delay);
                }
                catch (Exception ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    _logger.LogWarning(ex, "Analysis failed, retrying in {Delay} seconds (attempt {RetryCount}/{MaxRetries}): {ErrorMessage}", 
                        delay.TotalSeconds, retryCount, maxRetries, ex.Message);
                    await Task.Delay(delay);
                }
            }

            if (operation == null)
            {
                _logger.LogError("Document analysis failed after {MaxRetries} attempts for: {ContainerName}/{BlobName}", 
                    maxRetries, request.ContainerName, request.BlobName);
                return new AnalyzeDocumentResponse
                {
                    Success = false,
                    Message = $"Analysis failed after {maxRetries} attempts"
                };
            }

            if (operation.HasCompleted && operation.HasValue)
            {
                var result = operation.Value;
                var analysisResult = MapToDocumentAnalysisResult(result, $"{request.ContainerName}/{request.BlobName}", request.ModelId);

                _logger.LogInformation("Document analysis completed successfully for: {ContainerName}/{BlobName}", 
                    request.ContainerName, request.BlobName);

                return new AnalyzeDocumentResponse
                {
                    Success = true,
                    OperationId = operation.Id,
                    Result = analysisResult,
                    Message = "Analysis completed successfully"
                };
            }
            else
            {
                _logger.LogWarning("Document analysis did not complete for: {ContainerName}/{BlobName}", 
                    request.ContainerName, request.BlobName);
                return new AnalyzeDocumentResponse
                {
                    Success = false,
                    Message = "Analysis did not complete successfully"
                };
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 400)
        {
            _logger.LogError(ex, "Bad request when analyzing document: {ContainerName}/{BlobName}. Check file format and model compatibility", 
                request.ContainerName, request.BlobName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = $"Invalid document format or model incompatibility: {ex.Message}"
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            _logger.LogWarning(ex, "Rate limit exceeded when analyzing document: {ContainerName}/{BlobName}", 
                request.ContainerName, request.BlobName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = "Service is currently busy. Please try again later."
            };
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "Document too large to process in memory: {ContainerName}/{BlobName}", 
                request.ContainerName, request.BlobName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = "Document is too large to process. Please use a smaller file."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze document from storage: {ContainerName}/{BlobName}", 
                request.ContainerName, request.BlobName);
            return new AnalyzeDocumentResponse
            {
                Success = false,
                Message = $"Analysis failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Determines content type from file extension
    /// </summary>
    private static string GetContentTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tiff" or ".tif" => "image/tiff",
            ".bmp" => "image/bmp",
            ".heic" => "image/heic",
            _ => "application/octet-stream"
        };
    }
}