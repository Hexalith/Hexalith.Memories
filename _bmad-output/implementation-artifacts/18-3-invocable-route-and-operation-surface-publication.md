---
baseline_commit: 7a92ce4b667171a55453c8fefd2d8d5bb1e72a2b
---
# Story 18.3: Invocable Route and Operation Surface Publication

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

| Field | Value |
| :---- | :---- |
| Epic | 18 — Downstream Consumer Integration Contract Hardening |
| Story key | `18-3-invocable-route-and-operation-surface-publication` |
| Origin | MEM-3 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27, pass 9-3) |
| Lifecycle track | Engineering / Operational Readiness — Downstream Consumer Integration Hardening. **Not MVP-counted.** |
| Release impact | **None.** Docs + a drift-guard test only — **NO `feat:`**. Use `docs:` / `test:` commits. No `src/` public-contract change, no `tools/release-packages.json` edit, no version bump. (Per Epic 18 release-timing note, only Story 18.4 is release-sensitive.) |
| Deliverable | A published, drift-guarded **route/operation-surface contract** under `docs/operations/` that enumerates the real invocable surface (`/api/*` REST routes + the Dapr pub/sub delivery/discovery routes) so an external Dapr ACL can be verified against real paths. Full OpenAPI/Swagger emission stays explicitly deferred. |
| Parties-side follow-up | Parties corrects the `/process` operation path in `accesscontrol.memories.yaml` and adds an end-to-end ACL assertion against the published surface. |

## Story

As an operator authoring a Dapr access-control policy for Memories,
I want the invocable HTTP route and pub/sub operation surface published,
so that `accesscontrol.memories.yaml` can be verified against real operation paths instead of an unverified `/process` placeholder.

## Acceptance Criteria

**AC1 — Enumerate the real invocable surface and refute `/process`**
**Given** the Parties ACL references an operation path `/process` that does not exist on the Memories surface,
**When** the route surface is published,
**Then** the documentation enumerates the real invocable surface — the `/api/*` REST routes and the Dapr pub/sub subscription endpoint (`[HttpPost("ingest")]` → `/events/ingest`, topic from `MEMORIES_EVENTSTORE_TOPIC`) — and explicitly states that no `/process` operation exists.

**AC2 — Publish in an ACL-verifiable form covering method, path, Dapr operation semantics**
**Given** an external ACL must be machine-verifiable,
**When** this story completes,
**Then** the route surface is published in a form an ACL can be checked against (an OpenAPI document or a maintained route-surface doc under `docs/dev` or `docs/operations`), covering method, path, and Dapr operation semantics.

**AC3 — Guard the published surface against drift**
**Given** the surface can drift as endpoints are added,
**When** the surface is published,
**Then** a test or generation step keeps the published surface in sync with the actual mapped endpoints, or a documented review trigger requires updating it whenever routes change.

**AC4 — Include the Dapr subscription/delivery contract and the publish-via-DAPR statement**
**Given** the Memories Server sidecar manages event delivery,
**When** the route surface is published,
**Then** it includes the DAPR subscription discovery contract (`/dapr/subscribe`) and the pub/sub delivery route (`POST /events/ingest`), and it states that domain modules publish CloudEvents to DAPR rather than invoking Memories REST ingestion for event streams.

## Tasks / Subtasks

- [x] **Task 0 — Preflight: re-verify every cited anchor before writing (Epic 18 mandate).** (AC: 1,2,3,4)
  - [x] Re-extract the full `/api/*` route inventory from `src/Hexalith.Memories.Server/Program.cs`: `grep -nE 'app\.Map(Get|Post|Put|Delete|Patch)\(' src/Hexalith.Memories.Server/Program.cs`. Confirm the count (baseline `7a92ce4` = **45** `/api/*` minimal-API endpoints, no `MapGroup`/route groups) and reconcile the table in Dev Notes against the live source — update any moved line refs or added/removed routes before authoring.
  - [x] Re-confirm `EventIngestionController` route surface: `[Route("events")]` (`src/Hexalith.Memories.EventStore/EventIngestionController.cs:32`) + `[HttpPost("ingest")]` (`:56`) compose `POST /events/ingest`; consts `PubSubName == "pubsub"` (`:38`) and `TopicEnvVar == "MEMORIES_EVENTSTORE_TOPIC"` (`:43`); `[EnvironmentTopic(PubSubName, TopicEnvVar)]` (`:57`) resolves the topic from the env var at startup (`EnvironmentTopicAttribute.cs:45`).
  - [x] Re-confirm subscription discovery wiring `app.MapSubscribeHandler();` (`src/Hexalith.Memories.Server/Program.cs:347`) and middleware order `UseCloudEvents()` (`:345`) → `MapControllers()` (`:346`) → `MapSubscribeHandler()` (`:347`); the controller is registered via `AddControllers().AddApplicationPart(typeof(EventIngestionController).Assembly)` (`EventStoreIntegrationServiceCollectionExtensions.cs:77-78`).
  - [x] Re-confirm health endpoints `/health` `/alive` `/ready` are mapped via `app.MapDefaultEndpoints()` (`Program.cs:337`) → `MapHealthChecks(HealthEndpointPaths.Health/Alive/Ready, …)` (`ServiceDefaults/Extensions.cs:615,617,624`); the path constants live in `ServiceDefaults/Health/HealthEndpointPaths.cs:16,19,22`.
  - [x] Re-confirm the MCP surface `app.MapMcp("/mcp").RequireAuthorization();` (`src/Hexalith.Memories.Mcp/Program.cs:21`) plus its own `MapDefaultEndpoints()` (`:17`) — note the MCP runs under the **separate** Dapr app-id `memories-mcp`, not the ACL target `memories`.
  - [x] Re-confirm **`/process` is absent**: `grep -rn '/process' src/` returns no production route literal (only test/submodule references). Record this as the evidence for AC1's negative claim.
  - [x] If any anchor moved, update this story's route table / line refs before authoring.
