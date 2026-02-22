namespace OpenDeepWiki.Chat.Exceptions;

/// <summary>
/// Chat system base exception
/// </summary>
public class ChatException : Exception
{
    /// <summary>
    /// Error code
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Whether should retry
    /// </summary>
    public bool ShouldRetry { get; }
    
    public ChatException(string message, string errorCode, bool shouldRetry = false)
        : base(message)
    {
        ErrorCode = errorCode;
        ShouldRetry = shouldRetry;
    }
    
    public ChatException(string message, string errorCode, Exception innerException, bool shouldRetry = false)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ShouldRetry = shouldRetry;
    }
}
