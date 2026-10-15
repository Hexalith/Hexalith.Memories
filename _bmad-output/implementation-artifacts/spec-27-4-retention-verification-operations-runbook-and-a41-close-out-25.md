---
title: 'Story 27.4 retained C1.15 evidence custody'
type: 'feature'
created: '2026-10-15'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '99431f0fed102ea6ddbae33d28b9da4fc7f215a0'
context:
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Spec 24 holds plan/run/status during corroboration; other evidence arrives without acquisition custody.

**Approach:** Acquire private retained files inside approved-plan composition; inspect and corroborate before final readback.

## Boundaries & Constraints

**Always:** Preserve outside approval, fresh withdrawal, explicit UTC `now`, `observe`, owner/PG2/self-review checks and existing contracts. Hold evidence descriptors through inspection, Git and evidence readback; finish owner checks with status last. Enforce canonical private single-link files/ancestors, distinct artifact paths/inodes, 1 MiB per artifact and incremental 32 MiB semantic occurrence accounting including run/both source forms. Control manifests have separate 1 MiB caps. Preserve Git enforcement/deadlines.

**Never:** Contact targets, execute candidate code/collectors, create checkouts or mutate Git/dependencies. Grant no execution, registration, acceptance, simultaneous snapshot or continuing lock. Preserve live tasks, sprint/history, Production and A41; owner policy approval persists.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Behavior |
| --- | --- | --- |
| Valid | Approved files/clean standalone checkout | Frozen corroboration, detached bytes, self-review disclosure |
| Invalid authority | Bad binding, expiry/withdrawal | Refuse before evidence acquisition/Git |
| Invalid evidence | Schema/hash/budget, aliases, unsafe files or foreign scope | Refuse before Git |
| Concurrent change | Drift, withdrawal/cleanup fault | Discard result; close files/children; content-free refusal |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_owner_custody.py`: `_compose_owner_run_files`, current authority/status-last checks and shared artifact identities.
- `tools/access_telemetry_c1_owner_dispatch.py`: reuse `_open_held_file` guards.
- `tools/access_telemetry_c1_owner_c115.py`: reuse `inspect_owner_c115`/budgets.
- `tools/access_telemetry_c1_owner_c115_provenance.py`: reuse `_corroborate_owner_inspection`.

## Tasks & Acceptance

**Execution:**

- [x] `tools/access_telemetry_c1_owner_custody.py`: share artifact identity tracking with the private evidence composition, covering control/evidence aliases without changing public wrappers.
- [x] `tools/access_telemetry_c1_retained_files.py`: extract a private callback seam retaining packet/stream descriptors through callback/readback. Preserve public API/sidecar format.
- [x] `tools/access_telemetry_c1_owner_c115_retained.py`: add `corroborate_owner_c115_retained_files(plan_path, approved_plan_sha256, *, custody_root, repository_root, now)`. Within held owner composition, require plan-bound `evidenceManifestPath`/`evidenceManifestSha256`. Verify manifest digest before following refs. Define closed schema `hexalith.access-telemetry.c1.owner-retained-evidence/v1` of five JSON refs (registry, capture, statement, command contract, role policy) and nineteen complete SourceSnapshot descriptors with file refs replacing byte slots; reuse capture's ordered stream sidecar. Refs bind canonical paths below custody and exact SHA-256. Validate inventory before following paths; bound reads incrementally. Inspect once, corroborate, reread evidence, then complete outer status checks/cleanup. Raise content-free `OwnerC115RetainedFilesError` with bounded `owner-c115-retained-...` codes after cleanup/handlers, without cause/context/partial result.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_retained.py`: reuse private-file/multi-Pod/Git fixtures without importing TestCases directly. Cover matrix, 1/2/3 Pods, identities, budgets, foreign tenant/namespace/session/commit, counted denial order, same-size drift, mid-Git/final withdrawal, held descriptors and cleanup faults.
- [x] `docs/operations/access-telemetry-c1-owner-verification.md`: document schema/layout/API/order/limits, remaining execution/acceptance boundary and later standalone-checkout preparation.

