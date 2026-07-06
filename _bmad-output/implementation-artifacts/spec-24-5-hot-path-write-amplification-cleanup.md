---
title: '24.5 Hot-Path Write-Amplification Cleanup'
type: 'refactor'
created: '2026-07-06T08:32:08+02:00'
status: 'done'
baseline_revision: '4126ac12e3e105d5a03478025e8fae833b3846f8'
final_revision: '3b96b0d1797ddc36b43be13ea0becc14263391c2'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** Several operational hot paths still amplify durable writes or unbounded state: corpus-stat reads re-save actor state, case activity streams are untrimmed and scanned for status, case-scoped searches append durable activity records, the replay gate enumerates all workflows on every startup poll, and the NL retry queue uses serialized payloads as sorted-set members. Under load this inflates Redis/Dapr writes, status latency, and memory growth.

**Approach:** Move read paths back to read-only behavior, bound activity and retry structures, and replace broad replay-gate enumeration with an app-owned in-flight registry updated by the ingestion scheduler/status reader. Preserve EventStore as domain source of truth and keep Redis/FalkorDB state as rebuildable projections.

## Boundaries & Constraints

**Always:** Keep tenant and case IDs explicit in all keys and contracts; keep workflow orchestration replay-safe; keep all durable mutation paths idempotent; use bounded configurable defaults with safe clamps; preserve existing structured errors and metric/tag naming from Story 24.4.

**Block If:** The Dapr Workflow SDK cannot read individual workflow state by app-tracked instance ID; Redis APIs cannot express bounded stream or id-keyed retry operations without losing current retry semantics; removing per-search activity writes would break an explicit contract test or public API requirement.

