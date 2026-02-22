using OpenDeepWiki.Models.Auth;
using OpenDeepWiki.Models.UserProfile;

namespace OpenDeepWiki.Services.UserProfile;

/// <summary>
/// User profile service interface
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Update user profile
    /// </summary>
    Task<UserInfo> UpdateProfileAsync(string userId, UpdateProfileRequest request);

    /// <summary>
    /// Change password
    /// </summary>
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request);

    /// <summary>
    /// Get user settings
    /// </summary>
    Task<UserSettingsDto> GetSettingsAsync(string userId);

    /// <summary>
    /// Update user settings
    /// </summary>
    Task<UserSettingsDto> UpdateSettingsAsync(string userId, UserSettingsDto settings);
}
