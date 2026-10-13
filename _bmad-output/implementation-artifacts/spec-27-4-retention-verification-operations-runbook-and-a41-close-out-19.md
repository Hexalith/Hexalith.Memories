---
title: 'Story 27.4 C1.15 exact stream retention and private readback'
type: 'feature'
created: '2026-10-13'
status: 'ready-for-dev'
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

- [ ] `tools/verify-access-telemetry-c1.ps1` — write validated pairs and closed manifest before publishing the packet; refuse unsafe custody or incomplete retention with no observed inspectable packet.
- [ ] `tools/access_telemetry_c1_retained_files.py` — add `inspect_c115_retained_files(packet_path, *, custody_root)`: safely read the private packet and exact sidecar, validate the manifest/pair set and final readback, then call the pure inspector. Return inspection only.
- [ ] `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py` and `tests/tooling/access_telemetry_c1_interchange/test_capture_retained_files.py` — cover the matrix, limits, races and unchanged legacy behavior with fake kubectl and offline dependency barriers.
- [ ] `docs/operations/c1-retained-output-inspection-contract.md` and `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md` — document exact layout, private handling, reader API, refusals and proof limits.

**Acceptance Criteria:**

- Given a safe fake PG2 C1.15 run, when read back, then exact bytes reproduce each receipt and observation without adding v2 fields.
- Given any missing, unsafe, changed or substituted input, when offline readback runs, then it returns no inspection and performs no target call.
- Given protected consumers and status files, when tests pass, then deployed refusals, Production disablement and A41 remain unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

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
