namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Chat configuration options
/// </summary>
public class ChatConfigOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Chat";
    
    /// <summary>
    /// Whether to validate configuration on startup
    /// </summary>
    public bool ValidateOnStartup { get; set; } = true;
    
    /// <summary>
    /// Configuration cache expiration time (seconds)
    /// </summary>
    public int CacheExpirationSeconds { get; set; } = 300;
    
    /// <summary>
    /// Whether to enable configuration hot-reload
    /// </summary>
    public bool EnableHotReload { get; set; } = true;
}
