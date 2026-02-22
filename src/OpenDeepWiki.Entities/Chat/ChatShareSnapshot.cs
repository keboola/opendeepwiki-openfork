using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Chat share snapshot entity
/// Stores conversation content and metadata when creating a share
/// </summary>
public class ChatShareSnapshot : AggregateRoot<Guid>
{
    /// <summary>
    /// Public share ID (random token)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ShareId { get; set; } = string.Empty;

    /// <summary>
    /// Share title
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Share description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Share creator (nullable)
    /// </summary>
    [StringLength(200)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Share content snapshot (JSON)
    /// </summary>
    [Required]
    public string SnapshotJson { get; set; } = string.Empty;

    /// <summary>
    /// Share metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Share creation time (UTC)
    /// </summary>
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Share expiration time (UTC)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Share revocation time (UTC)
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}
