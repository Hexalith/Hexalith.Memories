---
title: 'Story 27.4 runtime qualification prerequisite handoff'
type: 'chore'
created: '2026-10-07'
status: 'done'
route: 'oneshot'
baseline_commit: '95dd8758f2f36d262a2b5c23269a0d73a2d7825b'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4's canonical handoff omits the runtime qualification gate's historical PG1 profile pin. The current Python producers use PG2, so passing their offline checks does not establish that the Server can accept an enabled PG2 qualification gate. The newly committed authenticated-interchange proposal records this separate prerequisite as P7.

**Approach:** Correct the canonical handoff with the verified runtime-versus-producer identities, the existing test's limitation, and a precise owner/reopen condition linked to P7. Retain the current-commit offline verification receipt. This bounded documentation slice leaves the remaining live Story 27.4 work pending: accepted PG2 C1.15, the 23 held owners, authenticated interchange, eligible same-session evidence and scoped Operations/Security execution authority remain unavailable. Runtime migration, prerequisite implementation, successor registration, target execution and close-out remain separate work.

</frozen-after-approval>

## Implementation Notes

- Planning found no supplied live prerequisite bundle or execution authority. Existing spec -5 completed its authorized repository tasks; conditional live verification and closure are still held. This slice repairs one concrete omission in the handoff rather than reimplementing predecessor or runtime migration work.
- Code Map: canonical `tests/27-4-retention-verification-evidence.md` contains the C0-C6 matrix; `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs` compares gate files with a historical PG1 constant; `tools/verify_access_telemetry_lifecycle.py` uses current PG2; `AccessTelemetryQualificationWorkloadTests.Gate_*` derives passing inputs from the runtime constant; separate interchange `implementation-tasks.md` P7 owns the migration decision. All executable sources and tests remain read-only.
- Changed only this supporting spec and the canonical evidence handoff. Added full runtime/producer hashes, the test limitation, proposal readiness, migration ownership and objective reopen evidence. The canonical matrix states, sprint tracker, historical records and Production configuration remain unchanged.
- Historical Context Classification: prior 27.4 specs and broad 27.3 story are historical-reference-only; the current canonical offline command and gate source are reverified narrow patterns; the new interchange spec is a proposed dependency contract, not implementation or authority. Slice Proof: one bounded documentation outcome, no successor registration or independently shippable prerequisite implementation absorbed here.
- Verification at full baseline: unchanged canonical ten-command block passed with receipt `/tmp/story-27-4-offline.DDRI57kg` (79 lifecycle cases, Debug/source-reference build with zero warnings/errors, 12 + 5 architecture guards). Read-only source comparison confirmed PG1 runtime versus PG2 producers. Two existing `Gate_*` cases passed with receipt `/tmp/story-27-4-runtime.Lp1vjnt4`; they do not prove PG2 compatibility.
- The resolved story key is `27-4-retention-verification-operations-runbook-and-a41-close-out`. Sprint synchronization to `in-progress` was a no-op because that is the existing state. Completion of this supporting slice cannot satisfy whole-story live acceptance or permit a sprint `review`/`done` transition. Preserve `in-progress`, A41/action open and disabled Production writes. The previously approved 27.4 intent requires explicit post-review authority before staging, committing or publishing; no such authority was provided for this run.
- Independent blind review completed. Four concrete documentation findings were classified individually and patched. Added the runtime migration condition directly to C2-C4 operator actions, stated that accountable people remain unassigned, retained the pre-edit build/log/dependency identifiers in the handoff and repeated the two gate tests with source/assembly identities recorded before and after. The rerun receipt `/tmp/story-27-4-runtime-bound.6lk0pkyl` passed all eight logged commands and exactly two cases; source HEAD and assembly hashes matched before/after. No findings were deferred.
- The initial post-edit architecture receipt `/tmp/story-27-4-handoff.IA6qoY2a` passed all 12 + 5 guards, XML SHA-256 `bced8226cafdeb5cac7d937b8bd371a729433791ad854e5cfa2e9344f13d2031`; it predates the review patches. Link resolution, protected-file byte comparison, CRLF and whitespace checks also passed. Final review-patch verification is recorded below after execution.
- Final review-patch receipt `/tmp/story-27-4-handoff-final.u1japj0y` at 2026-10-07 10:23:44 UTC: all six logged commands and script checks exited 0, exactly 12 + 5 architecture cases passed, zero failures/errors/skips/not-run. XML SHA-256 `c847f2257ffbc67c83c5f34d42fb910f372ec3245d57eaf8e1dd45e96097e1c6`; execution-time tracked diff SHA-256 `31abf301c4417c8850e517463af8fe986327381d881fb0753f3d1c2c92ac0265`. Links, receipt metadata, protected bytes, exact two-file footprint, CRLF and whitespace passed. Final receipt notes are recorded afterward. This supporting documentation slice is complete; the whole Story 27.4 is not. No staging, commit, publication or sprint transition occurred.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| :--- | :--- | :--- |
| B1: Matrix actions omitted runtime prerequisite | medium | Confirmed: C2-C4 directed execution after C1 alone despite the independently verified PG1 runtime pin. Patched each affected operator action to require approved and verified separately scoped P7 migration. Matrix states remain unchanged. |
| B2: Role handoff implied assigned ownership | low | Confirmed: P7 explicitly keeps accountable people unassigned. Patched the handoff to describe the proposed decision route and unassigned people; no appointment or approval inferred. |
| B3: Durable offline identifiers incomplete | low | Confirmed: current assembly, dependency and log identities existed only in the disposable receipt. Copied exact receipt-derived hashes and all nine nonrecursive dependency revisions into the canonical dated receipt. No custody claim added. |
| B4: Gate receipt omitted source/build binding | low | Confirmed: earlier XML/logs contained no execution-time assembly or source hash. Preserved that limitation and timestamp, then reran the two existing cases with matching full HEAD and assembly SHA-256 recorded before/after; eight commands and the block exited 0. No historical binding invented. |

