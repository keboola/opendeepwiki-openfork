using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// JWT service interface
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT token
    /// </summary>
    string GenerateToken(User user, List<string> roles);

    /// <summary>
    /// Validate JWT token
    /// </summary>
    bool ValidateToken(string token, out string userId);
}
