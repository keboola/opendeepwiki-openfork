using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin role service implementation
/// </summary>
public class AdminRoleService : IAdminRoleService
{
    private readonly IContext _context;

    public AdminRoleService(IContext context)
    {
        _context = context;
    }

    public async Task<List<AdminRoleDto>> GetRolesAsync()
    {
        var roles = await _context.Roles
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync();

        var roleIds = roles.Select(r => r.Id).ToList();
        var userCounts = await _context.UserRoles
            .Where(ur => roleIds.Contains(ur.RoleId) && !ur.IsDeleted)
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync();

        return roles.Select(r => new AdminRoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsActive = r.IsActive,
            IsSystemRole = r.IsSystemRole,
            UserCount = userCounts.FirstOrDefault(c => c.RoleId == r.Id)?.Count ?? 0,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<AdminRoleDto?> GetRoleByIdAsync(string id)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (role == null) return null;

        var userCount = await _context.UserRoles
            .CountAsync(ur => ur.RoleId == id && !ur.IsDeleted);

        return new AdminRoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = role.IsSystemRole,
            UserCount = userCount,
            CreatedAt = role.CreatedAt
        };
    }

    public async Task<AdminRoleDto> CreateRoleAsync(CreateRoleRequest request)
    {
        var exists = await _context.Roles.AnyAsync(r => r.Name == request.Name && !r.IsDeleted);
        if (exists) throw new InvalidOperationException("Role name already exists");

        var role = new Role
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return (await GetRoleByIdAsync(role.Id))!;
    }

    public async Task<bool> UpdateRoleAsync(string id, UpdateRoleRequest request)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (role == null) return false;

        if (role.IsSystemRole)
            throw new InvalidOperationException("System role cannot be modified");

        if (request.Name != null)
        {
            var exists = await _context.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id && !r.IsDeleted);
            if (exists) throw new InvalidOperationException("Role name already exists");
            role.Name = request.Name;
        }

        if (request.Description != null) role.Description = request.Description;
        if (request.IsActive.HasValue) role.IsActive = request.IsActive.Value;

        role.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoleAsync(string id)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (role == null) return false;

        if (role.IsSystemRole)
            throw new InvalidOperationException("System role cannot be deleted");

        var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id && !ur.IsDeleted);
        if (hasUsers)
            throw new InvalidOperationException("Role has assigned users, cannot delete");

        role.IsDeleted = true;
        role.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
