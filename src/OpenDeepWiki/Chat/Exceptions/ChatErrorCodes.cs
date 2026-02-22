namespace OpenDeepWiki.Chat.Exceptions;

/// <summary>
/// Chat assistant error code constants
/// Defines all chat-related error codes for unified frontend/backend error handling
/// Requirements: 11.1, 11.2, 11.3
/// </summary>
public static class ChatErrorCodes
{
    #region Feature status errors (1xxx)

    /// <summary>
    /// Chat assistant feature is not enabled
    /// </summary>
    public const string FEATURE_DISABLED = "FEATURE_DISABLED";
    
    /// <summary>
    /// Feature configuration missing
    /// </summary>
    public const string CONFIG_MISSING = "CONFIG_MISSING";

    #endregion

    #region Model-related errors (2xxx)

    /// <summary>
    /// Model unavailable
    /// </summary>
    public const string MODEL_UNAVAILABLE = "MODEL_UNAVAILABLE";
    
    /// <summary>
    /// Invalid model configuration
    /// </summary>
    public const string MODEL_CONFIG_INVALID = "MODEL_CONFIG_INVALID";
    
    /// <summary>
    /// No available models
    /// </summary>
    public const string NO_AVAILABLE_MODELS = "NO_AVAILABLE_MODELS";
    
    #endregion

    #region Application-related errors (3xxx)

    /// <summary>
    /// Invalid AppId
    /// </summary>
    public const string INVALID_APP_ID = "INVALID_APP_ID";
    
    /// <summary>
    /// Application has no model configured
    /// </summary>
    public const string APP_MODEL_NOT_CONFIGURED = "APP_MODEL_NOT_CONFIGURED";
    
    /// <summary>
    /// Application is disabled
    /// </summary>
    public const string APP_DISABLED = "APP_DISABLED";
    
    #endregion

    #region Domain validation errors (4xxx)

    /// <summary>
    /// Domain not in allowed list
    /// </summary>
    public const string DOMAIN_NOT_ALLOWED = "DOMAIN_NOT_ALLOWED";
    
    /// <summary>
    /// Unable to determine request origin domain
    /// </summary>
    public const string DOMAIN_UNKNOWN = "DOMAIN_UNKNOWN";
    
    #endregion

    #region Rate limiting errors (5xxx)

    /// <summary>
    /// Rate limit exceeded
    /// </summary>
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
    
    #endregion

    #region Document-related errors (6xxx)

    /// <summary>
    /// Document not found
    /// </summary>
    public const string DOCUMENT_NOT_FOUND = "DOCUMENT_NOT_FOUND";
    
    /// <summary>
    /// Document access denied
    /// </summary>
    public const string DOCUMENT_ACCESS_DENIED = "DOCUMENT_ACCESS_DENIED";
    
    /// <summary>
    /// Repository not found
    /// </summary>
    public const string REPOSITORY_NOT_FOUND = "REPOSITORY_NOT_FOUND";
    
    #endregion

    #region Tool call errors (7xxx)

    /// <summary>
    /// MCP call failed
    /// </summary>
    public const string MCP_CALL_FAILED = "MCP_CALL_FAILED";
    
    /// <summary>
    /// Tool execution failed
    /// </summary>
    public const string TOOL_EXECUTION_FAILED = "TOOL_EXECUTION_FAILED";
    
    /// <summary>
    /// Tool not found
    /// </summary>
    public const string TOOL_NOT_FOUND = "TOOL_NOT_FOUND";
    
    #endregion

    #region Connection and timeout errors (8xxx)

    /// <summary>
    /// Connection failed
    /// </summary>
    public const string CONNECTION_FAILED = "CONNECTION_FAILED";
    
    /// <summary>
    /// Request timed out
    /// </summary>
    public const string REQUEST_TIMEOUT = "REQUEST_TIMEOUT";
    
    /// <summary>
    /// SSE stream interrupted
    /// </summary>
    public const string STREAM_INTERRUPTED = "STREAM_INTERRUPTED";
    
    #endregion

    #region Internal errors (9xxx)

    /// <summary>
    /// Internal server error
    /// </summary>
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    
    /// <summary>
    /// Unknown error
    /// </summary>
    public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
    
    #endregion

    /// <summary>
    /// Get the default message for an error code
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <returns>Default error message</returns>
    public static string GetDefaultMessage(string errorCode)
    {
        return errorCode switch
        {
            FEATURE_DISABLED => "Chat assistant is not enabled",
            CONFIG_MISSING => "Feature configuration missing",
            MODEL_UNAVAILABLE => "Model unavailable, please select another",
            MODEL_CONFIG_INVALID => "Invalid model configuration",
            NO_AVAILABLE_MODELS => "No models available, contact admin",
            INVALID_APP_ID => "Invalid application ID",
            APP_MODEL_NOT_CONFIGURED => "Application has no AI model configured",
            APP_DISABLED => "Application is disabled",
            DOMAIN_NOT_ALLOWED => "Domain not in allowed list",
            DOMAIN_UNKNOWN => "Unable to determine request origin domain",
            RATE_LIMIT_EXCEEDED => "Rate limit exceeded, please retry later",
            DOCUMENT_NOT_FOUND => "Document not found",
            DOCUMENT_ACCESS_DENIED => "Document access denied",
            REPOSITORY_NOT_FOUND => "Repository not found",
            MCP_CALL_FAILED => "MCP tool call failed",
            TOOL_EXECUTION_FAILED => "Tool execution failed",
            TOOL_NOT_FOUND => "Tool not found",
            CONNECTION_FAILED => "Connection failed, check network",
            REQUEST_TIMEOUT => "Request timed out, please retry",
            STREAM_INTERRUPTED => "Stream interrupted, please retry",
            INTERNAL_ERROR => "Internal server error",
            UNKNOWN_ERROR => "Unknown error",
            _ => "An error occurred"
        };
    }

    /// <summary>
    /// Determine if an error is retryable
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <returns>Whether the error is retryable</returns>
    public static bool IsRetryable(string errorCode)
    {
        return errorCode switch
        {
            CONNECTION_FAILED => true,
            REQUEST_TIMEOUT => true,
            STREAM_INTERRUPTED => true,
            RATE_LIMIT_EXCEEDED => true,
            INTERNAL_ERROR => true,
            _ => false
        };
    }
}
