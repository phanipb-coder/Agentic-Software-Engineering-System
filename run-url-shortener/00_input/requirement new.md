# Requirement: Scalable URL Shortener Service

> **High-level requirement:** Build a scalable URL shortener service with APIs, persistence, and analytics.
>
> **Mode:** greenfield | **Stack:** Visual Studio 2022 solution, C# / .NET 8, ASP.NET Core Web API

---

## 1. Problem Statement
Build a .NET 8 URL shortener that:
- turns long URLs into short codes;
- redirects visitors with low latency;
- stores the URL mappings and click events;
- gives URL owners analytics through versioned REST APIs.

The workload is read-heavy: redirects far outnumber creates.

---

## 2. Functional Requirements

### REQ-1 Create a short URL (must)
- **AC-1.1:** Given a valid http/https URL, when the client calls `POST /api/v1/urls`, then the service returns `201 Created` with `shortCode`, `shortUrl`, `originalUrl`, `createdAt` and `expiresAt`.
- **AC-1.2:** Given an invalid or malformed URL, or a scheme other than http/https, when the client creates it, then the service returns `400` with a ProblemDetails body.
- **AC-1.3:** Generated codes are unique, 7 characters long, and Base62 encoded.
- **AC-1.4:** Given a repeat request with the same `Idempotency-Key`, when it is submitted, then the service returns the original result and creates no duplicate.

### REQ-2 Custom alias (should)
- **AC-2.1:** Given an alias that is available and valid (3–30 characters, `[A-Za-z0-9-]`), when the client creates a URL with it, then the alias becomes the short code.
- **AC-2.2:** An alias that is already taken returns `409 Conflict`. Reserved words (`api`, `admin`, `health`, `swagger`) return `400`.

### REQ-3 Redirect (must)
- **AC-3.1:** Given an active code, when a visitor calls `GET /{code}`, then the service returns `302 Found` with `Location` set to the original URL.
- **AC-3.2:** An unknown code returns `404`. An expired or disabled code returns `410 Gone`.
- **AC-3.3:** Every redirect records a click event asynchronously, without blocking the redirect.

### REQ-4 Expiry (should)
- **AC-4.1:** The optional `expiresAt` must be a future UTC timestamp; otherwise the service returns `400`.
- **AC-4.2:** After `expiresAt` has passed, a redirect returns `410`.

### REQ-5 Manage URLs (must)
- **AC-5.1:** `GET /api/v1/urls/{code}` returns the URL's metadata.
- **AC-5.2:** `GET /api/v1/urls?page=&pageSize=` returns a paginated list of the caller's URLs.
- **AC-5.3:** `PATCH /api/v1/urls/{code}` can update `expiresAt` and `isActive`. `DELETE` soft-deletes the URL, after which a redirect returns `410`.
- **AC-5.4:** A caller who tries to manage another owner's URL receives `403 Forbidden`.

### REQ-6 Analytics (must)
- **AC-6.1:** Each click records:
  - the timestamp and code;
  - the referrer;
  - the parsed user agent (device, browser, OS);
  - the country.

  The IP address is hashed or anonymized and never stored raw.
- **AC-6.2:** `GET /api/v1/urls/{code}/analytics?from=&to=&granularity=hour|day` returns total clicks, unique clicks, and a time series.
- **AC-6.3:** The response includes top-N breakdowns by referrer, country, device, and browser.
- **AC-6.4:** Analytics are eventually consistent, lagging real time by no more than 60 seconds.

### REQ-7 Authentication (must)
- **AC-7.1:** The management and analytics APIs require an `X-Api-Key` header or a JWT bearer token. Requests without one get `401`.
- **AC-7.2:** The redirect endpoint is public.

### REQ-8 Rate limiting and abuse protection (must)
- **AC-8.1:** URL creation is rate-limited per API key (default: 100 per minute). Requests over the limit get `429` with a `Retry-After` header.
- **AC-8.2:** Destination URLs on a configurable domain blocklist are rejected with `400`.
- **AC-8.3:** URLs that point back to the service's own domain are rejected, to prevent redirect loops.

### REQ-9 Observability and health (must)
- **AC-9.1:** `/health/live` and `/health/ready` are available; the readiness check covers the database and the cache.
- **AC-9.2:** Logs are structured (Serilog) and carry a correlation ID.
- **AC-9.3:** OpenTelemetry metrics cover request count, request latency, and cache hit ratio.

### REQ-10 API documentation and versioning (must)
- **AC-10.1:** An OpenAPI 3 specification (`openapi.yaml`) and Swagger UI document every endpoint.
- **AC-10.2:** All management APIs are versioned under `/api/v1`.

---

## 3. Non-Functional Requirements

