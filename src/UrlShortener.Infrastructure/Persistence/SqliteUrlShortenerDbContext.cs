using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Infrastructure.Persistence;

/// <summary>SQLite flavour of the model. Its migrations live in <c>Persistence/Migrations/Sqlite</c>.</summary>
public sealed class SqliteUrlShortenerDbContext(DbContextOptions<SqliteUrlShortenerDbContext> options)
    : UrlShortenerDbContext(options);
