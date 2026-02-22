using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Queued message record type
/// </summary>
/// <param name="Id">Message unique identifier</param>
/// <param name="Message">Chat message</param>
/// <param name="SessionId">Associated session ID</param>
/// <param name="TargetUserId">Target user ID</param>
/// <param name="Type">Queue message type</param>
/// <param name="RetryCount">Retry count</param>
/// <param name="ScheduledAt">Scheduled execution time</param>
public record QueuedMessage(
    string Id,
    IChatMessage Message,
    string SessionId,
    string TargetUserId,
    QueuedMessageType Type,
    int RetryCount = 0,
    DateTimeOffset? ScheduledAt = null
);
