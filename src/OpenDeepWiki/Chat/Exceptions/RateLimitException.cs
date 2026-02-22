namespace OpenDeepWiki.Chat.Exceptions;

/// <summary>
/// Rate limit exception
/// </summary>
public class RateLimitException : ProviderException
{
    /// <summary>
    /// Retry wait duration
    /// </summary>
    public TimeSpan RetryAfter { get; }
    
    public RateLimitException(string platform, TimeSpan retryAfter)
        : base(platform, "Rate limit exceeded", "RATE_LIMIT", shouldRetry: true)
    {
        RetryAfter = retryAfter;
    }
    
    public RateLimitException(string platform, TimeSpan retryAfter, string message)
        : base(platform, message, "RATE_LIMIT", shouldRetry: true)
    {
        RetryAfter = retryAfter;
    }
}
