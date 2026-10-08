---
title: 'PG2 C1.15 offline declared-observation pin inspection'
type: 'feature'
created: '2026-10-08'
status: 'done'
route: 'dispatch'
baseline_commit: '906bc07ad6a8e4912a7222d9d097da434148266a'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/docs/operations/access-telemetry-c1-pg2-c1-15-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The completed offline I2 inspector compares retained source bytes. I1's capture parser checks structural consistency but admits internally consistent declarations of different profile/workload/target/runtime/image values. Add the next smallest fully specified offline prerequisite: compare declared C1.15 observation values with the existing published PG2 pins. The user's 2026-10-08 instruction authorizes selection, implementation, review and verification in a separate spec.

## Boundaries & Constraints

**Always:** Reuse unchanged `JsonSnapshot`, `parse_capture`, and bounded refusal helpers. Take one explicit immutable snapshot; return a frozen inspection retaining that snapshot and Pod-derived count/names in input order. Require `producerStatus: observed`. Compare the published C1.15 profile ID/identity/hash, workload ID/hash, five target fields, each Pod's runtime and approved image digest exactly, without normalization. Support both approved OCI index and authenticated child together; inherit existing raw image-prefix syntax and digest suffix agreement. Document source-cleanliness/byte checks as separate I2 work: an observed dirty-development capture can be inspected but remains ineligible. Preserve every existing file except an append to the prerequisite task ledger; preserve its entire current prefix and all current workspace changes.

**Never:** Add filesystem/process/network/clock/configuration reads, collectors, accepting entries, dependency changes, accepted artifacts, authority/session integration or consumer dispatch. No repository-prefix allowlist, new time/role/custody/cleanup policy, command grammar or output replay proof. No live operations, Git mutation, story/sprint/history transition, A41 closure or Production enablement. No submodule edits. This spec is a standalone preparation, not a new registered epic story.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected result | Refusal |
| --- | --- | --- | --- |
| Published pins | Neutral observed v2 fixture; approved index/child or supported prefixes | Frozen inspection, original exact bytes, count/names derived from Pods | No authority or acceptance result |
| Pin substitution | Self-consistent different profile/workload/target/runtime/digest; one bad Pod among valid ones | Bounded content-free refusal | No repair, trimming or case folding |
| Existing invariants | Malformed schema, blocked/empty capture, summary/count/image-suffix contradiction | Refuse through reused reader or observed-only boundary | No fabricated observations |
| Proof limits | Valid observed dirty-development fixture and simplified structural command ledger | Declared pins inspectable only | Source eligibility, execution, registered grammar and custody remain unproved |
| Isolation | Every positive and negative API call | Zero filesystem/process/network/ambient/clock calls | No target or output mutation |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_interchange.py:parse_capture` — already validates closed fields, neutral markers, hashes/receipt shape, Pod uniqueness, summaries, counts, scheduler/actor/alpha semantics and image syntax/suffix. It deliberately lacks immutable observation pins; leave unchanged.
- `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md:Invocation and identity` — authoritative literal profile/workload/target/runtime/index/child table. `tools/verify-access-telemetry-c1.ps1` lines 41–46 and 777–804 independently enforce target/runtime/image values. Keep producer unchanged.
- `tests/tooling/access_telemetry_c1_interchange/test_artifact_readers.py:capture_fixture, fixture_ps_bytes, snapshot` — independent structural fixture and encoding; reuse to build self-consistent pin substitutions. Its simplified commands intentionally do not prove registered grammar.
- `tools/access_telemetry_c1_producer_bindings.py:inspect_sources, lookup_deployed_binding` — completed offline I2 preparation and unconditional deployed refusal; preserve exact bytes.
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md` — incomplete I2–I6/P1–P7; append only scoped assessment/result, never mark integrated tasks complete.

## Tasks & Acceptance

