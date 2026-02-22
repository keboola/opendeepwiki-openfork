using System.Text.Json.Serialization;

namespace OpenDeepWiki.Chat.Providers.Feishu;

#region Webhook event models

/// <summary>
/// Feishu Webhook event base structure
/// </summary>
public class FeishuWebhookEvent
{
    /// <summary>
    /// Event schema (1.0 or 2.0)
    /// </summary>
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }
    
    /// <summary>
    /// Event header (2.0 format)
    /// </summary>
    [JsonPropertyName("header")]
    public FeishuEventHeader? Header { get; set; }
    
    /// <summary>
    /// Event content
    /// </summary>
    [JsonPropertyName("event")]
    public FeishuEventContent? Event { get; set; }
    
    /// <summary>
    /// Verification Token (1.0 format)
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    
    /// <summary>
    /// Event type (1.0 format)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    /// <summary>
    /// Verification challenge code (used during URL verification)
    /// </summary>
    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; }
    
    /// <summary>
    /// Encrypted content (when encryption is enabled)
    /// </summary>
    [JsonPropertyName("encrypt")]
    public string? Encrypt { get; set; }
}

/// <summary>
/// Feishu event header (2.0 format)
/// </summary>
public class FeishuEventHeader
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;
    
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;
    
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;
    
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;
    
    [JsonPropertyName("tenant_key")]
    public string TenantKey { get; set; } = string.Empty;
}

/// <summary>
/// Feishu event content
/// </summary>
public class FeishuEventContent
{
    [JsonPropertyName("sender")]
    public FeishuSender? Sender { get; set; }
    
    [JsonPropertyName("message")]
    public FeishuMessage? Message { get; set; }
}

/// <summary>
/// Feishu message sender
/// </summary>
public class FeishuSender
{
    [JsonPropertyName("sender_id")]
    public FeishuSenderId? SenderId { get; set; }
    
    [JsonPropertyName("sender_type")]
    public string SenderType { get; set; } = string.Empty;
    
    [JsonPropertyName("tenant_key")]
    public string TenantKey { get; set; } = string.Empty;
}

/// <summary>
/// Feishu sender ID
/// </summary>
public class FeishuSenderId
{
    [JsonPropertyName("union_id")]
    public string UnionId { get; set; } = string.Empty;
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("open_id")]
    public string OpenId { get; set; } = string.Empty;
}

#endregion

#region Message models

/// <summary>
/// Feishu message
/// </summary>
public class FeishuMessage
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;
    
    [JsonPropertyName("root_id")]
    public string? RootId { get; set; }
    
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }
    
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;
    
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;
    
    [JsonPropertyName("chat_type")]
    public string ChatType { get; set; } = string.Empty;
    
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("mentions")]
    public List<FeishuMention>? Mentions { get; set; }
}

/// <summary>
/// Feishu @ mention
/// </summary>
public class FeishuMention
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    [JsonPropertyName("id")]
    public FeishuSenderId? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("tenant_key")]
    public string TenantKey { get; set; } = string.Empty;
}

/// <summary>
/// Feishu text message content
/// </summary>
public class FeishuTextContent
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Feishu image message content
/// </summary>
public class FeishuImageContent
{
    [JsonPropertyName("image_key")]
    public string ImageKey { get; set; } = string.Empty;
}

#endregion


#region API response models

/// <summary>
/// Feishu API base response
/// </summary>
public class FeishuApiResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Feishu Access Token response
/// </summary>
public class FeishuTokenResponse : FeishuApiResponse
{
    [JsonPropertyName("tenant_access_token")]
    public string TenantAccessToken { get; set; } = string.Empty;
    
    [JsonPropertyName("expire")]
    public int Expire { get; set; }
}

/// <summary>
/// Feishu send message response
/// </summary>
public class FeishuSendMessageResponse : FeishuApiResponse
{
    [JsonPropertyName("data")]
    public FeishuSendMessageData? Data { get; set; }
}

/// <summary>
/// Feishu send message response data
/// </summary>
public class FeishuSendMessageData
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;
}

#endregion

#region Message card models

/// <summary>
/// Feishu message card
/// </summary>
public class FeishuCard
{
    [JsonPropertyName("config")]
    public FeishuCardConfig? Config { get; set; }
    
    [JsonPropertyName("header")]
    public FeishuCardHeader? Header { get; set; }
    
    [JsonPropertyName("elements")]
    public List<object>? Elements { get; set; }
}

/// <summary>
/// Feishu card configuration
/// </summary>
public class FeishuCardConfig
{
    [JsonPropertyName("wide_screen_mode")]
    public bool WideScreenMode { get; set; } = true;
    
    [JsonPropertyName("enable_forward")]
    public bool EnableForward { get; set; } = true;
}

/// <summary>
/// Feishu card header
/// </summary>
public class FeishuCardHeader
{
    [JsonPropertyName("title")]
    public FeishuCardText? Title { get; set; }
    
    [JsonPropertyName("template")]
    public string Template { get; set; } = "blue";
}

/// <summary>
/// Feishu card text
/// </summary>
public class FeishuCardText
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "plain_text";
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Feishu card Markdown element
/// </summary>
public class FeishuCardMarkdown
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "markdown";
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Feishu card divider
/// </summary>
public class FeishuCardDivider
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "hr";
}

#endregion

#region Send message request models

/// <summary>
/// Feishu send message request
/// </summary>
public class FeishuSendMessageRequest
{
    [JsonPropertyName("receive_id")]
    public string ReceiveId { get; set; } = string.Empty;
    
    [JsonPropertyName("msg_type")]
    public string MsgType { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

#endregion
