---
title: 'Story 27.4 current owner record and private custody'
type: 'feature'
created: '2026-10-12'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'e001d1d60c695ab172120ca458c70e58edeacdd2'
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The owner-plan dispatcher requires its caller to read the current record, but no filesystem caller establishes that freshness or private custody.

**Approach:** Add an offline file wrapper that binds the plan-named current record, checks local private custody, and retains file identities through dispatch before returning the existing inspection.

## Boundaries & Constraints

**Always:** Preserve PG2, owner policy, action/time checks and self-review. Compare caller custody root with approved plan; pin its exact bytes to the outside digest. Require canonical, distinct plan/record/status paths below the plan's custody root. The wrapper requires an additional plan `recordPath`; the existing byte dispatcher retains its seven-field contract. Check effective OS-user ownership, no group/other permissions on custody directories/files, regular single-link files, bounded reads, stable metadata and non-following canonical traversal. Hold plan/record/status and ancestor identities through dispatch; revalidate before return. OS ownership proves custody only. Sanitize refusals.

**Never:** Produce execution, gate, checkpoint, publication or A41 authority. Alter deployed refusals, pure inspector, sprint/history, Production configuration or A41; contact targets or register collectors. Cache record/status bytes between calls or downgrade on refusal. Infer GitHub authentication from an OS UID.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :--- | :--- | :--- | :--- |
| Current | Private bound plan/record/status; valid action/window | Existing `OwnerRunInspection` only | No execution flag |
| Drift | Replacement, removal, rewrite or ancestor movement during dispatch | No returned inspection | Revalidate held identities |
| Custody | Outside, aliased, symlink, hard-link, foreign-owner or non-private input | Refusal | No outside read |
| Later use | Record or withdrawal changed since prior call | Fresh refusal | No cached approval |
| Invalid | Missing binding, malformed, oversized, unsupported filesystem | Content-free refusal | No partial result or exception chain |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_owner_dispatch.py` — reuse plan/status validation and descriptor traversal; existing bytes API behavior stays compatible.
- `tools/access_telemetry_c1_owner_run.py` — pure inspector/result, unchanged.
- `tools/access_telemetry_c1_interchange.py` — `JsonSnapshot`, canonical validation and artifact bounds; no filesystem reader.

## Tasks & Acceptance

**Execution:**
- [x] `tools/access_telemetry_c1_owner_custody.py`, `tools/access_telemetry_c1_owner_dispatch.py` — implement the file wrapper and reuse traversal safely; keep descriptors through dispatch and normalize refusals after internal handlers exit.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_owner_custody.py` — cover every matrix row, record/plan/status races during dispatch, private-mode/UID denials, exact/oversize budgets and forbidden process/network dependencies.
- [x] `docs/operations/access-telemetry-owner-handoff.md`, `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md` — document actual API, recordPath binding, local permission/freshness evidence and remaining execution requirements.

**Acceptance Criteria:**
- Given an approved plan and current private files, when the wrapper is called twice, then each result uses freshly read matching bytes and grants only the requested inspection.
- Given record, plan, status or ancestor drift before return, when dispatch runs, then no inspection escapes and no target dependency runs.
- Given existing byte-dispatch callers and deployed consumers, when the affected suite runs, then compatibility and deployed refusals remain intact.

## Implementation Notes

