---
title: 'Story 27.4 current-plan C1.15 local source corroboration'
type: 'feature'
created: '2026-10-15'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9'
context:
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current-plan C1.15 inspection and local checkout corroboration are separate APIs. Sequential calls release plan/run/status descriptors before Git and require caller anchors again.

**Approach:** Compose both under held current-plan acquisition; return the existing result after final readback. Spec 14 live tasks remain open.

## Boundaries & Constraints

**Always:** Derive anchors solely from outside-approved plan bytes. Preserve `observe`, explicit UTC `now`, owner/PG2/self-review checks, budgets, detachment and existing public contracts. Hold plan/run/status and ancestor descriptors through corroboration and status-last readback. Reuse the enforced local Git lane, deadlines and clean standalone checkout requirement.

**Never:** Contact targets, run candidate code/collectors, fetch, grant authority or weaken predecessor refusal. No dependency, Git mutation, Production, sprint/history or A41 changes. Claim no complete custody, simultaneous snapshot or continuing lock. Existing owner policy approval persists.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Behavior | Failure Behavior |
| --- | --- | --- | --- |
| Current plan and checkout | Approved files/evidence and clean checkout | Existing frozen corroboration and self-review disclosure | No gate verdict |
| Stale or inconsistent input | Invalid bytes/bindings, withdrawal/expiry or foreign scope | Reject at the existing validation boundary before local Git | Content-free refusal |
| Local or final drift | Unsafe checkout/Git failure or file/withdrawal drift | Discard result; clean up files/children | No partial result or exception detail leak |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_owner_custody.py` — reuse `_compose_owner_run_files` unchanged for held lifetime/readback.
- `tools/access_telemetry_c1_source_provenance.py` — reuse `_corroborate_inspected_sources(..., owner_safety_preflight=True)` unchanged.
- `test_owner_custody.py` and `test_owner_c115_provenance.py` provide private-file/disposable-checkout fixtures; import modules without rediscovering TestCases.

## Tasks & Acceptance

**Execution:**

- [x] `tools/access_telemetry_c1_owner_c115_plan_files.py` — extract a private composition seam invoking an internal callback after full inspection and before held final readback; preserve existing wrapper behavior.
- [x] `tools/access_telemetry_c1_owner_c115_provenance.py` — add `corroborate_owner_c115_sources_from_plan_files` with existing plan-files inputs plus `repository_root`; accept no caller run, anchors, withdrawal or registry digest. Share checked-source corroboration/result construction with the existing API. Return existing frozen `OwnerC115LocalSourceCorroboration` after final cleanup. Add bounded content-free `OwnerC115PlanSourceCorroborationError` (`owner-c115-plan-source-...`), raised without cause/context/partial result after cleanup.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_plan_provenance.py` — cover the matrix, fresh bindings, detachment/budgets, one/two/three Pods, foreign tenant/namespace/session/commit denial, held lifetime, mid-Git/final withdrawal, same-size drift and cleanup faults. Count forbidden calls: invalid containers precede file reads; invalid current files/evidence precede Git; successes allow only the existing bounded local Git lane.
- [x] `docs/operations/access-telemetry-c1-owner-verification.md` — document API/order/limits and remove the obsolete composition gap. This workspace's initialized submodules are unsupported; corroboration needs an existing clean standalone checkout. Creating one remains later preparation.

**Acceptance Criteria:**

- Given valid inputs, when composition succeeds, then exactly one full inspection supplies detached sources to corroboration before final status readback releases the result.
- Given drift or withdrawal during corroboration, when final readback runs, then no result escapes and descriptors/children are cleaned up.
- Given existing callers, when regressions run, then public behavior and deployed refusal remain compatible.

## Implementation Notes

2026-10-15: Owner chose "approve and stop". Spec approved and ready for development; the frozen intent is locked. Implementation remains pending.

2026-10-15: Development added a private checked-inspection continuation within the existing held-file composition and the current-plan local-source API. Existing explicit-anchor corroboration shares the checked-source/result builder; the file-custody and enforced local Git implementations are unchanged. No target operation, dependency update, workspace Git mutation, Production/sprint/history/A41 change or operational standalone-checkout preparation occurred. Disposable repositories are test fixtures only.

