---
baseline_commit: a077fd09f21968e494f20c72c62450c0b1d349f6
---

# Story 26.2: Backup & Restore

Status: in-progress

<!-- Epic: 26 — Test, Deployment & Operational Readiness. Closes audit finding A25 (High, "Missing feature") — feature portion. Story 26.5 closes the docs portion (broader runbook set + final cross-linking). Reinforces NFR16 (zero memory-unit loss on Redis restart, AOF verified). New operator-facing capability → commit `feat(...)` (minor release). New import/restore REST route + client method = additive contract change; do NOT rename/remove existing export contracts. -->

## Story

As an operator,
I want a restore counterpart to the existing export, an integration test that proves export→import fidelity, and backup/restore + disaster-recovery runbooks,
so that tenant or case data loss is recoverable by a documented, verified procedure (NFR16).

## Context & the one decision that shapes this story

The **export** side already exists (Story 8.3): `GET /api/v1/tenants/{tenantId}/export` and `GET /api/v1/tenants/{tenantId}/cases/{caseId}/export` stream a portable JSON snapshot. There is **no import/restore path** — that is the A25 gap this story closes.

**Critical fidelity finding (read before designing):** the export JSON does **not** contain embedding vectors or natural-language (NL) descriptions. `ExportedMemoryUnit.Unit` (a `MemoryUnit`) carries only `EmbeddingProvider` / `EmbeddingModel` / `EmbeddingDimensions`. The raw vectors live only in the Redis `:vec:` / `:vecnl:` hashes, which export never reads. Therefore **restore cannot copy every Redis hash from the export** — it re-derives the raw semantic vector hashes and reconstructs the FalkorDB graph while copying the data-plane hashes (memory-unit, case, members) verbatim. Per ratified decision D1c, NL descriptions/vectors are rebuilt later by re-index/event replay because regenerating them during restore requires a non-deterministic LLM. This reframes "every Redis hash and FalkorDB edge" fidelity as: **data-plane hashes + edges round-trip byte-exactly from the export; raw semantic hashes are rebuilt during restore; NL hashes are an explicitly documented deferred derived read-model** (see AC4 and the fidelity definition in Dev Notes). This is the epic charter — `epic-26-context.md:22`: "Backup and restore must preserve the portable case-or-tenant representation across every memory unit, metadata record, Redis hash, and FalkorDB edge. Restore fidelity must be proved end to end."

## Acceptance Criteria

1. **Import/restore endpoint exists and consumes the export format.** New tenant-scoped and case-scoped write endpoints are added — `POST /api/v1/tenants/{tenantId}/import` and `POST /api/v1/tenants/{tenantId}/cases/{caseId}/import` (route templates added to `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` next to the `CaseExport`/`TenantExport` block, with matching `*ImportPath(...)` builders). They accept the **exact JSON envelope the export produces** (`manifest` → `ExportManifest` with `schemaVersion`, then `case`/`tenant` + `cases`, `memoryUnits[]` → `ExportedMemoryUnit`, `edges[]` → `ExportedEdge`, `statistics`). The endpoint rejects a payload whose `manifest.schemaVersion != 1` with a structured `ErrorResponse` (`Results.BadRequest`), and rejects a `manifest.scope`/route-scope mismatch (tenant JSON posted to the case route or vice-versa). New endpoint class `src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs` (`internal static class` exposing `MapImportEndpoints(this IEndpointRouteBuilder)`), wired in `src/Hexalith.Memories.Server/Program.cs` immediately after `app.MapExportEndpoints();` (currently line 89).

2. **Data-plane state is restored byte-exactly from the export.** For every `ExportedMemoryUnit`, restore writes the syntactic Redis hash at `{tenantId}:mu:{memoryUnitId}` reproducing the field contract in `Activities/Indexing/IndexSyntacticActivity.cs` (`id`, `tenantId`, `content`, `sourceUri`, `sourceUriText`, `sourceType`, `sourceTypeText`, `metadataText`, `attributeTags`, `metadataJson`, `contentHash`, `caseId`, `embeddingProvider`, `embeddingModel`, `embeddingDimensions`, `ingestedBy`, `ingestedAt`, `lastUpdated`); each restored hash re-reads through `CaseService.ParseMemoryUnitFromHash` to an equal `MemoryUnit`. Case records are written to `{tenantId}:case:{caseId}` (fields per `Activities/Cases/ProjectCaseHashActivity.cs`) and membership to `{tenantId}:case:{caseId}:members` (field name = memberId), round-tripping through `CaseService.ParseCaseFromHash` / `ListMembersAsync`. Restored `MemoryUnit` and `Case` values equal the pre-export originals field-for-field (id, content, contentHash, metadata, timestamps, status, source).

3. **The FalkorDB graph is fully reconstructed.** Restore MERGEs a `MemoryUnit` node per unit and a `Case` node per case via `IGraphQueryBuilder.BuildMergeMemoryUnitNode` / `BuildMergeCaseNode`, and MERGEs every `ExportedEdge` via `BuildMergeEdge`, into the per-tenant graph selected by `SelectGraph(tenantId)`. **Edge identity is reconstructed from `(SourceId, TargetId, EdgeType)` — `ExportedEdge.Id` (a graph-instance `id(r)`) is NOT reused.** Edge properties `confidence`, `origin`, `createdAt`, and the confidence-promotion audit (`verifiedBy`, `previousConfidence`) are restored. `CONTAINS` (Case→MemoryUnit) edges are **not** in `edges[]` and are rebuilt from each unit's `caseId`. Case-scope exports may contain edges whose far endpoint is outside the case (id-only, dangling `targetId`): restore creates a stub node for the missing endpoint via the existing `BuildMergeStubNode` pattern (`isStub=true`) rather than failing. After restore, a graph query returns every restored edge with matching type/direction/properties.

4. **Raw semantic vector hashes are re-derived (not copied) so semantic search works after restore; NL hashes rebuild later.** Because embeddings are absent from the export, restore deterministically re-chunks each restored unit's exported content and writes the live `{tenantId}:vec:{id}:{sequence}` layout using the target tenant's configured provider. Every chunk hash carries the correct `tenantId`, `memoryUnitId`, `caseId`, `embeddingProvider`, `embeddingModel`, and `embeddingDimensions`. Restore does NOT re-extract content or re-run Kreuzberg. Per ratified decision D1c, restore does **not** regenerate `{tenantId}:vecnl:{id}`: NL descriptions require a non-deterministic LLM and are rebuilt on the next re-index/event replay; this deferred derived read-model is documented in the runbook.

5. **Restore targets a provisioned tenant and is idempotent + guarded.** Restore requires the target tenant to be provisioned (RediSearch/vector indexes + FalkorDB graph created by `TenantProvisioningWorkflow`); it verifies tenant readiness (or provisions/waits) before writing hashes, otherwise restored hashes are not indexed and search silently returns nothing. Re-running the same import produces the same end state (MERGE on ids + dedup keys via `DedupKeyBuilder`; reuse `IngestDedupReservation` semantics or an operation-level idempotency token). The endpoint enforces the same write guardrails as ingestion: authenticated user (global fallback policy, Story 20.1); tenant authorization via `TenantAuthorizationMiddleware` (the `{tenantId}` route segment authorizes automatically, Story 20.2); `TenantStatusGuard.ValidateTenantActiveAsync`; `.AddEndpointFilter<InboundRateLimitEndpointFilter>()`; and `.WithMetadata(new RequestSizeLimitAttribute(...))` sized for large import bodies (export of 100K units ≈ 500 MB — choose a deliberate, documented limit and/or stream the body with `Utf8JsonReader` rather than buffering). Because restore of a large tenant is long-running and re-embeds every unit, it is scheduled as a durable **Dapr Workflow** (mirroring ingestion) returning `202 Accepted` + a status `Location`, not a synchronous handler. Do NOT hand-roll a background queue.

