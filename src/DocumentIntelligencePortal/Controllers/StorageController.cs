using Microsoft.AspNetCore.Mvc;
using DocumentIntelligencePortal.Models;
using DocumentIntelligencePortal.Services;

namespace DocumentIntelligencePortal.Controllers;

/// <summary>
/// Controller for managing Azure Storage operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly IAzureStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IAzureStorageService storageService, ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available containers in the storage account
    /// </summary>
    /// <returns>List of container names</returns>
    [HttpGet("containers")]
    public async Task<ActionResult<ListContainersResponse>> GetContainers()
    {
        try
        {
            _logger.LogInformation("Getting storage containers");
            var result = await _storageService.ListContainersAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage containers");
            return StatusCode(500, new ListContainersResponse 
            { 
                Success = false, 
                ErrorMessage = "Internal server error" 
            });
        }
    }

    /// <summary>
    /// Gets all documents in a specific container
    /// </summary>
    /// <param name="containerName">Name of the container</param>
    /// <returns>List of documents in the container</returns>
    [HttpGet("containers/{containerName}/documents")]
    public async Task<ActionResult<ListDocumentsResponse>> GetDocuments(string containerName)
    {
        try
        {
            _logger.LogInformation("Getting documents from container: {Container}", containerName);
            
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return BadRequest(new ListDocumentsResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Container name is required" 
                });
            }

            var result = await _storageService.ListDocumentsAsync(containerName);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents from container: {Container}", containerName);
            return StatusCode(500, new ListDocumentsResponse 
            { 
                Success = false, 
                ErrorMessage = "Internal server error" 
            });
        }
    }

    /// <summary>
    /// Downloads a specific document from storage
    /// </summary>
    /// <param name="containerName">Name of the container</param>
    /// <param name="blobName">Name of the blob/document</param>
    /// <returns>File stream</returns>
    [HttpGet("containers/{containerName}/documents/{blobName}/download")]
    public async Task<IActionResult> DownloadDocument(string containerName, string blobName)
    {
        try
        {
            _logger.LogInformation("Downloading document: {Container}/{Blob}", containerName, blobName);
            
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName))
            {
                return BadRequest("Container name and blob name are required");
            }

            var stream = await _storageService.GetDocumentStreamAsync(containerName, blobName);
            
            if (stream == null)
            {
                return NotFound($"Document not found: {containerName}/{blobName}");
            }

            // Determine content type based on file extension
            var contentType = GetContentType(blobName);
            
            return File(stream, contentType, blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document: {Container}/{Blob}", containerName, blobName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Searches for documents in a specific container by name
    /// </summary>
    /// <param name="containerName">Name of the container</param>
    /// <param name="searchTerm">Search term (supports wildcards * and ?)</param>
    /// <param name="maxResults">Maximum number of results to return (default: 100)</param>
    /// <returns>Search results with matching documents</returns>
    [HttpGet("containers/{containerName}/documents/search")]
    public async Task<ActionResult<SearchDocumentsResponse>> SearchDocuments(
        string containerName, 
        [FromQuery] string searchTerm,
        [FromQuery] int maxResults = 100)
    {
        try
        {
            _logger.LogInformation("Searching documents in container: {Container} with term: {SearchTerm}", 
                containerName, searchTerm);
            
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return BadRequest(new SearchDocumentsResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Container name is required",
                    SearchTerm = searchTerm ?? string.Empty,
                    MaxResults = maxResults
                });
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new SearchDocumentsResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Search term is required",
                    SearchTerm = searchTerm ?? string.Empty,
                    MaxResults = maxResults
                });
            }

            // Validate maxResults parameter
            if (maxResults < 1 || maxResults > 1000)
            {
                return BadRequest(new SearchDocumentsResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Max results must be between 1 and 1000",
                    SearchTerm = searchTerm,
                    MaxResults = maxResults
                });
            }

            var result = await _storageService.SearchDocumentsAsync(containerName, searchTerm, maxResults);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents in container: {Container} with term: {SearchTerm}", 
                containerName, searchTerm);
            return StatusCode(500, new SearchDocumentsResponse 
            { 
                Success = false, 
                ErrorMessage = "Internal server error",
                SearchTerm = searchTerm ?? string.Empty,
                MaxResults = maxResults
            });
        }
    }

    /// <summary>
    /// Determines the content type based on file extension
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",
            ".tif" => "image/tiff",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