- [x] `tools/access_telemetry_c1_capture_semantics.py` — add `inspect_c115_observation_pins(snapshot)` and frozen `C115ObservationInspection`; enforce only the existing published pins after reused structural checks.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_capture_semantics.py` — independent mixed-image/prefix positives, every pin substitution with structural success first, per-Pod negatives, exact comparison, existing structural refusals, dirty inspection, immutable output, and denial barriers around every API case.
- [x] `docs/operations/c1-observation-pin-inspection-contract.md` — document actual API, literal pins/proof limits, current I2–I6/P1–P7 reassessment and reproducible offline verification.
- [x] `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md` — append bounded preparation and assessment with executed results; retain all existing text/holds.

**Acceptance Criteria:**
- Given a structurally valid current-pin capture, when inspected, then exact retained bytes and input Pod order remain intact, declared count/names are independently derived, and index/child coexist without requiring a shared raw image representation.
- Given a self-consistent changed pin, when inspected, then every profile/workload/target/runtime/image substitution refuses, including a later bad Pod; the unchanged structural parser first admits each semantic-negative fixture.
- Given malformed/contradictory or blocked evidence, when inspected, then no inspection is returned; dirty observed evidence remains explicitly inspection-only and source eligibility still refuses independently.
- Given any API case, when executed under denial barriers, then no prohibited dependency runs and neither an accepted artifact nor an execution handle is returned.
- Given final review/verification, when baseline receipts are compared, then all existing working-file hashes outside the ledger match, its prior prefix matches, HEAD/root gitlinks and Story 27.4/A41/Production/sprint/history bytes remain unchanged, and focused/full interchange tests pass without skips.

## Design Notes

No intent gaps or irreversible action remain in this bounded slice. Footprint: three new implementation/test/document files, this separate spec, and one append-only ledger update; no production caller. Fixed constants belong to the gate-specific library, avoiding ambient imports from operational verifiers. Structural snapshots remain untrusted declarations: exact pins cannot authenticate real cluster/runtime execution, selection completeness, stream outputs or image provenance.

## Implementation Notes

Implemented the four task paths. No existing production caller changes. The parent read the complete scoped source/test/document diff and ledger append, checked the published constants against the producer, and audited all five matrix rows against the executed twenty-test log (`parent-matrix-audit.json`). Existing profile-ID and inconsistent per-Pod runtime refusals remain structural; self-consistent semantic negatives establish structural admission first.

Before independent review, a parent in-memory mutation returning constant Pod count two survived all nineteen initial focused tests. The implementation agent added single-Pod positives for each original selected Pod; focused verification now passes twenty tests. No source behavior or frozen intent changed.


Review corrections completed in the new test/document paths only; the seventy-line inspector source is unchanged from independent review. Added counted detection of prebound clock/environment/filesystem aliases and additional filesystem access/write/removal/rename operations, fullwidth Unicode substitutions, a three-Pod positive and final-Pod image refusal. All four reproduced mutation gaps now fail their targeted checks (`final-mutation-probes.json`), without editing source bytes. Intent, public API and operational holds remain unchanged.

The supporting [verification receipt archive](tests/pg2-c1-15-offline-observation-pins/verification-receipts.zip) retains nineteen bounded local verification files, including the full original working-byte manifest, ledger prefix, checker, final tests, source hashes, mutation probes and scoped final diff. This is software verification evidence, not C1 acceptance or authenticated custody/authority.

## Spec Change Log

## Review Triage Log

All three independent layers reported before triage. Edge-case review and verification-gap review returned no findings; blind review returned seven individually assessed findings. The parent reproduced BH1–BH4 against all twenty focused tests without repository edits (`review-reproductions.json`). Each mutation survived. These findings strengthen evidence of the existing contract, rather than add policy or production behavior.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH1 — pre-bound clock alias bypasses denial patches | medium | A clock alias captured before setUp executed uncounted and all twenty tests passed. Future pure-inspector regressions could violate the advertised no-clock contract unnoticed. Patch test barriers to cover bound dependency aliases and verify detection. |
| BH2 — unbarred filesystem operations | medium | An injected os.access('/tmp', 0) executed while all twenty tests and zero-call assertions passed. Patch the missing access/descriptor/removal/rename barriers and representative detection coverage. |
| BH3 — Unicode normalization gap | medium | NFKC comparison passed all twenty tests and admitted a structurally valid fullwidth profile identity. Patch a self-consistent Unicode substitution with structural admission before exact refusal. |
| BH4 — count capped at two | medium | Returning min(actual_count, 2) passed all twenty tests. Single-Pod positives close the prior constant-two gap but do not prove larger counts. Patch a three-Pod positive and final-Pod image refusal. |
| BH5 — custody-label wording | low | The fixture has a neutral session label, source commit and evidence-directory argument; it has no custody field or approved custody evidence. Correct the test comment and contract to identify the actual arguments. |
| BH6 — temporary preservation evidence | low | Full working-byte baseline, prefix and executed logs currently exist only under /tmp. Their deletion would prevent replay of the recorded preservation comparison. Retain the offline receipt set as a supporting verification archive and record its content identities; this evidence grants no acceptance. |
| BH7 — preservation command omitted | low | The published recipe has tests and whitespace checks but omits the existing recheck-preservation.py invocation and required baseline files. Add its exact command, inputs and expected result, including durable archive extraction. |

All seven findings route separately to patch: their demonstrated outcomes have direct test/document/evidence corrections, no public API and no new operational state. Nothing is deferred and no spec loopback or frozen-intent change is required. The supporting receipt archive is part of verification of this same offline goal.

## Verification

Run from repository root with `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0`: first `python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture_semantics.py' -v`, then the same command with `-p 'test_*.py'`. Require zero failures/errors/skips. Read the complete scoped diff and audit each matrix/AC against executed cases. Run `git diff --check` and compare the working-file manifest and ledger-prefix receipt in `/tmp/pg2-c1-i4-observations-41meebiy/baseline.json`. Record commands/counts and independent review triage after execution.