**Never:** Do not add a new authoritative domain store; do not write durable state merely to answer a read; do not use Redis key scans on hot paths or startup gates when an app-owned index can be maintained; do not broaden into failed-unit retention, permanent dedup retention, event-type telemetry cardinality, directory-batch sharding, or retry-jitter cache cleanup in this story.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Corpus stats read | Existing `corpusStats` actor state | Getter returns the cached snapshot without `SetStateAsync` | Corrupt/missing state returns safe zero/default without a read-side durable write |
| Activity event append | Case mutation or ingestion milestone | Redis stream append uses bounded max length and updates summary counter/timestamp | Redis failure returns `false` and logs warning as today |
| Case-scoped search | Successful search with `caseId` | Search returns results and emits telemetry, but no durable `SearchExecuted` activity event | Search behavior and degraded errors remain unchanged |
| Replay gate startup | Registry has tracked ingestion instance IDs | Gate reads only tracked IDs, removes terminal/missing entries, and delays for active ones | Sidecar/read failure keeps existing fail-open logging behavior |
| NL retry duplicate | Same tenant/memory unit enqueued twice | One live retry entry remains, keyed by memory-unit ID with latest bounded payload/attempt state | Corrupt payloads are ignored and do not enter workflow scheduling |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs` -- removes read-side actor state saves; scheduled refresh remains writer.
- `tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs` -- invert tests that currently pin read writes.
- `src/Hexalith.Memories.Server/Cases/CaseActivityService.cs` -- bounded `XADD`, summary counter/timestamp, no full-stream failed-count scan.
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseActivityServiceTests.cs` -- stream trim and summary-read contract tests.
- `src/Hexalith.Memories.Server/Program.cs` -- remove per-search durable activity append; register new options/registry services.
- `src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowInFlightRegistry.cs` -- app-owned tracked-ingestion abstraction.
- `src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs` -- Redis sorted-set implementation with lookup hash and initialized marker; no unchecked pruning before Dapr status observation.
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs` -- track instance ID before scheduling and remove the tracked candidate if scheduling fails before acceptance.
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowStateReader.cs` -- remove tracked IDs when status reads observe missing/terminal state.
- `src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs` -- count tracked IDs, with a one-time public-SDK enumeration fallback for uninitialized empty registries during rollout.
- `tests/Hexalith.Memories.Server.Tests/Hosting/WorkflowReplaySafetyHostedServiceTests.cs` -- registry-first gate behavior, pruning, initialization fallback, and canceled-state tests.
- `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs` -- id-keyed retry queue, tenant backlog set, live/dead caps, stale-payload guards, legacy tenant discovery, and live-overflow dead-lettering.
- `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptions.cs` -- retry queue cap options.
- `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedService.cs` -- canceled workflow status treated as terminal for retry finalization.
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistryTests.cs` -- duplicate/id-keyed/cap/backlog-set tests.
- `docs/dev/eventstore-integration.md`, `docs/dev/consistency.md` -- update activity/retry/replay-state operational notes if behavior wording changes.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs` -- return existing actor state directly, remove `PersistStatsBeforeReturnAsync`, and make scheduled refresh skip unchanged state.
- [x] `tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs` -- assert read methods do not call `SetStateAsync`; assert refresh still writes at most once.
- [x] `src/Hexalith.Memories.Server/Cases/CaseActivityService.cs` -- add bounded stream append and summary state for failed count/last activity; change reads to use summary state.
- [x] `tests/Hexalith.Memories.Server.Tests/Cases/CaseActivityServiceTests.cs` -- verify bounded stream append and no full stream scan for failed count/timestamp.
- [x] `src/Hexalith.Memories.Server/Program.cs` -- delete the durable search activity write and wire activity/replay registry options/services.
- [x] `src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowInFlightRegistry.cs` -- add the app-owned tracked-ingestion abstraction used by scheduler, status reader, and replay gate.
- [x] `src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs` -- implement the abstraction with a tenant-aware Redis sorted set, lookup hash, initialization marker, and lookup-independent removal fallback.
- [x] `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs` -- track the instance ID before `ScheduleNewWorkflowAsync` and remove the tracked candidate if scheduling fails before acceptance.
- [x] `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowStateReader.cs` -- remove tracked IDs when a status read observes missing or terminal workflow state.
- [x] `src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs` -- count registry-tracked IDs, prune missing/terminal tracked entries, and use a one-time rollout fallback only while the empty registry is uninitialized.
- [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionWorkflowInFlightRegistryTests.cs` -- prove registry add/list/remove, initialized marker, no unchecked stale/cap pruning, and lookup-independent remove behavior.
- [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs` -- prove successful schedules are tracked before scheduling and failed schedules remove the candidate.
- [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowStateReaderTests.cs` -- prove missing, terminal, and canceled status reads prune tracked IDs.
- [x] `tests/Hexalith.Memories.Server.Tests/Hosting/WorkflowReplaySafetyHostedServiceTests.cs` -- prove startup checks tracked IDs, removes terminal/missing tracked entries, and performs the rollout fallback only while uninitialized.
- [x] `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs`, `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptions.cs`, `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedService.cs` -- convert live/dead queues to stable memory-unit members with payload hashes, tenant backlog set, stale-payload guards, legacy discovery, bounded live overflow to dead-letter, and canceled terminal handling.
- [x] `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistryTests.cs`, `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedServiceTests.cs` -- prove duplicate enqueue coalesces, corrupt payloads are skipped, tenant set avoids scans, legacy tenants are discovered, stale payloads do not delete newer work, and live overflow moves to dead-letter.
- [x] `docs/dev/eventstore-integration.md`, `docs/dev/consistency.md`, this spec -- record final operational semantics and verification results.

**Acceptance Criteria:**
- Given existing corpus statistics actor state, when document count, average length, or full stats are read, then no durable actor state write occurs.
- Given case activity is recorded, when Redis stores the event, then the stream is capped and failed-count/last-activity reads do not scan the full stream.
- Given a case-scoped search succeeds, when the response is returned, then no durable case activity event is appended for that read.
- Given ingestion workflows are scheduled through the server scheduler, when the replay safety gate starts, then it counts app-tracked ingestion IDs, prunes missing or terminal IDs, and uses broad enumeration only for the uninitialized empty-registry rollout fallback.
- Given the same NL retry work item is enqueued repeatedly, when the retry backlog is read, then one live item keyed by tenant and memory unit is returned and queue memory is bounded by configured caps.

## Spec Change Log

## Review Triage Log

### 2026-07-06 — Review pass

- intent_gap: 0
- bad_spec: 0
- patch: 10: (high 8, medium 2, low 0)
- defer: 1: (high 0, medium 1, low 0)
- reject: 1
- addressed_findings:
  - `[high]` `[patch]` Added a rollout-safe enumeration fallback for an uninitialized empty replay registry and mark it initialized once no active ingestion workflows are found.
  - `[high]` `[patch]` Moved ingestion workflow tracking before `ScheduleNewWorkflowAsync`; failed scheduling removes the candidate instead of leaving an invisible accepted workflow window.
  - `[high]` `[patch]` Removed unchecked stale/over-cap registry pruning and made removal recover when the lookup hash is missing.
  - `[medium]` `[patch]` Treated `Canceled` workflow status as terminal for replay-gate, ingestion status-reader, and NL retry finalization paths.
  - `[medium]` `[patch]` Added case activity summary backfill for legacy streams when failed-count or last-activity summary fields are missing.
  - `[high]` `[patch]` Added legacy NL retry tenant discovery when the tenant backlog set is absent, then populate the set for subsequent polls.
  - `[high]` `[patch]` Added stale-payload guards so retry completion/increment cannot delete newer live retry work.
  - `[high]` `[patch]` Moved recoverable live retry overflow to dead-letter instead of silently deleting it.
  - `[high]` `[patch]` Updated dead-letter re-enqueue docs to remove the dead copy after manually re-enqueueing.
  - `[high]` `[patch]` Removed the dead in-flight registry cap/stale options to avoid an operator-facing knob that no longer prunes unchecked candidates.
- deferred_findings:
  - `[medium]` `[defer]` Case activity stream append and summary update remain separate Redis operations; added `24.5-CASE-ACTIVITY-ATOMIC-SUMMARY` to `deferred-work.md`.

### 2026-07-06 — Review pass (follow-up)

- intent_gap: 0
- bad_spec: 0
- patch: 0
- defer: 6: (high 0, medium 6, low 0)
- reject: 8
- addressed_findings:
  - none
- deferred_findings:
  - `[medium]` `[defer]` Case activity legacy `failedCount` one-time backfill is pre-empted by the write-path `HashIncrement` (and undercounts on maxlen-trimmed streams); added `24.5-CASE-ACTIVITY-BACKFILL-PREEMPTED` to `deferred-work.md`.
  - `[medium]` `[defer]` NL retry tenant backlog-set TOCTOU (non-atomic enqueue vs check-then-`SREM` prune) can strand live entries and orphan payloads; added `24.5-NL-RETRY-TENANT-SET-ATOMICITY`.
  - `[medium]` `[defer]` NL retry legacy-tenant discovery runs only when the tenant set is entirely empty, stranding pre-24.5 queues after a rollout; added `24.5-NL-RETRY-LEGACY-TENANT-DISCOVERY`.
  - `[medium]` `[defer]` NL retry `CompleteAsync`/`IncrementAttemptsAsync` skip the optimistic condition on a null payload, clobbering a concurrent fresh re-enqueue; added `24.5-NL-RETRY-NULL-PAYLOAD-CLOBBER`.
  - `[medium]` `[defer]` In-flight ingestion registry is unbounded and pruned only by status polls/startup, inflating the startup drain (and O(N) lookup-miss removal); added `24.5-INFLIGHT-REGISTRY-UNBOUNDED`.
  - `[medium]` `[defer]` Replay-gate initialization marker set on first `TrackAsync` can disable the rollout enumeration fallback for a sibling replica during a multi-replica upgrade; added `24.5-REPLAY-GATE-ROLLOUT-MARKER`.
- rejected_findings (noise or by-design, dropped): stream/summary non-atomic divergence and the backfill-vs-increment race (already covered by `24.5-CASE-ACTIVITY-ATOMIC-SUMMARY` plus the new backfill-preempted defer); dead-letter/live duplicate on overflow (idempotent re-processing, inspection-only); `ListAsync` bad-score → designed fail-open (non-operational trigger); scheduler cancellation preserves tracking (replay-safe over-count that self-heals via later prune); enumeration fallback re-running during the one-time rollout drain (accurate counts are wanted in that window); corpus stats zero-on-miss (explicitly spec-designed safe default).

## Design Notes

Deferred findings from planning: failed-unit backlog retention, permanent dedup cleanup/TTL, observed event-type telemetry cardinality, directory-batch state sharding, and retry jitter cache cleanup are real but intentionally out of this story because they are independently shippable and would exceed the sprint-change anchor for A46. Record any newly discovered hard blockers in `deferred-work.md` instead of expanding this spec mid-run.

## Implementation Notes

- Corpus statistics actor getters are read-only and return cached actor state or zero/default state when missing; scheduled refresh persists only when corpus count or average length changes.
- Case activity writes use a bounded Redis stream plus a summary hash for failed-count and last-activity reads. Missing legacy summaries backfill once from the stream. Successful case-scoped search no longer writes a durable `SearchExecuted` activity event.
- The replay startup gate now reads an app-owned Redis in-flight registry populated before workflow scheduling and pruned only after status reads or gate checks observe missing/terminal workflows. First rollout from an empty uninitialized registry uses a public Dapr enumeration fallback; later startups use the registry.
- Natural-language retry queues use stable memory-unit sorted-set members, separate live/dead payload hashes, a tenant backlog set, stale-payload guards, and configurable live/dead caps. Live overflow moves recoverable records to dead-letter instead of deleting them.
- Legacy pre-24.5 NL retry queue members that stored the full record JSON in the sorted set are still readable and removable, and legacy tenant keys are discovered if the backlog tenant set is absent, so queued retry work is not stranded during deployment.

## File List

- [../../src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs](../../src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs)
- [../../src/Hexalith.Memories.Server/Cases/CaseActivityOptions.cs](../../src/Hexalith.Memories.Server/Cases/CaseActivityOptions.cs)
- [../../src/Hexalith.Memories.Server/Cases/CaseActivityService.cs](../../src/Hexalith.Memories.Server/Cases/CaseActivityService.cs)
- [../../src/Hexalith.Memories.Server/Program.cs](../../src/Hexalith.Memories.Server/Program.cs)
- [../../src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowInFlightRegistry.cs](../../src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowInFlightRegistry.cs)
- [../../src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowInFlightEntry.cs](../../src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowInFlightEntry.cs)
- [../../src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs](../../src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs)
- [../../src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs](../../src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs)
- [../../src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowStateReader.cs](../../src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowStateReader.cs)
- [../../src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs](../../src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs)
- [../../src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs](../../src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs)
- [../../src/Hexalith.Memories.Server/NaturalLanguage/IFailedNaturalLanguageEmbeddingRegistry.cs](../../src/Hexalith.Memories.Server/NaturalLanguage/IFailedNaturalLanguageEmbeddingRegistry.cs)
- [../../src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptions.cs](../../src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptions.cs)
- [../../src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedService.cs](../../src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedService.cs)
- [../../tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs](../../tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/Cases/CaseActivityServiceTests.cs](../../tests/Hexalith.Memories.Server.Tests/Cases/CaseActivityServiceTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionWorkflowInFlightRegistryTests.cs](../../tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionWorkflowInFlightRegistryTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs](../../tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowStateReaderTests.cs](../../tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowStateReaderTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/Hosting/WorkflowReplaySafetyHostedServiceTests.cs](../../tests/Hexalith.Memories.Server.Tests/Hosting/WorkflowReplaySafetyHostedServiceTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistryTests.cs](../../tests/Hexalith.Memories.Server.Tests/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistryTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedServiceTests.cs](../../tests/Hexalith.Memories.Server.Tests/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedServiceTests.cs)
- [../../docs/dev/eventstore-integration.md](../../docs/dev/eventstore-integration.md)
- [../../docs/dev/consistency.md](../../docs/dev/consistency.md)

## Verification

**Commands:**
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -- expected: server builds with warnings as errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- expected: focused test project builds.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~CorpusStatisticsActorTests|FullyQualifiedName~CaseActivityServiceTests|FullyQualifiedName~WorkflowReplaySafetyHostedServiceTests|FullyQualifiedName~FailedNaturalLanguageEmbeddingRegistryTests|FullyQualifiedName~NaturalLanguageEmbeddingRetryHostedServiceTests|FullyQualifiedName~IngestionWorkflowInFlightRegistryTests|FullyQualifiedName~DaprIngestionWorkflowSchedulerTests|FullyQualifiedName~DaprIngestionWorkflowStateReaderTests"` -- expected: hot-path cleanup regression tests pass.
- `rg "RecordSearchActivity|StreamRangeAsync\\(key, null, null\\)|ListInstanceIdsAsync\\(" src/Hexalith.Memories.Server tests/Hexalith.Memories.Server.Tests` -- expected: no removed search/full-scan patterns remain; `ListInstanceIdsAsync` appears only in the rollout fallback and its tests.
- `git diff --check` -- expected: no whitespace errors.

**Results:**
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -- passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- passed, 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~CorpusStatisticsActorTests|FullyQualifiedName~CaseActivityServiceTests|FullyQualifiedName~WorkflowReplaySafetyHostedServiceTests|FullyQualifiedName~FailedNaturalLanguageEmbeddingRegistryTests|FullyQualifiedName~NaturalLanguageEmbeddingRetryHostedServiceTests|FullyQualifiedName~IngestionWorkflowInFlightRegistryTests|FullyQualifiedName~DaprIngestionWorkflowSchedulerTests|FullyQualifiedName~DaprIngestionWorkflowStateReaderTests"` -- passed, 81/81 after review fixes.
- `rg "RecordSearchActivity|StreamRangeAsync\\(key, null, null\\)|ListInstanceIdsAsync\\(" src/Hexalith.Memories.Server tests/Hexalith.Memories.Server.Tests` -- returned only the intentional `ListInstanceIdsAsync` rollout fallback and tests; no removed search/activity/full-scan patterns remain.
- `git diff --check` -- passed.

## Auto Run Result

**Summary:** Implemented Story 24.5 hot-path write-amplification cleanup: corpus stats reads are read-only, case activity streams are bounded with summary reads, case search no longer writes durable activity, replay safety uses an app-owned in-flight registry with rollout fallback, and NL retry queues use stable members plus payload hashes.

**Files changed:** Server actors, case activity, ingestion scheduler/state reader/replay gate, NL retry registry/options/hosted service, DI registration, focused server tests, and operator docs/spec/deferred-work.

**Review findings breakdown:** patch 10 (high 8, medium 2), defer 1 (medium), reject 1. Review-driven fixes included replay rollout fallback, track-before-schedule, no unchecked registry pruning, canceled terminal handling, legacy summary/tenant compatibility, stale retry payload guards, live-overflow dead-lettering, and dead config removal.

**Follow-up review recommendation:** true. The final pass applied behavior-significant review fixes across startup safety, retry data retention, and compatibility paths.

**Verification performed:** server build passed with 0 warnings/errors; test project build passed with 0 warnings/errors; focused regression test slice passed 81/81; pattern scan found only the intentional replay rollout fallback/tests; `git diff --check` passed.

**Residual risks:** case activity stream append and summary hash update are not atomic; tracked as `24.5-CASE-ACTIVITY-ATOMIC-SUMMARY` in `deferred-work.md`.

### Follow-up review (2026-07-06)

**Summary:** Independent follow-up review pass (Blind Hunter + Edge Case Hunter, Opus capability) over the full `4126ac1..HEAD` diff. No new code changes were made: every surviving finding was either an intentional/by-design behavior or a real but scope-expanding concurrency/migration/rollout edge whose correct fix (Redis atomicity, a migration sweep, terminal-state pruning, or rollout-marker redesign) exceeds a trivial in-run patch and is independently shippable. Per the spec's Design Notes, these were recorded in `deferred-work.md` rather than expanding the story mid-run.

**Triage:** intent_gap 0, bad_spec 0, patch 0, defer 6 (medium 6), reject 8. No spec amendment or implementation loopback was triggered.

**Deferred (new ledger entries):** `24.5-CASE-ACTIVITY-BACKFILL-PREEMPTED`, `24.5-NL-RETRY-TENANT-SET-ATOMICITY`, `24.5-NL-RETRY-LEGACY-TENANT-DISCOVERY`, `24.5-NL-RETRY-NULL-PAYLOAD-CLOBBER`, `24.5-INFLIGHT-REGISTRY-UNBOUNDED`, `24.5-REPLAY-GATE-ROLLOUT-MARKER`.

**Rejected (by-design / non-operational / already-covered):** stream/summary non-atomic divergence and the backfill-vs-increment race (covered by the existing atomic-summary + new backfill-preempted defers); dead-letter/live duplicate on overflow (idempotent, inspection-only); `ListAsync` bad-score → designed fail-open; scheduler cancellation preserving tracking (replay-safe over-count that self-heals); enumeration fallback re-running during the one-time rollout drain; corpus stats zero-on-miss (spec-designed default).

**Verification performed:** Findings were verified against the post-change source (`RedisIngestionWorkflowInFlightRegistry.cs`, `WorkflowReplaySafetyHostedService.cs`, `CaseActivityService.cs`, `FailedNaturalLanguageEmbeddingRegistry.cs`, `DaprIngestionWorkflowScheduler.cs`) before triage; no build/test rerun was needed because no code changed this pass.

**Follow-up review recommendation:** false — this pass made no review-driven code changes; the deferred items are tracked for focused, independently-scheduled work.
