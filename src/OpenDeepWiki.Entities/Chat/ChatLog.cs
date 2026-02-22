using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// User question log entity
/// Records conversation content from embedded scripts
/// </summary>
public class ChatLog : AggregateRoot<Guid>
{
    /// <summary>
    /// Associated application ID
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// User identifier (optional)
    /// </summary>
    [StringLength(100)]
    public string? UserIdentifier { get; set; }

    /// <summary>
    /// User question content
    /// </summary>
    [Required]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// AI response summary
    /// </summary>
    [StringLength(500)]
    public string? AnswerSummary { get; set; }

    /// <summary>
    /// Input token count
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Output token count
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Model used
    /// </summary>
    [StringLength(100)]
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Request source domain
    /// </summary>
    [StringLength(500)]
    public string? SourceDomain { get; set; }
}
