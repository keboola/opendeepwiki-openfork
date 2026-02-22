namespace OpenDeepWiki.Models.Subscription;

/// <summary>
/// Subscription list response
/// </summary>
public class SubscriptionListResponse
{
    /// <summary>
    /// List of subscription items
    /// </summary>
    public List<SubscriptionItemResponse> Items { get; set; } = [];

    /// <summary>
    /// Total count
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}

/// <summary>
/// Subscription item response
/// </summary>
public class SubscriptionItemResponse
{
    /// <summary>
    /// Subscription record ID
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID
    /// </summary>
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrgName { get; set; } = string.Empty;

    /// <summary>
    /// Repository description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Star count
    /// </summary>
    public int StarCount { get; set; }

    /// <summary>
    /// Fork count
    /// </summary>
    public int ForkCount { get; set; }

    /// <summary>
    /// Subscription count
    /// </summary>
    public int SubscriptionCount { get; set; }

    /// <summary>
    /// Subscribed at timestamp
    /// </summary>
    public DateTime SubscribedAt { get; set; }
}
