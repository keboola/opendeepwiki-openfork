using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Chat assistant configuration entity
/// Stores admin-configured models, MCPs, Skills, etc.
/// </summary>
public class ChatAssistantConfig : AggregateRoot<Guid>
{
    /// <summary>
    /// Whether chat assistant feature is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Enabled model ID list (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? EnabledModelIds { get; set; }

    /// <summary>
    /// Enabled MCP ID list (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? EnabledMcpIds { get; set; }

    /// <summary>
    /// Enabled Skill ID list (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? EnabledSkillIds { get; set; }

    /// <summary>
    /// Default model ID
    /// </summary>
    [StringLength(100)]
    public string? DefaultModelId { get; set; }

    /// <summary>
    /// Whether image upload feature is enabled
    /// </summary>
    public bool EnableImageUpload { get; set; } = false;
}