Executed final parent verification from the repository root on 2026-10-08:

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture_semantics.py' -v`: exit 0; **24 tests passed**, zero failures/errors/skips (0.543 seconds).
- The same command with `-p 'test_*.py'`: exit 0; **219 tests passed**, zero failures/errors/skips (44.327 seconds), including all 195 preexisting interchange tests. Logs and exact argv/environment/elapsed/source identities are retained in the archive.
- All five matrix rows were re-audited against executed passing cases (`final-matrix-audit.json`). Structural-success-first covers every new semantic-negative fixture; existing parser-only refusals remain separately checked.
- Final mutation probes detect the previously surviving prebound clock call, os.access call, NFKC comparison and count capped at two. Their deliberate failures were checked with actual test setup/cleanup, avoiding unittest traceback rendering inside filesystem denial barriers; no source file was modified by probes.
- All three independent layers returned. Seven blind findings were individually triaged and addressed; edge-case review reported no findings and verification-gap review no gaps. No finding was deferred.
- Preservation recheck passed before archive creation and again after extracting the archive into a fresh external temporary directory using the exact documented recipe: **6,137 existing nonledger hashes unchanged**, exact **21,169-byte prior ledger prefix**, unchanged HEAD/root gitlinks, no unexpected changed path. The ledger additions retain all earlier evidence verbatim. The staged index remains empty.
- `git diff --check` passed. New Python files use LF; Markdown uses CRLF; final newlines and whitespace checks pass. The archive validates its members against exact lengths/SHA-256 and passes ZIP integrity checks.

Durable archive identity:

- Path: `tests/pg2-c1-15-offline-observation-pins/verification-receipts.zip`.
- Exact length: **281,633 bytes**, nineteen members.
- SHA-256: `8212ced57c9b4b510278310d77a9a41323f190325e44046bbc61574d6a3489a6`.
- `receipt-index.json` SHA-256: `66f5271647d199a0a9555e58b75490c1c81e3c46d683b73fd24dd1bfd6afb8dc`.

Final source identities:

| Path | SHA-256 |
| --- | --- |
| `tools/access_telemetry_c1_capture_semantics.py` | `8688ede229bd35bc28bcf9f762ddef89fd84712c020d421692fcf14d739300fd` |
| `tests/tooling/access_telemetry_c1_interchange/test_capture_semantics.py` | `87df102ff75ba1bf7f20f279d3e31946507c08252d710e996b9564eef36ac5f6` |
| `docs/operations/c1-observation-pin-inspection-contract.md` | `eeac049c5d890359b993c20283ff083693a7c39b3d600ef2f66bb5990fbb6e8c` |
| `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md` | `f78f3a9712dcb08f250d656aeff92c0ad19fb4f6aee5f1c813193f4fed59c498` |

The tested source-hash record precedes the final documentation-only ledger append; `final-source-hashes.json` records that final append separately. The archive excludes this evolving spec and itself to avoid hash cycles. Original local receipts remain in `/tmp/pg2-c1-i4-observations-41meebiy/`; the archive preserves the reproduction inputs durably.

This separately tracked offline prerequisite is complete. Integrated I2–I6/P1–P7 remain incomplete, Story 27.4 stays pending, A41 stays open and Production stays disabled. No live operation, accepting entry, accepted artifact, dependency change, Git mutation, commit or push was performed.
