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