- Added `dispatch_owner_run_files(plan_path, approved_plan_sha256, *, custody_root, now, action)` and content-free `OwnerRunCustodyError`. The plan's new `recordPath` binds the current record; exact caller/plan custody root equality and outside plan digest validation precede record/status reads.
- Reused the existing byte dispatcher's seven-field plan validation, bounded non-following descriptor traversal, status binding and pure inspector path. The byte API remains compatible; the pure inspector and deployed consumers are unchanged.
- Enforced effective OS-user ownership, no group/other permissions at/below custody, regular single-link distinct files, metadata/read bounds, and held plan/record/status/ancestor descriptors through inspection and final revalidation. Custody directory ctime checks begin at the approved root; unrelated public-ancestor content changes do not invalidate stable private inputs. Refusals are normalized after descriptor cleanup and handlers exit.
- OS ownership is local custody evidence only. No process/network/target call, collector registration, execution/gate/checkpoint/publication/A41 authority or persistent byte/status cache was added. Metadata checks describe local freshness at those checks, not immutability or a lock after return.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| :--- | :--- | :--- |
| BH1 timestamp repetition | medium | Parent reproduced 77/100 inspections after same-size false-to-true status rewrites with restored mtime. Patch exact-byte readback of held artifacts and a deterministic coarse-timestamp regression. |
| BH2 late withdrawal | medium | Parent reproduced status withdrawal during the second post-inspection root-stat check while an observe inspection returned. Patch final validation/readback order with status last and cover mutations during other artifacts' checks. |
| BH3 public ancestor link count | medium | Parent reproduced refusal when an unrelated sibling directory was created above custody. Patch directory identity comparison to ignore directory link-count churn while retaining file single-link enforcement; cover mkdir/rmdir. |
| BH4 unavailable metadata | medium | Parent supplied otherwise valid os.stat_result values with absent nanosecond timestamps; inspection returned. Patch rejection of unavailable required freshness metadata and regressions. |
| BH5 device-open activation | maybe-false | Leaf open precedes its regular-file check, but the alleged device activation was not demonstrated; it would need an allowed private device whose open has an observable side effect. In this owner-controlled inspection boundary that hypothetical harm is low. Rejected: a new metadata-handle protocol adds platform dependence and branches beyond a direct correction; no device bytes are read. |
| BH6 traceback locals | low | Python frames retain plan bytes as reported; normal exception rendering emits the code and no raw input or chain. Rejected: clearing caller/frame locals adds complexity for deliberate debugger-local inspection and does not establish a new confidentiality boundary. |
| BH7 custody scope wording | medium | The reader checks three named inputs and their ancestor chains, while both documents say every descendant is checked. Patch those documents to describe the actual checked scope; no recursive archive scan is required. |
| BH8 distinct-file branch evidence | low | No existing test reaches the distinct single-link device/inode refusal; ordinary hard links and equal paths fail earlier. Patch a consistent alias-metadata denial fixture without production changes. |
| BH9 denied-file read ordering | medium | Permission/UID/hard-link tests assert refusal and inspector call count but do not observe denied-file reads. Patch inode-specific read sentinels so a read-before-denial regression fails. |
| EC1 public ancestor mkdir/rmdir | medium | Independently reproduced public-directory link-count churn; same root cause as BH3. Patch with BH3, retaining full-chain name/inode/mode/ownership checks. |
| EC2 all-drift-before-return claim | medium | The reported late status rewrite is reproduced by the parent. The proposed spec-only narrowing is rejected under the workflow; the runtime ordering correction and regression are covered by BH2 without editing frozen intent. Individual filesystem checks remain point-in-time evidence. |
| VG1 restored directory permissions | medium | Pre-verified mutation removes private-directory ctime checks while all 99 owner tests pass; restored directory chmod then returns inspection. Patch root/nested-directory restoration regressions with deterministic changed-ctime evidence. |

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_*.py' -v` — all owner cases pass without skips.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v` — comparable affected discovery and regression results.
- `git diff --check` and baseline diff of pure inspector, deployed refusals, sprint/A41/Production files — no protected changes.

### Development evidence (2026-10-12)

