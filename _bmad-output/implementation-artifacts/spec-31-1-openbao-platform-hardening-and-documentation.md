---
title: 'OpenBao Platform Hardening and Documentation'
type: 'feature'
created: '2026-09-06'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '115e2839d8902a3b20b866913a70fb8474b94f83'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 31.1 already documented the deployed OpenBao `hexalith-keys` platform and bound it with executable guards, but it is still `in-progress`: C4b/C5b have no independent countersignature, C7 is a waiver expiring 2026-10-26, and `docs/operations/openbao.md` still calls the helm empty-diff a Story 31.1 `done` gate after that outcome was carved out to Platform Operations.

**Approach:** Keep `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` as the implementation of record. Re-run the existing read-only kubectl probes against `jpiquot@local`, then align the helm wording with the 2026-07-28 carve-out. Do not invent a security evaluation, migrate the runtime secret store, or mutate the cluster.

**Decisions (2026-09-06):**
- Session path is **bounded live re-measure**. Repeat the story's existing read-only kubectl probes only (nodes, replicas, HA, NetworkPolicy, automount, secret names/types). If they match the 2026-07-28 table, stamp the re-measure in evidence and do the wording fix. If they drifted, update `docs/operations/openbao.md`, `deploy/openbao/values.yaml`, and `Measured*` constants together. If kubectl fails, record a blocker and fall back to wording-only. No helm apply. No live NetworkPolicy or auth-delegator writes.
- Independent countersignature is **leave open**. C4b/C5b stay `not complete`. C7 stays waived until 2026-10-26. Do not name a reviewer, impersonate `murat-tea-for-jpiquot`, or draft a replacement waiver.

## Boundaries & Constraints

**Always:**
- Availability profile as measured: three Raft voters, `ha_enabled: true`, all on one Kubernetes node (the whole failure domain) — unless this re-measure proves otherwise, in which case the bound trio updates together.
- Exactly two accepted-limitations rows (`Static file-based seal`, `Namespace-wide port 8200 ingress`), each with owner, consequence, compensating controls, and reopen trigger; never call either hardened, production-HA, highly available, or production-ready.
- No secret values in docs, evidence, or snapshots. `kubectl get secret` is names and types only.
- C1/C2/C3/C4a/C5a/C6 stay `complete` unless this re-measure proves them stale. C4b/C5b stay `not complete`. C7 stays waived until an independent evaluation or 2026-10-26.
- Story 31.1 does not reach `done` while C4b, C5b, or C7 are undischarged. Story 31.2 may enter implementation on the 2026-08-01 gate (C1/C2/C3/C4a/C5a/C6).
- `docs/operations/openbao.md` stays CRLF.

