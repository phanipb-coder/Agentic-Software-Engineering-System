---
name: requirement-to-engineering-outcome
description: Turn a software requirement (greenfield, brownfield, bug fix, refactor, test or docs work; clear or ambiguous) into a reviewable .NET/C# engineering outcome via a gated, multi-agent SDLC workflow with human approval gates.
---

# Requirement → Reviewable Engineering Outcome (.NET / C#)

An agentic system that turns a software requirement into a reviewable engineering outcome: code, API contracts and schemas, tests, documentation, and a structured engineering summary. It is an orchestrated SDLC workflow, **not a chatbot**. Agents work on their own across multiple steps, coordinate through shared state, recover from failures, and stop at defined human approval gates.

**Target stack:** a Visual Studio solution (`.sln`) in C# on .NET 8 LTS, unless the requirement or the existing code says otherwise. Everything must build and run its tests both in Visual Studio and through the `dotnet` CLI.

## Core requirement coverage map
| # | Core requirement | Where it is met |
|---|---|---|
| 1 | Requirement understanding | Stage 1: intent, ambiguity handling, normalized problem statement |
| 2 | Task decomposition | Stage 3: task DAG, dependencies, execution waves, critical path |
| 3 | Codebase reasoning (brownfield) | Stage 2: architecture map, impacted services/modules/APIs/data flows |
| 4 | Workflow orchestration | Stage 4: shared blackboard, event-driven coordination, feedback loops, error recovery |
| 5 | Engineering output generation | Stage 4: code, OpenAPI contracts, schemas/migrations, unit and integration tests, docs |
| 6 | Validation and risk control | Stages 3 and 5: risk register, trade-offs, failure scenarios, test strategy, guardrails |
| 7 | Controlled autonomy | Autonomy model and human gates HG-1 to HG-4 |
| 8 | Final output | Stage 6: `ENGINEERING_SUMMARY.md` |

## When to use
- A requirement arrives and the user wants working, validated output rather than advice.
- Any scenario: **greenfield** (new feature or system), **brownfield** (enhancement, refactor, bug fix), or **quality** work (tests, documentation).
- The requirement may be well defined or ambiguous.

---

## Controlled autonomy model
Agents run on their own *between* gates. Humans approve *at* gates. Every gate decision is written to `approvals.md` with who decided, when, and what was approved, changed or rejected.

| Gate | After stage | Human approves | Agents may not proceed until |
|---|---|---|---|
| **HG-1 Requirement sign-off** | 1 (and 2 for brownfield) | Problem statement, ACs, assumptions, scope; impact analysis for brownfield | Approved or amended |
| **HG-2 Plan and contract approval** | 3 | Design, task graph, API contracts, schema changes, risk register | Approved |
| **HG-3 Exception gate** (on demand) | Any | Raised automatically on: a high-risk action, a breaking API/schema change, a task blocked after recovery, a scope change, or a gate the agent wants to waive | Human decides: approve, modify, skip, or abort |
| **HG-4 Release approval** | 5 | Engineering summary, validation results, diff | Approved → commit/PR; rejected → feedback routed back as tasks |

**Autonomy levels** (default L2; the user may set another level):
- **L1 Supervised:** pause after every stage.
- **L2 Gated (default):** run on their own between HG-1, HG-2 and HG-4; HG-3 is raised on exceptions.
- **L3 Autonomous demo:** gates are auto-approved *only* when no risk is high and no assumption is blocking. Each auto-approval is logged and flagged in the summary. Never on brownfield changes to public APIs or data schemas.

If the user is absent at a gate under L2, prepare everything, record the pending decision, and stop. Don't continue past the gate.

