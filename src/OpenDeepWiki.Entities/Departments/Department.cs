using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Department entity
/// </summary>
public class Department : AggregateRoot<string>
{
    /// <summary>
    /// Department name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent department ID
    /// </summary>
    [StringLength(36)]
    public string? ParentId { get; set; }

    /// <summary>
    /// Department description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Parent department navigation property
    /// </summary>
    [ForeignKey("ParentId")]
    public virtual Department? Parent { get; set; }
}
