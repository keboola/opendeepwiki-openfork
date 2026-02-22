using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Document catalog entity (supports tree structure)
/// </summary>
public class DocCatalog : AggregateRoot<string>
{
    /// <summary>
    /// Branch language ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string BranchLanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Parent catalog ID (null indicates root node)
    /// </summary>
    [StringLength(36)]
    public string? ParentId { get; set; }

    /// <summary>
    /// Catalog title
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly path, e.g. "1-overview"
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Sort order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Associated document file ID
    /// </summary>
    [StringLength(36)]
    public string? DocFileId { get; set; }

    /// <summary>
    /// Parent catalog navigation property
    /// </summary>
    [ForeignKey("ParentId")]
    public virtual DocCatalog? Parent { get; set; }

    /// <summary>
    /// Child catalog collection
    /// </summary>
    public virtual ICollection<DocCatalog> Children { get; set; } = new List<DocCatalog>();

    /// <summary>
    /// Branch language navigation property
    /// </summary>
    [ForeignKey("BranchLanguageId")]
    public virtual BranchLanguage? BranchLanguage { get; set; }

    /// <summary>
    /// Document file navigation property
    /// </summary>
    [ForeignKey("DocFileId")]
    public virtual DocFile? DocFile { get; set; }
}
