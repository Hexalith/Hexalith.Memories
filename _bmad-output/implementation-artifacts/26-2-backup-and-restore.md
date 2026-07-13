---
baseline_commit: a077fd09f21968e494f20c72c62450c0b1d349f6
---

# Story 26.2: Backup & Restore

Status: ready-for-dev

<!-- Epic: 26 — Test, Deployment & Operational Readiness. Closes audit finding A25 (High, "Missing feature") — feature portion. Story 26.5 closes the docs portion (broader runbook set + final cross-linking). Reinforces NFR16 (zero memory-unit loss on Redis restart, AOF verified). New operator-facing capability → commit `feat(...)` (minor release). New import/restore REST route + client method = additive contract change; do NOT rename/remove existing export contracts. -->

## Story

As an operator,
I want a restore counterpart to the existing export, an integration test that proves export→import fidelity, and backup/restore + disaster-recovery runbooks,
so that tenant or case data loss is recoverable by a documented, verified procedure (NFR16).

## Context & the one decision that shapes this story

The **export** side already exists (Story 8.3): `GET /api/v1/tenants/{tenantId}/export` and `GET /api/v1/tenants/{tenantId}/cases/{caseId}/export` stream a portable JSON snapshot. There is **no import/restore path** — that is the A25 gap this story closes.

**Critical fidelity finding (read before designing):** the export JSON does **not** contain embedding vectors or natural-language (NL) descriptions. `ExportedMemoryUnit.Unit` (a `MemoryUnit`) carries only `EmbeddingProvider` / `EmbeddingModel` / `EmbeddingDimensions`. The raw vectors live only in the Redis `:vec:` / `:vecnl:` hashes, which export never reads. Therefore **restore cannot copy every Redis hash from the export** — it must **re-derive** the semantic and NL vector hashes by re-embedding, and reconstruct the FalkorDB graph, while copying the data-plane hashes (memory-unit, case, members) verbatim. This reframes "every Redis hash and FalkorDB edge" fidelity as: **data-plane hashes + edges round-trip byte-exactly from the export; derived vector/index hashes are rebuilt so they exist and are consistent** (see AC4 and the fidelity definition in Dev Notes). This is the epic charter — `epic-26-context.md:22`: "Backup and restore must preserve the portable case-or-tenant representation across every memory unit, metadata record, Redis hash, and FalkorDB edge. Restore fidelity must be proved end to end."

## Acceptance Criteria

1. **Import/restore endpoint exists and consumes the export format.** New tenant-scoped and case-scoped write endpoints are added — `POST /api/v1/tenants/{tenantId}/import` and `POST /api/v1/tenants/{tenantId}/cases/{caseId}/import` (route templates added to `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` next to the `CaseExport`/`TenantExport` block, with matching `*ImportPath(...)` builders). They accept the **exact JSON envelope the export produces** (`manifest` → `ExportManifest` with `schemaVersion`, then `case`/`tenant` + `cases`, `memoryUnits[]` → `ExportedMemoryUnit`, `edges[]` → `ExportedEdge`, `statistics`). The endpoint rejects a payload whose `manifest.schemaVersion != 1` with a structured `ErrorResponse` (`Results.BadRequest`), and rejects a `manifest.scope`/route-scope mismatch (tenant JSON posted to the case route or vice-versa). New endpoint class `src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs` (`internal static class` exposing `MapImportEndpoints(this IEndpointRouteBuilder)`), wired in `src/Hexalith.Memories.Server/Program.cs` immediately after `app.MapExportEndpoints();` (currently line 89).

