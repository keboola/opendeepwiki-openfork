using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// MCP configuration entity
/// </summary>
public class McpConfig : AggregateRoot<string>
{
    /// <summary>
    /// MCP name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MCP description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// MCP server URL
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key
    /// </summary>
    [StringLength(500)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
