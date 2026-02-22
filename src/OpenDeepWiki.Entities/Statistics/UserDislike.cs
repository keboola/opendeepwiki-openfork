using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User dislike record
/// Used to exclude repositories that users have explicitly marked as not interested
/// </summary>
public class UserDislike : AggregateRoot<string>
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

    /// <summary>
    /// Dislike reason (optional)
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }

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
