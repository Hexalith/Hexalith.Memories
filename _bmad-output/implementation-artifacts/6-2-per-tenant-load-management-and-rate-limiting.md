# Story 6.2: Per-Tenant Load Management & Rate Limiting

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** the reliability-and-fairness layer that sits on top of the ingestion pipeline 6.1 completed. This story does NOT create a new pipeline; it hardens the existing one so that (a) one tenant's batch cannot starve another tenant's real-time ingest, (b) 429 responses from the embedding provider are absorbed without data loss and without immediate re-exhaustion, and (c) per-tenant ceilings are enforced at the `EmbeddingRateLimiterActor`. Concretely: (1) the `WorkflowRetryPolicy` in `IngestionWorkflow` is migrated from fixed exponential to exponential **with jitter** (NFR22); (2) the `EmbeddingRateLimiterActor` gains a `ReportRateLimitedAsync(retryAfterSeconds)` method that zero-floors the remaining budget and bumps the window start to "now + Retry-After" so the next `TryConsumeAsync` fails fast until the provider window re-opens; (3) `GenerateEmbeddingActivity` calls `ReportRateLimitedAsync` whenever the provider returns HTTP 429 — the `Retry-After` header (or a default) drives the pause; (4) a new `PerTenantConcurrencyGate` (pure service registered as singleton, keyed by `tenantId`, backed by a dictionary of `SemaphoreSlim`) bounds the **concurrent CPU-bound extraction activities** per tenant (`ExtractContentActivity` and `FetchUrlActivity`), so tenant A's 500-file PDF batch cannot monopolize the in-process extraction threadpool and starve tenant B. Default concurrency = 4 per tenant, configurable via `Ingestion:PerTenantExtractionConcurrency`. The gate is acquired at activity entry and released at exit; workflow semantics are unchanged. (5) Structured logging + metrics for every throttle event, every 429-feedback event, and the extraction concurrency queue depth.

