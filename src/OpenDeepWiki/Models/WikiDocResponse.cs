namespace OpenDeepWiki.Models;

/// <summary>
/// Wiki document response
/// </summary>
public class WikiDocResponse
{
    /// <summary>
    /// Document path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Document title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Markdown content
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
