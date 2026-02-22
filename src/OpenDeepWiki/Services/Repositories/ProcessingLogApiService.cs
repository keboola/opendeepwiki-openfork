using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Repositories;

/// <summary>
/// Processing log API service
/// </summary>
[MiniApi(Route = "/api/v1/repos")]
[Tags("Processing Logs")]
public class ProcessingLogApiService(IContext context, IProcessingLogService processingLogService)
{
    /// <summary>
    /// Get repository processing logs
    /// </summary>
    [HttpGet("/{owner}/{repo}/processing-logs")]
    public async Task<IResult> GetProcessingLogsAsync(
        string owner,
        string repo,
        [FromQuery] DateTime? since,
        [FromQuery] int limit = 100)
    {
        if (limit <= 0) limit = 100;
        if (limit > 500) limit = 500;

        var repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrgName == owner && r.RepoName == repo);

        if (repository is null)
        {
            return Results.NotFound(new { error = "Repository does not exist" });
        }

        var response = await processingLogService.GetLogsAsync(repository.Id, since, limit);

        return Results.Ok(new ProcessingLogApiResponse
        {
            Status = repository.Status,
            StatusName = repository.Status.ToString(),
            CurrentStep = response.CurrentStep,
            CurrentStepName = response.CurrentStep.ToString(),
            TotalDocuments = response.TotalDocuments,
            CompletedDocuments = response.CompletedDocuments,
            StartedAt = response.StartedAt,
            Logs = response.Logs.Select(log => new ProcessingLogApiItem
            {
                Id = log.Id,
                Step = log.Step,
                StepName = log.Step.ToString(),
                Message = log.Message,
                IsAiOutput = log.IsAiOutput,
                ToolName = log.ToolName,
                CreatedAt = log.CreatedAt
            }).ToList()
        });
    }
}

/// <summary>
/// Processing log API response
/// </summary>
public class ProcessingLogApiResponse
{
    public RepositoryStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public ProcessingStep CurrentStep { get; set; }
    public string CurrentStepName { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int CompletedDocuments { get; set; }
    public DateTime? StartedAt { get; set; }
    public List<ProcessingLogApiItem> Logs { get; set; } = new();
}

/// <summary>
/// Processing log API item
/// </summary>
public class ProcessingLogApiItem
{
    public string Id { get; set; } = string.Empty;
    public ProcessingStep Step { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAiOutput { get; set; }
    public string? ToolName { get; set; }
    public DateTime CreatedAt { get; set; }
}
