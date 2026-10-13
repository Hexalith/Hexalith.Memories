---
title: 'Story 27.4 C1.15 retained output inspection'
type: 'feature'
created: '2026-10-13'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '47187a8acdda52b56b110adee4067173872161ee'
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Exact C1.15 command declarations still permit correctly rehashed output receipts whose contents contradict the observations.

**Approach:** Add pure inspection of caller-supplied retained stdout/stderr bytes, binding every command receipt and deriving the declared observations from those outputs.

## Boundaries & Constraints

**Always:** Reuse strict command inspection and PG2 pins. Hash untrimmed bytes; trim only for producer-compatible parsing. Limit each stream to 1 MiB and capture plus all stream occurrences to 32 MiB. Check strict UTF-8, depth-30 JSON, duplicate decoded keys and raw/decoded credential safety throughout, including stderr. Support harmless extra fields/finite numbers. Return frozen consistency data and content-free refusals.

**Never:** Perform filesystem/process/network/configuration/clock access. Change existing APIs, deployed refusals, producers, registries, consumers, sprint/history or A41. Claim execution, provenance, custody, authority or gate acceptance. Commit, push or update dependencies. Separate owner inspection uses `dispatch_owner_run_files` without execution authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Refusal |
| --- | --- | --- | --- |
| Complete | One immutable pair per command, exact hashes and consistent contents | Frozen result retaining exact capture and stream bytes | No acceptance flags |
| Missing/drift | Missing, duplicate, surplus, reordered, mutable or hash-mismatched streams | No inspection | Content-free code |
| Contradiction | Rehashed foreign context/Pod/UID/image, runtime, metadata or alpha | No inspection | Content-free code |
| Unsafe | Invalid UTF-8, malformed/duplicate/deep JSON, escaped credentials, excess bytes | No inspection | No raw error context |
| Stable recheck | Reordered frames and harmless annotations/timestamps | Match stable Running identities | Changed identity refuses |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_capture_semantics.py` — reuse `inspect_c115_command_declarations`.
- `tools/verify-access-telemetry-c1.ps1` — authoritative parsing; read only.
- `tools/access-telemetry-c1-component-backend.ps1` — credential scanning; read only.
- `tests/tooling/access_telemetry_c1_interchange/test_capture_commands.py` — declarations/producer harness.

## Tasks & Acceptance

**Execution:**

- [x] `tools/access_telemetry_c1_capture_streams.py` — add frozen `C115CommandStreams(purpose, stdout, stderr)`, `C115StreamInspection` and `inspect_c115_retained_streams(snapshot, streams)`. Require an exact tuple in receipt order and built-in bytes. Retain snapshot/streams/Pod names/command count; omit raw content from representations.
- [x] `tools/access_telemetry_c1_capture_streams.py` — derive context, complete initial Running Pod identities/order, version, metadata and booleans; compare all observations/projection hashes. Parse successive `[metadata,status]` frames. Match rechecked Running identities by name, UID, app label, deletion, Ready and lifecycle/daprd names/readiness/raw images; ignore incidental metadata/non-Running identities. Support either scheduler spelling, refuse both; compare duplicate-free arrays using producer semantics. Permit safe nonempty stderr.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_capture_streams.py` — cover the matrix, 1/2/3 Pods/order, later-Pod/completeness negatives, rehashed contradictions, exact bytes/budgets, immutability and counted dependency barriers. Discover actual offline producer compatibility for framing/trimming, diagnostics, extra numbers and rechecks.
- [x] `docs/operations/c1-retained-output-inspection-contract.md` — document API/limits, refusals, compatibility and remaining acquisition/custody/registration.

**Acceptance Criteria:**

- Given rehashed contradictory outputs, when inspected, then declaration inspection succeeds and retained-output inspection refuses.
- Given consistent outputs, when inspected, then exact bytes/counts remain immutable without authority.
- Given the baseline, when tests and three review layers finish, then legacy behavior remains intact, 27.4 in-progress, Production disabled and A41 open.

## Implementation Notes