2. **Data-plane state is restored byte-exactly from the export.** For every `ExportedMemoryUnit`, restore writes the syntactic Redis hash at `{tenantId}:mu:{memoryUnitId}` reproducing the field contract in `Activities/Indexing/IndexSyntacticActivity.cs:75-98` (`id`, `tenantId`, `content`, `sourceUri`, `sourceUriText`, `sourceType`, `sourceTypeText`, `metadataText`, `attributeTags`, `metadataJson`, `contentHash`, `caseId`, `embeddingProvider`, `embeddingModel`, `ingestedBy`, `ingestedAt`, `lastUpdated`); each restored hash re-reads through `CaseService.ParseMemoryUnitFromHash` (`CaseService.cs:958`) to an equal `MemoryUnit`. Case records are written to `{tenantId}:case:{caseId}` (fields per `Activities/Cases/ProjectCaseHashActivity.cs`) and membership to `{tenantId}:case:{caseId}:members` (field name = memberId), round-tripping through `CaseService.ParseCaseFromHash` / `ListMembersAsync`. Restored `MemoryUnit` and `Case` values equal the pre-export originals field-for-field (id, content, contentHash, metadata, timestamps, status, source).

3. **The FalkorDB graph is fully reconstructed.** Restore MERGEs a `MemoryUnit` node per unit and a `Case` node per case via `IGraphQueryBuilder.BuildMergeMemoryUnitNode` / `BuildMergeCaseNode`, and MERGEs every `ExportedEdge` via `BuildMergeEdge`, into the per-tenant graph selected by `SelectGraph(tenantId)`. **Edge identity is reconstructed from `(SourceId, TargetId, EdgeType)` — `ExportedEdge.Id` (a graph-instance `id(r)`) is NOT reused.** Edge properties `confidence`, `origin`, `createdAt`, and the confidence-promotion audit (`verifiedBy`, `previousConfidence`) are restored. `CONTAINS` (Case→MemoryUnit) edges are **not** in `edges[]` and are rebuilt from each unit's `caseId`. Case-scope exports may contain edges whose far endpoint is outside the case (id-only, dangling `targetId`): restore creates a stub node for the missing endpoint via the existing `BuildMergeStubNode` pattern (`isStub=true`) rather than failing. After restore, a graph query returns every restored edge with matching type/direction/properties.

4. **Semantic and NL vector hashes are re-derived (not copied) so search works after restore.** Because embeddings/NL descriptions are absent from the export, restore re-runs the indexing side of the pipeline for each restored unit — reusing `Activities/Indexing/IndexSemanticActivity` (writes `{tenantId}:vec:{id}`) and `IndexNaturalLanguageSemanticActivity` (writes `{tenantId}:vecnl:{id}`) — using the target tenant's configured embedding provider, so the `:vec:`/`:vecnl:` hashes exist with correct `tenantId`, `memoryUnitId`, `caseId`, `embeddingProvider`, `embeddingModel`, `embeddingDimensions`. Restore does NOT re-extract content (the extracted text is already in `content`); it does NOT re-run Kreuzberg. The story documents that NL descriptions are regenerated (or, if regeneration is out of scope, that NL search is rebuilt on next re-index) — see the decision note in Dev Notes.

5. **Restore targets a provisioned tenant and is idempotent + guarded.** Restore requires the target tenant to be provisioned (RediSearch/vector indexes + FalkorDB graph created by `TenantProvisioningWorkflow`); it verifies tenant readiness (or provisions/waits) before writing hashes, otherwise restored hashes are not indexed and search silently returns nothing. Re-running the same import produces the same end state (MERGE on ids + dedup keys via `DedupKeyBuilder`; reuse `IngestDedupReservation` semantics or an operation-level idempotency token). The endpoint enforces the same write guardrails as ingestion: authenticated user (global fallback policy, Story 20.1); tenant authorization via `TenantAuthorizationMiddleware` (the `{tenantId}` route segment authorizes automatically, Story 20.2); `TenantStatusGuard.ValidateTenantActiveAsync`; `.AddEndpointFilter<InboundRateLimitEndpointFilter>()`; and `.WithMetadata(new RequestSizeLimitAttribute(...))` sized for large import bodies (export of 100K units ≈ 500 MB — choose a deliberate, documented limit and/or stream the body with `Utf8JsonReader` rather than buffering). Because restore of a large tenant is long-running and re-embeds every unit, it is scheduled as a durable **Dapr Workflow** (mirroring ingestion) returning `202 Accepted` + a status `Location`, not a synchronous handler. Do NOT hand-roll a background queue.

