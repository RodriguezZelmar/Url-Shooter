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

## Flow: creating a short URL

```
POST /api/urls
  │
  ▼
ShortUrlsController.Create          (Api)  model validation: [Required], [MaxLength] → 400 automatically
  │
  ▼
ShortUrlService.CreateAsync         (Application)
  ├─ validate all business rules (ShortUrlRules + "not pointing to this service" + reserved codes)
  │     └─ any error → RequestValidationException → 400 with every error
  ├─ customCode given?
  │     ├─ yes: exists? → 409 · insert · unique violation? → 409
  │     └─ no:  repeat up to MaxGenerationAttempts:
  │              generate code → reserved or exists? → retry
  │              insert → unique violation (concurrent insert)? → retry
  │           all attempts failed → ShortCodeGenerationException → 500
  └─ ShortUrl.Create(...)            (Domain) guards its invariants again
  │
  ▼
ShortUrlRepository.AddAsync          (Infrastructure) INSERT; translates the provider's
                                       unique-constraint error into DuplicateShortCodeException
  │
  ▼
201 Created + Location: /api/urls/{shortCode}
```

### Why a pre-check *and* a unique index

The `ShortCodeExistsAsync` check avoids a failed insert in the common case, but two requests can
pass it at the same time with the same code. Only the database can decide atomically, so the
unique index on `ShortCode` is the real guarantee and the repository turns its violation into a
retry (generated codes) or a `409` (custom codes). The rejected entity is detached from the
`DbContext` so the retry does not try to insert it again.

### Short code generation

`RandomShortCodeGenerator` uses `RandomNumberGenerator.GetString` over a Base62 alphabet: uniform
(no modulo bias), cryptographically secure, URL-safe without encoding. Codes are not derived from
the database id (an encoded id would be sequential and enumerable). The generator sits behind
`IShortCodeGenerator`, which also lets the tests force collisions deterministically.

Codes are case-sensitive (`abc1` ≠ `ABC1`). Words that would shadow the service's own routes
(`swagger`, `openapi`, `health`) are reserved, compared case-insensitively because ASP.NET Core
matches literal route segments that way.

## Flow: redirection

```
GET /{shortCode}
  │   route constraint: 4–32 letters/digits, otherwise 404 without touching the database
  ▼
RedirectController                  (Api)
  ▼
RedirectService.ResolveAsync        (Application)
  ├─ find by ShortCode (unique index lookup) ── not found → 404
  ├─ inactive?                                ── yes       → 404 (same answer as "not found")
  ├─ expired (ExpiresAt <= now)?            ── yes       → 410 Gone (the link is kept)
  ├─ record UrlVisit (date, user agent, referrer origin) ── failure is logged, never breaks the redirect
  └─ return OriginalUrl
  ▼
302 Found, Location: <OriginalUrl>, Cache-Control: no-store
```

### Redirect status code: 302 Found

| Option | Cached by browsers | Visits counted on repeat | Can be disabled later |
|--------|--------------------|--------------------------|-----------------------|
| 301 / 308 (permanent) | yes, indefinitely | no | not for browsers that cached it |
| 302 / 307 (temporary) | no (unless told to) | yes | yes |

The service needs statistics, expiration and deactivation, so a permanent redirect is ruled out.
Between the temporary ones, `307` only differs in guaranteeing the method is preserved, which is
irrelevant for `GET`; `302` is used because it is the universally understood choice. The decision
is applied in a single place (`RedirectController`).

### Visit tracking

Only successful redirects are recorded (`404`/`410` answers are not visits). The visit is inserted
synchronously before answering; it is a single-row insert on an indexed table, cheap enough at
this scale. If the insert fails, the error is logged and the visitor is still redirected:
statistics are secondary to the link working. A high-traffic deployment could move the insert to a
background queue (for example a `Channel<T>` drained by a hosted service) without changing the
Application API.

### Expiration

`ShortUrl.IsExpired(now)` is the single definition: a link is expired when `ExpiresAt` is set and
`ExpiresAt <= now`. The current time comes from `TimeProvider`, so tests control the clock.

Expiration is evaluated when a link is used; nothing deletes or rewrites expired rows. Keeping
them preserves statistics and history and avoids a background job. Expired links answer
`410 Gone` (they existed and will not come back), while unknown or deactivated links answer `404`.

## Flow: statistics

```
GET /api/urls/{shortCode}/stats?from=&to=
  ▼
UrlStatisticsService.GetAsync        (Application)
  ├─ validate code and period (default: last 30 days, max 366 days)
  ├─ load the link (404 if missing)
  └─ IUrlVisitRepository.GetAggregatesAsync   (Infrastructure, 3 SQL queries)
        1. SELECT COUNT(*), MAX(VisitedAt)            WHERE ShortUrlId = @id
        2. SELECT day(VisitedAt), COUNT(*)            WHERE ShortUrlId = @id AND VisitedAt in period GROUP BY day
        3. SELECT Referrer, COUNT(*) ... ORDER BY COUNT(*) DESC LIMIT 5   (same filter)
```

All counting and grouping happens in the database, so the cost of a request does not depend on
how many visit rows exist in memory — only the aggregated rows (at most 366 days + 5 referrers)
are transferred. Every query starts with `ShortUrlId = @id` and filters `VisitedAt` by range,
which is exactly the shape of the `(ShortUrlId, VisitedAt)` index. Days are UTC calendar days.

The period is bounded (366 days) so a single request cannot ask for an unbounded series.

## Validation layers

1. **API model** — data annotations on `CreateShortUrlRequest` reject obviously malformed input
   before any code runs (`[ApiController]` returns `400` automatically).
2. **Application** — `ShortUrlService` checks every business rule and reports all errors at once.
   It never trusts that step 1 happened, so other hosts can call it directly.
3. **Domain** — `ShortUrl.Create` re-checks its invariants and throws if they are broken.

All three use the constants and rules in `ShortUrlRules`, so limits are defined once.

## Error handling

Use cases throw exceptions that describe business outcomes (`RequestValidationException`,
`ShortUrlNotFoundException`, `ShortCodeConflictException`, …). They know nothing about HTTP.
`ApiExceptionHandler` (an `IExceptionHandler`) is the single place that maps them to status codes
and writes `ProblemDetails`. Unknown exceptions become a generic `500`; their details are logged,
never returned.

## Configuration

`UrlShortenerOptions` (section `UrlShortener`) holds the public `BaseUrl`, code length, retry limit
and optional default lifetime. `BaseUrl` is the only place the service's address is defined; short
URLs are built with `UrlShortenerOptions.BuildShortUrl`. `UrlShortenerOptionsValidator` runs at
startup (`ValidateOnStart`), so a bad configuration fails fast instead of at the first request.

The Application layer does not read configuration itself: the host binds the options. Another
application embedding the service can bind them from any source.

## Logging

Code depends only on `ILogger<T>` (Microsoft.Extensions.Logging). Creation logs the short code and
the destination **host**, not the full URL, because query strings frequently contain tokens or
personal data.
