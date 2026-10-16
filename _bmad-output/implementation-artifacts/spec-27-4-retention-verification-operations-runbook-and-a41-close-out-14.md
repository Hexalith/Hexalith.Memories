---
title: 'Story 27.4 sole-owner qualification and A41 close-out'
type: 'feature'
created: '2026-10-12'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'b1d8014fa0dbacaf71528ddda74ff83cf19ca3ca'
context:
  - '/home/administrator/projects/hexalith/platform/_bmad-output/specs/spec-platform/acceptance-criteria.md'
  - '/home/administrator/projects/hexalith/platform/_bmad-output/planning-artifacts/architecture/architecture-platform-2026-09-27/ARCHITECTURE-SPINE.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-16.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-12-sole-owner-qualification.md'
  - '_bmad-output/implementation-artifacts/27-4-bootstrap-recovery-result-2026-10-16.md'
  - 'docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json'
  - '_bmad/custom/story-scope-guard.md'
  - '_bmad/custom/epic-ac-verification.md'

---

# Story 27.4 — approved Platform ownership implementation

**Current instruction, 2026-10-16:** owner replied “approve” to the Platform correction in the existing proposal. This approval explicitly renegotiates the frozen environment intent in the historical sections below. This section governs current work. Parent remains `in-progress`; preserve original baseline and all past execution/approval evidence. The new preparation delta begins with the snapshot `/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-preparation-20261016T145211Z-review-fixes-6gwly3kt/preservation/baseline/before.json`.

## Intent

Deliver the smallest real PostgreSQL retention experiment through Hexalith.Platform's supported local run-owned environment. Platform owns infrastructure and lifecycle; Memories consumes its existing runner descriptor/result and verifies behavior. One existing story, verifier and consolidated report; no new framework, AppHost, runner, receipt protocol or numbered specification.

## Boundaries & Constraints

Implementation/offline preparation only: no workload, VM, container, AppHost, Dapr, cluster or server start/contact; no storage allocation/format, credentials/active permission change, dependency init/update, Git mutation, A41/sprint transition or Production. No server contact at 192.168.1.30; hosted attachment is refused by Platform FR4. Standing owner policy is already approved. Preserve previous closed and unused VM budget/decision bytes without consuming, withdrawing or transferring them.

