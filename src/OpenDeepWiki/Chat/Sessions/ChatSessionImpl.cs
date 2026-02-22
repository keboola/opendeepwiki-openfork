using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Chat session implementation class
/// Maintains conversation context between user and Agent
/// </summary>
public class ChatSessionImpl : IChatSession
{
    private readonly List<IChatMessage> _history = [];
    private readonly object _lock = new();
    private SessionState _state;
    private DateTimeOffset _lastActivityAt;
    
    /// <summary>
    /// Maximum history message count, default 100
    /// </summary>
    public int MaxHistoryCount { get; set; } = 100;
    
    /// <inheritdoc />
    public string SessionId { get; }
    
    /// <inheritdoc />
    public string UserId { get; }
    
    /// <inheritdoc />
    public string Platform { get; }
    
    /// <inheritdoc />
    public SessionState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IChatMessage> History
    {
        get
        {
            lock (_lock)
            {
                return _history.ToList().AsReadOnly();
            }
        }
    }
    
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; }
    
    /// <inheritdoc />
    public DateTimeOffset LastActivityAt
    {
        get
        {
            lock (_lock)
            {
                return _lastActivityAt;
            }
        }
    }
    
    /// <inheritdoc />
    public IDictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Create a new session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="platform">Platform identifier</param>
    public ChatSessionImpl(string sessionId, string userId, string platform)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Platform = platform ?? throw new ArgumentNullException(nameof(platform));
        CreatedAt = DateTimeOffset.UtcNow;
        _lastActivityAt = CreatedAt;
        _state = SessionState.Active;
    }
    
    /// <summary>
    /// Restore session from existing data
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="platform">Platform identifier</param>
    /// <param name="state">Session state</param>
    /// <param name="createdAt">Creation time</param>
    /// <param name="lastActivityAt">Last activity time</param>
    /// <param name="history">Message history</param>
    /// <param name="metadata">Metadata</param>
    public ChatSessionImpl(
        string sessionId,
        string userId,
        string platform,
        SessionState state,
        DateTimeOffset createdAt,
        DateTimeOffset lastActivityAt,
        IEnumerable<IChatMessage>? history = null,
        IDictionary<string, object>? metadata = null)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _state = state;
        CreatedAt = createdAt;
        _lastActivityAt = lastActivityAt;
        Metadata = metadata;
        
        if (history != null)
        {
            _history.AddRange(history);
        }
    }
    
    /// <inheritdoc />
    public void AddMessage(IChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        
        lock (_lock)
        {
            _history.Add(message);
            
            // If exceeding maximum history count, remove the earliest messages
            while (_history.Count > MaxHistoryCount && MaxHistoryCount > 0)
            {
                _history.RemoveAt(0);
            }
            
            _lastActivityAt = DateTimeOffset.UtcNow;
        }
    }
    
    /// <inheritdoc />
    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
            _lastActivityAt = DateTimeOffset.UtcNow;
        }
    }
    
    /// <inheritdoc />
    public void UpdateState(SessionState state)
    {
        lock (_lock)
        {
            _state = state;
            _lastActivityAt = DateTimeOffset.UtcNow;
        }
    }
    
    /// <inheritdoc />
    public void Touch()
    {
        lock (_lock)
        {
            _lastActivityAt = DateTimeOffset.UtcNow;
        }
    }
}