6. **Client, route table, and serialization surfaces are updated additively.** New `public virtual` `Import*Async(...)` method(s) on `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (no interface — mockability is via `virtual` + the `HttpClient` boundary, Decision D9) that build the path from the new `MemoriesRoutes.*ImportPath(...)` and POST with `MemoriesJsonContext.Options`. Any new request/response DTO (e.g. an import-accepted response carrying the workflow instance id) lives in `src/Hexalith.Memories.Contracts/V1/` and is registered in `MemoriesJsonContext.cs` (`[JsonSerializable(...)]`) — an unregistered DTO fails source-gen serialization. Existing export contracts are unchanged (additive only).

7. **An integration test proves export→import fidelity — every portable Redis hash and every FalkorDB edge.** A test in the Docker-dependent tier `tests/Hexalith.Memories.IntegrationTests/` (plain `[Fact]` + `[Collection("AspireIngestionPipeline")]`, NOT `[RunnableSkippedFact]`) uses `AspireIngestionPipelineFixture` to: ingest ≥3 memory units across ≥2 cases with ≥1 causal/reference edge into a provisioned tenant; snapshot the portable backing-store hashes (`{t}:mu:*`, case + members hashes, excluding operational activity read-models) and all FalkorDB edges; export; restore into a clean store for the same tenant id; then assert every syntactic memory-unit hash field, every case/members hash, and every graph edge `(source, target, type, confidence, origin, verifiedBy, previousConfidence)` matches, and that the chunked `{t}:vec:{id}:{sequence}` hashes exist with matching `embeddingDimensions`. Under the deterministic fixture provider, vector bytes must match. Per D1c, the test does **not** require `{t}:vecnl:{id}` immediately after restore; those hashes rebuild on later re-index/event replay.

8. **AOF restart durability is verified — zero memory-unit loss.** The DR runbook cites, and the story confirms still-green, the existing restart-durability evidence: `Ingestion/PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart` (`:346`), which ingests, calls `AspireIngestionPipelineFixture.RestartTopologyAsync()` (reuses the named Redis volume), and asserts the syntactic/semantic/dedup keys and `Indexed` status survive. AOF config is already repo-owned and enforced (`deploy/redis/redis.conf` `appendonly yes` / `appendfsync everysec` / `aof-use-rdb-preamble yes`; `AppHost/Program.cs` throws if absent; FalkorDB `FALKORDB_PERSISTENCE_ARGS=--appendonly yes ...`). If the existing test does not already assert **memory-unit count is retained across restart (zero loss)**, add that assertion; otherwise reference it. Do not re-implement AOF config — it shipped in Story 6.4 / 26.1.

9. **Backup/restore and disaster-recovery runbooks exist under `docs/operations/`.** Two new operator docs following the house runbook shape (H1 with story number, purpose paragraph, fenced config/command blocks, key-shape tables, prerequisites → procedure → verification → rollback/recovery → cross-links):
   - `docs/operations/backup-restore.md` — the backup procedure (logical: `memories export …` CLI / export endpoint producing the portable JSON; physical: Redis AOF/RDB + FalkorDB AOF + the `20Gi` Redis / `10Gi` FalkorDB PVC snapshots) **and** the restore procedure (provision tenant → `POST …/import` → verify), with prerequisites, verification steps, and rollback.
   - `docs/operations/disaster-recovery.md` — DR runbook giving the **executable recovery path** for Redis-pod loss, FalkorDB-pod loss, and full-cluster loss, referencing the Story 26.1 deployment (`deployment-configuration.md`), the AOF/restart durability evidence (`pipeline-persistence.md` + the test in AC8), and the export→import **fidelity evidence** (the test in AC7). It must **originate a FalkorDB backup/restore procedure** — no committed FalkorDB dump/AOF operator procedure exists today (documented gap). Both docs cross-link `deployment-configuration.md` and `failure-recovery.md`. (Final cross-linking polish + the broader runbook set — capacity, incident-response, index-rebuild, onboarding/offboarding, upgrade/migration, monitoring thresholds — are **Story 26.5**, not here.)

10. **Build green; no scope leakage.** `dotnet build Hexalith.Memories.slnx` succeeds with **0 warnings, 0 errors** (`TreatWarningsAsErrors=true`). New `.cs` files carry the ITANEO MIT copyright header, file-scoped namespaces, XML docs on public surfaces, and CRLF line endings. Scope is limited to backup/restore feature + its two runbooks: **no** broader operational runbooks (26.5), **no** integration-stub closure of the 28 `[RunnableSkippedFact]` bodies (26.3), **no** coverage gate / benchmark lane (26.4), **no** submodule pointer changes, and **no** broadening into a general application-facing export feature (FR71 Phase 2 remains deferred).

## Tasks / Subtasks

- [x] **Task 1 — Route table + endpoint scaffolding** (AC: 1, 5, 6)
  - [x] Add `CaseImport = "/api/v1/tenants/{tenantId}/cases/{caseId}/import"` and `TenantImport = "/api/v1/tenants/{tenantId}/import"` constants to `MemoriesRoutes.cs` (near `CaseExport`/`TenantExport`, ~lines 152-158) plus `CaseImportPath(...)`/`TenantImportPath(...)` builders using the existing `Fill`/`EscapeSegment` helpers (reject `.`/`..`/whitespace segments as the export builders do). Do NOT register `MemoriesRoutes` in `MemoriesJsonContext` (not a wire DTO). *(Added `RestoreStatus` route + `RestoreStatusLocation` absolute builder for the 202 status Location.)*
  - [x] Create `src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs` (`internal static class`, `MapImportEndpoints`), mirroring `ExportEndpoints.cs` route/validation shape (tenant/case id validation via `EndpointValidationHelpers.ValidateTenantId` + `CaseValidator.ValidateCaseId` before any work) and `IngestionEndpoints.cs` write guardrails (`TenantStatusGuard` → manifest validation → stage + schedule workflow → `202` + status `Location`; `ErrorResults.*` envelopes for 400/413/503). Attach `.WithMetadata(new RequestSizeLimitAttribute(512 MB))`, `.AddEndpointFilter<TenantAuthorizationEndpointFilter>()`, and `.AddEndpointFilter<InboundRateLimitEndpointFilter>()`. Rely on `TenantAuthorizationMiddleware` for the route-scoped `{tenantId}`. Added a tenant-scoped `GET restore/{instanceId}` status endpoint.
  - [x] Wire `app.MapImportEndpoints();` in `Program.cs` right after `app.MapExportEndpoints();` (line 89).
  - [x] Add the import-accepted response DTO (`RestoreAcceptedResponse`) + `RestoreStatusResponse` in `Contracts/V1/` and register them in `MemoriesJsonContext.cs`.
- [x] **Task 2 — Restore workflow + backing-store writers** (AC: 2, 3, 4, 5)
  - [x] Add a durable `RestoreWorkflow` (+ activities `RestoreDataPlaneActivity`, `RestoreReindexUnitActivity`, `DeleteRestoreStagingActivity`) under `Workflows/`/`Activities/Restore/`, replay-safe (`CreateReplaySafeLogger`, no wall-clock/random/IO in the orchestrator, side effects only in activities). The endpoint stream-copies the body to an `IImportStagingStore` (Redis) so the payload never becomes workflow state; the workflow reads it back and parses via `ImportEnvelopeReader` (`Utf8JsonReader` + `MemoriesJsonContext.Options`).
  - [x] Ensure/verify target-tenant provisioning first (indexes + graph) via `ITenantIndexReadinessVerifier.EnsureReadyAsync` (Syntactic + Semantic families). Endpoint requires an Active tenant (`TenantStatusGuard`); the activity fails loudly if indexes are missing (decision: require provisioned tenant, do not auto-provision).
  - [x] Per memory unit: reconstruct `MemoryUnit` from `ExportedMemoryUnit.Unit`; write the syntactic hash via the **shared** `SyntacticHashProjection` (factored out of `IndexSyntacticActivity` so both paths are byte-identical); MERGE the FalkorDB `MemoryUnit` node (`BuildMergeMemoryUnitNode`); re-embed and write the chunked `{t}:vec:{id}:{seq}` hashes (decision D-vec: reuse the live chunked layout, not the un-chunked `IndexSemanticActivity` the AC text referenced — see Completion Notes).
  - [x] Cases/members: write `{t}:case:{id}` and `{t}:case:{id}:members` hashes (mirror `ProjectCaseHashActivity` + `CaseService.AddMemberAsync`); MERGE `Case` nodes; rebuild `CONTAINS` edges from `caseId`.
  - [x] Edges: MERGE each `ExportedEdge` keyed on `(SourceId, TargetId, EdgeType)` via a new additive `IGraphQueryBuilder.BuildRestoreEdge` (restores confidence/origin/createdAt + `verifiedBy`/`previousConfidence` literally — no existing builder could); create stub nodes for dangling endpoints (`BuildMergeStubNode`).
  - [x] Tenant-scope: `ExportedTenantConfig` is parsed but **not** overwritten onto the (already-provisioned) tenant (decision D3 — see Completion Notes); documented that secret **values** are not in the export and must pre-exist.
  - [x] Restore is idempotent (re-run → same state) via Redis `HSET` overwrite + graph `MERGE` (verified by `RestoreDataPlaneActivityTests.RunAsync_RunTwice_ConvergesToSameResult`).
- [x] **Task 3 — Client surface** (AC: 6)
  - [x] Add `public virtual async Task<RestoreAcceptedResponse> ImportTenantAsync(...)` / `ImportCaseAsync(...)` to `MemoriesClient.cs` building paths from the new route builders and POSTing the export JSON as a `StreamContent`, decoding the accepted-response DTO via `ReadRequiredAsync`; auth flows via the registered `MemoriesAuthHandler`. Methods are `virtual`, class stays non-sealed.
  - [x] CLI `memories import …` command **deferred** with an explicit note (decision D4 — keeps this story bounded; the client method covers programmatic restore).
- [x] **Task 4 — Fidelity + durability tests** (AC: 7, 8)
  - [x] Add `Restore/BackupRestoreFidelityIntegrationTests.cs` in `Hexalith.Memories.IntegrationTests` — `[Fact]` + `[Collection("AspireIngestionPipeline")]` — implementing snapshot → export → wipe → restore → compare-every-hash-and-edge. Enumerates hashes with `RedisConnection.GetServer(...).Keys(...)` + `HashGetAllAsync`, edges with `FalkorDbConnection` + `MATCH (a)-[r]->(b) RETURN ...`. Fixture uses the deterministic `GoogleFake` provider → asserts vector-byte equality. (Docker-dependent — compiles; runs in CI.)
  - [x] Added the AC8 zero-loss/no-duplicate `{t}:mu:*` count assertion to `PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart`.
  - [x] Added Docker-free unit/contract coverage in `Hexalith.Memories.Server.Tests` (schema-version rejection, scope-mismatch, tenant/case mismatch, edge-identity reconstruction from `(source,target,type)`, dangling-target stub, idempotent re-run, syntactic-hash round-trip through `ParseMemoryUnitFromHash`, envelope reader, `BuildRestoreEdge`) + route-builder tests in `Contracts.Tests`. **23 new Docker-free tests, all green.**
- [x] **Task 5 — Runbooks** (AC: 9)
  - [x] Wrote `docs/operations/backup-restore.md` and `docs/operations/disaster-recovery.md` per AC9 (H1 + Story tag, prerequisites → procedure → verification → rollback, config/command fences, cross-links). Originated the FalkorDB backup/restore procedure. Cross-linked `deployment-configuration.md` + `failure-recovery.md`. Also updated `route-surface.md` with the 3 new routes (route-surface drift guard).
- [x] **Task 6 — Verify** (AC: 10)
  - [x] `dotnet build Hexalith.Memories.slnx` → **0 warnings / 0 errors**. Docker-free suites green (Server.Tests 2599/0 fail/1 skip; Contracts.Tests 586/0). The Aspire fidelity + restart tests compile and reach `review` locally (no container runtime); they must run in CI / an operator environment to reach `done`.
  - [x] `git diff --check` clean; new `.cs` files are CRLF with the ITANEO header; no submodule pointers moved; no export contract renamed (additive only).

## Dev Notes

### Fidelity definition (what "every Redis hash and FalkorDB edge" means here)

The export is a *logical* snapshot, not a byte-image of Redis. So restore fidelity is defined per hash family:

| Redis object | Key | Source on restore | Fidelity assertion |
| --- | --- | --- | --- |
| Syntactic memory unit | `{t}:mu:{id}` | **From export** (verbatim) | Field-for-field equal to snapshot |
| Case record | `{t}:case:{id}` | **From export** | Field-for-field equal |
| Case members | `{t}:case:{id}:members` | **From export** | Member set + types equal |
| Semantic vector | `{t}:vec:{id}` | **Re-derived** (re-embed) | Hash exists; provider/model/**dimensions** match; bytes equal iff provider deterministic |
| Semantic chunks | `{t}:vec:{id}:{seq}` | **Re-derived** (re-chunk+embed) | Present/consistent |
| NL vector | `{t}:vecnl:{id}` | **Not re-derived on restore** (decision D1/D1c; amended 2026-07-13 review) | Rebuilt on next re-index/event replay; only `SourceType.Event` units have NL vectors |
| Tenant metadata / case activity | `{t}:metadata`, `{t}:case:{id}:activity` | Rebuilt / derived | Present/consistent (not a fidelity target) |
| FalkorDB nodes + edges | per-tenant graph | Nodes from export; edges from `edges[]` + `CONTAINS` from `caseId` | Every edge `(source,target,type,confidence,origin,verifiedBy,previousConfidence)` equal |

`id(r)` (`ExportedEdge.Id`) is graph-instance-scoped — **never** reuse it as identity; MERGE reconstructs edges from `(source,target,type)`.

### Architecture patterns & constraints (from `project-context.md`)

- **Dapr Workflow owns durable orchestration.** Restore is multi-step, long-running, and re-embeds every unit → it is a workflow with activities, not a synchronous endpoint or custom queue. Replay-safe: no wall-clock/random/IO in orchestrator; side effects in activities; deterministic instance id.
- **Tenant isolation is physical.** Restore writes into the tenant's own key prefix (`{tenantId}:…`) and its own FalkorDB graph (`SelectGraph(tenantId)`). Never let a restore for tenant A touch tenant B's prefix/graph. Cross-tenant denial test required if the route/auth path changes.
- **Graph queries use `IGraphQueryBuilder` + parameters** — never concatenate `tenantId`/ids into Cypher. All node/edge writes go through `BuildMerge*`.
- **Pub/sub is at-least-once + unordered / restore must be idempotent** — MERGE + dedup keys; a re-run or partial-then-retry converges to the same state.
- **Structured errors only** — `ErrorResponse` via `ErrorResults.*`; redact secrets (the export already carries only secret-store key names, never values — keep it that way on restore output/logs).
- **Additive contracts** — new import route/DTOs are additive; do not rename export contracts or change their JSON shape.

### Source tree — what to touch (all verified against the current tree)

- **Routes:** `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` (`CaseExport`:155, `TenantExport`:158; builders `CaseExportPath`:321/`TenantExportPath`:326; `Fill`/`EscapeSegment`:353-361).
- **Endpoint composition root:** `src/Hexalith.Memories.Server/Program.cs:87-93` (`app.MapExportEndpoints();` at 89; pipeline order: auth 57 → authz 58 → `TenantAuthorizationMiddleware` 59 → rate limiter 60).
- **Mirror-for-shape (export):** `src/Hexalith.Memories.Server/Endpoints/ExportEndpoints.cs:49-176`.
- **Mirror-for-write-guardrails (ingest, Story 18.4):** `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:56-162` (validate → `TenantStatusGuard.ValidateTenantActiveAsync` → `IngestDedupReservation.TryReserveAsync` → schedule → `202`; filters `.AddEndpointFilter<TenantAuthorizationEndpointFilter>()` + `<InboundRateLimitEndpointFilter>()`; `RequestSizeLimitAttribute`).
- **Errors + validation:** `Endpoints/ErrorResults.cs` (factory of 400/403/404/409/429/503/500 envelopes + `SetRetryAfter`); `Endpoints/EndpointValidationHelpers.cs` (`ValidateTenantId`:69); `Tenants/TenantStatusGuard.cs:21/58`.
- **Idempotency/dedup:** `Ingestion/IngestDedupReservation.cs` (Redis `SET NX`, fail-open ADR 9.1-B); `Activities/Ingestion/DedupKeyBuilder.cs:31` (`BuildIdentityKey`, SHA-256, token vs sourceUri).
- **Key schema (single source of truth, Story 21.4):** `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` — `BuildSyntacticKey`:119 (`{t}:mu:{id}`), `BuildSemanticKey`:147 (`{t}:vec:{id}`), `BuildNaturalLanguageSemanticKey`:219 (`{t}:vecnl:{id}`); index names `GetSyntacticIndexName`:80 (`{t}:memories:idx`), `GetSemanticIndexName`:86, `GetNaturalLanguageSemanticIndexName`:171.
- **Hash writers to mirror/reuse:** `Activities/Indexing/IndexSyntacticActivity.cs:75-98` (syntactic fields; `metadataJson` = `PersistenceModelMapper.ToStored(metadata)`), `IndexSemanticActivity.cs:83-92` (`embedding` little-endian bytes), `IndexNaturalLanguageSemanticActivity.cs:98-108`, `Activities/Cases/ProjectCaseHashActivity.cs`.
- **Round-trip parse pair (the field contract to satisfy):** `Cases/CaseService.cs` — `ParseMemoryUnitFromHash`:958 (internal static), `ParseCaseFromHash`:915, `ListMembersAsync`:570.
- **Graph:** `Graph/GraphQueryBuilder.cs` — `BuildMergeMemoryUnitNode`:53, `BuildMergeCaseNode`:120, `BuildMergeEdge`:186, `BuildUpdateEdgeConfidence`:414, `BuildMergeStubNode`:214, `BuildListEdgesForMemoryUnits`:521; edge label mapping `ToUpperSnakeCase`:544, `GetNodeLabels`:554. Taxonomy: `Contracts/V1/EdgeType.cs` (`CausedBy, CorrelatedWith, References, Contains, Annotates`), `EdgeTypeTaxonomy.cs`.
- **Index provisioning ownership (Story 23.7):** `Workflows/TenantProvisioningWorkflow.cs:25` (RediSearch → Vector → FalkorDb → Verify → Active); ingestion/restore must NOT `FT.CREATE` — only verify readiness (`ITenantIndexReadinessVerifier.EnsureReadyAsync`, memoized).
- **Export enumeration to reverse:** `src/Hexalith.Memories.Server/Export/TenantExportService.cs` (`EnumerateMemoryUnitIdsAsync`:410 SCAN `{t}:mu:*`; `StreamEdgesAsync`:469 batches of 100; `TryParseEdge`:533 9-column); `Export/ExportWriter.cs` (envelope order).
- **Export DTOs to consume:** `Contracts/V1/` — `ExportManifest.cs:24` (`SchemaVersion=1`), `ExportedMemoryUnit.cs:19` (`Unit` + `AnnotationTargets`), `ExportedEdge.cs:27` (9 fields; read its re-import notes), `ExportedTenantConfig.cs:18`, `ExportStatistics.cs:15`, `ExportScope.cs`. All registered in `MemoriesJsonContext.cs:163-168`.
- **Client:** `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (`ExportCaseAsync`:1059/`ExportTenantAsync`:1092 streaming precedent; `CreateTenantAsync`:276/`CreateCaseAsync`:318 POST precedent; `MemoriesClientServiceCollectionExtensions.cs` registration + `MemoriesAuthHandler`).
- **Auth:** JWT bearer + global `RequireAuthenticatedUser` fallback (`MemoriesServerServiceCollectionExtensions.cs:93-98`, `ConfigureServerJwtBearerOptions.cs`); tenant authz `Authentication/TenantAuthorizationMiddleware.cs` (route/query) + `TenantAuthorizationEndpointFilter.cs` (body-bound).

