---
title: 'Epic 23 documentation verification pass'
type: 'chore'
created: '2026-08-02'
status: 'in-review'
baseline_commit: 'feac22bbc78c290f7ed8b1c2d5e1bfedf4dab133'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02-epic-23-documentation-verification.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 23 is complete, but its retrospective action remains open because five ingestion behaviors lack a current-tree, fail-closed documentation pass. Existing pages also contain claims needing correction.

**Approach:** Verify against source/tests, update the five authoritative docs, record re-runnable evidence/verdicts, and close only the named action after every checkpoint passes.

## Boundaries & Constraints

**Always:** Treat current source/tests as authoritative; qualify cancellation, best-effort cleanup, cache, retention, and truncated counts; record baseline, reviewer/date, inventory, commands/output, and one verdict per checkpoint; preserve Epic/story statuses.

**Ask First:** Halt for contradictory/missing proof, `unverifiable` verdicts, product defects, test changes, or scope beyond approved docs/tracking.

**Never:** Change product/test code, APIs, schemas, infrastructure, packages, submodules, planning artifacts, or runtime configuration; expose payload references, secrets, or tenant data; claim unverified provider quotas; close the action early.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Verifiable | Docs agree after correction | Five docs/evidence; `confirmed` or `corrected`; action `done` | Preserve unrelated state |
| Claim drift | Doc differs from behavior | Correct, reverify, mark `corrected` | Remove stale wording |
| Mismatch/blocker | Proof contradicts or cannot run | Mark `unverifiable`; action stays `open` | Halt for Correct Course |

</frozen-after-approval>

## Code Map

- `docs/operations/` and `docs/dev/` -- existing/new authoritative surfaces.
- `src/Hexalith.Memories.Server/{Ingestion,Infrastructure,Workflows}/` -- behavior authority.
- `tests/Hexalith.Memories.{Server,Contracts}.Tests/` -- behavioral/guard/serialization evidence.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- exact action row.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/epic-23-documentation-verification-2026-08-02.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/review-prompt-epic-23-documentation-verification-gap.md`
- `_bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02-epic-23-documentation-verification.md`
- `docs/dev/ingestion-workflow-determinism.md`
- `docs/operations/directory-ingestion.md`
- `docs/operations/failure-recovery.md`
- `docs/operations/index-rebuild.md`
- `docs/operations/rate-limiting.md`
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs`

## Tasks & Acceptance

**Execution:**
- [x] `docs/operations/rate-limiting.md` -- correct/cross-link admission, feedback, retry, cache, shared-key, and jitter claims.
- [x] `docs/operations/failure-recovery.md` -- cover payload lifecycle, retention, pre-claim denial, restoration, and bulk outcomes.
- [x] `docs/operations/directory-ingestion.md` -- create auth, safety, bounds, checkpoint/order, status, cancellation/failure, and cleanup guidance.
- [x] `docs/operations/index-rebuild.md` -- add provisioning/readiness ownership, additive upgrades, failures, and decisions.
- [x] `docs/dev/ingestion-workflow-determinism.md` -- cover capture, entry points/children, defaults, JSON, orchestration, and guard limits.
- [x] `_bmad-output/implementation-artifacts/epic-23-documentation-verification-2026-08-02.md` -- record five claims, evidence, and verdicts.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- close only the named action after all gates pass.

**Acceptance Criteria:**
- Given the final docs, when each claim is checked against current source/tests, then every checkpoint is complete, reproducible, and `confirmed` or `corrected`.
- Given edge cases, when guidance is read, then cancellation, cleanup, expiry, truncated skips, bulk outcomes, and pre-claim denial are precise.
- Given missing/incompatible indexes, when operators follow the guide, then ingestion never creates indexes and retry/reprovision/migration/rebuild paths remain distinct.
- Given a new entry point, when the guide is followed, then config is captured before slimming/scheduling, preserved through children/JSON, and consumed without mutable snapshots.
- Given all gates pass, when tracking is updated, then only the exact action changes to `done`; `epic-23`, `23-*`, and its retrospective remain unchanged.

## Spec Change Log

## Design Notes

The rows are one checkpoint set. Correct the upgrade matrix: syntactic accepts `cloudeventSubject`/`attributeTags`, raw semantic accepts `cloudeventSubject`, and NL accepts none.

## Verification

**Commands:**
- Run the exact Section V5 focused command from the approved proposal -- expected: 127/127, with additional scheduling/serialization proof recorded when run.
- `rg -n 'TryConsumeWithCeilingAsync|SourcePayloadReference|DirectorySchedulingParallelism|ITenantIndexReadinessVerifier|WorkflowConfiguration' docs src tests --glob '*.{md,cs}'` -- expected: five current anchor sets.
- `dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests -parallel none -noLogo` -- expected: operational documentation links and preserved runbook contracts pass.
- `git diff --check` -- expected: no whitespace errors.
