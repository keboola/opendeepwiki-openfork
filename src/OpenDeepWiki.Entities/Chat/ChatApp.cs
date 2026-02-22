using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Chat application entity created by users
/// Contains AppId, AppSecret, and configuration for embedding into external websites
/// </summary>
public class ChatApp : AggregateRoot<Guid>
{
    /// <summary>
    /// Owner user ID
    /// </summary>
    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Application name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Application description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Application icon URL
    /// </summary>
    [StringLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Public application ID (used for embed scripts)
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Server-side verification secret key
    /// </summary>
    [Required]
    [StringLength(128)]
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether domain validation is enabled
    /// </summary>
    public bool EnableDomainValidation { get; set; } = false;

    /// <summary>
    /// Allowed domain list (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? AllowedDomains { get; set; }

    /// <summary>
    /// AI model provider type (OpenAI, OpenAIResponses, Anthropic)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProviderType { get; set; } = "OpenAI";

    /// <summary>
    /// API key
    /// </summary>
    [StringLength(500)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// API base URL
    /// </summary>
    [StringLength(500)]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Available model list (JSON array)
    /// </summary>
    [StringLength(1000)]
    public string? AvailableModels { get; set; }

    /// <summary>
    /// Default model
    /// </summary>
    [StringLength(100)]
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Rate limit per minute
    /// </summary>
    public int? RateLimitPerMinute { get; set; }

    /// <summary>
    /// Whether active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
