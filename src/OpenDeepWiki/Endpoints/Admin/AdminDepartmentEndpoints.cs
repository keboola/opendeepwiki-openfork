using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin department management endpoints
/// </summary>
public static class AdminDepartmentEndpoints
{
    public static RouteGroupBuilder MapAdminDepartmentEndpoints(this RouteGroupBuilder group)
    {
        var deptGroup = group.MapGroup("/departments")
            .WithTags("Admin - Department Management");

        // Get department list
        deptGroup.MapGet("/", async ([FromServices] IAdminDepartmentService deptService) =>
        {
            var result = await deptService.GetDepartmentsAsync();
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetDepartments")
        .WithSummary("Get department list");

        // Get department tree structure
        deptGroup.MapGet("/tree", async ([FromServices] IAdminDepartmentService deptService) =>
        {
            var result = await deptService.GetDepartmentTreeAsync();
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetDepartmentTree")
        .WithSummary("Get department tree structure");

        // Get department details
        deptGroup.MapGet("/{id}", async (
            string id,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            var result = await deptService.GetDepartmentByIdAsync(id);
            if (result == null)
                return Results.NotFound(new { success = false, message = "Department not found" });
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetDepartment")
        .WithSummary("Get department details");

        // Create department
        deptGroup.MapPost("/", async (
            [FromBody] CreateDepartmentRequest request,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            try
            {
                var result = await deptService.CreateDepartmentAsync(request);
                return Results.Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminCreateDepartment")
        .WithSummary("Create department");

        // Update department
        deptGroup.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateDepartmentRequest request,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            try
            {
                var result = await deptService.UpdateDepartmentAsync(id, request);
                if (!result)
                    return Results.NotFound(new { success = false, message = "Department not found" });
                return Results.Ok(new { success = true, message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminUpdateDepartment")
        .WithSummary("Update department");

        // Delete department
        deptGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            try
            {
                var result = await deptService.DeleteDepartmentAsync(id);
                if (!result)
                    return Results.NotFound(new { success = false, message = "Department not found" });
                return Results.Ok(new { success = true, message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminDeleteDepartment")
        .WithSummary("Delete department");

        // Get department user list
        deptGroup.MapGet("/{id}/users", async (
            string id,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            var result = await deptService.GetDepartmentUsersAsync(id);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetDepartmentUsers")
        .WithSummary("Get department user list");

        // Add user to department
        deptGroup.MapPost("/{id}/users", async (
            string id,
            [FromBody] AddUserToDepartmentRequest request,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            try
            {
                await deptService.AddUserToDepartmentAsync(id, request.UserId, request.IsManager);
                return Results.Ok(new { success = true, message = "Added successfully" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminAddUserToDepartment")
        .WithSummary("Add user to department");

        // Remove user from department
        deptGroup.MapDelete("/{id}/users/{userId}", async (
            string id,
            string userId,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            var result = await deptService.RemoveUserFromDepartmentAsync(id, userId);
            if (!result)
                return Results.NotFound(new { success = false, message = "User is not in this department" });
            return Results.Ok(new { success = true, message = "Removed successfully" });
        })
        .WithName("AdminRemoveUserFromDepartment")
        .WithSummary("Remove user from department");

        // Get department repository list
        deptGroup.MapGet("/{id}/repositories", async (
            string id,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            var result = await deptService.GetDepartmentRepositoriesAsync(id);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetDepartmentRepositories")
        .WithSummary("Get department repository list");

        // Assign repository to department
        deptGroup.MapPost("/{id}/repositories", async (
            string id,
            [FromBody] AssignRepositoryRequest request,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            try
            {
                await deptService.AssignRepositoryToDepartmentAsync(id, request.RepositoryId, request.AssigneeUserId);
                return Results.Ok(new { success = true, message = "Assigned successfully" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminAssignRepositoryToDepartment")
        .WithSummary("Assign repository to department");

        // Remove repository from department
        deptGroup.MapDelete("/{id}/repositories/{repositoryId}", async (
            string id,
            string repositoryId,
            [FromServices] IAdminDepartmentService deptService) =>
        {
            var result = await deptService.RemoveRepositoryFromDepartmentAsync(id, repositoryId);
            if (!result)
                return Results.NotFound(new { success = false, message = "Repository is not assigned to this department" });
            return Results.Ok(new { success = true, message = "Removed successfully" });
        })
        .WithName("AdminRemoveRepositoryFromDepartment")
        .WithSummary("Remove repository from department");

        return group;
    }
}
