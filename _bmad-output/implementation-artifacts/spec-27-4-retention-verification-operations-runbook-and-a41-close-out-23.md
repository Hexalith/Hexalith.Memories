---
title: 'Story 27.4 current-plan C1.15 inspection'
type: 'feature'
created: '2026-10-14'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '379cb1a2f031f63c4a7148bbf32eb9d4d917dd6a'
context:
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** C1.15 inspection relies on caller-acquired run anchors and withdrawal data. The private-file wrapper checks plan/run/status but releases its descriptors before C1.15 inspection.

**Approach:** Hold the approved plan, current record and status through complete offline C1.15 inspection and final readback. Spec 14 live tasks remain open.

## Boundaries & Constraints

**Always:** Preserve owner policy/principal/self-review, PG2 pins, exact-byte checks, budgets and existing APIs/results. Derive anchors solely from outside-digest-approved plan bytes. Fix action to `observe`; require caller-supplied aware UTC `now`. Custody covers only the three acquired files; supplied evidence receives byte-consistency checks. The retained 2026-10-12 owner decision delegates repository implementation.

**Never:** Contact targets, invoke Git/producers/ambient clocks, issue execution/registration/acceptance authority, invent acceptance schemas, weaken deployed/predecessor refusal, update dependencies/submodules, stage/commit/push, or change Production/sprint/history/A41. Claim no complete evidence custody, simultaneous snapshot or continuing lock.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Behavior | Failure Behavior |
| --- | --- | --- | --- |
| Current approved plan | Matching private plan/run/status and valid one/two/three-Pod evidence | Existing frozen `OwnerC115Inspection` with detached exact evidence and self-review disclosure | No authority flag |
| Stale or unsafe files | Withdrawal, expiry, binding drift, missing/oversized/nonprivate/linked files, ancestor movement or readback drift | No result | Content-free refusal after descriptor cleanup |
| Foreign or forged evidence | Scope/source/Ref/argv/output mismatch, altered caches or excessive byte occurrences | Reinspect exact detached bytes and refuse inconsistencies | No partial inspection or dependency execution |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_owner_dispatch.py` — reuse `_validated_plan`, `_open_held_file` and closed status validation; preserve its seven-field contract.
- `tools/access_telemetry_c1_owner_c115.py` — reuse `inspect_owner_c115`, occurrence budgets, reparsing and detachment.
- `tests/tooling/access_telemetry_c1_interchange/test_owner_custody.py`, `test_owner_c115.py` — reuse filesystem/multi-Pod fixtures without duplicate test discovery.

## Tasks & Acceptance

**Execution:**

- [x] `tools/access_telemetry_c1_owner_custody.py` — extract private held-file composition. Preserve public checks/refusals and complete descriptor lifetime. Run composed inspection before exact bounded readback, status last.
- [x] `tools/access_telemetry_c1_owner_c115_plan_files.py` — add `inspect_owner_c115_from_plan_files(plan_path, approved_plan_sha256, *, custody_root, registry, capture, sources, streams, statement, command_contract, review_role_policy, now)`. Reuse plan `recordPath`; require additional canonical raw `registrySha256` only in this API. Derive six `OwnerRunAnchors` from the approved plan; acquire run/status through the held helper. Preflight supplied closed types/budgets, reparse snapshots and copy source/stream records before filesystem callbacks; include acquired run in the existing C1.15 budget. Call the full inspector for `observe` with validated status. Return existing result after final checks. Raise bounded content-free `OwnerC115PlanFilesError` without cause/context/partial result.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_plan_files.py` — cover the matrix, repeated fresh reads, plan-derived bindings, one/two/three Pods, caller/cache mutation, rehashed foreign namespace/tenant/session/commit/Pod inputs, byte boundaries, mid-inspection/final-readback withdrawal, coarse-timestamp rewrites and descriptor cleanup. Invalid supplied containers must precede filesystem reads; rejected run/status must precede C1.15 inspection. Count forbidden process/network/Git/clock calls; retain legacy regression coverage.
- [x] `docs/operations/access-telemetry-c1-owner-verification.md` — document API/bindings, acquisition order, custody limits and remaining live work.

**Acceptance Criteria:**

- Given an outside-approved plan and current private files, when the wrapper succeeds, then C1.15 uses that plan's exact registry digest and anchors and the freshly acquired run, with all three descriptors held through inspection and final status readback.
- Given final file drift, when readback runs, then even a computed valid inspection cannot escape.
- Given existing fixture and owner consumers, when affected regression suites run, then their public behavior and unconditional deployed refusal remain compatible.

