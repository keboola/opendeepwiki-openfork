using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Models;

/// <summary>
/// Regenerate repository documentation request
/// </summary>
public class RegenerateRequest
{
    /// <summary>
    /// Repository owner
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Repo { get; set; } = string.Empty;
}

/// <summary>
/// Regenerate repository documentation response
/// </summary>
public class RegenerateResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}
