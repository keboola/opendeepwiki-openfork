namespace OpenDeepWiki.Models.Bookmark;

/// <summary>
/// Bookmark operation response
/// </summary>
public class BookmarkResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message (only has value on failure)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Bookmark record ID (only has value on success)
    /// </summary>
    public string? BookmarkId { get; set; }
}
