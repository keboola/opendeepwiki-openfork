namespace OpenDeepWiki.Chat.Providers;

/// <summary>
/// Send result
/// </summary>
public record SendResult(
    bool Success,
    string? MessageId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    bool ShouldRetry = false
);
