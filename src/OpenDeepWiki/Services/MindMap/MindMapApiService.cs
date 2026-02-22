using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.MindMap;

/// <summary>
/// Mind map API service
/// </summary>
[MiniApi(Route = "/api/v1/repos")]
[Tags("Mind Map")]
public class MindMapApiService(IContext context)
{
    /// <summary>
    /// Get repository project architecture mind map
    /// </summary>
    [HttpGet("/{owner}/{repo}/mindmap")]
    public async Task<IResult> GetMindMapAsync(
        string owner,
        string repo,
        [FromQuery] string? branch,
        [FromQuery] string? lang)
    {
        var repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrgName == owner && r.RepoName == repo);

        if (repository is null)
        {
            return Results.NotFound(new { error = "Repository does not exist" });
        }

        var branchQuery = context.RepositoryBranches
            .AsNoTracking()
            .Where(b => b.RepositoryId == repository.Id);

        if (!string.IsNullOrEmpty(branch))
        {
            branchQuery = branchQuery.Where(b => b.BranchName == branch);
        }

        var repoBranch = await branchQuery.FirstOrDefaultAsync();
        if (repoBranch is null)
        {
            return Results.NotFound(new { error = "Branch does not exist" });
        }

        var languageQuery = context.BranchLanguages
            .AsNoTracking()
            .Where(l => l.RepositoryBranchId == repoBranch.Id && !l.IsDeleted);

        if (!string.IsNullOrEmpty(lang))
        {
            languageQuery = languageQuery.Where(l => l.LanguageCode == lang);
        }
        else
        {
            languageQuery = languageQuery.OrderByDescending(l => l.IsDefault);
        }

        var branchLanguage = await languageQuery.FirstOrDefaultAsync();
        if (branchLanguage is null)
        {
            return Results.NotFound(new { error = "Language does not exist" });
        }

        return Results.Ok(new MindMapResponse
        {
            Owner = owner,
            Repo = repo,
            Branch = repoBranch.BranchName,
            Language = branchLanguage.LanguageCode,
            Status = branchLanguage.MindMapStatus,
            StatusName = branchLanguage.MindMapStatus.ToString(),
            Content = branchLanguage.MindMapContent
        });
    }
}

/// <summary>
/// Mind map API response
/// </summary>
public class MindMapResponse
{
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public MindMapStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Content { get; set; }
}
