using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Models;

namespace OpenDeepWiki.Services.Wiki;

[MiniApi(Route = "/api/v1/wiki")]
[Tags("Wiki")]
public class WikiService(IContext context)
{
    private const string DefaultLanguageCode = "zh";

    /// <summary>
    /// Get Wiki catalog structure
    /// </summary>
    [HttpGet("/{org}/{repo}/catalog")]
    public async Task<WikiCatalogResponse> GetCatalogAsync(string org, string repo)
    {
        var repository = await GetRepositoryAsync(org, repo);
        var branch = await GetDefaultBranchAsync(repository.Id);
        var language = await GetDefaultLanguageAsync(branch.Id);

        var catalogs = await context.DocCatalogs
            .AsNoTracking()
            .Where(c => c.BranchLanguageId == language.Id)
            .OrderBy(c => c.Order)
            .ToListAsync();

        if (catalogs.Count == 0)
        {
            throw new InvalidOperationException("Wiki catalog not found");
        }

        var rootItems = BuildCatalogTree(catalogs);
        var defaultPath = FindFirstPath(rootItems);

        return new WikiCatalogResponse
        {
            Owner = repository.OrgName,
            Repo = repository.RepoName,
            DefaultPath = defaultPath,
            Items = rootItems
        };
    }

    /// <summary>
    /// Get Wiki document content
    /// </summary>
    [HttpGet("/{org}/{repo}/doc/{*path}")]
    public async Task<WikiDocResponse> GetDocAsync(string org, string repo, string path)
    {
        var repository = await GetRepositoryAsync(org, repo);
        var branch = await GetDefaultBranchAsync(repository.Id);
        var language = await GetDefaultLanguageAsync(branch.Id);
        var normalizedPath = NormalizePath(path);

        var catalog = await context.DocCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BranchLanguageId == language.Id && c.Path == normalizedPath);

        if (catalog is null)
        {
            throw new KeyNotFoundException($"Document path '{normalizedPath}' does not exist");
        }

        if (string.IsNullOrEmpty(catalog.DocFileId))
        {
            throw new KeyNotFoundException($"Document path '{normalizedPath}' has no associated content");
        }

        var docFile = await context.DocFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == catalog.DocFileId);

        if (docFile is null)
        {
            throw new KeyNotFoundException($"Document content does not exist");
        }

        return new WikiDocResponse
        {
            Path = normalizedPath,
            Title = catalog.Title,
            Content = docFile.Content
        };
    }

    private async Task<Repository> GetRepositoryAsync(string org, string repo)
    {
        var repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrgName == org && r.RepoName == repo);

        if (repository is null)
        {
            throw new KeyNotFoundException($"Repository '{org}/{repo}' does not exist");
        }

        return repository;
    }

    private async Task<RepositoryBranch> GetDefaultBranchAsync(string repositoryId)
    {
        var branches = await context.RepositoryBranches
            .AsNoTracking()
            .Where(b => b.RepositoryId == repositoryId)
            .ToListAsync();

        if (branches.Count == 0)
        {
            throw new KeyNotFoundException("Repository branch does not exist");
        }

        return branches.FirstOrDefault(b => string.Equals(b.BranchName, "main", StringComparison.OrdinalIgnoreCase))
               ?? branches.FirstOrDefault(b => string.Equals(b.BranchName, "master", StringComparison.OrdinalIgnoreCase))
               ?? branches.OrderBy(b => b.CreatedAt).First();
    }

    private async Task<BranchLanguage> GetDefaultLanguageAsync(string branchId)
    {
        var languages = await context.BranchLanguages
            .AsNoTracking()
            .Where(l => l.RepositoryBranchId == branchId)
            .ToListAsync();

        if (languages.Count == 0)
        {
            throw new KeyNotFoundException("Repository language does not exist");
        }

        return languages.FirstOrDefault(l => string.Equals(l.LanguageCode, DefaultLanguageCode, StringComparison.OrdinalIgnoreCase))
               ?? languages.OrderBy(l => l.CreatedAt).First();
    }

    private static List<WikiCatalogItemResponse> BuildCatalogTree(List<DocCatalog> catalogs)
    {
        var lookup = catalogs.ToLookup(c => c.ParentId);
        return BuildChildren(lookup, null);
    }

    private static List<WikiCatalogItemResponse> BuildChildren(ILookup<string?, DocCatalog> lookup, string? parentId)
    {
        return lookup[parentId]
            .OrderBy(c => c.Order)
            .Select(c => new WikiCatalogItemResponse
            {
                Title = c.Title,
                Path = c.Path,
                Order = c.Order,
                HasContent = !string.IsNullOrEmpty(c.DocFileId),
                Children = BuildChildren(lookup, c.Id)
            })
            .ToList();
    }

    private static string FindFirstPath(List<WikiCatalogItemResponse> items)
    {
        if (items.Count == 0)
        {
            return string.Empty;
        }

        var first = items.OrderBy(i => i.Order).First();
        return first.HasContent ? first.Path : FindFirstPath(first.Children);
    }

    private static string NormalizePath(string path)
    {
        return path.Trim().Trim('/');
    }
}
