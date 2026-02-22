using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Document file entity
/// </summary>
public class DocFile : AggregateRoot<string>
{
    /// <summary>
    /// Branch language ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string BranchLanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Document content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Source file list (stored in JSON array format)
    /// Records the source code file paths read when generating this document
    /// </summary>
    public string? SourceFiles { get; set; }

    /// <summary>
    /// Branch language navigation property
    /// </summary>
    [ForeignKey("BranchLanguageId")]
    public virtual BranchLanguage? BranchLanguage { get; set; }
}
