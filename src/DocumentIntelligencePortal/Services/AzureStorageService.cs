using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocumentIntelligencePortal.Models;

namespace DocumentIntelligencePortal.Services;

/// <summary>
/// Service for interacting with Azure Storage Blobs using managed identity
/// </summary>
public interface IAzureStorageService
{
    Task<ListContainersResponse> ListContainersAsync();
    Task<ListDocumentsResponse> ListDocumentsAsync(string containerName);
    Task<SearchDocumentsResponse> SearchDocumentsAsync(string containerName, string searchTerm, int maxResults = 100);
    Task<Stream?> GetDocumentStreamAsync(string containerName, string blobName);
    Task<string> GetDocumentSasUriAsync(string containerName, string blobName);
}

public class AzureStorageService : IAzureStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureStorageService> _logger;
    private readonly IConfiguration _configuration;

    public AzureStorageService(ILogger<AzureStorageService> logger, IConfiguration configuration, IAzureCredentialProvider credentialProvider)
    {
        _logger = logger;
        _configuration = configuration;

        // Get storage account configuration
        var storageAccountName = _configuration["Azure:StorageAccountName"];
        if (string.IsNullOrEmpty(storageAccountName))
        {
            throw new InvalidOperationException("Azure:StorageAccountName configuration is missing");
        }

        // Check if using development storage (Azurite)
        var connectionString = _configuration.GetConnectionString("AzureStorage");
        if (!string.IsNullOrEmpty(connectionString) && 
            (connectionString.Contains("UseDevelopmentStorage=true") || connectionString.Contains("127.0.0.1")))
        {
            _logger.LogInformation("Using Azure Storage connection string for development/testing");
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        else
        {
            // Use credential provider for authentication
            var storageUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
            var credential = credentialProvider.GetCredential();
            _blobServiceClient = new BlobServiceClient(storageUri, credential);
        }

        _logger.LogInformation("Azure Storage Service initialized for account: {StorageAccount}", storageAccountName);
    }

    /// <summary>
    /// Lists all containers in the storage account
    /// </summary>
    public async Task<ListContainersResponse> ListContainersAsync()
    {
        try
        {
            _logger.LogInformation("Listing storage containers");
            
            var containers = new List<string>();
            await foreach (var container in _blobServiceClient.GetBlobContainersAsync())
            {
                containers.Add(container.Name);
            }

            _logger.LogInformation("Found {ContainerCount} containers", containers.Count);
            
            return new ListContainersResponse
            {
                Success = true,
                Containers = containers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list storage containers");
            return new ListContainersResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Lists all documents (blobs) in a specific container
    /// </summary>
    public async Task<ListDocumentsResponse> ListDocumentsAsync(string containerName)
    {
        try
        {
            _logger.LogInformation("Listing documents in container: {Container}", containerName);
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var documents = new List<StorageDocument>();

            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                var document = new StorageDocument
                {
                    Name = blob.Name,
                    BlobUri = containerClient.GetBlobClient(blob.Name).Uri.ToString(),
                    Size = blob.Properties.ContentLength ?? 0,
                    ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                    LastModified = blob.Properties.LastModified ?? DateTimeOffset.MinValue,
                    Container = containerName
                };
                documents.Add(document);
            }

            _logger.LogInformation("Found {DocumentCount} documents in container {Container}", 
                documents.Count, containerName);
            
            return new ListDocumentsResponse
            {
                Success = true,
                Documents = documents
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list documents in container: {Container}", containerName);
            return new ListDocumentsResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets a document stream for reading - returns a seekable MemoryStream
    /// </summary>
    public async Task<Stream?> GetDocumentStreamAsync(string containerName, string blobName)
    {
        try
        {
            _logger.LogInformation("Getting document stream for: {Container}/{Blob}", containerName, blobName);
            
            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            
            if (await blobClient.ExistsAsync())
            {
                // Download the blob content to a MemoryStream to ensure it's seekable
                // This is required for Azure Document Intelligence which needs seekable streams
                var memoryStream = new MemoryStream();
                
                try
                {
                    _logger.LogInformation("Downloading blob content to memory: {Container}/{Blob}", containerName, blobName);
                    
                    // Download directly to memory stream
                    await blobClient.DownloadToAsync(memoryStream);
                    memoryStream.Position = 0; // Reset to beginning for reading
                    
                    _logger.LogInformation("Successfully downloaded blob to memory. Size: {Size} bytes", memoryStream.Length);
                    return memoryStream;
                }
                catch (Exception ex)
                {
                    // Dispose memory stream if download failed
                    memoryStream.Dispose();
                    _logger.LogError(ex, "Failed to download blob content: {Container}/{Blob}", containerName, blobName);
                    throw;
                }
            }

            _logger.LogWarning("Document not found: {Container}/{Blob}", containerName, blobName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document stream: {Container}/{Blob}", containerName, blobName);
            return null;
        }
    }

    /// <summary>
    /// Generates a SAS URI for the document (required for Document Intelligence)
    /// </summary>
    public async Task<string> GetDocumentSasUriAsync(string containerName, string blobName)
    {
        try
        {
            _logger.LogInformation("Generating SAS URI for: {Container}/{Blob}", containerName, blobName);
            
            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Document not found: {containerName}/{blobName}");
            }

            // Note: When using managed identity, we can't generate SAS tokens directly
            // Instead, we return the blob URI which can be accessed using the same managed identity
            // that the Document Intelligence service should be configured to use
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URI: {Container}/{Blob}", containerName, blobName);
            throw;
        }
    }

    /// <summary>
    /// Searches for documents in a container by name with efficient pagination
    /// Supports wildcard patterns and case-insensitive matching
    /// </summary>
    public async Task<SearchDocumentsResponse> SearchDocumentsAsync(string containerName, string searchTerm, int maxResults = 100)
    {
        try
        {
            _logger.LogInformation("Searching documents in container: {Container} with term: {SearchTerm}", 
                containerName, searchTerm);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new SearchDocumentsResponse
                {
                    Success = false,
                    ErrorMessage = "Search term is required",
                    SearchTerm = searchTerm,
                    MaxResults = maxResults
                };
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var documents = new List<StorageDocument>();
            var totalMatches = 0;
            var processedCount = 0;
            
            // Normalize search term for case-insensitive matching
            var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();
            var isWildcardSearch = normalizedSearchTerm.Contains('*') || normalizedSearchTerm.Contains('?');
            
            // Configure blob listing options for efficient pagination
            var blobTraits = BlobTraits.Metadata;
            var blobStates = BlobStates.All;
            
            try
            {
                await foreach (var blob in containerClient.GetBlobsAsync(traits: blobTraits, states: blobStates))
                {
                    processedCount++;
                    var normalizedBlobName = blob.Name.ToLowerInvariant();
                    bool isMatch = false;
                    
                    if (isWildcardSearch)
                    {
                        // Use pattern matching for wildcard searches
                        isMatch = IsWildcardMatch(normalizedBlobName, normalizedSearchTerm);
                    }
                    else
                    {
                        // Simple contains match for non-wildcard searches
                        isMatch = normalizedBlobName.Contains(normalizedSearchTerm);
                    }
                    
                    if (isMatch)
                    {
                        totalMatches++;
                        
                        // Only add to results if we haven't exceeded maxResults
                        if (documents.Count < maxResults)
                        {
                            var document = new StorageDocument
                            {
                                Name = blob.Name,
                                BlobUri = containerClient.GetBlobClient(blob.Name).Uri.ToString(),
                                Size = blob.Properties.ContentLength ?? 0,
                                ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                                LastModified = blob.Properties.LastModified ?? DateTimeOffset.MinValue,
                                Container = containerName
                            };
                            documents.Add(document);
                        }
                    }
                    
                    // Performance optimization: stop processing if we have enough results
                    // and have processed a reasonable number of blobs
                    if (documents.Count >= maxResults && processedCount >= maxResults * 2)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching blobs in container: {Container}", containerName);
                throw;
            }

            var hasMoreResults = totalMatches > maxResults;
            
            _logger.LogInformation("Search completed. Found {TotalMatches} matches, returning {ReturnedCount} results. " +
                "Processed {ProcessedCount} blobs. HasMore: {HasMore}", 
                totalMatches, documents.Count, processedCount, hasMoreResults);
            
            return new SearchDocumentsResponse
            {
                Success = true,
                Documents = documents,
                SearchTerm = searchTerm,
                TotalMatches = totalMatches,
                MaxResults = maxResults,
                HasMoreResults = hasMoreResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search documents in container: {Container} with term: {SearchTerm}", 
                containerName, searchTerm);
            return new SearchDocumentsResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                SearchTerm = searchTerm,
                MaxResults = maxResults
            };
        }
    }

    /// <summary>
    /// Performs wildcard pattern matching (supports * and ? wildcards)
    /// </summary>
    private static bool IsWildcardMatch(string input, string pattern)
    {
        // Convert wildcard pattern to regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            // Fallback to simple contains if regex fails
            return input.Contains(pattern.Replace("*", "").Replace("?", ""));
        }
    }
}
