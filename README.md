# URL Shortener API

REST service that turns long URLs into short codes (`https://ejemplo.com/un/url/muy/largo` →
`http://localhost:5000/a8F2k`) and redirects visitors back to the original address.

> **Status:** work in progress. This README only describes what is implemented so far.

## Tech stack

- C# / .NET 10 (LTS)
- ASP.NET Core Web API (controllers)
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
- [ ] 3. URL creation and short code generation
- [ ] 4. Redirection
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
