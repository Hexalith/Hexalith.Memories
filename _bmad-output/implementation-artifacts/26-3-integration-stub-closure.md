---
baseline_commit: a8a0dbd60e2c49d183248c41b952a06fb897e8d7
---

# Story 26.3: Integration Stub Closure

Status: done

<!-- Epic 26 — Test, Deployment & Operational Readiness. Closes audit finding A23. Test-only changes use `test(integration): ...`; a production defect exposed by a real test may be fixed in scope, but unrelated product work and package upgrades are not. -->

## Story

As a test architect,
I want the empty integration stubs implemented or explicitly skipped,
so that failure-mode coverage is real, not apparent.

## Context

The audited count is still exact at the baseline revision: `tests/Hexalith.Memories.IntegrationTests` contains 29 uses of `[RunnableSkippedFact]`; 28 target methods have assertion-free `_ = _fixture;` bodies, while `Fixtures/AppHostComponentFileOrderingTests.cs` is the one non-empty method. The IntegrationTests version of `RunnableSkippedFactAttribute` is runnable by default and sets `Skip` only when `HEXALITH_SKIP_RUNNABLE_TESTS=true` or `1`. Consequently, the 28 placeholders currently execute and report success.

This story closes that false evidence. Every original target scenario must finish with a recorded disposition: a real, runnable integration test that proves observable behavior and persisted end state, or a genuine xUnit skip with a current, specific reason and an explicit enabling condition. The non-empty AppHost component-ordering proof remains part of `integration-fast` and must not be lost.

Story 26.2 remains `in-progress` in parallel. Its Aspire fidelity/restart tests are useful source-shape precedents but do not yet constitute passed container evidence. Preserve concurrent fixture changes and do not treat 26.2 completion as a prerequisite for contexting or implementing this story.

## Acceptance Criteria

1. **All 28 false-positive stubs are closed.** At the current baseline, the target inventory is exactly 28 methods across `IngestionRetryIntegrationTests`, `UrlIngestionIntegrationTests`, `RetryFailureIntegrationTests`, `DirectoryIngestionIntegrationTests`, `RateLimitingIntegrationTests`, `DegradationIntegrationTests`, and `TenantConfigurationIntegrationTests`. On completion, none contains `_ = _fixture;`, a comment-only scenario, a bare `Task.CompletedTask`, or any other assertion-free success path. Each original method is either implemented as a normal runnable xUnit test or carries a real xUnit `Skip` reason; deleting or renaming one is allowed only when the 28-row completion matrix maps it to an equivalent assertion-bearing test.

2. **The priority failure modes have real executable proof.** At minimum, the container-backed suite implements and passes all of the following rather than skipping them:
   - transient ingestion failure followed by successful durable retry, ending with one `Indexed` memory unit and no failed-unit record;
   - retry exhaustion, ending with a `Failed` memory unit plus the failed-unit hash and per-case failed index exposed by the failure API;
   - provider `429` with `Retry-After`, ending with a durable workflow retry, persisted per-tenant rate-limiter state, one indexed unit, and no failed-unit record;
   - two-tenant rate-limit independence, proving distinct actor/state partitions and no cross-tenant budget leakage;
   - FalkorDB loss and recovery, proving hybrid-search degradation/traversal failure while unavailable, preservation of unaffected Redis-backed state, and automatic non-degraded recovery after restart.
   Existing unit tests may support these scenarios but do not replace the real Aspire/DAPR/Redis/FalkorDB execution required here.

3. **Implemented scenarios prove outcomes, not activity.** Each runnable target test asserts both the public behavior (HTTP/REST contract, workflow/case status, search response, or tenant API) and the final relevant backing state in Redis, DAPR workflow/actor state, and/or FalkorDB. Retry tests assert converged state rather than brittle exact activity-call counts because DAPR activities are at-least-once. Failure tests prove no partial or cross-tenant corruption; success-after-retry tests prove no duplicate memory unit, vector, failed-unit, or graph record.

4. **Deferred scenarios are honestly skipped.** An intentionally deferred method uses literal xUnit skip semantics, for example `[Fact(Skip = "...")]`; retaining the IntegrationTests `[RunnableSkippedFact]` attribute is not an explicit skip. Every reason names the current technical blocker and a stable structured entry created or updated in `_bmad-output/implementation-artifacts/deferred-work.md` with its owner recorded in the rationale and a reopen trigger. Generic or stale reasons such as "Requires Aspire fixture," "Story 6.4," or "Epic 7" are rejected because those facilities/stories already exist. Story 26.4 is not the default owner and may be named only for a blocker specifically about coverage or benchmark scheduling. The 28-row completion matrix records the disposition, reason or proof, persisted-state assertions, lane, and result for every original method.

5. **Failure injection is deterministic and leaves the shared topology healthy.** New fixture seams use bounded cancellation/timeouts, unique tenant/case/source identifiers, and the existing non-parallel Aspire collection. Each destructive test restores stopped resources and clears scoped fault plans in `finally`, waits for the resource and Memories service to recover, and reconnects or refreshes clients when necessary. A failed test cannot poison the next test. Test-only provider failure plans are request/tenant scoped or use an isolated fixture variant; process-global mutable plans that can leak between tests are forbidden.

6. **Topology claims are truthful.** The AppHost has one Redis Stack resource (`memories-vectors`) serving RediSearch, vectors, DAPR state, actors, workflows, and pub/sub, plus one FalkorDB resource (`memories-graphs`). A test must not stop that Redis container and claim a semantic-only or syntactic-only outage. Such a scenario requires a targeted capability fault seam or a genuine skip explaining the shared-resource topology. Stopping all Redis-backed axes also removes state-store availability, so the asserted API path must be validated against that real dependency graph rather than assumed from old comments.

