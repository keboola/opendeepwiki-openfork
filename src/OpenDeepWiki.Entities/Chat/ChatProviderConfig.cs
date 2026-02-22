using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Provider configuration entity
/// Stores integration configuration for each platform
/// </summary>
public class ChatProviderConfig : AggregateRoot<Guid>
{
    /// <summary>
    /// Platform identifier
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Configuration data (encrypted JSON)
    /// </summary>
    [Required]
    public string ConfigData { get; set; } = string.Empty;

    /// <summary>
    /// Webhook URL
    /// </summary>
    [StringLength(500)]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Message sending interval (milliseconds)
    /// </summary>
    public int MessageInterval { get; set; } = 500;

    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
}
