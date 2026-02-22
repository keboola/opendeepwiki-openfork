using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models;

/// <summary>
/// Repository submit request
/// </summary>
public class RepositorySubmitRequest
{
    /// <summary>
    /// Git URL
    /// </summary>
    [Required]
    [StringLength(500)]
    public string GitUrl { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// Repository organization
    /// </summary>
    [Required]
    [StringLength(100)]
    public string OrgName { get; set; } = string.Empty;

    /// <summary>
    /// Authentication account
    /// </summary>
    [StringLength(200)]
    public string? AuthAccount { get; set; }

    /// <summary>
    /// Authentication password (stored in plaintext)
    /// </summary>
    [StringLength(500)]
    public string? AuthPassword { get; set; }

    /// <summary>
    /// Repository branch
    /// </summary>
    [Required]
    [StringLength(200)]
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Current generation language for the repository
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether the repository is public
    /// </summary>
    public bool IsPublic { get; set; } = true;
}