7. **False-pass regression is structurally and outcome guarded.** A Docker-free guard in the normal test lane fails if IntegrationTests reintroduces `RunnableSkippedFact`, `_ = _fixture;`, a known no-op placeholder body, or an empty/generic explicit skip reason. A checked-in `tools/integration-stub-targets.txt` records all 28 original fully-qualified methods plus any unambiguous `original|replacement` mapping, and a focused source/TRX verifier proves that all 28 resolve exactly once, all five AC2 priority failure-mode groups have `Passed` outcomes, every other row is `Passed` or genuinely skipped, and no target is absent. This is closure evidence, not Story 26.4 coverage gating. The IntegrationTests custom attribute and its opt-out environment variable are removed once its one real AppHost use is converted to plain `[Fact]`. The unrelated skip-by-default attribute in `Hexalith.Memories.Server.Tests` is outside this story and remains untouched. `integration-fast` continues to select and execute `AppHostProjectResolutionTests` and the non-empty `AppHostComponentFileOrderingTests`; class-name presence alone is not accepted as proof for the 28 targets.

8. **Verification is complete and scoped.** The solution builds in Release with zero warnings/errors; focused xUnit v3 runs cover the Docker-free guard and every implemented target class/method; the full `Hexalith.Memories.IntegrationTests` container-backed lane and CI `integration-fast` verifier pass. Evidence reports passed/skipped/failed outcomes for all 28 targets, with no unexpected skips. A Docker-less compile is not completion evidence. Changes remain limited to the 28 target scenarios, necessary test fixture/test-double/verifier support, and minimal production defects exposed by those tests; deployment (26.1), restore/runbooks (26.2/26.5), coverage thresholds/benchmark scheduling (26.4), broad topology redesign, package upgrades, submodule pointers, and the Server.Tests `RunnableSkippedFact` are out of scope.

## Tasks / Subtasks

- [x] **Task 1 — Reconcile the audit anchors and freeze the 28-row disposition matrix** (AC: 1, 4, 8)
  - [x] Recount `[RunnableSkippedFact]` and `_ = _fixture;` at implementation start. Record the date, baseline revision, moved anchors, and adaptations in the Dev Agent Record rather than copying July audit line numbers blindly.
  - [x] Create the completion matrix from the inventory in Dev Notes. For each original method record: final method, `implemented`/`skipped` disposition, exact public assertion, exact persisted-state assertion, skip blocker/enabler when applicable, execution lane, and result.
  - [x] Add the same stable inventory to `tools/integration-stub-targets.txt`; record replacement FQNs explicitly when consolidating a duplicate so removal or renaming cannot make a row disappear.
  - [x] Revalidate every old skip comment against live code. Do not preserve reasons that name completed Story 6.4/Epic 7 or claim the Aspire fixture does not exist.
  - [x] For each accepted skip, create or update a structured `deferred-work.md` entry with unique ID, status, source story, target artifact, reopen trigger, and rationale/evidence (including the owner); reference that ID in the literal skip reason.

- [x] **Task 2 — Remove the false-pass mechanism and add a Docker-free regression guard** (AC: 1, 4, 7)
  - [x] Convert the real `AppHostComponentFileOrderingTests` method to plain `[Fact]`, retaining its `Integration` traits and its `integration-fast` required-surface coverage.
  - [x] Convert implemented targets to plain `[Fact]` (and add `IntegrationSlow`/`Performance` only when the actual scenario warrants the existing slower lane). Convert accepted deferrals to literal `[Fact(Skip = "specific reason; owner/follow-up; unskip condition")]`.
  - [x] Delete `tests/Hexalith.Memories.IntegrationTests/RunnableSkippedFactAttribute.cs` and eliminate `HEXALITH_SKIP_RUNNABLE_TESTS` use from the IntegrationTests assembly.
  - [x] Add `tests/Hexalith.Memories.Server.Tests/Architecture/IntegrationStubClosureGuardTests.cs`, following existing repo-root source-guard patterns. Scan only `tests/Hexalith.Memories.IntegrationTests`; report relative file/method violations. Guard against the custom attribute, canonical no-op bodies, and blank/generic explicit-skip reasons without touching the separate Server.Tests attribute.
  - [x] Add a focused verifier (for example `tools/verify-integration-stub-closure.py`) that joins the 28-target manifest, current source dispositions, and xUnit TRX outcomes. It must fail on a duplicate/missing mapping, a priority target that is not Passed, an unapproved skip, or an absent test result.

- [x] **Task 3 — Add only the fixture seams needed for deterministic failure and recovery** (AC: 2, 3, 5, 6)
  - [x] Reuse `AspireIngestionPipelineFixture`, `ScriptedHttpServer`, `MemoriesClient`, Redis/Falkor handles, tenant provisioning, actor proxies, captured logs, and existing bounded polling. Promote genuinely shared helpers instead of copying private polling/key-enumeration loops between seven classes.
  - [x] For URL/source retry, script per-request `500`/`404`/success sequences with `ScriptedHttpServer` and its atomic request counter where that exercises the intended contract. Add a provider-specific isolated seam only for embedding `429`/provider failure behavior that the fake embedding path bypasses.
  - [x] Add resource stop/start/restart support through Aspire's `ResourceCommandService` using `KnownResourceCommands.StopCommand`, `StartCommand`, or `RestartCommand`, checking `ExecuteCommandAsync` results and waiting through `ResourceNotificationService` with cancellation deadlines. Do not expand ad-hoc Docker process discovery for new resource control.
  - [x] Keep the current fake embedding default for ordinary tests. A real-provider/fault-plan fixture variant must remain Development-only, use no live secret, be deterministic, and reset its configuration after each test.
  - [x] Add focused Docker-free tests for new fixture/fault-plan helpers where logic can be verified without starting the AppHost.

- [x] **Task 4 — Implement the priority retry and rate-limit proofs** (AC: 2, 3, 5)
  - [x] Implement transient retry success and retry exhaustion. Assert workflow completion/failure, stable memory-unit identity, exact failed-registry membership, final `MemoryUnit.Status`, and absence of duplicates/failed residue after recovery.
  - [x] Implement provider `429` → durable timer → success. Assert the actor's persisted paused/refill state, workflow convergence to `Indexed`, no failed registry entry, and request attempts in a non-brittle range; do not combine workflow and operator resiliency in a way that makes the expected attempt count ambiguous.
  - [x] Implement two-tenant independent ceilings through tenant-scoped actor proxies/configuration. Assert both actor states and a negative cross-tenant check; counting HTTP 429 responses alone is insufficient.
  - [x] Preserve the completed unit-level Story 23.3/23.5 coverage, but treat it only as supporting evidence. The provider-429 integration target closes the still-missing acceptance proof from Story 23.3.

