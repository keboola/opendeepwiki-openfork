namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Message queue interface
/// Used for handling sequential message sending and platform rate limiting
/// </summary>
public interface IMessageQueue
{
    /// <summary>
    /// Enqueue message
    /// </summary>
    /// <param name="message">Message to enqueue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnqueueAsync(QueuedMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dequeue message (get the next pending message)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queued message, or null if the queue is empty</returns>
    Task<QueuedMessage?> DequeueAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get queue length
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of pending messages in the queue</returns>
    Task<int> GetQueueLengthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark message as completed
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CompleteAsync(string messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark message as failed
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="reason">Failure reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FailAsync(string messageId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add message to retry queue
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="delaySeconds">Delay in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RetryAsync(string messageId, int delaySeconds = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the number of messages in the dead letter queue
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of messages in the dead letter queue</returns>
    Task<int> GetDeadLetterCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages from the dead letter queue
    /// </summary>
    /// <param name="skip">Number of messages to skip</param>
    /// <param name="take">Number of messages to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dead letter queue messages</returns>
    Task<IReadOnlyList<DeadLetterMessage>> GetDeadLetterMessagesAsync(
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocess a message from the dead letter queue
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether the operation succeeded</returns>
    Task<bool> ReprocessDeadLetterAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a message from the dead letter queue
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether the operation succeeded</returns>
    Task<bool> DeleteDeadLetterAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the dead letter queue
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of messages deleted</returns>
    Task<int> ClearDeadLetterQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Move message to dead letter queue
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="reason">Reason for moving</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MoveToDeadLetterAsync(string messageId, string reason, CancellationToken cancellationToken = default);
}
