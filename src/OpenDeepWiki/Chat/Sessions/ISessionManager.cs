namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Session manager interface
/// Responsible for session creation, lookup, update, and cleanup
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Get or create a session
    /// Returns an existing session if one exists for the specified user and platform; otherwise creates a new one
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="platform">Platform identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session instance</returns>
    Task<IChatSession> GetOrCreateSessionAsync(
        string userId, 
        string platform, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get session by session ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session instance, or null if not found</returns>
    Task<IChatSession?> GetSessionAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update session to persistent storage
    /// </summary>
    /// <param name="session">Session instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSessionAsync(
        IChatSession session, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Close session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CloseSessionAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
