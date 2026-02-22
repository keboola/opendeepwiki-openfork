using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Message history entity
/// Records message history within chat sessions
/// </summary>
public class ChatMessageHistory : AggregateRoot<Guid>
{
    /// <summary>
    /// Associated session ID
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Message ID (platform message ID)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Sender identifier
    /// </summary>
    [Required]
    [StringLength(200)]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Message content
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Message type (Text/Image/File/Audio/Video/RichText/Card/Unknown)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string MessageType { get; set; } = "Text";

    /// <summary>
    /// Message role (User/Assistant)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "User";

    /// <summary>
    /// Message timestamp
    /// </summary>
    public DateTime MessageTimestamp { get; set; }

    /// <summary>
    /// Message metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Associated session
    /// </summary>
    [ForeignKey("SessionId")]
    public virtual ChatSession Session { get; set; } = null!;
}
