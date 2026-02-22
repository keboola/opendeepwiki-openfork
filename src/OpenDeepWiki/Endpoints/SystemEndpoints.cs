using System.Reflection;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// System information related endpoints
/// </summary>
public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/system")
            .WithTags("System");

        group.MapGet("/version", GetVersion)
            .WithSummary("Get system version info")
            .WithDescription("Returns the current system version number and build information");
    }

    /// <summary>
    /// Get system version information
    /// </summary>
    private static IResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

        return Results.Ok(new
        {
            success = true,
            data = new
            {
                version = informationalVersion,
                assemblyVersion = version,
                productName = "OpenDeepWiki"
            }
        });
    }
}