- [x] **Task 5 — Implement FalkorDB degradation and recovery without topology corruption** (AC: 2, 3, 5, 6)
  - [x] Seed unique, searchable state before failure and snapshot the relevant Redis keys/values and graph facts.
  - [x] Stop `memories-graphs`; assert hybrid search remains HTTP 200 with the graph axis reported unavailable/degraded, graph traversal returns the established structured 503 contract, and unaffected Redis-backed results/state remain intact.
  - [x] Use an in-place `StopCommand` → `StartCommand` cycle for `memories-graphs`; `RestartTopologyAsync` or any operation that recreates the FalkorDB container is invalid no-reseed evidence. Restart in `finally`, wait for resource/service health, and prove the pre-stop graph facts and same request return without re-seeding or process restart. The AppHost Falkor resource has no named volume; if Aspire recreates the container, add an isolated fixture-owned Falkor volume with deterministic cleanup before claiming recovery without data loss.
  - [x] For semantic-only, syntactic-only, and all-backend scenarios, use a capability-scoped seam that matches production behavior or write a truthful explicit skip. Never split the AppHost's shared Redis resource solely to make an obsolete test comment pass.

- [x] **Task 6 — Resolve the remaining URL, directory, retry/failure, and tenant-config targets** (AC: 1, 3, 4, 5)
  - [x] Implement feasible API/state scenarios using the existing full fixture: small URL success, URL 404, counter actor state, tenant list/config/404, display-name persistence/audit, breaking-config conflict/no mutation, force-reindex flag, and rate-limit propagation.
  - [x] Give private-host rejection a fixture variant with `Memories__Ingestion__Url__AllowPrivateHosts=false`, or explicitly skip with that current configuration blocker; the shared fixture currently forces the value to `true` before AppHost startup.
  - [x] Classify directory batch/starvation/cross-tenant latency scenarios deliberately. If implemented, use the existing `IntegrationSlow`/`Performance` taxonomy, bounded datasets, recorded baselines, and persisted per-unit results. If deferred, name the missing directory-root/load harness and owning follow-up; never leave a runnable comment body.
  - [x] For tenant backend-health scenarios, respect the single-Redis topology. Any implemented mutation/config test must read the subsequent API view and the tenant registry/actor/hash state, and must verify a different tenant remains unchanged when an auth/tenant-routing surface is touched.

- [x] **Task 7 — Execute and record all gates** (AC: 7, 8)
  - [x] Build `Hexalith.Memories.slnx` in Release using the pinned .NET 10 SDK; do not add versions to `.csproj` files or upgrade dependencies for this test-only story.
  - [x] Build the affected test assemblies and run the xUnit v3 executable directly with `-class`/`-method` for focused evidence; run the Docker-free guard in Server.Tests.
  - [x] Run the complete container-backed IntegrationTests lane, then the same `integration-fast` filter and `tools/verify-integration-fast-coverage.py` used by CI. Preserve both required AppHost surfaces.
  - [x] Run the integration-stub outcome verifier against the complete-lane TRX. All 28 manifest rows must resolve once; the five AC2 priority groups must be Passed; only skips linked to accepted deferred-work IDs are expected.
  - [x] Record a final 28-row passed/skipped/failed matrix and exact commands/results. Zero unexpected skips and zero false-positive bodies are mandatory; container evidence must come from CI/operator execution if unavailable locally.
  - [x] Run `git diff --check`; preserve CRLF/C# headers, file-scoped namespaces, public XML documentation, one type per file, and avoid line-ending-only churn.

### Review Findings

- [x] [Review][Patch] Make the closure guard reject empty, comment-only, return-only, and other assertion-free runnable facts [tests/Hexalith.Memories.Server.Tests/Architecture/IntegrationStubClosureGuardTests.cs:38]
- [x] [Review][Patch] Exercise transient embedding-provider failure and durable workflow recovery instead of source-fetch retries [tests/Hexalith.Memories.IntegrationTests/Ingestion/EmbeddingProviderFailureIntegrationTests.cs:63]
- [x] [Review][Patch] Guarantee FalkorDB recovery when the stop command succeeds but state convergence throws [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:658]
- [x] [Review][Patch] Parse the matching deferred-work entry and require its accepted status, owner rationale, and reopen trigger [tools/verify-integration-stub-closure.py:276]
- [x] [Review][Patch] Inspect explicit skip attributes structurally so additional arguments or alternate xUnit shapes cannot bypass generic-reason checks [tests/Hexalith.Memories.Server.Tests/Architecture/IntegrationStubClosureGuardTests.cs:69]
- [x] [Review][Patch] Assert the URL-404 failed-unit hash and per-case failed index directly in Redis [tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionIntegrationTests.cs:100]
- [x] [Review][Patch] Restore deterministic PDF extraction coverage in the mixed-directory scenario [tests/Hexalith.Memories.IntegrationTests/Ingestion/DirectoryIngestionIntegrationTests.cs:37]
- [x] [Review][Patch] Assert the provider-500 IngestionFailed activity-stream event required by the original target [tests/Hexalith.Memories.IntegrationTests/Ingestion/EmbeddingProviderFailureIntegrationTests.cs:164]
- [x] [Review][Patch] Make workflow polling fail fast on unexpected named or numeric terminal states and preserve timeout diagnostics [tests/Hexalith.Memories.IntegrationTests/Fixtures/IngestionIntegrationTestDriver.cs:156]
- [x] [Review][Patch] Dispose the fake embedding server even when Aspire fixture cleanup throws [tests/Hexalith.Memories.IntegrationTests/Ingestion/EmbeddingProviderFailureIntegrationTests.cs:50]
- [x] [Review][Patch] Bound Docker volume-removal processes and report teardown timeout diagnostics [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:1450]
- [x] [Review][Patch] Reject undefined HTTP status values above 599 in embedding fault plans [tests/Hexalith.Memories.IntegrationTests/Fixtures/EmbeddingProviderFaultPlan.cs:24]

## Dev Notes

### Non-negotiable implementation contract

