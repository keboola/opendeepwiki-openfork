namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Queue message type
/// </summary>
public enum QueuedMessageType
{
    /// <summary>
    /// Incoming message (received from platform)
    /// </summary>
    Incoming,
    
    /// <summary>
    /// Outgoing message (sent to platform)
    /// </summary>
    Outgoing,
    
    /// <summary>
    /// Retry message
    /// </summary>
    Retry
}