## Spec Change Log

## Review Triage Log

| ID / layer | Finding | Verdict | Evidence and disposition |
| --- | --- | --- | --- |
| B1 / blind-hunter | `forbidden_calls` does not deny direct/prebound environment reads. | low | The helper guards `os.getenv` but not `os.environ`, so the isolated probe demonstrates a test-barrier blind spot. The new production modules read no environment and the shared Git lane supplies a literal environment; no current ambient-configuration regression was shown. Reject: this speculative future test defect is unlikely in everyday use, and an environment proxy/alias guard adds machinery beyond a direct correction. |
| B2 / blind-hunter | Pre-Git refusal barriers do not whitelist every filesystem operation. | low | The probe demonstrates that unrelated `os.stat` calls are uncounted, while the current call graph reaches checkout/filesystem access exclusively through the explicitly denied shared helper. Invalid containers separately use the all-filesystem denial lane. Reject: no unauthorized read occurs in the implementation, and a path/descriptor whitelist would add substantial guarding for a hypothetical future path. |
| B3 / blind-hunter | Invalid-container denial inventory omits newly composed modules' hypothetical prebound filesystem aliases. | low | `checkout.denials` omits those modules and an injected `os.open` alias can bypass its scan. The new production modules have no prebound filesystem aliases; custody/dispatch access OS functions through the globally patched `os` module, so their actual file calls are denied. Reject: the future-alias case is unlikely in everyday use and requires additional alias guarding/probe machinery. |
| B4 / blind-hunter | The new success case checks blob-ID count rather than exact returned identity order. | low | Confirmed at the one/two/three-Pod success assertion: paths are checked exactly, but IDs only by length. Existing callers verify the shared builder's exact IDs; strengthen the new entry point directly with the expected ordered tuple. Route: patch; no public or production behavior change. |
| B5 / blind-hunter | The new composed suite lacks a post-Git effective-UID change case. | false | `test_effective_os_uid_is_custody_evidence_only_and_changes_refuse` already tests the unchanged custody helper's final UID check. The composition runs entirely in that helper's existing callback interval; the new held-lifetime/status-last tests prove the callback returns into the same final readback and cleanup. No UID-check bypass or distinct untested branch is introduced. |
| B6 / blind-hunter | Every named-file/privacy mutation needs repetition during composed Git. | false | The unchanged held-file revalidation is exercised for every artifact and replacement/removal/rewrite/mode/hardlink mutation by `test_replacement_removal_or_rewrite_of_each_file_during_inspection_returns_nothing`; `test_custody_ancestor_replacement_or_outside_symlink_during_inspection_refuses` covers late symlink substitution; the new tests prove held descriptors and final readback surround the whole Git callback and reject late byte/ancestor drift. The assertion that the introduced interval lacks its named-identity/privacy enforcement is disproved by that unchanged path and its passing tests. |
| B7 / blind-hunter | Additional child-failure leaves need composed tests to prove wrapper mapping/cleanup. | false | Output overflow and enforcement failure are covered by `test_local_git_output_bound_and_timeout_reap_children_without_partial_results` and `test_native_enforcement_unavailable_refuses_in_the_owned_child`. They raise the same `InterchangeFormatError` normalized by the new facade; the new real deadline and child-close cases prove normalization after held-stack/child cleanup. No distinct new mapping or cleanup branch exists for the proposed additional leaves. |
| B8 / blind-hunter | The documentation omits `tools` import-path setup. | false | The same document's Pure API section explicitly says to put the repository's `tools` directory on the Python import path before using its examples. The new section follows that shared prerequisite; it does not claim the tools module is installed. |
| B9 / blind-hunter | The historical approval note says implementation remains pending. | false | That dated note records the owner's earlier approve-and-stop state and is immediately followed by the dated development entry and completed evidence. It is historical continuity, not the current status. Also reject any proposed fix that edits the build spec itself, as directed by the review workflow. |

Verification-gap reviewer: no verification gaps found. Edge-case hunter: empty findings list. All three layers completed before triage; no functional defect, intent/spec gap or deferred issue survived. B4 is the only patch.

