---
title: 'Story 27.22: Verify the isolated linkage SQL contract'
type: 'chore'
created: '2026-10-06'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-22-component-and-backend-identity'
baseline_commit: 'f5beeed52874f7feedf3d48d040feaea163cd3cd'
context:
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.22 fixtures manufacture JSON without executing SQL. V3 requires real PostgreSQL 18.6 execution before live collection.

**Approach:** Add and execute a separate disposable local integration lane using the existing emitted query and validation. Discharge V3 after verification/review; keep live acceptance pending.

## Boundaries & Constraints

**Always:** Use approved linux/amd64 child `docker.io/library/postgres@sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04`; verify actual 18.6 / 180006. Mirror database, runtime role, peer mapping and TLS identity. Bound commands; publish no ports; use disposable credentials and secret-safe receipts; clean up only uniquely named owned resources. Preserve collector/deployment bytes, historical evidence, neutral packet fields and the pending C1.16 checkpoint.

**Never:** Contact Kubernetes, read real credentials/tenant records, create tenant data, change profile/dependencies/workloads/Git, or claim live linkage, other-gate, Production, Story 27.4 or A41 credit. Missing prerequisites must fail, never skip/pass.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :------- | :------------ | :------------------------- | :------------- |
| Valid | One idle TLS runtime session; local peer observer | Real emitted SQL passes existing fields/types, identity, UTC ordering and TLS validation | Retain bounded receipt |
| Invalid | Missing/duplicate session, wrong role/database or TLS | Existing validation refuses real result | Assert specific denial |
| Mutation | Rename emitted alias in temporary source | Contract verification detects broken field | Tracked source unchanged |
| Setup failure | Missing image/runtime/TLS or timeout | Nonzero failure; V3 unresolved | Cleanup owned resources |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1-linkage.ps1:738` — `Get-LinkageSessions` emits read-only psql arguments and validates output. Capture its actual command through a local adapter; load only necessary function definitions, never the collector's top-level code or handwritten SQL.
- `deploy/kubernetes/base/access-telemetry-postgresql.yaml:18` — peer mapping `postgres` to `memories_admin`, database/runtime role and TLS contracts; read only.

## Tasks & Acceptance

- [ ] `tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py` — isolated setup, actual emitted-command execution/validation, matrix regressions, alias mutation, bounded evidence and cleanup.
- [ ] `docs/operations/access-telemetry-adapter-production.md` — document the integration command and restrictive interpretation.
- [ ] `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md` and `deferred-work.md` — append actual reviewed V3 disposition; retain B6 target authority and all live prerequisites.

**Acceptance Criteria:**
- Given reviewed success, when reconciled, then only V3 closes; Story 27.22 stays in-progress with live acceptance pending.

## Dev Notes

### Historical Context Classification

| Influence | Classification | Permitted use |
| :-------- | :------------- | :------------ |
| Completed 27.22 offline spec | current-narrow-pattern | Re-verified collector and explicit V3 gap only. |
| Story 27.21 | historical-reference-only | Preserve evidence; no inherited live authorization. |
| Broad 27.3/27.4 | anti-template | Provenance only; no bundled qualification scope. |

### Slice Proof

Deployment Adapter Developer owns one C1.16 prerequisite.

### Epic AC Verification

Verified 2026-10-06 against the frontmatter baseline.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "Synthetic kubectl JSON does not execute that SQL." | Behavioral | `tests/tooling/access_telemetry_c1/linkage_test.py:140` | Fixture constructs session JSON. | confirmed |
| "execute the emitted session SQL contract in an isolated PostgreSQL 18.6 environment" | Existence | `rg -n 'isolated PostgreSQL' docs/operations/access-telemetry-adapter-production.md` | V3 is pending. | confirmed |

## Implementation Notes

2026-10-06: The user approved this spec and requested stopping before implementation. The frozen intent is approved; implementation tasks remain pending. Story 27.22 and its live C1.16 checkpoint remain in-progress/pending.

## Spec Change Log

## Review Triage Log

## Verification

`PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v` must pass without skips, including denials. Receipt binds collector/query/test hashes, image/server identity, commands/results and raw-log hashes. Run story slice, whitespace, file-scope and review-readiness checks; preserve live status.

## File Scope

- `tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py`
- `docs/operations/access-telemetry-adapter-production.md`
- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md`

## File List

- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-06 | create-story | Root plans isolated V3 verification. | C1 85 -> 85 test methods; phase/cumulative delta +0. Absent integration scope: separate baseline 0. Discovery below. | matched 1/1 against frontmatter baseline; `git status --short`; only this new spec. |

Before/after discovery: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases())"` returned 85 with zero discovery errors. No implementation tests ran.
