namespace OpenDeepWiki.Chat.Exceptions;

/// <summary>
/// Message send exception
/// </summary>
public class MessageSendException : ProviderException
{
    /// <summary>
    /// Message ID
    /// </summary>
    public string? MessageId { get; }
    
    public MessageSendException(string platform, string? messageId, string message, string errorCode, bool shouldRetry = true)
        : base(platform, message, errorCode, shouldRetry)
    {
        MessageId = messageId;
    }
    
    public MessageSendException(string platform, string? messageId, string message, string errorCode, Exception innerException, bool shouldRetry = true)
        : base(platform, message, errorCode, innerException, shouldRetry)
    {
        MessageId = messageId;
    }
}
