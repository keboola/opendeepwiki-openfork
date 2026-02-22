using System.Text.Json.Serialization;

namespace OpenDeepWiki.Chat.Providers.QQ;

#region Webhook event models

/// <summary>
/// QQ Bot Webhook event base structure
/// </summary>
public class QQWebhookEvent
{
    /// <summary>
    /// Operation code
    /// </summary>
    [JsonPropertyName("op")]
    public int OpCode { get; set; }
    
    /// <summary>
    /// Event sequence number
    /// </summary>
    [JsonPropertyName("s")]
    public int? Sequence { get; set; }
    
    /// <summary>
    /// Event type
    /// </summary>
    [JsonPropertyName("t")]
    public string? EventType { get; set; }
    
    /// <summary>
    /// Event ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? EventId { get; set; }
    
    /// <summary>
    /// Event data
    /// </summary>
    [JsonPropertyName("d")]
    public QQEventData? Data { get; set; }
}

/// <summary>
/// QQ event data
/// </summary>
public class QQEventData
{
    /// <summary>
    /// Message ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Message timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// Channel ID
    /// </summary>
    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }
    
    /// <summary>
    /// Guild ID
    /// </summary>
    [JsonPropertyName("guild_id")]
    public string? GuildId { get; set; }
    
    /// <summary>
    /// Group chat ID
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }
    
    /// <summary>
    /// Group chat OpenID
    /// </summary>
    [JsonPropertyName("group_openid")]
    public string? GroupOpenId { get; set; }
    
    /// <summary>
    /// Message author
    /// </summary>
    [JsonPropertyName("author")]
    public QQAuthor? Author { get; set; }
    
    /// <summary>
    /// Message member information
    /// </summary>
    [JsonPropertyName("member")]
    public QQMember? Member { get; set; }
    
    /// <summary>
    /// @ mention list
    /// </summary>
    [JsonPropertyName("mentions")]
    public List<QQMention>? Mentions { get; set; }
    
    /// <summary>
    /// Attachment list
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<QQAttachment>? Attachments { get; set; }
    
    /// <summary>
    /// Message sequence number (used for passive replies)
    /// </summary>
    [JsonPropertyName("msg_seq")]
    public int? MsgSeq { get; set; }
}

/// <summary>
/// QQ message author
/// </summary>
public class QQAuthor
{
    /// <summary>
    /// User ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// User OpenID (group chat scenario)
    /// </summary>
    [JsonPropertyName("member_openid")]
    public string? MemberOpenId { get; set; }
    
    /// <summary>
    /// Username
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Avatar URL
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    /// <summary>
    /// Whether a bot
    /// </summary>
    [JsonPropertyName("bot")]
    public bool IsBot { get; set; }
}

/// <summary>
/// QQ member information
/// </summary>
public class QQMember
{
    /// <summary>
    /// Join time
    /// </summary>
    [JsonPropertyName("joined_at")]
    public string JoinedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// Nickname
    /// </summary>
    [JsonPropertyName("nick")]
    public string? Nick { get; set; }
    
    /// <summary>
    /// Role list
    /// </summary>
    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }
}

/// <summary>
/// QQ @ mention
/// </summary>
public class QQMention
{
    /// <summary>
    /// Mentioned user ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Mentioned username
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether a bot
    /// </summary>
    [JsonPropertyName("bot")]
    public bool IsBot { get; set; }
}

/// <summary>
/// QQ attachment
/// </summary>
public class QQAttachment
{
    /// <summary>
    /// Attachment ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Filename
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
    
    /// <summary>
    /// Content type
    /// </summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// File size
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    /// <summary>
    /// File URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Image width
    /// </summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }
    
    /// <summary>
    /// Image height
    /// </summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }
}

#endregion

#region C2C private message models

/// <summary>
/// QQ C2C private message event
/// </summary>
public class QQC2CMessageEvent
{
    /// <summary>
    /// Message ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Message timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// Message author
    /// </summary>
    [JsonPropertyName("author")]
    public QQC2CAuthor? Author { get; set; }
    
