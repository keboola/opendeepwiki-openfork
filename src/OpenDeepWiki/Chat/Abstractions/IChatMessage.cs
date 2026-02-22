namespace OpenDeepWiki.Chat.Abstractions;

/// <summary>
/// Unified message abstraction interface
/// </summary>
public interface IChatMessage
{
    /// <summary>
    /// Unique message identifier
    /// </summary>
    string MessageId { get; }
    
    /// <summary>
    /// Sender identifier (platform user ID)
    /// </summary>
    string SenderId { get; }
    
    /// <summary>
    /// Receiver identifier (optional, used for group chat scenarios)
    /// </summary>
    string? ReceiverId { get; }
    
    /// <summary>
    /// Message content
    /// </summary>
    string Content { get; }
    
    /// <summary>
    /// Message type
    /// </summary>
    ChatMessageType MessageType { get; }
    
    /// <summary>
    /// Platform source
    /// </summary>
    string Platform { get; }
    
    /// <summary>
    /// Message timestamp
    /// </summary>
    DateTimeOffset Timestamp { get; }
    
    /// <summary>
    /// Additional data (platform-specific information)
    /// </summary>
    IDictionary<string, object>? Metadata { get; }
}
