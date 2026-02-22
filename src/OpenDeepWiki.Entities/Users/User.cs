using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User entity
/// </summary>
public class User : AggregateRoot<string>
{
    /// <summary>
    /// Username
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password (encrypted storage)
    /// </summary>
    [StringLength(255)]
    public string? Password { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    [StringLength(500)]
    public string? Avatar { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// User status: 0-disabled, 1-active, 2-pending verification
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// Whether this is a system user
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Last login IP
    /// </summary>
    [StringLength(50)]
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// User role association collection
    /// </summary>
    [NotMapped]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// OAuth login association collection
    /// </summary>
    [NotMapped]
    public virtual ICollection<UserOAuth> UserOAuths { get; set; } = new List<UserOAuth>();
}
