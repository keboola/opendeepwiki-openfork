namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin endpoint registration
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/admin")
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin");

        // Register endpoints for each admin module
        adminGroup.MapAdminStatisticsEndpoints();
        adminGroup.MapAdminRepositoryEndpoints();
        adminGroup.MapAdminUserEndpoints();
        adminGroup.MapAdminRoleEndpoints();
        adminGroup.MapAdminDepartmentEndpoints();
        adminGroup.MapAdminToolsEndpoints();
        adminGroup.MapAdminSettingsEndpoints();
        adminGroup.MapAdminChatAssistantEndpoints();
        adminGroup.MapAdminGitHubImportEndpoints();

        return app;
    }
}