## Verification

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_plan_provenance.py' -v` — new composition and named cross-scope denial cases pass.
- Same command with `-p 'test_*.py'` — all affected regressions pass without failures/errors/skips; record discovery deltas separately.
- `git diff --check` and baseline-derived changed-file/File List comparison — whitespace clean, exact scope accounted for.

## Dev Agent Record

- Read the full spec, both frontmatter context files, root guidance/configuration, owner verification documentation, and the existing held-file/owner/local-Git implementation and fixtures before editing.
- Focused verification: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_plan_provenance.py' -v` — 27 tests in 57.187s, `OK`; failures 0, errors 0, skips 0.
- Full affected regression verification: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — 611 tests in 280.774s, `OK`; failures 0, errors 0, skips 0. Existing pure/current-plan/explicit-anchor/legacy-fixture/deployed-refusal behavior remains green.
- Named tenant/namespace/session/commit denial evidence: `test_rehashed_foreign_tenant_and_namespace_refuse_before_git`, `test_rehashed_foreign_session_and_commit_refuse_before_git`, `test_rehashed_later_pod_foreign_argv_outputs_and_recheck_refuse_before_git`, and `test_foreign_sources_refs_and_raw_stream_inconsistency_refuse_before_git` pass in that focused command. Counted barriers assert no checkout/Git/process/network/ambient-clock access on those refusals. This is the focused denial evidence for the new owner-evidence composition surface; it preserves the earlier Story 20.2 and Story 24.3 boundaries.
- Named withdrawal/drift/cleanup evidence: `test_mid_git_withdrawal_discards_result_after_cleaning_held_files_and_children`, `test_final_readback_withdrawal_after_successful_git_discards_computed_result`, `test_same_size_coarse_timestamp_plan_run_and_status_drift_discard_local_result`, `test_acquisition_seek_and_cleanup_faults_are_content_free_after_complete_cleanup`, and `test_child_output_cleanup_fault_refuses_after_closing_files_and_child` pass. Exceptions assert bounded exact codes, no cause/context or partial inspection, and descriptor/child cleanup.
- Discovery: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"` — 611 test cases, loader errors 0. Compared with the immediately observed 584-case development baseline: phase +27, cumulative +27, external +0; new `test_owner_c115_plan_provenance.py` scope `0 -> 27` test cases. Fixture modules are imported without rediscovering TestCases.
- Scope/whitespace: `git diff --check` exits 0. The set union from `git diff --name-only 785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9` and `git ls-files --others --exclude-standard` matches the five cumulative File List paths exactly, with no exclusions. Changed Python files have LF and clean trailing whitespace; changed Markdown files retain CRLF.
- Limits: only existing clean standalone checkouts are supported. This workspace's initialized submodules remain unsupported; preparing a standalone checkout is later work. Source/stream/declaration acquisition custody, live Spec 14 tasks, gate/result acceptance, Production activation and A41 close-out remain open.

## Code Review Record

2026-10-15: All three context-free reviewers completed against the full baseline-derived diff, including untracked files. Edge-case and verification-gap layers returned no findings. Each of the nine blind-hunter findings was independently triaged above; B4 received the sole patch, an exact ordered blob-ID assertion in the one/two/three-Pod success case. No production change, unresolved finding, deferral, intent gap or spec gap resulted from review.

The root agent read the entire diff, checked all execution tasks and all three matrix rows, and confirmed every new test ran and passed from the complete final focused log. Positive frozen/self-review output, pre-Git cross-tenant/namespace/session/commit refusal, and local/final drift/withdrawal cleanup all have executed focused evidence. The four named cross-scope tests and five named withdrawal/drift/cleanup tests in Dev Agent Record pass in the final commands below. Original custody and enforced local Git implementations remain unchanged.