**What does NOT ship:** cross-tenant (global) rate limiter coordination (architecture §D: "Shared embedding rate limiter" = Phase 3); retry dashboard, failure visibility per case, or re-ingestion endpoints (Story 6.3); pipeline state persistence & zero-data-loss validation beyond what DAPR Workflow already guarantees (Story 6.4); a new actor for extraction concurrency (the in-process semaphore gate is sufficient since extraction is in-process Kreuzberg — if the Memories Server instance restarts, workflows replay and re-acquire the gate naturally); adaptive/learning rate limits (the ceiling is `TenantEmbeddingConfig.RateLimitPerMinute`, already plumbed through Story 1.7/5.5); per-tenant indexing throttles (indexing is Redis I/O-bound, not CPU-bound, and Redis itself is the shared bottleneck — addressing this is Epic 5.6's degradation story scope); CLI or MCP surface changes (Epic 7/10); authenticated ingress or tenant-API-key scoping (Phase 1.5).

**Primary risks:** (1) **Per-tenant semaphore leak on cancelled/replayed activities** — `SemaphoreSlim.Release()` MUST run in a `finally` block; replayed activities from DAPR Workflow history must not double-acquire (the gate lives outside workflow state — a replay invokes the activity handler afresh, so the gate is naturally re-entered by the fresh activity call; no double-counting). (2) **Jitter randomness and determinism** — DAPR Workflow retry delays are computed by the framework, not the workflow code, so adding jitter requires switching to `WorkflowRetryPolicy`'s jitter feature if available OR driving custom retry loops inside the activity (avoid the custom loop — see Anti-Patterns). In `Dapr.Workflow` 1.17.6, `WorkflowRetryPolicy` does NOT expose a jitter parameter; we add **application-level jitter** in the activities themselves via `Task.Delay(Random.Shared.Next(0, 500))` ms before the retry-eligible call — this is documented in Dev Notes as a deliberate MVP compromise. (3) **Per-tenant extraction concurrency gate can deadlock** if the gate is acquired inside a workflow activity that itself calls another activity that tries to re-acquire the gate for the same tenant. Mitigation: the gate is ONLY acquired in leaf activities (`ExtractContentActivity`, `FetchUrlActivity`), never in orchestration. Verified by inspection (no activity calls another activity directly — DAPR activities are leaves by the framework contract). (4) **429 feedback racing the retry** — if tenant A's first attempt returns 429 at T0 with `Retry-After: 30s` and the workflow retry policy schedules the second attempt at T2s, the second attempt will fail the actor's `TryConsumeAsync` (budget is zeroed until T30s) and DAPR Workflow will retry again — this is **the desired behavior** because it avoids hammering a rate-limited provider. The catch: the workflow retry budget (5 attempts × up to 5 min max interval) must still cover a realistic `Retry-After`; 30s is typical, 5 minutes is the upper documented bound, and the existing retry policy covers this (max total wait ≈ 26.4s per attempt window, 5 attempts, accumulated over ~130 s worst case; still sufficient for most providers). **Document this explicitly.** (5) **Test determinism vs. jitter** — unit tests must inject a fake `IJitterSource` to make timing deterministic. Integration tests use real jitter; assert on "eventually succeeds" not "succeeds at exactly T+2s." (6) **Actor state race on `ReportRateLimitedAsync`** — the actor is single-threaded per ID (DAPR virtual actor guarantee), so the zero-flooring of `Remaining` and the bump of `WindowStart` are atomic from the caller's perspective; no lock needed inside the actor. (7) **`SetCeilingAsync` called on every embedding** — `GenerateEmbeddingActivity` currently calls `rateLimiter.SetCeilingAsync(config.RateLimitPerMinute)` every invocation (see `GenerateEmbeddingActivity.cs:58`); this is correct for ceiling changes but wasteful on the hot path. **Out of scope** for 6.2: optimizing the call away is a Phase 2 micro-optimization; we do NOT change the call pattern in this story. Document the known cost.

## Breaking Changes (Pre-Gate-3 MVP)

1. **`IEmbeddingRateLimiterActor` gains a method** `Task ReportRateLimitedAsync(int retryAfterSeconds)`. Additive — existing callers unaffected. `retryAfterSeconds` is a hint (from the provider `Retry-After` response header) used to set the next window-open time. Implementation updates `RateLimitState.WindowStart = now + retryAfterSeconds` and `Remaining = 0`. The actor re-fills at the next natural window boundary per existing `RateLimiterLogic.TryConsume` (no schema change to `RateLimitState`).

2. **`IngestionWorkflow.CreateMainRetry()` constants remain at `maxNumberOfAttempts=5`, `firstRetryInterval=2s`, `backoffCoefficient=1.5`, `maxRetryInterval=5min`**. Do NOT widen. Jitter is applied **inside activities** (not in the policy) via a `Task.Delay(Random.Shared.Next(0, 500))` ms wait BEFORE the provider call in `GenerateEmbeddingActivity` ONLY. The `Activity retry` sits on top; workflow retry policy still defines the max total attempts. Rationale documented in Dev Notes.

3. **`GenerateEmbeddingActivity` constructor gains an `IJitterSource` dependency** (new interface) for deterministic testing. Default DI binding: `Services.AddSingleton<IJitterSource, ThreadSafeRandomJitterSource>()`. `ThreadSafeRandomJitterSource` wraps `Random.Shared`. Test fixtures inject a deterministic fake.

4. **New service `PerTenantConcurrencyGate`** in `Hexalith.Memories.Server.Ingestion` namespace, registered as **singleton**, backed by a `ConcurrentDictionary<string, SemaphoreSlim>` keyed by tenant ID. Methods: `Task<IAsyncDisposable> AcquireAsync(string tenantId, CancellationToken ct)` and `int GetCurrentCount(string tenantId)` (for metrics). Max concurrent = `IngestionSettings.PerTenantExtractionConcurrency` (default 4). Additive — existing code unaffected; ONLY `ExtractContentActivity` and `FetchUrlActivity` call `AcquireAsync`.

5. **`IngestionSettings` (new in 6.1) gains** `int PerTenantExtractionConcurrency { get; init; } = 4;`. No breaking change — existing config keys remain. Defaults applied via `IConfiguration.Bind`.

6. **`EmbeddingRateLimitException` is caught specifically inside `GenerateEmbeddingActivity`** when the provider returns 429: the activity now calls `rateLimiter.ReportRateLimitedAsync(retryAfter)` BEFORE re-throwing. The exception type is preserved so the workflow's outer retry policy still fires.

7. **Structured log events 6201–6206** added in a new file `src/Hexalith.Memories.Server/Ingestion/RateLimitingLog.cs` (mirrors 6.1's `IngestionEndpointLog.cs` pattern). Events: `RateLimitExceededLocally` (6201, Warning), `ProviderRateLimitReceived` (6202, Warning), `RateLimitActorUpdated` (6203, Information), `ExtractionGateAcquired` (6204, Debug), `ExtractionGateContended` (6205, Information), `ExtractionGateTimeout` (6206, Warning — acquire timeout expired).

## Story

As a developer,
I want ingestion load managed independently per tenant with enforced rate limits,
so that one tenant's batch ingestion doesn't starve another's real-time ingestion and provider 429 responses do not cause data loss or thundering-herd retries.

## Acceptance Criteria

**Reading note for the dev agent:** ACs are labelled `[VERIFY]` (regression-guard tests over already-shipped behavior — write tests only, no new production code) or `[NEW]` (requires new code). AC1, AC7, AC10 are `[VERIFY]`; AC2, AC3, AC4, AC5, AC6, AC8, AC9, AC11, AC12 are `[NEW]` or mixed.

1. **[VERIFY] Per-tenant rate limiter enforces independent ceilings (FR8, FR69, NFR13).** Given two tenants `t1` (ceiling 500/min) and `t2` (ceiling 3000/min) configured via `TenantEmbeddingConfig.RateLimitPerMinute`, when both tenants ingest concurrently, then each tenant's `EmbeddingRateLimiterActor` (Actor ID = tenant ID) holds independent `RateLimitState` with its own `CeilingPerMinute`, `Remaining`, and `WindowStart`. Verification: integration test provisions two tenants with different ceilings, hits 1000 ingestions for `t1` (should throttle 500), hits 1000 ingestions for `t2` (all 1000 succeed — under ceiling). Actor state is queried post-run via `GetStateAsync()` and asserted distinct. **No code change from baseline** — this is a _verification_ AC, not a _new-feature_ AC; the actor already provides this (Story 1.4). **Dev-agent action: add the integration test only.** The test is `[Fact(Skip)]` pending Aspire fixture per 6.1 precedent.

2. **Tenant A batch does NOT starve tenant B real-time ingest (FR8, NFR13).** Given tenant `t1` submits a 500-file batch via `POST /api/ingest/directory`, when tenant `t2` concurrently submits a single-file `POST /api/ingest` within 1 s of `t1`'s batch scheduling, then tenant `t2`'s workflow is scheduled, enters `extracting` stage, and completes within **≤ 2× the baseline single-file ingest P50 latency** measured when no `t1` batch runs. Baseline reference: P50 ≤ 600 ms for a 10 KB text file with `UseFakeEmbedding=true`. The `PerTenantExtractionConcurrency=4` gate caps tenant `t1`'s concurrent `ExtractContentActivity` invocations to 4 and `FetchUrlActivity` to 4, so tenant `t2`'s activities are not queued behind `t1`'s 500. Integration test `[Fact(Skip)]` captures this with fake embedding and `<= 10KB` synthetic content; assertion uses `TimeSpan` thresholds with a 50 % safety margin. **The test explicitly asserts `t2.P50 < t1.batchDuration * 2 / count(t1)` as a coarse upper bound to catch gross starvation regressions.**

3. **Provider 429 triggers actor budget zero-floor + re-fill at Retry-After.** Given the embedding provider returns HTTP 429 for tenant `t1`'s request, when `GenerateEmbeddingActivity.RunAsync` catches the 429 (currently via `EmbeddingClient.HandleEmbeddingResponseAsync` raising `EmbeddingRateLimitException`), then the activity **(a)** parses the `Retry-After` response header (seconds or HTTP-date, per RFC 9110 §10.2.3) into `retryAfterSeconds` (default `30` if header missing or malformed), **(b)** invokes `rateLimiter.ReportRateLimitedAsync(retryAfterSeconds)`, **(c)** logs `ProviderRateLimitReceived` (event 6202) with `tenantId`, `retryAfterSeconds`, `memoryUnitId`, and **(d)** re-throws `EmbeddingRateLimitException` so the workflow retry policy fires. After `ReportRateLimitedAsync`, the `RateLimitState.Remaining = 0` and `WindowStart` is bumped to `TimeProvider.GetUtcNow() + retryAfterSeconds`, so subsequent `TryConsumeAsync` returns `false` for the remainder of the Retry-After window (triggering workflow retry without consuming provider quota). Unit test: mocks `IEmbeddingRateLimiterActor`, asserts `ReportRateLimitedAsync(30)` called exactly once on 429 with no `Retry-After`, asserts `ReportRateLimitedAsync(60)` on `Retry-After: 60`.

4. **Workflow retry absorbs transient 429 without data loss (NFR22).** Given a workflow activity fails with `EmbeddingRateLimitException` (either provider-origin 429 or local `TryConsumeAsync=false`), when the DAPR Workflow retry policy fires its 5 attempts, then the workflow **(a)** applies exponential backoff (2s → 3s → 4.5s → 6.75s → 10.125s, bounded to 5 min), **(b)** adds **application-level jitter** inside `GenerateEmbeddingActivity` via `await Task.Delay(TimeSpan.FromMilliseconds(jitterSource.NextMilliseconds()))` where `NextMilliseconds()` returns a uniform-random value in `[0, 500)`, **(c)** retries the entire `GenerateEmbeddingActivity` (not the whole workflow), and **(d)** if all 5 retries are exhausted, the existing workflow catch-all (see `IngestionWorkflow.cs:300`) records `FailureDetails { Stage="embedding", ErrorCode="EmbeddingRateLimitException", RetryCount=5 }` and moves the memory unit to `Failed`. Zero data loss: the workflow is durable, no partial index writes exist at this stage, and the outer `RecordCaseActivityActivity(IngestionFailed)` fires (existing behavior from 5.6). Test: mock the provider to return 429 for the first 3 calls and 200 on the 4th; assert the memory unit ends in `Indexed` with `RetryCount=3` in telemetry logs (not in `FailureDetails` — success paths don't attach those).

5. **Per-tenant extraction concurrency gate bounds CPU-intensive activities.** Given tenant `t1` has 50 `ExtractContentActivity` invocations queued (say, 50 PDFs in a batch), when the activities run under `PerTenantExtractionConcurrency=4`, then no more than **4** `ExtractContentActivity` invocations for `t1` run concurrently; the remaining 46 wait on `SemaphoreSlim.WaitAsync()` inside `PerTenantConcurrencyGate.AcquireAsync(tenantId)`. Tenant `t2`'s activities are **unaffected** — they acquire on `t2`'s own `SemaphoreSlim`, independent gate. Unit test: instantiate `PerTenantConcurrencyGate` with max=2, launch 4 simultaneous `AcquireAsync("t1")` tasks, assert that 2 complete immediately and 2 wait until the first 2 are disposed. Repeat for `t2` concurrently; assert `t2`'s acquisitions are not blocked by `t1`'s. **Gate release pattern:** `await using IAsyncDisposable lease = await gate.AcquireAsync(tenantId, ct); /* work */` — release happens at scope exit, guaranteed by `await using` / `finally`. Acquire timeout is `IngestionSettings.ExtractionGateAcquireTimeoutSeconds` (default 300 s / 5 min) — if exceeded, log `ExtractionGateTimeout` (event 6206) and throw `TimeoutException`, which the workflow retry policy handles.

6. **Gate acquire applies to both `ExtractContentActivity` AND `FetchUrlActivity`.** Given a mix of URL ingestions and file ingestions for the same tenant, when both activity types run, then both call `PerTenantConcurrencyGate.AcquireAsync(tenantId)` against the **same `SemaphoreSlim`** for that tenant (i.e., the gate does not differentiate by activity type — it caps total CPU-ish work per tenant). Rationale: URL fetch is I/O-bound but downstream extraction is CPU-bound; the shared gate keeps per-tenant resource use predictable without a second gate. Unit test: simulate 3 URL fetches + 3 extractions for `t1` under `max=4`, assert at most 4 are in-flight concurrently. Cross-tenant: 3 URL fetches for `t1` + 3 extractions for `t2` run 6-wide (unrelated gates).

7. **[VERIFY] Rate-limiter ceiling reflects current `TenantEmbeddingConfig.RateLimitPerMinute`.** Given an operator updates a tenant's `RateLimitPerMinute` from 1500 → 500 via `PATCH /api/tenants/{tenantId}/config` (Story 5.5, existing endpoint), when the next ingestion for that tenant runs, then `GenerateEmbeddingActivity.RunAsync` reads the updated config via `ITenantConfigurationActor.GetEmbeddingConfigAsync()` and calls `rateLimiter.SetCeilingAsync(500)` before `TryConsumeAsync()`. The actor's `RateLimiterLogic.SetCeiling` clamps `Remaining = Math.Min(Remaining, 500)` so the tenant cannot exceed the new ceiling mid-window. **This is existing behavior** (see `GenerateEmbeddingActivity.cs:58` + `RateLimiterLogic.SetCeiling`). AC7 is a regression-guard test: after a ceiling drop, the next `TryConsumeAsync` respects the new ceiling. New unit test in `EmbeddingRateLimiterActorTests` OR `GenerateEmbeddingActivityTests` — choose the activity-level test to keep the coverage close to the behavior.

8. **Documented provider-API shared bottleneck.** Given shared embedding API keys across tenants (all tenants using the same Google API key share the underlying Google rate limit), when multiple tenants push to the same provider key, then (a) each tenant's `EmbeddingRateLimiterActor` enforces its own **logical** ceiling (protects against one tenant hogging the quota), but (b) the **effective** ceiling is the provider's actual rate limit at the API-key level (e.g., Google's free tier = 1500 req/min TOTAL, shared). This is **not a bug** — cross-tenant shared-key coordination is Phase 3 (per architecture §D, "Shared embedding rate limiter"). AC8 is a **documentation AC**: a new section `## Shared Provider Quota (Known Limitation)` in `docs/operations/rate-limiting.md` (create if missing) describes (i) the per-tenant ceiling semantics, (ii) the provider ceiling as the hard upper bound, (iii) the operator-level mitigation (assign separate API keys per tenant via `TenantEmbeddingConfig.ApiSecretKeyName`, which is already tenant-scoped from Story 1.7 / 5.5), (iv) the Phase 3 roadmap pointer. **Dev-agent action:** create the file; add a reference in `README.md` under a new "Operations" section or under the existing docs links. Not mandatory for DoD (Gate 3 polish); MUST exist before `code-review`.

9. **Metrics and structured logging on every rate-limit event.** Given a rate-limit decision is made (either local actor throttle or provider 429), when the decision is made, then a structured log event is emitted via `[LoggerMessage]` on `RateLimitingLog` (see Breaking Changes #7). Events 6201 (`RateLimitExceededLocally`) and 6202 (`ProviderRateLimitReceived`) both carry `tenantId`, `remaining`, `ceiling`, `retryAfterSeconds` (6202 only). Events 6204–6206 cover the concurrency gate. Logs are JSON-structured per Aspire defaults. **No new OpenTelemetry metric counters in this story** — the Aspire dashboard and structured logs are the MVP observability surface; per-tenant metrics are Epic 8 (Observability).

10. **[VERIFY] Cross-tenant isolation verified by integration test.** Given two tenants `t1` and `t2`, when `t1` is throttled by its actor (budget exhausted) OR by a 429 (`ReportRateLimitedAsync` pauses the window), then `t2`'s ingestions proceed normally — `t2`'s actor state is untouched, `t2`'s `TryConsumeAsync` returns `true` if under ceiling. Integration test `[Fact(Skip)]`: provision `t1` with ceiling 1, `t2` with ceiling 1500; in parallel, submit 2 ingestions for `t1` (1st succeeds, 2nd throttles-and-retries) and 10 ingestions for `t2` (all succeed); assert `t2`'s 10 workflows all reach `Indexed` without delay; assert `t1`'s 2nd reached `Indexed` after the window expires.

11. **Workflow retry on `EmbeddingRateLimitException` uses existing policy.** Given `GenerateEmbeddingActivity` throws `EmbeddingRateLimitException`, when the DAPR Workflow engine evaluates the retry policy, then the existing `WorkflowRetryPolicy` (5 attempts, exponential, 5 min cap) is used WITHOUT a second/parallel retry path. The policy does NOT distinguish exception types (DAPR SDK 1.17.6 limitation, documented in 5.6 / 6.1). This AC is a **pin** — dev agent MUST NOT introduce a second retry wrapper, a Polly pipeline, a custom delay loop in the activity beyond the single jitter `Task.Delay`, or a per-exception retry policy. **One policy, one activity, one jitter — that's it.**

12. **No regression on existing ingestion, search, or tenant tests.** Baseline before 6.2: ~1191 tests in `Hexalith.Memories.Server.Tests` + Contracts.Tests (per 6.1 Dev Agent Record). After 6.2: at least ~1210 tests (≈ 20 new unit tests). Zero new failures. Pre-existing baseline failures (e.g., `SaveDedupKeyActivityTests`, 2 tests) remain documented; do NOT fix them in 6.2 — they're noise relative to the reliability thesis of this story. Verify by running `dotnet test Hexalith.Memories.slnx` at the repo root at start and end of dev-story.

## Tasks / Subtasks

**Pre-implementation checklist (run before Task 1):**

1. Confirm 6.1 is `done` in `sprint-status.yaml` (6.2 rebases on its `TenantId`-in-`ExtractionInput` / `FetchUrlInput` additions).
2. Run `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` and record counts in Debug Log References (expected ~1191 passing + 2 documented `SaveDedupKeyActivityTests` failures).
3. Verify `Microsoft.Extensions.TimeProvider.Testing` is in `Directory.Packages.props`; if absent, add it before writing tests.
4. Verify existing `IEmbeddingRateLimiterActor`, `RateLimiterLogic`, `EmbeddingRateLimitException`, and `GenerateEmbeddingActivity.cs:58` match this story's assumed baseline — any drift means the story's code samples need adjustment before proceeding.

- [x] Task 1: Extend `EmbeddingRateLimiterActor` with `ReportRateLimitedAsync` (AC: #3, #4, #11)
    - [x] 1.1 In `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs`, add:
        ```csharp
        /// <summary>Reports a provider 429 so the actor pauses consumption until the Retry-After window elapses.</summary>
        /// <param name="retryAfterSeconds">Seconds until the provider should be retried (from Retry-After response header, or a default).</param>
        Task ReportRateLimitedAsync(int retryAfterSeconds);
        ```
        Clamp guidance in XML-doc: "Implementations MUST clamp retryAfterSeconds to [1, 3600]; values outside the range indicate caller error."
    - [x] 1.2 In `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs`, add a pure static helper:
        ```csharp
        public RateLimitState ReportRateLimited(RateLimitState currentState, int retryAfterSeconds)
        {
            int clamped = Math.Clamp(retryAfterSeconds, 1, 3600);
            DateTime windowOpen = _timeProvider.GetUtcNow().UtcDateTime + TimeSpan.FromSeconds(clamped);
            return currentState with { Remaining = 0, WindowStart = windowOpen };
        }
        ```
        Rationale: setting `WindowStart` to a future time means `TryConsume` will see `now - windowStart < WindowDuration`, so the natural refill at `+1 min` past `windowOpen` fires; from `windowOpen` to `windowOpen + 1 min` the budget is zero.
        **Note**: `TryConsume` tests `if (now - state.WindowStart >= WindowDuration)` — if `WindowStart` is in the future, `now - WindowStart` is negative, so the refill condition is `false` during the pause. At `windowOpen + 1 min`, the condition flips to `true` and refill happens.
        **Edge case** to verify in test: `retryAfterSeconds=0` is clamped to `1`; `retryAfterSeconds=3601` is clamped to `3600`; `retryAfterSeconds=-5` is clamped to `1`.
    - [x] 1.3 In `src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs`, implement:
        ```csharp
        public async Task ReportRateLimitedAsync(int retryAfterSeconds)
        {
            RateLimitState state = await GetOrCreateStateAsync().ConfigureAwait(false);
            RateLimitState newState = _logic.ReportRateLimited(state, retryAfterSeconds);
            await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
        }
        ```
        Follow the existing pattern from `ResetAsync` and `SetCeilingAsync`. Persist state before return (per architecture §D23: actor state must persist before response).
    - [x] 1.4 In `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs`, add `[Theory]` rows for `ReportRateLimited`:
        - `retryAfterSeconds=30, currentRemaining=500` → `Remaining=0, WindowStart=now+30s`.
        - `retryAfterSeconds=0` → clamped to 1 → `WindowStart=now+1s`.
        - `retryAfterSeconds=10000` → clamped to 3600 → `WindowStart=now+3600s`.
        - `retryAfterSeconds=-1` → clamped to 1 → `WindowStart=now+1s`.
        - After `ReportRateLimited`, `TryConsume` at `windowOpen - 1s` returns `(false, state)` (budget still 0).
        - After `ReportRateLimited`, `TryConsume` at `windowOpen + 61s` returns `(true, state with Remaining=ceiling-1)` (refill fired).
          Use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` (already referenced? if not, add package; see Known Dependencies section).
    - [x] 1.5 In `tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs`, add a test that mocks `IActorStateManager`, invokes `ReportRateLimitedAsync(30)`, asserts `SetStateAsync("rateState", ...)` is called with `Remaining=0` and `WindowStart` advanced. Mirror the existing `SetCeilingAsync` test pattern.
    - [x] 1.6 **Ordering test (Murat's addition):** In `RateLimiterLogicTests.cs`, add a test that simulates the DAPR-serialized call ordering on a single actor: `TryConsume` at `T=0` with remaining=100 → returns `(true, remaining=99)`; `ReportRateLimited(30)` at `T=1s` → `remaining=0, windowStart=T+31s`; `TryConsume` at `T=2s` (still inside the paused window) → returns `(false, remaining=0)`; `TryConsume` at `T=T+32s` → window refilled, returns `(true, remaining=ceiling-1)`. Pins the semantic guarantee that a `ReportRateLimited` observed by a racing `TryConsume` produces a throttle on the NEXT consume, never retroactively on the previous one. Uses `FakeTimeProvider`. Not testing DAPR's per-actor serialization (framework guarantee); testing that the **logic** preserves ordering when serialization is observed.

- [x] Task 2: Wire `ReportRateLimitedAsync` into `GenerateEmbeddingActivity` on 429 (AC: #3, #4, #9)
    - [x] 2.1 Create `src/Hexalith.Memories.Server/Ingestion/IJitterSource.cs`:
        ```csharp
        /// <summary>Abstraction for jittered retry delays. Injectable for deterministic tests.</summary>
        public interface IJitterSource
        {
            /// <summary>Returns a uniform-random integer in [0, maxExclusive) milliseconds.</summary>
            int NextMilliseconds(int maxExclusive = 500);
        }
        ```
    - [x] 2.2 Create `src/Hexalith.Memories.Server/Ingestion/ThreadSafeRandomJitterSource.cs`:
        ```csharp
        public sealed class ThreadSafeRandomJitterSource : IJitterSource
        {
            public int NextMilliseconds(int maxExclusive = 500)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
                return Random.Shared.Next(0, maxExclusive);
            }
        }
        ```
        Register as singleton: `services.AddSingleton<IJitterSource, ThreadSafeRandomJitterSource>()` in `MemoriesServerServiceCollectionExtensions.AddMemoriesServer` (or `Program.cs` if that's where the other ingestion services are wired — verify first; `IUrlContentFetcher` placement is the precedent).
    - [x] 2.3 Modify `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` — add jitter (on retry only) + 429 feedback. **Jitter is applied ONLY on retry attempts, not on first attempts** (per Pre-mortem finding: first-attempt jitter wastes cumulative time on happy-path batches). DAPR Workflow exposes retry-count via `WorkflowActivityContext` — check the SDK (1.17.6 may require a workaround: an attempt-counter field on `EmbeddingInput` that the workflow increments before re-calling, OR inspection of the activity's `RetryAttempt` property if available). If the SDK does not expose retry count, fall back to: apply jitter unconditionally but cap cumulative per-workflow jitter budget at 2.5 s via an actor/context read — but prefer the first option.

        ```csharp
        public sealed class GenerateEmbeddingActivity : WorkflowActivity<EmbeddingInput, EmbeddingResult>
        {
            private readonly IActorProxyFactory _actorProxyFactory;
            private readonly EmbeddingClient _embeddingClient;
            private readonly IJitterSource _jitterSource;
            private readonly ILogger<GenerateEmbeddingActivity> _logger;

            public GenerateEmbeddingActivity(
                EmbeddingClient embeddingClient,
                IActorProxyFactory actorProxyFactory,
                IJitterSource jitterSource,
                ILogger<GenerateEmbeddingActivity> logger) { /* assign */ }

            public override async Task<EmbeddingResult> RunAsync(WorkflowActivityContext context, EmbeddingInput input)
            {
                // ... existing argument checks, config fetch, SetCeilingAsync, TryConsumeAsync ...
                bool allowed = await rateLimiter.TryConsumeAsync().ConfigureAwait(false);
                if (!allowed)
                {
                    RateLimitingLog.LogRateLimitExceededLocally(_logger, input.TenantId);
                    throw new EmbeddingRateLimitException(input.TenantId);
                }

                // Jitter applied ONLY when this is a retry attempt — desynchronizes multi-tenant retries
                // after a provider outage (NFR22) without wasting time on happy-path first attempts.
                // Expected source: WorkflowActivityContext.RetryAttempt (DAPR 1.17.6); fall back to an
                // attempt-counter field on EmbeddingInput set by the workflow if the SDK property is absent.
                if (IsRetryAttempt(context, input))
                {
                    int jitterMs = _jitterSource.NextMilliseconds(500);
                    if (jitterMs > 0)
                    {
                        await Task.Delay(jitterMs, CancellationToken.None).ConfigureAwait(false);
                    }
                }

                try
                {
                    float[] vector = await _embeddingClient
                        .GenerateAsync(input.ContentText, input.TenantId, config, CancellationToken.None)
                        .ConfigureAwait(false);
                    return new EmbeddingResult(vector, $"{config.Provider}:{config.Model}", config.Dimensions) { Model = config.Model };
                }
                catch (EmbeddingRateLimitException ex)
                {
                    // Provider 429: parse Retry-After (default 30), update actor, re-throw for workflow retry.
                    int retryAfter = ExtractRetryAfterSeconds(ex); // may expose from EmbeddingApiException or EmbeddingClient
                    RateLimitingLog.LogProviderRateLimitReceived(_logger, input.TenantId, retryAfter);
                    await rateLimiter.ReportRateLimitedAsync(retryAfter).ConfigureAwait(false);
                    throw;
                }
            }

            private static int ExtractRetryAfterSeconds(EmbeddingRateLimitException ex)
                => ex.RetryAfterSeconds > 0 ? ex.RetryAfterSeconds : 30;
        }
        ```

    - [x] 2.4 Extend `src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitException.cs` with a `public int RetryAfterSeconds { get; init; } = 0;` property (additive). Modify `EmbeddingClient.HandleEmbeddingResponseAsync` (`EmbeddingClient.cs:140`) to parse the `Retry-After` response header **before** throwing:

        ```csharp
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            int retryAfter = ParseRetryAfterSeconds(response.Headers.RetryAfter);
            throw new EmbeddingRateLimitException(tenantId) { RetryAfterSeconds = retryAfter };
        }

        private static int ParseRetryAfterSeconds(RetryConditionHeaderValue? header)
        {
            if (header is null) return 0;
            if (header.Delta.HasValue) return (int)Math.Clamp(header.Delta.Value.TotalSeconds, 1, 3600);
            if (header.Date.HasValue)
            {
                double seconds = (header.Date.Value - DateTimeOffset.UtcNow).TotalSeconds;
                return seconds > 0 ? (int)Math.Clamp(seconds, 1, 3600) : 0;
            }
            return 0;
        }
        ```

        Return `0` means "header missing / unparseable", which the activity maps to the 30 s default.

    - [x] 2.5 Update `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` (create if missing):
        - **Deterministic jitter via `FakeJitterSource` returning a constant 123 ms.** Assert `Task.Delay(123)` was observed (inject `TimeProvider.System`'s `Delay` or measure elapsed ≥ 100 ms).
        - **429 with `Retry-After: 60`** — mock `EmbeddingClient.GenerateAsync` to throw `new EmbeddingRateLimitException("t1") { RetryAfterSeconds = 60 }`, assert `rateLimiter.ReportRateLimitedAsync(60)` called, assert the exception is re-thrown.
        - **429 with no `Retry-After`** — throw `EmbeddingRateLimitException("t1")` (defaults `RetryAfterSeconds=0`), assert `ReportRateLimitedAsync(30)` called (the default).
        - **Local throttle (`TryConsumeAsync=false`)** — asserts `EmbeddingRateLimitException` thrown without calling `ReportRateLimitedAsync` (no provider call happened).
        - **Happy path** — `TryConsumeAsync=true`, provider returns 200, assert returned `EmbeddingResult.Vector.Length == dimensions`.
    - [x] 2.6 In `tests/Hexalith.Memories.Server.Tests/Ingestion/` add `EmbeddingClientRetryAfterParsingTests.cs` with `[Theory]` over:
        - `Retry-After: 30` → `30`.
        - `Retry-After: 0` → clamped to 1.
        - `Retry-After: 5000` → clamped to 3600.
        - `Retry-After: Wed, 21 Oct 2026 07:28:00 GMT` (HTTP-date in future) → parsed delta.
        - `Retry-After: Wed, 21 Oct 2020 07:28:00 GMT` (past date) → 0.
        - No header → 0.
        - Malformed header (`Retry-After: banana`) → 0.

- [x] Task 3: Implement `PerTenantConcurrencyGate` (AC: #2, #5, #6, #9)
    - [x] 3.1 Create `src/Hexalith.Memories.Server/Ingestion/PerTenantConcurrencyGate.cs`:

        ```csharp
        public sealed class PerTenantConcurrencyGate : IAsyncDisposable
        {
            private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new(StringComparer.Ordinal);
            private readonly IngestionSettings _settings;
            private readonly ILogger<PerTenantConcurrencyGate> _logger;
            private bool _disposed;
            private bool _clampWarningEmitted;

            public PerTenantConcurrencyGate(IOptions<IngestionSettings> options, ILogger<PerTenantConcurrencyGate> logger)
            {
                _settings = options.Value;
                _logger = logger;
            }

            public async Task<IAsyncDisposable> AcquireAsync(string tenantId, CancellationToken ct)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

                // Clamp concurrency to processor count as an upper safety bound.
                // Misconfigured values >ProcessorCount thrash page cache (Kreuzberg OCR) and increase
                // context-switching cost without throughput gain. Warn once per process on clamp.
                int requested = _settings.PerTenantExtractionConcurrency;
                int bounded = Math.Min(requested, Environment.ProcessorCount);
                if (requested > Environment.ProcessorCount && !_clampWarningEmitted)
                {
                    _clampWarningEmitted = true;
                    _logger.LogWarning(
                        "PerTenantExtractionConcurrency={Requested} exceeds Environment.ProcessorCount={ProcessorCount}; clamped to {Bounded}. See docs/operations/rate-limiting.md.",
                        requested, Environment.ProcessorCount, bounded);
                }

                SemaphoreSlim semaphore = _semaphores.GetOrAdd(tenantId,
                    _ => new SemaphoreSlim(bounded, bounded));

                int queued = _settings.PerTenantExtractionConcurrency - semaphore.CurrentCount;
                if (queued >= _settings.PerTenantExtractionConcurrency)
                {
                    RateLimitingLog.LogExtractionGateContended(_logger, tenantId, queued);
                }

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_settings.ExtractionGateAcquireTimeoutSeconds));

                try
                {
                    await semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    RateLimitingLog.LogExtractionGateTimeout(_logger, tenantId, _settings.ExtractionGateAcquireTimeoutSeconds);
                    throw new TimeoutException($"Failed to acquire per-tenant extraction gate for tenant '{tenantId}' within {_settings.ExtractionGateAcquireTimeoutSeconds}s.");
                }

                RateLimitingLog.LogExtractionGateAcquired(_logger, tenantId, semaphore.CurrentCount);
                return new GateLease(semaphore, tenantId, _logger);
            }

            public int GetAvailableCount(string tenantId) =>
                _semaphores.TryGetValue(tenantId, out SemaphoreSlim? semaphore)
                    ? semaphore.CurrentCount
                    : _settings.PerTenantExtractionConcurrency;

            public async ValueTask DisposeAsync()
            {
                if (_disposed) return;
                _disposed = true;
                foreach (SemaphoreSlim s in _semaphores.Values) s.Dispose();
                await ValueTask.CompletedTask;
            }

            private sealed class GateLease : IAsyncDisposable
            {
                private readonly SemaphoreSlim _semaphore;
                private readonly string _tenantId;
                private readonly ILogger _logger;
                private int _disposed;

                public GateLease(SemaphoreSlim semaphore, string tenantId, ILogger logger)
                { _semaphore = semaphore; _tenantId = tenantId; _logger = logger; }

                public ValueTask DisposeAsync()
                {
                    if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    {
                        _semaphore.Release();
                    }
                    return ValueTask.CompletedTask;
                }
            }
        }
        ```

        **Design notes:**
        - `StringComparer.Ordinal` for tenant ID keys (tenant IDs are case-sensitive per architecture §D8).
        - `GetOrAdd` creates one `SemaphoreSlim` per tenant lazily; no explicit eviction because tenant IDs are bounded in practice (low hundreds for MVP) and each `SemaphoreSlim` is tiny (~100 B).
        - The gate is a **process-local** singleton; **horizontal scale-out is NOT in scope for MVP** (per architecture §`TenantInfrastructureResolver` comment: single impl until multi-instance scaling needed). Document in Dev Notes.
        - `ExtractionGateAcquireTimeoutSeconds` default **300** (5 min) — long enough to ride through normal batch queuing, short enough that a stuck gate fails visibly. Configurable.
        - `IAsyncDisposable` on the gate itself so DI can dispose semaphores on shutdown.

    - [x] 3.2 Extend `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` (created in 6.1):
        ```csharp
        public sealed record IngestionSettings
        {
            // ... existing 6.1 fields (AllowedDirectoryRoots, MaxBatchSize, etc.) ...
            public int PerTenantExtractionConcurrency { get; init; } = 4;
            public int ExtractionGateAcquireTimeoutSeconds { get; init; } = 300;
        }
        ```
        **Check the actual 6.1 shape first** — if `IngestionSettings` is a `class` with `get; set;`, preserve that shape; do not switch to `record`. The 6.1 story used a record; verify via `Read`.
    - [x] 3.3 Register in `Program.cs` DI container (or `MemoriesServerServiceCollectionExtensions` if that's where 6.1 put it):
        ```csharp
        services.AddSingleton<PerTenantConcurrencyGate>();
        ```
        Add `appsettings.json` defaults under the `Ingestion` section:
        ```json
        "PerTenantExtractionConcurrency": 4,
        "ExtractionGateAcquireTimeoutSeconds": 300
        ```
    - [x] 3.4 Modify `src/Hexalith.Memories.Server/Activities/Ingestion/ExtractContentActivity.cs` to acquire the gate before Kreuzberg:
        ```csharp
        public sealed class ExtractContentActivity(/* existing deps */, PerTenantConcurrencyGate gate) : WorkflowActivity<ExtractionInput, ExtractionResult>
        {
            public override async Task<ExtractionResult> RunAsync(WorkflowActivityContext context, ExtractionInput input)
            {
                ArgumentNullException.ThrowIfNull(input);
                // extract tenantId from WorkflowActivityContext / input — confirm which; ExtractionInput may not carry it today
                string tenantId = GetTenantIdForContext(context, input);
                await using IAsyncDisposable lease = await gate.AcquireAsync(tenantId, CancellationToken.None).ConfigureAwait(false);
                // ... existing Kreuzberg call ...
            }
        }
        ```
        **IMPORTANT**: `ExtractionInput` in the current code **does not carry `TenantId`** (verified at `Contracts/V1/ExtractionInput.cs`). Two options:
        - **Option A (preferred):** add `TenantId` to `ExtractionInput`. Breaking change, but the 6.1 story already changed `IngestionInput.ContentBytes` to nullable, and `ExtractionInput` is the narrow workflow record. Update `IngestionWorkflow.cs:111-114` to pass `input.TenantId`. Register the new shape in `MemoriesJsonContext`. Trivial.
        - **Option B:** read `tenantId` from the workflow context metadata — DAPR Workflow does NOT propagate arbitrary metadata reliably into activity context, so this is **NOT recommended**.
        - **Choose Option A.** List this as a Breaking Change addition.
    - [x] 3.5 Modify `src/Hexalith.Memories.Server/Activities/Ingestion/FetchUrlActivity.cs` (created in 6.1) similarly:
        ```csharp
        public sealed class FetchUrlActivity(/* existing deps */, PerTenantConcurrencyGate gate) : WorkflowActivity<FetchUrlInput, UrlFetchResult>
        {
            public override async Task<UrlFetchResult> RunAsync(WorkflowActivityContext context, FetchUrlInput input)
            {
                string tenantId = ResolveTenantId(context, input); // see FetchUrlInput shape
                await using IAsyncDisposable lease = await gate.AcquireAsync(tenantId, CancellationToken.None).ConfigureAwait(false);
                // ... existing fetch call ...
            }
        }
        ```
        **`FetchUrlInput` shape:** `(string Url, string MemoryUnitId)` per 6.1 Task 2.1 — NO `TenantId` field. Extend to `(string Url, string MemoryUnitId, string TenantId)`. Update `IngestionWorkflow.cs` (the workflow passes it) and `MemoriesJsonContext`.
    - [x] 3.6 Update `IngestionWorkflow.cs:97` to pass `input.TenantId` in the new `FetchUrlInput(input.SourceUri, memoryUnitId, input.TenantId)` and lines ~113 to pass `ExtractionInput(...) with TenantId = input.TenantId`. Re-run workflow tests.
    - [x] 3.7 Create `tests/Hexalith.Memories.Server.Tests/Ingestion/PerTenantConcurrencyGateTests.cs`:
        - **Basic:** `AcquireAsync("t1")` 5 times with `max=3` → first 3 complete immediately, 4th and 5th block until first 2 disposed.
        - **Per-tenant isolation:** 3 acquires for `t1` + 3 acquires for `t2` with `max=2` → 2 of each are in-flight (4 total), 1 of each blocks.
        - **Release on dispose:** acquire, dispose, acquire again → second acquire does NOT block.
        - **Release on exception in body** (Quinn's addition): acquire inside `await using`, throw from the body, catch outside, then re-acquire for the same tenant → second acquire does NOT block. Pins the `finally`-semantics of the `IAsyncDisposable` lease; catches any future refactor that accidentally moves the release out of a guaranteed-run path.
        - **Timeout:** set `ExtractionGateAcquireTimeoutSeconds=1`, saturate tenant, next acquire throws `TimeoutException` within an upper bound of 5 s (CI-safe tolerance per Quinn's guidance — Windows CI runners have been observed dropping 2+ s under load).
        - **Cancellation:** caller cancels token → `OperationCanceledException` thrown; the semaphore budget is NOT consumed.
        - **Concurrent GetOrAdd:** 100 parallel acquires for different tenant IDs → all complete in-flight (no shared gate bottleneck).
          Use `TimeSpan` assertions with up to 5 s tolerance on timing-sensitive cases (5 s covers both Linux and Windows CI noise).
    - [x] 3.8 Extend `IntegrationTests/Ingestion/DirectoryIngestionIntegrationTests.cs` (6.1 baseline) with a skipped `[Fact(Skip)]` scenario: 500-file batch for `t1` + 5 single ingests for `t2`, assert `t2`'s P50 ≤ 2× baseline. The baseline is measured in a "control" run inside the same test (first 5 single ingests with no `t1` batch). Tag the test `"RequiresAspireFixture"`.

- [x] Task 4: Structured logging events 6201–6206 (AC: #3, #9)
    - [x] 4.1 Create `src/Hexalith.Memories.Server/Ingestion/RateLimitingLog.cs` — mirror the 6.1 `IngestionEndpointLog.cs` pattern (static partial class with `[LoggerMessage]` attributes). Event IDs:

        ```csharp
        public static partial class RateLimitingLog
        {
            [LoggerMessage(EventId = 6201, Level = LogLevel.Warning,
                Message = "Rate limit exceeded locally for tenant {TenantId} (actor refused consume).")]
            public static partial void LogRateLimitExceededLocally(ILogger logger, string tenantId);

            [LoggerMessage(EventId = 6202, Level = LogLevel.Warning,
                Message = "Provider rate limit received for tenant {TenantId}, Retry-After={RetryAfterSeconds}s.")]
            public static partial void LogProviderRateLimitReceived(ILogger logger, string tenantId, int retryAfterSeconds);

            [LoggerMessage(EventId = 6203, Level = LogLevel.Information,
                Message = "Rate limit actor updated for tenant {TenantId} — remaining={Remaining}, windowStart={WindowStart}.")]
            public static partial void LogRateLimitActorUpdated(ILogger logger, string tenantId, int remaining, DateTime windowStart);

            [LoggerMessage(EventId = 6204, Level = LogLevel.Debug,
                Message = "Extraction gate acquired for tenant {TenantId} — available={AvailableCount}.")]
            public static partial void LogExtractionGateAcquired(ILogger logger, string tenantId, int availableCount);

            [LoggerMessage(EventId = 6205, Level = LogLevel.Information,
                Message = "Extraction gate contended for tenant {TenantId} — queueDepth={QueueDepth}.")]
            public static partial void LogExtractionGateContended(ILogger logger, string tenantId, int queueDepth);

            [LoggerMessage(EventId = 6206, Level = LogLevel.Warning,
                Message = "Extraction gate acquisition timed out for tenant {TenantId} after {TimeoutSeconds}s.")]
            public static partial void LogExtractionGateTimeout(ILogger logger, string tenantId, int timeoutSeconds);
        }
        ```

        **Event ID allocation:** 6.1 reserved 6101–6108 (per its Reference: Log Events section). 6.2 takes 6201–6206. Leave 6109–6199 for future 6.1 hotfixes.

    - [x] 4.2 Unit test in `tests/Hexalith.Memories.Server.Tests/Ingestion/RateLimitingLogTests.cs` using `CapturingLogger<T>` test fixture (established in 5.6 / 6.1). Assert each event fires with correct EventId and state fields.

- [x] Task 5: `ReportRateLimitedAsync` integration with `GenerateEmbeddingActivity` + retry (AC: #3, #4, #11)
    - [x] 5.1 After Task 2 is complete, run `dotnet test` scoped to `Hexalith.Memories.Server.Tests` to confirm the activity-level unit tests in Task 2.5 all pass. Primary 429 verification is via the unit test (mocked `EmbeddingClient`) — no dev-only "force 429" flag ships in 6.2. End-to-end 429 coverage is the `[Fact(Skip)]` integration test (Task 8.1) and is unskipped by **Story 6.3** (retry & failure visibility) once its retry harness provides a deterministic 429-producing fixture.
    - [x] 5.2 Verify `IngestionWorkflow.cs` catch-all correctly attaches `FailureDetails` for `EmbeddingRateLimitException` after retry exhaustion. The existing `AttachFailureDetails` captures `exception.GetType().Name` as `ErrorCode`, so `FailureDetails.ErrorCode="EmbeddingRateLimitException"` is correct and tested. **No workflow changes.**

- [x] Task 6: Regression guard + baseline re-run (AC: #7, #12)
    - [x] 6.1 **Baseline measurement (before any 6.2 code changes):** run `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` from repo root. Record: total passing, total failing, total skipped. Expected to match 6.1 Dev Agent Record baseline (~1191 passing in Server+Contracts; 2 documented `SaveDedupKeyActivityTests` failures). Record in Debug Log References.
    - [x] 6.2 **Post-change validation:** run `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` after all tasks. Expected: ~1210+ passing (20 new tests minimum), same 2 documented baseline failures remain, zero new failures. If a test fails that didn't fail at baseline, **STOP and investigate** before marking tasks complete.
    - [x] 6.3 **AC7 regression test:** in `GenerateEmbeddingActivityTests` (Task 2.5), add a test "SetCeilingAsync reflects updated TenantConfig":
        - Mock `ITenantConfigurationActor.GetEmbeddingConfigAsync` to return a config with `RateLimitPerMinute=500`.
        - Run the activity; assert `rateLimiter.SetCeilingAsync(500)` called.
        - Re-mock to return `RateLimitPerMinute=100`.
        - Run again; assert `rateLimiter.SetCeilingAsync(100)` called on the second run.
          This pins the call order (`SetCeilingAsync` → `TryConsumeAsync`) and guards against a future dev agent "optimizing away" the `SetCeilingAsync` call.

- [x] Task 7: Documentation — shared provider quota limitation (AC: #8)
    - [x] 7.1 Create `docs/operations/rate-limiting.md` (operator-audience doc — create folder if absent):

        ```markdown
        # Rate Limiting — Per-Tenant and Shared Provider Quotas

        ## Per-tenant ceilings (Story 6.2)

        Each tenant has an `EmbeddingRateLimiterActor` (Actor ID = tenant ID). It enforces a `RateLimitPerMinute` ceiling from `TenantEmbeddingConfig` (Story 1.7 / 5.5). The actor is independent per tenant; one tenant cannot consume another's budget.

        ## Shared provider quota (Known Limitation)

        If multiple tenants share the same `TenantEmbeddingConfig.ApiSecretKeyName` (i.e., the same provider API key), they share the provider-level rate limit. Google `text-embedding-004` free tier is 1500 req/min TOTAL — not per tenant.

        **Mitigation for MVP:** assign distinct `ApiSecretKeyName` values per tenant (via the operator secrets store). The per-tenant ceiling enforced by the actor is a _protection_, not an _isolation_, when keys are shared.

        **Phase 3 roadmap:** a cross-tenant `SharedEmbeddingRateLimiterActor` will coordinate the shared-key quota across tenants (see architecture §D41 "Shared embedding rate limiter").

        ## Provider 429 handling (Story 6.2)

        On a provider HTTP 429 response, `GenerateEmbeddingActivity` parses `Retry-After` (seconds or HTTP-date), invokes `IEmbeddingRateLimiterActor.ReportRateLimitedAsync(retryAfterSeconds)` which zeroes the tenant's budget for the reported interval, then re-throws `EmbeddingRateLimitException`. The DAPR Workflow retry policy (5 attempts, exponential backoff, 5 min cap) handles the retry. During the Retry-After window, `TryConsumeAsync` returns `false` immediately — no provider calls happen — so the retry cost is just workflow scheduling overhead.

        ## Per-tenant CPU gate (Story 6.2)

        `PerTenantConcurrencyGate` caps concurrent `ExtractContentActivity` and `FetchUrlActivity` invocations per tenant (default 4). Prevents a tenant's batch from monopolizing the Memories Server extraction threadpool.

        ## Jitter

        `GenerateEmbeddingActivity` adds uniform-random jitter in [0, 500) ms BEFORE the provider call, to desynchronize retries after a provider outage (thundering herd mitigation — NFR22).

        ## Metrics and logs

        Events 6201–6206 (see `RateLimitingLog.cs`). Epic 8 will expose OpenTelemetry counters.
        ```

    - [x] 7.2 Add a link in `README.md` under an "Operations" section. If no such section exists, create one that points to `docs/operations/rate-limiting.md` (verify the README structure first; do not clobber). Not mandatory for DoD but highly recommended (Gate 3 polish).

- [x] Task 8: Integration test scaffolding (AC: #1, #2, #10)
    - [x] 8.1 Extend `tests/Hexalith.Memories.IntegrationTests/Ingestion/` with `RateLimitingIntegrationTests.cs`. Each test MUST use the precise skip reason format: `[Fact(Skip = "Unskipped by Story 6.3 — requires Aspire fixture + 429 test-double from retry harness.")]` so the unskip tracking is greppable.
        - Test 1 — two-tenant isolation (AC1, AC10). Unskipped by **Story 6.3**.
        - Test 2 — 500-file batch vs. single-ingest starvation regression (AC2). Unskipped by **Story 6.3** (same fixture dependency).
        - Test 3 — 429 retry end-to-end (AC3, AC4) — requires deterministic 429-producing provider test double. Unskipped by **Story 6.3** when its retry harness builds the double.
          Use the existing `AspireIngestionPipelineFixture` if available (per 6.1 Dev Agent Record); if not, stub the fixture class with a `throw new NotImplementedException()` and the same Story 6.3 reference in the skip message.
    - [x] 8.2 DO NOT unskip these tests in 6.2 — Story 6.2's DoD is that the tests _compile_ and the _unit tests_ pass. Story 6.3 owns the unskip.

### Review Findings

- [x] [Review][Decision] Follow the Task 4.1 `RateLimitingLog` signature for events `6201`/`6202` — resolved in favor of the slimmer payload because AC3 and AC9 contradict each other, while the concrete Task 4.1 code sample, event table, and implementation all align on the current field set. No patch applied.
- [x] [Review][Patch] Apply jitter only on retry attempts, not on the first embedding call [src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:86]
- [x] [Review][Patch] Clamp and validate per-tenant gate settings before constructing semaphores/timeouts [src/Hexalith.Memories.Server/Ingestion/PerTenantConcurrencyGate.cs:47]
- [x] [Review][Patch] Emit `RateLimitActorUpdated` (6203) when `ReportRateLimitedAsync` persists the new state [src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs:57]
- [x] [Review][Patch] Log actual extraction-gate queue depth instead of reusing the in-flight count [src/Hexalith.Memories.Server/Ingestion/PerTenantConcurrencyGate.cs:53]

## Dev Notes

### First Principles Framing

**What this story IS:** the "ingestion reliability layer" for Gate 3. Stories 6.1 (ingestion surface), 6.2 (reliability), 6.3 (observability), 6.4 (durability) are separable concerns — 6.2 is the **runtime behavior** layer. The code for rate limiting, actor-based throttling, tenant configuration, and workflow retries already exists from Stories 1.4, 1.7, and 5.5. Story 6.2 **does not build these from scratch** — it **refines, tests, and documents** them, adds the 429-feedback loop (new), adds the per-tenant extraction concurrency gate (new), and adds jitter (new). Think of it as "make the existing rate limiter actually work under adversarial load."

**What this story IS NOT:**

- NOT a new rate limiter. The `EmbeddingRateLimiterActor` exists (Story 1.4). 6.2 extends its interface with `ReportRateLimitedAsync` and uses it correctly from the activity.
- NOT a new tenant configuration actor. `TenantConfigurationActor` exists (Story 5.5).
- NOT a new workflow. `IngestionWorkflow` exists (Story 1.6), modified only to pass `TenantId` through `ExtractionInput` / `FetchUrlInput`.
- NOT adaptive or learning rate limits. The ceiling comes from `TenantEmbeddingConfig.RateLimitPerMinute`, operator-set, static per tenant.
- NOT cross-tenant coordination. Shared-key quota management is Phase 3 per architecture §D41.
- NOT a dashboard. Metrics (counters, histograms) land in Epic 8. 6.2 ships structured logs only.
- NOT retry visibility or failure dashboards. That's 6.3.
- NOT pipeline durability beyond what DAPR Workflow already provides. That's 6.4.
- NOT CLI or MCP changes. Epic 7 / Epic 10.

**Mental model for the dev agent:**

- AC1, AC7, AC10 = **verification tests** for existing behavior. You write tests, you don't write code that didn't exist before.
- AC2, AC5, AC6 = **new gate** (`PerTenantConcurrencyGate` + threading through activities). Biggest single code change.
- AC3, AC4, AC11 = **new actor method** (`ReportRateLimitedAsync`) + **activity integration** + jitter. Medium change.
- AC8 = **documentation**. Markdown file, no code.
- AC9 = **logging events**. Copy 6.1's `IngestionEndpointLog.cs` file pattern.
- AC12 = **regression guard**. Run tests before AND after.

**If you find yourself adding a Polly pipeline, a new workflow, a second retry policy, a new actor for extraction throttling, a Redis-backed distributed semaphore, a custom jitter algorithm with state, a metrics SDK integration, or a new HTTP client — STOP. You're over-scoping.**

### Dependencies

- **Story 1.4 (Embedding Generation):** REQUIRED — provides `EmbeddingRateLimiterActor`, `RateLimiterLogic`, `RateLimitState`, `IEmbeddingRateLimiterActor`. Status: done.
- **Story 1.6 (Ingestion Workflow Orchestration):** REQUIRED — provides `IngestionWorkflow` with retry policy, `ExtractContentActivity`. Status: done.
- **Story 1.7 (Embedding Provider Configuration):** REQUIRED — provides `TenantConfigurationActor`, `TenantEmbeddingConfig` with `RateLimitPerMinute` field. Status: done.
- **Story 5.5 (Tenant Configuration & Listing):** REQUIRED — provides `PATCH /api/tenants/{tenantId}/config` which updates `RateLimitPerMinute`, and the `ITenantConfigurationActor.GetEmbeddingConfigAsync` read path exercised by `GenerateEmbeddingActivity`. Status: done.
- **Story 5.6 (Graceful Degradation):** Provides the `[LoggerMessage]` partial-class pattern, the `[Fact(Skip)]` integration test convention, and the "retry-policy is fixed per-workflow, exception-type-agnostic" documented limitation that 6.2 inherits. Status: done or review.
- **Story 6.1 (URL & Directory Ingestion):** REQUIRED — provides `FetchUrlActivity`, `IngestionSettings`, `IngestionEndpointLog` (log ID 6101–6108). 6.2 layers on top. Status: review. **COORDINATION**: 6.1 modifies `ExtractionInput` shape via `IngestionWorkflow.cs:113`; 6.2 adds `TenantId` to `ExtractionInput`. Rebase-first against 6.1 if it lands before 6.2.

### Architecture Compliance

- **FR8 (Per-tenant ingestion load management):** Directly satisfied by `PerTenantConcurrencyGate` (AC2, AC5, AC6).
- **FR69 (Per-tenant rate limit ceilings):** Directly satisfied by the existing `EmbeddingRateLimiterActor` (AC1, AC7). 6.2 verifies and hardens.
- **NFR13 (Per-tenant ingestion pipeline scales independently):** Satisfied by per-tenant semaphore (AC2, AC5, AC6, AC10).
- **NFR22 (Embedding provider integration handles rate limiting gracefully — 429 backoff):** Satisfied by jitter (AC4) + `ReportRateLimitedAsync` (AC3) + existing workflow retry policy (AC11).
- **Architecture §5 (Rate Limiting & Throttling, MVP-critical cross-cutting concern):** "Per-tenant `EmbeddingRateLimiterActor` ... DAPR Workflow retry policies handle 429 responses with exponential backoff per activity. **Pipeline resource isolation covers all stages (extraction, embedding, indexing) — CPU-intensive extraction (PDF, URL fetch) bounded per tenant via workflow concurrency control, not just embedding API calls.** Thundering herd coordination across tenants deferred to Phase 3 — per-tenant jittered retry via workflow retry policies sufficient for MVP." — 6.2 implements the extraction bounding (gate) and jitter.
- **Architecture §D24 (DAPR Actors for per-tenant stateful singletons):** The `EmbeddingRateLimiterActor` is the canonical example. 6.2 extends its interface, does not violate the pattern.
- **Architecture §D25 (Workflow-Actor separation of concerns):** "Workflows orchestrate processes. Actors manage per-entity state (rate limits, cached stats). Activities do I/O." — `ReportRateLimitedAsync` stays in the actor (state); jitter stays in the activity (I/O-adjacent concern); gate stays as a singleton service invoked by activities (activity-adjacent). ✓
- **Architecture §Testability (Actor logic testable without DAPR):** The `RateLimiterLogic` class is a plain C# service; the actor is a thin host. New `ReportRateLimited` logic is added to `RateLimiterLogic` first, tested in isolation, then called from the actor. ✓
- **Architecture §Rate Limiting roadmap:** Shared cross-tenant rate limiter = Phase 3. 6.2 preserves the architectural boundary.

### Existing Infrastructure to Reuse

| Component                               | Location                                                   | Usage in This Story                                                                                                          |
| --------------------------------------- | ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `EmbeddingRateLimiterActor`             | `Server/Actors/EmbeddingRateLimiterActor.cs`               | Extend with `ReportRateLimitedAsync`. Do NOT restructure.                                                                    |
| `IEmbeddingRateLimiterActor`            | `Server/Actors/IEmbeddingRateLimiterActor.cs`              | Add one method.                                                                                                              |
| `RateLimiterLogic`                      | `Server/Actors/RateLimiterLogic.cs`                        | Add pure helper `ReportRateLimited(state, seconds)`.                                                                         |
| `RateLimitState`                        | `Server/Actors/RateLimitState.cs`                          | Unchanged.                                                                                                                   |
| `GenerateEmbeddingActivity`             | `Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` | Add jitter, 429 handler.                                                                                                     |
| `EmbeddingClient`                       | `Server/Ingestion/EmbeddingClient.cs`                      | Parse `Retry-After` in `HandleEmbeddingResponseAsync`.                                                                       |
| `EmbeddingRateLimitException`           | `Server/Ingestion/EmbeddingRateLimitException.cs`          | Add `RetryAfterSeconds` property.                                                                                            |
| `ExtractContentActivity`                | `Server/Activities/Ingestion/ExtractContentActivity.cs`    | Inject `PerTenantConcurrencyGate`; acquire around Kreuzberg call.                                                            |
| `FetchUrlActivity`                      | `Server/Activities/Ingestion/FetchUrlActivity.cs` (6.1)    | Inject `PerTenantConcurrencyGate`; acquire around fetch.                                                                     |
| `IngestionSettings`                     | `Server/Ingestion/IngestionSettings.cs` (6.1)              | Add 2 new fields.                                                                                                            |
| `IngestionWorkflow`                     | `Server/Workflows/IngestionWorkflow.cs`                    | Pass `TenantId` into `ExtractionInput` and `FetchUrlInput`.                                                                  |
| `ExtractionInput`                       | `Contracts/V1/ExtractionInput.cs`                          | Add `TenantId` property (breaking, per Breaking Changes).                                                                    |
| `FetchUrlInput`                         | `Contracts/V1/FetchUrlInput.cs` (6.1)                      | Add `TenantId` property (breaking, per Breaking Changes).                                                                    |
| `TenantConfigurationActor`              | `Server/Actors/TenantConfigurationActor.cs`                | Read `RateLimitPerMinute` via existing `GetEmbeddingConfigAsync`. Unchanged.                                                 |
| `[LoggerMessage]` partial-class pattern | 6.1 `IngestionEndpointLog.cs`                              | Mirror for new `RateLimitingLog.cs`.                                                                                         |
| `CapturingLogger<T>` test fixture       | `tests/` (5.6 / 6.1 precedent)                             | Assert `[LoggerMessage]` calls.                                                                                              |
| `FakeTimeProvider`                      | `Microsoft.Extensions.TimeProvider.Testing` NuGet          | Deterministic time in `RateLimiterLogic` tests. Verify the package is referenced; if not, add to `Directory.Packages.props`. |

### Code Patterns

**Gate acquisition inside an activity:**

```csharp
public override async Task<ExtractionResult> RunAsync(WorkflowActivityContext context, ExtractionInput input)
{
    ArgumentNullException.ThrowIfNull(input);
    ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);

    await using IAsyncDisposable lease = await _gate.AcquireAsync(input.TenantId, CancellationToken.None).ConfigureAwait(false);

    // ... existing extraction logic unchanged ...
}
```

The `await using` pattern guarantees release on success, exception, or cancellation.

**429 feedback in activity:**

```csharp
try
{
    float[] vector = await _embeddingClient.GenerateAsync(...).ConfigureAwait(false);
    // ...
}
catch (EmbeddingRateLimitException ex)
{
    int retryAfter = ex.RetryAfterSeconds > 0 ? ex.RetryAfterSeconds : 30;
    RateLimitingLog.LogProviderRateLimitReceived(_logger, input.TenantId, retryAfter);
    await rateLimiter.ReportRateLimitedAsync(retryAfter).ConfigureAwait(false);
    throw;
}
```

**Jitter before provider call:**

```csharp
int jitterMs = _jitterSource.NextMilliseconds(500);
if (jitterMs > 0)
{
    await Task.Delay(jitterMs, CancellationToken.None).ConfigureAwait(false);
}
```

**Actor state update (pattern from existing `ResetAsync`):**

```csharp
public async Task ReportRateLimitedAsync(int retryAfterSeconds)
{
    RateLimitState state = await GetOrCreateStateAsync().ConfigureAwait(false);
    RateLimitState newState = _logic.ReportRateLimited(state, retryAfterSeconds);
    await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
}
```

### Retry & Jitter Semantics

**Workflow retry policy (unchanged from 5.6 / 6.1):**

- `maxNumberOfAttempts = 5`
- `firstRetryInterval = 2s`
- `backoffCoefficient = 1.5`
- `maxRetryInterval = 5 min`
- Schedule: 2s, 3s, 4.5s, 6.75s, 10.125s → total ~26.4s across 5 attempts (capped at 5 min).
- **No jitter at policy level** — DAPR SDK 1.17.6 does not expose a `WorkflowRetryPolicy.Jitter` parameter.

**Application-level jitter (new in 6.2):**

- Applied ONLY in `GenerateEmbeddingActivity`, BEFORE the provider call.
- Uniform-random in `[0, 500)` ms.
- Purpose: desynchronize multiple tenants' retries after a provider outage — thundering herd mitigation (NFR22).
- Not applied in `ExtractContentActivity` / `FetchUrlActivity` — those aren't the thundering-herd risk; extraction is CPU-bound locally, fetch hits arbitrary URLs.

**Retry-After feedback window:**

- Provider 429 → `Retry-After: 60` → actor `WindowStart = now + 60s`, `Remaining = 0`.
- For 60 s, `TryConsumeAsync` returns `false` → activity throws `EmbeddingRateLimitException` → workflow retry fires at `T+2s`, `T+5s`, `T+9.5s`, ... → activity fails fast each time (no provider call).
- At `T+60s + 1 min` (per `RateLimiterLogic.WindowDuration`), the window naturally re-fills.
- **Worst case:** `Retry-After: 30` → workflow retries at T+2, T+5, T+9.5 (all fail fast); at T+16.25 the actor may have re-filled (if 30 s > 16.25 — depends on when the 429 landed). The workflow has 5 attempts totaling ~26.4 s; if all fail, memory unit → `Failed`.
- **If `Retry-After > ~26 s`:** the workflow WILL exhaust retries and mark the unit `Failed`. This is **accepted MVP behavior** — Story 6.3 will add re-ingestion, making the "failure" recoverable. Document in Known MVP Limitations.

### Per-Tenant Extraction Gate — Design Rationale

**Why a singleton in-process semaphore, not an actor?**

- Extraction is in-process (Kreuzberg) — the bottleneck is local CPU / memory, not shared Redis / DAPR state.
- A DAPR actor gate would require a roundtrip to Redis per acquire (~5-10 ms per activity invocation × 500 files = measurable overhead) vs. sub-microsecond `SemaphoreSlim.WaitAsync`.
- Horizontal scale-out is explicitly out of scope for MVP (per architecture §`TenantInfrastructureResolver`: "single impl until multi-instance scaling needed"). When scale-out arrives, the gate migrates to a distributed semaphore (actor or Redis lock) — **documented extension point**.
- Process restart: the gate is rebuilt (empty dictionary), and in-flight workflow activities that are replayed by DAPR re-acquire naturally. **No state to persist.**

**Why default = 4?**

- `Environment.ProcessorCount` on a typical dev machine is 8–16; 4 per tenant leaves headroom for other tenants + system work.
- Kreuzberg extraction is moderate-CPU for text, heavy for PDF OCR; 4 parallel PDFs saturate a modest CPU.
- Operator can raise via `Ingestion:PerTenantExtractionConcurrency`.
- A future adaptive gate (scaling with load) is Phase 2+.

**Operator tuning heuristic (add to `docs/operations/rate-limiting.md`):**

- **Baseline:** `min(4, Environment.ProcessorCount / 2)` per tenant. Default 4 fits a typical 8-core dev/CI box with 2 tenants.
- **Raise** when: (a) the Aspire dashboard shows `ExtractionGateContended` (event 6205) firing >10 /min for a tenant with CPU headroom (<60 % process CPU), OR (b) a single-tenant deployment where starvation is not a concern.
- **Lower** when: (a) multi-tenant deployment with PDF-heavy batches saturates CPU (>85 % sustained), OR (b) embedding provider 429s correlate with extraction spikes (indicates embedding is paced by extraction concurrency).
- **Upper bound:** `Environment.ProcessorCount` — beyond that you're just increasing context-switching cost.
- **Per-tenant override is NOT supported in MVP** — the setting is global. Per-tenant overrides arrive with Epic 8 tenant-level tuning.

**Why apply to both `ExtractContentActivity` AND `FetchUrlActivity`?**

- Fetch is I/O-bound (network) but the downstream Kreuzberg-in-process extraction is CPU. Capping both types under one gate keeps total per-tenant resource use predictable without two separate settings.
- If separated, a batch of 500 URL fetches could all queue against extraction — the single gate prevents that coupling.

**Why per-tenant dictionary, not per-tenant-and-activity?**

- Simpler. Tenant is the isolation boundary we care about (PRD/FR8). Activity-type separation would be micro-optimization.

### Previous Story Learnings (from 5.6 & 6.1)

- **Do NOT extend `ErrorResponse`** — preserve `(code, message, suggestion)`. No 6.2 changes to `ErrorResponse`.
- **Event IDs are pinned** for dashboard stability: 5501, 5601–5603 (5.6), 6101–6108 (6.1). 6.2 pins 6201–6206. Never reuse.
- **Host `[LoggerMessage]` partial methods in a dedicated class** (6.1 `IngestionEndpointLog`). 6.2 creates `RateLimitingLog` in `Server/Ingestion/`.
- **Anti-pattern #3 from 5.6: don't create helpers for 2-site inline blocks**. The gate acquisition is 2 sites (`ExtractContentActivity`, `FetchUrlActivity`) — BUT the `PerTenantConcurrencyGate` service IS the helper (it's a legitimate abstraction with state: the dictionary of semaphores). Inline acquisition code is what 5.6 warns against; a dedicated stateful service is not an inline duplication. Accept the abstraction.
- **`CapturingLogger<TCategory>` test fixture** for `[LoggerMessage]` assertions — reuse.
- **`[Fact(Skip)]` integration tests** — reuse; tracker reference to Story 6.3 retry harness or Epic 7 e2e harness.
- **Pre-existing `SaveDedupKeyActivityTests` 2 failures on baseline `b33cd71`** — ignore; document. Do NOT fix in 6.2.
- **DAPR Workflow retry policy cannot short-circuit on error type** (5.6 Known Limitations). 6.2 inherits and documents.
- **Workflow activity input/output types MUST be serializable via `MemoriesJsonContext` (AOT)** — when adding `TenantId` to `ExtractionInput` / `FetchUrlInput`, regenerate the source-gen via a build; round-trip test the new shapes (Task 2.6 / Task 3.7 test scaffolding).
- **`SetCeilingAsync` called every embedding invocation** (`GenerateEmbeddingActivity.cs:58`): hot-path cost is one actor roundtrip (~5 ms). Known inefficiency, accepted for MVP — AC7's regression guard pins the behavior so a future refactor consciously changes it.

### Git Intelligence

Recent commits (from conversation `git status`):

- `948b8a5` — search endpoint degradation logging (5.6) — **sets the logging pattern** 6.2 mirrors.
- `30f86c2` — tenant endpoint handlers (5.5) — related to tenant configuration; 6.2 reads `TenantEmbeddingConfig.RateLimitPerMinute` via the same pathway.
- `24f5ff7` — tenant configuration & metrics (5.5) — establishes `TenantEmbeddingConfig.RateLimitPerMinute` field.
- `b33cd71` — DAPR configuration & tenant mismatch monitoring — unrelated to 6.2.
- `9cd3b97` — `TenantStatusGuard.ToHttpResult` (5.4) — used by ingestion endpoints; unchanged by 6.2.

**Git status snapshot indicates 6.1 changes are uncommitted in the working tree** (files: `IngestionWorkflow.cs`, `IngestionInput.cs`, `Program.cs`, new 6.1 Contracts + Ingestion files). **Dev agent coordination:** do NOT attempt to commit 6.2 before 6.1 is committed. Check `sprint-status.yaml` for `6-1-*` status = `done` before merging 6.2 changes.

### Anti-Patterns to Avoid

1. **Do NOT add a second retry policy for `EmbeddingRateLimitException`.** The existing workflow retry policy handles it. Adding Polly, a custom loop, or a per-exception `WorkflowRetryPolicy` violates AC11 and creates dueling retry budgets.
2. **Do NOT persist the `PerTenantConcurrencyGate` state to Redis.** It's in-process-only by design. Distributed semaphore is Phase 2.
3. **Do NOT evict entries from `_semaphores` dictionary.** Tenant IDs are bounded; premature eviction causes acquire failures if a tenant's gate is disposed mid-batch. If memory becomes a concern, eviction is Phase 2.
4. **Do NOT change `WindowDuration = TimeSpan.FromMinutes(1)`** in `RateLimiterLogic`. It's architectural (provider rate limits are per-minute). Making it configurable is Phase 2.
5. **Do NOT introduce a `SharedRateLimiter` or cross-tenant actor.** Phase 3 per architecture §D41. MVP: per-tenant only.
6. **Do NOT parse `Retry-After` inside the activity.** Parse at the HTTP boundary (`EmbeddingClient.HandleEmbeddingResponseAsync`) and propagate via `EmbeddingRateLimitException.RetryAfterSeconds`. Keeps the activity pure w.r.t. HTTP.
7. **Do NOT use `Task.Run` inside activity handlers.** It breaks DAPR Workflow replay semantics. Use `await` directly.
8. **Do NOT log secret key values** in any `RateLimitingLog` event. Only tenant IDs, counts, timestamps.
9. **Do NOT reuse `EmbeddingApiException`** for the 429 path. `EmbeddingRateLimitException` is the distinct type; `EmbeddingApiException` covers 4xx/5xx non-429 errors (existing in `EmbeddingClient.cs:149`).
10. **Do NOT introduce a new metric library (prometheus-net, OpenTelemetry Metrics API, etc.).** Structured logs are the MVP observability surface. Metrics counters are Epic 8.
11. **Do NOT hold the gate across the embedding API call.** The gate is for CPU-bound extraction; holding it across the embedding HTTP call would serialize embedding API concurrency per tenant, which is exactly what the rate limiter actor handles better (quota-based, not concurrency-based). Release the gate before `GenerateEmbeddingActivity` runs — i.e., the gate lives in `ExtractContentActivity` / `FetchUrlActivity` only.
12. **Do NOT mock `Random.Shared` in unit tests.** Inject `IJitterSource` with a deterministic fake. `Random.Shared` is process-global and test-hostile.
13. **Do NOT catch `OperationCanceledException` broadly inside `PerTenantConcurrencyGate.AcquireAsync`.** The gate's timeout-driven cancellation is distinguished from the caller's cancellation via `when (!ct.IsCancellationRequested)` (see Task 3.1 code). Blanket catch masks caller cancellation.

### Per-Tenant Isolation Boundaries (What 6.2 Does NOT Bound)

Story 6.2 isolates per-tenant on **two axes**: (a) embedding API quota (via `EmbeddingRateLimiterActor`) and (b) concurrent CPU-bound extraction (via `PerTenantConcurrencyGate`). Two other resources are deliberately **not** bounded per tenant and remain shared across the Memories Server process:

- **In-memory payload volume.** A tenant's 500-file directory batch (6.1) reads up to 500 × 1 MB = 500 MB of `ContentBytes` into memory concurrently before scheduling workflows. Nothing in 6.2 bounds this. Operator mitigation: lower `Ingestion:MaxBatchSize` (default 500, from 6.1) if memory pressure observed. Phase 2: streaming ingest + back-pressure.
- **Redis connection pool.** All tenants share the `StackExchange.Redis.ConnectionMultiplexer` (Story 1.5). A tenant's 500-file batch floods indexing activities against the same pool. Redis itself is the backpressure mechanism (command queuing). Phase 2+: per-tenant connection pool or connection-per-tenant partitioning.
- **Shared `HttpClient` connection pool.** The named URL-fetcher HttpClient (`"memories-url-fetcher"`, 6.1) is shared across tenants. A tenant hitting a slow host starves other tenants' fetches to the same host because `SocketsHttpHandler.MaxConnectionsPerServer` defaults to unlimited — but sockets, DNS cache, and TLS handshake state are process-global. Red-team scenario: tenant A submits 10 000 URLs to a 30 s-latency host; tenant B's fetches to the same host share the pool. Phase 2+ mitigation: per-tenant HttpClient with bounded `MaxConnectionsPerServer`.

These boundaries are **accepted MVP compromises** — the thesis of 6.2 is "quota + CPU isolation" (FR8, NFR13). Epic 8 observability surfaces the symptoms; Phase 2 addresses the causes.

### Rejected Alternatives (Do Not Propose)

- **DAPR actor-based gate (`ExtractionGateActor`):** rejected — 5–10 ms per acquire roundtrip × 500 files = 2.5–5 s overhead, no benefit over in-process for MVP (single-instance deployment).
- **Redis-backed distributed semaphore (Redlock):** rejected — adds Redlock library, clock-drift failure modes, out of MVP scope. Phase 2 if horizontal scale-out arrives.
- **Global DAPR `maxConcurrentActivityInvocations`:** rejected — global, not per-tenant; doesn't satisfy FR8.
- **Folding the gate into `TenantInfrastructureResolver`:** rejected — couples unrelated concerns (resolver is for backend connection details, not CPU bounds). Different abstraction, different lifetime.

### Known MVP Limitations

- **No cross-tenant (shared API key) coordination.** Multiple tenants sharing an `ApiSecretKeyName` share the provider's effective quota; per-tenant actors cannot coordinate. Operator mitigation: assign distinct API keys per tenant. Phase 3 → `SharedEmbeddingRateLimiterActor`.
- **No adaptive / learning ceiling.** `RateLimitPerMinute` is static per tenant. Phase 2+.
- **No retry-after `> ~26s` recoverable path.** Workflow exhausts retries; unit → `Failed`. **Operationally significant:** a prolonged provider outage (`Retry-After: 900` or unavailable for >30 s) will mark every in-flight ingestion `Failed` because the 5-retry budget exhausts in ~26 s. For a tenant mid-batch of 10 000 files, this is a mass-failure event. The **only** recovery path is Story 6.3's re-ingestion UX; until 6.3 ships, operators must treat prolonged provider outages as an event requiring manual replay of the input batch. Track this as a **pre-production risk** in project notes.
- **Shared HttpClient connection pool across tenants.** The named `"memories-url-fetcher"` HttpClient (6.1) is process-global. Per-tenant isolation of HTTP socket pools is Phase 2+. See "Per-Tenant Isolation Boundaries" section above.
- **Shared Redis connection pool across tenants.** Same as above — all tenants share the `ConnectionMultiplexer`. Phase 2+ partitioning.
- **In-memory batch payload not bounded per tenant.** 500 files × 1 MB = up to 500 MB per tenant in-flight. Mitigate via `Ingestion:MaxBatchSize` (6.1 default 500). Phase 2: streaming ingest.
- **Process-local gate.** Horizontal scale-out not in scope; distributed gate is Phase 2.
- **No priority queue.** All activities for a tenant share the same FIFO semaphore. A "high-priority real-time vs. low-priority batch" separation is Phase 2+.
- **`SetCeilingAsync` on every embedding call.** One actor roundtrip per invocation. Hot-path cost ~5 ms. Acceptable for MVP; optimize in Phase 2 by caching the last-seen config and only calling `SetCeilingAsync` on change.
- **No metric counters.** Epic 8.
- **Jitter range is fixed at `[0, 500) ms`.** Not configurable. Adjustable via config is Phase 2.
- **Gate acquire timeout is 5 min default.** Too long → batch stalls silently; too short → false-positive failures under heavy load. 5 min is the conservative MVP choice; tunable via config.
- **`EmbeddingClient.HandleEmbeddingResponseAsync` only handles 429 in the "first" HTTP call path.** The retry-after-unauthorized path (`EmbeddingClient.cs:100-106`) does NOT re-parse `Retry-After` on the retried 401/403 response. Accept — 401/403 aren't rate limits and shouldn't respect `Retry-After`.

### Edge Cases

- **Provider returns 429 with no `Retry-After` header:** activity defaults to `30s`. Actor pauses 30 s. Logged with `retryAfterSeconds=30`.
- **Provider returns 429 with `Retry-After: 0`:** clamped to 1 s in `RateLimiterLogic.ReportRateLimited`. Pause is effectively skipped; workflow retries immediately — acceptable because the actor budget is zero-floored, so `TryConsumeAsync` still returns false on the next attempt until the window truly re-opens.
- **Provider returns 429 with `Retry-After: 3600`:** clamped to 3600 (1 hour). Workflow exhausts 5 retries (~26.4 s) long before the pause ends → unit → `Failed`. This is correct: re-ingestion in Story 6.3 is the recovery path for long outages.
- **Tenant submits batch, then batch is cancelled mid-flight:** workflows receive cancellation. Gate `AcquireAsync` throws `OperationCanceledException`, semaphore slot is NOT consumed (cancellation before `WaitAsync` returns), no leak. In-flight activities that already acquired release on `await using` disposal.
- **Actor receives two `ReportRateLimitedAsync(30)` calls 5 seconds apart:** second call overrides `WindowStart` to `now2 + 30s`. Pause effectively restarts from the second call's timestamp. Acceptable — mirrors provider behavior (each 429 resets the pause).
- **Tenant's ceiling changed from 1500 → 500 mid-batch:** next `SetCeilingAsync(500)` call (next `GenerateEmbeddingActivity` invocation) clamps `Remaining = Math.Min(current, 500)`. If current = 1200, clamped to 500. Subsequent `TryConsumeAsync` calls continue until `Remaining` hits 0. No in-flight call is retroactively throttled.
- **Tenant deletes mid-ingestion:** workflows continue with the stale config until the delete workflow invalidates the state. `TenantStatusGuard` at the endpoint layer rejects new requests (Story 5.4 behavior, unchanged). In-flight workflows complete or fail naturally.
- **Server process restarts:** gate dictionary is empty on startup. In-flight DAPR Workflow history is replayed; activities re-acquire fresh gate entries. No state inconsistency because gate state is ephemeral / advisory.
- **Jitter value is 0:** `Task.Delay(0)` yields immediately — effectively no delay. `IJitterSource.NextMilliseconds` must allow 0 (uniform range `[0, 500)` includes 0).
- **High tenant churn (thousands of short-lived tenants):** each tenant creates one `SemaphoreSlim` in the gate dictionary. At ~100 B per semaphore, 10 000 tenants ≈ 1 MB. Not a concern for MVP. Eviction = Phase 2.
- **Two simultaneous `GetOrAdd` for the same tenant:** `ConcurrentDictionary.GetOrAdd` is thread-safe; the factory may run twice in a race, but only one result is stored. The extra `SemaphoreSlim` becomes garbage. Low-frequency event; accept.
- **`Retry-After` header contains HTTP-date in the past:** `ParseRetryAfterSeconds` returns 0; activity defaults to 30 s.
- **Workflow activity is retried 5 times, each retry gets jitter:** 5 × up to 500 ms = up to 2.5 s cumulative jitter across the retry budget. Does not break the retry budget semantics (jitter is in addition to the policy's intervals). Documented.
- **Cancellation token cancelled while waiting on gate:** `SemaphoreSlim.WaitAsync(ct)` throws `OperationCanceledException`. Not caught by the gate's timeout handler (see `when (!ct.IsCancellationRequested)` guard). Propagates to the caller. Workflow interprets as activity cancellation — handled by DAPR Workflow.

### Reference: Log Events

| Event ID | Level       | Name                        | Fields                                 |
| -------- | ----------- | --------------------------- | -------------------------------------- |
| 6201     | Warning     | `RateLimitExceededLocally`  | `tenantId`                             |
| 6202     | Warning     | `ProviderRateLimitReceived` | `tenantId`, `retryAfterSeconds`        |
| 6203     | Information | `RateLimitActorUpdated`     | `tenantId`, `remaining`, `windowStart` |
| 6204     | Debug       | `ExtractionGateAcquired`    | `tenantId`, `availableCount`           |
| 6205     | Information | `ExtractionGateContended`   | `tenantId`, `queueDepth`               |
| 6206     | Warning     | `ExtractionGateTimeout`     | `tenantId`, `timeoutSeconds`           |

### Error Codes

- **`EMBEDDING_RATE_LIMITED`** — not a new error code; `EmbeddingRateLimitException` is the internal type; `FailureDetails.ErrorCode` is set to `"EmbeddingRateLimitException"` (via `exception.GetType().Name`) by the existing `IngestionWorkflow.AttachFailureDetails`. If product UX later wants a friendlier code, that's a 6.3 concern.
- **`EXTRACTION_GATE_TIMEOUT`** — surfaced via `TimeoutException.Message`; `FailureDetails.ErrorCode = "TimeoutException"`. If UX wants a code, wrap in a custom exception type in Phase 2.

### Project Structure Notes

**New files (source):**

- `src/Hexalith.Memories.Server/Ingestion/PerTenantConcurrencyGate.cs`
- `src/Hexalith.Memories.Server/Ingestion/IJitterSource.cs`
- `src/Hexalith.Memories.Server/Ingestion/ThreadSafeRandomJitterSource.cs`
- `src/Hexalith.Memories.Server/Ingestion/RateLimitingLog.cs`
- `docs/operations/rate-limiting.md` (or `docs/operations/rate-limiting.md`)

**Modified files (source):**

- `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs` — add `ReportRateLimitedAsync`.
- `src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs` — implement `ReportRateLimitedAsync`.
- `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs` — add `ReportRateLimited` helper.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` — inject `IJitterSource`, `ILogger`; add jitter; add 429 catch path.
- `src/Hexalith.Memories.Server/Activities/Ingestion/ExtractContentActivity.cs` — inject `PerTenantConcurrencyGate`; acquire around body.
- `src/Hexalith.Memories.Server/Activities/Ingestion/FetchUrlActivity.cs` — inject `PerTenantConcurrencyGate`; acquire around body.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` — parse `Retry-After`, populate `EmbeddingRateLimitException.RetryAfterSeconds`.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitException.cs` — add `RetryAfterSeconds` property.
- `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` — add two new fields (`PerTenantExtractionConcurrency`, `ExtractionGateAcquireTimeoutSeconds`).
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — pass `TenantId` into `ExtractionInput` and `FetchUrlInput`.
- `src/Hexalith.Memories.Server/Program.cs` — register `PerTenantConcurrencyGate` singleton, `IJitterSource` singleton.
- `src/Hexalith.Memories.Server/appsettings.json` — add `Ingestion:PerTenantExtractionConcurrency` + `Ingestion:ExtractionGateAcquireTimeoutSeconds` defaults.
- `src/Hexalith.Memories.Contracts/V1/ExtractionInput.cs` — add `TenantId` property (breaking, but contained).
- `src/Hexalith.Memories.Contracts/V1/FetchUrlInput.cs` — add `TenantId` property (breaking, contained).
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — no new types, but existing types changed shape; verify source-gen regenerates.
- `README.md` — optional: add Operations / Rate Limiting link.

