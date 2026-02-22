using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Auth;
using OpenDeepWiki.Services.Auth;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// Authentication-related endpoints
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // User login
        group.MapPost("/login", async ([FromBody] LoginRequest request, [FromServices] IAuthService authService) =>
        {
            try
            {
                var response = await authService.LoginAsync(request);
                return Results.Ok(new { success = true, data = response });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("Login")
        .WithSummary("User login")
        .Produces<LoginResponse>(200)
        .Produces(401)
        .Produces(400);

        // User registration
        group.MapPost("/register", async ([FromBody] RegisterRequest request, [FromServices] IAuthService authService) =>
        {
            try
            {
                var response = await authService.RegisterAsync(request);
                return Results.Ok(new { success = true, data = response });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("Register")
        .WithSummary("User registration")
        .Produces<LoginResponse>(200)
        .Produces(400);

        // Get current user info
        group.MapGet("/me", async (HttpContext context, [FromServices] IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var userInfo = await authService.GetUserInfoAsync(userId);

            if (userInfo == null)
            {
                return Results.NotFound(new { success = false, message = "User not found" });
            }

            return Results.Ok(new { success = true, data = userInfo });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get current user info")
        .Produces<UserInfo>(200)
        .Produces(401)
        .Produces(404);

        return app;
    }
}
