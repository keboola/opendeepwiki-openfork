using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Processing step type
/// </summary>
public enum ProcessingStep
{
    /// <summary>
    /// Prepare workspace
    /// </summary>
    Workspace = 0,

    /// <summary>
    /// Generate catalog
    /// </summary>
    Catalog = 1,

    /// <summary>
    /// Generate document content
    /// </summary>
    Content = 2,

    /// <summary>
    /// Multi-language translation
    /// </summary>
    Translation = 3,

    /// <summary>
    /// Generate mind map
    /// </summary>
    MindMap = 4,

    /// <summary>
    /// Complete
    /// </summary>
    Complete = 5
}

/// <summary>
/// Repository processing log entity
/// </summary>
public class RepositoryProcessingLog : AggregateRoot<string>
{
    /// <summary>
    /// Associated repository ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Current processing step
    /// </summary>
    public ProcessingStep Step { get; set; } = ProcessingStep.Workspace;

    /// <summary>
    /// Log message
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is AI output
    /// </summary>
    public bool IsAiOutput { get; set; } = false;

    /// <summary>
    /// Tool call name (if it is a tool call)
    /// </summary>
    [StringLength(100)]
    public string? ToolName { get; set; }

    /// <summary>
    /// Associated repository navigation property
    /// </summary>
    [ForeignKey("RepositoryId")]
    public virtual Repository? Repository { get; set; }
}
