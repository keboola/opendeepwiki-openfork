using OpenDeepWiki.Models.Auth;

namespace OpenDeepWiki.Services.OAuth;

/// <summary>
/// OAuth service interface
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Get OAuth authorization URL
    /// </summary>
    Task<string> GetAuthorizationUrlAsync(string providerName, string? state = null);

    /// <summary>
    /// Handle OAuth callback
    /// </summary>
    Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string? state = null);
}
