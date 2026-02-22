namespace OpenDeepWiki.Models.Admin;

/// <summary>
/// Admin repository list response
/// </summary>
public class AdminRepositoryListResponse
{
    public List<AdminRepositoryDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Admin repository DTO
/// </summary>
public class AdminRepositoryDto
{
    public string Id { get; set; } = string.Empty;
    public string GitUrl { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
    public int BookmarkCount { get; set; }
    public int ViewCount { get; set; }
    public string? OwnerUserId { get; set; }
    public string? OwnerUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Update repository request
/// </summary>
public class UpdateRepositoryRequest
{
    public bool? IsPublic { get; set; }
    public string? AuthAccount { get; set; }
    public string? AuthPassword { get; set; }
}

/// <summary>
/// Update status request
/// </summary>
public class UpdateStatusRequest
{
    public int Status { get; set; }
}

/// <summary>
/// Sync statistics result
/// </summary>
public class SyncStatsResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
}

/// <summary>
/// Batch sync statistics result
/// </summary>
public class BatchSyncStatsResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<BatchSyncItemResult> Results { get; set; } = new();
}

/// <summary>
/// Batch sync individual item result
/// </summary>
public class BatchSyncItemResult
{
    public string Id { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
}

/// <summary>
/// Batch delete result
/// </summary>
public class BatchDeleteResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailedIds { get; set; } = new();
}

/// <summary>
/// Admin repository deep management information
/// </summary>
public class AdminRepositoryManagementDto
{
    public string RepositoryId { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public List<AdminRepositoryBranchDto> Branches { get; set; } = new();
    public List<AdminIncrementalTaskDto> RecentIncrementalTasks { get; set; } = new();
}

/// <summary>
/// Admin branch information
/// </summary>
public class AdminRepositoryBranchDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LastCommitId { get; set; }
    public DateTime? LastProcessedAt { get; set; }
    public List<AdminBranchLanguageDto> Languages { get; set; } = new();
}

/// <summary>
/// Admin branch language information
/// </summary>
public class AdminBranchLanguageDto
{
    public string Id { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int CatalogCount { get; set; }
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Admin incremental update task information
/// </summary>
public class AdminIncrementalTaskDto
{
    public string TaskId { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsManualTrigger { get; set; }
    public int RetryCount { get; set; }
    public string? PreviousCommitId { get; set; }
    public string? TargetCommitId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Admin document regeneration request
/// </summary>
public class RegenerateRepositoryDocumentRequest
{
    public string BranchId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string DocumentPath { get; set; } = string.Empty;
}

/// <summary>
/// Admin document content update request
/// </summary>
public class UpdateRepositoryDocumentContentRequest
{
    public string BranchId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string DocumentPath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Admin operation unified response
/// </summary>
public class AdminRepositoryOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Batch operation request
/// </summary>
public class BatchOperationRequest
{
    public string[] Ids { get; set; } = Array.Empty<string>();
}
