# Requirement: Scalable URL Shortener Service

> High-level requirement: Build a scalable URL shortener service with APIs, persistence, and analytics.
>
> Mode: greenfield | Stack: Visual Studio 2022 solution, C# / .NET 8, ASP.NET Core Web API

## 1. Problem Statement
Build a .NET 8 URL shortener that:
- turns long URLs into short codes;
- redirects visitors with low latency;
- stores the URL mappings and click events;
- gives URL owners analytics through versioned REST APIs.

The workload is read-heavy: redirects far outnumber creates.

## 2. Functional Requirements

### REQ-1 Create a short URL (must)
- AC-1.1: Given a valid http/https URL, when the client calls POST /api/v1/urls, then the service returns 201 Created with shortCode, shortUrl, originalUrl, createdAt and expiresAt.
- AC-1.2: Given an invalid or malformed URL, or a scheme other than http/https, when the client creates it, then the service returns 400 with a ProblemDetails body.
- AC-1.3: Generated codes are unique, 7 characters long, and Base62 encoded.
- AC-1.4: Given a repeat request with the same Idempotency-Key, when it is submitted, then the service returns the original result and creates no duplicate.

### REQ-2 Custom alias (should)
- AC-2.1: Given an alias that is available and valid (3-30 characters, [A-Za-z0-9-]), when the client creates a URL with it, then the alias becomes the short code.
- AC-2.2: An alias that is already taken returns 409 Conflict. Reserved words (api, admin, health, swagger) return 400.

### REQ-3 Redirect (must)
- AC-3.1: Given an active code, when a visitor calls GET /{code}, then the service returns 302 Found with Location set to the original URL.
- AC-3.2: An unknown code returns 404. An expired or disabled code returns 410 Gone.
- AC-3.3: Every redirect records a click event asynchronously, without blocking the redirect.

### REQ-4 Expiry (should)
- AC-4.1: The optional expiresAt must be a future UTC timestamp; otherwise the service returns 400.
- AC-4.2: After expiresAt has passed, a redirect returns 410.

### REQ-5 Manage URLs (must)
- AC-5.1: GET /api/v1/urls/{code} returns the URL metadata.
- AC-5.2: GET /api/v1/urls?page=&pageSize= returns a paginated list of the caller's URLs.
- AC-5.3: PATCH /api/v1/urls/{code} can update expiresAt and isActive. DELETE soft-deletes the URL, after which a redirect returns 410.
- AC-5.4: A caller who tries to manage another owner's URL receives 403 Forbidden.

### REQ-6 Analytics (must)
- AC-6.1: Each click records timestamp and code, referrer, parsed user agent (device, browser, OS), and country. The IP address is hashed or anonymized and never stored raw.
- AC-6.2: GET /api/v1/urls/{code}/analytics?from=&to=&granularity=hour|day returns total clicks, unique clicks, and a time series.
- AC-6.3: The response includes top-N breakdowns by referrer, country, device, and browser.
- AC-6.4: Analytics are eventually consistent, lagging real time by no more than 60 seconds.

### REQ-7 Authentication (must)
- AC-7.1: The management and analytics APIs require an X-Api-Key header or JWT bearer token. Requests without one get 401.
- AC-7.2: The redirect endpoint is public.

### REQ-8 Rate limiting and abuse protection (must)
- AC-8.1: URL creation is rate-limited per API key (default: 100 per minute). Requests over the limit get 429 with Retry-After.
- AC-8.2: Destination URLs on a configurable domain blocklist are rejected with 400.
- AC-8.3: URLs that point back to the service's own domain are rejected.

### REQ-9 Observability and health (must)
- AC-9.1: /health/live and /health/ready are available; the readiness check covers the database and cache.
- AC-9.2: Logs are structured (Serilog) and carry a correlation ID.
- AC-9.3: OpenTelemetry metrics cover request count, request latency, and cache hit ratio.

### REQ-10 API documentation and versioning (must)
- AC-10.1: An OpenAPI 3 specification (openapi.yaml) and Swagger UI document every endpoint.
- AC-10.2: All management APIs are versioned under /api/v1.

## 3. Non-Functional Requirements

- NFR-1: Redirect p95 < 50 ms on a cache hit and < 150 ms on a miss; create p95 < 200 ms
- NFR-2: Stateless API that scales horizontally; designed for ~1,000 redirects/sec and ~100 creates/sec
- NFR-3: 99.9% target for redirects; redirect availability should continue if analytics pipeline is down
- NFR-4: No mapping is lost once 201 has been returned; click events are delivered at least once
- NFR-5: Supports 100M+ URLs; raw click events kept 90 days, aggregates kept indefinitely
- NFR-6: OWASP Top 10 mitigated; HTTPS only; input validated; no secrets in code
- NFR-7: Privacy-aware retention and anonymization
- NFR-8: Clean Architecture; >= 80% line coverage on Domain and Application; no analyzer warnings
- NFR-9: Dockerfile plus docker-compose (API, Worker, SQL, Redis); config via appsettings and environment variables

## 4. Technical Constraints
- Visual Studio 2022 17.8+ `.sln`, C# on .NET 8 LTS, ASP.NET Core.
- EF Core on SQL Server; Redis read-through cache.
- Click ingestion through a background queue with `System.Threading.Channels` on local runs and Azure Service Bus for production.
- Tests use xUnit, FluentAssertions, Moq/NSubstitute, `WebApplicationFactory`, and Testcontainers.

## 5. API Summary
- POST /api/v1/urls: create short URL
- GET /api/v1/urls: list URLs
- GET /api/v1/urls/{code}: metadata
- PATCH /api/v1/urls/{code}: update expiry or active flag
- DELETE /api/v1/urls/{code}: soft delete
- GET /api/v1/urls/{code}/analytics: analytics
- GET /{code}: redirect
- GET /health/live and /health/ready: health checks

This requirement replaces the earlier demonstration requirement and is the basis for the approved workflow sign-off. 