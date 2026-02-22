namespace OpenDeepWiki.Chat.Providers.Feishu;

/// <summary>
/// Feishu Provider configuration options
/// </summary>
public class FeishuProviderOptions : ProviderOptions
{
    /// <summary>
    /// Feishu application App ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;
    
    /// <summary>
    /// Feishu application App Secret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Feishu Verification Token (used to verify Webhook requests)
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Feishu Encrypt Key (used for message encryption/decryption, optional)
    /// </summary>
    public string? EncryptKey { get; set; }
    
    /// <summary>
    /// Feishu API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://open.feishu.cn/open-apis";
    
    /// <summary>
    /// Access Token cache duration (seconds), default 7000 seconds (slightly less than Feishu's 7200-second validity)
    /// </summary>
    public int TokenCacheSeconds { get; set; } = 7000;
}
