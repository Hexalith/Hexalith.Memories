---
title: 'Story 27.4 strict C1 predecessor consumption'
type: 'feature'
created: '2026-10-10'
status: 'draft'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'f298a2071ae19e4cd8eae465c7f86397a2241560'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/schemas.md'
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/producer-bindings.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 consumers still accept an unversioned C1 predecessor with structural gate fields and reviewer labels. That path can report a pass or reach a live launcher without the authenticated P1–P7 interchange selected by the owner. The Platform response requested in issue #57 has not arrived, so an accepting v2 path cannot yet be proved.

**Approach:** Make every 27.4 C1 consumer reject legacy and incomplete v2 authorization before target or other dependency access. Retain historical inspection separately. Wire an accepting v2 path only after its authenticated verifier and approved operational inputs exist; this slice records the refusal boundary and its coverage.

## Boundaries & Constraints

**Always:** Apply one strict authorization boundary to the C2–C4 producer launchers, offline checkpoint, terminal bundle, close-out preflight and CLI input routes. Require `hexalith.access-telemetry.c1.predecessor/v2`, exact retained references, current authority and session status, and the approved P7 time limits for any future acceptance. Keep PG-ONPREM-2 profile and workload hashes, disabled Production writes, pending C1–C6, and open A41.

**Never:** Convert v2 into legacy fields, infer approval from `status: passed`, labels, fixture readers, local paths or issue #57. Do not add an accepting registry/provider stub, run live producers, contact a target, mutate A41, or advance the whole-story sprint status. Do not change the earlier frozen 27.4 handoff.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Legacy | Unversioned `gates` or `successors`, including 25 claimed passes | No authorization or checkpoint pass | Explicit version refusal before dependency access |
| Incomplete v2 | Exact v2 shape without authenticated I3–I5 verdict/current status | No authorization or terminal pass | Explicit unavailable-authority refusal before dependency access |
| Bypass attempt | `--input`, offline/terminal path or freshness opt-out | Same strict result | No downgrade or alternate accepting route |

</frozen-after-approval>

## Code Map

- `tools/verify_access_telemetry_lifecycle.py:2642` — `_validate_predecessor` currently accepts legacy `gates`/`successors`; replace its authorizing use with a shared strict boundary. Its label, hash and optional freshness checks are insufficient for v2 authority.
- `tools/verify_access_telemetry_lifecycle.py:3679`, `:3805`, `:4058` — direct checkpoint validation, offline runner and live producer runner consume C1; reject before target creation/calls.
- `tools/verify_access_telemetry_lifecycle.py:4527`, `:4918` — terminal bundle and close-out preflight assume legacy C1 fields before calling `_validate_predecessor`; route C1 through strict dispatch and preserve bounded artifact accounting.
- `tools/verify-access-telemetry-lifecycle.py:79` — CLI `--input`/`--scenario-input` routes must share the refusal boundary.
- `tools/access_telemetry_c1_interchange.py:1000` — v2 reader is inspection only; its structural result cannot authorize execution.
- `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — replace legacy authorization expectations and add zero-dependency-call denial cases; retain independent historical inspection tests.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/verify_access_telemetry_lifecycle.py` — introduce strict C1 authorization dispatch and use it in direct, offline, producer, terminal and close-out consumers; refuse legacy and unverifiable v2 before side effects.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — ensure both CLI input routes and freshness flags cannot bypass dispatch.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — cover all matrix cases, producer dependency sentinels and terminal/preflight denial; update legacy happy-path fixtures to express historical inspection only.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — append an offline refusal receipt; preserve prior packet bytes and C0–C6 verdicts.

**Acceptance Criteria:**
- Given a complete-looking legacy C1 packet, when any 27.4 authorizing consumer reads it, then it refuses before target access and emits no passing checkpoint.
- Given a structurally valid v2 packet without independently verified current authority, when a launcher or close-out guard reads it, then it refuses before dependency access.
- Given any CLI mode or freshness option, when the same unverified packet is supplied, then no mode can downgrade the refusal.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — focused lifecycle suite passes with explicit zero-call denials.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — historical structural readers remain inspection only.
- `git diff --check` — no whitespace errors.
