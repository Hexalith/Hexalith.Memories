---
title: 'Make the disabled access-telemetry lifecycle inert in Production'
type: 'bugfix'
created: '2026-11-01'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'e8956eb5c1bd49297f86702f9606eb9b013bc2f1'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 27 retro items 63 (B3, B4, B8) and 64 (B7, B11). The Production overlay disables the access-telemetry lifecycle, but its render (attached to every release as `hexalith-memories-production.yaml`) still deploys:
- the lifecycle PostgreSQL at `replicas: 1` (4 CPU, 8Gi, 400Gi PVC) and its `minAvailable: 1` PDB;
- the `access-telemetry-secrets` and `access-telemetry-config` components scoped to the Server app-id `memories`, so the Server sidecar depends on the disabled feature's bootstrap secret;
- a Server Role that can `get` `openbao-access-telemetry-bootstrap`;
- disable markers that cite closed Story 27.3.

In the Server, one untested guard keeps the lifecycle services out of a disabled pod. The disabled health text reads the same as enabled-but-idle. Binding the whole options section before reading `Enabled` lets an unconvertible value such as `0` crash a pod whose feature is off.

**Approach:** Patch only the Production overlay: scale the StatefulSet to 0, drop its PDB, unscope `memories`, drop the Role entry, and re-key the markers to DW-730. Read `Enabled` first and bind the rest only when it is enabled. Give the disabled state its own Healthy text. Prove all of this with rendered-manifest and Production composition tests, plus mutation kill checks.

**Decisions:**
- An unconvertible `Enabled` means disabled, plus a startup warning that names the key, never the value.
- Scope and RBAC removals live in the Production overlay. The base and the Qualification render stay unchanged.
- DW-17, the runbook, ADR 27.1, every other retro item's status and the item-62 deferred entries stay untouched.
- Never run a mutating `kubectl` command. Live-cluster checks are read-only `kubectl get`.
- *Owner answer, 2026-11-01:*
  - The two Production overlay files are SHA-256 inputs to the approved PG-ONPREM-2 profile. Changing them makes the parked C1 tooling refuse with `approved-profile-input-drift`.
  - Ship items 63 and 64 together, and take the parked lanes that this drift turns red out of PR CI. Record that move against DW-730.
  - Never update the approved-profile pins or identity.
- *Owner answer, 2026-11-01:* keep one spec, despite exceeding the token guideline.

## Boundaries & Constraints

**Always:**
- Render offline with `kubectl kustomize`.
- Every Production removal is a JSON6902 `test` + `remove` pair, so a reordered base fails the render instead of removing the wrong entry.
- Keep the StatefulSet's `volumeClaimTemplates`. They are immutable on a live StatefulSet.
- `.cs`, `.md` and `.ps1` use CRLF; YAML and Python use LF.
- After kill checks, restore each mutated source byte-identically and prove it with SHA-256.
- Keep `ACCESS_TELEMETRY_ENABLED=false`, hash `unproven` and both lifecycle Deployments at `replicas: 0` pinned in PR CI.

**Never:**
- Delete or shrink PVCs, or set `persistentVolumeClaimRetentionPolicy`.
- Edit `tools/render-production-deployment.ps1` or `tools/publish-production-deployment*.ps1`.
- Edit any C1 approved-profile pin, `_PG_ONPREM_2_IDENTITY_JSON` or the profile hashes.
- Run `dotnet test`, or the whole IntegrationTests assembly.
- Commit or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Disabled | Production; `Enabled` false or absent | No lifecycle registrations. Health is Healthy, "Access telemetry lifecycle is disabled by configuration.", tags exactly `["lifecycle"]` | N/A |
| Unproven | Production; `Enabled=true`; hash `unproven`; all else valid | No lifecycle registrations. Health is Unhealthy (terminal `ConfigurationInvalid`) | N/A |
| Positive control | As Unproven, but with a 64-hex hash | Every lifecycle registration is present | N/A |
| Unconvertible | `Enabled=0` or another non-Boolean; rest unset | No throw; disabled; no lifecycle registrations | One warning that names `AccessTelemetryLifecycle:Enabled` / `AccessTelemetryLifecycle__Enabled`, never the value |
| Malformed while off | `Enabled=false`, `Retention=not-a-timespan` | No throw; disabled; no warning | N/A |
| Malformed while on | `Enabled=true`, bad `Retention` | Throws at composition, as today (fail fast) | Binder exception |

