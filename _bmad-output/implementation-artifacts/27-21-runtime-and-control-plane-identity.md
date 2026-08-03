---
baseline_commit: 3f758f9ab019ca64a793e268470a7e4663cbc1fa
---

# Story 27.21: Runtime and Control-Plane Identity

Status: backlog

## Story

As a Deployment Adapter Developer,
I want one read-only C1.15 producer for the running access-telemetry lifecycle workload,
so that an independent reviewer can evaluate runtime and control-plane identity without another gate being inferred or discharged.

This is the first successor transaction. It introduces the neutral shared runner and packet scaffold, implements only the C1.15 mode, and discharges no other gate.

## Acceptance Criteria

1. **Given** one or more ready, running pods selected only by `app.kubernetes.io/name=memories-access-telemetry`, **when** the literal C1.15 producer runs, **then** it emits a new immutable JSON packet for `C1.15` / `PG-ONPREM-1` containing the lifecycle app ID, Dapr runtime version, daprd `imageID` digest, Scheduler connected addresses, actor types, enabled features, and explicit runtime values for `AccessTelemetryLifecycle__ComponentIsAlpha` and `AccessTelemetryLifecycle__AllowAlphaComponent`; the packet includes source and command hashes, reads metadata with the in-container `DAPR_API_TOKEN` without exposing that value, records `producerStatus: observed`, and keeps `gateStatus: not-evaluated` and `productionGatePassed: false`.
2. **Given** no running lifecycle pod, a partial or malformed observation, a wrong lifecycle app ID, an invalid alpha pair, or secret-shaped probe output, **when** the producer runs, **then** it writes a secret-safe blocker packet, exits nonzero, keeps the gate `not-evaluated`, and never queries or falls back to a Server pod.
3. **Given** an unsupported gate value, **when** the runner is invoked, **then** PowerShell parameter validation rejects it before any producer or `kubectl` command runs. Registering this story as `backlog` and proving its producer fixture do not pass C1.15, enable Production lifecycle writes, advance Story 27.4, or close A41.

## Tasks / Subtasks

- [x] Add the neutral C1 runner/packet scaffold with only literal mode `C1.15`.
- [x] Add a focused fake-target fixture for complete, blocked, malformed, secret-shaped, and unsupported-gate paths.
- [x] Record one owner, one command/artifact, one independent reviewer, and one incomplete checkpoint for C1.15.
- [x] Reconcile C1.15 ownership in the Epic 27 context and deferred-work register without claiming a Production pass.
- [x] Register only Story 27.21 as `backlog` after the focused fixture, story-slice guard, and whitespace check pass.

## Exact Evidence Command

```powershell
pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-1 -EvidenceDirectory ./artifacts/access-telemetry-c1/C1.15
```

The command is a read-only observation producer. An exit-zero packet proves capture completeness only. C1.15 stays pending and not complete until the real command runs against the approved running target and an independent reviewer evaluates its packet.

## Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Approved Sprint Change Proposal 2026-08-01, Annex A C1.15 row | `historical-reference-only` | Requirement and evidence semantics for the C1.15 observation only; it supplies no current ownership, producer, or pass state. |
| Withdrawn Stories 27.5/27.6 and their bundled successor definitions | `anti-template` | Split provenance only; never reuse their tasks, acceptance-criterion density, checkpoint grouping, or proof shape. |
| Story 27.3 adapter-profile collector and packet mechanics in current source | `current-narrow-pattern` | Reverified use is limited to daprd `imageID`/version capture and secret-safe hash-ledger mechanics; the broad Story 27.3 packet is not C1.15 proof and its Server target is never reused. |

## Slice Proof

This story owns exactly one independently demonstrable gate. The row is also the complete checkpoint table for this story.

| Gate | Single outcome | Accountable owner | Literal evidence command / artifact | Independent reviewer | Review state | Completion state |
| :--- | :------------- | :---------------- | :---------------------------------- | :------------------- | :----------- | :--------------- |
| C1.15 | Runtime and control-plane identity captured from the running `memories-access-telemetry` target | Deployment Adapter Developer | `pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-1 -EvidenceDirectory ./artifacts/access-telemetry-c1/C1.15`; resulting immutable JSON packet | Independent code/planning reviewer | pending | not complete |

## Dev Notes

- The approved execution order places identity Stories 27.21-27.24 before the capability/evidence successors. This transaction creates only Story 27.21 and only C1.15.
- Dapr `enabledFeatures` and the lifecycle options `ComponentIsAlpha` / `AllowAlphaComponent` are separate observations. An empty but present feature list is a valid observation; either missing alpha value is blocking.
- The producer executes the authenticated metadata request inside the lifecycle container. The bounded raw response crosses `kubectl exec` only into runner memory, is secret-scanned, and is discarded; only strictly validated allowlisted fields and their projection hash enter the packet. No token value is accepted into a packet, blocker, transcript, source hash, or command ledger.
- The checked-in Production manifest keeps the lifecycle Deployment at zero replicas and does not yet set the explicit alpha pair. The producer therefore exists before real C1.15 evidence can exist; this is expected and is not a Production pass.

### Epic AC Verification

