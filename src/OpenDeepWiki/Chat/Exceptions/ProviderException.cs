namespace OpenDeepWiki.Chat.Exceptions;

/// <summary>
/// Provider exception
/// </summary>
public class ProviderException : ChatException
{
    /// <summary>
    /// Platform identifier
    /// </summary>
    public string Platform { get; }
    
    public ProviderException(string platform, string message, string errorCode, bool shouldRetry = false)
        : base(message, errorCode, shouldRetry)
    {
        Platform = platform;
    }
    
    public ProviderException(string platform, string message, string errorCode, Exception innerException, bool shouldRetry = false)
        : base(message, errorCode, innerException, shouldRetry)
    {
        Platform = platform;
    }
}
