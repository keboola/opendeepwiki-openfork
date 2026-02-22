using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin tools configuration endpoints
/// </summary>
public static class AdminToolsEndpoints
{
    public static RouteGroupBuilder MapAdminToolsEndpoints(this RouteGroupBuilder group)
    {
        var toolsGroup = group.MapGroup("/tools")
            .WithTags("Admin - Tools Config");

        MapMcpEndpoints(toolsGroup);
        MapSkillEndpoints(toolsGroup);
        MapModelEndpoints(toolsGroup);

        return group;
    }

    private static void MapMcpEndpoints(RouteGroupBuilder group)
    {
        var mcpGroup = group.MapGroup("/mcps");

        mcpGroup.MapGet("/", async ([FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.GetMcpConfigsAsync();
            return Results.Ok(new { success = true, data = result });
        }).WithName("AdminGetMcps");

        mcpGroup.MapPost("/", async (
            [FromBody] McpConfigRequest request,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.CreateMcpConfigAsync(request);
            return Results.Ok(new { success = true, data = result });
        }).WithName("AdminCreateMcp");

        mcpGroup.MapPut("/{id}", async (
            string id,
            [FromBody] McpConfigRequest request,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.UpdateMcpConfigAsync(id, request);
            return result ? Results.Ok(new { success = true })
                : Results.NotFound(new { success = false });
        }).WithName("AdminUpdateMcp");

        mcpGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.DeleteMcpConfigAsync(id);
            return result ? Results.Ok(new { success = true })
                : Results.NotFound(new { success = false });
        }).WithName("AdminDeleteMcp");
    }

    private static void MapSkillEndpoints(RouteGroupBuilder group)
    {
        var skillGroup = group.MapGroup("/skills");

        // Get all Skills
        skillGroup.MapGet("/", async ([FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.GetSkillConfigsAsync();
            return Results.Ok(new { success = true, data = result });
        }).WithName("AdminGetSkills");

        // Get Skill details
        skillGroup.MapGet("/{id}", async (
            string id,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.GetSkillDetailAsync(id);
            return result != null 
                ? Results.Ok(new { success = true, data = result })
                : Results.NotFound(new { success = false, message = "Skill not found" });
        }).WithName("AdminGetSkillDetail");

        // Upload Skill (ZIP archive)
        skillGroup.MapPost("/upload", async (
            HttpRequest request,
            [FromServices] IAdminToolsService toolsService) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { success = false, message = "Please use multipart/form-data format" });
            }

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { success = false, message = "Please upload a ZIP file" });
            }

            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { success = false, message = "Only ZIP format is supported" });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await toolsService.UploadSkillAsync(stream, file.FileName);
                return Results.Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        }).WithName("AdminUploadSkill")
          .DisableAntiforgery();

        // Update Skill (admin fields only)
        skillGroup.MapPut("/{id}", async (
            string id,
            [FromBody] SkillUpdateRequest request,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.UpdateSkillAsync(id, request);
            return result ? Results.Ok(new { success = true })
                : Results.NotFound(new { success = false });
        }).WithName("AdminUpdateSkill");

        // Delete Skill
        skillGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.DeleteSkillAsync(id);
            return result ? Results.Ok(new { success = true })
                : Results.NotFound(new { success = false });
        }).WithName("AdminDeleteSkill");

        // Get Skill file content
        skillGroup.MapGet("/{id}/files/{*filePath}", async (
            string id,
            string filePath,
            [FromServices] IAdminToolsService toolsService) =>
        {
            try
            {
                var content = await toolsService.GetSkillFileContentAsync(id, filePath);
                return content != null 
                    ? Results.Ok(new { success = true, data = content })
                    : Results.NotFound(new { success = false, message = "File not found" });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.BadRequest(new { success = false, message = "Invalid path" });
            }
        }).WithName("AdminGetSkillFile");

        // Refresh Skills from disk
        skillGroup.MapPost("/refresh", async ([FromServices] IAdminToolsService toolsService) =>
        {
            await toolsService.RefreshSkillsFromDiskAsync();
            return Results.Ok(new { success = true, message = "Refresh completed" });
        }).WithName("AdminRefreshSkills");
    }

    private static void MapModelEndpoints(RouteGroupBuilder group)
    {
        var modelGroup = group.MapGroup("/models");

        modelGroup.MapGet("/", async ([FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.GetModelConfigsAsync();
            return Results.Ok(new { success = true, data = result });
        }).WithName("AdminGetModels");

        modelGroup.MapPost("/", async (
            [FromBody] ModelConfigRequest request,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.CreateModelConfigAsync(request);
            return Results.Ok(new { success = true, data = result });
        }).WithName("AdminCreateModel");

        modelGroup.MapPut("/{id}", async (
            string id,
            [FromBody] ModelConfigRequest request,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.UpdateModelConfigAsync(id, request);
            return result ? Results.Ok(new { success = true })
                : Results.NotFound(new { success = false });
        }).WithName("AdminUpdateModel");

        modelGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IAdminToolsService toolsService) =>
        {
            var result = await toolsService.DeleteModelConfigAsync(id);
            return result ? Results.Ok(new { success = true })
                : Results.NotFound(new { success = false });
        }).WithName("AdminDeleteModel");
    }
}
