namespace OpenDeepWiki.Services.Repositories;

/// <summary>
/// Incremental update configuration options
/// </summary>
public class IncrementalUpdateOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "IncrementalUpdate";

    /// <summary>
    /// Polling interval (seconds)
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Default update check interval (minutes)
    /// </summary>
    public int DefaultUpdateIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Minimum update check interval (minutes)
    /// </summary>
    public int MinUpdateIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry base delay (milliseconds)
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Manual trigger task priority
    /// </summary>
    public int ManualTriggerPriority { get; set; } = 100;
}