**Never:**
- Never edit `deploy/kubernetes/base/dapr/secretstore.yaml`, AppHost/Aspire OpenBao topology, access-telemetry secrets, `PG-ONPREM-1`, or the production-deployment verifier.
- Never weaken Restricted PSA, TLS, the image digest pin, or NetworkPolicy to pass a check.
- Never `helm upgrade`, `helm diff` (helm is absent), or change live NetworkPolicy / auth-delegator bindings.
- Never read Secret `.data` (`openbao-seal`, `openbao-operator-credentials`, `openbao-server-tls`, `hexalith-keys-pki`).
- Never claim a security evaluation occurred, close C4b/C5b/C7, or revert to single-voter Raft.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Re-measure matches | Read-only probes equal the 2026-07-28 table | Evidence records UTC timestamp, context `jpiquot@local`, and "unchanged"; then wording fix only | N/A |
| Re-measure drifted | Replica count, HA, node, NP, or automount differs | Update doc, `values.yaml`, and `Measured*` together; keep two limitation rows | Do not update only one of the three |
| kubectl unreachable | Probe command fails | Record blocker with owner/reopen; fall back to wording-only | Do not synthesize cluster facts |
| Helm done-gate wording | Divergence still says Story 31.1 `done` gate | Row stays keyed `has not been re-applied`, owner Platform Operations, reopen is empty `helm diff`; not a 31.1 checkpoint | Guard fails if the key vanishes |
| C7 waiver in date | Evidence section 4, expiry 2026-10-26 | Waived branch accepted; no evaluation claimed | Do not back-date or delete the waiver |
| Secret-shaped paste | PEM, `hvs.`/`s.`, Unseal Key labels | `PlatformRecords_ContainNoSecretShapedMaterial` fails | Delete the paste; do not weaken the guard |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` -- implementation of record; checkpoints C1–C7; probe list in Dev Notes
- `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` -- smoke §§3/3.1, C7 waiver §4, open obligations §6.4 (helm row is Platform Operations reopen, not a 31.1 checkpoint), bounded re-measure §8
- `docs/operations/openbao.md` -- measured profile, named divergences line 279, accepted-limitations table
- `deploy/openbao/{values,namespace,service-account-hardening,smoke-test}.yaml` -- owned manifests; values HA `replicas: 3`, static seal, CA-only smoke Job; cert-manager NP is live-first `PENDING PLATFORM CHANGE`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` -- file-only guards; `MeasuredRaftVoters="3"`; `ShouldBindManifest`; does not observe the cluster
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` -- `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-scope-ratifications.md` -- helm reproduce-release carve-out
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` -- C4a/C5a vs C4b/C5b; 31.2 gate

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` -- run the existing read-only kubectl probes against `jpiquot@local`; record UTC timestamp, context, and match-or-drift vs the 2026-07-28 table; names/types only for secrets
- [x] `docs/operations/openbao.md` and `deploy/openbao/values.yaml` and `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` -- if drifted, update the bound trio together; if matched, leave measured literals as they are
- [x] `docs/operations/openbao.md` -- reword the `has not been re-applied` divergence so helm empty-diff is a Platform Operations reopen, not a Story 31.1 `done` gate
- [x] `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` -- same helm reword in §6.4; do not mark that obligation discharged
- [x] `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` -- record this continuation; do not flip C4b, C5b, C7, or sprint-status to `done`

**Acceptance Criteria:**
- Given context `jpiquot@local`, when the existing read-only probes run, then evidence records a 2026-09-06 UTC result that either matches the 2026-07-28 table or updates doc, `values.yaml`, and `Measured*` together.
- Given the 2026-07-28 scope ratifications, when the named-divergences and evidence §6.4 helm rows are read, then they still record an unproven reproduce-release gap owned by Platform Operations and they no longer call it a Story 31.1 `done` gate.
- Given `OpenBaoPlatformDocumentationTests`, when it runs in Release, then every method passes and the accepted-limitations table still has exactly two keyed rows with no empty or placeholder cells.
- Given C4b, C5b, and C7, when this slice finishes, then those completion states are unchanged and no text claims an independent evaluation occurred.
- Given Story 31.2-owned and Epic 29-owned paths, when the diff is reviewed, then none of those files changed.

## Implementation Notes

2026-09-06T21:55:01Z bounded re-measure against `jpiquot@local` matched the 2026-07-28 table for nodes, replicas, HA mode, `hexalith-keys` NetworkPolicy, and automount. Measured literals left in place. Helm `has not been re-applied` rows reworded to a Platform Operations empty-`helm diff` reopen. C4b/C5b remain `not complete`; C7 remains waived until 2026-10-26. Additional namespace objects (`deployment-seal-transit`, `deployment-seal-external`, `deployment-seal-runner-token`) recorded in evidence §8.8 only. Matrix coverage: matching re-measure and helm wording are pinned in `OpenBaoPlatformDocumentationTests`; trio-bind covers the unused drift branch; the kubectl-unreachable branch was not taken (probes succeeded).

## Spec Change Log

- 2026-09-06: Executed the bounded live re-measure path. Execution checkboxes marked complete. Story status left `in-progress`.


## Review Triage Log

## Design Notes

C4a/C5a are documentation; C4b/C5b are countersignature. Development cannot produce an independent evaluator. The re-measure uses the story's existing probe list only — no new tooling, no smoke-test Job re-apply, no helm. Continue from the existing guards; do not add a third accepted limitation without a sprint change.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: build succeeded
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo` -- expected: all facts pass
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Deployment.ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal -parallel none -noLogo` -- expected: pass

**Manual checks (if no CLI):**
- Confirm `sprint-status.yaml` key `31-1-openbao-platform-hardening-and-documentation` is not moved to `done`.
- Confirm evidence records the re-measure timestamp and does not contain Secret `.data`.
