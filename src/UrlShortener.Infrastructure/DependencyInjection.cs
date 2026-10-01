using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Abstractions;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Persistence.Repositories;

namespace UrlShortener.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence. The provider is chosen through configuration (<c>Database:Provider</c>),
    /// so switching databases needs no code change.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName);

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new();
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{DatabaseOptions.ConnectionStringName}' is not configured.");

        switch (databaseOptions.Provider)
        {
            case DatabaseProvider.Sqlite:
                services.AddDbContext<UrlShortenerDbContext, SqliteUrlShortenerDbContext>(
                    options => options.UseSqlite(connectionString));
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider '{databaseOptions.Provider}'.");
        }

        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
