using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.UserProfile;

namespace OpenDeepWiki.Services.UserProfile;

/// <summary>
/// User profile API service
/// </summary>
[MiniApi(Route = "/api/user")]
[Tags("User Profile")]
[Authorize]
public class UserProfileApiService(IUserProfileService profileService, IHttpContextAccessor httpContextAccessor)
{
    private string? GetCurrentUserId()
    {
        return httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut("/profile")]
    public async Task<IResult> UpdateProfileAsync([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var userInfo = await profileService.UpdateProfileAsync(userId, request);
            return Results.Ok(new { success = true, data = userInfo });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPut("/password")]
    public async Task<IResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            await profileService.ChangePasswordAsync(userId, request);
            return Results.Ok(new { success = true, message = "Password changed successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.BadRequest(new { success = false, message = "Current password is incorrect" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get user settings
    /// </summary>
    [HttpGet("/settings")]
    public async Task<IResult> GetSettingsAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var settings = await profileService.GetSettingsAsync(userId);
            return Results.Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Update user settings
    /// </summary>
    [HttpPut("/settings")]
    public async Task<IResult> UpdateSettingsAsync([FromBody] UserSettingsDto request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var settings = await profileService.UpdateSettingsAsync(userId, request);
            return Results.Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Message });
        }
    }
}
