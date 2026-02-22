using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Repositories;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// Incremental update endpoint logger class (used for generic logger)
/// </summary>
public class IncrementalUpdateEndpointsLogger { }

/// <summary>
/// Incremental update API endpoints
/// Provides functionality for manually triggering incremental updates, querying task status, and retrying failed tasks
/// </summary>
public static class IncrementalUpdateEndpoints
{
    /// <summary>
    /// Register all incremental update related endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapIncrementalUpdateEndpoints(this IEndpointRouteBuilder app)
    {
        // Repository incremental update trigger endpoints
        var repoGroup = app.MapGroup("/api/v1/repositories")
            .WithTags("Incremental Updates");

        repoGroup.MapPost("/{repositoryId}/branches/{branchId}/incremental-update", TriggerIncrementalUpdateAsync)
            .WithName("TriggerIncrementalUpdate")
            .WithSummary("Manually trigger incremental update")
            .WithDescription("Create a high-priority incremental update task for the specified repository and branch");

        // Incremental update task management endpoints
        var taskGroup = app.MapGroup("/api/v1/incremental-updates")
            .WithTags("Incremental Update Tasks");

        taskGroup.MapGet("/{taskId}", GetTaskStatusAsync)
            .WithName("GetIncrementalUpdateTaskStatus")
            .WithSummary("Get task status")
            .WithDescription("Get the detailed status of a specific incremental update task");

        taskGroup.MapPost("/{taskId}/retry", RetryFailedTaskAsync)
            .WithName("RetryFailedIncrementalUpdateTask")
            .WithSummary("Retry failed task")
            .WithDescription("Retry a failed incremental update task");

        return app;
    }

    /// <summary>
    /// Manually trigger incremental update
    /// POST /api/v1/repositories/{repositoryId}/branches/{branchId}/incremental-update
    /// </summary>
    private static async Task<IResult> TriggerIncrementalUpdateAsync(
        string repositoryId,
        string branchId,
        [FromServices] IIncrementalUpdateService updateService,
        [FromServices] IContext context,
        [FromServices] ILogger<IncrementalUpdateEndpointsLogger> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Manual incremental update requested. RepositoryId: {RepositoryId}, BranchId: {BranchId}",
            repositoryId, branchId);

        try
        {
            // Verify repository exists
            var repository = await context.Repositories
                .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken);

