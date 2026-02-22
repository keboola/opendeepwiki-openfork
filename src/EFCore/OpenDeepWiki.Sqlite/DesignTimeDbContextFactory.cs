using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenDeepWiki.Sqlite;

/// <summary>
/// Design-time database context factory, used for EF Core migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        // Use a dummy connection string, only for generating migrations
        optionsBuilder.UseSqlite("Data Source=opendeepwiki.db");
        return new SqliteDbContext(optionsBuilder.Options);
    }
}