- This is a test-evidence story, not permission to redesign production topology. Prefer test-only seams and existing public contracts. A minimal production fix is in scope only when a newly real integration test demonstrates the defect and the fix retains architecture/security/tenant boundaries.
- A passed HTTP response is not end-state proof. Pair it with the authoritative store: Redis memory/vector/dedup/failed keys, DAPR workflow/actor state, or FalkorDB graph data. Select exact Redis key families and verify Redis value type before `HGETALL`; Story 26.2 review found that broad `:case:*` scans can include activity streams and fail with `WRONGTYPE` before assertions.
- DAPR workflow activities are at-least-once. Make fault plans and assertions idempotent, use durable workflow retry/timers, and avoid exact-call-count assertions unless the component contract—not scheduling—owns the count.
- Tenant isolation is physical and security-sensitive. Every test uses unique tenant/case/source ids; actor ids and Redis/Falkor selectors remain tenant-scoped. Add focused negative cross-tenant proof if fixture authentication, route authorization, tenant routing, or shared state helpers change.
- The assembly already disables parallelization globally, and the Aspire collection also disables it. Keep destructive tests serialized and clean up in `finally`; do not add sleeps or unbounded polling.

### Audited 28-scenario inventory and required evidence

| # | Original target | Required proof or honest disposition |
|---:|---|---|
| 1 | `IngestionRetryIntegrationTests.TransientIngestionFailure_ShouldCompleteSuccessfullyAfterRetries` | **Implement:** transient failure → durable retry → one Indexed unit; no failed record or duplicate. |
| 2 | `UrlIngestionIntegrationTests.UrlIngestion_SmallTextPage_ShouldCompleteAndBeSearchable` | Implement with `ScriptedHttpServer`; workflow + Redis/vector/search result. |
| 3 | `UrlIngestionIntegrationTests.UrlIngestion_404Url_ShouldFailAfterRetries` | Implement; structured URL error + persisted Failed unit/failed registry. |
| 4 | `UrlIngestionIntegrationTests.UrlIngestion_PrivateIpWithAllowDisabled_ShouldRejectBeforeScheduling` | Implement with an `AllowPrivateHosts=false` fixture variant, or skip with that exact startup-configuration blocker; prove no workflow/state was created. |
| 5 | `RetryFailureIntegrationTests.IngestUrl_ProviderReturns500_ExhaustsRetriesAndPersistsFailedUnit` | **Implement or consolidate to the existing equivalent slow proof:** exhausted retry + failed hash, per-case index, API/activity, and Failed memory unit. |
| 6 | `RetryFailureIntegrationTests.ReIngestSingle_PreservesMemoryUnitId_AndClearsRegistry` | Implement or consolidate to the existing equivalent slow proof; stable id, cleared registry/dedup, final Indexed state. |
| 7 | `RetryFailureIntegrationTests.ReIngestBulk_MixedOutcomes_EnumeratedInResponse` | Implement with deterministic five-way setup, or skip with the exact missing claim/hiccup seam and owner. |
| 8 | `RetryFailureIntegrationTests.CounterActor_TracksConcurrentInflightWorkflows` | Implement; public case status plus persisted actor state and final drain. |
| 9 | `DirectoryIngestionIntegrationTests.DirectoryIngestion_MixedFiles_ShouldIndexSupportedAndSkipUnsupported` | Implement against the AppHost's existing repository `test-data` directory harness; assert per-file persisted outcomes. |
| 10 | `DirectoryIngestionIntegrationTests.DirectoryIngestion_CrossTenantIsolation_ShouldNotSerialize` | Performance/chaos proof with bounded baseline and tenant states, or explicit performance-lane deferral. |
| 11 | `RateLimitingIntegrationTests.TwoTenantIsolation_ShouldEnforceIndependentCeilings` | **Implement:** independent actor/state partitions and negative cross-tenant assertion. |
| 12 | `RateLimitingIntegrationTests.BatchVsSingleIngest_ShouldNotStarveRealTimeTenant` | Performance lane with baseline and persisted outcomes, or explicit load-harness deferral. |
| 13 | `RateLimitingIntegrationTests.Provider429_ShouldReportToActorAndRetry` | **Implement:** provider 429/Retry-After → actor pause + durable retry → Indexed/no failed record. |
| 14 | `DegradationIntegrationTests.HybridSearch_RedisVectorStopped_ShouldReturn200Degraded` | Targeted semantic-capability fault only; otherwise skip because Redis is shared by retrieval/state/pubsub. |
| 15 | `DegradationIntegrationTests.HybridSearch_FalkorDbStopped_ShouldDegradeToSyntacticAndSemantic` | **Implement:** HTTP 200 degraded, graph unavailable, Redis-backed state/results preserved. |
| 16 | `DegradationIntegrationTests.HybridSearch_AllBackendsStopped_ShouldReturn503AllBackendsUnavailable` | Implement only against the real state-store collapse path, or skip with exact shared-Redis blocker. |
| 17 | `DegradationIntegrationTests.HybridSearch_AfterBackendRestart_ShouldReturnNonDegradedResult` | **Implement with #15:** recovery without reseed/process restart; same backing state. |
| 18 | `DegradationIntegrationTests.SingleAxisSearch_RedisStopped_ShouldReturn503BackendUnavailable` | Implement only with truthful dependency semantics; assert structured 503 + recovery, or exact skip. |
| 19 | `TenantConfigurationIntegrationTests.ListTenants_ReturnsEnrichedSummaryWithCountsAndIndexHealth` | Implement; API summary matches tenant registry and indexed data. |
| 20 | `TenantConfigurationIntegrationTests.ListTenants_WhenOneBackendStopped_TenantStillListedWithUnknownOnThatAxis` | Falkor axis is feasible; semantic-only Redis axis needs targeted fault seam or exact skip. |
| 21 | `TenantConfigurationIntegrationTests.GetConfiguration_ReturnsComposedView_WithFullEmbeddingConfig` | Implement; API view equals persisted registry/config. |
| 22 | `TenantConfigurationIntegrationTests.GetConfiguration_UnknownTenant_Returns404TenantNotFound` | Implement; structured 404 and no created tenant state. |
| 23 | `TenantConfigurationIntegrationTests.PatchDisplayName_UpdatesRegistryAndReflectsInSubsequentGet` | Implement; subsequent GET + registry value + audit evidence. |
| 24 | `TenantConfigurationIntegrationTests.PatchDisplayName_NonActiveTenant_Returns409` | Implement with deterministic non-Active registry state, or exact state-seeding blocker. |
| 25 | `TenantConfigurationIntegrationTests.PutEmbeddingConfig_BreakingChange_WithoutForceReindex_Returns409` | Implement; structured 409 and byte/field-equivalent unchanged persisted config. |
| 26 | `TenantConfigurationIntegrationTests.PutEmbeddingConfig_BreakingChange_WithForceReindex_Returns200AndSetsReindexRequired` | Implement; API view and persisted `ReindexRequired`. |
| 27 | `TenantConfigurationIntegrationTests.PutEmbeddingConfig_RateLimitChange_PropagatesToRateLimiterOnNextIngest` | Implement; cache-bound propagation plus actor state after ingest. |
| 28 | `TenantConfigurationIntegrationTests.IngestMemoryUnit_EndToEnd_PersistsEmbeddingProviderAndModel` | Implement with deterministic fake provider; API and exact Redis hash fields. |

