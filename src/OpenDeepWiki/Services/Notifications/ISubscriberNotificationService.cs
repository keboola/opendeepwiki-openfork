namespace OpenDeepWiki.Services.Notifications;

/// <summary>
/// Subscriber notification service interface
/// Responsible for notifying subscribed users after repository updates
/// </summary>
public interface ISubscriberNotificationService
{
    /// <summary>
    /// Send repository update notification to all subscribers
    /// </summary>
    /// <param name="notification">Notification content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifySubscribersAsync(
        RepositoryUpdateNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all subscribers for a repository
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subscriber IDs</returns>
    Task<IReadOnlyList<string>> GetSubscribersAsync(
        string repositoryId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository update notification
/// </summary>
public class RepositoryUpdateNotification
{
    /// <summary>
    /// Repository ID
    /// </summary>
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Repository name (org/repo)
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Branch name
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Update summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Number of changed files
    /// </summary>
    public int ChangedFilesCount { get; set; }

    /// <summary>
    /// Update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// New commit ID
    /// </summary>
    public string CommitId { get; set; } = string.Empty;
}
