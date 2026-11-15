# Problem Statement

## Problem
Provide a reliable service that turns long URLs into compact, shareable links, redirects users quickly, persists link state, and exposes usage analytics for each short link.

## Context
This is a greenfield .NET 8 service. The initial deployment must be runnable locally without external infrastructure while preserving clear seams for a distributed production deployment.

## Inputs / Outputs
- Input: destination URL, optional custom alias, optional expiration timestamp.
- Output: short code, short URL, destination, timestamps, and analytics counters.
- Redirect input: short code and request metadata.
- Analytics output: aggregate clicks, unique visitors, and recent event data.

## Functional Requirements
- REQ-1 (must): Create a short link for a valid absolute HTTP/HTTPS URL.
- REQ-2 (must): Redirect a valid short code to its destination.
- REQ-3 (must): Persist links and click events across process restarts.
- REQ-4 (must): Return analytics for a short code, including clicks and unique visitors.
- REQ-5 (must): Reject duplicate custom aliases and malformed requests with problem details.
- REQ-6 (should): Support expiration and prevent redirects after expiry.
- REQ-7 (should): Expose health information for operations.

## Non-functional Requirements
- NFR-1: p95 redirect application latency target is under 100 ms excluding network latency in a single-node deployment.
- NFR-2: API must be stateless; persistence is behind an interface so multiple API instances can be deployed.
- NFR-3: All public endpoints use async cancellation-aware code paths.
- NFR-4: Invalid input returns HTTP 400; missing links return HTTP 404; expired links return HTTP 410.
- NFR-5: No secrets are stored in source or configuration.

## Constraints
- .NET 8, ASP.NET Core, nullable reference types enabled.
- Local JSON persistence is the zero-dependency demo provider; production can replace it with SQL/Redis.
- Analytics are aggregate and privacy-preserving; raw IP addresses are not persisted.

## Out of Scope
Authentication, billing, multi-region replication, QR code generation, custom domains, and real-time dashboards.

## Success Criteria
See `requirements.json` for testable Given/When/Then acceptance criteria.
