using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UrlShortener.Infrastructure.Persistence;

// Used only by the `dotnet ef` tooling to create migrations; the running API never uses them.
// Generating a migration does not open a connection, so the defaults are placeholders.
// Set URLSHORTENER_DESIGN_CONNECTION to point the tooling at a real database (e.g. `dotnet ef database update`).

public sealed class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteUrlShortenerDbContext>
{
    public SqliteUrlShortenerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("URLSHORTENER_DESIGN_CONNECTION")
            ?? "Data Source=urlshortener.db";

        var options = new DbContextOptionsBuilder<SqliteUrlShortenerDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new SqliteUrlShortenerDbContext(options);
    }
}
