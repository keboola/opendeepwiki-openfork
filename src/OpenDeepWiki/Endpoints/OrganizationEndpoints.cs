using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Services.Organizations;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// Organization endpoints
/// </summary>
public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .RequireAuthorization()
            .WithTags("Organization");

        // Get current user's department list
        group.MapGet("/my-departments", async (
            ClaimsPrincipal user,
            [FromServices] IOrganizationService orgService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await orgService.GetUserDepartmentsAsync(userId);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("GetMyDepartments")
        .WithSummary("Get current user's department list");

        // Get repository list for current user's departments
        group.MapGet("/my-repositories", async (
            ClaimsPrincipal user,
            [FromServices] IOrganizationService orgService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await orgService.GetDepartmentRepositoriesAsync(userId);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("GetMyDepartmentRepositories")
        .WithSummary("Get repository list for current user's departments");

        return app;
    }
}
