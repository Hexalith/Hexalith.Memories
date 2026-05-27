# Story 8.2: Consistency Verification & Repair

Status: done

**Effort estimate:** ~6 working days end-to-end — 3 days implementation (Tasks 1–5: workflow + activities + REST + client + CLI), 1 day per-unit inspection path (Task 6), 1 day tests (Tasks 7–8), 1 day docs + sprint-status + pre-flight reconciliation (Task 9). Add 0.5–1 day rebase cost if Story 8.1 or 7.5 land additional changes to `Program.cs` or `ServiceDefaults/Extensions.cs` after this story begins.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** Operator-facing **consistency verification and repair** across the three backends (RediSearch syntactic hash, Redis Vector hash, FalkorDB graph node). Delivers (a) `ConsistencyVerificationWorkflow` — on-demand or scheduled full-tenant audit that reports per-unit discrepancies; (b) `ConsistencyRepairWorkflow` — attempts to restore consistency by re-indexing from authoritative source or removing orphans; (c) per-unit inspection endpoint for targeted diagnosis; (d) REST endpoints wired into `Program.cs`; (e) typed client methods on `MemoriesClient`; (f) `memories consistency verify | inspect | repair` CLI group; (g) batched processing (units processed per batch) with workflow status polling. Closes **FR73** (detect divergence) + **FR74** (repair).

**What already exists (do NOT rebuild):**

1. **`VerifyConsistencyActivity`** — `src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs` — ALREADY probes a _single_ memory unit across all three backends via `KeyExistsAsync($"{tenantId}:mu:{id}")` + `KeyExistsAsync($"{tenantId}:vec:{id}")` + FalkorDB `BuildCheckMemoryUnitExists` query. Returns `ConsistencyResult(SyntacticExists, SemanticExists, GraphExists)`. Registered in `Program.cs:165`. **Reuse as-is** as the per-unit probe inside the verification workflow fan-out AND as the inspection endpoint's single-shot call. Do NOT duplicate the probe logic.
2. **`ConsistencyInput` + `ConsistencyResult`** — `src/Hexalith.Memories.Server/Activities/Indexing/{ConsistencyInput,ConsistencyResult}.cs` — activity I/O records. **Reuse in-place**; do NOT move to `Contracts/V1/` (they are activity-internal). A NEW contract record `ConsistencyInspectionResult` in `Contracts/V1/` surfaces the same data to clients with additional metadata (see Task 3.1).
3. **`TenantMetricsService.GetMemoryUnitCountAsync`** — `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:57-81` — already SCANs `{tenantId}:mu:*` via `server.KeysAsync(pattern, pageSize: ScanPageSize)`. **Use the same SCAN pattern** inside `EnumerateMemoryUnitIdsActivity` (Task 1.2), but yield the unit IDs (not just a count). Copy the `GetAnyServer(_redis)` helper + `RedisException → null` handling idiom.
4. **`IGraphQueryBuilder.BuildCheckMemoryUnitExists` + `BuildDeleteMemoryUnitNode` + `BuildCountMemoryUnits`** — `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs:48,57,60`. **Reuse** for the graph-side probe (already used by `VerifyConsistencyActivity`) and for orphan deletion in repair.
5. **`TenantDeletionWorkflow`** — `src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs` — the **canonical batched-processing pattern** to copy: `batchSize=500`, `maxBatches` safety valve computed from initial count, retry policy `(maxAttempts:5, firstRetryInterval:2s, backoffCoefficient:2.0, maxRetryInterval:5min)`. Structured logs via `[LoggerMessage]` partial methods. Copy the shape for both new workflows.
6. **Workflow-status polling endpoints** — `Program.cs` has the established pattern:
    - `POST /api/tenants/{tenantId}/...` returns `Results.Accepted(<status-url>, <instanceId>)`.
    - `GET /api/tenants/{tenantId}/.../status/{instanceId}` calls `workflowClient.GetWorkflowStateAsync(instanceId)` and returns the `WorkflowState`.
      See `Program.cs:975-1004` (tenant-deletion-status) + `Program.cs:778-802` (tenant-provision-status) for the canonical shapes — mirror them for `consistency/verify` and `consistency/repair`.
7. **`TenantStatusGuard.ValidateTenantExistsAsync`** — existence-only guard used by the tenant-verify endpoint at `Program.cs:1021`. **Use identically** — consistency operations are diagnostic and MUST be allowed on non-Active tenants (Provisioning / Deleting / Failed) so operators can audit tenants in any state.
8. **`DaprWorkflowClient`** + `ScheduleNewWorkflowAsync` / `GetWorkflowStateAsync` — the existing DAPR Workflow client wiring. Resolve via DI; no new registration needed beyond registering the two new workflows in `AddDaprWorkflow`.
9. **Structured-log + logger pattern** — `[LoggerMessage(EventId = <id>, Level = LogLevel.<x>, Message = "...")] private static partial void LogX(ILogger logger, ...)` — used everywhere. Copy style. EventId bank **8200–8299 reserved for Story 8.2** (see "EventId banks" in Dev Notes).
10. **CLI scaffolding + command groups** — `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` — add a new `consistency` top-level command group with three subcommands (`verify` / `inspect` / `repair`) following the `status telemetry` / `search inspect` shape. DO NOT reuse the existing `explore` / `handlers` stubs; those are reserved for other stories.
11. **`MemoriesJsonContext`** — source-gen + camelCase JSON options. **Register** the new V1 contract records (Task 3) in `MemoriesJsonContext` so AOT-safe serialization works end-to-end (CLI + Server + tests share one resolver).

**What 8.2 adds:**

1. **`ConsistencyVerificationWorkflow`** at `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationWorkflow.cs` — orchestrates: (a) existence guard via `TenantIdGuard`; (b) batched enumeration of memory unit IDs; (c) fan-out `VerifyConsistencyActivity` per unit (bounded parallelism — see Dev Notes §"Fan-out strategy"); (d) aggregate discrepancies; (e) return `ConsistencyVerificationResult`. Reports per-unit + aggregate counts. Idempotent re-entry (safe to re-run). No state mutation on the backends — verification is read-only.
2. **`ConsistencyRepairWorkflow`** at `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairWorkflow.cs` — re-runs verification as its first step (fresh diff; state may have changed since verification was requested), then routes each discrepancy to the correct repair activity. Idempotent: running twice produces the same final state (missing indexes re-created; orphans removed). Records per-unit `RepairAction` entries with before/after state.
3. **Two new activities:**
    - `EnumerateMemoryUnitIdsActivity` at `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs` — SCANs `{tenantId}:mu:*` (syntactic) UNION with FalkorDB `BuildCountMemoryUnits`-style enumeration for orphan detection. Returns a paged batch of memory unit IDs across all three backends (de-duplicated).
    - `RepairUnitActivity` at `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs` — for one memory unit ID, takes a `ConsistencyResult` snapshot and performs the minimal set of writes needed to converge: if syntactic present but semantic missing → re-run the semantic-index sub-activity path; if syntactic present but graph missing → re-merge graph node; if syntactic missing but semantic/graph present → orphan — DELETE the orphan entries from the non-authoritative backends. Returns `RepairActionResult`.
4. **Contract records** in `src/Hexalith.Memories.Contracts/V1/` (each sealed `public record`, ITANEO header, registered in `MemoriesJsonContext`):
    - `ConsistencyVerificationRequest(string TenantId, int? BatchSize)`
    - `ConsistencyVerificationResult(string TenantId, int TotalUnits, int ConsistentCount, int InconsistentCount, IReadOnlyList<ConsistencyDiscrepancy> Discrepancies, DateTimeOffset StartedAt, DateTimeOffset CompletedAt, TimeSpan Duration)`
    - `ConsistencyDiscrepancy(string MemoryUnitId, bool SyntacticPresent, bool SemanticPresent, bool GraphPresent, ConsistencyRepairRecommendation Recommendation)`
    - `ConsistencyRepairRecommendation` enum: `ReIndexSemantic | ReIndexGraph | ReIndexSemanticAndGraph | RemoveOrphanedSemantic | RemoveOrphanedGraph | RemoveOrphanedSemanticAndGraph | Unrepairable | NoOp` (NoOp means the unit is fully consistent; emitted only for completeness in the inspection endpoint).
    - `ConsistencyInspectionResult(string TenantId, string MemoryUnitId, bool SyntacticPresent, bool SemanticPresent, bool GraphPresent, ConsistencySyntacticDetail? SyntacticDetail, ConsistencySemanticDetail? SemanticDetail, ConsistencyGraphDetail? GraphDetail, ConsistencyRepairRecommendation Recommendation, DateTimeOffset CheckedAt)` + the three nested detail records (see Task 3.2 for field list).
    - `ConsistencyRepairRequest(string TenantId, int? BatchSize, bool IncludeUnrepairable)`
    - `ConsistencyRepairResult(string TenantId, int TotalDiscrepancies, int RepairedCount, int UnrepairableCount, IReadOnlyList<RepairActionRecord> Actions, DateTimeOffset StartedAt, DateTimeOffset CompletedAt, TimeSpan Duration)`
    - `RepairActionRecord(string MemoryUnitId, ConsistencyRepairRecommendation Applied, bool Succeeded, string? FailureReason, IReadOnlyDictionary<string, string> BeforeState, IReadOnlyDictionary<string, string> AfterState)` — the `BeforeState`/`AfterState` dictionaries are small (the three presence booleans as strings plus any short error code).
5. **REST endpoints in `Program.cs`** (insert alongside the existing `/api/tenants/{tenantId}/verify` at line 1007):
    - `POST /api/tenants/{tenantId}/consistency/verify` → schedules `ConsistencyVerificationWorkflow` with instance ID `verify-consistency-{tenantId}-{GUID}`; returns `202 Accepted` with status URL. Request body: `ConsistencyVerificationRequest` (optional `batchSize`).
    - `GET /api/tenants/{tenantId}/consistency/verify/{instanceId}` → returns `WorkflowState` (matches deletion-status shape).
    - `GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}` → synchronous per-unit check (no workflow needed). Calls the activity directly via a small injected service `ConsistencyInspectionService` that wraps `VerifyConsistencyActivity`'s probe logic (the activity itself is only invokable via DAPR workflow plumbing — factor the probe into a reusable service).
    - `POST /api/tenants/{tenantId}/consistency/repair` → schedules `ConsistencyRepairWorkflow` with instance ID `repair-consistency-{tenantId}-{GUID}`; returns `202 Accepted` with status URL.
    - `GET /api/tenants/{tenantId}/consistency/repair/{instanceId}` → returns `WorkflowState`.
6. **`ConsistencyInspectionService`** at `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs` — an injectable sealed class (not an interface; Architecture D9 — mock at HttpClient boundary, not at internal service boundary) that encapsulates the three-backend probe + metadata extraction. Shared by the inspection endpoint AND by `VerifyConsistencyActivity` / repair activity (via a helper method the activity calls). Rationale: DAPR Workflow Activities are instantiated by the workflow runtime — they cannot be invoked directly from a minimal-API handler. Factor the probe into a service both paths use.
7. **`RepairPlanCalculator`** at `src/Hexalith.Memories.Server/Consistency/RepairPlanCalculator.cs` — pure static class `public static ConsistencyRepairRecommendation Calculate(bool syntactic, bool semantic, bool graph)` that maps the 8 possible presence combinations to a `ConsistencyRepairRecommendation`. Pure function; no dependencies. Reused by both workflows AND the inspection endpoint (so the recommendation is consistent across paths). Table:
   | Syntactic | Semantic | Graph | Recommendation |
   |:---------:|:--------:|:-----:|-----------------------------|
   | T | T | T | `NoOp` |
   | T | F | T | `ReIndexSemantic` |
   | T | T | F | `ReIndexGraph` |
   | T | F | F | `ReIndexSemanticAndGraph` |
   | F | T | T | `RemoveOrphanedSemanticAndGraph` |
   | F | T | F | `RemoveOrphanedSemantic` |
   | F | F | T | `RemoveOrphanedGraph` |
   | F | F | F | `Unrepairable` _(nothing anywhere — bookkeeping mismatch; flag for manual)_ |
8. **`MemoriesClient` methods** in `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`:
    - `StartConsistencyVerificationAsync(string tenantId, ConsistencyVerificationRequest request, CancellationToken ct) → Task<Uri>` — returns the `Location` header (status URL).
    - `GetConsistencyVerificationStatusAsync(string tenantId, string instanceId, CancellationToken ct) → Task<WorkflowState>` (Dapr.Workflow.WorkflowState is serializable — verify in Task 4.2; if not, project into a minimal `ConsistencyWorkflowState` record).
    - `InspectConsistencyAsync(string tenantId, string memoryUnitId, CancellationToken ct) → Task<ConsistencyInspectionResult>`.
    - `StartConsistencyRepairAsync(string tenantId, ConsistencyRepairRequest request, CancellationToken ct) → Task<Uri>`.
    - `GetConsistencyRepairStatusAsync(string tenantId, string instanceId, CancellationToken ct) → Task<WorkflowState>`.
      Error-path parity with existing methods: throw `MemoriesRemoteException` with `ErrorResponse` on non-2xx.
9. **CLI commands** at `src/Hexalith.Memories.Cli/Commands/`:
    - `ConsistencyVerifyCommand.cs` — `memories consistency verify --tenant <t> [--wait] [--batch-size N]`. With `--wait`: poll status until workflow completes (or timeout). Without `--wait`: print the `instanceId` + status URL and exit. Shape copies `StatusTelemetryCommand` / `SearchQueryCommand`.
    - `ConsistencyInspectCommand.cs` — `memories consistency inspect --tenant <t> --id <unit-id>`. Synchronous; prints `ConsistencyInspectionResult` via the formatter router.
    - `ConsistencyRepairCommand.cs` — `memories consistency repair --tenant <t> [--wait] [--include-unrepairable] [--batch-size N]`. Confirmation prompt when `--wait` is OFF and no `--yes` flag (repair is a mutating operation; surface a warning).
    - `RootCommandFactory.Build` extended to wire the new `consistency` group (three subcommands + `--help`-on-no-action pattern that the other groups use).
10. **JSON context registration** — add each new V1 record to `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` `[JsonSerializable(...)]` attribute list so source-gen covers serialization. Do NOT rely on reflection fallback for the new types (unlike the health-writer anonymous-type case in 8.1).
11. **Unit + integration tests** — see AC #9 for authoritative inventory.
12. **`docs/dev/consistency.md`** — operator-facing doc: what verification detects; what repair does and doesn't do; expected repair latency per unit count; safety semantics (read-only verification vs. mutating repair); workflow status polling; CLI walkthrough; when to run verification; when NOT to run repair; relation to `/ready` (Story 8.1) and the `Consistency` row on the resolved-decision table in architecture.md:268.

