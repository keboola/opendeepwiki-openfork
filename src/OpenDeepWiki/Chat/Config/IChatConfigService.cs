namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Chat configuration service interface
/// Provides management capabilities for Provider configuration
/// </summary>
public interface IChatConfigService
{
    /// <summary>
    /// Get configuration for a specific platform
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration object, or null if not found</returns>
    Task<ProviderConfigDto?> GetConfigAsync(string platform, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all configurations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all configurations</returns>
    Task<IEnumerable<ProviderConfigDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save configuration (create or update)
    /// </summary>
    /// <param name="config">Configuration object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveConfigAsync(ProviderConfigDto config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete configuration
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteConfigAsync(string platform, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate configuration integrity
    /// </summary>
    /// <param name="config">Configuration object</param>
    /// <returns>Validation result</returns>
    ConfigValidationResult ValidateConfig(ProviderConfigDto config);
    
    /// <summary>
    /// Validate all configurations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of validation results</returns>
    Task<IEnumerable<ConfigValidationResult>> ValidateAllConfigsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Register configuration change callback
    /// </summary>
    /// <param name="callback">Callback function</param>
    /// <returns>IDisposable for unregistering</returns>
    IDisposable OnConfigChanged(Action<string> callback);
    
    /// <summary>
    /// Trigger configuration reload
    /// </summary>
    /// <param name="platform">Platform identifier, or null to reload all configurations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReloadConfigAsync(string? platform = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider configuration DTO
/// </summary>
public class ProviderConfigDto
{
    /// <summary>
    /// Platform identifier
    /// </summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Configuration data (plaintext JSON)
    /// </summary>
    public string ConfigData { get; set; } = string.Empty;
    
    /// <summary>
    /// Webhook URL
    /// </summary>
    public string? WebhookUrl { get; set; }
    
    /// <summary>
    /// Message send interval (milliseconds)
    /// </summary>
    public int MessageInterval { get; set; } = 500;
    
    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigValidationResult
{
    /// <summary>
    /// Platform identifier
    /// </summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// List of error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Missing configuration fields
    /// </summary>
    public List<string> MissingFields { get; set; } = new();
    
    /// <summary>
    /// Create a successful validation result
    /// </summary>
    public static ConfigValidationResult Success(string platform) => new()
    {
        Platform = platform,
        IsValid = true
    };
    
    /// <summary>
    /// Create a failed validation result
    /// </summary>
    public static ConfigValidationResult Failure(string platform, params string[] errors) => new()
    {
        Platform = platform,
        IsValid = false,
        Errors = errors.ToList()
    };
}