**Acceptance Criteria:**

- Given valid files, when composition succeeds, then acquired bytes feed one inspection and corroboration before evidence readback and status-last finalization.
- Given invalid or drifting inputs, when invoked, then barriers prove ordered refusal and cleanup without data leaks.
- Given existing callers, when regressions run, then behavior/deployed refusal stay compatible.

## Implementation Notes

2026-10-15: Owner chose "approve and stop". Spec approved and ready for development; the frozen intent is locked. Implementation remains pending.

2026-10-15: Development completed the retained-evidence acquisition composition, sharing control/evidence path and inode identities and holding evidence descriptors through the complete inspection, enforced local Git and exact evidence readback. The existing public wrappers and stream sidecar format remain compatible. This completion is limited to Spec 25: its frozen prohibition preserves live tasks, sprint/history, owner policy approval, Production and A41. No workspace Git mutation, dependency update or operational checkout preparation was performed; disposable Git repositories belong only to test fixtures.

## Spec Change Log

## Review Triage Log

| ID / layer | Finding | Verdict | Evidence and disposition |
| --- | --- | --- | --- |
| B1 / blind-hunter | Evidence descriptors close before outer plan/run/status readback. | false | The approved lifetime is explicitly through inspection, Git and evidence readback, followed by status-last owner checks. The evidence stack encloses all those evidence operations; the outer stack retains every control descriptor through its final readback. The successful lifetime test observes every descriptor still open during post-Git evidence reads. No continuing lock or simultaneous snapshot is promised; extending evidence custody through later control reads is not an unmet requirement. |
| B2 / blind-hunter | The runbook's before-read size claim includes outer control files. | low | Confirmed: the unchanged owner reader takes at most 1 MiB plus one overflow-detection byte before JsonSnapshot rejects oversized plan/run/status. The new evidence reader prechecks size, but the runbook states the stronger condition for all artifacts. Route: patch the runbook to distinguish evidence preflight from bounded control reads, preserving existing public behavior. |
| B3 / blind-hunter | Reopened ancestor descriptors can exhaust a 1024-file limit for a 20-Pod capture below byte limits. | low | Independently reproduced with a disposable 20-Pod fixture and RLIMIT_NOFILE=1024: bounded `owner-c115-retained-filesystem-unavailable` before inspection, with descriptor count unchanged after cleanup. This is a resource-availability condition, not acceptance of unsafe evidence. Route: patch the runbook to disclose per-artifact held ancestor costs and the need for sufficient OS descriptor capacity; no automatic limit change or new pooling machinery. |
| B4 / blind-hunter | Packet/stream files receive two readback passes. | low | Confirmed in the private stream seam and outer complete-evidence readback. Both remain bounded and preserve the existing standalone helper's checks; they introduce no incorrect result. Reject: material overhead is unlikely in everyday small captures, and eliminating the overlap while preserving directory-inventory validation adds a private mode/branch or callback machinery. |
| B5 / blind-hunter | The plan's registry digest mismatch is detected after other evidence acquisition. | low | Confirmed: the manifest authenticates registry bytes on acquisition and the one complete owner inspector later checks the plan's registry digest, before all checkout/Git access. Only approved manifest refs inside private custody are read. Reject: conflicting approved bindings are exceptional, no unsafe result or target operation follows, and an earlier duplicate guard adds a branch without changing the required refusal boundary. |
| B6 / blind-hunter | A syntactically valid foreign source commit survives inventory shape validation. | low | Confirmed: complete descriptors are shape/path/digest validated before following refs, and cross-scope semantics are enforced by the complete owner inspector before Git. The executed foreign-source test proves that denial order. Reject: an extra plan-commit branch/parameter duplicates semantic validation for an exceptional invalid manifest; no valid result or outside-custody read occurs. |
| B7 / blind-hunter | Lexically ancestor-related file refs survive inventory shape validation. | low | Confirmed: canonical distinct names can be lexically ancestor-related, then the held regular-file/non-following opener refuses the impossible layout before Git. This grants no additional path or authority and returns no partial result. Reject: this malformed approved layout is unlikely in everyday use; pairwise ancestry/sidecar collision guards add complexity beyond the required canonical/distinct-file checks. |
| B8 / blind-hunter | New evidence lacks focused late ownership/permission mutation tests. | false | The unchanged `_HeldFile.read/revalidate` applies the same uid/gid/mode/nlink and ancestor identity/ctime checks to every new artifact, and the retained tests verify all descriptors survive Git and are read back. Existing `test_replacement_removal_or_rewrite_of_each_file_during_inspection_returns_nothing` and `test_restored_permissions_require_directory_ctime_for_root_and_each_artifact_chain` exercise late privacy failure in that shared guard. No unverified new ownership branch was identified. |
| B9 / blind-hunter | Valid executable and identical source byte forms lack positive retained integration coverage. | low | The new reconstruction transfers SourceSnapshot metadata and bytes, while successful fixtures only exercise 100644 and CRLF/LF pairs. Route: patch a valid executable source case that also uses identical executed/blob bytes in distinct files and verifies metadata and occurrence accounting. |
| E1 / edge-case-hunter | Oversized controls are read before the newly documented pre-read refusal. | low | Same factual mismatch as B2: existing controls use bounded overflow-detecting reads, while new evidence uses size preflight. Route: patch the overbroad documentation, grouped with B2 by the same runbook wording root cause. |
| V1 / verification-gap | Losing 100755 during retained reconstruction survives all 28 tests. | low | Pre-verified regression gap: the reviewer ran the mode-forcing mutation against all retained tests and a separate real executable fixture. Route: patch positive executable-mode acquisition/corroboration coverage; grouped with the executable-mode part of B9. |

