namespace OpenDeepWiki.Chat.Providers.QQ;

/// <summary>
/// QQ bot Provider configuration options
/// </summary>
public class QQProviderOptions : ProviderOptions
{
    /// <summary>
    /// QQ bot App ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;
    
    /// <summary>
    /// QQ bot App Secret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// QQ bot Token (used for Webhook verification)
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// QQ Open Platform API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.sgroup.qq.com";
    
    /// <summary>
    /// Sandbox environment API base URL
    /// </summary>
    public string SandboxApiBaseUrl { get; set; } = "https://sandbox.api.sgroup.qq.com";
    
    /// <summary>
    /// Whether to use sandbox environment
    /// </summary>
    public bool UseSandbox { get; set; } = false;
    
    /// <summary>
    /// Access Token cache duration (seconds), default 7000 seconds
    /// </summary>
    public int TokenCacheSeconds { get; set; } = 7000;
    
    /// <summary>
    /// Heartbeat interval (milliseconds), default 30000ms (30 seconds)
    /// </summary>
    public int HeartbeatInterval { get; set; } = 30000;
    
    /// <summary>
    /// WebSocket reconnection interval (milliseconds)
    /// </summary>
    public int ReconnectInterval { get; set; } = 5000;
    
    /// <summary>
    /// Maximum reconnection attempts
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 10;
}
