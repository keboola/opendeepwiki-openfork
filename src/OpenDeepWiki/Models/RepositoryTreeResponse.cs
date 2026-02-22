using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Models;

/// <summary>
/// Repository directory tree response
/// </summary>
public class RepositoryTreeResponse
{
    /// <summary>
    /// Organization name
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    public string Repo { get; set; } = string.Empty;

    /// <summary>
    /// Default slug
    /// </summary>
    public string DefaultSlug { get; set; } = string.Empty;

    /// <summary>
    /// Directory tree nodes
    /// </summary>
    public List<RepositoryTreeNodeResponse> Nodes { get; set; } = [];

    /// <summary>
    /// Repository processing status
    /// </summary>
    public RepositoryStatus Status { get; set; } = RepositoryStatus.Completed;

    /// <summary>
    /// Status name
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Whether the repository exists
    /// </summary>
    public bool Exists { get; set; } = true;

    /// <summary>
    /// Current branch
    /// </summary>
    public string CurrentBranch { get; set; } = string.Empty;

    /// <summary>
    /// Current language
    /// </summary>
    public string CurrentLanguage { get; set; } = string.Empty;
}
