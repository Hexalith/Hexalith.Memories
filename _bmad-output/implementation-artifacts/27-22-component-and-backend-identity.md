---
title: 'Story 27.22: Component and backend identity'
status: done
baseline_commit: '8b9d87aba37a268e7490f63fb26ef7251c7b3de6'
registration: registered
created: '2026-10-04'
story_key: '27-22-component-and-backend-identity'
gate_id: 'C1.16'
accountable_role: 'Deployment Adapter Developer'
profile_decision: 'Exact PG-ONPREM-2 approved and adopted 2026-10-04'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad/custom/story-scope-guard.md'
  - '{project-root}/_bmad/custom/epic-ac-verification.md'
---

# Story 27.22: Component and backend identity

One reviewed owner for C1.16. Exact PG2 identity and independent full-set
connection linkage are accepted for the captured 2026-10-06 window. Repository
changes and completion records are complete; other gates remain held.

## Story

As a Deployment Adapter Developer,
I want one independently attributable component/backend identity capture on PG-ONPREM-2,
So that an independent reviewer can assess C1.16 without another gate being inferred.

## Acceptance Criteria

1. **Given** a separately authorized eligible target on exact approved PG2 bytes,
   **when** the literal producer below runs, **then** it emits a new immutable,
   secret-safe packet of stable selected Component/API/settings/reference identity,
   loaded component capability advertisements, exact PostgreSQL/Dapr images, and
   actual PostgreSQL 18.6 / 180006 with read-only `peer:postgres` local identity.
2. **Given** missing, malformed, stale, replaced, wrong-profile, wrong-version,
   wrong-image or secret-bearing observations, **when** capture runs, **then** it
   exits nonzero with no positive partial observations; invalid mode/profile and
   source drift are refused before target calls, and invalid modes create no directory.
3. **Given** a complete capture, **when** its result is recorded, **then**
   `gateStatus`, `componentBehavior`, `productionLifecycleWrites` and
   `connectionLinkage` remain `not-evaluated`, with `productionGatePassed: false`.
   Independent evidence must link Dapr's connection to the observed backend;
   selected secret references and a local peer query alone cannot establish linkage.
4. **Given** historical PG1 capture or a successful fixture, **when** reviewed,
   **then** it grants no PG2, independent acceptance, behavioral, security,
   Production activation, Story 27.4 advancement or A41 credit. PG1 C1.16 remains
   explicit historical opt-in; legacy C1.15/PG1 behavior is retained unchanged.

## Profile and Capture Contract

