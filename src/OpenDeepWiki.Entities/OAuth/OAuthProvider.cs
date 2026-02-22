using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// OAuth2 provider entity
/// </summary>
public class OAuthProvider : AggregateRoot<string>
{
    /// <summary>
    /// Provider name (e.g.: github, google, microsoft, feishu, gitee)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider display name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 authorization endpoint URL
    /// </summary>
    [Required]
    [StringLength(500)]
    public string AuthorizationUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 token endpoint URL
    /// </summary>
    [Required]
    [StringLength(500)]
    public string TokenUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 user info endpoint URL
    /// </summary>
    [Required]
    [StringLength(500)]
    public string UserInfoUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client ID
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client secret (encrypted storage)
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Callback URL
    /// </summary>
    [Required]
    [StringLength(500)]
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Authorization scope (multiple scopes separated by spaces)
    /// </summary>
    [StringLength(500)]
    public string? Scope { get; set; }

    /// <summary>
    /// User info mapping configuration (JSON format)
    /// </summary>
    /// <example>
    /// {
    ///   "id": "id",
    ///   "name": "name",
    ///   "email": "email",
    ///   "avatar": "avatar_url"
    /// }
    /// </example>
    [StringLength(1000)]
    public string? UserInfoMapping { get; set; }

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether email verification is required
    /// </summary>
    public bool RequireEmailVerification { get; set; } = false;

    /// <summary>
    /// OAuth user association collection
    /// </summary>
    [NotMapped]
    public virtual ICollection<UserOAuth> UserOAuths { get; set; } = new List<UserOAuth>();
}