"Lifecycle registrations" means:
- `AccessTelemetryLifecycleLoggerProvider` and `BoundedAccessTelemetryQueue`;
- the keyed `access-telemetry-lifecycle` and `access-telemetry-clock` HttpClients;
- the three lifecycle Dapr clients;
- the hosted services `AccessTelemetryLifecycleBootstrapService`, `AccessTelemetryDeliveryWorker` and `AccessTelemetryHeartbeatWorker`.

</frozen-after-approval>

## Code Map

- `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` -- markers at `:9,20,31` and the stale "Story 27.3 pending" comment (`:1-3`). The A41 test counts `replicas: 0` occurrences here, so comments must not contain that text.
- `deploy/kubernetes/overlays/production/kustomization.yaml` -- base indices:
  - `memories` is at `/scopes/0` of both components;
  - `openbao-access-telemetry-bootstrap` is at `/rules/0/resourceNames/8` of Role `memories-dapr-secret-reader`.

  A scratch prototype rendered cleanly, and a wrong `test` index fails the render with exit 1. Qualification builds on `../../base`, not on this overlay.
- PG-ONPREM-2 pins on both overlay files:
  - `tools/access-telemetry-c1-profile.ps1:16-17` and `tools/verify-access-telemetry-c1-linkage.ps1:819-820`;
  - `tools/verify_access_telemetry_lifecycle.py:444-445`, hashed into `7f9f69…` and then `95aa36…`.

  All are read-only. A trailing-comment drift in an isolated worktree turned these lanes red, and a failing case passes on the clean tree:
  - `access_telemetry_c1`: 212 subtest failures and 20 errors;
  - `access_telemetry_c1_interchange`: 6 subtest failures;
  - `access_telemetry_lifecycle`: 37 failures and 45 errors.

  `access_telemetry_owner_discovery`, `access_telemetry_functional_retention` and `access_telemetry_c1_sql_contract` were not measured.
