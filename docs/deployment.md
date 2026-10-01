# Deployment

## Running locally

```bash
dotnet run --project src/UrlShortener.Api
```

The `http` launch profile listens on `http://localhost:5000`.

## Configuration

| Key                                 | `appsettings.json` | `appsettings.Development.json` | Description                                          |
|-------------------------------------|--------------------|--------------------------------|------------------------------------------------------|
| `ConnectionStrings:UrlShortener`    | — (required)       | `Data Source=urlshortener.db`  | Database connection string                           |
| `Database:Provider`                 | `Sqlite`           | `Sqlite`                       | Database provider                                    |
| `Database:ApplyMigrationsOnStartup` | `false`            | `true`                         | Apply pending EF Core migrations when the API starts |

Any key can be overridden with an environment variable, using `__` as the section separator
(for example `ConnectionStrings__UrlShortener`).

## Migrations

Migrations live in `src/UrlShortener.Infrastructure/Persistence/Migrations/<Provider>`. The
`dotnet-ef` tool is pinned in `dotnet-tools.json`:

```bash
dotnet tool restore
```

Add a migration (SQLite):

```bash
dotnet ef migrations add <Name> --project src/UrlShortener.Infrastructure --startup-project src/UrlShortener.Api --context SqliteUrlShortenerDbContext --output-dir Persistence/Migrations/Sqlite
```

Apply migrations manually instead of on startup:

```bash
dotnet ef database update --project src/UrlShortener.Infrastructure --startup-project src/UrlShortener.Api --context SqliteUrlShortenerDbContext
```

The design-time factory uses `Data Source=urlshortener.db` unless the
`URLSHORTENER_DESIGN_CONNECTION` environment variable points to another database.
