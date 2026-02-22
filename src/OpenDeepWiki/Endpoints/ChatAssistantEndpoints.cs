using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Chat.Exceptions;
using OpenDeepWiki.Services.Auth;
using OpenDeepWiki.Services.Chat;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// Chat assistant endpoints
/// Provides SSE streaming API for document chat assistant
/// </summary>
public static class ChatAssistantEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Register chat assistant endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapChatAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/chat")
            .WithTags("Chat Assistant");

        // Get assistant configuration
        group.MapGet("/config", GetChatConfigAsync)
            .WithName("GetChatAssistantConfig")
            .WithSummary("Get chat assistant configuration");

        // Get available model list
        group.MapGet("/models", GetAvailableModelsAsync)
            .WithName("GetChatAssistantModels")
            .WithSummary("Get available model list");

        // SSE streaming chat
        group.MapPost("/stream", StreamChatAsync)
            .WithName("StreamChat")
            .WithSummary("SSE streaming chat");

        // Create share
        group.MapPost("/share", CreateChatShareAsync)
            .WithName("CreateChatShare")
            .WithSummary("Create chat share")
            .RequireAuthorization();

        // Get share details
        group.MapGet("/share/{shareId}", GetChatShareAsync)
            .WithName("GetChatShare")
            .WithSummary("Get chat share details")
            .AllowAnonymous();

        // Revoke share
        group.MapDelete("/share/{shareId}", RevokeChatShareAsync)
            .WithName("RevokeChatShare")
            .WithSummary("Revoke chat share")
            .RequireAuthorization();

        return app;
    }

    /// <summary>
    /// Get chat assistant configuration
    /// </summary>
    private static async Task<IResult> GetChatConfigAsync(
        [FromServices] IChatAssistantService chatAssistantService,
        CancellationToken cancellationToken)
    {
        var config = await chatAssistantService.GetConfigAsync(cancellationToken);
        return Results.Ok(config);
    }

    /// <summary>
    /// Get available model list
    /// </summary>
    private static async Task<IResult> GetAvailableModelsAsync(
        [FromServices] IChatAssistantService chatAssistantService,
        CancellationToken cancellationToken)
    {
        var models = await chatAssistantService.GetAvailableModelsAsync(cancellationToken);
        return Results.Ok(models);
    }

    /// <summary>
    /// Create chat share
    /// </summary>
    private static async Task<IResult> CreateChatShareAsync(
        [FromBody] CreateChatShareRequest request,
        [FromServices] IChatShareService chatShareService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || string.IsNullOrWhiteSpace(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        request.CreatedBy = userContext.UserId;
        var share = await chatShareService.CreateShareAsync(request, cancellationToken);
        return Results.Ok(share);
    }

    /// <summary>
    /// Get chat share
    /// </summary>
    private static async Task<IResult> GetChatShareAsync(
        string shareId,
        [FromServices] IChatShareService chatShareService,
        CancellationToken cancellationToken)
    {
        var share = await chatShareService.GetShareAsync(shareId, cancellationToken);
        if (share == null)
        {
            return Results.NotFound(new { message = "Share not found or has expired" });
        }

        return Results.Ok(share);
    }

    /// <summary>
    /// Revoke chat share
    /// </summary>
    private static async Task<IResult> RevokeChatShareAsync(
        string shareId,
        [FromServices] IChatShareService chatShareService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || string.IsNullOrWhiteSpace(userContext.UserId))
        {
            return Results.Unauthorized();
        }

        var success = await chatShareService.RevokeShareAsync(shareId, userContext.UserId, cancellationToken);
        if (!success)
        {
            return Results.NotFound(new { message = "Share not found or you do not have permission to revoke it" });
        }

        return Results.NoContent();
    }


    /// <summary>
    /// SSE streaming chat
    /// </summary>
    private static async Task StreamChatAsync(
        HttpContext httpContext,
        [FromBody] ChatRequest request,
        [FromServices] IChatAssistantService chatAssistantService,
        CancellationToken cancellationToken)
    {
        // Set SSE response headers
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var sseEvent in chatAssistantService.StreamChatAsync(request, cancellationToken))
            {
                var eventData = FormatSSEEvent(sseEvent);
                await httpContext.Response.WriteAsync(eventData, cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            // Request timeout (not client cancellation)
            var errorEvent = new SSEEvent
            {
                Type = SSEEventType.Error,
                Data = SSEErrorResponse.CreateRetryable(
                    ChatErrorCodes.REQUEST_TIMEOUT,
                    "Request timed out, please retry",
                    2000)
            };
            var eventData = FormatSSEEvent(errorEvent);
            await httpContext.Response.WriteAsync(eventData, cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, normal exit
        }
        catch (HttpRequestException ex)
        {
            // Connection failed
            var errorEvent = new SSEEvent
            {
                Type = SSEEventType.Error,
                Data = SSEErrorResponse.CreateRetryable(
                    ChatErrorCodes.CONNECTION_FAILED,
                    $"Connection failed: {ex.Message}",
                    1000)
            };
            var eventData = FormatSSEEvent(errorEvent);
            await httpContext.Response.WriteAsync(eventData, cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Send error event
            var errorEvent = new SSEEvent
            {
                Type = SSEEventType.Error,
                Data = SSEErrorResponse.CreateRetryable(
                    ChatErrorCodes.INTERNAL_ERROR,
                    ex.Message,
                    3000)
            };
            var eventData = FormatSSEEvent(errorEvent);
            await httpContext.Response.WriteAsync(eventData, cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Format SSE event
    /// </summary>
    private static string FormatSSEEvent(SSEEvent sseEvent)
    {
        var sb = new StringBuilder();
        sb.Append("data: ");
        
        // Use unified JSON format containing type and data fields
        var eventPayload = new
        {
            type = sseEvent.Type,
            data = sseEvent.Data
        };
        sb.AppendLine(JsonSerializer.Serialize(eventPayload, JsonOptions));
        
        sb.AppendLine(); // SSE events require blank line separator
        return sb.ToString();
    }
}
