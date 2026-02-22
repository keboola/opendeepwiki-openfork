using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin system settings service interface
/// </summary>
public interface IAdminSettingsService
{
    Task<List<SystemSettingDto>> GetSettingsAsync(string? category);
    Task<SystemSettingDto?> GetSettingByKeyAsync(string key);
    Task UpdateSettingsAsync(List<UpdateSettingRequest> requests);
}