- CI hosting guards:
  - `.github/workflows/ci.yml:376-398` holds the lane steps.
  - `tests/tooling/ci_alignment/ci_alignment_test.py:91,93,95` pins them in `MANDATORY_COMMANDS`.
  - `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:810-841` requires every `tests/tooling/*` suite to be run by some workflow, and `:856` names two parked steps as kubectl consumers.
  - The setup steps "Build source Debug descriptor reader" and "Initialize root Platform review-reader dependency" stay, for item 7 to judge.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:535-603` -- `AddAccessTelemetryLifecycle`. Only `AccessTelemetryLifecycleBootstrapService` (`:62` secret, `:188` config) reads the two components, and it sits behind the guard. The Server C# never reads `ACCESS_TELEMETRY_ENABLED`: `server-deployment.yaml:102-106` maps that ConfigMap key into env `AccessTelemetryLifecycle__Enabled`. `AccessTelemetryQualificationGate.cs:44` already parses it with `bool.TryParse`.
- `.../AccessTelemetryLifecycle/AccessTelemetryLifecycleHealthCheck.cs:27-46` -- the disabled path reaches `NoData`. No test, runbook or doc references that text.
- `tools/verify-production-deployment.ps1:1156-1190` -- the CI job `production-deployment-verification`, green on `76c737bf`, reads `access-telemetry-secrets/access-telemetry-marker-key` through the Server sidecar and expects success. The marker is already registered for redaction at `tools/production-deployment-openbao.ps1:539`. Source-text pins: `production_deployment_evidence_test.py:950-953` and `CiTestInventoryTests.cs:923-924`.
- Test pins:
  - `ProductionDeploymentArtifactsTests.cs:127` (`replicas: 1`) and `:329` (Role entry). Reuse `GetDocument`, `ReadYamlSequence` and `ReadIntegerField`.
  - `AccessTelemetryA41CloseOutTests.cs:84-86`.
  - `AccessTelemetryLifecycleIntegrationCheckpointTests.cs:597`. The `pending-story-27-3` assertion at `:599` is an actor-state data default and stays.
  - `AccessTelemetryLifecycleRegistrationTests.cs` provides the builder pattern.

## Tasks & Acceptance

**Execution:**
- [x] `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` -- re-key to `disabled-pending-dw-730` and rewrite the header comment. Add a StatefulSet patch (`replicas: 0`, same annotation) and a PDB `$patch: delete` -- B3, B8
- [x] `deploy/kubernetes/overlays/production/kustomization.yaml` -- add test+remove patches for both component scopes and the Role entry -- B3, B4
- [x] `src/.../MemoriesServerServiceCollectionExtensions.cs` -- read `Enabled` first. On disabled, register default options, the status, the health check, the meter and the always-on qualification services, then return. Register the warning service only when the value is unconvertible. Bind the full section only when enabled -- B7, B11
- [x] `src/.../AccessTelemetryLifecycle/AccessTelemetryLifecycleEnabledValueWarning.cs` (new) -- an `IHostedService` that logs one source-generated warning at start -- B11
- [x] `src/.../AccessTelemetryLifecycle/AccessTelemetryLifecycleHealthCheck.cs` -- disabled branch with its own description, cause and nextAction -- B7
- [x] `tools/verify-production-deployment.ps1` -- replace the three Server `access-telemetry-secrets` probes with one out-of-scope denial plus a leak check. Add `auth can-i get secret/openbao-access-telemetry-bootstrap` for SA `memories` expecting `no`, mirroring `:1102-1110`
- [x] `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py` and `CiTestInventoryTests.cs:923-924` -- re-point the verifier text pins
- [x] `.github/workflows/ci.yml`, `ci_alignment_test.py`, `CiTestInventoryTests.cs` -- remove the parked lane steps and their mandatory entries. Add a parked-suite exemption whose suites must exist and must stay unwired. Re-point the `:856` kubectl consumers to steps that remain. Measure the three unmeasured access-telemetry lanes after the change, and treat any red one the same way
- [x] `tests/.../Deployment/ProductionDeploymentArtifactsTests.cs` -- flip `:127` and `:329`. Add `ProductionOverlay_DisabledLifecycleIsInert` (which also pins `ACCESS_TELEMETRY_ENABLED: "false"` and `unproven`) and `QualificationOverlay_KeepsLifecycleScopesStorageAndRbac`
- [x] `tests/.../Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleProductionCompositionTests.cs` (new) -- cover the I/O matrix through `AddServiceDefaults` + `AddMemoriesServerServices` with `EnvironmentName = "Production"`
- [x] `AccessTelemetryA41CloseOutTests.cs`, `AccessTelemetryLifecycleIntegrationCheckpointTests.cs` -- re-key the pins and set the `replicas: 0` count to three
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- append a section with two entries: the parked lanes off PR CI (reopen with DW-730), and applying the inert overlay to the live cluster (operator, PVC decision)
- [x] `sprint-status.yaml` -- close items 63 and 64 only

**Acceptance Criteria:**
- Given the Production render, when inspected, then:
  - the lifecycle StatefulSet has `replicas: 0`;
  - no PDB or PVC document exists for it;
  - no `access-telemetry*` Component lists `memories`;
  - the Server Role lacks `openbao-access-telemetry-bootstrap`;
  - `disabled-pending-story-27-3` is absent;
  - both lifecycle Deployments stay at `replicas: 0`.
- Given the Qualification render, when inspected, then:
  - both components still scope `memories`;
  - the Role still grants the bootstrap secret;
  - the StatefulSet stays at `replicas: 1` with its PDB.
- Given the CI workflows, when the inventory and alignment fixtures run, then:
  - they pass;
  - no workflow runs a parked suite;
  - every other `tests/tooling` suite stays wired.
- Given each mutation below, when the new tests run, then at least one fails; given the restore, then each source's SHA-256 matches its baseline:
  - drop the disabled early return;
  - drop the invalid-options return;
  - bind the full section before `Enabled`;
  - revert the disabled health text.

## Implementation Notes

Baseline `e8956eb5c1bd49297f86702f9606eb9b013bc2f1`. A fresh subagent implemented the spec. The orchestrator then re-checked the diff and re-ran the focused classes on assemblies confirmed fresh.

**Final rendered Production inventory** (`kubectl kustomize deploy/kubernetes/overlays/production`, 50 documents; the baseline had 51, including the lifecycle PDB). This is for the items 4/5 docs prompt.

| Kind | What is rendered, and at what scale |
| :--- | :--- |
| Deployment | `memories` 2 and `memories-mcp` 2; `memories-access-telemetry` 0 and `memories-access-telemetry-clock` 0 (`disabled-pending-dw-730`) |
| StatefulSet | `redis-stack` 1 (20Gi PVC template) and `falkordb` 1 (10Gi); `access-telemetry-postgresql` **0**, which keeps its 4 CPU / 8Gi request and 400Gi `openebs-hostpath-retain` template, so no pod and no new PVC |
| PodDisruptionBudget | none |
| Component | `access-telemetry-secrets` [lifecycle, clock]; `access-telemetry-config` [lifecycle]; `access-telemetry-store` [lifecycle]; `secretstore` and `pubsub` [eventstore, memories]; `statestore` and `llm-openai` [memories] |
| Role `memories-dapr-secret-reader` | the same resource names as before, minus `openbao-access-telemetry-bootstrap`. The lifecycle and clock Roles still grant it, to accounts whose Deployments run 0 pods |
| Inert, still rendered | the two PostgreSQL Services, the three lifecycle NetworkPolicies, the PostgreSQL ConfigMap and ServiceAccount, the lifecycle and clock ServiceAccounts, Services and Dapr Configurations |

The ConfigMap keeps `ACCESS_TELEMETRY_ENABLED: "false"` and `ACCESS_TELEMETRY_COMPONENT_PROFILE_HASH: unproven`. The Qualification render is byte-identical to baseline. `memories-config` still lists an `access-telemetry-secrets` secret scope, which is inert because the Server no longer loads that store.

**Decisions taken during implementation:**
- A blank or whitespace `Enabled` counts as absent: disabled, with no warning. Only a present non-Boolean value triggers the warning.
- `bool.TryParse` matches the binder's Boolean grammar, and `AccessTelemetryQualificationGate.cs:44` already uses it.
- The disabled options singleton is `new AccessTelemetryOptions()`. No Server consumer reads anything except `Enabled` when disabled.
- The warning uses EventId 7530 and `CapturingAuditLoggerProvider` captures it in the test. 7530 collides with no other event ID in `src/`.
- The setup steps "Build source Debug descriptor reader" and "Initialize root Platform review-reader dependency" stay in `ci.yml` for retro item 68 to judge.
- Orchestrator correction: the subagent's deferred entry claimed a client-side apply might leave the drifted lifecycle Deployment at 1. That was wrong, because three-way apply patches every rendered field whose live value differs. The entry now says so.

**Evidence:**
- Builds: Server.Tests, Cli.Tests and IntegrationTests, each with 0 warnings and 0 errors.
- Freshness: `strings -el` finds the new health text and the warning in the Server assembly, with the old "no data in the bounded window" text as a positive control. `[InlineData]` arguments are UTF-8 attribute blobs, so they are found with `strings -a`.
- Orchestrator re-runs: composition 11/11, `ProductionDeploymentArtifactsTests` 14/14, A41 5/5 and registration 2/2, so 32/32 together. Also `CiTestInventoryTests` 136/136 and `ci_alignment` 12/12.
- Subagent runs:
  - `AccessTelemetryLifecycleIntegrationCheckpointTests` 8/8; `production_deployment_evidence` 123.
  - The unparked lanes all pass: `access_telemetry_owner_discovery` 31, `access_telemetry_functional_retention` 162, `access_telemetry_c1_sql_contract` 19.
  - The ledger readers pass: `integration_stub_closure`, `story_review_readiness`, `story_scope`, `story_slice_scope`, `bmad_customization`, `line_endings`.
  - The PowerShell parser finds 0 errors in the verifier.
- Kill checks (subagent; logs in the session scratchpad), each restored with a matching SHA-256:
  - dropping the disabled return fails 8 tests;
  - dropping the invalid-options return fails 1;
  - binding the full section before `Enabled` fails 5;
  - reverting the health text fails 7.
- Drift: the approved-profile check refuses the new overlay with `approved-profile-input-drift` and passes with the overlay restored to `e8956eb5`.

**Not executable here:** the verifier's out-of-scope denial and the `can-i` check run only in the CI job `production-deployment-verification`, which needs a kind cluster.

## Spec Change Log

## Review Triage Log

Pass 1 (2026-11-02). Layers: Blind Hunter (BH), Edge Case Hunter (EC) and Verification Gap (VG).

**Patch**

| Finding | Verdict | Evidence |
| :--- | :--- | :--- |
| EC1, EC8, VG2, BH7a: a blank or whitespace `Enabled` is silently disabled with no warning | medium | `TryReadAccessTelemetryLifecycleEnabled` treats `IsNullOrWhiteSpace` as absent. The frozen matrix says any non-Boolean warns, and the binder throws on `""` and `"   "`, so the "same grammar" doc claim is false. No test sends a blank value. Smallest fix: only `null` counts as absent. |
| BH13a, EC7, VG other: EventId 7530 sits in the reserved audit bank | low | `docs/dev/telemetry.md` and `AccessTelemetryLog.cs:17` reserve 7500-7599 for audit events. 76xx is unused across `src/`. |
| BH4b: the disabled health `impact` says writes "may be unavailable" | low | When disabled, writes are definitely off. Operators read this payload in every disabled Production pod. |
| BH7c: no test combines an unconvertible `Enabled` with a malformed sibling value | low | That is B11's crash scenario, and the code comment cites it. Adding the case is direct. |
| BH7d: the unconvertible test never builds the host | low | It uses `BuildServiceProvider()`, not `builder.Build()`. Switching is direct. |
| BH9: two `ci.yml` setup-step comments name consumers that no longer run | low | The Debug descriptor reader comment cites campaign-environment fixtures in the parked lifecycle suite. The Platform-init comment cites the parked interchange review fixtures. |
| BH12a: item 63 is `done` without saying its verifier checks are CI-only | low | The denial and `can-i` checks run only in `production-deployment-verification`. The status note omits this. |
| BH3: the path to re-enable Production is written down nowhere | low | Re-enabling now also needs reverting the JSON6902 removals, the StatefulSet scale, the PDB delete and the verifier checks. The fix is a sentence in this change's own deferred entry. |

**Defer**

| Finding | Verdict | Evidence |
| :--- | :--- | :--- |
| BH1: the DW-17 control citations are stale | medium | Control (2) cites `tests/tooling/access_telemetry_c1` (`ci.yml:388`), which is now unwired. The `ci.yml:421` and `:545` anchors moved when steps were removed. The intent reserves DW-17 for the next prompt. |
| BH2: sprint items 65 and 68 are overtaken | medium | Item 65 says "not the PostgreSQL StatefulSet", which is now false. Item 68 plans nightly or path-filtered runs of lanes that now refuse with drift. The intent forbids changing other items. |
| VG1, BH10: parking the interchange lane un-runs 636 passing tests | medium | VG ran the suite: 642 tests, and only 6 subtests of 2 methods fail on drift. The wired `access_telemetry_functional_retention.py:35-36` imports `JsonSnapshot`, `InterchangeFormatError` and owner-dispatch helpers whose direct tests no longer run. The owner chose lane-level parking with the 6-subtest count in hand, so narrowing it is item 68 or an owner decision. |
| EC5: the Server Role still grants the k8s Secret `access-telemetry-marker-key` | low | This is pre-existing. No component loaded by `memories` references it, and the intent limited the removal to the bootstrap secret. `memories-config` also still allow-lists it for `access-telemetry-secrets`, which is inert. |

**Rejected**

| Finding | Verdict | Evidence |
| :--- | :--- | :--- |
| BH4a: a typo reads as a deliberate Healthy disable | false | Disabled plus a startup warning is the owner's frozen decision. |
| BH5: the PVC assertion can never fail | false | It fails if any standalone PVC for `access-telemetry-postgresql` renders, which is the regression it guards against. |
| BH6, EC3: the probe accepts any non-zero exit | low | The exec channel to the same container was just proven by the allowed `secretstore` read and two denials. A re-scoped Server would read the allow-listed marker and exit 0, so the probe catches the regression it exists for. Parsing HTTP status in a CI-only script is not a direct fix. |
| BH6b: no runtime check for the absence of a PostgreSQL pod | low | `replicas: 0` is pinned at render time. A runtime check would be new verifier code. |
| BH7b: no `"TRUE"` or `" true "` cases | low | `bool.TryParse` accepts both, operators write `true`/`false`, and the positive control is a `Fact`, so adding cases means restructuring it. |
| BH8: the AccessTelemetry host still binds the whole section first | low | Pre-existing. That host runs only when the lifecycle is enabled (0 replicas in Production) and fails loudly on a typo. The fix is a `Program.cs` refactor. |
| BH11: stale parked C1 contract docs, `architecture.md:669`, `openbao.md:386` | low | Following the parked docs now yields the correct drift refusal. The architecture and openbao statements stay true for Qualification. The fix spans seven docs. |
| BH12b: mutation evidence lives in the scratchpad; the diff lacks the spec | false | The spec records the kill counts. Excluding the spec from the diff is the review protocol. |
| BH13b: the warning should name `ACCESS_TELEMETRY_ENABLED` | false | The message names the configuration key in both forms valid on any host. The ConfigMap key is a deployment alias. |
| EC2: `can-i` may print `no - <reason>` | false | The identical exact-`no` check for `memories-mcp` passes in green CI run 41327788865 on the same kind cluster. Empty stdout on an API error fails closed, as in the existing pattern. |
| EC4: parked suites could return under other invocation shapes | low | A `-s tests/tooling` root, `pytest` or a `.yaml` workflow is unlikely, and broadening detection is not a direct fix. |
| EC6: an out-of-band `whenScaled: Delete` could delete the PVC | false | Read-only `kubectl get` shows the live StatefulSet with `whenScaled: Retain` and `whenDeleted: Retain`, and the PV reclaim policy `Retain`. |

## Design Notes

**Scale, not exclude.** Read-only `kubectl get` on `jpiquot@local` shows this overlay is applied there (retro open question 3: yes).
- Namespace `hexalith-memories` carries `disabled-pending-story-27-3`, `ACCESS_TELEMETRY_ENABLED=false` and hash `unproven`.
- `access-telemetry-postgresql` is running 1/1 (104 days). Its PDB allows 0 disruptions. `data-access-telemetry-postgresql-0` is a bound 400Gi PVC on `openebs-hostpath-retain`.
- `memories-access-telemetry` runs 1/1, drifted from the overlay's 0.

`kubectl apply` does not prune. Excluding the StatefulSet would therefore leave that PostgreSQL running, and removing `volumeClaimTemplates` would make the apply fail. `replicas: 0` drains it on the next apply and keeps the Retain PVC. Deleting that data is irreversible and is the operator's call.

The PDB is excluded. A leftover live PDB selects zero pods once scaled, so it cannot block drains. The Services, the NetworkPolicies, the PostgreSQL ConfigMap and ServiceAccount, and `access-telemetry-store` (scoped only to the zero-replica lifecycle app) stay rendered and are inert.

**Handoff to the items 4/5 docs prompt:**
- Dapr HotReload is off and the Server pod template does not change. Server pods therefore drop the two components only after a restart.
- DW-17 control (2) cites `tests/tooling/access_telemetry_c1` (`ci.yml:388`), which no longer runs in PR CI. The pins now live in `ProductionDeploymentArtifactsTests` and integration-fast.

## Verification

**Commands:**
- `kubectl kustomize deploy/kubernetes/overlays/production` and `.../qualification` -- expected: both render; the inventory diff touches only the targets above.
- `dotnet build` on Server.Tests, Cli.Tests and IntegrationTests -- expected: 0 warnings, 0 errors. Then confirm freshness with `strings -el` on a new literal, with an old literal as a positive control.
- `DiffEngine_Disabled=true dotnet exec <dll> -class <FullName>` -- expected: all pass. Classes:
  - the new composition class;
  - `ProductionDeploymentArtifactsTests`, `AccessTelemetryA41CloseOutTests`, `AccessTelemetryLifecycleRegistrationTests`;
  - `CiTestInventoryTests`;
  - `AccessTelemetryLifecycleIntegrationCheckpointTests`.
- `python3 -m unittest discover` with an explicit `-p` for each of `production_deployment_evidence`, `ci_alignment` and the three unmeasured access-telemetry lanes -- expected: pass.
- PowerShell parser over `tools/verify-production-deployment.ps1` -- expected: no parse errors.
- `commitlint` on the proposed subjects -- expected: exit 0.
