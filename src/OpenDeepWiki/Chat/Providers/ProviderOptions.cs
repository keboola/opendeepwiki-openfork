namespace OpenDeepWiki.Chat.Providers;

/// <summary>
/// Provider configuration options
/// </summary>
public class ProviderOptions
{
    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Message send interval (milliseconds)
    /// </summary>
    public int MessageInterval { get; set; } = 500;
    
    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
    
    /// <summary>
    /// Retry delay base (milliseconds)
    /// </summary>
    public int RetryDelayBase { get; set; } = 1000;
    
    /// <summary>
    /// Request timeout (seconds)
    /// </summary>
    public int RequestTimeout { get; set; } = 30;
}
