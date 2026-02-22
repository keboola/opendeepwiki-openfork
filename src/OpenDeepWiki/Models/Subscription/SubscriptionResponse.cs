namespace OpenDeepWiki.Models.Subscription;

/// <summary>
/// Subscription operation response
/// </summary>
public class SubscriptionResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message (only has value on failure)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Subscription record ID (only has value on success)
    /// </summary>
    public string? SubscriptionId { get; set; }
}
