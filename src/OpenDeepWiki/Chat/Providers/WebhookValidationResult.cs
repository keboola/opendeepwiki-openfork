namespace OpenDeepWiki.Chat.Providers;

/// <summary>
/// Webhook validation result
/// </summary>
public record WebhookValidationResult(
    bool IsValid,
    string? Challenge = null,
    string? ErrorMessage = null
);
