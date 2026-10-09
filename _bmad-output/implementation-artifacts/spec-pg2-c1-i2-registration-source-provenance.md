---
title: 'PG2 C1 closed I2 registration and source provenance boundary'
type: 'feature'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
baseline_commit: '1ead5a1b2432e24211b18060fe1d2da6126b31b3'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/producer-bindings.md'
  - '{project-root}/_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Offline I2 inspection binds caller-supplied source bytes to caller-supplied capture receipts, but registration Refs and Git provenance remain assertions. No approved PG2 C1.15 successor registration or complete P5 evidence exists in this repository.

**Approach:** Add an offline, fail-closed I2 verification boundary that corroborates exact retained registration materials and actual repository source bytes/revision without granting deployed eligibility. Keep every accepting entry absent until genuine approval and authority integration exist.

## Boundaries & Constraints

**Always:** Reuse the closed thirteen-field registry and C1.15 source inspector. Require exact Ref bytes, explicit full commit, a clean local repository, exact tracked regular blobs, safe source reads and supported Git attributes before returning an immutable corroboration. Fixture positives are nonauthoritative. Preserve Story 27.4 RECHECK_ONLY/in-progress, open A41, disabled Production writes, and the two existing user edits.

**Never:** Infer approval from a Ref, `done` label, matching hash, fixture, local Git author/signature or C2 registry. Add deployed accepting entries, run collectors/targets, fetch Git objects, contact a network, stage/commit/publish, change a consumer, or claim I3-I6/P1-P7 closure.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Isolated fixture | Exact C1.15 inventory/capture/source pairs, Ref-matching materials and clean local Git repo at one commit | Immutable corroboration with exact identities; no acceptance | No deployed lookup enabled |
| Registration drift | Missing/changed Ref bytes, wrong story/gate/profile/entry/revision, unsupported role/command material | No corroboration | Content-free refusal |
| Source drift | Dirty tree, unapproved HEAD, foreign/missing blob, alias/symlink, mode/attribute or executed-byte mismatch | No corroboration | Content-free refusal before acceptance/dependency call |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_producer_bindings.py` — `inspect_registry` and `inspect_sources` validate shapes and supplied bytes; `lookup_deployed_binding` always refuses. Retain that refusal.
- `tools/access_telemetry_c1_interchange.py` — reuse immutable snapshots, exact Refs, path/commit rules, capture parser and content-free errors.
- `tools/access-telemetry-c1-profile.ps1` — source producer uses Git HEAD, blob and worktree checks; do not invoke a collector.
- `tests/tooling/access_telemetry_c1_interchange/test_producer_bindings.py` — isolated C1.15 inventory/capture/source fixture builders.
- `docs/operations/c1-producer-binding-inspection-contract.md` — current proof limits; update for added offline API.

## Tasks & Acceptance

**Execution:**
- [x] `tools/access_telemetry_c1_producer_bindings.py` — add strict Ref material binding and explicitly nonauthoritative registration inspection.
- [x] `tools/access_telemetry_c1_source_provenance.py` — corroborate supplied source identities with a clean, explicit local Git repository and safely read regular working files, without network or deployed authorization.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_registration_provenance.py` — isolated positive and negative repo/registration fixtures, denial ordering and zero network/target assertions.
- [x] `docs/operations/c1-producer-binding-inspection-contract.md` and `implementation-tasks.md` — document exact API/results and remaining owner-evidence hold.

**Acceptance Criteria:**
- Given an exact fixture bundle, when offline corroboration runs, then all registration material references and source identities bind one entry and commit, and deployed lookup still refuses.
- Given a substituted valid Git blob, symlink, dirty/untracked source, wrong revision or unsupported attribute, when corroboration runs, then it refuses with no accepted output or external dependency call.
- Given current repository evidence, when deployed eligibility is queried, then C1.15 and held gates refuse because no authenticated approved successor registration exists.
- Given completion, when focused tests and preservation checks run, then all pass without skips and protected Story 27.4/A41/Production bytes remain unchanged.

## Implementation Notes

The user selected “Close verifier; keep deployment held.” The implementation
adds proposed fixture registration statement and exact Ref-material inspection,
then corroborates retained source bytes against one explicit clean local Git
checkout and commit. The source check includes the registered story and verifier
paths. Deployed lookup remains an unconditional refusal. No approved PG2 C1.15
successor registration, authenticated authority/custody, command grammar or
accepted gate was created.

