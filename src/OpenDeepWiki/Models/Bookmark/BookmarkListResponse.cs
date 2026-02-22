namespace OpenDeepWiki.Models.Bookmark;

/// <summary>
/// Bookmark list response
/// </summary>
public class BookmarkListResponse
{
    /// <summary>
    /// List of bookmark items
    /// </summary>
    public List<BookmarkItemResponse> Items { get; set; } = [];

    /// <summary>
    /// Total count
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}
