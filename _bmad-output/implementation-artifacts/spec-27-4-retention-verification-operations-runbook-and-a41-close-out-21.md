---
title: 'Story 27.4 owner-bound C1.15 verification'
type: 'feature'
created: '2026-10-13'
status: 'done'
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

- [x] `docs/operations/access-telemetry-c1-owner-verification.md` — document API, schemas, anchors and assurance.
- [x] `tools/access_telemetry_c1_owner_c115.py` — implement `inspect_owner_c115` with registry/capture/material snapshots, source/stream tuples, run snapshot/anchors, outside approved registry SHA-256, aware UTC `now`, boolean `withdrawn`. Reinspect inputs; require `observe`; match profile/workload/session/target/source to run. Capture start/finish must be within collection and finish at/before `now`. Bind source/Refs/argv/streams; return frozen inspection with tuples and concealed bytes.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_owner_c115.py` — cover matrix, multi-Pod permutations, immutability, budget/time boundaries and counted dependency denial including prebound aliases; prove rehashed cross-namespace/tenant/Pod refusal.

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

2026-10-14 implementation: Implemented the pure owner-bound C1.15 composition, distinct closed owner materials, raw registry approval binding, capture/run scope and 100 ns timing checks, occurrence-counted budgets, 35 focused tests and the API contract. Every approved execution task is complete. Legacy inspectors and deployed refusal are unchanged. Parent Story 27.4, sprint status, Production, A41, historical evidence and Spec 14 live tasks remain untouched. This completion covers this bounded offline slice only.

2026-10-14 final review record: All three independent layers completed and all twelve findings were individually triaged. Six patch groups were resolved: purpose type/callback safety, missing exact-instance fields, detached source/stream results, composed prebound/native-clock barriers, owner statement inventory/source comparisons and six-snapshot returned-cache assertions. The outer agent read the patched cumulative diff. No findings were deferred; rejected findings and their evidence remain in Review Triage Log. Parent/sprint/A41/live status is unchanged by this standalone spec. The repository's canonical `review` spelling is used for the completion gate.

## Spec Change Log

## Review Triage Log

2026-10-14: All three review layers completed before triage. Every finding below was assessed separately before grouping. The blind review's finding-floor arithmetic is metadata, not a defect.

| ID | Verdict | Route | Verified evidence / disposition |
| --- | --- | --- | --- |
| B1 | high | patch | A forged first stream purpose with custom equality was accepted and its callback executed once. This lets untrusted metadata execute dependencies in the pure API; revalidate the exact string type before comparison. Same root cause as E1/E2. |
| B2 | medium | patch | Deleting capture.raw, source.executed_bytes, stream.stdout or anchors.record_sha256 produced AttributeError in four executable probes. Convert missing exact-instance fields to the documented content-free refusal without preserving exception context. |
| B3 | medium | patch | Mutating the original source after inspection changed the result's retained bytes while inspected identities/count stayed fixed. Detach validated source and stream snapshots from caller instances so downstream readers retain the inspected bytes. |
| B4 | medium | patch | With the new test barriers active, owner_run.datetime remained the original class while owner_inspector.datetime was denied. Cover composed modules' known prebound dependencies too, including counted alias probes. |
| B5 | low | reject | Empty streams do cause bounded source checks before sequence refusal, but hashing is permitted pure work and all supplied bytes are capped before it. A new early-count branch addresses unusual invalid input rather than an everyday defect; reject the added complexity under the low-finding rule. |
| B6 | medium | patch | Complete rehashed owner statement inventory/source mismatches lack direct assertions. This shares V1's verified mutant-survival gap; add a present-field mismatch table after rebinding the Ref. |
| B7 | medium | patch | The six-cache test only asserts capture bytes, so it cannot detect returning a stale caller registry/run/material while checks use fresh snapshots. Assert each corresponding returned snapshot's exact raw bytes and recomputed digest. |
| B8 | false | reject | The composition delegates metadata/UTF-8 to unchanged inspect_sources, whose transform/nonregular and bounded-UTF-8 cases ran in the 482-case lane. No new metadata implementation or bypass was shown; focused wrapper coverage plus executed reusable-rule coverage satisfies this slice without duplicate assertions for every inherited rule. |
| B9 | low | reject | Approval/draft statements describe the earlier approval checkpoint, followed by dated implementation and execution evidence. The requested remedy edits the build spec, so it is rejected under the workflow's explicit spec-edit finding rule; final completion bookkeeping will record current gates. |
| E1 | high | patch | The cited purpose guard is absent; the B1 probe confirms custom equality runs on the retained purpose. Apply the same exact-string preflight fix. |
| E2 | high | patch | The claim about dependency-free refusal is violated by the same executable purpose callback; a clock call can occur before rejection. Same verified root cause as B1/E1. |
| V1 | medium | patch | Pre-verified gap: replacing the statement inventory comparison loop with pass leaves all 35 owner tests green and admits a foreign producerPath after rehashing. Add present-field mismatch assertions to prevent contradictory registration results. Same root cause as B6. |

## Verification

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115.py' -v` — nonzero positive/negative tests pass without skips.
- Repeat with `-p 'test_*.py'`; baseline 447 unittest cases; discovery proves no execution.
- `git diff --check` and file-scope gate passed. Review-readiness invocation `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21 --changed-file _bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21.md` exited 1: draft status is unrecognized. Keep workflow draft until approval, then rerun. Protected tracked bytes equal baseline.