6. **Client, route table, and serialization surfaces are updated additively.** New `public virtual` `Import*Async(...)` method(s) on `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (no interface — mockability is via `virtual` + the `HttpClient` boundary, Decision D9) that build the path from the new `MemoriesRoutes.*ImportPath(...)` and POST with `MemoriesJsonContext.Options`. Any new request/response DTO (e.g. an import-accepted response carrying the workflow instance id) lives in `src/Hexalith.Memories.Contracts/V1/` and is registered in `MemoriesJsonContext.cs` (`[JsonSerializable(...)]`) — an unregistered DTO fails source-gen serialization. Existing export contracts are unchanged (additive only).

7. **An integration test proves export→import fidelity — every Redis hash and every FalkorDB edge.** A new test in the Docker-dependent tier `tests/Hexalith.Memories.IntegrationTests/` (plain `[Fact]` + `[Collection("AspireIngestionPipeline")]`, NOT `[RunnableSkippedFact]`) uses `AspireIngestionPipelineFixture` to: ingest ≥3 memory units across ≥2 cases with ≥1 causal/reference edge into a provisioned tenant; snapshot the backing stores (all `{t}:mu:*`, `{t}:case:*` hashes via `RedisConnection.GetServer(...).Keys(...)` + `HashGetAllAsync`, and all FalkorDB edges via `FalkorDbConnection`); export the tenant; restore the export into a clean store for the same tenant id (or a second provisioned tenant); then assert **every** syntactic memory-unit hash field, every case/members hash, and every graph edge `(source, target, type, confidence, origin, verifiedBy, previousConfidence)` matches the snapshot, and that a `{t}:vec:{id}` and `{t}:vecnl:{id}` hash exists for every restored unit with matching `embeddingDimensions`. Under a deterministic fixture embedding provider, assert vector-byte equality; otherwise assert structural presence + dimensions and document why. Model it on `Export/ExportWorkflowIntegrationTests.cs` (export round-trip) and `Ingestion/PipelinePersistenceIntegrationTests.cs` (backing-store enumeration + restart), and use `TenantDeletionIntegrationTests.cs` as the "every hash + every edge" enumeration template.

8. **AOF restart durability is verified — zero memory-unit loss.** The DR runbook cites, and the story confirms still-green, the existing restart-durability evidence: `Ingestion/PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart` (`:346`), which ingests, calls `AspireIngestionPipelineFixture.RestartTopologyAsync()` (reuses the named Redis volume), and asserts the syntactic/semantic/dedup keys and `Indexed` status survive. AOF config is already repo-owned and enforced (`deploy/redis/redis.conf` `appendonly yes` / `appendfsync everysec` / `aof-use-rdb-preamble yes`; `AppHost/Program.cs` throws if absent; FalkorDB `FALKORDB_PERSISTENCE_ARGS=--appendonly yes ...`). If the existing test does not already assert **memory-unit count is retained across restart (zero loss)**, add that assertion; otherwise reference it. Do not re-implement AOF config — it shipped in Story 6.4 / 26.1.

9. **Backup/restore and disaster-recovery runbooks exist under `docs/operations/`.** Two new operator docs following the house runbook shape (H1 with story number, purpose paragraph, fenced config/command blocks, key-shape tables, prerequisites → procedure → verification → rollback/recovery → cross-links):
   - `docs/operations/backup-restore.md` — the backup procedure (logical: `memories export …` CLI / export endpoint producing the portable JSON; physical: Redis AOF/RDB + FalkorDB AOF + the `20Gi` Redis / `10Gi` FalkorDB PVC snapshots) **and** the restore procedure (provision tenant → `POST …/import` → verify), with prerequisites, verification steps, and rollback.
   - `docs/operations/disaster-recovery.md` — DR runbook giving the **executable recovery path** for Redis-pod loss, FalkorDB-pod loss, and full-cluster loss, referencing the Story 26.1 deployment (`deployment-configuration.md`), the AOF/restart durability evidence (`pipeline-persistence.md` + the test in AC8), and the export→import **fidelity evidence** (the test in AC7). It must **originate a FalkorDB backup/restore procedure** — no committed FalkorDB dump/AOF operator procedure exists today (documented gap). Both docs cross-link `deployment-configuration.md` and `failure-recovery.md`. (Final cross-linking polish + the broader runbook set — capacity, incident-response, index-rebuild, onboarding/offboarding, upgrade/migration, monitoring thresholds — are **Story 26.5**, not here.)

10. **Build green; no scope leakage.** `dotnet build Hexalith.Memories.slnx` succeeds with **0 warnings, 0 errors** (`TreatWarningsAsErrors=true`). New `.cs` files carry the ITANEO MIT copyright header, file-scoped namespaces, XML docs on public surfaces, and CRLF line endings. Scope is limited to backup/restore feature + its two runbooks: **no** broader operational runbooks (26.5), **no** integration-stub closure of the 28 `[RunnableSkippedFact]` bodies (26.3), **no** coverage gate / benchmark lane (26.4), **no** submodule pointer changes, and **no** broadening into a general application-facing export feature (FR71 Phase 2 remains deferred).

## Tasks / Subtasks

- [ ] **Task 1 — Route table + endpoint scaffolding** (AC: 1, 5, 6)
  - [ ] Add `CaseImport = "/api/v1/tenants/{tenantId}/cases/{caseId}/import"` and `TenantImport = "/api/v1/tenants/{tenantId}/import"` constants to `MemoriesRoutes.cs` (near `CaseExport`/`TenantExport`, ~lines 152-158) plus `CaseImportPath(...)`/`TenantImportPath(...)` builders using the existing `Fill`/`EscapeSegment` helpers (reject `.`/`..`/whitespace segments as the export builders do). Do NOT register `MemoriesRoutes` in `MemoriesJsonContext` (not a wire DTO).
  - [ ] Create `src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs` (`internal static class`, `MapImportEndpoints`), mirroring `ExportEndpoints.cs` route/validation shape (tenant/case id validation via `EndpointValidationHelpers.ValidateTenantId` before any work) and `IngestionEndpoints.cs` write guardrails (`TenantStatusGuard` → dedup/idempotency → schedule workflow → `202` + status `Location`; `ErrorResults.*` envelopes for 400/404/409/429/503/500). Attach `.WithMetadata(new RequestSizeLimitAttribute(...))` and `.AddEndpointFilter<InboundRateLimitEndpointFilter>()`. Rely on `TenantAuthorizationMiddleware` for the route-scoped `{tenantId}` (no explicit tenant filter needed; adding it is harmless/defensive).
  - [ ] Wire `app.MapImportEndpoints();` in `Program.cs` right after `app.MapExportEndpoints();` (line 89).
  - [ ] Add the import-accepted response DTO (workflow instance id) in `Contracts/V1/` and register it in `MemoriesJsonContext.cs`.
- [ ] **Task 2 — Restore workflow + backing-store writers** (AC: 2, 3, 4, 5)
  - [ ] Add a durable `RestoreWorkflow` (+ activities) under `Workflows/` scheduled by the endpoint, replay-safe (`context.CurrentUtcDateTime`, `CreateReplaySafeLogger`, deterministic ids, side effects only in activities). Stream-deserialize the envelope with `Utf8JsonReader` + `MemoriesJsonContext.Options` (do not buffer the whole 500 MB body).
  - [ ] Ensure/verify target-tenant provisioning first (indexes + graph). Reuse `ITenantIndexReadinessVerifier.EnsureReadyAsync`; provision via `TenantProvisioningWorkflow` if the tenant is absent (or require an Active tenant and 404/409 otherwise — see decision note).
  - [ ] Per memory unit: reconstruct `MemoryUnit` from `ExportedMemoryUnit.Unit`; write the syntactic hash reproducing `IndexSyntacticActivity`'s field set (prefer factoring a shared hash-writer used by both index + restore over duplicating field logic); MERGE the FalkorDB `MemoryUnit` node (`BuildMergeMemoryUnitNode`); re-embed and write `:vec:` via `IndexSemanticActivity` and `:vecnl:` via `IndexNaturalLanguageSemanticActivity`.
  - [ ] Cases/members: write `{t}:case:{id}` and `{t}:case:{id}:members` hashes (mirror `ProjectCaseHashActivity` + members-write path); MERGE `Case` nodes; rebuild `CONTAINS` edges from `caseId`.
  - [ ] Edges: MERGE each `ExportedEdge` via `BuildMergeEdge` keyed on `(SourceId, TargetId, EdgeType)`, setting confidence/origin/createdAt and `verifiedBy`/`previousConfidence`; create stub nodes for dangling case-scope targets (`BuildMergeStubNode`).
  - [ ] Tenant-scope only: restore `ExportedTenantConfig` (status + `TenantConfigurationView`); document that secret **values** are not in the export (only secret-store key names) and must pre-exist in the target secret store.
  - [ ] Make the whole restore idempotent (re-run → same state) via MERGE + `DedupKeyBuilder`/`IngestDedupReservation` reuse or an operation idempotency token.
- [ ] **Task 3 — Client surface** (AC: 6)
  - [ ] Add `public virtual async Task<...> ImportTenantAsync(...)` / `ImportCaseAsync(...)` to `MemoriesClient.cs` building paths from the new route builders and POSTing (stream large bodies with `HttpCompletionOption.ResponseHeadersRead` if applicable), decoding the accepted-response DTO; auth flows via the registered `MemoriesAuthHandler`. Keep methods `virtual`, class non-sealed (mockability guard).
  - [ ] (If in scope) add a `memories import …` CLI command mirroring `export case`/`export tenant`; otherwise explicitly note CLI import is deferred. See decision note.
- [ ] **Task 4 — Fidelity + durability tests** (AC: 7, 8)
  - [ ] Add `Export/BackupRestoreFidelityIntegrationTests.cs` (or `Restore/…`) in `Hexalith.Memories.IntegrationTests` — `[Fact]` + `[Collection("AspireIngestionPipeline")]` — implementing the snapshot → export → restore → compare-every-hash-and-edge flow from AC7. Enumerate hashes with `RedisConnection.GetServer(...).Keys(pattern: "{t}:mu:*" | "{t}:case:*")` + `HashGetAllAsync`; enumerate edges with `FalkorDbConnection` + a `MATCH (m:MemoryUnit)-[r]-(n) RETURN ...` query. Determine the fixture's embedding provider; assert vector-byte equality if deterministic, else presence + dimensions (document choice).
  - [ ] Confirm `PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart` is green; if it lacks a memory-unit-count/zero-loss assertion, add one.
  - [ ] Add Docker-free unit/contract coverage in `Hexalith.Memories.Server.Tests` for: schema-version rejection, scope-mismatch rejection, edge-identity reconstruction from `(source,target,type)`, dangling-target stub creation, and idempotent re-run (mock `IConnectionMultiplexer`/graph builder). Mirror `Export/TenantExportServiceTests.cs` patterns.
- [ ] **Task 5 — Runbooks** (AC: 9)
  - [ ] Write `docs/operations/backup-restore.md` and `docs/operations/disaster-recovery.md` per AC9, mirroring the style of `failure-recovery.md` / `pipeline-persistence.md` (H1 + Story tag, prerequisites → procedure → verification → rollback, config/command fences, cross-links). Originate the FalkorDB backup/restore procedure. Cross-link `deployment-configuration.md` + `failure-recovery.md`.
- [ ] **Task 6 — Verify** (AC: 10)
  - [ ] `dotnet build Hexalith.Memories.slnx` → 0 warnings / 0 errors. Run Docker-free suites via the sandbox runner (see Testing). Note the Aspire fidelity test can only reach `review` locally (no container runtime); it must run in CI / an operator environment to reach `done`.
  - [ ] `git diff --check`; verify new `.cs` files are CRLF with the ITANEO header; confirm no submodule pointers moved and no export contract renamed.

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
| NL vector | `{t}:vecnl:{id}` | **Re-derived** (regen description + embed) | Hash exists; dimensions match (see NL decision) |
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

### Debug Log References

### Completion Notes List

### File List
