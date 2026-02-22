using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Infrastructure;

/// <summary>
/// Database initialization service
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initialize database (create default roles and OAuth providers)
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Ensure database has been created
        if (context is DbContext dbContext)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        // Initialize default roles
        await InitializeRolesAsync(context);

        // Initialize default admin account
        await InitializeAdminUserAsync(context);

        // Initialize OAuth providers
        await InitializeOAuthProvidersAsync(context);

        // Initialize system settings defaults (only created from environment variables on first run)
        await SystemSettingDefaults.InitializeDefaultsAsync(configuration, context);
    }

    private static async Task InitializeAdminUserAsync(IContext context)
    {
        const string adminEmail = "admin@routin.ai";
        const string adminPassword = "Admin@123";

        var exists = await context.Users.AnyAsync(u => u.Email == adminEmail && !u.IsDeleted);
        if (exists) return;

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && !r.IsDeleted);
        if (adminRole == null) return;

        var adminUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = "admin",
            Email = adminEmail,
            Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Status = 1,
            IsSystem = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);

        var userRole = new UserRole
        {
            Id = Guid.NewGuid().ToString(),
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();
    }

    private static async Task InitializeRolesAsync(IContext context)
    {
        var roles = new[]
        {
            new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Admin",
                Description = "System administrator",
                IsActive = true,
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = "User",
                Description = "Regular user",
                IsActive = true,
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var role in roles)
        {
            var exists = await context.Roles.AnyAsync(r => r.Name == role.Name && !r.IsDeleted);
            if (!exists)
            {
                context.Roles.Add(role);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task InitializeOAuthProvidersAsync(IContext context)
    {
        var providers = new[]
        {
            new OAuthProvider
            {
                Id = Guid.NewGuid().ToString(),
                Name = "github",
                DisplayName = "GitHub",
                AuthorizationUrl = "https://github.com/login/oauth/authorize",
                TokenUrl = "https://github.com/login/oauth/access_token",
                UserInfoUrl = "https://api.github.com/user",
                ClientId = "YOUR_GITHUB_CLIENT_ID",
                ClientSecret = "YOUR_GITHUB_CLIENT_SECRET",
                RedirectUri = "http://localhost:8080/api/oauth/github/callback",
                Scope = "user:email",
                UserInfoMapping = "{\"id\":\"id\",\"name\":\"login\",\"email\":\"email\",\"avatar\":\"avatar_url\"}",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            },
            new OAuthProvider
            {
                Id = Guid.NewGuid().ToString(),
                Name = "gitee",
                DisplayName = "Gitee",
                AuthorizationUrl = "https://gitee.com/oauth/authorize",
                TokenUrl = "https://gitee.com/oauth/token",
                UserInfoUrl = "https://gitee.com/api/v5/user",
                ClientId = "YOUR_GITEE_CLIENT_ID",
                ClientSecret = "YOUR_GITEE_CLIENT_SECRET",
                RedirectUri = "http://localhost:8080/api/oauth/gitee/callback",
                Scope = "user_info emails",
                UserInfoMapping = "{\"id\":\"id\",\"name\":\"name\",\"email\":\"email\",\"avatar\":\"avatar_url\"}",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var provider in providers)
        {
            var exists = await context.OAuthProviders.AnyAsync(p => p.Name == provider.Name && !p.IsDeleted);
            if (!exists)
            {
                context.OAuthProviders.Add(provider);
            }
        }

        await context.SaveChangesAsync();
    }
}
