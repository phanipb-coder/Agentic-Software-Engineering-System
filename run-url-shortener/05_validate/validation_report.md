# Validation Report

## Approval status
- HG-1: approved
- HG-2: approved
- HG-3: not raised
- HG-4: approved

## Checks

| Check | Command / evidence | Result |
|---|---|---|
| Restore | `dotnet restore 04_build/Shortener.sln` | Passed |
| Release build | `dotnet build 04_build/Shortener.sln -c Release --no-restore` | Passed with one MSB4121 solution configuration warning; direct API and test project builds passed with zero warnings |
| Tests | `dotnet test 04_build/tests/Shortener.Tests/Shortener.Tests.csproj -c Release --no-restore` | Passed: 5 total, 5 succeeded, 0 failed |
| Format | `dotnet format 04_build/Shortener.sln --verify-no-changes --no-restore` | Passed after applying formatter |
| Secret scan | Source/config reviewed; no secrets or credentials present | Pass by review |
| Contract trace | OpenAPI paths map to Program.cs endpoints | Pass by review |

## AC traceability
- AC-1.1: `CreateLink_ValidUrl_ReturnsShortLink`
- AC-1.2: `CreateLink_InvalidUrl_ThrowsValidationException`
- AC-2.1: `ResolveLink_ActiveLink_ReturnsDestinationAndRecordsClick`
- AC-2.2: API maps `LinkNotFoundException` to 404
- AC-3.1: `JsonFilePersistenceStore` uses durable atomic file replacement
- AC-4.1: analytics endpoint returns clicks, unique visitors, and recent events
- AC-5.1: `CreateLink_DuplicateAlias_ThrowsConflict`
- AC-6.1: `ResolveLink_ExpiredLink_ThrowsGoneException`

## Limitation
The solution-level build emits MSB4121 for the test project configuration even though direct project build/test commands pass. The warning is recorded explicitly and does not block the approved demonstration release.
