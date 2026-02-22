namespace OpenDeepWiki.Models.Subscription;

/// <summary>
/// Subscription status response
/// </summary>
public class SubscriptionStatusResponse
{
    /// <summary>
    /// Whether the user is subscribed
    /// </summary>
    public bool IsSubscribed { get; set; }

    /// <summary>
    /// Subscribed at timestamp (only has value when subscribed)
    /// </summary>
    public DateTime? SubscribedAt { get; set; }
}
