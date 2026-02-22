using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin tools configuration service interface
/// </summary>
public interface IAdminToolsService
{
    // MCP configuration
    Task<List<McpConfigDto>> GetMcpConfigsAsync();
    Task<McpConfigDto> CreateMcpConfigAsync(McpConfigRequest request);
    Task<bool> UpdateMcpConfigAsync(string id, McpConfigRequest request);
    Task<bool> DeleteMcpConfigAsync(string id);

    // Skill configuration (follows Agent Skills standard)
    Task<List<SkillConfigDto>> GetSkillConfigsAsync();
    Task<SkillDetailDto?> GetSkillDetailAsync(string id);
    Task<SkillConfigDto> UploadSkillAsync(Stream zipStream, string fileName);
    Task<bool> UpdateSkillAsync(string id, SkillUpdateRequest request);
    Task<bool> DeleteSkillAsync(string id);
    Task<string?> GetSkillFileContentAsync(string id, string filePath);
    Task RefreshSkillsFromDiskAsync();

    // Model configuration
    Task<List<ModelConfigDto>> GetModelConfigsAsync();
    Task<ModelConfigDto> CreateModelConfigAsync(ModelConfigRequest request);
    Task<bool> UpdateModelConfigAsync(string id, ModelConfigRequest request);
    Task<bool> DeleteModelConfigAsync(string id);
}
