using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UrlShortener.Infrastructure.Persistence;

/// <summary>Applies pending migrations at startup when <see cref="DatabaseOptions.ApplyMigrationsOnStartup"/> is enabled.</summary>
public sealed class DatabaseInitializer(
    UrlShortenerDbContext dbContext,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        logger.LogInformation("Applying database migrations ({Provider})", options.Value.Provider);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
