# Story 31.1 Create-Story Scope Evidence

Generated: 2026-07-28

## Declared Baseline Snapshot

- Repository baseline: `327d1a9d7eaef063c656a6af9df4eea84f47ca30`.
- Pre-create `_bmad-output/implementation-artifacts/sprint-status.yaml` SHA-256: `be1272f1e43cef4ae01829056442678a8b62b0dbef70bc74a4fb8448fccb3e20`.
- Story file state: absent.
- Evidence file state: absent.
- Story-owned sprint values before create-story:

```yaml
epic-31: backlog
31-1-openbao-platform-hardening-and-documentation: backlog
31-2-runtime-dapr-secret-store-migration: backlog
```

## Story-Owned Diff

```diff
- epic-31: backlog
- 31-1-openbao-platform-hardening-and-documentation: backlog
+ epic-31: in-progress
+ 31-1-openbao-platform-hardening-and-documentation: ready-for-dev
  31-2-runtime-dapr-secret-store-migration: backlog
```

Post-create `sprint-status.yaml` SHA-256: `3e19d80eee3fd19df8b030fd8a4e3380b9847a3a885a7293d6b6f8bc7ea5c8ff`.

`epic-31` moved to `in-progress` because Story 31.1 is the first story created in Epic 31, per the create-story epic-transition rule. Story 31.2 was left at `backlog`; its activation gate requires Story 31.1 to be `done`.

## Reconciled Name-Status Set

```text
?? _bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md
 M _bmad-output/implementation-artifacts/sprint-status.yaml
?? _bmad-output/implementation-artifacts/tests/31-1-create-story-scope-evidence.md
```

Matched File List: 3/3.

Comparison commands: `git status --porcelain` for added working-tree files and `git diff --name-status 327d1a9d7eaef063c656a6af9df4eea84f47ca30` for modifications.

## Named Exclusions

The worktree was already dirty at creation. Every path below is owned by Jérôme Piquot, belongs to concurrent Story 27.3 and Epic 30/31 planning work, and is neither edited nor credited by Story 31.1:

- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-c1-blocked-gate-split-and-c3-c4-ratification.md`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py`
- `tools/verify_access_telemetry_lifecycle.py`

## Runner-Derived Test Baseline

Named unit: **xUnit test method**.

Build (0 warnings, 0 errors):

```text
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

Discovery:

```text
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo
```

| Scope | Baseline methods |
| :---- | ---------------: |
| `Hexalith.Memories.Server.Tests`, all methods | 2,190 |
| `Hexalith.Memories.Server.Tests.Deployment` namespace | 48 |
| `ProductionDeploymentArtifactsTests` | 9 |
| `OperationalRunbookSetTests` | 9 |
| `DeploymentConfigurationContractTests` | 7 |
| `OpenBaoPlatformDocumentationTests` | 0 (class absent) |

- Sorted method-set SHA-256: `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`
- Built assembly SHA-256: `3d87d277470fc3f419f5946724c33e528c102dea361e16610c7b12161d09c30c`

Actual create-story phase delta: **+0**. Cumulative: **+0**. Planned Story 31.1 delta: **+6..10** methods, all in the `Deployment` namespace. Planned values are not evidence.

## Deployed-Platform Probe Record

Read-only probes were run against Kubernetes context `jpiquot@local`, namespace `openbao`, on 2026-07-28 to establish the drift table in the story's `### Deployed-State Drift Measured At Creation`. No Secret contents were read, printed, or stored; `kubectl get secret` was invoked for names and types only. No cluster state was mutated.

The measured drift is a creation-time observation, not story evidence. Story 31.1 Task 1 re-derives all of it before any documentation is written.

## Amendment 2026-07-28 — correct-course

The `Matched File List: 3/3` and `Reconciled Name-Status Set` above record the **create-story** phase and remain accurate for it. They were superseded the same day by the `correct-course` phase, which ratified the Story 31.1 AC2 deployed-profile qualifier under `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md`.

That phase raised the cumulative story-scoped set to **5/5** by adding:

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md` (new)
- `_bmad-output/planning-artifacts/epics.md` (**partial ownership** — Story 31.1 owns only its three hunks at the Story 31.1 acceptance criteria and Implementation-evidence clause; the remainder of that file's diff is Jérôme Piquot's concurrent 2026-07-28 Epic 27 work)

A follow-up read-only probe (`kubectl get nodes -o wide`; `kubectl -n openbao get pods -o custom-columns=NAME:.metadata.name,NODE:.spec.nodeName`) confirmed the cluster has exactly one node, `node1` (`control-plane,worker`, `v1.34.9`), with all three OpenBao voters co-located on it. That measurement is what narrowed the amendment: "single-node" was retained where it describes the hosting and failure domain, and dropped where it wrongly described the Raft voter count.

Test discovery was not re-run for that phase and the 2,190-method baseline is unchanged; no test project, test source, or build input was touched. The authoritative reconciliation for the amendment is the `correct-course` row in the story's Change Log.