- [x] **Task 1 — Author `docs/operations/route-surface.md`.** (AC: 1,2,4)
  - [x] Line 1 = HTML review-cadence comment (mirror `deployment-configuration.md:1`); H1 `# Invocable Route and Operation Surface Contract (Story 18.3)`; one-sentence scope intro; `Origin: MEM-3 …` line; a `> Code is the source of truth.` callout naming the drift-guard test.
  - [x] **Dapr ACL framing section:** state the ACL target is the **Memories Server** Dapr app-id `memories` (default; see `../operations/deployment-configuration.md`), and explain the Dapr operation mapping: REST routes are reached as the `method/<path>` operation segment of Dapr service invocation (`/v1.0/invoke/memories/method/api/...`), and the pub/sub `POST /events/ingest` is a pub/sub-delivery operation (not a service-invocation method). This satisfies "method, path, and Dapr operation semantics" (AC2).
  - [x] **REST `/api/*` surface table:** enumerate ALL `/api/*` routes (method + full path template + one-line purpose), grouped by area (Ingestion, Tenants, Cases, Members, Memory units & annotations, Consistency, Search, Graph, Handlers/diagnostics, Telemetry) — use the canonical inventory in Dev Notes. Mark `/api/handlers` and `/api/tenants/{tenantId}/handlers/mismatches` as **experimental (`HXL002`)** (they emit `X-Memories-API-Experimental: HXL002`; see `../dev/experimental-apis.md`).
  - [x] **Pub/sub event-intake operation section (AC4):** `GET /dapr/subscribe` (discovery) and `POST /events/ingest` (delivery), topic resolved from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`; **state explicitly that domain modules publish CloudEvents to DAPR rather than invoking Memories REST ingestion for event streams.** Cross-link `../dev/eventstore-integration.md` §1.6 and `./deployment-configuration.md` for the deep routing semantics instead of duplicating them.
  - [x] **Health/infra probes section:** list `/health`, `/alive`, `/ready` (Server and MCP, via `MapDefaultEndpoints`) as infrastructure probes; cross-link `../dev/health-checks.md`.
  - [x] **MCP surface note:** `POST /mcp` (JWT-auth, Streamable HTTP) lives under the separate `memories-mcp` app-id; an ACL for `memories` must not reference it. Cross-link `../dev/mcp-server.md`.
  - [x] **The `/process` refutation (AC1):** an explicit section stating no `/process` operation exists anywhere on the Memories surface and that ACLs referencing `/process` use the wrong path (mirror the wording already in `../dev/eventstore-integration.md:137-138`).
  - [x] **"The guarantee (rename = breaking-change-for-consumers)" section** + **"Automated enforcement"** section naming the drift-guard test by path and stating what is test-enforced vs review-enforced + `## References`.
