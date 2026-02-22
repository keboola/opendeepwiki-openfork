using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Repositories;
using OpenDeepWiki.Services.Wiki;

namespace OpenDeepWiki.Services.MindMap;

/// <summary>
/// Mind map generation background service
/// Independent from the document generation flow, asynchronously processes mind map generation tasks
/// Queries repositories that have completed processing but have not yet generated mind maps
/// </summary>
public class MindMapWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MindMapWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    public MindMapWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MindMapWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MindMap worker started. Polling interval: {PollingInterval}s",
            PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMindMapsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MindMap worker is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MindMap processing loop failed unexpectedly");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("MindMap worker stopped");
    }

    private async Task ProcessPendingMindMapsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetService<IContext>();
        var repositoryAnalyzer = scope.ServiceProvider.GetService<IRepositoryAnalyzer>();
        var wikiGenerator = scope.ServiceProvider.GetService<IWikiGenerator>();
        var processingLogService = scope.ServiceProvider.GetService<IProcessingLogService>();

        if (context == null || repositoryAnalyzer == null || wikiGenerator == null)
        {
            _logger.LogWarning("Required services not registered, skip mindmap processing");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            // Query branch languages where processing is completed but mind map status is Pending or Failed
            var branchLanguage = await context.BranchLanguages
                .Include(bl => bl.RepositoryBranch)
                .ThenInclude(rb => rb!.Repository)
                .Where(bl => !bl.IsDeleted &&
                             bl.RepositoryBranch != null &&
                             !bl.RepositoryBranch.IsDeleted &&
                             bl.RepositoryBranch.Repository != null &&
                             !bl.RepositoryBranch.Repository.IsDeleted &&
                             bl.RepositoryBranch.Repository.Status == RepositoryStatus.Completed &&
                             (bl.MindMapStatus == MindMapStatus.Pending || bl.MindMapStatus == MindMapStatus.Failed))
                .OrderBy(bl => bl.CreatedAt)
                .FirstOrDefaultAsync(stoppingToken);

            if (branchLanguage == null)
            {
                _logger.LogDebug("No pending mindmap tasks found");
                break;
            }

            await ProcessMindMapAsync(
                branchLanguage,
                context,
                repositoryAnalyzer,
                wikiGenerator,
                processingLogService,
                stoppingToken);
        }
    }

    private async Task ProcessMindMapAsync(
        BranchLanguage branchLanguage,
        IContext context,
        IRepositoryAnalyzer repositoryAnalyzer,
        IWikiGenerator wikiGenerator,
        IProcessingLogService? processingLogService,
        CancellationToken stoppingToken)
    {
        var repository = branchLanguage.RepositoryBranch!.Repository!;
        var branch = branchLanguage.RepositoryBranch;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting mindmap generation. BranchLanguageId: {BranchLanguageId}, Repository: {Org}/{Repo}, Language: {Lang}",
            branchLanguage.Id, repository.OrgName, repository.RepoName, branchLanguage.LanguageCode);

        // Set current repository ID on WikiGenerator (for logging)
        if (wikiGenerator is WikiGenerator generator)
        {
            generator.SetCurrentRepository(repository.Id);
        }

        // Log generation start
        if (processingLogService != null)
        {
            await processingLogService.LogAsync(
                repository.Id,
                ProcessingStep.MindMap,
                $"Starting mind map generation: {branchLanguage.LanguageCode}",
                cancellationToken: stoppingToken);
        }

        try
        {
            // Prepare workspace
            var workspace = await repositoryAnalyzer.PrepareWorkspaceAsync(
                repository,
                branch!.BranchName,
                branch.LastCommitId,
                stoppingToken);

            try
            {
                // Execute mind map generation
                await wikiGenerator.GenerateMindMapAsync(workspace, branchLanguage, stoppingToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "MindMap generation completed. BranchLanguageId: {BranchLanguageId}, Duration: {Duration}ms",
                    branchLanguage.Id, stopwatch.ElapsedMilliseconds);

                // Log completion
                if (processingLogService != null)
                {
                    await processingLogService.LogAsync(
                        repository.Id,
                        ProcessingStep.MindMap,
                        $"Mind map generation completed: {branchLanguage.LanguageCode}, elapsed: {stopwatch.ElapsedMilliseconds}ms",
                        cancellationToken: stoppingToken);
                }
            }
            finally
            {
                // Cleanup workspace
                await repositoryAnalyzer.CleanupWorkspaceAsync(workspace, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "MindMap generation failed. BranchLanguageId: {BranchLanguageId}, Duration: {Duration}ms",
                branchLanguage.Id, stopwatch.ElapsedMilliseconds);

            // Log failure (MindMapStatus is already updated in WikiGenerator.GenerateMindMapAsync)
            if (processingLogService != null)
            {
                await processingLogService.LogAsync(
                    repository.Id,
                    ProcessingStep.MindMap,
                    $"Mind map generation failed: {branchLanguage.LanguageCode} - {ex.Message}",
                    cancellationToken: stoppingToken);
            }
        }
    }
}
