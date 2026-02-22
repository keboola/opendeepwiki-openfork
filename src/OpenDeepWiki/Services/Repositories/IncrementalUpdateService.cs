using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Notifications;
using OpenDeepWiki.Services.Wiki;

namespace OpenDeepWiki.Services.Repositories;

/// <summary>
/// Incremental update service implementation
/// Encapsulates the core business logic for incremental updates
/// </summary>
public class IncrementalUpdateService : IIncrementalUpdateService
{
    private readonly IRepositoryAnalyzer _repositoryAnalyzer;
    private readonly IWikiGenerator _wikiGenerator;
    private readonly ISubscriberNotificationService _notificationService;
    private readonly IContext _context;
    private readonly IncrementalUpdateOptions _options;
    private readonly ILogger<IncrementalUpdateService> _logger;

    public IncrementalUpdateService(
        IRepositoryAnalyzer repositoryAnalyzer,
        IWikiGenerator wikiGenerator,
        ISubscriberNotificationService notificationService,
        IContext context,
        IOptions<IncrementalUpdateOptions> options,
        ILogger<IncrementalUpdateService> logger)
    {
        _repositoryAnalyzer = repositoryAnalyzer;
        _wikiGenerator = wikiGenerator;
        _notificationService = notificationService;
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        string repositoryId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Checking for updates. RepositoryId: {RepositoryId}, BranchId: {BranchId}",
            repositoryId, branchId);

