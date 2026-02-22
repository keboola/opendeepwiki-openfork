namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Message queue configuration options
/// </summary>
public class MessageQueueOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Chat:MessageQueue";
    
    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
    
    /// <summary>
    /// Default retry delay (seconds)
    /// </summary>
    public int DefaultRetryDelaySeconds { get; set; } = 30;
    
    /// <summary>
    /// Message send interval (milliseconds)
    /// </summary>
    public int MessageIntervalMs { get; set; } = 500;
    
    /// <summary>
    /// Short message merge threshold (character count)
    /// </summary>
    public int MergeThreshold { get; set; } = 500;
    
    /// <summary>
    /// Short message merge time window (milliseconds)
    /// </summary>
    public int MergeWindowMs { get; set; } = 2000;
}
