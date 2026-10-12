---
title: 'Story 27.4 owner-run plan and status dispatch'
type: 'feature'
created: '2026-10-12'
status: 'done'
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
- [x] `tools/access_telemetry_c1_owner_dispatch.py` — add a reader that hashes the caller-supplied plan bytes against the outside digest, requires `recordSha256`, `policySha256`, `sessionId`, `sourceCommit`, `targetSha256`, `custodyRoot`, and a canonical `statusPath` inside that custody root, ignores every other plan field as non-authority, re-reads a status object keyed by plan digest, record digest, and session, and calls `inspect_owner_run` only when `withdrawn` is a boolean.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_owner_dispatch.py` — cover the matrix, including symlink or non-canonical status paths, a later call that observes a changed withdrawal file, and refusal that returns no inspection.

**Acceptance Criteria:**
- Given matching plan bytes and a fresh in-window `withdrawn: false` status, when dispatch requests one included action, then it returns that action's `OwnerRunInspection` and no gate or execution flag.
- Given a missing or mismatched plan, record, session, or status, when dispatch runs, then it refuses and does not substitute `withdrawn: false`.
- Given the current lifecycle, authority, and binding modules, when this slice finishes, then their refusal behavior is unchanged.

## Implementation Notes

2026-10-12: Resumed this existing ready-for-dev slice under the owner's repository implementation delegation. `dispatch_owner_run(snapshot, plan_bytes, approved_plan_sha256, *, now, action)` matches exact plan/record bytes, derives anchors from the retained plan, and freshly reads the closed keyed status using non-following directory descriptors. Resource and cleanup fields remain non-authoritative. The pure inspector and three existing deployed refusal modules are unchanged.

Matrix audit: all five rows passed in the 52-test owner command. Matching/action-only results are covered by `test_matching_fresh_status_returns_only_one_inspection_and_self_review`; drift by the plan/record/status digest and anchor tests; unknown status by the missing/unreadable and non-boolean tests; closed actions by the phase/window/withdrawal tests; other versions by `test_pg1_other_versions_and_institutional_records_are_not_selected`. Symlink/outside-path tests prove refusal before status content is read; the target sentinel proves no target/process/network call.

Focused verification: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_*.py' -v` passed 52 test methods in 0.162s, zero failures/errors/skips. Parent receipt: `/tmp/story-27-4-owner-dispatch-focused.log`. Implementation-agent full affected verification: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v` passed 294 methods in 75.893s, zero failures/errors/skips. Pre-change parent baseline passed 267 methods in 61.151s; receipt: `/tmp/story-27-4-owner-dispatch-before.log`.

The four-file protected-module diff command in Verification and `git diff --check` both exit 0. The six live tasks in spec-14 remain open. Parent Story 27.4 stays in-progress; this bounded offline inspection slice supplies no gate, target action, or A41 closure.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| :--- | :--- | :--- |
| BH1 obsolete status inode | medium | Parent reproduced inspection after replacement with matching withdrawn:true and after unlink during the first read. Patch named-file identity validation and regressions. |
| BH2 incoherent multiple-read snapshot | medium | Reviewer reproduced short-read/rewrite synthesis; parent reproduced an in-place rewrite to withdrawn:true while the old false bytes were accepted. Patch before/after metadata stability and a deterministic short-read regression. |
| BH3 close-failure descriptor leak | low | Injected close failure can strand the next descriptor; healthy valid-descriptor close does not encounter this path in ordinary use. Rejected: additional cleanup branches for a rare injected close failure add complexity beyond a direct correction. |
| BH4 unsupported operation exception | low | Parent injected NotImplementedError at the filesystem boundary and observed the unrestricted exception. Patch the existing normalization envelope; no capability fallback or public surface is added. |
| BH5 raw filesystem error context | medium | Parent reproduced PermissionError retained in the refusal's __context__. Patch sanitized refusal outside active internal exception handlers, including dispatch parsing, and inspect the exception chain in regressions. |
| BH6 byte-budget regression gap | medium | Existing oversize test verifies eventual refusal only, so unbounded reading would satisfy it. Patch byte-consumption and exact-boundary tests. |
| BH7 durable dispatcher contract absent | medium | Both durable operational documents describe plan/status requirements but do not describe the implemented dispatcher contract. Directly document the seven bindings, closed status, current-record caller responsibility and unsupported filesystem refusal without execution credit. |
| EC1 status pathname replacement/removal | medium | Independently reproduced by reviewer and parent; same file-identity root cause as BH1. Patch with BH1. |
| EC2 opened custody moved outside canonical path | medium | Parent reproduced rename/replacement of the opened custody root: old false status was inspected while canonical status was true. Patch canonical traversal revalidation and directory-swap coverage. |
| EC3 missing-status acceptance claim | medium | Parent reproduced inspection after unlink during read. Same root cause as BH1/EC1; patch together. |
| VG1 traversal-replacement regression gap | medium | Pre-verified mutation retained static symlink checks but passed all 27 methods while substituting outside status during a directory swap. Add a deterministic directory-swap test that requires refusal/no returned inspection. |

No intent gap or spec amendment is required: these corrections enforce the already approved current, canonical, content-free status read. Existing public dispatcher/inspector result shapes and protected refusal modules stay unchanged.

## Design Notes

The plan schema stays open for resource grammar. This dispatcher requires only the seven binding fields above and does not interpret resource, verb, or cleanup entries. A status document is `{planSha256, recordSha256, sessionId, withdrawn}` with no extra authority fields. `statusPath` must already be canonical; a resolved path that differs from the spelled path refuses.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_*.py' -v` — owner-run and dispatch tests pass.
- `git diff --exit-code -- tools/verify_access_telemetry_lifecycle.py tools/access_telemetry_c1_authority_boundary.py tools/access_telemetry_c1_producer_bindings.py tools/access_telemetry_c1_owner_run.py` — exit 0; those four files stay unchanged.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_owner_dispatch.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_dispatch.py`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md`
- `docs/operations/access-telemetry-owner-handoff.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-15.md`

