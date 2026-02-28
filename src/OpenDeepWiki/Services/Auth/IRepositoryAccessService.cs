using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// Determines whether the current user can access a given repository.
/// Public repos are always accessible. Private repos require authentication
/// and either ownership or department assignment.
/// </summary>
public interface IRepositoryAccessService
{
    /// <summary>
    /// Check if the current HTTP request has access to a repository.
    /// Returns true if the repo is public, or if the caller is authenticated
    /// and has access via ownership or department assignment.
    /// </summary>
    Task<bool> CanAccessRepositoryAsync(Repository repository);
}
