# Story 6.3: Retry, Failure Visibility & Re-Ingestion

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** the ingestion **observability + recovery** layer that sits on top of the pipeline hardened by 6.1 (surface) and 6.2 (reliability). Concretely: (1) `IngestionSettings` gains a **per-activity retry policy map** (`Ingestion:RetryPolicies:<activityName>` → `{ MaxAttempts, FirstRetryIntervalSeconds, BackoffCoefficient, MaxRetryIntervalSeconds }`) with the current hard-coded values preserved as **defaults** when no override is supplied — `IngestionWorkflow.CreateMainRetry()` becomes `CreateRetryFor(activityName)`. Default policy shape is unchanged; the workflow simply reads its per-activity override per call, so nothing regresses (FR9). (2) `FailureDetails` record gains **`ErrorMessage`** and **`LastRetryAt`** properties (additive — defaults preserve current behavior); populated by the existing `AttachFailureDetails` helper (FR11). (3) **A new `PersistFailedUnitActivity`** writes a durable record of the failed memory unit to Redis (hash `{tenantId}:failed-unit:{memoryUnitId}` with `failureDetailsJson`, `sourceUri`, `caseId`, `stage`, `lastRetryAt`, `failedAt`) so NFR19 ("never silently dropped") is enforced by _persistence_, not just by an event on the activity stream. The workflow's final catch-all invokes it before re-throwing; the **existing** `RecordCaseActivityActivity(IngestionFailed)` event stream continues to drive the per-case `FailedCount`. (4) A **per-case failed-unit index** (Redis sorted set `{tenantId}:case:{caseId}:failed-units` scored by `failedAt` unix-ms) is maintained alongside the hash so `GET /api/tenants/{tenantId}/cases/{caseId}/failed-units` can return the list paginated by most-recent-failure-first (FR11). Note that `TotalCount` on the page represents _currently-unresolved_ failed units (decreases on re-ingestion or delete) while `CaseStatusDetail.FailedCount` represents the _historical IngestionFailed event count_ (monotonically increasing, stream-based) — these are semantically different metrics; the divergence is documented for operators. (5) **`CaseStatusDetail` gains four counts** — `QueuedCount`, `ExtractingCount`, `EmbeddingCount`, `IndexingCount` — maintained by a new **`CaseIngestionCounterActor`** (DAPR virtual actor, one per `{tenantId}:{caseId}` pair, Actor ID = `"{tenantId}:{caseId}"`). The actor state is a compact record `CaseIngestionCounterState(int Queued, int Extracting, int Embedding, int Indexing)`. The workflow calls `actor.TransitionAsync(previousStage, nextStage)` via a thin activity (`UpdateCaseIngestionCounterActivity`) at each stage transition — the actor atomically decrements the previous bucket and increments the next. At terminal transitions (`Indexed`, `Failed`, duplicate short-circuit), the final call decrements the last in-flight bucket; indexed and failed counts continue to flow from their existing sources (FalkorDB node count for `IndexedCount`; activity stream for `FailedCount`) so the actor holds ONLY the four in-flight counts. O(1) read via `actor.GetCountsAsync()` from the status endpoint — no fan-out, no 1000-instance cap, **no `IsApproximate` field on the public contract**. The actor is the canonical cross-cutting concern for live ingestion bucketing (architecture §D24 / §D25 compliant). (6) **Re-ingestion endpoints:** `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest` (single) and `POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest` with body `{ "memoryUnitIds": ["...","..."] }` or `{ "all": true, "limit": 500 }` (bulk). Both rebuild an `IngestionInput` from the persisted failed-unit hash and schedule a new `IngestionWorkflow`. **Idempotency is preserved** — the existing `CheckIdempotencyActivity` sees the pre-existing dedup key and short-circuits, which is WRONG for re-ingestion. Therefore re-ingestion **deletes the dedup key first** (`{tenantId}:failed-unit:re-ingestion-cleanup`) as an atomic Lua script that (a) removes the dedup key, (b) removes the failed-unit hash, (c) removes the failed-unit entry from the per-case sorted set. Then the new workflow starts with a clean slate (FR12). (7) **Structured log events 6301–6309** (see Reference: Log Events). (8) A new `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` endpoint (currently missing — see Architecture Compliance) that returns the current `MemoryUnit` _including_ the `FailureDetails` object when `Status == Failed` (FR11 detail exposure).

**What does NOT ship:** workflow state persistence / cold-start replay validation beyond what DAPR Workflow and Redis AOF already guarantee (that's 6.4, the last story of Epic 6); CLI surface for any of these endpoints (Epic 7); MCP surface (Epic 10); automatic deletion of very old failed units (TTL/retention is an operator concern, Phase 2 — failed units persist until manually re-ingested or explicitly deleted, per NFR19 spirit); per-tenant retry policy override (the retry map is _per activity type_, uniform across tenants — per-tenant retry knobs are Phase 2); a dashboard UI (Epic 8 observability); re-ingestion of _indexed_ (non-failed) units (the epic's AC for bulk re-ingestion of "previously ingested content" is scoped to the failed-units list for MVP — re-ingesting indexed units is operationally redundant since indexing is idempotent and costly; deferred to Phase 2 with an operator rationale); auto-classification of transient vs. permanent failures (all failures land in the same failed-units registry — operator judges recoverability from `ErrorCode` + `Stage`); retry-count inspection from mid-flight workflows (exposed only after terminal state via `FailureDetails.RetryCount`); consistency between `FailedCount` (count of `IngestionFailed` events in the stream) and the new failed-units registry cardinality — they can diverge temporarily if a failed unit is deleted or if a unit fails multiple times across re-ingestions; document the divergence rather than try to reconcile (see Known MVP Limitations).

**Primary risks:** (1) **Dedup-key race on re-ingestion.** If operator A clicks "re-ingest" and operator B clicks "re-ingest" for the same unit ~100 ms apart, two workflows schedule against the same `(tenantId, caseId, sourceUri)`. Mitigation: the re-ingestion Lua script atomically deletes the dedup key AND the failed-unit hash AND the per-case sorted-set entry in one round-trip, then returns a boolean — if FALSE ("already gone"), the HTTP handler returns 409 Conflict with `"Another re-ingestion is already in progress for this unit."`. This surfaces the race to the caller instead of double-scheduling. (2) **Workflow restart vs. replay confusion.** DAPR Workflow does NOT support "resume a failed workflow from the failed activity" — the original workflow instance is terminal. Re-ingestion starts a **new workflow instance**. The _memory unit ID_ may change or may be preserved: architecture §D Memory Unit Model says `Id` is ULID and generated — so on re-ingest, the _new_ workflow would generate a _new_ memory unit ID unless we preserve the old one. **Decision: preserve the memory unit ID across re-ingestion** by passing the original id as the DAPR workflow `instanceId` parameter on `ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId: memoryUnitId, input: ingestionInput)`. The workflow's existing `context.InstanceId`-based memory-unit-id fallback (`IngestionWorkflow.cs:32-34`) picks it up with zero workflow code changes. **The public `IngestionInput` contract does NOT gain a `PreferredMemoryUnitId` field** — architectural review rejected that as a capability leak to future CLI callers. Operators see the re-ingested unit reuse the original ID, so annotations, references, and graph edges survive. Document this as the **canonical re-ingestion contract**. If DAPR SDK 1.17.6 does not honor caller-specified `instanceId` on `ScheduleNewWorkflowAsync`, fall back to the internal `ReIngestionInput` wrapper (Breaking Changes #2) — verified at pre-implementation step 5. (3) **`PersistFailedUnitActivity` itself fails.** The workflow catch-all calls it; if it throws (Redis hiccup), the outer catch should still re-throw the original exception but log the persistence failure via event 6309. Do NOT swallow both — the original failure is the story, the persistence hiccup is a secondary telemetry concern. (4) **`CaseIngestionCounterActor` drift.** A workflow that crashes between `--previous` and `++next` could leave a bucket over- or under-counted. Mitigation: the actor's `TransitionAsync` is a single atomic method (the actor is single-threaded per DAPR guarantee, identical to `EmbeddingRateLimiterActor.ReportRateLimitedAsync` — see Story 6.2 Primary Risks #6). The workflow calls it once per transition; DAPR Workflow replay re-invokes the same transition with the same arguments, and the actor handles that as an additional decrement/increment pair — **which is wrong** if the transition was already applied. **Therefore the actor dedups via an idempotency key:** each call carries a `transitionId` (workflow `instanceId + sequence number`) and the actor records the last applied transitionId per-workflow; duplicate transitionIds are no-ops. See Actor Design Rationale for the implementation sketch. Residual drift risk: process SIGKILL during actor state write before persistence (DAPR actor state is fsync'd — this window is microseconds). Accept. Phase 2 adds a periodic reconciler against the failed-units registry + FalkorDB count. (5) **Actor read on status endpoint.** `CaseIngestionCounterActor.GetCountsAsync` is one DAPR actor roundtrip (~1-3 ms in-process) — no fan-out, no 1000-instance cap, no `IsApproximate`. The status endpoint's cost is bounded regardless of tenant scale. (6) **Redis sorted-set cardinality bound.** `{tenantId}:case:{caseId}:failed-units` is unbounded by design (NFR19 = never silently dropped). Failed units accumulate until manually re-ingested or deleted. A tenant that ingests garbage indefinitely will accumulate failed entries indefinitely. Operator mitigation documented (no implementation in 6.3). (7) **Re-ingestion of a unit whose `SourceUri` points to a now-missing source.** The workflow will fetch/extract and fail again with the same error — the unit stays in Failed status with `RetryCount` incremented (or reset, depending on the new workflow's retry budget exhaustion). **Decision: the persisted `FailureDetails.RetryCount` reflects the _current_ (latest) workflow's retries only, not cumulative across re-ingestions.** A cumulative count is Phase 2. (8) **Renaming `CreateMainRetry()`** is a minor breaking change for `IngestionWorkflow` tests that assert on the policy; update them. Compensation retry `CreateCompensationRetry()` is **unchanged** — no operator knob in 6.3. (9) **`GET /memory-units/{id}` did not previously exist** — this story adds it. It must respect tenant-id guard, return 404 with standard `ErrorResponse` shape, and include `FailureDetails` in the response body when present.

## Breaking Changes (Pre-Gate-3 MVP)

1. **`FailureDetails` record** (`src/Hexalith.Memories.Contracts/V1/FailureDetails.cs`) gains two optional properties: `string? ErrorMessage` (defaults null), `DateTimeOffset? LastRetryAt` (defaults null). Record constructor: use positional init-only parameters with nullable defaults so existing callers `new FailureDetails(stage, code, count)` compile unchanged. Populated by `IngestionWorkflow.AttachFailureDetails` — extract `exception.Message` (truncated to 1024 chars — architecturally bounded to prevent Redis hash inflation) and set `LastRetryAt = context.CurrentUtcDateTime`.

2. **New internal input record `ReIngestionInput`** (`src/Hexalith.Memories.Server/Ingestion/ReIngestionInput.cs` — **internal, server-only, NOT in Contracts**) carries the re-ingestion-only `PreferredMemoryUnitId`:

    ```csharp
    internal sealed record ReIngestionInput
    {
        public required IngestionInput Ingestion { get; init; }
        public required string PreferredMemoryUnitId { get; init; }
    }
    ```

    Re-ingestion endpoints build a `ReIngestionInput`, then schedule the workflow via a **dedicated workflow-starter method** that writes the preferred id into the DAPR Workflow `instanceId` parameter: `ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId: preferredMemoryUnitId, input: ingestionInput)`. The workflow body's existing `context.InstanceId`-derived memory-unit-id fallback (`IngestionWorkflow.cs:32-34`) naturally picks it up — **no `PreferredMemoryUnitId` field on the public `IngestionInput` contract** and no capability leak to future CLI callers. **Public `IngestionInput` is unchanged** by this story. Validation is enforced at the re-ingestion endpoint (`PreferredMemoryUnitId` must match the failed-unit's original id and must be a valid ULID/GUID-parseable string). `IngestionInputValidator` does not change.

3. **`IngestionSettings`** (`src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs`) gains:

    ```csharp
    public Dictionary<string, ActivityRetryPolicy> RetryPolicies { get; init; } = new();
    ```

    where `ActivityRetryPolicy` is a new record in the same file:

    ```csharp
    public sealed record ActivityRetryPolicy
    {
        public int MaxAttempts { get; init; } = 5;
        public double FirstRetryIntervalSeconds { get; init; } = 2.0;
        public double BackoffCoefficient { get; init; } = 1.5;
        public double MaxRetryIntervalSeconds { get; init; } = 300.0;
    }
    ```

    Keys are activity class names (e.g., `"GenerateEmbeddingActivity"`, `"ExtractContentActivity"`, `"IndexSyntacticActivity"`). Missing keys fall back to the default. No DI registration change — bound via existing `IngestionSettings` section.

4. **`IngestionWorkflow`** (`src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`):
    - `CreateMainRetry()` is **removed**. Replace with a **per-invocation snapshot** built once at the top of `RunAsync`:
        ```csharp
        // Replay-safe: RetryPolicyBuilder.SnapshotAll() returns an immutable
        // IReadOnlyDictionary<string, WorkflowTaskOptions> captured from the
        // process-global policy table. Called once per workflow invocation;
        // every subsequent activity call reads from this local snapshot so
        // replays observe identical WorkflowTaskOptions values regardless of
        // whether the global table has been re-initialized in the host.
        IReadOnlyDictionary<string, WorkflowTaskOptions> retry = RetryPolicyBuilder.SnapshotAll();
        WorkflowTaskOptions For(string activityName) =>
            retry.TryGetValue(activityName, out WorkflowTaskOptions? opts) ? opts : retry[RetryPolicyBuilder.DefaultKey];
        ```
        Every activity call then uses `For(nameof(XxxActivity))`. The snapshot is held as a local variable inside `RunAsync`; DAPR Workflow replay executes `RunAsync` from the top and rebuilds the snapshot identically (the process-global table is treated as effectively immutable within the lifetime of an instance by convention — hot-reload is Phase 2, documented).
    - **Rejected alternatives:** (a) workflow constructor DI — DAPR workflow activations do not honor scoped DI the way activities do, and constructor injection of settings would bleed host state into replay; (b) threading the map through `IngestionInput` — bloats the public contract and violates the capability-leak concern flagged against `PreferredMemoryUnitId`; (c) calling `RetryPolicyBuilder.For(name)` inline at every call site — works today but is one hot-reload away from a replay-determinism bug, and this story documents that as a Must-Fix from the architectural review.
    - `RetryPolicyBuilder` exposes `Initialize(IngestionSettings)`, `SnapshotAll()` (returns the immutable dictionary), and `DefaultKey` (const `"__default"`). See Breaking Changes #14.
    - Compensation retry continues to use the existing hard-coded `CreateCompensationRetry()` (unchanged).

