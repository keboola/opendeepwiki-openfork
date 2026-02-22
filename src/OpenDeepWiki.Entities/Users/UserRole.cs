using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User-Role association entity (many-to-many relationship)
/// </summary>
public class UserRole : AggregateRoot<string>
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Role ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// User entity navigation property
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    /// <summary>
    /// Role entity navigation property
    /// </summary>
    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }
}
