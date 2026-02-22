using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Message queue entity
/// Used for handling continuous message sending and platform rate limiting
/// </summary>
public class ChatMessageQueue : AggregateRoot<Guid>
{
    /// <summary>
    /// Associated session ID (optional)
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Target user ID
    /// </summary>
    [Required]
    [StringLength(200)]
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// Platform identifier
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Message content (JSON)
    /// </summary>
    [Required]
    public string MessageContent { get; set; } = string.Empty;

    /// <summary>
    /// Queue type (Incoming/Outgoing/Retry)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string QueueType { get; set; } = "Incoming";

    /// <summary>
    /// Processing status (Pending/Processing/Completed/Failed)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Retry count
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Scheduled execution time
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}
