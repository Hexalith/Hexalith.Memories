---
title: 'Story 27.4: reliable telemetry admission before retention verification'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '382835acf193162aab7ba2942db570c93cd28b75'
context:
  - 'docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
  - '_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 combines functional testing with a qualification campaign. Its last run stopped at 124/125 rows. Independently, the queue rejects records when its consumer holds a lock despite spare capacity; a unit test requires this behavior. The live failure's cause remains unproved.

**Approach:** Fix admission, verify concurrent delivery in fast tests, then reuse the existing retention scenario. Spec 14 remains the execution contract.

## Boundaries & Constraints

**Always:** Preserve record/byte bounds, FIFO acknowledgement, retry accounting, expiry removal, nonblocking admission, business-request isolation and tenant/privacy checks. Use the current result report.

**Never:** Drop on consumer contention, wait for backend I/O/capacity in logging, retry-until-pass, lower 125/125 acceptance, shorten retention, replace Dapr/Platform or add a runner. No live launch, dependency update or Production/A41 completion is authorized.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/BoundedAccessTelemetryQueue.cs` — `TryEnqueue` conflates lock contention with capacity exhaustion. Consumer scans and queue-property reads share that lock.
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs` — owns expiry, batch delivery, retry and acknowledgement; preserve these semantics.
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleLogger.cs` — admission/drop accounting and queue metrics; successful admission currently reads locking properties.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryCheckpointTests.cs` — existing capacity and delivery tests; replace the test requiring contention rejection.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadTests.cs` — existing fixed-workload accounting coverage.
- `tools/access_telemetry_functional_retention.py` — existing first-cohort accounting/readback precedes the long wait; reuse unchanged acceptance and diagnostic capture.

## Tasks & Acceptance

**Execution:**
- [x] Queue and delivery worker paths above — use standard producer/consumer concurrency primitives, with one consumer owning pending delivery; retain both capacity bounds through acknowledgement. Choose the smallest implementation that satisfies the existing public-internal contract; no general queue framework.
- [x] Logger path above — ensure metric reads do not restore the consumer-lock dependency; preserve bounded counters and privacy.
- [x] Both test paths above — cover admission during consumer activity, simultaneous producers near both bounds, retry/partial acknowledgement, expiry and exact fixed-workload accounting. Use synchronization barriers rather than timing luck.
- [x] `spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md` and `tests/27-4-retention-verification-evidence.md` in this directory — record the correction and focused results. Live results belong only after a newly authorized run under the existing contract.

**Acceptance Criteria:**
- Given remaining capacity and a paused consumer, when another thread submits a valid record, then admission succeeds and returns before the consumer resumes.
- Given concurrent producers and pending retries, when capacity is exhausted or records are acknowledged/expired, then neither bound is exceeded and every removal is counted exactly once.
- Given 125 valid records and an available successful delivery dependency, when producer and worker overlap, then all 125 are enqueued and persisted without rejection/drop.
- Given only offline passing tests, when results are reported, then retention remains unproved; the full live finish requires existing native evidence, expiry/purge/newer-record assertions and verified Platform disposition.

## Implementation Notes

2026-10-22: Owner approved and continued this focused correction. Preserve the frozen intent. No live launch is authorized, so the repository's usual Aspire startup baseline is outside this scope; use focused offline tests. Initial worktree contains only this draft. HEAD has advanced to `8b9f27e6b23e6831b6645feda4478185eb914e29`; its six submodule pointer changes since the preserved baseline are pre-existing and excluded from this correction. Parent Story 27.4 and Epic 27 are already in progress; do not advance their status for this supporting correction.

2026-10-22 implementation: ConcurrentQueue handoff and consumer-owned pending FIFO retain atomic record/byte reservations. Producer metrics read snapshots; worker drain passes serialize; qualification admission accounting precedes publication. All four tasks and offline acceptance criteria verified against the full diff. The paused-consumer, capacity, retry/partial acknowledgement/expiry and 125-record overlap tests passed. No I/O matrix is present. [Current results](tests/27-4-retention-verification-evidence.md#offline-reliable-admission-correction--2026-10-22) contain exact commands: Debug build 0 warnings/errors; two xUnit classes 68/68; accounting diagnostics 3/3. External Hexalith package references remain despite the source-reference flag; no dependency changes were made. Live retention remains unproved.

## Verification

Investigation reproduced the mechanism using actual queue source in a temporary harness, with stub record serialization/types: `dotnet run --project /tmp/story27-4-queue-contention-5doijof2/QueueContention.csproj --configuration Release` exited 0. Admission returned `false/QueueFull` at 1/8192 records and 100/67108864 bytes while a scan held the gate; it succeeded after release. This is not a full Server integration test or proof of the earlier run's cause.

Existing diagnostic regression command: `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected_active_sequence.py' -k accounting -v` — 3/3 passed during investigation.

