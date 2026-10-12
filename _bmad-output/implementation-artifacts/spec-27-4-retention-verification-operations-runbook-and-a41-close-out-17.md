---
title: 'Story 27.4 C1.15 command declaration verification'
type: 'feature'
created: '2026-10-12'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '0281b30fb2618668efa9b8904afc75e18820eed8'
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
  - 'docs/operations/access-telemetry-c1-pg2-c1-15-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Pinned C1.15 captures can declare correctly hashed commands that target another cluster or perform another operation.

**Approach:** Add a separate pure inspector reusing pin inspection and checking exact producer arguments and per-Pod command sequence. Return inspection data only.

## Boundaries & Constraints

**Always:** Bind exact context, namespace, selector, projection, Pod, container, executable and both fixed probes. Require context, initial selection, three commands per Pod in observation order, final recheck. Preserve immutable bytes, structural/hash/chronology checks and sanitized refusals. Reuse dispatch_owner_run_files for any current owner inspection; it grants no execution authority.

**Never:** Execute commands, read ambient configuration/time, access targets, faults, purge or publish. Alter existing APIs, deployed refusals, Production disablement, parent Story 27.4 in-progress, A41 open or history. Matching declarations prove no execution, source provenance, stream content, custody, authority, registration or gate acceptance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Complete | Exact producer arguments, 1/2/3 Pods and any Pod order | Immutable inspection retaining exact snapshot and derived commands | No authority flags |
| Wrong meaning | Rehashed context/namespace/selector/Pod/container/probe/verb/option substitution | No command inspection | Content-free refusal |
| Wrong sequence | Same structurally valid receipts reordered, missing or surplus | No command inspection | Sequence or inherited structural refusal |
| Invalid capture | Blocked, pin mismatch, malformed/hash/chronology contradiction | No command inspection | Existing refusals preserved |
| Compatibility | Simplified structural fixtures and legacy consumers | Existing APIs behave as before | New strict API may refuse |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_capture_semantics.py` — reuse inspect_c115_observation_pins; add C115CommandInspection and inspect_c115_command_declarations.
- `tools/verify-access-telemetry-c1.ps1` — read-only grammar at 678, 683, 789, 803, 904, 939; preserves observation Pod order.
- `tools/access_telemetry_c1_interchange.py` — unchanged parse_capture validates hashes, purpose sets and chronology, not argv meaning.
- `tools/access_telemetry_c1_producer_bindings.py` — unchanged source API; one-Pod fixture registration is separate outstanding work.
- `tests/tooling/access_telemetry_c1_interchange/test_capture_semantics.py` — independent fixtures and counted denial barriers; preserve simplified-fixture tests.

## Tasks & Acceptance

**Execution:**
- [x] `tools/access_telemetry_c1_capture_semantics.py` — implement exact grammar/sequence comparison and immutable command inspection; no I/O or accepting consumer.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_capture_commands.py` — test every matrix row using independent argv fixtures, rehashed negatives, later Pods, order, immutable bytes and dependency barriers.
- [x] `docs/operations/c1-command-declaration-inspection-contract.md`, `docs/operations/c1-observation-pin-inspection-contract.md` — document API, grammar, compatibility and proof limits.
- [x] `_bmad-output/implementation-artifacts/tests/27-4-capture-command-verification/verification-receipts.zip` — retain exact focused/affected/review evidence and source/diff hashes.

**Acceptance Criteria:**
- Given a correctly hashed and pinned capture whose command selects a different scope or operation, when strict command inspection runs, then it refuses even though the old declaration inspector succeeds.
- Given valid commands for every declared Pod, when strict inspection runs, then its count is 3 times Pod count plus 3 and its purposes follow observation order, without acceptance or execution authority.
- Given the protected baseline, when affected tests and all three review layers finish, then legacy refusals and protected state remain unchanged and only this slice completes locally.

## Implementation Notes