No intent gaps or irreversibles; four-file footprint. Live actions/publication need separate plans. Approved by the owner with “approve and continue” on 2026-10-13. This delegated implementation runs this spec within the existing Build workflow.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| BH1 overlapping fragments | medium | Parent reproduced inspection of an unmatched-quote diagnostic with an earlier harmless object followed by a credential pair or duplicate JSON keys. Patch inspection of every overlapping recognizable fragment. |
| BH2 malformed overlap | medium | Parent reproduced inspection for incomplete/malformed recognizable objects hidden by a diagnostic quote. Patch strict refusal for these observed fragments while preserving harmless literal prose. |
| BH3 standalone nonfinite | medium | Parent reproduced bare NaN, Infinity and 1e9999 stderr returning inspection. Patch bounded standalone-number inspection, retaining safe prose. |
| BH4 quoted prose | medium | Parent reproduced refusal of ordinary quoted backslash prose; the reviewer verified the unchanged producer accepts it. Patch token scanning to ignore non-JSON quoted prose after raw safety scanning. |
| BH5 integer configuration | medium | Parent reproduced a 700-digit extra integer changing verdict at Python digit limits 4300/640. Local ConvertFrom-Json accepts lexemes of at most 380 characters including minus and refuses longer integers. Patch deterministic producer-compatible lexical bound and boundary tests. |
| BH6 array-order wording | low | Unsorted rehashed declarations inspect by intentional exact-membership comparison without ambient culture. Producer sort order is not established. Directly clarify the contract that declared projection order is hash-bound but no locale normalization is independently proved; preserve the approved membership implementation. |
| BH7 empty credential pairs | medium | Parent reproduced inspection of name token with empty list/object value; reviewer verified producer refusal. Patch the distinct non-null non-string pair rule, retaining harmless empty direct credential fields. |
| BH8 surplus cardinality | medium | Code walks all surplus pair objects and allocates their purpose tuple before mismatch. Patch exact tuple cardinality immediately after the declaration prerequisite; test early refusal without traversing surplus contents. |
| BH9 affirmative alpha coverage | medium | Existing successful independent and actual-producer fixtures declare only false/false, allowing an affirmative-only rejection regression to escape. Patch true/true and false/true successful cases, including later Pods and actual producer output. |
| EC1 array nonfinite | medium | Parent reproduced [NaN] and [Infinity] stderr returning inspection because the fragment-start matcher omits those constants. Patch those observed candidate forms; same nonfinite-recognition family as BH3. |
| EC2 case-varied pairs | medium | Parent reproduced NAME/VALUE credential-pair bytes returning inspection, contrary to the producer case-insensitive property lookup. Patch case-insensitive pair-key matching and both-stream fixtures. |
| P1 diagnostic suffix scans | medium | Parent metered 8,192 diagnostic bytes: 4,238 candidate searches examined 8,401,806 suffix characters. Repeated quoted prose causes quadratic work within the accepted byte limit. Patch monotonic candidate traversal and deterministic work-budget coverage. |

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture*.py' -v`
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v`
- `git diff --check`; explicit slice-key file-scope/readiness checks; compare protected bytes with baseline.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_capture_streams.py`
- `tests/tooling/access_telemetry_c1_interchange/test_capture_streams.py`
- `docs/operations/c1-retained-output-inspection-contract.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-18.md`

## File List

