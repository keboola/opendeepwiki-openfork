using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin chat assistant configuration service interface
/// </summary>
public interface IAdminChatAssistantService
{
    /// <summary>
    /// Get chat assistant configuration (including available options list)
    /// </summary>
    Task<ChatAssistantConfigOptionsDto> GetConfigWithOptionsAsync();

    /// <summary>
    /// Get chat assistant configuration
    /// </summary>
    Task<ChatAssistantConfigDto> GetConfigAsync();

    /// <summary>
    /// Update chat assistant configuration
    /// </summary>
    Task<ChatAssistantConfigDto> UpdateConfigAsync(UpdateChatAssistantConfigRequest request);
}