Three independent review layers reported eighteen findings, including one parent
audit. The verified hardening issues were corrected; the ignored-file and linked
worktree limits and command-grammar boundary are documented. Nothing was deferred.
The final focused lane passed 15 tests with zero skips; the full interchange lane
passed 234 tests with zero failures, errors or skips in 60.280s. `git diff --check`
passed. The two preexisting Story 27.4 edits, sprint status and disabled
Production overlay retain their pre-work SHA-256 values. No target/network,
stage, commit or publication action was used.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| B1 replacement refs | high | Git calls do not set `GIT_NO_REPLACE_OBJECTS`; a local replace ref can make an unrelated tree appear at the expected commit. Patch. |
| B2 ignored untracked file | low | Git status omits ignored files, but an unrelated ignored path is not a tracked source or an unignored worktree change. Clarify the documented clean scope; no accepting path exists. |
| B3 skip-worktree | high | Status can hide changed tracked attributes or sources when the index suppresses checks. Reject skip-worktree and assume-unchanged entries. Patch. |
| B4 object alternates | medium | A checkout can resolve a required object from another object store; this weakens explicit local-repository provenance. Reject alternates. Patch. |
| B5 command arguments | medium | Purpose/read-only declarations do not check `kubectl` arguments; a mutating command with updated hashes can pass this nonauthoritative fixture inspection. State the unsupported grammar explicitly and prevent the result from being described as command verification; deployed refusal remains mandatory. Patch documentation. |
| B6 linked worktree | low | `.git` file is refused by design, but the API contract did not disclose this supported-root restriction. Document the restriction. |
| B7 Git output bound | medium | `subprocess.run` buffers full stdout before checking the 1 MiB cap. Bound stdout during acquisition. Patch. |
| B8 missing-blob test | low | The named case deletes a working file and stops at the dirty-tree guard; it does not exercise missing object refusal. Correct the test. |
| B9 no-network test | medium | Python socket patches do not observe a Git child process. Assert the bounded Git command/environment boundary in the fixture test. Patch. |
| E1 assume-unchanged | high | An index assume-unchanged bit can hide modified tracked bytes from status. Same root cause as B3; patch. |
| E2 ignored path | low | Same behavior as B2; document unignored untracked scope. |
| E3 executable mode | medium | Any execute bit is accepted, while Git executable mode follows the owner execute bit. Patch. |
| E4 Git output bound | medium | Same buffering defect as B7; patch. |
| E5 trailing-space root | low | `.strip()` removes valid path spaces as well as the Git newline. Remove only the newline. Patch. |
| E6 clean-repository claim | high | Assume-unchanged can invalidate the clean-tree claim, as in E1/B3. Patch. |
| V1 role exclusion test | medium | The prior `sh` mutation persists into the role-negative case, so deleting the role guard would not fail that test. Reset valid command before role case. Patch. |
| V2 command/role Ref tests | medium | Only registration Ref mismatch is tested at this call site. Add separate command and role Ref mismatch cases. Patch. |
| P1 story/verifier existence | medium | The positive fixture references a nonexistent registered story and verifier path; current Git corroboration checks only the nineteen capture sources. Require tracked regular story/verifier files at the same commit while retaining no claim about owner approval or verifier behavior. Patch. |

## Design Notes

The new offline result is a corroboration of local and retained bytes, not an authenticated registration. The registration authority/provider is P1/P5 and remains external; its absence forces deployed refusal. Local Git corroboration can prove agreement with the specified checkout, not that the checkout or owner was approved.

Use a proposed exact fixture registration statement with `schemaVersion`, `gate`, `profileId`, `registeredStory`, `producerPath`, `helperPaths`, `inputPaths`, `captureSchema`, `verifierPath`, `verifierSchema`, `commandContract`, `cleanupRequired`, `reviewRolePolicy`, and `sourceCommit`. Match every field except `schemaVersion` and `sourceCommit` to the inspected entry, avoiding a circular digest through `registrationReceipt`. Its `registrationReceipt` Ref, plus the entry's command/role Refs, must authenticate the exact retained snapshot bytes. Treat statement status or owner labels as data, never approval. The selected local Git commit must match its source commit and the capture. This schema is a proposed offline fixture contract, not an adopted owner registration format.

## Verification

**Commands:**
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_registration_provenance.py' -v` — new focused lane passes without skips.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — full interchange lane passes without skips.
- `git diff --check` — no whitespace errors.
