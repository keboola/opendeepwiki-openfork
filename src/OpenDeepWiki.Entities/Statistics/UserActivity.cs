using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User activity type
/// </summary>
public enum UserActivityType
{
    /// <summary>
    /// View repository
    /// </summary>
    View = 0,

    /// <summary>
    /// Search
    /// </summary>
    Search = 1,

    /// <summary>
    /// Bookmark
    /// </summary>
    Bookmark = 2,

    /// <summary>
    /// Subscribe
    /// </summary>
    Subscribe = 3,

    /// <summary>
    /// Analyze repository
    /// </summary>
    Analyze = 4
}

/// <summary>
/// User activity record entity
/// Records user behavior to support recommendation algorithms
/// </summary>
public class UserActivity : AggregateRoot<string>
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID (optional, search behavior may not have one)
    /// </summary>
    [StringLength(36)]
    public string? RepositoryId { get; set; }

    /// <summary>
    /// Activity type
    /// </summary>
    public UserActivityType ActivityType { get; set; }

    /// <summary>
    /// Activity weight (used for recommendation algorithms)
    /// View=1, Search=2, Bookmark=3, Subscribe=4, Analyze=5
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Browse duration (seconds), only valid for View type
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Search keywords, only valid for Search type
    /// </summary>
    [StringLength(500)]
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Related language (used for language preference statistics)
    /// </summary>
    [StringLength(50)]
    public string? Language { get; set; }

    /// <summary>
    /// User navigation property
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    /// <summary>
    /// Repository navigation property
    /// </summary>
    [ForeignKey("RepositoryId")]
    public virtual Repository? Repository { get; set; }
}