## Guardrails (always on)
- **Scope:** agents write only inside `run-<slug>/`. Brownfield edits may touch only files listed in the approved impact analysis; any other file needs HG-3.
- **No destructive operations** without HG-3: no deleting projects or files outside the plan, no dropping DB tables or columns, no force-push, no edits to CI or deployment configuration.
- **Safe data changes:** schema changes go only through additive EF Core migrations by default. Destructive migrations need HG-3 plus a rollback script.
- **Secrets:** never written to code or config. Use placeholders, user secrets or environment variables. The scan in Stage 5 enforces this.
- **Budgets:** at most 3 retries per task, at most 2 replans per run, and at most roughly 400 changed lines per task (larger diffs must be split). Hitting a budget raises HG-3.
- **Honesty:** a check is reported as passed only if it was actually run. Missing tools (the SDK, Visual Studio, Docker) are reported as limitations.
- **Checkpointing:** the blackboard and task graph are saved after every task, so a run can resume from the last good state.

---

## Workspace layout
```
run-<slug>/
  blackboard.json                  # shared state for cross-agent coordination
  approvals.md                     # human gate decisions
  orchestration_log.jsonl          # every event
  00_input/requirement.md          # verbatim input
  00_input/codebase/               # brownfield: existing solution
  01_analysis/problem_statement.md # normalized engineering problem
  01_analysis/requirements.json
  01_analysis/clarifications.md
  02_codebase/architecture_map.md  # brownfield: system-level view + Mermaid diagrams
  02_codebase/impact_analysis.json
  02_codebase/baseline/            # pre-change build/test/coverage results
  03_plan/design.md                # design + ADRs + trade-offs
  03_plan/task_graph.json
  03_plan/risk_register.md
  03_plan/test_strategy.md
  03_plan/contracts/openapi.yaml   # API contracts (contract-first)
  03_plan/contracts/schema/        # DB schema / DTO JSON schemas
  04_build/<Solution>.sln
  04_build/src/...
  04_build/tests/...               # unit + integration tests
  04_build/docs/                   # README, API docs, ADRs, CHANGELOG
  05_validate/validation_report.md
  06_summary/ENGINEERING_SUMMARY.md
  06_summary/traceability.md
  06_summary/PR_SUMMARY.md
```