## Implementation Notes

- Extracted `_compose_owner_run_files` in the existing custody module. Its
  optional composed inspector runs after the current owner run/status checks
  and before the existing exact readback (plan, record, status last). Public
  `dispatch_owner_run_files` retains its original result and refusal contract.
- Added `inspect_owner_c115_from_plan_files` with the specified explicit inputs.
  Supplied occurrence budgets, closed types, aware UTC, copied source/stream
  records and five reparsed snapshots are checked before filesystem callbacks.
  The six anchors and canonical raw `registrySha256` come only from the exact
  outside-digest-approved plan. The acquired run counts in the full C1.15 budget.
- All three file/ancestor descriptor chains survive complete `observe` inspection
  and final exact-byte readback; no inspection escapes a later drift refusal.
  The new bounded content-free error is raised after cleanup without a retained
  cause/context. No process, producer, Git, network or ambient clock is used.
- Added 27 tests using the existing private-file and multi-Pod fixture modules,
  without importing their TestCase classes into new discovery. The runbook now
  describes the API, acquisition order, byte budgets and limited custody.
  Spec 14 live work, complete evidence custody, Production and A41 remain open.

## File Scope

**Allowed to modify:**

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-23.md`
- `tools/access_telemetry_c1_owner_custody.py`
- `tools/access_telemetry_c1_owner_c115_plan_files.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_plan_files.py`
- `docs/operations/access-telemetry-c1-owner-verification.md`

## Spec Change Log

## Review Triage Log

All three independent review layers completed. Edge-case hunter returned `[]`;
verification-gap reviewer reported no gaps. Blind hunter's 64.852 kB diff gave
`min(floor(sqrt(64.852) + 1), 10) = 9` findings, individually triaged below.

| Finding | Verdict | Route | Evidence |
| :--- | :--- | :--- | :--- |
| BH1 — impossible stream collection copies | low | rejected | The count bound permits a large zero-byte tuple and detachment copies it before receipt-count refusal. The existing bound and occurrence budget still apply; this is bounded overhead for deliberately inconsistent input. Adding cardinality guards and parsing branches is more than a direct correction for this uncommon case. |
| BH2 — repeated detachment/reparsing | low | rejected | Pre-I/O detachment and the full existing inspector both copy records/reparse snapshots; immutable byte objects are shared. This satisfies the approved preflight and full-inspection requirements. A new internal validated API or allocation policy would add complexity to remove negligible ordinary-call overhead. |
| BH3 — registry binding checked after acquisition | low | rejected | Missing registrySha256 causes two extra bounded private-file acquisitions, then refuses before full C1.15. All reads stay within the approved custody root and no result escapes. Earlier validation requires a new helper callback or duplicated acquisition, beyond a direct correction for this uncommon invalid-plan case. |
| BH4 — evidence preflight before argument validation | low | rejected | Invalid caller paths/digests are refused before filesystem access, after bounded evidence preflight. Inexpensive reordering would require duplicate path/digest validation and additional error translation. The existing ordering meets the specified refusal contract; the wasted bounded work is negligible in ordinary use. |
| BH5 — callback TypeError label | low | rejected | An injected callback TypeError is normalized as filesystem-unavailable. The required bounded content-free error, cleanup and absence of a partial result are preserved. The sole real callback is the existing full inspector; separating exception boundaries for an internal callback failure adds complexity without changing the promised public refusal behavior. |
| BH6 — dispatch-module prebound dependencies | medium | patch | The new barrier scans omit owner_dispatch although _compose_owner_run_files calls its dispatcher and held-file implementation. Canonical patches cannot detect a prebound alias in that composed module. Include it and retain a counted prebound-alias self-check. |
| BH7 — direct environment reads | low | rejected | Direct environment reads are outside this new fixture's counted process/network/Git/clock barrier. Neither new wrapper nor composed dispatch/custody code reads environment configuration, and the pure inspector retains its existing configuration-denial suite. Additional environment-mapping guards are more than a direct correction to the stated new barrier contract. |
| BH8 — new public API filesystem-error translation | medium | patch | Existing custody tests inject acquisition/seek/cleanup failures only through the legacy public API. New tests do not yet prove canary cause/context removal and computed-result suppression through OwnerC115PlanFilesError. Add focused fault-injection checks using the real composition. |
| BH9 — replacing an approved plan between calls | medium | patch | The repeated-read success test reacquires unchanged files; changed-anchor tests only exercise mismatch refusal. A successful second approved plan with changed registry/run bindings and later withdrawal is untested through this API. Add that transition to prove fresh binding consumption. |

## Verification

Baseline: 554 interchange test cases discovered, zero loader errors; 42 existing custody tests passed, zero failures/errors/skips. Development discovery: 581 interchange test cases, zero loader errors; new plan-files scope: 27 test cases. Phase and cumulative story discovery delta: +27; unrelated discovery delta: +0.

**Commands:**

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_plan_files.py' -v` — all new matrix/scope/dependency tests pass.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — all affected legacy/owner tests pass without failures/errors/skips; record discovery deltas separately from execution.
- `git diff --check` — no whitespace errors; manually confirm the four implementation paths plus this spec are the entire change footprint.

