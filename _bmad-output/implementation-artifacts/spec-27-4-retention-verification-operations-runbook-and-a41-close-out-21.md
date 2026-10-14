---
title: 'Story 27.4 owner-bound C1.15 verification'
type: 'feature'
created: '2026-10-13'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '8d09c91d134a7f78e40bb169bb4556e588d7915c'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Owner-run and C1.15 byte inspections are separate. Fixture registration cannot express the adopted owner mode.

**Approach:** Compose existing checks through a pure owner-bound C1.15 inspector and distinct owner registration schemas, preparing later acceptance/assembly.

## Boundaries & Constraints

**Always:** Use `sole-owner-qualification/2026-10-12`, `github:user:6775094`, `sole-owner/self-reviewed`, non-Production PG2 and unchanged profile/workload pins. Obtain approval anchors outside candidates; reparse bytes; reject unknown fields/versions; preserve legacy APIs.

**Never:** Use filesystem/process/network/configuration/clock dependencies, register deployment, accept gates, authorize targets, change Production/sprint/A41/history, stage, commit, push or update dependencies. Spec 14's live tasks remain open.

## I/O & Edge-Case Matrix

| Input | Outcome |
| --- | --- |
| Matching owner run, registry/materials, nineteen sources, one/multi-Pod capture/streams | Frozen combined inspection; no pass or execution flag |
| Rehashed foreign target/session/source, changed policy/registry, missing/withdrawn/expired run or out-of-window capture | Content-free refusal |
| Forged cache, altered later-Pod argv/output, wrong sources/schema/budget | Refusal before dependencies; no partial result |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_owner_run.py` — pure run/time/status checks.
- `tools/access_telemetry_c1_producer_bindings.py` — registry/source/Ref checks; preserve legacy refusal.
- `tools/access_telemetry_c1_capture_streams.py` — strict argv/output consistency; no execution/provenance claim.

## Tasks & Acceptance

**Execution:**

- [ ] `docs/operations/access-telemetry-c1-owner-verification.md` — document API, schemas, anchors and assurance.
- [ ] `tools/access_telemetry_c1_owner_c115.py` — implement `inspect_owner_c115` with registry/capture/material snapshots, source/stream tuples, run snapshot/anchors, outside approved registry SHA-256, aware UTC `now`, boolean `withdrawn`. Reinspect inputs; require `observe`; match profile/workload/session/target/source to run. Capture start/finish must be within collection and finish at/before `now`. Bind source/Refs/argv/streams; return frozen inspection with tuples and concealed bytes.
- [ ] `tests/tooling/access_telemetry_c1_interchange/test_owner_c115.py` — cover matrix, multi-Pod permutations, immutability, budget/time boundaries and counted dependency denial including prebound aliases; prove rehashed cross-namespace/tenant/Pod refusal.

**Acceptance Criteria:**

- Given complete same-run owner materials, when inspection runs, then component results bind identical bytes/run and disclose self-review.
- Given unsafe or inconsistent input, when inspection runs, then it refuses without invoking dependencies or returning partial inspection.
- Given existing fixtures and consumers, when affected regression lanes run, then legacy behavior and deployed refusal remain intact.

## Design Notes

Under `hexalith.access-telemetry.c1`, select `owner-registration/v1`, `owner-command-contract/v1`, `owner-review-role-policy/v1`. Registration: existing fixture-registration fields plus `policyId`, `policySha256`, `ownerPrincipal`, `assuranceMode`, matched to run. Command: existing seven fields/six ordered kinds, strict v2 argv and false cleanup. Role: exactly schemaVersion, gate, profileId and four owner fields. Bind three exact Refs; approved registry digest covers raw bytes. Matching caller approval assertions authenticates no owner/registration.

Before reparsing/hashing, cap each artifact at 1 MiB and all byte occurrences at 32 MiB, including streams/source forms. Reuse source metadata/normalization; require clean observed capture, zero failures/skips, unchanged final recheck. Add no filesystem wrapper or acceptance record.

## Implementation Notes

2026-10-14: Owner approved this spec and requested "approve and stop". The frozen intent is unchanged. Implementation has not started; all execution tasks remain open.
Approval-state validation: the declared file-scope and review-readiness commands both passed with exit 0; `git diff --check` also exited 0. This supersedes the earlier draft-status refusal, without claiming implementation or live evidence.

## Spec Change Log

## Review Triage Log

## Verification

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115.py' -v` — nonzero positive/negative tests pass without skips.
- Repeat with `-p 'test_*.py'`; baseline 447 unittest cases; discovery proves no execution.
- `git diff --check` and file-scope gate passed. Review-readiness invocation `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21 --changed-file _bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21.md` exited 1: draft status is unrecognized. Keep workflow draft until approval, then rerun. Protected tracked bytes equal baseline.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_owner_c115.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115.py`
- `docs/operations/access-telemetry-c1-owner-verification.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21.md`

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-13 | create-story | Prepare owner verification; owner: build agent; no implementation/live evidence. | Phase/cumulative +0; interchange 447 -> 447 unittest cases; new scope 0. Command: `env PYTHONDONTWRITEBYTECODE=1 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange').countTestCases())"`. | Matched 1/1 against `git diff --name-status 8d09c91d134a7f78e40bb169bb4556e588d7915c` plus `git ls-files --others --exclude-standard`: this spec only; no exclusions. |
