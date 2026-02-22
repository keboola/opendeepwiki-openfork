using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// AI model configuration entity
/// </summary>
public class ModelConfig : AggregateRoot<string>
{
    /// <summary>
    /// Model display name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Model provider (OpenAI, Anthropic, AzureOpenAI, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Model ID
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint
    /// </summary>
    [StringLength(500)]
    public string? Endpoint { get; set; }

    /// <summary>
    /// API key
    /// </summary>
    [StringLength(500)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Whether this is the default model
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Model description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}
