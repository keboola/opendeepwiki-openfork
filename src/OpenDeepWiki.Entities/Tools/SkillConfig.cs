using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities.Tools;

/// <summary>
/// Agent Skills configuration entity
/// Follows the Anthropic Agent Skills open standard (agentskills.io)
/// Skills are stored as folders; this entity only records metadata and management information
/// </summary>
public class SkillConfig : AggregateRoot<string>
{
    /// <summary>
    /// Skill name (unique identifier, also the folder name)
    /// Specification: max 64 characters, only lowercase letters, digits, and hyphens; cannot start or end with a hyphen
    /// </summary>
    [Required]
    [StringLength(64)]
    [RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Name can only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Skill description (parsed from SKILL.md frontmatter)
    /// Specification: max 1024 characters
    /// </summary>
    [Required]
    [StringLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// License information (parsed from SKILL.md frontmatter)
    /// </summary>
    [StringLength(100)]
    public string? License { get; set; }

    /// <summary>
    /// Compatibility requirements (parsed from SKILL.md frontmatter)
    /// Specification: max 500 characters
    /// </summary>
    [StringLength(500)]
    public string? Compatibility { get; set; }

    /// <summary>
    /// Pre-approved tool list (space-separated, parsed from SKILL.md frontmatter)
    /// </summary>
    [StringLength(1000)]
    public string? AllowedTools { get; set; }

    /// <summary>
    /// Skill folder relative path (relative to the skills root directory)
    /// Example: code-review, data-analysis
    /// </summary>
    [Required]
    [StringLength(200)]
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Author
    /// </summary>
    [StringLength(100)]
    public string? Author { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    [StringLength(20)]
    public new string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Source type
    /// </summary>
    public SkillSource Source { get; set; } = SkillSource.Local;

    /// <summary>
    /// Source URL (if imported from remote)
    /// </summary>
    [StringLength(500)]
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Whether it contains a scripts directory
    /// </summary>
    public bool HasScripts { get; set; }

    /// <summary>
    /// Whether it contains a references directory
    /// </summary>
    public bool HasReferences { get; set; }

    /// <summary>
    /// Whether it contains an assets directory
    /// </summary>
    public bool HasAssets { get; set; }

    /// <summary>
    /// SKILL.md file size (bytes)
    /// </summary>
    public long SkillMdSize { get; set; }

    /// <summary>
    /// Total Skill folder size (bytes)
    /// </summary>
    public long TotalSize { get; set; }
}

/// <summary>
/// Skill source type
/// </summary>
public enum SkillSource
{
    /// <summary>
    /// Local upload
    /// </summary>
    Local = 0,

    /// <summary>
    /// Imported from URL
    /// </summary>
    Remote = 1,

    /// <summary>
    /// Installed from marketplace
    /// </summary>
    Marketplace = 2
}