## Cross-Tenant Negative Evidence

**Surfaces:** Offline record/plan/session binding and custody/status selection; no live tenant/case routing, storage, marker or authorization behavior changes.
**Tests:** `test_status_key_drift_refuses_before_inspection`, `test_outside_and_prefix_sibling_status_paths_refuse_before_io`, `test_custody_replaced_by_outside_symlink_during_read_is_never_followed`, `test_unknown_or_non_boolean_withdrawal_is_never_coerced_to_false`.
**Command:** `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_*.py' -v`.
**Result:** 63 unittest test methods passed, zero failures/errors/skips; wrong session/custody/status and outside substitution refuse before returning inspection. No live cross-tenant proof is claimed by offline fixtures.

## File List

- `tools/access_telemetry_c1_owner_dispatch.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_dispatch.py`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md`
- `docs/operations/access-telemetry-owner-handoff.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-15.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-12 | create-story | Adopt phase ledger for the resumed approved slice; owner: Administrator. Capture pre-development comparable discovery baseline. | Phase +0, cumulative +0; 267 -> 267 unittest test methods. Command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v`. | Matched 1/1 initial changed path (this resumed spec) against `git diff --name-only f68fb6d60d8b4d0510d5f6584c71fff0455ea508`; later cumulative set tracked below. No earlier deltas reconstructed. |
| 2026-10-12 | dev-story | Implement plan/status dispatcher and 27 regression methods; audit every matrix row. | Phase +27, cumulative +27; 267 -> 294 unittest test methods, same command/scope as create baseline; 52 owner methods passed. | Matched 3/3 against `git diff --name-only f68fb6d60d8b4d0510d5f6584c71fff0455ea508` plus `git ls-files --others --exclude-standard`; no session exclusions. The preserved spec baseline predates the session and includes already-committed root gitlink changes, which this slice does not own. |
| 2026-10-12 | code-review | All three review layers complete; correct status file/ancestor races and exception envelope, add boundary/byte-budget regressions and durable dispatcher documentation. | Phase +11, cumulative +38; 294 -> 305 unittest test methods. Same full discovery command as create baseline passed in 52.169s; 63 owner methods passed in 0.201s. | Matched 5/5 against `git diff --name-only f68fb6d60d8b4d0510d5f6584c71fff0455ea508` plus `git ls-files --others --exclude-standard`; no session exclusions. |

## Final Reviewed Verification

- Parent full affected command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v` — 305 methods passed in 52.169s, exit 0, zero failures/errors/skips; receipt `/tmp/story-27-4-owner-dispatch-final-full.log`.
- Parent focused owner command in Verification — 63 methods passed in 0.201s, exit 0, zero failures/errors/skips; receipt `/tmp/story-27-4-owner-dispatch-final-focused.log`.
- All three review layers finished and every finding has its own triage row. All selected corrections are complete; nothing is deferred. Retaining descriptors in an ExitStack for required ancestor checks also removes BH3's original ownership-transfer gap without a separate close-failure recovery protocol.
- All five original matrix rows and the reproduced replacement/unlink/rewrite/ancestor cases have passing test coverage. Protected inspector and deployed refusal modules are unchanged.
- Only this bounded offline slice is done. Parent Story 27.4 remains in-progress, with six live tasks open; preserve sprint/history bytes and A41/Production state. The broader parent readiness invocation is a no-op because its older artifact carries no ledger/File List/evidence table; this slice uses its own explicit spec key for executable scope/readiness validation.
- Executable final gates, each with this slice's explicit spec key and all five changed paths: `tools/check-story-file-scope.py` ends `Story file scope validation passed.`; `tools/check-story-review-readiness.py` ends `Story review readiness validation passed.`; the tenant-surface inventory reports `No tenant-isolation surface changed; evidence check is a no-op.` The attached offline denial cases still ran and passed.
- `git diff --check` and the protected inspector/refusal/sprint/A41/project-context diff command each exit 0. Frozen intent matches the session's original approved bytes.