    /// <summary>
    /// Attachment list
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<QQAttachment>? Attachments { get; set; }
}

/// <summary>
/// QQ C2C message author
/// </summary>
public class QQC2CAuthor
{
    /// <summary>
    /// User OpenID
    /// </summary>
    [JsonPropertyName("user_openid")]
    public string UserOpenId { get; set; } = string.Empty;
}

#endregion

#region Group message models

/// <summary>
/// QQ group message event
/// </summary>
public class QQGroupMessageEvent
{
    /// <summary>
    /// Message ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Message timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// Group chat OpenID
    /// </summary>
    [JsonPropertyName("group_openid")]
    public string GroupOpenId { get; set; } = string.Empty;
    
    /// <summary>
    /// Message author
    /// </summary>
    [JsonPropertyName("author")]
    public QQGroupAuthor? Author { get; set; }
    
    /// <summary>
    /// Attachment list
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<QQAttachment>? Attachments { get; set; }
}

/// <summary>
/// QQ group message author
/// </summary>
public class QQGroupAuthor
{
    /// <summary>
    /// Member OpenID
    /// </summary>
    [JsonPropertyName("member_openid")]
    public string MemberOpenId { get; set; } = string.Empty;
}

#endregion


#region API response models

/// <summary>
/// QQ API base response
/// </summary>
public class QQApiResponse
{
    /// <summary>
    /// Error code (0 means success)
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// QQ Access Token response
/// </summary>
public class QQTokenResponse
{
    /// <summary>
    /// Access Token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Expiration time (seconds)
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>
/// QQ send message response
/// </summary>
public class QQSendMessageResponse : QQApiResponse
{
    /// <summary>
    /// Message ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    /// <summary>
    /// Message timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}

#endregion

#region Send message request models

/// <summary>
/// QQ send channel message request
/// </summary>
public class QQSendChannelMessageRequest
{
    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    /// <summary>
    /// Message embed content
    /// </summary>
    [JsonPropertyName("embed")]
    public QQEmbed? Embed { get; set; }
    
    /// <summary>
    /// Ark message
    /// </summary>
    [JsonPropertyName("ark")]
    public QQArk? Ark { get; set; }
    
    /// <summary>
    /// Reference message ID
    /// </summary>
    [JsonPropertyName("msg_id")]
    public string? MsgId { get; set; }
    
    /// <summary>
    /// Image URL
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }
    
    /// <summary>
    /// Markdown message
    /// </summary>
    [JsonPropertyName("markdown")]
    public QQMarkdown? Markdown { get; set; }
}

/// <summary>
/// QQ send group message request
/// </summary>
public class QQSendGroupMessageRequest
{
    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    /// <summary>
    /// Message type: 0 Text, 1 Mixed text/image, 2 Markdown, 3 Ark, 4 Embed, 7 Rich media
    /// </summary>
    [JsonPropertyName("msg_type")]
    public int MsgType { get; set; }
    
    /// <summary>
    /// Reference message ID (required for passive replies)
    /// </summary>
    [JsonPropertyName("msg_id")]
    public string? MsgId { get; set; }
    
    /// <summary>
    /// Message sequence number (required for passive replies)
    /// </summary>
    [JsonPropertyName("msg_seq")]
    public int? MsgSeq { get; set; }
    
    /// <summary>
    /// Rich media message
    /// </summary>
    [JsonPropertyName("media")]
    public QQMedia? Media { get; set; }
    
    /// <summary>
    /// Markdown message
    /// </summary>
    [JsonPropertyName("markdown")]
    public QQMarkdown? Markdown { get; set; }
    
    /// <summary>
    /// Ark message
    /// </summary>
    [JsonPropertyName("ark")]
    public QQArk? Ark { get; set; }
}

/// <summary>
/// QQ send C2C private message request
/// </summary>
public class QQSendC2CMessageRequest
{
    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    /// <summary>
    /// Message type: 0 Text, 1 Mixed text/image, 2 Markdown, 3 Ark, 4 Embed, 7 Rich media
    /// </summary>
    [JsonPropertyName("msg_type")]
    public int MsgType { get; set; }
    
