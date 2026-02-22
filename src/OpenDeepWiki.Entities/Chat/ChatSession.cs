using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Chat session entity
/// Maintains conversation context between user and Agent
/// </summary>
public class ChatSession : AggregateRoot<Guid>
{
    /// <summary>
    /// User identifier (platform user ID)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Platform identifier
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Session state (Active/Processing/Waiting/Expired/Closed)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string State { get; set; } = "Active";

    /// <summary>
    /// Last activity time
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Session metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Associated message history
    /// </summary>
    public virtual ICollection<ChatMessageHistory> Messages { get; set; } = new List<ChatMessageHistory>();
}
