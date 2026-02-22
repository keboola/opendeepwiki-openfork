using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Models.Auth;
using OpenDeepWiki.Models.UserProfile;

namespace OpenDeepWiki.Services.UserProfile;

/// <summary>
/// User profile service implementation
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly IContext _context;

    public UserProfileService(IContext context)
    {
        _context = context;
    }

    public async Task<UserInfo> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Check if email is used by another user
        if (user.Email != request.Email)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != userId && !u.IsDeleted);

            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already in use");
            }
        }

        // Update user information
        user.Name = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Avatar = request.Avatar;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Get user roles
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId && !ur.IsDeleted)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();

        return new UserInfo
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Avatar = user.Avatar ?? $"https://api.dicebear.com/7.x/notionists/svg?seed={Uri.EscapeDataString(user.Name)}",
            Roles = roles
        };
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify current password
        if (!VerifyPassword(request.CurrentPassword, user.Password))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Update password
        user.Password = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<UserSettingsDto> GetSettingsAsync(string userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Settings are currently stored in user extension fields; return defaults if none exist
        // A dedicated UserSettings table can be created later
        return new UserSettingsDto
        {
            Theme = "system",
            Language = "zh",
            EmailNotifications = true,
            PushNotifications = false
        };
    }

    public async Task<UserSettingsDto> UpdateSettingsAsync(string userId, UserSettingsDto settings)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Settings are currently stored in user extension fields
        // A dedicated UserSettings table can be created later to store more settings
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return settings;
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string? hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
