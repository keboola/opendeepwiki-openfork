using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Repository assignment entity
/// </summary>
public class RepositoryAssignment : AggregateRoot<string>
{
    /// <summary>
    /// Repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Department ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// Assignee user ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string AssigneeUserId { get; set; } = string.Empty;

    /// <summary>
    /// Repository navigation property
    /// </summary>
    [ForeignKey("RepositoryId")]
    public virtual Repository? Repository { get; set; }

    /// <summary>
    /// Department navigation property
    /// </summary>
    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    /// <summary>
    /// Assignee user navigation property
    /// </summary>
    [ForeignKey("AssigneeUserId")]
    public virtual User? AssigneeUser { get; set; }
}
