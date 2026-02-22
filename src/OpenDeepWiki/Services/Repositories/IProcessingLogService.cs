using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Repositories;

/// <summary>
/// Processing log service interface
/// </summary>
public interface IProcessingLogService
{
    /// <summary>
    /// Record processing log
    /// </summary>
    Task LogAsync(
        string repositoryId,
        ProcessingStep step,
        string message,
        bool isAiOutput = false,
        string? toolName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get processing logs for a repository
    /// </summary>
    Task<ProcessingLogResponse> GetLogsAsync(
        string repositoryId,
        DateTime? since = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear processing logs for a repository
    /// </summary>
    Task ClearLogsAsync(string repositoryId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Processing log response
/// </summary>
public class ProcessingLogResponse
{
    public ProcessingStep CurrentStep { get; set; }
    public List<ProcessingLogItem> Logs { get; set; } = new();
    
    /// <summary>
    /// Document generation progress - total count
    /// </summary>
    public int TotalDocuments { get; set; }

    /// <summary>
    /// Document generation progress - completed count
    /// </summary>
    public int CompletedDocuments { get; set; }

    /// <summary>
    /// Processing start time (time of the first log entry)
    /// </summary>
    public DateTime? StartedAt { get; set; }
}

/// <summary>
/// Processing log item
/// </summary>
public class ProcessingLogItem
{
    public string Id { get; set; } = string.Empty;
    public ProcessingStep Step { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsAiOutput { get; set; }
    public string? ToolName { get; set; }
    public DateTime CreatedAt { get; set; }
}
