namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Session state enum
/// </summary>
public enum SessionState
{
    /// <summary>
    /// Active state, can receive and process messages
    /// </summary>
    Active,
    
    /// <summary>
    /// Processing, Agent is executing
    /// </summary>
    Processing,
    
    /// <summary>
    /// Waiting, awaiting user response
    /// </summary>
    Waiting,
    
    /// <summary>
    /// Expired, exceeded configured expiration time
    /// </summary>
    Expired,
    
    /// <summary>
    /// Closed, session has ended
    /// </summary>
    Closed
}