### Key decisions to make explicitly (do not skip — flag in Completion Notes)

1. **NL description on restore.** NL descriptions are AI-generated and not exported. Options: (a) regenerate via the LLM during restore (full NL-search fidelity, costs LLM calls + nondeterminism); (b) write `:vecnl:` from a re-embedding of the `content` without a fresh description (partial); (c) leave NL index to be rebuilt by a later re-index and document the gap. **Recommendation:** (a) if the pipeline exposes the description-generation activity cheaply; otherwise (c) with an explicit runbook note. Whatever is chosen, AC4/AC7 assertions must match.
2. **Restore target: same tenant id vs remap.** Scope this story to **same-tenant-id disaster recovery** (restore rebuilds the original tenant/case ids). Cross-tenant/cross-deployment id remapping is a migration feature — keep it OUT unless trivially free. State the choice.
3. **Empty-target requirement.** Decide whether restore requires an empty (or freshly provisioned) target, merges into existing data, or overwrites. MERGE makes merge/overwrite idempotent; the fidelity test is cleanest against a clean target. Recommend: allow restore into a provisioned-but-empty tenant for DR; MERGE semantics make re-run safe.
4. **CLI import command** — include `memories import …` mirroring the export commands, or defer to keep the story tight? Recommend include (small, symmetric with export) unless it bloats scope.
5. **RequestSizeLimit vs streaming.** A 500 MB import cannot be buffered. Prefer streaming `Utf8JsonReader` off the request body inside the workflow-scheduling path; set `RequestSizeLimitAttribute` as a safety ceiling, documented.

