using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin user management endpoints
/// </summary>
public static class AdminUserEndpoints
{
    public static RouteGroupBuilder MapAdminUserEndpoints(this RouteGroupBuilder group)
    {
        var userGroup = group.MapGroup("/users")
            .WithTags("Admin - User Management");

        // Get user list
        userGroup.MapGet("/", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? search,
            [FromQuery] string? roleId,
            [FromServices] IAdminUserService userService) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            var result = await userService.GetUsersAsync(page, pageSize, search, roleId);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetUsers")
        .WithSummary("Get user list");

        // Get user details
        userGroup.MapGet("/{id}", async (
            string id,
            [FromServices] IAdminUserService userService) =>
        {
            var result = await userService.GetUserByIdAsync(id);
            if (result == null)
                return Results.NotFound(new { success = false, message = "User not found" });
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetUser")
        .WithSummary("Get user details");

        // Create user
        userGroup.MapPost("/", async (
            [FromBody] CreateUserRequest request,
            [FromServices] IAdminUserService userService) =>
        {
            try
            {
                var result = await userService.CreateUserAsync(request);
                return Results.Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("AdminCreateUser")
        .WithSummary("Create user");

        // Update user
        userGroup.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateUserRequest request,
            [FromServices] IAdminUserService userService) =>
        {
            var result = await userService.UpdateUserAsync(id, request);
            if (!result)
                return Results.NotFound(new { success = false, message = "User not found" });
            return Results.Ok(new { success = true, message = "Updated successfully" });
        })
        .WithName("AdminUpdateUser")
        .WithSummary("Update user");

        // Delete user
        userGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IAdminUserService userService) =>
        {
            var result = await userService.DeleteUserAsync(id);
            if (!result)
                return Results.NotFound(new { success = false, message = "User not found" });
            return Results.Ok(new { success = true, message = "Deleted successfully" });
        })
        .WithName("AdminDeleteUser")
        .WithSummary("Delete user");

        // Update user status
        userGroup.MapPut("/{id}/status", async (
            string id,
            [FromBody] UpdateStatusRequest request,
            [FromServices] IAdminUserService userService) =>
        {
            var result = await userService.UpdateUserStatusAsync(id, request.Status);
            if (!result)
                return Results.NotFound(new { success = false, message = "User not found" });
            return Results.Ok(new { success = true, message = "Status updated successfully" });
        })
        .WithName("AdminUpdateUserStatus")
        .WithSummary("Update user status");

        // Update user roles
        userGroup.MapPut("/{id}/roles", async (
            string id,
            [FromBody] UpdateUserRolesRequest request,
            [FromServices] IAdminUserService userService) =>
        {
            var result = await userService.UpdateUserRolesAsync(id, request.RoleIds);
            if (!result)
                return Results.NotFound(new { success = false, message = "User not found" });
            return Results.Ok(new { success = true, message = "Roles updated successfully" });
        })
        .WithName("AdminUpdateUserRoles")
        .WithSummary("Update user roles");

        // Reset password
        userGroup.MapPost("/{id}/reset-password", async (
            string id,
            [FromBody] ResetPasswordRequest request,
            [FromServices] IAdminUserService userService) =>
        {
            var result = await userService.ResetPasswordAsync(id, request.NewPassword);
            if (!result)
                return Results.NotFound(new { success = false, message = "User not found" });
            return Results.Ok(new { success = true, message = "Password reset successfully" });
        })
        .WithName("AdminResetPassword")
        .WithSummary("Reset user password");

        return group;
    }
}
