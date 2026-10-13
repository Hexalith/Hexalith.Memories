---
title: 'Story 27.4 C1.15 exact stream retention and private readback'
type: 'feature'
created: '2026-10-13'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'ec6b6c93787494cd0a1521fd3b542cb7d372837f'
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The PG2 C1.15 producer hashes kubectl stdout and stderr, then discards their exact bytes. The retained-output inspector has no producer-owned files to inspect.

**Approach:** Retain safe streams in a private, packet-bound sidecar and read them through `inspect_c115_retained_streams`.

## Boundaries & Constraints

**Always:** Apply only to PG2 C1.15. Preserve the closed v2 packet, neutral gate fields and PG1/C1.16 behavior. Retain untrimmed bytes, including empty stderr. Require an owner-only evidence root and a fresh per-capture sidecar named `<packet filename>.streams/`, containing only `manifest.json` and zero-padded indexed `.stdout`/`.stderr` pairs. The closed manifest binds packet SHA-256, receipt order and stream hashes. Keep 1 MiB per stream and 32 MiB aggregate. Writer and reader require exclusive creation, regular single-link files, no symlinks, stable identities, bounded reads and exact-byte readback. Publish an observed packet only with a complete sidecar.

**Never:** Contact a target during offline reading; infer execution, provenance or authority from matching bytes; print or stage raw streams; alter deployed refusals, Production state, story status or A41; commit, push or update dependencies.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Complete | Safe fake outputs for 1–3 Pods | Neutral v2 packet; private sidecar; frozen inspection | No gate claim |
| Incomplete | Missing, surplus or cross-packet files | No inspection | Content-free refusal |
| Tampered | Bytes/hash drift or file race | No inspection | No stale result |
| Unsafe | Secret, invalid UTF-8, size or custody failure | No observed inspectable pair | Bounded refusal |
| Legacy | PG1 or C1.16 invocation | Existing packet/output behavior | No new sidecar requirement |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1:166,558,1125` — transient streams, packet writer and publication; retain validated bytes before disposal.
- `tools/access_telemetry_c1_capture_streams.py:407` — existing pure semantic/hash inspector; keep it free of I/O.
- `tools/access_telemetry_c1_owner_custody.py:30` and `tools/access_telemetry_c1_owner_dispatch.py:142` — private, non-following read pattern; preserve owner semantics.

## Tasks & Acceptance

**Execution:**

- [x] `tools/verify-access-telemetry-c1.ps1` — write validated pairs and closed manifest before publishing the packet; refuse unsafe custody or incomplete retention with no observed inspectable packet.
- [x] `tools/access_telemetry_c1_retained_files.py` — add `inspect_c115_retained_files(packet_path, *, custody_root)`: safely read the private packet and exact sidecar, validate the manifest/pair set and final readback, then call the pure inspector. Return inspection only.
- [x] `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py` and `tests/tooling/access_telemetry_c1_interchange/test_capture_retained_files.py` — cover the matrix, limits, races and unchanged legacy behavior with fake kubectl and offline dependency barriers.
- [x] `docs/operations/c1-retained-output-inspection-contract.md` and `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md` — document exact layout, private handling, reader API, refusals and proof limits.

**Acceptance Criteria:**

- Given a safe fake PG2 C1.15 run, when read back, then exact bytes reproduce each receipt and observation without adding v2 fields.
- Given any missing, unsafe, changed or substituted input, when offline readback runs, then it returns no inspection and performs no target call.
- Given protected consumers and status files, when tests pass, then deployed refusals, Production disablement and A41 remain unchanged.

## Implementation Notes

Only PG2 C1.15 retains validated original target streams. The embedded Linux
writer holds descriptor-relative custody, creates private artifacts exclusively,
validates receipt hashes and the closed manifest, checks exact readback, and
publishes the observed packet only after the complete sidecar exists. It applies
strict raw/decoded credential and JSON safety before retention. The Python file
reader reuses the unchanged owner descriptor checks and pure semantic inspector.
PG1/C1.16 and the existing pure/owner APIs retain their behavior.

The writer requires Linux `statx`; the reader requires POSIX custody operations.
OS resource failures refuse without inspection, including descriptor exhaustion
with unusually large artifact sets. Private safe orphan sidecars require the
contract's owner handling procedure. Matching bytes confer no execution,
provenance, freshness, authority or gate credit.

No target was contacted outside fake transports. No raw stream was printed or
staged. Parent story/status, deployment refusals, Production disablement and A41
were preserved; the 53-file protection receipt is
`/tmp/story-27-4-streams-protected-sha256.json`. Initial mixed-source or incomplete
producer runs are excluded from verification; the final complete runs below are
authoritative for this slice. Changes remain uncommitted under the frozen scope.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence / decision |
| :--- | :--- | :--- | :--- |
| B1 | high | patch | SafeScan catches recognizable-fragment JsonException and continues; reproduced malformed named credential JSON is written in an observed sidecar. Refuse that existing failed parse branch before retaining bytes. |
| B2 | medium | patch | SafeTree enumerates object properties without checking decoded-key uniqueness; reproduced duplicate diagnostics are published and then refused by the pure reader. Add the missing uniqueness check. |
| B3 | medium | patch | The existing MaxDepth=30 parse throws for a 31-level fragment, but the catch continues. The writer publishes bytes the reader refuses; the failed-parse correction covers this outcome. |
| B4 | high | patch | SafeTree has no numeric-validation branch and failed NaN parses are ignored. Reproduced overflow and credential-bearing nonfinite fragments are published; validate existing numeric nodes and refuse failed candidate parses. |
| B5 | medium | patch | A numeric JsonElement can retain a 381-character integer lexeme; the pure reader caps it at 380. Add that existing numeric-node limit before retention. |
| B6 | low | reject | Per-artifact ancestor chains can exceed an OS descriptor limit for a 30-Pod fixture. The refusal is bounded and returns no inspection. This uncommon replica/resource combination does not justify a new shared-descriptor implementation in the bounded 1–3 Pod slice; unsupported resource availability remains fail-closed. |
| B7 | medium | patch | The writer permits an evidence root whose fresh packet path exceeds the unchanged 512-character canonical reader limit. Reproduced publication succeeds and readback refuses; check the generated-path budget before target calls. |
| B8 | medium | patch | Writer disposal is outside publication error handling. A demonstrated cleanup failure can escape unsanitized after creating a packet. Make cleanup best-effort across all owned descriptors and handle its refusal within the bounded publication policy. |
| B9 | false | reject | The frozen slice and both contracts promise current exact-byte readback and private custody, not crash durability, certified retention or power-loss recovery. Loss of directory names would make the reader refuse; no documented crash-durability guarantee is violated. |
| B10 | low | patch | The new contract explicitly permits private safe orphan sidecars but gives no handling steps. Add direct operational instructions to distinguish incomplete orphans from complete packet/sidecar pairs and preserve current status authority. |
| E1 | high | patch | Reproduced incomplete named credential fragments reach the same ignored JsonException branch as B1. Their nonempty credential values are retained; refuse before persistence. |
| E2 | high | patch | Reproduced U+001C–U+001F separators are whitespace for Python credential patterns but not the copied .NET patterns. Normalize those characters for safety scanning only, preserving original bytes. |
| E3 | medium | patch | Reproduced safe duplicate/nonfinite diagnostic JSON is published but cannot be inspected. Existing parse/property/numeric checks must match the pure reader refusal boundaries. |
| V1 | medium | patch | Verified regression gap: no test forces publication failure after creating the packet. Add end-state assertions that rollback removes the owned packet and preserves a substituted name. |
| V2 | medium | patch | Verified regression gap: current producer budget tests exceed 32 MiB in streams alone, before packet accounting. Add a case that fits collection but exceeds publication only when packet bytes are charged. |

## Design Notes

`manifest.json` has exactly `schemaVersion`, `captureSha256` and `receipts`. Each receipt has exactly `index`, `purpose`, `stdoutSha256` and `stderrSha256`; `index` is zero-based packet order. Stream filenames are four-digit indexes such as `0000.stdout` and `0000.stderr`. Hash the packet's original bytes and each stream's original bytes. Reject extra files. The 32 MiB inspector budget charges packet plus all stream occurrences; the manifest has its own 1 MiB cap.

## Verification

**Commands:**

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'pg2_runtime_control_plane_identity_test.py' -v` — fake producer cases pass.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v` — affected offline cases pass with no skips.
- `git diff --check` and story-key file-scope/readiness checks — declared files only; protected bytes unchanged.

## File Scope

**Allowed to modify:**

- `tools/verify-access-telemetry-c1.ps1`
- `tools/access_telemetry_c1_retained_files.py`
- `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py`
- `tests/tooling/access_telemetry_c1_interchange/test_capture_retained_files.py`
- `docs/operations/c1-retained-output-inspection-contract.md`
- `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-19.md`

## File List

- `tools/verify-access-telemetry-c1.ps1`
- `tools/access_telemetry_c1_retained_files.py`
- `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py`
- `tests/tooling/access_telemetry_c1_interchange/test_capture_retained_files.py`
- `docs/operations/c1-retained-output-inspection-contract.md`
- `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-19.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-13 | create-story | Adopt the phase ledger for this approved bounded slice; owner: Administrator. Preserve existing baseline revision and frozen intent. | Adoption phase +0, cumulative +0. Baseline: 26 producer unittest methods and 408 interchange unittest methods. Exact discovery command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1', pattern='pg2_runtime_control_plane_identity_test.py').countTestCases()); print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange').countTestCases())"`. No historical delta reconstructed. | Matched 1/1 against `git diff --name-status ec6b6c93787494cd0a1521fd3b542cb7d372837f` plus `git ls-files --others --exclude-standard`: this specification only; no exclusions. |
| 2026-10-13 | dev-story | Implement the private exact-byte writer, reader, regression matrix and operations contracts. Focused reader, producer roundtrip/legacy, credential-safety and budget cases passed. Full specified lanes are running; no pending execution result is claimed passed. | Producer phase +8, cumulative +8: 26 -> 34 unittest methods. Interchange phase +20, cumulative +20: 408 -> 428 unittest methods. New reader scope: 0 -> 20 methods. Exact discovery command is the create-story command above, rerun after implementation. Required execution logs pending: `/tmp/story-27-4-retention-producer-final.log` and `/tmp/story-27-4-retention-interchange-final.log`; owner: current build agent; consequence: final verification/completion held; reopen trigger: both commands finish successfully with zero skips. | Matched 7/7 against `git diff --name-status ec6b6c93787494cd0a1521fd3b542cb7d372837f` plus `git ls-files --others --exclude-standard`; the seven File List paths, no exclusions. |
| 2026-10-13 | code-review | Finish blind, edge-case and verification-gap review; record all 15 findings, apply all 13 selected corrections and reject two documented claims. Add five producer regression methods and verify the final patched tree. Nothing deferred. | Producer phase +5, cumulative +13: 34 -> 39 unittest methods; 39 passed in 684.569s, exit 0, no failures/errors/skips. Command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'pg2_runtime_control_plane_identity_test.py' -v > /tmp/story-27-4-retention-root-final-producer.log 2>&1`. Interchange phase +0, cumulative +20: 428 -> 428 unittest methods; 428 passed in 106.448s, exit 0, no failures/errors/skips. Command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v > /tmp/story-27-4-retention-root-final-interchange.log 2>&1`. New reader scope unchanged at 20 methods, all executed in the interchange lane. Exact create-story discovery command rerun: 39 and 428. | Matched 7/7 against `git diff --name-status ec6b6c93787494cd0a1521fd3b542cb7d372837f` plus `git ls-files --others --exclude-standard`; the exact seven File List paths, no exclusions. Frozen intent preserved and all 53 protected hashes unchanged. |

