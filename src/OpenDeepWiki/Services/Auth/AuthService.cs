using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Models.Auth;

namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly IContext _context;
    private readonly IJwtService _jwtService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(IContext context, IJwtService jwtService, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwtService = jwtService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password
        if (!VerifyPassword(request.Password, user.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.Status != 1)
        {
            throw new UnauthorizedAccessException("Account is disabled or pending verification");
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Get user roles
        var roles = await GetUserRolesAsync(user.Id);

        // Generate JWT token
        var token = _jwtService.GenerateToken(user, roles);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = _jwtOptions.ExpirationMinutes * 60,
            User = new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Avatar = user.Avatar ?? $"https://api.dicebear.com/7.x/notionists/svg?seed={Uri.EscapeDataString(user.Name)}",
                Roles = roles
            }
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already registered");
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Email = request.Email,
            Password = HashPassword(request.Password),
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Assign default role (User)
        var defaultRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "User" && !r.IsDeleted);

        if (defaultRole != null)
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                RoleId = defaultRole.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);
        }

        await _context.SaveChangesAsync();

        // Generate JWT token
        var roles = await GetUserRolesAsync(user.Id);
        var token = _jwtService.GenerateToken(user, roles);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = _jwtOptions.ExpirationMinutes * 60,
            User = new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Avatar = user.Avatar ?? $"https://api.dicebear.com/7.x/notionists/svg?seed={Uri.EscapeDataString(user.Name)}",
                Roles = roles
            }
        };
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            return null;
        }

        var roles = await GetUserRolesAsync(userId);

        return new UserInfo
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Avatar = user.Avatar ?? $"https://api.dicebear.com/7.x/notionists/svg?seed={Uri.EscapeDataString(user.Name)}",
            Roles = roles
        };
    }

    private async Task<List<string>> GetUserRolesAsync(string userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId && !ur.IsDeleted)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();
    }

    private static string HashPassword(string password)
    {
        // Hash password using BCrypt
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
