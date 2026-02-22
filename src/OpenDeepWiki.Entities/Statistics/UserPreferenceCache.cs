using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User preference cache entity
/// Periodically aggregates and computes user language and topic preferences
/// </summary>
public class UserPreferenceCache : AggregateRoot<string>
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Language preference weights (JSON format)
    /// Example: {"C#": 0.5, "TypeScript": 0.3, "Python": 0.2}
    /// </summary>
    [StringLength(2000)]
    public string? LanguageWeights { get; set; }

    /// <summary>
    /// Topic preference weights (JSON format)
    /// Example: {"web": 0.4, "api": 0.3, "database": 0.3}
    /// </summary>
    [StringLength(2000)]
    public string? TopicWeights { get; set; }

    /// <summary>
    /// User private repository language distribution (JSON format)
    /// Extracted from repositories added by the user
    /// </summary>
    [StringLength(2000)]
    public string? PrivateRepoLanguages { get; set; }

    /// <summary>
    /// Last calculation time
    /// </summary>
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