Work from each owning repository using the selected standalone Platform workspace `/home/administrator/projects/hexalith/platform`. The initial observations — Platform `06964fb8afe68b5d6bf496bb29c915e2a489e446`, direct Builds `86b9f351db87b00504cc98e125b0951ef2f4fa67` and direct Memories `923ded6abdd3b2efcceb074292bf01b48cc6aabe` — are superseded by the **dated 2026-10-16T15:26:54.112019Z preparation reinspection** in the [single report](tests/27-4-retention-verification-evidence.md#source-reinspection-and-next-implementation--2026-10-16t152654z). That observation is not an execution source binding; reinspection of actual owning revisions/bytes and direct candidate alignment is required before relying on it for execution. Preserve all independent work. Memories submodule Platform remains `55bee680e6901a45946dd8bf1e4a4060952bd7ea`; do not update that gitlink or use sibling/ancestor/nested source fallback. This workspace's prepared Memories helper is not yet Platform's directly declared candidate; document actual source alignment as a preparation blocker, never pretend they ran there. Inspect applicable guidance, config and tests before edits; never load skills in references/. Explicit no-workload hold overrides generic Aspire startup. No staging to form review diffs.

Technical coverage is unchanged for the functional experiment: Dapr1.18.1/PostgreSQL18.6 immutable identities from existing catalog; actual startup probes, verify-full PostgreSQL TLS/identity, real signed clock/nonce/profile/expiry validation, normal logger/acknowledgement path, strong reads, due-index/reminder purge, real elapsed time, positive per-record attribution, younger cohort preservation. No Redis substitution, arbitrary command/SQL/URL input, SQL writes/deletes, fake time, backdating, mocks as live evidence, new public test endpoints, capability bypass or permissive Production profile. Local file-backed secrets/development UTC/Debug are disclosed assurance limitations; no OpenBao/Kubernetes/independent UTC/CI candidate/full qualification credit.

The approved functional limits remain **four hours of execution**, **15 minutes of bounded stopping/diagnostic capture**, and Platform’s **600-second default startup deadline** unless its supported configuration carries a justified finite override. At most **two submissions per cohort in total**; the second is allowed only to reconcile transport uncertainty using the same run/segment/time identity. No blind mutation retry, cohort-clock reset or deadline reset is permitted. Actual supported resources, retries, absolute window and lifetime must be bound before the single operational request; these retained limits grant no execution authority.

The original unchanged [Full qualification and close-out checkpoints table](#full-qualification-and-close-out-checkpoints) below remains the **current deferred technical obligation inventory**, including its owner/evidence/review/completion columns. Only that original table is incorporated: surrounding historical VM/KVM target, profile, bootstrap and operational-plan context is excluded from current environment authority. All full technical gates, real long-duration cohorts and terminal/publication obligations remain required; no duplicated table or functional gate credit is introduced.

## Code Map and verified readiness gaps

- Memories `tools/verify-access-telemetry-lifecycle.py` routes functional preparation/execution to `tools/access_telemetry_functional_retention.py`; semantic functions `validate_accounting`, `validate_cohort`, `validate_purge`, fixed key generation are reusable. Its `_run_sequence` currently owns VM/gate/Lease/bootstrap; keep as explicitly historical, do not wrap/rename it into a Platform runner. `tools/access_telemetry_functional_target.py` and `_guest.py` are VM-only history and must leave the active entry point. Preserve old source/evidence/tests in place with clear historical designation.
- Platform `src/Hexalith.Platform.Composition/{PlatformAppModel,PlatformDaprBindings,PlatformCompositionOptions,PlatformLocalProfile}.cs`, `DaprComponents/reference/telemetry-state.yaml`, existing composition tests and declarations. Current type resolution forces telemetry state.redis. Extend existing supported profile/options seam for real run-owned PostgreSQL only if the existing runner can actually select and describe it; don't edit generated declarations outside provenance workflow or change other consumers' default Redis backend. Inspect existing qualification capability configuration before proposing a local binding.
- Actual Builds implementation is in Platform's directly declared `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Runtime/`. `ModuleCommandExecutionService.ExecutePlatformAsync` explicitly rejects v2 `Test` with HXR028 (lines950–960); `NativeTestHandoff` currently supplies EventStore/tenant/resource identity only. `CompositionRunPlan`, `CompositionReadiness`, `CompositionRunState`, `RunEvidence/ModuleRunEvidence` are the existing schemas. Their existence alone does not supply the full Platform FR4 descriptor/result. Read existing owning runner implementation/tests before changing it. No new descriptor schema in Memories. A large undeclared runner implementation must remain an explicit owner gap, not a fake accepting stub.
- `tests/tooling/access_telemetry_functional_retention/` contains historical VM/semantic regressions and current boundary tests; actual counts are recorded in verification. Keep historical tests working and add meaningful no-side-effect current-route refusal/readiness tests; full tooling, deployment/privacy negatives remain.

## Tasks & Acceptance

Implement sequentially; one cohesive slice enables the Platform-owned test boundary. If inspection proves a prerequisite is absent and cannot be repaired within the approved existing runner contract without another framework, finish all independent correction work and retain a precise preparation blocker with owner, source, consequence and reopen check. Do not label blocked readiness as complete or ask for an unusable operational approval.

- [x] Adopt this actual approval in current Spec14, epics, architecture/ADR, runbooks/handoff and existing report; preserve historical bodies, approvals and source snapshots. Retire VM boot/resource floor as current functional prerequisites. Use the existing bmad-spec workflow for interchange kernel changes; preserve4CAP IDs and append-only memories. Do not mass-edit old numbered stories or sprint/A41.
- [ ] Inspect/bind actual Platform/Builds current source and direct Memories candidate; establish existing runner test/profile/descriptor contract and exact gaps. Add the smallest supported PostgreSQL profile/composition extension and consumer on existing seams where implementable. Do not introduce a schema, alternate command runner or infrastructure fallback to mask HXR028 or missing descriptor fields. Record source mismatch/tool pin/missing backend/gate configuration as demonstrated blockers where unresolved.
- [x] Make existing functional entry and preparation obey Platform ownership: no active QEMU/SSH/guest bootstrap, no old VM operational-plan acceptance or host RAM/disk authority. Consume only existing valid Platform evidence/descriptor/result when sufficient; otherwise reject before spawning/contact and return precise preparation gaps. Preserve behavioral validators and full-mode fail-closed semantics. Old private helpers remain explicitly historical/unreachable from the public functional entry; no old budget rewrite.
- [ ] Verify missing/foreign/stale/malformed/wrong candidate or backend, insufficient lifetime, ownership conflict, partial startup, withdrawal, cleanup failure and outcome policy against actual available contracts. Never claim an unimplemented live boundary was exercised by an offline double. Keep normal default full route strict; run affected meaningful offline checks only, no workload/daemon starts.
- [x] Observe unconditional refusal and preservation at the unsupported boundary: legacy bundles and caller-supplied negative samples cause no input reads, writes, spawn or target contact. Actual descriptor, current withdrawal and outcome/cleanup validation remain pending on the missing owning contracts; these checks earn no interoperability or live credit.
- [x] Update the one consolidated report with observed source identities, changes, exact offline results, owner gaps, completion checklist and next action. Complete all three configured preparation-delta reviews and their focused corrections; preserve user changes.
- [ ] Prepare exact resources/actions/retries/window and an executable operational bundle after the supported Platform inputs exist. The demonstrated engineering gaps still block this; no reduced RAM number or operational approval is invented.

Given a legacy VM bundle, when current functional execution/preparation is requested, then it refuses without target side effects and directs to Platform. Given missing/insufficient actual Platform capability, when preparation is checked, then it names the owning code gap with no ready/pass/authority claim. Given a complete supported local Platform run, when eventual operationally approved, then the experiment must satisfy every observed-behavior criterion in the approved proposal (125+125 records >=1800sec apart, A readable before3600sec and absent/index cleaned/reminder advanced by+900sec, B unchanged, UTC+monotonic, attributed deletion). Given failure/withdrawal/success, then Platform's real outcome-aware retention/cleanup result governs; full K8s terminal predicates remain later. Given any functional output, full C1/C0–C6/A41 consumers still reject it as full qualification.

## Dev Notes

### Historical Context Classification

| Source | Classification | Permitted use |
| :--- | :--- | :--- |
| Current Platform declaration/composition and existing Builds runner source | current-narrow-pattern | Verified ownership/configuration and refusal patterns only; missing PostgreSQL/test/result contracts remain blockers. |
| Prior VM approvals, failed attempts, unused budgets and retained source observations | historical-reference-only | Preserve decisions and evidence; never transfer them into current Platform execution authority. |
| Memories VM bootstrap and accumulated numbered-spec programme | anti-template | Historical evidence and requirement inventory only; never copy its infrastructure runner or whole delivery shape. |

### Slice Proof

One Platform-owned retention experiment and its consumer boundary. Missing runner capabilities remain existing-owner blockers; this slice does not create a new infrastructure framework or additional qualification story.

### Epic AC Verification

Verified2026-10-16 against the selected worktrees. The approved proposal's verified-claim table remains context. Current rechecks: `sed -n '930,1048p' /home/administrator/projects/hexalith/platform/references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Runtime/ModuleCommandExecutionService.cs` confirms HXR028 v2 test refusal; `cat .../Runtime/NativeTestHandoff.cs` confirms the limited EventStore handoff; `rg -n 'state.redis' /home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Composition/PlatformDaprBindings.cs` confirms telemetry Redis binding; `git -C <each owning root> rev-parse HEAD` confirms recorded checkout identities. These are confirmed preparation gaps, not reasons to weaken desired acceptance. Live retention/resources/readiness/cleanup remain unverifiable under the explicit execution hold; Platform and Memories own resolution, live acceptance is blocked, reopen after supported prep plus one operational approval.

## Platform preparation implementation — 2026-10-16

The independent repository corrections are implemented; live preparation remains **blocked**, not complete. The [single report](tests/27-4-retention-verification-evidence.md#current-platform-preparation--approved-2026-10-16) holds actual source identities, source-hash receipt, all owning gaps/reopen checks and offline verification. Platform/Builds advanced independently during inspection; all their work and unrelated concurrent Memories runtime changes are preserved. No Platform/Builds code, generated declaration or gitlink was edited by this implementation.

`tools/verify-access-telemetry-lifecycle.py --checkpoint functional-retention` now returns the existing preparation-format blocked output, exit 1, with recorded Platform/Builds implementation blockers, their dated inspection provenance and a mandatory reinspection requirement, plus zero target calls/spawns. Legacy execution/preparation arguments return the same refusal without reading plan/decision/status or writing a bundle. `_historical_prepare_vm_bundle` and `_historical_run_vm_retention` immediately refuse, preserving their original bodies below the refusal; `_run_sequence` accepts only explicit offline doubles. Target/guest helpers are marked historical. Semantic accounting/cohort/purge validators and full-mode consumers are unchanged. No caller-authored readiness/success/cleanup object can substitute for the missing runner contract.

The supported PostgreSQL profile and accepting consumer were **not** fabricated: Builds HXR028 v2 Test refusal, limited identity/ownership/result encoding and 15-minute test lifetime prevent a supported run; Platform’s Redis-only binding, disabled/placeholder local capability configuration and KVM-only workload gate add concrete gaps. Missing tool pin and direct candidate alignment are also proven. The report names their existing owning files and reopen checks. The next action is the existing Builds v2 test/descriptor/result boundary coordinated with Platform’s PostgreSQL/local-capability profile; no operational request is usable yet.

Current guidance in epics, architecture/ADR, runbooks/handoff and dated-plan notices follows Platform ownership. The interchange kernel and seven companions were derived under `bmad-spec` from four appended memory entries; original entries and CAP-1–CAP-4 persist. Prior VM report/history is retained verbatim in the same report. All six historical live tasks, parent status, A41/sprint and Production remain unchanged.

Current preparation File List (only this delta; earlier changed files remain historical/pre-existing):

- `tools/verify-access-telemetry-lifecycle.py`
- `tools/access_telemetry_functional_retention.py`
- `tools/access_telemetry_functional_target.py`
- `tools/access_telemetry_functional_guest.py`
- `tests/tooling/access_telemetry_functional_retention/test_functional_retention.py`
- `tests/tooling/access_telemetry_functional_retention/test_platform_boundary.py`
- this existing Spec 14 and `tests/27-4-retention-verification-evidence.md`
- `../planning-artifacts/epics.md`
- `../planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md`
- `../specs/spec-pg2-c1-authenticated-predecessor-interchange/{.memlog.md,SPEC.md,authority-and-sessions.md,owner-run-records.md,producer-bindings.md,implementation-tasks.md,schemas.md,sole-owner-policy-revision-draft.md,p1-p4-security-operations-decision-packet.md}`
- `../../docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`
- `../../docs/operations/{access-telemetry-lifecycle.md,access-telemetry-adapter-production.md,access-telemetry-owner-handoff.md,story-27-4-execution-plan-2026-10-15.md,story-27-4-isolated-target-plan-2026-10-16.md}`

### Earlier offline verification — not source-bound to corrected bytes

The earlier four-lane run reported **328 tests**, zero failures/errors/skips. Its receipt did not capture tested source hashes; it is retained as earlier offline evidence and is not attributed to the corrected current bytes. No workload, daemon, target or hosted service was started/contacted by these tests; retained local process/render doubles are offline evidence only.

| Exact command | Measured result |
| :--- | :--- |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_*.py -v` | 79 tests in 16.169 seconds; exit 0, zero failures/errors/skips. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_*.py -v` | 85 tests in 18.588 seconds; exit 0, zero failures/errors/skips. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/production_deployment_evidence -p *_test.py -v` | 123 tests in 237.036 seconds; exit 0, zero failures/errors/skips. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/tenant_isolation_evidence -p *_test.py -v` | 41 tests in 2.191 seconds; exit 0, zero failures/errors/skips. |

Raw logs, command vectors/environment, wall times, exits and SHA-256 values are retained at `/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-preparation-20261016T145211Z-review-fixes-6gwly3kt/earlier-offline-tests/commands-results.json`, SHA-256 `65a564c2280f374019764d2cfa610375a1ee87f46f057ef36a4706c60a40a544`. After diagnostic source paths were clarified, the six new boundary tests were repeated with `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_platform_boundary.py -v`: six passed in 0.033 seconds, exit 0, no skips.

The earlier temporary preservation checker passed historical report/dated-plan/proposal/bootstrap body preservation, all original memory entries/four CAP IDs and protected A41/sprint/D0 hashes. `GIT_OPTIONAL_LOCKS=0 git diff --check` passed. No C# or owning Platform/Builds implementation changed, so no runtime build/test or live FR4 boundary is claimed. The three configured preparation-delta reviews are recorded in the current Review Triage Log; post-review functional verification is recorded below separately.

### Focused review corrections and durable verification — 2026-10-16

The public preparation output labels its fixed inventory **recorded implementation blockers**, includes the exact 14:34:55 UTC inspection timestamp/revisions/receipt hash, and requires reinspection. It does not inspect or bind a supplied source root. The insufficient readiness negative sample now follows the owning contract’s camelCase shape; it is explicitly not a serialized interoperability test. Fresh helper/CLI imports execute under an active historical-import restriction, whose rejection is independently demonstrated. These remain unconditional refusal tests, not implemented descriptor/withdrawal/outcome/cleanup validation.

Only the affected functional suite was rerun after these fixes:

`PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_*.py' -v`

**79 tests passed in 13.188 seconds**, exit 0, zero failures/errors/skips. The new receipt captures SHA-256 values for all seven affected functional source/test files before and after the run; both inventories are identical. It binds the exact command/environment/result and raw log SHA-256 `8f0d87ead48a4c78870dedce9c07ad0716963aa201541421776dfe9c0bf2f107`. Unchanged lifecycle/deployment/privacy suites were not rerun. The earlier 328-test receipt is preserved honestly as earlier offline evidence without source attribution to these corrected bytes.

All new preparation evidence is retained in the **new private directory outside every old VM bundle**:

`/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-preparation-20261016T145211Z-review-fixes-6gwly3kt`

- `recorded-inspection/inspection.json`: original exact inspection bytes and SHA-256 `4e3476249245ac80f8725112ba4ce5c1251d24cffddfa9d2c221210a960f4943`.
- `earlier-offline-tests/`: original four logs and `commands-results.json`, whose raw SHA-256 remains `65a564c2280f374019764d2cfa610375a1ee87f46f057ef36a4706c60a40a544`; its internal historical paths are preserved verbatim.
- `review/reviewed-preparation.diff`: original 26-file **pre-fix** diff read by the three reviewers, SHA-256 `2bd81031c373550983629df5dcf0838e4b4879653a201e599bdbfd5382bd249b`. Subsequent corrections and the later triage annotation are not claimed within those reviewed bytes.
- `post-fix-functional/verification-receipt.json`: source-bound verification of the **subsequent corrections**, SHA-256 `5a97f15fa3ab83ae1fd2fbeefad046017db81213895d9d7bbcb6c3284f7311d8`; retained source bytes, before/after inventories and raw log are alongside it.
- `preservation/`: exact original checker plus a rerunnable checker and its retained baseline dependencies. Run `python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/platform-preparation-20261016T145211Z-review-fixes-6gwly3kt/preservation/checker.py /home/administrator/projects/hexalith/memories`; it has no `/tmp` dependency. The original temporary checker bytes remain separately preserved.
- `checks/`: exact scope input, scope/preservation logs and command/results. The required `python3 tools/check-story-slice-scope.py --changed-files-file /tmp/story-27-4-doc-scope.txt --story-key 27-4-retention-verification-operations-runbook-and-a41-close-out --require-record` passed: one story checked, exit 0. The canonical Source/Classification/Permitted-use rows and separate Slice Proof now satisfy its required shape. Durable preservation checks also passed.

New evidence files/directories were created privately and exclusively, with byte readback; no existing file permissions, credentials, approvals, status, budgets, disks or workloads changed. The original full checkpoint table remains current deferred technical obligations, with historical VM context excluded; actual supported contract validation, operational preparation, live behavior, A41/sprint and Production remain pending.

## Review Triage Log — Platform preparation delta

All three configured layers reviewed `/tmp/story-27-4-platform-ownership-review.diff`: edge-case and verification-gap returned no findings; blind review returned ten findings. The preparation-only review does not transition the parent, consume a budget or complete live acceptance. Each finding is recorded before grouping; direct corrections below reuse the approved intent, with no new approval or qualification gate.

| ID | Verdict | Evidence / disposition |
| :--- | :--- | :--- |
| P-B1 | medium | Public preparation output reports a fixed gap inventory without its dated inspection identity; the helper docstring alone does not label emitted facts. Patch recorded-observation provenance, explicitly requiring reinspection and granting no readiness. |
| P-B2 | medium | Current task four is checked despite absence of a live descriptor/outcome contract; report correctly disclaims that coverage. Correct the observed completion record as the user requires: refusal tests complete, actual contract checks pending. No acceptance or scope change. |
| P-B3 | low | Negative sample uses PascalCase while actual CompositionDocumentStore uses JsonSerializerDefaults.Web/camelCase. Tests do not claim a wire validator, but correct the sample and its description to avoid misleading future fixture reuse. |
| P-B4 | medium | Import restrictions are installed after imports; they cannot detect a future top-level historical import. Patch a fresh import under restrictions and prove that an attempted historical import is trapped. |
| P-B5 | low | First active source paragraph names earlier observations; newer report snapshot is dated but the relation is implicit. Label earlier IDs superseded and route preparation to one dated snapshot, never an execution binding. |
| P-B6 | medium | New inspection/test/checker receipts exist only in /tmp; cleanup would remove rerunnable preservation evidence. Retain exact receipts, checker dependencies and selected baselines in existing private evidence custody, keeping hashes and no active authority. |
| P-B7 | medium | Old test receipt does not identify tested source bytes; retrospectively assigning final hashes would be false. Preserve it as historical, run only changed functional checks against captured before/after hashes and retain a source-bound post-correction receipt. |
| P-B8 | medium | Only full checkpoint inventory is enclosed as historical while active kernel still points to it. Explicitly retain that unchanged table as current deferred obligations; VM context remains history. Do not duplicate or edit historical rows. |
| P-B9 | low | Approved 4h execution/15m stopping/600s startup/two uncertain cohort-submission bounds remain in loaded proposal but are not explicit in current constraints. Carry them forward visibly; no operational approval or resource figure is invented. |
| P-B10 | medium | Architecture banner clarifies ownership but neighboring graph/AD-1/AD-17/environment wording still permits ambiguous AppHost responsibility. Apply the same approved ownership distinction at those decision points, retaining runtime packages and full gates. |
| V-SCOPE | high | Required scope check failed: current Historical Context Classification has no table rows. Add canonical Source/Classification/Permitted-use rows and a separate Slice Proof, then rerun the same check; this repairs record format without changing approved scope. |

The user’s explicit no-Git/no-parent-transition and truthful observed-completion boundaries override generic workflow staging, rollback and review/done transitions. These are direct source, test, evidence and documentation corrections under the already approved ownership decision. No deferral-ledger entry or new story is needed.

### Review correction status — verified

P-B1 through P-B10 and V-SCOPE are corrected. Parent inspection verified the five corrected files, dated emitted provenance, camelCase negative sample/fresh-import restriction, truthful pending contract checks, current full-table incorporation and finite bounds, clarified architecture ownership/full parity, and durable custody. The source-bound 79-test receipt and all seven current source/test hashes match; scope, whitespace and historical preservation checks pass. No review finding is deferred. Existing Platform/Builds engineering prerequisites remain open and are not review passes or operational authority. Parent status remains `in-progress`, with no A41/sprint or Git transition.

## Bounded source reinspection — 2026-10-16T15:26:54Z

Current run began at `f7c8ce84a8a765667beafc31154ee2d6981552f8`; the original frontmatter baseline is preserved. The [single report's dated reinspection](tests/27-4-retention-verification-evidence.md#source-reinspection-and-next-implementation--2026-10-16t152654z) records full current owning revisions, exact commands/results and private source-bound evidence. Platform's direct Builds still refuses v2 Test/named profiles, lacks the needed handoff/outcome/lifetime contract, and its direct Memories still lacks this helper. These are existing-owner implementation gaps; no supported accepting consumer or usable PostgreSQL-only extension can be derived from the shipped contract.

The only source change refreshes the recorded preparation observation and adds each blocker's concrete contract limitation and next owning implementation. The existing boundary assertions cover that actionable refusal output. Six Python boundary tests and two owning Builds refusal tests passed; the no-restore Debug test-project build passed without warnings/errors. The actual CLI returned expected blocked exit 1 with zero target calls/spawns. No live test, accepting descriptor, runner, profile, dependency/Git mutation, parent transition or new operational authority is claimed. The unresolved implementation and operational tasks above stay unchecked.

### Review Triage Log — bounded reinspection delta

All three configured reviewers inspected the current four-file delta from `f7c8ce84a8a765667beafc31154ee2d6981552f8`; the original frontmatter baseline and previously accepted work remain preserved. Edge-case and verification-gap reviewers reported no findings. Blind-review findings were individually verified before grouping; all five are direct corrections to the actionable diagnostic/evidence handoff, with no deferred review finding. The parent remains `in-progress`; no sprint/status or Git transition is authorized by this preparation review.

| ID | Verdict | Evidence / disposition |
| :--- | :--- | :--- |
| R-B1 | low | `NativeTestExecutor` derives credentials from `_timeout + 5 minutes`; the earlier new diagnostic incorrectly implied otherwise. Corrected to explain that a global increase widens unrelated tests and credentials while module-specific authorization is absent. |
| R-B2 | low | The changed field-validation assertion could omit the descriptor-result entry or hide duplicate IDs during dictionary construction. Added an exact multiset assertion for the six current blockers before constructing the dictionary. |
| R-B3 | low | The two named receipt hashes did not explicitly bind the separate successful build receipt. Added the actual `builds-focused-build.json` filename/hash to the report; its retained log hash is inside that receipt. |
| R-B4 | low | Newly described limitations relied on owning source files missing from their source pointers. Added the v2 schema, native-test declaration, engine/status, options and plan paths, using Platform's direct Builds path where that pointer is relative to Platform. |
| R-B5 | low | The finite-lifetime handoff omitted the already approved observation/execution/stopping bounds. Added 3,600-second retention, up to 900-second purge grace, setup/observation overhead, four-hour execution, separate 15-minute stopping and unchanged default 600-second startup. No new authority or technical limit is introduced. |

Post-correction parent verification passed six boundary tests in 0.038s, expected CLI refusal with zero target calls/spawns, required slice-scope check and whitespace check. The unchanged owning C# source/assembly hashes and prior receipts match; historical bodies, frozen blocks and unrelated tracked Memories files are preserved. Exact final source-bound receipt and hashes are in the report's current reinspection section. Only this bounded diagnostic/preparation correction is verified; unsupported contract, operational and live/full acceptance tasks remain open. No commit or parent transition was made.

Current-run File List:

- `tools/access_telemetry_functional_retention.py`
- `tests/tooling/access_telemetry_functional_retention/test_platform_boundary.py`
- this existing Spec 14
- `tests/27-4-retention-verification-evidence.md`

## Previous implementation and operational history — retained verbatim

<details>
<summary>Earlier VM-based current specification and all original historical notes</summary>


# Story 27.4 — current execution specification

**Current authority, 2026-10-16:** The owner approved the [course correction](../planning-artifacts/sprint-change-proposal-2026-10-16.md) with “yes”. This amends the frozen intent below explicitly and retires competing execution specifications as active guidance. The parent remains `in-progress`; full qualification, A41 and Production remain held. Preserve original `baseline_commit`; the approved preparation delta starts at `61074e981bd92019488db19da0c8572bd4bf8b97`.

<frozen-after-approval reason="human-owned intent renegotiated by the approved 2026-10-16 correction">

## Intent

**Problem:** Repeated approval/inspection cycles and overlapping plans have not produced a live retention test.

**Approach:** Reuse the existing verifier, real Dapr/PostgreSQL lifecycle and one consolidated report. Prepare the smallest bounded functional run first. Keep the complete exact-profile C1/C0–C6 and A41 publication requirements as later checkpoints; do not create another numbered 27.4 spec or framework.

## Boundaries & Constraints

Standing policy `sole-owner-qualification/2026-10-12`, owner `github:user:6775094`, assurance `sole-owner/self-reviewed`, and the approved KVM target persist. Base profile SHA-256 is `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`; full workload SHA-256 is `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.

The first functional run uses one writer, two 125-record cohorts and actual 3,600-second retention, with cohort B about 30 minutes after A. Keep actual signed attestation validation and real elapsed time. Explicitly use the existing non-Production development UTC sources and source-built Debug images identified by digest; disclose the absence of independent UTC and signed CI/package/Production assurance. Hash all execution deltas; modified settings earn no exact-profile gate credit. Real PostgreSQL/Dapr/actor/reminder behavior, strong reads, index cleanup and newer-record preservation remain necessary. No fake time, backdating, direct SQL insertion/deletion, mocked backend, new public endpoint or bypass of runtime capability checks.

Implementation/preparation only is authorized now. No target contact, VM/workload start, package installation/update, disk allocation/format/write, source-cluster access, active permission/status change, fault, Production activation, A41/sprint transition or Git mutation. Reversible local source/document edits, offline tests and non-deploying build/preparation are authorized. Do not run Aspire under the baseline's generic startup instruction: the user's explicit no-workload hold takes precedence. Do not stage/commit/push, initialize/update dependencies, or manufacture an owner decision. Prepare exact plan inputs before asking for the single operational approval; preserve both closed budgets and pending boot-only status unchanged.

One current plan: this file. One executable entry: `tools/verify-access-telemetry-lifecycle.py`. One human results report: `tests/27-4-retention-verification-evidence.md`. Internal bounded logs/packets are evidence, not additional reports. All numbered predecessor specs remain historical in place; retain original bodies, review records and statuses.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Outcome | Failure Behavior |
| :--- | :--- | :--- | :--- |
| Prepare | Implementation approval, no operational approval | Inspect local inputs and prepare exact command/plan; offline tests clearly labelled | Zero target or active-permission effects |
| Hold | Missing/expired/changed/withdrawn run approval, unsafe input or drift | Functional entry refuses before affected process/target action | Content-safe reason and no pass |
| Functional | Exact operationally approved target, sources, actions, limits and unused budget | A/B run through real pipeline; A expires/purges within grace, B remains unchanged; actual cleanup observed | Stop, bounded owned-resource restoration, rejected/incomplete result |
| Full qualification | Complete eligible same-session C1 and separately approved full actions | Existing C0/C2–C4 semantics and distinct owner-accepted C5/C6 records | Functional packets never accepted as predecessors |
| Close-out | Accepted C0–C6, terminal proof and separate publication authority | Existing exact four-path transition and verified remote containment | A41 and Production remain held otherwise |

</frozen-after-approval>

## Corrected bundle — approved, held for resource admission

The closed run exposed a transport defect: hashing the full retained OS through WSL exceeded the existing 120-second preflight limit. The original implementation agent applied the narrow correction in `tools/access_telemetry_functional_target.py` and its existing offline test file. The fixed, already pinned native Windows PowerShell/.NET command opens only the exact OS file read-only with write/delete sharing refused, hashes all 42,949,672,960 bytes, and requires the same expected SHA-256, native host/size/full-read/unchanged metadata and Linux no-follow descriptor/path identity before and after. The command is bounded to 90 seconds inside the unchanged 120-second whole preflight bound. No tool, dependency, disk, resource/retry limit, retention assertion, image, full qualification requirement or technical coverage was reduced.

The exact corrected command was exercised as a **read-only local preparation diagnostic** and verified the complete expected hash and unchanged metadata in **38.722 seconds**. It did not activate a VM or qualification authority. The 7 new offline cases cover full acceptance, immutable command/time bounds, partial/truncated data, wrong host/hash, native/Linux identity drift and fresh authority denial. The complete affected suite passed **73 tests**; repeat existing lifecycle **85**, deployment **12**, tenant/privacy **17** all passed: **187 total, zero failures/skips**. Earlier three-layer review findings remain resolved; the measured OP1 correction was checked after the original implementation agent returned.

The sole next operational candidate is `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T130434Z-native/plan.json`, SHA-256 **`2cabeebf35b309bcf68adb4e12a71951af85d1c0daf2078954f848577b9f49b9`**. It has **36 prepared inputs**, **393,338,698 bytes** and **4,812 source files**. All source/input/private-custody checks and a fresh re-render passed. Target, disks, tool/image pins, actions, resource/retry limits and functional assertions match the closed plan; only the target-helper source hash and derived fresh preparation inputs/window change. No images were rebuilt. The owner replied “approve” to this exact corrected manifest. The actual decision was recorded at **2026-10-16T13:10:04Z** and its source/input/tool bytes were checked again. Approval is recorded while executable authority remains false and the budget unused pending resource admission. The earlier preapproval held invocation rejected with zero target calls/spawns and no status/claim change. The previous consumed budget stays closed.

Absolute UTC window: **2026-10-16T13:04:37Z** through **2026-10-18T13:04:36Z**, latest start **2026-10-18T08:49:36Z**. The actual recorded owner decision binds this exact manifest; it does not reuse the closed decision. No further approval is needed for this unchanged manifest within its current window, provided source/inputs/identity remain exact, status is not withdrawn and resource admission passes before its unused budget is claimed. The scope is the same one functional setup/two 125-record cohorts/real one-hour retention/owned restoration run, on existing 40/600-GiB disks with 8 vCPUs/40 GiB RAM, 20-GiB staging, four-hour execution plus 15-minute restoration, and at most two eligible launch attempts.

**Runtime admission is presently blocked:** the fresh pre-budget observation at **2026-10-16T13:10:22.028055Z** reports **19.22 GiB native Windows available RAM**, below the unchanged **56-GiB** start floor. Linux headroom and native disk allocation/free-space are separately checked; none authorizes bypassing native RAM admission. A scoped read-cache advisory on the unchanged task-owned OS file did not restore headroom; no global cache flush, host permission/configuration change or storage mutation was performed. Do not consume another run budget while the floor is unsatisfied. Resource admission must be observed again before the replacement is launched; preparation `ready: true` means inputs are ready only.

A second local observation at **2026-10-16T13:12:12.596381Z** still measured only **18.69 GiB native available RAM**. Its `admission-confirmation.json` confirms the budget remains unused.

The corrected invocation has **not started** and has no `run.claim` or `run` directory. Before claiming its budget, the local admission check verified unchanged retained disk identities, 40/600-GiB sizes and full native allocation; NTFS free space was **165.39 GiB**, and Linux available RAM **60.01 GiB**. Native RAM alone failed the unchanged 56-GiB start floor. No downloads, guest launch, formatting, workload, cleanup mutation, host configuration change, A41/sprint transition or Git mutation occurred. Approval, original held-status snapshot and the fresh `admission-before-budget.json` remain in the same private bundle; its budget is preserved for a later in-window start after headroom is restored.

Raw correction tests, corrected native full-hash observation, held denial, resource observation and validation hashes are retained under `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T130434Z-native/preparation-verification`. The exact invocation is retained in `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T130434Z-native/invocation.json`. No live retention or terminal API/full qualification/A41 pass is claimed.

## Approved operational run — 2026-10-16

The owner replied “I approve” to the exact operational request for manifest `fdbbd399e9cf25689f0c71c3e37a94cd9a8454187bf04fbd6a5a65d35c8510f4`. The actual decision was recorded at **2026-10-16T12:51:48Z** in the prepared private bundle. Source, prepared input and tool hashes were checked again before admission. The single bounded invocation ended rejected before boot; this grants no full qualification, A41, Production or Git authority. The preparation-only hold described in the retained preparation observations below is superseded only for this exact run.

Evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T124615Z`. Preserve the original plan, preparation denials, owner receipt, decision, status history and raw execution outputs. The observed result is recorded below; no live retention assertion passed.

**Observed outcome:** The exact prepared command ran from **2026-10-16T12:52:07.661128Z** to **12:54:16.628250Z**, exit **1**, `status: rejected`, reason **`functional-target-os-read-deadline`**. Full-disk read through WSL exceeded the 120-second preflight limit. This is a verifier preparation defect, not evidence against retention behavior. The exclusive claim is retained; status now has `budgetConsumed: true`, `executableAuthority: false`. It must not be reopened. No transport assets were downloaded, VM launched, data disk formatted, guest credential activated or cohort emitted. Fresh local checks found both retained disk identities/size/mtime/ctime unchanged and no owned QEMU process. No gate/Lease/replica cleanup is claimed because no guest/API setup occurred.

A separate **read-only local preparation diagnostic**, not a second qualification run, verified the unchanged full 40-GiB OS SHA-256 `074502ca51d9cb20a79cdabd715f5e06afbbbec298e19b84a6770104da6361de` through the already pinned native Windows PowerShell/.NET path in **37.596 seconds**. Its file stream denies writes/deletes while hashing. The first native diagnostic found `Get-FileHash` unavailable; its failed observation is retained. A narrow source correction will use the successful full-hash transport within the same existing time/resource limits; any replacement exact manifest remains inactive pending approval. Raw records are in the same evidence root (`execution-exit.json`, `functional-result.json`, approval/status history and `post-failure-native-*-diagnostic.json`).

## Code Map

- `tools/verify-access-telemetry-lifecycle.py`, `tools/verify_access_telemetry_lifecycle.py`: existing CLI/closed full validators; add a distinct `functional-retention` route without changing existing predecessor behavior.
- `tools/access_telemetry_producer_common.py`: fixed workload IDs, target commands, bounded execution, Lease/gate and cleanup; compose useful implementation rather than duplicating its framework. A private focused helper module is permitted if needed, but no second executable or general command engine.
- `tools/access_telemetry_c1_owner_{run,dispatch,custody}.py`, `tools/access_telemetry_c1_capture_streams.py`: existing private bounded current-file/stream primitives. Their inspection objects confer no execution authority by themselves.
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadRunner.cs`: existing one-second 125-record normal logger pipeline. `src/Hexalith.Memories.AccessTelemetry/Lifecycle/`: actual processor, Dapr store and actor reminder. `src/Hexalith.Memories.AccessTelemetry.Clock/Program.cs`: existing non-Production development-source option. No new C# feature is planned; inspect these for actual semantics.
- `deploy/kubernetes/overlays/qualification/`: reuse inert overlay; source/secret/image placeholders must become actual bound functional configuration privately, not permissive defaults in tracked Production files.
- Private root `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc`: read existing corrected boot inputs and supervisors without running them or printing secrets. Copy the frontend/backend drive correction into the source-bound path; old 30-minute timeout/auto-poweroff cannot support the approved one-hour scenario.

## Tasks & Acceptance

Current authorized preparation slice (mark only after actually done; parent completion is not implied):

- [x] Consolidate current instructions: amend Epic 27's administrative story-registration predicate to this approved checkpoint table; current report front section and kernel profile/policy; short owner handoff; current ADR/runbook tier clarifications. Add supersession notices to the two dated execution/target plans. Preserve their historical bodies and all old specs; no new numbered spec. Spec kernel changes stay within the already approved exact correction and preserve its memories/history.
- [x] Implement the bounded functional-retention route in the existing verifier, including fresh owner approval/status checks before actions, exact-source/input/tool and target/resource bindings, real cohort read/purge/newer-control assertions, finite retry/timeout/output accounting, and bounded restoration. Inputs may select approved artifacts/identities, never arbitrary executables, shell fragments, SQL or URLs. Preserve full-mode refusal/strict version dispatch.
- [x] Prepare the complete local operational bundle: exact runnable command, staged-input/build/image and setup hashes, target/disk identities, rendered functional deltas, absolute UTC and retry bounds, and restoration. Keep executable status false and decision absent. Do not present a stub/fixture, hidden arbitrary command, unsupported pin or placeholder as ready; record a concrete demonstrated preparation blocker if it cannot be resolved without live authority.
- [x] Run meaningful affected offline tests for denial before spawn/contact, changed/expired/withdrawn authority, exact operations/resources, retry reconciliation, failed semantic evidence, cleanup failure and functional/full separation. Existing tenant/privacy negatives remain required. Test doubles prove orchestration only. Inspect installed tools/local artifacts read-only; add/update no dependencies.
- [x] Update this file's implementation/verification/File List and the existing consolidated report with measured preparation results, exact commands, limits and remaining live gates. Do not claim live passes, set parent review/done or modify sprint/A41/protected history. Produce a reviewable diff and have the configured Build reviews examine the preparation delta, not all historic work since the old baseline.

Acceptance for preparation:

- Given absent or invalid actual operational authority, when functional execution is invoked, then no target subprocess/VM/cluster action is performed; source-safe refusal is observable.
- Given a valid exact plan in offline doubles, when the sequence is exercised, then all authorized stages, budgets, cohort identities/times and owned-resource restoration are enforced; malformed observations, skipped stages or failed cleanup cannot return a live functional pass.
- Given a functional result, when any existing full predecessor/terminal/A41 consumer is invoked, then it refuses; those full consumers remain closed until their real prerequisites are implemented and pass.
- Given the consolidated documents, when an operator follows current links, then this existing story and its one results report define the route, existing policy/profile decisions are reused, historical requirements remain recognizable and no operational decision has been invented.

## Operational envelope — pending separate exact-plan approval

| Scope | Proposed bound |
| :--- | :--- |
| Target | Host `DESKTOP-VIOG240`; only guest `memories-qualification-27-4`, UUID `2fe71a5c-bdda-4767-8780-8f27f5bcd520`; private context `owner@memories-qualification-27-4`; namespace `hexalith-memories-qualification` and explicitly listed guest-only infrastructure namespaces. No contact with `192.168.1.30` or ambient kubeconfig. |
| Compute/storage | At most 8 vCPUs, 40 GiB guest RAM; reuse only existing 40/600-GiB raw disks in `/mnt/c/Users/JeromePiquot/AppData/Local/Hexalith/Qualification/memories-27-4-2fe71a5c/`. No new disk, resize or replacement allocation. One 400-GiB PostgreSQL PVC within the existing data disk; total existing proposed volume allowances stay within 600 GiB. Requests are not proof of free space. |
| Resource floors | Before guest start: at least 56 GiB host available RAM and 150 GiB C: free. During run: at least 16 GiB host available RAM and 150 GiB C: free; retain the 400-GiB 70/80/90-percent boundaries. Stop admission on insufficient observed guest filesystem/volume headroom or aggregate resource limits. Never resize to make a failed check pass. |
| Data initialization | Permit one initial filesystem creation on the exact blank 600-GiB data disk, then mount/install only task-owned guest paths. Refuse an existing filesystem, unknown signature or changed disk identity; no format retry. OS boot/setup writes are explicit; never reformat the retained OS or discard existing evidence. |
| Installation and credentials | Exact hash-bound package/image/chart list staged on the host, transferred over the fixed private SSH channel, installed only in the guest. Permit fresh guest-only service accounts, Roles, Secrets, config and TLS/marker/clock material named in the manifest. No host permission changes, external account grants, source-cluster credentials, package-version updates or public publishing. |
| Network | Retain `restrict=on` and loopback-only SSH/API forwards `12274`/`16443`; no bridge/TAP or guest external egress. Functional mode uses the existing development UTC sources explicitly. Images/packages are host-staged; their downloads/cache, if needed, have an exact allowlist and a 20-GiB total staging cap within the host free-space floor. No general guest Internet enablement. |
| Workload/actions | Guest bootstrap/setup, read-only identity observations, configuration of one-hour retention, two fixed 125-record segments from one writer, existing startup probes, record/index reads, normal actor reminder purge, short gate renewals and restoration. No injected backend/Placement/Scheduler faults, capacity filling, backup/restore or allocator reclamation job in this first run. |
| Window | One operational approval expires 48 hours after its actual recorded UTC time. One execution may start within that period and must finish tests/setup within **4 hours of first spawn**, with **15 minutes reserved for restoration**. Compile absolute `notBefore`, `latestStart` and `expires` values so the entire 4h15 fits before approval expiry. Setup must finish within 90 minutes; otherwise stop. Existing 30-minute guest auto-poweroff/timeout settings must be deliberately replaced in the new plan; the old bootstrap supervisor cannot run this scenario unchanged. |
| Retry budget | At most **two VM launch attempts total**, each with a 10-minute readiness limit; a second only for a transport/readiness transient after verified stop. Same QEMU syntax/device/resource error stops the run. Read-only commands: at most three attempts, 2/5-second delays, 120-second aggregate command bound. Downloads: at most two attempts within the staging cap. Mutations: no blind retry; re-read UID/resourceVersion and reconcile the same intended object. Cohort emit: at most two requests with the same run/segment/time identity, only for transport uncertainty, and require exact accounting. No second cohort experiment or reset of elapsed time on semantic failure. |
| Observation/outputs | Poll at most every 30 seconds with per-query deadlines. At most 1 MiB per retained stdout/stderr stream and 256 MiB total run evidence, plus the separate 20-GiB staging cap. Exceeded bounds fail with a retained reason. |
| Stop and restoration | Stop tests on denial, drift, withdrawal, expiry, resource floor violation or failed semantic assertion. Separately preapprove up to 15 minutes of restoration after a stop: disable only the run-owned gate, release only its Lease, scale only its writer/lifecycle/clock to zero, retain evidence/storage, then stop only the UUID/PID/start-time/argv-matching guest. Never delete namespaces/PVCs/disks as cleanup. Unverifiable cleanup makes the run incomplete and blocks another launch. |

The corrected disk vector is `-drive file=...,if=none,id=qual-os,format=raw,...` plus `-device virtio-blk-pci,drive=qual-os,serial=memories-qual-os`, with the equivalent data pair. Serial is not a raw backend option. Preserve paths/UUID and exact installed tool bytes; do not run either failed vector. One independent host deadline must bound the guest even if the agent dies. API cleanup must be observed before shutdown; stopped QEMU alone is not gate/Lease/replica cleanup.

A single later operational approval covers the complete exact setup/test/restore plan. Material scope changes and exhausted budgets require new authority; ordinary in-budget retries do not. No repeat standing-policy approval. Full campaign continuation and its 72-hour/168-hour mismatch are later work, not this slice.

## Full qualification and close-out checkpoints

The following checkpoint table is explicitly approved as the existing story's ownership structure. It replaces additional story-file registration as a qualification predicate, never any technical gate. Each owner is Jérôme Piquot, with agent/tool collection. Pending/open means no live pass. Current `95aa…` KVM identity and eligible session are required; historical PG1/closed-window results do not carry forward. C1.24 precedes C1.23 and C1.25 under the existing acyclic dependency graph. Full C2 workload remains two writers, 30 minutes, 150,000 backlog; accepted records/s and component operations/s remain separate quantities. Full C3 retains real 1/24/168-hour cohorts, 24-hour allocator proof and 216-hour producer bound; no silent freshness/window extension. Production activation/publication remain separate.

| Gate | Required observed evidence / artifact | Owner | Review state | Completion state |
| :--- | :--- | :--- | :--- | :--- |
| C1.1 | `c1/C1.1.json`: real CRUD round trip | Jérôme Piquot | pending | open |
| C1.2 | `c1/C1.2.json`: post-write strong read | Jérôme Piquot | pending | open |
| C1.3 | `c1/C1.3.json`: matched/stale ETags and FirstWrite | Jérôme Piquot | pending | open |
| C1.4 | `c1/C1.4.json`: later-operation failure with no partial transaction | Jérôme Piquot | pending | open |
| C1.5 | `c1/C1.5.json`: effective component TTL | Jérôme Piquot | pending | open |
| C1.6 | `c1/C1.6.json`: actor state survives reactivation | Jérôme Piquot | pending | open |
| C1.7 | `c1/C1.7.json`: Placement/Scheduler disruption and reminder recovery | Jérôme Piquot | pending | open |
| C1.8 | `c1/C1.8.json`: actual record/request limits | Jérôme Piquot | pending | open |
| C1.9 | `c1/C1.9.json`: full two-writer throughput and latency evidence | Jérôme Piquot | pending | open |
| C1.10 | `c1/C1.10.json`: 150,000-record catch-up and drain bounds | Jérôme Piquot | pending | open |
| C1.11 | `c1/C1.11.json`: tenant/isolation denials before dependencies | Jérôme Piquot | pending | open |
| C1.12 | `c1/C1.12.json`: TLS verify-full and at-rest posture | Jérôme Piquot | pending | open |
| C1.13 | `c1/C1.13.json`: measured amplification/headroom and 1/24/168-hour admission | Jérôme Piquot | pending | open |
| C1.14 | `c1/C1.14.json`: cohort-attributable allocator reuse and deadline | Jérôme Piquot | pending | open |
| C1.15 | `c1/C1.15.json`: fresh current-session runtime/control-plane capture | Jérôme Piquot | pending | open |
| C1.16 | `c1/C1.16.json`: fresh component/backend identity and actual connection linkage | Jérôme Piquot | pending | open |
| C1.17 | `c1/C1.17.json`: deployed image/manifest/epoch/source identity | Jérôme Piquot | pending | open |
| C1.18 | `c1/C1.18.json`: node/storage reservation, headroom and operating cost | Jérôme Piquot | pending | open |
| C1.19 | `c1/C1.19.json`: declared PostgreSQL process/pod fault, zero acknowledged loss | Jérôme Piquot | pending | open |
| C1.20 | `c1/C1.20.json`: exact out-of-profile exclusions | Jérôme Piquot | pending | open |
| C1.21 | `c1/C1.21.json`: real backup destination and successful restore | Jérôme Piquot | pending | open |
| C1.22 | `c1/C1.22.json`: measured RPO/RTO and no-HA claim | Jérôme Piquot | pending | open |
| C1.23 | `c1/C1.23.json`: owner Operations acceptance of required parents | Jérôme Piquot | pending | open |
| C1.24 | `c1/C1.24.json`: owner non-HA acknowledgement | Jérôme Piquot | pending | open |
| C1.25 | `c1/C1.25.json`: owner security risk acceptance after required parents | Jérôme Piquot | pending | open |
| C0 | `c0/adapter.json`: exact approved adapter/source/input evidence | Jérôme Piquot | pending | open |
| C1 aggregate | 25 verified same-session packets plus distinct owner role records | Jérôme Piquot | pending | open |
| C2 | Existing `c2-production-replacement` producer packet, all fixed replacements/accounting | Jérôme Piquot | pending | open |
| C3 | Existing `c3-retention-reclamation` packet and complete journal, all three cohorts | Jérôme Piquot | pending | open |
| C4 | Existing `c4-failure-privacy-observability` packet, every fault/privacy/continuity lane | Jérôme Piquot | pending | open |
| C5 | Operations result record over actual C0–C4, runbooks, risk and cleanup | Jérôme Piquot | pending | open |
| C6 | Distinct Security result record over the same evidence, accepted with C5 in one final owner response | Jérôme Piquot | pending | open |
| Terminal | Actual disabled gate, empty Lease, zero desired/ready lifecycle/clock and terminated Pods, bound to the result set | Jérôme Piquot | pending | open |
| A41 publication | Existing inventory → recoverable preflight → exact mutation → postflight → authorized publication → remote containment proof | Jérôme Piquot | pending | open |

## Dev Notes

### Historical Context Classification

| Prior work | Classification | Permitted use |
| :--- | :--- | :--- |
| Original Spec 14, owner/target approvals and bootstrap failures | historical-reference-only | Decisions and evidence, with frozen intent explicitly renegotiated by this correction. |
| Accumulated Spec 1–26 and one-story-per-C1 registration programme | anti-template | Requirement inventory only; no new numbered specs or repeated review/approval shape. |
| Existing custody/stream primitives, fixed workload and Dapr lifecycle | current-narrow-pattern | Reuse focused code after verifying current semantics. |

### Slice Proof

One directly demonstrable outcome: a source-bound, bounded functional retention command prepared for a real isolated target, with its operational approval still absent. Documentation consolidation enables that same outcome; full qualification and closure remain the approved later checkpoint inventory.

### Epic AC Verification

Verified current source/configuration against `61074e981bd92019488db19da0c8572bd4bf8b97` and the approved preparation worktree on 2026-10-16. Proposed run outcomes and the 25-gate checklist are desired end states, not claims that live evidence exists. Earlier dated claims remain historical; new implementation claims must be re-derived before acceptance.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--- | :--- | :--- | :--- | :--- |
| “Retention must be finite and between one hour and seven days inclusive.” | behavioral | `cat src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryOptionsValidator.cs` | Exact bounded validator; live enforcement is still pending. | confirmed |
| “The exact per-writer record rate” is 125 and “one fixed one-second segment.” | quantitative/behavioral | `rg -n -e RecordsPerSecond -e SegmentSeconds src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadRunner.cs` | 125 records/s, one-second segment through normal logger path; no new endpoint required. | confirmed |
| “The sole approved PG-ONPREM-2 Story 27.4 profile hash.” | location | `rg -n ApprovedProfileSha256 src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs`; `cat docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json` | Runtime and manifest use the approved KVM `95aa…` pin. | confirmed |
| “Current consumers remain closed.” | behavioral | `rg -n -e 'current authority unavailable' -e authority-prerequisites-unapproved -e deployed-producer-binding-unavailable tools/verify_access_telemetry_lifecycle.py tools/access_telemetry_c1_authority_boundary.py tools/access_telemetry_c1_producer_bindings.py` | Explicit full-mode refusals remain a genuine implementation gap; functional routing must not weaken them. | confirmed |
| “Production lifecycle writes remain disabled.” | behavioral | `cat deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` | Both lifecycle/clock Deployments have zero replicas in repository configuration. No live target claim. | confirmed |
| “Actual Story 27.7 through Story 27.31 files must exist … be registered … and be done.” | policy | Read the approved 2026-10-16 correction and current Story 27.4 section in `epics.md` | Explicit owner decision replaces this administrative predicate with this checkpoint table, while preserving technical requirements. Source planning correction applied together. | corrected |
| “The approved current profile is … 7f9f…” in old report/kernel guidance | location | Read current report identity table and interchange `SPEC.md`, then compare the approved KVM manifest | Both current sections now point to `95aa…`; old report body/log pins remain history. | corrected |

The retained bootstrap disk allocation is a dated observed result, not a fresh preflight fact. Current disk/headroom/ACL and guest/API observations remain unavailable until the actual approved run; the agent owns the before-launch check and must stop on failure. No unverifiable live claim is used to authorize execution.

## Current implementation record

Approval recorded in the course correction; implementation delegated through bmad-build using this file, not a new spec. Standing policy and profile persist. User's operational/no-Git/no-closure boundaries override generic workflow staging, startup and status transitions. Parent sprint synchronization is a no-op at `in-progress`; preserve its bytes. Review the preparation delta separately without checking full live tasks or changing parent status.

### 2026-10-16 documentation consolidation

Applied the approved owner decision to current Epic 27 ownership, existing story, report identity/policy front section, operator routing and ADR qualification tiers. The interchange kernel and companions were derived under bmad-spec from six appended decision/validation entries; all four CAP IDs and 58 original memory entries remain. The two old execution plans have supersession notices; their dated bodies, the previous report/handoff bodies and all older numbered specs remain in place. Active instructions now route to this story and the one report.

`python3 tools/check-story-slice-scope.py --changed-files-file /tmp/story-27-4-doc-scope.txt --story-key 27-4-retention-verification-operations-runbook-and-a41-close-out --require-record` passed for this existing story. `python3 /tmp/story-27-4-consolidation-preservation/validate.py` passed local added links, CRLF, historical-body/approved-proposal preservation and stable kernel capability/log checks. This is preparation/document evidence only. Source/test/build/bundle verification remains in progress; full live checkpoints and parent status are unchanged.

### Functional preparation implementation

The existing CLI now selects the private fixed orchestration, target and guest helpers. They bind the source/input/tool bytes, real SDK-produced OCI archives, an immutable host-only asset catalog, corrected QEMU frontend/backend syntax, new inactive expiring SSH seed, fresh process/UUID/guest/context identity, an exclusive consumed-once execution claim, scoped gate/Lease changes and measured terminal API readback before owned guest stop. Current owner authority is re-read before actions; long streams check fresh controls and input metadata. A source re-render after operational admission prevents prepared manifest substitution. Inputs cannot supply executable, shell, SQL or destination choices.

The functional render deliberately differs from the full approved profile: K3s `v1.35.9+k3s1`; static local Retain PVs instead of OpenEBS provisioner qualification; one 400-GiB PostgreSQL claim, Redis 20 GiB, FalkorDB 10 GiB, file-backed TLS OpenBao 60 GiB, and three 17-GiB Dapr Scheduler claims (541 GiB declared total within the retained 600-GiB disk). These are requests/reservations, not hard filesystem quotas or physical free-space proof. Dapr 1.18.1 and PostgreSQL 18.6 immutable image pins, PostgreSQL SCRAM/verify-full TLS, scoped OpenBao secrets, real startup capability probes and signed clock validation remain. The OpenBao chart is retained as a pinned input; this functional setup uses the existing disposable service pattern rather than claiming chart/HA/platform acceptance. No exact-profile, at-rest, capacity, backup/restore, allocator, fault or Production credit follows from this setup.

The collector retains strong record/hash/expiry observations, exact expiry-bucket reads and durable actor reminder/purge progress. TTL attribution requires strong absence while the exact expired PostgreSQL tuple is still physically present. A disappeared tuple alone cannot establish actor deletion; unresolved per-record attribution cannot pass. Read-only corroboration never inserts/deletes records or drives purge.

Local source Debug OCI builds succeeded with SDK `10.0.401`, the pinned ASP.NET base `eca1355b1fefb0a93617b7d01749dc8ac821e0eadcedbcf7d64e4b59cad60104`, and recorded commands/output hashes. Server/lifecycle/clock manifests are `98658dbfe0932dfa9b9cd652668da725ecc1b83fe195a880bfb6ec7e985cfd77`, `fee3679ccd37b885782602601120ef4f6bb255388811aad782d506eda2aa335a`, and `9f94adfae6bbabdc02fbc84949ba599e6ddde5e832a4f6eeb57eb47c0c4a5bc6`. Source-reference packaging/Release/registry/daemon publication still refuse. The functional Server omits the full C4 curl artifact; its existing pinned asset mismatch remains a full-lane preparation issue, with no pin update or C4 credit.

`python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -v` passed 66 tests, including nonzero action time, per-block disk floors, denied/withdrawn/changed authority, exclusive budget, malformed observations/cleanup, real local render/TLS, bounded child streams, retry identities, TTL attribution and refusal by full consumers. Existing lifecycle tooling (85), deployment contract (12) and tenant/privacy (17) checks passed with no skips. These are offline checks, not live qualification. All three configured preparation-delta reviews completed. Their 21 individual findings are triaged below and corrected without reducing requirements; exact bundle compilation follows.

## Review Triage Log

Three configured reviewers examined the preparation delta from `61074e981bd92019488db19da0c8572bd4bf8b97`; the parent remains in-progress because live/full checkpoints are excluded from this preparation approval. Each finding is recorded individually before grouping. These are local corrections within the approved intent; no frozen requirement is reduced.

| Finding | Verdict | Route | Verified evidence / correction |
| :--- | :--- | :--- | :--- |
| BH1 | medium | patch | New certificate/CSR/serial files are created after the chmod pass; private inventory rejects mode0644. Normalize the new bundle files after rendering. |
| BH2 | high | patch | Debug source references and imported root build inputs exist outside source_files; the current provenance omits demonstrated build inputs. Include permitted resolved dependencies and rebuild provenance. |
| BH3 | high | patch | Waits use UTC while monotonic is only an upper bound; a forward step can satisfy the lower retention bound early. Require elapsed monotonic intervals and reject clock discontinuity. |
| BH4 | medium | patch | startup_probes performs three bounded mutations/rollouts yet is classified as a 120-second read. Remove it from retryable read classification; retain setup deadline. |
| BH5 | medium | patch | That same read classification replays scaling after transport uncertainty. Stop/reconcile restoration instead of retrying the mutating startup method. |
| BH6 | high | patch | Guest fileDigests omit the downloaded executable/chart/airgap and third-party OCI content. Verify fixed download hashes and complete OCI content linkage at consumption. |
| BH7 | high | patch | Long setup children check authority/input drift but not resource floors. Add bounded monitoring during the already admitted operation. |
| BH8 | medium | patch | The caller dereferences missing transport after concrete missing-tool blockers, losing the blocker. Return a structured blocked preparation result. |
| BH9 | high | patch | OpenBao unseal key appears in Kubernetes exec arguments. Send the unseal request payload through protected stdin. |
| BH10 | medium | patch | Guest node wait retries all RuntimeError values for 300 seconds. Replace with an explicitly bounded readiness watch; do not retry semantic failures. |
| EC1 | high | patch | QMP quit is outside its handler; failure bypasses verified PID termination. Always reach identity-checked TERM/KILL fallback. |
| EC2 | high | patch | Retained disk identities are checked only before potentially long staging. Revalidate immediately before QEMU spawn. |
| EC3 | high | patch | Same demonstrated unchecked guest-consumption path as BH6; downloaded transport bytes need final consumption checks. |
| EC4 | medium | patch | Same startup read classification as BH4/BH5 causes both false 120-second refusal and mutation replay. |
| EC5 | high | patch | Gate disable and scaling verify objectUID but not the current Lease holder. Recheck ownership before every restoration mutation, including after release. |
| EC6 | high | patch | Preparation copies archives before aggregate capacity/free-space checks. Enforce the existing limits before and throughout writes. |
| EC7 | high | patch | Same demonstrated missing reference/build closure as BH2; Directory.Packages.props imports Hexalith.Builds input outside the current map. |
| VG1 | medium | patch | The filed searches establish no CI discovery of the new suite. Register it in the existing CI path with its local render prerequisites. |
| VG2 | medium | patch | The filed tests invoke guards directly and cannot detect disconnected BeforeTargets hooks. Add harmless publish/pack entry-target marker fixtures. |
| VG3 | high | patch | The filed searches establish no execution test of the formatting safeguard. Add sandboxed disk-identity/blankness/single-attempt tests without real devices. |
| VG4 | medium | patch | Authority tests use real datetime.now against a fixed 2026 window. Pin the fixture clock, preserving explicit expiry-negative cases. |

Operational follow-up **OP1 — high / patch**: the observed 120-second WSL OS-read deadline rejected the approved run before boot. Corrected only its full-read transport, with 7 meaningful new offline checks and the actual fixed native command read-only verification. The same expected full hash, fresh identity/authority, 90/120-second bounds and all resource limits remain; the original consumed budget is preserved.

Shared-root groups: BH2/EC7; BH4/BH5/EC4; BH6/EC3. All other findings are distinct. No finding is deferred.

All 21 findings were addressed. The 66-test affected suite passed in 13.698 seconds with no skips. New checks cover harmless actual MSBuild entry targets, sandboxed format identity/blankness/interruption, complete OCI/download consumption integrity, foreign Lease refusal and QMP failure fallback. Long setup operations monitor resources; startup is never replayed as a read. Both monotonic elapsed intervals and UTC observations are required. The fixed node-readiness watch stays within 120 seconds. Private generated files now satisfy custody permissions. Preparation writes enforce aggregate/free-space limits and retain concrete missing-tool blockers.

The source closure includes 4,812 Memories and resolved root dependency/build inputs, pruning skills and nested references without initialization. Existing build outputs were preserved at `/tmp/story-27-4-functional-source-build-snapshot-20261016T122910Z`; the same three fixed local Debug commands rebuilt the images successfully. The refreshed provenance receipt SHA-256 is `e1b45d0d10f0837dee190b197d2cd1ca29f543076e382557c56d0f90b0a98a8f`. No live gate or independent review claim follows from these checks. OpenBao unseal now uses the existing JSON-stdin write interface ([versioned source](https://github.com/openbao/openbao/blob/v2.6.4/command/write.go)); unseal material is absent from exec arguments.

The review scope was the approved preparation delta, including its new files, not older work since the preserved historical baseline. The skill's generic parent review/done, sprint, commit and deferred-ledger steps are overridden by the explicit user hold; no such transition or Git mutation occurred.

The final local volume check added `fsGroup: 1000` and `fsGroupChangePolicy: OnRootMismatch` to the functional Redis/FalkorDB pods, preserving their seccomp/capability controls and requiring no privileged init. The affected 66-test suite passed again. The earlier inactive bundle `functional-preparation-20261016T123800Z` and its verification remain preserved; the regenerated bundle below is the sole candidate for the next operational approval. The earlier corrected boot-only draft remains unapproved historical evidence and is not reactivated.

### Preparation snapshot — approved bundle subsequently closed before boot

At preparation time, the existing CLI preparation command completed successfully. Derived operational manifest `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T124615Z/plan.json` has SHA-256 `fdbbd399e9cf25689f0c71c3e37a94cd9a8454187bf04fbd6a5a65d35c8510f4`. Its 36 prepared inputs total 393,338,669 bytes; source closure contains 4,812 files. All input hashes/private custody checks and a fresh source re-render passed. `ready: true` means local preparation is complete. The actual owner decision is absent, executable authority is false and the unused budget remains unclaimed. A real invocation against this held bundle returned exit1, `functional-private-input-unavailable`, zero target process spawns/calls, and left the status/claim unchanged.

The absolute UTC window is **2026-10-16T12:46:18Z** through **2026-10-18T12:46:17Z**, with latest start **2026-10-18T08:31:17Z** so the complete 4h15 fits. The actual operational decision must be recorded when approved, never backdated. Its remaining approved lifetime may be shorter than 48 hours. No old budget or pending boot-only decision is reactivated.

One operational approval is requested for this exact manifest: fixed allowlisted host fetches/staging up to 20 GiB; corrected guest boot (two 600-second launch attempts only for eligible uncertainty after verified stop); one initial format of the exact blank 600-GiB data disk; guest-only cluster/services/scoped credentials; one writer's two 125-record cohorts with actual monotonic one-hour retention; and owned gate/Lease/three-deployment/API cleanup followed by verified guest stop. Reuse the 40/600-GiB disks and 8-vCPU/40-GiB guest limits; retain all existing resource floors, 90-minute setup, 4-hour execution and 15-minute restoration bounds. Separate full qualification/A41/Production scopes remain held.

Exact prepared invocation (still refuses while authority is absent):

```bash
/usr/bin/timeout --signal=TERM --kill-after=900s 14400s /usr/bin/python3.14 -B /home/administrator/projects/hexalith/memories/tools/verify-access-telemetry-lifecycle.py --checkpoint functional-retention --functional-plan /home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T124615Z/plan.json --approved-plan-sha256 fdbbd399e9cf25689f0c71c3e37a94cd9a8454187bf04fbd6a5a65d35c8510f4 --evidence-root /home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T124615Z --repository-root /home/administrator/projects/hexalith/memories
```

Raw offline verification is retained under `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T124615Z/preparation-verification`. The final repeat checks passed **180 tests** (66 functional, 85 existing lifecycle, 12 deployment, 17 tenant/privacy; no failures or skips). These packets support this sole human results report and provide no live checkpoint credit. The three review layers completed; all 21 findings were fixed and the affected checks rerun. No deferred ledger, sprint/A41, old active permission or Git mutation was performed.

## File List

- `.github/workflows/ci.yml`
- `Directory.Build.props`
- `Directory.Build.targets`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md`
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-16.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/.memlog.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/SPEC.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/authority-and-sessions.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/producer-bindings.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/schemas.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/sole-owner-policy-revision-draft.md`
- `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`
- `docs/operations/access-telemetry-adapter-production.md`
- `docs/operations/access-telemetry-lifecycle.md`
- `docs/operations/access-telemetry-owner-handoff.md`
- `docs/operations/story-27-4-execution-plan-2026-10-15.md`
- `docs/operations/story-27-4-isolated-target-plan-2026-10-16.md`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.QualificationCurl.targets`
- `tests/tooling/access_telemetry_functional_retention/test_functional_retention.py`
- `tests/tooling/access_telemetry_functional_retention/test_functional_target.py`
- `tools/access_telemetry_functional_guest.py`
- `tools/access_telemetry_functional_retention.py`
- `tools/access_telemetry_functional_target.py`
- `tools/verify-access-telemetry-lifecycle.py`

## Artifact disposition

| Artifact | Current disposition |
| :--- | :--- |
| Existing Spec 14 | Sole current execution specification; historical body below preserved. |
| `spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md`, numbered `-2` through `-13` and `-15` through `-26`, and the readiness/prerequisite/strict-predecessor supporting specs | Archived in place; original statuses, reviews and evidence preserved. Requirement inventory only, never competing active execution plans. |
| Dated 2026-10-15 execution plan and 2026-10-16 target plan | Supersession notice added; dated bodies retained unchanged. |
| Owner/target approvals, bootstrap/recovery results and private failed/pending plans | Retained unchanged. Existing policy/profile approval persists; consumed budgets stay closed. |
| Interchange kernel and companions | Maintained from append-only memory as technical contracts subordinate to this execution plan. No new framework/spec family. |
| Owner handoff / lifecycle runbook / PostgreSQL appendix | Short owner route / operating procedures / adapter procedures; links point here and to the one report. |
| Canonical report | Current identity/policy/result matrix first; older front matter and dated receipts retained as history. |

## Historical Spec 14 notes — retained verbatim below

The following is the pre-correction body, retained for history. It is not current execution guidance and cannot reinstate D0, old approval ceremonies or old competing plans.


<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 needs an owner-approved bounded run, complete PG2 C1 predecessor, live C2–C6 evidence and A41 publication proof.

**Approach:** Use the adopted sole-owner policy, prepare one bounded run record for owner approval, collect and verify C1 and qualification evidence, then record owner acceptance and verify A41 publication.

## Boundaries & Constraints

**Always:** Bind authority and evidence to approved `PG-ONPREM-2` SHA-256 `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. Check current owner approval, expiry and withdrawal. Require 25 verified C1 gates in one eligible session, explicit fault/purge scopes, distinct owner-accepted C5/C6 records, and disabled gate, empty Lease and zero lifecycle/clock replicas at finish. Use private owner-controlled hashed/readback evidence for non-Production qualification; separate receipt services, independent human reviewers and external immutable custody are not required for this mode.

**Never:** Treat policy adoption, historical PG1 C1.15, closed-window C1.16, fixtures or role labels as a live run approval. Do not claim independent human review for the sole owner’s decisions. Contact an unapproved target, enable Production writes, weaken predecessor refusal, expose secrets, change the zero-replica overlay, or resolve A41 and its sprint action before publication proof.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Outcome | Failure Behavior |
|----------|---------------|------------------|------------------|
| Hold | Missing, expired, changed or withdrawn owner run approval or C1 | `RECHECK_ONLY`; no target or A41 mutation | Name the missing fact |
| Qualification | Current owner run approval, verified C1, scoped C2–C4 actions | Hash-bound packets prove acknowledgement, retention, faults, audit continuity and privacy denial | Reject partial proof and restore disabled/empty/zero |
| Close-out | Owner-accepted C0–C6, terminal proof and publication approval | Exact A41 transition, protected bytes intact, remote containment proven | Reject drift or missing approval |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — current owner handoff; earlier institutional response request is historical.
- `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — authority and predecessor refusal; preserve fail-closed behavior.
- `tools/verify-access-telemetry-lifecycle.py`, `tools/access_telemetry_c{2,3,4}_producer.py` — fixed scenarios and close-out commands; use `docs/operations/access-telemetry-lifecycle.md`.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical C0–C6 matrix.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — prepare the owner run record with actual target, private evidence root, exact actions and windows; obtain scoped owner approval.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — verify PG2 C1.15 renewal, C1.16 eligibility or recapture, 23 other successors, 25 distinct gate passes and distinct owner Operations/Security records.
- [ ] `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — implement/verify migrated owner-mode interchange accepts the eligible predecessor; preserve refusal while absent.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — with the approved non-Production action scopes, run C0/C2–C4 and retain hash-bound packets and cleanup proof.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — record owner-accepted C5/C6 and terminal proof.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — apply preflight-approved A41 changes; verify postflight and remote publication.

**Acceptance Criteria:**
- Given missing, withdrawn, expired or changed owner run approval, when readiness or a producer is invoked, then it refuses before target access and leaves Production writes and A41 unchanged.
- Given accepted same-session C1 and scoped qualification approval, when C0/C2–C4 run, then hash-bound packets prove required lifecycle behavior and final disabled/empty/zero state.
- Given accepted C0–C6 and terminal evidence, when close-out runs, then only registered A41 paths change, protected history stays byte-identical, and the published commit contains the approved transition.

## Implementation Notes

2026-10-12: Platform P1 v1 infeasibility at `e229df9a58ee6899f2b24ba51f652435bc36dc62` (`docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md`, SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`) is recorded in the I2 request. The C1.15-only packet is not enrolled. The owner-choice and operational-record blocker is `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/c1-15-only-signed-evidence-packet-decision.md`, SHA-256 `d85537fbbf3a50eeff913a6c3363fbd3f0046ae112b0ce3d1f2b37cbce36eb04`. The first execution task stays open because no adoption, target, custody, principal, or grant record was returned. Disposition remains `RECHECK_ONLY`.

2026-10-12 current-pin recheck: Memories HEAD `f4c7d68f619f376debca440387428139a61080cc` records Platform `977f2e01347226ac727b707aee4f140f5dc8b010`, which contains the same P1 v1 infeasibility report and unchanged cited contract/qualification blobs. The [response audit](pg2-c1-i2-operations-security-request.md#2026-10-12-current-pin-response-audit-for-story-274-spec-14) records that no authenticated operational response, current status/revocation proof, accepted eligible C1 or separate action grant was supplied. Preserve the refusal and `RECHECK_ONLY` disposition. All six execution tasks remain open; live qualification, independent decisions, terminal proof and A41 publication remain pending. No target or A41 mutation occurred.


### 2026-10-15 authorized operational continuation

The owner explicitly resumed these six live tasks, with completed Spec 25 as a
dependency, and requested standalone preparation, missing collector integration,
actual same-session C1 and C0/C2-C6, terminal cleanup, governed A41 publication,
and a separately approved Production rollout. Standing sole-owner approval
persists; never ask for it again. Actual target/action/window approval and
post-execution result decisions remain separate. Preserve protected history and
mark no closure without its required live/publication proof.

Current preparation and the concrete downstream plan are in
[execution plan](../../docs/operations/story-27-4-execution-plan-2026-10-15.md).
A real standalone checkout, without initialized Git links, passed the existing
owner safety preflight and isolated revision/clean check. Exact source and
guidance snapshots are retained in the private evidence root named in that plan.
No cluster contact or live passing evidence has occurred in this continuation.

Named immediate operational blocker **D0**: fresh bounded read-only discovery
needs a source-bound collector that consumes outside-approved exact plan bytes,
current local approval/withdrawal, expiry, credential metadata and local API/CA
identity, and executes only the prepared twelve metadata projections. Implement
only this directly usable collector integration while run approval is missing;
do not broaden into speculative gate implementations without actual target facts.
Use `tools/access_telemetry_owner_discovery.py` and focused tests under
`tests/tooling/access_telemetry_owner_discovery/`. It must not create approval,
relax the existing owner/predecessor APIs or confer gate acceptance. It accepts
unknown target classification only for this exact metadata-discovery scope.

Read the private `discovery-plan.json` named by the execution plan. Preserve
the twelve fixed query vectors and bounds. Bind runner bytes into that plan
before final owner presentation. Require an outside approved plan digest and a
separate actual owner-decision record binding the plan and runner, plus freshly
read matching withdrawal state before each command and at final readback.
Absent approval, withdrawn/mismatched/malformed/private-path drift, expired
window, source or local API/CA/credential metadata drift must refuse before
network contact. Never follow a symlink for plan/decision/status/credential
inputs; the actual credential file is canonical, whereas `.kube/config` is a
symlink and must not be used. Credential values never enter evidence.

Retain command IDs/argv, UTC start/end, exit codes and bounded raw projection
stdout/stderr in exclusive private files, with hashes and readback. Use fixed
Kubernetes `get` vectors only; no exec, log, Secret, writes, retry loops or
ambient alternate context. Enforce aggregate/per-command time and output
bounds, close children on failure, and mark failures/NotFound as observations
requiring disposition, never qualification passes. Metadata identity/classification
observations cannot themselves authorize later mutations. Test denial-before-
spawn, exact allowed command selection, expiry/withdrawal/drift and retained
receipt behavior using offline test doubles; label them as offline verification.

After D0 preparation, report remaining gates and approvals concretely. Do not
contact the cluster without the owner's fresh discovery-plan approval; no such
approval has yet been supplied. Full live qualification remains pending. The
existing 72-hour owner collection bound cannot cover C3's 168-hour observation;
prepare explicit phase/renewal handling without weakening either technical bound.
Do not stage, commit, push, change branches/dependencies, mutate sprint/A41 or
Production during this preparation. Leave the six live tasks unchecked.

## Spec Change Log

- 2026-10-16: Owner explicitly approved the exact isolated KVM bootstrap and target-profile replacement after reviewing both hashes. Frozen current PG2 hash changes to the approved isolated-target manifest; technical gates, workload, history protection and separate Production authority remain unchanged. The first bounded bootstrap stopped at its OS allocation timeout and proves no qualification pass.

- 2026-10-12: Explicit owner “yes” adopts `sole-owner-qualification/2026-10-12` and renegotiates the frozen approval/custody requirements under the approved sprint change. Same owner may produce and accept evidence with mandatory self-review disclosure. Technical checks and live/publish scopes remain required. Prior implementation notes are dated history under the superseded policy; they do not reinstate institutional prerequisites.

## Review Triage Log

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — authority refusal tests pass.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — lifecycle and A41 guards pass.


2026-10-12 executed results: the authenticated-interchange suite passed 242 tests in 85.213s and the lifecycle suite passed 83 tests in 34.426s, both exit 0 with bare `OK` and zero failures/errors/skips. Local receipt `/tmp/story-27-4-spec14-oo85qkwd` retains commands, logs, exits, source/dependency identity and byte baselines; durable identifiers and the offline-only boundary are in the [canonical evidence addendum](tests/27-4-retention-verification-evidence.md#2026-10-12-spec-14-current-pin-readiness-hold). `git diff --check` exited 0 before these notes. The hold row is verified by refusal tests; no authorized live qualification or close-out row is claimed as executed.


2026-10-12 owner-mode first slice: the separately tracked
[owner-record inspection spec](spec-pg2-c1-sole-owner-run-record-inspection.md) is
done after three review layers, corrected timezone regressions and 25 focused /
267 full affected tests, with no deferred review finding. Policy adoption and
pure inspection do not complete the six live tasks above; existing deployed
consumers remain closed. See the canonical evidence addendum for exact results
and preservation proof.


2026-10-12 owner approval/acceptance continuation: the owner said “I approve and
accept”, accepting the sole-owner approach and presented 25 focused / 267 full
offline test results. The
[acceptance record](27-4-owner-approval-and-acceptance-2026-10-12.md) retains
exact snapshots in a private readback-checked archive. Bounded read-only queries
reached the configured cluster: the existing telemetry deployment has one ready
replica and the isolated qualification namespace is absent. No target mutation
or live gate acceptance occurred. Future collectors, full owner-mode integration
and concrete mutation setup remain agent implementation work; no repeat policy
approval or institutional grant is requested.


### 2026-10-15 D0 preparation implementation

Implemented only the authorized immediate D0 slice in
`tools/access_telemetry_owner_discovery.py`, with focused offline cases in
`tests/tooling/access_telemetry_owner_discovery/test_owner_discovery.py`.
Existing owner/predecessor APIs are unchanged and all six live tasks remain open.
The collector requires the outside exact plan digest, a separate actual owner
plan/runner decision and fresh matching withdrawal status; absent approval,
expiry, drift, unsafe paths or widened vectors refuse before a target query.
Unknown target classification is accepted only for these twelve metadata reads.

The runner separately binds its uncommitted bytes, rather than claiming them
inside baseline commit `8cc8fda7`. Current runner SHA-256 is
`71deee01aacdf88cc27bdb6113ed3dd7e0b8564c45e6f3a9dbccdedca49504d5`;
proposed plan SHA-256 is
`25adcae1ae4aa35c596bf6d121de1f0235e8ac19368cb1b7c7bad2cb22dca6d5`.
The plan retains the original twelve vectors/bounds, adds exact credential
metadata and fixed profile/workload pins, and leaves approval absent. Current
closed status matches that plan and is `withdrawn: true`, with an all-zero
missing-decision sentinel. No executable approval was created.

Canonical credential bytes are locally validated and passed only through a
sealed anonymous read-only input to the installed client. A pathname replacement
at spawn cannot redirect the API/credentials consumed by that child; named-file
drift then prevents a subsequent query. Owner-only regular credential-file
privacy is enforced without changing its public parent directory. Inputs and
ancestors are acquired without following symlinks. Raw bounded projection streams,
argv/IDs, UTC times, exits and hashes receive exclusive private creation and exact
readback; all observations require owner disposition and grant no qualification
credit. Child groups/pipes close on timeout, overflow, interruption and failure.

The execution plan documents the runnable invocation, closed plan/decision/status
formats, current missing approval, later concrete gates and a proposed C3
60-hour phase/renewal schedule. That schedule is non-executable: existing 72-hour
owner, C1/P7 eligibility/freshness, 168-hour cohort, 24-hour reclamation and
nine-day producer bounds remain unchanged; separately implemented/verified
continuation and actual phase authority are still required. It cannot extend an
expired grant, refresh C1 evidence, relabel a session or reset cohort clocks.

Named denial evidence includes
`test_missing_actual_decision_denies_before_process_or_target`,
`test_exact_twelve_queries_no_secret_log_exec_write_or_foreign_namespace`,
`test_local_api_ca_context_exec_proxy_or_external_key_drift_refuse`,
`test_mutable_credential_replacement_cannot_redirect_spawned_child`,
`test_mid_command_runner_source_drift_stops_before_second_command`, and
`test_terminal_status_is_checked_after_receipt_readback`.
These attach fail-closed target/namespace/attribution evidence to the new discovery
authority surface, preserving the Story 20.2 denial-before-dependency and Story
24.3 verifier-marker boundaries. Tests are offline doubles or local processes;
they prove no live gate, C1 registration, acceptance, terminal or publication result.

Actual proposed-plan denial is retained at
`evidence/discovery/missing-approval-refusal-1b752f9deaa543c099ccafb0b60ab55a.json`
under the execution plan's private root: `owner-discovery-owner-decision-required`,
zero process spawns and zero target calls. The supplied test digest was a
preparation binding, explicitly not owner approval. The independently evaluated
local `kubectl config view` fixture reads the dotted environment/gate JSON keys
and produces literal tabs/newline. Its namespace projection returns exact hex
`66697874757265097569642d666978747572650954657374095175616c696669636174696f6e0a`.
That local-only evaluation uses an inert private config, never target credentials.
No cluster was contacted by this implementation, no A41/sprint/Production bytes
changed, and no stage/commit/push/branch/dependency operation occurred.


D0 preparation verification completed at `2026-10-15T16:54:51Z`. All commands
below exited 0 with bare `OK`, zero failures, errors or skips. These are offline
verification results; the parent's later bounded owner-approved discovery has
its own actual decision/status and command receipts.

| Exact command | Measured result |
| :------------ | :-------------- |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_owner_discovery -p 'test_*.py' -v` | 31 tests in 4.132s. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` | 83 tests in 23.727s. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` | 640 tests in 463.820s. |
| `git diff --check` | Exit 0 after the implementation/verification notes. |

The execution plan's private evidence root retains exact raw combined logs,
commands, exits, counts and hashes in
`logs/d0-verification-20261015T165451Z-a4513ab3/commands-exits.json`, SHA-256
`a6b51ca3d76e22f33a94904a9a5724e13e9f40af3b9ba7c4a96be13bdb5e66ed`.
The focused/lifecycle/interchange log SHA-256 values are respectively
`7369c6feeee5b492683208f636b5d36568afb75e24244914a83706111dc24fff`,
`61369df04ac3c40de8316a7a2654eeb49a8c879ab9e12e71dac1bff21d8a8f44`,
and `1d04af96119e00dc133fac95910cd69d07e5e5e6bce4b59479f4fb1a67837e75`.
Exclusive owner-only files/directories, exact-byte readback and hashes were
verified at retention. Later owner decision/status and target observations must
be read from their current dedicated records, not these earlier held-state notes.

### 2026-10-15 actual owner-approved bounded discovery continuation

The owner approved exact discovery plan
`25adcae1ae4aa35c596bf6d121de1f0235e8ac19368cb1b7c7bad2cb22dca6d5`.
The root agent retained the actual decision, performed fresh binding/expiry/scope
checks, executed all twelve metadata queries and independently verified all
retained stream hashes. Exit 0 signifies completed discovery, not passing gates.
The receipt SHA-256 is
`cf26b491256421c931c65bf5e1921b82bb61aef5990b0bd94508409192c7079e`.
Observed qualification namespace is absent; existing namespace classification
remains unknown. Runtime application image digest candidates now exist in
metadata, but their signature/source/compatibility evidence is unverified.
The local discovery budget is complete and its execution status was held to
prevent reuse; the standing sole-owner policy persists. See the actual-discovery
section of the [execution plan](../../docs/operations/story-27-4-execution-plan-2026-10-15.md)
for UIDs, evidence locations, exact results and remaining target/impact question.
All six live tasks remain unchecked. No C1/C0-C6/terminal/A41/Production pass or
closure is claimed. This supersedes the earlier dated pending-discovery notes
for the D0 action only; it supplies no qualification mutation or fault authority.

### 2026-10-16 separate qualification target selected

The owner said “apply recommended” after considering reuse versus a separate
cluster. Apply the separate-target route while existing `jpiquot@local` remains
unclassified; do not reuse its completed discovery budget or touch its workloads.
The [isolated target plan](../../docs/operations/story-27-4-isolated-target-plan-2026-10-16.md)
records the prepared one-guest KVM configuration, verified public OS image, private
seed/keys, actual filesystem/ACL/KVM checks, exact bounded bootstrap and transparent
candidate profile delta. This is independent preparation, not exact bootstrap or
replacement-profile approval, gate acceptance, live qualification or closure.

Named blocker **QT1**: the original frozen PG2 hash cannot silently describe a
substituted node/resource envelope. The candidate and its exact hash are retained
outside the approved profile, with unchanged technical pins/capabilities/workload.
Obtain only the genuinely missing exact bootstrap/target-profile decision; never
ask for the adopted sole-owner policy again. No VM or new cluster is running, no
640-GiB disk reservation exists, and current consumers still refuse missing or
foreign-profile evidence. Do not migrate fixed profile constants or frozen intent
before the owner explicitly adopts the concrete change. Later real integration
work must be tied to observed target/gate blockers. Existing 72-hour authority,
168-hour retention and predecessor-freshness gates remain required. Protected
history, all six unchecked tasks, A41 and Production remain unchanged.

### 2026-10-16 exact bootstrap/profile approval and stopped attempt

The owner answered “yes I approve” to the exact prepared bootstrap and replacement
profile. The [actual decision/result](27-4-isolated-target-approval-2026-10-16.md)
binds the hashes and records the real one-attempt bootstrap: base conversion
passed, OS expansion hit its approved 300-second timeout (exit 124), no data disk
or VM was created, and zero guest commands executed. The retained 40-GiB OS file
has full Windows/Linux allocation readback and private DACL; no matching child
process, QEMU PID or QMP socket remains. The budget is consumed/closed, not an
owner withdrawal. Never replay or widen that failed plan. No gate pass or task
closure follows from the adopted profile or these disk observations.

This owner's concrete adoption renegotiates only the frozen current PG2 hash to
`95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`.
All other frozen requirements stay unchanged. The canonical approved manifest is
[retained in source](../../docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json);
its exact canonical identity/capabilities/workload must hash to this value.

The immediately authorized implementation slice is **QT1-P**, necessary to use
the approved new target: migrate the active PG2 canonical profile, current owner/
C1.15 target pins, current authority/tooling selectors, non-Production C# gate and
qualification overlay profile references to the exact adopted manifest. Preserve
all backend/runtime/security/deployment input hashes, 400-GiB boundaries, workload,
closed schemas, existing time limits and unconditional deployed-binding refusal.
Keep PG1 and the former PG2 canonical identity recoverable/unchanged as historical
material; update current-selector tests without rewriting their historical source
fixtures, planning artifacts, ADR, ledgers, sprint or acceptance history. Add
meaningful negative tests for former/foreign hashes and target/envelope drift;
assert the full canonical new manifest, technical invariants and default disabled/
empty/zero render. Run narrow affected Python lanes and appropriate C# gate/
deployment checks, retaining exact commands/results and any environment blocker.

Do not change the source-bound D0 runner `tools/access_telemetry_owner_discovery.py`
or its historical approved plan in this slice. Its digest and old-target twelve
queries are retained discovery-only history, not current qualification authority.
Do not implement speculative accepting gate registrations or C3 renewal admission
without actual target facts. No target operations, disk changes, retry, Kubernetes
install, source contact, Git stage/commit/push or Production write is authorized
by this implementation handoff. The parent handles operational recovery; tie any
additional implementation to a demonstrated blocker. Leave all six live tasks
unchecked and do not advance workflow closure.

</details>
