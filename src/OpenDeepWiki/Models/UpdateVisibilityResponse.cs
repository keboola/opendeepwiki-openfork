namespace OpenDeepWiki.Models;

/// <summary>
/// Update repository visibility response
/// </summary>
public class UpdateVisibilityResponse
{
    /// <summary>
    /// Repository ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Whether the repository is public
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message (only has value on failure)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
