namespace DocumentIntelligencePortal.Models;

/// <summary>
/// Represents the analysis result from Azure Document Intelligence
/// </summary>
public class DocumentAnalysisResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<DocumentPage> Pages { get; set; } = new();
    public List<DocumentTable> Tables { get; set; } = new();
    public List<DocumentKeyValuePair> KeyValuePairs { get; set; } = new();
    public List<DocumentEntity> Entities { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a page in the analyzed document
/// </summary>
public class DocumentPage
{
    public int PageNumber { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string Unit { get; set; } = string.Empty;
    public float Angle { get; set; }
    public List<DocumentLine> Lines { get; set; } = new();
}

/// <summary>
/// Represents a line of text in the document
/// </summary>
public class DocumentLine
{
    public string Content { get; set; } = string.Empty;
    public BoundingBox BoundingBox { get; set; } = new();
}

/// <summary>
/// Represents a table in the document
/// </summary>
public class DocumentTable
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public List<DocumentTableCell> Cells { get; set; } = new();
}

/// <summary>
/// Represents a cell in a document table
/// </summary>
public class DocumentTableCell
{
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public int RowSpan { get; set; }
    public int ColumnSpan { get; set; }
    public string Content { get; set; } = string.Empty;
    public BoundingBox BoundingBox { get; set; } = new();
}

/// <summary>
/// Represents a key-value pair extracted from the document
/// </summary>
public class DocumentKeyValuePair
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

/// <summary>
/// Represents an entity extracted from the document
/// </summary>
public class DocumentEntity
{
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public BoundingBox BoundingBox { get; set; } = new();
}

/// <summary>
/// Represents a bounding box for document elements
/// </summary>
public class BoundingBox
{
    public List<float> Points { get; set; } = new();
}
