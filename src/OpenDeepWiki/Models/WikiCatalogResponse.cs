namespace OpenDeepWiki.Models;

/// <summary>
/// Wiki catalog response
/// </summary>
public class WikiCatalogResponse
{
    /// <summary>
    /// Organization name
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    public string Repo { get; set; } = string.Empty;

    /// <summary>
    /// Default path
    /// </summary>
    public string DefaultPath { get; set; } = string.Empty;

    /// <summary>
    /// List of catalog items
    /// </summary>
    public List<WikiCatalogItemResponse> Items { get; set; } = [];
}

/// <summary>
/// Wiki catalog item response
/// </summary>
public class WikiCatalogItemResponse
{
    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Sort order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Whether the item has document content
    /// </summary>
    public bool HasContent { get; set; }

    /// <summary>
    /// Child catalog items
    /// </summary>
    public List<WikiCatalogItemResponse> Children { get; set; } = [];
}
