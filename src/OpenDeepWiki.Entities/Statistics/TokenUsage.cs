using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Token consumption record entity
/// </summary>
public class TokenUsage : AggregateRoot<string>
{
    /// <summary>
    /// Associated repository ID (optional)
    /// </summary>
    [StringLength(36)]
    public string? RepositoryId { get; set; }

    /// <summary>
    /// Associated user ID (optional)
    /// </summary>
    [StringLength(36)]
    public string? UserId { get; set; }

    /// <summary>
    /// Input token count
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Output token count
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Model name used
    /// </summary>
    [StringLength(100)]
    public string? ModelName { get; set; }

    /// <summary>
    /// Operation type (catalog, content, chat, etc.)
    /// </summary>
    [StringLength(50)]
    public string? Operation { get; set; }

    /// <summary>
    /// Record time
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Associated repository navigation property
    /// </summary>
    [ForeignKey("RepositoryId")]
    public virtual Repository? Repository { get; set; }

    /// <summary>
    /// Associated user navigation property
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
