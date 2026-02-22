using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Providers.QQ;

/// <summary>
/// QQ bot message Provider implementation
/// Supports channel messages, group chat messages, and C2C direct messages
/// </summary>
public class QQProvider : BaseMessageProvider
{
    private readonly HttpClient _httpClient;
    private readonly QQProviderOptions _qqOptions;
    private string? _accessToken;
    private DateTime _tokenExpireTime = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    
    /// <summary>
    /// Message types supported by QQ
    /// </summary>
    private static readonly HashSet<ChatMessageType> SupportedMessageTypes = new()
    {
        ChatMessageType.Text,
        ChatMessageType.Image,
        ChatMessageType.RichText,
        ChatMessageType.Card
    };
    
    public override string PlatformId => "qq";
    public override string DisplayName => "QQ Bot";
    
    public QQProvider(
        ILogger<QQProvider> logger,
        IOptions<QQProviderOptions> options,
        HttpClient httpClient)
        : base(logger, options)
    {
        _httpClient = httpClient;
        _qqOptions = options.Value;
    }
    
    /// <summary>
    /// Initialize Provider and obtain Access Token
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await base.InitializeAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(_qqOptions.AppId) || string.IsNullOrEmpty(_qqOptions.AppSecret))
        {
            Logger.LogWarning("QQ AppId or AppSecret not configured, provider will not be fully functional");
            return;
        }
        
        try
        {
            await GetAccessTokenAsync(cancellationToken);
            Logger.LogInformation("QQ provider initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize QQ provider");
        }
    }
    
    /// <summary>
    /// Parse raw QQ message into unified format
    /// </summary>
    public override async Task<IChatMessage?> ParseMessageAsync(string rawMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookEvent = JsonSerializer.Deserialize<QQWebhookEvent>(rawMessage);
            if (webhookEvent == null)
            {
                Logger.LogWarning("Failed to deserialize QQ webhook event");
                return null;
            }
            
            // Only process message events
            var eventType = webhookEvent.EventType;
            if (!IsMessageEvent(eventType))
            {
                Logger.LogDebug("Ignoring non-message event: {EventType}", eventType);
                return null;
            }
            
            return eventType switch
            {
                QQEventType.GroupAtMessageCreate => ParseGroupMessage(webhookEvent),
                QQEventType.C2CMessageCreate => ParseC2CMessage(webhookEvent),
                QQEventType.AtMessageCreate or QQEventType.MessageCreate => ParseChannelMessage(webhookEvent),
                QQEventType.DirectMessageCreate => ParseDirectMessage(webhookEvent),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse QQ message");
            return null;
        }
    }
    
    /// <summary>
    /// Send message to QQ
    /// </summary>
    public override async Task<SendResult> SendMessageAsync(
        IChatMessage message, 
        string targetUserId, 
        CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(async () =>
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            
            // Downgrade unsupported message types
            var processedMessage = DegradeMessage(message, SupportedMessageTypes);
            
            // Determine message type based on target user ID format
            var (messageType, targetId, groupOpenId) = ParseTargetUserId(targetUserId);
            
            return messageType switch
            {
                QQMessageTargetType.Group => await SendGroupMessageAsync(processedMessage, groupOpenId!, targetId, token, cancellationToken),
                QQMessageTargetType.C2C => await SendC2CMessageAsync(processedMessage, targetId, token, cancellationToken),
                QQMessageTargetType.Channel => await SendChannelMessageAsync(processedMessage, targetId, token, cancellationToken),
                _ => new SendResult(false, ErrorCode: "UNKNOWN_TARGET_TYPE", ErrorMessage: "Unknown target user type")
            };
        }, cancellationToken);
    }
    
    /// <summary>
    /// Validate QQ Webhook request
    /// </summary>
    public override async Task<WebhookValidationResult> ValidateWebhookAsync(
        HttpRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync(cancellationToken);
            request.Body.Position = 0;
            
            var webhookEvent = JsonSerializer.Deserialize<QQWebhookEvent>(body);
            if (webhookEvent == null)
            {
                return new WebhookValidationResult(false, ErrorMessage: "Invalid request body");
            }
            
            // Handle HTTP callback validation request (OpCode 13)
            if (webhookEvent.OpCode == 13)
            {
                // Validation request requires a specific response format
                return new WebhookValidationResult(true, Challenge: webhookEvent.Data?.Id);
            }
            
            return new WebhookValidationResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to validate QQ webhook");
            return new WebhookValidationResult(false, ErrorMessage: ex.Message);
        }
    }

    
    #region Message parsing methods
    
    /// <summary>
    /// Determine whether the event is a message event
    /// </summary>
    private static bool IsMessageEvent(string? eventType)
    {
        return eventType is QQEventType.AtMessageCreate 
            or QQEventType.MessageCreate 
            or QQEventType.DirectMessageCreate
            or QQEventType.GroupAtMessageCreate
            or QQEventType.C2CMessageCreate;
    }
    
    /// <summary>
    /// Parse group chat message
    /// </summary>
    private IChatMessage? ParseGroupMessage(QQWebhookEvent webhookEvent)
    {
        var data = webhookEvent.Data;
        if (data == null) return null;
        
        var senderId = data.Author?.MemberOpenId ?? data.Author?.Id ?? string.Empty;
        var content = CleanMentions(data.Content);
        var messageType = DetermineMessageType(data);
        
        return new ChatMessage
        {
            MessageId = data.Id,
            SenderId = senderId,
            ReceiverId = data.GroupOpenId,
            Content = content,
            MessageType = messageType,
            Platform = PlatformId,
            Timestamp = ParseTimestamp(data.Timestamp),
            Metadata = new Dictionary<string, object>
            {
                { "event_type", QQEventType.GroupAtMessageCreate },
                { "group_openid", data.GroupOpenId ?? string.Empty },
                { "msg_seq", data.MsgSeq ?? 0 },
                { "mentions", data.Mentions ?? new List<QQMention>() },
                { "raw_msg_id", data.Id }
            }
        };
    }
    
    /// <summary>
    /// Parse C2C direct message
    /// </summary>
    private IChatMessage? ParseC2CMessage(QQWebhookEvent webhookEvent)
    {
        var data = webhookEvent.Data;
        if (data == null) return null;
        
        // The sender ID for C2C messages is in author.user_openid
        var senderId = data.Author?.MemberOpenId ?? data.Author?.Id ?? string.Empty;
        var messageType = DetermineMessageType(data);
        
        return new ChatMessage
        {
            MessageId = data.Id,
            SenderId = senderId,
            ReceiverId = null,
            Content = data.Content,
            MessageType = messageType,
            Platform = PlatformId,
            Timestamp = ParseTimestamp(data.Timestamp),
            Metadata = new Dictionary<string, object>
            {
                { "event_type", QQEventType.C2CMessageCreate },
                { "msg_seq", data.MsgSeq ?? 0 },
                { "raw_msg_id", data.Id }
            }
        };
    }
    
    /// <summary>
    /// Parse channel message
    /// </summary>
    private IChatMessage? ParseChannelMessage(QQWebhookEvent webhookEvent)
    {
        var data = webhookEvent.Data;
        if (data == null) return null;
        
        var senderId = data.Author?.Id ?? string.Empty;
        var content = CleanMentions(data.Content);
        var messageType = DetermineMessageType(data);
        
        return new ChatMessage
        {
            MessageId = data.Id,
            SenderId = senderId,
            ReceiverId = data.ChannelId,
            Content = content,
            MessageType = messageType,
            Platform = PlatformId,
            Timestamp = ParseTimestamp(data.Timestamp),
            Metadata = new Dictionary<string, object>
            {
                { "event_type", webhookEvent.EventType ?? string.Empty },
                { "channel_id", data.ChannelId ?? string.Empty },
                { "guild_id", data.GuildId ?? string.Empty },
                { "mentions", data.Mentions ?? new List<QQMention>() },
                { "raw_msg_id", data.Id }
            }
        };
    }
    
    /// <summary>
    /// Parse direct message
    /// </summary>
    private IChatMessage? ParseDirectMessage(QQWebhookEvent webhookEvent)
    {
        var data = webhookEvent.Data;
        if (data == null) return null;
        
        var senderId = data.Author?.Id ?? string.Empty;
        var messageType = DetermineMessageType(data);
        
        return new ChatMessage
        {
            MessageId = data.Id,
            SenderId = senderId,
            ReceiverId = data.GuildId,
            Content = data.Content,
            MessageType = messageType,
            Platform = PlatformId,
            Timestamp = ParseTimestamp(data.Timestamp),
            Metadata = new Dictionary<string, object>
            {
                { "event_type", QQEventType.DirectMessageCreate },
                { "guild_id", data.GuildId ?? string.Empty },
                { "raw_msg_id", data.Id }
            }
        };
    }
    
    /// <summary>
    /// Clean @ mention tags from message content
    /// </summary>
    private static string CleanMentions(string content)
    {
        if (string.IsNullOrEmpty(content)) return content;
        
        // Remove @ mentions in <@!userID> format
        var cleaned = System.Text.RegularExpressions.Regex.Replace(content, @"<@!\d+>", "");
        // Remove @ mentions in <@userID> format
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<@\d+>", "");
        
        return cleaned.Trim();
    }
    
    /// <summary>
    /// Determine message type based on message content
    /// </summary>
    private static ChatMessageType DetermineMessageType(QQEventData data)
    {
        if (data.Attachments != null && data.Attachments.Count > 0)
        {
            var attachment = data.Attachments[0];
            if (attachment.ContentType.StartsWith("image/"))
                return ChatMessageType.Image;
            if (attachment.ContentType.StartsWith("audio/"))
                return ChatMessageType.Audio;
            if (attachment.ContentType.StartsWith("video/"))
                return ChatMessageType.Video;
            return ChatMessageType.File;
        }
        
        return ChatMessageType.Text;
    }
    
    #endregion
    
    #region Message sending methods
    
    /// <summary>
    /// Send group chat message
    /// </summary>
    private async Task<SendResult> SendGroupMessageAsync(
        IChatMessage message,
        string groupOpenId,
        string? msgId,
        string token,
        CancellationToken cancellationToken)
    {
        var request = new QQSendGroupMessageRequest
        {
            Content = message.Content,
            MsgType = ConvertToQQMsgType(message.MessageType),
            MsgId = msgId
        };
        
        // If there is an original message sequence number, add it to the request (passive reply)
        if (message.Metadata?.TryGetValue("msg_seq", out var msgSeqObj) == true && msgSeqObj is int msgSeq)
        {
            request.MsgSeq = msgSeq;
        }
        
        var url = $"{GetApiBaseUrl()}/v2/groups/{groupOpenId}/messages";
        return await SendApiRequestAsync<QQSendGroupMessageRequest, QQSendMessageResponse>(url, request, token, cancellationToken);
    }
    
    /// <summary>
    /// Send C2C direct message
    /// </summary>
    private async Task<SendResult> SendC2CMessageAsync(
        IChatMessage message,
        string userOpenId,
        string token,
        CancellationToken cancellationToken)
    {
        var request = new QQSendC2CMessageRequest
        {
            Content = message.Content,
            MsgType = ConvertToQQMsgType(message.MessageType)
        };
        
        // If there is an original message ID, add it to the request (passive reply)
        if (message.Metadata?.TryGetValue("raw_msg_id", out var rawMsgId) == true && rawMsgId is string msgId)
        {
            request.MsgId = msgId;
        }
        
        var url = $"{GetApiBaseUrl()}/v2/users/{userOpenId}/messages";
        return await SendApiRequestAsync<QQSendC2CMessageRequest, QQSendMessageResponse>(url, request, token, cancellationToken);
    }
    
    /// <summary>
    /// Send channel message
    /// </summary>
    private async Task<SendResult> SendChannelMessageAsync(
        IChatMessage message,
        string channelId,
        string token,
        CancellationToken cancellationToken)
    {
        var request = new QQSendChannelMessageRequest
        {
            Content = message.Content
        };
        
        // If there is an original message ID, add it to the request (passive reply)
        if (message.Metadata?.TryGetValue("raw_msg_id", out var rawMsgId) == true && rawMsgId is string msgId)
        {
            request.MsgId = msgId;
        }
        
        // Handle image message
        if (message.MessageType == ChatMessageType.Image)
        {
            request.Image = message.Content;
            request.Content = null;
        }
        
        var url = $"{GetApiBaseUrl()}/channels/{channelId}/messages";
        return await SendApiRequestAsync<QQSendChannelMessageRequest, QQSendMessageResponse>(url, request, token, cancellationToken);
    }
    
    /// <summary>
    /// Send API request
    /// </summary>
    private async Task<SendResult> SendApiRequestAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        string token,
        CancellationToken cancellationToken)
        where TResponse : QQSendMessageResponse
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("QQBot", token);
        
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var apiResponse = JsonSerializer.Deserialize<TResponse>(responseContent);
            if (apiResponse != null)
            {
                return new SendResult(true, apiResponse.Id);
            }
        }
        
        // Parse error response
        var errorResponse = JsonSerializer.Deserialize<QQApiResponse>(responseContent);
        var shouldRetry = IsRetryableError((int)response.StatusCode, errorResponse?.Code ?? 0);
        
        return new SendResult(
            false,
            ErrorCode: errorResponse?.Code.ToString() ?? response.StatusCode.ToString(),
            ErrorMessage: errorResponse?.Message ?? responseContent,
            ShouldRetry: shouldRetry);
    }
    
    /// <summary>
    /// Convert to QQ message type
    /// </summary>
    private static int ConvertToQQMsgType(ChatMessageType messageType)
    {
        return messageType switch
        {
            ChatMessageType.Text => QQMsgType.Text,
            ChatMessageType.RichText => QQMsgType.Markdown,
            ChatMessageType.Card => QQMsgType.Ark,
            _ => QQMsgType.Text
        };
    }
    
    /// <summary>
    /// Parse target user ID and determine message type
    /// </summary>
    private static (QQMessageTargetType Type, string TargetId, string? GroupOpenId) ParseTargetUserId(string targetUserId)
    {
        // Format: group:{groupOpenId}:{msgId} or c2c:{userOpenId} or channel:{channelId}
        var parts = targetUserId.Split(':');
        
        if (parts.Length >= 2)
        {
            return parts[0].ToLower() switch
            {
                "group" => (QQMessageTargetType.Group, parts.Length > 2 ? parts[2] : string.Empty, parts[1]),
                "c2c" => (QQMessageTargetType.C2C, parts[1], null),
                "channel" => (QQMessageTargetType.Channel, parts[1], null),
                _ => (QQMessageTargetType.Unknown, targetUserId, null)
            };
        }
        
        // Default to channel message
        return (QQMessageTargetType.Channel, targetUserId, null);
    }
    
    #endregion

    
    #region Authentication and Token management
    
    /// <summary>
    /// Get Access Token (with caching)
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpireTime)
        {
            return _accessToken;
        }
        
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpireTime)
            {
                return _accessToken;
            }
            
            var url = "https://bots.qq.com/app/getAppAccessToken";
            var request = new
            {
                appId = _qqOptions.AppId,
                clientSecret = _qqOptions.AppSecret
            };
            
            var response = await _httpClient.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
                cancellationToken);
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<QQTokenResponse>(content);
            
            if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                throw new InvalidOperationException($"Failed to get access token: {content}");
            }
            
            _accessToken = tokenResponse.AccessToken;
            _tokenExpireTime = DateTime.UtcNow.AddSeconds(_qqOptions.TokenCacheSeconds);
            
            Logger.LogDebug("QQ access token refreshed, expires at {ExpireTime}", _tokenExpireTime);
            
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
    
    /// <summary>
    /// Get API base URL
    /// </summary>
    private string GetApiBaseUrl()
    {
        return _qqOptions.UseSandbox ? _qqOptions.SandboxApiBaseUrl : _qqOptions.ApiBaseUrl;
    }
    
    #endregion
    
    #region Retry mechanism
    
    /// <summary>
    /// Determine whether the error is retryable
    /// </summary>
    private static bool IsRetryableError(int httpStatusCode, int errorCode)
    {
        // HTTP status code check
        if (httpStatusCode is 429 or 500 or 502 or 503 or 504)
            return true;
        
        // QQ platform error code check
        return errorCode switch
        {
            11281 => true,  // Rate limit
            11282 => true,  // Concurrency limit
            11264 => true,  // Internal server error
            _ => false
        };
    }
    
    /// <summary>
    /// Send logic with retry
    /// </summary>
    private async Task<SendResult> SendWithRetryAsync(
        Func<Task<SendResult>> sendFunc,
        CancellationToken cancellationToken)
    {
        var maxRetries = _qqOptions.MaxRetryCount;
        var retryDelayBase = _qqOptions.RetryDelayBase;
        
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await sendFunc();
                
                if (result.Success || !result.ShouldRetry || attempt >= maxRetries)
                {
                    return result;
                }
                
                // Exponential backoff
                var delay = retryDelayBase * (int)Math.Pow(2, attempt);
                Logger.LogWarning(
                    "QQ API call failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms. Error: {Error}",
                    attempt + 1, maxRetries + 1, delay, result.ErrorMessage);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = retryDelayBase * (int)Math.Pow(2, attempt);
                Logger.LogWarning(ex,
                    "QQ API call threw exception (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms",
                    attempt + 1, maxRetries + 1, delay);
                
                await Task.Delay(delay, cancellationToken);
            }
        }
        
        return new SendResult(false, ErrorCode: "MAX_RETRIES_EXCEEDED", ErrorMessage: "Maximum retry attempts exceeded");
    }
    
    #endregion
    
    #region Helper methods
    
    /// <summary>
    /// Parse timestamp
    /// </summary>
    private static DateTimeOffset ParseTimestamp(string timestamp)
    {
        if (DateTimeOffset.TryParse(timestamp, out var result))
        {
            return result;
        }
        
        if (long.TryParse(timestamp, out var ms))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }
        
        return DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Convert message to QQ format (for testing)
    /// </summary>
    public (int MsgType, string Content) ConvertToQQFormat(IChatMessage message)
    {
        var msgType = ConvertToQQMsgType(message.MessageType);
        return (msgType, message.Content);
    }
    
    #endregion
}

/// <summary>
/// QQ message target type
/// </summary>
public enum QQMessageTargetType
{
    /// <summary>
    /// Unknown type
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Group chat message
    /// </summary>
    Group,
    
    /// <summary>
    /// C2C direct message
    /// </summary>
    C2C,
    
    /// <summary>
    /// Channel message
    /// </summary>
    Channel
}
