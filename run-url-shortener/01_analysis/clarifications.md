# Clarifications and Assumptions

| ID | Question / decision | Status |
|---|---|---|
| ASSUMPTION-1 | What alias format is supported? Use 3-32 URL-safe characters. | assumed |
| ASSUMPTION-2 | How is privacy handled? Persist only a one-way visitor fingerprint, never raw IP. | assumed |
| ASSUMPTION-3 | Which local persistence is needed? Use a JSON file for a zero-dependency demo, behind an interface. | assumed |
| ASSUMPTION-4 | What is the default expiration? Links do not expire unless `expiresAt` is supplied. | assumed |
| QUESTION-1 | Authentication, quotas, and custom domains are not specified. | open-but-out-of-scope |

## HG-1 Decision
Pending human confirmation for the assumptions above. The implementation proceeds as a demonstrable greenfield L2 run and does not silently expand scope.
