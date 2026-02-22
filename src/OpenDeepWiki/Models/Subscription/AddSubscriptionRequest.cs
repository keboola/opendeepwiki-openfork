using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models.Subscription;

/// <summary>
/// Add subscription request
/// </summary>
public class AddSubscriptionRequest
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;
}
