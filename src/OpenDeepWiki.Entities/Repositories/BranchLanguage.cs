using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Branch language entity
/// </summary>
public class BranchLanguage : AggregateRoot<string>
{
    /// <summary>
    /// Repository branch ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryBranchId { get; set; } = string.Empty;

    /// <summary>
    /// Language code
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Language update summary
    /// </summary>
    [StringLength(2000)]
    public string? UpdateSummary { get; set; }

    /// <summary>
    /// Whether this is the default language
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Project architecture mind map content
    /// Format: # Level 1 heading\n## Level 2 heading:file path\n### Level 3 heading
    /// </summary>
    public string? MindMapContent { get; set; }

    /// <summary>
    /// Mind map generation status
    /// </summary>
    public MindMapStatus MindMapStatus { get; set; } = MindMapStatus.Pending;

    /// <summary>
    /// Repository branch navigation property
    /// </summary>
    [ForeignKey("RepositoryBranchId")]
    public virtual RepositoryBranch? RepositoryBranch { get; set; }
}

/// <summary>
/// Mind map generation status
/// </summary>
public enum MindMapStatus
{
    /// <summary>
    /// Pending generation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Generating
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Generation completed
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Generation failed
    /// </summary>
    Failed = 3
}
