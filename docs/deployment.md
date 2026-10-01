# Deployment

## Running locally

```bash
dotnet run --project src/UrlShortener.Api
```

The `http` launch profile listens on `http://localhost:5000` and opens Swagger UI.

In any environment other than `Development`, `ConnectionStrings:UrlShortener` and
`UrlShortener:BaseUrl` must be set to real values; the API refuses to start with invalid settings.

## Configuration

| Key                                 | `appsettings.json` | `appsettings.Development.json` | Description                                          |
|-------------------------------------|--------------------|--------------------------------|------------------------------------------------------|
| `ConnectionStrings:UrlShortener`    | — (required)       | `Data Source=urlshortener.db`  | Database connection string                           |
| `Database:Provider`                 | `Sqlite`           | `Sqlite`                       | Database provider                                    |
| `Database:ApplyMigrationsOnStartup` | `false`            | `true`                         | Apply pending EF Core migrations when the API starts |
| `UrlShortener:BaseUrl`             | `http://localhost:5000` | —                             | Public base address used to build short URLs         |
| `UrlShortener:ShortCodeLength`      | `7`                | —                              | Length of generated short codes (4–32)               |
| `UrlShortener:MaxGenerationAttempts`| `5`                | —                              | Retries when a generated code collides (1–20)        |
| `UrlShortener:DefaultLifetime`      | `null`             | —                              | Lifetime for links created without `expiresAt` (`d.hh:mm:ss`) |
| `Swagger:Enabled`                   | `false`            | `true`                         | Exposes Swagger UI (`/swagger`) and `/openapi/v1.json` |

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
