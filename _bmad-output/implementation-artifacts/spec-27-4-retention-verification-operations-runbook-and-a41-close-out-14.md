---
title: 'Story 27.4 sole-owner qualification and A41 close-out'
type: 'feature'
created: '2026-10-12'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'b1d8014fa0dbacaf71528ddda74ff83cf19ca3ca'
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - 'docs/operations/access-telemetry-owner-handoff.md'
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md'
  - '_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md'
  - '_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 needs an owner-approved bounded run, complete PG2 C1 predecessor, live C2–C6 evidence and A41 publication proof.

**Approach:** Use the adopted sole-owner policy, prepare one bounded run record for owner approval, collect and verify C1 and qualification evidence, then record owner acceptance and verify A41 publication.

## Boundaries & Constraints

**Always:** Bind authority and evidence to approved `PG-ONPREM-2` SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. Check current owner approval, expiry and withdrawal. Require 25 verified C1 gates in one eligible session, explicit fault/purge scopes, distinct owner-accepted C5/C6 records, and disabled gate, empty Lease and zero lifecycle/clock replicas at finish. Use private owner-controlled hashed/readback evidence for non-Production qualification; separate receipt services, independent human reviewers and external immutable custody are not required for this mode.

**Never:** Treat policy adoption, historical PG1 C1.15, closed-window C1.16, fixtures or role labels as a live run approval. Do not claim independent human review for the sole owner’s decisions. Contact an unapproved target, enable Production writes, weaken predecessor refusal, expose secrets, change the zero-replica overlay, or resolve A41 and its sprint action before publication proof.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Outcome | Failure Behavior |
|----------|---------------|------------------|------------------|
| Hold | Missing, expired, changed or withdrawn owner run approval or C1 | `RECHECK_ONLY`; no target or A41 mutation | Name the missing fact |
| Qualification | Current owner run approval, verified C1, scoped C2–C4 actions | Hash-bound packets prove acknowledgement, retention, faults, audit continuity and privacy denial | Reject partial proof and restore disabled/empty/zero |
| Close-out | Owner-accepted C0–C6, terminal proof and publication approval | Exact A41 transition, protected bytes intact, remote containment proven | Reject drift or missing approval |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — current owner handoff; earlier institutional response request is historical.
- `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — authority and predecessor refusal; preserve fail-closed behavior.
- `tools/verify-access-telemetry-lifecycle.py`, `tools/access_telemetry_c{2,3,4}_producer.py` — fixed scenarios and close-out commands; use `docs/operations/access-telemetry-lifecycle.md`.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical C0–C6 matrix.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — prepare the owner run record with actual target, private evidence root, exact actions and windows; obtain scoped owner approval.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — verify PG2 C1.15 renewal, C1.16 eligibility or recapture, 23 other successors, 25 distinct gate passes and distinct owner Operations/Security records.
- [ ] `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — implement/verify migrated owner-mode interchange accepts the eligible predecessor; preserve refusal while absent.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — with the approved non-Production action scopes, run C0/C2–C4 and retain hash-bound packets and cleanup proof.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — record owner-accepted C5/C6 and terminal proof.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — apply preflight-approved A41 changes; verify postflight and remote publication.

**Acceptance Criteria:**
- Given missing, withdrawn, expired or changed owner run approval, when readiness or a producer is invoked, then it refuses before target access and leaves Production writes and A41 unchanged.
- Given accepted same-session C1 and scoped qualification approval, when C0/C2–C4 run, then hash-bound packets prove required lifecycle behavior and final disabled/empty/zero state.
- Given accepted C0–C6 and terminal evidence, when close-out runs, then only registered A41 paths change, protected history stays byte-identical, and the published commit contains the approved transition.

## Implementation Notes

2026-10-12: Platform P1 v1 infeasibility at `e229df9a58ee6899f2b24ba51f652435bc36dc62` (`docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md`, SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`) is recorded in the I2 request. The C1.15-only packet is not enrolled. The owner-choice and operational-record blocker is `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/c1-15-only-signed-evidence-packet-decision.md`, SHA-256 `d85537fbbf3a50eeff913a6c3363fbd3f0046ae112b0ce3d1f2b37cbce36eb04`. The first execution task stays open because no adoption, target, custody, principal, or grant record was returned. Disposition remains `RECHECK_ONLY`.

2026-10-12 current-pin recheck: Memories HEAD `f4c7d68f619f376debca440387428139a61080cc` records Platform `977f2e01347226ac727b707aee4f140f5dc8b010`, which contains the same P1 v1 infeasibility report and unchanged cited contract/qualification blobs. The [response audit](pg2-c1-i2-operations-security-request.md#2026-10-12-current-pin-response-audit-for-story-274-spec-14) records that no authenticated operational response, current status/revocation proof, accepted eligible C1 or separate action grant was supplied. Preserve the refusal and `RECHECK_ONLY` disposition. All six execution tasks remain open; live qualification, independent decisions, terminal proof and A41 publication remain pending. No target or A41 mutation occurred.

## Spec Change Log

- 2026-10-12: Explicit owner “yes” adopts `sole-owner-qualification/2026-10-12` and renegotiates the frozen approval/custody requirements under the approved sprint change. Same owner may produce and accept evidence with mandatory self-review disclosure. Technical checks and live/publish scopes remain required. Prior implementation notes are dated history under the superseded policy; they do not reinstate institutional prerequisites.

## Review Triage Log

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — authority refusal tests pass.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — lifecycle and A41 guards pass.


2026-10-12 executed results: the authenticated-interchange suite passed 242 tests in 85.213s and the lifecycle suite passed 83 tests in 34.426s, both exit 0 with bare `OK` and zero failures/errors/skips. Local receipt `/tmp/story-27-4-spec14-oo85qkwd` retains commands, logs, exits, source/dependency identity and byte baselines; durable identifiers and the offline-only boundary are in the [canonical evidence addendum](tests/27-4-retention-verification-evidence.md#2026-10-12-spec-14-current-pin-readiness-hold). `git diff --check` exited 0 before these notes. The hold row is verified by refusal tests; no authorized live qualification or close-out row is claimed as executed.


2026-10-12 owner-mode first slice: the separately tracked
[owner-record inspection spec](spec-pg2-c1-sole-owner-run-record-inspection.md) is
done after three review layers, corrected timezone regressions and 25 focused /
267 full affected tests, with no deferred review finding. Policy adoption and
pure inspection do not complete the six live tasks above; existing deployed
consumers remain closed. See the canonical evidence addendum for exact results
and preservation proof.


2026-10-12 owner approval/acceptance continuation: the owner said “I approve and
accept”, accepting the sole-owner approach and presented 25 focused / 267 full
offline test results. The
[acceptance record](27-4-owner-approval-and-acceptance-2026-10-12.md) retains
exact snapshots in a private readback-checked archive. Bounded read-only queries
reached the configured cluster: the existing telemetry deployment has one ready
replica and the isolated qualification namespace is absent. No target mutation
or live gate acceptance occurred. Future collectors, full owner-mode integration
and concrete mutation setup remain agent implementation work; no repeat policy
approval or institutional grant is requested.
