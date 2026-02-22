namespace OpenDeepWiki.Models;

/// <summary>
/// Repository branches and languages response (fetched from database)
/// </summary>
public class RepositoryBranchesResponse
{
    /// <summary>
    /// List of branches
    /// </summary>
    public List<BranchItem> Branches { get; set; } = [];

    /// <summary>
    /// All available languages
    /// </summary>
    public List<string> Languages { get; set; } = [];

    /// <summary>
    /// Default branch
    /// </summary>
    public string DefaultBranch { get; set; } = string.Empty;

    /// <summary>
    /// Default language
    /// </summary>
    public string DefaultLanguage { get; set; } = string.Empty;
}

/// <summary>
/// Branch item
/// </summary>
public class BranchItem
{
    /// <summary>
    /// Branch name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of languages supported by this branch
    /// </summary>
    public List<string> Languages { get; set; } = [];
}

/// <summary>
/// Git platform branches list response (fetched from remote API)
/// </summary>
public class GitBranchesResponse
{
    /// <summary>
    /// List of branches
    /// </summary>
    public List<GitBranchItem> Branches { get; set; } = [];

    /// <summary>
    /// Default branch
    /// </summary>
    public string? DefaultBranch { get; set; }

    /// <summary>
    /// Whether fetching branches is supported (platform support)
    /// </summary>
    public bool IsSupported { get; set; }
}

/// <summary>
/// Git branch item
/// </summary>
public class GitBranchItem
{
    /// <summary>
    /// Branch name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default branch
    /// </summary>
    public bool IsDefault { get; set; }
}
