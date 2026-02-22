using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Role entity
/// </summary>
public class Role : AggregateRoot<string>
{
    /// <summary>
    /// Role name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system role (system roles cannot be deleted or modified)
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// User role association collection
    /// </summary>
    [NotMapped]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
