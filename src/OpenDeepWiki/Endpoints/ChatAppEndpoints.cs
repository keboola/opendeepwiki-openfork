using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Services.Auth;
using OpenDeepWiki.Services.Chat;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// User app management endpoints
/// Provides app CRUD, statistics queries, chat logs, and other APIs
/// </summary>
public static class ChatAppEndpoints
{
    /// <summary>
    /// Register user app management endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapChatAppEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/apps")
            .WithTags("User Apps")
            .RequireAuthorization();

        // Get user app list
        group.MapGet("/", GetUserAppsAsync)
            .WithName("GetUserApps")
            .WithSummary("Get current user's app list");

        // Create app
        group.MapPost("/", CreateAppAsync)
            .WithName("CreateApp")
            .WithSummary("Create new app");

        // Get app details
        group.MapGet("/{id:guid}", GetAppByIdAsync)
            .WithName("GetAppById")
            .WithSummary("Get app details");

        // Update app
        group.MapPut("/{id:guid}", UpdateAppAsync)
            .WithName("UpdateApp")
            .WithSummary("Update app configuration");

        // Delete app
        group.MapDelete("/{id:guid}", DeleteAppAsync)
            .WithName("DeleteApp")
            .WithSummary("Delete app");

        // Regenerate secret
        group.MapPost("/{id:guid}/regenerate-secret", RegenerateSecretAsync)
            .WithName("RegenerateAppSecret")
            .WithSummary("Regenerate app secret");

        // Get app statistics
        group.MapGet("/{id:guid}/statistics", GetAppStatisticsAsync)
            .WithName("GetAppStatistics")
            .WithSummary("Get app usage statistics");

        // Get query logs
        group.MapGet("/{id:guid}/logs", GetAppLogsAsync)
            .WithName("GetAppLogs")
            .WithSummary("Get app query logs");

        return app;
    }


    /// <summary>
    /// Get current user's app list
    /// </summary>
    private static async Task<IResult> GetUserAppsAsync(
        [FromServices] IChatAppService chatAppService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        var apps = await chatAppService.GetUserAppsAsync(userContext.UserId, cancellationToken);
        return Results.Ok(apps);
    }

    /// <summary>
    /// Create new app
    /// </summary>
    private static async Task<IResult> CreateAppAsync(
        [FromBody] CreateChatAppDto dto,
        [FromServices] IChatAppService chatAppService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Results.BadRequest(new { message = "App name cannot be empty" });
        }

        var app = await chatAppService.CreateAppAsync(userContext.UserId, dto, cancellationToken);
        return Results.Created($"/api/v1/apps/{app.Id}", app);
    }

    /// <summary>
    /// Get app details
    /// </summary>
    private static async Task<IResult> GetAppByIdAsync(
        Guid id,
        [FromServices] IChatAppService chatAppService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        var app = await chatAppService.GetAppByIdAsync(id, userContext.UserId, cancellationToken);
        if (app == null)
        {
            return Results.NotFound(new { message = "App not found" });
        }

        return Results.Ok(app);
    }


    /// <summary>
    /// Update app configuration
    /// </summary>
    private static async Task<IResult> UpdateAppAsync(
        Guid id,
        [FromBody] UpdateChatAppDto dto,
        [FromServices] IChatAppService chatAppService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        var app = await chatAppService.UpdateAppAsync(id, userContext.UserId, dto, cancellationToken);
        if (app == null)
        {
            return Results.NotFound(new { message = "App not found" });
        }

        return Results.Ok(app);
    }

    /// <summary>
    /// Delete app
    /// </summary>
    private static async Task<IResult> DeleteAppAsync(
        Guid id,
        [FromServices] IChatAppService chatAppService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        var success = await chatAppService.DeleteAppAsync(id, userContext.UserId, cancellationToken);
        if (!success)
        {
            return Results.NotFound(new { message = "App not found" });
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Regenerate app secret
    /// </summary>
    private static async Task<IResult> RegenerateSecretAsync(
        Guid id,
        [FromServices] IChatAppService chatAppService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        var newSecret = await chatAppService.RegenerateSecretAsync(id, userContext.UserId, cancellationToken);
        if (newSecret == null)
        {
            return Results.NotFound(new { message = "App not found" });
        }

        return Results.Ok(new { appSecret = newSecret });
    }


    /// <summary>
    /// Get app usage statistics
    /// </summary>
    private static async Task<IResult> GetAppStatisticsAsync(
        Guid id,
        [FromServices] IChatAppService chatAppService,
        [FromServices] IAppStatisticsService statisticsService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        // Verify the app belongs to the user
        var app = await chatAppService.GetAppByIdAsync(id, userContext.UserId, cancellationToken);
        if (app == null)
        {
            return Results.NotFound(new { message = "App not found" });
        }

        // Default to last 30 days if not specified
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var statistics = await statisticsService.GetStatisticsAsync(app.AppId, start, end, cancellationToken);
        return Results.Ok(statistics);
    }

    /// <summary>
    /// Get app query logs
    /// </summary>
    private static async Task<IResult> GetAppLogsAsync(
        Guid id,
        [FromServices] IChatAppService chatAppService,
        [FromServices] IChatLogService chatLogService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrEmpty(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        // Verify the app belongs to the user
        var app = await chatAppService.GetAppByIdAsync(id, userContext.UserId, cancellationToken);
        if (app == null)
        {
            return Results.NotFound(new { message = "App not found" });
        }

        var query = new ChatLogQueryDto
        {
            AppId = app.AppId,
            StartDate = startDate,
            EndDate = endDate,
            Keyword = keyword,
            Page = page,
            PageSize = Math.Min(pageSize, 100) // Limit max page size
        };

        var logs = await chatLogService.GetLogsAsync(query, cancellationToken);
        return Results.Ok(logs);
    }
}