No intent gaps or irreversibles. Implementation and local commit are explicitly authorized. This pure API needs no current owner record.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| BH1 discovered integration | medium | Actual producer-to-inspector compatibility was checked only by the archived runner; normal CI discovery misses future producer literal drift. Patch by promoting that check to the existing new test file. |
| BH2 archive completion | medium | Reviewed ZIP lacks review reports and reviewed diff/spec snapshot. Patch final explicit archive inventory after review, retaining original diff/hash and each layer report. |
| BH3 wildcard archive inventory | low | Generator glob admits any matching /tmp file, so a later run can absorb stale receipts. Patch direct correction to a closed named inventory; retain only this invocation's identified evidence. |
| BH4 hardcoded runner root | low | Both archived scripts use this checkout's absolute path. Patch direct correction to repository-root CWD and document exact runner invocation. |
| BH5 Git receipt grammar wording | low | sourceCommands arguments receive inherited structural/hash checks, so rehashed Git clean arguments remain inspectable. Broad read-only grammar wording is ambiguous. Patch documentation explicitly limiting exact argv checks to kubectl; source provenance stays separate. |
| BH6 later argument lengths | medium | New negative tests substitute all positions but cover added/removed argv mainly on initial selection. Patch same tests across all command families, later Pods and final recheck. |
| BH7 strict summary order | medium | Explicit summary-order positive coverage exists for the legacy pin API only. Patch an independently reordered-summary positive case for the strict API. |
| BH8 Code Map test location | false | Code Map accurately identifies test_capture_semantics.py as the fixture/barrier source, while Tasks & Acceptance explicitly identifies test_capture_commands.py as the implementation/test destination. It does not claim the former is the new test location; reject the proposed spec-only edit. |
| EC1 archive review identity | medium | Reviewed ZIP cannot yet identify the reviewed diff or review reports. Same root cause as BH2; patch final archive without changing frozen intent. |
| VG1 discovered producer compatibility | medium | Pre-verified symbol/CI search and alpha-probe mutation demonstrate producer/inspector drift can evade ordinary discovery. Same root cause as BH1; patch the discovered actual-producer regression. |

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture*.py' -v`
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v`
- `git diff --check`; explicit spec-key scope/readiness checks; protected-path baseline comparison; pre/post commitlint.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_capture_semantics.py`
- `tests/tooling/access_telemetry_c1_interchange/test_capture_commands.py`
- `docs/operations/c1-command-declaration-inspection-contract.md`
- `docs/operations/c1-observation-pin-inspection-contract.md`
- `_bmad-output/implementation-artifacts/tests/27-4-capture-command-verification/verification-receipts.zip`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-17.md`

## File List

- `tools/access_telemetry_c1_capture_semantics.py`
- `tests/tooling/access_telemetry_c1_interchange/test_capture_commands.py`
- `docs/operations/c1-command-declaration-inspection-contract.md`
- `docs/operations/c1-observation-pin-inspection-contract.md`
- `_bmad-output/implementation-artifacts/tests/27-4-capture-command-verification/verification-receipts.zip`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-17.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-10-12 | create-story | Plan strict C1.15 command declarations under explicit repository authorization. | Phase +0, cumulative +0; 347 -> 347 unittest methods. Exact full Verification command: 347 passed, zero failures/errors/skips in 56.455s; receipt /tmp/story-27-4-capture-verification-create-before.log. | Matched 1/1 against git diff --name-status 0281b30fb2618668efa9b8904afc75e18820eed8 plus git ls-files --others --exclude-standard; this spec only, no exclusions. |
| 2026-10-12 | dev-story | Add strict grammar/ordered per-Pod inspection, twenty regression methods, API documentation and retained receipts. | Phase +20, cumulative +20; 347 -> 367 unittest methods using exact full Verification command. Before: 347 passed in 49.397s, dev-before.log; after: 367 passed in 55.879s, dev-after.log; zero failures/errors/skips. Focused capture lane: 44 methods passed in 2.353s. Existing producer regression: 1 -> 1 method, phase +0/cumulative +0 using PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'pg2_runtime_control_plane_identity_test.py' -k 'index_child_bare_and_two_pods' -v; before/after passed in 32.856s/34.039s. | Matched 6/6 against git diff --name-status 0281b30fb2618668efa9b8904afc75e18820eed8 plus git ls-files --others --exclude-standard; six declared paths, no exclusions. |
| 2026-10-12 | code-review | Complete blind, edge-case and verification-gap layers; promote actual producer integration into discovery, strengthen argv-length/summary coverage, clarify Git proof limits and seal exact archive evidence. | Phase +2, cumulative +22; 367 -> 369 unittest methods with exact full Verification command; 369 passed in 66.015s, zero failures/errors/skips, final-full.log. Focused capture lane 44 -> 46 methods, phase +2/cumulative +22 relative to its 24-method baseline; 46 passed in 15.967s, final-focused.log. Existing producer regression remains 1 -> 1 method, phase +0/cumulative +0. | Matched 6/6 against git diff --name-status 0281b30fb2618668efa9b8904afc75e18820eed8 plus git ls-files --others --exclude-standard; six declared paths, no exclusions. |

## Development Evidence

Full/focused commands are in Verification. Exact logs use /tmp/story-27-4-capture-verification- prefix and are retained in the receipt ZIP. No external discovery delta.

The separate supporting producer-inspection.py runner executed one offline integration method in 13.071s: actual PowerShell producer packets for 1/2/3 Pods passed strict inspection with 6/9/12 commands, reversed Pod order, exact bytes and zero counted dependency attempts. This supporting runner is reported separately from repository discovery arithmetic. Exact packet identities: e4d91964421e63c9f74b9fe1cf2ae5f0b54300bb7e03b54b169cfae7a193ba25 (141132 bytes), 56398716083abecedf6c387d882c0a4cfcd01a06358d6ee911d9b155a13dc783 (146647 bytes), e7b3351b2f9577b087f4dc17a5d8882d7e8a637aa7634075ee20c3150e03baea (152162 bytes). All are dirty-development offline fixtures, granting no source eligibility or live credit.

| Matrix row | Executed covering methods in CaptureCommandTests | Result |
| --- | --- | --- |
| Complete | test_one_two_three_pods_in_every_observation_order; test_exact_retained_json_bytes_are_never_reserialized; test_result_snapshot_and_derived_command_values_are_frozen | Pass; counts, sequence and frozen bytes retained. |
| Wrong meaning | test_every_argument_including_later_pods_rehashed_substitution_refuses; test_wrong_cluster_namespace_selector_projection_and_container_refuse; test_cross_pod_version_metadata_and_alpha_refuse; test_both_fixed_probe_substitutions_and_exchange_refuse; test_rehashed_verb_option_addition_removal_split_and_option_reorder_refuse | Pass; old inspectors admit consistent hashes, strict API refuses. |
| Wrong sequence | test_context_selection_per_pod_and_recheck_reorders_with_valid_chronology_refuse; test_complete_pod_blocks_must_follow_observation_order; test_missing_surplus_and_duplicate_receipts_keep_structural_refusals | Pass; reordered complete receipts or invalid sets return no command inspection. |
| Invalid capture | test_hash_and_chronology_contradictions_keep_existing_refusals; test_blocked_pin_summary_and_observation_contradictions_keep_existing_refusals; test_snapshot_schema_neutral_markers_and_malformed_inputs_keep_refusals | Pass; inherited refusals preserved. |
| Compatibility | test_simplified_structural_fixture_and_legacy_pin_api_are_unchanged; test_neutral_session_evidence_directory_timeout_and_dirty_facts_grant_no_authority | Pass; legacy API/neutral behavior retained. |

All twenty methods passed without skips in the focused and full lanes. Counted barriers apply to every strict, legacy and structural API case; deliberate prebound/ambient barrier probes also pass. Existing observation API/result ASTs match baseline. Fifty-eight protected working-byte hashes and frozen intent match snapshots; exact protected baseline diff exits 0 in protected-check.log. Parent Story 27.4 remains in-progress, Production disabled and A41 open. Current owner inspection is unnecessary for this pure API; dispatch_owner_run_files remains unchanged and inspection-only.

## Cross-Tenant Negative Evidence

Surface: offline capture scope and Pod command attribution only; no live tenant routing or dependency change. test_wrong_cluster_namespace_selector_projection_and_container_refuse, test_cross_pod_version_metadata_and_alpha_refuse and test_every_argument_including_later_pods_rehashed_substitution_refuses deny foreign scope/Pod declarations under counted no-dependency barriers. Exact focused Verification command: 44 methods passed, zero failures/errors/skips. These are offline negative fixtures, not live tenant-isolation qualification.

## Final Reviewed Verification

All three review layers completed against SHA-256 a16c41ea90312eb4b00efb2eb268e00f17e6e6903ca6b2cd22581fd8471e54b5. Ten individual findings have triage rows; duplicate root causes were grouped only after each verdict. Every selected correction is implemented. The Code Map allegation is refuted by the explicit new test task and accurate fixture/barrier mapping; no deferred work or frozen intent edit.

Parent read the complete original diff and patch delta. Final focused command: 46 unittest methods passed in 15.967s; final affected command: 369 passed in 66.015s; zero failures/errors/skips, exit 0. Added review methods test_summary_array_reordering_retains_pod_and_receipt_order and CaptureCommandProducerTests.test_existing_offline_producer_packets_match_strict_inspection both executed and passed. Existing argv negative coverage now adds/removes every argument across all twelve three-Pod receipts. Every original matrix row retains passing coverage.

The receipt archive uses a closed named inventory. It retains exact original reviewed diff, each layer report, patched diff, complete baseline/development/final logs, frozen intent/protected working-byte hashes, actual producer packet bytes, a portable repository-root runner, source/spec snapshots and manifest entry hashes/lengths. The original runner's reproduced packet receipts remain separate from the newly discovered regression. No stale wildcard receipts are admitted.

Parent Story 27.4 remains in-progress; A41 remains open and Production disabled. Only this local inspection slice is complete. Integrated source/stream/custody/authority/registration/gate work and separately approved live operations remain outstanding.

Focused baseline mapping: create-before.log contains exactly 24 runner-emitted CaptureSemanticTests method rows, all passed. No command-test module existed then. focused-baseline-mapping.log retains those exact rows and explicitly maps the capture subset from full discovery to the later test_capture*.py command: 24 unchanged legacy methods + 22 new command methods = 46. This mapping supports the focused cumulative delta; full discovery arithmetic remains independently 347 + 22 = 369.

The workflow's default parent sprint transition is superseded by the invocation's explicit instruction to preserve Story 27.4 in-progress. The slice spec alone transitions to done; sprint/history bytes stay unchanged.

Final readiness initially refused because a blank line separated the appended code-review table row. Joining the canonical table rows corrected the record; the original failed receipt is retained as final-gates-first-attempt.log. No executable gate was changed or bypassed.

Final gates: python3 tools/check-story-file-scope.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-17 --changed-files-file /tmp/story-27-4-capture-verification-changed-files.txt exits 0, Story file scope validation passed. Same arguments to tools/check-story-review-readiness.py exit 0, Story review readiness validation passed. Its cumulative check is skipped at done; the earlier in-progress check and independent six-file baseline reconciliation provide that evidence. tools/check-tenant-isolation-evidence.py with the same arguments exits 0, No tenant-isolation surface changed; evidence check is a no-op. Focused offline scope denials did execute and pass. git diff --check and pre-commitlint pass.