- Focused command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_*.py' -v` — 99 unittest methods passed, zero failures/errors/skips; receipt `/tmp/story-27-4-owner-custody-owner-after.log`.
- Comparable full command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v` — pre-development 305 unittest methods passed (`/tmp/story-27-4-owner-custody-dev-before.log`); post-development 341 passed, zero failures/errors/skips in 68.249s (`/tmp/story-27-4-owner-custody-dev-final.log`). Phase +36, cumulative +36; no external discovery delta.
- `git diff --check` passed. `git diff --exit-code e001d1d60c695ab172120ca458c70e58edeacdd2 -- tools/access_telemetry_c1_owner_run.py tools/access_telemetry_c1_interchange.py tools/access_telemetry_c1_approval_policy.py tools/access_telemetry_c1_live_harness.py tools/access_telemetry_c1_predecessor.py _bmad-output/implementation-artifacts/sprint-status.yaml docs/governance src tests/Hexalith.Memories.AccessTelemetry.Tests .github deploy` exited 0; the entire cumulative changed set is the six allowed paths below. Pure inspector, deployed refusals, sprint/history, A41 and Production configuration are unchanged.

| Matrix row | Focused evidence in `OwnerRunCustodyTests` | Observed result |
| :--- | :--- | :--- |
| Current | `test_each_call_reads_all_current_files_and_returns_only_existing_inspection`, `test_existing_actions_windows_pg2_policy_and_self_review_checks_are_preserved` | Every call freshly reads all three files; only the requested immutable self-reviewed inspection returns. Existing action/time/PG2/policy refusals hold. |
| Drift | `test_replacement_removal_or_rewrite_of_each_file_during_inspection_returns_nothing`, `test_plan_or_record_replacement_while_reading_is_not_used_for_dispatch`, `test_file_rewrite_during_final_ancestor_checks_is_revalidated_before_return`, ancestor movement and nested-plan movement tests | Plan/record/status replacement, removal, rewrite and mode/link drift during inspection, read races and named ancestor movement return no inspection; descriptors stay held and are closed on success/refusal. |
| Custody | `test_outside_and_prefix_sibling_paths_are_refused_without_outside_reads`, `test_non_following_traversal_refuses_symlink_ancestor_above_custody_without_reads`, `test_foreign_owner_on_custody_directories_and_files_refuses`, private-mode, symlink and hard-link tests | Canonical/distinct/root bindings, effective UID, permissions, regular/single-link checks deny unsafe custody before inspection without following outside aliases. |
| Later use | `test_record_changes_between_calls_are_not_cached`, `test_withdrawal_and_missing_status_between_calls_are_not_cached` | Changed records, withdrawal and missing state produce fresh refusals; updated approved bindings are read anew. |
| Invalid | `test_malformed_duplicate_secret_and_status_binding_errors_are_sanitized`, `test_exact_limit_each_file_is_accepted_with_bounded_reads`, `test_each_oversize_file_consumes_only_its_budget_then_refuses`, `test_filesystem_errors_unsupported_operations_and_cleanup_errors_have_no_exception_chain` | Exact limits pass; malformed/oversize/unsupported inputs refuse with no partial result or internal cause/context. |
| No target dependencies | `test_positive_and_negative_calls_never_use_process_network_target_or_hidden_clock` | Positive and negative calls pass with process, network, target-adjacent file helpers and hidden clock dependencies forbidden. |
| Public ancestor availability | `test_unrelated_sibling_change_above_custody_does_not_invalidate_private_inputs` | Unrelated public-ancestor content changes pass; all named ancestor identity checks remain enforced. |

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_owner_custody.py`
- `tools/access_telemetry_c1_owner_dispatch.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_custody.py`
- `docs/operations/access-telemetry-owner-handoff.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-16.md`

## File List

- `tools/access_telemetry_c1_owner_custody.py`
- `tools/access_telemetry_c1_owner_dispatch.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_custody.py`
- `docs/operations/access-telemetry-owner-handoff.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-16.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :--- | :--- | :--- | :--- |
| 2026-10-12 | create-story | Plan current-record/private-custody wrapper under retained owner repository-implementation approval. | Phase +0, cumulative +0; 305 -> 305 unittest methods. Full discovery command above: 305 passed, zero failures/errors/skips; receipt `/tmp/story-27-4-owner-custody-before.log`. | Matched 1/1 against `git diff --name-only e001d1d60c695ab172120ca458c70e58edeacdd2` plus `git ls-files --others --exclude-standard`; this spec only, no exclusions. |
| 2026-10-12 | dev-story | Implement current plan-bound record/private custody wrapper, retained descriptor traversal and final revalidation; preserve byte API and deployed refusal boundaries; document API and remaining execution requirements. | Phase +36, cumulative +36; 305 -> 341 unittest methods. Exact comparable command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v`; receipts `/tmp/story-27-4-owner-custody-dev-before.log` and `/tmp/story-27-4-owner-custody-dev-final.log`. Post: 341 passed, zero failures/errors/skips. Focused owner regression: 99 methods passed via the owner command above, receipt `/tmp/story-27-4-owner-custody-owner-after.log`. | Matched 6/6 against `git diff --name-status e001d1d60c695ab172120ca458c70e58edeacdd2` plus `git ls-files --others --exclude-standard`; six allowed paths, no exclusions. |
| 2026-10-12 | code-review | Complete all three review layers; implement exact-byte readback/status-last ordering, timestamp availability, public-directory identity correction, denial regressions and precise custody documentation. No deferral. | Phase +6, cumulative +42; 341 -> 347 unittest methods; exact comparable full Verification command above. Parent post-patch receipt `/tmp/story-27-4-owner-custody-parent-final-full.log`: 347 passed, zero failures/errors/skips. Focused owner result 105 passed; its scope is reported separately without a separate delta. | Matched 6/6 against `git diff --name-status e001d1d60c695ab172120ca458c70e58edeacdd2` plus `git ls-files --others --exclude-standard`; six declared paths, no exclusions. |