            if (repository == null)
            {
                logger.LogWarning("Repository not found. RepositoryId: {RepositoryId}", repositoryId);
                return Results.NotFound(new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = "Repository not found",
                    ErrorCode = "REPOSITORY_NOT_FOUND"
                });
            }

            // Verify branch exists
            var branch = await context.RepositoryBranches
                .FirstOrDefaultAsync(b => b.Id == branchId && b.RepositoryId == repositoryId, cancellationToken);

            if (branch == null)
            {
                logger.LogWarning("Branch not found. BranchId: {BranchId}", branchId);
                return Results.NotFound(new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = "Branch not found",
                    ErrorCode = "BRANCH_NOT_FOUND"
                });
            }

            // Trigger incremental update
            var taskId = await updateService.TriggerManualUpdateAsync(repositoryId, branchId, cancellationToken);

            // Get task status
            var task = await context.IncrementalUpdateTasks
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            logger.LogInformation(
                "Incremental update task created/found. TaskId: {TaskId}, Status: {Status}",
                taskId, task?.Status);

            return Results.Ok(new TriggerIncrementalUpdateResponse
            {
                Success = true,
                TaskId = taskId,
                Status = task?.Status.ToString() ?? "Unknown",
                Message = task?.Status == IncrementalUpdateStatus.Processing
                    ? "Task is currently being processed"
                    : "Incremental update task created"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to trigger incremental update. RepositoryId: {RepositoryId}, BranchId: {BranchId}",
                repositoryId, branchId);

            return Results.Json(
                new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = "Failed to trigger incremental update",
                    ErrorCode = "TRIGGER_FAILED",
                    Details = ex.Message
                },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }


    /// <summary>
    /// Get task status
    /// GET /api/v1/incremental-updates/{taskId}
    /// </summary>
    private static async Task<IResult> GetTaskStatusAsync(
        string taskId,
        [FromServices] IContext context,
        [FromServices] ILogger<IncrementalUpdateEndpointsLogger> logger,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting task status. TaskId: {TaskId}", taskId);

        try
        {
            var task = await context.IncrementalUpdateTasks
                .Include(t => t.Repository)
                .Include(t => t.Branch)
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
            {
                logger.LogWarning("Task not found. TaskId: {TaskId}", taskId);
                return Results.NotFound(new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = "Task not found",
                    ErrorCode = "TASK_NOT_FOUND"
                });
            }

            return Results.Ok(new IncrementalUpdateTaskResponse
            {
                Success = true,
                TaskId = task.Id,
                RepositoryId = task.RepositoryId,
                RepositoryName = task.Repository != null
                    ? $"{task.Repository.OrgName}/{task.Repository.RepoName}"
                    : null,
                BranchId = task.BranchId,
                BranchName = task.Branch?.BranchName,
                Status = task.Status.ToString(),
                Priority = task.Priority,
                IsManualTrigger = task.IsManualTrigger,
                PreviousCommitId = task.PreviousCommitId,
                TargetCommitId = task.TargetCommitId,
                RetryCount = task.RetryCount,
                ErrorMessage = task.ErrorMessage,
                CreatedAt = task.CreatedAt,
                StartedAt = task.StartedAt,
                CompletedAt = task.CompletedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get task status. TaskId: {TaskId}", taskId);

            return Results.Json(
                new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = "Failed to get task status",
                    ErrorCode = "GET_STATUS_FAILED",
                    Details = ex.Message
                },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retry failed task
    /// POST /api/v1/incremental-updates/{taskId}/retry
    /// </summary>
    private static async Task<IResult> RetryFailedTaskAsync(
        string taskId,
        [FromServices] IContext context,
        [FromServices] ILogger<IncrementalUpdateEndpointsLogger> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Retry requested for task. TaskId: {TaskId}", taskId);

        try
        {
            var task = await context.IncrementalUpdateTasks
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
            {
                logger.LogWarning("Task not found. TaskId: {TaskId}", taskId);
                return Results.NotFound(new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = "Task not found",
                    ErrorCode = "TASK_NOT_FOUND"
                });
            }

            // Can only retry failed tasks
            if (task.Status != IncrementalUpdateStatus.Failed)
            {
                logger.LogWarning(
                    "Cannot retry task with status {Status}. TaskId: {TaskId}",
                    task.Status, taskId);

                return Results.BadRequest(new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = $"Can only retry failed tasks, current status: {task.Status}",
                    ErrorCode = "INVALID_TASK_STATUS"
                });
            }

            // Reset task status
            task.Status = IncrementalUpdateStatus.Pending;
            task.RetryCount++;
            task.ErrorMessage = null;
            task.StartedAt = null;
            task.CompletedAt = null;
            task.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Task reset for retry. TaskId: {TaskId}, RetryCount: {RetryCount}",
                taskId, task.RetryCount);

            return Results.Ok(new RetryTaskResponse
            {
                Success = true,
                TaskId = task.Id,
                Status = task.Status.ToString(),
                RetryCount = task.RetryCount,
                Message = "Task has been reset and will be reprocessed on the next poll"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retry task. TaskId: {TaskId}", taskId);

            return Results.Json(
                new IncrementalUpdateErrorResponse
                {
                    Success = false,
                    Error = "Failed to retry task",
                    ErrorCode = "RETRY_FAILED",
                    Details = ex.Message
                },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}


#region Response models

/// <summary>
/// Trigger incremental update response
/// </summary>
public class TriggerIncrementalUpdateResponse
{
    /// <summary>
    /// Whether successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Task status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Incremental update task detail response
/// </summary>
public class IncrementalUpdateTaskResponse
{
    /// <summary>
    /// Whether successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID
    /// </summary>
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// Repository name (org/repo)
    /// </summary>
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Branch ID
    /// </summary>
    public string BranchId { get; set; } = string.Empty;

    /// <summary>
    /// Branch name
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Task status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Task priority
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether manually triggered
    /// </summary>
    public bool IsManualTrigger { get; set; }

    /// <summary>
    /// Previous processed Commit ID
    /// </summary>
    public string? PreviousCommitId { get; set; }

    /// <summary>
    /// Target Commit ID
    /// </summary>
    public string? TargetCommitId { get; set; }

    /// <summary>
    /// Retry count
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Created time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Processing start time
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Retry task response
/// </summary>
public class RetryTaskResponse
{
    /// <summary>
    /// Whether successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Task status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Retry count
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Incremental update error response
/// </summary>
public class IncrementalUpdateErrorResponse
{
    /// <summary>
    /// Whether successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Error code
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Details
    /// </summary>
    public string? Details { get; set; }
}

#endregion
