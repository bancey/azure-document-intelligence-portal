using Microsoft.AspNetCore.Mvc;
using DocumentIntelligencePortal.Models;
using DocumentIntelligencePortal.Services;

namespace DocumentIntelligencePortal.Controllers;

/// <summary>
/// Controller for document analysis operations using Azure Document Intelligence
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentAnalysisController : ControllerBase
{
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly IAzureStorageService _storageService;
    private readonly ILogger<DocumentAnalysisController> _logger;

    public DocumentAnalysisController(
        IDocumentIntelligenceService documentIntelligenceService,
        IAzureStorageService storageService,
        ILogger<DocumentAnalysisController> logger)
    {
        _documentIntelligenceService = documentIntelligenceService;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a document from Azure Storage using Document Intelligence
    /// </summary>
    /// <param name="request">Analysis request containing blob URI and model ID</param>
    /// <returns>Analysis result</returns>
    [HttpPost("analyze")]
    public async Task<ActionResult<AnalyzeDocumentResponse>> AnalyzeDocument([FromBody] AnalyzeDocumentRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.BlobUri))
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = "Blob URI is required" 
                });
            }

            _logger.LogInformation("Analyzing document: {BlobUri} with model: {ModelId}", 
                request.BlobUri, request.ModelId);

            var result = await _documentIntelligenceService.AnalyzeDocumentAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document: {BlobUri}", request is null ? "null" : request.BlobUri);
            return StatusCode(500, new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = "Internal server error" 
            });
        }
    }

    /// <summary>
    /// Analyzes a document by container and blob name
    /// </summary>
    /// <param name="containerName">Name of the storage container</param>
    /// <param name="blobName">Name of the blob/document</param>
    /// <param name="modelId">Document Intelligence model to use (optional, defaults to prebuilt-document)</param>
    /// <returns>Analysis result</returns>
    [HttpPost("analyze/{containerName}/{blobName}")]
    public async Task<ActionResult<AnalyzeDocumentResponse>> AnalyzeDocumentByPath(
        string containerName, 
        string blobName, 
        [FromQuery] string modelId = "prebuilt-document")
    {
        try
        {
            _logger.LogInformation("Analyzing document: {Container}/{Blob} with model: {ModelId}", 
                containerName, blobName, modelId);

            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName))
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = "Container name and blob name are required" 
                });
            }

            // Get the blob URI from the storage service
            var blobUri = await _storageService.GetDocumentSasUriAsync(containerName, blobName);
            
            var request = new AnalyzeDocumentRequest
            {
                BlobUri = blobUri,
                ModelId = modelId,
                IncludeFieldElements = true
            };

            var result = await _documentIntelligenceService.AnalyzeDocumentAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Document not found: {Container}/{Blob}", containerName, blobName);
            return NotFound(new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = $"Document not found: {containerName}/{blobName}" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document: {Container}/{Blob}", containerName, blobName);
            return StatusCode(500, new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = "Internal server error" 
            });
        }
    }

    /// <summary>
    /// Gets the analysis result for a specific operation
    /// </summary>
    /// <param name="operationId">Operation ID from a previous analysis request</param>
    /// <returns>Analysis result if available</returns>
    [HttpGet("result/{operationId}")]
    public async Task<ActionResult<DocumentAnalysisResult>> GetAnalysisResult(string operationId)
    {
        try
        {
            _logger.LogInformation("Getting analysis result for operation: {OperationId}", operationId);

            if (string.IsNullOrWhiteSpace(operationId))
            {
                return BadRequest("Operation ID is required");
            }

            var result = await _documentIntelligenceService.GetAnalysisResultAsync(operationId);
            
            if (result == null)
            {
                return NotFound($"Analysis result not found for operation: {operationId}");
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis result: {OperationId}", operationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the list of available Document Intelligence models
    /// </summary>
    /// <returns>List of available model IDs</returns>
    [HttpGet("models")]
    public async Task<ActionResult<List<string>>> GetAvailableModels()
    {
        try
        {
            _logger.LogInformation("Getting available Document Intelligence models");
            
            var models = await _documentIntelligenceService.GetAvailableModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available models");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Analyzes a document uploaded via multipart form data using Document Intelligence
    /// </summary>
    /// <param name="file">The uploaded document file</param>
    /// <param name="modelId">Document Intelligence model to use (optional, defaults to prebuilt-document)</param>
    /// <returns>Analysis result</returns>
    [HttpPost("analyze/upload")]
    public async Task<ActionResult<AnalyzeDocumentResponse>> AnalyzeUploadedDocument(
        IFormFile file,
        [FromForm] string modelId = "prebuilt-document")
    {
        try
        {
            _logger.LogInformation("Analyzing uploaded document: {FileName} (Size: {FileSize} bytes) with model: {ModelId}", 
                file.FileName, file.Length, modelId);

            // Validate file
            if (file.Length == 0)
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = "No file uploaded or file is of zero size." 
                });
            }

            // Check file size (limit to 50MB for streaming)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = $"File size exceeds maximum limit of {maxFileSize / (1024 * 1024)}MB" 
                });
            }

            // Validate file type
            var allowedContentTypes = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/bmp",
                "image/tiff",
                "image/heif"
            };

            if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = $"Unsupported file type: {file.ContentType}. Supported types: {string.Join(", ", allowedContentTypes)}" 
                });
            }

            var request = new AnalyzeDocumentStreamRequest
            {
                FileName = file.FileName,
                ModelId = modelId,
                ContentType = file.ContentType,
                IncludeFieldElements = true
            };

            // Use the file stream for analysis
            using var stream = file.OpenReadStream();
            var result = await _documentIntelligenceService.AnalyzeDocumentFromStreamAsync(stream, request);
            
            if (result.Success)
            {
                _logger.LogInformation("Successfully analyzed uploaded document: {FileName}", file.FileName);
                return Ok(result);
            }
            
            _logger.LogWarning("Failed to analyze uploaded document: {FileName}, Reason: {Message}", 
                file.FileName, result.Message);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing uploaded document: {FileName}", file?.FileName ?? "unknown");
            return StatusCode(500, new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = "Internal server error" 
            });
        }
    }

    /// <summary>
    /// Analyzes a document from Azure Storage using streaming (instead of URI)
    /// </summary>
    /// <param name="containerName">Name of the storage container</param>
    /// <param name="blobName">Name of the blob/document</param>
    /// <param name="modelId">Document Intelligence model to use (optional, defaults to prebuilt-document)</param>
    /// <returns>Analysis result</returns>
    [HttpPost("analyze/stream/{containerName}/{blobName}")]
    public async Task<ActionResult<AnalyzeDocumentResponse>> AnalyzeDocumentByStreamPath(
        string containerName, 
        string blobName, 
        [FromQuery] string modelId = "prebuilt-document")
    {
        try
        {
            _logger.LogInformation("Analyzing document via stream: {Container}/{Blob} with model: {ModelId}", 
                containerName, blobName, modelId);

            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName))
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = "Container name and blob name are required" 
                });
            }

            // Get the document stream from storage
            var documentStream = await _storageService.GetDocumentStreamAsync(containerName, blobName);
            
            if (documentStream == null)
            {
                return NotFound(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = $"Document not found: {containerName}/{blobName}" 
                });
            }

            var request = new AnalyzeDocumentStreamRequest
            {
                FileName = $"{containerName}/{blobName}",
                ModelId = modelId,
                ContentType = GetContentTypeFromFileName(blobName),
                IncludeFieldElements = true
            };

            // Analyze using the stream
            using (documentStream)
            {
                var result = await _documentIntelligenceService.AnalyzeDocumentFromStreamAsync(documentStream, request);
                
                if (result.Success)
                {
                    _logger.LogInformation("Successfully analyzed document via stream: {Container}/{Blob}", containerName, blobName);
                    return Ok(result);
                }
                
                _logger.LogWarning("Failed to analyze document via stream: {Container}/{Blob}, Reason: {Message}", 
                    containerName, blobName, result.Message);
                return BadRequest(result);
            }
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Document not found: {Container}/{Blob}", containerName, blobName);
            return NotFound(new AnalyzeDocumentResponse
            {
                Success = false,
                Message = $"Document not found: {containerName}/{blobName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document via stream: {Container}/{Blob}", containerName, blobName);
            return StatusCode(500, new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = "Internal server error" 
            });
        }
    }

    /// <summary>
    /// Analyzes a document from Azure Storage using streaming (no SAS URI required)
    /// </summary>
    /// <param name="request">Request containing container name, blob name, and model ID</param>
    /// <returns>Analysis result</returns>
    [HttpPost("analyze/stream")]
    public async Task<ActionResult<AnalyzeDocumentResponse>> AnalyzeDocumentFromStorage([FromBody] AnalyzeDocumentFromStorageRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing document from storage: {Container}/{Blob} with model: {ModelId}", 
                request.ContainerName, request.BlobName, request.ModelId);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.ContainerName))
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = "Container name is required" 
                });
            }

            if (string.IsNullOrWhiteSpace(request.BlobName))
            {
                return BadRequest(new AnalyzeDocumentResponse 
                { 
                    Success = false, 
                    Message = "Blob name is required" 
                });
            }

            if (string.IsNullOrWhiteSpace(request.ModelId))
            {
                request.ModelId = "prebuilt-document"; // Default model
            }

            // Call the service to analyze the document from storage
            var result = await _documentIntelligenceService.AnalyzeDocumentFromStorageAsync(request);
            
            if (result.Success)
            {
                _logger.LogInformation("Document analysis completed successfully for: {Container}/{Blob}", 
                    request.ContainerName, request.BlobName);
                return Ok(result);
            }
            
            _logger.LogWarning("Document analysis failed for: {Container}/{Blob}. Message: {Message}", 
                request.ContainerName, request.BlobName, result.Message);
            return BadRequest(result);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Document not found: {Container}/{Blob}", request.ContainerName, request.BlobName);
            return NotFound(new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = $"Document not found: {request.ContainerName}/{request.BlobName}" 
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access to document: {Container}/{Blob}", request.ContainerName, request.BlobName);
            return StatusCode(403, new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = "Access denied to the specified document" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document from storage: {Container}/{Blob}", request.ContainerName, request.BlobName);
            return StatusCode(500, new AnalyzeDocumentResponse 
            { 
                Success = false, 
                Message = "Internal server error occurred during document analysis" 
            });
        }
    }

    /// <summary>
    /// Helper method to determine content type from file extension
    /// </summary>
    private static string GetContentTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            ".heif" => "image/heif",
            _ => "application/octet-stream"
        };
    }
}
