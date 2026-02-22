namespace OpenDeepWiki.Models.Admin;

/// <summary>
/// Admin user list response
/// </summary>
public class AdminUserListResponse
{
    public List<AdminUserDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Admin user DTO
/// </summary>
public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create user request
/// </summary>
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<string>? RoleIds { get; set; }
}

/// <summary>
/// Update user request
/// </summary>
public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
}

/// <summary>
/// Update user roles request
/// </summary>
public class UpdateUserRolesRequest
{
    public List<string> RoleIds { get; set; } = new();
}

/// <summary>
/// Reset password request
/// </summary>
public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