**Executed development evidence (2026-10-14):**

- Focused command above, with output redirected to `/tmp/spec23-plan-files-tests.log`: 27 tests passed in 20.101 seconds, zero failures/errors/skips.
- Full command above, with output redirected to `/tmp/spec23-interchange-tests.log`: 581 tests passed in 240.290 seconds, zero failures/errors/skips. All 554 existing cases remained discovered and executed, including the 42 existing custody cases, owner/fixture registration, command contracts and unconditional deployed refusal.
- Discovery command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases())"` — 581. Focused discovery with `pattern='test_owner_c115_plan_files.py'` — 27; loader error count — 0.
- Matrix coverage passed: approved current plans and one/two/three Pods (`test_one_two_three_pods_and_orders_return_existing_frozen_detached_inspection`); stale/unsafe files and cleanup (`test_changed_record_status_withdrawal_expiry_and_observe_reject_before_c115`, `test_missing_oversized_nonprivate_linked_and_moved_files_refuse_before_c115`, `test_early_plan_run_and_status_refusals_close_all_partial_descriptor_stacks`); forged/foreign evidence and budgets (`test_forged_snapshot_caches_are_discarded_before_file_acquisition`, `test_filesystem_callback_caller_mutations_cannot_replace_detached_evidence`, `test_excessive_occurrences_and_every_artifact_byte_form_refuse_before_io`, `test_32_mib_boundary_charges_acquired_run_and_one_extra_byte_refuses`).
- Cross-tenant/scope denial evidence passed in the focused and full commands: `test_rehashed_foreign_namespace_tenant_session_commit_refuse`, `test_rehashed_foreign_later_pod_arguments_and_outputs_refuse`, and `test_foreign_source_ref_and_raw_output_consistency_refuse`. Counted process/network/Git/clock barriers recorded no attempted dependency calls during inspection.
- Descriptor lifetime and result suppression passed: `test_all_artifact_and_ancestor_descriptors_survive_complete_c115_then_close`, `test_mid_source_inspection_withdrawal_prevents_complete_valid_inspection_escaping`, `test_withdrawal_during_inspection_discards_computed_valid_result_and_closes`, `test_final_readback_withdrawal_is_checked_after_plan_and_run`, and `test_coarse_timestamp_same_size_rewrites_of_each_artifact_refuse_exact_bytes`.
- `git diff --check` — exit 0, no whitespace errors. Syntax parsing of all three affected Python files also passed. Changed-file reconciliation: exactly the four implementation paths and this spec; no dependencies/submodules, staging, commits, pushes, Production, sprint, history or A41 records changed.

## Code Review Record

All three independent review layers and every in-scope chunk completed on
2026-10-14. Every blind-hunter finding has its own verdict above; none was
deferred. BH6, BH8 and BH9 were patched with three additional test cases:
`test_dispatch_prebound_process_and_clock_aliases_are_counted_and_denied`,
`test_acquisition_seek_and_cleanup_canary_faults_normalize_after_descriptor_cleanup`,
and `test_between_call_approved_plan_registry_and_run_replacements_use_new_bindings`.
The parent reread the complete patched unified diff at
`/tmp/spec23-implementation-k89xtc9y.diff`, checked all four tasks and three
acceptance criteria, and confirmed that every matrix row has executed passing
tests. The production implementation needed no review patch.

