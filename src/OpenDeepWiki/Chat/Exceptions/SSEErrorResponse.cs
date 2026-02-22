using System.Text.Json.Serialization;

namespace OpenDeepWiki.Chat.Exceptions;

/// <summary>
/// SSE error response data
/// Used to unify error event format in SSE streams
/// Requirements: 11.1, 11.2, 11.3
/// </summary>
public class SSEErrorResponse
{
    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether retryable
    /// </summary>
    [JsonPropertyName("retryable")]
    public bool Retryable { get; set; }

    /// <summary>
    /// Retry delay (milliseconds), only valid when Retryable is true
    /// </summary>
    [JsonPropertyName("retryAfterMs")]
    public int? RetryAfterMs { get; set; }

    /// <summary>
    /// Additional error details (optional)
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; set; }

    /// <summary>
    /// Create an SSE error response
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="message">Error message, uses default message if empty</param>
    /// <param name="details">Additional details</param>
    /// <returns>SSE error response</returns>
    public static SSEErrorResponse Create(string code, string? message = null, object? details = null)
    {
        return new SSEErrorResponse
        {
            Code = code,
            Message = message ?? ChatErrorCodes.GetDefaultMessage(code),
            Retryable = ChatErrorCodes.IsRetryable(code),
            RetryAfterMs = ChatErrorCodes.IsRetryable(code) ? GetRetryDelay(code) : null,
            Details = details
        };
    }

    /// <summary>
    /// Create a retryable error response
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="message">Error message</param>
    /// <param name="retryAfterMs">Retry delay (milliseconds)</param>
    /// <returns>SSE error response</returns>
    public static SSEErrorResponse CreateRetryable(string code, string? message = null, int retryAfterMs = 1000)
    {
        return new SSEErrorResponse
        {
            Code = code,
            Message = message ?? ChatErrorCodes.GetDefaultMessage(code),
            Retryable = true,
            RetryAfterMs = retryAfterMs
        };
    }

    /// <summary>
    /// Create a non-retryable error response
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="message">Error message</param>
    /// <returns>SSE error response</returns>
    public static SSEErrorResponse CreateNonRetryable(string code, string? message = null)
    {
        return new SSEErrorResponse
        {
            Code = code,
            Message = message ?? ChatErrorCodes.GetDefaultMessage(code),
            Retryable = false,
            RetryAfterMs = null
        };
    }

    /// <summary>
    /// Get the default retry delay for an error code
    /// </summary>
    private static int GetRetryDelay(string code)
    {
        return code switch
        {
            ChatErrorCodes.RATE_LIMIT_EXCEEDED => 5000,  // Rate limiting requires longer wait
            ChatErrorCodes.REQUEST_TIMEOUT => 2000,
            ChatErrorCodes.CONNECTION_FAILED => 1000,
            ChatErrorCodes.STREAM_INTERRUPTED => 1000,
            ChatErrorCodes.INTERNAL_ERROR => 3000,
            _ => 1000
        };
    }
}
