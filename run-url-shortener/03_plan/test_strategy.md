# Test Strategy

## Pyramid
- Unit: URL and alias validation, code generation, expiration, analytics aggregation.
- Integration: API endpoint behavior and persistence across store instances.
- Contract: response and status shapes are checked against `contracts/openapi.yaml` during review.
- Regression: run the complete `dotnet test` suite on every change.

## Acceptance mapping
| AC | Test |
|---|---|
| AC-1.1 | `CreateLink_ValidUrl_ReturnsShortLink` |
| AC-1.2 | `CreateLink_InvalidUrl_ThrowsValidationException` |
| AC-2.1 | `ResolveLink_ActiveLink_ReturnsDestinationAndRecordsClick` |
| AC-2.2 | `ResolveLink_UnknownCode_ReturnsNotFound` |
| AC-3.1 | `JsonStore_Reopened_RetainsLinkAndClick` |
| AC-4.1 | `GetAnalytics_AfterClicks_ReturnsAggregates` |
| AC-5.1 | `CreateLink_DuplicateAlias_ThrowsConflict` |
| AC-6.1 | `ResolveLink_ExpiredLink_ReturnsGone` |

## Verification order
1. Restore and build Release.
2. Run unit/integration tests.
3. Run format verification if SDK formatter is available.
4. Review OpenAPI and AC traceability.
5. Run a secret scan and inspect changed files.

Target: at least 80% line coverage on changed application code when coverage tooling is available.