| ID | Category | Requirement |
|---|---|---|
| NFR-1 | Performance | Redirect p95 < 50 ms on a cache hit and < 150 ms on a miss; create p95 < 200 ms |
| NFR-2 | Scalability | Stateless API that scales horizontally; designed for ~1,000 redirects/sec and ~100 creates/sec (read:write ≈ 100:1) |
| NFR-3 | Availability | 99.9% target for redirects; redirects keep working if the analytics pipeline is down |
| NFR-4 | Durability | No mapping is lost once `201` has been returned; click events are delivered at least once |
| NFR-5 | Storage | Supports 100M+ URLs; raw click events kept for 90 days, aggregates kept indefinitely |
| NFR-6 | Security | OWASP Top 10 mitigated; HTTPS only; input validated; no secrets in code |
| NFR-7 | Privacy | IP addresses anonymized; GDPR-friendly retention |
| NFR-8 | Maintainability | Clean Architecture; ≥ 80% line coverage on Domain and Application; no analyzer warnings |
| NFR-9 | Deployability | Dockerfile plus docker-compose (API, Worker, SQL Server, Redis); config via appsettings and environment variables |

---

## 4. Technical Constraints
- Visual Studio 2022 (17.8+) `.sln`, C# on .NET 8 LTS, ASP.NET Core Web API.
- EF Core on SQL Server (Azure SQL in production); Redis as a read-through cache for code → URL lookups.
- Click ingestion through a background queue: `System.Threading.Channels` locally, with Azure Service Bus as the production option. A Worker service aggregates the events.
- Tests use xUnit, FluentAssertions, Moq/NSubstitute, `WebApplicationFactory`, and Testcontainers (SQL Server, Redis).

---

## 5. API Summary

| Method | Endpoint | Auth | Purpose |
|---|---|---|---|
| POST | `/api/v1/urls` | API key | Create short URL |
| GET | `/api/v1/urls` | API key | List own URLs (paginated) |
| GET | `/api/v1/urls/{code}` | API key | Get URL metadata |
| PATCH | `/api/v1/urls/{code}` | API key | Update expiry / active status |
| DELETE | `/api/v1/urls/{code}` | API key | Soft delete |
| GET | `/api/v1/urls/{code}/analytics` | API key | Click analytics |
| GET | `/{code}` | Public | Redirect |
| GET | `/health/live`, `/health/ready` | Public | Health checks |

---

## 6. Data Model (initial)

| Entity | Key fields |
|---|---|
| `ShortUrl` | Id, ShortCode (unique index), OriginalUrl, OwnerId, IsCustomAlias, CreatedAt, ExpiresAt, IsActive, IsDeleted |
| `ApiKey` | Id, OwnerId, KeyHash, Name, CreatedAt, RevokedAt |
| `ClickEvent` | Id, ShortCode, OccurredAt, Referrer, Country, Device, Browser, Os, VisitorHash |
| `ClickAggregate` | ShortCode, BucketStart, Granularity, TotalClicks, UniqueClicks |

---

## 7. Expected Engineering Outputs
- **Solution projects:** `UrlShortener.Api`, `.Application`, `.Domain`, `.Infrastructure`, `.Worker`, plus `.UnitTests` and `.IntegrationTests`.
- **Contracts and schema:** `openapi.yaml` and EF Core migrations, each with `Up` and `Down`.
- **Documentation:**
  - README covering how to run the service in Visual Studio, from the CLI, and with docker-compose;
  - ADRs on the short-code generation strategy, the cache strategy, and the analytics pipeline;
  - sequence diagrams for the create and redirect flows;
  - a risk register and a test strategy.
- **Load test:** a script (k6 or NBomber) to check NFR-1 and NFR-2.

---

## 8. Ambiguities and Assumptions (confirm at HG-1)

| ID | Question | Assumption if unconfirmed |
|---|---|---|
| A-1 | Should redirects use 301 or 302? | **302**, so every click reaches the server and can be counted |
| A-2 | How are short codes generated? | Random Base62 with a retry on collision; a counter- or Snowflake-based scheme is documented as an alternative |
| A-3 | Does the same long URL always get the same code? | No. Each request gets a new code unless the idempotency key matches |
| A-4 | How is "scalable" verified? | Against the NFR-2 targets with a local load test; no cloud deployment |
| A-5 | Can anonymous users create links? | No. Creating a link requires an API key |
| A-6 | Where does country data come from? | A pluggable `IGeoLocator` with a stub implementation; a MaxMind database is optional |
| A-7 | What counts as a "unique click"? | A distinct hash of IP + user agent per day |
| A-8 | Is a UI required? | No. The service is API-only, with Swagger UI |

---

## 9. Out of Scope
- Web UI or dashboard
- QR code generation
- Bulk import
- Branded custom domains
- Billing
- Full user registration
- Malware scanning beyond the domain blocklist

---

## 10. Known Risks (carry into the Stage 3 risk register)

| ID | Risk | Initial mitigation |
|---|---|---|
| R-1 | Cache stampede or stale entries after a URL is deleted or expires | Cache TTL ≤ expiry, invalidate on update, single-flight loading |
| R-2 | Very popular codes overloading the database | Redis cache plus in-memory LRU; read replicas |
| R-3 | Click events lost if the queue or worker crashes | Durable queue (Service Bus) or the outbox pattern; at-least-once delivery with idempotent aggregation |
| R-4 | Short-code collisions as volume grows | Unique index plus retry; monitor the collision rate; option to extend code length |
| R-5 | Open-redirect or phishing abuse | Domain blocklist, rate limits, ability to disable a code |
| R-6 | Unbounded growth of the `ClickEvents` table | Retention job, aggregation, table partitioning |
