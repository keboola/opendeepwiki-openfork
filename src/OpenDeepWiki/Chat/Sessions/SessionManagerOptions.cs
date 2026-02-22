namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Session manager configuration options
/// </summary>
public class SessionManagerOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Chat:Session";
    
    /// <summary>
    /// Maximum history message count, default 100
    /// </summary>
    public int MaxHistoryCount { get; set; } = 100;
    
    /// <summary>
    /// Session expiration time (minutes), default 30 minutes
    /// </summary>
    public int SessionExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Cache expiration time (minutes), default 10 minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 10;
    
    /// <summary>
    /// Whether to enable caching, enabled by default
    /// </summary>
    public bool EnableCache { get; set; } = true;
}