        // Get repository and branch information
        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken);

        if (repository == null)
        {
            _logger.LogWarning("Repository not found. RepositoryId: {RepositoryId}", repositoryId);
            return new UpdateCheckResult { NeedsUpdate = false };
        }

        var branch = await _context.RepositoryBranches
            .FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken);

        if (branch == null)
        {
            _logger.LogWarning("Branch not found. BranchId: {BranchId}", branchId);
            return new UpdateCheckResult { NeedsUpdate = false };
        }

        var previousCommitId = branch.LastCommitId;

        try
        {
            // Prepare workspace (clone or pull)
            var workspace = await PrepareWorkspaceWithRetryAsync(
                repository, branch.BranchName, previousCommitId, cancellationToken);

            var currentCommitId = workspace.CommitId;

            // If commit IDs are the same, no update needed
            if (previousCommitId == currentCommitId)
            {
                _logger.LogInformation(
                    "No changes detected. RepositoryId: {RepositoryId}, CommitId: {CommitId}",
                    repositoryId, currentCommitId);

                return new UpdateCheckResult
                {
                    NeedsUpdate = false,
                    PreviousCommitId = previousCommitId,
                    CurrentCommitId = currentCommitId,
                    ChangedFiles = Array.Empty<string>()
                };
            }

            // Get list of changed files
            var changedFiles = await _repositoryAnalyzer.GetChangedFilesAsync(
                workspace, previousCommitId, currentCommitId, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Update check completed. RepositoryId: {RepositoryId}, PreviousCommit: {PreviousCommit}, " +
                "CurrentCommit: {CurrentCommit}, ChangedFiles: {ChangedFilesCount}, Duration: {Duration}ms",
                repositoryId, previousCommitId ?? "none", currentCommitId,
                changedFiles.Length, stopwatch.ElapsedMilliseconds);

            return new UpdateCheckResult
            {
                NeedsUpdate = changedFiles.Length > 0 || string.IsNullOrEmpty(previousCommitId),
                PreviousCommitId = previousCommitId,
                CurrentCommitId = currentCommitId,
                ChangedFiles = changedFiles
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Failed to check for updates. RepositoryId: {RepositoryId}, BranchId: {BranchId}, Duration: {Duration}ms",
                repositoryId, branchId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IncrementalUpdateResult> ProcessIncrementalUpdateAsync(
        string repositoryId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing incremental update. RepositoryId: {RepositoryId}, BranchId: {BranchId}",
            repositoryId, branchId);

        try
        {
            // Check for updates
            var checkResult = await CheckForUpdatesAsync(repositoryId, branchId, cancellationToken);

            if (!checkResult.NeedsUpdate)
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "No update needed. RepositoryId: {RepositoryId}, Duration: {Duration}ms",
                    repositoryId, stopwatch.ElapsedMilliseconds);

                return new IncrementalUpdateResult
                {
                    Success = true,
                    PreviousCommitId = checkResult.PreviousCommitId,
                    CurrentCommitId = checkResult.CurrentCommitId,
                    ChangedFilesCount = 0,
                    UpdatedDocumentsCount = 0,
                    Duration = stopwatch.Elapsed
                };
            }

            // Get repository and branch information
            var repository = await _context.Repositories
                .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken);

            var branch = await _context.RepositoryBranches
                .FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken);

            if (repository == null || branch == null)
            {
                throw new InvalidOperationException(
                    $"Repository or branch not found. RepositoryId: {repositoryId}, BranchId: {branchId}");
            }

            // Get branch languages
            var branchLanguages = await _context.BranchLanguages
                .Where(bl => bl.RepositoryBranchId == branchId)
                .ToListAsync(cancellationToken);

            // Prepare workspace
            var workspace = await PrepareWorkspaceWithRetryAsync(
                repository, branch.BranchName, checkResult.PreviousCommitId, cancellationToken);

            var updatedDocumentsCount = 0;

            // Execute incremental update for each language
            foreach (var branchLanguage in branchLanguages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "Updating wiki for language {LanguageCode}. RepositoryId: {RepositoryId}",
                    branchLanguage.LanguageCode, repositoryId);

                await _wikiGenerator.IncrementalUpdateAsync(
                    workspace,
                    branchLanguage,
                    checkResult.ChangedFiles ?? Array.Empty<string>(),
                    cancellationToken);

                updatedDocumentsCount++;
            }

            // Update branch's LastCommitId
            branch.LastCommitId = checkResult.CurrentCommitId;
            branch.LastProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Update repository's LastUpdateCheckAt
            repository.LastUpdateCheckAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Notify subscribers
            await NotifySubscribersSafelyAsync(
                repository, branch, checkResult, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Incremental update completed. RepositoryId: {RepositoryId}, " +
                "ChangedFiles: {ChangedFilesCount}, UpdatedLanguages: {UpdatedLanguagesCount}, Duration: {Duration}ms",
                repositoryId, checkResult.ChangedFiles?.Length ?? 0,
                updatedDocumentsCount, stopwatch.ElapsedMilliseconds);

            return new IncrementalUpdateResult
            {
                Success = true,
                PreviousCommitId = checkResult.PreviousCommitId,
                CurrentCommitId = checkResult.CurrentCommitId,
                ChangedFilesCount = checkResult.ChangedFiles?.Length ?? 0,
                UpdatedDocumentsCount = updatedDocumentsCount,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Incremental update failed. RepositoryId: {RepositoryId}, BranchId: {BranchId}, Duration: {Duration}ms",
                repositoryId, branchId, stopwatch.ElapsedMilliseconds);

            return new IncrementalUpdateResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc />
    public async Task<string> TriggerManualUpdateAsync(
        string repositoryId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Manual update triggered. RepositoryId: {RepositoryId}, BranchId: {BranchId}",
            repositoryId, branchId);

        // Check if a pending or processing task already exists for the same repository/branch
        var existingTask = await _context.IncrementalUpdateTasks
            .Where(t => t.RepositoryId == repositoryId
                        && t.BranchId == branchId
                        && (t.Status == IncrementalUpdateStatus.Pending
                            || t.Status == IncrementalUpdateStatus.Processing))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTask != null)
        {
            _logger.LogInformation(
                "Existing task found. TaskId: {TaskId}, Status: {Status}",
                existingTask.Id, existingTask.Status);
            return existingTask.Id;
        }

        // Get branch info to obtain the last CommitId
        var branch = await _context.RepositoryBranches
            .FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken);

        // Create high-priority task
        var task = new IncrementalUpdateTask
        {
            Id = Guid.NewGuid().ToString(),
            RepositoryId = repositoryId,
            BranchId = branchId,
            PreviousCommitId = branch?.LastCommitId,
            Status = IncrementalUpdateStatus.Pending,
            Priority = _options.ManualTriggerPriority,
            IsManualTrigger = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.IncrementalUpdateTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Manual update task created. TaskId: {TaskId}, Priority: {Priority}",
            task.Id, task.Priority);

        return task.Id;
    }

    /// <summary>
    /// Workspace preparation with retry logic
    /// </summary>
    private async Task<RepositoryWorkspace> PrepareWorkspaceWithRetryAsync(
        Repository repository,
        string branchName,
        string? previousCommitId,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < _options.MaxRetryAttempts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _repositoryAnalyzer.PrepareWorkspaceAsync(
                    repository, branchName, previousCommitId, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;

                _logger.LogWarning(ex,
                    "Workspace preparation failed. Attempt {Attempt}/{MaxAttempts}, Repository: {Org}/{Repo}",
                    retryCount, _options.MaxRetryAttempts, repository.OrgName, repository.RepoName);

                if (retryCount < _options.MaxRetryAttempts)
                {
                    // Check if the workspace is corrupted
                    if (IsWorkspaceCorrupted(ex))
                    {
                        _logger.LogInformation(
                            "Workspace appears corrupted, cleaning up. Repository: {Org}/{Repo}",
                            repository.OrgName, repository.RepoName);

                        await CleanupCorruptedWorkspaceAsync(repository, cancellationToken);
                    }

                    // Exponential backoff
                    var delay = _options.RetryBaseDelayMs * (int)Math.Pow(2, retryCount - 1);
                    _logger.LogInformation(
                        "Retrying in {Delay}ms. Repository: {Org}/{Repo}",
                        delay, repository.OrgName, repository.RepoName);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to prepare workspace after {_options.MaxRetryAttempts} attempts",
            lastException);
    }

    /// <summary>
    /// Check if the exception indicates workspace corruption
    /// </summary>
    private static bool IsWorkspaceCorrupted(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("corrupt")
               || message.Contains("invalid")
               || message.Contains("not a git repository")
               || message.Contains("bad object")
               || message.Contains("broken");
    }

    /// <summary>
    /// Clean up a corrupted workspace
    /// </summary>
    private async Task CleanupCorruptedWorkspaceAsync(
        Repository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create a temporary workspace object for cleanup
            var workspace = new RepositoryWorkspace
            {
                Organization = repository.OrgName,
                RepositoryName = repository.RepoName
            };

            await _repositoryAnalyzer.CleanupWorkspaceAsync(workspace, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to cleanup corrupted workspace. Repository: {Org}/{Repo}",
                repository.OrgName, repository.RepoName);
        }
    }

    /// <summary>
    /// Safely notify subscribers (does not affect the main flow)
    /// </summary>
    private async Task NotifySubscribersSafelyAsync(
        Repository repository,
        RepositoryBranch branch,
        UpdateCheckResult checkResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var notification = new RepositoryUpdateNotification
            {
                RepositoryId = repository.Id,
                RepositoryName = $"{repository.OrgName}/{repository.RepoName}",
                BranchName = branch.BranchName,
                Summary = $"Updated with {checkResult.ChangedFiles?.Length ?? 0} changed files",
                ChangedFilesCount = checkResult.ChangedFiles?.Length ?? 0,
                UpdatedAt = DateTime.UtcNow,
                CommitId = checkResult.CurrentCommitId ?? string.Empty
            };

            await _notificationService.NotifySubscribersAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            // Notification failure does not affect the main flow
            _logger.LogWarning(ex,
                "Failed to notify subscribers. RepositoryId: {RepositoryId}",
                repository.Id);
        }
    }
}
