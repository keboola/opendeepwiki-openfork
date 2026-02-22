using System.Text;
using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Chat.Config;
using OpenDeepWiki.Chat.Queue;
using OpenDeepWiki.Chat.Routing;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// Chat endpoint logger class (used for generic logger)
/// </summary>
public class ChatEndpointsLogger { }

/// <summary>
/// Chat system endpoints
/// Contains Webhook receiver endpoints and management endpoints
/// </summary>
public static class ChatEndpoints
{
    /// <summary>
    /// Register all Chat-related endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        // Webhook endpoint group
        var webhookGroup = app.MapGroup("/api/chat/webhook")
            .WithTags("Chat Webhook");
        
        webhookGroup.MapWebhookEndpoints();
        
        // Admin endpoint group
        var adminGroup = app.MapGroup("/api/chat/admin")
            .WithTags("Chat Management");
        
        adminGroup.MapChatAdminEndpoints();
        
        return app;
    }
    
    /// <summary>
    /// Register Webhook endpoints
    /// </summary>
    private static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        // Feishu Webhook
        app.MapPost("/feishu", HandleFeishuWebhookAsync)
            .WithName("FeishuWebhook")
            .WithSummary("Feishu message Webhook")
            .AllowAnonymous();
        
        // QQ bot Webhook
        app.MapPost("/qq", HandleQQWebhookAsync)
            .WithName("QQWebhook")
            .WithSummary("QQ bot message Webhook")
            .AllowAnonymous();
        
        // WeChat customer service Webhook (GET for verification, POST for receiving messages)
        app.MapGet("/wechat", HandleWeChatVerificationAsync)
            .WithName("WeChatVerification")
            .WithSummary("WeChat Webhook verification")
            .AllowAnonymous();
        
        app.MapPost("/wechat", HandleWeChatWebhookAsync)
            .WithName("WeChatWebhook")
            .WithSummary("WeChat customer service message Webhook")
            .AllowAnonymous();
        
        // Slack Webhook
        app.MapPost("/slack", HandleSlackWebhookAsync)
            .WithName("SlackWebhook")
            .WithSummary("Slack message Webhook")
            .AllowAnonymous();

        // Generic Webhook (routes based on platform parameter)
        app.MapPost("/{platform}", HandleGenericWebhookAsync)
            .WithName("GenericWebhook")
            .WithSummary("Generic message Webhook")
            .AllowAnonymous();
        
        return app;
    }
    
    /// <summary>
    /// Register admin endpoints
    /// </summary>
    private static IEndpointRouteBuilder MapChatAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Provider configuration management
        app.MapGet("/providers", GetAllProvidersAsync)
            .WithName("GetAllProviders")
            .WithSummary("Get all provider configurations");
        
        app.MapGet("/providers/{platform}", GetProviderConfigAsync)
            .WithName("GetProviderConfig")
            .WithSummary("Get specific provider configuration");
        
        app.MapPost("/providers", SaveProviderConfigAsync)
            .WithName("SaveProviderConfig")
            .WithSummary("Save provider configuration");
        
        app.MapDelete("/providers/{platform}", DeleteProviderConfigAsync)
            .WithName("DeleteProviderConfig")
            .WithSummary("Delete provider configuration");
        
        app.MapPost("/providers/{platform}/enable", EnableProviderAsync)
            .WithName("EnableProvider")
            .WithSummary("Enable provider");
        
        app.MapPost("/providers/{platform}/disable", DisableProviderAsync)
            .WithName("DisableProvider")
            .WithSummary("Disable provider");
        
        app.MapPost("/providers/{platform}/reload", ReloadProviderConfigAsync)
            .WithName("ReloadProviderConfig")
            .WithSummary("Reload provider configuration");
        
        // Queue status monitoring
        app.MapGet("/queue/status", GetQueueStatusAsync)
            .WithName("GetQueueStatus")
            .WithSummary("Get queue status");
        
        app.MapGet("/queue/deadletter", GetDeadLetterMessagesAsync)
            .WithName("GetDeadLetterMessages")
            .WithSummary("Get dead letter queue messages");
        
        app.MapPost("/queue/deadletter/{messageId}/reprocess", ReprocessDeadLetterAsync)
            .WithName("ReprocessDeadLetter")
            .WithSummary("Reprocess dead letter message");
        
        app.MapDelete("/queue/deadletter/{messageId}", DeleteDeadLetterAsync)
            .WithName("DeleteDeadLetter")
            .WithSummary("Delete dead letter message");
        
        app.MapDelete("/queue/deadletter", ClearDeadLetterQueueAsync)
            .WithName("ClearDeadLetterQueue")
            .WithSummary("Clear dead letter queue");
        
        return app;
    }

    
    #region Webhook handler methods
    
    /// <summary>
    /// Handle Feishu Webhook
    /// </summary>
    private static async Task<IResult> HandleFeishuWebhookAsync(
        HttpContext httpContext,
        [FromServices] IMessageRouter messageRouter,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        return await HandlePlatformWebhookAsync("feishu", httpContext, messageRouter, logger);
    }
    
    /// <summary>
    /// Handle QQ bot Webhook
    /// </summary>
    private static async Task<IResult> HandleQQWebhookAsync(
        HttpContext httpContext,
        [FromServices] IMessageRouter messageRouter,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        return await HandlePlatformWebhookAsync("qq", httpContext, messageRouter, logger);
    }
    
    /// <summary>
    /// Handle WeChat verification request (GET)
    /// </summary>
    private static async Task<IResult> HandleWeChatVerificationAsync(
        HttpContext httpContext,
        [FromServices] IMessageRouter messageRouter,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        var provider = messageRouter.GetProvider("wechat");
        if (provider == null)
        {
            logger.LogWarning("WeChat provider not registered");
            return Results.NotFound(new { error = "Provider not found" });
        }
        
        var validationResult = await provider.ValidateWebhookAsync(httpContext.Request);
        
        if (validationResult.IsValid && !string.IsNullOrEmpty(validationResult.Challenge))
        {
            // WeChat verification requires directly returning echostr
            return Results.Text(validationResult.Challenge);
        }
        
        return Results.BadRequest(new { error = validationResult.ErrorMessage ?? "Validation failed" });
    }
    
    /// <summary>
    /// Handle WeChat message Webhook (POST)
    /// </summary>
    private static async Task<IResult> HandleWeChatWebhookAsync(
        HttpContext httpContext,
        [FromServices] IMessageRouter messageRouter,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        return await HandlePlatformWebhookAsync("wechat", httpContext, messageRouter, logger);
    }
    
    /// <summary>
    /// Handle Slack Webhook
    /// </summary>
    private static async Task<IResult> HandleSlackWebhookAsync(
        HttpContext httpContext,
        [FromServices] IMessageRouter messageRouter,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        return await HandlePlatformWebhookAsync("slack", httpContext, messageRouter, logger);
    }

    /// <summary>
    /// Handle generic Webhook
    /// </summary>
    private static async Task<IResult> HandleGenericWebhookAsync(
        string platform,
        HttpContext httpContext,
        [FromServices] IMessageRouter messageRouter,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        return await HandlePlatformWebhookAsync(platform, httpContext, messageRouter, logger);
    }
    
    /// <summary>
    /// Unified platform Webhook handling logic
    /// </summary>
    private static async Task<IResult> HandlePlatformWebhookAsync(
        string platform,
        HttpContext httpContext,
        IMessageRouter messageRouter,
        ILogger logger)
    {
        try
        {
            var provider = messageRouter.GetProvider(platform);
            if (provider == null)
            {
                logger.LogWarning("Provider not registered for platform: {Platform}", platform);
                return Results.NotFound(new { error = $"Provider not found for platform: {platform}" });
            }
            
            if (!provider.IsEnabled)
            {
                logger.LogWarning("Provider {Platform} is disabled", platform);
                return Results.BadRequest(new { error = $"Provider {platform} is disabled" });
            }
            
            // Validate Webhook request
            var validationResult = await provider.ValidateWebhookAsync(httpContext.Request);
            
            // Handle verification request (e.g. Feishu url_verification)
            if (validationResult.IsValid && !string.IsNullOrEmpty(validationResult.Challenge))
            {
                logger.LogDebug("Webhook validation challenge for {Platform}", platform);
                return Results.Json(new { challenge = validationResult.Challenge });
            }
            
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Webhook validation failed for {Platform}: {Error}", 
                    platform, validationResult.ErrorMessage);
                return Results.BadRequest(new { error = validationResult.ErrorMessage ?? "Validation failed" });
            }
            
            // Read request body
            httpContext.Request.EnableBuffering();
            httpContext.Request.Body.Position = 0;
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            var rawMessage = await reader.ReadToEndAsync();
            
            // Parse message
            var message = await provider.ParseMessageAsync(rawMessage);
            
            if (message == null)
            {
                // Possibly a non-message event, return success
                logger.LogDebug("No message parsed from {Platform} webhook, possibly non-message event", platform);
                return Results.Ok(new { status = "ok" });
            }
            
            // Route message (async processing, immediately return acknowledgment)
            _ = Task.Run(async () =>
            {
                try
                {
                    await messageRouter.RouteIncomingAsync(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to route message {MessageId} from {Platform}", 
                        message.MessageId, platform);
                }
            });
            
            logger.LogDebug("Message {MessageId} from {Platform} accepted for processing", 
                message.MessageId, platform);
            
            return Results.Ok(new { status = "ok", messageId = message.MessageId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling webhook for platform: {Platform}", platform);
            return Results.Json(
                new { error = "Internal server error" }, 
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
    
    #endregion

    
    #region Admin endpoint handler methods
    
    /// <summary>
    /// Get all provider configurations
    /// </summary>
    private static async Task<IResult> GetAllProvidersAsync(
        [FromServices] IChatConfigService configService,
        [FromServices] IMessageRouter messageRouter)
    {
        var configs = await configService.GetAllConfigsAsync();
        var providers = messageRouter.GetAllProviders();
        
        var result = configs.Select(c => new ProviderStatusResponse
        {
            Platform = c.Platform,
            DisplayName = c.DisplayName,
            IsEnabled = c.IsEnabled,
            IsRegistered = providers.Any(p => p.PlatformId == c.Platform),
            WebhookUrl = c.WebhookUrl,
            MessageInterval = c.MessageInterval,
            MaxRetryCount = c.MaxRetryCount
        });
        
        return Results.Ok(result);
    }
    
    /// <summary>
    /// Get specific provider configuration
    /// </summary>
    private static async Task<IResult> GetProviderConfigAsync(
        string platform,
        [FromServices] IChatConfigService configService,
        [FromServices] IMessageRouter messageRouter)
    {
        var config = await configService.GetConfigAsync(platform);
        if (config == null)
        {
            return Results.NotFound(new { error = $"Provider config not found for platform: {platform}" });
        }
        
        var provider = messageRouter.GetProvider(platform);
        
        return Results.Ok(new ProviderStatusResponse
        {
            Platform = config.Platform,
            DisplayName = config.DisplayName,
            IsEnabled = config.IsEnabled,
            IsRegistered = provider != null,
            WebhookUrl = config.WebhookUrl,
            MessageInterval = config.MessageInterval,
            MaxRetryCount = config.MaxRetryCount,
            ConfigData = config.ConfigData // Note: sensitive information should be masked
        });
    }
    
    /// <summary>
    /// Save provider configuration
    /// </summary>
    private static async Task<IResult> SaveProviderConfigAsync(
        [FromBody] ProviderConfigDto config,
        [FromServices] IChatConfigService configService)
    {
        // Validate configuration
        var validationResult = configService.ValidateConfig(config);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(new 
            { 
                error = "Configuration validation failed",
                errors = validationResult.Errors,
                missingFields = validationResult.MissingFields
            });
        }
        
        await configService.SaveConfigAsync(config);
        
        return Results.Ok(new { status = "ok", platform = config.Platform });
    }
    
    /// <summary>
    /// Delete provider configuration
    /// </summary>
    private static async Task<IResult> DeleteProviderConfigAsync(
        string platform,
        [FromServices] IChatConfigService configService)
    {
        var config = await configService.GetConfigAsync(platform);
        if (config == null)
        {
            return Results.NotFound(new { error = $"Provider config not found for platform: {platform}" });
        }
        
        await configService.DeleteConfigAsync(platform);
        
        return Results.Ok(new { status = "ok", platform });
    }
    
    /// <summary>
    /// Enable provider
    /// </summary>
    private static async Task<IResult> EnableProviderAsync(
        string platform,
        [FromServices] IChatConfigService configService)
    {
        var config = await configService.GetConfigAsync(platform);
        if (config == null)
        {
            return Results.NotFound(new { error = $"Provider config not found for platform: {platform}" });
        }
        
        config.IsEnabled = true;
        await configService.SaveConfigAsync(config);
        
        // Trigger configuration reload
        await configService.ReloadConfigAsync(platform);

        return Results.Ok(new { status = "ok", platform, isEnabled = true });
    }
    
    /// <summary>
    /// Disable provider
    /// </summary>
    private static async Task<IResult> DisableProviderAsync(
        string platform,
        [FromServices] IChatConfigService configService)
    {
        var config = await configService.GetConfigAsync(platform);
        if (config == null)
        {
            return Results.NotFound(new { error = $"Provider config not found for platform: {platform}" });
        }
        
        config.IsEnabled = false;
        await configService.SaveConfigAsync(config);
        
        // Trigger configuration reload
        await configService.ReloadConfigAsync(platform);

        return Results.Ok(new { status = "ok", platform, isEnabled = false });
    }
    
    /// <summary>
    /// Reload provider configuration
    /// </summary>
    private static async Task<IResult> ReloadProviderConfigAsync(
        string platform,
        [FromServices] IChatConfigService configService)
    {
        var config = await configService.GetConfigAsync(platform);
        if (config == null)
        {
            return Results.NotFound(new { error = $"Provider config not found for platform: {platform}" });
        }
        
        await configService.ReloadConfigAsync(platform);
        
        return Results.Ok(new { status = "ok", platform, message = "Configuration reloaded" });
    }
    
    #endregion

    
    #region Queue monitoring endpoint handler methods
    
    /// <summary>
    /// Get queue status
    /// </summary>
    private static async Task<IResult> GetQueueStatusAsync(
        [FromServices] IMessageQueue messageQueue)
    {
        var queueLength = await messageQueue.GetQueueLengthAsync();
        var deadLetterCount = await messageQueue.GetDeadLetterCountAsync();
        
        return Results.Ok(new QueueStatusResponse
        {
            PendingCount = queueLength,
            DeadLetterCount = deadLetterCount,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
    
    /// <summary>
    /// Get dead letter queue messages
    /// </summary>
    private static async Task<IResult> GetDeadLetterMessagesAsync(
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromServices] IMessageQueue messageQueue)
    {
        if (take <= 0) take = 20;
        if (take > 100) take = 100;
        if (skip < 0) skip = 0;
        
        var messages = await messageQueue.GetDeadLetterMessagesAsync(skip, take);
        var total = await messageQueue.GetDeadLetterCountAsync();
        
        return Results.Ok(new DeadLetterListResponse
        {
            Messages = messages.Select(m => new DeadLetterMessageResponse
            {
                MessageId = m.Id,
                Platform = m.Message.Platform,
                TargetUserId = m.TargetUserId,
                Content = m.Message.Content,
                FailureReason = m.ErrorMessage ?? string.Empty,
                RetryCount = m.RetryCount,
                CreatedAt = m.CreatedAt,
                FailedAt = m.FailedAt
            }).ToList(),
            Total = total,
            Skip = skip,
            Take = take
        });
    }
    
    /// <summary>
    /// Reprocess dead letter message
    /// </summary>
    private static async Task<IResult> ReprocessDeadLetterAsync(
        string messageId,
        [FromServices] IMessageQueue messageQueue,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        var success = await messageQueue.ReprocessDeadLetterAsync(messageId);
        
        if (!success)
        {
            return Results.NotFound(new { error = $"Dead letter message not found: {messageId}" });
        }
        
        logger.LogInformation("Dead letter message {MessageId} requeued for processing", messageId);
        
        return Results.Ok(new { status = "ok", messageId, message = "Message requeued for processing" });
    }
    
    /// <summary>
    /// Delete dead letter message
    /// </summary>
    private static async Task<IResult> DeleteDeadLetterAsync(
        string messageId,
        [FromServices] IMessageQueue messageQueue,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        var success = await messageQueue.DeleteDeadLetterAsync(messageId);
        
        if (!success)
        {
            return Results.NotFound(new { error = $"Dead letter message not found: {messageId}" });
        }
        
        logger.LogInformation("Dead letter message {MessageId} deleted", messageId);
        
        return Results.Ok(new { status = "ok", messageId, message = "Message deleted" });
    }
    
    /// <summary>
    /// Clear dead letter queue
    /// </summary>
    private static async Task<IResult> ClearDeadLetterQueueAsync(
        [FromServices] IMessageQueue messageQueue,
        [FromServices] ILogger<ChatEndpointsLogger> logger)
    {
        var count = await messageQueue.ClearDeadLetterQueueAsync();
        
        logger.LogInformation("Dead letter queue cleared, {Count} messages deleted", count);
        
        return Results.Ok(new { status = "ok", deletedCount = count });
    }
    
    #endregion
}

#region Response models

/// <summary>
/// Provider status response
/// </summary>
public class ProviderStatusResponse
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsRegistered { get; set; }
    public string? WebhookUrl { get; set; }
    public int MessageInterval { get; set; }
    public int MaxRetryCount { get; set; }
    public string? ConfigData { get; set; }
}

/// <summary>
/// Queue status response
/// </summary>
public class QueueStatusResponse
{
    public int PendingCount { get; set; }
    public int DeadLetterCount { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Dead letter message list response
/// </summary>
public class DeadLetterListResponse
{
    public List<DeadLetterMessageResponse> Messages { get; set; } = new();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

/// <summary>
/// Dead letter message response
/// </summary>
public class DeadLetterMessageResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset FailedAt { get; set; }
}

#endregion
