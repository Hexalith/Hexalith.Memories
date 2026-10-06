---
title: 'Story 27.22: Component and backend identity'
status: in-progress
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

One checked in-progress owner for C1.16, registered after approved PG2 adoption and
focused fixture verification. Repository support is callable; live capture,
connection linkage, independent acceptance and completion remain pending.

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
- [ ] Obtain separately scoped target authorization, an external archive,
  protected runtime credential access and a named independent reviewer before contact.
- [ ] Capture actual eligible-target evidence and independently prove connection
  linkage without reading/exporting credentials or mutating tenant data.
- [ ] Retain independent disposition and durable archive receipts; record pending
  work honestly until the single C1.16 capture outcome is independently accepted.

## Checkpoint

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| :--------- | :---- | :--------------------------- | :----------- | :--------------- |
| C1.16 | Deployment Adapter Developer | `pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -EvidenceDirectory /approved-evidence/access-telemetry-c1/C1.16` | pending independent live review | pending / not complete |

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
