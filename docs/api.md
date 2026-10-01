# API

Base address in local development: `http://localhost:5000`. The interactive documentation
(Swagger UI) is at `/swagger` and the OpenAPI document at `/openapi/v1.json` when
`Swagger:Enabled` is `true` (the default in `Development`).

All request and response bodies are JSON. Dates are ISO 8601; responses are always UTC (`Z`).

## Endpoints

| Method | Path                        | Description                  |
|--------|-----------------------------|------------------------------|
| POST   | `/api/urls`                 | Create a short URL           |
| GET    | `/api/urls/{shortCode}`     | Get the details of a short URL |
| GET    | `/api/urls/{shortCode}/stats` | Usage statistics of a short URL |
| GET    | `/api/urls`                 | List short URLs (paging + filters) |
| PATCH  | `/api/urls/{shortCode}`     | Activate or deactivate a short URL |
| GET    | `/{shortCode}`              | Redirect to the original URL |

---

### POST /api/urls

Creates a short URL. A random code is generated unless `customCode` is supplied.

**Request body**

| Field        | Type              | Required | Rules                                                                 |
|--------------|-------------------|----------|-----------------------------------------------------------------------|
| `url`        | string            | yes      | Absolute `http`/`https` URL, max 2048 characters, no embedded credentials (`user:pass@`), must not point to this service |
| `expiresAt`  | date-time         | no       | Must be in the future. Any offset is accepted and stored as UTC      |
| `customCode` | string            | no       | 4–32 letters (`A-Z`, `a-z`) and digits; not a reserved word (`swagger`, `openapi`, `health`) |
| `createdBy`  | string            | no       | Free-form label of the creating system/user, 1–100 characters        |

```json
{
  "url": "https://example.com/a/very/long/url",
  "expiresAt": "2026-12-31T23:59:59Z"
}
```

**201 Created** — `Location: /api/urls/{shortCode}`

```json
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

When `expiresAt` is omitted, the link never expires — unless `UrlShortener:DefaultLifetime` is
configured, in which case it expires that long after creation.

**Errors**

| Status | When                                                                 |
|--------|----------------------------------------------------------------------|
| 400    | Missing/invalid URL, forbidden scheme (`javascript:`, `data:`, `ftp:`…), expiration in the past, invalid or reserved custom code |
| 409    | `customCode` is already used by another link                         |
| 500    | A unique code could not be generated after `MaxGenerationAttempts` tries (should never happen with a sensible code length) |

---

### GET /api/urls/{shortCode}

Returns the same body as the creation response.

| Status | When                                     |
|--------|------------------------------------------|
| 200    | The link exists (active, inactive or expired) |
| 400    | `shortCode` has an invalid format         |
| 404    | No link with that code                    |

---

### GET /api/urls/{shortCode}/stats

Usage statistics. Available for active, inactive and expired links.

| Query  | Type              | Default                    | Rules                                  |
|--------|-------------------|----------------------------|----------------------------------------|
| `from` | date (yyyy-MM-dd) | 29 days before `to`        | First UTC day of the period, inclusive |
| `to`   | date (yyyy-MM-dd) | today (UTC)                | Last UTC day of the period, inclusive  |

The period is at most 366 days and `from` must not be after `to`.

```http
GET /api/urls/redir01/stats?from=2026-09-30&to=2026-10-01
```

**200 OK**

```json
{
  "shortCode": "redir01",
  "originalUrl": "https://example.com/x?y=1",
  "createdAt": "2026-10-01T21:35:19.4056054Z",
  "expiresAt": null,
  "isActive": true,
  "isExpired": false,
  "totalVisits": 42,
  "lastVisitAt": "2026-10-01T21:41:38.5096592Z",
  "from": "2026-09-30",
  "to": "2026-10-01",
  "visitsByDay": [
    { "date": "2026-09-30", "visits": 12 },
    { "date": "2026-10-01", "visits": 30 }
  ],
  "topReferrers": [
    { "referrer": "https://news.example.com", "visits": 25 },
    { "referrer": "https://other.example.org", "visits": 3 }
  ]
}
```

| Field                 | Covers                                                                 |
|-----------------------|------------------------------------------------------------------------|
| `totalVisits`, `lastVisitAt` | The whole life of the link                                      |
| `visitsByDay`         | The period; UTC days, ascending; days without visits are omitted       |
| `topReferrers`        | The period; up to 5 referrer **origins**, most visits first            |

| Status | When                                                     |
|--------|----------------------------------------------------------|
| 200    | Statistics returned                                      |
| 400    | Malformed short code, unparsable date, `from` > `to`, or period longer than 366 days |
| 404    | No link with that code                                   |

---

### GET /{shortCode}

The public short link. Resolves the code and redirects the visitor.

```http
GET /Pr3s5R8 HTTP/1.1

HTTP/1.1 302 Found
Location: https://example.com/a/very/long/url
Cache-Control: no-store
```

| Status | When                                                                                       |
|--------|--------------------------------------------------------------------------------------------|
| 302    | The link exists, is active and has not expired. `Location` holds the original URL. The visit is recorded |
| 404    | Unknown code, malformed code (only 4–32 letters and digits match the route) or deactivated link |
| 410    | The link has expired (`expiresAt` is in the past)                                          |

**Why `302 Found`.** A permanent redirect (`301`/`308`) is cached by browsers, so repeat visits
would never reach the service: they would not be counted and a later deactivation or expiration
would be ignored by those browsers. `302` and `307` are both temporary; for `GET` they behave the
same, and `302` is the status every client and crawler understands. `Cache-Control: no-store` makes
the "do not cache" intent explicit for proxies.

A deactivated link answers `404`, exactly like a missing one, so visitors learn nothing about it.

### Expired links

A link is expired from the exact `expiresAt` instant onwards. Requests to an expired link get
`410 Gone` instead of a redirect:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.11",
  "title": "Short URL expired.",
  "status": 410,
  "detail": "The short URL 'promo2026' expired at 2026-12-31T23:59:59.0000000Z."
}
```

`410` (rather than `404`) tells the client that the link did exist and will not come back.
Expired links are **not deleted**: `GET /api/urls/{shortCode}` still returns them and their
history is preserved.

Swagger UI lists this endpoint, but the browser follows the redirect itself and usually cannot
show the destination (cross-origin). Try it from the address bar or with `curl -i`.

---

### GET /api/urls

Administrative listing, newest first. Query: `page` (default 1), `pageSize` (1–100, default 20),
`shortCode` (prefix, case-sensitive), `status` (`Active`|`Inactive`), `expired` (`true`|`false`),
`createdBy` (exact). Response: `{ items, page, pageSize, totalCount, totalPages }`. `400` on invalid paging.

### PATCH /api/urls/{shortCode}

Body `{ "isActive": false }` disables the link (redirect answers `404`, statistics are kept);
`true` enables it again. Returns `200` with the link, `400` on invalid input, `404` if missing.

## Errors

Every error uses the `ProblemDetails` format (RFC 9457) with content type
`application/problem+json`. Validation errors (400) list every problem per field:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "url": ["Only http and https URLs are allowed."],
    "expiresAt": ["The expiration date must be in the future."]
  },
  "traceId": "00-f34244761adf33a1dcbe457f8ed2f67d-4d0bb8ecf049861f-00"
}
```

Other errors carry a `title` and, when useful, a `detail`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Short code already in use.",
  "status": 409,
  "detail": "The short code 'promo2026' is already in use."
}
```

Unexpected errors return `500` with a generic message; details are only written to the server log.
