using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Repository branch entity
/// </summary>
public class RepositoryBranch : AggregateRoot<string>
{
    /// <summary>
    /// Repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Branch name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Last processed commit ID
    /// </summary>
    [StringLength(40)]
    public string? LastCommitId { get; set; }

    /// <summary>
    /// Last processed time (UTC)
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }

    /// <summary>
    /// Repository navigation property
    /// </summary>
    [ForeignKey("RepositoryId")]
    public virtual Repository? Repository { get; set; }
}
