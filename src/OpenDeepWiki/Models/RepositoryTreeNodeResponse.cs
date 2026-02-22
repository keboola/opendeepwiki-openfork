namespace OpenDeepWiki.Models;

/// <summary>
/// Repository directory tree node
/// </summary>
public class RepositoryTreeNodeResponse
{
    /// <summary>
    /// Display name
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Route slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Child nodes
    /// </summary>
    public List<RepositoryTreeNodeResponse> Children { get; set; } = [];
}
