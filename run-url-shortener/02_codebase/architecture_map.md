# Architecture Map

## Scenario
Greenfield; no existing solution was supplied. Stage 2 brownfield inventory is therefore not applicable. The generated solution is in `04_build/`.

## Target architecture
A layered ASP.NET Core Web API with inward dependencies:

```mermaid
graph TD
  Client --> Api[Shortener.Api]
  Api --> App[Application services]
  App --> Domain[Domain models and validation]
  App --> Store[IPersistenceStore]
  Store --> Json[JsonFilePersistenceStore]
  App --> Analytics[Click analytics]
```

```mermaid
sequenceDiagram
  participant C as Client
  participant A as API
  participant S as ShortenerService
  participant P as Persistence
  C->>A: POST /api/v1/links
  A->>S: CreateAsync(request)
  S->>P: SaveLinkAsync(link)
  P-->>S: saved link
  S-->>A: LinkResponse
  A-->>C: 201 Created
```

## Cross-cutting concerns
- Dependency injection wires the service and persistence store.
- ASP.NET Core ProblemDetails handles error shape.
- `ILogger<T>` records operational events without secrets.
- Options pattern configures storage path and public base URL.
- The persistence interface is the replacement seam for PostgreSQL/Redis in production.
