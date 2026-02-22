using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenDeepWiki.EFCore;

namespace OpenDeepWiki.Services.Notifications;

/// <summary>
/// Null implementation of subscriber notification service
/// Retrieves subscriber list from database but does not send actual notifications
/// Used as default behavior when notification channels are not configured
/// </summary>
public class NullSubscriberNotificationService : ISubscriberNotificationService
{
    private readonly IContext _context;
    private readonly ILogger<NullSubscriberNotificationService> _logger;

    public NullSubscriberNotificationService(
        IContext context,
        ILogger<NullSubscriberNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifySubscribersAsync(
        RepositoryUpdateNotification notification,
        CancellationToken cancellationToken = default)
    {
        // Get subscriber list
        var subscribers = await GetSubscribersAsync(notification.RepositoryId, cancellationToken);

        if (subscribers.Count == 0)
        {
            _logger.LogDebug(
                "No subscribers found for repository {RepositoryId} ({RepositoryName})",
                notification.RepositoryId,
                notification.RepositoryName);
            return;
        }

        // Null implementation: only log, do not send actual notifications
        _logger.LogInformation(
            "Skipping notification for repository {RepositoryName} (branch: {BranchName}, commit: {CommitId}). " +
            "Would notify {SubscriberCount} subscriber(s). Changed files: {ChangedFilesCount}",
            notification.RepositoryName,
            notification.BranchName,
            notification.CommitId,
            subscribers.Count,
            notification.ChangedFilesCount);

        // Log each subscriber (debug level)
        foreach (var subscriberId in subscribers)
        {
            _logger.LogDebug(
                "Would notify subscriber {SubscriberId} about update to {RepositoryName}",
                subscriberId,
                notification.RepositoryName);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSubscribersAsync(
        string repositoryId,
        CancellationToken cancellationToken = default)
    {
        var subscribers = await _context.UserSubscriptions
            .Where(s => s.RepositoryId == repositoryId)
            .Select(s => s.UserId)
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Found {SubscriberCount} subscriber(s) for repository {RepositoryId}",
            subscribers.Count,
            repositoryId);

        return subscribers;
    }
}
