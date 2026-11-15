# URL Shortener

A scalable-by-design URL shortener demo built with .NET 8 minimal APIs.

## Prerequisites
- .NET 8 SDK
- Visual Studio 2022 17.8+ (optional)

## Run from the CLI
```bash
dotnet restore 04_build/Shortener.sln
dotnet run --project 04_build/src/Shortener.Api/Shortener.Api.csproj --urls http://localhost:5080
```

Create and use a link:
```bash
curl -X POST http://localhost:5080/api/v1/links -H "Content-Type: application/json" -d '{"url":"https://example.com/docs"}'
curl -i http://localhost:5080/<code>
curl http://localhost:5080/api/v1/links/<code>/analytics
```

Run tests:
```bash
dotnet test 04_build/Shortener.sln --configuration Release
```

## Configuration
`appsettings.json` contains `Shortener:StoragePath` and `Shortener:PublicBaseUrl`. Environment variables can override them using `Shortener__StoragePath` and `Shortener__PublicBaseUrl`. The JSON store is created on first write.

## API
The contract-first definition is in `03_plan/contracts/openapi.yaml`. Endpoints are:
- `POST /api/v1/links`
- `GET /{code}`
- `GET /api/v1/links/{code}/analytics`
- `GET /health`

## Persistence and production scale
The demo uses atomic JSON file replacement and a process semaphore. For multiple instances, replace `IPersistenceStore` with a transactional database implementation and optionally add Redis for hot redirect reads. Click events should move to a durable queue and asynchronous aggregator at high volume.

## Validation
See `05_validate/validation_report.md` and `06_summary/ENGINEERING_SUMMARY.md`.
