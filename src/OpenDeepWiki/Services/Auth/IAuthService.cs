using OpenDeepWiki.Models.Auth;

namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// User login
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// User registration
    /// </summary>
    Task<LoginResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Get user information
    /// </summary>
    Task<UserInfo?> GetUserInfoAsync(string userId);
}
