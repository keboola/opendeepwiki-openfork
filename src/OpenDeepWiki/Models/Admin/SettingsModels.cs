namespace OpenDeepWiki.Models.Admin;

/// <summary>
/// System setting DTO
/// </summary>
public class SystemSettingDto
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Update setting request
/// </summary>
public class UpdateSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}
