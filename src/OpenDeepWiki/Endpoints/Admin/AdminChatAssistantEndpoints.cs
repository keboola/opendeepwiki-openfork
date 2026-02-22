using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin chat assistant configuration endpoints
/// </summary>
public static class AdminChatAssistantEndpoints
{
    public static RouteGroupBuilder MapAdminChatAssistantEndpoints(this RouteGroupBuilder group)
    {
        var chatAssistantGroup = group.MapGroup("/chat-assistant")
            .WithTags("Admin - Chat Assistant Config");

        // Get chat assistant configuration (including option lists)
        chatAssistantGroup.MapGet("/config", async (
            [FromServices] IAdminChatAssistantService chatAssistantService) =>
        {
            var result = await chatAssistantService.GetConfigWithOptionsAsync();
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("AdminGetChatAssistantConfig")
        .WithSummary("Get chat assistant configuration")
        .WithDescription("Get chat assistant configuration, including available models, MCP, and Skill lists");

        // Update chat assistant configuration
        chatAssistantGroup.MapPut("/config", async (
            [FromBody] UpdateChatAssistantConfigRequest request,
            [FromServices] IAdminChatAssistantService chatAssistantService) =>
        {
            var result = await chatAssistantService.UpdateConfigAsync(request);
            return Results.Ok(new { success = true, data = result, message = "Configuration updated successfully" });
        })
        .WithName("AdminUpdateChatAssistantConfig")
        .WithSummary("Update chat assistant configuration")
        .WithDescription("Update the chat assistant's enabled status, available models, MCP, and Skill configuration");

        return group;
    }
}