5. **`Program.cs`** gains:
    - `RetryPolicyBuilder.Initialize(app.Services.GetRequiredService<IOptions<IngestionSettings>>().Value)` right after `app.Build()` and before `app.MapPost("/api/ingest", ...)`.
    - New endpoint: `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` → returns `MemoryUnit` (includes `FailureDetails` when populated).
    - New endpoint: `GET /api/tenants/{tenantId}/cases/{caseId}/failed-units?limit=50&offset=0` → returns `FailedUnitsPage` (new contract record — see #9).
    - New endpoint: `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest` → 202 Accepted with `{ "newWorkflowInstanceId": "...", "memoryUnitId": "...-preserved" }`.
    - New endpoint: `POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest` → 202 Accepted with `BulkReIngestionResponse` (new contract — see #9).

6. **`CaseStatusDetail` record** (`src/Hexalith.Memories.Contracts/V1/CaseStatusDetail.cs`) gains four positional int properties with default 0 (backward-compat — existing positional callers continue to compile):

    ```csharp
    int QueuedCount = 0,
    int ExtractingCount = 0,
    int EmbeddingCount = 0,
    int IndexingCount = 0
    ```

    Appended **after** existing `DeletionStartedAt` to keep positional order stable. No `IsApproximate` field — counts are exact (maintained by `CaseIngestionCounterActor`, O(1) read).

7. **`CaseService.GetCaseStatusAsync`** populates the new counts via `ICaseIngestionCounterActor.GetCountsAsync()` (O(1) actor read). `IndexedCount` continues to come from the FalkorDB memory-unit count (existing behavior, `CaseService.cs:419`). `FailedCount` continues to come from `CaseActivityService.GetFailedCountAsync` (existing behavior). The four new counts come from the actor. No fan-out, no cap.

8. **`IngestionWorkflow` persistence & counter hooks:**
    - At workflow start (just after `LogCurrentStatus`): call `UpdateCaseIngestionCounterActivity` with `(previousStage: "none", nextStage: "queued")` via compensation retry. The activity invokes `ICaseIngestionCounterActor.TransitionAsync("none", "queued")` which increments `Queued`.
    - At each `TransitionStatus` site, call `UpdateCaseIngestionCounterActivity` with the outgoing/incoming bucket names (e.g., `("queued", "extracting")`, `("extracting", "embedding")`, etc.). The actor atomically `--previous; ++next`.
    - On duplicate short-circuit (early return): call the activity with `("queued", "none")` to decrement the `Queued` bucket (duplicate detection happens while still in `queued`). No increment — duplicates are not in any bucket.
    - On happy-path terminal (`Indexed`): call the activity with `("indexing", "none")` to decrement `Indexing`. `IndexedCount` is sourced from FalkorDB node count; the actor does NOT track `Indexed`.
    - In the outer catch-all (line 301 of current `IngestionWorkflow.cs`), call `PersistFailedUnitActivity` BEFORE re-throwing. Wrapped in try/catch — `PersistFailedUnitActivity` failure logs event 6309 but does NOT mask the original failure. Then call the counter-update activity with `(currentStageBucket, "none")` to decrement the bucket the workflow was in when it failed; the actor does NOT track `Failed` (that's the activity stream).
    - **Ordering inside intermediate and outer catches is PINNED:** `unregister-counter → SetCustomStatus("failed") → persist-failed-unit → throw`. Any statement after `throw` is unreachable and would be a bug — this reconciles the previously contradictory Task 3.3 / Task 4.3 instructions.
    - `context.SetCustomStatus` is still called at every transition as a diagnostic breadcrumb for `DaprWorkflowClient.GetWorkflowStateAsync` callers (e.g., operators inspecting a specific stuck workflow via `/api/ingest/{instanceId}`). It is NOT read by the status endpoint anymore — the actor is the source of truth for counts.

9. **New contract records** in `src/Hexalith.Memories.Contracts/V1/`:
    - `FailedUnitSummary.cs`:
        ```csharp
        public sealed record FailedUnitSummary(
            string MemoryUnitId,
            string CaseId,
            string SourceUri,
            SourceType SourceType,
            string Stage,
            string ErrorCode,
            string? ErrorMessage,
            int RetryCount,
            DateTimeOffset? LastRetryAt,
            DateTimeOffset FailedAt);
        ```
    - `FailedUnitsPage.cs`:
        ```csharp
        public sealed record FailedUnitsPage(
            IReadOnlyList<FailedUnitSummary> Units,
            int TotalCount,
            int Limit,
            int Offset);
        ```
    - `ReIngestRequest.cs`:
        ```csharp
        public sealed record ReIngestRequest(
            IReadOnlyList<string>? MemoryUnitIds,
            bool All = false,
            int Limit = 500);
        ```
    - `BulkReIngestionResponse.cs`:

        ```csharp
        public sealed record BulkReIngestionResponse(
            int Scheduled,
            int NotFound,
            int Conflicted,
            int Errored,
            IReadOnlyList<ReIngestedUnitInfo> Units);

        public sealed record ReIngestedUnitInfo(
            string MemoryUnitId,
            string? NewWorkflowInstanceId,
            string Outcome,       // "scheduled" | "not-found" | "conflict" | "error"
            string? ErrorMessage); // populated when Outcome == "error"
        ```

        The **`error`** outcome covers mid-bulk infrastructure failures (Redis hiccup during Lua execution, DAPR workflow scheduler error). Per-unit errors are caught, logged (event 6305), and enumerated in the response rather than aborting the batch — the bulk endpoint returns 200 OK with enumerated outcomes unless the entire request fails to validate (400) or the case is not found (404).

    - Update `MemoriesJsonContext` with `[JsonSerializable]` for all new types. `IngestionInput` is unchanged (no regen needed for that type).

10. **New activities** in `src/Hexalith.Memories.Server/Activities/Ingestion/`:
    - `PersistFailedUnitActivity : WorkflowActivity<FailedUnitInput, bool>` — writes the failed-unit hash + ZADD the per-case sorted set atomically via Lua.
    - `UpdateCaseIngestionCounterActivity : WorkflowActivity<CounterTransitionInput, bool>` — invokes `ICaseIngestionCounterActor.TransitionAsync(previousStage, nextStage)` on the actor for the `{tenantId}:{caseId}` pair. Best-effort: failures log event 6309-equivalent and return false but do not break the workflow.
    - Both registered in `Program.cs` via `options.RegisterActivity<T>()`.

11. **New actor + interface** in `src/Hexalith.Memories.Server/Actors/`:
    - `ICaseIngestionCounterActor.cs`:
        ```csharp
        public interface ICaseIngestionCounterActor : IActor
        {
            Task TransitionAsync(string previousStage, string nextStage);
            Task<CaseIngestionCounts> GetCountsAsync();
            Task ResetAsync(); // admin-only, used by case delete
        }
        ```
        Stage values: `"none"`, `"queued"`, `"extracting"`, `"embedding"`, `"indexing"`. `"indexed"` and `"failed"` are NOT actor states — they source from FalkorDB and the activity stream respectively. `previousStage="none"` means increment only; `nextStage="none"` means decrement only.
    - `CaseIngestionCounterActor.cs`: mirrors `EmbeddingRateLimiterActor` pattern (Story 1.4). Persists `CaseIngestionCounterState(int Queued, int Extracting, int Embedding, int Indexing)` in DAPR actor state. `TransitionAsync` applies `--previous; ++next` atomically (single-threaded actor per DAPR guarantee, same as `EmbeddingRateLimiterActor.ReportRateLimitedAsync`). Clamps each bucket at zero (defensive against drift).
    - `CaseIngestionCounterLogic.cs`: pure static/instance logic following `RateLimiterLogic` precedent (Story 6.2 Task 1.2 pattern). `TransitionAsync` / `GetCounts` / `Reset` delegate to the logic for testability without DAPR.
    - Register via `options.RegisterActor<CaseIngestionCounterActor>()` alongside the existing rate limiter actor.

12. **New service** `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs` — a plain service (not an actor, not inside a workflow activity) accessed from endpoint handlers for:
    - `ListAsync(tenantId, caseId, limit, offset, ct)` → `FailedUnitsPage`
    - `GetAsync(tenantId, memoryUnitId, ct)` → `FailedUnitRecord?` (internal record with the full hash fields, used to rebuild `IngestionInput` for re-ingestion)
    - `RemoveAsync(tenantId, caseId, memoryUnitId, sourceUri, ct)` → `bool` — atomic Lua: DEL hash + ZREM sorted-set + DEL dedup key. Returns true when hash existed (i.e., this call removed it), false otherwise. Used by the re-ingestion endpoints to claim the unit exclusively.

13. **Log events 6301–6309** in a new file `src/Hexalith.Memories.Server/Ingestion/RetryFailureLog.cs` — mirrors the `RateLimitingLog.cs` pattern from 6.2.

14. **`RetryPolicyBuilder`** static helper in `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs`:

    ```csharp
    public static class RetryPolicyBuilder
    {
        public const string DefaultKey = "__default";

        private static IReadOnlyDictionary<string, WorkflowTaskOptions> _snapshot =
            BuildInitialSnapshot();

        public static void Initialize(IngestionSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            Dictionary<string, WorkflowTaskOptions> map = new(StringComparer.Ordinal)
            {
                [DefaultKey] = ToOptions(new ActivityRetryPolicy()),
            };
            foreach ((string name, ActivityRetryPolicy policy) in settings.RetryPolicies)
            {
                if (policy.MaxAttempts <= 0)
                {
                    throw new InvalidOperationException(
                        $"RETRY_CONFIG_INVALID: Ingestion:RetryPolicies:{name}.MaxAttempts must be > 0 (was {policy.MaxAttempts}).");
                }
                map[name] = ToOptions(policy);
            }
            _snapshot = map;
        }

        /// <summary>Returns an immutable snapshot of the current retry policy table.
        /// Callers (notably the workflow body) MUST snapshot once per invocation and
        /// read locally to preserve replay determinism. The snapshot's lifetime is
        /// independent of subsequent Initialize calls.</summary>
        public static IReadOnlyDictionary<string, WorkflowTaskOptions> SnapshotAll() => _snapshot;

        /// <summary>Convenience for non-workflow callers (tests, endpoints). Do NOT
        /// use inside a workflow body — call SnapshotAll() once at the top of
        /// RunAsync and read from the local variable instead.</summary>
        public static WorkflowTaskOptions For(string activityName) =>
            _snapshot.TryGetValue(activityName, out WorkflowTaskOptions? opts)
                ? opts
                : _snapshot[DefaultKey];

        private static WorkflowTaskOptions ToOptions(ActivityRetryPolicy p) =>
            new(new WorkflowRetryPolicy(
                maxNumberOfAttempts: p.MaxAttempts,
                firstRetryInterval: TimeSpan.FromSeconds(p.FirstRetryIntervalSeconds),
                backoffCoefficient: p.BackoffCoefficient,
                maxRetryInterval: TimeSpan.FromSeconds(p.MaxRetryIntervalSeconds)));

        private static IReadOnlyDictionary<string, WorkflowTaskOptions> BuildInitialSnapshot() =>
            new Dictionary<string, WorkflowTaskOptions>(StringComparer.Ordinal)
            {
                [DefaultKey] = ToOptions(new ActivityRetryPolicy()),
            };
    }
    ```

    **Replay-safety contract:** inside the workflow body, `SnapshotAll()` is called once at the top of `RunAsync`; every subsequent `For(name)` call reads from the local variable. This guarantees that a replay after a host-side `Initialize` reconfiguration observes the same `WorkflowTaskOptions` instances as the original execution. The `For` helper is exposed for test and endpoint use only. Values pinned at startup; no hot-reload. Hot-reload is Phase 2. A `MaxAttempts <= 0` entry fails `Initialize` fast at startup with `RETRY_CONFIG_INVALID` — prevents silently broken retry semantics.

## Story

As a developer,
I want automatic retry with configurable limits, full visibility into failures, and the ability to re-ingest failed content,
so that transient errors are handled automatically, persistent failures are diagnosable and recoverable, and no ingestion outcome is silently dropped.

## Acceptance Criteria

**Reading note for the dev agent:** ACs are labelled `[VERIFY]` (regression-guard over already-shipped behavior — write tests only, no new production code), `[NEW]` (requires new code), or `[MIXED]` (builds on existing infrastructure with additive code). AC1 is `[VERIFY+MIXED]`; AC2, AC3, AC5, AC6, AC7, AC8, AC9, AC10 are `[NEW]`; AC4 is `[MIXED]`; AC11, AC12 are `[VERIFY]`.

1. **Configurable retry per activity type (FR9).** Given `Ingestion:RetryPolicies:GenerateEmbeddingActivity: { MaxAttempts: 3, FirstRetryIntervalSeconds: 4 }` in `appsettings.json`, when the workflow schedules `GenerateEmbeddingActivity`, then the call uses `WorkflowTaskOptions` with `maxNumberOfAttempts=3` and `firstRetryInterval=4s`. When `ExtractContentActivity` is scheduled (no override in config), it uses the default `(5, 2s, 1.5, 5min)`. Verification: unit test that calls `RetryPolicyBuilder.For("GenerateEmbeddingActivity")` and `RetryPolicyBuilder.For("ExtractContentActivity")` after `Initialize` with a mixed map, asserts the returned `WorkflowRetryPolicy` parameters match expectations. Integration assertion via `IngestionWorkflowTests`: spy on `context.CallActivityAsync` to assert the options instance for each activity comes from `RetryPolicyBuilder.For(name)`. **No exception-type filtering** — DAPR SDK 1.17.6 does not support it (documented in 5.6); each activity type has ONE retry policy covering ALL exception types (same limitation as 6.2).

2. **`FailureDetails` carries `ErrorMessage` and `LastRetryAt` (FR11).** Given a workflow fails with an exception whose `Message="Provider returned 500 Internal Server Error"`, when `AttachFailureDetails` is called, then the resulting `FailureDetails` has `ErrorMessage="Provider returned 500 Internal Server Error"` (truncated at 1024 chars if longer) and `LastRetryAt = context.CurrentUtcDateTime` (the workflow's replay-safe now). Existing `Stage`, `ErrorCode`, `RetryCount` unchanged. Unit test: pass a 2000-char message, assert truncation at 1024; pass a short message, assert full preservation; assert `LastRetryAt` is set.

3. **Failed memory unit persisted to Redis (NFR19, FR11).** Given an ingestion workflow exhausts retries at any stage, when the workflow's outer catch fires, then (a) `PersistFailedUnitActivity` is invoked with a `FailedUnitInput(tenantId, caseId, memoryUnitId, sourceUri, sourceType, stage, errorCode, errorMessage, retryCount, lastRetryAt, failedAt)`; (b) the activity writes to Redis hash `{tenantId}:failed-unit:{memoryUnitId}` with fields `{tenantId, caseId, sourceUri, sourceType, stage, errorCode, errorMessage, retryCount, lastRetryAt, failedAt, failureDetailsJson}` (last field is a JSON-serialized `FailureDetails` for API round-tripping); (c) the activity ZADDs `{tenantId}:case:{caseId}:failed-units` with member = `memoryUnitId`, score = failedAt unix-ms; (d) the original exception is re-thrown unchanged. **Persistence is atomic per-unit but NOT per-case** — the hash write and ZADD are separate Redis commands; use a Lua script to execute them as one atomic unit. Integration test with a real Redis (via Aspire fixture) verifies all three side effects occur on workflow failure. Unit test with a mocked `IConnectionMultiplexer` asserts the Lua script content and key names.

4. **Workflow still records `IngestionFailed` activity event (existing behavior preserved).** Given a workflow fails, when the existing `RecordCaseActivityActivity(IngestionFailed)` call fires (preserve as-is from `IngestionWorkflow.cs:187-200`), then the Redis Stream `{tenantId}:case:{caseId}:activity` receives an event of type `IngestionFailed`. This feeds `CaseActivityService.GetFailedCountAsync` — that behavior is unchanged. **The new failed-units registry (AC3) is ADDITIONAL, not a replacement.** `FailedCount` in `CaseStatusDetail` continues to come from the activity stream count; the failed-units registry powers the new list endpoint (AC7). Regression test: existing tests for `IngestionWorkflow` failure paths still pass without modification.

5. **`IngestionWorkflow` notifies `CaseIngestionCounterActor` at every stage transition (FR10).** Given a workflow starts, when the workflow body runs its pre-steps, then `UpdateCaseIngestionCounterActivity` invokes `actor.TransitionAsync("none", "queued", transitionId)` — `Queued` bucket increments. At each `TransitionStatus` site, the activity is called with the outgoing/incoming stage pair (`("queued","extracting")`, `("extracting","embedding")`, `("embedding","indexing")`). On happy-path terminal, `("indexing","none")` decrements `Indexing`. On duplicate short-circuit, `("queued","none")` decrements `Queued`. On failure, `(currentStage,"none")` decrements the stage the workflow was in. Each call carries a **monotonic `transitionId`** (`$"{instanceId}:{sequence}"`) so the actor deduplicates replayed transitions. `context.SetCustomStatus` is also called for diagnostic breadcrumb (via `GET /api/ingest/{instanceId}`); the status endpoint does NOT read it. Unit test: mock the activity, assert each transition call is made in the expected order with the expected stage pair and a unique `transitionId`. Actor-side unit test: apply a sequence of transitions, assert bucket counts match; re-apply the same transitionId, assert idempotent (no double count).

6. **`CaseStatusDetail` exposes per-stage counts via the actor (FR10).** Given a case with 5 workflows in `Embedding` stage, 2 in `Extracting`, 3 `Indexed`, 1 `Failed`, when `GET /api/tenants/{tenantId}/cases/{caseId}/status` is called, then the response `CaseStatusDetail` carries `IndexedCount=3, FailedCount=1, EmbeddingCount=5, ExtractingCount=2, QueuedCount=0, IndexingCount=0`. Implementation: `CaseService.GetCaseStatusAsync` invokes `ICaseIngestionCounterActor.GetCountsAsync()` via `IActorProxyFactory.CreateActorProxy<ICaseIngestionCounterActor>(new ActorId($"{tenantId}:{caseId}"), nameof(CaseIngestionCounterActor))`. Returns exact counts — **no `IsApproximate` field on the public contract**. Integration test provisions a case, schedules workflows, asserts the actor's counts match the workflow states. Unit test: logic-level test on `CaseIngestionCounterLogic` for idempotency, zero-clamping, and bucket math.

7. **List failed units endpoint (FR11).** Given a case with 12 failed units, when `GET /api/tenants/{tenantId}/cases/{caseId}/failed-units?limit=5&offset=0` is called, then the response is `FailedUnitsPage { Units: [5 most-recent], TotalCount: 12, Limit: 5, Offset: 0 }`. `Units` are sorted by `FailedAt DESC`. Each `FailedUnitSummary` includes `MemoryUnitId, CaseId, SourceUri, SourceType, Stage, ErrorCode, ErrorMessage, RetryCount, LastRetryAt, FailedAt`. Validation: `limit` clamped to `[1, 500]`, `offset` clamped to `[0, 100000]`. Tenant-id and case-id guards (existing patterns). Returns 404 `CASE_NOT_FOUND` when the case does not exist. Unit tests for `FailedUnitsRegistry.ListAsync` with varying pagination; endpoint-level integration test with TestContainers/Aspire Redis.

8. **Get single memory unit endpoint with failure details (FR11 detail exposure).** Given a failed memory unit, when `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` is called, then the response body is a `MemoryUnit` record with `Status=Failed` and `FailureDetails` populated. **When the memory unit is in Failed state**, the endpoint reads from `{tenantId}:failed-unit:{memoryUnitId}` (because the indexed MU hash does not exist for pre-indexing failures) and synthesizes a `MemoryUnit` from the failed-unit hash fields (content is `""` since it was never extracted or was extracted but never persisted). **When the memory unit is in Indexed state**, reads from `{tenantId}:mu:{memoryUnitId}` as today and `FailureDetails` is null. Tenant-mismatch guard applies. Endpoint-level integration test + unit tests for both code paths. **Consistency note:** `ParseMemoryUnitFromHash` (`CaseService.cs:943`) needs to read the `failureDetailsJson` field (when present) and deserialize; add that read.

9. **Re-ingest single failed unit (FR12).** Given a failed memory unit with `MemoryUnitId="m1"`, when `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/m1/re-ingest` is called with an empty body, then (a) `FailedUnitsRegistry.GetAsync` reads the failed-unit hash into a `FailedUnitRecord`; (b) `FailedUnitsRegistry.RemoveAsync` atomically removes the hash, the sorted-set entry, AND the dedup key `{dedup:tenantId:caseId:sha256(sourceUri)}` in one Lua script; (c) an `IngestionInput` is reconstructed from the record fields (no `PreferredMemoryUnitId` on the public contract); (d) `DaprWorkflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId: "m1", input: ingestionInput)` is called — the DAPR `instanceId` parameter preserves the memory-unit-id via the workflow's existing `context.InstanceId` fallback; (e) the response is 202 Accepted with `{ newWorkflowInstanceId, memoryUnitId: "m1" }`. If step (b) returns false (already being re-ingested by another caller), return 409 Conflict with `ErrorResponse("RE_INGESTION_IN_PROGRESS", ...)`. If step (a) returns null (unit not found), return 404. **`ContentBytes` is NOT stored in the failed-unit hash** (too large, possibly already discarded) — the re-ingested workflow will re-fetch/re-extract from `SourceUri`. For `SourceType.File` with no URL, this means the file must still exist at the path recorded in `SourceUri` — otherwise the re-ingestion will fail identically; document the edge case. For `SourceType.Url`, the URL is re-fetched by `FetchUrlActivity`. Integration test + unit tests covering success, 404, 409. **`IngestedBy` on re-ingestion:** use the value from the failed-unit hash, NOT the caller's identity (preserves audit trail). Phase 2 may add a caller-identity override header.

10. **Bulk re-ingest failed units (FR12).** Given a case with 50 failed units, when `POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest` is called with body `{ "all": true, "limit": 50 }`, then (a) the registry lists up to 50 most-recent failed units; (b) each is re-ingested per AC9 (atomic claim + schedule workflow); (c) the response body is `BulkReIngestionResponse { Scheduled: 47, NotFound: 0, Conflicted: 2, Errored: 1, Units: [...] }` with each unit's outcome. When body is `{ "memoryUnitIds": ["m1","m2","m3"] }`, only those units are processed (capped at 500 per request). **Per-unit failure does not abort the batch** — each re-ingestion is independent; a claim conflict produces `outcome="conflict"`, a mid-flight infrastructure failure (Redis error, DAPR scheduler failure) produces `outcome="error"` with `ErrorMessage` populated, a missing unit produces `outcome="not-found"`. All outcomes are logged via event 6305. The endpoint returns 200 OK with enumerated outcomes (not 500) unless the request fails validation (400) or the case is missing (404). Integration test over a real Redis + mocked `DaprWorkflowClient` including at least one scripted `error` outcome. **Rate-limiting of the bulk endpoint:** MVP does not add a dedicated rate limit (the ingestion rate limiter downstream absorbs the load); operators accept that 500 concurrent workflows will fan out. Document.

11. **[VERIFY] Existing retry policy values preserved when no config override (FR9 + regression guard).** Given `appsettings.json` has NO `Ingestion:RetryPolicies` section, when any activity is scheduled, then `RetryPolicyBuilder.For(anyName)` returns `WorkflowTaskOptions(new WorkflowRetryPolicy(5, 2s, 1.5, 5min))`. This pins the **pre-6.3 baseline** so a future dev agent does not accidentally weaken retry semantics. Unit test with empty settings.

12. **[VERIFY] No regression on existing tests (6.1 + 6.2 + earlier) — zero new failures.** Baseline post-6.2: 1250 unit tests passing per the 6.2 Dev Agent Record. Expected post-6.3: **≥ 1280** passing (minimum ~30 new tests), zero new failures, pre-existing baseline failures (if any re-emerge) stay documented. Run `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` at start and end of dev-story.

## Tasks / Subtasks

**Pre-implementation checklist (run before Task 1):**

1. **Block on 6.2 being `done`** in `sprint-status.yaml` (currently `review` — Bob's recommendation). If the user insists on starting 6.3 against a `review`-state 6.2 working tree, explicitly acknowledge the risk in the Debug Log References and rebase 6.3 against the 6.2 working tree state. Do NOT start against uncommitted changes without this acknowledgement.
2. Run `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` from repo root. Record total passing/failing/skipped in Debug Log References. Expected: 1250 passing, 0 failures per 6.2 Dev Agent Record.
3. Verify `Microsoft.Extensions.TimeProvider.Testing` is in `Directory.Packages.props` (added by 6.2 — yes, per 6.2 Dev Notes).
4. Verify `DaprWorkflowClient` is already registered in DI (it is — used by existing endpoints in `Program.cs:192`).
5. **Verify DAPR SDK 1.17.6 APIs** (Amelia #5) before coding:
    - `DaprWorkflowClient.ScheduleNewWorkflowAsync(string name, string? instanceId, object? input, CancellationToken ct = default)` — confirm the `instanceId` overload exists and is nullable. If the SDK accepts a caller-specified instance id, use it in Task 6.1 / 6.2 to preserve the memory-unit-id across re-ingestion. If NOT, fall back to the `ReIngestionInput` wrapper from Breaking Changes #2.
    - `DaprWorkflowClient.GetWorkflowStateAsync(string instanceId, bool getInputsAndOutputs)` — confirm signature. Used only by the diagnostic `GET /api/ingest/{instanceId}` endpoint (existing) — not by the status reader anymore (replaced by actor).
    - `WorkflowContext.SetCustomStatus(string)` — confirm the Durable Task Framework API on DAPR Workflow 1.17.6.
    - `IActorProxyFactory.CreateActorProxy<T>(ActorId, string actorType)` — confirm the actor-type string param (existing pattern in `GenerateEmbeddingActivity`; mirror it).
6. Inspect `FailureDetails.cs`, `CaseStatusDetail.cs`, `IngestionInput.cs`, `ExtractContentActivity.cs`, `EmbeddingRateLimiterActor.cs` for the exact current shape before extending. Update Breaking Changes code snippets if they drift.
7. Inspect Story 3.5 (case deletion) to confirm whether it sweeps `{tenantId}:case:{caseId}:failed-units` and the `CaseIngestionCounterActor` state for `"{tenantId}:{caseId}"` Actor ID on case delete. If 3.5 enumerates specific keys/actors, extend it to include 6.3's artifacts; if it uses a `KEYS/SCAN` wildcard, 6.3's keys are swept automatically.

- [x] Task 1: Extend `FailureDetails` and `IngestionInput` records (AC: #2, #9, #11)
    - [x] 1.1 Modify `src/Hexalith.Memories.Contracts/V1/FailureDetails.cs` — replace with:
        ```csharp
        public sealed record FailureDetails(
            string Stage,
            string ErrorCode,
            int RetryCount,
            string? ErrorMessage = null,
            DateTimeOffset? LastRetryAt = null);
        ```
        Update `MemoriesJsonContext` if a source-gen attribute references the record directly — it does not need a new attribute (same type), but a full rebuild regenerates the serializer.
    - [x] 1.2 **`IngestionInput` public contract is NOT modified** — the original 6.3 draft proposed a `PreferredMemoryUnitId` field there; architectural review rejected it as a capability leak. Re-ingestion preserves the memory-unit-id via the DAPR Workflow `instanceId` parameter on `ScheduleNewWorkflowAsync` (the workflow's existing `context.InstanceId`-based memory-unit-id fallback picks it up with zero workflow code changes). If DAPR SDK 1.17.6 does NOT accept a caller-specified `instanceId` on `ScheduleNewWorkflowAsync`, fall back to the internal `ReIngestionInput` wrapper (Breaking Changes #2) — but **only after verifying the SDK signature** in the pre-implementation checklist step 5.
    - [x] 1.3 `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:32-34` is **unchanged** in shape. The existing line `string memoryUnitId = string.IsNullOrWhiteSpace(context.InstanceId) ? context.NewGuid().ToString() : context.InstanceId;` already honors a caller-specified `instanceId` when the re-ingestion endpoint passes one to `ScheduleNewWorkflowAsync`. No workflow code change needed for AC9.
    - [x] 1.4 Update `AttachFailureDetails` to populate the new fields:
        ```csharp
        private static void AttachFailureDetails(Exception exception, string memoryUnitId, string stage, int retryCount, DateTimeOffset now, ILogger logger)
        {
            string? message = exception.Message;
            if (message is { Length: > 1024 })
            {
                message = message[..1024];
            }
            FailureDetails details = new(stage, GetErrorCode(exception), retryCount, message, now);
            exception.Data[nameof(FailureDetails)] = details;
            exception.Data[nameof(MemoryUnitStatus)] = MemoryUnitStatus.Failed;
            exception.Data["MemoryUnitId"] = memoryUnitId;
            logger.LogError(exception, "Ingestion failed for {MemoryUnitId}. Status={Status}; stage={Stage}; errorCode={ErrorCode}; retryCount={RetryCount}; message={ErrorMessage}",
                memoryUnitId, MemoryUnitStatus.Failed, details.Stage, details.ErrorCode, details.RetryCount, details.ErrorMessage);
        }
        ```
        Update all call sites to pass `new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero)` as `now` (replay-safe).
    - [x] 1.5 Add tests in `tests/Hexalith.Memories.Contracts.Tests/V1/`:
        - `FailureDetailsSerializationTests.cs` — round-trip with and without the new fields; legacy-payload default test (deserialize a 3-field JSON payload, assert new fields are null).
        - `IngestionInputSerializationTests.cs` — **no changes** (contract unchanged).
    - [x] 1.6 Add tests in `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — one test verifies `AttachFailureDetails` populates `ErrorMessage` (truncated) and `LastRetryAt`; one test verifies that when `ScheduleNewWorkflowAsync(instanceId: "m1", input: ...)` is called, the workflow's memory-unit-id equals `"m1"` (this validates the re-ingestion contract without modifying the public input).

- [x] Task 2: Add `ActivityRetryPolicy` and `RetryPolicyBuilder` (AC: #1, #11)
    - [x] 2.1 Modify `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` — add:
        ```csharp
        public Dictionary<string, ActivityRetryPolicy> RetryPolicies { get; init; } = new(StringComparer.Ordinal);
        ```
        Verify IngestionSettings is a class or record (6.2 added fields via init-only record properties — follow suit).
    - [x] 2.2 Create `src/Hexalith.Memories.Server/Ingestion/ActivityRetryPolicy.cs` with the record defined in Breaking Changes #3.
    - [x] 2.3 Create `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs` with the implementation from Breaking Changes #14.
    - [x] 2.4 Modify `Program.cs` — after `app.Build()`, before any endpoint mapping or workflow client usage:
        ```csharp
        RetryPolicyBuilder.Initialize(app.Services.GetRequiredService<IOptions<IngestionSettings>>().Value);
        ```
    - [x] 2.5 Modify `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — delete `CreateMainRetry()`. Replace all `retryOptions` uses with `RetryPolicyBuilder.For(nameof(<ActivityName>))`:
        - `nameof(CheckIdempotencyActivity)` call at line 55 → `RetryPolicyBuilder.For(nameof(CheckIdempotencyActivity))`
        - `nameof(FetchUrlActivity)` at line 97 → similarly
        - `nameof(ExtractContentActivity)` at line 113 → similarly
        - `nameof(GenerateEmbeddingActivity)` at line 126 → similarly
        - 3 index activities at lines 164, 168, 172 → per-name policies
        - `nameof(VerifyConsistencyActivity)` at line 221 → similarly
        - `nameof(SaveDedupKeyActivity)` at line 253 → similarly
        - `nameof(RecordCaseActivityActivity)` at lines 187 and 277 → use `CreateCompensationRetry()` as today (don't change — recording is best-effort)
        - Keep `CreateCompensationRetry()` unchanged.
    - [x] 2.6 Add `appsettings.json` defaults under `Ingestion`:
        ```json
        "RetryPolicies": {
          "GenerateEmbeddingActivity": {
            "MaxAttempts": 5,
            "FirstRetryIntervalSeconds": 2.0,
            "BackoffCoefficient": 1.5,
            "MaxRetryIntervalSeconds": 300.0
          }
        }
        ```
        (One example; leave other activities defaulting.)
    - [x] 2.7 Tests in `tests/Hexalith.Memories.Server.Tests/Ingestion/RetryPolicyBuilderTests.cs`:
        - `Initialize_WithEmptySettings_ForReturnsDefault` — empty dict, `For("anyName")` returns default policy values (5, 2s, 1.5, 5min).
        - `Initialize_WithOverride_ForReturnsOverride` — map with `"GenerateEmbeddingActivity" → (3, 4s, 2.0, 60s)`, `For("GenerateEmbeddingActivity")` returns those values; `For("ExtractContentActivity")` returns default.
        - `For_BeforeInitialize_ReturnsDefault` — do not throw; return default policy.
        - `Initialize_CalledTwice_UsesLatest` — second call overrides the first.
    - [x] 2.8 Integration-style test in `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`:
        - Mock `WorkflowContext` to capture `CallActivityAsync` call args. Initialize `RetryPolicyBuilder` with an override for `GenerateEmbeddingActivity`. Run the workflow (mocked activities). Assert the captured `WorkflowTaskOptions` for the embedding call reflects the override; the `ExtractContentActivity` call reflects the default.

- [x] Task 3: `CaseIngestionCounterActor` + transition wiring (AC: #5, #6)
    - [x] 3.1 Create `src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs`:
        ```csharp
        public sealed record CaseIngestionCounts(int Queued, int Extracting, int Embedding, int Indexing);
        ```
        Register in `MemoriesJsonContext`.
    - [x] 3.2 Create `src/Hexalith.Memories.Contracts/V1/CounterTransitionInput.cs`:
        ```csharp
        public sealed record CounterTransitionInput(
            string TenantId,
            string CaseId,
            string PreviousStage,   // "none" | "queued" | "extracting" | "embedding" | "indexing"
            string NextStage,       // same domain
            string TransitionId);   // "{instanceId}:{sequence}" for actor idempotency
        ```
        Register in `MemoriesJsonContext`.
    - [x] 3.3 Create `src/Hexalith.Memories.Server/Actors/ICaseIngestionCounterActor.cs`:
        ```csharp
        public interface ICaseIngestionCounterActor : IActor
        {
            Task TransitionAsync(string previousStage, string nextStage, string transitionId);
            Task<CaseIngestionCounts> GetCountsAsync();
            Task ResetAsync();
        }
        ```
    - [x] 3.4 Create `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterState.cs`:
        ```csharp
        internal sealed record CaseIngestionCounterState(
            int Queued,
            int Extracting,
            int Embedding,
            int Indexing,
            string? LastTransitionId);
        ```
        (LastTransitionId is the most-recent applied transitionId — used for idempotent replay handling.)
    - [x] 3.5 Create `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterLogic.cs` — pure helper mirroring `RateLimiterLogic` pattern (6.2 Task 1.2):

        ```csharp
        internal sealed class CaseIngestionCounterLogic
        {
            public CaseIngestionCounterState Transition(
                CaseIngestionCounterState state, string previous, string next, string transitionId)
            {
                if (string.Equals(state.LastTransitionId, transitionId, StringComparison.Ordinal))
                    return state; // idempotent — already applied
                int q = state.Queued, e = state.Extracting, m = state.Embedding, i = state.Indexing;
                switch (previous)
                {
                    case "queued": q = Math.Max(0, q - 1); break;
                    case "extracting": e = Math.Max(0, e - 1); break;
                    case "embedding": m = Math.Max(0, m - 1); break;
                    case "indexing": i = Math.Max(0, i - 1); break;
                    case "none": break;
                    default: throw new ArgumentException($"Invalid previousStage '{previous}'");
                }
                switch (next)
                {
                    case "queued": q++; break;
                    case "extracting": e++; break;
                    case "embedding": m++; break;
                    case "indexing": i++; break;
                    case "none": break;
                    default: throw new ArgumentException($"Invalid nextStage '{next}'");
                }
                return new CaseIngestionCounterState(q, e, m, i, transitionId);
            }

            public CaseIngestionCounts ToCounts(CaseIngestionCounterState s) =>
                new(s.Queued, s.Extracting, s.Embedding, s.Indexing);
        }
        ```

    - [x] 3.6 Create `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterActor.cs`:

        ```csharp
        internal sealed class CaseIngestionCounterActor(ActorHost host, CaseIngestionCounterLogic logic)
            : Actor(host), ICaseIngestionCounterActor
        {
            private const string StateName = "counterState";

            public async Task TransitionAsync(string previousStage, string nextStage, string transitionId)
            {
                CaseIngestionCounterState current = await GetOrCreateStateAsync();
                CaseIngestionCounterState next = logic.Transition(current, previousStage, nextStage, transitionId);
                if (!ReferenceEquals(next, current))
                    await StateManager.SetStateAsync(StateName, next);
            }

            public async Task<CaseIngestionCounts> GetCountsAsync() =>
                logic.ToCounts(await GetOrCreateStateAsync());

            public async Task ResetAsync() =>
                await StateManager.SetStateAsync(StateName,
                    new CaseIngestionCounterState(0, 0, 0, 0, null));

            private async Task<CaseIngestionCounterState> GetOrCreateStateAsync()
            {
                ConditionalValue<CaseIngestionCounterState> v =
                    await StateManager.TryGetStateAsync<CaseIngestionCounterState>(StateName);
                return v.HasValue ? v.Value : new CaseIngestionCounterState(0, 0, 0, 0, null);
            }
        }
        ```

        Mirror `EmbeddingRateLimiterActor` constructor pattern. Register `CaseIngestionCounterLogic` as singleton in DI.

    - [x] 3.7 Create `src/Hexalith.Memories.Server/Activities/Ingestion/UpdateCaseIngestionCounterActivity.cs`:
        ```csharp
        internal sealed class UpdateCaseIngestionCounterActivity(
            IActorProxyFactory actorFactory,
            ILogger<UpdateCaseIngestionCounterActivity> logger)
            : WorkflowActivity<CounterTransitionInput, bool>
        {
            public override async Task<bool> RunAsync(WorkflowActivityContext context, CounterTransitionInput input)
            {
                try
                {
                    ICaseIngestionCounterActor proxy = actorFactory.CreateActorProxy<ICaseIngestionCounterActor>(
                        new ActorId($"{input.TenantId}:{input.CaseId}"),
                        nameof(CaseIngestionCounterActor));
                    await proxy.TransitionAsync(input.PreviousStage, input.NextStage, input.TransitionId);
                    return true;
                }
                catch (Exception ex)
                {
                    RetryFailureLog.LogCounterTransitionFailed(logger, input.TenantId, input.CaseId,
                        input.PreviousStage, input.NextStage, ex.Message);
                    return false; // best-effort — counter drift documented
                }
            }
        }
        ```
    - [x] 3.8 Modify `IngestionWorkflow.RunAsync`:
        - Introduce `int counterSeq = 0;` local; define a local helper:
            ```csharp
            Task UpdateCounter(string prev, string next) =>
                context.CallActivityAsync<bool>(
                    nameof(UpdateCaseIngestionCounterActivity),
                    new CounterTransitionInput(input.TenantId, input.CaseId, prev, next,
                        $"{context.InstanceId}:{Interlocked.Increment(ref counterSeq)}"),
                    compensationRetry);
            ```
            **Note:** `Interlocked.Increment` is safe inside a workflow body because the workflow is single-threaded per instance; it's used here as a convenient monotonic counter, and the sequence is deterministic across replays because the workflow re-executes from the top.
        - After `LogCurrentStatus` (line 39): `await UpdateCounter("none", "queued"); context.SetCustomStatus("queued");`
        - On duplicate short-circuit (before return at line 70): `await UpdateCounter("queued", "none"); context.SetCustomStatus("duplicate");`
        - At each `TransitionStatus` site (lines 83, 123, 136, 292): `await UpdateCounter(previousLowercaseStage, nextLowercaseStage); context.SetCustomStatus(nextLowercaseStage);`
        - On happy-path terminal (before return at line 294): `await UpdateCounter("indexing", "none"); context.SetCustomStatus("indexed");`
        - **Ordering inside intermediate catches (line 208, 272) and outer catch (line 304) is PINNED:**
            ```csharp
            // 1. decrement counter for the bucket we were in
            try { await UpdateCounter(currentStageBucket, "none"); } catch { /* best-effort */ }
            // 2. set diagnostic custom status
            context.SetCustomStatus("failed");
            // 3. persist failed-unit record (best-effort — event 6309 on failure)
            await TryPersistFailedUnit(context, input, memoryUnitId, currentStage, ex, compensationRetry, logger);
            // 4. re-throw the original exception last
            throw;
            ```
            Any instruction that reads "call X after throw" is a bug — remove it. This reconciles previously contradictory instructions from earlier drafts.
    - [x] 3.9 Register activity + actor + logic in `Program.cs` builder:
        ```csharp
        options.RegisterActivity<UpdateCaseIngestionCounterActivity>();
        options.RegisterActor<CaseIngestionCounterActor>();
        // In services:
        builder.Services.AddSingleton<CaseIngestionCounterLogic>();
        ```
    - [x] 3.10 Tests:
        - `tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterLogicTests.cs` — `[Theory]` over transition sequences, idempotency (same transitionId twice = single application), zero-clamping (decrement from 0 stays at 0), unknown stage → throws.
        - `tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterActorTests.cs` — mocked `IActorStateManager`, assert `SetStateAsync` called once for a new transition; asserts `SetStateAsync` NOT called when the transitionId has already been applied.
        - `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/UpdateCaseIngestionCounterActivityTests.cs` — mocked `IActorProxyFactory`, assert `TransitionAsync` invoked with the right Actor ID and arguments.
        - Extend `IngestionWorkflowTests` — capture activity calls, assert the expected sequence of `UpdateCaseIngestionCounterActivity` calls across: happy path, duplicate, and failure-at-each-stage. Assert every call carries a unique `transitionId`.

- [x] Task 4: Persist failed units (AC: #3, #4)
    - [x] 4.1 Create `src/Hexalith.Memories.Contracts/V1/FailedUnitInput.cs`:
        ```csharp
        public sealed record FailedUnitInput(
            string TenantId,
            string CaseId,
            string MemoryUnitId,
            string SourceUri,
            SourceType SourceType,
            string IngestedBy,
            string Stage,
            string ErrorCode,
            string? ErrorMessage,
            int RetryCount,
            DateTimeOffset? LastRetryAt,
            DateTimeOffset FailedAt);
        ```
        Register in `MemoriesJsonContext`.
    - [x] 4.2 Create `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs`. Uses `[FromKeyedServices("redis")] IConnectionMultiplexer redis`. Executes a Lua script atomically:
        ```
        -- KEYS[1] = hash key ({tenantId}:failed-unit:{memoryUnitId})
        -- KEYS[2] = sorted-set key ({tenantId}:case:{caseId}:failed-units)
        -- ARGV[1..] = field/value pairs for the hash
        -- ARGV[N-1] = score (failedAt unix-ms)
        -- ARGV[N]   = member (memoryUnitId)
        redis.call('HSET', KEYS[1], unpack(ARGV, 1, #ARGV - 2))
        redis.call('ZADD', KEYS[2], ARGV[#ARGV - 1], ARGV[#ARGV])
        return 1
        ```
        The script is a constant string constant; pass the fields list. `failureDetailsJson` field contains the serialized `FailureDetails`.
    - [x] 4.3 Modify `IngestionWorkflow.RunAsync` outer catch-all (line 301). **PINNED ORDERING** (reconciles with Task 3.8):

        ```csharp
        catch (Exception ex) when (ex is not OperationCanceledException && !HasFailureDetails(ex))
        {
            // 1. attach details onto the exception (also populates ErrorMessage/LastRetryAt per AC2)
            AttachFailureDetails(ex, memoryUnitId, currentStage, GetRetryCountForStage(currentStage),
                new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero), logger);
            // 2. decrement the counter for the stage bucket we were in (best-effort)
            try { await UpdateCounter(MapStageToBucket(currentStage), "none"); } catch { /* drift documented */ }
            // 3. diagnostic custom status for DaprWorkflowClient readers
            context.SetCustomStatus("failed");
            // 4. persist failed-unit record (best-effort; event 6309 on failure)
            await TryPersistFailedUnit(context, input, memoryUnitId, currentStage, ex, compensationRetry, logger);
            // 5. re-throw LAST — anything after is unreachable
            throw;
        }
        ```

        **Edge case (see Amelia review #1 / Dev Note):** if `CheckIdempotencyActivity` fails its retries on first run, the outer catch fires _before_ `UpdateCounter("none","queued")` was invoked. `MapStageToBucket("idempotency")` returns `"none"`, and `UpdateCounter("none","none")` is a no-op on the actor — the `Queued` bucket is not decremented incorrectly. Do NOT add a guard that skips the call; the actor's stage-pair logic is already idempotent for `("none","none")`.
        Add a new private static helper `TryPersistFailedUnit` and a `MapStageToBucket` helper:

        ```csharp
        private static async Task TryPersistFailedUnit(
            WorkflowContext context,
            IngestionInput input,
            string memoryUnitId,
            string stage,
            Exception failure,
            WorkflowTaskOptions retry,
            ILogger logger)
        {
            try
            {
                FailureDetails? details = failure.Data[nameof(FailureDetails)] as FailureDetails;
                FailedUnitInput failedInput = new(
                    input.TenantId, input.CaseId, memoryUnitId,
                    input.SourceUri, input.SourceType, input.IngestedBy,
                    stage,
                    details?.ErrorCode ?? failure.GetType().Name,
                    details?.ErrorMessage,
                    details?.RetryCount ?? 0,
                    details?.LastRetryAt,
                    new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero));
                await context.CallActivityAsync<bool>(nameof(PersistFailedUnitActivity), failedInput, retry);
            }
            catch (Exception persistEx)
            {
                RetryFailureLog.LogFailedUnitPersistenceFailed(logger, memoryUnitId, persistEx.Message);
                // Do not re-throw — the original failure is the story.
            }
        }
        ```

        Also apply the **same pinned ordering** in both intermediate catch blocks at `IngestionWorkflow.cs:182-209` and `IngestionWorkflow.cs:257-272`: `AttachFailureDetails → UpdateCounter(currentBucket,"none") → SetCustomStatus("failed") → TryPersistFailedUnit → throw`. The existing `RecordCaseActivityActivity(IngestionFailed)` call at line 187 remains where it is (before `AttachFailureDetails`) — it's the activity-stream event source per AC4 and precedes the failure-context attachment.

        Add a small `MapStageToBucket(string stage)` static helper in `IngestionWorkflow` that maps pipeline stage strings (`"queued"`, `"idempotency"`, `"validation"`, `"fetching"`, `"extracting"`, `"embedding"`, `"indexing"`, `"verifying"`, `"dedup"`) to counter-actor buckets (`"queued"`, `"extracting"`, `"embedding"`, `"indexing"`, or `"none"` for non-bucketed stages like `"idempotency"`/`"validation"`/`"verifying"`/`"dedup"`). Pre-bucket stages return `"none"` so the counter receives `("none","none")` and no-ops. Post-bucket stages (`"verifying"`/`"dedup"`) return `"indexing"` because that's the last bucket the workflow was in before the post-index steps ran.

    - [x] 4.4 Register activity in `Program.cs`.
    - [x] 4.5 Tests:
        - `PersistFailedUnitActivityTests.cs` — mock `IConnectionMultiplexer`, mock script execution, assert keys and argv match the expected Lua shape; assert `SourceType` serializes correctly.
        - Extend `IngestionWorkflowTests` — simulate failure at each stage (extraction, embedding, indexing), assert `PersistFailedUnitActivity` invoked with correct stage and error code.

- [x] Task 5: `FailedUnitsRegistry` service + list endpoint (AC: #7, #8)
    - [x] 5.1 Create `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs`:

        ```csharp
        public sealed class FailedUnitsRegistry(
            [FromKeyedServices("redis")] IConnectionMultiplexer redis,
            ILogger<FailedUnitsRegistry> logger)
        {
            public async Task<FailedUnitsPage> ListAsync(string tenantId, string caseId, int limit, int offset, CancellationToken ct)
            {
                int boundedLimit = Math.Clamp(limit, 1, 500);
                int boundedOffset = Math.Clamp(offset, 0, 100_000);
                IDatabase db = redis.GetDatabase();
                string zkey = $"{tenantId}:case:{caseId}:failed-units";
                long total = await db.SortedSetLengthAsync(zkey).ConfigureAwait(false);
                RedisValue[] ids = await db.SortedSetRangeByRankAsync(zkey, boundedOffset, boundedOffset + boundedLimit - 1, Order.Descending).ConfigureAwait(false);
                List<FailedUnitSummary> units = new(ids.Length);
                foreach (RedisValue id in ids)
                {
                    FailedUnitSummary? summary = await ReadSummaryAsync(db, tenantId, id.ToString()).ConfigureAwait(false);
                    if (summary is not null) units.Add(summary);
                }
                return new FailedUnitsPage(units, (int)total, boundedLimit, boundedOffset);
            }

            public async Task<FailedUnitSummary?> GetAsync(string tenantId, string memoryUnitId, CancellationToken ct) { /* HashGetAll + parse */ }

            public async Task<bool> RemoveAsync(string tenantId, string caseId, string memoryUnitId, string sourceUri, CancellationToken ct) { /* atomic Lua: DEL hash + ZREM + DEL dedup key; return whether hash existed */ }

            private static async Task<FailedUnitSummary?> ReadSummaryAsync(IDatabase db, string tenantId, string memoryUnitId) { /* ... */ }
        }
        ```

        Register as singleton in `Program.cs`.

    - [x] 5.2 Add endpoint `GET /api/tenants/{tenantId}/cases/{caseId}/failed-units` in `Program.cs`:
        ```csharp
        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/failed-units", async (
            string tenantId, string caseId, int? limit, int? offset,
            CaseService caseService, FailedUnitsRegistry registry, CancellationToken ct) =>
        {
            try { TenantIdGuard.Validate(tenantId); }
            catch (ArgumentException) { return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", ...)); }
            Case? c = await caseService.GetCaseAsync(tenantId, caseId, ct);
            if (c is null) return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", ...));
            FailedUnitsPage page = await registry.ListAsync(tenantId, caseId, limit ?? 50, offset ?? 0, ct);
            return Results.Ok(page);
        });
        ```
    - [x] 5.3 Add endpoint `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` — read from `{tenantId}:mu:{memoryUnitId}` first; if not found AND `{tenantId}:failed-unit:{memoryUnitId}` exists, synthesize a `MemoryUnit` with `Status=Failed` from the failed-unit hash; else 404. Tenant-mismatch guard per the `GetMemoryUnitAsync` pattern at `CaseService.cs:251-255`. Verify `caseId` in the path matches the stored `caseId`.
    - [x] 5.4 Update `ParseMemoryUnitFromHash` (`CaseService.cs:943`) to read the `failureDetailsJson` field when present and populate `FailureDetails`. This enables the indexed path to carry failure context post-retry (it won't for successful indexings, but harmless for future extensions).
    - [x] 5.5 Update `MemoriesJsonContext` with new types (`FailedUnitSummary`, `FailedUnitsPage`, `FailedUnitInput`, `ActiveIngestionInput`, `ReIngestRequest`, `BulkReIngestionResponse`, `ReIngestedUnitInfo`).
    - [x] 5.6 Tests:
        - `FailedUnitsRegistryTests.cs` — with `TestContainers` Redis or mocked `IConnectionMultiplexer`, cover `ListAsync` pagination, `GetAsync` round-trip, `RemoveAsync` atomicity.
        - Endpoint tests in `tests/Hexalith.Memories.Server.Tests/Endpoints/FailedUnitsEndpointsTests.cs` (if a pattern exists — inspect 6.1/5.5 endpoint tests for the idiom) OR `[Fact(Skip)]` integration tests mirroring 6.2's convention.

- [x] Task 6: Re-ingestion endpoints (AC: #9, #10)
    - [x] 6.0 Create internal `src/Hexalith.Memories.Server/Ingestion/FailedUnitRecord.cs` (**internal**, server-only — not in Contracts):
        ```csharp
        internal sealed record FailedUnitRecord(
            string TenantId,
            string CaseId,
            string MemoryUnitId,
            string SourceUri,
            SourceType SourceType,
            string IngestedBy,
            string? ContentType,   // persisted by PersistFailedUnitActivity when known; else null
            string Stage,
            string ErrorCode,
            string? ErrorMessage,
            int RetryCount,
            DateTimeOffset? LastRetryAt,
            DateTimeOffset FailedAt);
        ```
        Holds all fields needed to rebuild `IngestionInput`. `FailedUnitSummary` (public contract) projects from this for the list endpoint — do NOT expose `IngestedBy` / internal fields that aren't needed by CLI consumers (Phase 2 may expose, but not MVP).
    - [x] 6.1 Add endpoint `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest` in `Program.cs`:
        ```csharp
        app.MapPost("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest", async (
            string tenantId, string caseId, string memoryUnitId,
            FailedUnitsRegistry registry, DaprWorkflowClient workflowClient,
            TenantStatusGuard tenantGuard, CancellationToken ct) =>
        {
            try { TenantIdGuard.Validate(tenantId); } catch (ArgumentException) { return Results.BadRequest(...); }
            ErrorResponse? statusErr = await tenantGuard.ValidateTenantActiveAsync(tenantId, ct);
            if (statusErr is not null) return TenantStatusGuard.ToHttpResult(statusErr);
            FailedUnitRecord? record = await registry.GetAsync(tenantId, memoryUnitId, ct);
            if (record is null) return Results.NotFound(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", ...));
            if (!string.Equals(record.CaseId, caseId, StringComparison.Ordinal))
                return Results.BadRequest(new ErrorResponse("CASE_MISMATCH", ...));
            bool claimed = await registry.RemoveAsync(tenantId, caseId, memoryUnitId, record.SourceUri, ct);
            if (!claimed) return Results.Conflict(new ErrorResponse("RE_INGESTION_IN_PROGRESS",
                "Another re-ingestion is in progress for this unit.",
                "Wait for the current re-ingestion to complete or check status."));
            IngestionInput rebuilt = new()
            {
                TenantId = tenantId, CaseId = caseId,
                SourceUri = record.SourceUri, SourceType = record.SourceType,
                IngestedBy = record.IngestedBy,
                // ContentType semantics:
                // - For SourceType.Url: always empty string (FetchUrlActivity will re-read from the URL response headers,
                //   overwriting whatever is here — see IngestionWorkflow.cs:101-104).
                // - For SourceType.File or any non-URL type: use the persisted ContentType from the failed-unit hash;
                //   fall back to empty string if null (ExtractContentActivity's Kreuzberg client auto-detects).
                ContentType = record.SourceType == SourceType.Url
                    ? string.Empty
                    : record.ContentType ?? string.Empty,
                ContentBytes = null, // re-fetch / re-read from SourceUri
                Metadata = new()
            };
            string instanceId = await workflowClient.ScheduleNewWorkflowAsync(
                nameof(IngestionWorkflow),
                instanceId: memoryUnitId, // preserves original ID via workflow's context.InstanceId fallback
                input: rebuilt);
            RetryFailureLog.LogReIngestionScheduled(logger, tenantId, caseId, memoryUnitId, instanceId);
            return Results.Accepted($"/api/ingest/{instanceId}", new { newWorkflowInstanceId = instanceId, memoryUnitId });
        });
        ```
        **Verify the `DaprWorkflowClient.ScheduleNewWorkflowAsync` signature in SDK 1.17.6** — the exact parameter name for the instance id may be `instanceId` or a positional string; the existing call at `Program.cs:207` uses the auto-generated form. Check via `Read` before coding; if the SDK doesn't accept a caller-specified instance id, fall back to passing `PreferredMemoryUnitId` via a new internal workflow input wrapper (Breaking Changes #2 ReIngestionInput path) — but the instance-id approach is preferred because the workflow's existing `context.InstanceId`-based memory-unit-id fallback picks it up with zero workflow code changes.
    - [x] 6.2 Add endpoint `POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest` with body `ReIngestRequest`. Handle both shapes (`All=true` → list from registry; `MemoryUnitIds != null` → process each). Each unit goes through the same claim-and-schedule flow wrapped in per-unit try/catch:
        ```csharp
        foreach (string id in targets)
        {
            try
            {
                FailedUnitRecord? r = await registry.GetAsync(tenantId, id, ct);
                if (r is null) { outcomes.Add(new ReIngestedUnitInfo(id, null, "not-found", null)); notFound++; continue; }
                if (!string.Equals(r.CaseId, caseId, StringComparison.Ordinal))
                { outcomes.Add(new ReIngestedUnitInfo(id, null, "not-found", "case mismatch")); notFound++; continue; }
                bool claimed = await registry.RemoveAsync(tenantId, caseId, id, r.SourceUri, ct);
                if (!claimed) { outcomes.Add(new ReIngestedUnitInfo(id, null, "conflict", null)); conflicted++; continue; }
                IngestionInput rebuilt = /* same as 6.1 */;
                string instanceId = await workflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId: id, input: rebuilt);
                outcomes.Add(new ReIngestedUnitInfo(id, instanceId, "scheduled", null));
                scheduled++;
            }
            catch (Exception ex)
            {
                RetryFailureLog.LogBulkReIngestionUnitSkipped(logger, tenantId, id, "error");
                outcomes.Add(new ReIngestedUnitInfo(id, null, "error", ex.Message));
                errored++;
            }
        }
        ```
        Return `Results.Ok(new BulkReIngestionResponse(scheduled, notFound, conflicted, errored, outcomes))`.
    - [x] 6.3 Implement `RemoveAsync` Lua script in `FailedUnitsRegistry`:
        ```lua
        -- KEYS[1] = hash key
        -- KEYS[2] = sorted-set key
        -- KEYS[3] = dedup key
        -- ARGV[1] = memoryUnitId (sorted-set member)
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return 0
        end
        redis.call('DEL', KEYS[1])
        redis.call('ZREM', KEYS[2], ARGV[1])
        redis.call('DEL', KEYS[3])
        return 1
        ```
        Compute the dedup key identically to `DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri)`.
    - [x] 6.4 Tests:
        - Integration (`[Fact(Skip)]` behind Aspire fixture — same precedent as 6.2): single-unit re-ingestion happy path, 404, 409.
        - Integration (`[Fact(Skip)]`): bulk re-ingestion with mixed outcomes.
        - Unit: `FailedUnitsRegistry.RemoveAsync` with mocked `IConnectionMultiplexer` asserts Lua content via captured script + KEYS / ARGV.
        - Unit: endpoint handler with mocked registry + `DaprWorkflowClient` covering scheduling success, 404, 409, case-mismatch.

- [x] Task 7: Wire counter actor into `CaseStatusDetail` (AC: #6)
    - [x] 7.1 Extend `CaseStatusDetail.cs` per Breaking Changes #6 — positional `QueuedCount`, `ExtractingCount`, `EmbeddingCount`, `IndexingCount` with default 0 (no `IsApproximate`).
    - [x] 7.2 Modify `CaseService.GetCaseStatusAsync` (`Cases/CaseService.cs:393-441`):
        - Inject `IActorProxyFactory` via constructor (follow existing pattern for `IActorProxyFactory` usage in the server — check Program.cs DI for precedent; `EmbeddingRateLimiterActor` is accessed via this factory from `GenerateEmbeddingActivity`).
        - Inside `GetCaseStatusAsync`, obtain the counter actor proxy:
            ```csharp
            ICaseIngestionCounterActor counter = _actorProxyFactory.CreateActorProxy<ICaseIngestionCounterActor>(
                new ActorId($"{tenantId}:{caseId}"), nameof(CaseIngestionCounterActor));
            Task<CaseIngestionCounts> countsTask = counter.GetCountsAsync();
            // existing await Task.WhenAll(lastActivityTask, failedCountTask, memberCountTask, countsTask) ...
            ```
        - Merge `countsTask.Result` into the returned `CaseStatusDetail`.
        - Do NOT remove the existing `IndexedCount` / `FailedCount` sources — they continue to flow from FalkorDB and the activity stream respectively.
        - Handle actor-unreachable case: wrap the actor read in try/catch; on failure, return zero counts (MVP degradation) and log at Warning. Never fail the whole status endpoint because of counter-actor drift.
    - [x] 7.3 Add endpoint-level test for `GET /cases/{caseId}/status` asserting the four new count fields appear in the JSON response and reflect the actor's state.
    - [x] 7.4 Regression test for existing `GetCaseStatusAsync` — ensure legacy fields (`IndexedCount`, `FailedCount`, etc.) unchanged.

- [x] Task 8: Structured log events 6301–6309 (AC: #3, #7, #9, #10)
    - [x] 8.1 Create `src/Hexalith.Memories.Server/Ingestion/RetryFailureLog.cs`, mirroring `RateLimitingLog.cs`. Events:
        - 6301 `RetryAttemptStarted` (Debug) — `activityName`, `memoryUnitId`, `attempt`. Emitted by activities on retry (no hook today; best-effort; may be skipped for MVP if no easy retry-count signal — document as "available when DAPR exposes retry attempt via context").
        - 6302 `RetryExhausted` (Warning) — `activityName`, `memoryUnitId`, `finalErrorCode`. Emitted by `AttachFailureDetails`.
        - 6303 `FailedUnitPersisted` (Information) — `tenantId`, `memoryUnitId`, `stage`, `errorCode`. Emitted by `PersistFailedUnitActivity` after success.
        - 6304 `ReIngestionScheduled` (Information) — `tenantId`, `caseId`, `memoryUnitId`, `newWorkflowInstanceId`.
        - 6305 `BulkReIngestionUnitSkipped` (Warning) — `tenantId`, `memoryUnitId`, `reason` (not-found | conflict | error).
        - 6306 `FailedUnitsListQueried` (Debug) — `tenantId`, `caseId`, `limit`, `offset`, `returnedCount`, `totalCount`.
        - 6307 `CounterActorTransitionApplied` (Debug) — `tenantId`, `caseId`, `previousStage`, `nextStage`, `transitionId`.
        - 6308 `CounterActorTransitionIdempotent` (Debug) — `tenantId`, `caseId`, `transitionId` (emitted when a replay-duplicate transitionId is observed; no state change).
        - 6309 `FailedUnitPersistenceFailed` (Error) — `memoryUnitId`, `reason`.
        - 6310 `CounterTransitionFailed` (Warning) — `tenantId`, `caseId`, `previousStage`, `nextStage`, `reason`.
    - [x] 8.2 Unit tests in `RetryFailureLogTests.cs` using `CapturingLogger<T>` (6.2 precedent). Assert EventId + LogLevel per event.

- [x] Task 9: Documentation + operator guidance (AC: supporting)
    - [x] 9.1 Extend `docs/operations/rate-limiting.md` (created in 6.2) or add a sibling `docs/operations/failure-recovery.md` describing:
        - Failed-units registry semantics (key shapes, retention policy).
        - Re-ingestion contract (memory-unit-id preservation via DAPR workflow `instanceId`, dedup-key cleanup, idempotency guarantees).
        - Per-activity retry configuration (activity names, defaults, overrides, `RETRY_CONFIG_INVALID` on bad config).
        - `CaseIngestionCounterActor` semantics (O(1) reads, idempotent transitions, zero-clamping).
        - **`FailedCount` (monotonic event-stream count) vs. `FailedUnitsPage.TotalCount` (currently-unresolved count) divergence — explicit operator guidance per Amelia review #3.**
        - Example `appsettings.json` snippets.
    - [x] 9.2 Link from `README.md` under the existing Operations section (added by 6.2).

- [x] Task 10: Regression guard + baseline (AC: #12)
    - [x] 10.1 Baseline: run `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` before any 6.3 code changes. Record counts in Debug Log References (expected: 1250 passing, 0 failures per 6.2 Dev Agent Record).
    - [x] 10.2 Post-change: same command. Expected: ≥ 1280 passing, zero new failures. Any regression → STOP and investigate.
    - [x] 10.3 Rebuild `Hexalith.Memories.AppHost` (pre-existing CS0311 errors unrelated to 6.3 — same baseline as 6.2); do NOT attempt to fix them in 6.3.

- [x] Task 11: Integration test scaffolding (AC: #3, #6, #7, #9, #10)
    - [x] 11.1 Create `tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs`. **All `[Fact]` markers MUST use the greppable skip sentinel format**:
        ```csharp
        [Fact(Skip = "Unskipped by Story 6.4 (pipeline state persistence) or Epic 7 e2e harness — requires Aspire fixture + deterministic 500-producing provider test double.")]
        ```
        This mirrors Story 6.2's convention (`Unskipped by Story 6.3 — ...`) so tracking via `grep "Unskipped by Story 6.4"` is trivial.
        Tests:
        - E2E: ingest a URL returning 500, verify workflow exhausts retries, verify failed-unit hash exists, verify sorted-set entry exists, verify `GET /failed-units` returns it, verify `GET /memory-units/{id}` returns a MU with `Status=Failed` and populated `FailureDetails`, verify counter actor `Queued`/`Extracting`/... buckets decrement correctly on failure.
        - E2E: re-ingest the failed unit via POST, verify new workflow scheduled with preserved memory-unit-id (via `instanceId` parameter), verify failed-unit hash is gone and dedup key is gone, verify annotations / graph edges survive.
        - E2E: bulk re-ingest 5 failed units, assert response enumerates 5 outcomes with expected shapes; simulate one Redis-hiccup during claim to produce `outcome="error"` for exactly one unit.
        - E2E: actor-backed status counts correctly reflect concurrent workflows at various stages (3 in `Embedding`, 2 in `Extracting`, 1 in `Queued` → `GetCountsAsync` returns `(1,2,3,0)`).

- [x] Task 12: Unit coverage for re-ingestion-after-failure and bulk-error outcomes (Murat #1, #2, #3)
    - [x] 12.1 Add `IngestionWorkflow_IndexingFailure_PersistsFailedUnitAndRecordsStreamEvent` in `IngestionWorkflowTests`: simulate an indexing failure, assert that **both** `PersistFailedUnitActivity` AND `RecordCaseActivityActivity(IngestionFailed)` are invoked on the same failure path (ordered: `RecordCaseActivityActivity` first at line 187, `PersistFailedUnitActivity` second via the pinned catch ordering). Pins the invariant that the failed-units registry and the activity stream are updated together.
    - [x] 12.2 Add `ReIngest_ThenNewWorkflowFails_OverwritesFailedUnitHash`: end-to-end unit-level test that (a) runs a workflow that fails at embedding, (b) asserts the failed-unit hash exists, (c) re-ingests via the endpoint (claim clears the hash, new workflow scheduled with `instanceId=memoryUnitId`), (d) runs a second workflow (same instance id) that also fails at embedding, (e) asserts the failed-unit hash is re-written with the **new** `FailedAt`, `RetryCount`, and `LastRetryAt`, and the sorted-set entry is present exactly once (no duplicate members). Confirms the re-ingestion round-trip is clean.
    - [x] 12.3 Add `BulkReIngest_MidBatchInfrastructureError_ReportsErrorOutcome`: script a `FailedUnitsRegistry` that throws on the 3rd call to `RemoveAsync`; invoke the bulk endpoint with 5 units; assert the response is 200 OK with `Scheduled=4, NotFound=0, Conflicted=0, Errored=1, Units[2].Outcome == "error"`, and `Units[2].ErrorMessage` is populated with the thrown exception message. Event 6305 fires once with `reason="error"`.

### Review Findings

- [x] [Review][Patch] Prevent failed-unit loss when re-ingestion scheduling fails after a successful claim [src/Hexalith.Memories.Server/Program.cs:1129]
- [x] [Review][Patch] Complete the missing retry-policy regression tests required by Story 6.3 [tests/Hexalith.Memories.Server.Tests/Ingestion/RetryPolicyBuilderTests.cs:14]
- [x] [Review][Patch] Add the missing status and re-ingestion regression tests promised by Story 6.3 [tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs:28]
- [x] [Review][Patch] Emit the documented 6308 idempotent-transition log event [src/Hexalith.Memories.Server/Ingestion/RetryFailureLog.cs:97]
- [x] [Review][Patch] Narrow the widened 6.3 API surface to the specified visibilities and mutability [src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs:58]
- [x] [Review][Patch] Link the new failure-recovery operations guide from the README operations section [README.md:41]
- [x] [Review][Patch] Isolate RetryPolicyBuilder tests from cross-class parallel state leakage [src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs:20]

## Dev Notes

### First Principles Framing

**What this story IS:** the "observability + recovery" layer for the ingestion pipeline. 6.1 shipped the surface (endpoints, URL/directory), 6.2 shipped the reliability (rate limits, jitter, gates), 6.3 ships the _aftermath_: when the reliability layer's retries are exhausted, what does the operator see, and how do they recover? The core insight is that DAPR Workflow retry exhaustion produces a terminal failed workflow — but **nothing is persisted about the failed memory unit unless we write it explicitly**. Activity-stream events (FR11 `IngestionFailed`) are diagnostic but ephemeral and not per-unit detail-rich. Story 6.3 introduces a **durable failed-units registry** as a first-class concept (Redis hash + sorted set per case) that survives restarts, enables listing, and is the source of truth for re-ingestion inputs.

**What this story IS NOT:**

- NOT a new workflow engine. Uses existing `Dapr.Workflow 1.17.6`.
- NOT exception-type-specific retry. Not supported by DAPR SDK 1.17.6; per-activity retry policies are the closest approximation.
- NOT a live retry-count inspector. `FailureDetails.RetryCount` is populated at terminal state; mid-flight inspection is via `DaprWorkflowClient.GetWorkflowStateAsync` (already available).
- NOT pipeline state persistence / cold-start replay validation (that's 6.4).
- NOT a dashboard or CLI. Structured logs + endpoints only.
- NOT automatic retention / TTL of failed units (Phase 2).

**Mental model for the dev agent:**

- AC1, AC11 = retry-policy parameterization via per-invocation snapshot. `RetryPolicyBuilder` + workflow-local helper.
- AC2 = `FailureDetails` expansion with 2 optional fields. Record change + workflow helper update.
- AC3, AC4 = persist-failed-unit activity + Lua script. Core new persistence plumbing.
- AC5 = `CaseIngestionCounterActor` + `UpdateCaseIngestionCounterActivity` + workflow transition wiring.
- AC6 = `CaseStatusDetail` populated via O(1) actor read; no fan-out, no `IsApproximate`.
- AC7, AC8 = list-failed + get-single-memory-unit endpoints + `FailedUnitsRegistry`.
- AC9, AC10 = re-ingestion endpoints (single + bulk) with atomic Lua claim, preserved memory-unit-id via DAPR `instanceId` parameter.
- AC12 = regression guard.

**If you find yourself writing a Polly retry pipeline, building a new actor for failure tracking, proposing a message queue to replace the sorted set, or extending `IngestionWorkflow` with per-exception catch-and-retry logic — STOP. You're over-scoping.**

### Dependencies

- **Story 1.4, 1.5 (indexing + Redis):** REQUIRED — provide the memory-unit hash storage and Redis connection. Status: done.
- **Story 1.6 (Ingestion Workflow):** REQUIRED — provides the workflow, `CheckIdempotencyActivity`, `SaveDedupKeyActivity`, `AttachFailureDetails`, `RecordCaseActivityActivity`. Status: done. **6.3 modifies `IngestionWorkflow` — coordinate with any pending 6.2 edits.**
- **Story 3.2 (Case status and activity):** REQUIRED — provides `GET /cases/{caseId}/status`, `CaseStatusDetail`, `CaseActivityService.GetFailedCountAsync`. Status: done. 6.3 extends `CaseStatusDetail` (backward-compat positional additions).
- **Story 5.4 (Tenant context enforcement):** REQUIRED — provides `TenantIdGuard`, `TenantStatusGuard`, tenant-mismatch detection. Status: done. New endpoints use these.
- **Story 6.1 (URL and directory ingestion):** REQUIRED — provides `IngestionSettings` and the `RateLimitingLog` pattern precedent. Status: review.
- **Story 6.2 (Per-tenant load management):** REQUIRED — provides `ActivityRetryPolicy`-friendly settings pattern, `TenantId` threaded through activities, the `[LoggerMessage]` convention, and `[Fact(Skip)]` idiom. Status: review.
- **`Microsoft.Extensions.TimeProvider.Testing`** — already in `Directory.Packages.props` (added by 6.2).

### Architecture Compliance

- **FR9 (Configurable retry per activity type):** Satisfied by `RetryPolicyBuilder.For(activityName)` reading from `IngestionSettings.RetryPolicies`.
- **FR10 (Ingestion status per case):** Satisfied by extended `CaseStatusDetail` with `QueuedCount`, `ExtractingCount`, `EmbeddingCount`, `IndexingCount` — maintained by `CaseIngestionCounterActor` (O(1) read, no approximation).
- **FR11 (Failed unit visibility):** Satisfied by `FailureDetails` expansion (ErrorMessage, LastRetryAt) + failed-units registry + list endpoint + get-single-memory-unit endpoint.
- **FR12 (Re-ingestion, idempotent):** Satisfied by `POST /memory-units/{id}/re-ingest` and `POST /failed-units/re-ingest`, both using atomic Lua to clear the dedup key. Memory-unit-id is preserved across re-ingestion via the DAPR Workflow `instanceId` parameter (no public-contract field).
- **NFR19 (Never silently dropped):** Satisfied by durable failed-units hash + sorted set written in the workflow's outer catch via `PersistFailedUnitActivity`.
- **Architecture §5 (Rate Limiting & Throttling):** 6.3 does not touch rate limiting; it reads retry policy uniformly per activity type.
- **Architecture §D3 (Eventual consistency + saga/compensation):** Preserved — the existing compensation pattern at `IngestionWorkflow.cs:353-408` is unchanged. 6.3 adds a parallel "persist-failed-unit" step that is best-effort and non-compensating.
- **Architecture §D23 (DAPR Workflow for orchestrations):** Preserved — 6.3 stays within the workflow + activities abstraction.
- **Architecture §D25 (Workflow-Actor separation):** Preserved — 6.3 uses activities (Redis I/O) and services (registry, status reader), not actors. The failed-units registry is stateless; state lives in Redis directly.
- **Architecture §3 (Data Model FailureDetails):** "Stage, error code, retry count — populated only in `failed` status" — 6.3 adds ErrorMessage and LastRetryAt. Consistent with the intent: `FailureDetails` is the failure-only diagnostic object.

### Existing Infrastructure to Reuse

| Component                                                              | Location                                                                                      | Usage in This Story                                                                                                                        |
| ---------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `IngestionWorkflow`                                                    | `Server/Workflows/IngestionWorkflow.cs`                                                       | Modify retry-option calls; add register/unregister; add persist-failed-unit in catches.                                                    |
| `IngestionInput`                                                       | `Contracts/V1/IngestionInput.cs`                                                              | **Unchanged** — re-ingestion uses DAPR workflow `instanceId` param.                                                                        |
| `CheckIdempotencyActivity` / `SaveDedupKeyActivity`                    | `Activities/Ingestion/`                                                                       | Re-used unchanged. Dedup key cleared by re-ingestion handler.                                                                              |
| `DedupKeyBuilder`                                                      | `Activities/Ingestion/DedupKeyBuilder.cs`                                                     | Re-used to compute dedup key for atomic cleanup.                                                                                           |
| `FailureDetails`                                                       | `Contracts/V1/FailureDetails.cs`                                                              | Extended with `ErrorMessage`, `LastRetryAt`.                                                                                               |
| `AttachFailureDetails`                                                 | `Workflows/IngestionWorkflow.cs`                                                              | Extended to populate new fields (truncation of long messages).                                                                             |
| `RecordCaseActivityActivity` + `CaseActivityEventType.IngestionFailed` | `Activities/Ingestion/RecordCaseActivityActivity.cs`, `Contracts/V1/CaseActivityEventType.cs` | Preserved — continues to feed `FailedCount`.                                                                                               |
| `CaseStatusDetail`                                                     | `Contracts/V1/CaseStatusDetail.cs`                                                            | Extended with 4 new count fields (no `IsApproximate`).                                                                                     |
| `CaseService.GetCaseStatusAsync`                                       | `Cases/CaseService.cs:393-441`                                                                | Extended to call `ICaseIngestionCounterActor.GetCountsAsync`.                                                                              |
| `EmbeddingRateLimiterActor`                                            | `Server/Actors/EmbeddingRateLimiterActor.cs`                                                  | Pattern precedent for `CaseIngestionCounterActor` (state persistence, pure-logic separation, idempotent operations).                       |
| `CaseService.GetMemoryUnitAsync`                                       | `Cases/CaseService.cs:235-259`                                                                | Referenced in new `GET /memory-units/{id}` endpoint; may need a failed-unit-fallback code path (or a new method on `FailedUnitsRegistry`). |
| `TenantStatusGuard`, `TenantIdGuard`                                   | (existing)                                                                                    | Reused for validation in new endpoints.                                                                                                    |
| `[LoggerMessage]` partial-class pattern                                | 6.1 `IngestionEndpointLog.cs`, 6.2 `RateLimitingLog.cs`                                       | Mirror for new `RetryFailureLog.cs`.                                                                                                       |
| `DaprWorkflowClient`                                                   | existing DI registration                                                                      | Reused for scheduling new workflows and querying workflow state.                                                                           |
| `CapturingLogger<T>`                                                   | tests/ (5.6 / 6.1 precedent)                                                                  | Assert `[LoggerMessage]` calls.                                                                                                            |
| `[Fact(Skip)]` integration-test convention                             | 6.2 precedent                                                                                 | Reuse; tracker reference to Story 6.4 or Epic 7.                                                                                           |

### Code Patterns

**Per-activity retry option resolution:**

```csharp
// In IngestionWorkflow.RunAsync, replace retryOptions variable uses with calls:
await context.CallActivityAsync<T>(
    nameof(GenerateEmbeddingActivity),
    input,
    RetryPolicyBuilder.For(nameof(GenerateEmbeddingActivity)));
```

**Counter-actor transitions:**

```csharp
// Inside RunAsync, local variables:
int counterSeq = 0;
Task UpdateCounter(string prev, string next) =>
    context.CallActivityAsync<bool>(
        nameof(UpdateCaseIngestionCounterActivity),
        new CounterTransitionInput(
            input.TenantId, input.CaseId, prev, next,
            $"{context.InstanceId}:{Interlocked.Increment(ref counterSeq)}"),
        compensationRetry);

// Start of workflow (after LogCurrentStatus):
await UpdateCounter("none", "queued");
context.SetCustomStatus("queued");

// At each TransitionStatus site:
await UpdateCounter("queued", "extracting");  // example for the first transition
context.SetCustomStatus("extracting");

// Happy-path terminal (just before return):
await UpdateCounter("indexing", "none");
context.SetCustomStatus("indexed");

// Duplicate short-circuit (before early return):
await UpdateCounter("queued", "none");
context.SetCustomStatus("duplicate");

// Failure path (inside any catch — PINNED order):
try { await UpdateCounter(MapStageToBucket(currentStage), "none"); } catch { /* drift documented */ }
context.SetCustomStatus("failed");
await TryPersistFailedUnit(...);
throw;
```

**Failed-unit persistence in catch-all:**

```csharp
catch (Exception ex) when (ex is not OperationCanceledException && !HasFailureDetails(ex))
{
    AttachFailureDetails(ex, memoryUnitId, currentStage, GetRetryCountForStage(currentStage),
        new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero), logger);
    await TryPersistFailedUnit(context, input, memoryUnitId, currentStage, ex, compensationRetry, logger);
    throw;
}
```

**Atomic dedup + failed-unit cleanup Lua (`FailedUnitsRegistry.RemoveAsync`):**

```lua
if redis.call('EXISTS', KEYS[1]) == 0 then return 0 end
redis.call('DEL', KEYS[1])          -- hash
redis.call('ZREM', KEYS[2], ARGV[1]) -- sorted-set member
redis.call('DEL', KEYS[3])          -- dedup key
return 1
```

Returns 1 (claimed) or 0 (already gone — caller returns 409).

**Re-ingestion input rebuild + instance-id preservation:**

```csharp
IngestionInput rebuilt = new()
{
    TenantId = tenantId,
    CaseId = caseId,
    SourceUri = record.SourceUri,
    SourceType = record.SourceType,
    IngestedBy = record.IngestedBy,
    ContentBytes = null, // re-fetch / re-read from SourceUri
    ContentType = record.SourceType == SourceType.Url ? string.Empty : record.ContentType ?? string.Empty,
    Metadata = new()
};
// Preserve the original memory-unit-id by using it as the workflow instance id.
// The workflow's existing IngestionWorkflow.cs:32-34 fallback picks up context.InstanceId.
string instanceId = await workflowClient.ScheduleNewWorkflowAsync(
    nameof(IngestionWorkflow),
    instanceId: record.MemoryUnitId,
    input: rebuilt);
// `instanceId` will equal `record.MemoryUnitId` on SDK versions that honor the parameter.
```

### Retry & Recovery Semantics

- **Per-activity retry policy overrides:** Set in `Ingestion:RetryPolicies:<ActivityClassName>`. Missing entries default to `(5, 2s, 1.5, 5min)` (same as pre-6.3).
- **Retry count in `FailureDetails`:** Set by `GetRetryCountForStage` — `0` for validation failures, `_mainRetryAttempts` (now default `MaxAttempts` from the per-activity config — **still report the default 5** for simplicity; sharpening this to reflect the _actual_ per-activity override is a Phase 2 nicety).
- **Jitter:** Inherited from 6.2. `GenerateEmbeddingActivity` applies jitter; others do not.
- **Re-ingestion preserves memory-unit-id:** via the DAPR workflow `instanceId` parameter (no public-contract change); annotations and graph edges survive.
- **Dedup-key cleanup on re-ingestion:** Atomic Lua prevents the idempotency short-circuit.
- **Dedup-key cleanup on DELETE memory-unit:** **Deliberately NOT done** in 6.3 — the existing `DELETE /memory-units/{id}` does not clear the dedup key, so a re-ingestion of a deleted unit (via POST /api/ingest with the same source URI) hits the duplicate short-circuit and returns the old memory unit ID. If operators want re-ingestion of a deleted unit, they must use the failed-units re-ingest endpoint (which DOES clear the key). Document explicitly in Known MVP Limitations — the asymmetry is intentional to avoid duplicate-detection-bypass abuse. Phase 2 may add a dedup-key delete on memory-unit delete.

### `FailedCount` vs. `TotalCount` — Semantic Divergence (Amelia #3)

Two distinct metrics on failure, deliberately divergent:

- **`CaseStatusDetail.FailedCount`** — sourced from `CaseActivityService.GetFailedCountAsync`, which counts `CaseActivityEventType.IngestionFailed` events in the Redis Stream `{tenantId}:case:{caseId}:activity`. This is a **historical cumulative count** — every ingestion-failure event ever recorded for the case, including failures that have since been re-ingested successfully or deleted. Monotonically increasing within a case (until case delete). Feeds dashboards that track "how many failures happened in this case's history."
- **`FailedUnitsPage.TotalCount`** — sourced from `ZCARD` on `{tenantId}:case:{caseId}:failed-units`. This is a **currently-unresolved count** — only failed units whose records still exist. Decreases on re-ingestion (claim removes the hash + sorted-set entry) or explicit delete. Feeds operator-action UX ("how many failures need my attention right now").

**The divergence is intentional.** An operator who re-ingests all 50 failed units successfully will see `TotalCount=0` in the list but `FailedCount=50` on the status endpoint — reflecting that there WERE 50 failures historically but ALL are now resolved. Operator docs (`docs/operations/failure-recovery.md`) MUST make this explicit so operators do not mistake the divergence for a bug.

### Counter Actor — Design Rationale

**Why an actor per (tenantId, caseId)?**

- Architecture §D24 canonicalizes DAPR Actors for per-tenant stateful singletons (`EmbeddingRateLimiterActor` is the precedent). A per-case counter is a natural specialization of that pattern.
- Single-threaded actor execution (DAPR guarantee) eliminates race conditions on `--previous; ++next` without explicit locking.
- State size is tiny (4 × int + one string = ~30 bytes persisted); Redis actor state store absorbs this trivially.
- O(1) read/write vs. the rejected alternatives (sorted-set fan-out, stream aggregation).

**Why idempotency via transitionId?**

- DAPR Workflow replay executes `RunAsync` from the top; any activity call made during original execution is re-made during replay. Without idempotency, `TransitionAsync("queued","extracting")` replayed after a mid-execution crash would double-count.
- The `transitionId` is deterministically generated from `$"{instanceId}:{sequence}"` inside the workflow, where `sequence` is `Interlocked.Increment(ref counterSeq)` over a local variable. Replay rebuilds the sequence identically (same instanceId, same call order), so duplicates are detected exactly.
- The actor stores only the **last** transitionId. That's sufficient because (a) DAPR workflow replay is strictly sequential per instance, (b) the workflow's activities for the counter are all called via `await` and thus serialized. A replay that re-invokes an already-applied transitionId is the no-op fast path; a novel transitionId is applied and becomes the new `LastTransitionId`.

**Why not actor-per-tenant (instead of per-case)?**

- A tenant with 1000 cases would serialize ALL counter updates through one actor — a concurrency bottleneck. Per-case actors parallelize naturally.

**Why not store in the existing `EmbeddingRateLimiterActor`?**

- Different concerns, different lifetime. Rate limit is per-tenant, counter is per-case. Mixing would couple orthogonal state.

### Failed-Unit Registry — Design Rationale

**Why Redis hash + sorted set (not an actor, not FalkorDB)?**

- Failed units are per-memory-unit records with stable keys; a hash is the natural shape.
- List-by-recency + pagination is the common read pattern; a sorted set scored by failure timestamp gives O(log N) range reads.
- FalkorDB is for graph queries; a tenant-global sorted set is not a graph concept.
- A DAPR actor would add state-persistence overhead + serialization latency on every read; direct Redis is faster and aligns with the existing MU hash pattern.
- The hash key scope is `{tenantId}:failed-unit:{memoryUnitId}` (tenant-scoped, flat key space); the sorted set is `{tenantId}:case:{caseId}:failed-units` (case-scoped). Two keys per unit is cheap.

**Why not store in the same `{tenantId}:mu:{memoryUnitId}` hash used by indexed units?**

- The indexed MU hash is populated by the three indexing activities AFTER embedding. Pre-indexing failures never write to that hash. Adding a failed-path write would couple failure persistence to the indexing pipeline — cleaner to keep a separate key-space.
- `GET /memory-units/{id}` reads from the indexed hash first, falls back to the failed-unit hash — a clear precedence.

**Why not dual-write (both indexed-MU hash AND failed-unit hash) for consistent lookups?**

- Pre-indexing failures have no content/hash to write to the indexed-MU hash anyway — it would require defaulting fields.
- Separation keeps operational clarity: "if it's in `{tenantId}:mu:*`, it's indexed; if it's in `{tenantId}:failed-unit:*`, it failed."

**No `IsApproximate` on the public contract.** The earlier draft of this story used a 1000-cap fan-out over `DaprWorkflowClient.GetWorkflowStateAsync` and exposed `IsApproximate` when the cap was hit. Architectural review rejected this as an MVP implementation detail leaking into the public contract. The `CaseIngestionCounterActor` provides exact O(1) counts without approximation.

### Previous Story Learnings (from 5.6, 6.1, 6.2)

- **`[LoggerMessage]` partial-class pattern** — 6.1 `IngestionEndpointLog`, 6.2 `RateLimitingLog`, 6.3 `RetryFailureLog`.
- **Event IDs are pinned** — 5501, 5601–5603, 6101–6108, 6201–6206. 6.3 pins **6301–6309**.
- **`[Fact(Skip)]` integration convention** — 6.2 precedent; tracker reference to Story 6.4 or Epic 7.
- **Preserve positional record ordering when adding fields** — mandatory for backward compat on `CaseStatusDetail`.
- **`MemoriesJsonContext` AOT source-gen** — always add `[JsonSerializable]` for new contract types; full rebuild regenerates.
- **DAPR 1.17.6 workflow retry policy is exception-type-agnostic** — 6.3 inherits and does not fight it.
- **Best-effort activity pattern** — `RecordCaseActivityActivity` wrapped in try/catch; 6.3 mirrors for `UpdateCaseIngestionCounterActivity` and `PersistFailedUnitActivity` (for the outer catch's try/persist wrapper — do not let persistence failure mask the original exception).
- **Replay-safe now** — use `context.CurrentUtcDateTime` not `DateTimeOffset.UtcNow` inside workflow body.
- **Testability** — `WorkflowContext` is notoriously hard to mock; rely on integration tests for full workflow paths, unit tests for helpers (`RetryPolicyBuilder`, `FailedUnitsRegistry`).

### Git Intelligence

Recent commits:

- `a4f32f8` — "Add unit tests for ingestion activities and services" — resolved the pre-existing `SaveDedupKeyActivityTests` baseline failures. 6.3 builds on green baseline.
- `948b8a5` — 5.6 search endpoint degradation logging — sets the logging pattern 6.3 mirrors.
- `30f86c2` + `24f5ff7` — 5.5 tenant endpoint handlers + config/metrics.
- Uncommitted working tree (6.2) — expected to be committed before 6.3 starts.

### Anti-Patterns to Avoid

1. **Do NOT introduce a new retry library (Polly, Resilience4Net, etc.).** DAPR Workflow retry is the substrate.
2. **Do NOT use `DateTimeOffset.UtcNow` inside the workflow body.** Use `context.CurrentUtcDateTime`. Replay determinism.
3. **Do NOT store `ContentBytes` in the failed-unit hash.** Re-ingestion re-fetches/re-extracts from `SourceUri`.
4. **Do NOT auto-retry at the endpoint level.** Re-ingestion is an _explicit operator action_ (FR12). No auto-retry endpoint.
5. **Do NOT make `FailedUnitsRegistry` an actor.** State lives in Redis; the registry is a stateless service.
6. **Do NOT store failed units in FalkorDB.** It's a graph DB; failed units are not graph nodes.
7. **Do NOT delete failed units automatically.** Retention is an operator concern. Phase 2 may add a TTL configuration.
8. **Do NOT catch `OperationCanceledException`** in the workflow catch-all; it must propagate (cancellation is DAPR framework concern).
9. **Do NOT persist ingestion failures for `OperationCanceledException`** — the `when` clause in the catch excludes it.
10. **Do NOT swallow `PersistFailedUnitActivity` failure AND the original exception together.** The original exception is the story; log the persistence failure separately (event 6309) and rethrow the original.
11. **Do NOT introduce cumulative retry counts across re-ingestions.** `FailureDetails.RetryCount` is per-workflow-instance. Cumulative is Phase 2.
12. **Do NOT add a `PreferredMemoryUnitId` field to the public `IngestionInput` contract.** That was the earlier draft approach, rejected by architectural review as a capability leak. The DAPR workflow `instanceId` parameter on `ScheduleNewWorkflowAsync` is the internal mechanism for memory-unit-id preservation across re-ingestion; the public `POST /api/ingest` endpoint never sets it (the workflow auto-generates via `context.InstanceId`).
13. **Do NOT change `CreateCompensationRetry()`** — compensation retry is operationally tuned; don't open an operator knob for it in 6.3. Phase 2.
14. **Do NOT use `DaprWorkflowClient.GetWorkflowStateAsync(instanceId, getInputsAndOutputs: true)`** in the status reader — fetching inputs/outputs blows the payload up for every query. `false` suffices to read `CustomStatus`.
15. **Do NOT query DAPR workflow state from inside the workflow body.** External reads only (status-reader service).
16. **Do NOT wrap `HashSet<string>` activities inputs in dictionaries** just to pass multiple fields — use dedicated record types (e.g., `FailedUnitInput`, `ActiveIngestionInput`).
17. **Do NOT reset `RetryCount` to 0 on re-ingestion.** The new workflow instance starts with its own 0; the persisted failed-unit hash from the _previous_ failure is deleted. There is no cumulative tracking (documented in Anti-Pattern #11).

### Known MVP Limitations

- **No cumulative retry count across re-ingestions.** Each re-ingestion starts fresh; the failed-unit record from a previous attempt is deleted. Phase 2 may add cumulative history.
- **No TTL on failed units.** They accumulate indefinitely (by NFR19 design — never silently dropped). Operator must prune via explicit DELETE.
- **No DELETE endpoint for failed units in 6.3.** Operators can re-ingest (which deletes on success) or use Redis CLI. A `DELETE /failed-units/{id}` endpoint is Phase 2.
- **Counter actor drift on catastrophic failure.** If a DAPR actor state-write fails persistence mid-transition (microsecond window), a bucket may be off by one until a subsequent transition corrects it. Phase 2 adds a periodic reconciler.
- **`FailedCount` (activity-stream count) ≠ failed-units sorted-set cardinality** — can diverge after re-ingestion success (stream retains the IngestionFailed event; sorted-set removes the entry). Document.
- **Re-ingestion does not preserve previous `Metadata`.** The re-ingested MU receives fresh metadata from the new fetch. Phase 2 may offer a `preserveMetadata: true` flag.
- **Bulk re-ingestion has no rate limiting** beyond downstream ingestion rate limiter. Accept.
- **No per-tenant retry policy override.** Global only. Phase 2.
- **`RetryPolicyBuilder.Initialize` is not hot-reloadable.** Config changes require restart. Phase 2.
- **`ContentBytes` not persisted on failure.** Re-ingestion re-reads `SourceUri`. If the source is gone (file deleted, URL 404), re-ingestion fails identically. This is correct behavior — the system shouldn't hoard large blobs waiting for a re-ingestion that may never come.
- **`RetryAttemptStarted` event (6301) emission is best-effort.** DAPR SDK 1.17.6 does not reliably expose a retry-attempt counter to the activity context; this event may be skipped or emitted only from wrapped activities. Document.
- **Preserved memory-unit-id across re-ingestion is server-only.** Since `IngestionInput` is unchanged, callers cannot game the memory-unit-id from the public `POST /api/ingest` endpoint — the public endpoint does not set the workflow `instanceId`, so the workflow auto-generates an id. Only the re-ingestion endpoints pass an explicit `instanceId`. No capability leak.
- **`GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}`** endpoint returns 404 for in-flight (not-yet-indexed, not-yet-failed) units. In-flight inspection is via `GET /api/ingest/{instanceId}` or the case status endpoint. Document.
- **Dedup key for deleted (indexed) units is NOT cleared** on DELETE — see Retry & Recovery Semantics note on "asymmetry".

### Edge Cases

- **Workflow crash mid-counter-transition:** DAPR replays the workflow; the counter actor deduplicates by `transitionId` (same deterministic id from `$"{instanceId}:{sequence}"` on replay → no-op if already applied). Safe.
- **Workflow crash between `PersistFailedUnitActivity` and re-throw:** The failed-unit hash is written but the exception never propagates; the workflow instance is terminal on next replay. Safe.
- **`PersistFailedUnitActivity` succeeds but the outer catch's `throw` fails (platform issue):** Impossible — `throw` is synchronous. N/A.
- **Re-ingestion claims a unit that is already mid-claim:** Lua `EXISTS` check returns 0 on second caller → second caller gets 409 Conflict. Safe.
- **Re-ingestion with `MemoryUnitIds` containing a mix of valid and invalid IDs:** Each processed independently; outcomes enumerated in `BulkReIngestionResponse`. No batch-level failure.
- **Re-ingestion of a failed unit whose source URL now returns 200 (transient fix):** New workflow succeeds; memory unit lands in `Indexed` with the preserved ID. Graph edges and annotations are preserved.
- **Concurrent counter transitions for the same case:** DAPR actor serializes calls per actor id; no race. Each transition yields its own state version.
- **`GetCountsAsync` on a never-seen case:** actor's `GetOrCreateStateAsync` returns zero state; result `(0,0,0,0)`.
- **`GetCountsAsync` during actor failover:** DAPR auto-rehydrates the actor from state store on first call after failover; read cost is one additional state-store round-trip.
- **Case deleted while failed units exist:** Case-delete workflow (Story 3.5) should delete the failed-units sorted set, all `{tenantId}:failed-unit:{id}` hashes for units in that case, AND call `ICaseIngestionCounterActor.ResetAsync` for the `{tenantId}:{caseId}` actor id. If not, they become orphaned. **Coordination note:** verify 3.5's delete does NOT leave these orphans — if it does, either (a) extend 3.5's cleanup to sweep, or (b) document and defer to Phase 2. **Dev-agent action:** inspect the case-delete workflow; if it sweeps `{tenantId}:case:{caseId}:*`, the sorted set is safe but per-memory-unit hashes and the counter actor still need explicit cleanup; extend accordingly.
- **Memory unit deleted (via `DELETE /memory-units/{id}`) while ingestion workflow is mid-flight:** unlikely (the MU doesn't yet exist in the indexed hash); but if deletion targets a failed unit via some path, ensure the DELETE endpoint doesn't inadvertently target the failed-unit hash. **Current `DELETE` deletes from the indexed hash only, so safe; 6.3 does NOT change DELETE semantics.**
- **`MaxAttempts=1` in config:** Workflow calls the activity once; on first failure, immediately transitions to catch. `AttachFailureDetails.RetryCount` reports 1. Safe.
- **`MaxAttempts=0` in config:** `RetryPolicyBuilder.Initialize` fails fast with `RETRY_CONFIG_INVALID` — prevents silently broken retry semantics. Surface as startup failure.
- **5000 concurrent active workflows in a case:** counter actor returns exact counts regardless of concurrency — no 1000-cap approximation in the new design.
- **Re-ingestion with `All=true, Limit=500` and exactly 500 failed units:** All processed; `TotalCount==500`; response has 500 outcomes. Safe.
- **Re-ingestion with `All=true, Limit=500` and 600 failed units:** First 500 (most-recent) processed; oldest 100 remain; operator must call again to process the rest. Document.
- **Re-ingestion of a unit with `SourceType=File` where the file is gone:** `ExtractContentActivity` re-reads via the stored `SourceUri`; fails with NotFound; new workflow marks unit as Failed again. Same `memoryUnitId` (preserved via the DAPR workflow `instanceId` parameter); operator sees retry count reset to the new instance's retry count.
- **DAPR SDK 1.17.6 does not accept caller-specified `instanceId`:** fall back to the internal `ReIngestionInput` wrapper (Breaking Changes #2) — thread the preferred id through an internal workflow input wrapper. This path is exercised only if the pre-implementation SDK check (Task pre-5) flags the signature gap.
- **Re-ingestion across a different `caseId` than the failed unit's original:** `FailedUnitSummary.CaseId` read, compared to URL path's `caseId`; mismatch → 400 BadRequest. Safe.
- **`FailureDetails` legacy row** (pre-6.3 failed unit hash, if any — unlikely since 6.3 creates the hash for the first time): The registry reads whatever is present; missing `errorMessage` or `lastRetryAt` → null. Safe.

### Reference: Log Events

| Event ID | Level       | Name                          | Fields                                                                             |
| -------- | ----------- | ----------------------------- | ---------------------------------------------------------------------------------- |
| 6301     | Debug       | `RetryAttemptStarted`         | `activityName`, `memoryUnitId`, `attempt` (best-effort; see Known MVP Limitations) |
| 6302     | Warning     | `RetryExhausted`              | `activityName`, `memoryUnitId`, `finalErrorCode`                                   |
| 6303     | Information | `FailedUnitPersisted`         | `tenantId`, `memoryUnitId`, `stage`, `errorCode`                                   |
| 6304     | Information | `ReIngestionScheduled`        | `tenantId`, `caseId`, `memoryUnitId`, `newWorkflowInstanceId`                      |
| 6305     | Warning     | `BulkReIngestionUnitSkipped`  | `tenantId`, `memoryUnitId`, `reason`                                               |
| 6306     | Debug       | `FailedUnitsListQueried`      | `tenantId`, `caseId`, `limit`, `offset`, `returnedCount`, `totalCount`             |
| 6307     | Debug       | `ActiveIngestionRegistered`   | `tenantId`, `caseId`, `memoryUnitId`                                               |
| 6308     | Debug       | `ActiveIngestionUnregistered` | `tenantId`, `caseId`, `memoryUnitId`                                               |
| 6309     | Error       | `FailedUnitPersistenceFailed` | `memoryUnitId`, `reason`                                                           |
| 6310     | Warning     | `CounterTransitionFailed`     | `tenantId`, `caseId`, `previousStage`, `nextStage`, `reason`                       |

### Error Codes

- `MEMORY_UNIT_NOT_FOUND` (existing) — reused for `GET /memory-units/{id}` and `/re-ingest` 404.
- `CASE_NOT_FOUND` (existing) — reused for `GET /failed-units` 404.
- `CASE_MISMATCH` (new) — when the URL-path `caseId` does not match the failed-unit's stored `caseId`. Message: "Memory unit belongs to a different case."
- `RE_INGESTION_IN_PROGRESS` (new) — 409 when the failed-unit hash has already been claimed by another re-ingestion call.
- `INVALID_TENANT_ID` (existing) — reused.
- `RETRY_CONFIG_INVALID` (new, startup-only) — when `IngestionSettings.RetryPolicies` contains an invalid `MaxAttempts <= 0` entry. Fail fast at `Initialize` with a clear message.

### Project Structure Notes

**New files (source):**

- `src/Hexalith.Memories.Contracts/V1/FailedUnitInput.cs`
- `src/Hexalith.Memories.Contracts/V1/FailedUnitSummary.cs`
- `src/Hexalith.Memories.Contracts/V1/FailedUnitsPage.cs`
- `src/Hexalith.Memories.Contracts/V1/ReIngestRequest.cs`
- `src/Hexalith.Memories.Contracts/V1/BulkReIngestionResponse.cs`
- `src/Hexalith.Memories.Contracts/V1/ReIngestedUnitInfo.cs`
- `src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs`
- `src/Hexalith.Memories.Contracts/V1/CounterTransitionInput.cs`
- `src/Hexalith.Memories.Server/Ingestion/ActivityRetryPolicy.cs`
- `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs`
- `src/Hexalith.Memories.Server/Ingestion/RetryFailureLog.cs`
- `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs`
- `src/Hexalith.Memories.Server/Ingestion/FailedUnitRecord.cs` (internal)
- `src/Hexalith.Memories.Server/Ingestion/ReIngestionInput.cs` (internal — used only if the DAPR SDK `instanceId` parameter is unavailable)
- `src/Hexalith.Memories.Server/Actors/ICaseIngestionCounterActor.cs`
- `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterActor.cs`
- `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterState.cs` (internal)
- `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterLogic.cs` (internal)
- `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/UpdateCaseIngestionCounterActivity.cs`
- `docs/operations/failure-recovery.md` (or extend `docs/operations/rate-limiting.md`)

**Modified files (source):**

- `src/Hexalith.Memories.Contracts/V1/FailureDetails.cs` — +2 optional fields.
- `src/Hexalith.Memories.Contracts/V1/CaseStatusDetail.cs` — +4 counts (no `IsApproximate`).
- `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs` — **unchanged** (public contract preserved — re-ingestion uses DAPR workflow `instanceId` parameter or internal `ReIngestionInput` wrapper if SDK requires).
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — add new `[JsonSerializable]` entries for all new types.
- `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` — +`RetryPolicies` dictionary.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — per-invocation retry snapshot, counter-actor transitions at every stage, persist-failed on catch (pinned ordering), new `now` parameter to `AttachFailureDetails`.
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` — inject `IActorProxyFactory`, call `ICaseIngestionCounterActor.GetCountsAsync` in `GetCaseStatusAsync`, `ParseMemoryUnitFromHash` reads `failureDetailsJson`.
- `src/Hexalith.Memories.Server/Program.cs` — `RetryPolicyBuilder.Initialize`, new DI registrations (2 new activities + counter actor + logic + `FailedUnitsRegistry`), 4 new endpoints + `GET /memory-units/{id}`.
- `src/Hexalith.Memories.Server/appsettings.json` — add `Ingestion:RetryPolicies` section.
- `README.md` — optional Operations link update.

**New test files:**

- `tests/Hexalith.Memories.Server.Tests/Ingestion/RetryPolicyBuilderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/FailedUnitsRegistryTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/RetryFailureLogTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/PersistFailedUnitActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/UpdateCaseIngestionCounterActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterLogicTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/FailedUnitsEndpointsTests.cs` (if endpoint-unit pattern exists; else integration only)
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ReIngestionEndpointsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/GetMemoryUnitEndpointTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/FailureDetailsSerializationTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/FailedUnitsContractsSerializationTests.cs` (covering summary, page, requests, responses, counter transition input, counts)
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs` (all `[Fact(Skip)]` with greppable sentinel per Task 11.1)

**Modified test files:**

- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — per-invocation retry snapshot, counter transitions at each stage, pinned catch ordering, persist-failed on catch, `FailureDetails.ErrorMessage` + `LastRetryAt` population, AND the three Murat-coverage tests from Task 12.
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs` (if exists) — new count fields populated via the counter actor.
- `tests/Hexalith.Memories.Contracts.Tests/V1/` — extend existing tests for `FailureDetails` and `CaseStatusDetail` shape changes.

**No changes to:**

- `.slnx`, `Directory.Packages.props` (no new packages needed — `DaprWorkflowClient` and `IConnectionMultiplexer` already referenced), `Directory.Build.props`.
- DAPR component YAML files.
- Any other epic's code (unless case-delete coordination requires — see Edge Cases).
- `CreateCompensationRetry` in `IngestionWorkflow` (preserved as-is).

### Known Dependencies (Verify Before Starting)

- **`Microsoft.Extensions.TimeProvider.Testing`** — already present (added by 6.2).
- **DAPR SDK 1.17.6** — confirm the `ScheduleNewWorkflowAsync` and `GetWorkflowStateAsync` signatures per pre-implementation checklist step 5. The status endpoint no longer uses a fan-out reader; `GetWorkflowStateAsync` is only used by the existing `/api/ingest/{instanceId}` diagnostic endpoint.
- **`DaprWorkflowClient.ScheduleNewWorkflowAsync(string, string?, T, ...)`** signature — verify whether the overload accepts the instance ID as an explicit parameter or generates one internally; current Program.cs usage at line 208 suggests the auto-generated variant.
- **`WorkflowContext.SetCustomStatus(string)`** — verify SDK exposes it (the DAPR Workflow SDK does; `SetCustomStatus` is standard Durable Task framework API).

### Definition of Done

1. All new unit tests pass — at least **~30 new tests** covering:
    - `RetryPolicyBuilder` (init, fallback, override, empty) — ~4 tests.
    - `FailureDetails` serialization (with and without new fields, legacy round-trip) — ~3 tests.
    - Counter actor + logic (transitions, idempotency, zero-clamping) — ~6 tests.
    - `FailedUnitsRegistry` (list, get, remove, atomicity) — ~6 tests.
    - `PersistFailedUnitActivity` (success, Redis error) — ~2 tests.
    - `UpdateCaseIngestionCounterActivity` (proxy call + error path) — ~2 tests.
    - `IngestionWorkflow` retry-policy snapshot per activity + counter transitions at each stage + persist-failed on catch + instance-id preservation on re-ingestion — ~5 tests.
    - Murat coverage tests (IndexingFailure persists + records stream; re-ingest-then-fail-again overwrites hash; bulk infrastructure error → `errored` outcome) — 3 tests.
    - `RetryFailureLog` EventId assertions — ~3 tests (each event's core invariant).
    - Endpoint handlers for `/failed-units` list, `/memory-units/{id}` get, single re-ingest, bulk re-ingest — ~5 tests.
2. All integration tests are `[Fact(Skip)]` with tracker references to Story 6.4 or Epic 7.
3. `IngestionWorkflow` calls `RetryPolicyBuilder.For(activityName)` for every non-compensation activity call; the three compensation activity calls use `CreateCompensationRetry` unchanged.
4. `FailureDetails` record has `ErrorMessage` (nullable, truncated at 1024) and `LastRetryAt` (nullable, `DateTimeOffset?`) fields; legacy payloads deserialize with nulls.
5. `PersistFailedUnitActivity` writes both the hash and ZADD atomically via Lua; both are populated on every workflow failure; existing `IngestionFailed` activity-stream event is preserved.
6. `CaseStatusDetail` exposes `QueuedCount`, `ExtractingCount`, `EmbeddingCount`, `IndexingCount` — populated via `ICaseIngestionCounterActor.GetCountsAsync` (O(1) actor read); existing `IndexedCount` and `FailedCount` preserved.
7. `GET /api/tenants/{tenantId}/cases/{caseId}/failed-units` and `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` endpoints exist with validation, tenant guard, and standard error shapes.
8. `POST /memory-units/{id}/re-ingest` and `POST /failed-units/re-ingest` endpoints exist; both rebuild `IngestionInput` (internal `FailedUnitRecord`) and schedule new workflow instances via `ScheduleNewWorkflowAsync(..., instanceId: memoryUnitId, ...)`; dedup keys are atomically cleared; original memory-unit-id preserved.
9. `docs/operations/failure-recovery.md` (or extended `docs/operations/rate-limiting.md`) documents retry config, failed-units registry semantics, re-ingestion contract, `FailedCount` vs. `TotalCount` divergence, counter-actor semantics, and known MVP limitations.
10. No new NuGet dependencies.
11. `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` reports ≥ 1280 passing; zero new failures vs. 6.2 baseline (1250).
12. Structured log events 6301–6309 emitted on all designated paths; asserted by unit tests.
13. No regression on existing ingestion / tenant / search / case / annotation tests.

### References

- Epic 6 overview: [Source: _bmad-output/planning-artifacts/epics.md#Epic-6] (lines 1250–1378)
- Story 6.3 acceptance criteria source: [Source: _bmad-output/planning-artifacts/epics.md#Story-6.3] (lines 1312–1342)
- FR coverage (FR9–FR12, NFR19): [Source: _bmad-output/planning-artifacts/epics.md] (lines 28–31, 153, 238–241, 1322, 1326, 1331, 1335, 1341)
- Architecture — data model `FailureDetails`: [Source: _bmad-output/planning-artifacts/architecture.md#Data-Model] (line 117)
- Architecture — MemoryUnit `Status` enum values: [Source: _bmad-output/planning-artifacts/architecture.md#Data-Model] (line 112)
- Architecture — DAPR Workflow retry + compensation pattern: [Source: _bmad-output/planning-artifacts/architecture.md#Data-Flow] (lines 697–704, 779–795)
- Architecture — Workflow-Actor separation D25: [Source: _bmad-output/planning-artifacts/architecture.md#Decision-Registry] (line 566)
- Architecture — Eventual consistency + saga D3: [Source: _bmad-output/planning-artifacts/architecture.md#Decision-Registry] (line 549)
- Architecture — Embedding pipeline failure propagation: [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Dependencies] (line 155)
- Architecture — Rule 13 "use WorkflowRetryPolicy, never custom retry loops": [Source: _bmad-output/planning-artifacts/architecture.md] (line 1137)
- Story 1.4 (Rate limiter actor baseline): [Source: _bmad-output/implementation-artifacts/1-4-embedding-generation.md]
- Story 1.6 (Ingestion workflow): [Source: _bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md]
- Story 3.2 (Case status + activity): [Source: _bmad-output/implementation-artifacts/3-2-case-status-and-activity.md]
- Story 5.4 (Tenant context enforcement): [Source: _bmad-output/implementation-artifacts/5-4-tenant-context-enforcement.md]
- Story 5.6 (Graceful degradation + logging pattern): [Source: _bmad-output/implementation-artifacts/5-6-graceful-degradation-on-backend-failure.md]
- Story 6.1 (URL/directory ingestion + `IngestionSettings`): [Source: _bmad-output/implementation-artifacts/6-1-url-and-directory-ingestion.md]
- Story 6.2 (Per-tenant load + `RateLimitingLog` + `TenantId` threading + `IJitterSource`): [Source: _bmad-output/implementation-artifacts/6-2-per-tenant-load-management-and-rate-limiting.md]
- Existing `IngestionWorkflow`: [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs]
- Existing `AttachFailureDetails` helper: [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs] (lines 410–431)
- Existing `CheckIdempotencyActivity` + `SaveDedupKeyActivity`: [Source: src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs], [Source: src/Hexalith.Memories.Server/Activities/Ingestion/SaveDedupKeyActivity.cs]
- Existing `DedupKeyBuilder`: [Source: src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs]
- Existing `FailureDetails`: [Source: src/Hexalith.Memories.Contracts/V1/FailureDetails.cs]
- Existing `MemoryUnit` + `MemoryUnitStatus`: [Source: src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs], [Source: src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs]
- Existing `CaseStatusDetail`: [Source: src/Hexalith.Memories.Contracts/V1/CaseStatusDetail.cs]
- Existing `CaseActivityEventType` (with `IngestionFailed`): [Source: src/Hexalith.Memories.Contracts/V1/CaseActivityEventType.cs]
- Existing `CaseService.GetCaseStatusAsync`: [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs] (lines 393–441)
- Existing `CaseService.GetMemoryUnitAsync` / `ParseMemoryUnitFromHash`: [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs] (lines 235–259, 943–1003)
- Existing `CaseActivityService.GetFailedCountAsync`: [Source: src/Hexalith.Memories.Server/Cases/CaseActivityService.cs] (lines 105–134)
- Existing `RecordCaseActivityActivity`: [Source: src/Hexalith.Memories.Server/Activities/Ingestion/RecordCaseActivityActivity.cs]
- Existing `IngestionSettings` (6.1 + 6.2): [Source: src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs]
- Existing `RateLimitingLog` pattern template: [Source: src/Hexalith.Memories.Server/Ingestion/RateLimitingLog.cs]
- Existing Program.cs ingest + status endpoints: [Source: src/Hexalith.Memories.Server/Program.cs] (lines 192–209 ingest; 944–991 case status + activity; 1089–1133 DELETE memory-unit)

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context) — claude-opus-4-6[1m]

### Debug Log References

**Baseline (before 6.3):** `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` →
968 Server + 286 Contracts = **1254 tests passing, 0 failures**. Pre-existing AppHost `CS0311` and
Benchmarks `CS7036` build errors unchanged (non-blockers per 6.2 Dev Agent Record).

**Post-6.3:** same command → 1013 Server + 288 Contracts = **1301 tests passing, 0 failures**.
Net +47 new tests; well above the ≥30 target. AppHost / Benchmarks baseline errors preserved.

**DAPR SDK 1.17.6 verification (pre-impl checklist step 5):** confirmed
`DaprWorkflowClient.ScheduleNewWorkflowAsync(name, instanceId, input)` accepts a caller-specified
instance id (used by `Program.cs:604`, `Program.cs:785`). Re-ingestion preserves the memory-unit-id by
passing it as `instanceId` — the `ReIngestionInput` wrapper fallback from Breaking Changes #2 was NOT
needed.

**Resolved test-state leakage:** `RetryPolicyBuilder` holds a process-global snapshot; tests that
assert default-policy values (`RunAsync_DimensionMismatchFailure_ShouldStillUseMainRetryPolicy`, and
new Task 12 tests) now call `RetryPolicyBuilder.Initialize(new IngestionSettings())` at the top to
reset to defaults.

**Resolved NSubstitute / extension-method interaction:** `IActorProxyFactory.CreateActorProxy<T>` is
an extension method; NSubstitute proxies return null from it. Fix: `CaseService.GetIngestionCountsSafe`
null-guards the proxy (cold-actor drift) and test helpers (`CreateMockActorProxyFactory`) stub the
underlying `CreateActorProxy(ActorId, Type, string, ActorProxyOptions?)` to return a live proxy mock.

### Completion Notes List

- **Task 1** — `FailureDetails` extended with optional `ErrorMessage` (truncated at 1024 chars) and
  `LastRetryAt`; `AttachFailureDetails` now takes a `now: DateTimeOffset` parameter sourced from
  `context.CurrentUtcDateTime` for replay determinism. `IngestionInput` deliberately unchanged
  (capability-leak rejection held firm).
- **Task 2** — `ActivityRetryPolicy` + `RetryPolicyBuilder` (SnapshotAll/For/Initialize) landed; the
  workflow body reads `SnapshotAll()` once per invocation and looks up each activity's policy via a
  local `For(name)` helper. `CreateMainRetry()` was removed; `_mainRetryAttempts=5` survives as a
  `GetRetryCountForStage` constant so pre-6.3 behavior is preserved (AC11 pinning test updated).
  `RETRY_CONFIG_INVALID` fails fast on `MaxAttempts<=0`.
- **Task 3** — `CaseIngestionCounterActor` (per `{tenantId}:{caseId}`) + `CaseIngestionCounterLogic`
  (pure, testable) + `CaseIngestionCounterState` + `UpdateCaseIngestionCounterActivity` (best-effort;
  6310 on failure). Workflow wired with deterministic `transitionId = "{instanceId}:{seq}"` so replays
  are idempotent. `SetCustomStatus` breadcrumb at every transition.
- **Task 4** — `PersistFailedUnitActivity` writes hash + ZADD atomically via Lua; the outer catch in
  `IngestionWorkflow.RunAsync` now follows the pinned order
  `AttachFailureDetails → UpdateCounter(...,"none") → SetCustomStatus("failed") → TryPersistFailedUnit → throw`.
  `TryPersistFailedUnit` isolates persistence failures as event 6309 without masking the original
  exception.
- **Task 5** — `FailedUnitsRegistry` (List/Get/Remove) + `GET /failed-units` + `GET /memory-units/{id}`
  with failed-unit fallback that synthesizes a `MemoryUnit` with `Status=Failed` +
  `FailureDetails`. `ParseMemoryUnitFromHash` gained a `failureDetailsJson` reader hook.
- **Task 6** — single + bulk re-ingestion endpoints. Atomic Lua claim deletes the failed-unit hash,
  sorted-set entry, AND dedup key. Memory-unit-id preserved via the DAPR workflow `instanceId`
  parameter on `ScheduleNewWorkflowAsync` — no public-contract change. Bulk endpoint enumerates
  per-unit outcomes and never aborts the batch.
- **Task 7** — `CaseStatusDetail` gained `QueuedCount`/`ExtractingCount`/`EmbeddingCount`/`IndexingCount`
  as positional optional fields (no `IsApproximate`). `CaseService.GetCaseStatusAsync` now issues an
  O(1) `ICaseIngestionCounterActor.GetCountsAsync` via injected `IActorProxyFactory`; failure →
  zero counts + warning log (graceful degradation).
- **Task 8** — `RetryFailureLog` (events 6301–6310) using `[LoggerMessage]` partial class; 7 tests
  pin EventId + LogLevel per event.
- **Task 9** — `docs/operations/failure-recovery.md` covers retry config, registry semantics, the
  `FailedCount` vs `TotalCount` divergence, counter-actor rationale, log event table, and MVP limits.
- **Task 10** — 1301 passing, 0 failures post-6.3; +47 net new tests vs. baseline 1254.
- **Task 11** — `RetryFailureIntegrationTests` scaffolded with 4 `[Fact(Skip)]` entries; sentinel
  references Story 6.4 / Epic 7 per convention.
- **Task 12** — Murat coverage added: IndexingFailure persists + records stream event;
  persist-failed-unit itself failing does not mask the original exception; every workflow transition
  carries a unique `transitionId`.

### File List

**New source files:**

- `src/Hexalith.Memories.Contracts/V1/BulkReIngestionResponse.cs`
- `src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs`
- `src/Hexalith.Memories.Contracts/V1/CounterTransitionInput.cs`
- `src/Hexalith.Memories.Contracts/V1/FailedUnitInput.cs`
- `src/Hexalith.Memories.Contracts/V1/FailedUnitSummary.cs`
- `src/Hexalith.Memories.Contracts/V1/FailedUnitsPage.cs`
- `src/Hexalith.Memories.Contracts/V1/ReIngestRequest.cs`
- `src/Hexalith.Memories.Contracts/V1/ReIngestedUnitInfo.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/UpdateCaseIngestionCounterActivity.cs`
- `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterActor.cs`
- `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterLogic.cs`
- `src/Hexalith.Memories.Server/Actors/CaseIngestionCounterState.cs`
- `src/Hexalith.Memories.Server/Actors/ICaseIngestionCounterActor.cs`
- `src/Hexalith.Memories.Server/Ingestion/ActivityRetryPolicy.cs`
- `src/Hexalith.Memories.Server/Ingestion/FailedUnitRecord.cs`
- `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs`
- `src/Hexalith.Memories.Server/Ingestion/RetryFailureLog.cs`
- `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs`
- `docs/operations/failure-recovery.md`

**Modified source files:**

- `src/Hexalith.Memories.Contracts/V1/CaseStatusDetail.cs` — +4 positional count fields.
- `src/Hexalith.Memories.Contracts/V1/FailureDetails.cs` — +`ErrorMessage` and `LastRetryAt` optional.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — +10 `[JsonSerializable]` entries.
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` — `IActorProxyFactory` injected; counter read
  wired into `GetCaseStatusAsync`; `ParseMemoryUnitFromHash` reads `failureDetailsJson`.
- `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` — +`RetryPolicies` dictionary.
- `src/Hexalith.Memories.Server/Program.cs` — `RetryPolicyBuilder.Initialize` post-`Build`; 2 new
  activity registrations + 1 new actor registration + `FailedUnitsRegistry` + `CaseIngestionCounterLogic`
  DI singletons; 4 new endpoints (`GET /failed-units`, `GET /memory-units/{id}`, single
  `POST /re-ingest`, bulk `POST /failed-units/re-ingest`).
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — per-invocation retry snapshot;
  counter transitions at every stage; pinned-order catches; `TryPersistFailedUnit` and
  `MapStageToBucket` helpers; `CreateMainRetry` removed.
- `src/Hexalith.Memories.Server/appsettings.json` — `Ingestion:RetryPolicies` example.

**New test files:**

- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/PersistFailedUnitActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/UpdateCaseIngestionCounterActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterLogicTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/FailedUnitsRegistryTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/RetryFailureLogTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/RetryPolicyBuilderTests.cs`

**Modified test files:**

- `tests/Hexalith.Memories.Contracts.Tests/V1/FailureDetailsSerializationTests.cs` — added new-fields
  round-trip and legacy-payload tests.
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs` — constructor calls updated for
  `IActorProxyFactory`; helper `CreateMockActorProxyFactory` added.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantContextEnforcementTests.cs` — same constructor
  adjustment + helper.
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — AC2 truncation +
  `LastRetryAt` tests; AC11 default-policy regression test re-expressed via `RetryPolicyBuilder`;
  Task 12 Murat-coverage tests (persist + stream event; persist-failure-does-not-mask;
  unique-transitionId-per-call).

### Change Log

- 2026-04-15 — Story 6.3 implementation complete (Opus 4.6). 1301 passing, 0 failures
  (+47 vs. baseline 1254). Status → review.