Verified 2026-08-03 against worktree baseline `3f758f9ab019ca64a793e268470a7e4663cbc1fa` and the bounded Story 27.21 transaction.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "Story 27.21 maps only C1.15 — runtime/control-plane identity." | Existence/location | `rg -n '^\| 27\.21 \| C1\.15 — runtime/control-plane identity' _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md` | The approved mapping contains exactly the Story 27.21 / C1.15 row and names its six identity families. | `confirmed` |
| "The running target is the lifecycle app ID `memories-access-telemetry`." | Location | `rg -n -e 'app.kubernetes.io/name: memories-access-telemetry$' -e 'dapr.io/app-id: memories-access-telemetry$' -e 'name: lifecycle$' deploy/kubernetes/base/access-telemetry-deployments.yaml` | The Deployment label, Dapr app ID, and lifecycle container are present under the named workload. | `confirmed` |
| "The checked-in Production target is not currently able to supply running C1.15 evidence." | Behavioral | `test "$(rg -c '^  replicas: 0$' deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml)" -eq 2 && ! rg -q -F -e 'AccessTelemetryLifecycle__ComponentIsAlpha' -e 'AccessTelemetryLifecycle__AllowAlphaComponent' deploy/kubernetes/base/access-telemetry-deployments.yaml` | Both lifecycle Deployments are zero-scaled by the Production patch and the explicit alpha environment pair is absent from the base workload. | `confirmed` |
| "The runner accepts only literal C1.15 and never marks the Production gate passed." | Behavioral | `rg -n -F -e "ValidateSet('C1.15')" -e "gateStatus = 'not-evaluated'" -e 'productionGatePassed = $false' tools/verify-access-telemetry-c1.ps1` | One literal gate is accepted; every observed or blocked packet stays `not-evaluated` with `productionGatePassed = false`. | `confirmed` |
| "The focused fixture emits every required observation, fails incomplete evidence closed, avoids Server fallback, and excludes the token canary." | Behavioral | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'runtime_control_plane_identity_test.py' -v` | Twelve test methods pass across complete, disabled, partial, malformed, secret-shaped, authentication, timeout, multi-pod drift, alpha, empty-feature, wrong-context, readiness, and unsupported-gate cases. | `confirmed` |
| "All twenty-five running-target C1 gates are held without a registered story owner." | Quantitative/existence | `rg -n -F -e 'all twenty-five' -e 'All twenty-five' -e 'Story 27.21' -e 'remaining twenty-four' _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/{sprint-status.yaml,epic-27-context.md}` | This transaction registers Story 27.21 as the sole owner of C1.15; the remaining twenty-four C1 gates stay held and unowned. The planning and compiled-context copies are corrected in the same transaction. | `corrected` |

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'runtime_control_plane_identity_test.py' -v` — 12 tests passed.
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CiWorkflow_RunsEveryToolingFixtureSuiteThatGuardsAShippedTool' --noLogo` — 1 test passed; the new tooling lane is wired into CI.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-21-runtime-and-control-plane-identity` — required before registration and rerun after registration.
- `git diff --check` — required before registration and rerun after registration.

## Dev Agent Record

### Completion Notes

- Implemented only the C1.15 observation producer and a neutral packet envelope.
- Fixture success establishes producer behavior, not the Production gate result.
- Story status and checkpoint state remain `backlog`, `pending`, and `not complete`; Production lifecycle writes, Story 27.4, and A41 remain unchanged.

### File List

- `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md`
- `.github/workflows/ci.yml`
- `tools/verify-access-telemetry-c1.ps1`
- `tests/tooling/access_telemetry_c1/fixtures/c1_15_complete.json`
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py`
- `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-08-03 | create-story | Created the one-gate C1.15 story, producer, fixture, and conditional backlog registration; no Production pass or write enablement. | Creation baseline records 6 discovered test methods; phase delta `+0`, cumulative story delta `+0`; exact evidence: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'runtime_control_plane_identity_test.py' -v` (6 passed). | `matched 9/9` story-scoped paths from `git status --short -- _bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md tools/verify-access-telemetry-c1.ps1 tests/tooling/access_telemetry_c1/fixtures/c1_15_complete.json tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py _bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md _bmad-output/implementation-artifacts/epic-27-context.md _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/sprint-status.yaml`; unrelated pre-existing dirty paths and unrelated pre-existing hunks in `epics.md` are user-owned and outside this scoped comparison. |
| 2026-08-03 | code-review | Applied bounded review patches for authenticated-fixture enforcement, timeout handling, strict metadata schema/bounds, stable-pod readiness, complete multi-pod drift detection, allowlisted source-hash provenance, read-only packet creation, exact hash assertions, and CI adoption. C1.15 remains unevaluated. | Review phase delta `+6`, cumulative story delta `+6`; comparable discovery `6 -> 12` test methods under `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'runtime_control_plane_identity_test.py' -v` (12 passed). CI inventory: 1/1 passed under the exact focused `dotnet test` command recorded above. | `matched 10/10` story-scoped paths from `git status --short -- .github/workflows/ci.yml _bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md tools/verify-access-telemetry-c1.ps1 tests/tooling/access_telemetry_c1/fixtures/c1_15_complete.json tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py _bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md _bmad-output/implementation-artifacts/epic-27-context.md _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/sprint-status.yaml`; unrelated pre-existing dirty paths and unrelated pre-existing hunks in `epics.md` remain user-owned and outside this scoped comparison. |
