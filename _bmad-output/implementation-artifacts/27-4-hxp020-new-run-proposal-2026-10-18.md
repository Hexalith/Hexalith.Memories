## 2026-10-18 additional authorized cache cleanup — 09:45:47Z

The owner asked **“can we free more?”**. Removed 970 unchanged, inactive regular cache files from `/home/administrator/.cache/dotnet/Containers/Content` (SDK container layer cache) and `/home/administrator/.vscode-server/data/CachedExtensionVSIXs` (downloaded editor installers). Files modified within 30 minutes, directories and symlinks were excluded; cache file identities, protected-file inode separation, active container-publish commands and open descriptors were checked before removal. Installed editor servers/extensions remain. `dotnet nuget locals http-cache --clear` exited0; installed NuGet packages remain. No Docker resources, workloads, retained run data or source files were cleaned.

Linux available space rose from25,977,094,144 to35,141,824,512 bytes: **8.535GiB additional available space;32.728GiB available** at09:45:47Z. Capacity can change with other active work. All376 protected failed-run/candidate files still match their prior sizes and hashes, including all63 historical failed-run files. The private plan, removed-file metadata, NuGet command output and hash verification result are retained under `/home/administrator/.local/state/hexalith/memories/access-telemetry/disk-cleanup-extra-20261018T094527Z`.

**The source-verification hold remains unchanged.** No functional invocation was consumed; the dispatch marker remains absent. The externally advanced dependency graph still requires rebinding and verification before any amended source/run decision. The original10:00Z admission cutoff is unchanged. The failed run's retained/unverified census, open A41, Story27.4 and Production remain unchanged. Earlier cleanup measurements below describe their recorded times.

## Current hold after authorized cleanup — 2026-10-18T09:39Z

Cache cleanup recovered17.918GiB;24.865GiB was available, satisfying the20GiB staging gate. All376 protected files match their prior hashes. However, external Platform commit `fee359fd4b587af1432f534a647cec6a0fe899b4` moved13 direct submodules after the approved source binding, including executable source changes. The old approval cannot admit this new graph without rebinding, verification and an amended source/run decision. No runtime invocation was consumed and no source revision was reverted. The original10:00Z cutoff remains unchanged. See the consolidated evidence for exact cleanup commands, capacity measurements and source-custody records. Earlier capacity holds below are historical.

## Latest capacity recheck — 2026-10-18T09:23:07Z

The owner's cleaning follow-up was checked read-only. The runner's Linux filesystem currently has2.750GiB available,17.250GiB below the20GiB staging budget. Windows C: is a separate filesystem. Candidate artifacts and all63 failed-run files remain unchanged; no new invocation was dispatched or consumed. The existing approval's10:00Z admission cutoff remains unchanged. See the latest consolidated evidence entry for the exact command and retained receipt. Earlier capacity figures below describe their earlier observations.

## Approval and current admission hold — 2026-10-18

**Approved by the owner's “I approve”; one invocation remains unused.** The exact original proposal below is preserved in private `approved-invocation-20261018T0851Z/approved-proposal.md`, SHA-256 `dbf4e0fa60e9ac4d30ed330eb77f6e5c2fd593b08af961f29359792219e4e395`. Its09:00–10:00Z admission window,14400/600/900-second limits and14:15Z stopping cutoff are unchanged.

Read-only admission refused before dispatch: the Linux workspace filesystem had8.598GiB available against the20GiB staging budget. `preflight-r2.py` exited1 with `insufficient staging/backing disk space`; all refusals remain retained. The one-shot dispatch marker is absent, there is no new runtime runId, and the old failed state/resource verdict is unchanged. The current [consolidated evidence](tests/27-4-retention-verification-evidence.md) records source/model/tool verification and exact capacity observations. Restore sufficient free Linux capacity, then repeat fresh admission within this window; the same unchanged scope needs no repeat approval. After10:00Z the proposal expires.

The candidate environment also binds runner-required `HEXALITH_DAPR_HOME` to the same approved Dapr directory named below; the proposal's `DAPR_RUNTIME_PATH` spelling alone would not configure the runner. No runtime/tool bytes, actions or bounds changed. This is recorded in the private source classification and admitted binding. Historical “not approved” wording below describes the proposal before the owner's subsequent approval.

# Story 27.4: proposed new invocation after HXP020 repair

**Proposal only — execution is not approved.** The previous additional invocation is consumed. This document neither renews its window nor changes failed run `f482f16095f3027867cec221b0a35220`, Story 27.4, A41, C0–C6 or Production settings.

The technical child-loader blocker has an offline verified owning fix in `/home/administrator/projects/hexalith/platform/references/Hexalith.Builds`, based on HEAD `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9` plus the retained uncommitted patch. `Tools/build-g4-tool-packages.ps1` now passes the same Version/PackageVersion to restore, solution build and pack. Its new gate compiles a fixture against CLI Tooling and runs the real packaged AppHost `--validate-platform` child before successful package inventory. No dependency, loader redirect or accepted version range changes.

Private review bundle: `/home/administrator/.local/state/hexalith/memories/access-telemetry/loader-fix-20261018` (abbreviated `R` below). `R/reviewed-source-binding.json` and `R/reviewed-owning-source.patch` bind the exact source files. Source edits remain uncommitted; no gitlink, branch or HEAD was moved. Other Platform planning/evidence changes are external and must be classified afresh before admission.

