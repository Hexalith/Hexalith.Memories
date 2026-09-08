---
title: 'OpenBao Platform Hardening and Documentation'
type: 'feature'
created: '2026-09-08'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9bdaa30fbfa6d002d8753aa7eb63d1fc507937ac'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 31.1's parent record is still `in-progress`. Five 2026-09-07 review patches are unchecked: ClusterIP/"no ingress" prose still reads as namespace-wide after evidence §8.8 recorded `deployment-seal-transit` NodePort `8200:30820`; C2 review text still calls helm empty-diff a Story 31.1 `done` gate; 2026-09-06 completion notes deny later edits; evidence §8.2 asserts ClusterIP/PVC state with no probe; the 2026-09-06 ledger row has no `matched N/N`.

**Approach:** Close those five patches on the implementation of record. Keep C4b/C5b `not complete` and C7 waived until 2026-10-26. Do not set the story or sprint-status to `done`.

**Decisions (2026-09-08):**
- Session path is **review-patch close-out**, not another live re-measure. No extra kubectl. Qualify evidence §8.2 ClusterIP/PVC sentences as unprobed against the 2026-09-06 bound list.
- Independent countersignature stays **leave open**. Do not name a reviewer, impersonate `murat-tea-for-jpiquot`, or replace the C7 waiver.
- `deployment-seal-external` `.spec` stays **DW-729**; this slice does not capture it.
- Extra §8.8 objects are **option A**: add three rows to `Deployed platform state not tracked in this repository` with owner Platform Operations (`jpiquot`) and reopen triggers; pin those keys in `NamedDivergencesAndUntrackedState_CarryOwnerAndReopenTriggerPerRow`. Not a third accepted limitation.
- **Keep full spec** — accept the 1,600-token risk; the five patches stay one close-out.

## Boundaries & Constraints

**Always:**
- Availability profile as last measured: three Raft voters, `ha_enabled: true`, one Kubernetes node as the failure domain.
- Exactly two accepted-limitations rows (`Static file-based seal`, `Namespace-wide port 8200 ingress`); never call either hardened, production-HA, highly available, or production-ready.
- Helm empty-diff is Platform Operations work, **not** a Story 31.1 checkpoint. C1/C2/C3/C4a/C5a/C6 stay `complete` unless a file this slice edits proves them stale.
- Story 31.1 does not reach `done` while C4b, C5b, or C7 are undischarged.
- `docs/operations/openbao.md` stays CRLF. Secret values never appear. `kubectl get secret` is names and types only.

