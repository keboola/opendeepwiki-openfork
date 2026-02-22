using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Recommendation;

namespace OpenDeepWiki.Services.Recommendation;

/// <summary>
/// Recommendation API service
/// </summary>
[MiniApi(Route = "/api/v1/recommendations")]
[Tags("Recommendations")]
public class RecommendationApiService(RecommendationService recommendationService)
{
    /// <summary>
    /// Get recommended repository list
    /// </summary>
    [HttpGet]
    public async Task<RecommendationResponse> GetRecommendationsAsync(
        [FromQuery] string? userId,
        [FromQuery] string? strategy,
        [FromQuery] string? language,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 20;
        if (limit > 100) limit = 100;

        var request = new RecommendationRequest
        {
            UserId = userId,
            Limit = limit,
            Strategy = strategy ?? "default",
            LanguageFilter = language
        };

        return await recommendationService.GetRecommendationsAsync(request, cancellationToken);
    }

    /// <summary>
    /// Get popular repositories
    /// </summary>
    [HttpGet("/popular")]
    public async Task<RecommendationResponse> GetPopularReposAsync(
        [FromQuery] string? language,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 20;
        if (limit > 100) limit = 100;

        var request = new RecommendationRequest
        {
            UserId = null,
            Limit = limit,
            Strategy = "popular",
            LanguageFilter = language
        };

        return await recommendationService.GetRecommendationsAsync(request, cancellationToken);
    }

    /// <summary>
    /// Get available programming languages list
    /// </summary>
    [HttpGet("/languages")]
    public async Task<AvailableLanguagesResponse> GetAvailableLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        return await recommendationService.GetAvailableLanguagesAsync(cancellationToken);
    }

    /// <summary>
    /// Record user activity
    /// </summary>
    [HttpPost("/activity")]
    public async Task<RecordActivityResponse> RecordActivityAsync(
        [FromBody] RecordActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return new RecordActivityResponse
            {
                Success = false,
                ErrorMessage = "User ID cannot be empty"
            };
        }

        if (string.IsNullOrWhiteSpace(request.ActivityType))
        {
            return new RecordActivityResponse
            {
                Success = false,
                ErrorMessage = "Activity type cannot be empty"
            };
        }

        var success = await recommendationService.RecordActivityAsync(request, cancellationToken);

        return new RecordActivityResponse
        {
            Success = success,
            ErrorMessage = success ? null : "Failed to record activity"
        };
    }


    /// <summary>
    /// Mark repository as not interested
    /// </summary>
    [HttpPost("/dislike")]
    public async Task<DislikeResponse> MarkAsDislikedAsync(
        [FromBody] DislikeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return new DislikeResponse
            {
                Success = false,
                ErrorMessage = "User ID cannot be empty"
            };
        }

        if (string.IsNullOrWhiteSpace(request.RepositoryId))
        {
            return new DislikeResponse
            {
                Success = false,
                ErrorMessage = "Repository ID cannot be empty"
            };
        }

        var success = await recommendationService.MarkAsDislikedAsync(request, cancellationToken);

        return new DislikeResponse
        {
            Success = success,
            ErrorMessage = success ? null : "Failed to mark"
        };
    }

    /// <summary>
    /// Remove not-interested mark
    /// </summary>
    [HttpDelete("/dislike/{repositoryId}")]
    public async Task<IResult> RemoveDislikeAsync(
        string repositoryId,
        [FromQuery] string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.BadRequest(new { error = "User ID cannot be empty" });
        }

        var success = await recommendationService.RemoveDislikeAsync(userId, repositoryId, cancellationToken);

        return success
            ? Results.Ok(new { success = true })
            : Results.Json(new { success = false, error = "Failed to remove" }, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Refresh user preference cache
    /// </summary>
    [HttpPost("/refresh-preference/{userId}")]
    public async Task<IResult> RefreshUserPreferenceAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.BadRequest(new { error = "User ID cannot be empty" });
        }

        await recommendationService.UpdateUserPreferenceCacheAsync(userId, cancellationToken);

        return Results.Ok(new { success = true, message = "User preference cache refreshed" });
    }
}
