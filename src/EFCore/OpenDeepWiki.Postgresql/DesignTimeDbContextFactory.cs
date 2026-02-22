using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenDeepWiki.Postgresql;

/// <summary>
/// Design-time database context factory, used for EF Core migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresqlDbContext>
{
    public PostgresqlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgresqlDbContext>();
        // Use a dummy connection string, only for generating migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=opendeepwiki;Username=postgres;Password=postgres");
        return new PostgresqlDbContext(optionsBuilder.Options);
    }
}
