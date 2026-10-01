# Architecture

## Layers

```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure
```

| Project                       | Responsibility                                                                 | Depends on            |
|-------------------------------|--------------------------------------------------------------------------------|-----------------------|
| `UrlShortener.Domain`         | Entities and the rules that protect them. No framework dependencies.           | —                     |
| `UrlShortener.Application`    | Use cases, DTOs and the interfaces they need (persistence, code generation…).  | Domain                |
| `UrlShortener.Infrastructure` | Implementations of the Application interfaces (EF Core, random generation…).    | Application           |
| `UrlShortener.Api`            | HTTP: controllers, status codes, configuration. Composition root (DI wiring).   | Application, Infrastructure |

Dependencies point inwards. The Domain and Application layers do not know about HTTP or EF Core,
so they can be reused from any host (another Web API, a worker, a console application).

## Why four projects and not one

The service is small, but it is meant to be embedded into larger systems later (marketing,
notifications, e-commerce…). Keeping the business logic in `Application`/`Domain`, with no
reference to ASP.NET Core, is what makes that reuse possible: a host only needs to reference
`Application` + `Infrastructure` and register their services.

It is deliberately **not** a microservice framework: there is no message bus, no CQRS library and
no mediator. Plain services and interfaces are enough at this size.

## Domain model

`ShortUrl` and `UrlVisit` are plain classes with private setters: they can only be created through
factory methods (`ShortUrl.Create`, `UrlVisit.Record`) that validate their input.

The validation rules live in one place, `ShortUrlRules`. Each rule returns an error message (or
`null`), so the same code is used twice:

1. by the application layer, to return a friendly `400 Bad Request` listing every error;
2. by the entity itself, which throws `DomainValidationException` if a caller skipped step 1.

### Identifiers

`ShortUrl.Id` is an auto-incremented `long`, used only inside the database (primary key and
foreign key of `UrlVisit`, cheap to index and join). It is never returned by the API. The public
identifier is `ShortCode`, generated independently, so it does not reveal how many links exist and
does not let anyone enumerate them.

### Dates

All dates are UTC `DateTime` values. `DateTimeOffset` was avoided because the SQLite provider cannot
translate comparisons or ordering on it, and the service needs both (expiration filters, statistics
by day). A value converter marks dates read from SQLite as UTC.

### Privacy

`UrlVisit` stores the visit date, the user agent (truncated to 512 characters) and only the
**origin** of the referrer (`https://news.example.com`, never the full URL, whose path or query
string may contain personal data). IP addresses are not stored: none of the statistics offered by
the service need them.

## Database

| Table       | Index                     | Why                                                                     |
|-------------|---------------------------|-------------------------------------------------------------------------|
| `ShortUrls` | `ShortCode` **unique**    | Lookup on every redirect; final guarantee against duplicate codes.      |
| `ShortUrls` | `ExpiresAt`               | Filtering links by expiration.                                          |
| `ShortUrls` | `CreatedAt`               | Ordering the administrative listing.                                    |
| `UrlVisits` | `(ShortUrlId, VisitedAt)` | Statistics filter by link and group by date. The leading column also serves the foreign key. |

`UrlVisits.ShortUrlId` is a foreign key with cascade delete. Expired links are **not** deleted,
so their history is kept.

### One model, one migration set per provider

`UrlShortenerDbContext` holds the provider-agnostic model. Each provider has a tiny derived context
(`SqliteUrlShortenerDbContext`) whose only purpose is to own its migration set
(`Persistence/Migrations/Sqlite`), because migrations contain provider-specific column types.
