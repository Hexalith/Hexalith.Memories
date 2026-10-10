---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-10'
status: 'ready-for-dev'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - 'docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 has reviewed producers but lacks authenticated PG-ONPREM-2 authority and an accepted 25-gate C1 predecessor. C1-C6 are pending, Production writes disabled, and A41 open.

**Approach:** Follow the owner-selected prerequisite route: authenticate Platform decisions and grants, consume same-session C1, execute scoped qualification, obtain independent decisions, then close A41 after terminal and publication checks.

## Boundaries & Constraints

**Always:** Use exact profile SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. Bind source, authority, session, registered/done successors and 25 separately accepted C1 gates. Require scoped action/custody grants, separate C5/C6 reviewers and final disabled/empty/zero cleanup.

**Never:** Treat planning approval, issue #57, PG1 evidence, closed-window C1.16, labels or fixtures as grants. Under `RECHECK_ONLY`, contact no live target before authentic grants and predecessor pass; repeat offline checks only when inputs change or before authorized execution. Preserve A41, Production disablement, Epic 20/20.5 history and sprint bytes until authorized close-out.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :--- | :--- | :--- | :--- |
| Held | Missing current authority or C1 gate | No target contact or gate advance | Record blocker and owner |
| Qualification | Accepted C1 and action grants | C0/C2-C4 prove required two-writer lifecycle behavior | Reject gaps, drift or failed cleanup |
| Close-out | Accepted C0-C6 and publication grant | Terminal, pre/postflight and remote checks bind exact A41 transition | Refuse incomplete chain or protected-byte drift |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-10.md` — preserve prior frozen decision and history.
- `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` and `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md` — Platform request and operational deny values.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical C0-C6 ledger and close-out contract; append authentic receipts.
- `tools/access_telemetry_c1_producer_bindings.py`, `tools/access_telemetry_c1_authority_boundary.py`, `tools/verify_access_telemetry_lifecycle.py` — keep I2/I3/predecessor refusal before target access.
- `tools/verify-access-telemetry-lifecycle.py`, `tools/access_telemetry_c{2,3,4}_producer.py` — fixed producer and close-out commands; reuse.
- `docs/operations/access-telemetry-lifecycle.md`, `docs/operations/access-telemetry-adapter-production.md` — operator contract and runbooks.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — authenticate Platform's versioned decision, target, custody, session, grant and source records; record denials.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — verify P1-P7/I2-I6, eligible C1.15/C1.16, 23 other registered/done successors and all 25 accepted C1 gates in one profile/session.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` and `tests/tooling/access_telemetry_c1_interchange/test_authority_boundary.py` — unit-test held-state refusal, drift and cleanup edges; extend only if an edge lacks coverage.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — after action grants, run C0/C2-C4 in the approved isolated target; retain receipts and final cleanup proof.
- [ ] `docs/operations/access-telemetry-lifecycle.md`, `docs/operations/access-telemetry-adapter-production.md` — reconcile observed operations and obtain independent C5/C6 decisions.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — record terminal, A41 pre/postflight and remote publication checks.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md`, and canonical evidence — mutate only four approved A41 paths after publication authority; preserve protected history.

**Acceptance Criteria:**
- Given missing authority or C1, when readiness runs, then no target is contacted and Production writes/A41 remain held.
- Given accepted C1 and scoped grants, when two-writer expiry and faults run, then packets prove acknowledgement, recovery, purge, newer records, audit continuity, tenant denial and cleanup.
- Given accepted C0-C6 and publication authority, when terminal and publication checks pass, then A41 binds one evidence set and protected history remains intact.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — focused offline lifecycle guards pass.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — unauthenticated predecessor continues to refuse.
- `git diff --check` — no whitespace errors; inspect source, packet and protected-file hashes before close-out.
