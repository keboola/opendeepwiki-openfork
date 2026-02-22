using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models.Auth;

/// <summary>
/// Login request
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password length must be between 6 and 100")]
    public string Password { get; set; } = string.Empty;
}
