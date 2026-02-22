using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models.Bookmark;

/// <summary>
/// Add bookmark request
/// </summary>
public class AddBookmarkRequest
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;
}