## Agents
| Agent | Responsibility |
|---|---|
| **Orchestrator** | Owns the blackboard and task graph; schedules waves; routes events; handles recovery; raises gates |
| **Requirements Analyst** | Interprets intent, resolves ambiguity, writes the problem statement and ACs |
| **Codebase Analyst** | Maps the architecture and dependencies; produces the impact analysis and baseline |
| **Architect / Planner** | Design, ADRs, trade-offs, contracts, task DAG, risk register |
| **Developer (C#)** | Implements, modifies and refactors code |
| **Test Engineer** | Unit, integration, characterization and repro tests |
| **Technical Writer** | README, XML docs, API docs, ADRs, CHANGELOG |
| **Validator** | Runs builds, tests, coverage, contract and security checks |
| **Reviewer** (fresh context) | Independently reviews the diff against requirements, design and standards |

Agents do not call each other directly. They read from and write to the **blackboard**, and the Orchestrator reacts to the events they post.

---

## Stage 0: Classify the scenario
Choose one primary `mode` (secondary modes are allowed) and record it on the blackboard:

| Mode | Pipeline differences |
|---|---|
| `greenfield` | Skip Stage 2; scaffold a new solution |
| `enhancement` | Stage 2 required; extend the existing architecture |
| `refactor` | Characterization tests first; behaviour must be identical |
| `bugfix` | Failing repro test first → root cause → minimal fix |
| `test-improvement` | Production code untouched; report any bugs found instead of fixing them |
| `docs-improvement` | Code untouched; docs verified against the code |

Brownfield work with no codebase supplied → ask for the repo or solution. Never invent one.

## Stage 1: Requirement understanding
**1a. Interpret intent.** Identify the business goal behind the request (*why*), the actors, success measures, and the implicit expectations (security, performance, compatibility).

**1b. Identify ambiguities.** Look for missing rules, undefined terms, unstated edge cases, conflicting statements, and vague qualities ("fast", "secure", "better"). Classify the requirement's clarity:

| Clarity | Action |
|---|---|
| Clear | Proceed |
| Partial | Make explicit `ASSUMPTION-n` entries and proceed; they are confirmed at HG-1 |
| High ambiguity | Ask up to 4 targeted questions (multiple choice preferred). If no answer is possible, propose 2–3 interpretations, pick the most conservative, and record why |
| Contradictory | Stop and raise HG-1 with the conflict laid out. Never resolve it silently |

All items go in `clarifications.md` with an ID and a status (`open`, `answered` or `assumed`).

**1c. Normalize into an engineering problem.** `problem_statement.md`:
```
Problem:        one-paragraph engineering framing
Context:        system, users, current state (brownfield)
Inputs/Outputs: data in, data out, interfaces touched
Functional requirements:      REQ-n (must/should/could)
Non-functional requirements:  measurable (e.g. p95 < 300 ms, OWASP ASVS L1)
Constraints:    stack, compatibility, regulatory (e.g. BFSI audit logging)
Out of scope
Success criteria: acceptance criteria in Given/When/Then, IDs AC-n.m
```
The machine-readable version is `requirements.json` (fields: `mode`, `clarity`, `requirements[]` with `acceptance_criteria[]`, `nfrs[]`, `out_of_scope[]`, `assumptions[]`).

**Gate 1 (automated):** every `must` REQ has at least one testable AC; NFRs are measurable; no blocking question is open.
**→ HG-1** (combined with Stage 2 output for brownfield).

## Stage 2: Codebase reasoning (brownfield)
Show **system-level** understanding, not just file search.

1. **Architecture map** (`architecture_map.md`):
   - Solution and project inventory, with target frameworks, NuGet packages and test frameworks.
   - Architecture style (layered, Clean, microservices, MVC), with layer boundaries and dependency direction.
   - Mermaid diagrams: a project/service dependency graph, and a component diagram of the affected area.
   - Cross-cutting concerns: dependency injection setup, authentication, logging, configuration, error handling, persistence (EF Core DbContexts and repositories), messaging.
2. **Impact analysis** (`impact_analysis.json`):
```json
{
  "entry_points": ["POST /api/orders", "OrderCreatedConsumer"],
  "services": [{"name": "Orders.Api", "impact": "modify", "reason": ""}],
  "modules": [{"project": "Orders.Application", "types": ["OrderService"], "impact": "modify"}],
  "apis": [{"endpoint": "POST /api/orders", "change": "additive | breaking | none", "consumers": []}],
  "data": [{"entity": "Order", "table": "dbo.Orders", "change": "add column Status", "migration": true}],
  "data_flows": [{"name": "Create order", "path": ["Controller", "OrderService", "IOrderRepository", "DbContext", "dbo.Orders", "OrderCreated event"]}],
  "callers_and_dependents": [],
  "files_allowed_to_change": [],
  "risk_hotspots": ["low test coverage in OrderService", "shared DbContext"]
}
```
   - Trace each affected **data flow** end to end: entry point → application logic → domain → persistence or external call → events and outputs.
   - Classify each API change as additive, breaking or none, and list its known consumers.
3. **Baseline:** run `dotnet build`, `dotnet test` and coverage *before* any change, and save the results to `02_codebase/baseline/`. Pre-existing failures are recorded, not blamed on this change.
4. **Conventions:** naming, folder layout, DI style, async patterns, nullable settings, `.editorconfig`, error-handling pattern. New code must follow them.
5. **Mode extras:**
   - `bugfix`: a failing repro test plus a root-cause hypothesis with evidence (stack trace, data path).
   - `refactor`: characterization tests where the code being changed is thinly covered.

**Gate 2 (automated):** baseline captured; every impacted item names a reason; every data flow is traced; `files_allowed_to_change` is set.

## Stage 3: Design, decomposition and risk planning
**3a. Design** (`design.md`): components and their responsibilities, interfaces, and sequence diagrams for the key flows. Each significant choice is recorded as an ADR (context, options, decision, consequences).

Clean-design rules: SOLID principles; dependencies point inward (Domain has no infrastructure references); interfaces at boundaries; small cohesive classes; no God services.

Greenfield default structure (use only the layers the requirement needs):
```
src/<Name>.Api            ASP.NET Core Web API, controllers or minimal APIs, ProblemDetails
src/<Name>.Application    use cases, DTOs, validators, interfaces
src/<Name>.Domain         entities, value objects, domain rules
src/<Name>.Infrastructure EF Core, repositories, external clients
tests/<Name>.UnitTests
tests/<Name>.IntegrationTests
Directory.Build.props     Nullable, TreatWarningsAsErrors, AnalysisLevel=latest
```
Brownfield work follows the existing structure. Any new pattern or package must be justified in an ADR.

**3b. Contracts (contract-first):**
- `contracts/openapi.yaml` (OpenAPI 3.x): endpoints, request/response DTOs, status codes, ProblemDetails errors, versioning, auth.
- `contracts/schema/`: EF Core entity model changes as DDL previews and the planned migration names, plus JSON Schemas for DTOs and messages where events are used.
- Brownfield: show a contract diff with each change marked additive or breaking. A breaking change needs a versioning strategy and is flagged at HG-2.

**3c. Task decomposition** (`task_graph.json`):
```json
{
  "tasks": [{
    "id": "T-3",
    "title": "Implement CreateOrder use case",
    "type": "analysis | scaffold | contract | schema | implement | refactor | fix | test | docs",
    "agent": "Developer",
    "requirement_ids": ["REQ-1"], "acceptance_criteria_ids": ["AC-1.1"],
    "depends_on": ["T-1", "T-2"],
    "consumes": ["contracts/openapi.yaml#/paths/~1api~1orders"],
    "produces": ["src/Orders.Application/CreateOrder/CreateOrderHandler.cs"],
    "risk": "low | medium | high",
    "definition_of_done": ["builds", "unit tests for AC-1.1 pass"],
    "recovery": "retry | replan | escalate",
    "status": "pending"
  }],
  "waves": [["T-1","T-2"], ["T-3","T-4"], ["T-5"]],
  "critical_path": ["T-1","T-3","T-5"]
}
```
- Compute **execution waves** (tasks in the same wave can run in parallel when their `produces` sets don't overlap) and the **critical path**.
- Mode ordering:
  - `bugfix`: repro → fix → regression tests → docs.
  - `refactor`: characterization tests → small steps, each followed by a green test run.
  - Contract and schema tasks come before the implementation tasks that consume them.

**3d. Risk register** (`risk_register.md`): ID, risk, category (technical, data, security, delivery, compatibility), likelihood, impact, mitigation, owner, and a linked task or test.

**Trade-offs:** for each ADR, list the options considered, the pros and cons of each, and why the chosen one won.

**Failure scenarios:** list the ways the change could fail in production (bad input, dependency down, concurrency, partial writes, migration failure, auth bypass), with the handling for each (validation, retry with Polly, transactions or the outbox pattern, idempotency keys, a rollback migration) and the test that proves it.

**3e. Test strategy** (`test_strategy.md`):
- The test pyramid: **unit** (domain and application logic), **integration** (API through `WebApplicationFactory<T>`, data through SQLite, EF InMemory or Testcontainers SQL Server), **contract** (responses validated against `openapi.yaml`), and **regression** (the baseline suite).
- An AC → test mapping, the NFR verification method, coverage targets (for example 80% line coverage on changed code), and the order of verification steps.

**Gate 3 (automated):** the DAG is acyclic; every `must` AC is covered by tasks and tests; every high risk has a mitigation; contracts validate (OpenAPI lint).
**→ HG-2** (design, contracts, schema, task graph, risks).

## Stage 4: Workflow orchestration and output generation
### Coordination model: blackboard plus events, not a linear script
`blackboard.json` holds the shared state that every agent reads and writes:
```json
{
  "mode": "", "autonomy": "L2", "gate_status": {},
  "contracts_version": "v1", "schema_version": "",
  "tasks": {"T-3": {"status": "done", "artifacts": [], "attempts": 1}},
  "events": [], "open_issues": [], "assumptions": [], "budgets": {"replans_used": 0}
}
```
Agents post **events**. The Orchestrator reacts to them, which gives cross-step coordination:

| Event | Posted by | Orchestrator reaction |
|---|---|---|
| `contract.changed` | Developer or Architect | Mark dependent implementation and test tasks `stale`; re-run them; raise HG-3 if the change is breaking |
| `schema.changed` | Developer | Regenerate the migration task; re-run data integration tests |
| `test.failed` | Test Engineer or Validator | Diagnose: code defect → reopen the producing task; test defect → reopen the test task; requirement gap → send back to the Analyst |
| `defect.found` | Reviewer | Create a fix task linked to the defect, slotted into the next wave |
| `assumption.invalidated` | Any | Pause the affected tasks; replan or raise HG-3 |
| `scope.expansion` | Any | Block the work; raise HG-3 |
| `budget.exceeded` | Orchestrator | Raise HG-3 |

### Execution loop
```
load the checkpoint (resume if one exists)
for wave in waves (recomputed after any replan):
    run the ready tasks in the wave in parallel (no overlap in produces)
    each task: agent reads blackboard → generates → runs task checks
               (dotnet build; dotnet test --filter Trait=AC-x) → posts events
    the Orchestrator handles events (table above), which may:
        mark tasks stale, insert fix tasks, re-sequence waves, or raise a gate
    checkpoint the blackboard and task graph
```

### Error handling and recovery
| Failure | Strategy |
|---|---|
| Transient (restore or tool timeout) | Retry with backoff (up to 3) |
| Compile or test failure in a task | Retry with the error context fed back to the agent (up to 3) |
| Repeated failure or wrong approach | **Replan:** the Architect revises the affected sub-graph (up to 2 per run) |
| Downstream breakage caused by an upstream task | **Rollback** that task's changes to the last checkpoint and reopen it |
| Invalid assumption or contract conflict | Pause dependents → HG-3 |
| Unrecoverable | Mark the task `blocked`; finish the independent branches; report a partial outcome honestly |

Every retry, replan, rollback and escalation is logged in `orchestration_log.jsonl` (`ts, wave, task_id, agent, event, attempt, detail`) and summarised in the final output.

### Engineering outputs (a cohesive set)
1. **Production-quality C# code:**
   - Nullable reference types enabled; async/await all the way down, with a `CancellationToken` on async calls; constructor dependency injection; `ILogger<T>`; the Options pattern for configuration.
   - FluentValidation or guard clauses; ProblemDetails for API errors; no swallowed exceptions; idempotency and transactions where the failure scenarios need them.
   - XML doc comments on public APIs; follow `.editorconfig`; no `TODO` or `NotImplementedException` on `must` paths; no secrets.
2. **API contracts and schemas:**
   - Code matches `openapi.yaml` (Swashbuckle or NSwag output is diffed against the contract).
   - EF Core migrations with `Up` and `Down`; DTO and event schemas are versioned.
3. **Tests:**
   - xUnit (or the framework the solution already uses), FluentAssertions, Moq or NSubstitute.
   - **Unit tests:** named `Method_Scenario_ExpectedResult`, tagged with `[Trait("AC","AC-1.1")]`, in Arrange/Act/Assert form; happy path, edge cases, and at least one failure path per `must` REQ.
   - **Integration tests:** API end to end through `WebApplicationFactory<T>`; persistence against a real provider (SQLite or Testcontainers); contract tests against the OpenAPI spec; at least one test per failure scenario.
4. **Documentation:**
   - `README.md`: prerequisites (.NET 8 SDK, Visual Studio 2022 17.8 or later), how to run in Visual Studio and from the CLI, configuration, how to test.
   - API docs (Swagger UI plus `openapi.yaml`), ADRs, a sequence diagram for each key flow, a CHANGELOG entry, and migration and rollback notes.

## Stage 5: Validation and risk control
Run each check and record the evidence in `validation_report.md`:
1. `dotnet restore` and `dotnet build -c Release`: zero errors and no new warnings (analyzers on).
2. `dotnet format --verify-no-changes`.
3. `dotnet test --collect:"XPlat Code Coverage"`: pass/fail counts for unit and integration tests, and coverage on changed code.
4. **AC trace:** 100% of `must` ACs have a passing test carrying `[Trait("AC", …)]`.
5. **Contract conformance:** the generated OpenAPI matches `contracts/openapi.yaml`, and no unapproved breaking changes exist.
6. **Schema:** migrations apply cleanly to an empty DB *and* to a baseline DB; `Down` reverses them.
7. **Regression (brownfield):** compared against the baseline, no test that passed before now fails, coverage hasn't dropped, and only `files_allowed_to_change` were modified.
8. **Mode checks:**
   - `bugfix`: the repro test fails on the baseline and passes after the fix.
   - `refactor`: behaviour is identical.
   - `test-improvement`: the coverage change is reported and production code is unchanged.
   - `docs-improvement`: the code samples compile.
9. **Security:** `dotnet list package --vulnerable`; a secrets scan; checks for SQL built from strings and missing authorization attributes on new endpoints.
10. **Risk closure:** each risk in the register is marked mitigated, accepted or open, with evidence.
11. **Independent review:** a fresh-context Reviewer checks the diff against the requirements, design, contracts and C# standards. Any defects are posted as `defect.found` events and routed back to Stage 4 (up to 3 loops).

**Gate 5 (automated):** all of the above pass, or any exceptions are explicitly approved through HG-3.
**→ HG-4 Release approval.** On approval, commit to a `feature/`, `fix/` or `refactor/` branch and open a PR if a repo is available. Otherwise deliver the zipped run folder; the `.sln` must open directly in Visual Studio.

## Stage 6: Final output — `ENGINEERING_SUMMARY.md`
```
# Engineering Summary: <title>
Mode: <mode> | Clarity: <level> | Autonomy: <L1/L2/L3> | Status: <complete / partial>

## 1. Problem statement (normalized)
## 2. Implementation plan and rationale
   - Approach and architecture (with diagram); key ADRs and why
   - Task graph summary: waves, critical path, parallelism used
   - Brownfield: impacted services, modules, APIs and data flows
## 3. Generated artifacts
   | Artifact | Path | Purpose | Traces to |
   (code, OpenAPI contract, schema and migrations, unit and integration tests, docs)
## 4. Orchestration record
   - Events handled, retries, replans, rollbacks, escalations
   - Human gate decisions (from approvals.md)
## 5. Validation approach and results
   - Test strategy summary; verification steps run
   - Results: build, tests (unit/integration), coverage, AC coverage %, contract, schema, regression, security
## 6. Risks, trade-offs and failure scenarios
   - Risk register summary with status; trade-offs made; failure scenarios and their handling
## 7. Assumptions and limitations
   - Assumptions (confirmed / unconfirmed); environment limits; what was not verified
## 8. Follow-ups and reviewer checklist
```
Also produce `traceability.md` (REQ | AC | Task | Files | Test | Result) and `PR_SUMMARY.md` (a condensed version of the summary for the PR description).

## Final response to the user
Keep it short: the mode and outcome in one line; the validation headline (for example, "58/58 tests pass (41 unit, 17 integration), 7/7 ACs covered, no regressions, 0 breaking API changes"); the decisions waiting at a gate or the assumptions needing confirmation; and the delivered files.
