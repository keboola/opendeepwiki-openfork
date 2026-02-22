namespace OpenDeepWiki.Models.Bookmark;

/// <summary>
/// Bookmark status response
/// </summary>
public class BookmarkStatusResponse
{
    /// <summary>
    /// Whether the item is bookmarked
    /// </summary>
    public bool IsBookmarked { get; set; }

    /// <summary>
    /// Bookmarked at timestamp (only has value when bookmarked)
    /// </summary>
    public DateTime? BookmarkedAt { get; set; }
}
