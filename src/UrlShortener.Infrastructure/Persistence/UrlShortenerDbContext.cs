using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.ShortUrls;
using UrlShortener.Domain.Visits;
using UrlShortener.Infrastructure.Persistence.Converters;

namespace UrlShortener.Infrastructure.Persistence;

/// <summary>
/// Provider-agnostic model. Each database provider has a small derived context
/// (<see cref="SqliteUrlShortenerDbContext"/>, ...) that only exists so it can own its migration set.
/// </summary>
public abstract class UrlShortenerDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    public DbSet<UrlVisit> UrlVisits => Set<UrlVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UrlShortenerDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every DateTime in the model is UTC; see UtcDateTimeConverter.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
