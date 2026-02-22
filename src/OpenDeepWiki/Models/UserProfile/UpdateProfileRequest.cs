using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models.UserProfile;

/// <summary>
/// Update profile request
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// Username
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Username length must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    [StringLength(500)]
    public string? Avatar { get; set; }
}

/// <summary>
/// Change password request
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// User settings DTO
/// </summary>
public class UserSettingsDto
{
    /// <summary>
    /// Theme: light, dark, system
    /// </summary>
    public string Theme { get; set; } = "system";

    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = "zh";

    /// <summary>
    /// Whether email notifications are enabled
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Whether push notifications are enabled
    /// </summary>
    public bool PushNotifications { get; set; } = false;
}
