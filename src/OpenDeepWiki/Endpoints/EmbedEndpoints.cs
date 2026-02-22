using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Chat.Exceptions;
using OpenDeepWiki.Services.Chat;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// Embed script API endpoints
/// Provides embed mode configuration retrieval and SSE streaming chat
/// </summary>
public static class EmbedEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Register embed script endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapEmbedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/embed")
            .WithTags("Embed Scripts");

        // Get embed configuration
        group.MapGet("/config", GetEmbedConfigAsync)
            .WithName("GetEmbedConfig")
            .WithSummary("Get embed configuration (validate AppId and domain)");

        // Embed mode SSE streaming chat
        group.MapPost("/stream", StreamEmbedChatAsync)
            .WithName("StreamEmbedChat")
            .WithSummary("Embed mode SSE streaming chat");

        return app;
    }

    /// <summary>
    /// Get embed configuration
    /// Validate AppId and domain, return application configuration
    /// </summary>
    private static async Task<IResult> GetEmbedConfigAsync(
        HttpContext httpContext,
        [FromQuery] string appId,
        [FromServices] IEmbedService embedService,
        CancellationToken cancellationToken)
    {
        // Get the origin domain from request headers
        var origin = httpContext.Request.Headers.Origin.FirstOrDefault();
        var referer = httpContext.Request.Headers.Referer.FirstOrDefault();
        var domain = ExtractDomain(origin) ?? ExtractDomain(referer);

        var config = await embedService.GetAppConfigAsync(appId, domain, cancellationToken);

        if (!config.Valid)
        {
            return Results.Json(config, statusCode: config.ErrorCode == "DOMAIN_NOT_ALLOWED" ? 403 : 401);
        }

        return Results.Ok(config);
    }

    /// <summary>
    /// Embed mode SSE streaming chat
    /// Uses the application-configured model and API for conversation
    /// </summary>
    private static async Task StreamEmbedChatAsync(
        HttpContext httpContext,
        [FromBody] EmbedChatRequest request,
        [FromServices] IEmbedService embedService,
        CancellationToken cancellationToken)
    {
        // Set SSE response headers
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        // Add CORS headers to support cross-origin requests
        httpContext.Response.Headers.AccessControlAllowOrigin = "*";
        httpContext.Response.Headers.AccessControlAllowMethods = "POST, OPTIONS";
        httpContext.Response.Headers.AccessControlAllowHeaders = "Content-Type";

        // Get the origin domain from request headers
        var origin = httpContext.Request.Headers.Origin.FirstOrDefault();
        var referer = httpContext.Request.Headers.Referer.FirstOrDefault();
        var sourceDomain = ExtractDomain(origin) ?? ExtractDomain(referer);

        try
        {
            await foreach (var sseEvent in embedService.StreamEmbedChatAsync(request, sourceDomain, cancellationToken))
            {
                var eventData = FormatSSEEvent(sseEvent);
                await httpContext.Response.WriteAsync(eventData, cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            // Request timed out (not client cancellation)
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
            // Client disconnected, exit normally
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
    /// Extract domain from URL
    /// </summary>
    private static string? ExtractDomain(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }

    /// <summary>
    /// Format SSE event
    /// </summary>
    private static string FormatSSEEvent(SSEEvent sseEvent)
    {
        var sb = new StringBuilder();
        sb.Append("event: ");
        sb.AppendLine(sseEvent.Type);
        sb.Append("data: ");

        if (sseEvent.Data is string strData)
        {
            sb.AppendLine(strData);
        }
        else
        {
            sb.AppendLine(JsonSerializer.Serialize(sseEvent.Data, JsonOptions));
        }

        sb.AppendLine(); // SSE events require an empty line separator
        return sb.ToString();
    }
}
