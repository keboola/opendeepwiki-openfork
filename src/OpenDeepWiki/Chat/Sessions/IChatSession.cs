using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Chat session interface
/// Maintains conversation context between user and Agent
/// </summary>
public interface IChatSession
{
    /// <summary>
    /// Session unique identifier
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// User identifier
    /// </summary>
    string UserId { get; }
    
    /// <summary>
    /// Platform identifier
    /// </summary>
    string Platform { get; }
    
    /// <summary>
    /// Session state
    /// </summary>
    SessionState State { get; }
    
    /// <summary>
    /// Conversation history
    /// </summary>
    IReadOnlyList<IChatMessage> History { get; }
    
    /// <summary>
    /// Creation time
    /// </summary>
    DateTimeOffset CreatedAt { get; }
    
    /// <summary>
    /// Last activity time
    /// </summary>
    DateTimeOffset LastActivityAt { get; }
    
    /// <summary>
    /// Session metadata
    /// </summary>
    IDictionary<string, object>? Metadata { get; }
    
    /// <summary>
    /// Add message to history
    /// </summary>
    /// <param name="message">Message to add</param>
    void AddMessage(IChatMessage message);
    
    /// <summary>
    /// Clear history
    /// </summary>
    void ClearHistory();
    
    /// <summary>
    /// Update state
    /// </summary>
    /// <param name="state">New state</param>
    void UpdateState(SessionState state);
    
    /// <summary>
    /// Update last activity time
    /// </summary>
    void Touch();
}
