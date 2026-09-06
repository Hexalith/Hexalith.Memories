---
title: 'OpenBao Platform Hardening and Documentation'
type: 'feature'
created: '2026-09-06'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 31.1 already documented the deployed OpenBao `hexalith-keys` platform and bound it with executable guards, but it is still `in-progress`: C4b/C5b have no independent countersignature, C7 is a waiver expiring 2026-10-26, and `docs/operations/openbao.md` still calls the helm empty-diff a Story 31.1 `done` gate after that outcome was carved out to Platform Operations.

**Approach:** Keep `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` as the implementation of record. Align the remaining file-level helm wording with the approved 2026-07-28 carve-out. Do not invent a security evaluation, migrate the runtime secret store, or mutate the cluster unless the human authorizes that path.

## Boundaries & Constraints

**Always:**
- Availability profile as measured: three Raft voters, `ha_enabled: true`, all on one Kubernetes node (the whole failure domain).
- Exactly two accepted-limitations rows (`Static file-based seal`, `Namespace-wide port 8200 ingress`), each with owner, consequence, compensating controls, and reopen trigger; never call either hardened, production-HA, highly available, or production-ready.
- No secret values in docs, evidence, or snapshots. `kubectl get secret` is names and types only.
- C1/C2/C3/C4a/C5a/C6 stay `complete` unless a live re-measure proves them stale. C4b/C5b stay `not complete` until an evaluator independent of `jpiquot` / `murat-tea-for-jpiquot` signs. C7 stays waived until that evaluation or 2026-10-26.
- Story 31.1 does not reach `done` while C4b, C5b, or C7 are undischarged. Story 31.2 may enter implementation on the 2026-08-01 gate (C1/C2/C3/C4a/C5a/C6).
- `docs/operations/openbao.md` stays CRLF.

**Never:**
- Never edit `deploy/kubernetes/base/dapr/secretstore.yaml`, AppHost/Aspire OpenBao topology, access-telemetry secrets, `PG-ONPREM-1`, or the production-deployment verifier.
- Never weaken Restricted PSA, TLS, the image digest pin, or NetworkPolicy to pass a check.
- Never `helm upgrade` (non-dry-run) or change live NetworkPolicy / auth-delegator bindings unless the human picks the Platform Ops path.
- Never read Secret `.data` (`openbao-seal`, `openbao-operator-credentials`, `openbao-server-tls`, `hexalith-keys-pki`).
- Never claim a security evaluation occurred, close C4b/C5b/C7 with `murat-tea-for-jpiquot`, or revert to single-voter Raft.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Helm done-gate wording | Divergence still says Story 31.1 `done` gate | Row stays keyed `has not been re-applied`, owner Platform Operations, reopen is empty `helm diff`; not a 31.1 checkpoint | Guard fails if the key vanishes |
| C7 waiver in date | Evidence section 4, expiry 2026-10-26 | Waived branch accepted; no evaluation claimed | Do not back-date or delete the waiver |
| Secret-shaped paste | PEM, `hvs.`/`s.`, Unseal Key labels | `PlatformRecords_ContainNoSecretShapedMaterial` fails | Delete the paste; do not weaken the guard |
| Unauthorized cluster write | `helm upgrade` or live NetPol without that path | Not performed | Stop and record a blocker |

</frozen-after-approval>

## Open Questions

- Session path — options: Verify and align only (re-run the documentation guards, reword helm empty-diff from a Story 31.1 `done` gate to a Platform Operations reopen ratified 2026-07-28, no cluster contact) / Live re-measure (also re-run read-only kubectl probes against `jpiquot@local` and update doc, evidence, and `Measured*` constants together if drifted; still no helm apply) / Platform Ops attempt (also try helm empty-diff and/or live NetPol / auth-delegator mutations; needs helm and explicit approval of each write)
- Independent countersignature — options: Leave open (C4b/C5b `not complete`, C7 waived until 2026-10-26; no reviewer invented here) / Name a reviewer now (you supply an identity independent of `jpiquot` and `murat-tea-for-jpiquot`; wait for their dated evaluation before any move toward `done`) / Draft a new waiver (replacement time-bounded waiver for approval; still not an evaluation)

## Code Map

- `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` -- implementation of record; checkpoints C1–C7
- `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` -- smoke §§3/3.1, C7 waiver §4, open obligations §6.4 (helm row still `done` gate)
- `docs/operations/openbao.md` -- measured profile, named divergences line 279, accepted-limitations table
- `deploy/openbao/{values,namespace,service-account-hardening,smoke-test}.yaml` -- owned manifests; values HA `replicas: 3`, static seal, CA-only smoke Job; cert-manager NP is live-first `PENDING PLATFORM CHANGE`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` -- file-only guards; `MeasuredRaftVoters="3"`; `ShouldBindManifest`; does not observe the cluster
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` -- `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-scope-ratifications.md` -- helm reproduce-release carve-out
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` -- C4a/C5a vs C4b/C5b; 31.2 gate

## Tasks & Acceptance

**Execution:**
- [ ] `docs/operations/openbao.md` -- reword the `has not been re-applied` divergence so helm empty-diff is a Platform Operations reopen, not a Story 31.1 `done` gate -- implements the 2026-07-28 carve-out without dropping the row
- [ ] `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` -- same reword in §6.4; do not mark the obligation discharged
- [ ] `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` -- keep the `has not been re-applied` key; change assertions only if they still require the words `done gate`
- [ ] `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` -- record this continuation; do not flip C4b, C5b, C7, or sprint-status to `done`

**Acceptance Criteria:**
- Given the 2026-07-28 scope ratifications, when the named-divergences and evidence §6.4 helm rows are read, then they still record an unproven reproduce-release gap owned by Platform Operations and they no longer call it a Story 31.1 `done` gate.
- Given `OpenBaoPlatformDocumentationTests`, when it runs in Release, then every method passes and the accepted-limitations table still has exactly two keyed rows with no empty or placeholder cells.
- Given C4b, C5b, and C7, when this slice finishes, then those completion states are unchanged and no text claims an independent evaluation occurred.
- Given Story 31.2-owned and Epic 29-owned paths, when the diff is reviewed, then none of those files changed.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

C4a/C5a are documentation; C4b/C5b are countersignature. Development cannot produce an independent evaluator. Continue from the existing guards; do not add a third accepted limitation without a sprint change.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: build succeeded
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo` -- expected: all facts pass
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Deployment.ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal -parallel none -noLogo` -- expected: pass

**Manual checks (if no CLI):**
- Confirm `sprint-status.yaml` key `31-1-openbao-platform-hardening-and-documentation` is not moved to `done`.
