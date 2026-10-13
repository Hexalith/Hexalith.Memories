---
title: 'Story 27.4 multi-Pod C1.15 registration command inspection'
type: 'feature'
created: '2026-10-13'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '24b1fa7408eabc1db24b7536fd35f575c7a572a7'
context:
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Offline registration inspection requires exactly six command receipts. The real C1.15 grammar emits three receipts per Pod plus three shared receipts, so valid multi-Pod captures cannot complete registration inspection.

**Approach:** Add explicitly dispatched `hexalith.access-telemetry.c1.fixture-command-contract/v2`, using the existing strict C1.15 command inspector. Preserve v1 behavior and return the unchanged frozen registration inspection.

## Boundaries & Constraints

**Always:** Keep v2's seven fields identical to v1. Its six ordered `purposes` are grammar kinds: context, selection, version/metadata/alpha for each observed Pod in order, then recheck. Validate exact pinned argv through `inspect_c115_command_declarations`; bind all existing Ref bytes, clean source receipts, nineteen source snapshots and budgets. Require `cleanupRequired: false`. Preserve the existing registration and independent role-policy schemas. Document v2 as an offline fixture declaration, with no owner-registration claim.

**Never:** Change v1 acceptance/refusals, owner policy or deployed lookup; reinterpret producer exclusion; execute collectors/verifiers or contact targets; change Production, sprint status, historical evidence or A41; stage, commit, push or update dependencies.

## I/O & Edge-Case Matrix

| Scenario | Input | Outcome |
| --- | --- | --- |
| Complete v2 | One, two or three Pods; matching ordered argv and source/Ref bytes | Frozen inspection, including reversed Pod order |
| Substitution | Rehashed later-Pod argv, probe, scope or reordered receipt blocks | Content-free refusal before local Git/filesystem access |
| Binding drift | Source, Ref, schema, kinds, cleanup or role mismatch | Existing bounded refusal; no inspection |
| Legacy | Existing v1 fixtures, including multi-Pod refusal | Identical behavior |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_producer_bindings.py:319` — `inspect_registration` authenticates materials, then compares six flattened purposes. Extend only command-schema dispatch; reuse strict inspection from `access_telemetry_c1_capture_semantics.py`.
- `tools/access_telemetry_c1_source_provenance.py:219` — calls registration before any local dependency. Preserve its implementation and return type.
- `tests/tooling/access_telemetry_c1_interchange/test_capture_commands.py:49` — independently authored `command_fixture`; combine its strict receipt/Pod facts with nineteen source snapshots from `test_producer_bindings.fixture_sources`.

## Tasks & Acceptance

**Execution:**

- [ ] `tools/access_telemetry_c1_producer_bindings.py` — add closed v2 dispatch without altering v1 or role checks.
- [ ] `tests/tooling/access_telemetry_c1_interchange/test_registration_commands.py` — cover every matrix row with independent fixtures and counted process/network/filesystem/configuration/clock denial barriers.
- [ ] `tests/tooling/access_telemetry_c1_interchange/test_registration_provenance.py` — prove valid v2 reaches clean local corroboration and malformed v2 refuses before its dependencies.
- [ ] `docs/operations/c1-producer-binding-inspection-contract.md` — document version selection, expanded grammar and unchanged assurance limits; preserve the historical manifest/results.

**Acceptance Criteria:**

- Given a clean multi-Pod capture and matching retained materials, when v2 registration and isolated local corroboration run, then both retain exact byte hashes and return inspection without gate or execution authority.
- Given another tenant/namespace/Pod or unsafe command with recomputed hashes, when v2 is inspected, then denial precedes local dependencies.
- Given existing consumers and v1 fixtures, when the affected lane runs, then compatibility, deployed refusal, Production disablement and open A41 are preserved.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_registration*.py' -v` — focused positive, denial and compatibility checks pass.
- Same command without `-p 'test_registration*.py'` — full affected lane passes without skips.
- `git diff --check` and story-key file-scope/readiness gates — only declared paths, protected state unchanged.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_producer_bindings.py`
- `tests/tooling/access_telemetry_c1_interchange/test_registration_commands.py`
- `tests/tooling/access_telemetry_c1_interchange/test_registration_provenance.py`
- `docs/operations/c1-producer-binding-inspection-contract.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-20.md`

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-20.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-13 | create-story | Prepare versioned command inspection; owner: current build agent. Offline reproduction: v1 returns inspection for one Pod; two/three Pods refuse `command-contract-unsupported`. No live observation or gate credit. | Phase +0, cumulative +0; interchange 428 -> 428 unittest methods. New command-test scope baseline 0 methods. Command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange').countTestCases())"` — 428. | Matched 1/1 against `git diff --name-status 24b1fa7408eabc1db24b7536fd35f575c7a572a7` plus `git ls-files --others --exclude-standard`: this spec only, no exclusions. |