- [x] **Task 2 — Add the route-surface drift-guard test.** (AC: 3)
  - [x] New `Deployment/RouteSurfaceContractTests.cs` in `tests/Hexalith.Memories.Server.Tests/` (mirror `Deployment/DeploymentConfigurationContractTests.cs` exactly: repo-root `.slnx` marker walk, plain `[Fact]`s, no Docker/fixture, Shouldly, ITANEO MIT header, file-scoped namespace `Hexalith.Memories.Server.Tests.Deployment`, no `using Xunit;`).
  - [x] **Forward tie (code → doc) — the core sync guard:** regex-extract every `app.Map(Get|Post|Put|Delete|Patch)("<route>"` literal from `src/Hexalith.Memories.Server/Program.cs` (read via the marker walk) and assert each extracted route template appears verbatim (`Case.Sensitive`) in `docs/operations/route-surface.md`. A new endpoint added without documenting it fails the build.
  - [x] **Count tie:** assert the number of extracted `/api/*` route literals equals the number of `/api/` rows in the doc (defends against silent omission AND phantom rows). Emit the count in the failure message so the Change Log delta is visible.
  - [x] **Pub/sub constant tie (bidirectional):** assert `EventIngestionController.PubSubName == "pubsub"` and that the controller source contains `[Route("events")]` and `[HttpPost("ingest")]`, AND that the doc contains `POST /events/ingest`; assert `app.MapSubscribeHandler()` appears in `Program.cs` and the doc contains `/dapr/subscribe`.
  - [x] **Health constant tie:** assert the doc contains `HealthEndpointPaths.Health` / `.Alive` / `.Ready` values (`/health`, `/alive`, `/ready`) — reflect the constants so a code rename also fails.
  - [x] **`/process` negative assertion (AC1, code-tied):** assert `Program.cs` source and `EventIngestionController.cs` source contain **no** `"/process"` route literal, AND that the doc contains the explicit "no `/process`" statement. This proves AC1's refutation against code, not just prose.
  - [x] **MCP route tie (source-text):** read `src/Hexalith.Memories.Mcp/Program.cs` via the marker walk and assert it contains `MapMcp("/mcp")`, AND the doc documents `/mcp` under the `memories-mcp` app-id. (Document any item left review-enforced in the doc's "Automated enforcement" section — e.g. `/dapr/subscribe` is framework-emitted, so it is doc-presence + `MapSubscribeHandler`-presence enforced, not a literal-string tie.)
  - [x] Negative-proof the guard (rename a route in the doc → test fails; restore → passes) and record it in the Debug Log.
- [x] **Task 3 — Record the OpenAPI deferral + resolve MEM-3.** (AC: 2)
  - [x] In `_bmad-output/implementation-artifacts/deferred-work.md`: update **MEM-3** (currently `carried-forward`, lines ~1420-1425) to `resolved` with an `Evidence:` line pointing at the new doc + test (mirror the MEM-2 `resolved` edit at ~1408-1413). Add a new open-ended entry **`MEM-3-OPENAPI`** (`carried-forward`) tracking the residual **OpenAPI/Swagger document generation** (none exists today — `AddOpenApi`/`MapOpenApi`/Swashbuckle absent), mirroring the `MEM-2-ASPIRATE` precedent. Honor the Story 14.5 schema exactly (`ID`, `Status` ∈ {`open`|`resolved`|`accepted`|`carried-forward`}, `Source story`, `Target artifact`, `Re-open trigger`, and `Evidence:`(resolved) or `Rationale:`(carried-forward)). No follow-up story id is assigned — keep it open-ended. The `CiTestInventoryTests` parser validates these entries.
- [x] **Task 4 — Verify and finalize.** (AC: 1,2,3,4)
  - [x] Build + run the new test via the sandbox workaround (see Dev Notes → Running tests in this sandbox); record the discovery-count delta.
  - [x] Run the full `Server.Tests` suite to confirm no regression; re-run `Cli.Tests` `CiTestInventoryTests` to validate the `deferred-work.md` edits.
  - [x] Update this file's File List, Completion Notes, and Change Log (with the test-count delta) before handoff.

## Dev Notes

### Scope and intent (read first)
This is a **documentation + drift-guard test** story, not a feature. MEM-3's residual gap is precisely: *the real Memories operation surface is not published anywhere an external Dapr ACL author can verify against, and the Parties ACL references a non-existent `/process` path.* The routes already exist in code; this story **publishes** them in one ACL-facing place and **guards** them against drift. Do **not** add OpenAPI/Swagger tooling, do **not** touch public `src/` contracts, do **not** edit `.slnx` / `Directory.Packages.props` / `release-packages.json`. Commits are `docs:` / `test:` only — a `feat:` would trigger an unwanted minor release (project-context release rules; Epic 18 release-timing note: only Story 18.4 is release-sensitive).

**What is genuinely new here vs. 18.2:** the pub/sub *operation* surface (`/dapr/subscribe`, `POST /events/ingest`, the `/process` refutation) is **already** published in `docs/dev/eventstore-integration.md` §1.6 and `docs/operations/deployment-configuration.md` (Story 18.2). The new content is the **full `/api/*` REST route inventory** as the ACL-verifiable surface, consolidated into one operator-facing route-surface contract that *cross-links* (not duplicates) the existing pub/sub docs. Re-publishing the pub/sub routes here is required only because AC1/AC4 demand a single enumerated surface — keep the pub/sub section thin and link out for semantics.

### Decision: maintained route-surface doc, NOT OpenAPI generation
AC2 permits *"an OpenAPI document **or** a maintained route-surface doc under `docs/dev` or `docs/operations`."* The repo has **no** OpenAPI/Swagger setup today (`AddOpenApi`, `MapOpenApi`, `AddSwaggerGen`, `UseSwagger`, Swashbuckle, `Microsoft.AspNetCore.OpenApi` are all absent; no `openapi.json` exists). Standing up OpenAPI generation for 45 minimal-API endpoints + a controller is a larger, separable effort and is **explicitly deferred** (Task 3, `MEM-3-OPENAPI`). The chosen deliverable — a maintained `docs/operations/route-surface.md` + a content-asserting drift-guard test — is the form that (a) satisfies all four ACs, (b) matches the established 18.1/18.2 doc-contract + drift-guard pattern, and (c) carries zero release risk. **Operations** (not `docs/dev`) is the right folder: the consumer is "an operator authoring a Dapr access-control policy", and it sits alongside `docs/operations/deployment-configuration.md` (Story 18.2).

### Canonical route inventory — code is the source of truth (verified at baseline `7a92ce4`)

**Memories Server (Dapr app-id `memories`) — REST `/api/*` minimal-API routes (45), all in `src/Hexalith.Memories.Server/Program.cs`:**

| Area | Method + path | Line |
| :--- | :--- | :--- |
| Ingestion | `POST /api/ingest` | 372 |
| Ingestion | `GET /api/ingest/{instanceId}` | 433 |
| Ingestion | `POST /api/ingest/url` | 439 |
| Ingestion | `POST /api/ingest/directory` | 541 |
| Ingestion | `GET /api/ingest/batches/{batchId}` | 685 |
| Tenants | `GET /api/tenants/{tenantId}/embedding-config` | 754 |
| Tenants | `PUT /api/tenants/{tenantId}/embedding-config` | 778 |
| Tenants | `POST /api/tenants` | 843 |
| Tenants | `GET /api/tenants/{tenantId}/provision-status/{instanceId}` | 908 |
| Tenants | `GET /api/tenants` | 941 |
| Tenants | `GET /api/tenants/{tenantId}` | 955 |
| Tenants | `GET /api/tenants/{tenantId}/configuration` | 973 |
| Tenants | `PATCH /api/tenants/{tenantId}` | 976 |
| Tenants | `DELETE /api/tenants/{tenantId}` | 979 |
| Tenants | `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` | 1105 |
| Tenants | `POST /api/tenants/{tenantId}/verify` | 1137 |
| Consistency | `POST /api/tenants/{tenantId}/consistency/verify` | 1177 |
| Consistency | `GET /api/tenants/{tenantId}/consistency/verify/{instanceId}` | 1228 |
| Consistency | `GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}` | 1259 |
| Consistency | `POST /api/tenants/{tenantId}/consistency/repair` | 1306 |
| Consistency | `GET /api/tenants/{tenantId}/consistency/repair/{instanceId}` | 1356 |
| Export | `GET /api/tenants/{tenantId}/cases/{caseId}/export` | 1391 |
| Export | `GET /api/tenants/{tenantId}/export` | 1462 |
| Cases | `POST /api/tenants/{tenantId}/cases` | 1524 |
| Cases | `GET /api/tenants/{tenantId}/cases` | 1548 |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}` | 1569 |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}/status` | 1591 |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}/failed-units` | 1613 |
| Memory units | `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` | 1644 |
| Memory units | `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest` | 1740 |
| Memory units | `POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest` | 1792 |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}/activity` | 1848 |
| Members | `PUT /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` | 1876 |
| Members | `DELETE /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` | 1916 |
| Members | `GET /api/tenants/{tenantId}/cases/{caseId}/members` | 1941 |
| Memory units | `DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` | 1972 |
| Cases | `DELETE /api/tenants/{tenantId}/cases/{caseId}` | 2018 |
| Annotations | `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations` | 2052 |
| Annotations | `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations` | 2115 |
| Search | `GET /api/search` | 2150 |
| Graph | `GET /api/tenants/{tenantId}/traverse` | 2902 |
| Telemetry | `GET /api/tenants/{tenantId}/telemetry/summary` | 3039 |
| Handlers (exp. `HXL002`) | `GET /api/handlers` | 3063 |
| Handlers (exp. `HXL002`) | `GET /api/tenants/{tenantId}/handlers/mismatches` | 3073 |
| Graph | `PATCH /api/tenants/{tenantId}/edges/confidence` | 3098 |