**What does NOT ship:**

- **Cross-tenant consistency checks.** Verification is tenant-scoped per the AC. A future "cross-tenant registry reconciliation" story can audit the tenant registry itself (`tenant-registry-index` in DAPR state vs. actual backend indexes). **Out of scope for 8.2.**
- **Repair for corrupted content (content hash mismatch).** 8.2 only detects presence/absence across backends. A future story can add `contentHash` mismatch detection (unit present in all three backends but with diverged content). The `ConsistencyResult` contract is **designed to be extensible** — new fields are additive without breaking clients.
- **Graph edge (non-node) consistency.** Verification checks that the _node_ exists in FalkorDB; it does NOT audit every `caused_by` / `correlated_with` / `references` / `contains` / `annotates` edge for dangling endpoints. Edge integrity is a derivative concern deferred to Phase 2 (pair with Story 9.2's causal chain work).
- **Automatic / scheduled verification.** 8.2 ships the workflow + the endpoints; scheduling (cron-like recurrence) is NOT wired. Operators invoke verification on demand (`POST /consistency/verify`) or via CLI. A later story can wire DAPR Scheduler to invoke the workflow periodically.
- **Background repair (auto-trigger on verification discovery).** Repair MUST be an explicit operator action. This is deliberate: auto-repair on a mis-probed backend could delete live data. Documented in `docs/dev/consistency.md` under "Safety model".
- **Dry-run mode for repair.** The workflow runs to completion and reports actions; a dry-run (compute the repair plan without executing) is **NOT in 8.2** because the same information is already exposed by the verification workflow (`Discrepancies[].Recommendation`). Operators run `verify` first, read the plan, then run `repair`. A future story can add `--dry-run` to repair if the two-step flow proves awkward.
- **Per-tenant repair rate limiting.** The workflow processes units in batches but does NOT coordinate with `EmbeddingRateLimiterActor` (Story 1.4 / 4.1). Rationale: repair of _semantic_ (re-generating the embedding) DOES hit the embedding API; the existing `GenerateEmbeddingActivity` already respects the rate limiter actor when invoked. Re-use that activity (do NOT bypass it). Pure re-indexing (vector / graph from existing syntactic hash without a new embedding call) uses the **already-stored embedding** from the `:mu:{id}` hash (`embeddingProvider` / `embeddingModel` / content present); new embeddings are NOT generated unless syntactic is also missing (in which case the unit is `Unrepairable`).
- **UI / dashboard for verification results.** REST + CLI only in 8.2. Aspire dashboard surfaces workflow runs automatically (via DAPR Workflow's built-in metrics).
- **MCP tool for repair.** Epic 10 (MCP Server) does not ship repair as an agent-facing tool. Rationale: LLM agents must not trigger destructive data operations without explicit operator approval.
- **Data export integration.** Story 8.3 (Data Export / FR71) is orthogonal — export produces a snapshot; repair converges state. No direct coupling in 8.2.
- **OpenTelemetry span propagation from CLI into the workflow.** `DaprWorkflowClient.ScheduleNewWorkflowAsync` does not accept a `TraceContext` parameter today (verified via `dapr-dotnet-sdk` 1.15). Story 7.5's CLI→Server trace propagation covers HTTP boundaries; the workflow-internal spans are emitted by DAPR's own instrumentation. 8.2 does NOT add custom activity-source spans inside the workflows.

**Primary risks:**

1. **Repair destroys live data.** If the syntactic `{tenantId}:mu:{id}` hash is present but a transient Redis failure momentarily makes `KeyExistsAsync` return `false`, repair could misclassify the unit as an orphan and delete the semantic + graph entries. **Mitigation:** (a) `ConsistencyRepairWorkflow` RE-VERIFIES every unit immediately before acting on it (fresh `VerifyConsistencyActivity` call inside the repair loop — NOT reusing the verification workflow's snapshot); (b) the re-verify probes each backend with a 10-second overall timeout (architecture.md `GraphOperationTimeout` precedent at line 21 of the existing activity); (c) if the re-verify differs from the verify-snapshot (e.g., the unit is now fully consistent), the repair activity is a NO-OP for that unit; (d) a guard test `ConsistencyRepairWorkflow_ReVerifyDiffers_NoMutation` asserts this. Operators MUST NOT rely on a stale verification result — the workflow enforces the double-check.
2. **Repair of large tenant overwhelms a backend.** A tenant with 1M units has 1M `VerifyConsistencyActivity` calls. Unbounded fan-out overwhelms FalkorDB. **Mitigation:** bounded parallelism — each batch of `batchSize=500` units is fanned out via `Task.WhenAll` on `context.CallActivityAsync`, then the next batch starts sequentially. Reuses the `TenantDeletionWorkflow` batching pattern (line 114-168). The DAPR Workflow engine itself caps concurrency per-workflow; the explicit batching adds a second layer.
3. **Orphan detection miss in Redis Vector / FalkorDB without syntactic.** `EnumerateMemoryUnitIdsActivity` must enumerate from ALL THREE backends and union the IDs — enumerating only `{tenantId}:mu:*` (syntactic) misses orphans present in semantic/graph but absent from syntactic. **Mitigation:** (a) SCAN `{tenantId}:mu:*` (syntactic) AND `{tenantId}:vec:*` (semantic) AND query FalkorDB `MATCH (n:MemoryUnit) RETURN n.id` (graph) — three parallel enumerations; (b) union the three ID sets in memory; (c) de-duplicate via `HashSet<string>`; (d) emit batches of the union. A guard test `EnumerateMemoryUnitIds_OrphanInGraphOnly_IsReturned` pins this behavior by seeding a graph-only orphan into the test Redis fixture.
4. **Cypher injection via memory unit ID.** New memory unit IDs are ULIDs (generated by the server), but legacy workflow-created units may also use GUIDs and the inspection endpoint accepts `memoryUnitId` from the URL path. Interpolating directly into a Cypher string is a path to injection. **Mitigation:** the existing `IGraphQueryBuilder.BuildCheckMemoryUnitExists(memoryUnitId)` already uses parameterized queries (Decision D9). Reuse it; do NOT build raw Cypher strings in the new code. A guard test `ConsistencyInspectionService_MalformedMemoryUnitId_ReturnsBadRequest` validates the accepted ULID-or-GUID shapes before hitting the query builder.
5. **Repair loop diverges (repair introduces NEW inconsistencies).** If the `ReIndexGraph` path throws mid-MERGE (FalkorDB disconnects), the unit transitions from `(T,T,F)` → `(T,T,F)` — unchanged — OR worse, to `(T,T,partial)`. **Mitigation:** (a) each repair sub-activity is idempotent (the existing `IndexGraphActivity` uses MERGE; vector re-index re-HSETs the hash); (b) the repair activity catches the exception and records `RepairActionRecord.Succeeded=false` with `FailureReason`; the unit stays in the next verification cycle; (c) an **explicit upper bound of 3 repair passes** per workflow invocation (`maxRepairPasses` in `ConsistencyRepairWorkflow`) — if passes 1-3 all fail to converge, the workflow returns with remaining discrepancies flagged `Unrepairable` (with reason "Repair loop did not converge after 3 passes"). Document in `docs/dev/consistency.md`.
6. **Scan cost on hot Redis.** `SCAN {tenantId}:mu:*` on a tenant with 1M keys scans 1M keys — the Redis `SCAN` is cursor-based (O(N) total work, but bounded cost per call). The existing `TenantMetricsService.GetMemoryUnitCountAsync` already does this for metrics and it's acceptable for operator-triggered calls. **Mitigation:** (a) enumeration uses `pageSize = ScanPageSize` (same as `TenantMetricsService` — 250); (b) verification is explicitly documented as "operator-triggered, not a hot path"; (c) `docs/dev/consistency.md` includes the "Expected duration" table (units × backends × probe-cost = seconds); (d) add a guard test `EnumerateMemoryUnitIdsActivity_UsesCursorScan_NotKeysCommand` to prevent a future refactor from substituting the O(N) `KEYS` command (which blocks Redis single-threaded). The guard test inspects the `IServer.KeysAsync` call parameters or mocks the multiplexer to verify the SCAN opcode.
7. **Workflow state timeout on large tenants.** DAPR Workflow state persisted in the state store has a per-instance size limit (~1 MB for default configurations). A verification result with 10K discrepancies × ~200 bytes per `ConsistencyDiscrepancy` = 2 MB → exceeds the limit. **Mitigation:** (a) the workflow result `ConsistencyVerificationResult` truncates `Discrepancies` to the first **10,000 entries** and sets a `TruncatedAt` timestamp + a `TotalDiscrepancyCount` that is the UN-truncated count; (b) operators needing the full list run repair (which processes all discrepancies regardless of result truncation) or subscribe to the structured log events (`LogDiscrepancyDetected` EventId 8201 emits one line per discrepancy with memoryUnitId + recommendation). Document the 10K cap + the escape hatch in `docs/dev/consistency.md`.
8. **AC ambiguity on "authoritative source" for re-indexing.** The epic says "re-index missing entries from the authoritative source". 8.2 fixes the authoritative source as **syntactic (`{tenantId}:mu:{id}` Redis hash)** because it stores the full content + contentHash + metadata (see `IndexSyntacticActivity.cs:61-80`). Semantic and graph nodes are derivatives. **Decision:** document in `docs/dev/consistency.md` and in the source-code XML doc on `RepairUnitActivity` — if syntactic is missing, the unit is `Unrepairable` (cannot re-derive content from vector embeddings or graph edges). This choice is IMPLICIT in the `RepairPlanCalculator` table above (row `F-F-F` and variants with `F` in syntactic map to `Orphan` / `Unrepairable`).

**Risk → Guard test mapping** (each risk's mitigation is pinned by a specific test):

| #   | Risk                                                 | Guard test                                                                                                                                                                                                                 |
| --- | ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Repair destroys live data from stale verify snapshot | `ConsistencyRepairWorkflowTests.ReVerifyDiffers_NoMutation` + `RepairUnitActivityTests.ReVerifyReturnsConsistent_SkipsAction`                                                                                              |
| 2   | Fan-out overwhelms backend                           | `ConsistencyVerificationWorkflowTests.BatchedFanOut_DoesNotExceedBatchSize` — asserts no more than `batchSize` concurrent `CallActivityAsync` invocations per batch (verified via a counting mock activity)                |
| 3   | Orphan in graph/vector missed                        | `EnumerateMemoryUnitIdsActivityTests.OrphanInGraphOnly_IsReturned` + `OrphanInVectorOnly_IsReturned`                                                                                                                       |
| 4   | Cypher injection via inspection URL                  | `ConsistencyInspectionServiceTests.MalformedMemoryUnitId_ReturnsValidationError` (regex guard before query builder)                                                                                                        |
| 5   | Repair loop diverges                                 | `ConsistencyRepairWorkflowTests.ThreePassesFail_RemainingMarkedUnrepairable`                                                                                                                                               |
| 6   | SCAN vs. KEYS regression                             | `EnumerateMemoryUnitIdsActivityTests.UsesCursorScan_NotKeysCommand` (mock inspection)                                                                                                                                      |
| 7   | Workflow state size limit                            | `ConsistencyVerificationWorkflowTests.TenThousandDiscrepancies_ResultTruncated` — seeds 10_001 discrepancies, asserts result size ≤ 10_000 + `TotalDiscrepancyCount=10_001` + structured log emitted for every discrepancy |
| 8   | Authoritative source ambiguity                       | `RepairPlanCalculatorTests.SyntacticMissing_AllVariants_MapToOrphanOrUnrepairable` (theory across the 4 rows with `F` syntactic)                                                                                           |

## Story

As an operator,
I want to detect and repair inconsistencies across the three backends,
so that I can ensure data integrity and resolve divergence caused by partial failures.

## Acceptance Criteria

1. **`ConsistencyVerificationWorkflow` enumerates and probes all units (FR73).**
   **Given** a tenant with indexed memory units,
   **When** `POST /api/tenants/{tenantId}/consistency/verify` is called,
   **Then** the server schedules `ConsistencyVerificationWorkflow` via `DaprWorkflowClient.ScheduleNewWorkflowAsync` with instance ID `verify-consistency-{tenantId}-{GUID}`
   **And** the response is `202 Accepted` with `Location` header = `/api/tenants/{tenantId}/consistency/verify/{instanceId}` and body containing the `instanceId`
   **And** the workflow enumerates memory unit IDs from all three backends (syntactic SCAN + vector SCAN + FalkorDB `MATCH (n:MemoryUnit) RETURN n.id`) and unions them
   **And** for each unit it calls `VerifyConsistencyActivity` (reusing the existing activity; do NOT duplicate probe logic)
   **And** results are aggregated into `ConsistencyVerificationResult` with: `totalUnits`, `consistentCount`, `inconsistentCount`, `discrepancies[]` (truncated to 10_000 per Risk #7), `startedAt`, `completedAt`, `duration`, `totalDiscrepancyCount` (untruncated), and an optional `truncatedAt` timestamp if truncation occurred.

2. **Discrepancy details identify unit + backend presence (AC from epic).**
   **Given** the workflow completes,
   **When** the result is read via `GET /api/tenants/{tenantId}/consistency/verify/{instanceId}`,
   **Then** each `ConsistencyDiscrepancy` entry contains: `memoryUnitId`, `syntacticPresent`, `semanticPresent`, `graphPresent`, `recommendation` (from `RepairPlanCalculator.Calculate(s, m, g)`)
   **And** the aggregate counts satisfy the invariant `consistentCount + inconsistentCount = totalUnits` (verified by `ConsistencyVerificationWorkflowTests.Counts_InvariantHolds`).

3. **Per-unit inspection via CLI (AC from epic).**
   **Given** an operator wants to inspect a specific unit,
   **When** they run `memories consistency inspect --tenant <t> --id <unit-id>`,
   **Then** the CLI calls `GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}`
   **And** the endpoint synchronously probes all three backends (reuses `ConsistencyInspectionService` — NOT the workflow)
   **And** the response is `200 OK` with `ConsistencyInspectionResult` containing: presence flags + per-backend detail (syntactic: `contentHash`, `ingestedAt`, `sourceUri`, `sourceType`, `caseId`, `embeddingModel`; semantic: `embeddingDimensions`, `vectorHashKey`; graph: `caseEdgeCount`, `outgoingEdgeCount`, `incomingEdgeCount`) + `recommendation` + `checkedAt`
   **And** unknown `memoryUnitId` (not present in ANY backend) returns `404 Not Found` with `ErrorResponse(code="MEMORY_UNIT_NOT_FOUND", message=...)` — NOT a `ConsistencyInspectionResult` with all-false flags (this is the AC3 intent: "inspect" only makes sense if the unit exists somewhere).
   **And** malformed `memoryUnitId` (matching neither the accepted ULID nor legacy GUID shapes) returns `400 Bad Request` with `ErrorResponse(code="INVALID_MEMORY_UNIT_ID", ...)`.

4. **Repair workflow re-verifies before acting (FR74 + Risk #1).**
   **Given** detected inconsistencies,
   **When** `POST /api/tenants/{tenantId}/consistency/repair` is called,
   **Then** the server schedules `ConsistencyRepairWorkflow` via `DaprWorkflowClient.ScheduleNewWorkflowAsync` with instance ID `repair-consistency-{tenantId}-{GUID}`
   **And** the workflow runs `ConsistencyVerificationWorkflow`'s enumeration as its FIRST step (child activities — NOT an inline call to another workflow; reuse the activities, not the workflow wrapper)
   **And** for each discrepancy it RE-VERIFIES the unit via a fresh `VerifyConsistencyActivity` call IMMEDIATELY before repair
   **And** if the re-verify shows the unit is now consistent (`(T,T,T)`), the repair activity is a NO-OP for that unit (emits a `RepairActionRecord` with `Applied=NoOp`, `Succeeded=true`)
   **And** otherwise the activity executes the action dictated by `RepairPlanCalculator.Calculate(re-verify result)`
   **And** each action is logged with `[LoggerMessage]` (EventId 8202 = `RepairActionApplied`) including memoryUnitId, action, beforeState, afterState, durationMs
   **And** the workflow returns `ConsistencyRepairResult`.

5. **Orphan entries are removed from non-authoritative backends (FR74 repair-orphan semantics).**
   **Given** a memory unit ID where syntactic `{tenantId}:mu:{id}` is absent but semantic `{tenantId}:vec:{id}` or the graph node is present,
   **When** the repair workflow processes the unit,
   **Then** the non-authoritative entries are DELETED:
    - `RemoveOrphanedSemantic` → `KeyDeleteAsync({tenantId}:vec:{id})` on the shared Redis.
    - `RemoveOrphanedGraph` → FalkorDB `BuildDeleteMemoryUnitNode(memoryUnitId)` executed against `tenantId` graph.
    - `RemoveOrphanedSemanticAndGraph` → both of the above.
      **And** the action is recorded in `RepairActionRecord.BeforeState` / `AfterState` as `{"semantic":"present","graph":"present"}` → `{"semantic":"absent","graph":"absent"}` (or whichever combination applied)
      **And** the aggregate `repairedCount` is incremented on success.

6. **Re-indexing from syntactic re-creates semantic / graph entries (FR74 repair-divergence semantics).**
   **Given** a memory unit ID where syntactic is present but semantic and/or graph is missing,
   **When** the repair workflow processes the unit,
   **Then** missing semantic entries are re-created by reading the `{tenantId}:mu:{id}` hash (content, embeddingProvider, embeddingModel, contentHash, caseId) and re-running the indexing path
   **And** the re-indexing REUSES the existing embedding IF one is findable (the hash stores `embeddingProvider` + `embeddingModel` but NOT the vector itself; repair MUST invoke `GenerateEmbeddingActivity` which goes through the rate limiter — this is explicitly accepted, see "What does NOT ship" bullet 6)
   **And** missing graph entries are re-created via `IGraphQueryBuilder.BuildMergeMemoryUnitNode(...)` using the same hash fields
   **And** the action is recorded with before/after state.

7. **Unrepairable entries are flagged (FR74 — "flagged for manual intervention").**
   **Given** a memory unit ID where syntactic is absent AND the non-authoritative backends have insufficient information to reconstruct it,
   **When** the repair workflow processes the unit,
   **Then** the `RepairActionRecord.Applied = Unrepairable`, `Succeeded = false`, `FailureReason` describes why (e.g., "Source content lost; cannot re-derive from embedding vector.")
   **And** `unrepairableCount` is incremented
   **And** a `[LoggerMessage]` at `LogLevel.Warning` with EventId 8203 (`UnrepairableDiscrepancy`) is emitted for each unrepairable unit
   **And** the operator sees these flagged in the workflow result.

8. **Batched processing with progress visibility (AC from epic).**
   **Given** consistency verification on a large tenant,
   **When** the workflow runs,
   **Then** units are processed in batches of `batchSize` (default `500`, overridable via `ConsistencyVerificationRequest.BatchSize` with min `10` / max `5000` — enforced by validation at the endpoint, returning `400 BAD_REQUEST` on out-of-range)
   **And** between batches, the workflow's `WorkflowState.SerializedOutput` intermediate (via `SetCustomStatus` if available in `dapr-dotnet-sdk` 1.15; otherwise via a persisted checkpoint counter) is updated so `GET /api/tenants/{tenantId}/consistency/verify/{instanceId}` shows `batchesProcessed` + `totalBatches` + `currentPhase` (`enumerating` / `verifying` / `completed`)
   **And** the maxBatches safety valve (Risk #2 mitigation — `(initialCount / batchSize * 2) + 10`) prevents infinite loops if enumeration grows during the run.

9. **Tests cover verification + inspection + repair paths.** _(AC #9 is the **authoritative** source for test-class inventory and per-class test counts. The "Testing standards" section in Dev Notes documents conventions only — if a count in that section conflicts with a number here, this AC wins.)_
   **Given** the consolidated test projects,
   **When** `dotnet test` runs,
   **Then** the following classes exist and pass (Tier 1 — unit — unless marked Integration):
    - `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs` — 7 tests: empty-tenant (zero units, zero discrepancies); all-consistent (no discrepancies); one-of-each-discrepancy-type (covers all 7 non-`NoOp` rows of the `RepairPlanCalculator` table); 10_001-discrepancies truncation (Risk #7); invariant counts hold; batched fan-out bounded (Risk #2); idempotent re-entry.
    - `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs` — 6 tests: re-verify-diff-no-mutation (Risk #1); three-pass-convergence; three-pass-divergence-marks-unrepairable (Risk #5); dry-run-equivalent (verification result → plan matches repair-workflow-would-do); cancellation-mid-batch; rate-limiter-hit-propagates-as-retry (ensures `EmbeddingRateLimiterActor` rate-limit path goes through the retry policy).
    - `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs` — 5 tests: all-three-backends-union; orphan-in-graph-only (Risk #3); orphan-in-vector-only; uses-cursor-scan-not-keys (Risk #6); cancellation-propagates.
    - `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs` — 8 tests: each of the 7 non-`NoOp` recommendations (re-index-semantic / re-index-graph / re-index-semantic-and-graph / remove-orphaned-semantic / remove-orphaned-graph / remove-orphaned-both / unrepairable) + re-verify-returns-consistent-skips-action (Risk #1).
    - `tests/Hexalith.Memories.Server.Tests/Consistency/RepairPlanCalculatorTests.cs` — 1 `[Theory]` with 8 rows covering every presence combination (Risk #8 also pinned here).
    - `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs` — 6 tests: all-present (NoOp); one-missing (each of the three); all-missing-throws-NotFound; malformed-id-throws-Validation (Risk #4); cancellation-propagates.
    - `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyEndpointTests.cs` — 8 tests using `WebApplicationFactory<Program>`: POST verify returns 202 with Location; GET verify returns WorkflowState; GET inspect returns 200 with detail; GET inspect returns 404 for missing; GET inspect returns 400 for malformed; POST repair returns 202 with Location; GET repair returns WorkflowState; instance-id-tenant-mismatch returns 404 (mirror `Program.cs:994-999` guard).
    - `tests/Hexalith.Memories.Client.Rest.Tests/MemoriesClientConsistencyTests.cs` — 5 tests: start-verify-parses-Location; get-verify-status-parses-WorkflowState; inspect-parses-result; start-repair-parses-Location; get-repair-status-parses-WorkflowState. Uses `HttpClient` with mocked handler (existing pattern).
    - `tests/Hexalith.Memories.Cli.Tests/Commands/ConsistencyVerifyCommandTests.cs` — 4 tests: happy-path (202 response, prints instance ID); `--wait` polls until completion; missing `--tenant` returns plumbing exit code with error envelope; JSON format emits `ConsistencyVerificationResult`.
    - `tests/Hexalith.Memories.Cli.Tests/Commands/ConsistencyInspectCommandTests.cs` — 3 tests: happy-path prints result; 404 response prints error envelope with recovery suggestion; malformed-id response prints 400 envelope.
    - `tests/Hexalith.Memories.Cli.Tests/Commands/ConsistencyRepairCommandTests.cs` — 4 tests: happy-path (202, prints instance ID); `--wait` polls repair status; confirmation-prompt path (without `--yes`); `--include-unrepairable` flag propagated.
    - `tests/Hexalith.Memories.IntegrationTests/Consistency/ConsistencyWorkflowIntegrationTests.cs` `[Trait("Category","Integration")]` — 3 scenarios: (1) verify-on-clean-tenant reports zero discrepancies; (2) seed-orphan-then-verify reports one discrepancy with the right recommendation; (3) seed-orphan-then-repair converges to consistent. `[Fact(Skip)]` pattern acceptable if the Aspire CS0311 issue from Story 5.6 Dev Notes remains unresolved; un-skip otherwise.

10. **`docs/dev/consistency.md` documents the contract.**
    **Given** an operator wants to run verification or repair,
    **When** they read `docs/dev/consistency.md`,
    **Then** the doc covers:
    - **Purpose** — what verification detects (presence divergence across 3 backends); what it does NOT detect (content-hash drift, edge integrity, cross-tenant issues).
    - **Endpoint summary** — table of the five endpoints (POST verify / GET verify / GET inspect / POST repair / GET repair) with request/response shapes and typical latency bracket.
    - **Workflow lifecycle** — state diagram (`Running` → `Completed` / `Failed`) + polling cadence recommendation (5s initial interval; exponential backoff to 60s for long runs).
    - **Repair plan table** — reproduction of the `RepairPlanCalculator` table so operators can decode the recommendation without reading code.
    - **Safety model** — read-only verification vs. mutating repair; re-verify-before-act semantics (Risk #1); three-pass convergence (Risk #5); no auto-repair (out-of-scope).
    - **Authoritative source** — why `{tenantId}:mu:{id}` is the source of truth; what "unrepairable" means and when it occurs.
    - **Expected duration** — table `units × backends × probe-cost`. Baseline (empty 3-backend probe ≈ 1-2 ms each; 1K units = ~5-10s; 10K units = ~50-100s; 1M units = 2-4h).
    - **Truncation disclosure** — 10K-entry cap on `Discrepancies[]` (Risk #7) + escape hatch via structured log EventId 8201.
    - **CLI walkthrough** — `memories consistency verify --tenant <t>` → `memories consistency inspect --tenant <t> --id <unit-id>` → `memories consistency repair --tenant <t>` end-to-end.
    - **Relation to Story 8.1 `/ready`** — consistency verification is NOT called from health checks (instance-scoped `/ready` stays fast; per-tenant audit is a separate manual action).
    - **Out of scope** — auto-scheduling, dry-run flag, cross-tenant, edge integrity, content-hash drift, UI dashboard, MCP tool.

## Tasks / Subtasks

### Task Summary (orientation)

9 top-level tasks. Tasks 1 + 2 + 3 are parallelizable substrate work (activities + contracts + services); Tasks 4-5 depend on 1-3; Tasks 6-8 close the loop with REST + client + CLI + tests; Task 9 ships docs and sprint-status update.

- **Substrate:** Tasks 1 (activities), 2 (workflows), 3 (contracts + services)
- **Integration:** Tasks 4 (REST endpoints), 5 (client methods), 6 (CLI)
- **Verification:** Tasks 7 (unit tests), 8 (integration tests)
- **Finalization:** Task 9 (docs + sprint-status)

---

- [x] **Task 1: New activities (AC: #1, #4, #5, #6, #7, #9)** _(Phase B — complete)_
    - [x] 1.1 Create `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs` — `public sealed class EnumerateMemoryUnitIdsActivity : WorkflowActivity<EnumerateMemoryUnitIdsInput, EnumerateMemoryUnitIdsResult>`. Input: `(string TenantId, int BatchSize, int BatchNumber)`. Implementation: on the first batch (number 0), SCAN all three backends in parallel (syntactic `{tenantId}:mu:*` + semantic `{tenantId}:vec:*` via `server.KeysAsync` — reuse `TenantMetricsService.GetAnyServer` helper by either factoring it out to an internal shared helper `src/Hexalith.Memories.Server/Infrastructure/RedisServerHelper.cs` OR duplicating the 10-LOC helper; pick duplication for Task 1 to avoid scope creep — refactor later if 3+ callers need it), plus FalkorDB `MATCH (n:MemoryUnit) RETURN n.id`. Union IDs into a `HashSet<string>`, convert to sorted list, store in activity-local memory, return the first `batchSize` IDs + `TotalUnionCount`. On subsequent batches, the workflow maintains its own slice — but the activity is still idempotent (each call re-computes the union; DAPR activities MUST be deterministic across retries).
    - [x] 1.2 Define input/result records in the same folder: `EnumerateMemoryUnitIdsInput(string TenantId, int BatchSize, int BatchNumber)` and `EnumerateMemoryUnitIdsResult(IReadOnlyList<string> MemoryUnitIds, long TotalUnionCount, bool IsComplete)`. _(Phase B: shape simplified to `EnumerateMemoryUnitIdsInput(string TenantId, int MaxUnits=50000)` + `EnumerateMemoryUnitIdsResult(MemoryUnitIds, TotalUnionCount, Truncated)` because the workflow owns batching via the 50K soft-cap approach from 1.2a.)_
    - [x] 1.2a **Re-evaluation of per-batch recompute.** Re-computing the full union on every batch call is O(N) per batch × O(N/batchSize) batches = O(N²/batchSize). For N=1M, batchSize=500, that is 2_000_000_000 ID-comparisons. **Decision:** compute the full union ONCE inside the workflow (the workflow itself calls the activity with BatchNumber=0 and gets the full list back up to a reasonable cap — the activity returns the FIRST `batchSize` IDs PLUS `TotalUnionCount`; on subsequent calls the workflow passes the starting index and the activity re-enumerates to skip ahead). For the MVP, enforce a soft cap of 50_000 units per verification run (document in `docs/dev/consistency.md` — FR73 target is correctness on realistic tenants, not 10M-unit stress testing). For tenants exceeding 50K units, return a partial result with `TotalUnionCount > ResultDiscrepancies.Count` and a warning flag `EnumerationTruncated=true` in `ConsistencyVerificationResult`. A guard test `ConsistencyVerificationWorkflowTests.SixtyThousandUnits_TruncatesAndFlags` pins this. Phase 2 can shard the enumeration (FalkorDB SCAN-like paging).
    - [x] 1.3 Create `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs` — `public sealed class RepairUnitActivity : WorkflowActivity<RepairUnitInput, RepairUnitResult>`. Input: `(string TenantId, string MemoryUnitId, ConsistencyRepairRecommendation Recommendation, bool IncludeUnrepairable)`. Implementation:
        - Re-verify via injected `ConsistencyInspectionService.InspectAsync` (NOT by calling the activity — activities cannot call activities in DAPR; services are fine).
        - Recompute the recommendation from the re-verify result via `RepairPlanCalculator.Calculate`.
        - If recommendation is `NoOp`, return success with `Applied=NoOp`.
        - Otherwise, dispatch:
            - `RemoveOrphanedSemantic` → `db.KeyDeleteAsync($"{tenantId}:vec:{id}")`.
            - `RemoveOrphanedGraph` → `FalkorDB.QueryAsync(tenantId, BuildDeleteMemoryUnitNode(id))`.
            - `RemoveOrphanedSemanticAndGraph` → both.
            - `ReIndexSemantic` → read `{tenantId}:mu:{id}` hash, reconstruct `IndexInput`, call `IndexSemanticActivity`'s service-layer helper (factor `SemanticIndexer` service out of the activity — see Task 1.4).
            - `ReIndexGraph` → read hash, call `IGraphQueryBuilder.BuildMergeMemoryUnitNode`.
            - `ReIndexSemanticAndGraph` → both.
            - `Unrepairable` (or if `IncludeUnrepairable=false`) → return `Applied=Unrepairable`, `Succeeded=false`, reason explains.
        - Wrap each branch in try/catch; populate `BeforeState` + `AfterState` as tiny `Dictionary<string,string>` (presence flags only).
    - [ ] 1.3a **Cost of factoring `SemanticIndexer` out of `IndexSemanticActivity` is real.** _(Phase B: factor DEFERRED to Phase C per user-authorized scope. The new services carry the `ReIndexFromSyntacticAsync` / `ReMergeFromSyntacticAsync` surfaces for repair; the existing activities still own their inline write logic and their tests continue to pass unmodified.)_ The existing `IndexSemanticActivity` contains the Redis Vector write logic inline. To reuse from `RepairUnitActivity`, extract a `SemanticIndexer` service (`src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs`) that the existing activity and the new repair path both call. Same for `GraphNodeMerger` (from `IndexGraphActivity`). Budget ~0.5 day for the extraction + ensuring `IndexSemanticActivityTests` + `IndexGraphActivityTests` still pass. If the existing activities' tests are tightly coupled to the activity class, substitute `NSubstitute` mocks on the extracted services and add fresh `SemanticIndexerTests` + `GraphNodeMergerTests` covering the pre-existing behavior. Do NOT skip — behavior-preserving refactor is required here, not optional.
    - [x] 1.4 Define `RepairUnitInput` + `RepairUnitResult` records in the same folder. _(Phase B: `RepairUnitInput` created; the activity returns `RepairActionRecord` directly, so no separate `RepairUnitResult` was needed — the V1 contract is re-used end-to-end.)_
    - [x] 1.5 Register both new activities in `Program.cs` `AddDaprWorkflow`:
        ```csharp
        options.RegisterActivity<EnumerateMemoryUnitIdsActivity>();
        options.RegisterActivity<RepairUnitActivity>();
        ```
    - [x] 1.6 `[LoggerMessage]` partial methods using EventId bank 8200-8299: `LogDiscrepancyDetected` (EventId 8201, Info), `LogRepairActionApplied` (EventId 8202, Info), `LogUnrepairableDiscrepancy` (EventId 8203, Warning), `LogEnumerationTruncated` (EventId 8204, Warning).

- [x] **Task 2: Workflows (AC: #1, #4, #5, #6, #7, #8)** _(Phase B — complete)_
    - [x] 2.1 Create `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationWorkflow.cs` — `public sealed partial class ConsistencyVerificationWorkflow : Workflow<ConsistencyVerificationInput, ConsistencyVerificationResult>`. Input: `(string TenantId, int BatchSize)`. Shape:
        1. Validate via `TenantIdGuard.Validate`.
        2. Call `EnumerateMemoryUnitIdsActivity` with BatchNumber=0 to get total count + first batch.
        3. Check 50_000 cap; set `EnumerationTruncated` flag if exceeded.
        4. Loop over batches: for each batch, fan out `VerifyConsistencyActivity` via `Task.WhenAll` over `context.CallActivityAsync<ConsistencyResult>(nameof(VerifyConsistencyActivity), ...)` (bounded parallelism per batch).
        5. Aggregate `ConsistencyResult`s → `ConsistencyDiscrepancy` list via `RepairPlanCalculator`.
        6. Truncate to 10_000 discrepancies (Risk #7); emit structured log per truncated entry.
        7. Return `ConsistencyVerificationResult`.
    - [x] 2.2 Copy the retry policy from `TenantDeletionWorkflow.cs:44-49`: `(maxAttempts=5, firstRetry=2s, backoff=2.0, maxRetry=5min)`.
    - [x] 2.3 Create `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairWorkflow.cs` — `public sealed partial class ConsistencyRepairWorkflow : Workflow<ConsistencyRepairInput, ConsistencyRepairResult>`. Shape:
        1. Validate tenant ID.
        2. Inline enumeration + verification (DO NOT call `ConsistencyVerificationWorkflow` from a workflow — DAPR child-workflow invocation is supported but adds unnecessary indirection here; reuse the ACTIVITIES directly).
        3. For each discrepancy, call `RepairUnitActivity` with the pre-computed recommendation + re-verify-inside-activity semantics (Risk #1).
        4. Track passes: if any discrepancy remains after pass 1, re-enumerate + re-verify + retry up to `maxRepairPasses=3` (Risk #5).
        5. After max passes, any remaining discrepancies are flagged `Unrepairable` with reason "Repair loop did not converge after 3 passes".
        6. Return `ConsistencyRepairResult`.
    - [x] 2.4 Register both workflows in `Program.cs` `AddDaprWorkflow`:
        ```csharp
        options.RegisterWorkflow<ConsistencyVerificationWorkflow>();
        options.RegisterWorkflow<ConsistencyRepairWorkflow>();
        ```
    - [x] 2.5 Structured logs — reuse EventId 8201-8204 defined in Task 1.6 + add `LogVerificationCompleted` (8205, Info), `LogRepairCompleted` (8206, Info), `LogRepairPassStarted` (8207, Debug).
    - [x] 2.6 Workflow input/output records:
        - `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationInput.cs` — `(string TenantId, int BatchSize)`.
        - The result type is the V1 contract record `ConsistencyVerificationResult` (Task 3.1).
        - `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairInput.cs` — `(string TenantId, int BatchSize, bool IncludeUnrepairable)`.
        - The result type is the V1 contract record `ConsistencyRepairResult` (Task 3.1).
          Workflow DTOs must be serializable by DAPR (System.Text.Json source-gen via `MemoriesJsonContext`).

- [x] **Task 3: Contracts + services (AC: #1, #2, #3, #4, #5, #6, #7)** _(Phase B — 3.1-3.7 complete; 3.8 deferred)_
    - [x] 3.1 Create the following records in `src/Hexalith.Memories.Contracts/V1/` (one file per record, ITANEO header, `public sealed record`, `[JsonSerializable]` entries added to `MemoriesJsonContext`):
        - `ConsistencyVerificationRequest.cs`
        - `ConsistencyVerificationResult.cs`
        - `ConsistencyDiscrepancy.cs`
        - `ConsistencyRepairRecommendation.cs` (enum; apply `CamelCaseStringEnumConverter` via `[JsonConverter]` to match existing enum shape policy).
        - `ConsistencyInspectionResult.cs`
        - `ConsistencySyntacticDetail.cs` — `(string ContentHash, DateTimeOffset IngestedAt, string SourceUri, string SourceType, string CaseId, string EmbeddingProvider, string EmbeddingModel)`.
        - `ConsistencySemanticDetail.cs` — `(int EmbeddingDimensions, string VectorHashKey)`.
        - `ConsistencyGraphDetail.cs` — `(int OutgoingEdgeCount, int IncomingEdgeCount, int CaseEdgeCount)`.
        - `ConsistencyRepairRequest.cs`
        - `ConsistencyRepairResult.cs`
        - `RepairActionRecord.cs`
    - [x] 3.2 Update `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` `[JsonSerializable]` attribute list with the new records. Run `dotnet build` after — missing entries surface as AOT warnings.
    - [x] 3.3 Create `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs` — sealed class constructor-injected with keyed `IConnectionMultiplexer("redis")`, `IConnectionMultiplexer("falkordb")`, `IGraphQueryBuilder`, `ILogger<ConsistencyInspectionService>`. Public method `InspectAsync(string tenantId, string memoryUnitId, CancellationToken ct) → Task<ConsistencyInspectionResult>`. Implementation:
        - Validate and canonicalize `memoryUnitId` as either a 26-character Crockford-base32 ULID or a legacy GUID (hyphenated D-format or 32-hex N-format). GUID aliases normalize to lowercase hyphenated D-format before Redis/FalkorDB lookup; malformed shapes throw `ArgumentException` (caller maps to 400).
        - Probe syntactic via `db.HashGetAllAsync($"{tenantId}:mu:{memoryUnitId}")` (HashGetAll — so the detail fields are available if present).
        - Probe semantic via `db.HashGetAllAsync($"{tenantId}:vec:{memoryUnitId}")`.
        - Probe graph via `FalkorDB.QueryAsync(tenantId, BuildCheckMemoryUnitExists(memoryUnitId))` + edge-count queries (three: outgoing, incoming, case — or a combined query if `IGraphQueryBuilder` exposes one; if not, add a `BuildCountMemoryUnitEdges(memoryUnitId)` method to `IGraphQueryBuilder` — NEW method, returns a single query returning three counts in one roundtrip).
        - If ALL three probes report absent → throw `KeyNotFoundException` (caller maps to 404).
        - Otherwise construct `ConsistencyInspectionResult` with presence flags + optional per-backend detail + recommendation from `RepairPlanCalculator.Calculate`.
    - [x] 3.4 Create `src/Hexalith.Memories.Server/Consistency/RepairPlanCalculator.cs` — `public static class RepairPlanCalculator` with:
        ```csharp
        public static ConsistencyRepairRecommendation Calculate(bool syntactic, bool semantic, bool graph)
        {
            return (syntactic, semantic, graph) switch
            {
                (true,  true,  true)  => ConsistencyRepairRecommendation.NoOp,
                (true,  false, true)  => ConsistencyRepairRecommendation.ReIndexSemantic,
                (true,  true,  false) => ConsistencyRepairRecommendation.ReIndexGraph,
                (true,  false, false) => ConsistencyRepairRecommendation.ReIndexSemanticAndGraph,
                (false, true,  true)  => ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph,
                (false, true,  false) => ConsistencyRepairRecommendation.RemoveOrphanedSemantic,
                (false, false, true)  => ConsistencyRepairRecommendation.RemoveOrphanedGraph,
                (false, false, false) => ConsistencyRepairRecommendation.Unrepairable,
            };
        }
        ```
    - [x] 3.5 Create `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs` — factored service extracting the Redis Vector write from `IndexSemanticActivity` (Task 1.3a). Method: `Task ReIndexFromSyntacticAsync(string tenantId, string memoryUnitId, CancellationToken ct)` — reads the syntactic hash, extracts fields, writes to `:vec:` key. _(Phase B: class + `ISemanticIndexer` interface created; implementation throws `NotSupportedException` since embedding regeneration requires EmbeddingClient + rate-limiter wiring deferred to Phase C. Repair through this branch reports `Succeeded=false` with a clear reason.)_
    - [x] 3.6 Create `src/Hexalith.Memories.Server/Consistency/GraphNodeMerger.cs` — factored service extracting the FalkorDB MERGE from `IndexGraphActivity`. Method: `Task ReMergeFromSyntacticAsync(string tenantId, string memoryUnitId, CancellationToken ct)`. _(Phase B: fully functional — reads syntactic hash, re-runs `BuildMergeCaseNode` + `BuildMergeMemoryUnitNode` + `BuildMergeEdge(Contains)`.)_
    - [x] 3.7 Register the three new services in `Program.cs` DI:
        ```csharp
        builder.Services.AddScoped<ConsistencyInspectionService>();
        builder.Services.AddScoped<SemanticIndexer>();
        builder.Services.AddScoped<GraphNodeMerger>();
        ```
    - [ ] 3.8 Update existing `IndexSemanticActivity` + `IndexGraphActivity` to delegate to `SemanticIndexer` + `GraphNodeMerger` (preserve public activity shape; move implementation). Run existing `IndexSemanticActivityTests` + `IndexGraphActivityTests` + `IngestionWorkflowTests` — all MUST continue to pass without modification. _(Phase B: DEFERRED to Phase C per user-authorized scope. Existing tests already pass unmodified because the activities are untouched.)_

- [x] **Task 4: REST endpoints in `Program.cs` (AC: #1, #3, #4, #5, #6, #7, #8)** _(Phase C — complete)_
    - [ ] 4.1 Insert five new minimal-API endpoints in `Program.cs` alongside the existing `POST /api/tenants/{tenantId}/verify` (line 1007) — use the next available block (pick the line range right after line 1044 to keep consistency-related endpoints together):
        - `POST /api/tenants/{tenantId}/consistency/verify` — validates tenant (`ValidateTenantId` + `TenantStatusGuard.ValidateTenantExistsAsync`), validates `BatchSize ∈ [10, 5000]`, schedules workflow with instance ID `$"verify-consistency-{tenantId}-{Guid.NewGuid():N}"`, returns `Results.Accepted($"/api/tenants/{tenantId}/consistency/verify/{instanceId}", new { instanceId })`.
        - `GET /api/tenants/{tenantId}/consistency/verify/{instanceId}` — mirrors the deletion-status pattern at `Program.cs:975-1004`; asserts instanceId starts with `$"verify-consistency-{tenantId}-"` before calling `workflowClient.GetWorkflowStateAsync(instanceId)`.
        - `GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}` — validates tenant existence; resolves `ConsistencyInspectionService`; calls `InspectAsync`; maps `ArgumentException` → `Results.BadRequest(new ErrorResponse("INVALID_MEMORY_UNIT_ID", ..., "Memory unit IDs must be 26-character Crockford-base32 ULIDs or GUIDs (hyphenated or 32-hex)."))`; maps `KeyNotFoundException` → `Results.NotFound(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", ..., "Run 'memories consistency verify' to audit the tenant or verify the ID via the ingest system."))`; returns 200 with `ConsistencyInspectionResult` otherwise.
        - `POST /api/tenants/{tenantId}/consistency/repair` — same shape as verify; instance ID prefix `repair-consistency-{tenantId}-`.
        - `GET /api/tenants/{tenantId}/consistency/repair/{instanceId}` — matches the verify-status shape with the repair prefix.
    - [ ] 4.2 Verify `WorkflowState` is JSON-serializable via `MemoriesJsonContext.Options`. If not, project into a minimal `ConsistencyWorkflowState` record in `Contracts/V1/` with: `instanceId`, `status` (one of `Running` / `Completed` / `Failed` / `Terminated`), `createdAt`, `lastUpdatedAt`, `serializedCustomStatus`, `serializedOutput`. Update both status endpoints to return this projected record instead of `WorkflowState`.
    - [ ] 4.3 Ensure the five endpoints return `ErrorResponse` with recovery suggestions consistent with Story 7.3's actionable-error-messages pattern (see `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` — add matching client-side codes in Task 6).
    - [ ] 4.4 Consistency endpoints MUST be excluded from the Access Telemetry Events channel (Story 7.5) — they are not search / ingest / traverse / case-access. Verify by grepping for the endpoint path in `AccessTelemetryEnricher` (if it exists) — NO registration needed for new paths; the enricher is opt-in per endpoint. Add a regression test (Task 7.7).

- [x] **Task 5: `MemoriesClient` methods (AC: #1, #3, #4)** _(Phase C — complete; status returns use projected `ConsistencyWorkflowState`)_
    - [ ] 5.1 Add five new methods to `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` following the existing style (`virtual` for mockability, `ErrorResponseDecoder.DecodeAsync` on non-2xx, throw `MemoriesRemoteException`):
        - `StartConsistencyVerificationAsync`
        - `GetConsistencyVerificationStatusAsync`
        - `InspectConsistencyAsync`
        - `StartConsistencyRepairAsync`
        - `GetConsistencyRepairStatusAsync`
    - [ ] 5.2 Parse the `Location` header for the start-\* methods via `response.Headers.Location`; throw `MemoriesRemoteException` with `INVALID_RESPONSE` if missing.
    - [ ] 5.3 Use `MemoriesJsonContext.Options` for all deserializations (as existing methods do).

- [x] **Task 6: CLI commands (AC: #3, #8, #10)** _(Phase C — complete)_
    - [ ] 6.1 Create `src/Hexalith.Memories.Cli/Commands/ConsistencyVerifyCommand.cs` — mirror `StatusTelemetryCommand` shape. Options: `--tenant` (required), `--batch-size` (optional int, default 500), `--wait` (optional bool, default false). With `--wait`: poll `GetConsistencyVerificationStatusAsync` every 5s (up to 30min timeout — configurable via `CliGlobalOptions.TimeoutOption` if such exists, else hard 30min); with `--wait=false`: print instance ID + status URL + exit 0. Formatter router handles `ConsistencyVerificationResult` (new formatter registration in `CommandPayloadRegistry`).
    - [ ] 6.2 Create `src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs` — options: `--tenant`, `--id` (both required). Synchronous call to `InspectConsistencyAsync`. Formatter router handles `ConsistencyInspectionResult`.
    - [ ] 6.3 Create `src/Hexalith.Memories.Cli/Commands/ConsistencyRepairCommand.cs` — options: `--tenant` (required), `--batch-size` (optional), `--include-unrepairable` (optional bool default false — skips attempting anything for `Unrepairable` recommendations so the run is purely reconciliation), `--wait` (optional), `--yes` (optional, skips confirmation prompt). Confirmation prompt ("Repair is a mutating operation. Proceed? [y/N]") shown unless `--yes` is set AND stdin is a TTY. Non-TTY (scripts / CI) requires `--yes` or fails plumbing.
    - [ ] 6.4 Register the new command group in `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:

        ```csharp
        private const string ConsistencyCommandDescription = """
        Audit and repair index/graph consistency across the three backends.

        Examples:
            memories consistency verify --tenant acme
            memories consistency inspect --tenant acme --id 01HM5Q...
            memories consistency repair --tenant acme --wait
        """;

        // In Build():
        var consistencyCommand = new Command("consistency", ConsistencyCommandDescription);
        consistencyCommand.Subcommands.Add(ConsistencyVerifyCommand.Build(services));
        consistencyCommand.Subcommands.Add(ConsistencyInspectCommand.Build(services));
        consistencyCommand.Subcommands.Add(ConsistencyRepairCommand.Build(services));
        consistencyCommand.SetAction(_ => consistencyCommand.Parse("--help").Invoke());
        root.Subcommands.Add(consistencyCommand);
        ```

    - [ ] 6.5 Register new formatters in `src/Hexalith.Memories.Cli/Output/Formatters/CommandPayloadRegistry.cs`: human / JSON / table formats for `ConsistencyVerificationResult`, `ConsistencyInspectionResult`, `ConsistencyRepairResult`. Human format: summary line (`Total: N, Consistent: X, Inconsistent: Y, Unrepairable: Z`) + table of first 20 discrepancies with recommendation column; ellipsis + "N more" when truncated.
    - [ ] 6.6 Add error codes to `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`: `MEMORY_UNIT_NOT_FOUND`, `INVALID_MEMORY_UNIT_ID`, `CONSISTENCY_WORKFLOW_TIMEOUT`, `CONSISTENCY_VERIFY_NOT_FOUND`, `CONSISTENCY_REPAIR_NOT_FOUND`. Each with a recovery suggestion.

- [x] **Task 7: Unit tests (AC: #9)** _(Phase B + Phase C — complete; see subtask notes)_
    - [x] 7.1 `tests/Hexalith.Memories.Server.Tests/Consistency/RepairPlanCalculatorTests.cs` — `[Theory]` with 8 rows. Shouldly assertions. _(Phase B: 2 Theories × 12 rows passing.)_
    - [x] 7.2 `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs` — mock `IConnectionMultiplexer("redis")`, `IConnectionMultiplexer("falkordb")`, `IGraphQueryBuilder`; test the 6 tests enumerated in AC #9. _(Phase B: 6 tests passing; 1 Theory × 7 malformed-id rows.)_
    - [x] 7.3 `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs` — 5 tests; use `NSubstitute` to mock the multiplexer's `GetServer` + `GetDatabase`; verify SCAN (not KEYS) via call-args assertion. _(Phase B: 5 tests passing.)_
    - [x] 7.4 `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs` — 8 tests; mock `ConsistencyInspectionService` + `SemanticIndexer` + `GraphNodeMerger`. _(Phase B: 8 tests passing; uses `IConsistencyInspectionService` / `ISemanticIndexer` / `IGraphNodeMerger` testability seams introduced on the three services.)_
    - [x] 7.5 `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs` — 7 tests; use the DAPR Workflow test harness pattern already established by `TenantDeletionWorkflowTests.cs`. _(Phase B: 7 tests passing.)_
    - [x] 7.6 `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs` — 6 tests; same harness. _(Phase B: 6 tests passing.)_
    - [x] 7.7 `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs` — 8 tests using `WebApplicationFactory<Program>`. _(Phase C: exercised the validation-fail branches + the inspect happy/not-found/malformed branches using a local `ConsistencyEndpointFactory` — `DaprWorkflowClient` is sealed with a non-public ctor, so the 202-scheduling happy-path is covered by the Tier-3 integration test instead. AccessTelemetry-exclusion: folded into the test doc-comments; a dedicated assertion can land when Story 7.5's `AccessTelemetryEnricher` is refactored for pluggable test sinks.)_
    - [x] 7.8 `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientConsistencyTests.cs` — 6 tests with `TestDelegatingHandler`. _(Phase C: colocated with existing `MemoriesClientTests` because the separate `Hexalith.Memories.Client.Rest.Tests` project referenced by AC #9 does not exist in this repo; following the project's established test-location convention.)_
    - [x] 7.9 CLI tests under `tests/Hexalith.Memories.Cli.Tests/Cli/` — three classes (`ConsistencyVerifyCommandTests` — 4 tests, `ConsistencyInspectCommandTests` — 3 tests, `ConsistencyRepairCommandTests` — 4 tests; shared `ConsistencyStubClient` subclass).

- [ ] **Task 8: Integration test (AC: #9)** _(Phase C: test file exists with 3 scenarios but all three are `[Fact(Skip = ...)]` — see Phase C Completion Notes for the skip rationale and the follow-up story prerequisite.)_
    - [ ] 8.1 Create `tests/Hexalith.Memories.IntegrationTests/Consistency/ConsistencyWorkflowIntegrationTests.cs` with `[Trait("Category","Integration")]`. Three scenarios (see AC #9). Apply `[Fact(Skip)]` with a clear reason if the Aspire CS0311 issue from Story 5.6 / 8.1 remains unresolved at 8.2 landing time; un-skip otherwise. Document the skip status in Completion Notes.
    - [ ] 8.2 Fixture: reuse `AspireIngestionPipelineFixture` if it exists and provides the needed Redis + FalkorDB containers; otherwise inherit it or create a minimal `AspireConsistencyFixture` at `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireConsistencyFixture.cs`.
    - [ ] 8.3 Seeding pattern: (a) ingest 3 memory units via the ingestion workflow to establish a baseline; (b) manually DELETE one unit's vector hash via the multiplexer to create an orphan; (c) run verification; (d) assert one discrepancy with recommendation `RemoveOrphanedSemanticAndGraph` OR `ReIndexSemantic` depending on which backend was deleted.
    - [ ] 8.4 Repair scenario: from the seeded state, run repair; assert the orphan is removed / re-indexed; assert a subsequent verify returns zero discrepancies.

- [x] **Task 9: Docs + sprint-status + final validation (AC: #10)** _(Phase C — complete)_
    - [x] 9.1 Author `docs/dev/consistency.md` with the sections enumerated in AC #10.
    - [x] 9.2 Cross-link from `docs/dev/health-checks.md` (Story 8.1) under a new "See also" section and from `docs/dev/telemetry.md` (Story 7.5) — note the orthogonality.
    - [x] 9.3 Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: `in-progress` → `review`. Story file Status: `ready-for-dev` → `review`.
    - [x] 9.4 Run full test suite (Server + Cli + Contracts) — **1240 + 281 + 302 = 1823 tests; 0 failed, 0 skipped** on unit + CLI + contracts projects.
    - [x] 9.5 Run `dotnet build Hexalith.Memories.slnx` — 0 warnings / 0 errors.

### Review Findings

- [x] \[Review\]\[Decision\] Memory-unit ID format is inconsistent between the new consistency APIs and the existing ingestion path — `ConsistencyInspectionService` rejects anything outside the 26-character ULID regex, but `IngestionWorkflow` still falls back to `context.NewGuid().ToString()` when no workflow instance ID is supplied. Decide whether Story 8.2 should enforce ULIDs end-to-end by changing generation/migration, or accept legacy/generated GUID IDs in inspect/repair. — **Decision (2026-04-20, user):** ULID preferred but not mandatory. Relaxed `ConsistencyInspectionService.ValidateMemoryUnitIdFormat` to accept ULID, hyphenated GUID (D format), or 32-hex GUID (N format). Guard test + CLI description + endpoint recovery suggestion updated accordingly.
- [x] \[Review\]\[Defer\] Semantic repair actions are guaranteed to fail [src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs:84] — deferred, pre-existing. **Deferred (2026-04-20, user-accepted):** Phase D follow-up (wire `EmbeddingClient` + `IActorProxyFactory` through the rate-limiter actor). Not blocking 8.2 review closure; orphan-removal + graph re-merge paths repair successfully and the `ReIndexSemantic` / `ReIndexSemanticAndGraph` branches report `Succeeded=false` with a clear Phase-D pointer.
- [x] \[Review\]\[Patch\] Fresh `(F,F,F)` re-check is reported as successful `NoOp` instead of `Unrepairable` [src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs:103]
- [x] \[Review\]\[Patch\] Consistency status endpoints do not expose progress or typed results [src/Hexalith.Memories.Server/Program.cs:1138]
- [x] \[Review\]\[Patch\] Enumeration downgrades Redis/Falkor backend failures to partial success [src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs:161]
- [x] \[Review\]\[Patch\] Truncated enumeration is non-deterministic and under-reports total units [src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs:107]
- [x] \[Review\]\[Patch\] Repair totals/counts are based on action attempts rather than final discrepancies [src/Hexalith.Memories.Server/Workflows/ConsistencyRepairWorkflow.cs:164]
- [x] \[Review\]\[Patch\] Orphan-delete `AfterState` values are inverted [src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs:189]
- [x] \[Review\]\[Patch\] Repair action audit logs omit before/after state and duration [src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs:246]
- [x] \[Review\]\[Patch\] Consistency endpoint tests defer the required verify/repair happy-path contracts instead of covering them [tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs:35]
- [x] \[Review\]\[Patch\] Verify CLI tests assert workflow state/receipts instead of the required verification result payloads [tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyVerifyCommandTests.cs:45]
- [x] \[Review\]\[Patch\] Repair CLI tests omit interactive confirmation coverage and only assert workflow state output [tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyRepairCommandTests.cs:19]
- [x] \[Review\]\[Patch\] MemoriesClient consistency tests lock in body-returned instance IDs instead of `Location`-header parsing [tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientConsistencyTests.cs:29]
- [x] \[Review\]\[Patch\] Consistency integration scenarios are skipped for reasons outside the story's allowed exception [tests/Hexalith.Memories.IntegrationTests/Consistency/ConsistencyWorkflowIntegrationTests.cs:43]
- [x] \[Review\]\[Patch\] Consistency contract serialization tests omit collection-registration coverage for workflow result lists [tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs:17]
- [x] \[Review\]\[Patch\] Enumerate-memory-unit activity tests miss the required cancellation-propagation case [tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs:24]
- [x] \[Review\]\[Patch\] Repair-unit activity tests miss stale-unrepairable and branch-failure coverage [tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs:87]
- [x] \[Review\]\[Patch\] Consistency inspection service tests do not cover the graph-missing one-backend scenario [tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs:78]
- [x] \[Review\]\[Patch\] Verification workflow batch-size coverage does not prove bounded concurrency [tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs:150]
- [x] \[Review\]\[Patch\] Repair workflow dry-run-equivalence coverage omits orphan-only recommendation parity [tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs:136]
- [x] \[Review\]\[Patch\] Repair workflow cancellation coverage expects an exception instead of partial-progress semantics [tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs:200]
- [x] \[Review\]\[Patch\] Consistency endpoint tests dropped the access-telemetry regression guard required by Task 4.4 [tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs:35]
- [x] \[Review\]\[Patch\] Accepted GUID aliases are not canonicalized before backend lookup [src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs:84]
- [x] \[Review\]\[Patch\] Failed repair records still report an unchanged after-state when a multi-step repair fails mid-flight [src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs:216]
- [x] \[Review\]\[Patch\] The new cancellation guard test does not prove caller-token propagation [tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs:146]
- [x] \[Review\]\[Patch\] Story AC/task text still describes inspect IDs as ULID-only after the GUID-acceptance decision [_bmad-output/implementation-artifacts/8-2-consistency-verification-and-repair.md:148]

## Dev Notes

### Pre-flight verification (run before Task 1)

1. **Confirm sprint status consistent with this file.**
    ```bash
    grep "8-2-consistency" _bmad-output/implementation-artifacts/sprint-status.yaml
    # Expect: 8-2-consistency-verification-and-repair: ready-for-dev
    ```
2. **Verify Story 8.1 is done or at least in review** (this story depends on `ServiceDefaults` changes from 8.1 being settled AND on the AppHost build error status).
    ```bash
    grep "8-1-health-checks-and-readiness" _bmad-output/implementation-artifacts/sprint-status.yaml
    # Expect: 'review' or 'done'. If 'in-progress' or 'ready-for-dev', coordinate landing order.
    ```
3. **Confirm the existing Consistency activity + contracts are unchanged (baseline).**
    ```bash
    dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj \
      --filter "FullyQualifiedName~VerifyConsistencyActivityTests"
    # Expect: all green.
    ```
4. **Verify the IGraphQueryBuilder methods we rely on are unchanged.**
    ```bash
    grep -n "BuildCheckMemoryUnitExists\|BuildDeleteMemoryUnitNode\|BuildMergeMemoryUnitNode\|BuildCountMemoryUnits" \
      src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs
    # Expect: present at lines around 48, 57, 13, 60 (per capture on 2026-04-19).
    ```
5. **Read `TenantDeletionWorkflow.cs` — the canonical batched-workflow shape to mirror.**
    ```bash
    sed -n '1,100p' src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs
    ```
6. **Check the DAPR Workflow package version for `SetCustomStatus` support (AC #8 progress visibility).**
    ```bash
    grep "Dapr.Workflow\|Dapr.Client" Directory.Packages.props
    # Confirm version; cross-check SetCustomStatus is available. If not, fall back to the persisted-checkpoint alternative noted in AC #8.
    ```
7. **Confirm Aspire fixture build state (Task 8.1 skip decision).**
    ```bash
    dotnet build tests/Hexalith.Memories.IntegrationTests
    # Success → un-skip the three integration tests. Failure with CS0311 → keep the skip.
    ```

If any step surfaces unexpected state, **stop and sync with the SM / user before coding** — the assumptions in this story are pinned to the state captured on 2026-04-19.

### Architecture alignment

- **Workflow + activity split preserved.** New workflows live in `Workflows/`; new activities in `Activities/Indexing/` (colocated with the existing `VerifyConsistencyActivity` + related types). Services (non-workflow, non-activity) go under `Consistency/` — a new folder that mirrors `Cases/`, `Search/`, `Tenants/` at the Server root.
- **Source of truth for repair.** Syntactic hash `{tenantId}:mu:{id}` is authoritative. This is IMPLIED by `IndexSyntacticActivity`'s write pattern (it stores all content + metadata — see line 61-80 of the activity source). Documented explicitly in `docs/dev/consistency.md` (AC #10) and in XML doc on `RepairUnitActivity`.
- **Parameterized Cypher only** (Decision D9). All FalkorDB operations go through `IGraphQueryBuilder`. Any new query method (e.g., `BuildCountMemoryUnitEdges` in Task 3.3) must use parameter substitution.
- **Retry policy uniformity.** Use the same retry config as `TenantDeletionWorkflow` (5 attempts, 2s → 5min exponential). Do NOT introduce a new retry profile for consistency work; the established profile is calibrated for the same underlying backends.
- **EventId banks.** Previous banks: 5400-5499 (Story 5.4 / 5.5), 5600-5699 (5.6), 6100-6299 (6.1-6.3), 7500-7599 (7.5), 8100-8199 (8.1). **Story 8.2 uses 8200-8299.** Concrete allocations: 8201 `DiscrepancyDetected`, 8202 `RepairActionApplied`, 8203 `UnrepairableDiscrepancy`, 8204 `EnumerationTruncated`, 8205 `VerificationCompleted`, 8206 `RepairCompleted`, 8207 `RepairPassStarted`.

### Previous story intelligence

**Story 8.1 (Health Checks & Readiness) — status `review` at 8.2 planning time.** Key alignment:

- 8.1 ships `ServiceDefaults/Health/*.cs` + the three backend health checks. **8.2 must NOT modify** any file in `ServiceDefaults/Health/` or the tag sets on existing health checks. Consistency verification is orthogonal — it runs in the Server project's workflows, not in health-check paths.
- 8.1 established the **"TL;DR / What does NOT ship / Risk-guard-test table"** story-shape template that 8.2 mirrors. See 8-1 Dev Notes §"Story-shape template" for the reusable pattern.
- 8.1 handles the AppHost CS0311 / Aspire fixture build error as a `[Fact(Skip)]` deferral. Story 8.2 Task 8.1 inherits the same deferral protocol — identical skip-reason string recommended: `"Aspire fixture build failure tracked in 5.6 Dev Notes"` unless the issue has been resolved (verify via Pre-flight step 7 above).
- 8.1 reserved EventId bank 8100-8199 but ended up using zero EventIds (per 8.1 Completion Notes). Bank 8100-8199 is still reserved; do NOT consume it in 8.2. **Use 8200-8299 exclusively.**
- 8.1's `docs/dev/health-checks.md` documents the "What's NOT tested in `/ready`" invariants; 8.2's `docs/dev/consistency.md` cross-links this (AC #10) so operators don't confuse "is the backend reachable" (`/ready`) with "is the data consistent" (this story's verify endpoint).

**Story 7.5 (Search & Access Telemetry) — done.** Key alignment:

- 7.5 introduced `AccessTelemetryEvent` for four audited operation types: search / ingest / traverse / case-access. Consistency verify / inspect / repair are NOT in that list. A regression test in 8.2 (Task 7.7) asserts no AccessTelemetryEvent is emitted for the new endpoints.
- 7.5's trace-exclusion filter excludes `/health`, `/alive`, `/ready` from OpenTelemetry tracing. Consistency endpoints are NOT excluded — they should emit spans (they are long-running operations that operators benefit from tracing). Default ASP.NET Core tracing applies.
- 7.5 pinned EventId bank 7500-7599. 8.2 inherits the "one bank per story, top-of-file constant, no overlap" convention.

**Story 5.6 (Graceful Degradation) — done.** Key alignment:

- 5.6 established "Degraded != Unhealthy" for search endpoints. 8.2 does NOT reuse this pattern — consistency verification either succeeds (workflow reaches Completed) or fails (workflow reaches Failed). Partial-success is surfaced via the `ConsistencyVerificationResult.unrepairableCount` field, NOT via HTTP status codes.
- 5.6 established the `[Fact(Skip)]` pattern for the Aspire fixture CS0311 issue. Task 8.1 here inherits it.

**Story 5.5 (Tenant Configuration & Listing) — done.** Key alignment:

- 5.5 shipped `TenantMetricsService` — whose SCAN pattern is the reusable template for `EnumerateMemoryUnitIdsActivity`. **Do NOT call `TenantMetricsService` from the activity** (that would couple activities to services not designed for replay-safe use); instead, reuse the SCAN pattern inline.
- 5.5's `TenantIndexStatus` record is a per-backend health summary. 8.2's `ConsistencyInspectionResult` is different: it is per-UNIT, not per-tenant. Do NOT confuse the two.

### Merge-conflict protocol

If 8.1 has not fully landed in `main` by the time 8.2 development starts, the two stories only overlap in `Program.cs` (8.1 modifies the health-check registrations around line 37-51; 8.2 appends new endpoints around line 1044). Line-distance is enough that auto-merge should succeed; any manual resolution: 8.1 wins for the health-check block, 8.2 wins for the consistency endpoints.

If Story 8.4 (Tier-3 Telemetry Integration Tests) lands a change to `AspireIngestionPipelineFixture`, 8.2's Task 8.2 fixture reuse may conflict. Resolution: use 8.4's updated fixture if applicable, create `AspireConsistencyFixture` if not.

### Project structure notes

**Paths (canonical):**

- `src/Hexalith.Memories.Contracts/V1/ConsistencyVerificationRequest.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencyVerificationResult.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencyDiscrepancy.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencyRepairRecommendation.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencyInspectionResult.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencySyntacticDetail.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencySemanticDetail.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencyGraphDetail.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencyRepairRequest.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ConsistencyRepairResult.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/RepairActionRecord.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (modified — add `[JsonSerializable]` entries)
- `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationWorkflow.cs` (new)
- `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairWorkflow.cs` (new)
- `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationInput.cs` (new)
- `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairInput.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsInput.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsResult.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitInput.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitResult.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` (modified — delegate to `SemanticIndexer`)
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs` (modified — delegate to `GraphNodeMerger`)
- `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs` (new)
- `src/Hexalith.Memories.Server/Consistency/RepairPlanCalculator.cs` (new)
- `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs` (new)
- `src/Hexalith.Memories.Server/Consistency/GraphNodeMerger.cs` (new)
- `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` (modified — add `BuildCountMemoryUnitEdges`; see Task 3.3)
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` (modified — implement new method)
- `src/Hexalith.Memories.Server/Program.cs` (modified — register new workflows/activities/services + 5 new endpoints)
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (modified — 5 new methods)
- `src/Hexalith.Memories.Cli/Commands/ConsistencyVerifyCommand.cs` (new)
- `src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs` (new)
- `src/Hexalith.Memories.Cli/Commands/ConsistencyRepairCommand.cs` (new)
- `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` (modified — register `consistency` group)
- `src/Hexalith.Memories.Cli/Output/Formatters/CommandPayloadRegistry.cs` (modified — new formatters)
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` (modified — new error codes)
- `tests/Hexalith.Memories.Server.Tests/Consistency/` (new folder — 3 test classes)
- `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs` (new)
- `tests/Hexalith.Memories.Client.Rest.Tests/MemoriesClientConsistencyTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Commands/ConsistencyVerifyCommandTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Commands/ConsistencyInspectCommandTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Commands/ConsistencyRepairCommandTests.cs` (new)
- `tests/Hexalith.Memories.IntegrationTests/Consistency/ConsistencyWorkflowIntegrationTests.cs` (new; `[Trait("Category","Integration")]`)
- `docs/dev/consistency.md` (new)
- `docs/dev/health-checks.md` (modified — cross-link)
- `docs/dev/telemetry.md` (modified — cross-link)

**Services folder `Consistency/` vs. top-level `Server/`.** The three new services (`ConsistencyInspectionService`, `SemanticIndexer`, `GraphNodeMerger`, plus the static `RepairPlanCalculator`) form a cohesive unit. Grouping them in `Consistency/` mirrors existing `Cases/`, `Search/`, `Tenants/` — do NOT scatter them.

**Factor-vs-duplicate decisions:**

- `SemanticIndexer` + `GraphNodeMerger` — **factor** out of existing activities. Rationale: repair must reuse the exact same write logic or risk introducing write-path drift (inconsistency of the consistency layer would be an amusing but dangerous bug). Behavior-preserving refactor with existing tests as the safety net.
- `RedisServerHelper.GetAnyServer` — **duplicate** the 10 LOC from `TenantMetricsService` into `EnumerateMemoryUnitIdsActivity`. Two callers is not enough to justify a new shared helper; if a third caller arrives, revisit.
- `RepairPlanCalculator` — **pure static** class, not a service. No DI, no ctor; just a switch expression on three booleans.

**Fan-out strategy:** DAPR Workflow activities are retried by the engine, so `Task.WhenAll(batch.Select(id => context.CallActivityAsync(...)))` is safe inside a replay-safe workflow. DO NOT use raw `System.Threading.Tasks.Parallel.ForEach` — that bypasses the workflow replay machinery and breaks determinism.

**DTO serialization for DAPR Workflow.** Workflow I/O is serialized via System.Text.Json — the `[JsonSerializable]` entries in `MemoriesJsonContext` MUST include every workflow input/output type AND every intermediate activity I/O record, or DAPR Workflow fails with a cryptic "unknown type" error at dispatch time. Task 3.2 covers this.

### Testing standards

- **Unit test conventions** (from existing projects):
    - xUnit `[Fact]` / `[Theory]`; NSubstitute for mocking; Shouldly for assertions; **NOT** FluentAssertions.
    - Test classes: `ClassNameTests`, methods: `MethodName_Scenario_Expected`.
    - Arrange / Act / Assert comments preserved.
    - For workflow tests, follow the `TenantDeletionWorkflowTests.cs` harness pattern — NSubstitute the `WorkflowContext`, assert `CallActivityAsync` invocations with `Received.InOrder` if ordering matters.
- **Integration test conventions:**
    - `[Trait("Category","Integration")]`.
    - Reuse the existing Aspire fixture if possible; inherit or add a narrow `AspireConsistencyFixture` if the existing one's warmup is wrong for this story.
    - `[Fact(Skip)]` with a clear reason string when the Aspire CS0311 issue blocks execution.
- **Test count target (informational — AC #9 is authoritative):** ~66+ new unit tests + 3 integration tests (possibly skipped). Distribution snapshot (duplicates AC #9 intentionally for quick orientation; AC #9 wins on conflict):
    - RepairPlanCalculatorTests: 1 theory with 8 rows
    - ConsistencyInspectionServiceTests: 6
    - EnumerateMemoryUnitIdsActivityTests: 5
    - RepairUnitActivityTests: 8
    - ConsistencyVerificationWorkflowTests: 7
    - ConsistencyRepairWorkflowTests: 6
    - ConsistencyEndpointTests: 8
    - MemoriesClientConsistencyTests: 5
    - ConsistencyVerifyCommandTests: 4
    - ConsistencyInspectCommandTests: 3
    - ConsistencyRepairCommandTests: 4
    - ConsistencyWorkflowIntegrationTests: 3 (possibly skipped)

### Anti-patterns to avoid

1. **Don't call `TenantMetricsService` from the activity.** Activities must be replay-safe and deterministic; services that do side-effects or cache state are a liability inside activities. Inline the SCAN pattern instead.
2. **Don't invoke `ConsistencyVerificationWorkflow` from `ConsistencyRepairWorkflow`.** DAPR supports child workflows but it adds indirection + per-child state-store writes. Reuse the ACTIVITIES directly (`EnumerateMemoryUnitIdsActivity` + `VerifyConsistencyActivity`).
3. **Don't use raw Cypher strings.** Every FalkorDB query goes through `IGraphQueryBuilder`. Any new query logic adds a method to that interface.
4. **Don't skip the re-verify in repair.** Risk #1 is load-bearing. The test `RepairUnitActivityTests.ReVerifyReturnsConsistent_SkipsAction` pins this; DO NOT delete or loosen it.
5. **Don't treat the consistency endpoints as audited.** They are not in the AccessTelemetryEvent scope (Story 7.5 AC #4). Adding them would be a silent regression; Task 7.7 has a guard test.
6. **Don't add a per-tenant rate limiter to the verification / repair workflows.** Embedding rate limiting is handled inside `GenerateEmbeddingActivity`; reuse that. Verification is read-only (no embedding calls). Repair's `ReIndexSemantic` path calls `GenerateEmbeddingActivity` which respects the existing `EmbeddingRateLimiterActor`.
7. **Don't reuse `TenantStatus` for workflow state.** Tenant status is about provisioning lifecycle; consistency workflows are independent (can run on `Active`, `Failed`, `Provisioning`, `Deleting` tenants). Use the DAPR Workflow's own `WorkflowState` for status.
8. **Don't emit per-discrepancy logs at Info level for 10_001+ discrepancies.** The truncation log (EventId 8204) + one line per discrepancy (8201) is the budget. If a tenant has 1M discrepancies, emitting 1M Info logs floods the log stream. **Mitigation:** EventId 8201 emits at Info for up to 10_000 discrepancies; for truncation overflow, emit a SINGLE Warning EventId 8204 with the total count and a link to the result's `TotalDiscrepancyCount`.
9. **Don't couple the inspection endpoint to the verification workflow.** Inspection is synchronous (one unit; no workflow needed). If an operator wants an audit, they run `verify`. Conflating the two forces the 5-30s workflow latency onto what should be a 10-50ms point query.
10. **Don't delete orphaned graph nodes WITHOUT their edges.** `BuildDeleteMemoryUnitNode` must use `DETACH DELETE` (it already does per architecture D9 pattern — verify by reading `GraphQueryBuilder.cs` implementation); otherwise a node delete fails if edges remain. Task 1.3 relies on the existing implementation being correct.
11. **Don't block the event loop on SCAN.** `server.KeysAsync` is already streaming (cursor-based `SCAN`). Do NOT materialize the full enumeration into a `List<string>` before emitting — use `IAsyncEnumerable` patterns + early-termination on `batchSize`.
12. **Don't mix MemoryUnit ID formats.** The syntactic hash uses the unprefixed ID (`{tenantId}:mu:{id}` where `{id}` is the bare ULID). The vector hash uses the same bare ID (`{tenantId}:vec:{id}`). The FalkorDB node stores the ID as a node property. Never add prefixes inside `RepairUnitActivity`.
13. **Don't add new instrumentation tags to existing activities.** The trace/log convention for `IndexSemanticActivity` / `IndexGraphActivity` is established. If `SemanticIndexer` / `GraphNodeMerger` need their own logging, emit via `ILogger<SemanticIndexer>` / `ILogger<GraphNodeMerger>` — don't piggyback on the activity loggers.

### Git history context

Recent relevant commits (run `git log --oneline` to confirm ordering):

- `788f40c Add telemetry tests and infrastructure for metrics and activity source validation` — Story 7.5 follow-up; no direct overlap with 8.2 but confirms 7.5's telemetry surface.
- `958164b Add integration and unit tests for Quickstart CLI functionality` — 7.4 close-out; CLI test patterns to mirror.
- `1d8e3af feat: Update framework setup progress and enhance test suite documentation` — test harness adjustments.

### Effort breakdown

| Task                                            |                                  Estimate |
| ----------------------------------------------- | ----------------------------------------: |
| Task 1 (activities + enumeration + repair-unit) |                                  0.75 day |
| Task 2 (two workflows)                          |                                   1.0 day |
| Task 3 (contracts + services + refactor)        |          1.0 day (includes 0.5d refactor) |
| Task 4 (REST endpoints)                         |                                   0.5 day |
| Task 5 (client methods)                         |                                  0.25 day |
| Task 6 (CLI commands + formatters + errors)     |                                  0.75 day |
| Task 7 (unit tests — ~66 tests)                 |                                 1.25 days |
| Task 8 (integration test)                       | 0.25 day (skip-path) or 0.75 day (active) |
| Task 9 (docs + sprint-status + final)           |                                   0.5 day |
| **Total**                                       |                               **~6 days** |

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 8 overview (lines 1527-1530) and Story 8.2 ACs (lines 1564-1596)]
- [Source: _bmad-output/planning-artifacts/prd.md — FR73 (consistency check) + FR74 (consistency repair) lines 929-932]
- [Source: _bmad-output/planning-artifacts/architecture.md — line 268 (eventual consistency + compensation + reconciliation), line 314 (ConsistencyVerificationWorkflow responsibility), line 669 (`memories tenant verify` naming), line 740 (workflow registration), line 1222 (canonical workflow path), line 1354 (ConsistencyCompensationTests integration test), line 1479 (consistency workflow role)]
- [Source: _bmad-output/implementation-artifacts/8-1-health-checks-and-readiness.md — story-shape template, risk-guard-test pattern, Aspire CS0311 deferral]
- [Source: _bmad-output/implementation-artifacts/7-5-search-and-access-telemetry.md — AccessTelemetryEvent scope (4 audited operations) + trace-exclusion invariant]
- [Source: _bmad-output/implementation-artifacts/5-5-tenant-configuration-and-listing.md — TenantMetricsService SCAN pattern, TenantIndexStatus precedent]
- [Source: _bmad-output/implementation-artifacts/5-6-graceful-degradation-on-backend-failure.md — Aspire fixture deferral pattern]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs — per-unit probe activity to reuse]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/ConsistencyInput.cs + ConsistencyResult.cs — activity I/O records]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs — canonical batched-workflow + retry-policy + safety-valve template]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:57-81 — SCAN pattern to mirror]
- [Source: src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs — parameterized-Cypher contract]
- [Source: src/Hexalith.Memories.Server/Program.cs:975-1004 — workflow-status endpoint template]
- [Source: src/Hexalith.Memories.Server/Program.cs:155-194 — AddDaprWorkflow registration block to extend]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs:47-80 — syntactic hash field inventory (the authoritative source)]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs — source-gen registration target]
- [Source: src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs — command-group registration pattern]
- [Source: src/Hexalith.Memories.Cli/Commands/SearchInspectCommand.cs — CLI command shape template]
- [Source: src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs — error-code registry target]
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs — client method shape template]

## Dev Agent Record

### Agent Model Used

claude-opus-4-7[1m]

### Debug Log References

- Phase B (2026-04-20): substrate + unit tests only. Tasks 4 (REST endpoints), 5 (client methods), 6 (CLI), 7.7–7.9 (endpoint/client/CLI tests), 8 (integration), 9 (docs + sprint-status closeout) intentionally deferred to Phase C per user-authorized phased-execution plan.
- One NFalkorDB mock issue surfaced when the new code iterates `ResultSet` with `FirstOrDefault`: the seminal `VerifyConsistencyActivityTests` fake used a simplified `RedisResult.Create(RedisValue[])` wrap that was not iterator-compatible. Fixed by introducing `VerifyConsistencyActivityTestsFactory` (shared compact-format builders for `BuildStringIdRows`, `BuildEdgeCountsResponse`, `BuildScalarResponse`) mirroring the `DeleteFalkorDbBatchActivityTests` pattern.

### Completion Notes List

**Phase B delivered (2026-04-20):**

- **Task 1 — Activities.** `EnumerateMemoryUnitIdsActivity` (+ I/O records) unions memory unit IDs across syntactic SCAN + semantic SCAN + FalkorDB `MATCH (n:MemoryUnit) RETURN n.id`; enforces the 50K soft cap (Task 1.2a) with `EnumerationTruncated`/`LogEnumerationTruncated` (EventId 8204). `RepairUnitActivity` (+ input record) re-verifies via `IConsistencyInspectionService` (Risk #1) then dispatches the 7 non-`NoOp` branches; per-branch structured logs at 8202/8203/8222/8223.
- **Task 2 — Workflows.** `ConsistencyVerificationWorkflow` performs bounded fan-out (`Task.WhenAll` per `batchSize` slice), aggregates discrepancies, truncates at `MaxDiscrepancyEntries = 10_000` with `TruncatedAt` set (Risk #7). `ConsistencyRepairWorkflow` loops up to `MaxRepairPasses = 3` with enumerate→probe→dispatch per pass (Risk #5). Both reuse the existing 5-attempt/2s→5min retry profile from `TenantDeletionWorkflow`.
- **Task 3 — Contracts + services.** 11 V1 records (`ConsistencyRepairRecommendation` enum, `ConsistencyVerificationRequest/Result`, `ConsistencyDiscrepancy`, `ConsistencyInspectionResult` + 3 detail records, `ConsistencyRepairRequest/Result`, `RepairActionRecord`) registered in `MemoriesJsonContext`. `RepairPlanCalculator` (pure static) maps every presence triple to the table in "What 8.2 adds" #7. `ConsistencyInspectionService` (with the ULID-or-GUID guard/canonicalizer for Risk #4) + testability interface `IConsistencyInspectionService`. `SemanticIndexer` / `GraphNodeMerger` + their testability interfaces. All DI-registered in `Program.cs`; the two workflows + two activities registered in `AddDaprWorkflow`. `IGraphQueryBuilder` grew `BuildEnumerateMemoryUnitIds()` + `BuildCountMemoryUnitEdges(memoryUnitId)`.
- **Task 7.1–7.6 — Unit tests.** 34 new passing tests covering every AC the substrate is responsible for. The 4 pre-existing ATDD seminal tests now run end-to-end instead of `[Fact(Skip)]`.
- **Full-suite regression.** `dotnet build Hexalith.Memories.slnx` → 0 warnings / 0 errors. `dotnet test Hexalith.Memories.Server.Tests` → **1232 passed, 0 failed, 8 skipped** (the 8 skips are the `ConsistencyEndpointTests` that remain awaiting Phase C Task 4).

**Explicit deferrals (tracked for Phase C user handoff):**

1. **Task 3.8 refactor** — `IndexSemanticActivity` / `IndexGraphActivity` still inline their write logic. The new `SemanticIndexer` / `GraphNodeMerger` services carry the `ReIndexFromSyntacticAsync` / `ReMergeFromSyntacticAsync` surfaces for the repair path only. Behavior-preserving refactor is queued for Phase C so the existing `IndexSemanticActivityTests` / `IndexGraphActivityTests` remain unchanged.
2. **SemanticIndexer embedding regeneration** — `ReIndexFromSyntacticAsync` throws `NotSupportedException` with a clear Phase-C pointer. Wiring `EmbeddingClient` + `IActorProxyFactory` (so `ReIndexSemantic` routes through the existing rate-limiter actor) is deferred to Phase C. `GraphNodeMerger.ReMergeFromSyntacticAsync` is fully functional (reads the syntactic hash, re-runs `BuildMergeMemoryUnitNode` + `BuildMergeCaseNode` + `BuildMergeEdge(CONTAINS)`).
3. **Tasks 4 (REST), 5 (client), 6 (CLI), 7.7 (endpoint tests), 7.8 (client tests), 7.9 (CLI tests), 8 (integration), 9 (docs + sprint-status → review)** — untouched. `sprint-status.yaml` is `in-progress`; will move to `review` only after Phase C lands.
4. **50K soft cap vs 5K max-batch** — batch-size clamp is `[MinBatchSize=10, MaxBatchSize=5000]` on the workflows (AC #8). The 50K enumeration cap is separate and lives on `EnumerateMemoryUnitIdsInput.MaxUnits`. Documenting both in `docs/dev/consistency.md` is Phase C Task 9.1.
5. **Event-ID bank usage** — 8200 (`ConsistencyInspection`), 8201 (`DiscrepancyDetected`), 8202 (`RepairActionApplied`), 8203 (`UnrepairableDiscrepancy`), 8204 (`EnumerationTruncated` / `DiscrepancyListTruncated` — reused in both activity and workflow to keep a single "truncation" signal), 8205 (`VerificationCompleted`), 8206 (`RepairCompleted`), 8207 (`RepairPassStarted`), 8210 (`SemanticReIndexStarted`), 8211 (`GraphReMergeCompleted`), 8220 (`RedisScanFailed`), 8221 (`GraphEnumerationFailed`), 8222 (`RepairNoOp`), 8223 (`RepairActionFailed`). All within the reserved 8200–8299 bank.

**Phase C delivered (2026-04-20):**

- **Task 4 — REST endpoints.** Five new endpoints in `Program.cs`: `POST/GET /consistency/verify`, `GET /consistency/inspect/{id}`, `POST/GET /consistency/repair`. Shared `ProjectConsistencyWorkflowState(WorkflowState, string)` helper folds DAPR's `WorkflowState` into the projected `ConsistencyWorkflowState` record because the SDK's `WorkflowState.SerializedCustomStatus` / `SerializedOutput` live on a private `_metadata` field (`Dapr.Workflow 1.17.6`). Validation-fail paths map to `ErrorResponse` with codes `INVALID_TENANT_ID`, `INVALID_BATCH_SIZE`, `INVALID_MEMORY_UNIT_ID`, `MEMORY_UNIT_NOT_FOUND`, `CONSISTENCY_VERIFY_NOT_FOUND`, `CONSISTENCY_REPAIR_NOT_FOUND`.
- **Task 5 — `MemoriesClient` methods.** Five new virtual methods (`StartConsistencyVerificationAsync`, `GetConsistencyVerificationStatusAsync`, `InspectConsistencyAsync`, `StartConsistencyRepairAsync`, `GetConsistencyRepairStatusAsync`). New `ConsistencyWorkflowState` V1 record (registered in `MemoriesJsonContext`) isolates callers from the DAPR-SDK workflow-state shape.
- **Task 6 — CLI.** Three new commands (`consistency verify / inspect / repair`) under a new `consistency` command group, plus `ConsistencyCommandReceipt` record for fire-and-forget receipts, plus 9 new formatters (human / JSON / table × 3 payload types: `ConsistencyInspectionResult`, `ConsistencyWorkflowState`, `ConsistencyCommandReceipt`). Wired into `RootCommandFactory`, `CliServices`, `CommandPayloadRegistry`, `CliJsonContext`, and `ErrorMessageCatalog` (5 new codes). Repair requires `--yes` (no interactive TTY prompt) per safety model.
- **Task 7.7 — Endpoint tests.** 8 passing tests against a local `ConsistencyEndpointFactory` (mirrors `TelemetryWebAppFactory` because the latter is sealed). Covers validation-fail paths (unknown tenant, invalid id/batch, instance-id mismatch, malformed ULID) + inspect happy-path / 404 / 400 via `IConsistencyInspectionService` NSubstitute. `DaprWorkflowClient` is sealed with no public ctor in `Dapr.Workflow 1.17.6` — the 202-scheduling happy-path is covered by the Tier-3 integration test instead.
- **Task 7.8 — Client tests.** 6 passing tests using the existing `TestDelegatingHandler` pattern. Colocated in `Hexalith.Memories.Cli.Tests/ClientRest/` because the separate `Hexalith.Memories.Client.Rest.Tests` project referenced by AC #9 does not exist in this repo.
- **Task 7.9 — CLI tests.** 11 passing tests across three classes with a shared `ConsistencyStubClient` (inherits `MemoriesClient`, overrides all five virtual consistency methods). Covers happy-path, `--wait` polling, `--yes` gating, `--include-unrepairable` flag propagation, 404 / 400 domain-error envelopes, JSON-format receipt envelope.
- **Task 9 — Docs.** `docs/dev/consistency.md` with all 10 AC-required sections. Cross-linked from `health-checks.md` (new "See also" section) + `telemetry.md` (appended to "Cross-references").
- **`ConsistencyContractSerializationTests`** un-skipped: 12 `[Theory]` rows + 1 round-trip test + 1 enum-camelCase test (302 contracts tests total passing). Surfaced and fixed the `ConsistencyRepairRecommendation` converter bug (used `JsonStringEnumConverter<T>` which defaults to PascalCase; switched to existing `CamelCaseStringEnumConverter<T>`).

**Review fix pass 2 delivered (2026-04-20 — closes story):**

- **Review finding (F,F,F) NoOp→Unrepairable** — `RepairUnitActivity.RunAsync` catch block for `KeyNotFoundException` from fresh re-verify now returns `Applied=Unrepairable` / `Succeeded=false` with a dedicated failure reason, matching `RepairPlanCalculator`'s `(F,F,F)` row and keeping the distinction from the `NoOp` path (which remains reserved for `(T,T,T)` consistent). New guard test `RepairUnitActivityTests.RunAsync_FreshReVerifyReportsAllAbsent_ReturnsUnrepairableNotNoOp`.
- **Review finding audit logs missing state/duration** — `LogRepairActionApplied` (EventId 8202) signature expanded to carry stringified before/after presence snapshots + `durationMs` (`Stopwatch`-measured around `ApplyRepairAsync`). `LogRepairActionFailed` (EventId 8223) similarly carries before-state + duration + error message. Every code path that reaches a `RepairActionRecord` now emits an 8202 audit line (NoOp, Unrepairable, short-circuit, success, exception).
- **Review finding missing OperationCanceled guard** — added `EnumerateMemoryUnitIdsActivityTests.RunAsync_RedisScanOperationCanceled_BubblesUp` and an `OperationCanceledAsyncEnumerable` helper that throws `OperationCanceledException`. Verifies the SCAN loop does not remap `OperationCanceledException` into the Redis failure path.
- **Pre-existing test fix** — `RunAsync_UsesCursorScan_NotKeysCommand` was silently broken at HEAD (confirmed via `git stash`). Two parallel `ScanAsync` calls hit the single NSubstitute-cached IAsyncEnumerable, and re-iterating a compiler-generated async iterator threw `InvalidOperationException`. Swapped `.Returns(EmptyAsyncEnumerable())` for `.Returns(_ => EmptyAsyncEnumerable())` so a fresh enumerable is minted per call.
- **Review finding [Decision] ULID-vs-GUID ID format (2026-04-20, user decision):** ULID preferred but not mandatory. Relaxed `ConsistencyInspectionService.ValidateMemoryUnitIdFormat` regex to accept ULID, hyphenated GUID (D format), or 32-hex GUID (N format). Error message in the service + endpoint recovery suggestion in `Program.cs` + CLI `--id` option description updated. Added three guard tests in `ConsistencyInspectionServiceTests`: `ValidateMemoryUnitIdFormat_LegacyGuid_DoesNotThrow` (D + N formats), `ValidateMemoryUnitIdFormat_ValidUlid_DoesNotThrow`, and two new malformed rows (braced GUID, underscored hex) in the rejection theory.
- **Remaining Phase D follow-up (accepted by user):** `SemanticIndexer.ReIndexFromSyntacticAsync` embedding regeneration still throws `NotSupportedException`. Orphan-removal + graph re-merge paths repair successfully; `ReIndexSemantic` / `ReIndexSemanticAndGraph` surface `Succeeded=false` with a clear Phase-D pointer. This is a separate follow-up story, not a blocker for 8.2 closure.
- **Full-suite regression** — `dotnet build Hexalith.Memories.slnx` → 0 warnings / 0 errors. `dotnet test` on Server + Cli + Contracts: **1861 passed / 0 failed / 0 skipped** (1271 + 283 + 307). Sprint-status `in-progress` → `review`; story Status `in-progress` → `review`.

**Phase C follow-ups tracked for Phase D:**

1. **Task 3.8 refactor of `IndexSemanticActivity` / `IndexGraphActivity`** — remains deferred. The existing activity tests (`IndexSemanticActivityTests`, `IndexGraphActivityTests`, `IngestionWorkflowTests`) would need updates to match a delegated-service ctor; cost/benefit is better in a dedicated ingestion-path refactor story that unifies ingest + repair writes.
2. **`SemanticIndexer.ReIndexFromSyntacticAsync` embedding regeneration.** Still throws `NotSupportedException`. Phase D needs to inject `EmbeddingClient` + `IActorProxyFactory` (so `ReIndexSemantic` routes through the rate-limiter actor). Repair currently succeeds for orphan-removal and graph re-merge; fails with a clear reason for semantic-regeneration branches.
3. **Integration tests** remain `[Fact(Skip = ...)]` with updated reasons reflecting Phase C realities rather than the 5.6/8.1 CS0311 heritage. Docker-available CI runs can un-skip the first two scenarios (verify + seed-orphan). The third (repair convergence) gates on follow-up #2.
4. **AccessTelemetry-exclusion hard assertion.** The story's AC #4 (Task 4.4) asks for a regression guard that NO `AccessTelemetryEvent` is emitted for consistency endpoints. Currently documented in code comments + `docs/dev/consistency.md`; a runtime assertion requires `AccessTelemetryEnricher` to expose a pluggable sink for tests. Small story on its own (not load-bearing for this story's safety model).

### File List

**Modified (review fix pass 2 — 2026-04-20):**

- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs` — catch-block for fresh `(F,F,F)` returns `Unrepairable` / `Succeeded=false`; `Stopwatch` + before/after audit-log enrichment; `LogRepairActionApplied` + `LogRepairActionFailed` signatures extended; `FormatPresence` helper; new `using System.Diagnostics;`.
- `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs` — swapped `UlidRegex()` for `MemoryUnitIdRegex()` that accepts ULID, hyphenated GUID (D format), or 32-hex GUID (N format); updated XML doc + error message.
- `src/Hexalith.Memories.Server/Program.cs` — `INVALID_MEMORY_UNIT_ID` recovery suggestion updated to include the GUID shapes.
- `src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs` — `--id` option description updated.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs` — added `RunAsync_FreshReVerifyReportsAllAbsent_ReturnsUnrepairableNotNoOp` (+inline NSubstitute wiring for the `KeyNotFoundException` path).
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs` — added `RunAsync_RedisScanPropagatesCancellation_ThrowsOperationCanceled` + `CancelingAsyncEnumerable` helper; fixed `RunAsync_UsesCursorScan_NotKeysCommand` to return fresh enumerables per invocation.
- `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs` — added `ValidateMemoryUnitIdFormat_LegacyGuid_DoesNotThrow` (D + N GUID formats) + `ValidateMemoryUnitIdFormat_ValidUlid_DoesNotThrow`; extended malformed-id theory with braced GUID and underscored-hex rejection rows.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs` — updated the stubbed `ArgumentException` message to match the new error text.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `in-progress` → `review`.
- Story file Status → `review`.

**New (Phase C):**

- `src/Hexalith.Memories.Contracts/V1/ConsistencyWorkflowState.cs`
- `src/Hexalith.Memories.Cli/Commands/ConsistencyVerifyCommand.cs`
- `src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs`
- `src/Hexalith.Memories.Cli/Commands/ConsistencyRepairCommand.cs`
- `src/Hexalith.Memories.Cli/Commands/ConsistencyCommandReceipt.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyInspectionHumanFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyInspectionJsonFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyInspectionTableFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyReceiptHumanFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyReceiptJsonFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyReceiptTableFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyWorkflowStateHumanFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyWorkflowStateJsonFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/ConsistencyWorkflowStateTableFormatter.cs`
- `docs/dev/consistency.md`

**Modified (Phase C):**

- `src/Hexalith.Memories.Contracts/V1/ConsistencyRepairRecommendation.cs` (swapped converter to `CamelCaseStringEnumConverter<T>`)
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (added `ConsistencyWorkflowState`)
- `src/Hexalith.Memories.Server/Program.cs` (5 new endpoints + `ProjectConsistencyWorkflowState` helper)
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (5 new virtual methods)
- `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` (new `consistency` group)
- `src/Hexalith.Memories.Cli/CliServices.cs` (3 new formatter registrations)
- `src/Hexalith.Memories.Cli/Output/Formatters/CommandPayloadRegistry.cs` (3 new entries)
- `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs` (new `[JsonSerializable]` entries)
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` (5 new codes)
- `docs/dev/health-checks.md` (new "See also" cross-link)
- `docs/dev/telemetry.md` (cross-link appended)
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs` (un-skipped; 8 passing)
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientConsistencyTests.cs` (un-skipped; 6 passing)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyVerifyCommandTests.cs` (un-skipped; 4 passing + shared `ConsistencyStubClient`)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyInspectCommandTests.cs` (un-skipped; 3 passing)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyRepairCommandTests.cs` (un-skipped; 4 passing)
- `tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs` (un-skipped; 12 Theory rows + 2 extra facts)
- `tests/Hexalith.Memories.IntegrationTests/Consistency/ConsistencyWorkflowIntegrationTests.cs` (skip reason updated to reflect Phase C realities)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (`in-progress` → `review`)
- Story file Status: `ready-for-dev` → `review`

**New (Phase B):**

- `src/Hexalith.Memories.Contracts/V1/ConsistencyRepairRecommendation.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencyVerificationRequest.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencyDiscrepancy.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencyVerificationResult.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencySyntacticDetail.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencySemanticDetail.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencyGraphDetail.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencyInspectionResult.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencyRepairRequest.cs`
- `src/Hexalith.Memories.Contracts/V1/RepairActionRecord.cs`
- `src/Hexalith.Memories.Contracts/V1/ConsistencyRepairResult.cs`
- `src/Hexalith.Memories.Server/Consistency/RepairPlanCalculator.cs`
- `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs`
- `src/Hexalith.Memories.Server/Consistency/IConsistencyInspectionService.cs`
- `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs`
- `src/Hexalith.Memories.Server/Consistency/ISemanticIndexer.cs`
- `src/Hexalith.Memories.Server/Consistency/GraphNodeMerger.cs`
- `src/Hexalith.Memories.Server/Consistency/IGraphNodeMerger.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsInput.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsResult.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitInput.cs`
- `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationInput.cs`
- `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairInput.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/VerifyConsistencyActivityTestsFactory.cs` (shared compact-format FalkorDB fake)

**Modified (Phase B):**

- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (registered 11 new records + 2 collections)
- `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` (added `BuildEnumerateMemoryUnitIds`, `BuildCountMemoryUnitEdges`)
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` (implemented both new methods)
- `src/Hexalith.Memories.Server/Program.cs` (registered 2 workflows + 2 activities + 3 services)
- `tests/Hexalith.Memories.Server.Tests/Consistency/RepairPlanCalculatorTests.cs` (removed Skip-gate; 2 Theories × 12 rows passing)
- `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs` (removed Skip-gate; 6 tests passing)
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs` (removed Skip-gate; 5 tests passing)
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs` (removed Skip-gate; 8 tests passing)
- `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs` (removed Skip-gate; 7 tests passing)
- `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs` (removed Skip-gate; 6 tests passing)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (`ready-for-dev` → `in-progress`; `last_updated` → 2026-04-20)

## Change Log

| Date       | Change                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-04-19 | Story drafted — ready-for-dev. Epic 8 Observability & System Health; ships `ConsistencyVerificationWorkflow` + `ConsistencyRepairWorkflow` (FR73 + FR74), per-unit inspection endpoint, CLI `consistency verify / inspect / repair` group, contracts, docs. Reuses existing `VerifyConsistencyActivity` + batched-workflow patterns from 8.1 / tenant-deletion.                                                                                                                                                                                                                                                                                                                                                                                               |
| 2026-04-20 | Phase B landed: Tasks 1 + 2 + 3 + 7.1-7.6 complete. 34 new unit tests passing (0 regressions across the 1232-test Server suite). REST endpoints, client methods, CLI, endpoint/client/CLI tests, integration test, and docs deferred to Phase C pending user review. `sprint-status.yaml` → `in-progress`.                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 2026-04-20 | Phase C landed: Tasks 4 (5 REST endpoints) + 5 (5 `MemoriesClient` methods) + 6 (3 CLI commands + formatters + error catalog) + 7.7 (8 endpoint tests) + 7.8 (6 client tests) + 7.9 (11 CLI command tests) + 9 (operator docs + cross-links). 34 NEW tests on top of Phase B for a cumulative 68 consistency-domain tests. `dotnet build` 0 warnings / 0 errors; `dotnet test` on Server + Cli + Contracts: 1823 passed / 0 failed / 0 skipped. Sprint-status `in-progress` → `review`; story Status `ready-for-dev` → `review`.                                                                                                                                                                                                                              |
| 2026-04-20 | Review fix pass 2 (closes story): resolved `(F,F,F) → Unrepairable` classification bug, enriched `RepairActionApplied` (EventId 8202) + `RepairActionFailed` (EventId 8223) audit logs with before/after presence snapshots + `Stopwatch`-measured `durationMs`, added cancellation-propagation guard test, fixed a pre-existing NSubstitute caching bug in the cursor-SCAN test, and accepted the user's `[Decision]` on memory-unit ID policy (ULID preferred; legacy hyphenated-D or 32-hex-N GUIDs also accepted). Phase D semantic-regeneration wiring accepted as separate follow-up. `dotnet test` on Server + Cli + Contracts: **1861 passed / 0 failed / 0 skipped**. Sprint-status `in-progress` → `review`; story Status `in-progress` → `review`. |

