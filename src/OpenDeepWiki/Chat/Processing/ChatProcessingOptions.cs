namespace OpenDeepWiki.Chat.Processing;

/// <summary>
/// Message processing configuration options
/// </summary>
public class ChatProcessingOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Chat:Processing";

    /// <summary>
    /// Maximum concurrent processing count
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// Queue polling interval (milliseconds)
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Delay after error (milliseconds)
    /// </summary>
    public int ErrorDelayMs { get; set; } = 5000;

    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Base retry delay (seconds)
    /// </summary>
    public int BaseRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable the Worker
    /// </summary>
    public bool Enabled { get; set; } = true;
}