### Reuse map and topology constraints

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` already provides the authenticated client, Redis/Falkor connections, actor proxies, tenant provisioning/seeding, topology restart, Falkor stop, DAPR sidecar stop, resource-health waits, and captured logs. Extend this fixture narrowly.
- `Fixtures/ScriptedHttpServer.cs` already provides a random-loopback scripted server, atomic request count, response status/body/headers (including `Retry-After`), and bounded async disposal. Reuse it for URL retry/error sequences.
- `Fixtures/OllamaOidcFakeServer.cs` is the existing deterministic embedding-provider boundary. Extend its scripted response behavior for provider-specific `429`/`500` cases before inventing another provider test mode; keep its current success/OIDC behavior intact.
- `Ingestion/PipelinePersistenceIntegrationTests.cs` is the current model for unique ids, bounded waits, restart recovery, exact Redis key counts, stable memory-unit ids, workflow status, actor drain, and no-duplicate end state. Extract shared helpers only when multiple target classes need them.
- Its existing slow tests already exercise server/source `500` failure, persisted failed-unit state, restart/re-ingest, stable identity, registry clearing, counter restoration, and persisted limiter state. Where an audited stub is semantically identical, consolidate it to that named real test and record the mapping instead of creating a second expensive topology test. Do not claim equivalence when the old test covers a different failure layer (for example source HTTP `500` versus embedding-provider `429`).
- `Restore/BackupRestoreFidelityIntegrationTests.cs` is a useful store-enumeration shape but its container execution remains unverified while Story 26.2 is in progress. Do not cite it as passed evidence until the lane actually runs.
- The AppHost already enables directory ingestion against the repository `test-data` path. Reuse that configured root for the mixed-file scenario; do not add a second directory resource or preserve the stale "missing Aspire fixture" reason.
- `Health/HealthEndpointIntegrationTests.cs` contains the established outage/health-response pattern; `Ingestion/OllamaEmbeddingEndToEndTests.cs` contains provider/model persistence assertions. Reuse their setup and assertions where applicable.
- AppHost resource names are `memories-vectors`, `memories-graphs`, `memories`, and `memories-mcp`. `memories-vectors` is one Redis Stack container for RediSearch, vectors, DAPR state/actors/workflows, and pub/sub. Resource-level failure is therefore not equivalent to one search-axis failure.
- Keep `AppHostProjectResolutionTests` and `AppHostComponentFileOrderingTests` in `tools/integration-fast-required-surfaces.txt`; do not add NuGet-version assertions to those topology resolution tests.

### Previous-story and Git intelligence

- Story 26.1 is done and proved the repository can execute a container-backed DAPR/kind lane with zero skips. Reuse its deterministic failure-evidence discipline; deployment artifacts themselves are out of scope.
- Story 26.2 is in progress. Its plain `[Fact]` Aspire fixture pattern is correct, but its fidelity/restart tests still need CI/operator execution and a prior broad Redis scan caused `WRONGTYPE`. Preserve its concurrent work and do not claim it as green precedent.
- Recent commit subjects are not a reliable guide to file scope; inspect diffs. No recent package change is needed here. For test-only work use a Conventional Commit such as `test(integration): close failure-mode stubs` and avoid line-ending-only churn.

### Current technology guidance

- Use the repository pins: .NET SDK `10.0.302` / C# 14, Aspire Hosting Testing `13.4.6`, DAPR/Dapr.Workflow `1.18.4`, Testcontainers `4.13.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `5.3.0`. Central package management applies; this story needs no new dependency.
- Aspire 13.4 exposes resource commands through `ResourceCommandService.ExecuteCommandAsync` and resource state/health waits through `ResourceNotificationService`. Check command results and use cancellation timeouts rather than shelling out to `docker stop` for new fixture behavior.
- xUnit `FactAttribute.Skip` is the actual skip reason; null means the test runs. This is why the current IntegrationTests attribute is unsafe for a closure story.
- DAPR retries may exist at workflow and operator-resiliency layers. Keep the test plan explicit about which layer is under test, respect `Retry-After`, and assert durable converged state instead of scheduler-dependent exact attempt counts.

### Project Structure Notes

- Modify the seven existing target test files in place. Shared fixture/test-server support remains under `tests/Hexalith.Memories.IntegrationTests/Fixtures/`; add one type per C# file.
- Put the Docker-free source guard under `tests/Hexalith.Memories.Server.Tests/Architecture/`, following the existing `ResolveRepoRoot` + relative-violation-reporting pattern.
- If a provider fault server/plan is necessary, keep it under IntegrationTests fixtures unless a real production defect requires a product seam. Do not add a new project or test-only production topology resource.
- Keep `[Collection("AspireIngestionPipeline")]`, `Trait("Category", "Integration")`, and current non-parallel behavior. Use `IntegrationSlow`/`Performance` only for truly slow/load scenarios so `integration-fast` remains meaningful.
- C# files require the ITANEO header, file-scoped namespace, CRLF, public XML docs, `Async` suffixes, `ConfigureAwait(false)` in library/client code, xUnit v3 + Shouldly, and warnings-as-errors compliance.

### Verification Commands

