using Microsoft.Extensions.Logging;
using OpenDeepWiki.Chat.Queue;

namespace OpenDeepWiki.Chat.Processing;

/// <summary>
/// Dead letter queue processor
/// Provides monitoring and management capabilities for the dead letter queue
/// Requirements: 10.4
/// </summary>
public interface IDeadLetterProcessor
{
    /// <summary>
    /// Get dead letter queue statistics
    /// </summary>
    Task<DeadLetterStats> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dead letter message list
    /// </summary>
    Task<IReadOnlyList<DeadLetterMessage>> GetMessagesAsync(
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocess a single dead letter message
    /// </summary>
    Task<bool> ReprocessAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch reprocess dead letter messages
    /// </summary>
    Task<int> ReprocessBatchAsync(
        IEnumerable<string> messageIds, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocess all dead letter messages
    /// </summary>
    Task<int> ReprocessAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a single dead letter message
    /// </summary>
    Task<bool> DeleteAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the dead letter queue
    /// </summary>
    Task<int> ClearAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Dead letter queue statistics
/// </summary>
public record DeadLetterStats(
    int TotalCount,
    int IncomingCount,
    int OutgoingCount,
    int RetryCount,
    DateTimeOffset? OldestMessageTime,
    DateTimeOffset? NewestMessageTime
);

/// <summary>
/// Dead letter queue processor implementation
/// </summary>
public class DeadLetterProcessor : IDeadLetterProcessor
{
    private readonly IMessageQueue _messageQueue;
    private readonly ILogger<DeadLetterProcessor> _logger;

    public DeadLetterProcessor(
        IMessageQueue messageQueue,
        ILogger<DeadLetterProcessor> logger)
    {
        _messageQueue = messageQueue;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DeadLetterStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.GetDeadLetterMessagesAsync(0, int.MaxValue, cancellationToken);
        
        var incomingCount = messages.Count(m => m.OriginalType == QueuedMessageType.Incoming);
        var outgoingCount = messages.Count(m => m.OriginalType == QueuedMessageType.Outgoing);
        var retryCount = messages.Count(m => m.OriginalType == QueuedMessageType.Retry);
        
        var oldestMessage = messages.MinBy(m => m.CreatedAt);
        var newestMessage = messages.MaxBy(m => m.FailedAt);

        return new DeadLetterStats(
            messages.Count,
            incomingCount,
            outgoingCount,
            retryCount,
            oldestMessage?.CreatedAt,
            newestMessage?.FailedAt
        );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeadLetterMessage>> GetMessagesAsync(
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default)
    {
        return await _messageQueue.GetDeadLetterMessagesAsync(skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ReprocessAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var result = await _messageQueue.ReprocessDeadLetterAsync(messageId, cancellationToken);
        if (result)
        {
            _logger.LogInformation("Dead letter message re-enqueued for processing: {MessageId}", messageId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<int> ReprocessBatchAsync(
        IEnumerable<string> messageIds, 
        CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        foreach (var messageId in messageIds)
        {
            if (await _messageQueue.ReprocessDeadLetterAsync(messageId, cancellationToken))
            {
                successCount++;
            }
        }
        
        _logger.LogInformation("Batch reprocessing of dead letter messages complete, succeeded: {SuccessCount}", successCount);
        return successCount;
    }

    /// <inheritdoc />
    public async Task<int> ReprocessAllAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.GetDeadLetterMessagesAsync(0, int.MaxValue, cancellationToken);
        var messageIds = messages.Select(m => m.Id);
        
        return await ReprocessBatchAsync(messageIds, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var result = await _messageQueue.DeleteDeadLetterAsync(messageId, cancellationToken);
        if (result)
        {
            _logger.LogInformation("Dead letter message deleted: {MessageId}", messageId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<int> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        var count = await _messageQueue.ClearDeadLetterQueueAsync(cancellationToken);
        _logger.LogInformation("Dead letter queue cleared, {Count} messages deleted", count);
        return count;
    }
}
