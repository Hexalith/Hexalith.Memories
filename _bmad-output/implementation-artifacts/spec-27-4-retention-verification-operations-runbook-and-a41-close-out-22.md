---
title: 'Story 27.4 owner-compatible C1.15 local source corroboration'
type: 'feature'
created: '2026-10-14'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'ed70998ddb0a26407b08724e347e490f10b67305'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The local Git corroborator accepts only legacy fixture registration. The completed owner C1.15 inspector compares supplied bytes without establishing their presence in a clean local checkout.

**Approach:** Add an owner-specific API that fully reinspects retained owner inputs before reusing the existing bounded local source checks.

## Boundaries & Constraints

**Always:** Preserve the adopted owner policy, principal, self-review disclosure, PG2 pins, outside approval anchors and occurrence-counted byte budgets. Use detached reinspected inputs. Permit only existing bounded local Git reads with isolated environment, network protocols/lazy fetching disabled and no shell. Keep legacy API/results/refusals compatible.

**Never:** Contact targets, run producers/verifiers, issue acceptance or execution authority, invent acceptance schemas, alter deployed/predecessor refusal, dependencies, submodules, Production, sprint, history or A41. No project staging, commits or pushes; disposable Git fixture creation is permitted. Spec 14 live tasks stay open.

## I/O & Edge-Case Matrix

| Input | Outcome |
| --- | --- |
| Matching owner bytes and clean standalone checkout at approved commit | Frozen owner-labelled source corroboration; no gate verdict |
| Rehashed foreign scope, withdrawn/expired run, forged snapshots, Ref/source/argv/output drift or excess bytes | Content-free refusal before local filesystem/Git access |
| Dirty/foreign checkout, missing objects, links/aliases, unsupported attributes/index flags or checkout drift | Bounded local refusal; no partial result |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_source_provenance.py` — reuse its complete local Git lane, nineteen-source and story/verifier checks; legacy entry calls `inspect_registration`.
- `tools/access_telemetry_c1_owner_c115.py` — reuse `inspect_owner_c115` and its detached sources, exact raw snapshots and explicit `now`/withdrawal checks.
- `tests/tooling/access_telemetry_c1_interchange/test_registration_provenance.py`, `test_owner_c115.py` in that directory — reuse isolated Git and owner/multi-Pod fixtures without importing extra discoverable test classes.

## Tasks & Acceptance

**Execution:**

- [ ] `tools/access_telemetry_c1_source_provenance.py` — extract shared local corroboration over inspected source data and retained sources. Preserve both legacy command versions, ordering, clean-HEAD checks, auxiliary story/verifier presence, final rereads, bounds and process cleanup.
- [ ] `tools/access_telemetry_c1_owner_c115_provenance.py` — add `corroborate_owner_c115_sources` accepting all existing owner-inspector inputs plus explicit canonical `repository_root`. Call the complete pure inspector before any local dependency; corroborate only its detached sources and inspected registration. Return frozen `OwnerC115LocalSourceCorroboration` carrying concealed inspection, commit/root, ordered paths/blob OIDs and owner/assurance. Normalize errors outside handlers to bounded content-free refusals with no cause/context or partial result.
- [ ] `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_provenance.py` — cover every matrix row with actual disposable Git checkouts, one/two/three Pods, frozen results, cache/caller mutation, rehashed cross-namespace/tenant/Pod inputs, budget/time/status boundaries, local drift and counted dependency barriers. Positive calls allow only the fixed bounded local Git lane; invalid retained inputs must call zero filesystem/process/network/configuration/clock dependencies, including known prebound aliases.
- [ ] `docs/operations/access-telemetry-c1-owner-verification.md` — document API, supported standalone `.git` directory checkout, point-in-time local evidence, ignored-file limits and remaining custody/live acceptance work.

**Acceptance Criteria:**

- Given valid owner materials and a matching clean checkout, when corroboration runs, then all nineteen sources and distinct tracked story/verifier files match the approved commit and the returned inspection retains the exact checked bytes with self-review disclosure.
- Given unsafe retained inputs, when corroboration runs, then it refuses before local access; given unsafe or drifting local inputs, when local checks run, then no result escapes.
- Given existing fixture consumers, when affected regression lanes execute, then their behavior and deployed refusal remain intact.

## Implementation Notes

No unresolved intent gaps or irreversible actions. Five-file footprint; new caller-facing API. Existing owner delegation covers repository implementation; concrete live plans and result acceptance remain separate. Only this standalone spec can advance to done.

2026-10-14 owner approval: "approve and stop". The frozen intent is locked and unchanged. Implementation has not started; all execution tasks remain open.

## Spec Change Log

## Review Triage Log

## Verification

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*provenance.py' -v` — positive/negative local and ordering cases execute with zero failures/skips.
- Repeat with `-p 'test_*.py'` — all affected regressions pass. Record discovery deltas separately from execution.
- `git diff --check`; explicit slice-key file-scope/readiness gates; reconcile cumulative File List and protected paths against baseline. Keep draft until approved; readiness requires a recognized approved status.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_source_provenance.py`
- `tools/access_telemetry_c1_owner_c115_provenance.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_provenance.py`
- `docs/operations/access-telemetry-c1-owner-verification.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-22.md`

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-22.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-14 | create-story | Prepare owner local-source corroboration; owner: build agent. | Phase/cumulative +0; interchange 488 -> 488 unittest cases; legacy provenance 18 -> 18; new owner provenance 0 -> 0. Discovery: `env PYTHONDONTWRITEBYTECODE=1 python3 -c "import unittest; l=unittest.TestLoader(); print([(p,l.discover('tests/tooling/access_telemetry_c1_interchange',pattern=p).countTestCases()) for p in ('test_*.py','test_registration_provenance.py','test_owner_c115_provenance.py')]); print(l.errors)"`; no errors. Discovery only; no execution claim. | Matched 1/1 against `git diff --name-status ed70998ddb0a26407b08724e347e490f10b67305` plus `git ls-files --others --exclude-standard`: this spec only; no exclusions. |
