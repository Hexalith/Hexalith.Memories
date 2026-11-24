---
title: 'Complete ten ordered deferred work items'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">
## Intent

Resume the priority-ordered deferred register after DW-288 and complete ten eligible items. Record the reason for each blocked or obsolete item. Give every completed item its own Conventional Commit and ledger evidence. Run the complete build and test suite before pushing main.

- DW-291 and DW-294 add executable compiler guards for the Dapr conversation suppression and ordinal metadata dictionary construction.
- DW-295, DW-298 and DW-301 verify replay-safety startup behavior when the gate cannot count, a query times out or the total budget expires.
- DW-296 uses the existing friend assembly for direct workflow metadata testing.
- DW-297 distinguishes note-only telemetry from repair findings.
- DW-307 gives CLI notes a consistent layout. DW-315 seeds the nightly trace through the real Redis projection without an SDK context proxy. DW-331 verifies integration inventory symbols at compile time.

</frozen-after-approval>

## Implementation Notes

- Baseline main and origin/main both point to 9384bda139de2590154d019bbfa86e65467a0d52. The AppHost started, but its EventStore process and OpenBao container exited during the initial resource inspection.
- The orphan CLI addition requires a migration ownership decision. Dapr.AI 1.19.1 has no choice log probabilities and the embedding limiter exposes no utilization query. The workflow upload condition belongs to Hexalith.CiCd.
- The integration project generates compile-time references for the 21 entries in the current surface inventory. An isolated inventory with a missing type and method produced CS0234 and CS0117.

## Review Triage Log

- High, deferred DW-300. The released Contracts package lacks the enum value and old readers reject the proposed wire token. The current batch leaves the V1 enum unchanged.
- Medium, corrected DW-298 and DW-301. The first cancellation callback owns classification. Startup retains an already-classified query timeout and registry reads check cancellation before starting another cleanup.
- Medium, corrected DW-291 and DW-294. The compiler guard exercises Debug and Release with evaluated project settings. The dictionary guard scans both EventStore profiles and rejects collection expressions that construct a default dictionary.
- Medium, corrected DW-297. The runbook covers old Information 8201 entries and explains which parts of a truncated note the log can recover.
- High, corrected DW-315. The initial full build found the linked nightly seed caller. The helper now lives in that nightly partial and the CLI assertion checks the seeded memory-unit ID.

## Verification defects

- The full fast integration run failed to initialize the Testcontainers resource reaper for the shared FalkorDB fixture. All 19 affected graph cases passed in one isolated rerun against the same binaries. The same fixture-start failure also occurred in the previous batch. Investigate reaper startup under concurrent host load if it recurs.
- The full run also saw a transient nonzero exit from the disposable runtime secret-store bootstrap probe. The same case passed in an isolated rerun on Dapr 1.19.1. The wrapper did not surface the subprocess diagnostic in the failed assertion. Retain subprocess output on recurrence to distinguish a startup timeout from a contract failure.
- The unfiltered run and isolated retries cover 642 passing expanded fast integration cases and nine existing skips. The TRX acceptance verifier found passing evidence for all 21 required surfaces.

- The nightly topology restart hit a Redis host-port readiness timeout and left three following cases without a usable fixture. All four passed independently after building the EventStore host prerequisite. One isolated startup also hit a gRPC port collision between the Memories and clock sidecars. A fresh topology passed that case.
- The benchmark fixture could not connect to Redis from the host even though its container health check passed. All three affected cases passed together on the isolated rerun.
- The complete .NET suite covered 6,349 passing cases and ten existing skips across all twelve projects. Both solution builds had zero warnings and errors.