    /// <summary>
    /// Referenced message ID (required for passive reply)
    /// </summary>
    [JsonPropertyName("msg_id")]
    public string? MsgId { get; set; }
    
    /// <summary>
    /// Message sequence number
    /// </summary>
    [JsonPropertyName("msg_seq")]
    public int? MsgSeq { get; set; }
    
    /// <summary>
    /// Rich media message
    /// </summary>
    [JsonPropertyName("media")]
    public QQMedia? Media { get; set; }
    
    /// <summary>
    /// Markdown message
    /// </summary>
    [JsonPropertyName("markdown")]
    public QQMarkdown? Markdown { get; set; }
    
    /// <summary>
    /// Ark message
    /// </summary>
    [JsonPropertyName("ark")]
    public QQArk? Ark { get; set; }
}

/// <summary>
/// QQ Embed message
/// </summary>
public class QQEmbed
{
    /// <summary>
    /// Title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Prompt text
    /// </summary>
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
    
    /// <summary>
    /// Thumbnail
    /// </summary>
    [JsonPropertyName("thumbnail")]
    public QQThumbnail? Thumbnail { get; set; }
    
    /// <summary>
    /// Field list
    /// </summary>
    [JsonPropertyName("fields")]
    public List<QQEmbedField>? Fields { get; set; }
}

/// <summary>
/// QQ Embed field
/// </summary>
public class QQEmbedField
{
    /// <summary>
    /// Field name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// QQ thumbnail
/// </summary>
public class QQThumbnail
{
    /// <summary>
    /// Image URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// QQ Ark message
/// </summary>
public class QQArk
{
    /// <summary>
    /// Ark template ID
    /// </summary>
    [JsonPropertyName("template_id")]
    public int TemplateId { get; set; }
    
    /// <summary>
    /// Ark key-value pair list
    /// </summary>
    [JsonPropertyName("kv")]
    public List<QQArkKv>? Kv { get; set; }
}

/// <summary>
/// QQ Ark key-value pair
/// </summary>
public class QQArkKv
{
    /// <summary>
    /// Key
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Value
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    /// <summary>
    /// Object list
    /// </summary>
    [JsonPropertyName("obj")]
    public List<QQArkObj>? Obj { get; set; }
}

/// <summary>
/// QQ Ark object
/// </summary>
public class QQArkObj
{
    /// <summary>
    /// Object key-value pair list
    /// </summary>
    [JsonPropertyName("obj_kv")]
    public List<QQArkObjKv>? ObjKv { get; set; }
}

/// <summary>
/// QQ Ark object key-value pair
/// </summary>
public class QQArkObjKv
{
    /// <summary>
    /// Key
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Value
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// QQ Markdown message
/// </summary>
public class QQMarkdown
{
    /// <summary>
    /// Markdown template ID
    /// </summary>
    [JsonPropertyName("template_id")]
    public int? TemplateId { get; set; }
    
    /// <summary>
    /// Custom Markdown content
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    /// <summary>
    /// Template parameters
    /// </summary>
    [JsonPropertyName("params")]
    public List<QQMarkdownParam>? Params { get; set; }
}

/// <summary>
/// QQ Markdown parameter
/// </summary>
public class QQMarkdownParam
{
    /// <summary>
    /// Parameter key
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter value list
    /// </summary>
    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }
}

/// <summary>
/// QQ rich media message
/// </summary>
public class QQMedia
{
    /// <summary>
    /// File information (obtained after upload)
    /// </summary>
    [JsonPropertyName("file_info")]
    public string FileInfo { get; set; } = string.Empty;
}

#endregion

#region WebSocket related models

/// <summary>
/// QQ WebSocket gateway response
/// </summary>
public class QQGatewayResponse
{
    /// <summary>
    /// WebSocket connection URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// QQ WebSocket authentication data
/// </summary>
public class QQIdentifyData
{
    /// <summary>
    /// Bot Token
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Subscribed event types
    /// </summary>
    [JsonPropertyName("intents")]
    public int Intents { get; set; }
    
