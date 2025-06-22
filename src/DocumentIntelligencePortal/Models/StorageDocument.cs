namespace DocumentIntelligencePortal.Models;

/// <summary>
/// Represents a document stored in Azure Storage
/// </summary>
public class StorageDocument
{
    public string Name { get; set; } = string.Empty;
    public string BlobUri { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset LastModified { get; set; }
    public string Container { get; set; } = string.Empty;
}
