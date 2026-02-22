using OpenDeepWiki.Models;

namespace OpenDeepWiki.Services.Organizations;

/// <summary>
/// Organization service interface
/// </summary>
public interface IOrganizationService
{
    Task<List<UserDepartmentInfo>> GetUserDepartmentsAsync(string userId);
    Task<List<DepartmentRepositoryInfo>> GetDepartmentRepositoriesAsync(string userId);
}

/// <summary>
/// User department information
/// </summary>
public class UserDepartmentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsManager { get; set; }
}

/// <summary>
/// Department repository information
/// </summary>
public class DepartmentRepositoryInfo
{
    public string RepositoryId { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string? GitUrl { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}