2026-10-14 executed evidence: focused command above exited 0, **35 unittest cases** in 11.797s, OK; full `test_*.py` command exited 0, **482 unittest cases** in 135.379s, OK. Both runs had zero failures/errors/skips. Regression output: `/tmp/owner-c115-interchange-regression.log`. The outer workflow independently read the cumulative unified diff `/tmp/owner-c115-implementation-eimf4ld8.diff` and verified all 35 named owner cases actually passed in that output.

Matrix audit: matching materials/one-to-three Pod permutations are covered by `test_same_run_combines_identical_reparsed_bytes_and_discloses_self_review`, `test_one_two_three_pods_in_every_observation_order_bind_complete_command_blocks` and the frozen-results test. Foreign target/session/source, policy/registry, missing/withdrawn/expired and capture-time cases are covered by their named refusal tests. Forged caches, later-Pod argv/output, source/schema and artifact/aggregate boundaries are covered by their named tests. Every inspector case uses counted ambient/prebound dependency-denial barriers; all three matrix rows are covered by executed passing cases.

2026-10-14 post-review verification: the focused `test_owner_c115.py` command passed **41 unittest cases**, zero skips; the full `test_*.py` command exited 0 with **488 unittest cases** in 131.544s, OK, zero failures/errors/skips. These post-patch totals supersede the earlier implementation totals for current verification. Exact full command: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`; output `/tmp/owner-c115-interchange-review.log`. The outer agent matched all 41 named owner tests to executed passing output; all three matrix rows remain covered.

2026-10-14 completion receipt: `python3 tools/check-story-file-scope.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21 --changed-files-file /tmp/owner-c115-changed-files-mlaftpi4.txt` exited 0, final line `Story file scope validation passed.` The corresponding `python3 tools/check-story-review-readiness.py` invocation with the same arguments exited 0 in canonical review and done states, final line `Story review readiness validation passed.` `git diff --check` exited 0. Cumulative scope matches 4/4; frozen intent and all tracked paths outside this spec are unchanged. HEAD/index are unchanged. The bounded spec alone is done; parent 27.4 remains in-progress and A41/live tasks stay open. No commit or sprint mutation was made because this approved scope explicitly forbids them.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_owner_c115.py`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115.py`
- `docs/operations/access-telemetry-c1-owner-verification.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21.md`

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-21.md`
- `docs/operations/access-telemetry-c1-owner-verification.md`
- `tests/tooling/access_telemetry_c1_interchange/test_owner_c115.py`
- `tools/access_telemetry_c1_owner_c115.py`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-13 | create-story | Prepare owner verification; owner: build agent; no implementation/live evidence. | Phase/cumulative +0; interchange 447 -> 447 unittest cases; new scope 0. Command: `env PYTHONDONTWRITEBYTECODE=1 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange').countTestCases())"`. | Matched 1/1 against `git diff --name-status 8d09c91d134a7f78e40bb169bb4556e588d7915c` plus `git ls-files --others --exclude-standard`: this spec only; no exclusions. |
| 2026-10-14 | dev-story | Implement pure owner-bound byte composition, closed owner schemas, API documentation and complete matrix/dependency-denial coverage; owner: build agent. | Phase +35; cumulative +35; interchange 447 -> 482 unittest cases; new owner scope 0 -> 35 unittest cases. Discovery: `env PYTHONDONTWRITEBYTECODE=1 python3 -c "import unittest; l=unittest.TestLoader(); print('interchange', l.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print('owner', l.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_owner_c115.py').countTestCases())"`. Both focused/full execution commands in Verification passed without skips. | Matched 4/4 against `git diff --name-status 8d09c91d134a7f78e40bb169bb4556e588d7915c` plus `git ls-files --others --exclude-standard`; all four File List paths; no exclusions. |
| 2026-10-14 | code-review | Complete all three independent review layers; resolve six patch groups and independently audit final diff, executed matrix coverage, ledger arithmetic and protected scope; owner: build agent; no deferred findings. | Phase +6; cumulative +41; interchange 482 -> 488 unittest cases; owner scope 35 -> 41 unittest cases. Discovery: `env PYTHONDONTWRITEBYTECODE=1 python3 -c "import unittest; l=unittest.TestLoader(); print('interchange', l.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_*.py').countTestCases()); print('owner', l.discover('tests/tooling/access_telemetry_c1_interchange', pattern='test_owner_c115.py').countTestCases())"`. Focused 41 and full 488 executions passed with zero skips; 447 + 35 + 6 = 488; external delta +0. | Matched 4/4 against `git diff --name-status 8d09c91d134a7f78e40bb169bb4556e588d7915c` plus `git ls-files --others --exclude-standard`; all four File List paths; no exclusions. |
