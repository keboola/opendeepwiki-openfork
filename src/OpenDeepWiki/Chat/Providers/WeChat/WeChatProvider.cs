using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Providers.WeChat;

/// <summary>
/// WeChat customer service message Provider implementation
/// Supports text, image, and voice messages
/// </summary>
public class WeChatProvider : BaseMessageProvider
{
    private readonly HttpClient _httpClient;
    private readonly WeChatProviderOptions _wechatOptions;
    private WeChatCrypto? _crypto;
    private string? _accessToken;
    private DateTime _tokenExpireTime = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    
    /// <summary>
    /// Message types supported by WeChat
    /// </summary>
    private static readonly HashSet<ChatMessageType> SupportedMessageTypes = new()
    {
        ChatMessageType.Text,
        ChatMessageType.Image,
        ChatMessageType.Audio
    };
    
    public override string PlatformId => "wechat";
    public override string DisplayName => "WeChat Customer Service";
    
    public WeChatProvider(
        ILogger<WeChatProvider> logger,
        IOptions<WeChatProviderOptions> options,
        HttpClient httpClient)
        : base(logger, options)
    {
        _httpClient = httpClient;
        _wechatOptions = options.Value;
    }
    
    /// <summary>
    /// Initialize Provider
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await base.InitializeAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(_wechatOptions.AppId) || string.IsNullOrEmpty(_wechatOptions.AppSecret))
        {
            Logger.LogWarning("WeChat AppId or AppSecret not configured, provider will not be fully functional");
            return;
        }
        
        // Initialize encryption/decryption utility (if EncodingAesKey is configured)
        if (!string.IsNullOrEmpty(_wechatOptions.EncodingAesKey) && !string.IsNullOrEmpty(_wechatOptions.Token))
        {
            _crypto = new WeChatCrypto(_wechatOptions.Token, _wechatOptions.EncodingAesKey, _wechatOptions.AppId);
            Logger.LogInformation("WeChat message encryption enabled");
        }
        
        try
        {
            await GetAccessTokenAsync(cancellationToken);
            Logger.LogInformation("WeChat provider initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize WeChat provider");
        }
    }

    
    /// <summary>
    /// Parse raw WeChat message into unified format
    /// </summary>
    public override async Task<IChatMessage?> ParseMessageAsync(string rawMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to parse encrypted message
            var xmlMessage = await ParseXmlMessageAsync(rawMessage, cancellationToken);
            if (xmlMessage == null)
            {
                Logger.LogWarning("Failed to parse WeChat XML message");
                return null;
            }
            
            // Ignore event messages (only process regular messages)
            if (xmlMessage.MsgType == WeChatMsgType.Event)
            {
                Logger.LogDebug("Ignoring WeChat event message: {Event}", xmlMessage.Event);
                return null;
            }
            
            var (messageType, content) = ParseWeChatMessageContent(xmlMessage);
            
            return new ChatMessage
            {
                MessageId = xmlMessage.MsgId.ToString(),
                SenderId = xmlMessage.FromUserName,
                ReceiverId = xmlMessage.ToUserName,
                Content = content,
                MessageType = messageType,
                Platform = PlatformId,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(xmlMessage.CreateTime),
                Metadata = new Dictionary<string, object>
                {
                    { "msg_type", xmlMessage.MsgType },
                    { "msg_data_id", xmlMessage.MsgDataId ?? string.Empty },
                    { "media_id", xmlMessage.MediaId ?? string.Empty },
                    { "pic_url", xmlMessage.PicUrl ?? string.Empty },
                    { "recognition", xmlMessage.Recognition ?? string.Empty }
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse WeChat message");
            return null;
        }
    }
    
    /// <summary>
    /// Send message to WeChat
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
            
            // Build customer service message request
            var request = BuildCustomMessageRequest(processedMessage, targetUserId);
            
            var url = $"{_wechatOptions.ApiBaseUrl}/cgi-bin/message/custom/send?access_token={token}";
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request, new JsonSerializerOptions 
                    { 
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
                    }),
                    Encoding.UTF8,
                    "application/json")
            };
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var apiResponse = JsonSerializer.Deserialize<WeChatSendMessageResponse>(responseContent);
            
            if (apiResponse?.ErrorCode == 0)
            {
                return new SendResult(true, apiResponse.MsgId?.ToString());
            }
            
            var shouldRetry = IsRetryableError(apiResponse?.ErrorCode ?? -1);
            return new SendResult(
                false,
                ErrorCode: apiResponse?.ErrorCode.ToString(),
                ErrorMessage: apiResponse?.ErrorMessage,
                ShouldRetry: shouldRetry);
        }, cancellationToken);
    }
    
    /// <summary>
    /// Validate WeChat Webhook request
    /// </summary>
    public override async Task<WebhookValidationResult> ValidateWebhookAsync(
        HttpRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get verification parameters
            var signature = request.Query["signature"].ToString();
            var timestamp = request.Query["timestamp"].ToString();
            var nonce = request.Query["nonce"].ToString();
            var echostr = request.Query["echostr"].ToString();
            var msgSignature = request.Query["msg_signature"].ToString();
            
            // URL verification request (GET request)
            if (request.Method == "GET" && !string.IsNullOrEmpty(echostr))
            {
                if (VerifySignature(signature, timestamp, nonce))
                {
                    return new WebhookValidationResult(true, Challenge: echostr);
                }
                return new WebhookValidationResult(false, ErrorMessage: "Invalid signature");
            }
            
            // Message request verification (POST request)
            if (request.Method == "POST")
            {
                // Plaintext mode: verify normal signature
                if (string.IsNullOrEmpty(msgSignature))
                {
                    if (!VerifySignature(signature, timestamp, nonce))
                    {
                        return new WebhookValidationResult(false, ErrorMessage: "Invalid signature");
                    }
                }
                // Encrypted mode: verify message signature
                else if (_crypto != null)
                {
                    request.EnableBuffering();
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    var body = await reader.ReadToEndAsync(cancellationToken);
                    request.Body.Position = 0;
                    
                    // Parse encrypted message to get the Encrypt field
                    var encryptedMsg = DeserializeXml<WeChatEncryptedMessage>(body);
                    if (encryptedMsg != null && !string.IsNullOrEmpty(encryptedMsg.Encrypt))
                    {
                        if (!_crypto.VerifySignature(msgSignature, timestamp, nonce, encryptedMsg.Encrypt))
                        {
                            return new WebhookValidationResult(false, ErrorMessage: "Invalid message signature");
                        }
                    }
                }
            }
            
            return new WebhookValidationResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to validate WeChat webhook");
            return new WebhookValidationResult(false, ErrorMessage: ex.Message);
        }
    }

    
    #region Private methods
    
    /// <summary>
    /// Get Access Token (with caching)
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
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
            
            var url = $"{_wechatOptions.ApiBaseUrl}/cgi-bin/token?grant_type=client_credential&appid={_wechatOptions.AppId}&secret={_wechatOptions.AppSecret}";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<WeChatTokenResponse>(content);
            
            if (tokenResponse?.ErrorCode != 0 || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException($"Failed to get access token: {tokenResponse?.ErrorMessage ?? content}");
            }
            
            _accessToken = tokenResponse.AccessToken;
            _tokenExpireTime = DateTime.UtcNow.AddSeconds(_wechatOptions.TokenCacheSeconds);
            
            Logger.LogDebug("WeChat access token refreshed, expires at {ExpireTime}", _tokenExpireTime);
            
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
    
    /// <summary>
    /// Parse XML message (supports encrypted and plaintext)
    /// </summary>
    private async Task<WeChatXmlMessage?> ParseXmlMessageAsync(string rawMessage, CancellationToken cancellationToken)
    {
        // First try to parse as encrypted message
        var encryptedMsg = DeserializeXml<WeChatEncryptedMessage>(rawMessage);
        
        if (encryptedMsg != null && !string.IsNullOrEmpty(encryptedMsg.Encrypt) && _crypto != null)
        {
            // Decrypt message
            var decryptedXml = _crypto.Decrypt(encryptedMsg.Encrypt);
            if (decryptedXml == null)
            {
                Logger.LogWarning("Failed to decrypt WeChat message");
                return null;
            }
            
            return DeserializeXml<WeChatXmlMessage>(decryptedXml);
        }
        
        // Plaintext message
        return DeserializeXml<WeChatXmlMessage>(rawMessage);
    }
    
    /// <summary>
    /// Parse WeChat message content
    /// </summary>
    private (ChatMessageType Type, string Content) ParseWeChatMessageContent(WeChatXmlMessage message)
    {
        return message.MsgType switch
        {
            WeChatMsgType.Text => (ChatMessageType.Text, message.Content ?? string.Empty),
            WeChatMsgType.Image => (ChatMessageType.Image, message.PicUrl ?? message.MediaId ?? string.Empty),
            WeChatMsgType.Voice => (ChatMessageType.Audio, message.Recognition ?? message.MediaId ?? string.Empty),
            WeChatMsgType.Video or WeChatMsgType.ShortVideo => (ChatMessageType.Video, message.MediaId ?? string.Empty),
            WeChatMsgType.Location => (ChatMessageType.Text, $"Location: {message.Label} ({message.LocationX}, {message.LocationY})"),
            WeChatMsgType.Link => (ChatMessageType.Text, $"{message.Title}: {message.Url}"),
            _ => (ChatMessageType.Unknown, message.Content ?? string.Empty)
        };
    }
    
    /// <summary>
    /// Build customer service message request
    /// </summary>
    private WeChatCustomMessageRequest BuildCustomMessageRequest(IChatMessage message, string targetUserId)
    {
        var request = new WeChatCustomMessageRequest
        {
            ToUser = targetUserId
        };
        
        switch (message.MessageType)
        {
            case ChatMessageType.Text:
                request.MsgType = WeChatMsgType.Text;
                request.Text = new WeChatTextContent { Content = message.Content };
                break;
                
            case ChatMessageType.Image:
                request.MsgType = WeChatMsgType.Image;
                request.Image = new WeChatMediaContent { MediaId = message.Content };
                break;
                
            case ChatMessageType.Audio:
                request.MsgType = WeChatMsgType.Voice;
                request.Voice = new WeChatMediaContent { MediaId = message.Content };
                break;
                
            default:
                // Default to sending text message
                request.MsgType = WeChatMsgType.Text;
                request.Text = new WeChatTextContent { Content = message.Content };
                break;
        }
        
        return request;
    }
    
    /// <summary>
    /// Verify signature (plaintext mode)
    /// </summary>
    private bool VerifySignature(string signature, string timestamp, string nonce)
    {
        if (string.IsNullOrEmpty(_wechatOptions.Token))
            return true; // Skip verification when Token is not configured
        
        var items = new[] { _wechatOptions.Token, timestamp, nonce };
        Array.Sort(items, StringComparer.Ordinal);
        
        var combined = string.Concat(items);
        var hash = System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(combined));
        var calculatedSignature = Convert.ToHexString(hash).ToLowerInvariant();
        
        return string.Equals(signature, calculatedSignature, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Deserialize XML
    /// </summary>
    private static T? DeserializeXml<T>(string xml) where T : class
    {
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return serializer.Deserialize(reader) as T;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Determine whether the error is retryable
    /// </summary>
    private static bool IsRetryableError(int errorCode)
    {
        // Common retryable WeChat error codes
        return errorCode switch
        {
            -1 => true,      // System busy
            40001 => true,   // Invalid access_token (possibly expired)
            40014 => true,   // Invalid access_token
            42001 => true,   // access_token expired
            45015 => true,   // Reply time limit exceeded (retryable)
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
        var maxRetries = _wechatOptions.MaxRetryCount;
        var retryDelayBase = _wechatOptions.RetryDelayBase;
        
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await sendFunc();
                
                // If token expired error, clear cache and retry
                if (!result.Success && result.ErrorCode is "40001" or "40014" or "42001")
                {
                    _accessToken = null;
                    _tokenExpireTime = DateTime.MinValue;
                    
                    if (attempt < maxRetries)
                    {
                        Logger.LogWarning("WeChat access token expired, refreshing and retrying");
                        continue;
                    }
                }
                
                if (result.Success || !result.ShouldRetry || attempt >= maxRetries)
                {
                    return result;
                }
                
                // Exponential backoff
                var delay = retryDelayBase * (int)Math.Pow(2, attempt);
                Logger.LogWarning(
                    "WeChat API call failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms. Error: {Error}",
                    attempt + 1, maxRetries + 1, delay, result.ErrorMessage);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = retryDelayBase * (int)Math.Pow(2, attempt);
                Logger.LogWarning(ex,
                    "WeChat API call threw exception (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms",
                    attempt + 1, maxRetries + 1, delay);
                
                await Task.Delay(delay, cancellationToken);
            }
        }
        
        return new SendResult(false, ErrorCode: "MAX_RETRIES_EXCEEDED", ErrorMessage: "Maximum retry attempts exceeded");
    }
    
    #endregion
    
    #region Public helper methods
    
    /// <summary>
    /// Convert message to WeChat format (for testing)
    /// </summary>
    public (string MsgType, string Content) ConvertToWeChatFormat(IChatMessage message)
    {
        return message.MessageType switch
        {
            ChatMessageType.Text => (WeChatMsgType.Text, message.Content),
            ChatMessageType.Image => (WeChatMsgType.Image, message.Content),
            ChatMessageType.Audio => (WeChatMsgType.Voice, message.Content),
            _ => (WeChatMsgType.Text, message.Content)
        };
    }
    
    /// <summary>
    /// Get encryption/decryption utility instance (for testing)
    /// </summary>
    public WeChatCrypto? GetCrypto() => _crypto;
    
    #endregion
}
