using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Aggregate root base class (DDD design)
/// </summary>
/// <typeparam name="TKey">Primary key type</typeparam>
public abstract class AggregateRoot<TKey>
{
    /// <summary>
    /// Primary key ID
    /// </summary>
    [Key]
    public TKey Id { get; set; } = default!;

    /// <summary>
    /// Creation time (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Update time (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Deletion time (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Whether deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Version number (used for optimistic concurrency control)
    /// </summary>
    [Timestamp]
    public byte[]? Version { get; set; }

    /// <summary>
    /// Mark as deleted
    /// </summary>
    public virtual void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update timestamp
    /// </summary>
    public virtual void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
