namespace OpenDeepWiki.Models.Admin;

/// <summary>
/// MCP configuration request
/// </summary>
public class McpConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ServerUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// MCP configuration DTO
/// </summary>
public class McpConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ServerUrl { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Skill configuration DTO (follows Agent Skills standard)
/// </summary>
public class SkillConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? License { get; set; }
    public string? Compatibility { get; set; }
    public string? AllowedTools { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string? Author { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string Source { get; set; } = "local";
    public string? SourceUrl { get; set; }
    public bool HasScripts { get; set; }
    public bool HasReferences { get; set; }
    public bool HasAssets { get; set; }
    public long SkillMdSize { get; set; }
    public long TotalSize { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Parsed SKILL.md frontmatter for UI display (dynamic parameters, metadata, etc.)
    /// </summary>
    public Dictionary<string, object?> Frontmatter { get; set; } = new();
}

/// <summary>
/// Skill detail DTO (includes SKILL.md content)
/// </summary>
public class SkillDetailDto : SkillConfigDto
{
    public SkillDetailDto()
    {
    }

    public SkillDetailDto(SkillConfigDto source)
    {
        Id = source.Id;
        Name = source.Name;
        Description = source.Description;
        License = source.License;
        Compatibility = source.Compatibility;
        AllowedTools = source.AllowedTools;
        FolderPath = source.FolderPath;
        IsActive = source.IsActive;
        SortOrder = source.SortOrder;
        Author = source.Author;
        Version = source.Version;
        Source = source.Source;
        SourceUrl = source.SourceUrl;
        HasScripts = source.HasScripts;
        HasReferences = source.HasReferences;
        HasAssets = source.HasAssets;
        SkillMdSize = source.SkillMdSize;
        TotalSize = source.TotalSize;
        CreatedAt = source.CreatedAt;
        Frontmatter = new Dictionary<string, object?>(source.Frontmatter);
    }

    /// <summary>
    /// Full SKILL.md content
    /// </summary>
    public string SkillMdContent { get; set; } = string.Empty;

    /// <summary>
    /// List of files in the scripts directory
    /// </summary>
    public List<SkillFileInfo> Scripts { get; set; } = new();

    /// <summary>
    /// List of files in the references directory
    /// </summary>
    public List<SkillFileInfo> References { get; set; } = new();

    /// <summary>
    /// List of files in the assets directory
    /// </summary>
    public List<SkillFileInfo> Assets { get; set; } = new();
}

/// <summary>
/// Skill file information
/// </summary>
public class SkillFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Skill update request (updates management fields only)
/// </summary>
public class SkillUpdateRequest
{
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>
/// Model configuration request
/// </summary>
public class ModelConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Model configuration DTO
/// </summary>
public class ModelConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public bool HasApiKey { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