Final parent execution used the exact focused/full commands in Verification,
redirected respectively to `/tmp/spec23-parent-focused-tests.log` and
`/tmp/spec23-parent-interchange-tests.log`: 30 focused tests passed in 19.958
seconds and all 584 interchange tests passed in 222.770 seconds, both exit 0,
with zero failures/errors/skips. All 554 legacy cases remain discovered and
executed. Final discovery is 584 interchange and 30 plan-files test cases,
zero loader errors. Review delta is +3 (581 -> 584; focused 27 -> 30), cumulative
story delta +30 from 554, external delta +0; the focused cases are included in
the interchange count, not added twice. Evidence command:
`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_owner_c115_plan_files.py').countTestCases()); print('loader errors:', len(loader.errors))"`.
The named cross-tenant/scope denial, withdrawal/readback, descriptor cleanup,
byte-boundary and counted dependency tests all passed on the patched tree.

Repository completion commands use this standalone spec key:

- `env PYTHONDONTWRITEBYTECODE=1 python3 tools/check-story-file-scope.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-23 --changed-files-file /tmp/spec23-changed-files.txt`.
- `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-23 --changed-files-file /tmp/spec23-changed-files.txt`.
- `git diff --check`.

Before review the readiness command passed at in-progress, accounting for all
five paths. At the workflow's transient in-review state it exited 1: C3 reports
that in-review is not one of backlog, ready-for-dev, in-progress, review, done.
The final done state uses a recognized label; no bypass or gate change is used.
The initial file-scope command exited 1 because this spec had no parseable File
Scope; completion metadata now declares exactly the five already-approved paths.
File List is independently compared for exact set equality against the actual
baseline-derived changed/untracked inventory, because the readiness tool skips
its diff comparison at done. No exclusion is needed.

Completion receipt: the file-scope command above exited 0, final line
`Story file scope validation passed.` The readiness command exited 0 at done,
final line `Story review readiness validation passed.` `git diff --check`
exited 0. The independent baseline-derived inventory/File List comparison
matched 5/5 with no missing or extra path; HEAD and the index are unchanged.

Only this standalone spec completes. Its frozen constraints prohibit the
generic workflow's local commit and sprint synchronization defaults. HEAD and
the index, Production, sprint/history/A41 and Spec 14 live tasks remain
unchanged. Custody covers acquired plan/run/status only; this result grants no
execution, deployed registration, gate acceptance or continuing lock.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-14 | create-story | Spec 23 baseline adopted by Codex; owner approved and stopped before implementation. | Interchange: 554 -> 554 test cases; phase +0, cumulative +0. Command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases())"`. | matched 1/1 against `379cb1a2f031f63c4a7148bbf32eb9d4d917dd6a`; `git diff --name-only HEAD` plus `git ls-files --others --exclude-standard`; sole change is this spec. |
| 2026-10-14 | dev-story | Added current-plan C1.15 inspection within held private plan/run/status acquisition, detached preflight evidence, final status-last exact readback, bounded refusals and runbook documentation. Focused 27 and full 581 cases passed with zero failures/errors/skips; live work remains open. | Interchange: 554 -> 581 test cases; phase +27, cumulative +27, external +0. New plan-files discovery scope: 0 -> 27 test cases, included in the interchange +27 (not added twice). Command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases())"`; focused discovery uses `pattern='test_owner_c115_plan_files.py'`. Loader errors: 0. | matched 5/5 against `379cb1a2f031f63c4a7148bbf32eb9d4d917dd6a`; `git diff --name-only HEAD` plus `git ls-files --others --exclude-standard`; four implementation paths plus this spec, with no exclusions. |
| 2026-10-14 | code-review | All three independent review layers and all in-scope chunks completed; owner: Codex parent reviewer. Triaged all nine findings, patched three verification gaps with three cases, independently reread the patched diff and passed every task/AC/matrix check. No deferred findings; scope metadata records the existing five approved paths. | Interchange: 581 -> 584 test cases; phase +3, cumulative +30 from 554, external +0. Plan-files scope: 27 -> 30 test cases, included in interchange +3. Command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_owner_c115_plan_files.py').countTestCases()); print('loader errors:', len(loader.errors))"` — 584 / 30 / zero loader errors. Parent focused 30 and full 584 passed, zero failures/errors/skips, exit 0. | matched 5/5 against `379cb1a2f031f63c4a7148bbf32eb9d4d917dd6a`; `git diff --name-only HEAD` plus `git ls-files --others --exclude-standard`, with exact File List set equality and no exclusions. HEAD/index and all protected paths unchanged. |

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-23.md`
- `tools/access_telemetry_c1_owner_custody.py`
- `tools/access_telemetry_c1_owner_c115_plan_files.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115_plan_files.py`
- `docs/operations/access-telemetry-c1-owner-verification.md`
