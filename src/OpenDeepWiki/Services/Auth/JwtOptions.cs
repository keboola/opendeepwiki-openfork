namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// JWT configuration options
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Secret key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Issuer
    /// </summary>
    public string Issuer { get; set; } = "OpenDeepWiki";

    /// <summary>
    /// Audience
    /// </summary>
    public string Audience { get; set; } = "OpenDeepWiki";

    /// <summary>
    /// Expiration time (minutes)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 1440; // Default 24 hours
}