| Candidate artifact | SHA-256 |
| --- | --- |
| `R/fixed-packages/Hexalith.Builds.Module.Cli.4.31.2.nupkg` | `0b550b61d457337daba56184a7b3facd23001144f1ea9bddf3092a6ce485770d` |
| CLI/root and child Tooling bytes | `8285cbea14e5609b8d7b0e57e471fd38bfd8c8475cd280f4e7b6eb08ea457a06` |
| Exact retained Platform model used in isolated child verification | `c50613998a55e69318ba6c877b0400ad9d9af96f44f37f1ad6406adf5a3c5087` |
| Reviewed fixture child receipt (`R/reviewed-fixed/loader-result.json`) | `77ecb51bdf1bcdde9f221ae7b8e4483778276a961868ae249ccb5ee03ab70678` |
| Exact-model child receipt | `9787f8e787a607c1fc49c029051f775b5ed1430a4b34c1cd1517a2f7a83dca3c` |

The candidate remains local, unsigned, unpublished diagnostic4.31.2, outside Platform's accepted `[4.33.0,5.0.0-0)` range. HXW010 and `releaseEligible:false` remain. A new approval must explicitly permit this diagnostic artifact and exact source state for this experiment; this proposal grants no package acceptance/release credit.

## Proposed scope and bounds

Propose **one** new invocation of the existing `functional-retention` test, using fresh runner-owned runId/workspace, Development/source mode, the existing isolated PostgreSQL/TLS profile, two 125-record cohorts and a real one-hour observation. No automatic retry. No Production or general/full qualification scope. The old run identity and workspace may not be reused.

Proposed absolute admission window: **2026-10-18T09:00:00Z through 2026-10-18T10:00:00Z**. If approval and admission cannot complete within it, this proposal expires. Per-invocation execution ceiling14400s, startup600s within execution, and separate stopping/restoration ceiling900s. Latest absolute stopping cutoff **2026-10-18T14:15:00Z**. These are new proposed limits, not an extension of any prior approval. Reuse the prior experiment's 8-vCPU/40-GiB-RAM, 40/600-GiB backing disks and20-GiB staging bounds only after fresh capacity observations; capacity is not reserved.

Proposed command, **do not execute without separate approval**:

```bash
cd /home/administrator/projects/hexalith/platform
env ASPNETCORE_ENVIRONMENT=Development DOTNET_ENVIRONMENT=Development \
  DAPR_RUNTIME_PATH=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
  dotnet /home/administrator/.local/state/hexalith/memories/access-telemetry/loader-fix-20261018/fixed-packages/platform-loader/package/tools/net10.0/any/Hexalith.Builds.Module.Cli.dll \
  test --manifest /home/administrator/projects/hexalith/platform/references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source --output json
```

Preserve the admitted caller PATH and pinned Aspire/Dapr identities. The installed extracted payload above is the new locally verified package, not the old diagnostic installation. Bind its CLI, AppHost, deps/runtimeconfig, Tooling, package and source hashes in the new approval record. Current independently observed Platform HEAD is `d1eb16166ee1a62b791a3201cdcd689f33f4fa8f`; Memories primary HEAD when repair began was `fb39a0c59cc3befd3176dec5e3c790f94d0e5bd1` (reobserve during fresh admission). The source graph must be reobserved rather than copied from the failed run.

## Required admission and retained-resource decision

1. Owner explicitly approves the exact package/source diff, diagnostic exception, one invocation, window/bounds, private local custody/self-review, and retained-resource policy. Capture full current HEADs and dirty-file hashes across the19 source roots, all17 direct HEAD/index pairs, original caller PATH and exact argv/cwd/environment. Refuse unexplained drift. Do not silently move dependency pins or restore4.33 tooling.
2. Rebuild/bind the current source-mapped model with this exact Tooling and recheck the packaged child in separate offline scratch; the retained `c506…` success proves this boundary for those bytes only. Preserve the existing declaration/helper/profile hashes and verify the live-helper admission fields/timing before launch. No old cohort timestamp may be reused.
3. Keep the old terminal verdict **resourceDisposition=retained**, **remainingResources={containerIds:[],processIds:[],unverified:true,isEmpty:false}**. Approval may authorize a fresh read-only census for isolation; retain it as a new observation alongside the old unverified verdict. If old ownership/quiescence or port/resource separation cannot be established, refuse the new invocation. No down, manual deletion, blanket cleanup or fresh-budget cleanup of the old run is included.
4. Recheck actual tool versions, source/artifact hashes, capacity/backing storage and candidate run-store/ownership conditions immediately before dispatch. Retain preflight refusals without retrying the operational command. Claim the one new invocation at exec, once, only after all gates pass.
5. Apply existing FR4 ownership, failure retention and bounded stopping/restoration to the **new** run only. Any new cleanup grant must identify that new run and its exact owned resources; an ambiguous old/new resource set requires another owner decision. Preserve terminal state/diagnostics and report actual retained/cleaned/remaining resources without inferring an empty census.

Approval of a new functional experiment would not close A41 or satisfy Story27.4/full/C1/Production acceptance. Those decisions require their own actual technical evidence and authority. Until separately approved, no functional-retention/up/test/down, workload, resource census or cleanup is dispatched by this repair.
