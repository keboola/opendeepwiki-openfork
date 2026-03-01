using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Organizations;

/// <summary>
/// Organization service implementation
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly IContext _context;

    public OrganizationService(IContext context)
    {
        _context = context;
    }

    public async Task<List<UserDepartmentInfo>> GetUserDepartmentsAsync(string userId)
    {
        var userDepts = await _context.UserDepartments
            .Where(ud => ud.UserId == userId && !ud.IsDeleted)
            .ToListAsync();

        var deptIds = userDepts.Select(ud => ud.DepartmentId).ToList();
        var depts = await _context.Departments
            .Where(d => deptIds.Contains(d.Id) && d.IsActive)
            .ToDictionaryAsync(d => d.Id);

        return userDepts
            .Where(ud => depts.ContainsKey(ud.DepartmentId))
            .Select(ud => new UserDepartmentInfo
            {
                Id = ud.DepartmentId,
                Name = depts[ud.DepartmentId].Name,
                Description = depts[ud.DepartmentId].Description,
                IsManager = ud.IsManager
            }).ToList();
    }

    public async Task<List<DepartmentRepositoryInfo>> GetDepartmentRepositoriesAsync(string userId)
    {
        // Get departments the user belongs to
        var userDeptIds = await _context.UserDepartments
            .Where(ud => ud.UserId == userId && !ud.IsDeleted)
            .Select(ud => ud.DepartmentId)
            .ToListAsync();

        if (userDeptIds.Count == 0)
            return new List<DepartmentRepositoryInfo>();

        // Get department information
        var depts = await _context.Departments
            .Where(d => userDeptIds.Contains(d.Id) && d.IsActive)
            .ToDictionaryAsync(d => d.Id);

        // Get repositories assigned to these departments
        var assignments = await _context.RepositoryAssignments
            .Where(ra => userDeptIds.Contains(ra.DepartmentId) && !ra.IsDeleted)
            .ToListAsync();

        var repoIds = assignments.Select(a => a.RepositoryId).Distinct().ToList();
        var repos = await _context.Repositories
            .Where(r => repoIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        return assignments
            .Where(a => repos.ContainsKey(a.RepositoryId) && depts.ContainsKey(a.DepartmentId))
            .Select(a => new DepartmentRepositoryInfo
            {
                RepositoryId = a.RepositoryId,
                RepoName = repos[a.RepositoryId].RepoName,
                OrgName = repos[a.RepositoryId].OrgName,
                GitUrl = repos[a.RepositoryId].GitUrl,
                Status = (int)repos[a.RepositoryId].Status,
                StatusName = GetStatusName((int)repos[a.RepositoryId].Status),
                DepartmentId = a.DepartmentId,
                DepartmentName = depts[a.DepartmentId].Name,
                CreatedAt = repos[a.RepositoryId].CreatedAt
            })
            .DistinctBy(r => r.RepositoryId)
            .ToList();
    }

    public async Task<bool> ShareRepositoryWithMyDepartmentsAsync(string userId, string repositoryId)
    {
        // 1. Verify user owns the repository
        var repo = await _context.Repositories.FirstOrDefaultAsync(r => r.Id == repositoryId && !r.IsDeleted);
        if (repo == null || repo.OwnerUserId != userId) return false;

        // 2. Get user's departments
        var userDeptIds = await _context.UserDepartments
            .Where(ud => ud.UserId == userId && !ud.IsDeleted)
            .Select(ud => ud.DepartmentId)
            .ToListAsync();

        if (userDeptIds.Count == 0) return false;

        // 3. Get existing assignments to avoid duplicates
        var existingAssignments = await _context.RepositoryAssignments
            .Where(ra => ra.RepositoryId == repositoryId && userDeptIds.Contains(ra.DepartmentId) && !ra.IsDeleted)
            .Select(ra => ra.DepartmentId)
            .ToListAsync();

        // 4. Create assignments for departments that don't already have one
        foreach (var deptId in userDeptIds)
        {
            if (!existingAssignments.Contains(deptId))
            {
                _context.RepositoryAssignments.Add(new RepositoryAssignment
                {
                    Id = Guid.NewGuid().ToString("N"),
                    RepositoryId = repositoryId,
                    DepartmentId = deptId,
                    AssigneeUserId = userId
                });
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnshareRepositoryFromMyDepartmentsAsync(string userId, string repositoryId)
    {
        // 1. Verify user owns the repository
        var repo = await _context.Repositories.FirstOrDefaultAsync(r => r.Id == repositoryId && !r.IsDeleted);
        if (repo == null || repo.OwnerUserId != userId) return false;

        // 2. Get user's departments
        var userDeptIds = await _context.UserDepartments
            .Where(ud => ud.UserId == userId && !ud.IsDeleted)
            .Select(ud => ud.DepartmentId)
            .ToListAsync();

        // 3. Soft-delete assignments for user's departments
        var assignments = await _context.RepositoryAssignments
            .Where(ra => ra.RepositoryId == repositoryId && userDeptIds.Contains(ra.DepartmentId) && !ra.IsDeleted)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            assignment.IsDeleted = true;
            assignment.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private static string GetStatusName(int status) => status switch
    {
        0 => "Pending",
        1 => "Processing",
        2 => "Completed",
        3 => "Failed",
        _ => "Unknown"
    };
}
