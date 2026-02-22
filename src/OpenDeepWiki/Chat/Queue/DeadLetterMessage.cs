using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Dead letter queue message record type
/// </summary>
/// <param name="Id">Message unique identifier</param>
/// <param name="Message">Original chat message</param>
/// <param name="SessionId">Associated session ID</param>
/// <param name="TargetUserId">Target user ID</param>
/// <param name="OriginalType">Original queue message type</param>
/// <param name="RetryCount">Retry count</param>
/// <param name="ErrorMessage">Error message</param>
/// <param name="FailedAt">Failure time</param>
/// <param name="CreatedAt">Creation time</param>
public record DeadLetterMessage(
    string Id,
    IChatMessage Message,
    string SessionId,
    string TargetUserId,
    QueuedMessageType OriginalType,
    int RetryCount,
    string? ErrorMessage,
    DateTimeOffset FailedAt,
    DateTimeOffset CreatedAt
);
