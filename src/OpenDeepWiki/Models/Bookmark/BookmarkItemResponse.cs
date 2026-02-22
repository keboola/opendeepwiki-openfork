namespace OpenDeepWiki.Models.Bookmark;

/// <summary>
/// Bookmark item response
/// </summary>
public class BookmarkItemResponse
{
    /// <summary>
    /// Bookmark record ID
    /// </summary>
    public string BookmarkId { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID
    /// </summary>
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrgName { get; set; } = string.Empty;

    /// <summary>
    /// Repository description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Star count
    /// </summary>
    public int StarCount { get; set; }

    /// <summary>
    /// Fork count
    /// </summary>
    public int ForkCount { get; set; }

    /// <summary>
    /// Bookmark count
    /// </summary>
    public int BookmarkCount { get; set; }

    /// <summary>
    /// Bookmarked at timestamp
    /// </summary>
    public DateTime BookmarkedAt { get; set; }
}
