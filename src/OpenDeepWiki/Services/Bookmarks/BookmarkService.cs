using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Models.Bookmark;

namespace OpenDeepWiki.Services.Bookmarks;

/// <summary>
/// Bookmark service
/// Handles user repository bookmark business logic
/// </summary>
[MiniApi(Route = "/api/v1/bookmarks")]
[Tags("Bookmarks")]
public class BookmarkService(IContext context)
{
    /// <summary>
    /// Add bookmark
    /// Atomically increment repository bookmark count
    /// </summary>
    /// <param name="request">Add bookmark request</param>
    /// <returns>Bookmark operation response</returns>
    [HttpPost("/")]
    public async Task<IResult> AddBookmarkAsync([FromBody] AddBookmarkRequest request)
    {
        try
        {
            // Verify repository exists
            var repository = await context.Repositories
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.RepositoryId);

            if (repository is null)
            {
                return Results.NotFound(new BookmarkResponse
                {
                    Success = false,
                    ErrorMessage = "Repository not found"
                });
            }

            // Verify user exists
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user is null)
            {
                return Results.NotFound(new BookmarkResponse
                {
                    Success = false,
                    ErrorMessage = "User not found"
                });
            }

            // Check if already bookmarked
            var existingBookmark = await context.UserBookmarks
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == request.UserId && b.RepositoryId == request.RepositoryId);

            if (existingBookmark is not null)
            {
                return Results.Conflict(new BookmarkResponse
                {
                    Success = false,
                    ErrorMessage = "Repository already bookmarked"
                });
            }

            // Use transaction to ensure atomicity
            var dbContext = context as DbContext;
            if (dbContext is null)
            {
                return Results.Json(new BookmarkResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                }, statusCode: StatusCodes.Status500InternalServerError);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Create bookmark record
                var bookmark = new UserBookmark
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    RepositoryId = request.RepositoryId
                };

                context.UserBookmarks.Add(bookmark);
                await context.SaveChangesAsync();

                // Atomically increment bookmark count
                await dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE Repositories SET BookmarkCount = BookmarkCount + 1 WHERE Id = {0}",
                    request.RepositoryId);

                await transaction.CommitAsync();

                return Results.Ok(new BookmarkResponse
                {
                    Success = true,
                    BookmarkId = bookmark.Id
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception)
        {
            return Results.Json(new BookmarkResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Remove bookmark
    /// Atomically decrement repository bookmark count
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Bookmark operation response</returns>
    [HttpDelete("{repositoryId}")]
    public async Task<IResult> RemoveBookmarkAsync(string repositoryId, [FromQuery] string userId)
    {
        try
        {
            // Find bookmark record
            var bookmark = await context.UserBookmarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.RepositoryId == repositoryId);

            if (bookmark is null)
            {
                return Results.NotFound(new BookmarkResponse
                {
                    Success = false,
                    ErrorMessage = "Bookmark not found"
                });
            }

            // Use transaction to ensure atomicity
            var dbContext = context as DbContext;
            if (dbContext is null)
            {
                return Results.Json(new BookmarkResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                }, statusCode: StatusCodes.Status500InternalServerError);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Remove bookmark record
                context.UserBookmarks.Remove(bookmark);
                await context.SaveChangesAsync();

                // Atomically decrement bookmark count (ensure it doesn't go negative)
                await dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE Repositories SET BookmarkCount = CASE WHEN BookmarkCount > 0 THEN BookmarkCount - 1 ELSE 0 END WHERE Id = {0}",
                    repositoryId);

                await transaction.CommitAsync();

                return Results.Ok(new BookmarkResponse
                {
                    Success = true,
                    BookmarkId = bookmark.Id
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception)
        {
            return Results.Json(new BookmarkResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get user bookmark list
    /// Supports pagination, ordered by bookmark time descending
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (starting from 1)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Bookmark list response</returns>
    [HttpGet("/")]
    public async Task<BookmarkListResponse> GetUserBookmarksAsync(
        [FromQuery] string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Parameter validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Query total user bookmarks count
        var total = await context.UserBookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .CountAsync();

        // Paginated bookmark list query, ordered by creation time descending
        var bookmarks = await context.UserBookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.Repository)
            .ToListAsync();

        // Convert to response model
        var items = bookmarks
            .Where(b => b.Repository is not null)
            .Select(b => new BookmarkItemResponse
            {
                BookmarkId = b.Id,
                RepositoryId = b.RepositoryId,
                RepoName = b.Repository!.RepoName,
                OrgName = b.Repository.OrgName,
                Description = null, // Repository entity does not have a Description field yet
                StarCount = b.Repository.StarCount,
                ForkCount = b.Repository.ForkCount,
                BookmarkCount = b.Repository.BookmarkCount,
                BookmarkedAt = b.CreatedAt
            })
            .ToList();

        return new BookmarkListResponse
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Check bookmark status
    /// Query whether the user has bookmarked the specified repository
    /// </summary>
    /// <param name="repositoryId">Repository ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Bookmark status response</returns>
    [HttpGet("{repositoryId}/status")]
    public async Task<BookmarkStatusResponse> GetBookmarkStatusAsync(
        string repositoryId,
        [FromQuery] string userId)
    {
        // If user ID is empty, return not-bookmarked status
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new BookmarkStatusResponse
            {
                IsBookmarked = false,
                BookmarkedAt = null
            };
        }

        // Query bookmark record
        var bookmark = await context.UserBookmarks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.UserId == userId && b.RepositoryId == repositoryId);

        return new BookmarkStatusResponse
        {
            IsBookmarked = bookmark is not null,
            BookmarkedAt = bookmark?.CreatedAt
        };
    }
}
