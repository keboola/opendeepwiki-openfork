using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin role management endpoints
/// </summary>
public static class AdminRoleEndpoints
{
    public static RouteGroupBuilder MapAdminRoleEndpoints(this RouteGroupBuilder group)
    {
        var roleGroup = group.MapGroup("/roles")
            .WithTags("Admin - Role Management");

        // Get role list
        roleGroup.MapGet("/", async ([FromServices] IAdminRoleService roleService) =>
        {
            var result = await roleService.GetRolesAsync();
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetRoles")
        .WithSummary("Get role list");

        // Get role details
        roleGroup.MapGet("/{id}", async (
            string id,
            [FromServices] IAdminRoleService roleService) =>
        {
            var result = await roleService.GetRoleByIdAsync(id);
            if (result == null)
                return Results.NotFound(new { success = false, message = "Role not found" });
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetRole")
        .WithSummary("Get role details");

        // Create role
        roleGroup.MapPost("/", async (
            [FromBody] CreateRoleRequest request,
            [FromServices] IAdminRoleService roleService) =>
        {
            try
            {
                var result = await roleService.CreateRoleAsync(request);
                return Results.Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminCreateRole")
        .WithSummary("Create role");

        // Update role
        roleGroup.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateRoleRequest request,
            [FromServices] IAdminRoleService roleService) =>
        {
            try
            {
                var result = await roleService.UpdateRoleAsync(id, request);
                if (!result)
                    return Results.NotFound(new { success = false, message = "Role not found" });
                return Results.Ok(new { success = true, message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminUpdateRole")
        .WithSummary("Update role");

        // Delete role
        roleGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IAdminRoleService roleService) =>
        {
            try
            {
                var result = await roleService.DeleteRoleAsync(id);
                if (!result)
                    return Results.NotFound(new { success = false, message = "Role not found" });
                return Results.Ok(new { success = true, message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminDeleteRole")
        .WithSummary("Delete role");

        return group;
    }
}
