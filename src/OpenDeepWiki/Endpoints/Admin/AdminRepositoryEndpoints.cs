using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin repository management endpoints
/// </summary>
public static class AdminRepositoryEndpoints
{
    public static RouteGroupBuilder MapAdminRepositoryEndpoints(this RouteGroupBuilder group)
    {
        var repoGroup = group.MapGroup("/repositories")
            .WithTags("Admin - Repository Management");

        // Get repository list (paginated)
        repoGroup.MapGet("/", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            var result = await repositoryService.GetRepositoriesAsync(page, pageSize, search, status);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetRepositories")
        .WithSummary("Get repository list");

        // Get repository details
        repoGroup.MapGet("/{id}", async (
            string id,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.GetRepositoryByIdAsync(id);
            if (result == null)
                return Results.NotFound(new { success = false, message = "Repository not found" });
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetRepository")
        .WithSummary("Get repository details");

        // Get repository deep management info (branches, languages, incremental tasks)
        repoGroup.MapGet("/{id}/management", async (
            string id,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.GetRepositoryManagementAsync(id);
            if (result == null)
                return Results.NotFound(new { success = false, message = "Repository not found" });
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetRepositoryManagement")
        .WithSummary("Get repository management info");

        // Update repository
        repoGroup.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateRepositoryRequest request,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.UpdateRepositoryAsync(id, request);
            if (!result)
                return Results.NotFound(new { success = false, message = "Repository not found" });
            return Results.Ok(new { success = true, message = "Updated successfully" });
        })
        .WithName("AdminUpdateRepository")
        .WithSummary("Update repository");

        // Delete repository
        repoGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.DeleteRepositoryAsync(id);
            if (!result)
                return Results.NotFound(new { success = false, message = "Repository not found" });
            return Results.Ok(new { success = true, message = "Deleted successfully" });
        })
        .WithName("AdminDeleteRepository")
        .WithSummary("Delete repository");

        // Update repository status
        repoGroup.MapPut("/{id}/status", async (
            string id,
            [FromBody] UpdateStatusRequest request,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.UpdateRepositoryStatusAsync(id, request.Status);
            if (!result)
                return Results.NotFound(new { success = false, message = "Repository not found" });
            return Results.Ok(new { success = true, message = "Status updated successfully" });
        })
        .WithName("AdminUpdateRepositoryStatus")
        .WithSummary("Update repository status");

        // Sync single repository statistics
        repoGroup.MapPost("/{id}/sync-stats", async (
            string id,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.SyncRepositoryStatsAsync(id);
            return Results.Ok(new { success = result.Success, message = result.Message, data = result });
        })
        .WithName("AdminSyncRepositoryStats")
        .WithSummary("Sync repository statistics");

        // Trigger full repository regeneration
        repoGroup.MapPost("/{id}/regenerate", async (
            string id,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.RegenerateRepositoryAsync(id);
            return Results.Ok(new { success = result.Success, message = result.Message, data = result });
        })
        .WithName("AdminRegenerateRepository")
        .WithSummary("Trigger full repository regeneration");

        // Trigger specific document regeneration
        repoGroup.MapPost("/{id}/documents/regenerate", async (
            string id,
            [FromBody] RegenerateRepositoryDocumentRequest request,
            [FromServices] IAdminRepositoryService repositoryService,
            CancellationToken cancellationToken) =>
        {
            var result = await repositoryService.RegenerateDocumentAsync(id, request, cancellationToken);
            return Results.Ok(new { success = result.Success, message = result.Message, data = result });
        })
        .WithName("AdminRegenerateRepositoryDocument")
        .WithSummary("Trigger specific document regeneration");

        // Manually update specific document content
        repoGroup.MapPut("/{id}/documents/content", async (
            string id,
            [FromBody] UpdateRepositoryDocumentContentRequest request,
            [FromServices] IAdminRepositoryService repositoryService,
            CancellationToken cancellationToken) =>
        {
            var result = await repositoryService.UpdateDocumentContentAsync(id, request, cancellationToken);
            return Results.Ok(new { success = result.Success, message = result.Message, data = result });
        })
        .WithName("AdminUpdateRepositoryDocumentContent")
        .WithSummary("Manually update specific document content");

        // Batch sync repository statistics
        repoGroup.MapPost("/batch/sync-stats", async (
            [FromBody] BatchOperationRequest request,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.BatchSyncRepositoryStatsAsync(request.Ids);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminBatchSyncRepositoryStats")
        .WithSummary("Batch sync repository statistics");

        // Batch delete repositories
        repoGroup.MapPost("/batch/delete", async (
            [FromBody] BatchOperationRequest request,
            [FromServices] IAdminRepositoryService repositoryService) =>
        {
            var result = await repositoryService.BatchDeleteRepositoriesAsync(request.Ids);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminBatchDeleteRepositories")
        .WithSummary("Batch delete repositories");

        return group;
    }
}
