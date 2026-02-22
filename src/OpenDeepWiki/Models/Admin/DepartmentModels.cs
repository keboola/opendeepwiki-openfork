namespace OpenDeepWiki.Models.Admin;

/// <summary>
/// Admin department DTO
/// </summary>
public class AdminDepartmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AdminDepartmentDto> Children { get; set; } = new();
}

/// <summary>
/// Create department request
/// </summary>
public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update department request
/// </summary>
public class UpdateDepartmentRequest
{
    public string? Name { get; set; }
    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}


/// <summary>
/// Department user DTO
/// </summary>
public class DepartmentUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public bool IsManager { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Department repository DTO
/// </summary>
public class DepartmentRepositoryDto
{
    public string Id { get; set; } = string.Empty;
    public string RepositoryId { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string? GitUrl { get; set; }
    public int Status { get; set; }
    public string? AssigneeUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Add user to department request
/// </summary>
public class AddUserToDepartmentRequest
{
    public string UserId { get; set; } = string.Empty;
    public bool IsManager { get; set; } = false;
}

/// <summary>
/// Assign repository to department request
/// </summary>
public class AssignRepositoryRequest
{
    public string RepositoryId { get; set; } = string.Empty;
    public string AssigneeUserId { get; set; } = string.Empty;
}