- Patch-only check by implementation agent: the one/two/three-Pod success test passed, 1 test in 4.381s.
- Final root focused command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_plan_provenance.py' -v` — 27 tests in 32.029s, `OK`; failures 0, errors 0, skips 0. Full raw output: `/tmp/story27-4-spec24-focused-final.log`.
- Final root regression command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — 611 tests in 261.361s, `OK`; failures 0, errors 0, skips 0. Full raw output: `/tmp/story27-4-spec24-interchange-final.log`.
- Independent post-review discovery: 611 test cases, loader errors 0; phase +0, cumulative +27, external +0. New composition discovery remains 27 cases. File List matches 5/5 against full baseline `785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9`; no exclusions.
- Readiness invocation without explicit changed paths first refused an empty changed set. Supplying the five baseline-derived changed paths made the check pass before review. The repository checker does not recognize the workflow's temporary `in-review` status; completion uses its recognized `done` status and is checked separately below. No bypass or checker edit was used.
- This completion applies only to Spec 24. The approved prohibition on Git and sprint/history mutation takes precedence over the generic workflow's terminal commit and sprint-update actions. No staging, commit, push, branch, dependency update, sprint mutation or A41 mutation is performed. Story 27.4 and Spec 14 live qualification, standalone-checkout preparation, complete evidence custody, acceptance and Production activation remain open.

- Final readiness gate: `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-24 --changed-files-file /tmp/story27-4-spec24-changed-files.txt` — exit 0; final line `Story review readiness validation passed.` The terminal checker skips cumulative comparison for `done`; the independent five-file set audit above supplies that evidence. `git diff --check` exits 0.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-15 | create-story | Codex drafted the composition; implementation awaits approval. | Interchange 584 -> 584 test cases; phase/cumulative/external +0; loader errors 0. Evidence: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. | matched 1/1 against `785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9` using `git diff --name-only HEAD` and `git ls-files --others --exclude-standard`; only this spec changed. |
| 2026-10-15 | create-story | Owner approved and stopped; status set to ready-for-dev with frozen intent unchanged. | Interchange 584 -> 584 test cases; phase/cumulative/external +0; loader errors 0. Evidence: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. | matched 1/1 against `785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9` using `git diff --name-only HEAD` and `git ls-files --others --exclude-standard`; only this spec changed. |
| 2026-10-15 | dev-story | Added held-current-plan local-source composition, shared checked-source corroboration/result construction, 27 matrix/denial/cleanup cases and API/order/limit documentation; original custody/local-Git lanes unchanged. Focused 27/27 and full interchange 611/611 pass without failures/errors/skips; named cross-scope and withdrawal evidence is recorded in Dev Agent Record. | Interchange 584 -> 611 test cases; phase +27, cumulative +27, external +0; loader errors 0. New `test_owner_c115_plan_provenance.py` scope 0 -> 27 test cases. Discovery evidence: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. Focused execution: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_plan_provenance.py' -v`; full execution uses the same command with `-p 'test_*.py'`. | matched 5/5 against `785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9` using `git diff --name-only 785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9` plus `git ls-files --others --exclude-standard`; no exclusions. `git diff --check` exits 0. |
| 2026-10-15 | code-review | All three review layers complete; nine blind-hunter findings triaged, exact ordered blob-ID assertion patched, no functional defect or deferred work. Root matrix audit observed all 27 new cases passing. Post-patch focused 27/27 and full interchange 611/611 pass; no failures/errors/skips. | Interchange 611 -> 611 test cases; phase +0, cumulative +27, external +0; loader errors 0. Composition scope 27 -> 27 cases. Evidence: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. Final execution commands/results are in Code Review Record. | matched 5/5 against `785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9` using `git diff --name-only 785e2ae4cc5e7ff1e4ea5d2a530be0cb374b19e9` plus `git ls-files --others --exclude-standard`; no exclusions. `git diff --check` clean. |

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-24.md` - record the approved specification and verification evidence.
- `tools/access_telemetry_c1_owner_c115_plan_files.py` - compose checked inspection within held file custody.
- `tools/access_telemetry_c1_owner_c115_provenance.py` - expose current-plan local source corroboration.
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_plan_provenance.py` - verify composition, refusal boundaries, and cleanup.
- `docs/operations/access-telemetry-c1-owner-verification.md` - document the API and its operational limits.

2026-10-15: The user requested a commit. Commit preparation adds this explicit
scope from the existing execution tasks and File List to satisfy the repository
hook; the frozen intent and implementation scope are unchanged.

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-24.md`
- `tools/access_telemetry_c1_owner_c115_plan_files.py`
- `tools/access_telemetry_c1_owner_c115_provenance.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_plan_provenance.py`
- `docs/operations/access-telemetry-c1-owner-verification.md`