All three review layers completed before triage. Surviving patches are documentation corrections/resource disclosure and valid-source-variant verification. No production defect, intent/spec gap or deferred entry remains; all other findings are individually refuted or rejected above under the workflow's low-impact complexity rule.

## Verification

Run unittest discovery/execution in `tests/tooling/access_telemetry_c1_interchange`: `test_owner_c115_retained.py`, then `test_*.py`, with `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0`; require zero failures/errors/skips. Check `git diff --check` and path accounting.

## Dev Agent Record

- Baseline discovery immediately before development: 611 test cases, loader errors 0. Post-development discovery: 639 cases, loader errors 0; phase +28, cumulative +28, external +0. New `test_owner_c115_retained.py` discovery scope: 0 -> 28 cases. Fixture TestCases remain module-qualified and are not rediscovered.
- Focused execution: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_retained.py'` — 28 tests in 49.720s, `OK`; failures 0, errors 0, skips 0. Log: `/tmp/owner-c115-retained-focused.log`.
- Full affected regression execution: the same command with `-p 'test_*.py'` — 639 tests in 339.984s, `OK`; failures 0, errors 0, skips 0. Log: `/tmp/owner-c115-retained-interchange.log`. Existing pure/current-plan/explicit-anchor/legacy-fixture/deployed-refusal contracts pass.
- The root agent read the baseline-derived unified diff including all untracked files at `/tmp/story27-4-retained-implementation-881_h416.diff` and verified all five execution tasks. Every approved matrix row has passing focused evidence: valid one/two/three-Pod corroboration; authority refusal before evidence acquisition; invalid schema/hash/budget/aliases/privacy/scope before Git; drift/withdrawal/cleanup discarding results after closing held files and children.
- Named cross-tenant/namespace/session/commit denial evidence: `OwnerC115RetainedTests.test_foreign_tenant_namespace_session_and_commit_deny_before_git` and `test_foreign_source_commit_registry_and_owner_materials_deny_before_git` pass in the focused and full commands. Counted barriers deny checkout/Git/process/network/ambient-clock access. These are the new retained-acquisition surface's denial evidence, preserving the Story 20.2 and Story 24.3 boundaries.
- Descriptor/race evidence: `test_all_evidence_descriptors_survive_git_and_readback_with_status_last`, `test_same_size_evidence_drift_discards_computed_result_with_coarse_metadata`, `test_mid_git_and_final_withdrawal_discard_result_and_close_children`, `test_cleanup_faults_normalize_after_closing_every_descriptor`, `test_initial_evidence_substitution_refuses_before_git_and_cleans_files`, `test_git_deadline_and_child_cleanup_faults_close_all_evidence_and_child`, and `test_effective_user_drift_after_git_discards_retained_result` pass. Refusals verify bounded content-free codes, no original cause/context and no partial result.
- Scope: the baseline-derived changed-path union matches the six cumulative File List paths exactly, without exclusions; `git diff --check` exits 0. Python remains LF and Markdown remains CRLF.

