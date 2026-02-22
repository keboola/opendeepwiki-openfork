using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Auth;
using OpenDeepWiki.Services.OAuth;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// OAuth authentication related endpoints
/// </summary>
public static class OAuthEndpoints
{
    public static IEndpointRouteBuilder MapOAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/oauth")
            .WithTags("OAuth Authentication")
            .WithOpenApi();

        // Get OAuth authorization URL
        group.MapGet("/{provider}/authorize", async (
            [FromRoute] string provider,
            [FromQuery] string? state,
            [FromServices] IOAuthService oauthService) =>
        {
            try
            {
                var authUrl = await oauthService.GetAuthorizationUrlAsync(provider, state);
                return Results.Ok(new { success = true, data = new { authorizationUrl = authUrl } });
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
        .WithName("GetOAuthAuthorizationUrl")
        .WithSummary("Get OAuth authorization URL")
        .WithDescription("Supported providers: github, gitee")
        .Produces<object>(200)
        .Produces(400);

        // OAuth callback handler
        group.MapGet("/{provider}/callback", async (
            [FromRoute] string provider,
            [FromQuery] string code,
            [FromQuery] string? state,
            [FromServices] IOAuthService oauthService) =>
        {
            try
            {
                var response = await oauthService.HandleCallbackAsync(provider, code, state);
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
        .WithName("OAuthCallback")
        .WithSummary("OAuth callback handler")
        .WithDescription("OAuth provider calls this endpoint to complete the authorization flow")
        .Produces<LoginResponse>(200)
        .Produces(400);

        // GitHub login shortcut
        group.MapGet("/github/login", async (
            [FromQuery] string? state,
            [FromServices] IOAuthService oauthService) =>
        {
            try
            {
                var authUrl = await oauthService.GetAuthorizationUrlAsync("github", state);
                return Results.Redirect(authUrl);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("GitHubLogin")
        .WithSummary("GitHub login")
        .ExcludeFromDescription();

        // Gitee login shortcut
        group.MapGet("/gitee/login", async (
            [FromQuery] string? state,
            [FromServices] IOAuthService oauthService) =>
        {
            try
            {
                var authUrl = await oauthService.GetAuthorizationUrlAsync("gitee", state);
                return Results.Redirect(authUrl);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("GiteeLogin")
        .WithSummary("Gitee login")
        .ExcludeFromDescription();

        return app;
    }
}
