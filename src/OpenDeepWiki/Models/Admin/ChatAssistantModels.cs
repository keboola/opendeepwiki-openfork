namespace OpenDeepWiki.Models.Admin;

/// <summary>
/// Chat assistant configuration DTO
/// </summary>
public class ChatAssistantConfigDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public List<string> EnabledModelIds { get; set; } = new();
    public List<string> EnabledMcpIds { get; set; } = new();
    public List<string> EnabledSkillIds { get; set; } = new();
    public string? DefaultModelId { get; set; }
    public bool EnableImageUpload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Update chat assistant configuration request
/// </summary>
public class UpdateChatAssistantConfigRequest
{
    public bool IsEnabled { get; set; }
    public List<string> EnabledModelIds { get; set; } = new();
    public List<string> EnabledMcpIds { get; set; } = new();
    public List<string> EnabledSkillIds { get; set; } = new();
    public string? DefaultModelId { get; set; }
    public bool EnableImageUpload { get; set; }
}

/// <summary>
/// Selectable item DTO (used for model, MCP, and Skill selection lists)
/// </summary>
public class SelectableItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// Chat assistant configuration options response
/// </summary>
public class ChatAssistantConfigOptionsDto
{
    public ChatAssistantConfigDto Config { get; set; } = new();
    public List<SelectableItemDto> AvailableModels { get; set; } = new();
    public List<SelectableItemDto> AvailableMcps { get; set; } = new();
    public List<SelectableItemDto> AvailableSkills { get; set; } = new();
}