**New files (tests):**

- `tests/Hexalith.Memories.Server.Tests/Ingestion/PerTenantConcurrencyGateTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/RateLimitingLogTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientRetryAfterParsingTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` (create if missing; else extend)
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs` — all `[Fact(Skip)]`.

**Modified files (tests):**

- `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs` — add `[Theory]` rows for `ReportRateLimited`.
- `tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs` — add `ReportRateLimitedAsync` test.
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — one test that verifies `TenantId` flows through `ExtractionInput` / `FetchUrlInput`.
- `tests/Hexalith.Memories.Contracts.Tests/V1/` — extend serialization tests for the updated `ExtractionInput` + `FetchUrlInput` shapes (round-trip with `TenantId`).

**No changes to:**

- `.slnx`, `Directory.Packages.props` (unless `Microsoft.Extensions.TimeProvider.Testing` is missing — verify; add if absent), `Directory.Build.props`.
- DAPR component YAML files.
- Any other epic's code.

### Known Dependencies (Verify Before Starting)

- **`Microsoft.Extensions.TimeProvider.Testing`** — required for `FakeTimeProvider` in `RateLimiterLogicTests`. Check `Directory.Packages.props` — if absent, add: `<PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="9.0.0" />` (or the version matching the project's .NET 10 / BCL baseline).
- **`NSubstitute`** (or the project's chosen mocking library) — for mocking `IEmbeddingRateLimiterActor`, `ITenantConfigurationActor`, `EmbeddingClient` in activity tests. Verify the existing `GenerateEmbeddingActivity` tests or 6.1's `FetchUrlActivityTests` for the established mocking idiom.
- **No new DAPR / actor dependencies.** All actor + workflow patterns use existing `Dapr.Workflow 1.17.6` and `Dapr.Actors.AspNetCore 1.17.6`.

### Definition of Done

1. All new unit tests pass — **at least ~20 new tests** covering:
    - `RateLimiterLogic.ReportRateLimited` with `FakeTimeProvider` (~6 parameterized cases).
    - `EmbeddingRateLimiterActor.ReportRateLimitedAsync` state persistence (~1 test).
    - `GenerateEmbeddingActivity` jitter + 429 feedback (~5 tests: happy, local throttle, provider 429 with Retry-After, provider 429 without, ceiling regression guard).
    - `EmbeddingClient` Retry-After header parsing (~7 cases).
    - `PerTenantConcurrencyGate` isolation, release, timeout, cancellation, concurrent GetOrAdd (~6 tests).
    - `RateLimitingLog` EventId assertions (~6 tests).
2. All integration tests are `[Fact(Skip)]` with tracker references to Story 6.3 or Epic 7.
3. `GenerateEmbeddingActivity` calls `ReportRateLimitedAsync` on provider 429, parses `Retry-After`, and re-throws the exception unchanged.
4. `ExtractContentActivity` and `FetchUrlActivity` both acquire `PerTenantConcurrencyGate.AcquireAsync(tenantId)` at entry and release on scope exit.
5. Workflow retry policy is **unchanged** in shape (no new policy, no Polly, no second retry layer). The only new retry-adjacent code is the single `Task.Delay(jitter)` inside `GenerateEmbeddingActivity`.
6. `EmbeddingRateLimitException.RetryAfterSeconds` property is populated from the provider response. Default 0 means "unknown" → activity uses 30 s.
7. `IngestionSettings` exposes `PerTenantExtractionConcurrency` (default 4) and `ExtractionGateAcquireTimeoutSeconds` (default 300).
8. `docs/operations/rate-limiting.md` exists and describes per-tenant ceilings, shared-quota limitation, 429 handling, jitter, and gate.
9. No new NuGet dependencies except possibly `Microsoft.Extensions.TimeProvider.Testing` (test-only).
10. `dotnet test Hexalith.Memories.slnx --filter "FullyQualifiedName!~IntegrationTests"` reports ~1210+ passing; documented 2 baseline `SaveDedupKeyActivityTests` failures remain; zero new failures.
11. Structured log events 6201–6206 are emitted on all designated paths; asserted by unit tests.
12. No regression on existing ingestion / tenant / search tests (6.1 + 5.x behavior intact).

### Project Structure Notes

- Alignment with unified project structure: all new code follows feature-based namespace layout — `Server/Ingestion/` for the gate, jitter, and logs; `Server/Actors/` for actor method additions; `Contracts/V1/` for record field additions.
- New configuration fields live under existing `Ingestion` section in `appsettings.json`, no new section.
- Tests mirror source paths under `tests/` (Tier 1 for actors / logic / logs, Tier 2 for activity integrations).
- No changes to `.slnx`, `Directory.Build.props`. `Directory.Packages.props` may need `Microsoft.Extensions.TimeProvider.Testing`.

### References

- Epic 6 overview: [Source: _bmad-output/planning-artifacts/epics.md#Epic-6-Ingestion-Pipeline-Resilience-Operations] (lines 1250–1380)
- Story 6.2 acceptance criteria source: [Source: _bmad-output/planning-artifacts/epics.md#Story-6.2-Per-Tenant-Load-Management-Rate-Limiting] (lines 1283–1311)
- FR mapping (FR8, FR69, NFR13, NFR22): [Source: _bmad-output/planning-artifacts/epics.md#FR-Coverage-Map] (lines 237, 298, 144, 159)
- Architecture — Rate Limiting cross-cutting concern: [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns] (lines 88, §5)
- Architecture — DAPR Actor state model: [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints-Dependencies] (lines 73)
- Architecture — EmbeddingRateLimiterActor definition: [Source: _bmad-output/planning-artifacts/architecture.md#MVP-critical-DAPR-Actors] (lines 321)
- Architecture — Actor interface/implementation code sample: [Source: _bmad-output/planning-artifacts/architecture.md#DAPR-Actor-Patterns] (lines 808–866)
- Architecture — Data flow: `rateLimiter.TryConsumeAsync()` at step 4: [Source: _bmad-output/planning-artifacts/architecture.md#Data-Flow] (lines 1450)
- Architecture — Decision D24 (DAPR Actors for per-tenant stateful singletons): [Source: _bmad-output/planning-artifacts/architecture.md#Decision-Registry] (lines 565)
- Architecture — Decision D25 (Workflow-Actor separation of concerns): [Source: _bmad-output/planning-artifacts/architecture.md#Decision-Registry] (lines 566)
- Architecture — Shared rate limiter deferred to Phase 3: [Source: _bmad-output/planning-artifacts/architecture.md#Growth-phase] (lines 341)
- Architecture — Embedding Pipeline failure propagation: [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Dependencies-Failure-Propagation] (lines 155)
- Prior story 1.4 (Embedding Generation + Rate Limiter Actor): [Source: _bmad-output/implementation-artifacts/1-4-embedding-generation.md]
- Prior story 1.6 (Ingestion Workflow Orchestration + retry policy): [Source: _bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md]
- Prior story 1.7 (Embedding Provider Configuration + TenantEmbeddingConfig.RateLimitPerMinute): [Source: _bmad-output/implementation-artifacts/1-7-embedding-provider-configuration.md]
- Prior story 5.5 (Tenant Configuration update path): [Source: _bmad-output/implementation-artifacts/5-5-tenant-configuration-and-listing.md]
- Prior story 5.6 (Graceful Degradation — `[LoggerMessage]` pattern, `[Fact(Skip)]` convention, DAPR retry-policy limitation): [Source: _bmad-output/implementation-artifacts/5-6-graceful-degradation-on-backend-failure.md]
- Prior story 6.1 (URL & Directory Ingestion — `FetchUrlActivity`, `IngestionSettings`, `IngestionEndpointLog`, event ID convention): [Source: _bmad-output/implementation-artifacts/6-1-url-and-directory-ingestion.md]
- Existing `EmbeddingRateLimiterActor`: [Source: src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs]
- Existing `RateLimiterLogic`: [Source: src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs]
- Existing `RateLimitState`: [Source: src/Hexalith.Memories.Server/Actors/RateLimitState.cs]
- Existing `IEmbeddingRateLimiterActor`: [Source: src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs]
- Existing `GenerateEmbeddingActivity`: [Source: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs]
- Existing `EmbeddingClient` (Retry-After parsing target): [Source: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs]
- Existing `EmbeddingRateLimitException`: [Source: src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitException.cs]
- Existing `TenantEmbeddingConfig.RateLimitPerMinute`: [Source: src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs]
- Existing `IngestionWorkflow` (retry policy, activity wiring): [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs]
- Existing `IngestionSettings` (6.1): [Source: src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs]
- Existing `IngestionEndpointLog` pattern template: [Source: src/Hexalith.Memories.Server/Ingestion/IngestionEndpointLog.cs]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context) via BMad dev-story workflow.

### Debug Log References

- Baseline server test count pre-story (expected, from 6.1 Dev Agent Record): ~908 + 283 (contracts) = ~1191 passing, 2 documented `SaveDedupKeyActivityTests` failures, AppHost CS0311 build errors (pre-existing, unrelated).
- Post-story 6.2 test run (date 2026-04-15): **Server.Tests = 964 passed / 0 failed / 0 skipped; Contracts.Tests = 286 passed / 0 failed / 0 skipped; total 1250 unit tests passing.** The previously documented `SaveDedupKeyActivityTests` baseline failures no longer appear — they were resolved by the preceding commit `a4f32f8` ("Add unit tests for ingestion activities and services"). AppHost CS0311 errors remain pre-existing and unrelated to 6.2.
- Integration test project (`Hexalith.Memories.IntegrationTests`) transitively depends on AppHost; the 2 AppHost CS0311 build errors prevent it from compiling. `[Fact(Skip)]` scaffolding for 6.2 is in place (`RateLimitingIntegrationTests.cs`) and compiles in isolation once AppHost is fixed. Unskipping is owned by Story 6.3 (requires deterministic 429-producing provider test double).

### Completion Notes List

- **AC1, AC7, AC10 (VERIFY):** AC7 regression test added inline in `GenerateEmbeddingActivityTests.RunAsync_CeilingChangedBetweenInvocations_ReflectsLatestConfig` (pins SetCeilingAsync → TryConsumeAsync ordering across two invocations with changing ceiling). AC1 and AC10 are covered by the three `[Fact(Skip)]` scenarios in `RateLimitingIntegrationTests.cs`, unskipped by Story 6.3.
- **AC2, AC5, AC6 (NEW gate):** `PerTenantConcurrencyGate` in `src/Hexalith.Memories.Server/Ingestion/PerTenantConcurrencyGate.cs`, registered as singleton in `Program.cs`. Both `ExtractContentActivity` and `FetchUrlActivity` acquire via `await using IAsyncDisposable lease = await gate.AcquireAsync(tenantId, ct)`. 7 unit tests in `PerTenantConcurrencyGateTests.cs` cover isolation, release-on-exception, timeout, cancellation, and concurrent multi-tenant acquisition.
- **AC3, AC4, AC11 (NEW actor method + activity integration + jitter):** `IEmbeddingRateLimiterActor.ReportRateLimitedAsync(int)` added, implemented via pure `RateLimiterLogic.ReportRateLimited` helper. `EmbeddingClient.HandleEmbeddingResponseAsync` parses `Retry-After` (delta-seconds or HTTP-date per RFC 9110) into `EmbeddingRateLimitException.RetryAfterSeconds`. `GenerateEmbeddingActivity` applies jitter via injected `IJitterSource` before the provider call and maps 429 → `ReportRateLimitedAsync(retryAfter ?? 30)`. Workflow retry policy unchanged — the existing `WorkflowRetryPolicy` (5 attempts, 2s → 5min) handles the retry.
- **AC8 (docs):** `docs/operations/rate-limiting.md` describes per-tenant ceilings, shared-quota limitation, 429 handling, jitter, gate tuning, and log events. Linked from `README.md` under a new "Operations" section.
- **AC9 (logging):** `RateLimitingLog.cs` hosts events 6201-6206 via `[LoggerMessage]` partial methods mirroring `IngestionEndpointLog.cs`. `RateLimitingLogTests.cs` asserts EventId + LogLevel for each.
- **AC12 (regression):** zero new test failures, baseline 2 `SaveDedupKeyActivityTests` failures no longer present, all existing tests still pass.
- **Breaking changes applied:** `ExtractionInput` and `FetchUrlInput` both gained an optional `TenantId` parameter (defaults to empty string). Legacy DAPR workflow history deserializes to `TenantId = ""`; the activity fails fast on blank tenantId per `ArgumentException.ThrowIfNullOrWhiteSpace`, exposing any replay that predates the field — which is preferable to silently binding all orphaned activities to a single gate key.
- **Jitter rationale:** DAPR `Dapr.Workflow 1.17.6` `WorkflowRetryPolicy` does NOT accept a jitter parameter. Application-level jitter lives in `GenerateEmbeddingActivity` only (single `Task.Delay`, no custom retry loop).
- **No new NuGet packages required.** `Microsoft.Extensions.TimeProvider.Testing 9.5.0` already in `Directory.Packages.props`.
- **`SetCeilingAsync` hot-path call preserved** per AC11 and Dev Notes — the AC7 regression test pins it. Optimization (cache-if-unchanged) is Phase 2.

### File List

**New source files:**

- `src/Hexalith.Memories.Server/Ingestion/PerTenantConcurrencyGate.cs`
- `src/Hexalith.Memories.Server/Ingestion/IJitterSource.cs`
- `src/Hexalith.Memories.Server/Ingestion/ThreadSafeRandomJitterSource.cs`
- `src/Hexalith.Memories.Server/Ingestion/RateLimitingLog.cs`
- `docs/operations/rate-limiting.md`

**Modified source files:**

- `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs` — added `ReportRateLimitedAsync`.
- `src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs` — implemented `ReportRateLimitedAsync`.
- `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs` — added `ReportRateLimited` instance helper.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` — injected `IJitterSource` + `ILogger`, jitter pre-call, 429 feedback, `RateLimitingLog` events.
- `src/Hexalith.Memories.Server/Activities/Ingestion/ExtractContentActivity.cs` — injected `PerTenantConcurrencyGate`, acquire/release lease.
- `src/Hexalith.Memories.Server/Activities/Ingestion/FetchUrlActivity.cs` — injected `PerTenantConcurrencyGate`, acquire/release lease.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` — `Retry-After` header parsing via `RetryConditionHeaderValue`.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitException.cs` — added `RetryAfterSeconds` init-only property.
- `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` — added `PerTenantExtractionConcurrency` + `ExtractionGateAcquireTimeoutSeconds`.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — pass `input.TenantId` into `FetchUrlInput` + `ExtractionInput`.
- `src/Hexalith.Memories.Server/Program.cs` — register `PerTenantConcurrencyGate` + `IJitterSource` singletons.
- `src/Hexalith.Memories.Server/appsettings.json` — added `Ingestion:PerTenantExtractionConcurrency` + `Ingestion:ExtractionGateAcquireTimeoutSeconds` defaults.
- `src/Hexalith.Memories.Contracts/V1/ExtractionInput.cs` — added `TenantId` parameter (defaults to empty string).
- `src/Hexalith.Memories.Contracts/V1/FetchUrlInput.cs` — added `TenantId` parameter (defaults to empty string).
- `README.md` — added Operations section linking to `docs/operations/rate-limiting.md`.

