using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Organizations;

namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// Repository access control service.
/// Reuses the same logic as McpUserResolver.CanAccessRepositoryAsync,
/// adapted to use IUserContext for the web request context.
/// </summary>
public class RepositoryAccessService : IRepositoryAccessService
{
    private readonly IUserContext _userContext;
    private readonly IOrganizationService _orgService;

    public RepositoryAccessService(IUserContext userContext, IOrganizationService orgService)
    {
        _userContext = userContext;
        _orgService = orgService;
    }

    public async Task<bool> CanAccessRepositoryAsync(Repository repository)
    {
        // Public repos are always accessible
        if (repository.IsPublic)
            return true;

        // Private repos require authentication
        if (!_userContext.IsAuthenticated || string.IsNullOrEmpty(_userContext.UserId))
            return false;

        // Check if user owns the repository
        if (repository.OwnerUserId == _userContext.UserId)
            return true;

        // Check department assignment (same logic as McpUserResolver)
        var deptRepos = await _orgService.GetDepartmentRepositoriesAsync(_userContext.UserId);
        return deptRepos.Any(r => r.OrgName == repository.OrgName && r.RepoName == repository.RepoName);
    }
}