| Field | Required identity |
| :---- | :---------------- |
| Profile alias / canonical SHA-256 | `PG-ONPREM-2` / `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
| Context / namespace | `jpiquot@local` / `hexalith-memories`; actual eligible-target authority must be supplied independently |
| PostgreSQL | Exact authenticated 18.6 index or linux/amd64 child in the approved canonical identity; actual 18.6 / 180006 / `peer:postgres` |
| Dapr | Exact authenticated 1.18.1 index or linux/amd64 child; advertised `state.postgresql/v2` component identity |
| Repository inactive defaults at adoption | Offline renders retain Production writes disabled, lifecycle/clock replicas zero, qualification gate disabled and Lease released |

The inactive defaults above are repository observations, not observed live capture
state. Capture requires an independently authorized eligible running lifecycle
workload. This read-only producer grants no scaling/enablement authority and
does not evaluate live Production write state or Dapr-to-backend linkage.

## Tasks

- [x] Adopt exact approved PG2 and preserve historical profile/evidence identities.
- [x] Make literal C1.16/PG2 callable with bounded immutable/redacted provenance;
  preserve historical opt-in and reject invalid mode/profile before calls/output.
- [x] Execute complete, denied, missing, malformed, drift, secret, timeout,
  version/image/peer and immutability fixtures; run the one-file slice guard.
- [x] Prepare a separate bounded, read-only linkage candidate collector and offline
  attribution/denial fixtures; preserve the neutral producer and pending checkpoint.
- [x] Obtain separately scoped target authorization, an external archive,
  protected runtime credential access and a named independent reviewer before contact.
- [x] Capture actual eligible-target evidence and independently prove connection
  linkage without reading/exporting credentials or mutating tenant data.
- [x] Retain independent disposition and durable archive receipts; record pending
  work honestly until the single C1.16 capture outcome is independently accepted.

## Checkpoint

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| :--------- | :---- | :--------------------------- | :----------- | :--------------- |
| C1.16 | Deployment Adapter Developer | [Independent accepted identity and full-set linkage](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/independent-c1.16-accepted-disposition.json) | accepted 2026-10-06; captured window only; canonical single-session refusal preserved | completed 2026-10-06 |

The external evidence directory is an operator input, not an existing authorized
location. Source/configuration adoption and registration grant no target contact.
The producer is read-only; no Lease acquisition, qualification enablement,
application scaling or tenant data mutation is authorized by this story.

## Dev Notes

### Historical Context Classification

| Influence | Classification | Permitted use |
| :-------- | :------------- | :------------ |
| Approved 2026-08-03 one-gate allocation and 2026-08-01 Annex A | historical-reference-only | C1.16 gate identifier, Deployment Adapter Developer role and component/backend observation only; no bundled story shape. |
| D1-D4 and exact PG2 candidate | historical-reference-only | Approved identity and one registration transaction only. |
| Broad Stories 27.3/27.4 and withdrawn 27.5/27.6 | anti-template | Historical provenance only; no bundled tasks, scope or completion shape. |
| C1.16 capture preparation | current-narrow-pattern | Re-verified bounded Component/backend collection, safe immutable envelope and offline failure fixtures only. |
| Story 27.21 C1.15 command and accepted packet | historical-reference-only | Preserve its literal historical intent and acceptance; no successor profile credit. |

### Slice Proof

One gate, one observation outcome, one accountable role, one callable literal
command and one independent review/completion pair. The twenty-three other held
gates stay unregistered. No predecessor assembler or other gate is discharged.

### Epic AC Verification

Verified 2026-10-04 against `5b43fe2f8a0f04dc021921a077dff1a573c2ce5e`
and the approved adoption worktree before this registration.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "C1.16 — component/backend identity" | Location | `rg -n -F 'C1.16 — component/backend identity' _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md` | Approved one-gate allocation to Deployment Adapter Developer. | confirmed |
| "The remaining twenty-four running-target C1 gates are held without a registered story owner." | Quantitative/existence | `rg -n -F 'The remaining twenty-four running-target C1 gates are held without a registered story owner.' _bmad-output/planning-artifacts/epics.md`; exact story-file/sprint registration audit | Creation-time source claim described 24 held definitions. Current registration adds only Story 27.22/C1.16, leaving 23 held; dated correction is appended at the source claim. | corrected |
| "Story 27.21 is currently the only registered successor and is `in-progress`" | Existence/behavior | `rg -n 'Story 27.21 is currently the only registered successor' _bmad-output/planning-artifacts/epics.md`; `rg -n '27-21-runtime-and-control-plane-identity|27-22-component-and-backend-identity' _bmad-output/implementation-artifacts/sprint-status.yaml`; independent C1.15 review receipt linked from `epic-27-context.md` | Creation-time epic claim was stale: sprint state already recorded Story 27.21 done for historical C1.15. Current registration adds only checked Story 27.22/backlog; dated epic correction preserves original commands/intent. | corrected |
| "Registration and producer existence never enable Production lifecycle writes or close A41." | Configuration/behavior | `kubectl kustomize deploy/kubernetes/overlays/production`; `kubectl kustomize deploy/kubernetes/overlays/qualification`; focused neutral successor packet tests in `/tmp/pg2-c1-matrix-tests.log` | Current offline observations: Production disabled; lifecycle/clock replicas zero; qualification gate disabled, Lease released, reporter suspended; neutral capture grants no write or closure credit. | confirmed |

### Tenant and Privacy Evidence

Changed surfaces: literal profile dispatch, C1.16 selected Component/reference and
pod/server projections, exact PostgreSQL/Dapr pins and source/configuration hashes.
No tenant marker, tenant routing, data payload or storage mutation is introduced.
Focused `test_component_missing_duplicate_wrong_type_and_conflicting_identity_block`
covers wrong namespace/app scope and connection references; successor mismatch
fixtures reject old/wrong images, versions and peers without positive partials.
`test_opt_in_and_exact_gate_profile_denials_precede_calls_and_directory_creation`
proves zero dependency calls for invalid invocation. Physical tenant isolation
remains another held gate requiring live independently authenticated denial evidence.

## Verification

- Implementation-stage C1 focused matrix: 6 tests passed, zero failures/errors/skips, raw log
  `/tmp/pg2-c1-matrix-tests.log` (includes historical capture and legacy lowercase C1.15).
- Implementation-stage full C1 suite and scope guard receipts are recorded in the
  [adoption record](../planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md).
- Final review-patched C1.16 module: 19 passed; lifecycle: 79 passed; CI inventory:
  67 passed, zero failures/errors/skips. The
  [final review receipt](../planning-artifacts/c1-security-prerequisites-2026-10-04/parent-final-review-verification-evidence.json)
  retains exact commands, current source hashes and raw logs. Scope remains one
  backlog owner; no live completion or additional gate acceptance is inferred.

## Outstanding Operator Prerequisites

| Owner | Missing input | Consequence | Reopen trigger |
| :---- | :------------ | :---------- | :------------- |
| Deployment Adapter Developer / Platform Operations | Scoped eligible-target authorization, protected credential access and external archive | No live contact/capture. | Explicit scope and configured target/archive exist. |
| Deployment Adapter Developer / independent reviewer | Actual Dapr-to-backend linkage proof and reviewer assignment | C1.16 remains pending / not complete. | Independently attributable connection evidence and accepted immutable capture. |
| Platform Operations | Approved actual API endpoint/trust fingerprint and protected kubeconfig mapping | Context-name scope alone does not authorize live contact. | Retrievable authority binds and protects the selected target throughout observation. |
| Deployment Adapter Developer | Executed session SQL contract against isolated PostgreSQL 18.6 | Synthetic JSON does not prove the emitted SQL contract; live use remains prohibited. | Same-version execution proves exact fields, role/database, timestamps and TLS shapes without tenant records. |
| Security / qualification owners | Fresh same-hash security/operations dispositions and other gate evidence | Production, Story 27.4 completion and A41 remain blocked. | Their separately owned required gates and approvals pass. |

## Offline linkage preparation — 2026-10-06

Added `tools/verify-access-telemetry-c1-linkage.ps1` and
`tests/tooling/access_telemetry_c1/linkage_test.py` under the approved offline
preparation scope. The separate collector requires exact PG2, a bounded operator
scope receipt, approved source/application image hashes, a selected lifecycle pod,
an external archive binding and a named independent reviewer. It brackets one
authenticated strong GET for a random absent synthetic key with read-only selected
PostgreSQL session/TLS observations, requiring one stable candidate session
whose activity advances within the challenge interval. It rechecks Component,
pod UID/IP, images and container incarnation before emitting positive observations.
The existing C1.16 producer and historical PG1 evidence remain unchanged.

The command, scope schema and archive/reviewer handoff are documented in the
[production appendix](../../docs/operations/access-telemetry-adapter-production.md#separate-c116-connection-linkage-candidate).
Timestamp correlation alone cannot exclude hidden sequential pool reuse. The
operator must supply independently retained evidence of an exclusive read window;
the reviewer must verify that evidence and the immutable identity and linkage
receipts together. Missing or unprovable attribution remains a blocker. The packet
retains `connectionLinkage`, `componentBehavior`, `productionLifecycleWrites` and
`gateStatus` as `not-evaluated`, `productionGatePassed: false`, and independent
disposition `pending`, including after complete offline fixtures.

Changed attribution surfaces are the selected pod IP, runtime role/database, TLS
session identity/timestamps and Dapr absent-key GET. Focused
`test_scope_and_source_mismatches_precede_calls_and_directory_creation`,
`test_wrong_ip_role_database_tls_active_and_timestamp_fields_block`, and
`test_wrong_images_and_component_scope_block_before_challenge` attach negative
evidence for cross-namespace, wrong workload scope, another pod IP, wrong runtime
role and another database. These fixtures grant no physical tenant-isolation gate
credit and introduce no tenant-data writes or record exports.

Verification receipts for this offline preparation:

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v` — final review-patched run: 26 passed, zero failures/errors/skips in 274.195 seconds; raw log `/tmp/story-27-22-final-linkage-tests.log`.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` — final full C1 run: 85 passed, zero failures/errors/skips in 1075.938 seconds; raw log `/tmp/story-27-22-final-all-c1-tests-v2.log`.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-22-component-and-backend-identity` — one checked story.
- `git diff --check` — passed with no whitespace errors.
- PowerShell parser — passed; final source hashes and the frozen intent were checked, with original producers and approved deployment inputs unchanged.

No live collection ran. The configured target's 2026-10-05 preflight observed
PostgreSQL 18.4, so it remains ineligible for PostgreSQL 18.6 / 180006 exact-PG2
capture. `/approved-evidence` remains an absent example path. An eligible target,
separately scoped contact authority, protected runtime credential access, external
archive, independent exclusive-window evidence, actual endpoint/trust binding,
isolated same-version SQL-contract execution and named reviewer remain required
before live collection. The two additional review prerequisites are recorded in
the [deferred-work ledger](deferred-work.md). The C1.16 checkpoint remains pending / not complete;
C1.17, other C1 gates, security, Production, Story 27.4 and A41 receive no credit.

