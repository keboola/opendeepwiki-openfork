using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin statistics service interface
/// </summary>
public interface IAdminStatisticsService
{
    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    Task<DashboardStatisticsResponse> GetDashboardStatisticsAsync(int days);

    /// <summary>
    /// Get token consumption statistics
    /// </summary>
    Task<TokenUsageStatisticsResponse> GetTokenUsageStatisticsAsync(int days);
}