**Memories Server — non-`/api` operations:**

| Operation | Method + path | Authoritative source | Notes |
| :--- | :--- | :--- | :--- |
| Pub/sub delivery | `POST /events/ingest` | `EventIngestionController.cs:32` (`[Route("events")]`) + `:56` (`[HttpPost("ingest")]`) | CloudEvents intake; content types `application/json`, `application/cloudevents+json`. Topic from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`. |
| Subscription discovery | `GET /dapr/subscribe` | `app.MapSubscribeHandler()` — `Program.cs:347` | Framework-emitted; advertises topic + route `/events/ingest` on `pubsub`. |
| Health | `GET /health`, `GET /alive`, `GET /ready` | `MapDefaultEndpoints()` `Program.cs:337` → `ServiceDefaults/Extensions.cs:615,617,624`; consts `HealthEndpointPaths.cs:16,19,22` | Infra probes; cross-link `../dev/health-checks.md`. |

**Memories MCP (separate Dapr app-id `memories-mcp`) — `src/Hexalith.Memories.Mcp/Program.cs`:**

| Operation | Method + path | Source | Notes |
| :--- | :--- | :--- | :--- |
| MCP transport | `POST /mcp` | `app.MapMcp("/mcp").RequireAuthorization()` `:21` | JWT-Bearer, Streamable HTTP. Tools: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`. **Not** part of the `memories` ACL. |
| Health | `GET /health`, `/alive`, `/ready` | `MapDefaultEndpoints()` `:17` | Infra probes. |

**`/process`:** does **not** exist as a production route anywhere in `src/` (verified `grep -rn '/process' src/` → no route literal; only test/submodule references). This is the evidence for AC1's negative claim.

### Dapr operation semantics to state (AC2)
- The ACL `accesscontrol.memories.yaml` governs the **Memories Server** app-id `memories` (default; overridable via `MEMORIES_DAPR_APP_ID` — see `docs/operations/deployment-configuration.md`). The MCP app-id `memories-mcp` is a separate ACL target.
- A REST route `X /api/foo` is invoked through Dapr service invocation as `X /v1.0/invoke/memories/method/api/foo`; the ACL `operation` is the `method/...` segment with the matching HTTP verb. Make this mapping explicit so an ACL author can translate the table directly.
- `POST /events/ingest` is reached via **pub/sub delivery** (the sidecar POSTs the CloudEvent to the app route), not service invocation; `/dapr/subscribe` is the sidecar's discovery probe. Domain modules **publish CloudEvents to DAPR**, they do not call the REST ingestion routes for event streams (AC4).

