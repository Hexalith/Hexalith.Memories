---
title: 'Story 27.22: Verify the isolated linkage SQL contract'
type: 'chore'
created: '2026-10-06'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-22-component-and-backend-identity'
baseline_commit: 'f5beeed52874f7feedf3d48d040feaea163cd3cd'
context:
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.22 fixtures manufacture JSON without executing SQL. V3 requires real PostgreSQL 18.6 execution before live collection.

**Approach:** Add and execute a separate disposable local integration lane using the existing emitted query and validation. Discharge V3 after verification/review; keep live acceptance pending.

## Boundaries & Constraints

**Always:** Use approved linux/amd64 child `docker.io/library/postgres@sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04`; verify actual 18.6 / 180006. Mirror database, runtime role, peer mapping and TLS identity. Bound commands; publish no ports; use disposable credentials and secret-safe receipts; clean up only uniquely named owned resources. Preserve deployment bytes, historical evidence, neutral packet fields and the pending C1.16 checkpoint. The user explicitly renegotiated collector-byte preservation on 2026-10-06 ("Approve the collector fix and finish verification"): permit only the emitted client-address projection correction from `a.client_addr::text` to `host(a.client_addr)` and its verification; preserve all other collector behavior.

**Never:** Contact Kubernetes, read real credentials/tenant records, create tenant data, change profile/dependencies/workloads/Git, or claim live linkage, other-gate, Production, Story 27.4 or A41 credit. Missing prerequisites must fail, never skip/pass.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :------- | :------------ | :------------------------- | :------------- |
| Valid | One idle TLS runtime session; local peer observer | Real emitted SQL passes existing fields/types, identity, UTC ordering and TLS validation | Retain bounded receipt |
| Invalid | Missing/duplicate session, wrong role/database or TLS | Existing validation refuses real result | Assert specific denial |
| Mutation | Rename emitted alias in temporary source | Contract verification detects broken field | Tracked source unchanged |
| Setup failure | Missing image/runtime/TLS or timeout | Nonzero failure; V3 unresolved | Cleanup owned resources |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1-linkage.ps1:738` — `Get-LinkageSessions` emits read-only psql arguments and validates output. Capture its actual command through a local adapter; load only necessary function definitions, never the collector's top-level code or handwritten SQL.
- `deploy/kubernetes/base/access-telemetry-postgresql.yaml:18` — peer mapping `postgres` to `memories_admin`, database/runtime role and TLS contracts; read only.

## Tasks & Acceptance

- [x] `tools/verify-access-telemetry-c1-linkage.ps1` — apply the user-authorized one-line client-address projection correction; preserve all validators, transport, packet fields and live boundaries.
- [x] `tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py` — isolated setup, actual emitted-command execution/validation, matrix regressions, alias mutation, bounded evidence and cleanup.
- [x] `docs/operations/access-telemetry-adapter-production.md` — document the integration command and restrictive interpretation.
- [x] `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md` and `deferred-work.md` — append actual reviewed V3 disposition; retain B6 target authority and all live prerequisites.

**Acceptance Criteria:**
- Given reviewed success, when reconciled, then only V3 closes; Story 27.22 stays in-progress with live acceptance pending.

## Dev Notes

### Historical Context Classification

| Influence | Classification | Permitted use |
| :-------- | :------------- | :------------ |
| Completed 27.22 offline spec | current-narrow-pattern | Re-verified collector and explicit V3 gap only. |
| Story 27.21 | historical-reference-only | Preserve evidence; no inherited live authorization. |
| Broad 27.3/27.4 | anti-template | Provenance only; no bundled qualification scope. |

### Slice Proof

Deployment Adapter Developer owns one C1.16 prerequisite.

### Epic AC Verification

Verified 2026-10-06 against the frontmatter baseline.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "Synthetic kubectl JSON does not execute that SQL." | Behavioral | `tests/tooling/access_telemetry_c1/linkage_test.py:140` | Fixture constructs session JSON. | confirmed |
| "execute the emitted session SQL contract in an isolated PostgreSQL 18.6 environment" | Existence | `rg -n 'isolated PostgreSQL' docs/operations/access-telemetry-adapter-production.md` | V3 is pending. | confirmed |

## Implementation Notes

2026-10-06: The user approved this spec and requested stopping before implementation. The frozen intent is approved; implementation tasks remain pending. Story 27.22 and its live C1.16 checkpoint remain in-progress/pending.

2026-10-06: Implemented the isolated emitted-command lane and operations command. Real PostgreSQL 18.6 / 180006 execution found a blocker in the unchanged collector: `a.client_addr::text` returns `172.18.0.3/32`, while validation expects selected pod IP `172.18.0.3`. The valid TLS case fails `session-identity-or-tls-invalid`; V3 stays open. A temporary-copy `host(a.client_addr)` diagnostic passes existing validation but grants no closure. Owner: Deployment Adapter Developer; consequence: live linkage remains prohibited; reopen only after separately authorized collector correction, complete no-skip successful integration and independent review. Collector/deployment/historical bytes and C1.16 pending state are preserved. Reviewed disposition remains for parent review.

2026-10-06: Applied the explicit user-authorized one-line `host(a.client_addr)` correction. Real integration now passes 10/10 with zero failures/errors/skips; old-projection mutation restores the exact denial. V3 closure remains pending parent independent review; no live checkpoint/status advances.

## Spec Change Log

2026-10-06: User explicitly approved "Approve the collector fix and finish verification" after the concrete real-SQL /32 failure and successful temporary `host(a.client_addr)` diagnostic. Expanded scope only to the one-line collector SQL projection correction and focused regression verification; all live/deployment/profile/Git/neutral-state boundaries remain unchanged.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| :--- | :--- | :--- | :--- |
| BH1 | medium | patch | tearDown records success before addCleanup errors are entered in unittest results; source tracing and the reviewer lifecycle probe establish false passed receipts. |
| BH2 | medium | patch | tearDown consults only failures/errors, so skipped methods become passed; require skip-aware final outcomes. |
| BH3 | medium | patch | finish accepts any nonempty successful subset with observations; expected full-lane method identities must be reconciled before receipt success. |
| BH4 | medium | patch | Any nonzero Docker inspect currently skips removal as if absent; daemon errors must fail cleanup rather than certify absence. |
| BH5 | medium | patch | Uncaught source read/serialization errors occur before private-directory deletion; missing-source probe demonstrates retained credential workspace. |
| BH6 | medium | patch | bounded raises before run_command appends failure results and the adapter writes raw evidence; preserve bounded failed command evidence before raising. |
| BH7 | medium | patch | The timeout container is not in finish's three-resource inventory; a failed per-test finally leaves it outside class retry. |
| BH8 | medium | patch | Setup copies HBA/bootstrap but hardcodes server identity env/TLS flags; changed authoritative manifest identity can leave local verification testing old settings. Assert exact scoped deployment-setting agreement. |
| EC1 | medium | patch | Same verified early-outcome defect as BH1; cleanup errors occur after tearDown. |
| EC2 | medium | patch | Same verified inspect-result defect as BH4; daemon-unavailable results do not establish resource absence. |
| EC3 | medium | patch | Same verified private-deletion defect as BH5; source read errors bypass deletion and receipt creation. |
| EC4 | medium | patch | start_session performs one os.read and compares exact bytes; a split 1/newline response rejects a valid session. Accumulate only the bounded expected response until deadline. |
| VG1 | medium | patch | Verified Other finding: inherited unittest cleanup probe records one runner error but passing receipt; same defect as BH1. |
| VG2 | medium | patch | Verified Other finding: finish skips three daemon-unavailable inspections and emits passed; same defect as BH4. |

Resolution: all findings were patched and verified. Shared-root groups were
outcome timing (BH1/EC1/VG1), skip accounting (BH2), suite completeness (BH3),
inspection errors (BH4/EC2/VG2), private cleanup (BH5/EC3), failed-command evidence
(BH6), timeout-resource inventory (BH7), manifest identity agreement (BH8), and
response framing (EC4). No findings were rejected, deferred or left unresolved.
Independent verification recheck passed all nine harness methods and found no
remaining blocker; parent final execution passed all 19 methods.

## Verification

`PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v` must pass without skips, including denials. Receipt binds collector/query/test hashes, image/server identity, commands/results and raw-log hashes. Run story slice, whitespace, file-scope and review-readiness checks; preserve live status.

## File Scope

Allowed files for this story:

- `tools/verify-access-telemetry-c1-linkage.ps1`
- `tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py`
- `docs/operations/access-telemetry-adapter-production.md`
- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md`

