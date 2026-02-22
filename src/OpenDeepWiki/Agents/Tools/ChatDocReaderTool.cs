using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenDeepWiki.EFCore;

namespace OpenDeepWiki.Agents.Tools;

/// <summary>
/// AI Tool for reading document content in chat assistant.
/// Provides a single Read method with required startLine and endLine parameters.
/// The tool description dynamically includes the repository's document catalog.
/// </summary>
public class ChatDocReaderTool
{
    private readonly IContext _context;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _branch;
    private readonly string _language;
    private readonly string _documentCatalog;

    /// <summary>
    /// Initializes a new instance of ChatDocReaderTool.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="branch">The branch name.</param>
    /// <param name="language">The language code.</param>
    /// <param name="documentCatalog">Pre-formatted document catalog string.</param>
    public ChatDocReaderTool(
        IContext context, 
        string owner, 
        string repo, 
        string branch, 
        string language,
        string documentCatalog)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _branch = branch ?? throw new ArgumentNullException(nameof(branch));
        _language = language ?? throw new ArgumentNullException(nameof(language));
        _documentCatalog = documentCatalog ?? string.Empty;
    }

    /// <summary>
    /// Reads document content by path with required line range.
    /// </summary>
    public async Task<string> ReadAsync(
        [Description("Document path from the catalog")] 
        string path,
        [Description("Starting line number (1-based, inclusive)")] 
        int startLine,
        [Description("Ending line number (1-based, inclusive)")] 
        int endLine,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return JsonSerializer.Serialize(new { error = true, message = "Document path cannot be empty" });
        }

        if (startLine < 1)
        {
            return JsonSerializer.Serialize(new { error = true, message = "startLine must be greater than or equal to 1" });
        }

        if (endLine < startLine)
        {
            return JsonSerializer.Serialize(new { error = true, message = "endLine must be greater than or equal to startLine" });
        }

        if (endLine - startLine > 200)
        {
            return JsonSerializer.Serialize(new { error = true, message = "Maximum 200 lines per read, please reduce the range" });
        }

        try
        {
            var repository = await _context.Repositories
                .FirstOrDefaultAsync(r => r.OrgName == _owner && 
                                          r.RepoName == _repo && 
                                          !r.IsDeleted, cancellationToken);

            if (repository == null)
            {
                return JsonSerializer.Serialize(new { error = true, message = $"Repository '{_owner}/{_repo}' does not exist" });
            }

            var branch = await _context.RepositoryBranches
                .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && 
                                          b.BranchName == _branch && 
                                          !b.IsDeleted, cancellationToken);

            if (branch == null)
            {
                return JsonSerializer.Serialize(new { error = true, message = $"Branch '{_branch}' does not exist" });
            }

            var branchLanguage = await _context.BranchLanguages
                .FirstOrDefaultAsync(bl => bl.RepositoryBranchId == branch.Id && 
                                           bl.LanguageCode == _language && 
                                           !bl.IsDeleted, cancellationToken);

            if (branchLanguage == null)
            {
                return JsonSerializer.Serialize(new { error = true, message = $"Language '{_language}' does not exist" });
            }

            var catalog = await _context.DocCatalogs
                .FirstOrDefaultAsync(c => c.BranchLanguageId == branchLanguage.Id && 
                                          c.Path == path && 
                                          !c.IsDeleted, cancellationToken);

            if (catalog == null)
            {
                return JsonSerializer.Serialize(new { error = true, message = $"Document '{path}' does not exist" });
            }

            if (string.IsNullOrEmpty(catalog.DocFileId))
            {
                return JsonSerializer.Serialize(new { error = true, message = $"Document '{path}' has no content" });
            }

            var docFile = await _context.DocFiles
                .FirstOrDefaultAsync(d => d.Id == catalog.DocFileId && !d.IsDeleted, cancellationToken);

            if (docFile == null)
            {
                return JsonSerializer.Serialize(new { error = true, message = $"Document '{path}' content does not exist" });
            }

            var allLines = docFile.Content.Split('\n');
            var totalLines = allLines.Length;
            var actualEndLine = Math.Min(endLine, totalLines);
            
            if (startLine > totalLines)
            {
                return JsonSerializer.Serialize(new { 
                    error = true, 
                    message = $"startLine ({startLine}) exceeds total line count ({totalLines})" 
                });
            }

            var selectedLines = allLines.Skip(startLine - 1).Take(actualEndLine - startLine + 1);
            var content = string.Join("\n", selectedLines);

            // Parse the list of source code files the document depends on
            List<string>? sourceFiles = null;
            if (!string.IsNullOrEmpty(docFile.SourceFiles))
            {
                try
                {
                    sourceFiles = JsonSerializer.Deserialize<List<string>>(docFile.SourceFiles);
                }
                catch
                {
                    // Ignore parsing failures
                }
            }

            return JsonSerializer.Serialize(new { 
                content,
                startLine,
                endLine = actualEndLine,
                totalLines,
                path,
                sourceFiles
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = true, message = $"Failed to read document: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets the AI tool with dynamic description including document catalog.
    /// </summary>
    public AITool GetTool()
    {
        var description = BuildToolDescription();
        
        return AIFunctionFactory.Create(ReadAsync, new AIFunctionFactoryOptions
        {
            Name = "ReadDoc",
            Description = description
        });
    }

    /// <summary>
    /// Builds the tool description with document catalog.
    /// </summary>
    private string BuildToolDescription()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Read document content from the repository. Must specify start and end line numbers.");
        sb.AppendLine();
        sb.AppendLine("Parameters:");
        sb.AppendLine("- path: Document path (select from the catalog below)");
        sb.AppendLine("- startLine: Starting line number (1-based)");
        sb.AppendLine("- endLine: Ending line number (max 200 lines per request)");
        sb.AppendLine();
        sb.AppendLine("Returns: Document content for the specified line range, total line count");
        sb.AppendLine();
        sb.AppendLine($"Repository: {_owner}/{_repo}");
        sb.AppendLine($"Branch: {_branch}");
        sb.AppendLine($"Language: {_language}");
        sb.AppendLine();
        sb.AppendLine("Available Documents:");
        sb.AppendLine(_documentCatalog);
        
        return sb.ToString();
    }

    /// <summary>
    /// Creates a ChatDocReaderTool with document catalog loaded from database.
    /// </summary>
    public static async Task<ChatDocReaderTool> CreateAsync(
        IContext context,
        string owner,
        string repo,
        string branch,
        string language,
        CancellationToken cancellationToken = default)
    {
        var catalog = await LoadDocumentCatalogAsync(context, owner, repo, branch, language, cancellationToken);
        return new ChatDocReaderTool(context, owner, repo, branch, language, catalog);
    }

    /// <summary>
    /// Loads document catalog from database.
    /// </summary>
    private static async Task<string> LoadDocumentCatalogAsync(
        IContext context,
        string owner,
        string repo,
        string branch,
        string language,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = await context.Repositories
                .FirstOrDefaultAsync(r => r.OrgName == owner && 
                                          r.RepoName == repo && 
                                          !r.IsDeleted, cancellationToken);

            if (repository == null) return "(Repository not found)";

            var branchEntity = await context.RepositoryBranches
                .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && 
                                          b.BranchName == branch && 
                                          !b.IsDeleted, cancellationToken);

            if (branchEntity == null) return "(Branch not found)";

            var branchLanguage = await context.BranchLanguages
                .FirstOrDefaultAsync(bl => bl.RepositoryBranchId == branchEntity.Id && 
                                           bl.LanguageCode == language && 
                                           !bl.IsDeleted, cancellationToken);

            if (branchLanguage == null) return "(Language not found)";

            var catalogs = await context.DocCatalogs
                .Where(c => c.BranchLanguageId == branchLanguage.Id && 
                            !c.IsDeleted && 
                            !string.IsNullOrEmpty(c.DocFileId))
                .OrderBy(c => c.Order)
                .Select(c => new { c.Title, c.Path })
                .ToListAsync(cancellationToken);

            if (catalogs.Count == 0) return "(No documents available)";

            var sb = new StringBuilder();
            foreach (var doc in catalogs)
            {
                sb.AppendLine($"- title: {doc.Title}");
                sb.AppendLine($"  path: {doc.Path}");
            }

            return sb.ToString();
        }
        catch
        {
            return "(Failed to load catalog)";
        }
    }
}