### Doc placement and house style (mirror these precedents)
- **Location:** `docs/operations/route-surface.md` (operator/ACL-facing → operations, alongside `deployment-configuration.md`).
- **Shape to mirror** — `docs/operations/deployment-configuration.md` (Story 18.2): line-1 `<!-- Review cadence: … Last reviewed: 2026-06-25 -->`, single H1 citing the story, intro sentence, `Origin:` line, a `> Code is the source of truth.` callout, canonical **tables**, an explicit **"The guarantee (rename = breaking-change-for-consumers)"** section, an **"Automated enforcement"** section naming the guard test by path (test-enforced vs review-enforced), and a `## References` section with `[file.md](./file.md)` cross-links.
- **Avoid duplication:** deep pub/sub routing semantics live in `docs/dev/eventstore-integration.md` §1.3–§1.6 and the deploy-config view in `docs/operations/deployment-configuration.md` §"Pub/sub event-intake deployment surface". Cross-link both; publish only the *enumerated route/operation surface* here.

### Drift-guard test — the established pattern
The repo's doc↔code enforcement mechanism is **a content-asserting test**, not a markdown linter (no markdownlint/doc-lint config exists). Copy the structure of `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` (Story 18.2): repo-root `.slnx` marker walk to resolve files, read the markdown + the authoritative source files, assert literals with `ShouldContain(…, Case.Sensitive, "<message>")`, and tie code-backed names to constants via reflection (`EventIngestionController.PubSubName`). See also `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs` (same marker-walk idiom) and, for a reflection-anchor identity guard, `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs` (Story 18.1).

