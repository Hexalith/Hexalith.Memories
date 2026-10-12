---
title: 'Story 27.4 owner-run plan and status dispatch'
type: 'feature'
created: '2026-10-12'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '401d4c0a648eff9d6a6c645e936e8c60879a0168'
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md'
  - 'docs/operations/access-telemetry-owner-handoff.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `inspect_owner_run` checks a record only when the caller already supplies anchors and a withdrawal fact. Nothing yet proves those facts from a retained plan approval and a fresh status read, so a candidate record can still be mistaken for authority.

**Approach:** Add an offline dispatcher that accepts one action only after the outside plan digest matches the retained plan, the plan binds the record, and a fresh status read is a matching `withdrawn: false`. Return the existing inspection result and leave execution closed.

## Boundaries & Constraints

**Always:** Bind profile SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. Derive `OwnerRunAnchors` only from retained plan bytes whose outside digest matches. Re-read the plan-named status file immediately before inspection. Treat missing, unreadable, mismatched, or non-boolean withdrawal state as unknown. Keep `sole-owner/self-reviewed` disclosure. Leave `inspect_owner_run` free of I/O.

**Never:** Admit a gate, checkpoint, target action, or A41 close-out. Change `_validate_predecessor`, `require_authenticated_disposition`, or `lookup_deployed_binding`. Contact a cluster, register a collector, or follow a status path outside the plan custody root. Fall back from a failed owner path to predecessor/v2, PG1, or a cached `withdrawn: false`. Treat the adopted policy or the 2026-10-12 offline acceptance as a run approval.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Current plan | Matching plan digest, bound record, fresh `withdrawn: false`, in-window action | `OwnerRunInspection` for that action only | No execution handle |
| Drift | Plan, record, session, or status key mismatch | No inspection | Content-free refusal |
| Unknown status | Missing, unreadable, or non-boolean withdrawal | No inspection | Do not coerce to false |
| Closed action | Absent, expired, withdrawn, or wrong-phase action | Existing inspector refusal | No partial authority |
| Other versions | predecessor/v2 or institutional receipts | Dispatcher does not select them | Lifecycle refusal unchanged |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_owner_run.py` — reuse `inspect_owner_run`, `OwnerRunAnchors`, and `OwnerRunInspection`. Do not add filesystem or network calls.
- `tools/verify_access_telemetry_lifecycle.py` — `_validate_predecessor` still refuses predecessor/v2 after the shape check. Do not edit it.
- `tools/access_telemetry_c1_authority_boundary.py` — `require_authenticated_disposition` remains a terminal refusal. Do not edit it.
- `tools/access_telemetry_c1_producer_bindings.py` — `lookup_deployed_binding` remains `deployed-producer-binding-unavailable`. Do not edit it.
- `tests/tooling/access_telemetry_c1_interchange/test_owner_run.py` — copy its fixture pattern; do not weaken its no-authority assertions.
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md` — plan digest, record binding, and fresh-status rules. Resource lists in a plan grant nothing here.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/access_telemetry_c1_owner_dispatch.py` — add a reader that hashes the caller-supplied plan bytes against the outside digest, requires `recordSha256`, `policySha256`, `sessionId`, `sourceCommit`, `targetSha256`, `custodyRoot`, and a canonical `statusPath` inside that custody root, ignores every other plan field as non-authority, re-reads a status object keyed by plan digest, record digest, and session, and calls `inspect_owner_run` only when `withdrawn` is a boolean.
- [ ] `tests/tooling/access_telemetry_c1_interchange/test_owner_dispatch.py` — cover the matrix, including symlink or non-canonical status paths, a later call that observes a changed withdrawal file, and refusal that returns no inspection.

**Acceptance Criteria:**
- Given matching plan bytes and a fresh in-window `withdrawn: false` status, when dispatch requests one included action, then it returns that action's `OwnerRunInspection` and no gate or execution flag.
- Given a missing or mismatched plan, record, session, or status, when dispatch runs, then it refuses and does not substitute `withdrawn: false`.
- Given the current lifecycle, authority, and binding modules, when this slice finishes, then their refusal behavior is unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The plan schema stays open for resource grammar. This dispatcher requires only the seven binding fields above and does not interpret resource, verb, or cleanup entries. A status document is `{planSha256, recordSha256, sessionId, withdrawn}` with no extra authority fields. `statusPath` must already be canonical; a resolved path that differs from the spelled path refuses.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_*.py' -v` — owner-run and dispatch tests pass.
- `git diff --exit-code -- tools/verify_access_telemetry_lifecycle.py tools/access_telemetry_c1_authority_boundary.py tools/access_telemetry_c1_producer_bindings.py tools/access_telemetry_c1_owner_run.py` — exit 0; those four files stay unchanged.