```bash
dotnet restore Hexalith.Memories.slnx -p:Configuration=Release
dotnet build Hexalith.Memories.slnx --configuration Release --no-restore

# Focused Docker-free guard (build, then invoke the xUnit v3 executable directly).
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-restore
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Architecture.IntegrationStubClosureGuardTests

# Container runtime + DAPR required. Use method/class filters first, then the CI-equivalent lane.
dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --no-restore
bash ./tools/test.sh --filter "Category=Integration" \
  --configuration Release --no-build --results-directory TestResults/integration-all
python3 tools/verify-integration-stub-closure.py \
  --targets tools/integration-stub-targets.txt --results-directory TestResults/integration-all
bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" \
  --configuration Release --no-build --results-directory TestResults/integration-fast
python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast
git diff --check
```

### References

- [Source: `_bmad-output/planning-artifacts/epics.md`, Story 26.3] — canonical story, 28/29 count, state-store/explicit-skip acceptance, A23 closure (`:4577-4587`).
- [Source: `_bmad-output/implementation-artifacts/epic-26-context.md`] — real failure-mode tests, backing-store end state, explicit-skip-only rule, and 26.3-before-26.4 dependency (`:19-41`).
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`] — continuous Epic 26 track, 28-stub priority, and Test Architect ownership (`:59-69`, `:144-164`).
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md`, A23] — assertion-free test finding and recommendation (`:53-55`, `:88-90`).
- [Source: `_bmad-output/planning-artifacts/prd.md`, Integration Strategy / NFR verification] — full Aspire/DAPR integration expectations and intentional NFR13/NFR18/NFR19/NFR22 failure simulation (`:819-839`, `:975-999`).
- [Source: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`] — parallel in-progress predecessor, plain-Fact/store-end-state precedent, unexecuted container caveat, and Redis `WRONGTYPE` lesson.
- [Source: `tests/Hexalith.Memories.IntegrationTests/RunnableSkippedFactAttribute.cs`] — current opt-out implementation that runs placeholders by default.
- [Source: `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`] — fixture capabilities, process-wide configuration, resource names, bounded health waits, and non-parallel collection.
- [Source: `src/Hexalith.Memories.AppHost/Program.cs`] — one shared Redis Stack resource plus FalkorDB topology.
- [Source: `.github/workflows/ci.yml` and `tools/verify-integration-fast-coverage.py`] — current Docker-backed integration-fast command and class-presence verifier limitation.
- [Aspire resource testing](https://aspire.dev/testing/accessing-resources/) — current resource command and resource-notification testing APIs.
- [Aspire testing in CI](https://aspire.dev/testing/testing-in-ci/) — container runtime and bounded CI test guidance.
- [xUnit v3 `FactAttribute`](https://api.xunit.net/v3/3.2.2/Xunit.FactAttribute.html) — `Skip` semantics.
- [DAPR Workflow features and concepts](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/) — durable retries/timers and at-least-once activities.
- [DAPR retry policies](https://docs.dapr.io/operations/resiliency/policies/retries/retries-overview/) — retryable 429/5xx behavior and `Retry-After` considerations.
- [ASP.NET Core 10 rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0) — partitioned limiter and stress-test guidance.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-13 implementation-start reconciliation at `dbe8b72e16efe5751ad5a4f8543e14479b0c5968` (story baseline preserved as `a8a0dbd60e2c49d183248c41b952a06fb897e8d7`): 29 `RunnableSkippedFact` uses, 28 assertion-free target bodies across the seven audited classes, and one assertion-bearing AppHost component-ordering test. No audited method had moved; stale Story 6.4/Epic 7/Aspire-fixture reasons were rejected.
- Implementation plan: replace the false-pass attribute with plain facts or literal structured skips; share bounded API/store polling; use an isolated Ollama/OIDC fake fault plan for provider 500/429; use Aspire resource commands for in-place FalkorDB stop/start; consolidate only semantically equivalent expensive proofs through explicit manifest mappings; verify source dispositions plus TRX outcomes.
- 2026-07-13 broad-gate regression closure (user-authorized scope expansion): made Falkor edge snapshots numeric-safe and outside the export advisory window; made terminal restore runtime status authoritative over stale custom progress; accepted provider-only and canonical `provider:model` restore attribution; made repeated same-tenant authorization filters idempotent but conflicting state fail closed; aligned Development MCP bearer forwarding with the Server JWT realm; retained deny-by-default DAPR invocation policy in Kubernetes while removing the identity-dependent ACL from the local non-mTLS configuration; and made syntactic pagination scores deterministic.
- Final complete lane: `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Release --no-build --filter "Category=Integration"` passed 243, skipped the eight accepted deferrals, failed 0 (251 total, 12m25s). `verify-integration-stub-closure.py` resolved all 28 manifest rows exactly once: 20 passed, 8 accepted skips, 0 failed.
- Final CI-equivalent lane: `bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --no-build --results-directory TestResults/integration-fast-final` passed 224, skipped 8, failed 0 (232 total, 4m59s). `verify-integration-fast-coverage.py` passed every required surface, including both AppHost proofs.
- Final local gates: Release solution build passed with 0 warnings/errors; Server.Tests passed 2619 with 1 pre-existing skip and 0 failures; direct xUnit v3 runs passed the closure guard 2/2, tenant authorization 8/8, restore provider attribution 7/7, restore status 6/6, deployment security configuration 5/5, and fake provider 21/21; the verifier's four unit tests passed; `git diff --check` passed.
- 2026-07-15 adversarial review repair: the first complete lane intentionally exposed the missing provider-failure activity contract (242 passed, 8 accepted skips, 1 failed). The workflow now records best-effort `IngestionFailed` activity for pre-index and post-index failures; the provider class passed 3/3 and the repeated complete lane passed 243 with 8 accepted skips and 0 failures in 11m48s. The strengthened closure verifier resolved 20 passed, 8 accepted skips, 0 failed; its seven unit tests and the 12-case structural source guard passed.

#### 28-row completion matrix

| # | Original target | Final target / disposition | Public assertion | Persisted-state assertion or skip enabler | Lane / result |
|---:|---|---|---|---|---|
| 1 | `IngestionRetryIntegrationTests.TransientIngestionFailure_ShouldCompleteSuccessfullyAfterRetries` | `EmbeddingProviderFailureIntegrationTests.TransientIngestionFailure_ShouldCompleteSuccessfullyAfterRetries` / implemented | accepted workflow completes after transient provider 500s and memory-unit GET is `Indexed` | one syntactic/vector/graph unit; no failed registry or duplicate | complete diagnostic / Passed |
| 2 | `UrlIngestionIntegrationTests.UrlIngestion_SmallTextPage_ShouldCompleteAndBeSearchable` | same / implemented | URL accepted, completed, and searchable | exact Redis memory/vector keys plus graph node | `integration-fast` / Passed |
| 3 | `UrlIngestionIntegrationTests.UrlIngestion_404Url_ShouldFailAfterRetries` | same / implemented | workflow and memory-unit API report failure | failed hash and per-case failed index contain exactly the unit | `integration-fast` / Passed |
| 4 | `UrlIngestionIntegrationTests.UrlIngestion_PrivateIpWithAllowDisabled_ShouldRejectBeforeScheduling` | same / skipped | literal skip `26.3-PRIVATE-HOST-FIXTURE` | enable with isolated `AllowPrivateHosts=false` startup fixture and no-workflow/state proof | `integration-fast` / Skipped (accepted) |
| 5 | `RetryFailureIntegrationTests.IngestUrl_ProviderReturns500_ExhaustsRetriesAndPersistsFailedUnit` | `EmbeddingProviderFailureIntegrationTests.Provider500_ExhaustsRetriesAndPersistsFailedUnit` / implemented | provider-backed workflow reaches failed contract | failed memory hash, registry hash, and per-case index; no vector/graph residue | `integration-fast` / Passed |
| 6 | `RetryFailureIntegrationTests.ReIngestSingle_PreservesMemoryUnitId_AndClearsRegistry` | `PipelinePersistenceIntegrationTests.RestartTopology_ShouldKeepFailedUnitVisibleAndAllowReingestionAfterRecovery` / consolidated | re-ingest API completes with stable unit id | failed registry clears; dedup/vector identity remains stable | complete diagnostic / Passed |
| 7 | `RetryFailureIntegrationTests.ReIngestBulk_MixedOutcomes_EnumeratedInResponse` | same / skipped | literal skip `26.3-BULK-REINGEST-HICCUP` | enable with fixture-scoped five-way claim/Redis hiccup plan | `integration-fast` / Skipped (accepted) |
| 8 | `RetryFailureIntegrationTests.CounterActor_TracksConcurrentInflightWorkflows` | same / skipped | literal skip `26.3-COUNTER-STAGE-BARRIER` | enable with deterministic workflow-stage barriers and actor/API sample | `integration-fast` / Skipped (accepted) |
| 9 | `DirectoryIngestionIntegrationTests.DirectoryIngestion_MixedFiles_ShouldIndexSupportedAndSkipUnsupported` | same / implemented | batch accepts 5 and skips 2; terminal API reports 5 indexed | five unique memory/vector records and no unsupported-file records | `integration-fast` / Passed |
| 10 | `DirectoryIngestionIntegrationTests.DirectoryIngestion_CrossTenantIsolation_ShouldNotSerialize` | same / skipped | literal skip `26.3-DIRECTORY-CROSS-TENANT-PERF` | enable with bounded baseline/load harness and persisted per-tenant outcomes | `integration-fast` / Skipped (accepted) |
| 11 | `RateLimitingIntegrationTests.TwoTenantIsolation_ShouldEnforceIndependentCeilings` | same / implemented | both tenant configurations remain independently readable | distinct actor states and negative cross-tenant budget check | `integration-fast` / Passed |
| 12 | `RateLimitingIntegrationTests.BatchVsSingleIngest_ShouldNotStarveRealTimeTenant` | same / skipped | literal skip `26.3-BATCH-STARVATION-PERF` | enable with isolated performance lane and persisted outcomes | `integration-fast` / Skipped (accepted) |
| 13 | `RateLimitingIntegrationTests.Provider429_ShouldReportToActorAndRetry` | `EmbeddingProviderFailureIntegrationTests.Provider429_ShouldReportToActorAndRetry` / implemented | 429/Retry-After workflow converges to `Indexed` | actor pause/refill observed; one unit and no failed registry | `integration-fast` / Passed |
| 14 | `DegradationIntegrationTests.HybridSearch_RedisVectorStopped_ShouldReturn200Degraded` | same / skipped | literal skip `26.3-SEMANTIC-CAPABILITY-FAULT` | enable with semantic-only fault while shared Redis control plane stays available | `integration-fast` / Skipped (accepted) |
| 15 | `DegradationIntegrationTests.HybridSearch_FalkorDbStopped_ShouldDegradeToSyntacticAndSemantic` | same / implemented | hybrid 200 degraded, traversal 503, then non-degraded recovery | Redis snapshot unchanged; same Falkor facts return after in-place start | focused `IntegrationSlow` / Passed |
| 16 | `DegradationIntegrationTests.HybridSearch_AllBackendsStopped_ShouldReturn503AllBackendsUnavailable` | same / skipped | literal skip `26.3-ALL-BACKENDS-STATESTORE` | enable after truthful state-store-collapse API contract exists | `integration-fast` / Skipped (accepted) |
| 17 | `DegradationIntegrationTests.HybridSearch_AfterBackendRestart_ShouldReturnNonDegradedResult` | target #15 / consolidated | same request recovers without process restart | pre-stop graph fact remains without reseed | focused `IntegrationSlow` / Passed |
| 18 | `DegradationIntegrationTests.SingleAxisSearch_RedisStopped_ShouldReturn503BackendUnavailable` | same / skipped | literal skip `26.3-SINGLE-AXIS-REDIS-COLLAPSE` | enable with RediSearch-only fault or truthful control-plane-collapse contract | `integration-fast` / Skipped (accepted) |
| 19 | `TenantConfigurationIntegrationTests.ListTenants_ReturnsEnrichedSummaryWithCountsAndIndexHealth` | same / implemented | tenant list returns enriched active tenant | summary count/status matches indexed Redis data | `integration-fast` / Passed |
| 20 | `TenantConfigurationIntegrationTests.ListTenants_WhenOneBackendStopped_TenantStillListedWithUnknownOnThatAxis` | target #15 / consolidated | tenant remains listed during Falkor outage with graph unknown | tenant registry and Redis state remain intact | focused `IntegrationSlow` / Passed |
| 21 | `TenantConfigurationIntegrationTests.GetConfiguration_ReturnsComposedView_WithFullEmbeddingConfig` | same / implemented | composed configuration GET matches configured values | registry/config actor view matches persisted configuration | `integration-fast` / Passed |
| 22 | `TenantConfigurationIntegrationTests.GetConfiguration_UnknownTenant_Returns404TenantNotFound` | same / implemented | structured tenant-not-found 404 | no tenant-registry state is created | `integration-fast` / Passed |
| 23 | `TenantConfigurationIntegrationTests.PatchDisplayName_UpdatesRegistryAndReflectsInSubsequentGet` | same / implemented | PATCH and subsequent GET return new name with audit entry | tenant-registry hash/state contains new name | `integration-fast` / Passed |
| 24 | `TenantConfigurationIntegrationTests.PatchDisplayName_NonActiveTenant_Returns409` | same / implemented | non-active tenant PATCH returns lifecycle-specific 409 | seeded registry lifecycle/name remains unchanged | `integration-fast` / Passed |
| 25 | `TenantConfigurationIntegrationTests.PutEmbeddingConfig_BreakingChange_WithoutForceReindex_Returns409` | same / implemented | structured breaking-change 409 | byte/field-equivalent configuration remains unchanged | `integration-fast` / Passed |
| 26 | `TenantConfigurationIntegrationTests.PutEmbeddingConfig_BreakingChange_WithForceReindex_Returns200AndSetsReindexRequired` | same / implemented | forced update returns 200 and composed view flags reindex | persisted config has new dimensions and `ReindexRequired=true` | `integration-fast` / Passed |
| 27 | `TenantConfigurationIntegrationTests.PutEmbeddingConfig_RateLimitChange_PropagatesToRateLimiterOnNextIngest` | same / implemented | config update and ingest complete | tenant actor uses new ceiling and another tenant remains unchanged | `integration-fast` / Passed |
| 28 | `TenantConfigurationIntegrationTests.IngestMemoryUnit_EndToEnd_PersistsEmbeddingProviderAndModel` | same / implemented | memory-unit GET exposes provider/model | exact Redis hash fields match deterministic fake provider/model | `integration-fast` / Passed |

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Task 1 reconciled the live 29/28/1 audit counts at `dbe8b72`, froze all 28 originals plus explicit consolidations in the manifest/matrix, and replaced stale reasons with eight structured owner/trigger deferrals.
- Task 2 removed the IntegrationTests false-pass attribute, converted the one real AppHost proof to `[Fact]`, established eight literal structured skips, and added a normal-lane source guard plus a manifest/source/TRX verifier. The focused guard passed 2/2 and the verifier's four Docker-free unit tests passed.
- Task 3 added a server-instance-owned, atomic bounded embedding fault plan with deterministic 429/5xx recovery; all 21 fake-server tests passed. Shared ingestion API/store waits now live in `IngestionIntegrationTestDriver`, and FalkorDB control uses checked Aspire Stop/Start commands plus resource, connection, and `/ready` recovery waits instead of Docker discovery.
- Task 4 implemented real transient-success, provider-exhaustion, provider-429, and two-tenant limiter proofs. The provider HTTP client now leaves retry ownership to the durable provider-aware layer so `Retry-After` reaches the tenant actor; focused and `integration-fast` outcomes passed.
- Task 5 implemented the combined Falkor loss/recovery proof. A fixture-owned Falkor volume preserves graph facts when Aspire's Start command recreates the container, exact-name cleanup prevents volume leakage, hybrid/traversal contracts expose truthful degradation, and the focused post-fix run passed 1/1 in 57.6 seconds without reseeding.
- Task 6 implemented URL success/404, mixed directory batch, and all nine tenant configuration/provenance targets; infeasible private-host, load/performance, stage-barrier, and shared-Redis scenarios are literal skips linked to the eight structured deferrals. URL passed 2 with 1 accepted skip, directory passed 1 with 1 accepted skip, and tenant configuration passed 9/9.
- Task 7 completed after the user authorized closure of the broad-gate regressions. The complete and CI-fast container lanes are green with exactly the eight accepted deferred-work skips; both outcome/surface verifiers, all focused direct runs, the full Server.Tests suite, Release build, and whitespace gate pass. Story status is `review`.
- Code review closed all 12 patch findings: guards now reject no-op runnable tests and malformed deferrals, integration proofs cover embedding retries/failure activity, PDF extraction, and direct failed-state indexes, while workflow polling, Falkor recovery, fake-server disposal, fault-plan validation, and Docker cleanup fail safely and diagnostically.

### File List

- `.env.example`
- `_bmad-output/implementation-artifacts/26-3-integration-stub-closure.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `deploy/dapr/config.yaml`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `src/Hexalith.Memories.Mcp/appsettings.Development.json`
- `src/Hexalith.Memories.Server/Activities/Restore/RestoreReindexUnitActivity.cs`
- `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs`
- `src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `tools/integration-stub-targets.txt`
- `tools/verify-integration-stub-closure.py`
- `tests/tooling/integration_stub_closure/verify_integration_stub_closure_test.py`
- `tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreReindexUnitActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Architecture/IntegrationStubClosureGuardTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/TenantAuthorizationEndpointFilterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ImportEndpointStatusTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/EmbeddingProviderFaultPlan.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/IngestionIntegrationTestDriver.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/DirectoryIngestionIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/EmbeddingProviderFailureIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionRetryIntegrationTests.cs` (deleted)
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Mcp/McpAuthenticationIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Restore/BackupRestoreFidelityIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/RunnableSkippedFactAttribute.cs` (deleted)

### Change Log

- 2026-07-13: Implemented Story 26.3, closed all 28 audited false-pass integration stubs with 20 real proofs and 8 structured deferrals, added source/TRX guards, repaired broad-suite regressions exposed by the new gates, and moved the story to `review` after all required lanes passed.
- 2026-07-15: Applied all 12 adversarial code-review patches, repaired the provider-failure activity contract exposed by the new assertion, and revalidated the complete 251-test integration lane plus the strengthened 28-row closure gate.