## Code Review Record

2026-10-15: All three context-free review layers completed against the full baseline-derived diff, including untracked files. All eleven findings were independently classified before grouping; the verification gap's executable-mode mutation evidence was accepted as pre-verified. Every finding and disposition is retained in Review Triage Log. Three patch groups produced only runbook clarifications and one positive executable/identical-byte-forms test. No runtime changes, unresolved findings, intent/spec gaps or deferred work remain.

- Root final focused verification: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_retained.py' -v` — 29 tests in 54.761s, `OK`; failures 0, errors 0, skips 0. Complete raw log: `/tmp/story27-4-spec25-focused-final.log`.
- Root final affected regression verification: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — 640 tests in 343.932s, `OK`; failures 0, errors 0, skips 0. Complete raw log: `/tmp/story27-4-spec25-interchange-final.log`.
- Root matrix execution audit matched every one of the 29 discovered retained-case IDs to its verbose passing output. Every frozen matrix row has executed passing coverage, including the named cross-tenant/namespace/session/commit denials in Dev Agent Record, authority-before-acquisition, schema/hash/budget/privacy refusal before Git, and same-size drift/withdrawal/cleanup discarding computed results. The added `test_committed_executable_and_identical_byte_forms_preserve_metadata_and_occurrences` passes through real local corroboration and verifies all returned source metadata, distinct single-link files and both byte occurrences.
- Root post-patch discovery: 640 interchange test cases, loader errors 0; review phase +1, cumulative +29, external +0. Retained discovery unit 28 -> 29 cases; create baseline 611 + development 28 + review 1 = 640. Evidence command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`.
- Root independently reproduced the descriptor-capacity limitation with a disposable 20-Pod fixture under RLIMIT_NOFILE=1024: bounded `owner-c115-retained-filesystem-unavailable` before inspection; descriptor count unchanged after cleanup. The runbook now explicitly describes resource availability separately from byte caps. Larger/deeper captures need sufficient OS descriptor capacity; no limit is changed automatically.
- Root reread the final patch delta after rewriting `/tmp/story27-4-retained-implementation-881_h416.diff`. Final path union from `git diff --name-only 99431f0fed102ea6ddbae33d28b9da4fc7f215a0` plus `git ls-files --others --exclude-standard` exactly matches the six File List paths, with no exclusions. Frozen intent SHA-256 is unchanged. Python LF, Markdown CRLF and `git diff --check` pass.
- Before review, the explicit six-path readiness gate passed in `in-progress` status with final line `Story review readiness validation passed.` The repository checker does not recognize the workflow's temporary `in-review` status; terminal `done` is checked separately without a bypass. Its terminal C1 skip is covered by the independent six-path audit above.
- Completion applies only to Spec 25. Its approved prohibition on workspace Git and sprint/history mutation takes precedence over the generic workflow's terminal commit/sprint-update actions. No staging, commit, push, branch, dependency update, sprint/history, Production or A41 mutation occurred. Story 27.4 and Spec 14 live qualification, standalone-checkout preparation, registration/gate acceptance and Production activation remain open; A41 remains partially closed. Existing owner policy approval persists.

- The first terminal readiness run could not see development/review ledger rows because pre-existing/inter-row blank lines split the Markdown table. Joining the existing rows without changing their content corrected the formatting; the final readiness result below supersedes that refusal.

- Final readiness gate: `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-25 --changed-files-file /tmp/story27-4-retained-changed-files.txt` — exit 0; final line `Story review readiness validation passed.` No bypass or checker change. `git diff --check` exits 0.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-15 | create-story | Codex drafted retained-evidence acquisition; implementation awaits approval. | Interchange 611 -> 611 test cases; phase/cumulative/external +0; loader errors 0. Command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. | matched 1/1 against frontmatter baseline using `git diff --name-only HEAD` plus `git ls-files --others --exclude-standard`; only this spec. |
| 2026-10-15 | create-story | Owner approved and stopped; status set to ready-for-dev with frozen intent unchanged. | Interchange 611 -> 611 test cases; phase/cumulative/external +0; loader errors 0. Command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. | matched 1/1 against frontmatter baseline using `git diff --name-only HEAD` plus `git ls-files --others --exclude-standard`; only this spec. |
| 2026-10-15 | dev-story | Added retained-evidence acquisition, shared control/evidence identity checks, private stream callback composition, 28 matrix/denial/race/cleanup cases and schema/API runbook documentation. Focused 28/28 and full interchange 639/639 pass without failures/errors/skips. | Interchange 611 -> 639 test cases; phase +28, cumulative +28, external +0; loader errors 0. New `test_owner_c115_retained.py` scope 0 -> 28 cases. Discovery: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. Execution commands/results are in Dev Agent Record. | matched 6/6 against `99431f0fed102ea6ddbae33d28b9da4fc7f215a0` using `git diff --name-only 99431f0fed102ea6ddbae33d28b9da4fc7f215a0` plus `git ls-files --others --exclude-standard`; no exclusions. `git diff --check` exits 0. |
| 2026-10-15 | code-review | Completed all three review layers and all eleven individual triage rows. Patched size-read wording, documented descriptor capacity and added a valid executable/identical-source-forms regression case. No runtime patch or deferred work. Root matrix audit, focused 29/29 and full interchange 640/640 pass without failures/errors/skips. | Interchange 639 -> 640 test cases; phase +1, cumulative +29, external +0; loader errors 0. Retained discovery scope 28 -> 29 cases. Discovery: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(len(loader.errors))"`. Final execution commands/results are in Code Review Record. | matched 6/6 against `99431f0fed102ea6ddbae33d28b9da4fc7f215a0` using `git diff --name-only 99431f0fed102ea6ddbae33d28b9da4fc7f215a0` plus `git ls-files --others --exclude-standard`; no exclusions. `git diff --check` clean. |

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-25.md`
- `docs/operations/access-telemetry-c1-owner-verification.md`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_retained.py`
- `tools/access_telemetry_c1_owner_c115_retained.py`
- `tools/access_telemetry_c1_owner_custody.py`
- `tools/access_telemetry_c1_retained_files.py`

2026-10-15: The user explicitly requested a commit after implementation and review. That request authorizes staging and a local commit of this completed slice. This scope records the existing execution tasks and cumulative File List for the commit hook; the frozen implementation intent is unchanged. Earlier no-commit statements describe the completed build phase before this request.

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-25.md`
- `docs/operations/access-telemetry-c1-owner-verification.md`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_retained.py`
- `tools/access_telemetry_c1_owner_c115_retained.py`
- `tools/access_telemetry_c1_owner_custody.py`
- `tools/access_telemetry_c1_retained_files.py`