## Final Reviewed Verification

All three review layers completed against diff SHA-256 `9092766a8f961fd66ed2c447026910628de9eed8a9707d3a262c5c391cbab63d`. All twelve findings have individual triage rows. Every selected correction is implemented; nothing is deferred. The parent read the entire original diff and the complete correction delta; patched diff SHA-256 `cd139bd6862773d70f390d89b77ccfb12e388b926b7e6ab80c1bebe1101c31cf` is retained at `/tmp/story-27-4-owner-custody-patched-pjrufjw1.diff` (before final evidence notes).

Final parent full Verification command: 347 unittest methods passed in 48.465s, zero failures/errors/skips, exit 0; receipt `/tmp/story-27-4-owner-custody-parent-final-full.log`. Final parent focused owner command: 105 methods passed in 0.522s, zero failures/errors/skips, exit 0; receipt `/tmp/story-27-4-owner-custody-parent-final-focused.log`.

New review regressions executed and passed: `test_same_size_rewrite_with_repeated_ctime_and_restored_mtime_refuses_each_artifact`, `test_late_withdrawal_during_plan_or_record_final_checks_is_read_back_last`, `test_required_file_and_private_directory_nanoseconds_must_exist_as_integers`, `test_distinct_single_link_device_inode_alias_is_refused_before_status_read`, `test_restored_permissions_require_directory_ctime_for_root_and_each_artifact_chain`, and `test_unrelated_sibling_directory_creation_and_removal_above_custody_are_allowed`. Existing permission/foreign-owner/hard-link tests now assert no denied-artifact read by actual inode. Every original matrix row has passing coverage in the final run; exact-limit reads remain bounded and unsupported seek operations refuse.

The implementation keeps its inspection-only result and no execution authority. Byte/file acquisition checks provide sequential point-in-time evidence ending with status readback and validation; they do not establish a simultaneous filesystem snapshot or continuing lock. Only this bounded slice is complete. Parent Story 27.4 remains in-progress; six live tasks, gate/consumer integration, qualification and A41 close-out remain outstanding. The parent sprint row is deliberately unchanged by this slice.

Nine protected file hashes and frozen intent match their pre-implementation snapshots. `git diff --check` passes. Scope/readiness checks with this slice's explicit spec key and all six paths pass before the workflow's temporary in-review transition; the checker recognizes review rather than in-review, so final readiness is checked again at done without changing the checker or parent tracker.

## Cross-Tenant Negative Evidence

**Surfaces:** Offline owner record/plan/status selection, attribution and custody paths. No live tenant routing, storage or authorization changes.
**Tests:** `test_outside_and_prefix_sibling_paths_are_refused_without_outside_reads`, `test_malformed_duplicate_secret_and_status_binding_errors_are_sanitized`, `test_foreign_owner_on_custody_directories_and_files_refuses`, `test_distinct_single_link_device_inode_alias_is_refused_before_status_read` and the no-target-dependency sentinel.
**Command:** `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_*.py' -v`.
**Result:** 105 unittest methods passed, zero failures/errors/skips; wrong-root/session/status/owner and alias cases refuse without inspection or denied-file reads. Offline fixtures supply no live cross-tenant qualification evidence.

Final executable gates (all six changed paths in `/tmp/story-27-4-owner-custody-changed-files.txt`): `python3 tools/check-story-file-scope.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-16 --changed-files-file /tmp/story-27-4-owner-custody-changed-files.txt` exits 0 with `Story file scope validation passed.`; the same key/path arguments to `tools/check-story-review-readiness.py` exit 0 with `Story review readiness validation passed.` at done. Its status-dependent cumulative check is skipped at done; the prior in-progress check and independent six-file reconciliation supply that evidence. `tools/check-tenant-isolation-evidence.py` reports `No tenant-isolation surface changed; evidence check is a no-op.` The attached offline negative tests did run and pass.
