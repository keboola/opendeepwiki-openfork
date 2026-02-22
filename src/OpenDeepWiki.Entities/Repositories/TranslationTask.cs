using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Translation task status
/// </summary>
public enum TranslationTaskStatus
{
    /// <summary>
    /// Pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Processing
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Completed
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Failed
    /// </summary>
    Failed = 3
}

/// <summary>
/// Translation task entity
/// Stores pending Wiki translation tasks, processed asynchronously by background services
/// </summary>
public class TranslationTask : AggregateRoot<string>
{
    /// <summary>
    /// Repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Repository branch ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryBranchId { get; set; } = string.Empty;

    /// <summary>
    /// Source language branch ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string SourceBranchLanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Target language code
    /// </summary>
    [Required]
    [StringLength(10)]
    public string TargetLanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Task status
    /// </summary>
    public TranslationTaskStatus Status { get; set; } = TranslationTaskStatus.Pending;

    /// <summary>
    /// Error message (recorded on failure)
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Retry count
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Processing start time
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Repository navigation property
    /// </summary>
    [ForeignKey("RepositoryId")]
    public virtual Repository? Repository { get; set; }

    /// <summary>
    /// Repository branch navigation property
    /// </summary>
    [ForeignKey("RepositoryBranchId")]
    public virtual RepositoryBranch? RepositoryBranch { get; set; }

    /// <summary>
    /// Source language branch navigation property
    /// </summary>
    [ForeignKey("SourceBranchLanguageId")]
    public virtual BranchLanguage? SourceBranchLanguage { get; set; }
}
