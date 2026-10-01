namespace UrlShortener.Infrastructure.Persistence;

public enum DatabaseProvider
{
    Sqlite,
}

/// <summary>Bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Name of the entry under <c>ConnectionStrings</c>.</summary>
    public const string ConnectionStringName = "UrlShortener";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;

    /// <summary>
    /// Applies pending migrations when the application starts. Convenient for development and
    /// single-instance deployments; see docs/deployment.md for when to turn it off.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
