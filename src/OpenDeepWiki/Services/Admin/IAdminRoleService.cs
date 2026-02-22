using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin role service interface
/// </summary>
public interface IAdminRoleService
{
    Task<List<AdminRoleDto>> GetRolesAsync();
    Task<AdminRoleDto?> GetRoleByIdAsync(string id);
    Task<AdminRoleDto> CreateRoleAsync(CreateRoleRequest request);
    Task<bool> UpdateRoleAsync(string id, UpdateRoleRequest request);
    Task<bool> DeleteRoleAsync(string id);
}