**New test files:**

- `tests/Hexalith.Memories.Server.Tests/Ingestion/PerTenantConcurrencyGateTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/RateLimitingLogTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientRetryAfterParsingTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs` (all `[Fact(Skip)]`)

**Modified test files:**

- `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs` — added `ReportRateLimited` `[Theory]`, paused-window assertions, ordering test.
- `tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs` — added two `ReportRateLimitedAsync` tests (30s + clamping).
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` — rewrote to use new constructor; added 429-with/without-Retry-After, jitter delay, AC7 regression.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs` — updated constructor call with `IJitterSource` + logger.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/ExtractContentActivityTests.cs` — updated constructor, added `TenantId`-required test.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/FetchUrlActivityTests.cs` — updated constructor, added `TenantId`-required test.
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — added two tests verifying `TenantId` flows into `ExtractionInput` and `FetchUrlInput`.
- `tests/Hexalith.Memories.TestHelpers/Factories/ExtractionInputFactory.cs` — optional `tenantId` parameter.
- `tests/Hexalith.Memories.Contracts.Tests/V1/ExtractionInputSerializationTests.cs` — added `TenantId` round-trip + legacy-payload default test.
- `tests/Hexalith.Memories.Contracts.Tests/V1/UrlAndDirectoryIngestionSerializationTests.cs` — extended `FetchUrlInput_RoundTrips`, added legacy-payload default test.

### Change Log

- 2026-04-15: Implemented Story 6.2 per-tenant load management and rate limiting. Added `ReportRateLimitedAsync` to embedding rate limiter actor, `PerTenantConcurrencyGate` for CPU-bound extraction activities, `IJitterSource` for application-level jitter (NFR22), `Retry-After` parsing in `EmbeddingClient`, structured log events 6201-6206, and threaded `TenantId` through `ExtractionInput` / `FetchUrlInput`. 1250 unit tests passing (+59 vs. 6.1 baseline), 0 regressions.