**Strengthen beyond the 18.2 precedent for the route surface** (AC3's "keeps the published surface in sync with the actual mapped endpoints"):
- The novel guard is the **forward code→doc tie**: regex-extract the `app.MapX("…")` route literals from `Program.cs` and assert each is documented (18.2 only asserted a hand-picked literal list; here the test derives the list from source so a *newly added* endpoint can't slip through undocumented).
- Add a **count tie** (extracted `/api/*` count == documented `/api/` row count) so omissions and phantom rows both fail.
- Keep the `/process` **negative** assertion code-tied (assert the literal is absent from both `Program.cs` and `EventIngestionController.cs`) — this is the one piece that proves AC1's refutation mechanically.
- `Server.Tests` already references Server → EventStore, so `EventIngestionController`/`HealthEndpointPaths` constants resolve for free; the MCP `/mcp` route lives in a project Server.Tests does **not** reference, so tie it via **source-text** read (marker walk to `Mcp/Program.cs`), not reflection. Central package management — add no package versions. A plain `[Fact]` (no fixture/Docker) does not turn the project into an integration test.

### Deferral recording (Task 3)
MEM-3 is currently `carried-forward` in `_bmad-output/implementation-artifacts/deferred-work.md` (~1420-1425). Flip it to `resolved` with an `Evidence:` line (doc + test), exactly as MEM-2 was flipped (~1408-1413). Add a new open-ended `MEM-3-OPENAPI` (`carried-forward`) for the residual OpenAPI/Swagger emission, mirroring `MEM-2-ASPIRATE` (~1414-1419). Honor the Story 14.5 schema — a parser (`CiTestInventoryTests`) validates the `ID` token and `Status` vocabulary verbatim.

### Running tests in this sandbox (mandatory workaround)
`dotnet test` fails here with `SocketException (13)` (VSTest TCP-listener limitation). Build, then run the xUnit v3 dll directly:
```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj
DiffEngine_Disabled=true dotnet exec <…>/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Deployment.RouteSurfaceContractTests
# `-list methods` prints the discovery count for the Change Log delta.
```
`DiffEngine_Disabled=true` stops snapshot tooling from launching a diff tool. (Epic 17 retro Action Item 4; user auto-memory `running-dotnet-tests-in-sandbox.md`.)

### Process guardrails (Epic 17 retro carry-forwards)
- Track the test-count delta in the **Change Log at every phase** (Action Item 5) — count drift was a recurring review finding.
- Keep the **File List current through the QA phase** (Action Item 4) — QA gap-closure that adds tests after the Dev Agent Record is written caused omissions on prior stories.
- Respect `.editorconfig` (4-space C#, CRLF, UTF-8, final newline) and the ITANEO MIT header on any new `.cs`.

### Project Structure Notes
- New doc: `docs/operations/route-surface.md` (operations is the correct folder per `project-context.md` docs-placement rule; consumer is an operator/ACL author).
- New test: `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs`, reusing the existing project's references and central package pins — zero new wiring.
- No production `src/` change expected. A non-fixture `[Fact]` does not turn the project into a Docker test.
- Cross-doc consistency: the new doc and the 18.2 deploy-config doc both touch the pub/sub routes — keep the literals identical (`POST /events/ingest`, `/dapr/subscribe`, `pubsub`, `MEMORIES_EVENTSTORE_TOPIC`) so the existing `DocumentationCompletenessTests` / `DeploymentConfigurationContractTests` stay green.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 18.3 (lines 3498-3524)] — story statement, ACs, Parties-side follow-up; Epic 18 preamble (3429-3444), preflight mandate (3437), release-timing note (3442).
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-27-parties-consumer-integration-contract-hardening.md] — MEM-3 evidence (`/process` placeholder vs real surface).
- [Source: _bmad-output/implementation-artifacts/deferred-work.md (~1420-1425 MEM-3; ~1408-1419 MEM-2 / MEM-2-ASPIRATE precedent)] — deferred-entry schema (Story 14.5) and the `resolved` + open-ended-follow-up pattern to copy.
- [Source: _bmad-output/implementation-artifacts/18-2-deployment-configuration-contract-publication.md] — doc-contract + drift-guard precedent (done, approved); doc house style and test pattern.
- [Source: docs/operations/deployment-configuration.md] — sibling operations contract; pub/sub event-intake surface (§"Pub/sub event-intake deployment surface", lines 45-58) and "Automated enforcement" (75-90) to mirror.
- [Source: docs/dev/eventstore-integration.md §1.6 (lines 126-141)] — existing pub/sub route surface + the `/process` refutation wording (137-138); cross-link target.
- [Source: docs/dev/experimental-apis.md] — `HXL002` experimental marker for `/api/handlers` and `/api/tenants/{tenantId}/handlers/mismatches`.
- [Source: docs/dev/mcp-server.md] — MCP `/mcp` surface + four tools; cross-link for the MCP note.
- [Source: docs/dev/health-checks.md] — health probe semantics; cross-link for `/health` `/alive` `/ready`.
- [Source: src/Hexalith.Memories.Server/Program.cs] — 45 `/api/*` `app.MapX` route literals (see inventory table for lines); `MapDefaultEndpoints()` `:337`; middleware order `UseCloudEvents()` `:345` / `MapControllers()` `:346` / `MapSubscribeHandler()` `:347`.
- [Source: src/Hexalith.Memories.EventStore/EventIngestionController.cs:32,38,43,56,57] — `[Route("events")]`, `PubSubName`, `TopicEnvVar`, `[HttpPost("ingest")]`, `[EnvironmentTopic(...)]`.
- [Source: src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs:77-78] — `AddControllers().AddApplicationPart(typeof(EventIngestionController).Assembly)` registers the controller for `MapControllers()`.
- [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs:598-632] + [Health/HealthEndpointPaths.cs:16,19,22] — health endpoint mapping + path constants.
- [Source: src/Hexalith.Memories.Mcp/Program.cs:17,21] — `MapDefaultEndpoints()`, `MapMcp("/mcp").RequireAuthorization()`.
- [Source: tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs] — drift-guard pattern to copy (marker walk, `ShouldAppearInBoth`, constant ties).
- [Source: _bmad-output/project-context.md] — release-type rules (`docs:`/`test:`, never `feat:` for docs), docs placement, central package management, MIT header, CRLF/editorconfig.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- **Preflight (Task 0):** `grep -nE 'app\.Map(Get|Post|Put|Delete|Patch)\(' src/Hexalith.Memories.Server/Program.cs | wc -l` → **45**, every path + line ref matched the Dev Notes inventory verbatim (no anchors moved). `grep -rn '/process' src/` → only a `Hexalith.Memories.Web/bin/…` build-artifact comment, no production route literal. EventIngestionController anchors confirmed (`[Route("events")]`:32, `PubSubName`:38, `TopicEnvVar`:43, `[HttpPost("ingest")]`:56, `[EnvironmentTopic]`:57); Program.cs middleware order confirmed (`MapDefaultEndpoints`:337, `UseCloudEvents`:345, `MapControllers`:346, `MapSubscribeHandler`:347); `HealthEndpointPaths` `/health`:16 `/alive`:19 `/ready`:22; MCP `MapDefaultEndpoints`:17, `MapMcp("/mcp").RequireAuthorization()`:21; HXL002 header emitted at `Program.cs`:3068,3092.
- **Doc cross-check:** authored doc has exactly **45** backtick-wrapped `` `METHOD /api/…` `` spans, all on table rows; all 45 source paths present verbatim.
- **Forward-tie hardening:** the first forward-tie draft used a bare-substring check, which the Dapr `method/api/search` prose example silently satisfied even when the row was renamed. Strengthened the tie to assert the documented row form `` `<VERB> <path>` `` (verb + path code span), which prose substrings cannot satisfy.
- **Negative proof (Task 2):** renamed the doc row `` `GET /api/search` `` → `` `GET /api/seek` `` → `EveryMappedApiRoute_IsDocumented` **FAILED** with `Mapped route 'GET /api/search' … is not documented as a row`; restored → all 7 green. Confirms the guard is live and resists prose substrings.
- **Sandbox test runner:** `dotnet build` then `DiffEngine_Disabled=true dotnet exec <Server.Tests.dll> -class …` (per `running-dotnet-tests-in-sandbox` memory); `dotnet test` is unusable here (SocketException 13).

### Completion Notes List

- **AC1 (enumerate surface + refute `/process`):** the doc enumerates all 45 `/api/*` REST routes plus the pub/sub operation surface and includes a dedicated "No `/process` operation exists" section; the test ties the refutation to code (`/process` absent from `Program.cs` and `EventIngestionController.cs`).
- **AC2 (ACL-verifiable form: method/path/Dapr semantics):** chose a maintained `docs/operations/route-surface.md` (OpenAPI absent today, explicitly deferred) covering method + full path template per route, with a Dapr-operation-mapping section (service invocation `method/<path>` vs pub/sub delivery).
- **AC3 (drift guard):** `RouteSurfaceContractTests.cs` adds a forward code→doc route tie (derives the list from `Program.cs`), a 45-route count tie, bidirectional pub/sub + health constant ties, an MCP source-text tie, and the code-tied `/process` negative — 7 `[Fact]`s.
- **AC4 (pub/sub + publish-via-DAPR):** the doc includes `GET /dapr/subscribe` and `POST /events/ingest` (topic from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`) and states explicitly that domain modules publish CloudEvents to DAPR rather than invoking Memories REST ingestion for event streams.
- **Task 3:** `MEM-3` flipped `carried-forward` → `resolved` (Evidence = doc + test); new open-ended `MEM-3-OPENAPI` (`carried-forward`) records the deferred OpenAPI/Swagger emission. `CiTestInventoryTests` (48 tests) validates the schema — green.
- **No `src/` production change**; docs + test only (`docs:`/`test:` commits). No `.slnx`/`Directory.Packages.props`/`release-packages.json` edits; no new packages.
- **QA gap-closure pass (`bmad-qa-generate-e2e-tests`, 2026-06-25):** found 3 AC claims that were required but only review-enforced and promoted them to mechanically test-enforced — **AC4** publish-via-DAPR statement (`PublishViaDaprStatement_IsDocumented`), **AC2** Dapr service-invocation operation semantics (`DaprServiceInvocationOperationMapping_IsDocumented`), and **AC1/AC2** the `HXL002` experimental-handler marker as a bidirectional code↔doc tie (`ExperimentalHandlersSurface_IsTiedToCodeAndDocumented`). `RouteSurfaceContractTests` is now **10 `[Fact]`s** (7 → 10). The doc's "Automated enforcement" section was updated to match (the three moved out of the review-enforced bullet). Each new guard was negative-proven by doc mutation (mutate → FAIL, restore → pass). Full `Server.Tests`: **1871 passed, 0 failed, 1 skipped** (1868 → +3); `CiTestInventoryTests` 48 green; build clean (0 warnings, warnings-as-errors). No `src/` change.

### File List

- `docs/operations/route-surface.md` — **new** (dev), **modified** (QA pass: "Automated enforcement" section updated for the 3 promoted ties; **review pass:** "Health constant tie" bullet reworded to describe the strengthened row-anchored tie). Invocable route/operation-surface contract (Story 18.3).
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` — **new** (dev: 7 `[Fact]`s), **modified** (QA pass: +3 `[Fact]`s → **10**; **review pass:** hardened `HealthProbePaths_AreDocumentedFromConstants` to pin each probe to its health-table row rather than a bare path substring — no fact-count change). Drift-guard test.
- `_bmad-output/implementation-artifacts/tests/test-summary-18-3-route-surface.md` — **new** (QA pass). QA test-automation summary.
- `_bmad-output/implementation-artifacts/deferred-work.md` — **modified.** `MEM-3` → `resolved` (Evidence); added `MEM-3-OPENAPI` (`carried-forward`).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — **modified.** `18-3-…` `ready-for-dev` → `in-progress` → `review`.
- `_bmad-output/implementation-artifacts/18-3-invocable-route-and-operation-surface-publication.md` — **modified.** Tasks checked, Dev Agent Record, File List, Change Log, Status.

## Change Log

| Date | Phase | Change | Test count |
| :--- | :--- | :--- | :--- |
| 2026-06-25 | create-story | Initial story context created (ready-for-dev). Documentation + route-surface drift-guard test scope; full `/api/*` route inventory (45 endpoints) + pub/sub/health/MCP operations verified against code at baseline `7a92ce4`; OpenAPI generation confirmed absent → deferred (`MEM-3-OPENAPI`). | n/a (no tests added yet) |
| 2026-06-25 | dev-story Task 0 | Preflight re-verification of every cited anchor against live source at baseline `7a92ce4`: 45 `/api/*` routes (count + paths + line refs) matched, `/process` absent, all pub/sub/health/MCP/HXL002 anchors confirmed. No anchors moved → no story-table updates needed. | 0 added (preflight) |
| 2026-06-25 | dev-story Task 1 | Authored `docs/operations/route-surface.md` (45-route table, pub/sub/health/MCP sections, `/process` refutation, guarantee + automated-enforcement + references). 45 documented rows verified == 45 source routes. | 0 added (doc) |
| 2026-06-25 | dev-story Task 2 | Added `RouteSurfaceContractTests.cs` (7 `[Fact]`s). Forward tie hardened to documented-row form after the prose-substring weakness was found; negative-proof confirmed (rename fails, restore passes). | **+7** (0 → 7 new) |
| 2026-06-25 | dev-story Task 3 | `deferred-work.md`: `MEM-3` → `resolved` (Evidence), added `MEM-3-OPENAPI` (`carried-forward`). Validated by `CiTestInventoryTests` (48 green). | +0 (existing parser) |
| 2026-06-25 | dev-story Task 4 | Full `Server.Tests` suite: **1868 passed, 0 failed, 1 skipped** (new class = 7 of these). `Cli.Tests` `CiTestInventoryTests`: 48 passed. Build clean (0 warnings, warnings-as-errors). | 1868 total / **7 new** |
| 2026-06-25 | qa-generate-e2e-tests | Gap-closure: promoted 3 review-only AC claims to test-enforced — AC4 publish-via-DAPR (`PublishViaDaprStatement_IsDocumented`), AC2 Dapr operation semantics (`DaprServiceInvocationOperationMapping_IsDocumented`), AC1/AC2 `HXL002` experimental marker code↔doc tie (`ExperimentalHandlersSurface_IsTiedToCodeAndDocumented`). Doc "Automated enforcement" section updated to match. Negative-proven (mutate → FAIL, restore → pass). Full `Server.Tests`: **1871 passed, 0 failed, 1 skipped**; `CiTestInventoryTests` 48 green. | 1871 total / **+3** (7 → 10 in class) |
| 2026-06-25 | story-automator-review | Adversarial review (10/10 facts verified passing, build clean, all 4 ACs + every `[x]` task validated against live source: 45 routes, surface complete — one controller only, `/process` absent, deferred-work `MEM-3`→resolved + `MEM-3-OPENAPI` present). One LOW finding auto-fixed: hardened `HealthProbePaths_AreDocumentedFromConstants` (bare `/health` `/alive` `/ready` substring → row-anchored `` `<path>` \| `HealthEndpointPaths.<Name>` `` tie) so a doc-side deletion of the health table can no longer pass on incidental prose/link substrings. Negative-proven (`/healthDRIFT` → FAIL, restore → 10/10). 0 CRITICAL → status `done`. Full `Server.Tests`: **1871 passed, 0 failed, 1 skipped**. | 1871 total / 10 in class (no count change) |

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-06-25 · **Outcome:** ✅ Approve (status → `done`)

### Scope verified
Read all deliverables and validated every claim against live source at baseline `7a92ce4`. Adversarial focus on the story's own claims rather than trusting the Dev Agent Record.

### AC validation (all IMPLEMENTED)
- **AC1 (enumerate + refute `/process`):** doc enumerates all 45 `/api/*` routes + the pub/sub operation surface and has a dedicated "No `/process` operation exists" section. Independently re-ran `grep -rn '/process' src/` (excluding build artifacts) → **no production route literal**. `NoProcessOperation_…` ties the negative to `Program.cs` + `EventIngestionController.cs`. **Verified.**
- **AC2 (ACL-verifiable: method/path/Dapr semantics):** maintained `docs/operations/route-surface.md` covers method + full path per route; Dapr service-invocation mapping (`/v1.0/invoke/memories/method/<path>`) + pub/sub-delivery distinction documented and `DaprServiceInvocationOperationMapping_…`-enforced. OpenAPI correctly deferred (`AddOpenApi`/`MapOpenApi`/Swashbuckle confirmed absent). **Verified.**
- **AC3 (drift guard):** independently confirmed the forward-tie regex extracts **exactly 45** literals (all `/api/*`, each with an immediate string literal so none can be silently missed) and the doc has **exactly 45** row spans (no duplicates) — the forward-tie + count-tie together pin doc rows to exactly the code routes. **Verified.**
- **AC4 (pub/sub + publish-via-DAPR):** `GET /dapr/subscribe` + `POST /events/ingest` (topic `MEMORIES_EVENTSTORE_TOPIC` on `pubsub`) documented; explicit publish-via-DAPR statement present and `PublishViaDaprStatement_…`-enforced. **Verified.**

### Task audit
Every `[x]` task confirmed genuinely done. Spot-checks: no `MapGroup` in `Program.cs` (full-path claim holds); HXL002 header stamped at `Program.cs:3068,3092`; surface is complete (only `EventIngestionController` is registered, one `AddApplicationPart`); `deferred-work.md` `MEM-3` is `resolved` with Evidence and `MEM-3-OPENAPI` is `carried-forward` with Rationale (Story 14.5 schema honored). Cross-link targets and anchors all resolve.

### Findings
| Sev | Finding | Disposition |
| :-- | :--- | :--- |
| LOW | `HealthProbePaths_AreDocumentedFromConstants` asserted bare `/health` `/alive` `/ready` substrings, which also occur in MCP prose, the `../dev/health-checks.md` cross-links, and the References section — so a doc-side deletion of the authoritative health table would not fail the guard (the prose-substring weakness the dev hardened the forward-tie against, left unhardened here). | **Auto-fixed** — pinned each probe to its row form `` `<path>` \| `HealthEndpointPaths.<Name>` ``; negative-proven (`/healthDRIFT` → FAIL; restore → 10/10). |

No HIGH/CRITICAL issues. Two non-blocking observations (no action): the count-tie regex would mis-count if a future author wrote a backtick `` `VERB /api/…` `` span in prose (future footgun, not present today); and `git status` shows `.claude/scheduled_tasks.lock` + `_bmad-output/story-automator/orchestration-*.md` modified but absent from the File List — both are story-automator session bookkeeping, unrelated to this story's deliverables.

### Verification evidence
Build clean (0 warnings, warnings-as-errors). `RouteSurfaceContractTests`: **10 passed / 0 failed**. Full `Server.Tests`: **1871 passed, 0 failed, 1 skipped** (no regression from the hardening; class stays at 10 facts). Test runner: `dotnet build` + `DiffEngine_Disabled=true dotnet exec …` per the sandbox workaround.
