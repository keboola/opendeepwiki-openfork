using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Models.Subscription;

namespace OpenDeepWiki.Services.Subscriptions;

/// <summary>
/// Subscription service
/// Handles user repository subscription business logic
/// </summary>
[MiniApi(Route = "/api/v1/subscriptions")]
[Tags("Subscriptions")]
public class SubscriptionService(IContext context)
{
    /// <summary>
    /// Add subscription
    /// Atomically increment repository subscription count
    /// </summary>
    [HttpPost("/")]
    public async Task<IResult> AddSubscriptionAsync([FromBody] AddSubscriptionRequest request)
    {
        try
        {
            var repository = await context.Repositories
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.RepositoryId);

            if (repository is null)
            {
                return Results.NotFound(new SubscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Repository does not exist"
                });
            }

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user is null)
            {
                return Results.NotFound(new SubscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "User does not exist"
                });
            }


            var existingSubscription = await context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.RepositoryId == request.RepositoryId);

            if (existingSubscription is not null)
            {
                return Results.Conflict(new SubscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Already subscribed to this repository"
                });
            }

            var dbContext = context as DbContext;
            if (dbContext is null)
            {
                return Results.Json(new SubscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                }, statusCode: StatusCodes.Status500InternalServerError);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var subscription = new UserSubscription
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    RepositoryId = request.RepositoryId
                };

                context.UserSubscriptions.Add(subscription);
                await context.SaveChangesAsync();

                await dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE Repositories SET SubscriptionCount = SubscriptionCount + 1 WHERE Id = {0}",
                    request.RepositoryId);

                await transaction.CommitAsync();

                return Results.Ok(new SubscriptionResponse
                {
                    Success = true,
                    SubscriptionId = subscription.Id
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
            return Results.Json(new SubscriptionResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }


    /// <summary>
    /// Remove subscription
    /// Atomically decrement repository subscription count
    /// </summary>
    [HttpDelete("{repositoryId}")]
    public async Task<IResult> RemoveSubscriptionAsync(string repositoryId, [FromQuery] string userId)
    {
        try
        {
            var subscription = await context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RepositoryId == repositoryId);

            if (subscription is null)
            {
                return Results.NotFound(new SubscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Subscription record does not exist"
                });
            }

            var dbContext = context as DbContext;
            if (dbContext is null)
            {
                return Results.Json(new SubscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                }, statusCode: StatusCodes.Status500InternalServerError);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                context.UserSubscriptions.Remove(subscription);
                await context.SaveChangesAsync();

                await dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE Repositories SET SubscriptionCount = CASE WHEN SubscriptionCount > 0 THEN SubscriptionCount - 1 ELSE 0 END WHERE Id = {0}",
                    repositoryId);

                await transaction.CommitAsync();

                return Results.Ok(new SubscriptionResponse
                {
                    Success = true,
                    SubscriptionId = subscription.Id
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
            return Results.Json(new SubscriptionResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }


    /// <summary>
    /// Check subscription status
    /// </summary>
    [HttpGet("{repositoryId}/status")]
    public async Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(
        string repositoryId,
        [FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new SubscriptionStatusResponse
            {
                IsSubscribed = false,
                SubscribedAt = null
            };
        }

        var subscription = await context.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.RepositoryId == repositoryId);

        return new SubscriptionStatusResponse
        {
            IsSubscribed = subscription is not null,
            SubscribedAt = subscription?.CreatedAt
        };
    }

    /// <summary>
    /// Get user subscription list
    /// </summary>
    [HttpGet("/")]
    public async Task<SubscriptionListResponse> GetUserSubscriptionsAsync(
        [FromQuery] string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var total = await context.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .CountAsync();

        var subscriptions = await context.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Repository)
            .ToListAsync();

        var items = subscriptions
            .Where(s => s.Repository is not null)
            .Select(s => new SubscriptionItemResponse
            {
                SubscriptionId = s.Id,
                RepositoryId = s.RepositoryId,
                RepoName = s.Repository!.RepoName,
                OrgName = s.Repository.OrgName,
                Description = null,
                StarCount = s.Repository.StarCount,
                ForkCount = s.Repository.ForkCount,
                SubscriptionCount = s.Repository.SubscriptionCount,
                SubscribedAt = s.CreatedAt
            })
            .ToList();

        return new SubscriptionListResponse
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