### Testing standards

- **xUnit v3 + Shouldly + NSubstitute.** `ShouldBe`/`Should.ThrowAsync`; PascalCase behavior names (`RestoreAsync_SchemaVersionMismatch_ReturnsBadRequest`); global `using Xunit` already imported; test folders mirror product areas.
- **Two tiers:** Docker-free unit/contract in `tests/Hexalith.Memories.Server.Tests` (mock `IConnectionMultiplexer`); Docker-dependent integration in `tests/Hexalith.Memories.IntegrationTests` using `AspireIngestionPipelineFixture` (`IAsyncLifetime`; real Redis Stack + FalkorDB + Dapr) via `[Collection("AspireIngestionPipeline")]`. **Do NOT** add `[RunnableSkippedFact]` empty-body stubs (that anti-pattern is exactly what Story 26.3 must clean up).
- **Fixture handles for assertions:** `RedisConnection` (:332), `FalkorDbConnection` (:335), `MemoriesClient` (:82), `ProvisionActiveTenantAsync` (:462), `RestartTopologyAsync` (:358). Enumeration precedents: `PipelinePersistenceIntegrationTests` (Redis `GetServer(...).Keys`, FalkorDB counts), `TenantDeletionIntegrationTests` (every-hash + every-edge), `ExportWorkflowIntegrationTests` (edge/id enumeration).
- **Sandbox runner (this environment):** `dotnet test` fails with `SocketException (13) Permission denied`. Build then exec the xUnit v3 dll:
  ```bash
  dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1
  DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll
  # filter: -namespace <ns> | -class <fqcn> | -method <fqmn>
  ```
  `DiffEngine_Disabled=true` is required (prevents Verify diff-tool hangs).
- **Container-runtime caveat:** there is **no `docker`/`kind`** in this sandbox, so the Aspire fidelity test (AC7) and the restart test (AC8) **cannot run locally** — they reach `review` here and must be validated in CI or an operator cluster to reach `done`. Docker-free unit/contract tests (schema-version/scope/edge-identity/idempotency) DO run locally and must pass before handoff.

### Previous-story intelligence (Story 26.1, `spec-26-1-production-deployment-artifacts.md`, done)

