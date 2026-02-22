using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Translation;

/// <summary>
/// Translation service interface
/// Manages creation and querying of translation tasks
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Create translation task
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="repositoryBranchId">Repository branch ID</param>
    /// <param name="sourceBranchLanguageId">Source language branch ID</param>
    /// <param name="targetLanguageCode">Target language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created translation task, returns null if task already exists</returns>
    Task<TranslationTask?> CreateTaskAsync(
        string repositoryId,
        string repositoryBranchId,
        string sourceBranchLanguageId,
        string targetLanguageCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch create translation tasks
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="repositoryBranchId">Repository branch ID</param>
    /// <param name="sourceBranchLanguageId">Source language branch ID</param>
    /// <param name="targetLanguageCodes">Target language code list</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of created translation tasks</returns>
    Task<List<TranslationTask>> CreateTasksAsync(
        string repositoryId,
        string repositoryBranchId,
        string sourceBranchLanguageId,
        IEnumerable<string> targetLanguageCodes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get next pending translation task
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pending translation task, returns null if none</returns>
    Task<TranslationTask?> GetNextPendingTaskAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update task status to processing
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsProcessingAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update task status to completed
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsCompletedAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update task status to failed
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsFailedAsync(string taskId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get translation task list for a repository
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translation task list</returns>
    Task<List<TranslationTask>> GetTasksByRepositoryAsync(
        string repositoryId,
        CancellationToken cancellationToken = default);
}
