using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models;

/// <summary>
/// Update repository visibility request
/// </summary>
public class UpdateVisibilityRequest
{
    /// <summary>
    /// Repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the repository is public
    /// </summary>
    public bool IsPublic { get; set; }
}