**Never:**
- Never edit `deploy/kubernetes/base/dapr/secretstore.yaml`, AppHost/Aspire OpenBao topology, access-telemetry secrets, `PG-ONPREM-1`, the production-deployment verifier, or Story 31.2-owned paths.
- Never weaken Restricted PSA, TLS, the image digest pin, or NetworkPolicy to pass a check.
- Never `helm upgrade`, `helm diff`, or change live NetworkPolicy / auth-delegator bindings.
- Never read Secret `.data`. Never claim a security evaluation occurred. Never add a third accepted limitation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| ClusterIP language | Doc still says all Services ClusterIP / no ingress | Qualify to the four `hexalith-keys*` Services; NodePort stays named as extra | Guard fails if "every Service" / unqualified "no ingress" returns |
| Helm carve-out | C2 review text or helm row says done-gate | C2 review state matches the 2026-07-28 carve-out; `HelmRowMustRecordTheCarveOut` rejects `done gate` and `does not reach done until` after markdown strip | Do not weaken the positive "not a Story 31.1 checkpoint" pin |
| 2026-09-06 notes | Notes deny later doc/sprint-status edits | Append a dated correction; do not rewrite the 2026-09-06 note or Change Log row | New Change Log row carries discovery and `matched N/N` |
| §8.2 claims | ClusterIP/PVC asserted with no kubectl | Mark unprobed; do not invent kubectl output | Do not synthesize cluster facts |
| Extra objects | §8.8 NodePort / extra NP / token Secret | Three untracked-state rows plus test key pins; §8.8 may still name them | Do not invent a third limitation |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` -- implementation of record; C2 review state still says helm is an explicit `done` gate (~L258); five unchecked patches L228–232; 2026-09-06 completion notes L582–584; Change Log 2026-09-06 row L621 with no `matched N/N`
- `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` -- §8.2 L737–739 unprobed ClusterIP/PVC; §8.8 L822–835 extra objects; helm §6.4 already carved out
- `docs/operations/openbao.md` -- values bind cell L134 `all four Services are ClusterIP; no ingress exists`; seal compensating-control cell L304 `all four Services ClusterIP with no ingress`; Health L409 four `hexalith-keys*` ClusterIP (already scoped); untracked table L255–267
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` -- `HelmRowMustRecordTheCarveOut` L885–896; `OperationalSections_StayBoundToTheirRecordedRemediations` L487–508 (pins voters/`endpoints`, not ClusterIP scope); `ShouldBindManifest` ClusterIP L282; values `ShouldNotContain` NodePort L306–307 (manifest only)
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` -- `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal` — regression only; do not delete TLS/digest/ClusterIP/Restricted-PSA pins
- `_bmad-output/implementation-artifacts/deferred-work.md` -- DW-729 (`31.1-CR-NP-SPEC`) stays open; do not reclaim `DW 27.3-CR17`
- Do not change: `deploy/openbao/*.yaml` unless a doc bind forces a comment-only clarification; `MeasuredRaftVoters` / `MeasuredHaMode`

## Tasks & Acceptance

**Execution:**
- [x] `docs/operations/openbao.md` -- qualify ClusterIP/"no ingress" to the four `hexalith-keys*` Services; add untracked-state rows for `deployment-seal-transit`, `deployment-seal-external`, and `deployment-seal-runner-token` with owner Platform Operations (`jpiquot`) and reopen triggers; keep CRLF
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` -- bind ClusterIP qualification in `OperationalSections_StayBoundToTheirRecordedRemediations`; pin the three untracked keys in `NamedDivergencesAndUntrackedState_CarryOwnerAndReopenTriggerPerRow`; harden `HelmRowMustRecordTheCarveOut` so `does not reach done until` fails after markdown strip
- [x] `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` -- restate C2 review state to the measured-gap half (helm is not a 31.1 `done` gate); append a 2026-09-08 completion-note correction; add a Change Log row with `-list methods` discovery and File List `matched N/N`; check off the five patches
- [x] `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` -- mark §8.2 ClusterIP/PVC as unprobed; point §8.8 at the untracked-state rows; keep DW-729 for the uncaptured `.spec`

**Acceptance Criteria:**
- Given `docs/operations/openbao.md`, when Health, the `server.service.type: ClusterIP` bind cell, and the seal compensating-control cell are read, then ClusterIP/"no ingress" is scoped to `hexalith-keys*` and does not deny `deployment-seal-transit`.
- Given C2 and helm rows, when review state and `HelmRowMustRecordTheCarveOut` run, then helm empty-diff is not a Story 31.1 `done` gate, including the phrase `does not reach done until` after markdown strip.
- Given the 2026-09-06 completion notes, when the story file is read, then a later dated note records the doc/health/sprint-status edits those notes omitted, and a new Change Log row has discovery plus `matched N/N`.
- Given evidence §8.2, when it is read, then ClusterIP/PVC are not presented as kubectl-proven.
- Given the untracked-state table, when it is read with the guard, then it names `deployment-seal-transit`, `deployment-seal-external`, and `deployment-seal-runner-token` with owner and reopen trigger, and is not a third accepted limitation.
- Given C4b, C5b, and C7, when this slice finishes, then those states are unchanged and sprint-status is not `done`.

## Implementation Notes

2026-09-08 review-patch close-out. ClusterIP/"no ingress" scoped to `hexalith-keys*`. Three untracked-state rows added. C2 review state restated so helm empty-diff is not a Story 31.1 `done` gate. Evidence §8.2 ClusterIP/PVC marked unprobed. C4b/C5b remain `not complete`; C7 remains waived until 2026-10-26.

## Spec Change Log

- 2026-09-08: Executed the review-patch close-out path. Execution checkboxes marked complete. Story status left `in-progress`.

## Review Triage Log

- **medium** BH2/EC3 — `ClusterIpClaimMustStayScopedToHexalithKeysServices` already requires `hexalith-keys*` in the cell, then only checks that a `no ingress` sentence also contains `hexalith-keys`. Restoring `the four hexalith-keys* Services are ClusterIP; no ingress exists` stays green. Verified at `OpenBaoPlatformDocumentationTests.cs:1008-1025`.
- **medium** BH3 — Health expected results still require the four `hexalith-keys*` Services to remain ClusterIP and never name `deployment-seal-transit` NodePort `8200:30820`, so `kubectl get service` showing a NodePort looks like a health failure. Verified at `docs/operations/openbao.md:410-412`.
- **medium** BH5 — `ShouldContain("matched N/N")` is satisfied by the 2026-09-08 completion-note sentence; deleting Change Log `Matched **5/5** this-phase paths` stays green. Historical 2026-07-28 rows already contain other `Matched **N/N**` spellings. Verified at `OpenBaoPlatformDocumentationTests.cs` story assertions vs story Change Log 2026-09-08.
- **medium** BH15 — §8.8 notes for `deployment-seal-external` still say "that policy's spec is unchanged", which reads as if the extra NetworkPolicy were measured, while `.spec` is DW-729 / uncaptured. Verified at `31-1-openbao-platform-evidence.md:833`.
- **low** BH1 — 2026-09-08 Change Log `matched 5/5` names three planning exclusions while the worktree also has concurrent CI/Dapr/PRD/FrontComposer changes. Ledger readers can mis-attribute that tree to this slice. Direct named-exclusion sentence.
- **false** BH4 — selector/endpoints for `deployment-seal-transit` were never in the 2026-09-06 transcript; frozen intent forbids extra kubectl, so this close-out cannot invent them.
- **false** BH14-epic — `epic-31-context.md` restart-recovery/live-vault sentences came from compile-epic-context at workflow start, not from this spec's File List or tasks. Story 31.2 already owns the migration proof.
- **defer** BH6/EC4/VG1 — sibling WebAppFactory hosts omit `ApplicationTokenOverride`. Concurrent Dapr-token overlay work in this worktree, not Story 31.1. Settled by adding the EventStore/HealthCheck overlay to Telemetry/Consistency/MCP factories.
- **defer** BH7/EC1 — empty `ApplicationTokenOverride` skips token checks including Production. Concurrent middleware change; not this spec.
- **defer** BH8 — `DaprTokenEnvironmentCollection` comment vs double `UseSetting`. Concurrent Dapr test-host work.
- **defer** BH9/EC2 — `init-dapr-runtime.sh` does not wait for placement/scheduler ports. Concurrent CI work.
- **defer** BH10 — addendum release-date contradiction vs later PRD handoff. Concurrent planning review.
- **defer** BH11 — README missing MIT pledge required by `prd.md`. Concurrent PRD close-out.
- **defer** BH12 — G1 protocol has no owning story. Concurrent PRD rewrite.
- **defer** BH13 — Aspire fixture post-reconnect does not call OpenBao sidecar matrix wait. Concurrent integration-test work.

## Design Notes

Keep `ShouldBindManifest(..., "ClusterIP")` — it still binds the chart's Service type. Qualify the **prose** that overclaims every Service in the namespace. Append-only for 2026-09-06 notes and Change Log; do not backfill that row. Extra objects join the untracked-state table, not accepted limitations. `deployment-seal-external` reopen may name DW-729 until `.spec` is captured.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: build succeeded
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo` -- expected: all facts pass
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Deployment.ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal -parallel none -noLogo` -- expected: pass
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` -- expected: recorded in the new Change Log row

**Manual checks (if no CLI):**
- Confirm `sprint-status.yaml` key `31-1-openbao-platform-hardening-and-documentation` is not `done`.
- Confirm evidence contains no Secret `.data`.
