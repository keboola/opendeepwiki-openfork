using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User OAuth login association entity
/// </summary>
public class UserOAuth : AggregateRoot<string>
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth provider ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string OAuthProviderId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth provider user ID (third-party platform user ID)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string OAuthUserId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth provider user name
    /// </summary>
    [StringLength(200)]
    public string? OAuthUserName { get; set; }

    /// <summary>
    /// OAuth provider user email
    /// </summary>
    [StringLength(200)]
    public string? OAuthUserEmail { get; set; }

    /// <summary>
    /// OAuth provider user avatar
    /// </summary>
    [StringLength(500)]
    public string? OAuthUserAvatar { get; set; }

    /// <summary>
    /// Access token (encrypted storage)
    /// </summary>
    [StringLength(1000)]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token (encrypted storage)
    /// </summary>
    [StringLength(1000)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// Token scope
    /// </summary>
    [StringLength(500)]
    public string? Scope { get; set; }

    /// <summary>
    /// Token type
    /// </summary>
    [StringLength(50)]
    public string? TokenType { get; set; }

    /// <summary>
    /// Whether bound (true means bound to an existing user, false means temporarily bound)
    /// </summary>
    public bool IsBound { get; set; } = false;

    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// User entity navigation property
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    /// <summary>
    /// OAuth provider entity navigation property
    /// </summary>
    [ForeignKey("OAuthProviderId")]
    public virtual OAuthProvider? OAuthProvider { get; set; }
}
