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
