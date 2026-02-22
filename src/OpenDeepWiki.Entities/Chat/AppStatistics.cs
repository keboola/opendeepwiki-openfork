using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Application statistics entity
/// Records daily call counts, token consumption, and other statistics
/// </summary>
public class AppStatistics : AggregateRoot<Guid>
{
    /// <summary>
    /// Associated application ID
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Statistics date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total request count
    /// </summary>
    public long RequestCount { get; set; } = 0;

    /// <summary>
    /// Input token count
    /// </summary>
    public long InputTokens { get; set; } = 0;

    /// <summary>
    /// Output token count
    /// </summary>
    public long OutputTokens { get; set; } = 0;
}