    /// <summary>
    /// Shard information [current shard, total shards]
    /// </summary>
    [JsonPropertyName("shard")]
    public int[]? Shard { get; set; }
}

/// <summary>
/// QQ WebSocket heartbeat data
/// </summary>
public class QQHeartbeatData
{
    /// <summary>
    /// Last received message sequence number
    /// </summary>
    [JsonPropertyName("d")]
    public int? LastSequence { get; set; }
}

/// <summary>
/// QQ WebSocket Ready event data
/// </summary>
public class QQReadyData
{
    /// <summary>
    /// Protocol version
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }
    
    /// <summary>
    /// Session ID
    /// </summary>
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Bot user information
    /// </summary>
    [JsonPropertyName("user")]
    public QQAuthor? User { get; set; }
    
    /// <summary>
    /// Shard information
    /// </summary>
    [JsonPropertyName("shard")]
    public int[]? Shard { get; set; }
}

#endregion

#region Operation code constants

/// <summary>
/// QQ WebSocket operation codes
/// </summary>
public static class QQOpCode
{
    /// <summary>
    /// Server dispatch message
    /// </summary>
    public const int Dispatch = 0;
    
    /// <summary>
    /// Client heartbeat
    /// </summary>
    public const int Heartbeat = 1;
    
    /// <summary>
    /// Client authentication
    /// </summary>
    public const int Identify = 2;
    
    /// <summary>
    /// Client resume connection
    /// </summary>
    public const int Resume = 6;
    
    /// <summary>
    /// Server requests reconnect
    /// </summary>
    public const int Reconnect = 7;
    
    /// <summary>
    /// Authentication failed
    /// </summary>
    public const int InvalidSession = 9;
    
    /// <summary>
    /// Server sends Hello
    /// </summary>
    public const int Hello = 10;
    
    /// <summary>
    /// Heartbeat acknowledgement
    /// </summary>
    public const int HeartbeatAck = 11;
    
    /// <summary>
    /// HTTP callback acknowledgement
    /// </summary>
    public const int HttpCallbackAck = 12;
}

/// <summary>
/// QQ event type constants
/// </summary>
public static class QQEventType
{
    /// <summary>
    /// Connection ready
    /// </summary>
    public const string Ready = "READY";
    
    /// <summary>
    /// Resume successful
    /// </summary>
    public const string Resumed = "RESUMED";
    
    /// <summary>
    /// Channel message (public)
    /// </summary>
    public const string AtMessageCreate = "AT_MESSAGE_CREATE";
    
    /// <summary>
    /// Channel message (private)
    /// </summary>
    public const string MessageCreate = "MESSAGE_CREATE";
    
    /// <summary>
    /// Direct message
    /// </summary>
    public const string DirectMessageCreate = "DIRECT_MESSAGE_CREATE";
    
    /// <summary>
    /// Group chat @ message
    /// </summary>
    public const string GroupAtMessageCreate = "GROUP_AT_MESSAGE_CREATE";
    
    /// <summary>
    /// C2C private message
    /// </summary>
    public const string C2CMessageCreate = "C2C_MESSAGE_CREATE";
}

/// <summary>
/// QQ message type constants
/// </summary>
public static class QQMsgType
{
    /// <summary>
    /// Text message
    /// </summary>
    public const int Text = 0;
    
    /// <summary>
    /// Mixed text/image
    /// </summary>
    public const int Mixed = 1;
    
    /// <summary>
    /// Markdown
    /// </summary>
    public const int Markdown = 2;
    
    /// <summary>
    /// Ark
    /// </summary>
    public const int Ark = 3;
    
    /// <summary>
    /// Embed
    /// </summary>
    public const int Embed = 4;
    
    /// <summary>
    /// Rich media
    /// </summary>
    public const int Media = 7;
}

#endregion
