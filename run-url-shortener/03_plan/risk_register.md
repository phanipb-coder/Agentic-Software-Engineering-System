# Risk Register

| ID | Risk | Category | Likelihood | Impact | Mitigation | Test / owner |
|---|---|---|---|---|---|---|
| R-1 | Concurrent file writes lose updates | technical | medium | high | atomic replacement and semaphore; production DB | persistence tests / developer |
| R-2 | Short-code collision | technical | low | medium | uniqueness check and retry | create collision test / test engineer |
| R-3 | Visitor fingerprint re-identification | security | low | medium | no raw IP, retention policy, rotating salt in production | privacy review / reviewer |
| R-4 | Expired link is served | data | low | high | expiration checked before redirect | AC-6.1 / test engineer |
| R-5 | Provider cannot scale horizontally | delivery | medium | high | interface boundary and documented migration path | architecture review / architect |
