using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Providers;

/// <summary>
/// Provider base class, provides common implementation
/// </summary>
public abstract class BaseMessageProvider : IMessageProvider
{
    protected readonly ILogger Logger;
    protected readonly IOptions<ProviderOptions> Options;
    
    public abstract string PlatformId { get; }
    public abstract string DisplayName { get; }
    public virtual bool IsEnabled => Options.Value.Enabled;
    
    protected BaseMessageProvider(ILogger logger, IOptions<ProviderOptions> options)
    {
        Logger = logger;
        Options = options;
    }
    
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing {Provider}", DisplayName);
        return Task.CompletedTask;
    }
    
    public abstract Task<IChatMessage?> ParseMessageAsync(string rawMessage, CancellationToken cancellationToken = default);
    
    public abstract Task<SendResult> SendMessageAsync(IChatMessage message, string targetUserId, CancellationToken cancellationToken = default);
    
    public virtual async Task<IEnumerable<SendResult>> SendMessagesAsync(
        IEnumerable<IChatMessage> messages, 
        string targetUserId, 
        CancellationToken cancellationToken = default)
    {
        var results = new List<SendResult>();
        foreach (var message in messages)
        {
            var result = await SendMessageAsync(message, targetUserId, cancellationToken);
            results.Add(result);
            
            if (!result.Success && !result.ShouldRetry)
                break;
                
            // Default message interval
            await Task.Delay(Options.Value.MessageInterval, cancellationToken);
        }
        return results;
    }
    
    public virtual Task<WebhookValidationResult> ValidateWebhookAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WebhookValidationResult(true));
    }
    
    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Shutting down {Provider}", DisplayName);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Message type degradation handling
    /// When a message type is not supported by the target platform, degrade it to a text message
    /// </summary>
    /// <param name="message">Original message</param>
    /// <param name="supportedTypes">Set of message types supported by the target platform</param>
    /// <returns>Degraded message (if degradation is needed) or the original message</returns>
    protected virtual IChatMessage DegradeMessage(IChatMessage message, ISet<ChatMessageType>? supportedTypes = null)
    {
        // Text messages do not need degradation
        if (message.MessageType == ChatMessageType.Text)
            return message;
        
        // If no supported types specified, default to text only
        supportedTypes ??= new HashSet<ChatMessageType> { ChatMessageType.Text };
        
        // If the message type is supported, no degradation needed
        if (supportedTypes.Contains(message.MessageType))
            return message;
            
        Logger.LogWarning(
            "Message type {Type} not supported by platform {Platform}, degrading to text", 
            message.MessageType, 
            PlatformId);
        
        return new ChatMessage
        {
            MessageId = message.MessageId,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            Content = $"[{message.MessageType}] {message.Content}",
            MessageType = ChatMessageType.Text,
            Platform = message.Platform,
            Timestamp = message.Timestamp,
            Metadata = message.Metadata
        };
    }
}
