using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models;

/// <summary>
/// Repository assignment request
/// </summary>
public class RepositoryAssignRequest
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
}