Build the Server test project in Debug with source references; execute the two named classes through its xUnit assembly and record exact commands/results. The unchanged live test needs fresh authorization: previous launches are consumed.

Proposed planning change: functional delivery gets its own completion status; the existing full-qualification checklist retains the separate campaign and A41 obligations. No status change is adopted here.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| B1: marker counts omit unpublished reservations | medium | Admission reserves capacity and records qualification ownership before publication, but CountByMarkerKey sees only incoming/pending records. Heartbeat drain reporting must remain conservative during this demonstrated state. Patch internal marker reservation accounting; preserve nonblocking producer behavior. |
| B2: oldest-head gap during incoming transfer | low | CollectIncoming removes the incoming head before publishing the pending snapshot; concurrent metrics can briefly report no/younger work. Directly preserve head visibility across transfer and recheck it in the reader. |
| B3: admission racing capacity release lacks coverage | medium | Current capacity tests finish admission before releasing entries; add a barrier-controlled varying-size admission/removal regression with exact final accounting. |
| B4: callback failure rollback lacks coverage | low | The new catch releases reservations but is untested. Cover a throwing callback and restored capacity/marker ownership. The sole production callback performs internal accounting; no general callback transaction framework is needed. |
| B5: drain cancellation lacks coverage | medium | Same gate-cancellation gap as V1; additionally verify active cancellation preserves unacknowledged reservations and permits a later drain. Patch focused tests. |
| B6: actual hosted shutdown overlap is not exercised | low | StopAsync calls the serialized method, but the added regression directly invokes two drains. Narrow the verification claim to exercised overlap and describe shutdown reuse as source inspection. |
| B7: exact payload comparison is overstated | low | The substitute retains canonical bytes and tests their size plus record IDs, not independently expected body equality. Correct report wording rather than add unrelated payload-contract tests. |
| B8: ordinary logging allocates qualification callback | low | Capturing lambda is currently unconditional even when accounting is absent. Pass null for ordinary logging; a direct local correction. |
| E1: fake-time acknowledgement timer registration race | medium | RunSegmentAsync publishes Emit before checking accounting/registering its delay; the test's single clock advance can precede timer creation. Complete controlled delivery before allowing emission to return, avoiding this wait entirely. |
| V1: cancellation while waiting for drain gate | medium | Verified gap: no current test cancels a waiter while another delivery holds the semaphore. Add the proposed controlled cancellation test; shared correction with B5. |
| V2: acknowledged head timestamp advancement | medium | Current partial-ack records have equal times and only count assertions. Use distinct timestamps and verify head/age after partial and final acknowledgement. |

All findings are bounded patches to implementation, tests or evidence wording; none changes the approved intent or requires another qualification framework.

2026-10-22 review closure: all eleven findings above were resolved in one bounded patch pass; none was deferred. Marker counts also remain within the heartbeat protocol maximum during simultaneous admission attempts. Final combined verification passed **73/73** (44 checkpoint and 29 qualification cases), with no errors, failures, skips or unrun cases, in **1.701 seconds**; unchanged Python accounting checks passed **3/3** in **0.147 seconds**. Debug build passed with zero warnings/errors. Two earlier checkpoint fixture failures were corrected before this final run. The result report records commands, logs and limits. This focused correction is done; parent Story 27.4, full qualification and A41 remain open.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-27-4-reliable-telemetry-admission.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md`
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleLogger.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/BoundedAccessTelemetryQueue.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryCheckpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadTests.cs`
