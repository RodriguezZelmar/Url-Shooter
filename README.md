# URL Shortener API

REST service that turns long URLs into short codes (`https://ejemplo.com/un/url/muy/largo` →
`http://localhost:5000/a8F2k`) and redirects visitors back to the original address.

> **Status:** work in progress. This README only describes what is implemented so far.

## Tech stack

- C# / .NET 10 (LTS)
- ASP.NET Core Web API (controllers), ProblemDetails
- OpenAPI (Microsoft.AspNetCore.OpenApi) + Swagger UI
- Entity Framework Core 10
- SQLite (local development)
- xUnit

## Solution structure

```
UrlShortener.slnx
├── src/
│   ├── UrlShortener.Domain/          Entities and domain rules. No dependencies.
│   ├── UrlShortener.Application/     Use cases, DTOs, interfaces implemented by Infrastructure.
│   ├── UrlShortener.Infrastructure/  Persistence and other technical implementations.
│   └── UrlShortener.Api/             HTTP layer: controllers, configuration, composition root.
├── tests/
│   ├── UrlShortener.UnitTests/         Domain and application logic.
│   └── UrlShortener.IntegrationTests/  HTTP endpoints end to end.
└── docs/                             Architecture, API and deployment documentation.
```

See [docs/architecture.md](docs/architecture.md) for the responsibilities of each layer.

## Domain model

| Entity     | Purpose                                                                                     |
|------------|---------------------------------------------------------------------------------------------|
| `ShortUrl` | A short code mapped to an original URL, with creation date, optional expiration, active flag and an optional `CreatedBy` label |
| `UrlVisit` | One successful redirection (date, user agent and referrer origin), used for statistics       |

The database id of a `ShortUrl` is never exposed: clients only see the short code, which is generated
independently. No IP addresses are stored (see [Privacy](docs/architecture.md#privacy)).

## Database

EF Core with SQLite for local development. In the `Development` environment the API applies pending
migrations on startup and creates `urlshortener.db` next to the API project.

## Requirements

- .NET SDK 10 (the SDK band is pinned in [`global.json`](global.json))

## Running locally

```bash
dotnet run --project src/UrlShortener.Api --launch-profile http
```

The API listens on `http://localhost:5000`; Swagger UI opens at `http://localhost:5000/swagger`,
where every endpoint can be tried.

## Usage

```bash
curl -i -X POST http://localhost:5000/api/urls -H "Content-Type: application/json" -d '{"url":"https://example.com/a/very/long/url","expiresAt":"2026-12-31T23:59:59Z"}'
```

```http
HTTP/1.1 201 Created
Location: http://localhost:5000/api/urls/Pr3s5R8

{
  "shortCode": "Pr3s5R8",
  "shortUrl": "http://localhost:5000/Pr3s5R8",
  "originalUrl": "https://example.com/a/very/long/url",
  "createdAt": "2026-10-01T21:32:00.2960632Z",
  "expiresAt": "2026-12-31T23:59:59Z",
  "isActive": true,
  "createdBy": null
}
```

Opening the short URL redirects to the original address:

```bash
curl -i http://localhost:5000/Pr3s5R8
```

```http
HTTP/1.1 302 Found
Location: https://example.com/a/very/long/url
Cache-Control: no-store
```

The redirect is a temporary `302` so that every visit reaches the service and links can be
deactivated later (see [the decision](docs/architecture.md#redirect-status-code-302-found)).

See [docs/api.md](docs/api.md) for every endpoint, field rule and error.

## Short codes

- **Random and independent of the database id**: 7 Base62 characters (`A-Z a-z 0-9`, safe in any
  URL) chosen with a cryptographically secure generator. 62⁷ ≈ 3.5 trillion combinations, so codes
  cannot be guessed or enumerated.
- **Configurable length** (`UrlShortener:ShortCodeLength`, 4–32).
- **Collision handling**: if a generated code already exists, a new one is generated, up to
  `UrlShortener:MaxGenerationAttempts` times. The existence check is only an optimisation — the
  **unique index on `ShortCode`** is what guarantees uniqueness, including between concurrent requests.
- **Custom codes**: clients may request their own code (`customCode`); `409 Conflict` if it is taken.

## Configuration

| Key                                   | Default                 | Description                                              |
|---------------------------------------|-------------------------|----------------------------------------------------------|
| `UrlShortener:BaseUrl`                | `http://localhost:5000` | Public address used to build short URLs (the only place it is configured) |
| `UrlShortener:ShortCodeLength`        | `7`                     | Length of generated codes (4–32)                          |
| `UrlShortener:MaxGenerationAttempts`  | `5`                     | Retries when a generated code collides (1–20)             |
| `UrlShortener:DefaultLifetime`        | `null` (never expires)  | Lifetime (`d.hh:mm:ss`) for links created without `expiresAt` |
| `Swagger:Enabled`                     | `false` (`true` in Development) | Exposes `/swagger` and `/openapi/v1.json`         |

Invalid values stop the application at startup with a descriptive error. Database settings are
described in [docs/deployment.md](docs/deployment.md#configuration).

## Build and test

```bash
dotnet build
```

```bash
dotnet test
```

## Roadmap

- [x] 1. Solution and architecture
- [x] 2. Domain model and database
- [x] 3. URL creation and short code generation
- [x] 4. Redirection
- [ ] 5. Expiration
- [ ] 6. Visit tracking
- [ ] 7. Statistics
- [ ] 8. Administrative listing (pagination and filters)
- [ ] 9. Rate limiting
- [ ] 10. Unit tests
- [ ] 11. Integration tests
- [ ] 12. PostgreSQL
- [ ] 13. Docker
- [ ] 14. Architecture, documentation and quality review