## Remaining qualification prerequisites — dated 2026-10-04

Exact PG2 repository adoption is implemented; earlier pending exact-byte/adoption
claims in the earlier prerequisite proposal documents are historical. An accepted OCI index does
not observe actual execution platform; that qualification remains held under
C1.17 / the unregistered Story 27.23 draft and earns no credit from C1.16.

Deployment Adapter Developer owns a separately scoped, unregistered future PG2
C1.15 producer/review prerequisite. Current `C1.15/PG-ONPREM-2` remains rejected
before target calls/output-directory creation; Story 27.21 and its accepted PG1
packet remain historical. Reopen requires approved executable producer/source
changes, nonzero positive/negative fixtures, separately authorized same-PG2 live
capture and a retrievable independently reviewed disposition. Generic C0 runtime
observations cannot substitute for that renewal.

Inherited in-Pod restart/container-incarnation and remote metadata-response
buffering limitations remain pending maintenance, owned by Deployment Adapter
Developer. No fixes or changes to historical ledger entries are claimed. The
[current handoff](../planning-artifacts/c1-security-prerequisites-2026-10-04/implementation-handoff.md#remaining-qualification-prerequisites--dated-2026-10-04)
records concrete owner/consequence/reopen evidence for all four prerequisites.
No new story registration, live qualification, Production or A41 credit is granted.

## Isolated V3 SQL execution — 2026-10-06

Implemented the separate disposable local lane in
`tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py` and
its command in the production appendix. It uses only the approved linux/amd64
PostgreSQL child, actual 18.6 / 180006, deployment-derived runtime-role/database
bootstrap and `peer:postgres` mapping, and local service-DNS CA/TLS identity.
No collector/deployment bytes or historical receipts changed; no Kubernetes,
real credentials, tenant records or live workload were contacted.

`PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v`
exited **1**: 10 test methods, 9 passed, 1 failed, zero errors/skips in 23.600
seconds. The valid idle TLS runtime session returned `clientAddr: 172.18.0.3/32`
from emitted `a.client_addr::text`, while the existing validator requires selected
pod IP `172.18.0.3`; denial was `session-identity-or-tls-invalid`. Missing/duplicate
sessions, wrong runtime role/database, wrong observer database, non-TLS, DNS
mismatch, alias mutation, missing prerequisite and timeout cleanup checks ran.
These denials do not independently isolate all identity predicates while the
valid address contract is broken. A separately labelled temporary-copy
`host(a.client_addr)` diagnostic passed the full unchanged validation and returned
`172.18.0.3`; it is correction proof only, with no V3 closure.

Bounded local evidence:

- Receipt `/tmp/c1-sql-receipt-hmkwj_j_/receipt.json`, SHA-256 `f8b61d24ae5e324477f7ecc95d3dd4876e49e366a062731a1fb977980b61b168`; `status: failed`, `cleanupErrors: []`, `privateWorkspaceRemoved: true`.
- Unchanged SQL raw result `/tmp/c1-sql-receipt-hmkwj_j_/psql-6.json`; temporary diagnostic `/tmp/c1-sql-receipt-hmkwj_j_/psql-4.json`; each hash is bound in the receipt.
- Raw runner log `/tmp/story-27-22-sql-contract-final-v2.log`, SHA-256 `79efbe42cea6b790b574dbac793a75ed3e7e58f47da79adebc3e50c4d57c7486`.

The receipt binds source/test/query hashes, image/server identity, emitted and
executed arguments, raw-log hashes, test outcomes and cleanup. All uniquely owned
containers/network and private setup files were removed; local `/tmp` receipts
are not a durable external archive or independent acceptance.

**V3 remains open.** Owner: Deployment Adapter Developer. Consequence: the valid
emitted-query contract cannot pass, so live linkage collection remains prohibited.
Reopen trigger: separately authorized collector correction, a complete no-skip
successful integration run and independent review. B6 actual endpoint/trust and
protected context authority remain open, together with all eligible-target,
credential, exclusive-window, archive and reviewer prerequisites. Story 27.22
remains in-progress, C1.16 pending / not complete, and neutral packet fields,
other gates, Production, Story 27.4 and A41 receive no credit.

### Authorized V3 correction execution — 2026-10-06

The user explicitly approved "Approve the collector fix and finish verification"
after the real failure and temporary diagnostic. Changed only the emitted client
address projection to `host(a.client_addr) AS "clientAddr"`; existing validation,
transport and neutral packet fields remain unchanged. The former temporary
correction diagnostic is now a regression restoring the old projection in a
private source copy and asserting its real `/32` rejection.

The integration command above now exits **0**: 10/10 test methods passed, zero
failures/errors/skips in 25.468 seconds. Receipt
`/tmp/c1-sql-receipt-gylthuxh/receipt.json`, SHA-256
`12b08a9a9030c8b6d5ddec3437d44132127ceaea96fe42f3bd4f9c8e2050aaa3`, binds
actual 18.6 / 180006, local `peer:postgres`, valid idle TLS runtime identity,
source/query/test/command hashes, raw results, all ten test outcomes and clean
owned-resource removal. Valid raw result is `psql-6.json`; old-projection
regression is `psql-4.json`, with both hashes receipt-bound. Runner log
`/tmp/story-27-22-sql-contract-corrected.log`, SHA-256
`5e41e6c86996b89f7c0227dc8281ec7fbdef4e2b2cf4276eb2962a7b53362e1f`.

This supersedes the source-correction blocker above; V3 closure remains pending
parent independent review of the successful execution and focused regressions.
All B6/live prerequisites, Story 27.22 in-progress, pending C1.16 checkpoint and
neutral/no-other-credit interpretation remain unchanged.

Focused existing regression verification:
`PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v`
exited 0, 26/26 test methods passed, zero failures/errors/skips in 280.057 seconds.
Raw log `/tmp/story-27-22-linkage-corrected.log`, SHA-256 `446f925434011a2bcb5b6023a99d1604ece8d14fd89e743df12e84bef698939f`.
The unchanged complete C1 discovery still contains 85 methods; this focused lane
is its 26-method linkage subset. Slice, six-file scope, review-readiness,
PowerShell parser and `git diff --check` checks all passed. These are development
receipts; parent independent review still owns V3 disposition.

## Reviewed isolated V3 disposition — 2026-10-06

**V3 is closed for the isolated SQL-contract prerequisite only.** The three
independent build review layers completed; all 14 findings were recorded and
resolved through nine concrete harness corrections. The verification reviewer
independently rechecked the corrected receipt/cleanup boundaries: 9/9 harness
methods passed, with no remaining blocker. Parent final verification ran
`PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v`: exit 0, **19/19 test methods**, zero failures/errors/skips in 26.289
seconds (10 real PostgreSQL methods and 9 harness regressions). The unchanged
focused linkage lane also passed 26/26 in 280.057 seconds.

Final receipt `/tmp/c1-sql-receipt-3bd_gjy6/receipt.json`, SHA-256 `c42eb91b0b356cee4135c8da8b2289e07469a93b289eae8aa87425f226ac52b9`, binds all 19 expected
and executed outcomes, current collector/helper/deployment/test hashes, emitted
SQL and command hashes, real raw results, exact approved linux/amd64 child, actual
18.6 / 180006, local `peer:postgres`, runtime role/database, UTC ordering and TLS.
Parent checked every source/query/raw-log hash, neutral packet fields, private
workspace removal and absence of uniquely labelled owned containers/network.
Final runner log `/tmp/story-27-22-parent-final-sql.log`, SHA-256 `32482e58654fba8683843c39b4892dd6673c260a38e0233907cdeffb1978a111`. Focused regression log
`/tmp/story-27-22-linkage-corrected.log`, SHA-256 `446f925434011a2bcb5b6023a99d1604ece8d14fd89e743df12e84bef698939f`. Local receipts
remain local evidence, not an external archive or independent live acceptance.

This dated disposition supersedes only the isolated-SQL row in Outstanding
Operator Prerequisites and the earlier V3 pending/blocker statements. Original
entries and failure receipts remain historical. Owner: Deployment Adapter
Developer; re-execute and review this lane if approved source/query/profile
identity changes. B6 actual endpoint/trust authority and protected configuration,
eligible target, protected runtime credentials, exclusive-window evidence,
external archive and named live reviewer remain open. **Story 27.22 stays
in-progress; C1.16 stays pending / not complete.** No live contact, Dapr linkage,
other-gate, Production, Story 27.4 or A41 credit is granted.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

- `tools/access-telemetry-c1-component-backend.ps1`
- `tests/tooling/access_telemetry_c1/gate_c1_16_test.py`
- `tests/tooling/access_telemetry_c1/linkage_test.py`

- `docs/operations/access-telemetry-adapter-production.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`

## File List

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`

- `tools/access-telemetry-c1-component-backend.ps1`
- `tests/tooling/access_telemetry_c1/gate_c1_16_test.py`
- `tests/tooling/access_telemetry_c1/linkage_test.py`
- `docs/operations/access-telemetry-adapter-production.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :--- | :--- | :--- | :--- |
| 2026-10-06 | create-story | Root adopts the canonical phase ledger for this live-capture continuation against current full HEAD; earlier committed implementation deltas are historical and are not reconstructed. | C1 85 -> 85; SQL contract 19 -> 19 test methods; adoption phase/cumulative delta +0. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Discovery only. | matched 2/2 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; this story and spec3. Operator records/proposals are outside Git, not tracked changed-file exclusions. |
| 2026-10-06 | dev-story | Root reconciles bounded live preparation, two actual denied captures, independent blocked disposition and reviewed unapplied correction proposal; C1.16 and sprint remain in-progress. | C1 85 -> 85; SQL contract 19 -> 19 test methods; phase/cumulative delta +0. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Four private-copy diagnostic methods and two metadata replay cases pass; these do not change repository discovery or grant live acceptance. | matched 3/3 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; File List covers the exact three changed story records. Operational evidence and proposed source patch remain outside Git. |
| 2026-10-06 | correct-course | User explicitly approves reviewed three-file ACTOR capability correction and completion of regressions, fresh V3 review, protected live capture and build review; all other restrictions persist. | C1 85 -> 85; SQL contract 19 -> 19 test methods; phase/cumulative delta +0. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Observed immediately before/after this scope amendment, prior to code. | matched 3/3 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; three story records only. Approved source paths are allowed but unchanged. |
| 2026-10-06 | dev-story | Exact approved three-file correction applied; complete identity20/20, linkage26/26 and SQL19/19 methods pass without skips. Root verifies source/query/raw-log hashes and cleanup; live acceptance remains pending. | C1 85 -> 86 test methods, phase/cumulative delta +1; SQL contract 19 -> 19 test methods, phase/cumulative delta +0. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Source-application before85 and current discovery86 are comparable. Full execution commands and immutable logs recorded below. | matched 6/6 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; all three story records and three approved source/fixture paths. |
| 2026-10-06 | dev-story | Root verifies source-bound fresh V3 disposition, truthful neutral identities and actual HTTP500 reminder refusal, preserves failed attempts and restored controls; runtime diagnosis and independent live acceptance remain pending. | C1 86 -> 86; SQL contract 19 -> 19 test methods; phase delta +0 C1 / +0 SQL, cumulative story delta +1 C1 / +0 SQL. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Comparable discovery observed before/after this documentation phase; passed 20/26/19 suites were not repeated. | matched 6/6 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; exact three approved source/fixture files and three story records. External evidence remains outside Git. |
| 2026-10-06 | dev-story | Root reconciles the privately observed request cancellation, genuine corrected reminder404 and independently reviewed external bounded state bridge; fresh capture is released, with acceptance pending and historical failures preserved. | C1 86 -> 86; SQL contract 19 -> 19 test methods; phase delta +0 C1 / +0 SQL, cumulative story delta +1 C1 / +0 SQL. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Comparable discovery observed before/after these documentation edits. External8/27 cases are procedural checks outside repository discovery; no passed suite reruns. | matched 6/6 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; three approved source/fixture files and three story records. External operational evidence remains outside Git. |
| 2026-10-06 | dev-story | Root preserves independently reviewed canonical two-session refusal and restoration, reviews separate full-set observer/source/procedure and actual real-PG plus declared mocked/loader checks, and releases fresh read-only capture without weakening canonical behavior. | C1 86 -> 86; SQL contract 19 -> 19 test methods; phase delta +0 C1 / +0 SQL, cumulative story delta +1 C1 / +0 SQL. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Before/after comparable discovery86/19; external12 real/derived,12 mocked-flow and2 loader cases are procedural evidence, not repository discovery additions. | matched 6/6 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; three approved source/fixture files and three story records. External archive/scripts remain outside Git. |
| 2026-10-06 | dev-story | Root reconciles independently accepted actual C1.16 identity/full-set linkage, all immutable command/archive/closure proof and truthful canonical limitation; completes only its checkpoint and synchronizes Story27.22 to review for full build review. | C1 86 -> 86; SQL contract 19 -> 19 test methods; phase delta +0 C1 / +0 SQL, cumulative story delta +1 C1 / +0 SQL. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Before/after discovery86/19; passed20/26/19 suites are retained unchanged. Frozen matrix covering tests actually passed in root audit d0843a47d2c4f53ea5dc0199daefe7695f93ec8a3d5eb87dc492e8a768f1f2e1. | matched 7/7 against frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; three approved source/fixture files and spec/canonical/deferred/sprint records. Only Story27.22 sprint row changes; external archive/scripts remain outside Git. |
| 2026-10-06 | code-review | Root completes blind, edge-case and verification-gap layers, classifies all10 claims before grouping, resolves6 documentation/ledger claims in4 patch groups and rejects4 with evidence. All original7 and new2 documentation chunks are reviewed; exact approved source bytes and frozen intent remain unchanged. Full build review disposition SHA-256 `2ccb4a78a125a34fdd71785ccd36dbd96b39c0111b2e5a4da2e9a508aaa05552`. | C1 86 -> 86; SQL contract 19 -> 19 unittest test methods; review phase delta +0 C1 / +0 SQL, cumulative story delta +1 C1 / +0 SQL. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_sql_contract', pattern='*_test.py').countTestCases()); assert not loader.errors, loader.errors"`. Comparable before/after discoveries agree; retained identity20/20, linkage26/26 and SQL19/19 no-skip logs remain unchanged, with no repeated passed suites. | matched 9/9 against full frontmatter baseline; `git diff --name-status 8b9d87aba37a268e7490f63fb26ef7251c7b3de6 --` plus `git ls-files --others --exclude-standard`; all implementation/test/spec/canonical/deferred/sprint/runbook/epic paths declared, no exclusions. All in-scope review chunks complete before this final row and status synchronization. |


## Actual live capture and blocked disposition — 2026-10-06

The user's "do it yourself" delegated configured-target preparation, archive and
review ownership. Retrievable operator authority binds the actual API endpoint,
CA fingerprint and protected kubeconfig hash/mode. The target now runs the
approved PostgreSQL 18.6 image with actual `180006` and `peer:postgres`; the
original 400 GiB PVC UID/volume is retained. The narrow preparation addendum
aligned live `maxConns` from 64 to the unchanged approved 40. All other checked
Component settings and secret references are unchanged. Lifecycle is one Ready
replica with Enabled=false; clock is zero and Production writes remain false.
The original ingress policy was restored after both attempts, and all owned probes were removed.

Two actual producer commands exited **1**, without positive partial observations:

- `pwsh -NoLogo -NoProfile ./tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -EvidenceDirectory /home/administrator/evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/identity` — `component-settings-mismatch` before alignment.
- `pwsh -NoLogo -NoProfile ./tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -EvidenceDirectory /home/administrator/evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/attempt-2/identity` — `metadata-component-capabilities-mismatch` after alignment.

Actual authenticated Dapr 1.18.1 metadata advertises ETAG, TRANSACTIONAL, TTL,
KEYS_LIKE and ACTOR. The unchanged helper requires exactly the first four.
Pinned Dapr runtime source independently confirms ACTOR is appended when ETAG
and TRANSACTIONAL are available. No linkage collector or fixed-reminder GET ran;
no actor/reminder absence, session correlation or continuous exclusivity is
accepted from prospective plans. Neutral packet fields remain unchanged.

[Independent blocked disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/independent-blocked-disposition-1.json), SHA-256 `d25f8a805908df98efd633a0fef0f8dd3a135182667ca7a8449491def5b55454`, verifies 203 operator inventory entries, 111 command/result ledgers, both failed packets and final controls. [Operator handoff](../../../../../evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/operator-blocked-handoff.json), SHA-256 `316be45a881f44bc72fc6802e3feb64d58925a13e84ef838b4cb0d0afdbee9c1`, retains the exact commands, inventories and rollback records. The archive is persistent local storage outside Git; read-only modes and fsync are recorded, without off-site or WORM claims. Image source labels are provenance, not authenticated source-build attestation.

[Reviewed three-file correction proposal](../../../../../evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/collector-fix-proposal/v2/scope-amendment.md) remains unapplied because the frozen capture spec prohibits repository collector changes. Its exact [patch](../../../../../evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/collector-fix-proposal/v2/proposed.patch), SHA-256 `9085be3a099cbb6bea0ff441701ed9ebe9e6c1c44bd95f1e649e334481787d84`, passed four private-copy fixture methods, two actual-metadata offline replay cases and seven independent helper branch cases. This is diagnostic evidence only. Owner: Deployment Adapter Developer. Consequence: C1.16 remains pending. Reopen: narrowly expand the approved source/file scope, apply the reviewed correction, run affected regressions, re-execute/review V3 against the new helper hash, bind fresh sources/window and capture independently accepted live evidence.

This dated record supersedes only the fulfilled target-authority, protected-access, eligible-target, archive and reviewer-assignment prerequisites. The already reviewed V3 receipt is retained unchanged at `isolated-v3/receipt.json`; any applied helper drift reopens its re-execution/review requirement. Actual identity/linkage acceptance remains blocked. Story 27.22 and its unchanged sprint row stay in-progress. Full build review has not been entered because live capture/linkage remains incomplete; no other gate, Production, Story 27.4 or A41 credit is granted.

## Approved capability correction continuation — 2026-10-06

The user explicitly approved the retained three-file correction and completion scope. The allowed source paths are now added to File Scope; the collector prohibition is renegotiated only for that reviewed patch. Existing blocked capture and V3 receipts remain historical. Fresh affected-fixture verification, V3 source/receipt review, protected source binding, independently accepted live evidence and full build review are required before completion. No Production, other-gate, A41, dependency or Git mutation is authorized.

## Applied correction and offline verification — 2026-10-06

The exact approved patch is applied: historical PG1 retains the four-capability contract, and PG2 requires ACTOR, ETAG, KEYS_LIKE, TRANSACTIONAL and TTL. Profile/deployment/dependency bytes and neutral fields are unchanged. Complete affected modules and isolated V3 passed sequentially with zero failures/errors/skips:

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'gate_c1_16_test.py' -v` — exit0, 20/20 methods in 374.703 seconds. Log `/home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/offline-verification/identity-module.log`, SHA-256 `203ef9576b50cca14de2dba02cae626c14f59746b84303cfdf1466af103c5e5b`.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v` — exit0, 26/26 methods in 249.153 seconds. Log `/home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/offline-verification/linkage-module.log`, SHA-256 `85d9df8f8fa7d58c6e0dfd9915c5b94b28220a7f16572be266d03d8d50fab293`.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v` — exit0, 19/19 methods in25.056 seconds (10 real PostgreSQL methods, nine harness regressions). Log `/home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/offline-verification/sql-contract-lane.log`, SHA-256 `eb49d9dcf0e47a12c78f821f6b4302957af20144917666a29c1acf0301c7cc58`.

Retained full V3 receipt `/home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/offline-verification/v3-retained/receipt.json`, SHA-256 `859968055fd93a9551fc877b6f4dfd1d01ed6ad17b86e7b5232ffbf3d3fad582`, binds the corrected helper hash `bd1ddf38f390e4e0edff2295350ae003296bd420bcb9d8ab336991f8a2babb01`, all19 expected outcomes, ten actual observations, current source/query/raw hashes, actual180006/peer:postgres/local socket and valid idle TLS session. Root independently recomputed these hashes and verified absence of uniquely labelled owned Docker containers/network. The handoff finalizer initially selected a deliberately removed transient harness-unit receipt; it was corrected without rerunning successful suites, and the full primary/nested integration receipt remains retrievable. Independent source-bound V3 disposition is still required before live contact. Fresh local protected binding exists; no target call has occurred in this run, and C1.16 remains pending until actual independently accepted capture.

## Fresh V3 review and live continuation — 2026-10-06

The corrected-source [independent V3 disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/independent-v3-disposition.json), SHA-256 `76a789eaf2a84fd6a0c8e858ec4ef6912d406dae207087b9cd4a59937de91f77`, accepts only the disposable SQL contract prerequisite after all 19 outcomes, retained primary/nested receipts, source/query/raw/command hashes and owned cleanup were verified. Root binds the unchanged approved inputs and corrected helper in [fresh source binding](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/root-reviewed-source-binding.json), SHA-256 `046f7f733b5122efe70827642cc486eba80157724d2aaeffab15185fd4dc7b77`. This supersedes the preceding pending V3 review statement for these exact sources; it grants no live linkage or gate acceptance.

The first freshly bound attempt stopped on external JSONPath newline quoting before isolation, probes or collectors. Its immutable failure/rollback records remain retained. A UID/current-replica-bound recovery restored one Ready disabled lifecycle replica; a reviewed external quoting/archive fix then captured a successful neutral identity packet, but the private reminder check refused its response before linkage. The original policy and owned-probe absence were verified on closure.

The subsequent diagnostic attempt ran `pwsh -NoLogo -NoProfile ./tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -EvidenceDirectory /home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/retry-reminder-20261006T134605Z-a9f4e1cc/identity`, exit 0. Its [neutral identity packet](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/retry-reminder-20261006T134605Z-a9f4e1cc/identity/c1.16-component-backend-identity-20261006T135237255Z-d2978f7d154446f4b035ac1c7678b7ef.json), SHA-256 `c0ae4a1193951b8a729742f146bcf54f37d0ae3fc8b6d92d1359d0b2d87c8e99`, observes the exact five runtime capabilities and PostgreSQL 18.6 / 180006 while all producer acceptance fields remain neutral. The [private refusal ledger](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/operation-7ef4384dee5e44b5a3f91d8336c90fec.json) records actual HTTP 500 / `ERR_ACTOR_REMINDER_GET`, one matching 157-byte content length, no encoding and raw response hash `f52b01f01ca2c220383387cc5c8358c94940b9d8d9ff7b7fc436e522173472e4`; no message/body/header values were retained. This is a real missing runtime prerequisite, not reminder absence. Linkage did not run.

[Closed diagnostic window](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/retry-reminder-20261006T134605Z-a9f4e1cc/exclusive-window-post.json), SHA-256 `f909290793624fa2dfd622d0c413109066a971af4b620494a64fdb02ff61657a`, verifies original policy restoration, owned probe absence and no restoration errors. [Final selected controls](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/retry-reminder-20261006T134605Z-a9f4e1cc/final-operator-controls.json), SHA-256 `f75d778cdbe07f2b02ad4dfd36e91afdb178165de5e2e68647a9acbe30698bfd`, retain lifecycle one Ready and literal-disabled, clock zero, the actual Production disabled reference, approved PostgreSQL and unchanged PVC. The source-derived private failure classifier is preparation only; actual underlying cause and independent attempt disposition are pending. Owner: Deployment Adapter Developer. Consequence: Story 27.22 remains in-progress and C1.16 pending / not complete. Reopen: resolve the Scheduler reminder lookup prerequisite within reviewed authority, then capture genuine before/after absence and independently attributable linkage. No reminders or tenant data were mutated.


## Private diagnosis and bounded transport correction — 2026-10-06

The [independent interim disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/independent-interim-blocked-disposition-1.json), SHA-256 `272c60e074b420c77838a4d0f58f193808ff58acc4943d7eb097ca59ab577cb4`, verified all three fresh attempt inventories and truthful restored controls without accepting linkage. A new bounded private reminder lookup classified its actual HTTP500 as `request-canceled`; the response description remains unexported. That observation diagnoses only the new lookup, without reconstructing earlier hash-only bodies. The actual BusyBox1.37.0 runtime client half-closes TCP when stdin ends, and pinned Go HTTP source cancels the request context on that EOF. The external transport now keeps stdin open five seconds and bounds the in-pod client with TERM at seven seconds and KILL one second later. Eight checks passed, including actual approved-image EOF/hold/deadline behavior and private partial-output suppression; the Python response fixture is transport proof, not a Go/Scheduler simulation. Independent disposition SHA-256 `1ebac6bf7feb696a1f751231b83b159b6de1ef1a3ceb896ee23b8a737d36daf0` records that limit.

The [corrected actual read-only reminder lookup](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/diagnosis-transport-20261006T141822Z-5f217ed7/single-reminder-diagnosis.json), SHA-256 `493cdc413bb1b3091b3d31e9cfc1cdd38036c2e79a0cc27f2a928a59b46abecf`, genuinely returned HTTP404 / `ERR_ACTOR_REMINDER_NOT_FOUND` under the same selected incarnation and protected binding; controls were read back and no mutation occurred. This supersedes the unresolved reminder diagnostic for that observation only. Fresh before/after absence and independently accepted linkage are still required.

The canonical linkage producer uses the same EOF-sensitive pipeline. Its repository bytes remain unchanged. The [independently reviewed external state transport bridge](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-state-20261006T144023Z-a6a1f4b8/independent-state-transport-bridge-disposition.json), SHA-256 `38c1de4f62c599392bc564e5e1c7c0b1ccb7421790464e65f6bb46c7f89907a4`, matches the complete source-bound canonical GET template, preserves its private nonce, strong consistency and internal token handling, and changes only transport timing. All27 checks passed independently, including malformed carriers rejected before execution. Native exit0, genuine complete HTTP204 and an empty body are mandatory; original response headers/body/stderr/nonce remain private. Its safe HTTP projection is explicitly declared, with separate submitted-producer and actual-executed command hashes. Root released exact wrapper `b6b16a9ef87c2fc7da3336d3a254f6cb700edefe7eb0d2b5310ceaaf42972c85` and controller `10fb2d6f99ea237864635ba3734cd120451bad7d07c6e1bce32dd2da446dba9b` in immutable release SHA-256 `4a2d772453e6cea97f9a209e6d0cd62ce453c18adabd9ab02a099da415cbfe08` before a fresh sole-operator window. This grants no gate credit before the actual capture and independent acceptance.


## Canonical live linkage refusal — 2026-10-06

The freshly reviewed window captured identity successfully, then the unchanged canonical Observe linkage command exited1 with `session-attribution-ambiguous`, observations=null and neutral acceptance fields. Its [immutable refused packet](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-state-20261006T144023Z-a6a1f4b8/linkage/c1.16-connection-linkage-candidate-20261006T144732636Z-91a2904543a94215bc4d0918efbd7332.json), SHA-256 `b8499bc2f221a6e5579961b00f4207b3aad07243bdaeb3a95b28b697ee3c2476`, retains the actual source/scope/command provenance. The read-only before snapshot contains two idle TLSv1.3 runtime-role sessions from the selected fresh pod IP; neither is hidden or filtered. The state GET did not run, and no after-reminder or linkage acceptance is claimed. The fresh before reminder404/actor0 checks passed. [Closure](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-state-20261006T144023Z-a6a1f4b8/exclusive-window-post.json), SHA-256 `7020e1f7acd102c3c38d2a8bd153b85d73ed20516ef3d451426cfcf3f403be31`, proves the original policy restored, actual owned-probe absence and no restoration errors; final controls SHA-256 `75f609e07887d27cf0a7afa9feab6a1480e39aa9ac86b0979b668a3685d0a345` preserve disabled/ready lifecycle, clock0, Productionfalse, approved PostgreSQL and unchanged PVC.

Pinned startup source holds a migration transaction while its migration-version query uses the pool again, requiring a second connection. Both return idle, with five-minute idle retirement beyond the permitted fresh capture deadline. Root therefore authorizes preparation of separate read-only full-pool attribution evidence under the existing independent-linkage scope: preserve the canonical refusal, retain every selected-IP session with unchanged SQL, validate stable complete identities and set equality, require exactly one existing PID's query/state timestamps to advance within a single genuine204 GET and all other sessions to remain unchanged. This supplementary observer cannot change repository collectors, feed filtered observations into the canonical validator, label its failed packet successful, terminate sessions or change configuration. Exact source/procedure, meaningful real-PostgreSQL checks and independent review are required before a fresh scoped capture. Story and checkpoint remain pending during preparation.

## Supplemental full-set observer release — 2026-10-06

The [independent closed-attempt disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-state-20261006T144023Z-a6a1f4b8/independent-closed-state-capture-disposition.json), SHA-256 `a0f0abf3f4c90f3f0d84b9ca35b0724c2cac66bdcb48af930c5a17f15baad716`, verifies202 entries/77 operation ledgers, canonical refusal, neutral identity and restored controls. It accepts no linkage from that attempt. Pinned pool analysis SHA-256 `e4b607a13948efbb8aa22cf5c4a6370c37da63e86928bdce03241335bae6d0af` distinguishes the actual two-session result from source-derived startup/idle behavior and does not retrieve secret-derived pool settings.

The separately scoped observer is ready under [independent procedure disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/independent-external-pool-procedure-disposition.json), SHA-256 `adb86481f6f5e79c3466c3d71922b81073ba058bb03eea1d00334b734733a0cb`. Its exact canonical emitted SQL is unchanged, retaining all selected-IP sessions with a41-row overflow sentinel and rejecting counts outside1..40. Full fields/types, runtime role/database/IP, TLS/idle state, incarnation, freshness and ordered timestamps are checked for every row. The complete PID set and identities must stay fixed; exactly one existing PID must advance both activity timestamps in the observation/GET brackets, and every other complete row must remain unchanged. The original five-second freshness, thirty-second snapshot interval and one-second cross-host challenge tolerance are explicit.

`PYTHONDONTWRITEBYTECODE=1 python3 /tmp/story-27-22-check-pool-observer.py` actually exited0: all12 expected cases passed, no skips, with seven real18.6/TLS cases, four derived malformed/single-clock cases and one zero-contact source-drift case. [Fixture handoff](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/real-pg-fixture-handoff.json), SHA-256 `2be3686149bff256a48c3adacac42947e14b1b01066743ddc827bc1c62ea56a9`, binds receipt `e740b7a16679f2404bea1f6c4535488d49583358e3ec8c4fa74d3954c4f062be`, actual tool output, expected/actual manifest and owned-resource/private-workspace removal. The initial fixture omitted its intended pre-run source comparison; only the subsequent verified source comparison is claimed. Independent full-observer checks12/12, receipt `ea396c24a28b02d6aa091b6f064e422b6f473bf51dd0fe1c9e1bce648957be86`, use retained real snapshots and explicitly mocked204 callbacks, not new HTTP execution. Two actual loader checks, receipt `a3e5ca4596360342d2252ec41f89d4d7d3451c72633956437ac9d9fe0b581dd4`, prove hash verification precedes any observer module execution and exact bytes load without a pyc/reread. No previously passed repository module or V3 was rerun.

Root [exact source release](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/root-pool-observer-release.json), SHA-256 `c136cd7183fc301cf9dc19901751922714552ffe59e10f9d5bf652e92bf33915`, binds controller `a71866b8f8da931775c8189d465434856dbe75d9fe082ae8f44af4f746a4f7ce`, observer `0caf0fa051024059aeb999c09aad2c99c16cbfaaaf421c4872f7f9d971f063cd`, existing wrapper/probe, authority, source binding and V3 disposition before a fresh sole-operator capture. This is an interpretation of existing approved independent read-only linkage scope, not new human approval. The prospective plan declares the separate algorithm/schema and submitted/executed transport provenance. The canonical failed packet remains failed; no filtered input is fed to its validator. Complete common identity, backend/lifecycle incarnation, policies, before/after reminder/actor checks and restored-control proof remain required, with neutral provisional fields and independent acceptance pending.


## Independently accepted captured C1.16 — 2026-10-06

The [independent acceptance disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/independent-c1.16-accepted-disposition.json), SHA-256 `ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70`, accepts component/backend identity and independent connection linkage only for this fresh closed window. It verifies176 fixed-inventory files,96 actual sequential ledgers, both28-source/nine-command identity packets,48 safe raw projections, exact source/profile/authority/V3 bindings, full-set SQL/clock/transport evidence and all restoration controls. Root independently replayed the actual pair, source/SQL/raw/command hash layers, complete common identity equality and neutral canonical refusal, and read back176/176 attempt plus824/824 complete-snapshot files in receipt `516468092459e549f8a74cb5a537bac854c904ffa8252023500bd0f22fd13416`. This new disposition supersedes unresolved actual C1.16 attribution only; all earlier failures remain immutable historical failures.

The actual commands retained in their immutable invocation records were:

- `pwsh -NoLogo -NoProfile ./tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -EvidenceDirectory /home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/identity` — exit0; identity SHA-256 `0f3f58f9a91fc7935c9846b3e5ecb1251fdf0cc1607b4143c3285c0593fd4d29`.
- `pwsh -NoLogo -NoProfile ./tools/verify-access-telemetry-c1-linkage.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -Mode Observe -LifecyclePod memories-access-telemetry-cb49f44d-cscb4 -ScopeFile /home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/linkage-scope.json -EvidenceDirectory /home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/linkage` — exit1, `session-attribution-ambiguous`, observations=null; SHA-256 `bbe11c4a23436c7766d66244cd9376374e4c2b5cd48a4756084246903f4dfe28`. This canonical packet stays failed.
- `pwsh -NoLogo -NoProfile ./tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -EvidenceDirectory /home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/identity-after` — exit0; identity SHA-256 `f9987c0bf3f27ccb21308f93c1a68054d6e2bdfcfa752d1184d13170c8ed820c`; complete common observations equal the first identity.

- `PYTHONDONTWRITEBYTECODE=1 python3 /tmp/story-27-22-window-pool-observer.py --review-receipt /home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/independent-v3-disposition.json --review-sha256 76a789eaf2a84fd6a0c8e858ec4ef6912d406dae207087b9cd4a59937de91f77 --source-binding /home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/root-reviewed-source-binding.json --source-binding-sha256 046f7f733b5122efe70827642cc486eba80157724d2aaeffab15185fd4dc7b77` — exit0; [operator-run-receipt.json](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/operator-run-receipt.json), SHA-256 `01458329fc2bd12810884f83a7a596fd227e353d0023d24c2d162ec9dbc29180`, retains the four actual review/source-binding options, controller hash and run log.

The source-released external controller invoked the separate observer under the prospective scope. [Supplementary neutral linkage packet](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/supplemental-pool-linkage-candidate.json), SHA-256 `b8331c3a7c3b620455b8ad8436ce6dd06120d60b784559b5e0b58dbaedae80ba`, retains both same-pod runtime-role/database idleTLSv1.3 sessions. Existing PID25584 alone advances query/state clocks during the single genuine native0/complete204/empty-body GET; PID25585's entire row remains unchanged. Snapshot span is5.662066 seconds; independently computed PG/host clock bounds overlap within the explicit one-second tolerance. Submitted/actually executed public command hashes are separate and match the declared bridge. No original nonce, token, response headers/body or stderr is exported; discarded private bytes are hash-only and cannot be independently reparsed.

The fresh before/after reminder404 / ERR_ACTOR_REMINDER_NOT_FOUND and zero actors, complete selected labels/incarnations and exhaustive selecting policies are verified with source/control proof; samples alone establish no continuous absence. Capture finished at15:15:10.350674Z,51.350674 seconds after fresh Dapr start, before the15:18:19Z safety deadline and15:19:19Z earliest cleanup. [Closure](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/exclusive-window-post.json), SHA-256 `82d3a5e421126c77a2b042d156375951a7f3bc3a9a2c62d18adcb26a9ebd1b35`, and [final controls](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/final-operator-controls.json), SHA-256 `71b3832d9605d39ff0c5907c456ffc411fee964ac89f31c93bd9835817dc656e`, verify exact original policy restored, owned probe absent, no errors, lifecycle1Ready literal-disabled, clock0, approved PostgreSQL and unchanged400Gi PVC. The actual Server Deployment reference resolves to the false ConfigMap value; uptake by every existing Server process is not asserted.

[Handoff](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/closed-pool-capture-handoff.json), SHA-256 `b416b02b12d6925da5621e678a5e2522e7ff4bf0f48eba40bace26453bd535fc`, [attempt inventory](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/attempt-evidence-inventory.json), SHA-256 `0b355c92cd25d9d9b6efcd6079a75ad244f8d17bc9784fb4e9bb48366ed24576`, and [archive snapshot](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/full-archive-snapshot-inventory.json), SHA-256 `abacb408b9f01ea6e4f1eaf74c39fb2cea5b0d22064b752a2a1da6a57f97d27c`, retain actual invocation/source/raw/control proofs with0444/fsync/readback. Persistent local storage supplies no off-site/WORM guarantee. Image labels remain provenance without authenticated build-source attestation. All packet gate/linkage/behavior/write fields remain not-evaluated and productionGatePassed=false; independent acceptance is separate. No other C1/security/Production/Story27.4/A41 credit is granted.

The absolute local archive on the operator workstation is `/home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a`. Durable [controller](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/safe-sources/operator/story-27-22-window-pool-observer.py) and [full-set observer](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/safe-sources/operator/story_27_22_pool_observer.py) copies are under `safe-sources/operator/`, with the launch script, protected wrapper, shared controls and reminder/metadata probes. These source copies and the recorded command document this closed window; future execution requires fresh active authority/source/window bindings and review. The archive is local persistent evidence with no off-site or WORM protection claimed.

The C1.16 checkpoint is completed2026-10-06 and Story27.22 enters review. The canonical tool's exact-one-session limitation remains owned by Deployment Adapter Developer for future captures; permanent multi-session support requires a separately authorized change. The accepted observation uses the retained, separately reviewed full-set procedure. Full build review and final repository gates still govern story completion.


## Final build review — 2026-10-06

All three review layers and all nine changed paths are complete. The [full build disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/build-review/full-build-review-disposition.json), SHA-256 `2ccb4a78a125a34fdd71785ccd36dbd96b39c0111b2e5a4da2e9a508aaa05552`, retains exact reviewed diffs, each finding verdict, resolved documentation/ledger repairs, actual four guard outputs and the independent accepted C1.16 binding. Historical ledger cells and order are preserved; both single canonical tables expose every phase to the actual parser. Root verified same-unit arithmetic86/19, cumulative+1 C1/+0 SQL, File List9/9 and frozen/source hashes. Slice, file-scope and readiness commands using `/tmp/story-27-22-live-changed-files.txt` all exit0; final lines are respectively `story-slice-scope: OK - 1 story file(s) checked: _bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`, `Story file scope validation passed.`, and `Story review readiness validation passed.` `git diff --check` exits0 with empty output. No code/source drift or repeated passed suite is introduced by review. Final completion synchronization and protected clone cleanup follow this accepted review; the canonical single-session limitation remains explicit.

## Completed Story27.22 — 2026-10-06

Story27.22, its final capture spec and only its sprint row are done after independently accepted C1.16 evidence, all three build reviews, all9 reviewed paths and reconciled final code-review rows. The [credential cleanup receipt](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/build-review/protected-clone-cleanup.json), SHA-256 `62e05cd6795510647af07f4f5ce4ad00d8e5af53600f2abff15536e6c0d11430`, proves removal of only the uniquely owned protected clone; original kubeconfig bytes and its existing symlink are preserved. No further target calls occurred. Final repository checks and source/frozen/status readback are retained in [final-completion-verification.json](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/build-review/final-completion-verification.json). The unchanged canonical collector still refuses multiple sessions; future permanent support requires separately authorized work. No Production activation, Lease, other gate, Story27.4 or A41 credit is granted.
