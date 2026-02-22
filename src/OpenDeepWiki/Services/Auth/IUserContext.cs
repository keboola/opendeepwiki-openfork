using System.Security.Claims;

namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// User context interface for retrieving current logged-in user information
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Current user ID, null when not logged in
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Current user name
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Current user email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Whether the user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Get all Claims for the current user
    /// </summary>
    ClaimsPrincipal? User { get; }
}
