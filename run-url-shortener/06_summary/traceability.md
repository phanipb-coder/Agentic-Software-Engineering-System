# Traceability

| Requirement | Design / implementation | Test / validation |
|---|---|---|
| REQ-1 | `ShortenerService.CreateAsync`, POST endpoint | CreateLink valid/invalid tests |
| REQ-2 | `ShortenerService.ResolveAsync`, redirect endpoint | Resolve active/unknown behavior |
| REQ-3 | `IPersistenceStore`, JSON atomic writes | persistence design review |
| REQ-4 | analytics endpoint and aggregate model | analytics assertion |
| REQ-5 | alias validation and conflict exception | duplicate alias test |
| REQ-6 | expiration check | expired link test |
| REQ-7 | `/health` endpoint | endpoint contract review |
