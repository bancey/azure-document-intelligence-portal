namespace DocumentIntelligencePortal.Models;

/// <summary>
/// Request model for document analysis using URI
/// </summary>
public class AnalyzeDocumentRequest
{
    public string BlobUri { get; set; } = string.Empty;
    public string ModelId { get; set; } = "prebuilt-document";
    public bool IncludeFieldElements { get; set; } = true;
}

/// <summary>
/// Request model for document analysis using stream
/// </summary>
public class AnalyzeDocumentStreamRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ModelId { get; set; } = "prebuilt-document";
    public bool IncludeFieldElements { get; set; } = true;
    public string ContentType { get; set; } = "application/pdf";
}

/// <summary>
/// Request model for document analysis from storage using stream
/// </summary>
public class AnalyzeDocumentFromStorageRequest
{
    public string ContainerName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string ModelId { get; set; } = "prebuilt-document";
    public bool IncludeFieldElements { get; set; } = true;
}

/// <summary>
/// Response model for document analysis operations
/// </summary>
public class AnalyzeDocumentResponse
{
    public bool Success { get; set; }
    public string? OperationId { get; set; }
    public string? Message { get; set; }
    public DocumentAnalysisResult? Result { get; set; }
}

/// <summary>
/// Response model for listing storage containers
/// </summary>
public class ListContainersResponse
{
    public bool Success { get; set; }
    public List<string> Containers { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for listing documents in a container
/// </summary>
public class ListDocumentsResponse
{
    public bool Success { get; set; }
    public List<StorageDocument> Documents { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for searching documents in a container
/// </summary>
public class SearchDocumentsResponse
{
    public bool Success { get; set; }
    public List<StorageDocument> Documents { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public int MaxResults { get; set; }
    public bool HasMoreResults { get; set; }
    public string? ErrorMessage { get; set; }
}
