using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDeepWiki.Chat.Abstractions;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Session manager implementation
/// Includes session caching and database persistence
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly IContext _context;
    private readonly ILogger<SessionManager> _logger;
    private readonly SessionManagerOptions _options;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    
    private record CacheEntry(IChatSession Session, DateTimeOffset ExpiresAt);
    
    public SessionManager(
        IContext context,
        ILogger<SessionManager> logger,
        IOptions<SessionManagerOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }
    
    /// <inheritdoc />
    public async Task<IChatSession> GetOrCreateSessionAsync(
        string userId,
        string platform,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        
        var cacheKey = GetCacheKey(userId, platform);
        
        // Try to get from cache
        if (_options.EnableCache && TryGetFromCache(cacheKey, out var cachedSession))
        {
            _logger.LogDebug("Session found in cache for user {UserId} on platform {Platform}", userId, platform);
            return cachedSession!;
        }

        // Look up in database
        var entity = await _context.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.MessageTimestamp))
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Platform == platform && s.State != "Closed", cancellationToken);
        
        if (entity != null)
        {
            var session = MapToSession(entity);
            AddToCache(cacheKey, session);
            _logger.LogDebug("Session loaded from database for user {UserId} on platform {Platform}", userId, platform);
            return session;
        }
        
        // Create new session
        var newEntity = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = platform,
            State = SessionState.Active.ToString(),
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.ChatSessions.Add(newEntity);
        await _context.SaveChangesAsync(cancellationToken);
        
        var newSession = new ChatSessionImpl(
            newEntity.Id.ToString(),
            userId,
            platform)
        {
            MaxHistoryCount = _options.MaxHistoryCount
        };
        
        AddToCache(cacheKey, newSession);
        _logger.LogInformation("Created new session {SessionId} for user {UserId} on platform {Platform}", 
            newEntity.Id, userId, platform);
        
        return newSession;
    }
    
    /// <inheritdoc />
    public async Task<IChatSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        
        if (!Guid.TryParse(sessionId, out var id))
        {
            _logger.LogWarning("Invalid session ID format: {SessionId}", sessionId);
            return null;
        }
        
        // Try to get from cache (lookup by ID)
        if (_options.EnableCache)
        {
            foreach (var entry in _cache.Values)
            {
                if (entry.Session.SessionId == sessionId && entry.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    return entry.Session;
                }
            }
        }
        
        // Look up in database
        var entity = await _context.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.MessageTimestamp))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        
        if (entity == null)
        {
            return null;
        }
        
        var session = MapToSession(entity);
        var cacheKey = GetCacheKey(entity.UserId, entity.Platform);
        AddToCache(cacheKey, session);
        
        return session;
    }

    /// <inheritdoc />
    public async Task UpdateSessionAsync(
        IChatSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        
        if (!Guid.TryParse(session.SessionId, out var id))
        {
            throw new ArgumentException("Invalid session ID format", nameof(session));
        }
        
        var entity = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        
        if (entity == null)
        {
            _logger.LogWarning("Session {SessionId} not found for update", session.SessionId);
            return;
        }
        
        // Update session state
        entity.State = session.State.ToString();
        entity.LastActivityAt = session.LastActivityAt.UtcDateTime;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Metadata = session.Metadata != null 
            ? JsonSerializer.Serialize(session.Metadata) 
            : null;
        
        // Sync message history - only add new messages
        var existingMessageIds = entity.Messages.Select(m => m.MessageId).ToHashSet();
        var newMessages = new List<ChatMessageHistory>();
        
        foreach (var message in session.History)
        {
            if (!existingMessageIds.Contains(message.MessageId))
            {
                newMessages.Add(new ChatMessageHistory
                {
                    Id = Guid.NewGuid(),
                    SessionId = id,
                    MessageId = message.MessageId,
                    SenderId = message.SenderId,
                    Content = message.Content,
                    MessageType = message.MessageType.ToString(),
                    Role = message.SenderId == "assistant" ? "Assistant" : "User",
                    MessageTimestamp = message.Timestamp.UtcDateTime,
                    Metadata = message.Metadata != null 
                        ? JsonSerializer.Serialize(message.Metadata) 
                        : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        
        // Add new messages to database
        if (newMessages.Count > 0)
        {
            _context.ChatMessageHistories.AddRange(newMessages);
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        // Reload entity to get the latest message list
        entity = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        
        if (entity == null) return;
        
        // Limit history message count
        if (entity.Messages.Count > _options.MaxHistoryCount)
        {
            var messagesToRemove = entity.Messages
                .OrderBy(m => m.MessageTimestamp)
                .Take(entity.Messages.Count - _options.MaxHistoryCount)
                .ToList();
            
            _context.ChatMessageHistories.RemoveRange(messagesToRemove);
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        // Update cache
        var cacheKey = GetCacheKey(session.UserId, session.Platform);
        AddToCache(cacheKey, session);
        
        _logger.LogDebug("Updated session {SessionId}", session.SessionId);
    }
    
    /// <inheritdoc />
    public async Task CloseSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        
        if (!Guid.TryParse(sessionId, out var id))
        {
            _logger.LogWarning("Invalid session ID format: {SessionId}", sessionId);
            return;
        }
        
        var entity = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        
        if (entity == null)
        {
            _logger.LogWarning("Session {SessionId} not found for closing", sessionId);
            return;
        }
        
        entity.State = SessionState.Closed.ToString();
        entity.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        
        // Remove from cache
        var cacheKey = GetCacheKey(entity.UserId, entity.Platform);
        _cache.TryRemove(cacheKey, out _);
        
        _logger.LogInformation("Closed session {SessionId}", sessionId);
    }

    /// <inheritdoc />
    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var expirationTime = DateTime.UtcNow.AddMinutes(-_options.SessionExpirationMinutes);
        
        var expiredSessions = await _context.ChatSessions
            .Where(s => s.State != "Closed" && s.LastActivityAt < expirationTime)
            .ToListAsync(cancellationToken);
        
        foreach (var session in expiredSessions)
        {
            session.State = SessionState.Expired.ToString();
            session.UpdatedAt = DateTime.UtcNow;
            
            // Remove from cache
            var cacheKey = GetCacheKey(session.UserId, session.Platform);
            _cache.TryRemove(cacheKey, out _);
        }
        
        if (expiredSessions.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Marked {Count} sessions as expired", expiredSessions.Count);
        }
        
        // Clean up expired cache entries
        CleanupExpiredCacheEntries();
    }
    
    private static string GetCacheKey(string userId, string platform) => $"{platform}:{userId}";
    
    private bool TryGetFromCache(string cacheKey, out IChatSession? session)
    {
        if (_cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            session = entry.Session;
            return true;
        }
        
        session = null;
        return false;
    }
    
    private void AddToCache(string cacheKey, IChatSession session)
    {
        if (!_options.EnableCache) return;
        
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.CacheExpirationMinutes);
        _cache[cacheKey] = new CacheEntry(session, expiresAt);
    }
    
    private void CleanupExpiredCacheEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }
    
    private ChatSessionImpl MapToSession(ChatSession entity)
    {
        var state = Enum.TryParse<SessionState>(entity.State, out var s) ? s : SessionState.Active;
        
        var history = entity.Messages
            .OrderBy(m => m.MessageTimestamp)
            .Select(m => new ChatMessage
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                Content = m.Content,
                MessageType = Enum.TryParse<ChatMessageType>(m.MessageType, out var mt) ? mt : ChatMessageType.Text,
                Platform = entity.Platform,
                Timestamp = new DateTimeOffset(m.MessageTimestamp, TimeSpan.Zero),
                Metadata = !string.IsNullOrEmpty(m.Metadata) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(m.Metadata) 
                    : null
            })
            .Cast<IChatMessage>()
            .ToList();
        
        var metadata = !string.IsNullOrEmpty(entity.Metadata)
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Metadata)
            : null;
        
        return new ChatSessionImpl(
            entity.Id.ToString(),
            entity.UserId,
            entity.Platform,
            state,
            new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
            new DateTimeOffset(entity.LastActivityAt, TimeSpan.Zero),
            history,
            metadata)
        {
            MaxHistoryCount = _options.MaxHistoryCount
        };
    }
}
