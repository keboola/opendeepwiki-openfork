using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Repositories;
using OpenDeepWiki.Services.Wiki;

namespace OpenDeepWiki.Services.Translation;

/// <summary>
/// Translation background service
/// Independent from document generation flow, automatically discovers and processes translation tasks
/// </summary>
public class TranslationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TranslationWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    public TranslationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<TranslationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Translation worker started. Polling interval: {PollingInterval}s",
            PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingTasksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Translation worker is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Translation processing loop failed unexpectedly");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("Translation worker stopped");
    }

    private async Task ProcessPendingTasksAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var translationService = scope.ServiceProvider.GetService<ITranslationService>();
        var context = scope.ServiceProvider.GetService<IContext>();
        var repositoryAnalyzer = scope.ServiceProvider.GetService<IRepositoryAnalyzer>();
        var wikiGenerator = scope.ServiceProvider.GetService<IWikiGenerator>();
        var processingLogService = scope.ServiceProvider.GetService<IProcessingLogService>();
        var wikiOptions = scope.ServiceProvider.GetService<IOptions<WikiGeneratorOptions>>()?.Value;

        if (translationService == null || context == null || repositoryAnalyzer == null || 
            wikiGenerator == null || wikiOptions == null)
        {
            _logger.LogWarning("Required services not registered, skip translation processing");
            return;
        }

        // First scan and create needed translation tasks
        await ScanAndCreateTranslationTasksAsync(context, wikiOptions, stoppingToken);

        // Then process pending tasks
        while (!stoppingToken.IsCancellationRequested)
        {
            var task = await translationService.GetNextPendingTaskAsync(stoppingToken);
            if (task == null)
            {
                _logger.LogDebug("No pending translation tasks found");
                break;
            }

            await ProcessTaskAsync(
                task,
                translationService,
                context,
                repositoryAnalyzer,
                wikiGenerator,
                processingLogService,
                stoppingToken);
        }
    }

    /// <summary>
    /// Scan completed repositories and automatically create needed translation tasks
    /// </summary>
    private async Task ScanAndCreateTranslationTasksAsync(
        IContext context,
        WikiGeneratorOptions wikiOptions,
        CancellationToken stoppingToken)
    {
        // Query all branch languages from completed repositories
        var branchLanguages = await context.BranchLanguages
            .Include(bl => bl.RepositoryBranch)
            .ThenInclude(rb => rb!.Repository)
            .Where(bl => !bl.IsDeleted &&
                         bl.RepositoryBranch != null &&
                         !bl.RepositoryBranch.IsDeleted &&
                         bl.RepositoryBranch.Repository != null &&
                         !bl.RepositoryBranch.Repository.IsDeleted &&
                         bl.RepositoryBranch.Repository.Status == RepositoryStatus.Completed)
            .ToListAsync(stoppingToken);

        var createdCount = 0;

        foreach (var branchLanguage in branchLanguages)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var translationLanguages = wikiOptions.GetTranslationLanguages(branchLanguage.LanguageCode);
            if (translationLanguages.Count == 0)
            {
                continue;
            }

            var repository = branchLanguage.RepositoryBranch!.Repository!;

            foreach (var targetLang in translationLanguages)
            {
                // Check if target language already exists
                var existingLang = await context.BranchLanguages
                    .AnyAsync(l => l.RepositoryBranchId == branchLanguage.RepositoryBranchId &&
                                   l.LanguageCode == targetLang &&
                                   !l.IsDeleted, stoppingToken);

                if (existingLang)
                {
                    continue;
                }

                // Check if a translation task already exists (any status)
                var existingTask = await context.TranslationTasks
                    .FirstOrDefaultAsync(t => t.RepositoryBranchId == branchLanguage.RepositoryBranchId &&
                                              t.TargetLanguageCode == targetLang &&
                                              !t.IsDeleted, stoppingToken);

                if (existingTask != null)
                {
                    // Skip if task is pending or processing
                    if (existingTask.Status == TranslationTaskStatus.Pending ||
                        existingTask.Status == TranslationTaskStatus.Processing)
                    {
                        continue;
                    }

                    // Skip if task is completed (target language should already exist)
                    if (existingTask.Status == TranslationTaskStatus.Completed)
                    {
                        continue;
                    }

                    // If task failed and hasn't exceeded max retry count, reset to pending
                    if (existingTask.Status == TranslationTaskStatus.Failed &&
                        existingTask.RetryCount < existingTask.MaxRetryCount)
                    {
                        existingTask.Status = TranslationTaskStatus.Pending;
                        existingTask.ErrorMessage = null;
                        createdCount++;

                        _logger.LogDebug("Translation task reset to pending. TargetLang: {TargetLang}, Repository: {Org}/{Repo}, RetryCount: {RetryCount}",
                            targetLang, repository.OrgName, repository.RepoName, existingTask.RetryCount);
                    }

                    continue;
                }

                // Create translation task
                var task = new TranslationTask
                {
                    Id = Guid.NewGuid().ToString(),
                    RepositoryId = repository.Id,
                    RepositoryBranchId = branchLanguage.RepositoryBranchId,
                    SourceBranchLanguageId = branchLanguage.Id,
                    TargetLanguageCode = targetLang.ToLowerInvariant(),
                    Status = TranslationTaskStatus.Pending
                };

                context.TranslationTasks.Add(task);
                createdCount++;

                _logger.LogDebug("Translation task created. TargetLang: {TargetLang}, Repository: {Org}/{Repo}",
                    targetLang, repository.OrgName, repository.RepoName);
            }
        }

        if (createdCount > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Created {Count} translation tasks from scan", createdCount);
        }
    }

    private async Task ProcessTaskAsync(
        TranslationTask task,
        ITranslationService translationService,
        IContext context,
        IRepositoryAnalyzer repositoryAnalyzer,
        IWikiGenerator wikiGenerator,
        IProcessingLogService? processingLogService,
        CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting translation task. TaskId: {TaskId}, TargetLang: {TargetLang}, RetryCount: {RetryCount}",
            task.Id, task.TargetLanguageCode, task.RetryCount);

        // Mark as processing
        await translationService.MarkAsProcessingAsync(task.Id, stoppingToken);

        // Set current repository ID on WikiGenerator (for logging)
        if (wikiGenerator is WikiGenerator generator)
        {
            generator.SetCurrentRepository(task.RepositoryId);
        }

        // Log translation start
        if (processingLogService != null)
        {
            await processingLogService.LogAsync(
                task.RepositoryId,
                ProcessingStep.Translation,
                $"Starting translation task: {task.TargetLanguageCode}",
                cancellationToken: stoppingToken);
        }

        try
        {
            // Get repository information
            var repository = await context.Repositories
                .FirstOrDefaultAsync(r => r.Id == task.RepositoryId && !r.IsDeleted, stoppingToken);

            if (repository == null)
            {
                throw new InvalidOperationException($"Repository not found: {task.RepositoryId}");
            }

            // Get branch information
            var branch = await context.RepositoryBranches
                .FirstOrDefaultAsync(b => b.Id == task.RepositoryBranchId && !b.IsDeleted, stoppingToken);

            if (branch == null)
            {
                throw new InvalidOperationException($"Branch not found: {task.RepositoryBranchId}");
            }

            // Get source language information
            var sourceBranchLanguage = await context.BranchLanguages
                .FirstOrDefaultAsync(l => l.Id == task.SourceBranchLanguageId && !l.IsDeleted, stoppingToken);

            if (sourceBranchLanguage == null)
            {
                throw new InvalidOperationException($"Source branch language not found: {task.SourceBranchLanguageId}");
            }

            // Prepare workspace
            var workspace = await repositoryAnalyzer.PrepareWorkspaceAsync(
                repository,
                branch.BranchName,
                branch.LastCommitId,
                stoppingToken);

            try
            {
                // Execute translation
                await wikiGenerator.TranslateWikiAsync(
                    workspace,
                    sourceBranchLanguage,
                    task.TargetLanguageCode,
                    stoppingToken);

                // Mark as completed
                await translationService.MarkAsCompletedAsync(task.Id, stoppingToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Translation task completed. TaskId: {TaskId}, TargetLang: {TargetLang}, Duration: {Duration}ms",
                    task.Id, task.TargetLanguageCode, stopwatch.ElapsedMilliseconds);

                // Log completion
                if (processingLogService != null)
                {
                    await processingLogService.LogAsync(
                        task.RepositoryId,
                        ProcessingStep.Translation,
                        $"Translation completed: {task.TargetLanguageCode}, elapsed: {stopwatch.ElapsedMilliseconds}ms",
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
            // Reset to pending on cancellation
            await translationService.MarkAsFailedAsync(task.Id, "Task cancelled", stoppingToken);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Translation task failed. TaskId: {TaskId}, TargetLang: {TargetLang}, Duration: {Duration}ms",
                task.Id, task.TargetLanguageCode, stopwatch.ElapsedMilliseconds);

            // Mark as failed (will automatically retry)
            await translationService.MarkAsFailedAsync(task.Id, ex.Message, stoppingToken);

            // Log failure
            if (processingLogService != null)
            {
                await processingLogService.LogAsync(
                    task.RepositoryId,
                    ProcessingStep.Translation,
                    $"Translation failed: {task.TargetLanguageCode} - {ex.Message}",
                    cancellationToken: stoppingToken);
            }
        }
    }
}
