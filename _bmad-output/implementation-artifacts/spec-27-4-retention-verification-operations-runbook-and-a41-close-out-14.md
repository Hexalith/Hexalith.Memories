---
title: 'Story 27.4 authenticated qualification and A41 close-out'
type: 'feature'
created: '2026-10-12'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'b1d8014fa0dbacaf71528ddda74ff83cf19ca3ca'
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md'
  - '_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 lacks authenticated authority, a complete PG2 C1 predecessor, live C2–C6 evidence and A41 publication proof.

**Approach:** Authenticate operational records, complete C1 prerequisites, run the reviewed qualification producers, then verify independent acceptance and A41 publication.

## Boundaries & Constraints

**Always:** Bind authority and evidence to approved `PG-ONPREM-2` SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. Check current status and revocation. Require 25 accepted C1 gates in one eligible session, separate fault/purge grants, independent C5/C6 decisions, and disabled gate, empty Lease and zero lifecycle/clock replicas at finish.

**Never:** Treat issue comments, historical PG1 C1.15, closed-window C1.16, fixtures or role labels as execution authority. Contact an unapproved target, enable Production writes, weaken predecessor refusal, expose secrets, change the zero-replica overlay, or resolve A41 and its sprint action before publication proof.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Outcome | Failure Behavior |
|----------|---------------|------------------|------------------|
| Hold | Missing or revoked authority or C1 | `RECHECK_ONLY`; no target or A41 mutation | Name the missing fact |
| Qualification | Current authority, accepted C1, scoped C2–C4 grants | Immutable packets prove acknowledgement, retention, faults, audit continuity and privacy denial | Reject partial proof and restore disabled/empty/zero |
| Close-out | Accepted C0–C6, terminal proof and publication authority | Exact A41 transition, protected bytes intact, remote containment proven | Reject drift or missing approval |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — operational response contract; issue comments supply no grant.
- `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — authority and predecessor refusal; preserve fail-closed behavior.
- `tools/verify-access-telemetry-lifecycle.py`, `tools/access_telemetry_c{2,3,4}_producer.py` — fixed scenarios and close-out commands; use `docs/operations/access-telemetry-lifecycle.md`.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical C0–C6 matrix.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — verify any returned adoption, target, custody, principals and grants; record infeasibility if returned.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — verify PG2 C1.15 renewal, C1.16 eligibility or recapture, 23 other successors, 25 distinct gate passes and two authenticated bundle decisions.
- [ ] `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — verify separately approved interchange accepts the eligible predecessor; preserve refusal while absent.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — with scoped non-Production grants, run C0/C2–C4 and retain immutable packets and cleanup proof.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — record independent C5/C6 and terminal proof.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — apply preflight-approved A41 changes; verify postflight and remote publication.

**Acceptance Criteria:**
- Given missing or revoked authority, when readiness or a producer is invoked, then it refuses before target access and leaves Production writes and A41 unchanged.
- Given accepted same-session C1 and scoped qualification grants, when C0/C2–C4 run, then immutable packets prove required lifecycle behavior and final disabled/empty/zero state.
- Given accepted C0–C6 and terminal evidence, when close-out runs, then only registered A41 paths change, protected history stays byte-identical, and the published commit contains the approved transition.

## Implementation Notes

2026-10-12: Platform P1 v1 infeasibility at `e229df9a58ee6899f2b24ba51f652435bc36dc62` (`docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md`, SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`) is recorded in the I2 request. The C1.15-only packet is not enrolled. The owner-choice and operational-record blocker is `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/c1-15-only-signed-evidence-packet-decision.md`, SHA-256 `d85537fbbf3a50eeff913a6c3363fbd3f0046ae112b0ce3d1f2b37cbce36eb04`. The first execution task stays open because no adoption, target, custody, principal, or grant record was returned. Disposition remains `RECHECK_ONLY`.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — authority refusal tests pass.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — lifecycle and A41 guards pass.
