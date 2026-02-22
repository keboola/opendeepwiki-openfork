namespace OpenDeepWiki.Models.Auth;

/// <summary>
/// Login response
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Expiration time (in seconds)
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public UserInfo User { get; set; } = null!;
}

/// <summary>
/// User information
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public List<string> Roles { get; set; } = new();
}
