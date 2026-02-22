using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User-Department association entity
/// </summary>
public class UserDepartment : AggregateRoot<string>
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Department ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// Whether department manager
    /// </summary>
    public bool IsManager { get; set; } = false;

    /// <summary>
    /// User navigation property
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    /// <summary>
    /// Department navigation property
    /// </summary>
    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }
}
