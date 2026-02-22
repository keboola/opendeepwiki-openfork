using OpenDeepWiki.Chat.Abstractions;
using OpenDeepWiki.Chat.Providers;

namespace OpenDeepWiki.Chat.Callbacks;

/// <summary>
/// Message callback interface
/// Used to send Agent responses back to users
/// </summary>
public interface IMessageCallback
{
    /// <summary>
    /// Send a message to a user
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="userId">Target user ID</param>
    /// <param name="message">Message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    Task<SendResult> SendAsync(
        string platform, 
        string userId, 
        IChatMessage message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send messages to a user in batch
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="userId">Target user ID</param>
    /// <param name="messages">Collection of messages to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of send results</returns>
    Task<IEnumerable<SendResult>> SendBatchAsync(
        string platform, 
        string userId, 
        IEnumerable<IChatMessage> messages, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send streaming messages (real-time output)
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="userId">Target user ID</param>
    /// <param name="contentStream">Content stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of send results</returns>
    IAsyncEnumerable<SendResult> SendStreamAsync(
        string platform, 
        string userId, 
        IAsyncEnumerable<string> contentStream, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Track send status
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send status</returns>
    Task<SendStatus> GetSendStatusAsync(
        string messageId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Send status
/// </summary>
public enum SendStatus
{
    /// <summary>
    /// Pending send
    /// </summary>
    Pending,
    
    /// <summary>
    /// Sending
    /// </summary>
    Sending,
    
    /// <summary>
    /// Sent successfully
    /// </summary>
    Sent,
    
    /// <summary>
    /// Send failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Retrying
    /// </summary>
    Retrying,
    
    /// <summary>
    /// Unknown
    /// </summary>
    Unknown
}
