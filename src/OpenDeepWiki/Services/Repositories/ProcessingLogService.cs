using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Repositories;

/// <summary>
/// Processing log service implementation
/// </summary>
public class ProcessingLogService : IProcessingLogService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ProcessingLogService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        string repositoryId,
        ProcessingStep step,
        string message,
        bool isAiOutput = false,
        string? toolName = null,
        CancellationToken cancellationToken = default)
    {
        // Use an independent scope to save logs, avoiding interference with other operations
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();

        var log = new RepositoryProcessingLog
        {
            Id = Guid.NewGuid().ToString(),
            RepositoryId = repositoryId,
            Step = step,
            Message = message,
            IsAiOutput = isAiOutput,
            ToolName = toolName,
            CreatedAt = DateTime.UtcNow
        };

        context.RepositoryProcessingLogs.Add(log);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProcessingLogResponse> GetLogsAsync(
        string repositoryId,
        DateTime? since = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();

        var query = context.RepositoryProcessingLogs
            .Where(log => log.RepositoryId == repositoryId);

        if (since.HasValue)
        {
            query = query.Where(log => log.CreatedAt > since.Value);
        }

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .Take(limit)
            .Select(log => new ProcessingLogItem
            {
                Id = log.Id,
                Step = log.Step,
                Message = log.Message,
                IsAiOutput = log.IsAiOutput,
                ToolName = log.ToolName,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Reverse the list so it's in chronological order (earliest first)
        logs.Reverse();

        // Get current step (step from the latest log entry)
        var currentStep = logs.LastOrDefault()?.Step ?? ProcessingStep.Workspace;

        // Parse document generation progress
        var (totalDocuments, completedDocuments) = ParseDocumentProgress(logs);

        // Get start time (time of the first log entry)
        var startedAt = logs.FirstOrDefault()?.CreatedAt;

        return new ProcessingLogResponse
        {
            CurrentStep = currentStep,
            Logs = logs,
            TotalDocuments = totalDocuments,
            CompletedDocuments = completedDocuments,
            StartedAt = startedAt
        };
    }

    /// <summary>
    /// Parse document generation progress from logs
    /// </summary>
    private static (int total, int completed) ParseDocumentProgress(List<ProcessingLogItem> logs)
    {
        int total = 0;
        int completed = 0;

        foreach (var log in logs)
        {
            if (log.Step != ProcessingStep.Content || log.IsAiOutput || !string.IsNullOrEmpty(log.ToolName))
                continue;

            // Match "Found X documents to generate" or legacy "发现 X 个文档" format
            var totalMatch = System.Text.RegularExpressions.Regex.Match(
                log.Message, @"(?:Found\s+(\d+)\s+documents|发现\s*(\d+)\s*个文档)");
            if (totalMatch.Success)
            {
                total = int.Parse(totalMatch.Groups[1].Success ? totalMatch.Groups[1].Value : totalMatch.Groups[2].Value);
                continue;
            }

            // Match "Document completed (X/Y)" or legacy "文档完成 (X/Y)" format
            var completedMatch = System.Text.RegularExpressions.Regex.Match(
                log.Message, @"(?:Document completed|文档完成)\s*\((\d+)/(\d+)\)");
            if (completedMatch.Success)
            {
                completed = Math.Max(completed, int.Parse(completedMatch.Groups[1].Value));
                if (total == 0)
                {
                    total = int.Parse(completedMatch.Groups[2].Value);
                }
                continue;
            }

            // Match "Start generating document (X/Y)" or legacy Chinese format - only used to fill in total
            var progressMatch = System.Text.RegularExpressions.Regex.Match(
                log.Message, @"(Start generating document|Generating document|开始生成文档|正在生成文档)\s*\((\d+)/(\d+)\)");
            if (progressMatch.Success)
            {
                if (total == 0)
                {
                    total = int.Parse(progressMatch.Groups[3].Value);
                }
                continue;
            }

            // Match "Document generation completed" or legacy "文档生成完成" format
            if (log.Message.Contains("文档生成完成") || log.Message.Contains("Document generation completed"))
            {
                completed = total;
            }
        }

        return (total, completed);
    }

    /// <inheritdoc />
    public async Task ClearLogsAsync(string repositoryId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();

        var logs = await context.RepositoryProcessingLogs
            .Where(log => log.RepositoryId == repositoryId)
            .ToListAsync(cancellationToken);

        context.RepositoryProcessingLogs.RemoveRange(logs);
        await context.SaveChangesAsync(cancellationToken);
    }
}
