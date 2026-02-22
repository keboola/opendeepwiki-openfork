namespace OpenDeepWiki.Services.Repositories;

/// <summary>
/// Incremental update service interface
/// Encapsulates the core business logic for incremental updates
/// </summary>
public interface IIncrementalUpdateService
{
    /// <summary>
    /// Process incremental update for a single repository
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="branchId">Branch ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    Task<IncrementalUpdateResult> ProcessIncrementalUpdateAsync(
        string repositoryId,
        string branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if repository needs incremental update
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="branchId">Branch ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether update is needed and change information</returns>
    Task<UpdateCheckResult> CheckForUpdatesAsync(
        string repositoryId,
        string branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually trigger incremental update
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="branchId">Branch ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created task ID</returns>
    Task<string> TriggerManualUpdateAsync(
        string repositoryId,
        string branchId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Incremental update result
/// </summary>
public class IncrementalUpdateResult
{
    /// <summary>
    /// Whether successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Previous commit ID
    /// </summary>
    public string? PreviousCommitId { get; set; }

    /// <summary>
    /// Current commit ID
    /// </summary>
    public string? CurrentCommitId { get; set; }

    /// <summary>
    /// Number of changed files
    /// </summary>
    public int ChangedFilesCount { get; set; }

    /// <summary>
    /// Number of updated documents
    /// </summary>
    public int UpdatedDocumentsCount { get; set; }

    /// <summary>
    /// Processing duration
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Update check result
/// </summary>
public class UpdateCheckResult
{
    /// <summary>
    /// Whether update is needed
    /// </summary>
    public bool NeedsUpdate { get; set; }

    /// <summary>
    /// Previous commit ID
    /// </summary>
    public string? PreviousCommitId { get; set; }

    /// <summary>
    /// Current commit ID
    /// </summary>
    public string? CurrentCommitId { get; set; }

    /// <summary>
    /// List of changed files
    /// </summary>
    public string[]? ChangedFiles { get; set; }
}