## Final Verification Record

- Producer command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'pg2_runtime_control_plane_identity_test.py' -v > /tmp/story-27-4-retention-root-final-producer.log 2>&1` — 39 methods passed, 684.569s, exit 0; zero failures/errors/skips.
- Offline command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v > /tmp/story-27-4-retention-root-final-interchange.log 2>&1` — 428 methods passed, 106.448s, exit 0; zero failures/errors/skips.
- Discovery command in the adoption row rerun after review: 39 producer and 428 interchange methods. Producer baseline 26 + development 8 + review 5 = 39. Interchange baseline 408 + development 20 + review 0 = 428.
- All five frozen matrix rows are covered by executed passing tests: exact one/two/three-Pod and private-sidecar roundtrips; missing/surplus/cross-packet inputs; byte/name/permission and coarse-metadata races; credential/UTF-8/size/custody refusals; PG1/C1.16 compatibility.
- All 15 review rows were triaged individually. Safety parsing/whitespace, generated-path limits, bounded cleanup, owned-packet/substituted-name rollback, packet-inclusive aggregate accounting and orphan handling corrections are complete. No finding was deferred.
- `git diff --check` — exit 0. Story-key file-scope/readiness gates cover all seven declared paths. Parent story/status and A41 receive no completion transition or gate credit.

Final gate commands (exit 0):

- `python3 tools/check-story-file-scope.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-19 --changed-files-file /tmp/story-27-4-retention-changed-files.txt` — `Story file scope validation passed.`
- `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-19 --changed-files-file /tmp/story-27-4-retention-changed-files.txt` — `Story review readiness validation passed.`

The rendered workflow's transient `in-review` spelling is not accepted by the
repository readiness validator; its final `done` spelling is. The final bounded
spec alone is done. Parent sprint/story status is preserved, and no commit,
push, dependency update, live gate pass or A41 transition was performed.