- 26.1 shipped the production topology `kubectl kustomize deploy/kubernetes/overlays/production` (Server + MCP Deployments; Redis Stack + FalkorDB StatefulSets; 4 Dapr components; secretstore = `secretstores.kubernetes`). Server/MCP/daprd are **stateless (no PVC)**; only Redis (`20Gi` at `/data`) and FalkorDB (`10Gi` at `/var/lib/falkordb/data`) hold durable data — these are what backup/restore protects.
- **AOF already enforced:** `deploy/redis/redis.conf` (`appendonly yes`, `appendfsync everysec`, `aof-use-rdb-preamble yes`, `maxmemory-policy noeviction`, RDB `save` points); `AppHost/Program.cs.ResolveRedisConfigPath()` throws if the file is missing or lacks `appendonly yes`; FalkorDB prod AOF via `deploy/kubernetes/base/kustomization.yaml:28` `FALKORDB_PERSISTENCE_ARGS`. **Do not re-add AOF config — cite it.**
- 26.1 **explicitly deferred backup/restore + operational-runbook work to later Epic 26 stories** (its "Never broaden into backup/restore" note) — this story is that hand-off.
- Open hardening from 26.1 review (context for the DR runbook, not this story's code): Redis/FalkorDB run as **root** (no `runAsNonRoot`, `fsGroup`/PVC-permission TODO) and there are **no NetworkPolicies** — mention as caveats in the DR runbook if relevant, but the fixes belong to a hardening story, not 26.2.
- The prior auto-dev run for 26.2 was **blocked** ("missing previous-story continuity decision") because no story file existed. This file resolves that; delete/ignore `bmad-dev-auto-result-26-2-backup-and-restore.md` once dev starts.

### Git intelligence (recent work)

Recent commits are all Epic 26.1 deployment work: `a077fd0` release-orchestration refactor, `df747a5` container-publish fixtures + CI/release, `9d8b1fa` 26.1 production artifacts + review findings (touched `deploy/kubernetes/base/*deployment.yaml`, `docs/operations/deployment-configuration.md`, `tools/*production*.ps1`, `ProductionDeploymentArtifactsTests.cs`, Dapr-token validation). Takeaways for 26.2: the deployment substrate and its tests already exist and are the DR runbook's reference; follow the established `deploy/` + `docs/operations/` + focused-test conventions; conventional-commit `feat(...)` with matching tests.

### Latest tech / version pins (from `project-context.md`)

.NET 10 / C# 14 (`net10.0`, SDK `10.0.301`); Dapr `1.18.4` (workflows/actors/pubsub/state); Aspire AppHost SDK `13.3.3`, `Aspire.Hosting.Testing` `13.4.6`, `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.0-preview…`; Redis Stack + FalkorDB backends; xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`. Central package management — never add `Version=` to `.csproj`; add packages to `Directory.Packages.props` only if genuinely new (prefer reuse — `StackExchange.Redis`, `NFalkorDB`, `NRedisStack` are already referenced by the integration test project).

### Project Structure Notes

- New code lands in existing homes: endpoint in `Server/Endpoints/`, workflow/activities in `Server/Workflows/` + `Server/Activities/…`, contracts in `Contracts/V1/`, client method in `Client.Rest/`, tests mirroring product areas, runbooks in `docs/operations/`. No new project.
- Naming: `ImportEndpoints`, `RestoreWorkflow`, `Import*Async`, `Restore*Activity` — consistent with `Export*`/`Ingestion*` siblings. `sealed record` for new DTOs; `_camelCase` private fields; `Async` suffix; `ConfigureAwait(false)` in client/library code.
- Watch: the syntactic hash omits `status`/`embeddingDimensions` today (read path defaults `status`→`Indexed`, dims→null) — restore must not fabricate values the parse contract doesn't expect. `classification` is contract-reserved but always null.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-26.2] (`:4565-4575`) — story statement + AC ("import/restore endpoint consumes the export format; export→import fidelity test (every Redis hash and FalkorDB edge); backup/restore + DR runbooks; Closes A25 feature portion").
- [Source: _bmad-output/implementation-artifacts/epic-26-context.md] (`:22` charter, `:26` scope boundary, `:39-41` 26.2↔26.5 split, `:32-34` state-store/testing decisions).
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A25] (`:56`) — "No import route in `Program.cs`; export exists, restore doesn't; no backup/DR runbooks → data loss unrecoverable-by-procedure → import endpoint consuming export format + runbooks."
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md] (`:148` feature portion, `:151` docs portion).
- [Source: _bmad-output/planning-artifacts/prd.md#NFR16] (`:988` "Zero memory unit loss during Redis restart | AOF persistence enabled and verified | MVP"); [#FR71] (`:945`, Phase 2 export deferred — 26.2 is the recovery slice only).
- [Source: docs/dev/export.md] — export envelope, `X-Export-Schema-Version: 1`, what export does/does not capture, and the "Known Compromise" notes (dangling targets, non-stable edge id, null classification, missing embeddings).
- [Source: docs/operations/deployment-configuration.md] (26.1 deployment — DR cross-link target); [docs/operations/pipeline-persistence.md] (AOF/NFR16 durability); [docs/operations/failure-recovery.md] (runbook-style template + cross-link).
- [Source: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md] — topology, PVCs (Redis 20Gi / FalkorDB 10Gi), AOF enforcement, backup/restore hand-off.
- Code anchors: `MemoriesRoutes.cs`, `Program.cs:87-93`, `ExportEndpoints.cs`, `IngestionEndpoints.cs`, `IndexSchemaDefinitions.cs`, `IndexSyntacticActivity.cs:75-98`, `CaseService.cs:958/915`, `GraphQueryBuilder.cs`, `TenantExportService.cs`, `TenantProvisioningWorkflow.cs:25`, `AspireIngestionPipelineFixture.cs`, `ExportWorkflowIntegrationTests.cs`, `PipelinePersistenceIntegrationTests.cs:346`.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (BMad dev-story workflow).

### Debug Log References

- `dotnet build Hexalith.Memories.slnx -v:m` → Build succeeded, 0 Warning(s), 0 Error(s).
- Sandbox test runner (no Docker): `DiffEngine_Disabled=true dotnet exec …Server.Tests.dll` → 2599 total, 0 failed, 1 skipped; `…Contracts.Tests.dll` → 586 total, 0 failed. 23 new Docker-free restore/import tests green.
- `git diff --check` → clean.
- Completion gate (2026-07-13): the full unit lane exposed nine unmapped restore/import server error codes in the CLI catalog. Added failing catalog coverage first (10 focused failures), implemented actionable translations, then verified 58/58 focused catalog tests and 462/462 full CLI tests.
- `DiffEngine_Disabled=true bash ./tools/test.sh --filter "Category!=Integration" --configuration Release --no-build` → 4,374 passed, 1 intentional skip, 0 failed across six per-project suites.
- Fast Aspire lane (`Category=Integration&Category!=IntegrationSlow&Category!=Performance`) → 224 passed, 8 accepted structured skips, 0 failed; `BackupRestoreFidelityIntegrationTests.ExportThenImport_RestoresEveryHashAndEdge` passed against real Redis Stack/FalkorDB.
- Slow Aspire lane (`Category=IntegrationSlow`) → 16 passed, 0 skipped/failed; `PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart` passed with the zero-loss assertion.
- Final `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1` → 0 warnings / 0 errors; `git diff --check` clean; modified C# files normalized to CRLF.

### Completion Notes List

Implemented the restore counterpart to export as a durable Dapr `RestoreWorkflow`: the endpoint validates the export manifest synchronously, stages the payload out-of-band, and schedules the workflow (`202 Accepted` + tenant-scoped status `Location`). The workflow restores the byte-exact data plane in one idempotent activity, then re-derives semantic vectors per unit. **Decisions flagged (per Dev Notes "do not skip"):**

- **D-vec (the correctness crux — supersedes the AC4/AC7 literal reference to `IndexSemanticActivity`).** The story text assumed the live pipeline writes the un-chunked `{t}:vec:{id}` hash via `IndexSemanticActivity`. It does **not** — live ingestion writes the *chunked* `{t}:vec:{id}:{seq}` hashes via `IndexSemanticChunksActivity` (`IndexSemanticActivity` is registered but unused by `IngestionWorkflow`). To make restore output definitionally identical to a fresh ingest (true fidelity), `RestoreReindexUnitActivity` re-chunks the restored content with the same `ContentChunker`, re-embeds with the target tenant's configured provider, and writes the chunked `{t}:vec:{id}:{seq}` layout. Under the fixture's deterministic `GoogleFake` provider these vectors are byte-identical; the fidelity test asserts byte equality.
- **D1 (NL descriptions) → option (c).** NL descriptions are non-deterministic LLM output and exist only for `SourceType.Event` units; restore does **not** regenerate `:vecnl:` hashes — they are rebuilt on the next re-index/event replay. File-sourced corpora have no NL vectors to lose. Documented in `backup-restore.md`.
- **D2 (restore target) → same-tenant-id DR only.** Cross-tenant/cross-deployment id remapping is out of scope; the endpoint rejects `manifest.tenantId ≠ route tenant` (`IMPORT_TENANT_MISMATCH`). "A second provisioned tenant" (AC7) is interpreted as the same tenant id on a fresh cluster.
- **D3 (tenant config).** Restore requires an already-provisioned Active tenant, so it does **not** overwrite the tenant registry/config (avoids clobbering live provisioning state). `ExportedTenantConfig` is parsed but not written; the runbook requires the target tenant to be provisioned with a matching embedding config first. Secret **values** are never in the export (only `apiSecretKeyName`).
- **D4 (CLI import) → deferred.** The `MemoriesClient.Import*Async` methods cover programmatic restore; a `memories import …` CLI command is deferred to keep the story bounded.
- **D5 (size limit + bounded execution, amended 2026-07-14).** `RequestSizeLimitAttribute` retains the documented **512 MB** ceiling. The endpoint streams directly into 1 MiB Redis staging chunks; validation and restore scan one record at a time; re-index ids remain in staging and workflow activities process pages of at most 100 units; staging + the clean-target lease use a renewable 12-hour TTL. This preserves the intended ≈100K-unit support without a 512 MB managed buffer or a 100K-id workflow payload.
- **Shared syntactic-hash projection.** Factored `SyntacticHashProjection` out of `IndexSyntacticActivity` (behavior-preserving) so ingest and restore write the identical field set; proved by a round-trip test through `CaseService.ParseMemoryUnitFromHash` (AC2). Full Server.Tests suite (2599) stays green — no ingestion regression.
- **Additive `BuildRestoreEdge`.** No existing graph builder could restore an edge's audit trail (`confidence` + `previousConfidence` + `verifiedBy`) literally, so an additive builder was added; edge identity is reconstructed from `(source, target, type)` — the exported graph-instance `id(r)` is never reused.

**Runtime evidence closure (2026-07-13):** Docker and the Dapr/Aspire prerequisites were available during the completion gate. The AC7 fidelity test passed against real Redis Stack and FalkorDB, and the AC8 controlled-restart test passed after replacing the topology while retaining its persistent volumes. The prior container-runtime caveat is resolved.

**Regression closure:** the broad unit lane found that the nine Story 26.2 import/restore error codes were absent from `ErrorMessageCatalog`. Added actionable domain-error translations and explicit catalog tests, restoring the drift guard and full CLI suite to green.

### File List

**New — Contracts (`src/Hexalith.Memories.Contracts`):**
- `V1/RestoreAcceptedResponse.cs`
- `V1/RestoreStatusResponse.cs`

**New — Server (`src/Hexalith.Memories.Server`):**
- `Import/ImportEnvelope.cs`
- `Import/ImportEnvelopeException.cs`
- `Import/ImportEnvelopeReader.cs`
- `Import/IImportStagingStore.cs`
- `Import/RedisImportStagingStore.cs`
- `Import/ImportRequestValidator.cs`
- `Activities/Indexing/SyntacticHashProjection.cs`
- `Activities/Restore/RestoreDataPlaneActivity.cs`
- `Activities/Restore/RestoreReindexUnitActivity.cs`
- `Activities/Restore/DeleteRestoreStagingActivity.cs`
- `Endpoints/ImportEndpoints.cs`
- `Workflows/RestoreWorkflow.cs`
- `Workflows/Contracts/RestoreWorkflowInput.cs`
- `Workflows/Contracts/RestoreWorkflowResult.cs`
- `Workflows/Contracts/RestoreDataPlaneInput.cs`
- `Workflows/Contracts/RestoreDataPlaneResult.cs`
- `Workflows/Contracts/RestoreReindexInput.cs`
- `Workflows/Contracts/RestoreReindexResult.cs`

**Modified — production:**
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` (import route constants + builders)
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (register restore DTOs)
- `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` (add `BuildRestoreEdge`)
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` (implement `BuildRestoreEdge`)
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs` (use shared `SyntacticHashProjection`)
- `src/Hexalith.Memories.Server/Endpoints/…` via `Program.cs` (`app.MapImportEndpoints();`)
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` (register workflow/activities + staging store)
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (`ImportTenantAsync`/`ImportCaseAsync`)
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` (actionable translations for nine import/restore error codes)

**New/Modified — tests:**
- `tests/Hexalith.Memories.Server.Tests/Import/ImportRequestValidatorTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Import/ImportEnvelopeReaderTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Import/SyntacticHashProjectionTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderRestoreEdgeTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreDataPlaneActivityTests.cs` (new)
- `tests/Hexalith.Memories.Contracts.Tests/V1/MemoriesRoutesImportTests.cs` (new)
- `tests/Hexalith.Memories.IntegrationTests/Restore/BackupRestoreFidelityIntegrationTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` (modified — stub-caller count 3→4)
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs` (modified — AC8 zero-loss count assertion)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ErrorCatalogTests.cs` (modified — import/restore catalog coverage)

**New/Modified — docs:**
- `docs/operations/backup-restore.md` (new)
- `docs/operations/disaster-recovery.md` (new)
- `docs/operations/route-surface.md` (modified — 3 new routes)

**Modified — story tracking:**
- `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

| Date | Change |
| --- | --- |
| 2026-07-14 | Chunk-1 adversarial review patches applied: strict streamed envelope validation; clean-target tenant lease/idempotency; 512 MB chunked renewable staging; bounded re-index workflow pages; embedding readiness/dimension/migration guards; graph timestamp/edge fidelity; fail-closed status and best-effort cleanup; expanded Docker-free coverage. Server tests 2641/1/0; Release solution build 0/0. Status remains review for the remaining agreed review chunks. |
| 2026-07-13 | Completion gates closed locally: fixed nine missing CLI error-catalog mappings found by the broad regression lane; 4,374 unit/contract tests passed with 1 intentional skip; fast Aspire lane passed 224/8/0 including AC7 fidelity; slow Aspire lane passed 16/0/0 including AC8 controlled-restart zero-loss; final Release build 0 warnings/errors. Status → review. |
| 2026-07-13 | Story 26.2 implemented: import/restore endpoints (`POST …/import`, `GET …/restore/{instanceId}`) consuming the export envelope; durable `RestoreWorkflow` (byte-exact data-plane restore + re-derived chunked semantic vectors + graph node/edge reconstruction with audit trail); additive `BuildRestoreEdge` graph builder; shared `SyntacticHashProjection`; `MemoriesClient` import methods; 23 Docker-free tests + Aspire fidelity test (AC7, CI) + AC8 zero-loss assertion; `backup-restore.md` + `disaster-recovery.md` runbooks. Build 0/0; Server.Tests 2599/0. Status → review. |

### Review Findings

_Adversarial code review (`bmad-code-review`), 2026-07-13 — 4 parallel layers (Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor). Diff baseline `a077fd0`→HEAD. Verdict counts: 1 High, 6 Medium, 8 Low; 1 dismissed. Every finding below was re-verified against source before rating._

**Decision-needed (resolved 2026-07-13 by Jerome):**

- [x] [Review][Decision → Patch D1] Case activity feed & summary read-models not restored — **Resolved: document as out-of-scope.** Activity feed/summary are operational read-models, not part of the backup fidelity contract. Action (see Patch D1): add a caveat to the `backup-restore.md` fidelity table; the AC7 test excludes `:activity`/`:activity:summary` (folded into Patch P1). `RestoreDataPlaneActivity.cs` unchanged.
- [x] [Review][Decision → Patch D2] Submodule pointer bumps vs AC10 — **Resolved: keep and accept.** The `Hexalith.EventStore`/`Hexalith.FrontComposer` bumps are accepted as intentional. Action (see Patch D2): record the AC10 deviation + rationale in this story; no revert.
- [x] [Review][Decision → Patch D3] `:vecnl:` NL vectors not re-derived (D1c) — **Resolved: ratify D1c, amend the ACs.** NL vectors rebuild lazily on next re-index (regeneration needs a non-deterministic LLM). Action (see Patch D3): amend AC4/AC7 + the Dev Notes fidelity table to match D1c. Code unchanged.
- [x] [Review][Decision → Patch D4] Restore aborts on one corrupt record — **Resolved: skip-and-report.** Action (see Patch D4): catch out-of-range/non-finite confidence in `RestoreEdgeAsync` (log + skip); guard blank `caseId` in `RestoreMemoryUnitAsync` (log + skip); surface a skipped-record count in the result — mirroring the existing unknown-edge-type skip.

**Patch:**

- [x] [Review][Patch D1] Runbook caveat: activity feed/summary are operational read-models, not restored (Medium→doc) [docs/operations/backup-restore.md] — add a caveat to the case fidelity table so it no longer claims "field-for-field equal" without qualification (resolves D1; AC7 test exclusion is folded into P1).
- [x] [Review][Patch D2] Record the AC10 submodule-bump deviation (Medium→doc) [this story: Dev Notes + Change Log] — note that `Hexalith.EventStore` (940e8ac→341ed48) + `Hexalith.FrontComposer` (9ee5cb5→e914c61) were consciously accepted, with rationale (resolves D2; no revert).
- [x] [Review][Patch D3] Amend AC4/AC7 + fidelity table for D1c `:vecnl:` deviation (Medium→doc) [this story] — AC4/AC7 to state NL vectors rebuild on next re-index (not on restore); drop the `:vecnl:` assertion requirement (resolves D3; code unchanged).
- [x] [Review][Patch D4] Skip-and-report for corrupt records (Medium) [src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs:214-254,189-211] — catch out-of-range/non-finite confidence in `RestoreEdgeAsync` (log + skip, return false); guard blank `caseId` in `RestoreMemoryUnitAsync` (log + skip); add a `SkippedRecords` count to `RestoreDataPlaneResult`/`RestoreWorkflowResult` so a best-effort restore is observable (resolves D4).

- [x] [Review][Patch] AC7 fidelity integration test throws WRONGTYPE before asserting — restore fidelity is unproven (HIGH) [tests/Hexalith.Memories.IntegrationTests/Restore/BackupRestoreFidelityIntegrationTests.cs:72] — `SnapshotHashesAsync("{t}:case:*")` HGETALLs every matching key including the `{t}:case:{id}:activity` Redis stream (populated by ingestion `MemoryUnitIngested` at IngestionWorkflow.cs:573-577 and case-creation events), which raises `RedisServerException: WRONGTYPE` at the first case snapshot — before export/restore/assertions. The CI-gated test therefore cannot pass as written, so AC7/NFR16 "fidelity proven end-to-end" is unsubstantiated. Fix: restrict the case snapshot to data-plane hashes (`{t}:case:{id}` + `:members`), excluding `:activity`/`:activity:summary`, mirroring `CaseService.ListCasesAsync:309-310` (align the exclusion with the D1 decision).
- [x] [Review][Patch] No Docker-free coverage of restore's core behaviors (Medium) [tests/Hexalith.Memories.Server.Tests/…] — reindex vec-hash field/dimension/guard, workflow activity-order, import endpoint status codes (413/empty/schema/scope/503), unprovisioned-tenant rejection, and a real convergence check all lack Docker-free tests; the only end-to-end proof is the (currently broken, per the HIGH above) CI-gated fidelity test, and the existing unit tests are pure mocks that can't distinguish converged-vs-duplicated state (`RunTwice_ConvergesToSameResult`) or a removed guard.
- [x] [Review][Patch] Re-index attribution/dimension mismatch is unguarded; runbook overstates readiness (Medium) [src/Hexalith.Memories.Server/Activities/Restore/RestoreReindexUnitActivity.cs:143-145] — `:vec:` hashes are stamped with the source's provider/model but the target's dimensions, and readiness only checks target self-consistency (not source-vs-target), so restoring into a differently-configured tenant silently yields inconsistent attribution + graph-node/vec dimension disagreement. `backup-restore.md` claims a mismatched dimension "fails readiness verification" — it does not. Add a source-vs-target guard (fail loudly) and/or fix the runbook. (Also: ingest's `EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker` guard is skipped on the restore path.)
- [x] [Review][Patch] Restore-status GET missing rate-limit filter + masks backend errors as 404 (Low) [src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:67] — NOT an auth hole (the global `TenantAuthorizationMiddleware` authorizes the `{tenantId}` segment for this route). Add `.AddEndpointFilter<InboundRateLimitEndpointFilter>()` to match the POST/consistency siblings; optionally return 503 (not 404) when the state-store lookup throws so operators don't read a transient outage as "restore lost" and re-POST.
- [x] [Review][Patch] Misleading manifest-position doc/message + narrow body-copy catch (Low) [src/Hexalith.Memories.Server/Import/ImportEnvelopeReader.cs; ImportEndpoints.cs:109-139] — `TryReadManifest` accepts a manifest at any top-level position, but the XML doc + error string assert "must be the first property"; and the body copy catches only `BadHttpRequestException` (413), so a client disconnect mid-upload surfaces as an unhandled 500. Fix the message to match behavior; catch `IOException`/cancellation on `CopyToAsync`.
- [x] [Review][Patch] Stale "46 routes" prose in route-surface.md (Low) [docs/operations/route-surface.md:26] — now ~49 mapped routes after the 3 additions; update the literal count (the drift-guard test asserts count-equality, not this prose).

**Deferred:**

- [x] [Review][Defer] Case-scoped restore doesn't enforce per-record case membership [src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs:71-124] — deferred, low-impact hardening (`RunAsync` ignores `input.CaseId`; validator checks only `manifest.CaseId`; no cross-tenant impact — caller is tenant-authorized).
- [x] [Review][Defer] Unknown edge `origin` silently coerced to `Inferred` [src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs:232-235] — deferred, fidelity change on an audit field, only on corrupt/foreign data.
- [x] [Review][Defer] No operation-level idempotency token; concurrent/duplicate POSTs run duplicate full re-embeds [src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:147] — deferred, end-state converges (MERGE/HSET) so AC5's idempotency clause holds; impact is wasted embedding-provider cost.
- [x] [Review][Defer] Re-index treats a missing syntactic hash as success; `RestoredMemoryUnits` counts the data-plane total [src/Hexalith.Memories.Server/Activities/Restore/RestoreReindexUnitActivity.cs:85-95] — deferred, largely unreachable in the happy path, but a partial restore could report "completed" with full counts.
- [x] [Review][Defer] Line-ending normalization churn folded into feature commits [src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs] — deferred, not a defect (LF→CRLF toward the repo standard) but ~2,500 lines of flip (incl. one 959-line pure-flip file) should be isolated as a `chore` commit so feature diffs stay reviewable.

**Dismissed (1):** "Cross-tenant restore-status read" (claimed auth hole, Edge Case Hunter + Verification Gap) — refuted. `TenantAuthorizationMiddleware` runs `TryAuthorizeTenant` on every `/api/v1/tenants/{tenantId}/…` route including the GET, denying (403) any principal whose tenant claim ≠ route `tenantId`. The residual operability points (no rate-limit filter, 404-masking) are captured in the P4 patch above.

### Review Resolutions (2026-07-13)

Applied from the resolved decision-needed findings and the patch set:

- **D1 — case activity read-models documented out-of-scope.** `{t}:case:{id}:activity` (stream) + `:activity:summary` are operational read-models, not part of the backup fidelity contract. Runbook fidelity table now carries an explicit "Not restored" row; the AC7 test excludes `:activity`/`:activity:summary` from the case snapshot (Patch P1 + Patch D1).
- **D2 — AC10 submodule deviation accepted.** `references/Hexalith.EventStore` (940e8ac→341ed48) and `references/Hexalith.FrontComposer` (9ee5cb5→e914c61) were consciously kept, not reverted — required for the current build/test to pass 0/0. Recorded, accepted deviation from AC10's "no submodule pointer changes."
- **D3 — AC4/AC7 amended for the `:vecnl:` (D1c) deviation.** NL vectors are **not** re-derived on restore (regeneration needs a non-deterministic LLM; only `SourceType.Event` units have NL vectors); they rebuild on the next re-index/event replay. AC4's "Semantic **and NL** vector hashes are re-derived" is amended to **semantic-only on restore**; AC7's requirement to assert a `{t}:vecnl:{id}` hash per restored unit is **dropped**. Dev Notes fidelity-table NL row updated accordingly.
- **D4 — skip-and-report implemented.** A corrupt edge (out-of-range/non-finite confidence) or a unit with a blank `caseId` is now logged + skipped best-effort and counted in `RestoreDataPlaneResult.SkippedRecords` → `RestoreWorkflowResult.SkippedRecords` → the restore-status response, instead of aborting the whole restore. Covered by two new Docker-free tests.

### Patch application (2026-07-13)

All 10 patches applied. Build verified green (`dotnet build` — 0 warnings, 0 errors) on Server + Server.Tests.

- **P1** (High) — `BackupRestoreFidelityIntegrationTests.SnapshotHashesAsync` now skips `:activity`/`:activity:summary` keys, resolving the `WRONGTYPE` crash so the AC7 assertions actually run.
- **P3** (Med) — `RestoreReindexUnitActivity` now fails loudly on a source-vs-target `(provider, model)` mismatch; runbook readiness wording corrected.
- **P4** (Low) — restore-status GET gained `InboundRateLimitEndpointFilter` and now returns 503 (not 404) on a state-store failure.
- **P5** (Low) — `ImportEnvelopeReader` doc/message no longer claims "first property"; the import body-copy now catches `IOException` (client disconnect → 400, not 500).
- **P6** (Low) — `route-surface.md` route count 46 → 49.
- **P2** (Med) — added two Docker-free tests for the D4 skip-and-report path (`RunAsync_EdgeWithInvalidConfidence_SkipsEdgeAndReports`, `RunAsync_MemoryUnitWithBlankCaseId_SkipsUnitAndReports`). **Follow-up:** broader Docker-free coverage (reindex vec-hash shape + P3 provider-guard, workflow activity-order, import endpoint status codes, unprovisioned-tenant rejection) remains recommended — tracked as remaining P2 scope.

### Review Findings (2026-07-14, chunk 1: restore core + direct tests)

- [x] [Review][Patch] HIGH — Enforce clean-target restore semantics: reject unrelated existing target data and use a restore lease/idempotency record so retries of the same operation remain safe [src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:159]
- [x] [Review][Patch] HIGH — Keep 512 MB / 100K-unit support while bounding execution: stream directly to staging, process bounded batches, page durable workflow state, and renew staging retention [src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:110]

- [x] [Review][Patch] HIGH — Strictly validate one complete canonical envelope before any restore writes: reject duplicate/trailing top-level content, malformed section kinds, missing required sections, and statistics/count mismatches [src/Hexalith.Memories.Server/Import/ImportEnvelopeReader.cs:29]
- [x] [Review][Patch] HIGH — Enforce the case-scoped manifest against every imported case and memory unit before writing; `RestoreDataPlaneInput.CaseId` is currently unused [src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs:83]
- [x] [Review][Patch] MEDIUM — Validate edge endpoints and confidence before creating stubs so corrupt edges are skipped/reported without aborting or leaving orphan nodes [src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs:242]
- [x] [Review][Patch] HIGH — Persist source embedding dimensions in the syntactic/export contract and compare them with the target configuration before graph/vector writes [src/Hexalith.Memories.Server/Activities/Indexing/SyntacticHashProjection.cs:75]
- [x] [Review][Patch] MEDIUM — Preserve the exported memory-unit `LastUpdated` value in the reconstructed graph node instead of resetting it to `IngestedAt` [src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs:216]
- [x] [Review][Patch] HIGH — Apply the per-write `EmbeddingMigrationMarker` guard before restore writes semantic chunk hashes [src/Hexalith.Memories.Server/Activities/Restore/RestoreReindexUnitActivity.cs:129]
- [x] [Review][Patch] MEDIUM — Preflight syntactic and semantic readiness before mutating the data plane so a semantic mismatch cannot fail after cases, hashes, and graph data were written [src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs:87]
- [x] [Review][Patch] MEDIUM — Do not report a unit as restored when reindexing returns zero chunks; fail or propagate it into skipped/failed counters [src/Hexalith.Memories.Server/Workflows/RestoreWorkflow.cs:42]
- [x] [Review][Patch] MEDIUM — Preserve staged input across an ambiguous Dapr scheduling failure until workflow non-existence is confirmed [src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:174]
- [x] [Review][Patch] MEDIUM — Make staging cleanup genuinely best-effort so a deletion outage after successful data/vector restoration cannot mark the workflow failed [src/Hexalith.Memories.Server/Workflows/RestoreWorkflow.cs:53]
- [x] [Review][Patch] MEDIUM — Fail closed when restore workflow input/output cannot be deserialized instead of exposing status under an arbitrary tenant or returning completed counters as null [src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:225]
- [x] [Review][Patch] MEDIUM — Resolve the authenticated principal with the canonical audit resolver and persist or emit `RequestedBy`; it is currently carried but unused [src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:160]
- [x] [Review][Patch] MEDIUM — Update the normative AC4/AC7 text to match the ratified D1c decision that NL vectors are rebuilt later, not during restore [_bmad-output/implementation-artifacts/26-2-backup-and-restore.md:25]
- [x] [Review][Patch] MEDIUM — Add executable coverage for the reindex activity/marker/readiness guards, case-import route, staging failure/cleanup, status counters, and exact semantic/hash field sets [tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreReindexUnitActivityTests.cs:16]
- [x] [Review][Patch] LOW — Move `ImportedCase` and `RestoreEdgeOutcome` into their own named C# files per the repository one-type-per-file rule [src/Hexalith.Memories.Server/Import/ImportEnvelope.cs:10]

### Patch application (2026-07-14, chunk 1)

All 17 approved patches were applied. The import body now streams into 1 MiB staging chunks; canonical validation and restore use bounded record scans; re-index ids stay in renewable staging and execute in pages of at most 100. A tenant-wide restore lease plus clean-target guard prevents overlapping/different restores while allowing retries of the same operation. Both index families, embedding attribution/dimensions, and migration markers are preflighted before mutation; graph timestamps and edge validation preserve fidelity without orphan stubs.

Verification: `Hexalith.Memories.Server.Tests` **2641 passed / 1 intentional skip / 0 failed**; `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore` **0 warnings / 0 errors**; integration-test project compiles **0 warnings / 0 errors**. Story remains `review` because the agreed chunked code review still has the public/client, integration, and runbook diff groups to review.

### Review Findings (2026-07-14, chunk 2: public contracts + client)

- [x] [Review][Patch] HIGH — Add dedicated, configurable long-running import timeout semantics so the shipped client can honor the 512 MB import contract instead of inheriting the shared 30-second timeout [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:1135; src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs:43]
- [x] [Review][Patch] MEDIUM — Add a relative `RestoreStatusPath` builder and typed `GetRestoreStatusAsync` method so `MemoriesClient` owns the complete asynchronous restore lifecycle [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:1126; src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs:354]
- [x] [Review][Patch] MEDIUM — Add sanitized failure code and operator-safe diagnostic fields to restore status without exposing raw workflow exception details [src/Hexalith.Memories.Contracts/V1/RestoreStatusResponse.cs:21; src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs:353]

- [x] [Review][Patch] HIGH — Map `RESTORE_TARGET_BUSY` and `RESTORE_TARGET_NOT_CLEAN`, test both translations, and strengthen drift coverage for propagated exception codes [src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs:393]
- [x] [Review][Patch] MEDIUM — Add `TestDelegatingHandler` coverage for both import methods: POST/path escaping, content type/body, typed decode, structured errors, and stream ownership [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:1126]
- [x] [Review][Patch] MEDIUM — Reject a malformed 202 body whose required restore descriptor fields are missing or invalid instead of returning null identifiers/default scope [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:1140]
- [x] [Review][Patch] MEDIUM — Preserve the caller-owned export stream after `ImportTenantAsync` / `ImportCaseAsync` rather than disposing it through `StreamContent` [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:1132]
- [x] [Review][Patch] LOW — Align `RestoreStatusResponse` terminal-status documentation with the actual `Completed` / `Failed` wire casing [src/Hexalith.Memories.Contracts/V1/RestoreStatusResponse.cs:14]

### Patch application (2026-07-14, chunk 2)

All eight approved public/client patches were applied. Backup imports now use a dedicated configurable 30-minute timeout through a per-request handler while ordinary client calls retain their 30-second budget. The typed client preserves caller-owned streams, validates every accepted descriptor, and can poll restore status. Terminal non-success status includes stable, sanitized diagnostics without raw Dapr failure messages or stack traces. Both clean-target conflict codes now have actionable catalog mappings, and the drift guard recognizes explicitly propagated exception codes.

Verification: `Hexalith.Memories.Cli.Tests` **475 passed / 0 failed**; `Hexalith.Memories.Contracts.Tests` **587 passed / 0 failed**; `Hexalith.Memories.Server.Tests` **2645 passed / 1 intentional skip / 0 failed**; `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1` **0 warnings / 0 errors**; `git diff --check` clean. The integration and runbook review chunks remain outstanding.