- `tools/access_telemetry_c1_capture_streams.py`
- `tests/tooling/access_telemetry_c1_interchange/test_capture_streams.py`
- `docs/operations/c1-retained-output-inspection-contract.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-18.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-10-13 | create-story | Prepare additive retained-output consistency inspection. | Phase +0, cumulative +0; 369 -> 369 unittest methods. Full Verification command: 369 passed in 72.694s, exit 0, zero failures/errors/skips; /tmp/story-27-4-retained-streams-create-before.log. No test edits. | Matched 1/1: git diff --name-status 47187a8acdda52b56b110adee4067173872161ee plus git ls-files --others --exclude-standard; draft only, no exclusions. |
| 2026-10-13 | dev-story | Implement retained streams, independent fixtures and producer compatibility; parent corrects raw decoder exception contexts and strengthens all contradiction assertions. | Full Verification command: phase +31, cumulative +31; 369 -> 400 unittest methods; 400 passed in 87.606s, zero failures/errors/skips, exit 0, /tmp/story-27-4-retained-streams-dev-full.log. Capture Verification command: phase +31, cumulative +31; mapped baseline 46 -> 77 methods; 77 passed in 40.620s, exit 0, /tmp/story-27-4-retained-streams-dev-capture.log. Context correction strengthens existing methods, phase +0. | Matched 4/4 against git diff --name-status 47187a8acdda52b56b110adee4067173872161ee plus git ls-files --others --exclude-standard; four declared files, no exclusions. |
| 2026-10-13 | code-review | Complete all three review layers and all twelve selected corrections; add eight regression methods, including producer affirmative alpha/integer/prose compatibility and deterministic work checks. | Full Verification command: phase +8, cumulative +39; 400 -> 408 unittest methods; 408 passed in 107.549s, exit 0, zero failures/errors/skips, /tmp/story-27-4-retained-streams-final-full.log. Capture Verification command: phase +8, cumulative +39; 77 -> 85 methods; 85 passed in 49.142s, exit 0, zero failures/errors/skips, /tmp/story-27-4-retained-streams-final-capture.log. New focused file: 31 -> 39 methods; 39 passed in 40.243s with -p test_capture_streams.py, /tmp/story-27-4-retained-streams-review-focused.log. | Matched 4/4 against git diff --name-status 47187a8acdda52b56b110adee4067173872161ee plus git ls-files --others --exclude-standard; four declared files, no exclusions. |

## Development Verification

Parent read the complete unified diff and subsequent development delta. All four tasks are implemented. Every matrix row ran in the 77-method capture lane: Complete uses one/two/three Pods and exact retention; Missing/drift uses closed-type, missing/duplicate/order and each-stream hash negatives; Contradiction covers foreign context/runtime, every initial Pod, later metadata and alpha; Unsafe covers both-stream UTF-8/JSON/depth/credential/budget refusals with null cause/context; Stable recheck covers incidental metadata/reordering and every identity/image change. The actual unchanged offline producer compatibility method ran and passed under counted dependency barriers after fixture I/O. Full affected lane passed 400 methods. Remaining formal reviews are the final acceptance step.

Focused baseline mapping: create-before.log contains exactly 46 passed runner-emitted method rows from test_capture_semantics and test_capture_commands, the complete prior test_capture*.py scope. The new module contributes 31 discovered methods; the mapped 46 + 31 = 77 is separate from full discovery 369 + 31 = 400. No external discovery delta.

## Cross-Tenant Negative Evidence

Affected surface: offline scope/Pod attribution only. test_rehashed_foreign_context_and_runtime_outputs_refuse, test_every_initial_pod_uid_label_ready_deletion_and_images_are_checked and test_metadata_foreign_id_runtime_scheduler_actor_and_features_refuse_for_later_pods all ran and passed in the exact capture Verification command (77 methods, zero failures/errors/skips). They deny foreign supplied scope/Pod/app observations before any dependency attempt. These fixtures establish no live tenant-isolation qualification. Existing tenant routing and deployed consumers remain unchanged.

## Final Reviewed Verification

All three review layers finished against reviewed diff SHA-256 `337d554004680eb2cacded981ff30b2b8f6f3c2279d19110bf9401553f24c3d2`. Twelve individual triage rows were recorded before grouping. Every selected correction is implemented; no intent gap, spec re-derivation or deferred finding remains. Parent read the complete original diff, development delta and 409-line review patch. Every original matrix row remains covered by executed passing tests.

Final exact Verification commands passed 85 capture methods in 49.142s and 408 full affected methods in 107.549s, both exit 0, zero failures/errors/skips. Independent regression additions cover overlapping fragments, nonfinite documents, diagnostic prose, deterministic integer bounds, credential pairs, immediate surplus refusal, monotonic candidate work and all permitted alpha pairs. The actual unchanged offline producer method passed for one/two/three Pods and false/false, false/true and true/true alpha pairs. Parent independently reproduced each reported parser/credential/nonfinite refusal with null cause/context and safe quoted-prose success; receipt: `/tmp/story-27-4-retained-streams-parent-review-repro.log`.

The approved frozen block and all 6,194 pre-existing tracked files remain byte-identical. Scope is exactly the four declared new files. Parent 27.4 remains in-progress, A41 stays open, and Production remains disabled. The generic workflow parent review transition and automatic local commit are superseded by the approved spec explicitly forbidding sprint/history changes and commits. Only this offline inspection slice is done; actual acquisition/custody, source eligibility, deployed registration and live gate/owner acceptance remain outstanding.

Private readback-checked receipts: `/home/administrator/.local/state/hexalith/memories/access-telemetry/retained-streams-20261013-29845f17fe3e`. The 0700 directory and 0600 files retain the original approved intent, protected-byte baseline, exact baseline/development/final logs, original reviewed diff/sources, review findings, patched diff, final source/spec snapshots and SHA-256/length manifests. This is local evidence retention without immutability or live gate credit.

Pre-review readiness check passed at in-progress with all four explicit changed-file arguments: final line `Story review readiness validation passed.` Final slice-key gates are recorded below.

Exact review-patch focused command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p test_capture_streams.py -v`; 39 methods passed in 40.243s, exit 0, zero failures/errors/skips.

Final file-scope command: `python3 tools/check-story-file-scope.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-18 --changed-files-file /tmp/story-27-4-retained-streams-changed-files.txt`; exit 0, `Story file scope validation passed.` Final readiness command: `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-18 --changed-files-file /tmp/story-27-4-retained-streams-changed-files.txt`; exit 0, `Story review readiness validation passed.` Cumulative comparison is skipped at done; the earlier in-progress gate and independent four-file baseline reconciliation provide the comparison evidence. `python3 tools/check-tenant-isolation-evidence.py` with the same arguments exits 0, `No tenant-isolation surface changed; evidence check is a no-op.` The explicit offline foreign-scope/Pod negatives did execute and pass in the final 85-method capture lane. `git diff --check` exits 0.