## File List

- `tools/verify-access-telemetry-c1-linkage.ps1`
- `tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py`
- `docs/operations/access-telemetry-adapter-production.md`
- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md`

### File List Exclusions

- `references/Hexalith.EventStore` — owner: pre-existing repository dependency maintenance; committed in `9ec9f505f25659d8ee81eac3a5eaa5cd9e6c6031` before this implementation and outside this approved SQL-contract slice.
- `references/Hexalith.Platform` — owner: pre-existing repository dependency maintenance; committed in `9ec9f505f25659d8ee81eac3a5eaa5cd9e6c6031` before this implementation and outside this approved SQL-contract slice.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-06 | create-story | Root plans isolated V3 verification. | C1 85 -> 85 test methods; phase/cumulative delta +0. Absent integration scope: separate baseline 0. Discovery below. | matched 1/1 against frontmatter baseline; `git status --short`; only this new spec. |
| 2026-10-06 | dev-story | Implemented disposable emitted SQL lane, denial/mutation/setup checks and temporary correction proof. Actual valid-contract execution blocked by client_addr::text /32 mismatch; V3 remains open with owner/consequence/reopen trigger. | C1 test methods 85 -> 85, phase/cumulative delta +0; new SQL contract discovery unit 0 -> 10 test methods, phase/cumulative delta +10. Exact commands: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases())"` and same command with `access_telemetry_c1_sql_contract`. SQL scope absent at create baseline; final discovery has zero errors. Execution 9 passed / 1 failed / 0 errors / 0 skips; owner Deployment Adapter Developer, reopen after authorized collector fix and successful reviewed rerun. | matched 5/5 against `f5beeed52874f7feedf3d48d040feaea163cd3cd`; `git diff --name-status f5beeed52874f7feedf3d48d040feaea163cd3cd --` plus `git ls-files --others --exclude-standard`; pre-existing committed `references/Hexalith.EventStore` and `references/Hexalith.Platform` excluded under File List Exclusions (repository dependency maintenance owner). |
| 2026-10-06 | dev-story | Applied explicitly user-approved host(client_addr) SQL projection correction; repurposed temporary correction proof into old-projection denial regression. Integration and focused existing linkage execution pass; V3 disposition remains pending independent review. | C1 test methods 85 -> 85, phase/cumulative delta +0; SQL contract test methods 10 -> 10, phase delta +0 / cumulative delta +10 from absent-scope baseline 0. Focused linkage discovery 26 -> 26 methods (subset of C1 85), phase/cumulative delta +0. Exact discovery commands: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases())"` and same C1 discovery with `pattern='linkage_test.py'`. Integration 10/10 and focused linkage 26/26 passed without skips. | matched 6/6 against `f5beeed52874f7feedf3d48d040feaea163cd3cd`; `git diff --name-status f5beeed52874f7feedf3d48d040feaea163cd3cd --` plus `git ls-files --others --exclude-standard`; pre-existing committed EventStore/Platform submodule changes excluded under named File List Exclusions. |
| 2026-10-06 | code-review | Root audited every review layer and patched all 14 findings; nine harness regressions prove receipt outcome/completeness, cleanup, failed evidence, framing and manifest drift boundaries. Parent verification and independent recheck pass; only V3 closes. | C1 test methods 85 -> 85, phase/cumulative delta +0; SQL-contract scope 10 -> 19 test methods, phase delta +9 / cumulative delta +19 from create baseline 0; linkage subset 26 -> 26 methods, phase delta +0 (included in C1). Exact discovery: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print('C1 test methods:',loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print('Linkage subset methods:',loader.discover('tests/tooling/access_telemetry_c1', pattern='linkage_test.py').countTestCases()); print('SQL contract test methods:',loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Parent execution 19/19; independent harness recheck 9/9; focused linkage 26/26; zero failures/errors/skips. | matched 6/6 against `f5beeed52874f7feedf3d48d040feaea163cd3cd`; `git diff --name-status f5beeed52874f7feedf3d48d040feaea163cd3cd --` plus `git ls-files --others --exclude-standard`; named pre-existing EventStore/Platform exclusions remain unchanged. |

Creation-phase discovery: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases())"` returned 85 with zero discovery errors. No implementation tests ran in the create-story phase.

Development verification: the exact integration command above exited 1 with 10 test methods, 9 passed, 1 failed, zero errors/skips in 23.600 seconds. Raw log `/tmp/story-27-22-sql-contract-final-v2.log` SHA-256 `79efbe42cea6b790b574dbac793a75ed3e7e58f47da79adebc3e50c4d57c7486`; receipt `/tmp/c1-sql-receipt-hmkwj_j_/receipt.json` SHA-256 `f8b61d24ae5e324477f7ecc95d3dd4876e49e366a062731a1fb977980b61b168`, status failed with no cleanup errors and removed private workspace. Diagnostic and unchanged raw result hashes are receipt-bound. No skips/pass substitution is used; V3 cannot close.

Authorized-correction verification: integration exited 0 (10/10 methods, zero failures/errors/skips, 25.468 seconds). Receipt `/tmp/c1-sql-receipt-gylthuxh/receipt.json` SHA-256 `12b08a9a9030c8b6d5ddec3437d44132127ceaea96fe42f3bd4f9c8e2050aaa3`; runner log `/tmp/story-27-22-sql-contract-corrected.log` SHA-256 `5e41e6c86996b89f7c0227dc8281ec7fbdef4e2b2cf4276eb2962a7b53362e1f`. This supersedes the prior execution blocker, retaining V3 closure pending independent review. Focused linkage regression command with `-s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v` exited 0: 26/26 test methods, zero failures/errors/skips in 280.057 seconds; `/tmp/story-27-22-linkage-corrected.log` SHA-256 `446f925434011a2bcb5b6023a99d1604ece8d14fd89e743df12e84bef698939f`.

Development checks (all exit 0):

- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-22-component-and-backend-identity` — `story-slice-scope: OK - 1 story file(s) checked: _bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`.
- `python3 tools/check-story-file-scope.py --story-key spec-27-22-component-and-backend-identity-2 --changed-files-file /tmp/story-27-22-sql-contract-changed-files.txt` — `Story file scope validation passed.` The earlier missing parser label was repaired outside frozen intent.
- `python3 tools/check-story-review-readiness.py --story-key spec-27-22-component-and-backend-identity-2 --changed-files-file /tmp/story-27-22-sql-contract-changed-files.txt` — `Story review readiness validation passed.` Explicit changed set contains all six scoped paths, not the two named pre-existing submodule exclusions.
- `git diff --check` — no whitespace errors.
- PowerShell AST parser on `tools/verify-access-telemetry-c1-linkage.ps1` — `collector-parse: OK`.

## Final Review Record

Parent `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v` exited 0: 19/19 methods, zero failures/errors/skips, 26.289 seconds. Final receipt `/tmp/c1-sql-receipt-3bd_gjy6/receipt.json` SHA-256 `c42eb91b0b356cee4135c8da8b2289e07469a93b289eae8aa87425f226ac52b9`; log `/tmp/story-27-22-parent-final-sql.log` SHA-256 `32482e58654fba8683843c39b4892dd6673c260a38e0233907cdeffb1978a111`. Every current source/query/raw-log hash and all expected outcomes reconcile; uniquely owned containers/network are absent and private credentials removed. The earlier 26/26 focused collector regression result remains current because review patches changed only the test harness. The independent verification reviewer ran `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -k HarnessFailureTests -v`: 9/9 passed and both original receipt defects resolved, with no remaining blocker.

Matrix audit: valid — `test_valid_idle_tls_emitted_contract`; invalid — missing/duplicate, wrong runtime role/database, wrong observer database, non-TLS and TLS service-DNS methods; mutation — `test_emitted_alias_mutation_is_detected_without_source_changes` and old-inet projection regression; setup failure — missing-prerequisite and timeout cleanup methods. All ran and passed in the parent output. Harness failure probes preserve real runner outcomes and failed receipts while their enclosing assertions pass.

V3's reviewed isolated SQL prerequisite is closed in the story, deferred ledger and operations appendix. Story 27.22 and sprint remain in-progress with C1.16 pending. Terminal workflow status synchronization and automatic local commit are not applied: the approved frozen constraints explicitly preserve the live checkpoint and prohibit Git changes. No stage/commit/push/branch/submodule mutation occurred.

Repository status compatibility: the build workflow's temporary `in-review` spelling is rejected by the canonical review-readiness gate, which accepts `review`. The review phase uses `review` before terminal `done`; no sprint status changes or gate bypass were made.

Final status gate: `python3 tools/check-story-review-readiness.py --story-key spec-27-22-component-and-backend-identity-2 --changed-files-file /tmp/story-27-22-sql-contract-changed-files.txt` exited 0 at canonical review status; final line `Story review readiness validation passed.` Cumulative File List was independently reconciled 6/6, with only the two declared pre-existing committed submodule exclusions. `python3 tools/check-story-review-readiness.py --story-key 27-22-component-and-backend-identity` also exited 0 while retaining in-progress and the pending live checkpoint. Slice, file-scope, PowerShell parsing and whitespace checks pass.
