using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Incremental update task status
/// </summary>
public enum IncrementalUpdateStatus
{
    /// <summary>
    /// Pending processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Processing
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Processing completed
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Processing failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Cancelled
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// Incremental update task entity
/// Tracks the status of incremental update tasks
/// </summary>
public class IncrementalUpdateTask : AggregateRoot<string>
{
    /// <summary>
    /// Repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Branch ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string BranchId { get; set; } = string.Empty;

    /// <summary>
    /// Previously processed commit ID
    /// </summary>
    [StringLength(40)]
    public string? PreviousCommitId { get; set; }

    /// <summary>
    /// Current target commit ID
    /// </summary>
    [StringLength(40)]
    public string? TargetCommitId { get; set; }

    /// <summary>
    /// Task status
    /// </summary>
    public IncrementalUpdateStatus Status { get; set; } = IncrementalUpdateStatus.Pending;

    /// <summary>
    /// Task priority (higher value means higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether manually triggered
    /// </summary>
    public bool IsManualTrigger { get; set; } = false;

    /// <summary>
    /// Retry count
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }

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
    /// Branch navigation property
    /// </summary>
    [ForeignKey("BranchId")]
    public virtual RepositoryBranch? Branch { get; set; }
}
