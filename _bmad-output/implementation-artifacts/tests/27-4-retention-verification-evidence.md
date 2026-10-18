## 2026-10-18 additional authorized cache cleanup — 09:45:47Z

The owner asked **“can we free more?”**. Removed 970 unchanged, inactive regular cache files from `/home/administrator/.cache/dotnet/Containers/Content` (SDK container layer cache) and `/home/administrator/.vscode-server/data/CachedExtensionVSIXs` (downloaded editor installers). Files modified within 30 minutes, directories and symlinks were excluded; cache file identities, protected-file inode separation, active container-publish commands and open descriptors were checked before removal. Installed editor servers/extensions remain. `dotnet nuget locals http-cache --clear` exited0; installed NuGet packages remain. No Docker resources, workloads, retained run data or source files were cleaned.

Linux available space rose from25,977,094,144 to35,141,824,512 bytes: **8.535GiB additional available space;32.728GiB available** at09:45:47Z. Capacity can change with other active work. All376 protected failed-run/candidate files still match their prior sizes and hashes, including all63 historical failed-run files. The private plan, removed-file metadata, NuGet command output and hash verification result are retained under `/home/administrator/.local/state/hexalith/memories/access-telemetry/disk-cleanup-extra-20261018T094527Z`.

**The source-verification hold remains unchanged.** No functional invocation was consumed; the dispatch marker remains absent. The externally advanced dependency graph still requires rebinding and verification before any amended source/run decision. The original10:00Z admission cutoff is unchanged. The failed run's retained/unverified census, open A41, Story27.4 and Production remain unchanged. Earlier cleanup measurements below describe their recorded times.

## 2026-10-18 authorized cache cleanup completed; source drift now holds admission

The owner asked **“can we do a cleanup?”**. Only regenerable caches were cleared with their own tools: `npm --cache /home/administrator/.npm cache clean --force`; `python3 -m pip --isolated --cache-dir /home/administrator/.cache/pip --disable-pip-version-check cache purge`; and `uv --offline --no-config cache clean --cache-dir /home/administrator/.cache/uv`. All three exited0. uv retained its normal in-use checks. npm's local implementation was checked to target `_cacache`; `_npx` runtime files remain. Source trees, installed dependencies, NuGet packages, SDK/container caches, Docker resources/volumes and retained evidence were not deleted by this cleanup.

Measured Linux available space rose from **7,459,323,904 to26,698,326,016 bytes**: **17.918GiB recovered;24.865GiB available** at09:37:32Z. This clears the20GiB staging capacity gate. Apparent cache file sizes were larger than physical reclaimed space because installed environments retain shared/hardlinked files. All376 protected failed-run/candidate files were rehashed and match; the63 historical failed-run files remain unchanged. Private commands, logs and readback receipts are under **`/home/administrator/.local/state/hexalith/memories/access-telemetry/disk-cleanup-20261018T0933Z`** (`cleanup-plan.json`, individual command receipts and `cleanup-result.json`).

**No functional invocation was consumed.** The subsequent source check found externally created Platform commit **`fee359fd4b587af1432f534a647cec6a0fe899b4`**, `build(deps): update reference submodules to latest main`, after approved Platform `0d4c6dd91a0e5762b13dddb722e130f42320c062`. Thirteen direct submodule HEADs/index references changed. The new19-root observation covers125,633 files and includes changed executable sources in multiple owners, including EventStore, Tenants and Works; this is not the earlier documentation-only drift. The retained `source-after-cleanup.json` and full delta are under the approved-invocation custody root; `post-cleanup-source-hold.json` and the compact `source-drift-summary.json` retain exact old/new revisions and changed-path counts.

The existing approval binds the earlier source graph and model. Its runtime invocation remains unused; it does not admit this externally advanced graph without rebinding and verification of concrete current artifacts and an amended source/run decision. The source updates are preserved, with no rollback, dependency modification, automatic tool upgrade, workload launch or resource cleanup. The old terminal **retained / unverified:true / isEmpty:false** verdict, open A41, Story27.4 and Production remain unchanged. The original10:00Z admission cutoff is not extended by cleanup or this hold.

## 2026-10-18 cleaning follow-up: capacity still below admission budget

After the owner reported cleaning, a fresh read-only recheck at09:23:07Z still observed **2,953,027,584 available bytes (2.750GiB)** on `/dev/sdd`, the Linux filesystem holding both `/home/administrator/projects/hexalith/platform` and `/tmp`. The command `df -B1 /home/administrator/projects/hexalith/platform /tmp /mnt/c` exited0. Windows C: had76,551,634,944 available bytes on a separate filesystem; it does not supply space to the runner's current Linux workspace. The20GiB admission staging budget remains short by17.250GiB. The complete preflight/operational command was not repeated after this prerequisite failed.

Private receipt: `loader-fix-20261018/approved-invocation-20261018T0851Z/capacity-recheck-20261018T0922Z/capacity-recheck.json`, beneath the existing private access-telemetry root. Accessible same-owner `/proc` file-descriptor metadata showed no deleted-open file of at least100MiB on the workspace device; memory-backed memfd mappings are excluded from that disk observation. This is a scoped inspection, not a complete cross-user census or cleanup grant.

All63 original failed-run files and all bound candidate artifacts remain byte-identical after the reported cleaning. The dispatch marker remains absent: **no new invocation consumed, no workload started**. Approval is still valid for fresh admission before10:00Z under unchanged source/artifacts/actions/bounds; this recheck does not extend it. The historical failed run's retained/unverified verdict, A41, Story27.4 and Production are unchanged. No agent cleanup or file deletion occurred.

## 2026-10-18 new invocation approved; admission held on Linux staging capacity

The owner replied **“I approve”** to the HXP020 new-run proposal. The approval was observed at08:51:24Z and binds the retained proposal SHA-256 `dbf4e0fa60e9ac4d30ed330eb77f6e5c2fd593b08af961f29359792219e4e395`: one diagnostic4.31.2 Development/source functional-retention invocation, admission09:00–10:00Z, execution14400s/startup600s/stopping900s, latest stopping14:15Z, sole-owner/self-reviewed private custody, and preservation of the old failed run. No further policy approval is required for that unchanged scope within its window.

**No operational invocation was dispatched or consumed.** The read-only command `python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/loader-fix-20261018/approved-invocation-20261018T0851Z/preflight-r2.py` exited1 before dispatch with `AssertionError: insufficient staging/backing disk space`. At09:00:06Z, the Linux filesystem containing `/home/administrator/projects/hexalith/platform` had **9,232,289,792 available bytes (8.598GiB)**, below the20GiB staging budget used for admission. Windows backing volume had78,117,236,736 available bytes, and Linux MemAvailable was64,138,113,024 bytes; those do not increase the Linux workspace's available capacity. No data was removed, storage relocated, staging budget reduced or operational command retried.

Private continuation: **`/home/administrator/.local/state/hexalith/memories/access-telemetry/loader-fix-20261018/approved-invocation-20261018T0851Z`**. `approval.json` and `approved-proposal.md` preserve the original approval binding; `admitted-binding.json` records exact candidate argv/cwd/environment and all303 verified package payload entries. `capacity-refusal-details.json`, both `preflight-result*.json` refusals and `approval-admission-outcome.json` retain the measured hold. The first census refused three unreadable environments; read-only identity inspection classified exact boot-time user systemd/PAM and a non-running zombie Git process before the separate R2 preflight. No process was terminated and no old census was rewritten.

The19-root source snapshot covers114,750 files; all17 direct HEAD/index pairs agree. Platform is now `0d4c6dd91a0e5762b13dddb722e130f42320c062`, after two externally committed documentation/planning/evidence-only changes. The explicit source classification finds no unrelated executable changes beyond the six approved Builds script/test files. The current installed resolver/materializer/model builder passed in28.693s: mapping `b60cb2537e1bba8de1953f4dc65a369bf6168285e8e951268814fc7a7c8c3d88`, model SHA-256 `e70f764e38a7098188d7fd1fa029ecb0340eb645ce53a8d2c7258fbadd506b05`. Its packaged validation child passed in1.318s, with no readiness file or runtime-tool trap call. Pinned Aspire13.6.1 and Dapr CLI1.18.0/runtime1.18.1 plus OCI/native linkage checks passed. HXW009 discloses the approved dirty Builds scripts/tests; HXW010 and diagnostic release-ineligibility remain.

Before dispatch, the proposal's Dapr environment-name typo was corrected by binding runner-required `HEXALITH_DAPR_HOME` and the listed `DAPR_RUNTIME_PATH` to the same approved existing directory; runtime bytes, resource scope and time bounds did not change. This correction is retained in `source-classification.json` and the candidate environment binding. No functional helper/cohort, workload or cleanup ran. All63 original failed-run state/workspace/diagnostic files remain byte-identical. Its terminal verdict remains **retained / unverified:true / isEmpty:false**; the absence of observed g4-labelled containers does not convert it to cleaned or verified empty. A41 remains open, Story27.4 in-progress and Production unchanged.

The single new invocation remains unused. Restoring enough available Linux capacity permits fresh admission under the same approved artifacts/actions before10:00Z; after that cutoff the proposal expires and no automatic extension is authorized. No full/C1/A41/Production or functional-qualification credit follows from this admission hold.

## 2026-10-18 HXP020 owning packaging repair: offline child verified, new run unapproved

**The loader source defect is repaired and verified offline; Story27.4 remains in-progress and A41 remains open.** Failed run `f482f16095f3027867cec221b0a35220` and its consumed invocation remain historical failures. No functional-retention test, workload startup, resource census, cleanup or package publication occurred in this repair. The new [concrete invocation proposal](../27-4-hxp020-new-run-proposal-2026-10-18.md) requires separate approval.

Actual retained metadata identifies the conflict. The exact failed model `hosts/platform-model/Hexalith.Platform.Composition.dll` (SHA `c50613998a55e69318ba6c877b0400ad9d9af96f44f37f1ad6406adf5a3c5087`) references `Hexalith.Builds.Tooling, Version=4.31.2.0, Culture=neutral, PublicKeyToken=null`. The original diagnostic installation's `tools/net10.0/any/Hexalith.Builds.Tooling.dll` and model-directory Tooling share identity4.31.2.0 and SHA `8285cbea14e5609b8d7b0e57e471fd38bfd8c8475cd280f4e7b6eb08ea457a06`. Its `g4-host/Hexalith.Builds.Module.AppHost.dll` instead references Tooling1.0.0.0, and `g4-host/Hexalith.Builds.Tooling.dll` has identity1.0.0.0, SHA `ae2f71d679fabcaef74cd88b6596f3881a6a3c1fd7cea4c9a01a17596510a085`. The child deps select `Hexalith.Builds.Tooling/1.0.0-20261018062731`.

An isolated replay of the old package's **actual validation child** reproduces the same exit3/0x80131040. Its .NET host trace records the resolved TPA path under the replay's `old-package/package/tools/net10.0/any/g4-host/Hexalith.Builds.Tooling.dll`, assemblyVersion1.0.0.0. `actual-model-references.txt`, `child-apphost-references.txt`, `old-binding-excerpt.txt`, full host trace and `old-package/loader-result.json` retain exact referenced/resolved identities and absolute paths. This is a new offline reproduction; no trace is retroactively attributed to the original operational invocation.

The owning source is `/home/administrator/projects/hexalith/platform/references/Hexalith.Builds`, HEAD `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9` plus an uncommitted patch. `Tools/build-g4-tool-packages.ps1` previously built the solution without the requested version, then versioned only per-tool pack; the CLI csproj globbed prebuilt unversioned AppHost output. The minimal fix supplies identical Version/PackageVersion to restore/build/pack. A new loader-only fixture and `Tools/test-g4-packaged-platform-loader.ps1` gate execute the real packaged child and require the root/model and child Tooling hashes/identities to match before successful inventory. No Platform loader redirect, dependency upgrade or range change is needed. The distinct Memories Builds checkout/gitlink is untouched; external Platform planning edits are preserved.

Private receipt root **`/home/administrator/.local/state/hexalith/memories/access-telemetry/loader-fix-20261018`** (`R` below) contains exact command arrays, cwd, exits, durations, raw logs, metadata, patch and source bindings. All package/fixture restore sources were cleared for offline checks.

| Focused offline check | Observed result |
| --- | --- |
| Old packaged child regression | Gate refuses as expected; actual child exit3 with the original assembly mismatch. Original package copied into new scratch only. |
| Corrected official local build/pack at diagnostic4.31.2 | Exit0 in37.275s; solution build0 warnings/errors; fixture child exit0. New Module package SHA `0b550b61d457337daba56184a7b3facd23001144f1ea9bddf3092a6ce485770d`. Root and child Tooling are byte-identical SHA8285cbea / version4.31.2.0. Inventory remains `releaseEligible:false`. |
| Exact retained `c506…` model in corrected packaged child | Exit0 in2.283s with a copied/rebased plan/workspace, Development environment and synthetic offline signing key. No readiness file and zero calls to trapped Docker/Dapr/Aspire/psql/curl/wget tools. `actual-model-child-result.json` SHA `9787f8e787a607c1fc49c029051f775b5ed1430a4b34c1cd1517a2f7a83dca3c`. |
| Focused Debug test-project build and exact validation selectors | Build exit0,0 warnings/errors. Five tests passed,0 failures/errors/skips/not-run. Selectors cover ordinary validation failures and return before runtime startup. |
| Existing package qualification gate | Exit0 in164.194s;63 artifact-validator scenarios plus reused-directory/pre-package/post-package/duplicate-output refusal controls pass. One temporary EOL-fixture Git normalization warning is retained; no test failure. |
| Failed-run preservation | All63 original state/workspace/diagnostic files retain their baseline SHA-256. `preservation-before.json` and `preservation-after.json` retain the comparison; all prior report bytes remain intact below. |

Post-review checks retain the earlier receipts and add `reviewed-old/` (child3, exact mismatch, gate `result:failed`), `reviewed-fixed/` (child0, final assertions `result:passed`, SHA `77ecb51bdf1bcdde9f221ae7b8e4483778276a961868ae249ccb5ee03ab70678`), and `reviewed-timeout/` (deliberate fixture delay, gate refusal with partial stdout/stderr and truthful incomplete diagnostics/unknown exit). All stream/termination waits use the same operation deadline; the one-second timeout control observed1081ms including synchronous termination overhead. A separate default-location probe inside the owning checkout passes with inherited build/target/package configuration disabled and no `BeforeStartEvent` sentinel. Updated official-wrapper controls prove version propagation and post-pack loader refusal withholding inventory while retaining all four packages; the63 existing scenarios also pass (exit0,44.204s). The final receipt declares success only after contract and startup assertions. `reviewed-source-binding.json` / `reviewed-owning-source.patch` bind the six final source/test files. The blind review's six findings and dispositions are recorded in the bounded repair spec; no release/publication proof contract is expanded by this diagnostic fix.

The new `R/owner-handoff.json` records this source/test correction and its limits. The original R5 owner-handoff and terminal custody are unchanged. HXW010 remains the independent diagnostic4.31.2 warning outside `[4.33.0,5.0.0-0)`; it is not the HXP020 cause or release acceptance. The old resource verdict remains **retained / unverified:true / isEmpty:false**. No live cohort, backend, retention, cleanup, native-quiescence, C-VG3, full/C1/A41 or Production credit follows from these child-loader checks. Sprint, A41/deferred ledger and existing story/spec tracking are unchanged.

## 2026-10-18 approved additional invocation consumed: HXP020 before runtime startup

**Current outcome: blocked; Story 27.4 stays in-progress and A41 stays open.** The one approved additional invocation actually ran once and returned process exit **3**, `TopologyOrLifecycle / Topology / HXP020`. It is consumed; no additional invocation remains. There was no operational retry, down/cleanup call, source or dependency repair, tool-version change, or Git mutation. Earlier paragraphs describing an unused invocation are historical and are superseded by this actual outcome.

| Actual runner field | Recorded value |
| --- | --- |
| runId / ownerId | `f482f16095f3027867cec221b0a35220` |
| selected request time (actual runner UTC) | `2026-10-18T07:34:01.129711+00:00` |
| execution expiry,14400s | `2026-10-18T11:34:01.129711+00:00` |
| startup expiry,600s within execution | `2026-10-18T07:44:01.129711+00:00` |
| terminal UTC / startup duration | `2026-10-18T07:34:16.282017+00:00` / `15.1697424` seconds |
| separate stopping limit | 900 seconds; no new stopping or cleanup invocation was introduced |
| AppHost PID / start time / project PIDs | null / null / empty |
| observed candidate / backend identity / readiness / test counts | null / null / empty / null |
| qualificationCredit | false |

The fatal diagnostic is: `Could not load file or assembly 'Hexalith.Builds.Tooling, Version=4.31.2.0, Culture=neutral, PublicKeyToken=null'. The located assembly's manifest definition does not match the assembly reference. (0x80131040)`. The Builds-owned `--validate-platform` child fails before `StartAppHost`; the actual diagnostic states no resources were started. The current runtime model file is byte-identical to the selected offline fixture (SHA-256 `c50613998a55e69318ba6c877b0400ad9d9af96f44f37f1ad6406adf5a3c5087`). Thus the passing29-case fixture does not establish this actual packaged child-loader boundary. The conflicting located assembly path/identity was not emitted, so no more specific root cause is claimed. HXW010 remains a separate source-mode warning for diagnostic4.31.2 outside `[4.33.0,5.0.0-0)`; it is not relabelled as the cause or as release acceptance.

No functional helper/cohort started. No A/B anchor, fixed PostgreSQL expires_at SQL, verify-full backend identity, readiness, strong presence/absence, due-index/reminder progress, younger-control preservation or cleanup assertion passed. The actual selected request time above must not be used as a fabricated cohort timestamp. C-VG3 remains unobserved. No functional/full/C1/A41/Production credit is claimed.

The actual terminal record is **resourceDisposition=retained** and **remainingResources={containerIds:[],processIds:[],unverified:true,isEmpty:false}**. Preserve that verdict: an independent post-run Docker query for `label=hexalith.g4.run` exits0 with empty output, the runner PID1174 is absent, and no AppHost/project PID was recorded, but these observations do not convert the runner's unverified census to verified empty/cleaned. Failed state, workspace and diagnostics remain under FR4; no manual deletion, global cleanup, fresh-budget down or blanket disposal is authorized. Native gate closure and complete quiescence do not receive an observed pass from this early loader refusal.

| Final admission / verification evidence | Result |
| --- | --- |
| Source, tools and capacity before launch | R5 preflight passed11 fixed read-only commands;19 roots/114744 raw files were checked before and after,17 direct HEAD/index pairs matched, and g4 stores/owned label were initially empty.14 Docker CPUs,97021218816 memory bytes and60237803520 Linux MemAvailable bytes were observed. They are observations rather than reservations. |
| Read-only backing-storage correction | R4 had refused the inaccessible `/var/lib/docker` path in the Docker Desktop distribution; the failed logs remain unchanged. R5 only corrected the read-only mount/cwd to `/mnt/docker-desktop-disk/data/docker` with `--cd /` and added a5GiB free-space assertion. The narrow observation exited0,865547493376 bytes available; Windows backing volume free85087916032 bytes. No VM lifecycle or workload operation was performed. |
| Exact source/artifacts | Platform `d1eb16166ee1a62b791a3201cdcd689f33f4fa8f`, Builds `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`, primary `ef842c9abca9b9a0f77699ba1232e97c23405693`; actual sourceMappingHash `8dffe62ac2820defe47a441e71766e1d4572003bfea619f2a5d9a1c27524fe2e`. CLI098800… / tooling8285cb… and caller PATH/Aspire pins remain unchanged. Diagnostic unsigned/unpublished,releaseEligible:false. |
| Preparation checks | Latest selected29/29 graph,719 exact fixture bytes and four actual metadata-range seam checks passed. Earlier333 runtime/14 private timeout/91 functional/96 catalog/5 identity results retain their documented original source/binary scopes; they are not new live evidence. Independent reviews and external deferrals remain as recorded below. |
| Source after actual invocation | Independent19-root post-run snapshot matches the admitted raw files and all HEADs exactly; no source drift was observed. No broader suite was rerun. |

Actual custody root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-refresh-20261018-53vfi0yw/metadata-refresh-r2-_4u_999v/parent-admission-r5-228_lese`. `parent-admitted-binding.json` SHA-256 `2fc393815ba7e002c69451a8ac8b16f41f4659640340426c2ab11f52a14bdc92` binds exact expanded CLI argv/cwd/explicit Development/Dapr environment, original caller PATH, artifacts, receipts and helpers. `pre-invocation/single-invocation-dispatch.json` records the single exec at2026-10-18T07:34:00.952285Z. `operational-result.json` SHA-256 `7c13b1106464cd88380adb40db27db719e66a031f69eb4b928aee57a7256459e` binds process exit, actual outcome and raw artifacts; `terminal-custody/` preserves byte copies of mutable run state/diagnostics. `post-run-source-snapshot.json` and `post-run-source-observation.json` preserve the unchanged source observation. The primary actual state is `/home/administrator/.local/share/hexalith-builds/g4-runs/f482f16095f3027867cec221b0a35220.json`; workspace and diagnostics use that same runId.

Owner handoff / new deferred blocker `27.4-runtime-model-load`: Hexalith.Builds diagnostic package/AppHost loading owner, with Platform model owner, must capture the actual child-loader referenced/resolved assembly identities and verify the owning boundary before another local attempt. `owner-handoff.json` SHA-256 `fa5fdaa8ee65361b9246b8ce14a3caa917a0bcf5b3d53c71d6be3a8bff75d34e` records source hashes and the exact observed control-flow boundary. This task makes no source/test/version repair. A future operational attempt requires separate authority after concrete current artifacts and retained-resource disposition are reviewable; the consumed invocation, original bounds and cutoff are not reset. The primary deferred-work ledger, sprint tracking, telemetry docs, project context, spec frontmatter/baseline/review-loop and all historical document bytes remain unchanged. Full/A41 tasks stay unchecked.
## 2026-10-18 immediate preflight source hold and refreshed evidence custody

The immediate preflight bound to parent binding `ed6817619ee44cd679ca88ba2b010d7389f776864202fe34489559477bc15d22` refused before any probe or operational invocation at 07:30:38Z: the parallel evidence writer changed preservation/validation and added SHA256SUMS. That refusal remains in the earlier `pre-invocation/preflight-result.json`; no approved invocation was consumed. Parent classified all 12 current evidence/prose/checksum files and verified the 11 listed checksums. New custody root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-refresh-20261018-53vfi0yw/metadata-refresh-r2-_4u_999v/parent-admission-r4-p0sseqhv`; classification SHA-256 `3ece4aee332f38b0c5425b61ee912ef24f5f8f8e62f4781ac7964a82e0810e9a`. Model/tooling/manifest and all 17 dependency revisions remain unchanged; a fresh strict raw-source before/after preflight is still required, with the same single-invocation and14400/600/900 limits. No retry of an operational invocation or budget reset is authorized.

## 2026-10-18 parent metadata review triage and final admission

Metadata custody root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-refresh-20261018-53vfi0yw/metadata-refresh-r2-_4u_999v`. Three independent reviewers completed before parent triage: blind five findings (floor five), edge `[]`, verification one. Their input SHA-256 is `d7116723c976ba3eb4c2574a8b67870f59babbf5333ba27cc078f3b5b6ccf4f9`. Each finding is retained separately in `parent-review-triage.json` (SHA-256 `49f57b30a4118c5a195e7f8a74597dc67b2690c8b04fdefd58c550090329373f`).

| ID | Severity / confidence | Disposition and evidence | Owner / due |
| --- | --- | --- | --- |
| MB1 | low / confirmed | fixed: Three changed Platform tracking artifacts were omitted from the metadata review input. classified-platform-tracking.diff captures actual Spec1.9, sprint and deferred-work deltas (21650 bytes). Parent inspected these as external planning/review data; no executable paths or selected consumer change. | Story27.4 evidence owner / Before operational admission |
| MB2 | low / confirmed | fixed: Historical7f metadata paragraph could be mistaken for final source admission. Existing worker report clarification explicitly supersedes7f and failed admission with final902/final-* bindings; R3 further supersedes metadata identity only and retains prior refusals. | Story27.4 evidence owner / Before operational admission |
| MB3 | low / confirmed | fixed: Semantic mapping contentHash and JSON-file SHA256 were not clearly distinguished. Report clarification supplies separately labelled734b88... contentHash and8c0b7... fileSHA256. Current R3 carries both labels. | Story27.4 evidence owner / Before operational admission |
| MB4 | low / confirmed | fixed: Final metadata summary, artifact and source-admission indexes lacked direct hash anchors. Worker report tail includes final-* hash anchors; parent final binding will hash current indexes and receipts. | Story27.4 evidence owner / Before operational admission |
| MB5 | low / confirmed | deferred: External Platform README4.33 publication/acceptance assertion has no direct ratification/package proof citation. This authorized lane preserves diagnostic4.31.2 and cannot establish4.33 executable qualification. No4.33 binary restore or invocation, and no acceptance/release claim, is inferred from the pin, README or synthetic version seam. External README citation remains Platform owner work. | Platform release/Story2.8 owner / Before relying on4.33 package acceptance or release claims |
| MV1 | low / confirmed | deferred: Committed range and manifest boundary behavior lacks a permanent regression covering actual accepted minimum and excluded major/prerelease versions. Private focused actual-installed resolver seam checks4.33/4.32/5.0; latest-0 bound adds5.0.0-preview.1. They exercise metadata evaluation using unchanged4.31.2, not4.33 package/runtime qualification. Permanent CI coverage is outside the two-doc lane. | Platform release/Story2.8 test owner / Before treating committed range/pin as fully qualified release evidence |

The final `[4.33.0, 5.0.0-0)` declaration was additionally checked through the actual installed 4.31.2 resolver's existing optional version seam, using the actual committed record and 17 direct index entries: 4.33.0 accepted; 4.32.0, 5.0.0 and 5.0.0-preview.1 excluded by HXW010. The read-only command exited 0 in 1.971145608 seconds with HEAD/record/pin stable and empty stderr. Receipt: `metadata-refresh-r3-yt17b8h8/parent-range-receipt.json`, SHA-256 `3f8aba34ca5949b5c3f55043709876486a705643676ee78296ed8fb57dded4d9`. This checks metadata evaluation only; it does not restore or execute 4.33.0, qualify its package, or remove the actual 4.31.2 source-mode HXW010 warning. Diagnostic `releaseEligible:false` remains.

Parent independently rehashed all 719 selected fixture files, verified 29 XML passes and exact model/tooling resolution trace. Receipt: `metadata-refresh-r3-yt17b8h8/parent-fixture-verification.json`, SHA-256 `76506e402f79efa519ab7b910ccbbbdd35bdc6e02186ea2a6c77d8cc3b626c93`. Five newly appeared Platform JSON files were parsed and classified as external retained evidence, with no matching consumer in the selected source/test/build inputs; classification SHA-256 `e531e1dba14f2cc34877bedf0aa05be946feb0bc43a3e0a21b287e8c0e7366c4`. The first model-command raw drift remains recorded; the later stable model recapture, byte equality and fixture checks supply separate current evidence. No historical failure was relabelled as a pass.

The caller PATH restoration is explicit and binds the previously approved Aspire 13.6.1 executable; it does not install or change a tool. Final binding and read-only before/after source, artifact, prerequisite, capacity and collision receipts are under `parent-admitted-binding.json` and `pre-invocation/`. They precede the one still-unused approved existing-runner invocation. Actual T/runId, ownership, loaded-image and verify-full PostgreSQL identity, readiness, SQL expires_at support and all cohort timing/presence/absence gates remain unobserved until that runner supplies evidence. The 14400/600/900 bounds, initial-request cutoff, fixed SQL-before-cohort, failed-resource retention, no-retry and separate cleanup authority remain unchanged. Story status and A41 remain open.

The worker final source observation remains explicitly failed (`passedSourceObservation:false`): later external evidence edits outpaced its first five-file classification. Parent then classified the currently present 11 evidence/planning files at their exact hashes in `metadata-refresh-r3-yt17b8h8/parent-final-evidence-classification.json` (SHA-256 `dd56b17a0a8b4f14f1b3f9f4d866bf5a4b81738f3959c30ed9a4f711fd64285c`). This later classification does not rewrite the worker refusal; separate parent source admission and strict before/after preflight must pass before launch. No model rebuild is inferred from documentation data.

## R3 current metadata binding — 2026-10-18

Current actual Platform is **`d1eb16166ee1a62b791a3201cdcd689f33f4fa8f`**. Its accepted range is **`[4.33.0,5.0.0-0)`**; unchanged private diagnostic **4.31.2** remains outside it, with HXW010 and `releaseEligible:false`. Initial raw comparison against the final902 snapshot matches **114,724** files outside the six named Platform metadata/prose/tracking paths and two owned documents. Five new external evidence JSONs arrived during the first model command; that failed custody observation remains in `early-source-drift.json`. Parent separately classified their exact bytes as evidence-only with no executable consumers (`parent-external-evidence-classification.json`, SHA-256 `e531e1dba14f2cc34877bedf0aa05be946feb0bc43a3e0a21b287e8c0e7366c4`).

The stable actual installed resolver/materializer/full model build exits **0** in **14.465031 seconds**, semantic mapping **contentHash `8dffe62ac2820defe47a441e71766e1d4572003bfea619f2a5d9a1c27524fe2e`**, serialized `mapping.json` **file SHA-256 `0d2703e4433808ec68c3a1e3b22ec41c2a7a98f86a75118efbd11082ed65717b`**, model **SHA-256 `c50613998a55e69318ba6c877b0400ad9d9af96f44f37f1ad6406adf5a3c5087`**. Its bytes equal the freshly tested model: **29/29 selected cases pass** in **16.224783 seconds**, unchanged compiled test DLL/tooling, **719** unchanged fixture files and exact host-trace paths. Graph and stable recapture keep all19 HEADs/raw source maps unchanged; no broader suite or separate source/test build is used. R3 evidence is **`/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-refresh-20261018-53vfi0yw/metadata-refresh-r2-_4u_999v/metadata-refresh-r3-yt17b8h8`**: `artifact-binding.json` SHA-256 **`de50357b2acbb169f2eda0f9e3c1625760ce4d5064e09730a30e49a32b59bf92`**, `preparation-summary.json` SHA-256 **`93cbe350aadfb084ce920be2c5cdfeb0e714c3ddbbf1757f1c0250fa945e9227`**, plus final `source-snapshot.json` / `source-admission.json` and `preparation-delta.diff`. All prior bodies/frontmatter/private failures and completed R2 reviews remain intact. Parent owns the actual-metadata seam probe, immediate gates and still-unused invocation; no runtime action, version/source repair, new authority or operational-clock change occurs here.

## R2 final current binding — external tracking drift reverified, 2026-10-18

**Final offline binding is Platform `902200ee82f706d90637867617d3b0295370beeb`; the operational invocation remains unused.** After the first passing R2 checks below, the final source observation correctly refused a later external Story1.9 commit. It changed only Platform Spec1.9/tracking prose, including that repository's sprint/deferred-work documents. These are distinct from the four protected primary Memories files, which stay unchanged. All **114,724** other raw files match R1 after explicitly excluding six Platform metadata/prose/tracking paths and the two parent evidence documents; selected compiled/test/catalog/manifest inputs remain byte-identical. The earlier failed admission and both parent preflight refusals remain retained, not converted into passes.

The fresh actual installed resolver/materializer/**full model build** exits **0** in **30.252700 seconds**, mapping **`734b88a96ebc4f2277050dcbf6704eb953decbd5457ff61dc78f1e044e8de93d`**, 17 direct entries, model **`c87275be71d3f4765885f7599bd37c3dba41baf5f74e056f4fa100e612c8302d`**. Because model bytes differ, the same **29 selected offline graph/refusal cases** pass again, exit **0**, **19.328410 process seconds**, zero failures/errors/skips/not-run, using the exact unchanged R1 test DLL/tooling and a fresh **719-file** fixture. All fixture bytes remain unchanged and host trace confirms the actual assembly paths. All 19 HEADs/raw source maps remain stable during both final commands. No separate source/test build beyond the required actual model builder, broader suite, source repair or weaker expected behavior is used; full dependency closure/live readiness/SQL remain unproven.

Private diagnostic **4.31.2** and exact R1 CLI/tooling bytes remain unchanged, explicitly **outside** accepted **`[4.33.0,5.0.0)`**, with HXW010 and `releaseEligible:false`; no 4.33 tool is consumed or release acceptance claimed. One intervening private helper-path command exited **64** in **0.345510 seconds** before model entry because the intended helper had not been created after an unexpected-document assertion. Its exact receipt remains intact; a separately created helper/workspace supplies the successful final build above. No runtime or operational budget started.

Under the same R2 private root below, `final-preparation-summary.json` indexes all **five** command receipts/log hashes and retained failure; `final-artifact-binding.json`, `final-graph-load-binding.json`, `post-model-drift.json`, `first-final-admission-refusal.json`, final `final-source-snapshot.json` / `final-source-admission.json` and `final-preparation-delta.diff` identify current custody. Earlier R1/R2 evidence, all prior document/frontmatter bytes and all 18 R1 review rows remain intact. Parent reviews the complete R2 metadata/result delta and repeats immediate admission gates; it alone owns the still-unused invocation. Later external prose-only drift is returned for precise parent classification, without an indefinite worker loop. Operational limits, live/full tasks, A41 and sprint/publication/Production authority remain unchanged.

## R2 metadata refresh — selected offline checks pass, 2026-10-18

Two immediate parent preflights remain refusals with **zero operational invocations**: R1 `pre-invocation/preflight-result.json` refused caller-PATH bytes after the login shell reordered/duplicated the same entries; Aspire resolution was unchanged. Byte-identical helpers in `pre-invocation-path-bound` then used the exact already-bound PATH and passed that check, but refused new Platform `hexalith-platform.json`, `README.md` and `.config/dotnet-tools.json` bytes before any version/payload/capacity or runtime call. R1 `caller-path-binding.json` records explicit restoration of the expected PATH for checks and CLI. No operational T/runId or budget started.

External metadata/prose work advanced Platform through `efd3562a4c1c68109714fc07df0af8f48a458525` to the actual stable binding **`7f2394256c876688aaaee764b933e1cf8c1b9e94`**. The new accepted range is **`[4.33.0,5.0.0)`** and tracked tool manifest pins **4.33.0**. This preparation retains the exact approved private unsigned unpublished diagnostic **4.31.2**, explicitly **outside the accepted range**, with unchanged R1 CLI/tooling bytes and `releaseEligible:false`; no 4.33 tool is restored or invoked. The actual resolver reports HXW010 for that exact mismatch in source mode. This is not accepted-tool, release or full-qualification evidence. All **114,726** other raw files across 19 roots match R1 after excluding only four explicit Platform metadata/prose paths and the two parent evidence documents; selected compiled/test/catalog/manifest inputs are byte-identical.

The actual installed resolver/materializer/**full model build** exits **0** in **56.176187 seconds**, mapping **`95d6f5277e05582f6e4e05f9f6edabec079f760022ec64156cc3e491661577e4`**, 17 direct entries, model **`a4e757223c2bd7f6ed27943ff8c82405f43d2728da29195285a1a40b3be58f8c`**. Its bytes differ from R1, so only the existing **29 selected graph/readiness/refusal cases** are rerun using the unchanged R1 test DLL and a fresh fixture bound to the new model and exact installed tooling: **29/29 pass**, exit **0**, **29.463807 process seconds**, zero failures/errors/skips/not-run. Host trace confirms those assembly paths and all **719** fixture files remain unchanged. Both commands keep all 19 HEADs/raw source maps stable. No source/test rebuild, unsupported build properties or Identity/96/333/91/14 rerun is used. This remains selected offline fixture evidence, without full dependency-closure, live readiness or SQL credit; no full-model zero-warning claim follows.

R2 evidence: **`/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-refresh-20261018-53vfi0yw/metadata-refresh-r2-_4u_999v`**. Its `preparation-summary.json` indexes both exact argv/cwd/environment/time/exit/log receipts; `source-equivalence-at-model.json`, `artifact-binding.json`, `metadata-and-refusals.json`, `graph-load-binding.json`, final `source-snapshot.json` / `source-admission.json` and `preparation-delta.diff` preserve actual source/artifact/metadata custody and all 17 direct HEAD/index comparisons. Earlier R1 evidence, its 18 review rows and all previous document/frontmatter/protected bytes stay intact. Git commit-graph warnings are retained without maintenance. Parent reviews this metadata/result delta and repeats immediate source/artifact/tool/native/capacity/collision checks before the still-unused invocation under existing owner continuation. No runtime/service/container/SQL/cohort/cleanup action, Git/dependency/version repair or new approval occurs here; immutable operational timing, live/full task state, A41 and sprint/publication/Production remain unchanged.

## Review Triage Log — settled source refresh, 2026-10-18

All three configured context-free layers reviewed frozen **272,148-byte** input `3ee6bb2cc2f40617eef37ef1c1e49092ebce484255fa7bdd5c99600abe118e0b`: the current two-document preparation/owner-continuation delta plus externally authored Platform compiled/test, Builds catalog/workflow and EventStore source deltas since the prior preparation. Historical evidence archives and unrelated prose were excluded. Blind floor is `min(floor(sqrt(272.148)+1),10)=10`. All 10 blind, six edge, one verification-gap and one Other finding have separate dispositions below. No source/test file was edited by this continuation. The owner restriction preserves original in-progress frontmatter/baseline/review-loop and records follow-ups only in these two existing documents.

| Finding | Verdict | Route | Verified evidence / disposition |
| --- | --- | --- | --- |
| RF-BH1 | medium | defer-existing-Agents-host | Actual Agents.UI uses Microsoft.NET.Sdk.Razor and has no executable entry point; AddProject treats it as a runnable host. The synthetic Web fixture does not establish real UI launch. Platform owner must supply the real receiving web host before any positive Agents-host claim. |
| RF-BH2 | medium | defer-existing-Agents-infrastructure | AgentsHostComposition adds projects without sidecars/state/invocation/domain-registration wiring, while AgentDomainHostComposition requires Dapr and leaves several adapters deferred. An accepted optional profile is insufficient for working Agents readiness. |
| RF-BH3 | medium | defer-existing-Agents-bindings | The host emits Agents__Parties__Binding and Agents__Conversations__Binding, while the actual adapters select Parties:BaseUrl/Tenant and Agents:EventStore:BaseUrl. Those bindings do not activate the supported live clients/dispatcher. |
| RF-BH4 | medium | defer-existing-Agents-admission | TryCompose supplies null capabilities; the nonnull overload registers private services in the AppHost only. AdmitAsync has no receiving-process production caller. This does not establish a functioning cross-process replay/spool admission boundary. |
| RF-BH5 | medium | defer-existing-Agents-bootstrap | Tenants BootstrapHostedService constructs BootstrapGlobalAdmin with the configured literal UserId. Passing a reference label through Tenants__BootstrapGlobalAdminUserId does not resolve it to the intended administrator. |
| RF-BH6 | medium | defer-existing-authority-contract | QueryCallerAuthorityBinding emits only its digest; WorkloadAuthorizationDetails.Decode requires tenant/domain for every nonempty binding set, and AcquireBoundAssertionAsync invokes it before transport. Asymmetric caller-query issuance therefore refuses that producer output. |
| RF-BH7 | maybe-false | defer-unverified-authority-policy | Exact static grants do require every requested digest value to be enrolled. Whether generic fresh-query signing is an intended supported authority capability is unresolved: the issuer explicitly promises exact-binding grants, and no approved dynamic-digest authorization policy is present. EventStore authority owner must settle that policy before claiming general fresh-query support; do not weaken the grant check. |
| RF-BH8 | medium | defer-existing-recovery-integrity | For an original pending Match(ETag) envelope without a terminal anchor, recovery validates prior-value presence but not an independent digest. CompensateAsync restores PreviousBytes; its later anchor hashes the already-read bytes, so a different valid previous JSON value can be restored after a conflict. Reopen with persisted-value corruption evidence before stronger recovery-integrity claims. |
| RF-BH9 | medium | defer-existing-query-page-mapping | DaprDomainQueryInvoker preserves only InvalidEnvelope/InvalidCursor for HTTP400. A supported InvalidPage result is converted to ActorException, losing client-error classification; share the same follow-up as RF-EH5. |
| RF-BH10 | medium | defer-existing-profile-reference-validation | OptionalReference checks size/whitespace and a credential-substring blacklist, without requiring a reference scheme. Compact raw credential-shaped strings pass and are forwarded to project environment. The optional Agents profile owner must define and validate supported reference schemes before asserting secret exclusion. |
| RF-EH1 | medium | defer-existing-authority-contract | Independently traced the same digest-only producer to mandatory tenant/domain decode and bound issuance refusal. This row retains the separate edge finding; action shares RF-BH6. |
| RF-EH2 | medium | defer-existing-authority-body-deadline | ResponseHeadersRead finishes HttpClient send-time coverage at headers; ReadBoundedTokenResponseAsync streams with caller cancellation only. A stalled successful body can hold issuance and the ordinary shared gate indefinitely when the caller supplies no deadline. This is the HTTP authority branch, not local HMAC signing. |
| RF-EH3 | medium | defer-existing-authority-key-refresh | The bound branch validates against GetAuthorityMetadataAsync cached for five minutes and returns null on validation failure without a key-not-found refresh. Newly rotated signing keys can therefore deny fresh authority assertions until refresh. |
| RF-EH4 | maybe-false | defer-unverified-principal-provenance | Capture enumerates every identity role type and principal claim once the first identity is authenticated; a constructed mixed principal will include unauthenticated-identity claims. Actual untrusted claim provenance through a supported gateway authentication/transformation producer is not demonstrated. Need that producer/host trace and its trust contract before asserting privilege escalation; selected local state reads do not call Capture. |
| RF-EH5 | medium | defer-existing-query-page-mapping | Checked the separate edge trigger: HTTP400 carrying InvalidPage is replaced by ActorException in DaprDomainQueryInvoker. Same underlying mapping omission as RF-BH9; no pagination transport success is claimed. |
| RF-EH6 | medium | defer-existing-delegated-response-metadata | DelegatedOwnerQueryClient calls the two-argument FromPayload with query.QueryType as projectionType; that overload discards SubmitQueryResponse.Metadata. Owner query/projection types may differ, and freshness/paging metadata is lost. This client is absent from the retention consumer path. |
| RF-VG1 | low | defer-existing-query-denial-regression | Accepted the reviewer-preverified coverage gap: existing invoker/router/submission tests do not compose HTTP403/404 or body safe-denial through canonical QueryNotFoundException. Current router source contains the correct safe-denial branch; the gap is regression protection, not a demonstrated failed current result. Reopen before a stronger composed-denial verification claim. |
| RF-VO1 | medium | defer-existing-authority-contract | Verified the verification layer Other finding independently: opt-in/client credentials cannot overcome digest-only authorization_details being rejected before the asymmetric authority call. Separate row retained; shared action with RF-BH6 and RF-EH1. |

These pre-existing capability follow-ups remain with **Jérôme Piquot / Platform composition owner** (Agents) and **Jérôme Piquot / EventStore maintainers** (query authority, transport, delegation and recovery). The two maybe-false findings are explicitly unverified potential medium issues; the stated host/policy evidence is required to settle them. Reopen before enabling or making positive claims about the affected capability, signing profile, credential exclusion, pagination/delegation, recovery integrity or composed-denial regression coverage. No wildcard authorization, source repair or declaration change is introduced. The deferred-work ledger stays unchanged under the owner two-document restriction.

No finding establishes a defect in the selected local retention operation. Builds calls **PlatformAppModel.ComposeRun**, separately from the optional Agents ComposeEntry branch. The retention consumer posts only its fixed local workload and reads Dapr strong state/actor control and the fixed PostgreSQL query; it does not use handler/paged/delegated caller-query routes, authority HTTP issuance or pending-batch recovery. ComposeRun supplies the local Development signing contract; JwtWorkloadAssertionIssuer chooses its signing-key branch before asymmetric resource-binding handling. The 29-case graph evidence and five Identity cases retain their stated offline scope; neither claims working Agents deployment, production query authority or full dependency closure. These traceable exclusions support the approved bounded local experiment, not dismissal or repair of the deferred capabilities. No new broader test run is justified by this triage.

Parent independently matches the worker settled source across **19 roots / 114,731 raw files**, all **17** direct HEAD/index pairs, all **17** command receipt/log/helper hashes, **719** graph fixture files, **282** Identity files, **29/29 + 5/5** XML outcomes and exact installed tooling/model resolution in the hash-verified host trace. All **346** older artifacts rehash unchanged. Current source, artifacts, tools, capacity/collisions and actual runner-owned gates must still pass immediately before invocation. The additional invocation remains unused at this review checkpoint; no operational T/runId, readiness, SQL, cohort or cleanup is invented.

## Current preparation outcome — settled source verified, 2026-10-18

**The affected offline refresh passes; the additional operational invocation remains unused.** The existing owner continuation governs the unchanged operation after configured reviews and immediate admission checks. Current Platform is `2ac8e73c09fe631c5620723b54784d2e28e8941f`, direct Builds `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`, direct Memories `ef842c9abca9b9a0f77699ba1232e97c23405693` and EventStore `9ed9b9742bd006e568f026cc1c588275cf9db097`. All 17 direct HEAD/index pairs match at the settled observation; no agent Git mutation manufactured agreement. Preserve the earlier refusal: the actual installed resolver first exited 1 in **2.121742 seconds** because Conversations HEAD `476369f83d8fbdcecaa3b2b9ef33bc310d4131a6` differed from staged gitlink `46e42f99bedc030ff654d4d67d7086aa4f47523b`. External pointer work subsequently settled. The failed command, its then-null artifact binding and exact source observations remain separate evidence.

The existing official workflow builds/packs local unsigned unpublished diagnostic **4.31.2** in **58.647158 seconds**, zero warnings/errors. Actual publisher payload verification passes **303 Module / 34 Evidence** entries, version `4.31.2+544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`, release-ineligible. CLI SHA-256 is **`09880041c83d4077b4d613161d18b4ef304fd2850098a7097813fc3cc95a7c52`**; tooling is **`8285cbea14e5609b8d7b0e57e471fd38bfd8c8475cd280f4e7b6eb08ea457a06`**. The changed owning catalog workflow passes **96 scenarios** in **306.608121 seconds**. Concurrent Platform/Conversations changes during earlier catalog/payload receipts are disclosed there; unchanged owning Builds inputs and archive/install bytes establish those checks only within their owning scope.

After source agreement, the **actual installed** resolver/materializer/full model builder succeeds in **19.060438 seconds**, mapping **`f9ece585f44d9c9f12770a0b5bb84268254936e9adb36cdedacc31b7c0c4bacb`**, 17 direct entries, model **`4f03f637b1a8846c29e422011fa6d4c150f5bfe4c205bf9a986f67081d3fdbe4`**. HXW010 remains the unavailable Platform tool-range warning; no full-model zero-warning, release or complete dependency-closure claim follows. Source mode remains source mode.

Fresh offline verification passes **29/29 exact-installed-model graph/readiness/refusal cases** in **16.966501 seconds** and **5/5 current Identity admission cases** in **0.415439 seconds**, both exit 0 with zero failures/errors/skips/not-run. The graph selector retains the 26 retention/backend/image/native/source/TLS/Development cases and adds only the three affected Agents entry refusals for absent profile, empty credential slots and non-Development. Host trace resolves the exact installed tooling and model, and **719** fixture files remain unchanged. Identity output custody covers **282** unchanged files. These are private binary fixtures and offline process/listener checks; selected assembly binding does not establish the full runtime dependency closure or live readiness/SQL/identity behavior.

The final ordinary supported source Debug test build passes with zero warnings/errors in **22.556894 seconds**; the settled Identity build passes likewise in **1.780676 seconds**. Retain two intervening outcomes: a no-restore source test build exits 0 with **MSB3277 Redis 2.x/3.x** warning, and applying model-only installed-tooling properties to the test project exits **1**, **33 errors / 3 warnings**, because its tooling reference is not transitive to the tests. No source/test repair or weaker expectation follows. The ordinary supported build plus established binary-fixture lane supplies the reported graph result. The earlier Identity build saw a metadata-only Platform HEAD advance with unchanged raw source; its fresh settled rebuild changed four EventStore Server/Gateway output DLL/PDB files, so the five admission cases were rerun on the new bound output. All 19 HEADs/raw source maps stay unchanged throughout the final model, supported graph build/run and settled Identity build/run.

The unchanged selected source comparison covers **160 Builds runtime/workspace/test files** and **82 Memories helper/functional/module files**. Prior 333/26/14/91 results retain only their original exact inputs/binaries; they are not new proof for this catalog/model. The new installed single bare-name Aspire probe and native artifact verifier pass in **2.797745 / 3.698139 seconds**; Aspire resolves through captured caller PATH to the unchanged 13.6.1 executable, and the native verifier retains approved OCI/native linkage plus wrong-native refusal. No aggregate prerequisite is invoked. Preparation commands retain their **1,200-second outer bound**, and model construction its **600-second bound**, independently of operational timing. Git's `unable to find all commit-graph files` warnings are retained; no maintenance is performed.

Private evidence root: **`/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-refresh-20261018-53vfi0yw`**. `preparation-summary.json` indexes all **17** expanded command receipts, true exits, measured times, source/helper/stream hashes and explicit/selected inherited environment. `settled-artifact-binding.json`, `settled-graph-load-binding.json`, `settled-identity-binding.json`, `identity-rebind-history.json`, `final-source-admission.json` and the final `settled-source-snapshot.json` retain successful binding, earlier failures and final source custody. `current-artifact-binding.json` remains the first refusal's null binding. Complete environment replay is not claimed. `preparation-delta.diff` is the preparation-only two-document insertion against the worker baselines; prior bodies/frontmatter/status/baseline/review-loop and protected bytes remain intact.

**Concrete remaining action:** parent completes the configured three reviews and independently repeats immediate source/artifact/tool/native/capacity/collision gates before the still-unused single existing-runner invocation under the carried owner continuation. Host snapshots remain observations, not reservations or admission. No operational invocation, AppHost/workload/service/container start, SQL, cohort, teardown, cleanup or source repair occurred here. No operational T/runId or deadline is invented. The **2026-10-24T18:00:00Z** initial-request cutoff and immutable **14,400 execution / 600 startup within execution / separate 900 stopping seconds** remain unchanged. Live/full tasks stay unchecked; Story27.4 remains in progress, A41 open, sprint/publication/Production unchanged.

## Owner-directed fresh source admission — 2026-10-18

Owner continuation authorizes current-source binding/verification and the still-unused bounded invocation after all unchanged gates pass. [The current Spec14 handoff](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#current-owner-directed-source-refresh-and-unused-invocation--2026-10-18) records the exact scope, private continuation root and preserved limits. Current preparation/results and final operational outcome remain unobserved; historical source holds/results below remain intact.

## Owner-approved additional invocation — held on source drift, 2026-10-17

The owner replied **“approve”** to the exact reviewed additional-invocation proposal `44a8037c33220c6455bce2c8fde9b94851803d1499cc0c273cc7dde3aa06e695`. That actual direction is retained in `owner-direction.json` under the private evidence root below. It authorizes the single existing-runner invocation subject to the unchanged immediate admission checks; the prior HXR015 invocation remains consumed history. No repeat owner-policy or date-only decision is requested.

**Admission is `HELD_SOURCE_DRIFT`; the newly approved runner invocation was not used.** At **2026-10-17T19:11:23.300831+00:00**, the read-only preflight `python3` invocation of this continuation's `preflight.py` exits **1** at the source-byte assertion, after read-only Git/hash source inspection and before any version/payload/mapping/capacity probe, Docker observation, aggregate prerequisite or operational CLI call. Exact cwd/argv/result and helper hash are retained in `refusal-command.json`, `preflight-result.json` and `source-drift-result.json`. All 19 HEADs still equal the reviewed revisions, but the working-tree bytes no longer describe the reviewed source. A matching HEAD alone does not clear raw-source drift.

Platform now has unreviewed changes in `src/Hexalith.Platform.Composition/PlatformAppModel.cs`, its composition project and new Agents composition/profile types; the project graph adds a Custody reference. Custody/test project files, Agents host verification files and `global.json` also changed. The observed global change selects Microsoft.Testing.Platform; it does not change the SDK pin, and the local installed SDK readback remains **10.0.401**. Platform's direct Builds working tree also changed `README.md` and `Tools/test-platform-version-catalog.ps1`; those changes are documentation/test-workflow inputs, not a demonstrated new Builds runtime binary. The complete 19-root raw snapshot and per-path old/current hashes are retained; concurrent changes are preserved. The changed Platform compiled source/project graph is sufficient to refuse the exact approved source binding even though the declared retention candidate and the principal HEADs have not advanced.

Private evidence root: **`/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-readmission-20261017-0kz3rwbz/approved-additional-invocation-20261017-f9ln1tzh`**. It retains the actual owner direction, unchanged proposal hash, exact failed preflight and original refusal, complete `current-source-snapshot.json`, per-root working-tree diffs and private copies of newly introduced compiled/configuration sources. The readback observes both default `g4-runs` and `g4-workspaces` stores empty. No operational CLI invocation, AppHost/workload/container/service start, native experiment, SQL, cohort or cleanup/down operation occurred in this continuation. No accepted operational T, runId, execution deadline or stopping deadline exists; none is invented. The additional invocation remains **unused and held**, without budget credit, transfer/reset, automatic retry or candidate substitution. This is a source-admission refusal before the runner, distinct from the consumed earlier runner/HXR015 refusal.

The completed 333/333 Builds, 26/26 Platform and 14/14 private preparation checks remain evidence for their original exact snapshots. They supply no verification of this changed Platform candidate. No source repair, rollback, source copy, checkout, dependency update, new candidate build/rebinding or target contact was performed to bypass the hold. Missing current source/model/loaded-artifact and affected review evidence prevents advancing to capacity/startup/readiness/SQL/cohorts.

**Reopen condition:** Jérôme Piquot / the Platform composition owner settles the current compiled inputs, then the current source/model/artifact binding, affected offline checks and configured reviews are refreshed before any new candidate is presented as executable. Do not silently transfer this exact-proposal approval to changed bytes or restore another workstream's changes. No replacement operational approval is requested while this source prerequisite is unresolved. The carried initial-request cutoff **2026-10-24T18:00:00Z** and immutable **14,400/600/900-second execution/startup/separate-stopping** limits remain unchanged and unstarted. Original frontmatter/status/baseline and all earlier body bytes remain intact; all live/full tasks stay unchecked. Story27.4 is in progress, A41 open, and sprint/publication/Production authority unchanged.

## Current source refresh — offline checks pass; additional invocation inactive, 2026-10-17

Fresh preparation binds Platform `b49d5c1cbfc79e94d92d6228e7d2b3c4f65b56f9` and direct Builds `c8c72dcab8e1b05520ff9445d9df105f084f9813`; the directly declared Memories `d11adf8970ca9810b91cd04e19741bfcf3b93167` and EventStore `1353b83939de812671260f1d7a5910ee8fb60119` remain unchanged. Primary `ef842c9abca9b9a0f77699ba1232e97c23405693` is separate custody, outside the mapping. Four external Builds startup/teardown runtime/test files required fresh artifacts and affected checks; no source repair was needed. Existing concurrent changes remain preserved.

All nine preparation commands exit 0. Fresh official Release build/pack and separate source Debug test compilation report zero warnings/errors. Local unsigned unpublished diagnostic 4.31.2 packages verify **303 Module / 34 Evidence** installed entries, `releaseEligible:false`, actual version `4.31.2+c8c72dcab8e1b05520ff9445d9df105f084f9813`. CLI SHA-256 `d1551e262ebb809afd85d005c391b8bfdc733dc3a1f605a8f024dfabfe734ea3`, tooling `448410258fc0df9582631eb0c233e687a03c1670a757efc6483d5f03d7d7d8ad`. Actual installed mapping/model pass: **`aafb208c3b609ed7d2ae1729764aa7995e889f58c3a234acde47879054cf9544`**, 17 direct entries, model **`1b61dab6351a562039b254373d7a5c870626413a78e1f9ec1686d81c0ed4b367`**. All direct HEAD/index pairs match and all 19 roots are stable through the commands. HXW010 remains disclosed; successful full model construction does not establish zero warnings or package/release closure.

Fresh **333/333 Builds** cases and **26/26 installed graph/readiness** cases pass with zero failures/errors/skips/not-run. The new incomplete-teardown regression is present. Source Debug test-output binding covers 395 unchanged files; the rooted installed graph copy covers 713 unchanged files with host-trace resolution of exact tooling/model. Foreign source, wrong/missing digest, Production/non-Development and untrusted/non-TLS refusals remain covered. These are offline source tests and local fixture listeners/processes, with no live SQL/readiness/retention credit. The unchanged candidate's **91/91** actual direct functional results and narrow unchanged 57-case catalog/model evidence retain their original scope; prior 332 runtime cases are superseded for current-source proof. Fourteen prior logs and all 346 selected old artifacts rehash unchanged.

Exact new-installed single Aspire version probing accepts 13.6.1 through the captured caller PATH with null diagnostic, unchanged executable hash, 20-second method / 25-second cancellation bounds; no aggregate probe runs. Actual new-installed native/OCI verification accepts approved Dapr1.18.1 linkage and refuses wrong native bytes. CLI1.18.0/native1.18.1 and archive/native hashes remain unchanged. Every command has a 1,200-second outer preparation bound; the full model has its separate 600-second bound. No operational deadline starts here.

Private root **`/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-readmission-20261017-0kz3rwbz`** retains expanded argv/cwd, explicit overrides, selected inherited metadata, UTC bounds, source/helper/log hashes and exits in `receipts/<name>.json`, raw `logs/<name>.log`, XML and `verification-summary.json`. No complete inherited-environment capture is claimed. Measured process intervals below exclude surrounding source-snapshot overhead.

| Exact command receipt name | Measured result |
| --- | --- |
| `package-payload-provenance` | 2.968318 s; exit 0. |
| `installed-graph` | 17.845882 s; exit 0. |
| `installed-aspire-probe` | 2.387329 s; exit 0. |
| `current-installed-model` | 16.928968 s; exit 0. |
| `private-restore` | 2.090698 s; exit 0. |
| `builds-debug-build` | 22.285559 s; exit 0. |
| `official-diagnostic-pack` | 39.321056 s; exit 0. |
| `builds-startup-selection` | 112.910730 s; exit 0. |
| `native-artifact-verifier` | 3.576891 s; exit 0. |

The [current Spec14 source-refresh record](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#current-source-refresh--offline-checks-pass-additional-invocation-inactive-2026-10-17) supplies the exact proposed CLI argv/environment and operational conditions. The data-only fresh proposal **`refreshed-additional-invocation.json`**, SHA-256 **`44a8037c33220c6455bce2c8fde9b94851803d1499cc0c273cc7dde3aa06e695`**, is **inactive pending explicit owner direction for one additional invocation** after the consumed first refusal, and completed parent reviews/final verification are recorded below. Prior proposal/refusal/budget/history bytes stay unchanged. No repeat policy or date-only approval is requested. The window through **2026-10-24T18:00:00Z** and **14,400/600/900-second execution/startup/separate-stopping** bounds remain unchanged; T/runId remain unassigned.

Immediately before any separately directed invocation, recheck the current source/artifact/tool/PATH binding and collect sanitized complete-graph capacity/collision observations; previous host snapshots are historical observations, never reservations/current admission. Existing runner readiness/loaded identity and fixed read-only `expires_at` SQL admission must succeed before the unchanged real 250-record/two-cohort experiment. Preserve all eight image pins, seven-container closure, resource ceilings, real clocks, 3,600-second retention, younger-cohort preservation, submission/GET bounds, first failure and outcome-aware cleanup. No automatic retry or fresh-budget down is included. No workload/AppHost/service/container, SQL or cohort was started by this preparation. Actual SQL/C-VG3, live identities, retention/reminder/index proof, quiescence/cleanup, full qualification, terminal/A41/publication and Production remain unreached; sprint and all six live tasks remain open.

Only this existing report and Spec14 receive appended current records, with original frontmatter/status/baseline and all historical body bytes intact. Raw private preservation/binding evidence is retained alongside the nine receipts. One erroneous worker render before the parent handoff is retained in `extra-render-invocation.json`; no separate workflow or authority followed. Parent owns configured reviews and independent final verification.

### Completed preparation reviews and independent verification

All three configured layers reviewed the 31,021-byte preparation/external-dependency delta. The [current Spec14 triage](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#review-triage-log--current-source-refresh) records all nine findings individually. Three pre-existing generic diagnostic follow-ups remain with Jérôme Piquot / Builds lifecycle maintainer: summary-only expired-task labels, complete producer snapshots during post-task health refresh, and exact retry-test/comment coverage. Reopen on the next startup-diagnostic change or before stronger evidence claims. Current readiness acceptance, retention/ownership checks and bounded outcome policy are unchanged; no complete failed-startup snapshot or same-run retry is claimed. No source repair or ledger change was authorized or performed.

Two missing verification cases are covered by **14/14 private actual-engine timeout/persistence probes**, seven per exact installed Release/current-source Debug assembly, exit 0 in **2.941871 / 2.933399 seconds**. Cases cover mixed snapshot preservation, unsafe/oversized name/detail normalization, inclusive 128/120 boundaries and normalized healthy entries. Real summary.json and failed StateStore arrays match returned diagnostics. Evidence under `timeout-review-probes-8av07lxz/` includes both exact receipts, immutable script hash `1d8463f015d518173f58498cb60d5b920d3ddcb6424f8990304d79d7e3395478` and `final-readback.json`. Bounds are 120 seconds per process/five seconds per reflected call. These enter an already-elapsed private branch with RetainOnFailure; they do not exercise public StartAsync, the live progress producer, AppHost/dependency starts or real cleanup. The 333/26 source-test inventories are unchanged; private checks are counted separately.

Parent independently rechecks nine receipts/log/helper hashes, 333/333 and 26/26 XML, selected CLI/tooling/model/PATH/Aspire identities, all 346 earlier artifacts and 113,572 raw files across 19 roots. Concurrent unrelated Platform planning-document changes triggered an initial conservative audit hold; their exact bytes are preserved and hashed, and an existing installed resolver-only recheck then returns the same 17-entry mapping in **2.521336 seconds**. No runtime/model/test source changed and no unchanged broad lane was rerun. `parent-independent-verification.json`, `parent-mapping-recheck.json` and `parent-review-triage.json` retain this scope. Final post-review readback passes: original history/frontmatter/CRLF and protected sprint/A41 bytes are preserved, both authorized document paths are the entire repository diff, and git diff --check plus the required story-slice-scope check exit 0. The unchanged proposal `44a8037c33220c6455bce2c8fde9b94851803d1499cc0c273cc7dde3aa06e695` remains data-only/inactive; this separate review record supersedes its earlier pending review state without mutating the proposal. Explicit direction for one additional invocation and immediately fresh source/artifact/tool/capacity/collision admission remain required. No operational attempt, live gate, task/Story27.4 completion, A41/publication or Production action occurred.

## Current preparation — Aspire 13.6.1 verified; additional invocation inactive, 2026-10-17

Fresh preparation supersedes the old current-version mismatch only: Aspire now reports `13.6.1+3751e615ad660621f2a7aa4c390ef48e08a0eed8`, SHA-256 `82d2aacd2432040cb3f9391e39e3ea7ed3d6e6c0143c4e0dfe38cfbb04b02bc0`. This workflow did not install/update it. The existing installed `ProbeAspireAsync` returns null diagnostic, exit 0 in 1.830382 seconds against the unchanged 13.6.1 pin; method bound 20 seconds, stderr empty. No aggregate probe or operational retry occurred. Preserve the earlier exact one-invocation HXR015 refusal and its clocks; no accepted T, budget credit or run identity is invented.

The actual installed resolver/materializer/full model builder passes in 25.344763 seconds at Platform `263cef0139214058a70de863ab01f8668759b473`, Builds `b7a9c9ec9f57e098b408d3b940929ef4b160985b`, direct Memories `d11adf8970ca9810b91cd04e19741bfcf3b93167` and EventStore `1353b83939de812671260f1d7a5910ee8fb60119`. Mapping is `7612735cb2f0f63790e3ad86ff823728e33da273863c4ce72484d25cc5fcd748`; new model SHA-256 is `7e1ddab9f8fc3a254d1d129641ea653600fc12c3f180afc7430741b81e458bb2`. All 17 direct HEAD/index pairs match. Nineteen-root before/after snapshots remain stable across both commands. Primary Memories `8c30c1719089eeb224ea0231a59f0d70f8c6166c` is separate custody; concurrent primary NuGet/AppHost and unrelated Platform eng/evidence/document changes are preserved. HXW010 remains disclosed; the unsigned unpublished diagnostic artifacts remain release-ineligible.

Fresh **26/26** exact-model graph/readiness cases pass, exit 0 in 21.350148 seconds, zero failures/errors/skips/not-run. The trace binds installed tooling `9ce5bfd8b039214238c2b511e3ec72af237246a0cf09b052eb7f510baff373d7` and the new model in the 713-file rooted output copy; before/after bytes match. Fresh negative cases cover foreign source, missing/wrong backend digest, non-Development refusal, and untrusted/non-TLS PostgreSQL fixture observations. Exact test names are in the [Spec 14 preparation record](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#current-preparation--aspire-1361-verified-additional-invocation-inactive-2026-10-17). Prior 332/332 Builds and 91/91 functional results are reused for unchanged selected inputs; 14 retained command logs and all 346 prior artifacts rehash without drift. Those suites were not rerun. The new model is retained separately; old evidence is unchanged.

Private continuation root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/aspire-13-6-1-readmission-taltu72d`. The following record executed child commands and explicit overrides from cwd `/home/administrator/projects/hexalith/platform` (`R` below expands to that exact private root; it is only a shell abbreviation):

```bash
R='/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/aspire-13-6-1-readmission-taltu72d'
env DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development /home/administrator/.dotnet/tools/pwsh -NoProfile -File "$R/probe-installed-aspire.ps1"
env GIT_OPTIONAL_LOCKS=0 DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_GENERATE_ASPNET_CERTIFICATE=false /home/administrator/.dotnet/tools/pwsh -NoProfile -File "$R/current-installed-model.ps1"
env GIT_OPTIONAL_LOCKS=0 DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_GENERATE_ASPNET_CERTIFICATE=false COREHOST_TRACE=1 COREHOST_TRACEFILE="$R/current-graph-host-trace.log" dotnet /home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Composition.Tests/bin/Debug/spec14-aspire131-readmission-taltu72d/Hexalith.Platform.Composition.Tests.dll -method '*Retention*' -method '*PostgresqlReadiness*' -method '*Redis*' -method '*SelectedDaprHome*' -method '*ControlPlaneWithout*' -method '*FileEntryRefusesNonDevelopmentBeforeComposing' -parallelMode none -noLogo -failSkips -result-xml "$R/current-installed-graph.xml"
```

The initial absolute-path probe has recorded 20-second method / 25-second cancellation bounds; no outer deadline was recorded, and none is claimed. Model execution used a 600-second model / 660-second outer bound; the graph lane used a 120-second outer bound. These preparation bounds are distinct from the proposed operational clocks. Only explicit overrides and selected inherited metadata are recorded; no complete inherited-environment replay claim is made.

Receipts `installed-aspire-probe.json`, `current-installed-model.json`, `current-installed-graph.json` preserve actual argv, times, environment scope, exits and stream/source hashes. Model log SHA-256 is `bac58005494e29209eca1fcb1d4351305da376e2b949937dd8350c5fdfdf166e`; graph log is `7a7f641ebdd8caa7b282b6b2fda2b2d82abc15f7816eb53db33e0357467d409e`. `graph-load-binding.json`, `prior-green-log-readback.json` and `parent-artifact-recheck.json` retain independent binding/reuse checks. These are offline/local-fixture results; actual SQL/C-VG3, loaded live identities, real cohorts, quiescence and successful owned cleanup remain unreached.

The runner defaults to bare `aspire` and inherits the caller PATH through its existing process allowlist. A fresh exact-installed bare-name probe with the proposed caller PATH resolves `/home/administrator/.aspire/bin/aspire`, matches the hash above and passes in 2.422443 seconds (exit 0, null diagnostic; 20-second method / 25-second cancellation / 35-second outer bounds). Its receipt is `installed-aspire-caller-path-probe.json`; the exact caller PATH is retained in `caller-path.txt`, SHA-256 `3bb24612de16ca28bb549c3edac33a9de25869a20003ff5af5625d67e33b878f`. Require the same resolution/bytes immediately before any authorized invocation; drift refuses. An earlier private PowerShell bare-name check selected the legacy 13.5.3 global tool because the helper changed PATH. Its HXR015 refusal and outer assertion failure are retained in `first-path-probe-refusal.json`; no exact unrecorded child timing is invented. The corrected verification helper restores the captured caller PATH before calling the unchanged installed method. It changes no installed tool, runner or operational environment.

The reviewed data-only proposal is `reviewed-second-invocation.json`, SHA-256 **`400bcb872b28ed6c0c7f4cb5c4f1122751b0349af7def155d78f1f4fa1661be5`**. It supersedes the unchanged original proposal only as the current owner-review artifact. Its PATH and sanitized-observation bindings grant no authority.

Host observation retains Docker 14 CPUs / 97,021,218,816 memory bytes, MemAvailable 46,496,704 KiB and 14 unrelated running containers. The default run/workspace stores were empty. Capacity/listener observations are transient and must be repeated for the complete graph before an authorized invocation; ceilings are not reservations. No existing container was changed by this workflow. Only `sanitized-host-readback.json` is referenced for review/approval; original broad output remains byte-identical under an additional 0700 restricted directory, file mode 0600. `host-observation-sanitization.json` retains its private custody/hash mapping.

[The concrete additional-invocation proposal](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#concrete-additional-invocation-proposal--inactive-pending-owner-direction) binds the existing CLI argv/environment, fresh mapping/model and unchanged complete technical contract. It is inactive pending explicit owner direction for **one additional invocation after the consumed refusal**. The carried window ends 2026-10-24T18:00:00Z; execution/startup/separate-stopping bounds remain 14,400/600/900 seconds. No repeat owner-policy or date-only decision is requested. No automatic retry/down, operational start, workload, cohort or SQL occurred in this preparation. Original Spec 14 frontmatter/status/baseline and all prior report/spec body bytes remain intact. Full/live tasks, A41, sprint and Production remain open/unchanged.

### Review outcome — Aspire 13.6.1 preparation

All three configured layers completed. Edge-case and verification-gap layers found no issues; all four blind findings were verified and corrected individually in the [current Spec14 triage](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#review-triage-log--aspire-1361-preparation). No finding is deferred. The added PATH-specific installed probe passes; its initial helper-PATH refusal is preserved separately. No source/test/version change or operational invocation resulted. Parent verification checks the current proposal and stream hashes, prior-body/frontmatter preservation, CRLF, exact two-document scope and unchanged protected A41/sprint bytes.


Parent final audit at 2026-10-17T16:55:08Z confirms unchanged HEADs across all 19 roots, all 346 prior artifact hashes, the reviewed proposal/receipt hashes and the bound caller PATH. A concurrent unrelated Platform Story1.8 document change is preserved and separately hashed in `concurrent-platform-document-change.json`; selected runtime/model/test inputs remain unchanged. `parent-final-verification.json` records the exact two-document scope, historical/frontmatter/CRLF preservation and unchanged protected A41/sprint bytes. No operational invocation occurred.

## Current admission outcome — Aspire CLI prerequisite refused, 2026-10-17

**Admission is blocked: `HXR015`, `HELD_ASPIRE_CLI_VERSION_MISMATCH`.** The existing approved CLI command was invoked **exactly once**, then exited **2 / PrerequisiteUnavailable**, status `unavailable`, before any run identity/workspace or operational workload was created. Its exact diagnostic is:

> Aspire CLI observed '13.6.0+56f3e9c0d216c0c7069dabb49dd0464e4827744f' (exit 0); expected Aspire.AppHost.Sdk/13.6.1.

The source rebind, diagnostic payload verification, affected offline tests and configured reviews below remain measured evidence for their exact inputs. They did **not** establish the host Aspire CLI gate: the standalone preflight omitted this version check, and the existing runner correctly caught the mismatch before composition. This latest result supersedes any earlier wording that all artifact/tool admission gates had passed. Do not change the catalog pin or skip the version gate to admit13.6.0.

The exact existing command and cwd are retained in `final-source-rebind-20261017-h25ndaog/operational-invocation.json` and displayed in the preparation section immediately below. Its stdout is retained verbatim as `operational-stdout.json`, SHA-256 **`c23be507bcf0f1262b5af799390ef9ea22006bd5519dd54bbaacede9223dd248`**; stderr is empty. Tool session1946 reports exit2. Independent read-only confirmation from `/home/administrator/projects/hexalith/platform`:

```bash
/home/administrator/.aspire/bin/aspire --version
```

This returns exit0 and `13.6.0+56f3e9c0d216c0c7069dabb49dd0464e4827744f`. Executable SHA-256 is `2163af1b48f37d152a4d932c9c272524c0f3d3545a0b4ba4b5d354f9650ab11e`; actual installed catalog readback requires Aspire AppHost SDK/CLI13.6.1. The refusal is enforced by existing `CompositionPrerequisiteProbe.ProbeAspireAsync`, not a newly introduced runner or approval mechanism. No dependency/catalog/tool installation or update was performed.

The exclusive pre-invocation audit is timestamped **2026-10-17T15:53:40.522913+00:00**; refusal stdout modification is observed at **2026-10-17T15:53:48.945671+00:00**. The CLI constructs provisional execution/startup budget objects at entry, but its refusal exposes no exact internal `startedUtc`, accepted operational T, runId, execution deadline or stopping deadline. None is invented or backdated; no zero-duration, time credit, renewed budget or accepted runtime experiment is claimed. The owner window through **2026-10-24T18:00:00Z** and immutable **14,400-second execution /600-second startup within it /separate900-second stopping** limits remain unchanged. No additional date-only approval is requested, and **no second invocation or fresh-budget down is started automatically**.

Both default stores remain empty after refusal: `/home/administrator/.local/share/hexalith-builds/g4-runs/` and `/home/administrator/.local/share/hexalith-builds/g4-workspaces/`. No AppHost, workload, service, container, SQL session or cohort was started by this attempt; **0 real cohort records** were emitted. Actual loaded service/backend identity, live PostgreSQL readiness, fixed `expires_at` SQL/C-VG3, real-clock retention, reminder progress, quiescence and successful owned cleanup remain **not reached**. No run-owned resources exist to stop; no cleanup/down command or stopping clock was introduced. Existing unrelated Docker resources remain outside this attempt.

Post-refusal custody records all19 roots: every HEAD and functional/build input remains identical to invocation, and all346 selected diagnostic/model/archive artifact hashes remain unchanged. Only the external, unrelated Platform ext-secrets document changed again; its bytes are preserved. Primary Memories, Platform, Builds, direct Memories and EventStore remain at the revisions bound below. The final affected lanes pass332/332 Builds,26/26 Platform and reused unchanged91/91 functional cases; the15 individual review rows and four explicitly deferred generic follow-ups remain as recorded below. No additional broad test rerun is warranted by this host prerequisite refusal or these documentation-only additions.

**Concrete next action:** reconcile `/home/administrator/.aspire/bin/aspire` with the catalog-required **13.6.1** through the approved prerequisite/tool-preparation workflow, then repeat the exact host-tool, source/artifact, capacity and readiness/SQL admission checks before any separately directed attempt. This task does not perform that environment repair, substitute a tool/version, or infer another run from the current approval. The carried date window alone is not the blocker.

Private causal evidence is under `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/final-source-rebind-20261017-h25ndaog`: `operational-invocation.json`, `operational-session.json`, `operational-stdout.json`, `operational-stderr.log`, `operational-result.json`, `operational-clock-clarification.json`, `post-refusal-custody-and-host-readback.json`, and the preceding `final-offline-admission-audit.json`. These preserve exact argv/cwd, explicit environment, clock scope, code/result, command-output hashes, source/payload snapshots and post-refusal container observation. All earlier failure/interruption evidence and artifacts remain retained. Only this existing Spec14 and consolidated report are updated; original frontmatter/status/baseline and all historical bytes remain intact. **Live/full tasks stay unchecked. Story27.4 remains in-progress; sprint, A41 and Production are not transitioned.**

## Current source rebind and reviewed admission preparation — 2026-10-17

**Fresh preparation passes for Platform `1753153d772e5e8c5d0a6e53d33b0b8214d4f465` and direct Builds `b7a9c9ec9f57e098b408d3b940929ef4b160985b`.** This measured binding supersedes the earlier source-drift holds only for these actual inputs. The owner-authorized single initial request remains available through **2026-10-24T18:00:00Z**, with **14,400 seconds execution**, **600 seconds startup inside execution**, and **900 seconds separate stopping**. Actual T has not yet been assigned. The existing runner must still pass its live startup, readiness, identity and fixed read-only SQL gates before admitting any cohort. No date-only approval is requested.

Primary Memories custody is `92c25d22e329ccd37bd776b73725641750a697e9`; direct Memories is `d11adf8970ca9810b91cd04e19741bfcf3b93167`, and direct EventStore is `1353b83939de812671260f1d7a5910ee8fb60119`. **The actual mapping binds Platform and its 17 direct source entries; primary Memories is separate document/source custody, not a mapping entry.** The actual installed resolver/materializer returns mapping **`1c1f14a782e693af6f7890431c556ed04cd9dab5ffa51f3f2e9e84c8cb088944`**. Full model build passes in **17.150143 seconds**, model SHA-256 **`e2490233a3095c873fa0a00314bc32a4b3aba0f3ff6af6d02020da9854246459`**. All 19 repository roots are captured before/after each final command. External commits and the unrelated changing Platform ext-secrets document remain preserved. All 17 direct checkout HEADs/index gitlinks must still match at final admission; no initialization/update or Git mutation was performed.

A late external commit changed startup-task output draining and added an allocation test. Earlier c12 tooling/model results were therefore insufficient for final admission. The unchanged official diagnostic 4.31.2 workflow was repeated in a new private directory, retaining all older artifacts. Final Module archive SHA-256 is `d8846a246ee1c1d06665504ec5d7409bc1a71086a6da22f0be6d3633df42d3ba`, symbols `3e5f022564e7450464e3d16d2e9fcf8e0e9b0e6db5dfc8d0e90662c89db65bef`; Evidence archive `38226cfbe882588a21f5b293891270dc6f7181b89a6e75f56f4e17dd24b6ecb9`, symbols `1e085480658622acfe0323dc0247f29f31eb30cbea058fb160fb8542fa3aff14`. The publisher's actual payload function verifies **303 Module / 34 Evidence entries**. Installed CLI is **`a304b530e179a77a6e514aaff966ee9bd98fba3600403518912fea4be57fdbc3`**, tooling **`9ce5bfd8b039214238c2b511e3ec72af237246a0cf09b052eb7f510baff373d7`**, version `4.31.2+b7a9c9ec9f57e098b408d3b940929ef4b160985b`. These are local unsigned unpublished diagnostic bytes, `releaseEligible:false`. Clean Builds removes HXW009; the previously disclosed unavailable tool-range HXW010 remains a source-mode warning without release/package credit.

Final affected verification passes **332/332 Builds cases** and **26/26 exact-installed Platform graph/readiness cases**, zero errors/failures/skips/not-run. The rooted 713-file graph copy replaces exactly tooling and the actual final model; host trace and before/after hashes bind both. Debug test compilation, official Release build/pack and final catalog/TLS guard compilation report zero warnings/errors. A separate Linux-only private output-flow probe has one CA1416 portability warning, disclosed rather than suppressed. Its two actual-installed tests pass: original startup deadline and caller cancellation both terminate continuously flowing stdout/stderr after at least 1 MiB, with fixture-process exit observed in about 1.17 seconds each. These process fixtures are not operational runs.

The unchanged direct helper's **91/91** offline results and earlier **57/57** catalog/model cases are reused only at identical source/pin scope: the four catalog repair files, 306 unique selected identities and approved values remain unchanged. Fourteen additional active-consumer assertions pass across four completed cases in an interrupted private invocation and a clean ten-case continuation; **no clean all-14 invocation is claimed**. They cover valid stale/future/4500-second refusals before access, inclusive60/4501 boundaries, populated indexes, independent index/reminder failures, physical rows retained through observed TTL without reclamation credit, near/beyond900-second grace and six distinct endpoint/token pairs. The exact final installed report/access readers pass **eight** producer-generated success/failure/malformed/foreign/pre-existing controls; originals are preserved while disposable copies are consumed. Eight final catalog/TLS guard checks independently cover populated null metadata and same-trusted-CA hostname rejection. Controlled fixture clocks/HTTP/SQL confer no real retention, SQL/C-VG3 or qualification credit. All private fixture errors/interrupted attempts remain retained.

The actual final native/OCI verifier accepts the approved Dapr1.18.1 linkage and refuses the wrong native binary. Native SHA-256 remains `be0d0ee7362c6b8626538876fa2342d1c0789d126a60129a7bf27009a072769c`; OCI archive `ada1aa56a6c5e6a182bdba144de752e8c1f7b93dcd0822f32ed3f9d3e404fcda`. CLI1.18.0 and native1.18.1 read back. The embedded default Dapr catalog remains1.18.2, while the existing selected manifest/profile explicitly require the approved1.18.1; no catalog/default upgrade or fallback occurs. Redis readback equals approved `docker.io/library/redis:7.4-alpine@sha256:ff02b58f971e7d7d156a1267e283fcbbeee91773b6aa36c49dac28ecfe28eadf`. All other immutable images and complete operational-contract clauses below remain unchanged.

Read-only capacity/collision observation finds Docker14 CPUs /97,021,218,816 memory bytes, Linux MemAvailable56,054,892 KiB, local and Docker/backing-filesystem space available, and eight unrelated running containers. PostgreSQL's8 CPU /16 GiB /100 connection /400 GiB tmpfs ceilings and Dapr pool40 are limits, not reservations or workload-demand estimates. Host C backing free space is106,562,367,488 bytes; Docker virtual-disk free space865,435,824,128 bytes is a distinct observation. No retired VM floor is reinstated. Existing containers/listeners are preserved; the runner must allocate/check its own run-scoped ports/names and enforce ownership. CI and environment overrides are absent. The earlier local `df /var/lib/docker` exit1 is retained and corrected by read-only observation inside the already-running Docker Desktop distro; no storage/service mutation occurred.

### Review Triage Log — source-drift continuation

All three configured context-free lenses reviewed frozen diff **150,885 bytes / `6eee7bac93adbd72827bae9e7db20cbbbb7e60467760a281c212262cff8a9309`** (blind floor10). All three separately reviewed the subsequent **6,661-byte** output-draining delta **`c819cda65321d7c98ab65ec53ca4b13401b60e0c864bd45451b81ca1cdc0cc6e`** (blind floor3). The first edge review returns `[]`; final verification reports no gaps. Each filed finding has its own verdict below. Four generic follow-ups remain with their owners: package/debug configuration, pure-getter version validation, test-generator failure propagation and Windows test portability. They do not authorize source changes or invalidate the observed selected Linux source inputs. Per the owner's two-document restriction, no deferred-work ledger or other document is created. Original status/baseline/review-loop frontmatter and all historical bytes remain intact; no rollback, staging, commit or sprint transition follows generic workflow instructions.

| Finding | Verdict | Route | Evidence / disposition |
| --- | --- | --- | --- |
| SD-BH1 | medium | defer-outside-selected-source-mode | Package/debug launch configuration disagrees with package Release imports; launch behavior predates this delta. The approved selected declaration and ExecutePlatformAsync require source mode with DebugAttach=false, so that package/debug combination is not admitted. Preserve for package-path owner follow-up in these two authorized documents.  |
| SD-BH2 | medium | defer-pure-getter-package-validation | The pure catalog getter accepts a correctly self-hashed blank version, predating these guards. Current admission compares all selected catalog values/pins against the approved nonblank snapshot; coherent fabricated metadata is outside trusted materializer custody. No blank or altered pin is admitted. Preserve the generic API follow-up without a source change.  |
| SD-BH3 | false | reject | RunReadinessHealth.WaitAsync persists CompositionReadinessProgress after each unhealthy evaluation; CompositionEngine reads and preserves that progress on deadline expiry. Correlated module-unready does not erase those observations. A new observation never returned by a cancelled probe cannot truthfully be recorded as measured.  |
| SD-BH4 | low | private-focused-verification | Add same-CA correct-host control and wrong-host rejection against the exact parent model health check; no source edit. Final exact-model guards accept matching hostname and refuse wrong hostname signed by the same trusted CA, 8/8 combined checks. |
| SD-BH5 | low | private-focused-verification | Add otherwise valid stale/future observation and less-than-4501-second remaining budget refusals before access in the private fixture extension. Actual unchanged helper refuses otherwise-valid stale/future observations and 4500-second remaining budget before HTTP/SQL; accepts inclusive60/4501 boundary. |
| SD-BH6 | medium | private-focused-verification | Add populated expiry buckets, retained index entries and stationary reminder sequence failures through the actual active consumer. Populated expiry buckets are observed. Independent retained-index and stationary-reminder cases both refuse with functional-purge-deadline. |
| SD-BH7 | low | private-focused-verification | Add retained physical rows/observed TTL and near/beyond-grace cleanup cases in controlled offline fixtures. 125 physical rows survive nominal expiry and observed TTL without reclamation credit; 870-second cleanup passes, post900-second observations fail, including cleanup at899 not observed until901. |
| SD-BH8 | low | private-focused-verification | Use disjoint per-service app/Dapr endpoints and tokens and assert exact pairing in the private fixture extension. All six app/sidecar endpoints and all six tokens are distinct and paired exactly; selected producer and every state/actor request are checked. |
| SD-BH9 | medium | private-focused-verification | Feed generated retained TRX/observation copies through exact installed SelectedNativeTestReport readers, including refusal cases; retain original bytes before reader unlink. Actual final9ce5 installed readers pass eight generated-success/failed/malformed/foreign/pre-existing cases. Disposable input copies unlinked; retained originals unchanged. |
| SD-BH10 | low | documentation-clarification | The primary workspace revision is separate source/document custody; the mapping contains Platform and its direct entries. Append this correction while preserving earlier bytes. Latest appended documentation explicitly separates primary Memories custody from Platform/direct-entry mapping. |
| SD-VG1 | low | private-focused-verification | Preverified null ProjectVersions/value regression gap. Check otherwise populated correct-mapping snapshots independently, with a positive control, against installed CatalogVersion. Otherwise populated correct-mapping snapshots independently reject null collections and null project dictionary value as InvalidDataException; valid positive control and direct hash failure control pass. |
| SD-FBH1 | low | defer-test-robustness | External new test can pass if head fails. Current /usr/bin/head is independently verified to emit exactly33554432 zero bytes with exit0 and no stderr. Current332-case run includes the allocation test. Generator failure propagation remains an owning-test follow-up; no source edit authorized.  |
| SD-FBH2 | low | defer-test-portability | New output regression test intentionally skips Windows. This approved source experiment uses the observed Linux amd64 native/OCI tuple; no Windows assurance is claimed. Portable output generator remains an owning-test follow-up.  |
| SD-FBH3 | low | private-focused-verification | Exact installed9ce5 runner is now probed during continuous output on both streams. Deadline and caller cancellation pass in about1.17s each after at least1MiB actual output, with both fixture processes observed exited.  |
| SD-FEC1 | low | defer-test-robustness | Carried same generator-failure claim asSD-FBH1, preserved as its own finding. Current concrete generator precondition passes; future test should propagate generator errors.  |

Exact commands, UTC intervals, logs/XML, source/artifact hashes and private probe provenance are under `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r` and its **`final-source-rebind-20261017-h25ndaog/`** child. `reviews/final-review-triage.json` retains all15 rows; `review-gap-probes/final-private-probe-summary.json` retains every private failed/interrupted attempt. Final receipts include `final-official-diagnostic-pack`, `final-private-tool-restore`, `final-package-payload-provenance`, `final-current-installed-model`, `final-builds-affected-debug-build`, `final-builds-startup-selection`, `final-installed-graph`, `final-guard-cases`, `final-installed-readers`, `final-native-artifact-verifier`, `final-startup-flow-cases` and `final-pin-readback`. Each `.json` receipt contains exact argv/cwd/environment scope, timing/exit, source snapshots and command-log hash. Shared .NET runtime/host files and the reused source-labelled dependency closure remain outside complete package-closure acceptance.

The sole existing operational command, after final independent admission checks, is:

```bash
cd /home/administrator/projects/hexalith/platform
HEXALITH_DAPR_HOME='/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home' dotnet '/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/final-source-rebind-20261017-h25ndaog/tool-preparation/diagnostic-nuget-cache/hexalith.builds.module.cli/4.31.2/tools/net10.0/any/Hexalith.Builds.Module.Cli.dll' test --manifest /home/administrator/projects/hexalith/platform/references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --profile functional-retention --mode source --output json
```

This invokes the existing runner once, with its own immutable execution/startup/stopping clocks; the private offline recorder's1200-second timeout is **not** used. Startup/readiness/loaded-identity/SQL admission happens within the existing contract before any cohort. Preserve realUTC/monotonic time,125A + at least1800 real seconds +125B, one-hour retention and≤900-second grace, fixed read-only `expires_at` SQL, finite submissions and≤16 concurrent fixed-order GETs. A failure closes the mutation gate and retains FR4 evidence/resources; never automatically invoke a second run or fresh-budget down. Only successful owned cleanup earns success. Live/full tasks remain unchecked until actually evidenced; no A41 close-out, full qualification or Production activation is implied.

## Source-drift preparation and offline verification — 2026-10-17

The implementation preparation passes for its measured snapshot. **Operational admission remains `HELD_SOURCE_DRIFT` until the parent rebinds the externally advanced Platform revision and completes the configured reviews and independent final gates.** This addendum supplements the existing complete operational contract; it does not grant another run or change any deadline. No operational request T has been admitted by the implementation subagent.

Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r`. Each receipt below retains complete unabridged `argv`, cwd, UTC start/end, subprocess duration excluding snapshots, exit, raw log SHA-256, five-checkout source snapshots and local artifact inventories. Explicit recorder overrides and selected inherited metadata are labelled separately; this is not a complete effective-environment capture. Local process/listener doubles are offline verification, with no real AppHost/workload/service/container or selected runtime target started/contacted.

| Measured checkout | Full revision and scope |
| --- | --- |
| Primary Memories | `92c25d22e329ccd37bd776b73725641750a697e9`; only this report and existing Spec 14 change |
| Standalone Platform model | `9b73d777590a58f847e90d3658b09efd4185be63`; actual installed mapping/model interval `2026-10-17T15:12:26.490414Z`–`15:13:04.193378Z` |
| Platform final graph/readback | `3e9cc4c7cf374e114e8a22f553eeaec8de3c8142`; later external commit, not an implementation-agent Git operation. Requires fresh current mapping rebind |
| Direct Builds | `920389059d0fa4c0485709500d147873c4baaadb` plus existing dirty tracked patch `2acd8627326d88a787b0efac0e1c2e789bd6e1ea136548ca6f5e1507d1a384cb` and separately retained untracked test-helper bytes; dirty bytes are not contained in HEAD |
| Direct Memories | `d11adf8970ca9810b91cd04e19741bfcf3b93167`; actual current 91-case suite, distinct from historical 79-case evidence |
| Direct EventStore | `1353b83939de812671260f1d7a5910ee8fb60119`; runtime source/build declarations equal their earlier compared revision, while evidence/history advanced |

| Receipt under `receipts/` | Measured result | Exit | Process seconds | Raw-log SHA-256 |
| --- | --- | --- | --- | --- |
| `official-diagnostic-pack.json` | Official current Release solution build and two-tool diagnostic pack; zero warnings/errors | 0 | 63.414985 | `d5c1cb63ce41d14617a28eab24c1339e4dacd62d0f3fe1da1dc4bab06826f88a` |
| `private-tool-restore.json` | Separate private consumer/cache, exact version 4.31.2 | 0 | 2.091510 | `b1e834c0fb3d4a28eb7c126b7dfe0e8f250039735164062dd584e84a60f893f1` |
| `package-payload-provenance.json` | All 303 Module / 34 Evidence installed payload entries verified | 0 | 4.952298 | `f0e088c355d63e23857ffec107d3af1af90502b91cb35cdcc379a2b04d628635` |
| `actual-installed-model.json` | Actual installed resolver/materializer/full model succeeds at Platform 9b73d777 | 0 | 37.704277 | `07593e7eee9165660e0e0b67a8ce81d7b9d1d75edff09a24f279972b0f78ec73` |
| `candidate-functional-offline.json` | 91/91 actual direct Memories cases; 76.245s unittest time | 0 | 76.976695 | `fefa56615335f912cbf354a95d485e07fa2fac9b1f82df647b9dc13eb58a4527` |
| `builds-affected-debug-build.json` | Current affected Debug compilation; zero warnings/errors | 0 | 48.167789 | `3030a5b62f414be63e89efcb28dd7e17cb07f870938ce280f45152202537b447` |
| `builds-startup-selection-offline.json` | 331/331 affected cases; 121.502s xUnit time | 0 | 121.904501 | `a0cc467d2aa78a4602e4048a637b52ef7249fc1fac960ad0961511ae4dd196eb` |
| `platform-graph-debug-build.json` | Initial current test compilation; zero warnings/errors | 0 | 7.423287 | `6d080a0936a3477437df8b2c358353ef59ace9f8139671c8f5056b1a4b2c9567` |
| `platform-installed-model-graph-offline.json` | Superseded artifact: 25/26; concurrent test-source change between compile and execution | 1 | 19.219374 | `d1b6a9faf60e76ddb23df7c2a901a4b614c41303a805755e03be14fb36bc865f` |
| `platform-forced-test-recompile.json` | Forced current-source test compilation; zero warnings/errors | 0 | 10.984563 | `47307f6659e4fffb686ce307a7280ae0ae0371dc784d06efaad3144985de6921` |
| `platform-recompiled-installed-graph-offline.json` | 26/26 current graph/readiness cases; 15.127s xUnit time | 0 | 15.337841 | `3b57edadfd259cafd44765f17f2333a7fd186e30817bb5bfa650407dfc938f02` |

The fresh feed/cache is `tool-preparation/`, retaining unchanged observed diagnostic version 4.31.2. Module nupkg/snupkg are `d643895cd999dc2bd112663d91d5323f0572477338e1048d542bef632df8313c` / `ac019d6ecb6764faacc55550fa03e3486ce582d784ac710586918de5cbf75cf9`; Evidence nupkg/snupkg are `af3de744594559a0ef2201e3fad71ce57413ceb0e15c1bbcfecf05fbe9cecbc9` / `620086fa9ce0fa5090539e77c49ede31fa1663dadde2c6ad9048a60700993dd2`. Installed tooling is `c12fd2b0d5f7f7cc48f441b9f61d1c9dad0f1eebc1b98ccbcd7ac16cda206c08`, CLI `9633e4d5415431d8b0edb3680792a2af48740a705608800ac11b6c37a91e16c0`. Existing publisher-derived verification matches all 303/34 payload entries. The package version readback is `4.31.2+920389059d0fa4c0485709500d147873c4baaadb`; these unsigned unpublished local archives are distinct from published 4.31.2 and remain `releaseEligible:false`. No release/source/package assurance is invented from that label.

The actual installed resolver selects the existing Platform root/direct Memories and 17 direct entries, without a fallback or index/gitlink mutation. Source-mode HXW009 describes dirty Builds; HXW010 describes the absent declared tool range. Both remain disclosed warnings. Mapping content hash is `fdad4b9d6a0fb02015fa6ab2e292f1eee7bfe0fdf999120048a1c639313ed5ec`; actual model hash is `07a8822f57cb48774a58a2709a5bc194839723df8dc11d28f53bf36a97a64afd`. The model API discards successful child stdout, so its pass is not a zero-warning full-model observation. `inspection/current-materialized-catalog-comparison.json` proves identical selected Versions, PinnedFiles, ProjectVersions and CatalogFileHash; only CatalogCommit changes. The four reviewed catalog files match the earlier 57-case evidence byte-for-byte, as retained in `inspection/current-source-artifact-binding.json`; that unchanged lane and earlier 306-unique-version inventory are explicitly reused, not newly executed.

The final graph lane includes `*Retention*`, `*PostgresqlReadiness*`, `*Redis*`, `*SelectedDaprHome*`, `*ControlPlaneWithout*` and `*FileEntryRefusesNonDevelopmentBeforeComposing`. Its 26 cases include trusted/untrusted/non-TLS/open-port/caller-cancellation PostgreSQL observations and named Production refusal; these use local synthetic TLS/listener fixtures, not PostgreSQL or SQL execution. The initial 25/26 failure remains retained: compilation ended with test-source hash `153c61569f3c10b069704fc960702b3edc4717c62e5dc6ab1d0559727a2bbd83`, but execution read the concurrently changed `9bd8a804b8aa43686f6126832d903f9de90377d453f507d78eceaf60dbfb53b3`. Current source already corrects the digest/tag assertion. Forced compilation of that existing source, without edits, resolves the artifact mismatch. No test failure is erased, and no source repair is attributed to this task.

`inspection/graph-recompiled-rooted-test-copy.json` binds a 713-file generated test copy under Platform's ignored `bin/Debug/spec14-recompiled-source-drift-f50jvn7r/`, replacing exactly installed tooling and actual model DLL. All copied bytes remain unchanged across the passing run, and host trace resolves both selected DLLs. The freshly compiled test DLL hash is `08ee60fa0936096cfa42c2b382f5fa32d534f5621d0892c4ea881097b20771f5`. `BuildProjectReferences=false` reuses dependency outputs and source-labelled deps; shared .NET host/runtime files are outside local output hashes. No full dependency/package-closure acceptance follows. A preliminary private copy outside the repository was never executed: existing tests discover `apphost.cs` through assembly-path ancestors, so the existing rooted-copy seam is required; that unused copy is retained as preparation evidence.

The 331 Builds cases cover the affected startup, declaration closure/validation, engine, evidence serialization, AppHost validation/stop, workspace composition, selected test and observation classes. Those runtime/test source bytes remain stable through their compilation and execution. The 91 direct Memories cases cover active controlled retention sequencing, fixed SQL text, sixteen-read ordering/bound, foreign/partial/stale source and custody/lifetime refusal, plus historical semantic/render regressions. They grant no actual SQL, real retention or full qualification credit. No unchanged broad historical suite was rerun after documentation additions.

Re-runnable primary commands (fully resolved original vectors/environment are in the named receipts):

```bash
resume_root='/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r'
# cwd: /home/administrator/projects/hexalith/platform/references/Hexalith.Builds
pwsh -NoProfile -File Tools/build-g4-tool-packages.ps1 -Version 4.31.2 -OutputDirectory "$resume_root/tool-preparation/current-source-diagnostic-packages"
dotnet build test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Debug --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
# cwd: /home/administrator/projects/hexalith/platform/references/Hexalith.Memories
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_*.py' -v
# cwd: /home/administrator/projects/hexalith/platform
pwsh -NoProfile -File "$resume_root/restore-diagnostic-tools.ps1"
pwsh -NoProfile -File "$resume_root/verify-diagnostic-packages.ps1"
pwsh -NoProfile -File "$resume_root/verify-installed-model.ps1"
dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj --configuration Debug --no-restore --no-incremental -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

Evidence/preservation: `inspection/command-log-and-test-readback.json` rehashes retained logs/XML and case identities; `inspection/earlier-diagnostic-artifact-preservation.json` rechecks all 1,790 older feed/cache/consumer files unchanged. New private helpers derive only paths from inspected older patterns; their hashes are bound in receipts. All old archives/evidence remain intact. Per-command source snapshots disclose concurrent external Platform documents/secrets work and the externally advanced HEAD; no external delta is overwritten or attributed to this task. Snapshot integrity remains dependent on trusted private materialization/custody and does not lock source against concurrent writers.

One workflow mistake is explicitly retained: before reading the untruncated current header, the implementation subagent invoked `uv run --no-cache /home/administrator/projects/hexalith/memories/_bmad/scripts/render_skill.py --project-root /home/administrator/projects/hexalith/memories --skill /home/administrator/projects/hexalith/memories/.agents/skills/bmad-build` once. Exit 0 printed `read and follow /home/administrator/projects/hexalith/memories/_bmad/render/bmad-build/memories-70b56f25c56e/31ce012a7b2f817e2b58/workflow.md`. Exact output is in `inspection/extra-render-invocation.json`; no source or operational authority follows, no skill under references was loaded, and the command was not repeated.

Remaining work belongs to the parent: re-resolve/rebind actual current Platform mapping, finish all three configured reviews for changed inputs, independently verify final source/artifact/native/capacity/readiness, then admit at most the one already approved operational invocation only if every gate passes. No date-only reapproval is requested within the adopted window ending `2026-10-24T18:00:00Z`. The immutable 14,400-second execution, startup within 600 seconds and separate 900-second stopping budgets, fixed SQL before any cohort, 250-record real-clock experiment and outcome-aware cleanup all remain required. Actual SQL/C-VG3, backend/service identity, real retention/reminder progress, quiescence/cleanup, full qualification, A41 and Production remain open. Original Spec frontmatter/status/baseline/current instruction and all historical bytes remain verbatim; only this existing report and Spec 14 are repository deliverables. No operational test or real workload was invoked by this subagent.

## Owner-directed larger window and source admission hold — 2026-10-17

The owner instructed **“use a larger window”** after the operational approval and replacement-date request. The [current timing section in Spec 14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#larger-owner-directed-request-window--2026-10-17) extends the one initial-request window to **2026-10-17T14:25:00Z–2026-10-24T18:00:00Z**. The same single run retains immutable T+14,400-second execution, startup at most 600 seconds within that budget and one separate stopping budget at most 900 seconds. Latest execution end is **2026-10-24T22:00:00Z**, latest stopping end **22:15Z**. Actual T is assigned only on admission; no budget has started. This explicit owner timing change supersedes the narrower pending-date proposal without another date-only approval request. All other operational clauses remain unchanged.

Admission is **`HELD_SOURCE_DRIFT`**. The read-only check ending 2026-10-17T14:57:30Z observes Builds `920389059d0fa4c0485709500d147873c4baaadb`, direct Memories `d11adf8970ca9810b91cd04e19741bfcf3b93167` and EventStore `1353b83939de812671260f1d7a5910ee8fb60119`, each advanced from the previously verified source binding. Nine Builds startup/composition files and the direct Memories functional-retention helper/tests have changed. Platform remains at `2749ad8abdafc038ded44f6154d7182c2fd71422` with a changed `PlatformPostgresqlHealthCheck.cs`. The four reviewed catalog-repair files still match, while earlier 57/57, 21/21 and 79/79 results cover their original snapshots. Current observations during concurrent changes are not a newly verified candidate. Preserve the external changes and require fresh source/model/artifact binding and affected verification before startup; the wider dates do not bypass that gate.

Evidence is `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/owner-widened-window-20261017-9wkuk76g/`. `read-only-source-admission.json` records exact read-only Git argv/cwd/results, raw file hashes/deltas and the hold. Document baselines and preservation verification are retained separately, without rewriting older evidence. No source/package/dependency or Git mutation, runtime start or selected target contact occurred in this timing update. Only the existing Spec 14 and this report are edited; original frontmatter/status/baseline and earlier bytes remain intact. Full/live tasks, actual SQL/C-VG3, retention, quiescence/cleanup, A41 and Production remain open.

## Expired-window refusal and replacement proposal — 2026-10-17

The owner approved the reviewed operational proposal. At the first approval-time check, **2026-10-17T14:23:32Z**, its initial-request window had already ended at 12:00Z. The proposal explicitly requires an expired window to refuse execution without an automatic shift. Execution was therefore refused before any runtime start or selected target contact. The completed source repair and three reviews remain accepted.

The [current replacement proposal in the existing Spec 14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#replacement-finite-request-window--inactive-pending-approval-2026-10-17) remains inactive pending approval of the replacement dates: one actual initial request T after approval within **2026-10-17T14:25:00Z–2026-10-18T12:00:00Z**, immutable T+14,400-second execution, 600-second startup within that budget and one separate maximum 900-second stopping budget. Latest execution end is 2026-10-18T16:00:00Z; latest stopping end is 16:15Z. Its incorporated exact entry point, source/artifact bindings, 250-record real-clock experiment, gates, failure retention and owned cleanup remain unchanged. No budget has started, and T must not be backdated. No automatic shift or further run is authorized.

Read-only evidence is `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/expired-window-recheck-20261017-mr_eji4i/`. Its `read-only-recheck.json` records exact read-only Git argv/cwd/results and unchanged five-checkout revisions/source hashes, except the two intentional primary Spec/report additions since the last settled command. All **5,160** retained artifact paths match their final graph receipt; direct archive-to-install comparisons freshly verify **303 Module / 34 Evidence** payload entries. Retained native daprd and OCI archive hashes match, and **62** existing command-log hashes match. `recheck.py` and the new `spec-before.md` / `report-before.md` preserve the audit method and this continuation's document baseline. Earlier evidence files were not rewritten.

Existing 57/57 catalog/model, 21/21 current graph and 79/79 direct candidate results are reused for unchanged inputs; this continuation performs hash/preservation verification only. It does not repeat the model, test suites, version queries, native verifier or runtime observations. The existing model/source/package limitations remain. Original Spec frontmatter/status/baseline and prior document bytes remain intact; only Spec 14 and this report are repository deliverables. No Git/dependency mutation, workload/AppHost/container/service start or runtime target contact occurred. Actual SQL/C-VG3, capacity/startup/runtime observations, retention, quiescence/cleanup, full qualification, A41 and Production remain held, and incomplete full/live tasks stay unchecked.

## Final reviewed catalog repair and current binding — 2026-10-17

**Authorized source preparation and all three configured reviews are complete. Operational execution remains inactive pending separate approval.** Final current source is Platform `2749ad8abdafc038ded44f6154d7182c2fd71422`, Builds `83850cc62e7855ca64dee70e33f2e5c9bab1bc7c`, primary Memories `d11adf8970ca9810b91cd04e19741bfcf3b93167`, direct Memories `db5cccc80e22686c7c840fdc53cc11f1d8844ff8`, EventStore `b99a03dfc29021af16511ee664820e05126b73f8`. Task delta `c5b7bf02fe6de6e8f41959987b48be2743dfddbe74450460c880893b23c7f278` from original Builds 4f53d9f is unchanged across its four source/test files and now contained in externally created commit83850cc. The final 57/57 catalog/model result (33+24, 92.152610 process seconds) is reused because those exact files are unchanged; no audit/acceptance-fixture validation is inferred. Its Debug build has zero warnings/errors. The retained actual direct Memories 79/79 result likewise applies to unchanged direct source/test bytes.

Concurrent work committed external audit/fixture data in a15f184, our reviewed repair in 83850cc, Platform documentary/startup/gitlink work in d186ad6 and readiness-test/evidence changes in 2749ad8. `inspection/settled-repository-source-state.json` records all changed paths and original/current hashes. Package declarations and runtime source did not change; the changed selected readiness test was freshly compiled and rerun here. No commit, checkout, staging, index or gitlink update was performed by this task. The previously labelled dirty/c317/4f53 results below are historical; current installed verification binds the settled revisions above.

Private root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f`. Table paths are under `receipts/`; each retains exact argv/cwd/UTC/log, true subprocess time excluding snapshots, selected inherited environment and explicit recorder/helper declarations. This is not a complete effective-environment capture.

| Current receipt | Observed result | Exit | Process seconds | Raw-log SHA-256 |
| --- | --- | --- | --- | --- |
| `settled-official-diagnostic-pack.json` | Official current Release solution build and two-tool pack | 0 | 56.730754 | `3ee274269c78cd413f1c75ef12a07902a9ea2a3cd6f1e5eb70f53285477b471f` |
| `settled-private-tool-restore.json` | Fresh private two-tool restore | 0 | 2.350524 | `b1e834c0fb3d4a28eb7c126b7dfe0e8f250039735164062dd584e84a60f893f1` |
| `settled-package-payload-provenance.json` | All303/34 installed payload entries match archives | 0 | 3.776470 | `b6ecfd60d9663a42ed9c864c9939c3d7a57ad70e7a800a5f68b91eb98f0d8dec` |
| `parent-settled-actual-installed-model.json` | Actual current resolver/materializer/full model | 0 | 24.311475 | `33d927e45623021e9f3333b81397ea5cc13b778e56eb4925be8c2338cd0a1015` |
| `parent-settled-platform-test-build.json` | Current readiness-test compilation; zero warnings/errors | 0 | 6.571662 | `93437181eabf12a41f00b2367b4df43932abc6079aea94e78176771d05dda372` |
| `parent-settled-optional-import-refusal.json` | Absent optional accepted; appearing import/public boundary refused before output | 0 | 2.518199 | `06d7833e0ca45955acc691c397604234776bd5bb71a596303024b40244ff4933` |
| `parent-settled-installed-artifact-verifier.json` | Approved native/OCI accepted; wrong native refused; version-only queries | 0 | 4.178145 | `2d14baf154c41542c7ee66c457112039bbec4bd8389704092e5d0defe1c1444e` |
| `parent-settled-platform-graph-tests.json` | 21/21 current selected/default/Dapr/Production cases | 0 | 11.589985 | `de083445b9adf22763d969d580128bd6bf6e569ef4ef4bd14773b0a22829f167` |

| Current Builds task file | Raw SHA-256 |
| --- | --- |
| `src/libraries/Hexalith.Builds.Tooling/Runtime/PlatformManifestCommand.cs` | `86d2cdae6d1555e0a60ae65da3321db550e4bcd6a008e84c83a8f4ac9b16f57b` |
| `src/libraries/Hexalith.Builds.Tooling/Workspace/CatalogVersionMaterializer.cs` | `5683b35e413628949339646e9b035cd86f9ff4c1ed3ea45da50fcb9bb2682887` |
| `src/libraries/Hexalith.Builds.Tooling/Workspace/CatalogVersionValidationTask.cs` | `abdafab25e2ebb6661d2b2ff1f86a66530b8d8b9b4b598ee1b457903e3b7b743` |
| `test/Hexalith.Builds.Module.Tests/Workspace/PlatformModelCatalogTests.cs` | `4bf2bacf0a60e00dc7af4fbdfe238d2222cefc2b3dc5be1e336d8c8e2a073a7f` |

Final feed/cache is `settled-tool-preparation/`. Module nupkg `4ac58b7396365db07967691ace545a0ed4f43234e7d225ab5b77784f25538f6e` / snupkg `c4720635b0c63948d344c07313f156202423d2c6cc720b9c6dffc5f8ed8ccb0f`; Evidence nupkg `f03c7c321550f64c9256dcc35b98542074a73df36c0aaeab580f96288ff76150` / snupkg `4fe5489052a74abf2b872043aa2c67ab909492304544c4ea08f0e39f041ccffc`. Publisher-derived proof matches 303 Module / 34 Evidence entries. Installed tooling `701e5b5096bfa56c3b8d970145ac24504d67d28b8fa0aea105807988c558cd52`, CLI `9ea748e7b14e27571fba3f4235809c8036920eab3326bdd36b9275eb9a1692e6` report 4.31.2+83850cc62e7855ca64dee70e33f2e5c9bab1bc7c; these unsigned local diagnostic bytes remain at unchanged observed 4.31.2, distinct from published packages and `releaseEligible:false`. The earlier dirty-phase patch is not falsely attributed to original 4f53 HEAD; the current 83850cc commit contains the reviewed source.

Actual current mapping is `1aedaad245b60acfbecfab7118ea692b4748d99194e9a1d2c26a2f7a97bcc6a9`; full model SHA-256 `3eaed52a4ed85a4945b2021f2800c5de1aa23de18a8ded4816bf409c2482360c`. `inspection/settled-materialized-catalog-binding.json` confirms all current version/pinned-file values equal the retained actual 306-identity evaluation, catalog hash `467211a5af7a50fbf6b0611a938a292ae6998240b2f49e460da8c183977f4ae4`. That query is reused explicitly; the current full installed model independently passes. Clean current Builds no longer reports HXW009; absent Platform tool range HXW010 remains a source-mode warning without package/release credit.

`inspection/parent-graph-settled-copy.json` and `inspection/parent-settled-graph-readback.json` bind the freshly compiled 713-file test closure, exactly two DLL replacements (final installed tooling/current model), both host-resolution entries, 21 passing named cases and unchanged source/copied closures. Remaining dependency outputs/source-labelled deps are reused; no package/dependency-closure acceptance follows. Earlier five MSB3277 warnings and the successful-model API's discarded child stdout remain disclosed limits. Shared .NET host/runtime files are outside local artifact hashes. Earlier invocation/repair failures remain retained without passing credit.

The three fresh review lenses covered frozen diff 7dab79dc4dcffb2d39a7540d75d8c2935b78f7cd5f15c0759fae8d71657e92d3; all eight blind and one edge findings have individual Spec verdicts, five direct corrections/four refutations/no deferrals. Verification-gap found no gaps. The corrected source delta dafb1e4b78792e0f51fb5009f6ac6ea9df8373895f5401a95acacf554421d81a was independently checked through 57 tests and current installed refusal/model/native/graph checks. Snapshot integrity still assumes trusted materializer/private-workspace custody, not independent Git provenance, physical-link containment or protection from concurrent writers.

`inspection/settled-older-artifact-preservation.json` verifies five prior 356-file archive/cache inventories: all 1,780 files unchanged. Final metadata-aware source/artifact/log and historical-byte preservation checks are retained separately from frozen reviews. The [exact current proposal](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#fresh-finite-operational-proposal--inactive-separate-approval-required) binds these current revisions/hashes, one separately approved actual request T in 2026-10-17T09:40:00Z–12:00:00Z, 14,400-second execution/600-second startup/separate 900-second stopping, latest stop 16:15Z, and unchanged 250-record real-clock experiment. Old 08:00Z/VM/spent budgets transfer no authority. Actual SQL/C-VG3, capacity/startup/runtime/backend observations, retention/reminder progress, quiescence/cleanup, full qualification, A41 and Production remain held. Original Spec frontmatter/status/baseline and pre-continuation bytes remain intact. No workload/AppHost/service/container was started and no selected runtime target contacted.

# Story 27.4 — consolidated retention results

### Pre-review catalog-import repair and installed-source verification — historical

**The repaired actual installed source model now builds successfully.** EventStore evaluates 306 unique approved package identities, with central-version resolution enabled. This supersedes the earlier NU1506 model-build hold below for the exact dirty source and diagnostic bytes bound here. It does not complete Story 27.4, qualify a release or authorize operational execution. Parent independent offline checks also pass as recorded below; three fresh configured reviews and final binding remain separate workflow work; the fresh finite operational proposal stays inactive pending that finalization and operational approval.

Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f`. All eight frontmatter context files and the complete Spec 14 were loaded before implementation; no skill under references was loaded. Repository changes are limited to three existing Builds source files, one focused new test file, existing Spec 14 and this report.

`PlatformManifestCommand.BuildModelAsync` verifies the materialized catalog snapshot and complete pinned file closure before launching a model process. It imports the pinned catalog before owner early props, prevents the SDK's second import, and retains NuGet's `CentralPackageVersionsFileImported=true` marker. The existing materializer owns the shared pure validation; package validation delegates to the same checks and retains its source-mode behavior. Missing/altered authority remains a controlled refusal. No version declaration, warning suppression, dependency/tool manifest, EventStore/Platform source or gitlink is changed.

The fresh exact proposal is in [Spec 14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#fresh-finite-operational-proposal--inactive-separate-approval-required): one request in 2026-10-17T09:40:00Z–12:00:00Z, four-hour execution, 600-second startup and separate 900-second stopping. It binds the new installed diagnostic bytes, actual Dapr home, immutable images and unchanged real-time 250-record experiment. It remains inactive pending all three configured reviews and separate owner operational approval; no execution is inferred.

The shared guard checks the trusted materializer snapshot, its command mapping/commit and recorded file hashes. It does not independently rederive Git provenance after materialization, enforce physical symlink containment, or lock bytes throughout child evaluation. Fresh private-workspace custody and absence of concurrent writers remain required assumptions; coherent rewriting of metadata with files or replacement during the child is outside this snapshot-integrity check. The actual preparation separately binds source, materialized bytes and installed payloads, and observes no such changes.

### Dirty source and focused regression binding

Builds HEAD remains `4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77`; it does **not** contain these uncommitted bytes. `inspection/verified-builds-source-binding.json` and complete patch SHA-256 `c94cf95d5770642e9ec9970ca7de089d1c99113b3b5af4cebff4380465cd7ac7` include the untracked new test as well as the tracked source delta.

| Builds relative file | Raw SHA-256 |
| --- | --- |
| `src/libraries/Hexalith.Builds.Tooling/Runtime/PlatformManifestCommand.cs` | `86d2cdae6d1555e0a60ae65da3321db550e4bcd6a008e84c83a8f4ac9b16f57b` |
| `src/libraries/Hexalith.Builds.Tooling/Workspace/CatalogVersionMaterializer.cs` | `88bf10c04ceea864af8a3e0db3468f7da1aa3774f840214120aba09645691688` |
| `src/libraries/Hexalith.Builds.Tooling/Workspace/CatalogVersionValidationTask.cs` | `abdafab25e2ebb6661d2b2ff1f86a66530b8d8b9b4b598ee1b457903e3b7b743` |
| `test/Hexalith.Builds.Module.Tests/Workspace/PlatformModelCatalogTests.cs` | `be690d118e8c02203ff6f165be53ce14e345a8e1f4eff7da7c29e97370c49d0e` |

The final 18 model-builder cases cover source/package configurations, early properties, conditional per-project values, altered live catalog values, actual versionless-reference restore from a private local feed into isolated caches, resolved `Hexalith.Dep/1.0.0` assets marked centrally managed, and missing/changed catalog, snapshot, mapping, pin, mode and malformed-path refusals before model spawn. The 33 existing catalog cases passed in the separate earlier 49-case invocation; their task/materializer bytes are unchanged since that run. **51 unique cases are covered across those separate invocations**, not by one 51-case run. The earlier 16 model cases are superseded by the final 18.

| Receipt under `receipts/` | Observed result | Process seconds |
| --- | --- | --- |
| `verified-debug-build.json` | Focused Debug build; zero warnings/errors; exit 0 | 22.990762 |
| `materializer-catalog-tests.json` | 33 unchanged catalog cases within the earlier 49/49 lane; exit 0 | 57.597364 |
| `verified-model-catalog-tests.json` | 18/18 final model-builder cases; exit 0 | 41.651263 |
| `verified-official-diagnostic-pack.json` | Unchanged official workflow; two tools and symbols; exit 0 | 47.396348 |
| `verified-private-tool-restore.json` | Fresh private two-tool restore; exit 0 | 2.011533 |
| `verified-package-payload-provenance.json` | 303 Module / 34 Evidence installed payload entries; exit 0 | 3.322347 |
| `verified-actual-installed-model.json` | Actual installed resolver/materializer/BuildModelAsync; exit 0 | 19.415322 |
| `verified-eventstore-catalog-evaluation.json` | 306 unique approved catalog items; exit 0 | 0.794880 |

The final Debug command rebuilds its project references; interim test-only `BuildProjectReferences=false` builds reused already-built dependencies and are separately retained. The unchanged official package workflow performs its prescribed Release solution build and packs, with zero warnings/errors. No unrelated suite was rerun. Exact expanded argv, cwd, UTC bounds, exits and raw-log SHA-256 values are retained in each receipt. The final model log hash is `ac94ca4454638cbd7e9152352e54a3d6213ddef2bbda233bfaf1eb9e6fe5007d`; final model-test log is `82e378fa1f756e0269650e2215e3f4aef3cd43f7e751405c2c7495e96021c790`.

From the direct Builds cwd, the final build/test/package commands are:

```bash
repair_root='/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f'
dotnet build test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --no-restore -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet test/Hexalith.Builds.Module.Tests/bin/Debug/net10.0/Hexalith.Builds.Module.Tests.dll -class '*PlatformModelCatalogTests' -result-xml "$repair_root/logs/verified-model-catalog-tests.xml"
pwsh -NoProfile -File Tools/build-g4-tool-packages.ps1 -Version 4.31.2 -OutputDirectory "$repair_root/verified-tool-preparation/current-source-diagnostic-packages"
```

The last command requires a new unused output directory if reproduced; the retained evidence directory must not be overwritten. The separate private restore/version helpers select `verified-tool-preparation/diagnostic-consumer` with its exact manifest/config, `NUGET_PACKAGES` and `DOTNET_CLI_HOME`. Actual installed model reproduction, from `/home/administrator/projects/hexalith/platform`, is `pwsh -NoProfile -File /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/verify-verified-installed-model.ps1`; the helper is execution-time hash-bound before/after and invokes the actual installed method with a 600-second bound, without starting a runtime.

### Fresh private diagnostic artifacts and actual model

The existing unchanged publisher payload-hash function verifies every installed tool payload against the archives. These local unsigned 4.31.2 packages remain distinct from published release bytes and `releaseEligible:false`; their version string `4.31.2+4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77` does not assert the dirty source is in HEAD.

| Package at `verified-tool-preparation/current-source-diagnostic-packages/` | nupkg SHA-256 | snupkg SHA-256 |
| --- | --- | --- |
| Hexalith.Builds.Module.Cli | `403953002b109f916e0e5dba1621bbca412c710cb15ecebaa4ccc61f87fa3a60` | `097bfa28e58e80050fc42a1d3ff1908b4831883a481e2be5085f505f2d8d64e5` |
| Hexalith.Builds.Evidence.Cli | `89d8274dd5c3e2d2fab219b6fd682f680b624699b120d45caa426c8c2df17feb` | `ad3eed8ff2b2405319dc42fa09125e2ad251015f80f12843186bd18ef6eb0b31` |

Selected installed tooling is `verified-tool-preparation/diagnostic-nuget-cache/hexalith.builds.module.cli/4.31.2/tools/net10.0/any/Hexalith.Builds.Tooling.dll`, SHA-256 `c86f2015f8f97e04ce467dc96278f176801edd6188503efb69ba1d02eb737dc8`. It resolves standalone Platform `f81b864e17012bb75c60374f4efaab87c20d63e1`, direct Memories `db5cccc80e22686c7c840fdc53cc11f1d8844ff8`, 17 mapping entries and mapping hash `9bbbe66340b16dcde78eb84d1bc5ffa6456a5998f50766b9a90ff9940a0157b7`. Actual `BuildModelAsync` succeeds and returns `model-build-verified/hosts/platform-model/Hexalith.Platform.Composition.dll`, SHA-256 `209022639d2a8fd37b3efbce21e0caa527a2e77c0fd101710ac5c760c712603f`.

`inspection/verified-catalog-inventory.json` binds the actual EventStore ServiceDefaults evaluation: 306 items / 306 identities, zero snapshot-version differences, every defining origin the pinned catalog, all central-version/early-import markers true. Approved Dapr hosting remains `13.6.1-beta.935`, EventStore Aspire `3.124.0`, OpenTelemetry runtime instrumentation `1.20.0`; no NU1506 suppression is used. The pinned catalog raw hash remains `467211a5af7a50fbf6b0611a938a292ae6998240b2f49e460da8c183977f4ae4`.

Two intermediate repair failures are retained and corrected: calling the shared check on the MSBuild task type forced an unavailable packaged `Microsoft.Build.Framework` load; moving it to the existing pure materializer removed that dependency. Disabling the automatic import initially omitted NuGet's central-import marker, causing unbounded dependency restore errors; the marker and real local-feed regression now cover that seam. The malformed NUL-path red cases demonstrated an escaping `ArgumentException`; the model boundary now translates it to its controlled refusal. These were repair-caused failures, not independent prerequisites. Earlier artifacts were never overwritten: three preceding archive/cache inventories of 356 files each remain byte-identical (`inspection/older-artifact-preservation-after.json`).

### Parent independent installed checks

Parent independently invokes the installed model in its separate `parent-model-build` workspace: exit 0 in 25.821293 process seconds, same returned model hash `209022639d2a8fd37b3efbce21e0caa527a2e77c0fd101710ac5c760c712603f` (`parent-actual-installed-model.json`, raw log `c811e0465e7fcb0980f7693a15cb163b95711cb0a7238dd03c3da51ce6cc31e7`). The installed OCI/native verifier passes in 5.433416 seconds, accepts approved linkage, rejects wrong native bytes and reports CLI 1.18.0/runtime 1.18.1 through version-only queries (`parent-installed-artifact-verifier.json`).

Parent composition/test builds both exit 0 with `--no-restore -p:BuildProjectReferences=false` and the explicit installed tooling path. Composition emits **five MSB3277 warnings**, including StackExchange.Redis assembly 2.0.0 versus tooling's 3.0.0, while the test build has zero warnings/errors; the file-reference verification bypasses transitive project/package declarations, so zero-warning or package-closure acceptance is not claimed. Initial binding inspection finds the ordinary test output copied a different source Debug tooling DLL. Parent preserves that finding and creates an isolated generated 713-file copy under the existing Platform test `bin/Debug` tree, changes only its tooling DLL to exact installed `c86f2015...`, and preserves both source-labelled `.deps.json` and reused dependency outputs. A host trace confirms that actual installed DLL is selected; both original and copied 713-file closures remain unchanged during execution. This is local installed-DLL graph verification, not a new dependency build or accepted release closure.

The first copied-graph invocation omits explicit Development settings and correctly refuses default Production: **20/21**, exit 1, in 2.512593 seconds (`parent-installed-platform-graph-tests.json`); this is a retained parent invocation error, not a passing result or source defect. With explicit `DOTNET_ENVIRONMENT=Development` and `ASPNETCORE_ENVIRONMENT=Development`, the unchanged lane passes **21/21**, zero failures/errors/skips/not-run, exit 0, in **4.472297 process seconds** (`parent-installed-platform-graph-development-tests.json`, raw log `a398fca24693714d0539c66b1be0fd8f77d71014545649c54bd5deaba386c292`). XML names the selected ComposeRun and explicit Production-refusal cases. `inspection/parent-installed-graph-readback.json` binds XML, host trace and local artifacts. Five-checkout source snapshots remain unchanged for these parent commands. Three fresh configured reviews and final source/document binding remain for the parent workflow.

### Scope, custody and remaining hold

Current receipts capture before/after tracked regular-file raw hashes in **five named checkouts**: primary Memories, standalone Platform, direct Builds, direct Memories and direct EventStore `b99a03dfc29021af16511ee664820e05126b73f8`; the new untracked test is explicitly included. They observe no source changes during commands. Tracked symlink payloads are separately preserved by the link-aware scope check; the command recorder omits symlinks. This new coverage does not retroactively supply EventStore custody to the older four-checkout receipts, and the separate historical 17-reference cleanliness inventory remains only its initial observation. The final model-test artifact set (951 local helper/tool/test DLL/JSON files) and earlier 49-case set (641 files) are unchanged across their respective executions; shared .NET runtime files are not hashed. Model output creation is an expected build artifact change, separately hashed.

`processSeconds` measures only the subprocess interval; source/artifact snapshot overhead is excluded. Explicit recorder overrides are `GIT_OPTIONAL_LOCKS=0` and `PYTHONDONTWRITEBYTECODE=1`; selected inherited environment metadata is labelled separately and is not a complete effective-environment capture. Private restore/version helper declarations also set their isolated NuGet/CLI homes and first-run settings. One intermediate test-build receipt observed a recorder artifact-root extension; `inspection/recorder-extension-note.json` discloses that changed helper hash rather than claiming stability for it. Final test/package/model helpers are retained with execution-time before/after hashes.

Source mapping still reports HXW009 for the approved dirty Builds checkout and HXW010 for Platform's absent declared tool range. Existing source-mode policy treats them as warnings; no package/release acceptance or tool-range declaration is inferred. The source model prerequisite is cleared for these exact diagnostic bytes; final parent reviews/source binding and an inactive fresh exact operational proposal remain workflow work. Actual read-only SQL/C-VG3, capacity, TLS/startup probes, runtime/backend identities, real cohorts/retention, reminder progress, quiescence, cleanup, full qualification, A41 and Production remain held. No workload, AppHost, container, service or runtime target was started/contacted, and no Git, dependency, sprint or Production mutation occurred.

## Current source binding and model-build blocker — 2026-10-17

**The actual repaired direct candidate passes 79/79 and current Platform graph checks pass 21/21. Operational readiness remains held: the actual installed full source model build fails before compilation.** This supersedes the earlier direct-regression and unbound-newer-Platform observations only for the bytes measured here. The source-owned model-build failure, SQL/C-VG3, real retention, full qualification, A41 and Production remain open. The previous 08:00Z proposal is inactive; no fresh usable operational plan or execution approval is requested.

Private evidence root:
`/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/current-source-rebind-20261017-cjnssyes`.
Each of its ten original `receipts/*.json` records the exact expanded argv, cwd, selected inherited environment metadata, UTC interval, exit, raw-log hash and unchanged before/after tracked source snapshots for four checkouts only: primary Memories, standalone Platform, direct Builds and direct Memories. EventStore and the other direct references are excluded from those per-command source snapshots. `preservation/before.json` retains initially clean branch/tree/remotes/history and raw hashes for those same four checkouts. The receipt `environment` field is selected metadata, not a complete effective-input capture: it omits recorder-injected `PYTHONDONTWRITEBYTECODE=1` and `GIT_OPTIONAL_LOCKS=0`. Environment prefixes in command blocks state the declared command settings; they do not claim a complete captured effective environment. All eight frontmatter context files and the complete Spec 14 were loaded before work. No repository skill under `references/` was loaded.

| Checkout | Observed HEAD |
| --- | --- |
| Primary Memories | `d11adf8970ca9810b91cd04e19741bfcf3b93167` |
| Standalone Platform | `f81b864e17012bb75c60374f4efaab87c20d63e1` |
| Platform direct Builds | `4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77` |
| Platform direct Memories | `db5cccc80e22686c7c840fdc53cc11f1d8844ff8` |

`inspection/direct-references.json` separately confirms all 17 Platform direct references initialized, clean and at staged gitlinks at its initial observation, with no initialized nested reference. That initial inventory does not supply per-command EventStore revision/source hashes. Actual installed resolver/materializer execution selects this Platform root, direct Memories and pinned Builds without a Git mutation. Mapping hash is `e0842bda1b242617c1ef34e912715089dfca9d400a7465893dd183b474889735`. Direct/primary CLI and declaration bytes match; only historical `_recorded_platform_preparation_status` differs in the helper AST (`current-consumer-source-comparison.json`). No checkout substitutes for the direct candidate. Prior source/Git changes predate this snapshot and are not this continuation's work.

### Measured offline checks

From `/home/administrator/projects/hexalith/platform`, both commands pass with zero warnings/errors (`current-platform-composition-build.json`, `current-platform-test-build.json`):

```bash
dotnet build src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj --no-restore -c Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj --no-restore -c Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

Both cached graphs select declared Dapr hosting `13.6.1-beta.935`; no cache-version reconciliation was needed. `BuildProjectReferences=false` reuses existing dependency outputs; these commands do not rebuild or establish execution-time custody of that dependency closure.

The actual current graph command, also from Platform cwd, uses the absolute XML path retained in its receipt:

```bash
DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development dotnet tests/Hexalith.Platform.Composition.Tests/bin/Debug/net10.0/Hexalith.Platform.Composition.Tests.dll -method '*Retention*' -method '*Redis*' -method '*SelectedDaprHome*' -method '*ControlPlaneWithout*' -method '*FileEntryRefusesNonDevelopmentBeforeComposing' -result-xml /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/current-source-rebind-20261017-cjnssyes/logs/current-platform-graph-tests.xml
```

**21 passed**, zero failures/errors/skips/not-run; exit 0, runner time 2.242 seconds (`current-platform-graph-tests.json`). XML explicitly names `RetentionSelectedComposeRunAdoptsGraphCallbacksReadinessAndIsolatedControlPortsAsync` and the Production refusal `FileEntryRefusesNonDevelopmentBeforeComposing`. Redis unhealthy messages are expected offline negative fixtures, not observations of live resources. The original 21-case receipt/log/XML has no execution-time assembly or dependency-output hash binding. Its passing result is retained with that artifact-custody limitation; the focused builds reused dependency outputs through `BuildProjectReferences=false`.

From `/home/administrator/projects/hexalith/platform/references/Hexalith.Memories`:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_*.py' -v
```

**79 passed in 16.547 seconds**, exit 0, zero failures/errors/skips (`current-direct-functional-tests.json`). This includes the repaired boundary method, missing/foreign-input and legacy-plan refusals, preserving Story 20.2 denial-before-dependency and Story 24.3 verifier/attribution boundaries without live credit. Primary's unchanged historical 91-case lane is not rerun for documentation preparation.

| Current receipt | Exit / observed scope | Raw log SHA-256 |
| --- | --- | --- |
| `current-platform-graph-tests.json` | 0; 21 offline cases | `4c038eff7533eb3b5680b4da741c01521bf7083c2d35e6cddde0a81586749568` |
| `current-direct-functional-tests.json` | 0; 79 actual direct cases | `892ceeab26bd17728e97abeca513b7765b5969a282c69ae283c6f5b173b29fa1` |
| `current-installed-model-build.json` | 1; actual installed resolver/materializer/model-build API | `dded228d436e2f0f58187bf755e2f0e84121bfd3a526400cecca06763c64bff9` |
| `current-model-build-raw.json` | 1; exact model-build argv, five NU1506 errors | `5beda3007e348d8f4664e6bd8e145373f971dc963b9352f6c66330fc4bbc8b0a` |
| `current-package-duplicate-evaluation.json` | 0; read-only MSBuild evaluation | `c4746decc3be85380f29529e5cb6cebd65d7707b14b1a3a60d48c342acb30e8d` |
| `current-diagnostic-package-provenance.json` | 0; 303 Module / 34 Evidence payload entries and version | `5764551b383c739078946a0b7acd38e0c604df4dd505e224f0a401fbb58c7864` |
| `current-installed-runtime-verifier.json` | 0; existing installed OCI/native verifier, wrong-native refusal, version-only queries | `e8ef777df68221c289a582ac9ecc0fe5d6449d82c531f5a62923c90592cfa545` |

### Exact model-build hold

Reproduce only the offline source prerequisite from Platform cwd with:

```bash
pwsh -NoProfile -File /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/current-source-rebind-20261017-cjnssyes/verify-current-model-build.ps1
```

The retained script currently loads the installed diagnostic tooling DLL, resolves the absolute direct Memories manifest, materializes the existing mapping/catalog and calls existing `PlatformManifestCommand.BuildModelAsync` with a 600-second bound; it does not call runner/AppHost start. The original receipt records its path/argv but contains no execution-time hash of that private script. Reading the retained script now cannot establish its historical executed bytes, and no such script provenance is claimed. The original log reports successful mapping/materialization followed by exit 1.

The installed-script process interval from its recorded `startUtc`/`endUtc` is **4.455195 seconds**; the raw-build interval is **1.173964 seconds**. Their original recorder `elapsedSeconds` values remain respectively `5.77824393499759` and `2.4680653390096268`. Those recorder measurements include post-command preservation-snapshot overhead and are not process runtimes. The helper truncates its diagnostic, so `current-model-build-raw.json` separately retains the expanded raw build argv below. That raw command exits 1 with zero warnings and **five NU1506 errors during restore**, before compilation. From `/home/administrator/projects/hexalith/platform`, its exact recorded command is:

```bash
dotnet build /home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj --configuration Debug --output /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/current-source-rebind-20261017-cjnssyes/model-build/hosts/platform-model -p:BuildsToolingAssemblyPath=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/tool-preparation/diagnostic-nuget-cache/hexalith.builds.module.cli/4.31.2/tools/net10.0/any/Hexalith.Builds.Tooling.dll -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -nodeReuse:false -p:UseSharedCompilation=false -p:DirectoryPackagesPropsPath=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/current-source-rebind-20261017-cjnssyes/model-build/source-mapping/pinned-tree/Props/Directory.Packages.props
```

EventStore ServiceDefaults, Aspire, Contracts, DomainService and Client fail duplicate `PackageVersion` validation. No CS1705 result or full Platform pass is inferred. The focused `BuildProjectReferences=false` builds cannot clear this full source-build prerequisite.

The failure is localized to existing Builds `src/libraries/Hexalith.Builds.Tooling/Runtime/PlatformManifestCommand.cs:BuildModelAsync` and its existing catalog/import seam (`Workspace/SourceMappingMaterializer.cs`, `Workspace/CatalogVersionMaterializer.cs`), consumed by Platform build configuration and EventStore `Directory.Build.props`. The full build propagates pinned `DirectoryPackagesPropsPath` to referenced projects; EventStore early props already import the declared live Builds catalog before the SDK imports the separate pinned path. Read-only MSBuild evaluation of actual ServiceDefaults reports **612 items / 306 duplicated identities**. `inspection/duplicate-catalog-origin.json` identifies both defining paths for Dapr hosting `13.6.1-beta.935`, Aspire hosting `13.6.1` and EventStore Aspire `3.124.0`. This is evaluated import duplication before cache resolution; cache hydration cannot remove those duplicate definitions.

Live catalog raw SHA-256 is `1a9fcfe7360e4b756db11173ef9bbc4f6dc49dd983c297b7fc27d42bd7f31dcd`; materialized catalog is `467211a5af7a50fbf6b0611a938a292ae6998240b2f49e460da8c183977f4ae4`. Normalized text matches; working-tree BOM/CRLF versus materialized Git UTF-8/LF explains the raw difference. No byte equality is asserted. `inspection/model-build-source-blocker.json` retains exact source pointers, affected projects, hashes and raw argv. No pin/source/declaration edit, cache deletion or warning suppression was attempted.

### Tool/artifact boundary and next owner

The existing private manifest remains pinned to both diagnostic tools at 4.31.2. The unchanged `verify-diagnostic-packages.ps1` validates archive/inventory and all 303/34 installed payload entries through the existing publisher's hash function, then resolves the actual private consumer manifest for a version-only query. Module reports `4.31.2+4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77`. These unsigned/local bytes remain distinct from published 4.31.2 and `releaseEligible:false`; no new build/install/publication was required.

Actual mapping emits **HXW010** because Platform `hexalith-platform.json` has no `toolVersionRange`. Current `WorkspaceRootResolver` and `ModuleCommandExecutionService` report this as a source-mode warning (`ToolFailureCategory.None`), not the measured refusal. The verified private pin establishes no declared supported range or package/release acceptance. Neither the identity record nor a tracked manifest is changed. Direct installed-DLL `test --help` passes from Platform cwd (`current-installed-cli-help.json`); its packaged `g4-host` payload is present and verified. A later invocation must select those exact installed bytes while the absolute manifest selects Platform workspace identity. The old bare Platform-cwd `dotnet tool run` command cannot resolve an absent workspace manifest; no hypothetical `--tool-manifest` run option or active test invocation is used.

Actual installed `CompositionRuntimeArtifactVerifier.Verify` accepts approved OCI index `b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8`, amd64 manifest `edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b`, layers and native daprd; it rejects CLI bytes as native runtime. Version-only queries report CLI 1.18.0/runtime 1.18.1. Tooling DLL SHA-256 remains `885f0177f6c640cb8f76d7c1363067eb130d5dc8316e85c9c7ce6acaa58b9860`; OCI archive remains `ada1aa56a6c5e6a182bdba144de752e8c1f7b93dcd0822f32ed3f9d3e404fcda`; daprd remains `be0d0ee7362c6b8626538876fa2342d1c0789d126a60129a7bf27009a072769c`. Prepared home is the parent prerequisite root's `artifact-preparation/dapr-home`. `current-artifact-readback.json` and `current-native-archive-linkage.json` verify private regular-file custody, current CLI/Placement/Scheduler archive digests and exact extracted/native linkage. No Docker probe, service start or network retrieval occurs.

`inspection/cached-graphs-final.json` separately inventories 71 generated asset graphs. Its comparison starts **after** the first failed installed model build and finds zero hash changes through the subsequent diagnostic/evaluation interval; no complete earlier cache baseline is claimed. The original per-command snapshots show preserved tracked declaration/source bytes within the four named captured checkouts only; they do not establish execution-time EventStore source custody.

| Owner / source | Current consequence | Precise reopen condition |
| --- | --- | --- |
| Builds existing model-build/catalog-import seam and Platform/EventStore consuming inputs above | Exact full installed model cannot restore; passing focused graph, tool and native checks cannot supply usable operational readiness. | Authorize a bounded existing-seam source repair applying the declared catalog once. Preserve versions, source/package/default boundaries, error policy, Redis defaults and Production refusal. Rebuild/rebind any changed diagnostic tool through existing Builds workflow; pass actual installed `BuildModelAsync` and affected selected/default/Production checks against the actual direct candidate, then final source binding/reviews. Current no-source-edit scope prevents applying that repair here. |
| Platform identity record / source-mode assurance | HXW010 warning; no declared supported range or package/release acceptance. | Keep the warning disclosed for permitted source diagnostics. Any range declaration or stronger acceptance uses its source owner's authorized workflow; no inferred pin is written. |
| Spec 14 live hold; Memories fixed SQL and Platform readiness/result owners | SQL/C-VG3, capacity/headroom, startup probes, loaded runtime/backend identities, cohorts, elapsed retention, reminder progress, quiescence and cleanup unobserved. | After source prerequisites, final binding and parent reviews pass, prepare one fresh finite exact operational plan and obtain operational approval. Execute fixed read-only SQL against actual pinned PostgreSQL/Dapr-created schema before any cohort; retain actual results. |

Only this report and existing Spec 14 are repository deliverables. Original Spec frontmatter/status/baseline and all previous content remain verbatim; incomplete integration/full/live tasks stay unchecked. No workload, AppHost, container, service or runtime target is started/contacted, no prior budget or approval is reused, and no Git/sprint/A41/Production transition occurs. Parent owns three configured reviews and independent final verification of this new delta; their completion is not claimed in this implementation handoff.

### Parent review and independent verification

Parent independent verification is retained at `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/current-source-rebind-20261017-cjnssyes/parent-review-verification-zlkidljs`. All ten original raw-log hashes match their receipts. The newly hash-bound installed model prerequisite still exits **1**, in 5.273295 process seconds; its helper SHA-256 is `ad362dc13f6ae53dada301b5597f5e0132872044808b77c3aeac646396a0c9c1`, and all 275 captured helper/tool DLL/JSON artifacts remain unchanged. The new graph rerun passes **21/21**, zero failures/errors/skips/not-run, exit **0**, in 2.759802 process seconds (xUnit time 2.476 seconds); all 636 captured local test-output DLL/JSON artifacts remain unchanged. These are local output/artifact bindings; shared .NET runtime files are not hashed and no dependency rebuild is inferred.

Both new executions preserve tracked hashes across five checkouts, now including EventStore `b99a03dfc29021af16511ee664820e05126b73f8`. Explicit `GIT_OPTIONAL_LOCKS=0`, `DOTNET_ENVIRONMENT=Development` and `ASPNETCORE_ENVIRONMENT=Development` overrides are recorded separately from unrecorded inherited environment. Original missing custody is not retroactively asserted. Parent receipts retain exact argv, UTC bounds, process durations, log hashes, source/artifact maps and XML. Original document bytes, CRLF, the authorized two-document scope and whitespace pass independent readback. No unchanged direct functional suite or broader suite was rerun for prose corrections. The final preparation delta is reviewed and verified with its model-build blocker; Spec 14 remains in-progress and all live/full/A41/Production holds persist.

All three configured reviews completed; the five individually verified record corrections are triaged in the [existing Spec 14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#review-triage-log--current-source-binding). No review finding is deferred, and no source repair or usable operational approval is inferred.

## Approved boundary repair and publication — 2026-10-17

The owner replied “approve, then commit and push” to the reviewed one-method patch and its actual-candidate 79-case rerun. This supersedes the earlier no-source/no-Git restriction for this bounded test repair, its merge with current main, evidence updates and publication. Runtime/dependency/tool declarations, Platform gitlinks, operational actions, sprint/A41 and Production remain outside this approval. Keep the parent in-progress and historical/frozen content unchanged.

Applied exact approved patch SHA-256 `416164f7919b02f915d08807c904013d0bcda5ea24d652a8cdd44b10b765da1c` to the unchanged direct Memories candidate at baseline `f6b60d5a2e9b81d84fddad789c7d9bed40d3a533`. The actual full functional suite passes **79/79**, exit 0, zero failures/errors/skips, in 16.179 seconds. Command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_*.py' -v`, cwd `/home/administrator/projects/hexalith/platform/references/Hexalith.Memories`. `direct-functional-tests.json` binds UTC interval, command, source hashes and log SHA-256 `3e546fc9ae3e2f94d49d74bf808383b0f4dd302a2ae67dcc439a2fe55c3b9999`. This replaces the earlier unapplied-preview-only and 78/79 failure state for these exact repaired bytes.

The repair is committed as `db5cccc80e22686c7c840fdc53cc11f1d8844ff8`; commitlint passed before and after the commit, with tracked hooks executed. The verified test SHA-256 is `dddd76ae057b108a25f1087ad10d7c13ce690cfdbac1bf3bcb669946887d4503`; runtime helper/CLI bytes are unchanged. Private execution records are under the existing preparation root's `source-authorization-handoff-xpmbpt4t/approved-application-u2retp8u/`.

Current main already contains a newer historical observation and seven boundary cases. Its merge keeps that fixture/guard coverage and applies the shared exact refusal-status, reason and foreign-root assertions only inside the same method. An AST comparison confirms all other methods/imports remain unchanged. The source inventory is separately 79 direct cases and 91 existing main cases: six selected-sequence cases, five selected-consumer cases and one extra boundary case account for the difference; this task adds zero cases. The tested direct commit is preserved as a merge parent rather than rebased onto different candidate bytes.

Negative evidence is attached through `PlatformBoundaryTests.test_current_readiness_names_owning_contract_gaps_without_inputs`, `test_insufficient_existing_readiness_and_negative_variants_never_become_authority`, and `test_legacy_approved_plan_cannot_reach_vm_or_change_unused_budget`. These preserve Story 20.2 denial-before-dependency and Story 24.3 verifier/attribution boundaries. Their refusal observations earn no live interoperability, retention or qualification credit. The newer Platform source binding, operational approval and all full/live close-out work remain held.

Merged-checkout verification passes **91/91**, exit 0, zero failures/errors/skips, in 156.132 seconds, using the same full-suite command above from cwd `/home/administrator/projects/hexalith/memories`. `merged-functional-tests.json` binds primary parent `84f82ad95c9fcebec9f39dd9a1fb80bf77edcd8a`, direct repair parent `db5cccc80e22686c7c840fdc53cc11f1d8844ff8`, unchanged before/after helper/CLI/test hashes and log SHA-256 `53c3cb5ea8e07b489b37dfd4b6781029e73d6168e5bc1056304f878c2156b897`. The merged test hash is `54968a9bcebb1577cc88b05abf121dc2a4dce1284a0886edb100d1cbb05c76f5`. Both actual lanes retain zero test-count delta: direct 79 to 79 and current main 91 to 91. Additional named main denial evidence includes `SelectedConsumerTests.test_foreign_partial_malformed_source_and_short_lifetime_refuse_before_access` and `test_foreign_report_path_cannot_create_or_overwrite_an_output`; these are offline refusals, not live proof. Commit scope is exactly the existing Spec 14, existing report and the one boundary-test file; no runtime, dependency, sprint/A41 or gitlink content is changed by this repair.

## Source-authorization preparation — 2026-10-17

The directly declared Memories boundary regression is reproduced without editing candidate bytes. Primary HEAD is `84f82ad95c9fcebec9f39dd9a1fb80bf77edcd8a`; Platform, direct Builds and direct Memories retain the revisions recorded below. Private evidence is the existing prerequisite root's `source-authorization-handoff-xpmbpt4t/` directory. Its original `proposal.json` binds the sole proposed method, direct candidate HEAD `f6b60d5a2e9b81d84fddad789c7d9bed40d3a533`, original test SHA-256 `809d06a51e70b9582a0721533095102876e82203b6333dfbcdad4026e328245e` and proposed test SHA-256 `9398addb8c27357f4bacb8bb12c5cac9658a79f1713a76371360bb17521c694a`.

The original, now superseded `proposed-direct-boundary-test.patch` SHA-256 is `51f0f30549223e186b07b1aaecf302ca6bdaaafe4168d4be65e062ba43a2798f`. It changes only `PlatformBoundaryTests.test_current_readiness_names_owning_contract_gaps_without_inputs` in Platform's direct Memories boundary test: the six historical IDs and detailed assertions address `recordedBlockers`; the four current IDs are independently checked with owner/source/gap/reopen fields, the held reason and `prepared-source` label. Existing blocked/not-ready/no-authority/no-qualification/zero-spawn/zero-contact guards remain. No source/test patch is applied; no checkout or runtime source is copied.

Current final proposal: `final-proposed-direct-boundary-test.patch`, SHA-256 `416164f7919b02f915d08807c904013d0bcda5ea24d652a8cdd44b10b765da1c`; proposed test SHA-256 `dddd76ae057b108a25f1087ad10d7c13ce690cfdbac1bf3bcb669946887d4503`. `final-proposal.json` binds the unchanged baseline and both superseded proposals. This same sole method retains the original nonempty owner/source/gap/reopen checks, requires exact `prepared-source; actual startup and tool/artifact alignment unverified`, and compares current blockers/reason/status for ordinary and foreign-root calls. All historical assertions and no-effect guards remain. Intermediate substantive-prose proposal `3f7eacb8cd205d29a8536a44ae621823cb0f67e32ef8a89c0cde8a64d2e2c94e` and its preview are retained unchanged as superseded evidence; those prose assertions were rejected as unnecessary coupling to historical/unverified diagnostic text, including CS1705. No candidate source/test is edited.

| Exact check | Actual result and retained receipt |
| --- | --- |
| From Platform's direct Memories: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_platform_boundary.py -v` | Six cases: five pass, one failure, zero errors/skips; exit 1, 0.036 seconds. `baseline-boundary.json` and log SHA-256 `0ef3326a6700c4175937944878c49b4f9c218fca86341f54f7a99192365657bb`. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/source-authorization-handoff-xpmbpt4t/validate-proposed-test.py --preview-child` | **Original superseded proposed-test-only preview:** six cases pass, zero failures/errors/skips; exit 0, 0.057 seconds. The proposed test is compiled in memory with the original candidate test path and imports the unchanged actual direct helper/CLI. `preview-boundary.json` and log SHA-256 `5b7e90176df2dd0b58f495141f435ab50a3078e13ccc57d151a33c4fc8909cc0`. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/source-authorization-handoff-xpmbpt4t/validate-review-corrected-proposal.py --preview-child` | **Superseded intermediate unapplied preview:** six cases pass, zero failures/errors/skips; exit 0, 0.050 seconds. `review-corrected-preview.json`, log SHA-256 `79e752aa43ba97d53c839e0014f97434759cd4f740d4262dab506244eac045a9`. Actual direct helper/CLI are imported; proposed test compiles in memory only. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/source-authorization-handoff-xpmbpt4t/validate-final-proposal.py --preview-child` | **Current final unapplied preview:** six cases pass, zero failures/errors/skips; exit 0, 0.056 seconds. `final-proposed-preview.json`, log SHA-256 `a0de7db1605cb3df21b4e74647f4ceaa6d49a2871407ec02e602b81334a8bc5f`. Actual direct helper/CLI and candidate test hashes remain unchanged. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/source-authorization-handoff-xpmbpt4t/validate-review-corrected-proposal.py --discover-only` | Discovery only: 79 case IDs, zero executed tests, no discovery errors; exit 0. `direct-functional-discovery.json`, log SHA-256 `68455e1266821b716b0a5212f1d02499e8b110c9fea856a18219325885ede74e`. |

All receipts bind command/cwd/environment, UTC bounds and unchanged direct source hashes before/after. This preview grants no actual-candidate full-suite pass or live readiness. The existing 78/79 direct full-suite failure remains open; no broad build or already-passing prerequisite was repeated. The precise next source authorization is application of the current final exact one-method patch to its matching direct candidate, followed by that actual candidate's full functional suite and final source binding/reviews. Any source drift requires reinspection before application. No helper/runtime edit, dependency selection, gitlink update or operation is included. All full/live tasks stay unchecked, the stale operational window stays inactive, and SQL/retention/A41/Production remain held. Parent owns the three configured reviews of this continuation.

After source-owner authorization and application to the matching direct candidate, use cwd `/home/administrator/projects/hexalith/platform/references/Hexalith.Memories` and this exact environment/command:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 /usr/bin/python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_*.py' -v
```

Require the **79 discovery-bound cases**, exit 0, zero failures/errors/skips and no discovery/import errors. Zero/fewer/extra results do not satisfy this fixed source snapshot; any count/source drift requires reinspection. The full suite was not executed by this correction, and no actual candidate pass or source-application authority is inferred. Parent records the four individual review-triage decisions and performs final verification.

All three configured reviews are complete for frozen delta `7198356c1b03f75fbd5dc4075c762f944557e18c9271c5159a22c06130507bda`; individual findings and dispositions are in the current Spec 14 review table. Three direct corrections are reflected above; the low-value prose-mirroring expansion was rejected. Parent final proposed-test-only verification independently passed six cases in 0.041 seconds, with unchanged direct source hashes and a passing non-applying patch check (`parent-final-preview.json`). Original document bytes/frontmatter and held operational state remain preserved; this review does not authorize source application or claim an actual-candidate full-suite pass.

Final preservation audit separately detected concurrent Platform commit `b54dc5f2e8fe070db9a0ca3a72ad495b380d2b5e` replacing `b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b`, with 23 tracked raw-file hashes changed. The original strict audit refused this drift; the task did not make or revert those source/Git changes. Direct candidate and Builds source bytes remain unchanged. Existing Platform test/tool/runtime results remain bound to their earlier sources, so the newer Platform must be reinspected and rebound before an operational plan. `parent-final-source-audit.json` records all changed paths/hashes, the exact audit command and current ownership boundary; its strict mode exits 1 on this Platform drift. Preparation/document checks passing does not clear that source-binding hold.

The subsequent exact strict audit command `/usr/bin/python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/source-authorization-handoff-xpmbpt4t/parent-final-source-audit.py --require-original-platform` exits 1 and records 25 changed Platform paths: the initially observed 23 source/configuration paths plus the two concurrently updated Platform documents. Its independent documentation scope, original-byte/CRLF preservation, story-scope and whitespace checks pass. The broader Platform source binding remains held.

## Preparation prerequisite result — 2026-10-17

**Cache/tool/runtime preparation passes; operational readiness is held by a current direct-candidate regression.** No SQL, cohort, reminder, cleanup, runtime-target or full qualification result is inferred. The old 08:00Z window is inactive. No usable operational approval is requested until the source-owned blocker is repaired and reverified. This current addendum supersedes the dated three preparation-gap observations below; all historical report bytes are retained.

Evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr`. `receipts/*.json` bind exact argv/cwd, UTC bounds, exits, raw-log hashes, execution-time source HEADs and tracked hashes before/after. Every completed command has no tracked-file change during its measured run. Original parent preservation snapshots remain. Concurrent primary Memories commits are not this task's work. Recorded owners: Platform `b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b`, direct Builds `4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77`, direct Memories `f6b60d5a2e9b81d84fddad789c7d9bed40d3a533`.

### Dependency assets and actual graph

Both Platform composition/test assets already select current declared Dapr hosting `13.6.1-beta.935`, whose assembly is 13.6.1.0. The older mismatch is superseded, with no Platform restore/version edit attributed to this work. The unchanged official Builds tool-pack workflow restores seven of 15 project asset graphs; eight are already current. These are generated caches, separately inventoried in `inspection/current-source-and-cache-inventory.json` and `inspection/cached-graphs-after-official-build.json`, not dependency-declaration changes. All tracked declarations remain unchanged.

From `/home/administrator/projects/hexalith/platform`, owning composition and test builds use:

```bash
dotnet build src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj --no-restore -c Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj --no-restore -c Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

Both pass with zero warnings/errors. Receipts are `platform-composition-build.json` and `platform-test-build-refreshed.json`. The first 20-case run without explicit Development returned one expected profile refusal; `platform-retention-tests.json` retains it rather than hiding the failed attempt. `platform-retention-development.json` passes 20 cases with `DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development`. `platform-production-refusal-and-selected.json` separately passes the two exact methods `RetentionSelectedComposeRunAdoptsGraphCallbacksReadinessAndIsolatedControlPortsAsync` and `FileEntryRefusesNonDevelopmentBeforeComposing`; the latter explicitly sets Production and asserts refusal. XML names are in `logs/platform-selected-production.xml`. All are offline graph/event/fixture checks, never live readiness. Final named graph output is retained separately for parent readback.

### Supported private diagnostic tools

Builds' `Tools/README.md` permits local diagnostic packing at an observed published version; its published inventory names 4.31.2. The cached repository-signed Module archive SHA256 is `cc89eea1ae8ec9ded0dd304e94f7658082ae7614d339abb881de146b0471ffae`; Evidence is `1c0536f07ad2c78d127713d9b56244556a3654350c4652e771a250f87a969bf8`. Both pass `dotnet nuget verify --all`, with repository commit `9b44fedd0017d42ee1b3e887730ab667046bda4d` in their nuspecs. Repository signing changes the archive hashes relative to the unsigned historical inventory; no corruption or equivalence to newly packed bytes is inferred. Those published assemblies lack current selected-test/lifetime/observation/OCI contracts, as `published-tool-contract.json` proves. They are not the prepared runner.

The unchanged official command, from Platform's direct Builds checkout, is:

```bash
pwsh -NoProfile -File Tools/build-g4-tool-packages.ps1 -Version 4.31.2 -OutputDirectory /home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/tool-preparation/current-source-diagnostic-packages
```

`diagnostic-official-tool-pack.json`: exit 0 in 40.735 seconds; the official workflow restores and builds the 15-project `Hexalith.Builds.slnx` in Release with zero warnings/errors, then packs the two tools plus symbols and inventory. This Builds solution result is separate from the focused Platform Debug builds above with `BuildProjectReferences=false`; it gives no broad Platform build credit. This is the documented package workflow, not a claim of local Debug runtime or CI qualification. The inventory deliberately has source validation/controls not run and `releaseEligible:false`. No package-contracts execution branch, release verification, publication, dependency selection edit or Git mutation ran.

| Current-source diagnostic artifact | SHA256 |
| --- | --- |
| Module.Cli 4.31.2 nupkg | `ac9b9f69f2525bb45136722ed615f5e552cd706c6f5e9e6ff0e53f8b8200e03a` |
| Module symbols | `1f88855a9f922581c9f65c361a7331e8af75cf4d9a1d0a8694863e9164edd238` |
| Evidence.Cli 4.31.2 nupkg | `f3933a39213c7053e0407395f3871025e7a610063cec9a56717c8bfc804c1079` |
| Evidence symbols | `ff717d0d78ccebf977a091e836dc0e721ccf0f45f366ac8a79a2a34582dd44f4` |
| Installed diagnostic tooling DLL | `885f0177f6c640cb8f76d7c1363067eb130d5dc8316e85c9c7ce6acaa58b9860` |

The generated private manifest is `tool-preparation/diagnostic-consumer/.config/dotnet-tools.json`; its two pins are 4.31.2. Private `nuget.config` clears all feeds except `current-source-diagnostic-packages`. With `NUGET_PACKAGES=<root>/tool-preparation/diagnostic-nuget-cache`, `DOTNET_CLI_HOME=<root>/tool-preparation/diagnostic-cli-home`, and cwd `<root>/tool-preparation/diagnostic-consumer`, `dotnet tool restore --tool-manifest .config/dotnet-tools.json --configfile nuget.config` succeeds. First-use certificate generation is disabled for this diagnostic restore. No tracked Platform manifest or global installation is created. Future invocation must actually select this private manifest/installed binary; the earlier bare Platform-cwd command cannot resolve an absent workspace manifest.

`diagnostic-package-provenance.json` executes only the exact `Get-PackagePayloadEntries` function AST extracted from the existing publisher, never its publication body. Official inventory hashes/lengths and all 303 Module/34 Evidence installed `tools/` payload entries match. The installed command reports `4.31.2+4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77`. `diagnostic-tool-contract.json` is reflection-only inspection of type, property, field and API presence in the installed bytes. It provides no selected-execution, lifetime or outcome behavioral credit; actual installed-verifier execution is separately recorded by the parent below. These unsigned local diagnostic archives remain distinct from the published release and provide no publication/full qualification credit.

### Isolated approved Dapr artifacts

Actual `HEXALITH_DAPR_HOME` preparation path is `<root>/artifact-preparation/dapr-home`, with CLI at `tools/dapr`, native binaries at `.dapr/bin/` and retained archive `.dapr/runtime-image.oci.tar`. Effective private custody is enforced; no ambient tool is replaced. Official CLI release asset 444065780 identifies 1.18.0 archive SHA256 `2a94739e0aa101289d88418225319562bc6800db273b3d9cf819a0efd1ea1bfe`; matching cached archive bytes are copied/read back privately. Exact official v 1.18.1 Placement/Scheduler archives are retrieved and digest-verified; native hashes are in `artifact-preparation/prepared-control-binary-identities.json`, without launching either service.

The pinned GHCR index, approved amd64 manifest, image configuration and every referenced layer are fetched by digest and independently hash/size checked. Native daprd is extracted from that verified layer history. The retained OCI archive is 64,890,880 bytes, SHA-256 `ada1aa56a6c5e6a182bdba144de752e8c1f7b93dcd0822f32ed3f9d3e404fcda`; native daprd is 225,874,082 bytes, SHA-256 `be0d0ee7362c6b8626538876fa2342d1c0789d126a60129a7bf27009a072769c`. No tag/latest fallback occurs. `prepared-dapr-identities.json` retains fixed URLs/digests/lengths and all layer descriptors; no registry token is retained.

`existing-oci-verifier.json`: actual existing `CompositionRuntimeArtifactVerifier.Verify(archive, approvedIndex, approvedAmd64Manifest, nativeDaprd)` returns true; version-only queries report CLI 1.18.0/runtime 1.18.1 and daprd 1.18.1. The Debug verifier DLL hash is `994ede03ce15d2a01bc729ad3dc24d6d6cb11180320a353564d145536a130d82`; the parent separately invoked the installed diagnostic verifier bytes, with the readback recorded below. `CompositionPrerequisiteProbe.ProbeAsync` is deliberately uncalled: it queries Docker and launches Placement/Scheduler to inspect their startup version. Those real startup observations still require operational approval.

### Current source blocker and remaining holds

`inspection/current-source-and-cache-inventory.json` records all 17 Platform direct HEADs matching their staged gitlinks and no initialized nested submodule, using read-only Git only. No mapping resolver/init ran. Direct/primary CLI and manifest bytes match. Their helper hashes differ (`42e5998ed6e8e36f6d810a2571d760f5b3275df4452520fa3ff19480ebd8ff1b` direct; `4d7182f4f23966dc79fb65a5e5bf8096ea3f2d487147c46b2cf0dca11b759965` primary); `inspection/direct-candidate-ast-comparison.json` proves only historical `_recorded_platform_preparation_status` differs and the selected executable code AST is equal. The source roots remain separate, with no copy, substitution or gitlink update.

The current `platform_preparation_status` refusal inventory contains all four IDs: `platform-tool-source-alignment`, `platform-runtime-artifact`, `platform-startup-observations`, and `platform-real-functional-outcome`. Platform's `.config/dotnet-tools.json` remains absent; the verified private manifest/pin solves local preparation without creating that tracked manifest. Its static CS1705 broad-build claim is historical and unreverified in this continuation; neither a present full Platform pass nor a present broad-build blocker is inferred.

| Owning source | Observed blocker | Precise reopen condition |
| --- | --- | --- |
| Platform direct Memories `tests/tooling/access_telemetry_functional_retention/test_platform_boundary.py:57`, `PlatformBoundaryTests.test_current_readiness_names_owning_contract_gaps_without_inputs`; current consumer diagnostic | Actual direct suite 79 tests: 78 pass, 1 failure, no errors/skips. The assertion expects six old implementation-gap IDs; actual current refusal emits `platform-tool-source-alignment`, `platform-runtime-artifact`, `platform-startup-observations`, and `platform-real-functional-outcome`. `direct-candidate-functional-tests.json` retains exact failure and source hashes. Primary corresponding seven-case boundary lane passes in 0.040 seconds (`primary-boundary-tests.json`). The direct test inventory's smaller count alone is not a runtime mismatch. | Source owner authorizes/reconciles the stale direct candidate assertion/diagnostic against current intended refusal and reruns that actual candidate suite to zero failures/errors/skips, then rebinds final source bytes. Source/test edits are explicitly outside this prerequisite continuation; no permissive expectation or replacement checkout is supplied. |
| Spec 14/current no-live scope; runner readiness and Memories fixed SQL/actual sequence | Actual PostgreSQL SQL, capacity/headroom, startup probes, loaded service/runtime/backend identities, cohorts, elapsed retention, reminder progress, quiescence and cleanup remain unobserved. C-VG3 remains open. | After source-owned blockers, final source binding and all three configured parent reviews pass, prepare one fresh exact operational plan/window using the verified private artifacts. Obtain explicit operational approval, then execute the fixed read-only SQL against actual pinned PostgreSQL/Dapr-created schema before any cohort. No earlier stale window or approval is reused. |

All three configured reviews are complete, with all five documentation findings corrected; the independent installed-verifier readback below is complete. Full/live tasks remain unchecked; original Spec 14 baseline/in-progress status, history, Redis/default behavior, Production refusal, original retention/gate bounds, sprint/A41 and prior approval/budget bytes remain. Repository deliverables are only this existing report and Spec 14. Private command/artifact manifests are supporting evidence, not a new report or execution protocol.

Final implementation-agent verification: `platform-final-graph-named-tests.json` passes all 21 selected/default/Dapr/Production cases (zero failures/errors/skips), with exact names in `logs/platform-final-graph-tests.xml`. `primary-functional-tests.json` passes all 91 primary functional cases in 131.024 seconds, bound to primary HEAD `506b3a6c7f494e556a6c15c8b8c962482db6f590` both before and after, from `2026-10-17T06:47:55.614404Z` through `2026-10-17T06:50:06.854724Z`; this remains distinct from the direct candidate's retained 78/79 outcome. The primary boundary lane passes all seven cases. No live verification or source repair is inferred.

Parent independent installed-binary readback: `receipts/parent-installed-verifier.json` records exit 0, unchanged before/after source hashes in all four checkouts, and log SHA-256 `e8ef777df68221c289a582ac9ecc0fe5d6449d82c531f5a62923c90592cfa545`. A new PowerShell process loads the installed diagnostic tooling DLL `885f0177f6c640cb8f76d7c1363067eb130d5dc8316e85c9c7ce6acaa58b9860`; its existing verifier accepts the approved actual OCI/native linkage and rejects the CLI binary supplied as the wrong native runtime. Version-only queries confirm CLI 1.18.0/runtime 1.18.1. This checks the prepared installed bytes without starting a service.

Final parent verification: `inspection/parent-receipt-readback.json` checks all 21 command-receipt log hashes and unchanged execution-time tracked source snapshots, and confirms all 21 named Platform graph tests passed. `inspection/parent-final-preservation.json` records the final two-document scope check, `git diff --check`, original Spec frontmatter and historical suffix preservation, plus separately recorded concurrent Platform documentation changes. The initial strict whole-baseline check exited 1 when Platform Story 1.8 spec and deferred-work bytes changed externally; `inspection/concurrent-platform-documentation.json` retains those hashes and the exact failed check. They are preserved without attribution to this task; the revised check validates the observed concurrent hashes rather than overwriting them. The three reviews cover frozen diff `reviews/preparation-reviewed.diff`; `reviews/preparation-final.diff` retains the corrected continuation separately. Edge-case and verification-gap lenses found no issues; all five blind-review documentation corrections are resolved in Spec 14. No unchanged code suite was rerun for these prose corrections. The direct candidate regression remains open, so a fresh usable operational approval plan awaits its authorized source reconciliation and passing actual-candidate rerun; SQL, retention, full qualification and A41 remain held.

## Current integration preparation — 2026-10-16 continuation

**Source integration is prepared; live operation remains held.** This current section and existing Spec 14 supersede the dated gap observations below. No workload, service, container, AppHost or runtime target was started/contacted; no live retention, full qualification, A41 or Production credit is claimed. Spec 14 stays `in-progress`, its original baseline remains, and incomplete tasks stay unchecked. The original report body below is preserved byte-for-byte, including its dated headings.

Platform/Builds now select the existing fixed Memories source test and compose run-owned PostgreSQL 18.6 with verify-full TLS, scoped Dapr 1.18.1 control-plane/runtime expectations, resource/readiness bindings and observed-identity contracts. Redis/default selections remain. Actual readiness must observe mapped service assemblies, native daprd with approved OCI linkage, container image config ID/RepoDigest, run ownership, actual TLS/SQL backend identity and all three one-hour configurations. The mapped-file implementation uses ordinary held descriptors with matching device/inode and stability checks, avoiding privileged `map_files` opens. These observations are implemented, not collected live.

A strict canonical local profile binds current run/source, component, immutable runtime/backend, verify-full and 3,600 seconds with `qualificationCredit:false`. Local capability expiry equals the immutable selected execution deadline; invalid local intent cannot fall through to the full branch or run mutating probes after expiry. Production refusal and the full ten-behavior gate remain. Native execution renews the existing four-field gate every five minutes for at most ten minutes and never beyond the execution deadline, while its acceptance maximum remains fifteen minutes. Withdrawal closes the gate immediately; a closure fault preserves the first outcome and original stopping timestamp. The local purge/heartbeat correction and later actor/adapter write boundaries share that same gate. Renewal rejects expired, malformed, overflow, disabled and FIFO replacements through strict numeric validation and both nonblocking held-file reads; it cannot revive withdrawn authority.

The existing selected CLI uses the actual producer's string `source` mode and fixed no-argument invocation/native TRX contract. It validates bounded private runner inputs, fresh readiness, source/loaded bytes, run/profile/runtime/backend and sufficient remaining lifetime. Normal logger acknowledgements, strong Dapr reads, durable reminder/index progress and fixed read-only TCP/verify-full PostgreSQL observations drive the actual selected sequence. The controlled offline success test exercises those active methods; eleven negative subcases include younger-cohort change/loss, absent/early/late/changed TTL, non-TLS SQL, expiry, withdrawal, failed persistence and symlinked report custody. All canonical younger-record fields are compared.

The selected consumer executes its fixed read-only SQL before any cohort and requires no A rows. It captures every older record's actual full stored payload and PostgreSQL expiry at A+3,540 seconds, retains the A+3,590-second strong-presence checkpoint with at most sixteen concurrent existing GETs in key order, then requires authenticated backend time after those expiries and strong absence. Source tracing found the new query's original column incorrect: [Dapr1.18.1 selects components-contrib1.18.0](https://raw.githubusercontent.com/dapr/dapr/v1.18.1/go.mod), whose [PostgreSQL v2 implementation uses expires_at](https://raw.githubusercontent.com/dapr/components-contrib/v1.18.0/state/postgresql/v2/postgresql.go). The active query is corrected and a separate pinned-column assertion passes. This checks release-source compatibility only; actual SQL execution remains open as C-VG3. Historical full/VM SQL remains unchanged. It therefore reconciles cleanup that finished before the first post-expiry poll without inventing an actor receipt. The result claims **local TTL eligibility and strong absence only**; `actorDeletedRecordIds` remains empty. It earns no positive actor-deletion attribution, full physical reclamation or full campaign credit. Existing full validators and their stricter attribution requirements remain unchanged. Detailed bounded, secret-inspected observations are retained and hash-bound beside native TRX before workspace deletion. Exact credential values are also refused as JSON property names. Successful imported memories/functional-retention evidence requires its observation binding; reportless failed runs may retain only independently bound observation artifacts, with failure/count/outcome coherence. Every native pass/fail/abort report is subject to the existing secret refusal.

Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-integration-20261016T181813Z-m3wr263w`. Each `parent-*.json` receipt retains exact argv, cwd, UTC bounds, exit, log SHA256 and source hashes before/after. Logs are local test output, not runtime target evidence. Existing in-process TestServer fixtures print application/Qualification/Production startup messages; they do not start a live server or activate Production.

| Independent parent check | Result and receipt |
| --- | --- |
| Active functional Python suite, including SQL preflight/column, allowed SQL latency and bounded strong reads | 91 passed; `parent-final-python-functional.json`. Earlier87-case result remains in `parent-python-functional.json`. |
| Lifecycle Python suite | 85 passed; `parent-python-lifecycle.json`. |
| Authority/interchange Python suite | 642 passed; `parent-python-authority.json`. |
| Memories capability, hosted-context, immutable one-attempt proof and timeout cases | 53 passed; `parent-quiescence-capability-tests.json`. |
| Local purge/activation/withdrawal, actual actor/adapter mutation boundaries and lifecycle cases | 93 passed; `parent-final-memories-tests.json`. Earlier49-case result remains in `parent-quiescence-lifecycle-tests.json`. |
| Memories workload/gate, heartbeat and delivery cases | 62 passed; `parent-quiescence-server-tests.json`. |
| Builds selected-test, observation, gate, native OCI/report, evidence writer/import/serialization and regression classes | 120 passed; `parent-final-builds-tests.json`. Earlier106-case result remains in `parent-builds-focused-tests.json`. |
| Platform retention cases with the corrected filter | 11 total: 10 passed, 1 blocked/failing actual composition case; `parent-platform-retention-tests-correct-filter.json`. |
| Unprivileged real mapped-file/inert-process case | 1 passed; `parent-platform-mapped-artifact-tests.json`. |
| Final standard no-restore Debug owning Builds Tooling/AppHost/tests and Memories actor/adapter test compilation | All four passed with analyzers enabled, zero warnings/errors; `parent-final-builds-tooling.json`, `parent-final-builds-apphost.json`, `parent-final-builds-test-build.json`, `parent-final-memories-build.json`. Earlier capability/server build receipts remain `parent-quiescence-capability-build.json`, `parent-quiescence-server-build.json` (new26-path binding in `parent-p7-source-binding.json`). |
| Owning Platform composition/focused test compilation | Passed; composition zero warnings/errors; test project seven existing MSB3277 warnings; `parent-platform-composition-build.json`, `parent-platform-test-build.json`. |

The Python commands are `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_*.py' -v`, the corresponding `access_telemetry_lifecycle` command, and the corresponding `access_telemetry_c1_interchange` command, all from primary Memories. Exact selected .NET classes/build argv are in their receipts. Owning Platform/Builds builds use `--no-restore -c Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`; this gives no broad dependency-build credit. No dependency was restored, updated or installed by this work.

The required story slice scope check passes for all28 primary source/document paths (`parent-final-scope.json`). Narrow whitespace checks across all four owning checkouts and untracked-source trailing-space checks pass with the baseline's CRLF accepted (`parent-final-whitespace-crlf.json`): each Git command uses `-c core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol diff --check -- <owned paths>`. The initial plain Git check treated CR endings as trailing whitespace in Platform and Builds JSON/source files and failed; `parent-final-whitespace.json` retains that result. No actual trailing spaces/tabs were found, no line endings were normalized and no Git configuration was written.

The exact current failing graph command is `dotnet tests/Hexalith.Platform.Composition.Tests/bin/Debug/net10.0/Hexalith.Platform.Composition.Tests.dll -method '*Retention*'`, cwd `/home/administrator/projects/hexalith/platform`. `PlatformAppModelTests.RetentionSelectedComposeRunAdoptsGraphCallbacksReadinessAndIsolatedControlPortsAsync` fails loading `CommunityToolkit.Aspire.Hosting.Dapr, Version=13.6.1.0`; the other ten cases pass. Current composition assets select `13.6.1-beta.935`; test assets select `13.6.0-preview.1.261001-0243`. The graph test is implemented against actual `ComposeRun`, service environment callbacks, PostgreSQL dependency/health and selected control ports, but is not a passing execution. Owner must align the existing cached graph through an authorized dependency workflow, then rerun that method and affected default/Production graph checks.

Earlier failures remain retained: broad dependency compilation failed CS1705 on EventStore.ServiceDefaults JwtBearer10.0.13 versus Gateway10.0.12 (`platform-broad-build-original.log`); the earlier broad graph run had103 cases/45 failures/3 skipped on the Dapr load mismatch. An initial parent Builds test build failed the concurrent DeclarationClosureTests RCS1140 documentation check; external source repair preceded the passing standard retry. A mistaken combined `-method`/`-class` filter selected zero Platform tests and earns **zero verification credit**; the corrected command above supplies the actual result. Earlier analyzer-disabled correction builds and their failures are retained as intermediate receipts; the later standard parent builds supersede those limitations only for their captured snapshots. The earlier41 tenant/privacy and123 deployment checks remain dated preparation evidence, not newly repeated checks.

All three configured initial reviewers completed frozen diff SHA256 `f6de47d16acfabb474761eae163fefa93c3d5e92182d825b45dd62a2deccec2a`. Spec 14 records all26 original individual decisions plus parent P7/P8, before grouping. Fixes cover producer custody, mapped-file observation, TCP/IPv4 TLS, cancellation/causal outcomes, TTL reconciliation, detailed artifact retention, strict canonical profile binding, full younger payloads, active sequence verification, persistence/TRX custody, actual graph adoption test, renewal ticks, isolated control ports, allocated PostgreSQL port, actual backend digest, producer enum encoding and immutable capability/local intent. VG2 remains a blocked execution gap despite completed test source. P7/P8 bind local purge/heartbeat mutations to the same gate and restrict selected local capability proof to one immutable process attempt so failed retained resources can become quiescent. Already-admitted bounded operations may finish across closure; no new local probe retry is permitted. The correction is implemented and independently verified:53 capability,49 lifecycle and62 Server cases pass, with both affected standard no-restore builds at zero warnings/errors. The selected local first proof has a min(30 seconds, remaining lifetime) deadline; timeout is a retained failure, and actual Dapr probe code checks cancellation before subsequent SDK/cleanup operations. All three configured correction lenses then completed frozen delta SHA256 `4bbafcb3fc13411dbfa6a346f5b06ec2e4c3dbafd9382fd5b8d15302d2089138`. Spec14 separately records all sixteen reviewer findings plus parent P9 before shared-root grouping: fourteen patch decisions are corrected, two are rejected with evidence, and C-VG3 actual SQL execution remains held. Final parent checks pass91 functional Python,93 actor/adapter/lifecycle and120 Builds cases. They exercise closed-gate queued writes and later transaction boundaries, exact credential keys, observation custody/import, HXT008 missing-detail native execution, malformed/expired/disabled/FIFO renewal, SQL latency and ordered bounded reads. The final fourteen-path raw handoff is `followup2-handoff.json`; the final patch is `reviews/review-corrections-final.diff`, separate from both immutable reviewed deltas. Patch-route verification does not claim a third complete review round. Already-admitted SDK operations may finish across closure; later local mutations refuse. A failed one-shot capability attempt may leave finite probe keys in its retained failed backend as diagnostic state, with no cleanup credit or further attempt.

Concurrent repository commits/rebase, unrelated source changes and dependency-props edits were made by other work and preserved. This task made no Git mutation. `parent-repository-readback-before-final.json` records branches, heads, remotes, recent history and dirty state across all four checkouts. Source ownership is by changed hunk, not whole-file ownership. The642-case authority run observed seven concurrent Platform/Builds source changes; none touched its Python inputs. All final parent checks record unchanged before/after source snapshots across the4,162-path verification closure; the83 source paths are explicitly listed in `owned-files.txt`. No all-current-byte or broad-graph pass is claimed after later concurrent changes. `final-preservation-and-source.json` binds the final85 raw source/document snapshots, protected historical/frozen content, direct/primary selected-source alignment and read-only repository state. `reviews/current-integration.diff` is the cumulative reviewable delta; shared-file concurrent hunks remain explicitly unattributed. `parent-final-prerequisite-readback.json` confirms the same cached graph mismatch, missing local tool manifest and unset selected Dapr home without contacting a target.

Source-owned operational blockers are: **(1)** the actual selected graph dependency/cache alignment above; **(2)** absent Platform `.config/dotnet-tools.json` supported tool pin/provenance; **(3)** unset selected `HEXALITH_DAPR_HOME` and no verified actual native Dapr1.18.1/CLI1.18.0/retained OCI linkage. Reopen(2) through the supported tool owner's exact pin/binary workflow; reopen(3) with an existing isolated verified home and `.dapr/runtime-image.oci.tar` linking approved index, amd64 manifest, layers and native bytes through `CompositionRuntimeArtifactVerifier`. No live artifact absence is inferred merely from a filename search. After those prerequisites, the existing no-live instruction still requires one usable exact operational approval and actual startup/capacity/readiness/cohort/quiescence/cleanup evidence. C-VG3 is owned by `fixed_physical_bound_query` / `_PlatformRetentionConsumer.physical` and Platform's actual state.postgresql/v2 backend: reopen only by executing the fixed read-only query successfully against the pinned Dapr-created schema after those prerequisites and exact approval, before any cohort. Controlled SQL responses and source assertions cannot close it. The no-side-effect diagnostic keeps17:23 recorded blockers separate from present prerequisites and does not authenticate supplied source roots.

The [held operational plan in existing Spec14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) specifies the direct-source command, exact resource/image set, proposed2026-10-17 UTC window, 14,400/600/900-second limits, 125+125 records, real1,800/3,600-second timing, +900 grace, retry accounting, local failure retention/quiescence and truthful outcome custody. The plan includes the initial empty-A SQL preflight, A+3,540-second payload/TTL capture and A+3,590-second bounded strong-presence checkpoint. The proposed window grants no authority. An automatic later `down` cannot substitute for quiescence or reset the original stopping budget. No unusable approval is requested. Story/sprint/A41/Production/publication state remains unchanged.

## Current Platform preparation — approved 2026-10-16

The owner replied **“approve”** to the Platform ownership correction in the [existing proposal](../../planning-artifacts/sprint-change-proposal-2026-10-16.md). [Existing Spec 14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) governs. **Preparation is blocked by the existing Platform/Builds contract, and no functional run has executed.** Standing owner policy and `sole-owner/self-reviewed` persist; no further policy approval or premature operational approval is requested.

Platform owns infrastructure and lifecycle. Memories consumes its supported runner descriptor/result and owns retention assertions. The existing public `functional-retention` entry reports recorded implementation blockers with the 17:23:28 UTC inspection provenance, four recorded supported contracts and four remaining blockers, plus mandatory reinspection, rather than current-source readiness. It returns exit 1 before reading any caller plan/decision/status, creating a bundle, spawning a process or contacting a target. Both historical operational callables immediately refuse; the original VM implementation remains recognizable in place, and historical sequence tests require explicit offline doubles. No VM budget, approval, disk, credential or active permission is changed.

The one writer / two 125-record cohorts / at least 1,800 real seconds separation / 3,600-second retention / 900-second purge grace and positive per-record attribution remain required. Normal logger acknowledgements, strong reads, signed clock/nonce/profile/expiry validation, actual startup probes, PostgreSQL verify-full TLS and immutable Dapr 1.18.1 / PostgreSQL 18.6 identities remain. Local file-backed secrets, development UTC and Debug source evidence cannot qualify OpenBao/Kubernetes security, independent UTC, CI candidates, full C1/C0–C6, Production or A41. KVM `95aa…` is retained historical full-profile material, never the new local profile identity.

### Current runner contract reinspection — 2026-10-16T17:23:28Z

This is the current **offline preparation observation**, superseding the earlier 14:34 and 15:26 blocker observations below. It grants no execution authority. Memories began clean at `2a2daacaf39f08503a3f8bd93c665912a68d4abd`; selected standalone Platform is `40ede27ab6f6ccde5628b5ad644c968d155d757e`, its directly declared Builds is `a573a793e0d32db3304c3779ad062edaa51c6441`, and direct Memories is `2a2daacaf39f08503a3f8bd93c665912a68d4abd`. At inspection, the direct helper, CLI and declaration bytes matched this workspace's starting bytes. The old missing-helper/source-revision blocker is therefore superseded. Subsequent uncommitted changes here are not automatically that direct candidate; no loaded source/artifact observation or integration execution is claimed. This workspace's Platform gitlink remains `55bee680e6901a45946dd8bf1e4a4060952bd7ea`, unchanged.

Current Builds implements three previously missing portions on its existing selected-test path:

- The v2 declaration accepts module-owned tests with a fixed `python3` executor and `--profile` selection. `ModuleTestSelection` binds declaration/entry hashes; `NativeTestHandoff` includes plan, readiness and source-mapping paths. The selected Python child receives no CLI arguments; Memories has not enrolled a retention test in its declaration. The separate lifecycle-only HXR028 and legacy native-test default do not describe this new selected path.
- Selected tests carry an initial-request execution lifetime up to **14,400 seconds**, a separate stopping bound up to **900 seconds**, absolute deadlines and matching credential expiry. Setup/mapping/startup consume that same lifetime. No global timeout increase or startup change is needed; the approved experiment's four-hour/15-minute/600-second limits persist without granting an operational window.
- `ModuleTestRunRecord`, the command and engine record the first terminal outcome, owner withdrawal, retained local failures, CI/success cleanup and actual retained/removed/remaining/unknown disposition. Attachments are refused. The record's observed candidate/backend fields remain explicitly null and its qualification credit remains false.

The public diagnostic now separates these recorded supported portions and matching starting source from the **four remaining engineering blockers**. Builds `Runtime/` paths below are under `src/libraries/Hexalith.Builds.Tooling/` in Platform's direct Builds checkout. Platform C# filenames are under `src/Hexalith.Platform.Composition/`; other paths are owner-root relative.

| Remaining blocker / owner | Current source and consequence | Next implementation / reopen check |
| :--- | :--- | :--- |
| Observation and loaded identities — Builds with Platform | `Runtime/NativeTestHandoff.cs` still exposes only EventStore/UI endpoints; `CompositionReadiness` has no Memories workload/strong-Dapr/reminder/fixed read-only SQL observation binding. `CompositionEngine.RecordTestOutcomeAsync` creates `ModuleTestRunRecord` with `ObservedCandidateHash` and `ObservedBackendIdentity` null. Source/selection hashes and cleanup results do not establish actual loaded Memories/Dapr/PostgreSQL identity. `NativeTestExecutor` requires native TRX at `HEXALITH_G4_TEST_REPORT_PATH`, whereas the existing verifier emits JSON. | Supply actual measured identities and scoped fixed observation access through the existing plan/handoff/readiness/result, then consume and negatively validate that supported encoding. Produce the existing native TRX report and validate missing, malformed and failed reports through the existing runner report boundary. Reuse current lifetime/withdrawal/outcomes; no new report protocol, Memories substitute descriptor or fabricated success. |
| PostgreSQL profile — Platform | `PlatformDaprBindings.Type` resolves state to `state.redis`; `DaprComponents/reference/telemetry-state.yaml` is Redis. `ModuleTestSelection` selects an entry/lifetime, while `PlatformAppModel.ComposeRun` applies authentication configuration; neither selects/describes PostgreSQL/TLS/resources/runtime bindings. | Extend the existing Platform options/resource/Dapr seams together with existing runner plan/observations for immutable PostgreSQL18.6/Dapr1.18.1, verify-full TLS and run-owned resources. Other consumers retain Redis. A lone unused backend template cannot meet Spec14's select-and-describe condition. |
| Local capability/workload authorization — Platform with Memories | `Declarations/local-profile.json` keeps lifecycle disabled, capability/hash placeholders and **86,400-second retention in server, clock and telemetry**. Memories `AccessTelemetryQualificationGate.TryValidate` requires Qualification/KVM `95aa…` and has a **15-minute `MaximumGateLifetime`**. | A truthful Platform local profile must bind real startup/clock probes, verify consistent **3,600-second retention across all three services**, and authorize the normal fixed workload/acknowledgement and observations. Provide supported bounded run-owned gate renewal with fresh expiry/withdrawal checks while preserving the individual gate limit, absolute run lifetime and cohort clocks. No environment-name switch, copied KVM hash, new public endpoint or capability/Production bypass. |
| Tool and eventual consumer binding — Platform with Memories | `.config/dotnet-tools.json` remains absent. Starting direct source now matches, but both actual `module/hexalith.module-manifest.v2.json` files lack a test declaration and loaded artifacts remain unobserved. Selected Python receives no arguments; `tools/verify-access-telemetry-lifecycle.py` requires `--checkpoint`, while its helper has no main entry. | After backend/observation/local authorization support exists, adapt the fixed selected entry/checkpoint through the **existing verifier**, enroll that real consumer through the existing declaration, pin the supported tool via its owner workflow and bind exact direct candidate/loaded bytes. No new runner or accepting implementation is supplied by this diagnostic. No dependency update or sibling/ancestor/nested fallback is authorized here. |

An accepting consumer or PostgreSQL-only extension remains **blocked**, because entry selection alone cannot select and describe the required backend or provide its measurements. All independent stale-diagnostic corrections are complete; the existing end-to-end profile/consumer, contract-negative validation and operational-bundle tasks remain incomplete. No unusable operational approval is requested. No full gate, live retention or cleanup pass is claimed.

Re-runnable source checks: read `ModuleTestSelection.cs`, `ModuleTestExecutionBudget.cs`, `ModuleTestRunRecord.cs`, `NativeTestHandoff.cs`, `CompositionRunPlan.cs`, `CompositionReadiness.cs`, `NativeTestExecutor.cs:ExecuteSelectedAsync` and `CompositionEngine.cs:RecordTestOutcomeAsync` in `/home/administrator/projects/hexalith/platform/references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Runtime/`; `sed -n '993,1158p'` that root's `ModuleCommandExecutionService.cs`; read its `schemas/hexalith.module-manifest.v2.json`; inspect Platform `PlatformAppModel.ComposeRun`, `PlatformCompositionOptions`, `PlatformDaprBindings.Type` and named profiles. These are source inspections, not target observations.

Private source-bound evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-contract-refresh-20261016-pry0cqs1`. `contract-inspection.json` SHA-256 **`6b46089d0dcee22e802d3f10d3a5a46206c54b65319ddf75510efd34653277f6`** binds the actual direct/current owner declaration snapshots and `inspection.json` SHA-256 **`26e9101ee124304335307f2edaa867ab5631b5b24b0fc9315c97a28900b84a5f`**. The latter retains exact source bytes/hashes and repository revision/status commands; `preservation/before.json` records all 6,236 tracked regular/symlink entries before edits. Evidence files were exclusively created privately and read back; no old VM bundle, approval, budget or status was written.

The correction changes only the existing functional diagnostic, its boundary tests, current Spec14 and this one report. Seven focused Python boundary tests pass in **0.071 seconds**, exit0, zero failures/errors/skips, using `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_platform_boundary.py -v`. `boundary-tests.json` retains command, environment, log hash and identical before/after source hashes. The ambient selected-run handoff case is an **unconditional refusal test** with the supported report/endpoint/tenant/resource/domain and selected/source-mapping metadata, while retention observations remain absent. Its fixed initial request derives a finite 14,400-second deadline and the runner's next-whole-second credential expiry; it does not exercise expiry validation. Standard `builtins.open` and `io.open` join the no-read guards. Both stdout and stderr are captured and checked for supplied token/private-path disclosure. Existing old-plan/negative-object/history-import tests remain refusal evidence, never a wire-contract or live interoperability validator. The measurements and receipts in this section cover the pre-review bytes; post-correction verification is recorded separately by the parent.

The existing selected-test class exercises real inert Python children and stubbed composition/cleanup; it starts no AppHost, service, container or daemon. It proves runner boundaries only, not a PostgreSQL retention consumer. Source/assembly hashes match before/after; the build's inspected owning source hashes also remain unchanged. No broad-gate environment blocker occurred and unchanged historical suites were not rerun.

| Exact command | Measured offline result |
| :--- | :--- |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_platform_boundary.py -v` | **7 passed**, 0.071s; exit0, no failures/errors/skips. |
| From Platform's direct Builds: `dotnet build test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Debug --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` | Exit0, 139.168s; zero warnings/errors. |
| From that same Builds root: `dotnet test/Hexalith.Builds.Module.Tests/bin/Debug/net10.0/Hexalith.Builds.Module.Tests.dll -class Hexalith.Builds.ModuleTool.Tests.ModuleTestSelectionTests` | **31 passed**, 6.483s; exit0, zero failed/errors/skipped/not-run. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B tools/verify-access-telemetry-lifecycle.py --checkpoint functional-retention` | Expected exit1, blocked; zero target calls/spawns, no readiness or authority. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B tools/check-story-slice-scope.py --changed-files-file /home/administrator/.local/state/hexalith/memories/access-telemetry/platform-contract-refresh-20261016-pry0cqs1/changed-files.txt --story-key 27-4-retention-verification-operations-runbook-and-a41-close-out --require-record` | Exit0; one story checked. |
| `GIT_OPTIONAL_LOCKS=0 git diff --check` | Exit0. |

Retained receipts under the same private root: `boundary-tests.json` SHA-256 `8eb6d6c87eaf9f558751f92e455810f60359dce2d56156d8d225034e47bdc8b6`; `builds-build.json` SHA-256 `c38b86296989b3c5e72fb4525c5b3c0f710a71fb553d41678709ea91e69a550a`; `builds-selected-tests.json` SHA-256 `4555328b03161a64d24bf579a3f96340871b6ab7bd278b18a752e2b20491272c`; `command-checks.json` SHA-256 `60984001a9e2cce606e1f20cc08b169885bce87e5963aa8688c0566b4ccc4143`. Each retains exact argv/cwd/timing/exit and raw-log hashes; test receipts bind the tested source/assembly bytes. `final-verification.json` and the rerunnable `preservation/checker.py` retain final source/preservation/check results after these report notes.

Current check status: observed runner portions and diagnostic correction verified offline; accepting backend/consumer, actual descriptor-negative and outcome integration, resources/window/operational bundle and all live/full gates remain pending. Configured reviews belong to the parent preparation workflow; no new review pass or workflow transition is claimed in this implementation turn. No owner policy reapproval, workload/target/storage/credential/active-permission action, dependency or Git mutation, A41/sprint transition or Production change occurred.

#### Current review corrections and parent verification

All three configured preparation-delta reviews completed against the retained four-file snapshot (SHA-256 `ae76a6cd7a03a72b131f4d535c0a568ab3609e6e2da34c0313a2df4b722fc01c`). Edge-case and verification-gap reviewers returned no findings. Each of the eight blind findings was independently verified and corrected; the existing Spec14 records all eight dispositions. No finding is deferred and no parent review/done or Git transition is made.

The final handoff now names the existing verifier's fixed checkpoint/entry adaptation, native TRX production at the runner-provided path and missing/malformed/failed report refusal, verified 3600-second retention across all three services, and bounded run-owned renewal preserving the existing 15-minute gate lifetime, absolute deadline and cohort clocks. These are pending consumer requirements, not implementations or live passes. The handwritten ambient fixture includes the actual standard endpoints/tenant/resource/report/source-mapping envelope with finite execution and rounded credential deadlines. Both standard open functions and both output streams are guarded. The test remains explicitly unconditional refusal, never serialized descriptor, expiry, report or live validation.

The implementation's post-review boundary run passed seven tests in 0.077 seconds. The parent independently reran the exact boundary command above: **7 tests passed in 0.042 seconds**, exit 0, zero failures/errors/skips. Source hashes for the helper, boundary tests and unchanged CLI match before/after. The actual CLI again returned expected blocked exit 1 with four recorded supported contracts, four remaining blockers, no readiness/authority and zero target calls/spawns. The unchanged owning Builds source/assembly hashes and earlier 31-test/build receipts remain bound; no repeat build or broader historical test suite was needed.

Post-correction evidence is under `parent-final/` in the same private evidence root. `boundary-tests.json` SHA-256 is `b666c016e49f202b4deede7e89c4f9ec259b2c5c391f8d39077eccf92d46d515`, its raw log is `fe1d5b66bad20aef5057c5a7f354a307e1a933c66475f732e5d5939edafbc2c7`, and `cli.json` is `7c751b03070ab8b75207622840eb1e534d967ddcfd5654688161e75097f6e563`. `reviewed-preparation.diff` preserves the exact pre-correction review snapshot. Original `final-verification.json` remains the implementation's pre-review receipt; it is not assigned to later corrected bytes. `parent-final/checker.py` reuses the retained original baseline and checks the corrected test receipt; `parent-final/verification.json` records final source hashes, final diff, scope/whitespace and historical/frozen/unrelated-file preservation after these report notes.

Supported PostgreSQL profile/accepting consumer, actual descriptor-negative/outcome integration, operational bundle, live retention and full qualification/publication remain pending. Existing owner policy persists; no additional approval is requested for the blocked preparation. Story27.4 stays `in-progress`, A41 open and Production disabled.

### Source reinspection and next implementation — 2026-10-16T15:26:54Z

**Dated earlier observation, superseded by the current contract reinspection above.** Its source, results and review history remain unchanged.

This dated observation supersedes the earlier preparation snapshot below. It is not an execution binding. The source worktree began clean at Memories `f7c8ce84a8a765667beafc31154ee2d6981552f8`; standalone Platform was `b2890d20575356239555df5b113e3100c1d594c1`, its direct Builds was `63dda8fc14882aba383888afd7e6505afab1f345`, and its direct Memories remained `c285b84fddac7dda1efaf4435b8de18694282583`. Platform's independent uncommitted work was observed and left untouched. The direct Memories candidate still lacks the functional helper. This workspace's Platform gitlink remains `55bee680e6901a45946dd8bf1e4a4060952bd7ea`.

The existing preparation output now includes `implementationConstraint` and `nextImplementation` for each recorded blocker. The concrete next change is in Builds' existing v2 declaration/command path: establish supported module-owned test selection and invocation before removing HXR028/HXC002. Its shipped declaration has no executable test/profile member; legacy native tests run .NET test projects, not this Python verifier. Extend the existing plan/handoff/state/evidence and lifecycle together for loaded candidate/backend identities, observation access, finite ownership/withdrawal and actual retained/cleaned/remaining resources. The 15-minute native timeout and token lifetime cannot support a real one-hour observation; a startup-timeout override is not a test lifetime. These are coordinated existing-owner contract changes, not a consumer-only adapter.

Only after the runner can select and describe that supported test does Spec 14 permit the PostgreSQL options/binding/resource extension. Default Redis consumers must remain unchanged. A real local capability/clock/workload binding, supported tool pin and explicitly authorized direct candidate alignment must follow. Merely adding an unused PostgreSQL template, raising every native timeout, accepting caller-authored success or copying the KVM hash would not satisfy this boundary. No accepting path, new runner/schema, dependency update or operational bundle was introduced.

Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-reinspection-20261016-kyuru1r4`. `inspection.json` SHA-256 is `286d4907d2a8701c42b8cebfa2b664e28608b941708a21323038e623a36b9be6`; it retains exact inspected source snapshots, repository status/revisions and preservation baselines. `verification.json` SHA-256 is `ea134df068152dd34217eb78a91a22564e2aba6fd55a715190d2db8ad259563e`; tested source/assembly hashes match before and after. `builds-focused-build.json` SHA-256 is `25441d56280303e721008220c034e3f6fb6fed667b7a5d5ac045e222ad55e6b6`; it records the exact no-restore Debug build command, times, exit and raw build-log hash. The commands below are offline refusal checks, not live interoperability or retention evidence.

| Exact command | Measured result |
| :--- | :--- |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_platform_boundary.py -v` | 6 tests passed in 0.039s; exit 0. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B tools/verify-access-telemetry-lifecycle.py --checkpoint functional-retention` | Expected exit 1, blocked; zero target calls/spawns. |
| From Platform's direct Builds: `dotnet build test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Debug --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` | Exit 0 in 67.068s; zero warnings/errors. |
| From the same Builds root: `dotnet test/Hexalith.Builds.Module.Tests/bin/Debug/net10.0/Hexalith.Builds.Module.Tests.dll -method '*PlatformExecutionReviewTests.TestRefusalOccursBeforeStartupAsync' -method '*PlatformExecutionReviewTests.V2RunRejectsUndeclaredProfileBeforeStartupAsync'` | 2 tests passed in 0.636s; exit 0. |

No broad-gate environment failure occurred. Only the affected boundary lane and two owning refusal tests were run. Story 27.4 stays `in-progress`, supported execution remains blocked, and all live/full qualification/A41 obligations remain open. No further policy or unusable operational approval is requested.

#### Review corrections and final source-bound verification

All three configured reviewers completed the current four-file delta review. Edge-case and verification-gap reported no findings; the five blind-review findings were individually verified and corrected in the [existing Spec 14 triage log](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#review-triage-log--bounded-reinspection-delta). No review finding is deferred. Blocker IDs are checked for completeness/uniqueness; source pointers, timeout/token wording, the build-receipt link and the approved observation budget are corrected. Existing engineering prerequisites remain open.

The earlier `verification.json` above covers pre-review diagnostic bytes. After corrections, the parent reran the same six-test Python boundary command: **6 passed in 0.038s**, exit 0, zero failures/errors/skips. The actual CLI again returned expected blocked exit 1 with zero target calls/spawns. The required scope command `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B tools/check-story-slice-scope.py --changed-files-file /home/administrator/.local/state/hexalith/memories/access-telemetry/platform-reinspection-20261016-kyuru1r4/parent-final/changed-files.txt --story-key 27-4-retention-verification-operations-runbook-and-a41-close-out --require-record` passed for one story; `GIT_OPTIONAL_LOCKS=0 git diff --check` passed. The unchanged owning C# sources/assemblies and prior receipt hashes were independently matched; no repeated C# build or unrelated suite was needed.

Final verification receipt: `parent-final/verification.json` under the same private evidence root, SHA-256 `2c96a089aa38f7bfa0b77048acd5a22c4a4df9c3b1af01e808cda6837ef6d182`. It records exact commands/log hashes, corrected Python source hashes before/after, unchanged unrelated tracked Memories files and byte-identical historical bodies/frozen blocks. All four modified files are listed in the existing story. This correction supplies no live retention, interoperability, full qualification, A41 or Production credit; story status and all pending tasks remain unchanged. No commit was created because the approved preparation excludes Git mutations.

### Actual source binding and preservation

Read-only observation at **2026-10-16T14:34:55.614735Z**:

| Selected owning checkout | Observed full revision |
| :--- | :--- |
| This Memories worktree | `61074e981bd92019488db19da0c8572bd4bf8b97`, with existing uncommitted preparation preserved |
| Standalone Platform | `cafa3f0db528af66a03b542eb4f26fd1ef06433b` |
| Platform’s directly declared Builds | `63dda8fc14882aba383888afd7e6505afab1f345` |
| Platform’s directly declared Memories | `c285b84fddac7dda1efaf4435b8de18694282583` |
| This worktree’s Platform gitlink | `55bee680e6901a45946dd8bf1e4a4060952bd7ea`, not updated |

Platform/Builds advanced independently after the approval-time observations. Exact source/status observations and 14 selected owning source-file SHA-256 values are retained in `/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-preparation-20261016T145211Z-review-fixes-6gwly3kt/recorded-inspection/inspection.json`, SHA-256 `4e3476249245ac80f8725112ba4ce5c1251d24cffddfa9d2c221210a960f4943`. This is a preparation snapshot, never an executed-source claim. Recheck before future integration. Platform’s direct Memories lacks `tools/access_telemetry_functional_retention.py` and has the older CLI; this worktree has not run as that candidate. No sibling, ancestor, nested dependency or package fallback is used. No checkout/gitlink/dependency was changed by this implementation.

### Demonstrated blockers and reopen checks

**Historical 14:34 preparation inventory.** Use the four current blockers above; unconditional selected-test/lifetime/missing-helper claims below have been superseded. Original rows remain as observed history.

Builds paths below are relative to `src/libraries/Hexalith.Builds.Tooling/` in Platform’s direct Builds checkout. Platform C# filenames are under `src/Hexalith.Platform.Composition/`; component paths are Platform-root relative. The Memories workload gate is under `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/`. These are verified code gaps, not missing forms for the owner to fill.

| Blocker / owner | Actual source and consequence | Reopen check |
| :--- | :--- | :--- |
| V2 test profile — Builds | `Runtime/ModuleCommandExecutionService.cs:ExecutePlatformAsync` returns **HXR028** for v2 `Test`, before environment start. A declaration/readiness alone cannot run this test. | Existing runner selects and executes the module-owned test through a supported v2 profile. |
| Descriptor/result/lifecycle — Builds with Platform | `Runtime/NativeTestHandoff.cs` supplies EventStore/UI endpoints, tenant/resource/run IDs and a development token. `CompositionRunPlan`, `CompositionReadiness`, `CompositionRunState`, `RunEvidence/ModuleRunEvidence` do not encode the required loaded Memories/backend identities, finite test lifetime, attachment/withdrawal and outcome-aware retention/cleanup result. `CompositionRunStatus` has only Starting/Ready/Failed/Cancelled; existing failures tear down. | Extend and test these existing contracts under FR4/AD10; the consumer must validate their real supported encoding. No Memories replacement schema or fake accepting fixture. |
| Lifetime — Builds | `Runtime/NativeTestExecutor.cs:_timeout` is **15 minutes**, and token lifetime derives from it. A real one-hour retention observation cannot fit. | Add a bounded module-selected lifetime and matching credential handling in the existing runner; preserve unrelated tests’ limits. |
| PostgreSQL profile — Platform | `PlatformDaprBindings.cs:Type` forces state bindings to `state.redis`; `DaprComponents/reference/telemetry-state.yaml` is Redis. Existing options expose generic configuration/component directories, but the runner cannot select/describe a real PostgreSQL test profile. | Existing runner-selected composition describes run-owned PostgreSQL 18.6, verify-full TLS, immutable Dapr/backend identities, real resources/readiness and safe observations. Other consumers keep Redis defaults. |
| Local capability/workload binding — Platform and Memories | `Declarations/local-profile.json` disables lifecycle with placeholder hash/evidence; Memories’ `AccessTelemetryQualificationGate` requires `Qualification` and KVM `95aa…`. Setting Enabled or copying that hash would not establish a truthful local profile. | Supported local binding runs real capability probes and clock validation through the normal workload/acknowledgement path, with no public test endpoint, bypass or Production weakening. |
| Tool/source alignment — Platform and Memories | `.config/dotnet-tools.json` is absent; the direct candidate lacks the new helper and has older CLI bytes. | Pin a supported tool through the existing owner workflow, align the explicit direct candidate and bind actually loaded source/artifacts. No dependency update is authorized here. |

Verification commands for the gap observations: `sed -n '930,1048p' <Builds>/src/libraries/Hexalith.Builds.Tooling/Runtime/ModuleCommandExecutionService.cs`; `cat` the named handoff/plan/readiness/state/status/evidence classes; `rg -n 'FromMinutes\(15\)' <Builds>/src/libraries/Hexalith.Builds.Tooling/Runtime/NativeTestExecutor.cs`; `rg -n state.redis <Platform>/src/Hexalith.Platform.Composition/PlatformDaprBindings.cs`; `cat <Platform>/DaprComponents/reference/telemetry-state.yaml`; inspect the named local profile and workload gate. These inspections contact no service. Existing owning runner/composition tests were inspected; their presence is not a completed FR4 contract.

No Platform backend branch or Builds runner/schema is added: HXR028, missing evidence and unsupported lifetime make such a branch unusable here. The spec explicitly requires a precise blocker in that case. The next implementation action belongs in Builds’ **existing v2 test/profile and descriptor/result boundary**, coordinated with Platform’s supported PostgreSQL/local-capability profile. Recheck actual inputs, resources, actions/retries/window and source alignment only after that boundary exists, then prepare its one operational request. Do not invent a smaller RAM requirement or substitute VM/hosted/Redis infrastructure.

### Completion and evidence posture

- [x] Adopt the approved ownership correction in current story, epics, architecture/ADR, runbooks/handoff and interchange kernel; preserve four capability IDs and append-only memory.
- [x] Inspect and bind actual owning sources, proving the source/tool/runner/profile/lifetime/local gate gaps above.
- [x] Remove VM preparation/execution from the current public entry, preserve semantic validators and strict full consumers, and add refusal/preservation tests.
- [ ] Supported Platform PostgreSQL test/profile/descriptor/result consumer and complete operational bundle — blocked by the owning gaps above.
- [ ] Real A/B retention, resource use, current authority/withdrawal and Platform outcome/cleanup observations — unexecuted under the no-workload hold.
- [ ] Full C1/C0–C6/terminal/publication and A41 — unchanged, open.

The new refusal tests cover missing/foreign/stale/malformed/wrong-candidate/backend/lifetime/ownership/partial-startup/withdrawal/cleanup/claimed-success objects at this unsupported boundary. They prove no file/process/target side effects and no acceptance; they do **not** prove an implemented live descriptor validator, withdrawal mechanism or retention/cleanup policy. Historical VM tests and semantic fixtures likewise earn no live credit. Earlier offline commands/results follow below; the three completed reviews and their focused corrections are recorded in Spec 14’s current Review Triage Log.

### Earlier offline verification — not source-bound to corrected bytes

The earlier four-lane run reported **328 tests**, zero failures/errors/skips. Its receipt did not capture tested source hashes; it is retained as earlier offline evidence and is not attributed to the corrected current bytes. No workload, daemon, target or hosted service was started/contacted by these tests; retained local process/render doubles are offline evidence only.

| Exact command | Measured result |
| :--- | :--- |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_*.py -v` | 79 tests in 16.169 seconds; exit 0, zero failures/errors/skips. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_*.py -v` | 85 tests in 18.588 seconds; exit 0, zero failures/errors/skips. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/production_deployment_evidence -p *_test.py -v` | 123 tests in 237.036 seconds; exit 0, zero failures/errors/skips. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/tenant_isolation_evidence -p *_test.py -v` | 41 tests in 2.191 seconds; exit 0, zero failures/errors/skips. |

Raw logs, command vectors/environment, wall times, exits and SHA-256 values are retained at `/home/administrator/.local/state/hexalith/memories/access-telemetry/platform-preparation-20261016T145211Z-review-fixes-6gwly3kt/earlier-offline-tests/commands-results.json`, SHA-256 `65a564c2280f374019764d2cfa610375a1ee87f46f057ef36a4706c60a40a544`. After diagnostic source paths were clarified, the six new boundary tests were repeated with `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p test_platform_boundary.py -v`: six passed in 0.033 seconds, exit 0, no skips.

The earlier temporary preservation checker passed historical report/dated-plan/proposal/bootstrap body preservation, all original memory entries/four CAP IDs and protected A41/sprint/D0 hashes. `GIT_OPTIONAL_LOCKS=0 git diff --check` passed. No C# or owning Platform/Builds implementation changed, so no runtime build/test or live FR4 boundary is claimed. The three configured reviews are recorded in Spec 14’s current Review Triage Log; post-review functional verification is recorded separately below.

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

### Final preparation review disposition

All three configured reviews completed: edge-case and verification-gap reported no findings; blind review’s ten findings were individually triaged and corrected, together with the scope-record formatting failure. Parent inspection verified the corrections and matched the retained post-fix source/log/diff hashes. The scope and historical-preservation checks pass; no review finding is deferred. Full qualified-environment parity remains required. This completes the independent correction work only: the supported Platform test/profile/descriptor/result consumer, actual operational bundle and live observed behavior remain pending. Story 27.4 stays `in-progress`; A41 stays open. No workload/server contact, active permission/credential change, disk action or Git mutation was performed by this correction.

## Previous VM report and all dated receipts — retained verbatim

<details>
<summary>Historical VM-based preparation, decisions, attempts and earlier evidence</summary>

# Story 27.4 — consolidated retention results

## Current authority and evidence posture — 2026-10-16

The owner approved implementation of the [simplification correction](../../planning-artifacts/sprint-change-proposal-2026-10-16.md) with “yes”. [Existing Spec 14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) is the sole current execution plan. This is the sole human results report; raw packets, manifests and transcripts are supporting evidence.

| Field | Current value |
| :--- | :--- |
| Owner and assurance | Jérôme Piquot / `github:user:6775094`; `sole-owner/self-reviewed` |
| Base profile | `PG-ONPREM-2`, KVM qualification target, `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb` |
| Full workload hash | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |
| Functional scope | One writer; cohorts A/B, 125 records each; actual 3,600-second retention; B about 30 minutes after A |
| Functional limitations | Development UTC sources and source-built Debug images by digest; no independent UTC, CI/package/Production, full capacity, recovery, allocator or long-duration credit |
| Current authorization | Corrected manifest approved at 2026-10-16T13:10:04Z; execution held for native RAM admission, budget unused |
| Story / A41 / Production | Story 27.4 `in-progress`; A41/action open; repository Production overlay disabled |

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

## Current checkpoint matrix

| Checkpoint | State | Measured evidence or required next observation |
| :--- | :--- | :--- |
| Functional preparation | corrected bundle prepared and approved; resource hold | Fixed route/guest setup and source/image bindings implemented; 73 affected offline tests and 114 repeated lifecycle/deployment/privacy checks passed. Three earlier preparation reviews completed; 21 findings fixed. Measured OP1 correction verified with the actual read-only native command. Replacement bundle compiled/validated and approved; native RAM floor unsatisfied, budget unused. |
| Functional live retention | held before launch; budget unused | Actual guest identity, A/B acknowledgement and strong reads, real expiry/reminder/index progress, unchanged B and observed cleanup. |
| C0 | repository-validated | Existing offline exact-profile machinery only; live exact-profile execution pending. |
| C1 | operator-pending | 25 distinct eligible same-session technical gate passes and distinct owner role records. Checkpoint ownership is in Spec 14; more story files are not evidence. |
| C2 | operator-pending | Full two-writer throughput, acknowledgement and all required replacement/fault observations. |
| C3 | operator-pending | Actual 1/24/168-hour cohorts and attributable allocator reuse within the unchanged bounds. |
| C4 | operator-pending | Full failure/privacy/tenant/console/configured-OTLP continuity inventory. |
| C5 | operator-pending | Owner Operations acceptance of actual full evidence, risk and runbooks. |
| C6 | operator-pending | Distinct owner Security result record over the same full evidence; self-review disclosed. |
| Full terminal/publication | operator-pending | Authenticated full evidence, actual disabled/empty/zero state and separately authorized exact A41 publication/remote verification. |

The [recovery record](../27-4-bootstrap-recovery-result-2026-10-16.md) reports both disks allocated and QEMU stopped before an observed running guest because serial was passed to a raw backend. Both consumed attempts remain closed; the corrected boot-only proposal remains unapproved. These are bootstrap observations, not gate/Lease/replica cleanup or live retention evidence.

The current table supersedes the former profile and independent-review wording in the dated material below for the adopted owner mode. Original verdicts, failed captures, closed windows, source pins, review limitations and command receipts remain historical evidence; they cannot be relabelled into a current pass.

The fixed functional setup uses pinned K3s and real Dapr/PostgreSQL with private static Retain volumes and TLS/scoped file-backed OpenBao. Its declared PVC reservations are 400 + 20 + 10 + 60 + (3 × 17) = 541 GiB within the existing 600-GiB data disk; actual observed filesystem headroom is separately required at runtime. Static provisioning and this disposable OpenBao setup do not qualify OpenEBS, the approved OpenBao chart/platform, at-rest security or HA. Existing profile inputs are preserved. Missing or ambiguous deletion attribution rejects the functional result; positive expired physical tuples can corroborate TTL-filtered absence.

Local SDK Debug archive builds completed for Server, lifecycle and clock; source/build/output/image digests are retained in `/tmp/story-27-4-functional-source-build/source-image-build.json` and will be copied into the exact private bundle. Their qualification credit is false. The full C4 curl pin was not changed; the functional archive excludes that full-lane tool and cannot pass C4.

The affected suite passed 66 tests in 13.698 seconds after review fixes, with no skips. It is registered in the existing CI workflow; no CI run or live target pass is claimed. Source-image provenance now binds resolved dependency/shared build inputs as well as Memories. Previous image receipts/archives are preserved separately before refresh.

The final repeat verification passed 180 tests (66 functional, 85 lifecycle, 12 deployment, 17 tenant/privacy), with zero failures/skips. All 21 review findings are fixed; none is deferred. Raw logs and hash summary are in `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T124615Z/preparation-verification`. Actual source re-render/input/custody validation passed. A held invocation of the actual bundle refused with zero target spawns/calls and no status or budget change.

The derived operational manifest is `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/functional-preparation-20261016T124615Z/plan.json`, SHA-256 `fdbbd399e9cf25689f0c71c3e37a94cd9a8454187bf04fbd6a5a65d35c8510f4`; 36 prepared inputs, 393,338,669 bytes. At preparation time executable authority was false, decision absent and budget unused; that bundle was then approved, rejected and closed as recorded above. Its original proposed UTC admission started 2026-10-16T12:46:18Z, latest start 2026-10-18T08:31:17Z, expires 2026-10-18T12:46:17Z. The complete bounded boot/setup/one-hour test/restoration actions are listed in the existing Spec 14. No live retention, terminal API cleanup, full qualification or A41 completion is claimed.

The final local volume check added `fsGroup: 1000` and `fsGroupChangePolicy: OnRootMismatch` to the functional Redis/FalkorDB pods, preserving their seccomp/capability controls and requiring no privileged init. The affected 66-test suite passed again. The earlier inactive bundle `functional-preparation-20261016T123800Z` and its verification remain preserved; that regenerated bundle was subsequently approved and rejected before boot. The corrected native-read bundle above is now approved and held for resource admission. The earlier corrected boot-only draft remains unapproved historical evidence and is not reactivated.

## Historical report and dated receipts — preserved body

# Story 27.4 Retention Verification Evidence

## Evidence posture

This is the single canonical Story 27.4 C0-C6 status matrix. It records the
repository validation posture only; it is not a running-target evidence packet,
approval, or Production enablement decision. No target was queried while this
document was produced. Producer templates and fixture results are never represented
as executed Production evidence.

Production lifecycle writes remain disabled and
`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward/open until the complete
same-profile evidence, approval, terminal-validation, close-out, and remote-publish
chain passes. The [PG-ONPREM-2 adoption record](../../planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md)
and its [verification receipt](../../planning-artifacts/c1-security-prerequisites-2026-10-04/adoption-verification-evidence.json)
confirm repository bytes offline only; they provide no running-target gate pass or
independent approval. Historical PG-ONPREM-1 C1.15 capture grants no PG2 credit.

## Immutable decision identity

| Field | Value |
| :---- | :---- |
| Profile ID | `postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2` |
| Profile SHA-256 | `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
| Workload SHA-256 | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |
| Production lifecycle writes | `disabled` |
| A41 status | `carried-forward/open` |
| Repository mode | offline; no target query and no A41 mutation |

## Canonical C0-C6 matrix

| Checkpoint | State | Repository validation | Required external evidence | Owner | Blocker / operator action |
| :--------- | :---- | :-------------------- | :------------------------- | :---- | :------------------------ |
| C0 exact adapter profile | `repository-validated` | Exact `PG-ONPREM-2` profile, hash, workload, parser, and pre-query identity contracts have deterministic guards. This is not Production proof. | Immutable exact-profile qualification packet with executed commands and recomputed source/artifact identity. | Platform Operations | `operator-pending`: execute the authorized C0 producer on the declared target. |
| C1 canonical predecessor | `operator-pending` | The legacy verifier structurally checks each canonical `C1.1` through `C1.25` input and rejects a synthetic aggregate; it does not authenticate reviewer identity or establish accepted C1 evidence. | Twenty-five individually attributable passing gates with registered/done owners and eligible session, plus authenticated Operations and Security decisions bound to the exact manifest and profile hash. | Gate owners, Platform Operations, Security | Complete all 25 gates and the approved authenticated interchange. The required authenticated interchange must refuse missing owners, skips, zero-result commands and bare reviewer labels. The named-owner bundle exception below applies only after authenticated role, scope and producer-exclusion checks; the legacy guard remains stricter. |
| C2 production replacement | `operator-pending` | Producer schema, immutable packet writer, same-profile validator, concurrent fixed two-writer accounting, zero-default gate/Lease transition, and exact per-instance replacement selectors are repository-validated. | Controlled two-writer execution; replacement of both Servers and their sidecars, lifecycle/clock services and their sidecars, actor activation, all three Placement and Scheduler members; and adapter-fault execution with exact acknowledgements, recovery, and audit continuity. | Platform Operations | Run the reviewed C2 producer only after C1 passes and the separately scoped P7 runtime migration is approved and verified, with a named shared-system approval; its target-identity observation must prove an initially disabled exact-profile qualification namespace, empty Lease, and zero lifecycle/clock replicas. Zero acknowledged loss and a final disabled/empty/zero state are required. |
| C3 retention and reclamation | `operator-pending` | Cohort, 1/24/168-hour bounds, attestation negatives, interrupted-purge, newer-record, tuple-attribution, and logical/physical separation guards are repository-validated. | Executed expiry/purge and adapter reclamation commands bound to each of the three independent cohorts and its database/schema/table; newer records preserved and reusable allocator free-space increase observed within 86,400 seconds. | Lifecycle owner and adapter owner | Run the reviewed C3 producer after C1 and approved, verified separately scoped P7 runtime migration; an OS disk-shrink claim is prohibited. |
| C4 failure, privacy, and observability | `operator-pending` | Complete failure inventory, health precedence, NoData/last-evidence timestamp, bounded labels, and Story 20.2/24.3 denial guards are repository-validated. | Every declared dependency/fault lane, nonzero business samples with zero business failure, console/configured-OTLP continuity, alerts, and tenant denial before dependency access. | Platform Operations and Security | Run the reviewed C4 producer after C1 and approved, verified separately scoped P7 runtime migration; missing scenarios, raw/secret aliases, or dependency calls after denial reject. |
| C5 operations acceptance | `operator-pending` | Neutral and PostgreSQL-specific runbook structure, ownership, monitoring, RPO/RTO, rollback, rotation, and decommission contracts are repository-validated. | Named operations acceptance of the exact immutable profile, evidence set, capacity/cost, incident, restore, and maintenance procedures. | Platform Operations reviewer | Review actual C0-C4 packets and record an independent same-hash decision. |
| C6 security acceptance | `operator-pending` | Least-privilege, Dapr-only data plane, TLS/secret, bounded observability, evidence redaction, and tenant-isolation documentation guards are repository-validated. | Named security acceptance of the same profile and immutable evidence hashes, independent of the Platform Operations reviewer. | Security reviewer | Review actual packets and record a different named same-hash decision. |

Current C1 blockers, re-derived 2026-10-07 after the separate PG2 C1.15
preparation completed: accepted PG-ONPREM-2 C1.15 renewal with an authenticated
independent disposition, twenty-three registered/done gate owners, and executable
authenticated predecessor interchange. The C1.15/PG2 dispatcher now supports a
neutral v2 capture with a valid `QualificationSessionId` and the shared preflight;
repository preparation is complete. The earlier
`unsupported-successor-gate-or-historical-opt-in` refusal described historical
dispatch behavior before that preparation and is no longer a blocker for this
supported mode. A missing or invalid PG2 C1.15 session still fails with
`qualification-session-required-or-invalid` before target calls or output-directory
creation. The [capture/disposition contract](../../../docs/operations/access-telemetry-c1-pg2-c1-15-contract.md)
and separate preparation remain distinct from live capture, custody and accepted
renewal. Story 27.21's accepted PG1 capture provides no PG2 renewal credit.

C1.16 capture and linkage are accepted for the closed window. Story 27.22 is
done, and its independent disposition, SHA-256
`ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70`, accepts captured
component/backend identity and full-set connection linkage for one closed window.
Its canonical single-session refusal stays failed and owned by Deployment Adapter
Developer. That acceptance does not change the C1 row above and gives no Story
27.4, A41, Production or other-gate credit. It counts only when it is part of an
approved C1 predecessor bundle. A future bundle must establish the capture's
session eligibility and cannot reuse the closed scope as execution authority.
A bundle using a new session requires a separately authorized fresh C1.16 capture
and independent disposition; historical acceptance remains preserved.
Approved/done current-profile gate-owner registrations and twenty-five distinct
passed artifacts with two authenticated Platform Operations and Security bundle
decisions for the exact manifest/profile remain required. The owner-approved
exception permits Jérôme Piquot (`github:user:6775094`) to approve both C1 bundle
roles only through distinct decisions/receipts and authenticated role/scope/time
checks, excluding every capture producer. Other bundle reviewers must be distinct.
Reconsider the exception before Production activation or account/role/producer
changes. The existing label-only legacy predecessor still rejects a shared
reviewer, but accepts two distinct nonempty reviewer labels with claimed approval
fields as structurally valid. That structural verdict authenticates no decision
and cannot satisfy the required C1 acceptance. The prepared authority checks
supply no legacy bypass or accepting predecessor. Post-evidence C5/C6 still require separate reviewers.

The runtime profile mismatch identified on 2026-10-07 at
`95dd8758f2f36d262a2b5c23269a0d73a2d7825b` now has a separately scoped
[repository source/test correction](../spec-migrate-runtime-qualification-gate-to-pg-onprem-2.md).
`AccessTelemetryQualificationGate.ApprovedProfileSha256` pins current PG2
`7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`, matching
the canonical Python producer profile. Runtime tests independently author the
published PG2 and historical PG1 hashes rather than deriving inputs from the
runtime constant. They prove PG2 acceptance and PG1 rejection, Qualification-only
access, bounded expiry, fresh-file renewal and revocation. This source/test slice
is `repository-validated`; its dated receipt below is offline evidence only.

The [authenticated predecessor interchange specification](../../specs/spec-pg2-c1-authenticated-predecessor-interchange/SPEC.md)
has completed I1 structural readers, separately reviewed narrow GitHub
policy/session/gate/bundle observation preparation, the offline source-receipt
label/digest correction, offline I2 registry/source inspection, and offline
C1.15 declared-observation pin inspection. Those slices authenticate structure,
observations, isolated fixture bindings, or declared pins. They confer no
registration, deployed accepting entry, semantic gate verdict, or accepted
predecessor. Closed I2 registration and provenance, I3–I6 authority consumption,
gate semantics, assembly, consumer migration, and final verification remain
incomplete. Operational P1–P7 inputs/decisions remain unresolved; the full
interchange is not implementation-ready for live acceptance. See the
[authority preparation contract](../../../docs/operations/c1-github-authority-contract.md),
the [source-receipt correction](../spec-pg2-c1-15-source-receipt-labels.md),
the [producer-binding inspection contract](../../../docs/operations/c1-producer-binding-inspection-contract.md),
and the [observation-pin inspection contract](../../../docs/operations/c1-observation-pin-inspection-contract.md). Its
[P7 migration prerequisite](../../specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md#decisions-and-ownership-still-required)
requires a separately scoped runtime correction. The source/test correction above
satisfies that repository behavior check, while Architecture and Operations runtime
migration approval and authorized deployment remain pending before live C2-C4
execution. P7's remaining consumer inventory, compatibility/dispatch and
authenticated-interchange decisions remain unresolved. Its proposed decision route
assigns the handoff to Deployment Adapter Developer and the Story 27.4 machinery
owner; accountable people remain unassigned. These role labels and offline test
results confer no approval, completed ownership or running-target qualification.
The historical handoff and receipts below preserve the earlier mismatch as observed.

None of these live inputs has been supplied for this offline pass: an accepted
current-profile C1 predecessor bundle (external bundle path), approved/done gate
owners and two authenticated C1 bundle-role decisions under the named-owner
policy above, an authorized non-Production kube context
and namespace, an external evidence root and custody location, credential-file paths
(paths only, never credential values), and fault and purge authority.
Live C0/C2-C4 execution, independent post-evidence C5/C6 acceptance, terminal
validation, the exact four-path close-out, staged postflight, and authenticated
remote containment therefore remain pending. Story 27.4 remains incomplete, A41
and its sprint action remain open, and Production lifecycle writes stay disabled.

The only permitted states are `repository-validated`, `operator-pending`, `passed`,
and `rejected`. Only authentic external packets in state `passed` can satisfy C2-C6.
`repository-validated` means that the offline machinery is ready; it never advances
Production or A41.

## Close-out prerequisites

| Prerequisite | Current state | Required transition |
| :----------- | :------------ | :------------------ |
| Terminal validation | `operator-pending` | Authenticate the exact same-profile passing C0-C6 artifacts, including both independent post-evidence approvals. |
| A41 inventory and recoverable preflight snapshot | `operator-pending` | Run on a clean open commit; classify every A41 reference and approve exactly the four mutable paths. |
| Exact staged postflight | `operator-pending` | Stage only the approved semantic transitions; preserve Epic 20, Story 20.5, and `sprint-status.yaml` bytes. |
| Remote publish verification | `operator-pending` | Prove the exact close-out commit is contained by the authenticated intended remote branch. A local commit is insufficient. |

## Offline repository verification

Run from the repository root after the [README clone/setup step](../../../README.md#2-clone-and-initialize-submodules-2-min-cold).
The Debug/source-reference build requires those dependency checkouts. Only submodules
declared in the root `.gitmodules` may be initialized, without `--recursive`; this
verification block does not initialize or update any checkout.

These commands validate implementation structure and deterministic fixtures only.
The scoped Bash block stops on any command or pipeline failure and retains each
command, exit code, stdout/stderr log, result XML, source revision/worktree diff,
nonrecursive dependency revisions, and built assembly hash in a unique local
`/tmp` folder. Keep the printed folder for review. It is offline repository
verification output, not an external custody archive or live evidence authority.

```bash
(
    set -euo pipefail
    verification_dir=$(mktemp -d /tmp/story-27-4-offline.XXXXXXXX)
    trap 'verification_status=$?; printf "%s\n" "$verification_status" > "$verification_dir/exit-code"; printf "Offline verification folder: %s (exit %s)\n" "$verification_dir" "$verification_status"; exit "$verification_status"' EXIT

    run_logged() {
        local name=$1
        shift
        printf '%q ' "$@" > "$verification_dir/$name.command"
        printf '\n' >> "$verification_dir/$name.command"
        local command_status=0
        "$@" > "$verification_dir/$name.stdout.log" 2> "$verification_dir/$name.stderr.log" || command_status=$?
        printf '%s\n' "$command_status" > "$verification_dir/$name.exit-code"
        return "$command_status"
    }

    run_logged source-revision git rev-parse HEAD
    run_logged source-status git status --porcelain=v1 --untracked-files=all
    run_logged source-diff git diff --binary HEAD
    run_logged dependency-revisions git submodule status
    run_logged lifecycle env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v
    run_logged build dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true
    assembly=tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll
    run_logged assembly-sha256 sha256sum "$assembly"
    run_logged architecture env DiffEngine_Disabled=true dotnet exec "$assembly" -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo -failSkips -result-xml "$verification_dir/architecture.xml"
    run_logged architecture-results python3 - "$verification_dir/architecture.xml" <<'PY'
from collections import Counter
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

report = Path(sys.argv[1])
if not report.is_file():
    raise SystemExit("Missing architecture result XML")
expected = {
    "Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests": 12,
    "Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests": 5,
}
tests = ET.parse(report).getroot().findall(".//test")
counts = Counter(test.get("type") for test in tests)
if counts != Counter(expected):
    raise SystemExit(f"Wrong architecture per-class counts: {dict(counts)}; expected {expected}")
if any(test.get("result") != "Pass" for test in tests):
    raise SystemExit("Architecture results contain a non-Pass test")
print("12 retention-decision and 5 A41 guards passed; every result is Pass.")
PY
    run_logged whitespace git diff --check
)
```

Initial offline execution from the repository root on 2026-10-06 produced the
streamed results below; no retained command/source receipt was created for that
initial run. Use the block above to retain a fresh receipt. These historical counts
do not establish verification of a later worktree.

| Check | Result |
| :---- | :----- |
| Lifecycle tooling | 79 passed; zero failures, errors, or skips. Includes the existing scenario, predecessor-denial, disabled-state cleanup, and complete close-out fixtures. |
| Debug/source-reference Server test build | Zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision guards plus 5 A41 guards: 17 passed; zero errors, failures, skips, or not-run tests. |
| Whitespace | `git diff --check` passed. |

Final parent execution of the reviewed block on 2026-10-06 also passed: 79
lifecycle tests, exact 12/5 architecture counts with every result `Pass`, a Debug
build with zero warnings/errors, and clean whitespace. All ten logged commands
and the scoped block exited zero. The retained local receipt is
`/tmp/story-27-4-offline.4PZnqjZw`; it contains the executed commands, exit codes,
stdout/stderr, architecture XML, full source revision and execution-time diff,
nonrecursive dependency revisions, and assembly SHA-256. Its revision and assembly
hash were independently checked. Subsequent changes only record these results and
the review disposition; the receipt is offline validation, not live gate credit.

The receipt's identifiers, re-read on 2026-10-06, so the record does not depend on
the ephemeral `/tmp` folder:

| Item | Identifier |
| :--- | :--------- |
| Source revision | `b7a4377aaebce4b2d5f9e66d583ce9022f2f9fca`, with this file and the Story 27.4 spec modified in the worktree |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `e5678e0479bdb0ed4bf718a66be8d6f3afda84be599c42848582579852f011dd` |
| Built assembly | SHA-256 `c14d72222e20f4964e9fe928189f143ae2328cda3ac1a558889815cc54932700` (build identity only) |
| `architecture.xml` | SHA-256 `f86ffcc3b3b64b4e3ab038b9c6e2b9bc1372a640a9722f38e8bbea67d46a8e3c`; 17 total = 12 + 5, 17 passed, 0 failed, skipped or not run; run 2026-10-06 08:20:57 UTC |
| `lifecycle.stderr.log` | SHA-256 `e8b46a4515ea4cd3d58c1c994c6582a923b9fc907df8180276abe8d5a2c396ff`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `d067f4d063840a2d88f437e4b5fbd8dc79655a2f883b8d74d4cdb6a337a0b417`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

The folder also holds two files that the documented block did not produce. They
were added after it finished and had not been disclosed before:

- `final-a41.xml`, SHA-256 `c01b4a152f2066d6cb88e8b8ddd57cf20a8048c446587c800ada8baf5fb08a42`:
  a separate `AccessTelemetryA41CloseOutTests`-only rerun at 2026-10-06 08:22:40 UTC
  after the results were recorded; 5 total, 5 passed, 0 failed, skipped or not run.
- `final-worktree.diff`, SHA-256 `51bce7acc36bf9e3d8fd09b79d581ff8e49c405ec72b54ddf9bed3392510b570`:
  the worktree diff after the results were recorded, byte-identical to the reviewed
  `b7a4377a..95a38fd8` change.

Neither file is part of the ten-command receipt, and neither grants live credit.

Post-patch execution of the unchanged block on 2026-10-06, after the second-pass
review patches were applied, also passed. Receipt
`/tmp/story-27-4-offline.al6C55hg` (local, not custody):

| Item | Identifier |
| :--- | :--------- |
| Source revision | `6f3c727af1c9733fffb182b342f471f19b213b39`, with the review-patched Story 27.4 spec, this file and `deferred-work.md` modified in the worktree |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `f54084f65feab5cd2a8297d6f04d6af331a5b9e749e4ec68f0ee85d30f66d3b3` |
| Built assembly | SHA-256 `5f537e2be4b18f4c3e5c5054604d2d76ce112018f1813f4fd37104f29096f0b4` (build identity only; dependency gitlinks moved since the first receipt) |
| `architecture.xml` | SHA-256 `240fd6244aa6982782057f25e685cf6ae58c57b2b0db0c133de83420f707c122`; 17 total = 12 + 5, 17 passed, 0 failed, skipped or not run; run 2026-10-06 19:09:10 UTC |
| `lifecycle.stderr.log` | SHA-256 `d52bebbfa1cb5797f8414be1c6e4b938323441570fc3b231ee64d9cf6bcf71d8`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `fdcdd141083bb83633d7fd206511e1ee440c003a2c4c866f6f49ff3bce1e445f`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

A fail-fast check ran the same block text with only the lifecycle command replaced
by `false` and a separate folder prefix. Receipt
`/tmp/story-27-4-failfast.njdCm7qY`: the source and dependency commands exited 0,
the substituted lifecycle command exited 1, and the block exited 1. The folder
has no build, assembly, architecture, validator or whitespace file, so nothing ran
after the failure. It also holds `failfast-block.bash`, SHA-256
`69a51b4c9d1811b4c8e33a7ed5758dbe96ef4844564b1ddb8c03a431f597a7cb`, the exact
modified block, copied in after the run.

The post-patch execution-time diff was captured before these identifiers were
recorded, so it shows placeholders where they now appear. Recording them is the
only later change to this file. Both receipts are offline validation, not live
gate credit.

### 2026-10-07 offline resumption verification

The unchanged canonical block above passed again from the repository root before
these dated receipt notes were recorded. Retained local receipt:
`/tmp/story-27-4-offline.sSe9i00R`; all ten logged commands and the scoped block
exited `0`. Its logs confirm `Ran 79 tests` and bare `OK`, zero build warnings
and errors, and exactly 12 retention-decision plus 5 A41 results, every result
`Pass`, with zero failures, errors, skips or not-run tests.

| Item | Identifier |
| :--- | :--------- |
| Source revision | `5ccacbe5b017b1967dbd9e7b6a253cbab9d8b278`; the only execution-time worktree change was the existing, user-owned `references/Hexalith.Builds` gitlink at `3107b18de0b740fe77e274d06c79b47d52dd7b83`, preserved unchanged |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `c5af86641ddeb8afec282d230a3c15e634a69dd6affb81149dc51a72c1f37d73` |
| Built assembly | SHA-256 `226e529497658c3514e402cdb21390057fb07f753808d2d2445bbd9153846594` (build identity only) |
| `architecture.xml` | SHA-256 `509b8bf874c667613fde4ac3c7aad613abd99a34f6a44da283638f1aee1d5a52`; 17 total = 12 + 5, every result `Pass`; run 2026-10-07 04:50:48 UTC |
| `lifecycle.stderr.log` | SHA-256 `99e944ffa2818df20b6fffd985a88eebde5130c9f223f58501edaad8960b5366`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `bed98c98aa3604a100b5a6157f8c148ee49f03002d166fe0244b4e66a334df73`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

Current prerequisites remain absent: separately approved PG2 C1.15 renewal,
twenty-three registered/done gate owners, the accepted 25-artifact same-PG2 C1
bundle and independent Platform Operations/Security approvals, authorized
non-Production context/namespace, external evidence custody, credential-file
paths, and fault/purge authority. Story 27.22's accepted C1.16 capture remains
accepted for its captured window only. Tasks 3-4, C0-C6 live qualification,
terminal/postflight/remote verification and the exact four-path close-out remain
pending; Story 27.4 stays in progress, A41 and its sprint action stay open, and
Production writes stay disabled. This run contacted no live target and performed
no staging, commit or publication. No source, test or verification command changed.

The fixture close-out chain uses temporary repositories and mocked targets; it
grants no live checkpoint, approval, A41 transition, or publication credit. No new
tests were added for this command correction. The C0-C6 matrix and all close-out
prerequisites above retain their recorded states.

The Production producers, close-out preflight/postflight, and publish verifier are
documented in
[Access Telemetry Lifecycle Operations](../../../docs/operations/access-telemetry-lifecycle.md).

### 2026-10-07 PG2 C1.15 repository producer preparation

The separate [PG2 C1.15 preparation spec](../spec-pg2-c1-15-runtime-control-plane-renewal.md)
now has an offline-testable v2 runtime/control-plane collector, a shared sixteen-input
preflight and a [capture/independent-disposition contract](../../../docs/operations/access-telemetry-c1-pg2-c1-15-contract.md).
Successful fixtures bind the exact profile/workload/session/target, full source
HEAD, all three producer/helper sources and sixteen approved input hashes,
effective invocation, actual Kubernetes and Git command receipts and final source
recheck. Dirty development captures stay labelled; all packets remain neutral.
Live capture, external archive custody and independently authenticated acceptance
remain pending. Production is disabled and the C0-C6 matrix, Story 27.21/27.22,
Story 27.4 and A41 states remain unchanged.

| Offline check | Receipt and result |
| :--- | :--- |
| Review-patched PG2 C1.15 lane | `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'pg2_runtime_control_plane_identity_test.py' -v`; 22 cases passed in 547.824s, zero failures/errors/skips. Log `/tmp/pg2-c1-15-review-patch-focused.log`, SHA-256 `3edf582c882aaf8eed3dbe64532d8709b5759ab4f33a82b8bde6ecee4f82a9ac`. Covers exact profile/workload/target and unique 19-source bindings; parsed-source/helper load and final-query races; late dirty refresh; all sixteen input refusals; index/child/bare and mixed/reordered captures; harmless metadata changes; embedded/direct/decoded credential safety and selected secret references; scalar/array/UTF-8/malformed output; absolute LF/CRLF identity anchors; token/identity drift; Git/Kubernetes size/deadline refusals; immutable neutral output and PG1 v1 preservation. |
| Required-session legacy guard | The updated C1.16 mode-denial method passed separately: 1 test, zero failures/errors/skips; `/tmp/pg2-c1-15-rederived-session-denial.log`, SHA-256 `8daa346844e6cc7915155759de96e8f768224efa26f9359c8c4f66d27a440034`. Missing PG2 C1.15 session is explicitly refused before calls/output. |
| Earlier canonical lifecycle/architecture boundary block | The unchanged block above passed with receipt `/tmp/story-27-4-offline.7ETQL5cv`; all ten commands and the block exited 0. Lifecycle: 79 passed. Architecture: exactly 12 retention-decision + 5 A41 guards, all `Pass`, zero failures/errors/skips/not-run. Debug source-reference build: zero warnings/errors. This historical receipt predates source-snapshot/Git transport correction; the final-source receipt below supersedes it for current verification. |
| Earlier canonical execution source | Full HEAD `cc754ab1487ddcec40340f9afa370aa3114851f1` with implementation changes in the worktree. Its execution-time tracked diff hash is `aa5fc14da52419b4e389708561b9c7b679d1f07d063c79e5aa9fd20c350229e9`; untracked paths are listed in the retained status receipt. This boundary receipt predates the source-snapshot/Git transport correction; the corrected PG2 lane above validates the re-derived producer paths. |
| Final-source canonical lifecycle/architecture boundary block | The unchanged block above passed with receipt `/tmp/story-27-4-offline.jZZMVwe1`; all ten commands and the block exited 0. Lifecycle: 79 passed. Architecture: exactly 12 retention-decision + 5 A41 guards, all Pass, zero failures/errors/skips/not-run. Debug source-reference build: zero warnings/errors. XML SHA-256 `945fab28cbdd337e292f17bc57c5a3305997b54d968057d01624634a3f2d7fa9`; execution-time tracked diff SHA-256 `85415f9b95d6d974d8aaaf6704b8cbc550ea331dbdba987632bcff6c8d623adc`. Dated receipt metadata was recorded afterward; these are offline checks only. |
| Boundary refresh after external workspace commit | A separate `/pushall` operation committed the preparation and updated the Platform gitlink in `796710937e3dd85e06ed83056b9b36ee06ff96c3`; this build session performed no commit, push or dependency update. Collector/test bytes and all 72 protected files stayed unchanged. Receipt `/tmp/story-27-4-offline.sEO6mFSP` passed at that full HEAD and Platform `9cb2dc13f8e993293810ccbd984772a1e2feb019`: all ten commands/block exit 0, 79 lifecycle tests and exactly 12 + 5 architecture guards passed, zero failures/errors/skips/not-run; build zero warnings/errors. XML SHA-256 `58435d7e0547676b4e7cd75e61dee6ec5ba0632226d77441b7773ac2d3c1d1c8`. The execution-time tracked diff was empty; these dated receipt notes were recorded afterward. |
| Complete C1 regression | `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v`; 108 unittest cases = the existing 86 + 22 PG2 cases passed in 1957.806s, zero failures/errors/skips, exit 0 and bare OK. Parent-owned receipt `/tmp/pg2-c1-15-reviewed-verification-lo9pmav6/c1.stdout-stderr.log`, SHA-256 `4f59d7744389b2f6526867a93c8077d819e77358c1ed3c42b95cd0828792d5ca`. Earlier incomplete/stopped runs grant no full-suite verification. Three review layers completed; confirmed fixes verified, uncommon low findings rejected with evidence in the spec, no deferrals. |

These `/tmp` receipts are local repository verification, not live evidence or a
custody archive. The implementation makes no reviewer-authentication, C1.17,
aggregate C1, successor-registration or activation claim. No deployment input,
historical capture or independent disposition was changed.

### 2026-10-07 Story 27.4 current handoff reconciliation

The current handoff now distinguishes the callable neutral PG2 C1.15 v2 producer
from its missing live capture, custody and authenticated independent acceptance.
The unsupported-dispatch statement above has been reconciled with the completed
separate preparation; its historical refusal and the accepted PG1 packet remain
historical only. No producer, test, profile, registration or verification command
changed in this Story 27.4 slice.

The unchanged canonical ten-command Bash block passed after that correction and
before these dated receipt notes were appended. Retained local receipt:
`/tmp/story-27-4-offline.tCpEtJN6`. All ten commands and the scoped block exited
`0`; 79 lifecycle cases passed in 47.979 seconds with bare `OK`, zero failures,
errors or skips. The Debug/source-reference build had zero warnings/errors.
Architecture XML contains exactly 12 retention-decision and 5 A41 guards, all
`Pass`, with zero failures, errors, skips or not-run tests. Whitespace passed.

| Item | Identifier |
| :--- | :--------- |
| Source revision | `b350c094ab4bc10bd2daf723f9e2c90ffdddae2a`; execution-time changes were the existing Story 27.4 spec and this handoff file; no dependency revision changed |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `cbb03717ce9903db2f41d91eb5dc08e614aeab0c824346dc940ee46560d6de6c` |
| Built assembly | SHA-256 `54975db810c985922775a6392a113bed4dba80e7ab2f2e2b16b0575550dc48ad` (build identity only) |
| `architecture.xml` | SHA-256 `04a6ed02217099de59dd52eeb896de136b890804aba5ba84c36a1ed292090c2c`; run 2026-10-07 07:42:22 UTC, 17 total = 12 + 5, every result `Pass` |
| `lifecycle.stderr.log` | SHA-256 `16243498c2a1d4185286a74a098ac66ce2ec699583e059ce04972e3fb1298970`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `0cc511f4b843f173010d258b9444b753316bc1ce80a20e87fbc7f15c668f8c63`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

A separate initial-handoff post-edit recheck passed in
`/tmp/story-27-4-handoff.o88ERlot` at 2026-10-07 07:43:56 UTC. Its exact four logged
commands were `assembly-sha256`, `architecture`, `architecture-results` and
`whitespace`; each command and the scoped block exited `0`. Both architecture
classes passed with exactly 12 retention-decision + 5 A41 guards, every result
`Pass`, zero failures, errors, skips or not-run tests. Its `architecture.xml`
SHA-256 is `4fd0c2ec49cb87d097c445a6c9416e73fbd784290b5cfe6d76a0d0409eeff657`.
This four-command receipt records the initial handoff's post-edit checks separately
from the earlier ten-command `/tmp/story-27-4-offline.tCpEtJN6` receipt; it predates
these review clarifications.

The existing offline cases exercise structural/fixture behavior relevant to the
scenario rows below; every named method reports `ok` in the ten-command receipt's
lifecycle log. Temporary repositories and mocked targets confer no live credit.
Authenticated refusal of an unaccepted prerequisite before target access awaits
executable authenticated interchange. This run's no-target outcome was a deliberate
hold on live execution, not proof of that automated enforcement.

| Scenario | Existing passing fixture methods | Structural/fixture boundary |
| :--- | :--- | :--- |
| Denial | `test_current_target_predecessor_approvals_and_c0_reject_old_or_mixed_evidence`; `test_c1_requires_unique_25_gate_artifacts_disabled_production_and_authorization`; `test_both_python_cli_preflights_refuse_bound_input_drift_before_any_kubectl` | Reject historical/mixed identities, reused gate artifacts, enabled Production, missing qualification authorization and bound-input drift. Drift rejection precedes mocked Kubernetes calls. |
| Qualification | `test_complete_c2_c3_and_c4_packets_validate`; `test_producer_restores_disabled_state_when_body_fails_after_enable`; `test_producer_restores_disabled_state_when_enable_response_is_malformed`; `test_qualification_renewal_rejects_lost_lease_ownership`; `test_registered_producers_and_complete_close_out_chain` | Validate fixture packets, refuse source/Lease drift, restore the disabled gate, release the owned Lease and leave zero lifecycle/clock replicas. The complete-chain fixture also checks exact producer/source bindings and checkpoint artifact hashes. |
| Closure | `test_registered_producers_and_complete_close_out_chain` | Reject tampered terminal artifacts and dirty preflight; accept the exact four-path fixture transition; reject unpublished, wrong-branch and remapped-remote publication; accept containment in the fixture's local bare remote. |

The existing predecessor validator checks structure, hashes and command ledgers;
these fixture results do not authenticate reviewers, establish artifact semantics
or register gate producers. Executable authenticated interchange remains separate
prerequisite work under the [D3 contract](../../planning-artifacts/c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md).

Accepted PG2 C1.15 renewal, twenty-three registered/done owners, a genuine accepted
25-gate same-profile/session bundle and independent Operations/Security decisions
remain absent. The authorized non-Production context, namespace, deployment and
session, custody root, credential-file paths and shared-system/fault/purge scope
are also absent. Story 27.22 acceptance is limited to its closed C1.16 window.
Live C0/C2-C4, C5/C6 acceptance, terminal/postflight/publication and A41 closure
therefore remain pending. Story 27.4 stays incomplete; A41 and its sprint action
stay open; Production writes stay disabled. No live target was contacted and no
repository staging, commit, publication or dependency update was performed.

Parent verification after the three review clarifications passed using the unchanged
canonical ten-command block. Receipt `/tmp/story-27-4-offline.L9PhHyQb`: all ten
commands and the scoped block exited `0`; 79 lifecycle cases passed in 36.988s
with bare `OK`, zero failures/errors/skips; Debug/source-reference build zero
warnings/errors; exactly 12 retention-decision + 5 A41 guards, every result `Pass`,
zero failures/errors/skips/not-run. The XML run was 2026-10-07 07:53:27 UTC.
Source HEAD was `b350c094ab4bc10bd2daf723f9e2c90ffdddae2a`, with only this handoff,
its spec and an appended pre-existing dependency verification deferral changed.
Execution-time diff SHA-256 `2ffc7a3fe5449176b26d1dad574e4a1559ffff6e59c134168e3c29e118e35ff5`;
XML SHA-256 `efc7e061185c40c729d3fbeb2da559675a6d88efa9b69354600530e7600e327b`;
lifecycle log SHA-256 `b6d42dce0899fb7cb2fe742a1e9f20527e6f81f244240163a6bdfd80e3c2763d`;
build log SHA-256 `22171dcfcd072c2610830b777e590ee1628f05d2862921bbe7a760170c19a4e5`.
The assembly SHA-256 remained `54975db810c985922775a6392a113bed4dba80e7ab2f2e2b16b0575550dc48ad`.
These final receipt notes were recorded afterward. Three review layers completed;
three handoff clarifications were applied, two findings were rejected with evidence,
and one earlier dependency-composition verification gap was deferred. No live
checkpoint, acceptance, A41 transition or publication is claimed.

### 2026-10-07 runtime qualification prerequisite handoff

Reverified the runtime-versus-producer profile mismatch against source HEAD
`95dd8758f2f36d262a2b5c23269a0d73a2d7825b`. The canonical C0-C6 states remain
unchanged. The current interchange proposal and separately scoped P7 runtime
migration are explicit prerequisites, not executable authenticated acceptance.

Read-only source comparison, run from the repository root:

```bash
env PYTHONDONTWRITEBYTECODE=1 python3 - <<'PY'
import re, sys
from pathlib import Path
sys.path.insert(0, str(Path.cwd() / 'tools'))
import verify_access_telemetry_lifecycle as v
v.validate_current_profile_inputs(Path.cwd())
assert v.canonical_pg_onprem_2_profile().manifest()['profile_sha256'] == '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe'
assert v.STORY_27_4_WORKLOAD_SHA256 == '71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f'
text = Path('src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs').read_text()
runtime = re.search(r'ApprovedProfileSha256 = "([0-9a-f]{64})"', text).group(1)
print('Current profile, sixteen bound inputs and workload hashes match.')
print('Runtime profile:', runtime)
print('Producer profile:', v.STORY_27_4_PROFILE_SHA256)
print('Runtime matches producer:', runtime == v.STORY_27_4_PROFILE_SHA256)
PY
```

Exit 0; current profile, sixteen bound inputs and workload hashes matched. Runtime
and producer hashes were the full PG1 and PG2 values above, respectively;
`Runtime matches producer: False`. This comparison confirms a blocker, not a
successful qualification.

The unchanged canonical ten-command offline block ran before these documentation
edits, from the clean full HEAD above. Receipt
`/tmp/story-27-4-offline.DDRI57kg`: all ten commands and the block exited 0;
79 lifecycle cases passed in 36.053s, zero failures/errors/skips; Debug/source-reference
build succeeded with zero warnings/errors; exactly 12 retention-decision and 5 A41
architecture guards passed, every result `Pass`, zero failures/errors/skips/not-run.
XML run: 2026-10-07 10:14:17 UTC; XML SHA-256
`6707e37848c88a0a4346220da5ae481eea6dec6e4c17aaa35ef1ecd1e656d1bd`.
The receipt retains nonrecursive dependency revisions, command exits/logs and built
assembly identity; its source status and tracked diff were empty.

Durable identifiers from that pre-edit receipt:

| Artifact | SHA-256 |
| :--- | :--- |
| Built Server test assembly | `da26b4aef31549254c70cefb12608fac23a572797f4a0e57ff6184252ab82562` |
| `lifecycle.stdout.log` | `9ce7c22d0cd1bd995512adf2a81a8a30f539a888ab081ad57da18095e6698179` |
| `lifecycle.stderr.log` | `1d8002977625c076ebff9ba7eb45e44d7a71d6cca74af497a096bab3d362ee79` |
| `build.stdout.log` | `7a6c4df171722048b8359899d11924b674fb8e750b466fba61ca97bc91ec3ff8` |
| `build.stderr.log` (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `dependency-revisions.stdout.log` | `ed4d7c9f62b89011aff302c0039f448f62ed6d20fbbacb756cf663850c9f3cd1` |

The recorded nonrecursive dependency revisions were:

| Root-declared dependency | Full revision |
| :--- | :--- |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| Hexalith.Builds | `397c94a4e246c90b21cf408790fa0d55bf32d795` |
| Hexalith.Commons | `116d26815eb81e35b3c161e1799e5ee12805fc0a` |
| Hexalith.EventStore | `4e4ee8587ee1f6e73f704cd548734e963b2af3e9` |
| Hexalith.FrontComposer | `c561b3210f15206a90c39c82c58f2e5b1005cd60` |
| Hexalith.McpCli | `e159f82b7528797fc245045625ff387d65294ba9` |
| Hexalith.Platform | `eb16638491809df631723d60ed9884e1e2acf040` |
| Hexalith.PolymorphicSerializations | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |
| Hexalith.Tenants | `811447342e8f44b644a2074565e83f45519528fd` |

The existing gate-mechanics lane also passed:

```bash
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -method 'Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryQualificationWorkloadTests.Gate_*' -parallelMode none -noLogo -failSkips -result-xml /tmp/story-27-4-runtime.Lp1vjnt4/runtime-gate.xml
```

Receipt `/tmp/story-27-4-runtime.Lp1vjnt4`, exit 0: exactly
`Gate_AcceptsOnlyCurrentExactProfileInQualification` and
`Gate_AcceptsAConfigMapProjectionSymlinkOnlyInsideItsMount` passed; zero
failures/errors/skips/not-run. XML SHA-256
`307f1ec86ee9c2acf5da6bb12c61bf032b60a2c6551e71c6fcd9fe811dbebc14`.
These tests cover existing gate mechanics using the runtime constant; they do not
resolve the mismatch or supply current-profile compatibility evidence.
The XML records 2026-10-07 10:16:11 UTC. This earlier receipt did not capture its
own execution-time source revision or assembly hash and therefore does not
independently establish which build it executed. The reviewed rerun below records
those identities rather than retroactively assigning them to this receipt.

The source-bound rerun at 2026-10-07 10:21:46 UTC passed the same exact two tests,
zero failures/errors/skips/not-run, all eight logged commands and the block exit 0.
Receipt `/tmp/story-27-4-runtime-bound.6lk0pkyl` records command arguments,
exit codes and stdout/stderr, nonrecursive dependency revisions, execution-time
status/diff, and matching source HEAD and assembly hashes before and after the run.
Source HEAD: `95dd8758f2f36d262a2b5c23269a0d73a2d7825b`; assembly SHA-256
`da26b4aef31549254c70cefb12608fac23a572797f4a0e57ff6184252ab82562`;
XML SHA-256 `088301d9f4015d4950fb335312533b3b0cab0d255f0bae4d7366c002b018cd1d`;
execution-time tracked diff SHA-256
`058dc0aeb8eade5a08660bfa60e86dc5a11a4048a752a70513e64b2a5fd2ddfb`.
That diff contains the then-current handoff edits; the untracked supporting spec
is listed by the retained source status. Later receipt/review notes are recorded
after the rerun. These are local offline identities, not external custody or live
qualification evidence.

No target was queried, no qualification/fault/purge operation ran and no acceptance,
registration or publication was inferred. Story 27.4 remains `in-progress`, A41 and
its sprint action remain open, and Production lifecycle writes remain disabled.

Final review-patch verification at 2026-10-07 10:23:44 UTC: receipt
`/tmp/story-27-4-handoff-final.u1japj0y`, six logged commands and the script checks
exited 0; exactly 12 retention-decision + 5 A41 architecture cases passed, zero
failures/errors/skips/not-run. XML SHA-256
`c847f2257ffbc67c83c5f34d42fb910f372ec3245d57eaf8e1dd45e96097e1c6`;
execution-time tracked diff SHA-256
`31abf301c4417c8850e517463af8fe986327381d881fb0753f3d1c2c92ac0265`.
The source HEAD and assembly identity match those above. Link resolution, retained
log/dependency identifiers, protected-file byte comparison, two-file mutation scope,
CRLF and whitespace checks passed. The one-shot blind review's four findings were
corrected; no finding was deferred. These final receipt notes were recorded after
that check and provide no live gate, authority or closure credit.

### 2026-10-07 runtime PG2 source/test correction

This separately scoped runtime prerequisite was verified against full baseline
`f98e3f82b4b3f8ed804724811248556c2176139a` with the gate source and tests modified
in the worktree. Local receipt `/tmp/pg2-runtime-gate-4vo_ukjf` retains command
arguments, exits, logs, result XML, dependency revisions and build/source hashes.
The source change replaces only the runtime profile pin and its XML summary.
All valid gate fixtures use independently published PG2 bytes, including runner,
endpoint and ConfigMap projection coverage.

The corrected Debug/source-reference test build passed with zero warnings/errors.
Before changing the runtime pin, direct `Gate_*` execution discovered eight cases:
four failed, including independent PG2 acceptance and historical PG1 rejection;
four passed, zero errors/skips/not-run. This proves the independent assertions
catch the old pin. After the pin change, all eighteen workload-class cases passed,
including `Gate_RejectsHistoricalOrInexactProfile`,
`Gate_RejectsPg2OutsideQualification`, and
`Gate_RevalidatesPg2ExpiryRenewalAndRevocationWithoutCaching`. The latter checks
exact expiry, rejection of PG1 renewal, a valid PG2 renewal at the 15-minute plus
five-second bound, refusal one millisecond beyond it, disabled-state revocation,
re-enablement and file removal on the same gate instance. The twelve retention
decision and five A41 architecture guards also passed, with zero failures, errors,
skips or not-run tests. An independent canonical-profile comparison passed for the
runtime, producer and exact PG2 profile hash, all sixteen bound inputs and the
unchanged historical PG1 canonical hash.

Commands from the repository root:

```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryQualificationWorkloadTests -parallelMode none -noLogo -failSkips -result-xml /tmp/pg2-runtime-gate-4vo_ukjf/green-workload.xml
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo -failSkips -result-xml /tmp/pg2-runtime-gate-4vo_ukjf/architecture.xml
```

Identifiers for the initial passing run, before review coverage patches:

| Artifact | SHA-256 |
| :--- | :--- |
| Built Server test assembly | `576311a00f3b155db8214aa7fbab6ea5d70f159aaf9f72982fda55c18f9e85e2` |
| Runtime gate source bytes | `97b4c83000d4bb81ab8a33e1f46cc64a31ef16085f1527798693ea1a2ae9a8dd` |
| Workload test source bytes | `d8e22d95d178684184752d8e52b29b8af0fa6c8bdd1c7d13830c88f18086a8c5` |
| `green-workload.xml` | `ae6e2d2a770ba670ca0627f69dd03c4e7cc60fde70fe6b547da3c6ed5877457c` |
| `architecture.xml` | `858a7cef33ac2bddc2461194d2359b72038a1669ac66b4caaba622c5d49c8fbc` |

This receipt covers repository behavior, not live approval or qualification.
Documentation was recorded after these test runs; final documentation verification
and review are recorded in the supporting spec. The canonical C0-C6 states,
Production disabled state, open A41/action, sprint tracker and historical receipts
remain unchanged. No qualification target, fault or purge was executed. Architecture
and Operations runtime migration approval remains pending with the other P7 and
live prerequisites; this correction does not complete Story 27.4 or close A41.

Review added mounted-projection renewal through `gate.json -> ..data/gate.json`
on one gate instance, including expiry, independent PG2 renewal, historical PG1
rejection, disabled-state revocation and the original outside-mount refusal.
The managed fixture switches directory symlinks synchronously between reads;
it does not claim to test Kubernetes atomic publication. Runner/endpoint fixtures
now share their existing fake clock with the gate. The runner refuses a previously
successful cached segment after expiry without emitting another record; the
endpoint refuses an authenticated cached replay after disabled-state revocation
with HTTP 503, a bounded reason and unchanged accounting.

After these review patches the Debug/source-reference build passed again with
zero warnings/errors, and all eighteen workload cases passed with zero failures,
errors, skips or not-run tests. The final workload command uses the same selector
and arguments above with `-result-xml /tmp/pg2-runtime-gate-4vo_ukjf/final-workload.xml`.
The profile comparison below also ran and exited zero; its exact script, command,
exit and stdout/stderr are retained as `profile-comparison.*` in the local receipt.

```bash
env PYTHONDONTWRITEBYTECODE=1 python3 - <<'PY'
import re
import sys
from pathlib import Path
sys.path.insert(0, str(Path.cwd() / "tools"))
import verify_access_telemetry_lifecycle as v
expected_pg2 = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
expected_pg1 = "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14"
v.validate_current_profile_inputs(Path.cwd())
assert v.canonical_pg_onprem_2_profile().manifest()["profile_sha256"] == expected_pg2
assert v.canonical_pg_onprem_profile().manifest()["profile_sha256"] == expected_pg1
assert v.STORY_27_4_PROFILE_SHA256 == expected_pg2
source = Path("src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs").read_text()
runtime = re.search(r'ApprovedProfileSha256 = "([0-9a-f]{64})"', source).group(1)
assert runtime == expected_pg2
assert runtime != expected_pg1
print("Runtime, producer and canonical PG2 identities and sixteen bound inputs match; historical PG1 is unchanged and distinct.")
PY
```

| Review-patched artifact | SHA-256 |
| :--- | :--- |
| `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` | `124b6e459789c375d178833902d5a31d121fce2bc28e97f02a7b7d8b8be46665` |
| `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.dll` | `abd56e9ef0bdb506afdf6796afde41fddd60349d49003a3352a352df9b17803c` |
| `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadTests.cs` | `7dc059d635811abce6c9fa0a2cc43dbef6bbe772f6d353cabcfa779f9321a321` |
| `final-workload.xml` | `bd0ed6982270247337ebbc95dff9ed9ef90cad02f31cb842b7db002bdf066452` |

Final documentation checks and review triage are recorded in the supporting spec.


### 2026-10-08 current readiness handoff reconciliation

The current C1 handoff now separates completed I1, narrow authenticated GitHub
observations and source-receipt correction from incomplete I2–I6/P1–P7 and live
acceptance. The named-owner C1 bundle exception requires authenticated separate
Operations/Security receipts and producer exclusion. It changes neither the
legacy label-only predecessor refusal nor the separate C5/C6 reviewer rule.
The compiled epic context includes the current tenant-principal/erasure constraints.
The parent `-7` draft and its frozen intent remain unchanged.

At source `dbe4ce0a97c6a3a883af800f7873a99b79434d44`, the unchanged ten-command
canonical block passed: `/tmp/story-27-4-offline.FKA9f5hE`; all commands and block
exit 0, 80 lifecycle cases in 31.490s, exactly 12 retention + 5 A41 guards all
Pass with zero failures/errors/skips/not-run, and Debug/source-reference build
with zero warnings/errors. Execution-time tracked diff SHA-256:
`c148449fc8b8615dba96c7d4d4f775808398e2aba2d3d621d5a4947f83f39a33`; result XML SHA-256: `ce754554a10e606109e5cd9adbcb54550c95a1af4c855cb630ea2e0a83185694`;
assembly SHA-256: `8a9e11c489f78dc745e898077d282c02a6c0d52d9dcdd8d8bec092a863310013`.

Five existing real-TLS authority cases also passed in 1.764s, zero failures,
errors or skips. Exact executed command:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 PYTHONPATH=/home/administrator/projects/hexalith/memories/tests/tooling/access_telemetry_c1_interchange python3 -m unittest -v test_github_authority.GitHubAuthorityTests.test_real_tls_root_policy_session_parent_gate_and_owner_bundle_chain test_github_authority.GitHubAuthorityTests.test_verified_grants_drive_distinct_bundle_reviewers test_github_authority.GitHubAuthorityTests.test_reviewer_producer_overlap_including_root_session_and_owner_refuses test_github_authority.GitHubAuthorityTests.test_non_owner_same_bundle_reviewer_refuses_even_with_two_verified_roles test_github_authority.GitHubAuthorityTests.test_cross_target_tenant_session_profile_workload_and_source_denials_before_dependents
```

`test_reviewer_producer_overlap_including_root_session_and_owner_refuses`,
`test_non_owner_same_bundle_reviewer_refuses_even_with_two_verified_roles`, and
`test_cross_target_tenant_session_profile_workload_and_source_denials_before_dependents`
attach negative evidence to the current reviewer/scope-attribution claims. The
canonical lifecycle lane also passed
`test_c1_owner_exception_does_not_authorize_label_only_legacy_predecessor`.
Logs/commands/exits and 1,818 baseline file hashes:
`/tmp/story-27-4-readiness-92sb2tz2`. All existing user changes, source/dependency
revisions, matrix states, sprint/history/Production bytes and historical receipts
were preserved; current links and CRLF passed. These are local offline fixtures,
not live grants or custody. Dated receipt notes were added after execution.
Live gates, Story 27.4, A41 close-out and Production activation remain pending.


Read-only review follow-up confirmed Platform publication: the freshly observed
`origin/main` is `2c4f788a6ffde2646de1686492dc817f5505c922`; it contains the
authenticated-decision commit `48d5c6e64087bb33232651d8b59422e95185c699`,
transport commit `794e8c63fe945bf689064edb8a406801810cb4cf`, root-recorded pin
`495d1d0dfceb33e6bb628809a42a284526707ff6` and the preserved user checkout
`94c0359ffd4da1c186df7ad4218b20c0eae99a89`. The root-recorded pin already
contains both exact executed authority/transport files. The older prerequisite
publication wording does not establish a current missing handoff; provenance is
recorded in `platform-publication-check.json` in the readiness receipt. No
fetch, gitlink change, publication action or operational acceptance occurred.
Review corrections clarified the legacy structural-label limitation and added
AD-17 token stability/collision safeguards to the compiled context. All executable
sources remained identical; the historical receipt prefix and matrix states were
preserved. The next separate implementation dependency is I2 closed
registry/source binding, with genuine registration/command/role receipts still
required; this supporting slice supplies none of those approvals.

### 2026-10-08 Story 27.4 draft readiness recheck

The unchanged ten-command offline block passed at source revision
`906bc07ad6a8e4912a7222d9d097da434148266a`. Receipt `/tmp/story-27-4-offline.fq11FV9r`
retains all commands, exit codes, source/worktree and dependency identities,
logs, assembly hash and XML. All ten commands and the block exited `0`:
80 lifecycle tests, exactly 12 retention-decision plus 5 A41 guards, every XML
result `Pass`, zero failures/errors/skips/not-run; Debug/source-reference build
had zero warnings and errors.

| Receipt item | SHA-256 |
| :--- | :--- |
| Execution-time diff | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Built assembly (build identity only) | `7f4c851d0c4306937e8b4302be53bc36e8e6ec9aea033b5390e4e46e3ae28d89` |
| Architecture XML | `26c934d0544125bd72ea843207b4ca664bb8d348351600f8d0c657f671de0698` |
| Lifecycle stderr | `31bb90df5b743e447f862fddc67731880c9ccad17fad2bc63b20b23e5417a01f` |

`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`
exited `0`: 167 passes, zero failures/errors/skips. Logs and protected-file
baselines: `/tmp/story-27-4-current-readiness-3gv1qqwq` (local offline receipt).
The retained named negatives include
`test_c1_owner_exception_does_not_authorize_label_only_legacy_predecessor`,
`test_producer_refuses_unverifiable_target_identity_without_mutating_target`,
`test_producer_restores_disabled_state_when_body_fails_after_enable`, and
`test_business_and_privacy_canaries_use_curl_config_and_tenant_routes`;
all passed. They test refusal, cleanup and tenant/privacy boundaries using
fixtures; they confer no target authority or live gate acceptance.

Investigation found no executable remaining live task within the current draft.
I2 closed registry/source-binding preparation can be proposed separately with
isolated fixture contracts and no deployed accepting entries; the current
27.4 intent excludes implementing separate prerequisites. Semantic admission,
assembly, consumer migration and genuine accepted evidence/execution inputs
remain outstanding. Runtime PG2 correction is already complete offline.
No checkpoint state, A41/action status, Production deployment, sprint or
historical Epic 20/Story 20.5 byte changed; no target was contacted.

### 2026-10-09 current-HEAD readiness recheck

The owner session scope is recheck only: rerun the offline block, refresh this
readiness record, and keep Story 27.4 held. Current HEAD
`5d0cbefedc828ab829b0b9104c5ab77788dcd5b2` contains the investigation commit
`d172165ebe1ab39efebd38b875b94f6d4ef6d191`. No live target was contacted. C0–C6
matrix states, close-out prerequisites, A41, the sprint action, Production
lifecycle writes, and Epic 20/Story 20.5 bytes stay as recorded above.

Offline I2 registry/source inspection and C1.15 declared-observation pin
inspection are complete as isolated fixture contracts. Deployed lookup still
refuses, and neither slice registers an accepting entry or accepts a gate.
Closed I2 registration and provenance, I3–I6, and P1–P7 remain held. Accepted
PG-ONPREM-2 C1.15 renewal, twenty-three registered/done gate owners, an accepted
25-gate predecessor, authenticated Operations/Security decisions, and scoped
target/custody/credential/fault/purge grants remain absent. Live C0/C2–C4,
independent C5/C6, terminal validation, the exact four-path close-out, and
remote containment therefore stay pending.

The unchanged canonical ten-command block passed before these dated notes were
appended. Receipt `/tmp/story-27-4-offline.RJE0jKex`; all ten commands and the
block exited `0`. Lifecycle: 80 tests in 29.041s, bare `OK`, zero failures,
errors, or skips. Debug/source-reference build: zero warnings and zero errors.
Architecture XML run `2026-10-08T23:47:07Z`: exactly 12 retention-decision and
5 A41 guards, every result `Pass`, zero failures, errors, skips, or not-run
tests. `git diff --check` passed.

| Receipt item | SHA-256 |
| :--- | :--- |
| Execution-time diff | `16099db9e36a15fc5967b15a0f3cb8533b2b08d011935dd70c2d38146190e376` |
| Built assembly (build identity only) | `c183e5f3f6a8521e2cac5d1d3597769973b78d61838bcd4ca69433ae61c2f1b4` |
| Architecture XML | `dba7c7bc64fecffa20255976e475ee013d29264fc0c2ddb1d3e10d0286637299` |
| Lifecycle stderr | `a63616603754e00c6330d25ebc840b073c0c99b31fb14f7d783bec80e650f56d` |
| Build stdout | `9b8f6db3715cf60a0b7b859dcf34e27400bddf5783776b94d5d28d173a8ed183` |
| Dependency revisions | `fad42ddc1834631026c88e64caaf732c71f18e580c4b056249a5923bf45c920d` |

The execution-time worktree change was only this story's spec status field,
`ready-for-dev` to `in-progress`. Nonrecursive dependency revisions were
Hexalith.AI.Tools `3f194e17174994d308ec84af9ee2b5aa68674d0d`, Hexalith.Builds
`fef031806321793c9effb17235c2465118984432`, Hexalith.Commons
`b247ed116c6523f8c596ec0a933eff8973d11568`, Hexalith.EventStore
`07d1e23a6c5b06bbbb1fc8ddb5174cc3382d3d93`, Hexalith.FrontComposer
`0e114214007c22f5cdbac21a6853cff4208340ee`, Hexalith.McpCli
`1b1012d099b18067e8b915c6bf00b48ac6ce458d`, Hexalith.Platform
`f6f95cdf5a63e264ada564c4cbb5a5b40b0d5a9a`, Hexalith.PolymorphicSerializations
`98de6e013840ece9f0fa7c68ab7dcdf2bba3b375`, and Hexalith.Tenants
`10c9f6f66632f6861fe13fb9f41ab05b90c37f67`.

`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`
exited `0`: 219 tests in 48.603s, bare `OK`, zero failures, errors, or skips.
Receipt `/tmp/story-27-4-interchange.48qhOxQj`; stderr SHA-256
`a58b70d0cf17fe6ce31308076c776ebe41d48d601e712184d104ebcff8b43451`.

The lifecycle log again records `ok` for
`test_c1_owner_exception_does_not_authorize_label_only_legacy_predecessor`,
`test_producer_refuses_unverifiable_target_identity_without_mutating_target`,
`test_producer_restores_disabled_state_when_body_fails_after_enable`,
`test_business_and_privacy_canaries_use_curl_config_and_tenant_routes`,
`test_current_target_predecessor_approvals_and_c0_reject_old_or_mixed_evidence`,
and `test_qualification_renewal_rejects_lost_lease_ownership`. These fixture
results prove offline refusal, drift, cleanup, and tenant-boundary behavior.
They confer no live gate, custody, or publication credit.

Protected bytes read before this readiness text was appended, and left unchanged
by it: `sprint-status.yaml`
`0e288f08029843870db90402c0cc3d3a1ea5b661dc3c11282729e397a7fcc05a`;
`20-5-inbound-rate-limiting-quotas-and-audit-completeness.md`
`5bfdb89ef34f8cc8f113df5a0638865bf246e49c084f1c608159e1ea71d820dd`;
`deferred-work.md`
`8acedc1f0563b34baf2409e94fad13688d4e0cdfd7c38b2dcc7c864d82f9d48f`;
`deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml`
`0c2b4b836d14be457ab8ccc79a534cd9997193f9c7b10f3d11b8b100a8b40ab2`;
`_bmad-output/project-context.md`
`0b62f5ad02d34cb4ad5ddacb5ad0693d0251874f5733fbb4de87c7593636e665`;
`docs/dev/telemetry.md`
`2f7a252dd2c9d229058b929b90e2dc3b6e7ee338cec28d9f4eb494dbe7846b2a`.
The Production patch hash matches the lifecycle verifier's pinned disabled
overlay. Story 27.4 remains incomplete, A41 and its sprint action remain open,
and Production lifecycle writes remain disabled.

### 2026-10-09 follow-up recheck at `1ead5a1b`

The approved session remains `RECHECK_ONLY`. Source HEAD was
`1ead5a1b2432e24211b18060fe1d2da6126b31b3`, with a clean execution-time
worktree. The unchanged canonical ten-command block above exited `0`; every
logged command exited `0`. Receipt: `/tmp/story-27-4-offline.XLdeHWCY`.
The separately logged authenticated-interchange command exited `0`; receipt:
`/tmp/story-27-4-interchange.yDRybdwi`. These local receipts are offline
verification output and confer no operational or custody authority.

| Check | Observed result |
| :---- | :-------------- |
| Lifecycle tooling | 80 tests in 38.205s; bare `OK`, zero failures, errors or skips. Refusal, drift, cleanup and tenant-negative fixtures remain passing. |
| Debug/source-reference Server test build | Succeeded with zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision plus 5 A41 guards, all `Pass`; zero failures, errors, skips or not-run cases. |
| Authenticated interchange | 219 tests in 69.050s; bare `OK`, zero failures, errors or skips. |
| Whitespace | `git diff --check` exited `0`. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Nonrecursive dependency revisions | `299a9c30c2c462b0b5e5e8f4d2be34169668c10f0034bda198a5d5b623afd8c1` |
| Built assembly (build identity only) | `0fadef08ae1184f2ea006eead3cbcee5614538343f66f8c9ff14544e8ce4194f` |
| Architecture XML | `39a97b4dd5f25cb88ce74b0c348cc90b90559343d4bfdc0a13262cd0f50edd80` |
| Lifecycle stderr | `28b43af8172f975fe491fdc138f3fd39afff96220440a6032f2c5e2a7d4978f4` |
| Build stdout | `741edeb7005778fceb9b56e50fda6483d41dad94062ef216dc135f18179d3f82` |
| Interchange stderr | `2c0cb85603c0becd533d20c4635cfc4b9f5e44f93474ab7370a222fd77679b71` |

The protected sprint, Story 20.5, deferred-work, Production-disabled overlay,
project-context and telemetry-document hashes match the prior 2026-10-09
readiness record exactly. The root-recorded nonrecursive dependencies include
Hexalith.McpCli `ecf8952513eaf20e09a8ecb4f6fcc56dc0e829df`; the receipt
retains every revision. No source, test, target, sprint, history, protected or
Production byte changed during this recheck.

Closed I2 registration/provenance, I3-I6, P1-P7, accepted PG-ONPREM-2 C1.15
renewal, twenty-three registered/done gate owners, an accepted eligible
25-gate predecessor, authenticated Operations/Security decisions and scoped
target/custody/credential/fault/purge grants remain absent. Live C0/C2-C4,
independent C5/C6, terminal validation, exact four-path A41 close-out and
remote containment remain pending. Story 27.4 remains incomplete, A41 and its
sprint action remain open, and Production lifecycle writes remain disabled.

### 2026-10-09 recheck at `1cb117c5`

The approved Story 27.4 session remains `RECHECK_ONLY`. Source HEAD was
`1cb117c517e2644e239a22a82c1a9a9c876a21cb`, with a clean execution-time
worktree and an empty tracked diff. The unchanged canonical ten-command block
exited `0`; every logged command exited `0`. Receipt:
`/tmp/story-27-4-offline.uLtgJrsf`. The separately logged authenticated
interchange lane exited `0`; receipt:
`/tmp/story-27-4-interchange.dGuoGMS2`. These local receipts are offline
verification output only.

| Check | Observed result |
| :---- | :-------------- |
| Lifecycle tooling | 80 tests in 29.061s; bare `OK`, zero failures, errors or skips. Refusal, drift, cleanup and tenant-negative fixtures passed. |
| Debug/source-reference Server test build | Succeeded with zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision plus 5 A41 guards, all `Pass`; zero failures, errors, skips or not-run cases. |
| Authenticated interchange | 242 tests in 55.410s; bare `OK`, zero failures, errors or skips, including offline I2 provenance and I3 authority-boundary tests. |
| Whitespace | `git diff --check` exited `0`. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Nonrecursive dependency revisions | `d9be8ddf152d03571503787ee235b30aa0ab911fb60608e43d379592a66bcd2d` |
| Built assembly (build identity only) | `037b305d123eadea7901b2ebc234389c66ea29952403ad1a74dd1c456395c497` |
| Architecture XML | `a673502d4219717c0fdbcf95f6e19e73f273b9ce877430e031759a18be8c446b` |
| Lifecycle stderr | `63ddde3eab12852a27da05e1c5b45f9d85134b070a5cfb06f9041679ea357f10` |
| Build stdout | `eb25a81ab0d2fceced8b85051e4bf3c413b4f5f5f6f4094ee4b3706e8117468d` |
| Interchange stderr | `7af4974b62a9925541e6628865ef6c3bfe72f2d85453b8174822a45c2e22483d` |

Root-declared nonrecursive dependencies changed since the previous receipt;
its revision log records the exact current set. The protected sprint,
Story 20.5, deferred-work, Production-disabled overlay, project-context and
telemetry-document SHA-256 values still match the preceding 2026-10-09 record.
The Production patch remains at the lifecycle verifier's pinned disabled hash.
No source, test, target, sprint, history, protected or Production byte changed
during this recheck.

The [P1–P4 owner decision packet](../../specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md)
now records owner `APPROVE` dispositions and selected offline policy values.
It explicitly denies operational bootstrap, non-GitHub receipt issuer,
authenticated target/session grant, custody root and gate-reviewer grants; its
policy is not operationally effective. The offline I3 boundary always refuses
acceptance. Closed I2 registration/provenance, I3–I6 operational authority and
semantics, P5–P7, accepted PG-ONPREM-2 C1.15 renewal, twenty-three
registered/done gate owners, an eligible accepted 25-gate predecessor,
authenticated Operations/Security bundle decisions and scoped
target/custody/credential/fault/purge grants remain absent. Live C0/C2–C4,
independent C5/C6, terminal validation, exact four-path A41 close-out and
remote containment remain pending. Story 27.4 remains incomplete, A41 and its
sprint action remain open, and Production lifecycle writes remain disabled.

### 2026-10-09 current-revision offline recheck at `d5feac61`

The approved Story 27.4 scope remains `RECHECK_ONLY`. The canonical ten-command
block above ran against root HEAD
`d5feac61bb6fe90f13b338ab7c10a9220b6d7602` and exited `0`; every logged
command exited `0`. Its execution-time tracked diff was empty. Source status
recorded only the new, untracked one-shot spec for this recheck. The separate
authenticated-interchange command also exited `0`. Local receipts:
`/tmp/story-27-4-offline.NHHGW9aS` and
`/tmp/story-27-4-interchange.RurXxP9g`. They are offline logs, not target or
custody evidence.

The separate interchange invocation was
`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`.

| Check | Observed result |
| :---- | :-------------- |
| Lifecycle tooling | 80 tests in 49.971s; bare `OK`, zero failures, errors or skips. |
| Debug/source-reference Server test build | Succeeded with zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision plus 5 A41 guards, all `Pass`; zero failures, errors, skips or not-run cases. |
| Authenticated interchange | 242 tests in 73.871s; bare `OK`, zero failures, errors or skips. |
| Whitespace | Canonical `git diff --check` exited `0` before the evidence edit. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Nonrecursive dependency revisions | `38e19c88fc2694dd8dc6432bd88adc3b82f459a0ffc443eeeb52e6abf859c3ea` |
| Built assembly (build identity only) | `dfa0b03dea7a1a43544fa55d3da1b5e2d3fe2817ed5bc34121cf2c90cb2eb326` |
| Architecture XML | `33f6d812b80fb92d321ec4c827df5242e39f464e1205c5e509fbae83d8df454a` |
| Lifecycle stderr | `8555f3c09440020a5b97719e1e90288ae1544a02ccd6a56e7833e051942b34fc` |
| Build stdout | `1d53666167779357d021b1d94f353b1411d874fec12e422c4d5b86acabd83c5b` |
| Interchange stderr | `88684acf7c0389dcac98255a196a8d21da6a8055762f9f4967eab84f013b97a7` |

The receipt records all nine root-declared, nonrecursive dependency revisions:

| Dependency | Revision |
| :--------- | :------- |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| Hexalith.Builds | `468fdbba04e2d9a27d251298875125b57fa6d836` |
| Hexalith.Commons | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| Hexalith.EventStore | `5e5d2305d870a03d6d63f52470ce9f7a1ec058da` |
| Hexalith.FrontComposer | `0e114214007c22f5cdbac21a6853cff4208340ee` |
| Hexalith.McpCli | `bfb2ae376bfe0243e6283d24a6cad44315c48208` |
| Hexalith.Platform | `c489268fb347e7a65eedc6f2a4a40ab3ed4ce118` |
| Hexalith.PolymorphicSerializations | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |
| Hexalith.Tenants | `96cc6f115865f9e2307aedc3748e60e0f99edeae` |

The protected sprint, Story 20.5, Production-disabled overlay, project-context
and telemetry-document hashes still match the earlier 2026-10-09 record.
`deferred-work.md` now hashes to
`5aac0ee356fca0bb91f8271ccbfc0d27b5a0aa97e77c614f5132eadfc670bfad`
after the separately committed P1 receipt hardening; this recheck did not edit it.

Operational P1-P4 roots and grants, closed I2 registration/provenance, I3-I6,
P5-P7, accepted PG2 C1.15 renewal, twenty-three registered/done gate owners,
an eligible accepted 25-gate C1 predecessor, authenticated bundle decisions,
and scoped target/custody/credential/fault/purge grants remain absent. The
offline I3 boundary still refuses acceptance. No live target was contacted;
C0-C6 states, Production writes, Story 27.4, A41 and its sprint action remain
unchanged.

After the evidence edit and review corrections, `git diff --check` exited `0`.
The two exact architecture selectors were rerun from the built Debug assembly:
17 passed with zero errors, failures, skips or not-run cases. These are
post-edit repository checks only.

### 2026-10-10 current-revision offline recheck at `b0834097`

The approved Story 27.4 scope remains `RECHECK_ONLY`. The canonical ten-command
block above ran at root HEAD `b0834097b02a982f6635c5dbe6397053e61b9810`;
all ten commands and the block exited `0`. The execution-time tracked diff was
empty; status showed only the new untracked one-shot recheck spec. The separate
authenticated-interchange invocation
`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`
exited `0`. Local receipts are `/tmp/story-27-4-offline.kiDxsypC` and
`/tmp/story-27-4-interchange.ruf5qebs`; they are offline logs, not live target
or custody evidence.

| Check | Observed result |
| :---- | :-------------- |
| Lifecycle tooling | 80 tests in 41.552s; bare `OK`, zero failures, errors or skips. |
| Debug/source-reference Server test build | Succeeded with zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision plus 5 A41 guards, all `Pass`; zero failures, errors, skips or not-run cases. |
| Authenticated interchange | 242 tests in 72.877s; bare `OK`, zero failures, errors or skips. |
| Whitespace | Canonical `git diff --check` exited `0` before this evidence edit. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Nonrecursive dependency revisions | `1e2df938c9624328c73e0bbdbd11250cc19b8539d8bbf50f20d25f256fa5bdbe` |
| Built assembly (build identity only) | `efc8144890de432b3428982f789194c363b19ebcd1609d39c30cd83e6e410e34` |
| Architecture XML | `43e526cb81c5cc02e486bc153f88ebf559b27eb603ed16cddb67354f1d021751` |
| Lifecycle stderr | `4fd9d5015b8e11a4d1e29371a1e3d7373306439cd975fc196555ed5ecaa77a2b` |
| Build stdout | `510ed595ef49df76bde0855a81ce27c738c2fc3a3f7da8195d569c5431a4ec5d` |
| Interchange stderr | `f9ab7ec2fed371ba886f59dc72db1895c1d54cbce9f2cd831fa77c4a84ea05f5` |

The receipt records all nine root-declared, nonrecursive dependency revisions:

| Dependency | Revision |
| :--------- | :------- |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| Hexalith.Builds | `0d5f45c7612a0f9582690d5eb143948e7798946e` |
| Hexalith.Commons | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| Hexalith.EventStore | `ea4c9857e6865030a3b6eb900d55fdfaf4c621a1` |
| Hexalith.FrontComposer | `0e114214007c22f5cdbac21a6853cff4208340ee` |
| Hexalith.McpCli | `f83bac4939a24402e4386cd56c70cd3f2dfd22ac` |
| Hexalith.Platform | `8a56bd57837784806aae989159fbcbc71fbd41d4` |
| Hexalith.PolymorphicSerializations | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |
| Hexalith.Tenants | `cdd0c80c111109125f6bec63d7a3d64885` |

Protected sprint, Story 20.5, Production-disabled overlay, project-context and
telemetry-document hashes remain
`0e288f08029843870db90402c0cc3d3a1ea5b661dc3c11282729e397a7fcc05a`,
`5bfdb89ef34f8cc8f113df5a0638865bf246e49c084f1c608159e1ea71d820dd`,
`0c2b4b836d14be457ab8ccc79a534cd9997193f9c7b10f3d11b8b100a8b40ab2`,
`0b62f5ad02d34cb4ad5ddacb5ad0693d0251874f5733fbb4de87c7593636e665`
and `2f7a252dd2c9d229058b929b90e2dc3b6e7ee338cec28d9f4eb494dbe7846b2a`
respectively, matching the prior record.
`deferred-work.md` is `296b415ee8d9f8823cfccdd9f284a76d0da2d246710e8a98070c079e6a30367e`
at this root revision; this recheck did not edit it.

Operational P1-P4 roots and grants, closed I2 registration/provenance, I3-I6,
P5-P7, accepted PG2 C1.15 renewal, twenty-three registered/done gate owners,
an eligible accepted 25-gate C1 predecessor, authenticated bundle decisions,
and scoped target/custody/credential/fault/purge grants remain absent. No live
target was contacted; C0-C6 states, Production writes, Story 27.4, A41 and its
sprint action remain unchanged.

Post-review repository checks for the 2026-10-10 recheck: `git diff --check`
exited `0`; the same exact two architecture selectors passed all 17 tests with
zero errors, failures, skips or not-run cases. The post-review XML SHA-256 is
`00df7991291658d2521c46fd63ea76920a4f8db8244633c2881061b99d6d2ceb`.

### 2026-10-10 strict C1 predecessor consumer refusal receipt

This offline implementation pass moved every Story 27.4 C1 authorizing route to a
single strict version dispatch. Legacy `gates` and `successors` packets, including
complete-looking twenty-five-gate claims, refuse with `version refused`. A
structurally valid `hexalith.access-telemetry.c1.predecessor/v2` packet refuses
with `current authority unavailable` because the authenticated I3-I5 verdict,
retained-reference custody, current session status and approved P7 time limits
are not yet available. The legacy parser remains available for historical
inspection only; its structural result cannot authorize a checkpoint.

The C2-C4 producer test installs forbidden Git and process dependencies and
observes zero calls for both legacy and unverified v2 predecessors. CLI tests
cover both `--input` and `--scenario-input` refusal, even with missing scenario
input. Direct validation with `require_current_freshness=False` also refuses.
Terminal bundle and close-out preflight tests refuse C1 before reading any
other bundled artifact, querying Git, creating a recovery snapshot or emitting
a passing checkpoint. Rejection packets are bounded and retain no gate credit.

| Offline command | Result |
| :-------------- | :----- |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` | 82 tests; `OK`, zero failures, errors or skips. |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` | 242 tests; `OK`, zero failures, errors or skips. |
| `git diff --check` | Exit 0, no whitespace errors. |

No live producer or target was contacted. The canonical C0-C6 verdicts above
remain unchanged, Production lifecycle writes remain disabled, and A41 and its
sprint action remain open. This receipt grants no C1 acceptance or Story 27.4
completion. Acceptance still requires the authenticated verifier and approved
operational P1-P7 inputs.

### 2026-10-10 postflight and publish continuation refusal addendum

Review found that a self-hashed passing-looking preflight packet could reach
postflight or publish verification without rechecking current C1 authority. The
shared preflight-consumption boundary now refuses that packet before manifest,
snapshot, Git or A41 access. A focused dependency-sentinel test exercises both
continuations with missing downstream artifacts and a self-hashed preflight.

The complete lifecycle suite passed 83 tests, the unchanged interchange suite
passed 242 tests, and `git diff --check` exited 0 after the patch. This is an
offline refusal receipt only; no close-out or Production permission is granted.

### 2026-10-10 spec 11 held-state verification

At root commit `4167ce3fdcffd0923bc9f5d8960025492a279608`, no authenticated Platform authority response, deployed C1.15 binding, eligible same-session 25-gate C1 predecessor, or C2-C4 action grant was supplied to this run. The [current response audit](../pg2-c1-i2-operations-security-request.md#2026-10-10-response-audit-for-story-274-spec-11) records the limited repository and task-input observation. Existing refusal tests cover missing authority and C1, profile or scope drift, cleanup after failure, and forged preflight continuation. They cannot substitute for the absent live qualification row.

| Offline check | Result |
| :------------ | :----- |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` | 83 passed; zero failures, errors or skips. |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` | 242 passed; zero failures, errors or skips. |
| `git diff --check` | Exit 0 before this entry. |

No live target was contacted and no checkpoint was advanced. C0-C6, Production lifecycle writes, A41 and its sprint action retain their held states. The protected Story 20.5, sprint, Production overlay, project-context and telemetry bytes matched the prior recorded SHA-256 values. This note is offline verification, not an authenticated packet, reviewer decision, custody receipt or publication proof.

### 2026-10-12 P1 v1 infeasibility and C1.15 packet blocker

Independent retrieval of Platform `e229df9a58ee6899f2b24ba51f652435bc36dc62:docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md` matched SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`. P1 v1 is accepted as not currently feasible. No adoption manifest was created. The C1.15-only signed packet is not enrolled.

The blocker record is `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/c1-15-only-signed-evidence-packet-decision.md`, SHA-256 `d85537fbbf3a50eeff913a6c3363fbd3f0046ae112b0ce3d1f2b37cbce36eb04`. The owner choice of that packet and its signer is absent, and so are the authenticated status, trusted-time, fresh read-only grant, source and command binding, producer, independent reviewer, and external custody records. C1.16 eligibility, the other twenty-three gates, P5–P7, and later C2–C4 grants remain separate. The canonical C0–C6 rows, Production disablement, and open A41 state are unchanged. No live target was contacted.


### 2026-10-12 spec 14 current-pin readiness hold

The [current response audit](../pg2-c1-i2-operations-security-request.md#2026-10-12-current-pin-response-audit-for-story-274-spec-14) rechecked Memories HEAD `f4c7d68f619f376debca440387428139a61080cc` and its root-recorded Platform pin `977f2e01347226ac727b707aee4f140f5dc8b010`. The same P1 v1 infeasibility report and supporting contract/qualification blobs match their previously recorded digests. No authenticated operational response or accepted eligible C1 predecessor was supplied. The signed-packet blocker remains unenrolled; current authority, status and revocation cannot be authenticated from these local bytes. Disposition remains `RECHECK_ONLY`.

The two commands required by spec 14 ran sequentially from the repository root between `2026-10-12T14:03:58Z` and `2026-10-12T14:06:00Z`. Retained local receipt: `/tmp/story-27-4-spec14-oo85qkwd`. It contains the verification script, exact command arrays, stdout/stderr, exits, execution-time source revision/status/diff, nonrecursive dependency revisions, current-pin readback and tracked-file hashes. It is offline verification output, not an approved evidence root or custody archive.

| Executed command | Result |
| :--------------- | :----- |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` | 242 tests in 85.213s; bare `OK`, exit 0, zero failures/errors/skips. |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` | 83 tests in 34.426s; bare `OK`, exit 0, zero failures/errors/skips. |
| `git diff --check` | Exit 0 before these audit notes were appended. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff | `76e398c511aac59ee662bbaaba7b399bb6c58dc967e8d78dfded17ceadc5d88c` |
| Nonrecursive dependency revisions | `f8a2bd8315332d8171227c317b7e0fe643c65e914c3771ac20221cb3639b8385` |
| Interchange stderr | `a5454d458662abe3cee2f0720e6ef713e1f5e05fb5530556598d3ea443c0c0b2` |
| Lifecycle stderr | `ff6402859eb07aa72845001876b0886f808dc19e50252f1b3f12936b08e93e57` |
| Current-pin readback JSON | `932cbcdd2c280851e9f2b165549a36520a3e8c0ca0e819eb878db6c4317738cd` |
| Tracked-file baseline | `6c893c2707edcba36b448c311b82e62e27b38dab365a384e9490d90ed3da1980` |

The execution-time diff contained only the parent workflow's spec 14 status change to `in-progress`. Every tracked file stayed byte-identical during the checks. The blocker SHA-256 `d85537fbbf3a50eeff913a6c3363fbd3f0046ae112b0ce3d1f2b37cbce36eb04` identifies its checked-out CRLF bytes; its LF Git blob SHA-256 is `e5783bb1b45a80ef716e313fe1fa31cead3d3c6ff7c3c1f8d10bdf3b2002440e`. Both were checked without rewriting either representation.

The logs record `ok` for `test_matching_claim_and_fixture_receipt_refuse_without_provider_or_target_calls`, `test_deployed_lookup_always_refuses_for_empty_fixture_and_opaque_inputs`, `test_c1_authorization_refuses_legacy_and_unverified_v2_before_payload_validation`, `test_producer_refuses_c1_before_git_or_target_dependency`, and `test_postflight_and_publish_refuse_self_hashed_preflight_before_dependencies`. These cover the held-state refusal before provider, target, Git, snapshot or A41 dependencies. `test_complete_c2_c3_and_c4_packets_are_inspectable_without_authorization` and `test_producer_restores_disabled_state_when_body_fails_after_enable` also passed as offline packet/cleanup fixtures. They confer no live qualification or publication credit. The authorized qualification and close-out rows remain unexecuted.

All six spec 14 live tasks remain open. Accepted PG2 C1.15 renewal, C1.16 session eligibility or fresh capture, the twenty-three other gate successors, 25 distinct same-session passes and authenticated bundle decisions remain required. Separate fault/purge grants, independent C5/C6 decisions, terminal proof and A41 publication authority/proof remain absent. No live target was contacted. The canonical C0–C6 states, open A41/action, Production-disabled and qualification zero-replica overlays, sprint and historical bytes are unchanged. These dated notes were appended after verification; they do not complete Story 27.4.

### 2026-10-12 owner policy adoption

The owner explicitly approved `sole-owner-qualification/2026-10-12`, including
self-review and private local hashed/readback evidence for non-Production
qualification. Institutional receipt-service, separate human-reviewer and external
archive requirements above are historical for the older protocol. The current
[owner handoff](../../../docs/operations/access-telemetry-owner-handoff.md) replaces
them for this mode. Actual run approval, implemented collectors/verifiers and all
required same-session technical evidence remain outstanding. No live target was
contacted, C0–C6 row was advanced, Production write enabled or A41 action closed by
this policy adoption. The earlier 325 passing tests remain their recorded offline
evidence; any new verifier results must be recorded after execution.


### 2026-10-12 sole-owner record-inspection verification

The approved policy is reflected in planning, architecture, the operations
runbook and the re-derived interchange contract. The new pure
`tools/access_telemetry_c1_owner_run.py` inspects an exact bounded record against
externally supplied approved-plan anchors and current caller facts. It discloses
self-review; it performs no target or custody operation and returns no accepted
gate, predecessor or execution handle. Existing deployed consumers are unchanged.

All three review layers were triaged and concrete findings corrected, including
refusal of caller-defined mutable/throwing timezones and canonical immutable UTC
retention. The implementation agent's corrected focused lane passed 25 tests in
0.092s. Parent's full affected interchange lane passed 267 tests in 84.285s,
exit 0, bare `OK`, zero failures/errors/skips:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

Retained offline receipt: `/tmp/pg2-owner-run-final-58o8_ync`; stderr SHA-256
`431e213837cb8a158561b3bd461cc6ce0184f496e97dc788e0ac4315f23364e2`. Nineteen existing code/test files, the
five protected status/A41/Production-related files and the historical signed
packet blocker preserve their pre-adoption raw-byte hashes. This is offline
record inspection evidence only. Missing collectors, full owner-mode I3/I5,
scoped live approval and actual C0–C6/terminal/publication evidence remain
outstanding. All six Story 27.4 live tasks remain open; no target was contacted,
Production enabled or A41 action resolved.


### 2026-10-12 owner acceptance and bounded readiness continuation

The owner's **“I approve and accept”** accepts the presented sole-owner approach
and the 25 focused / 267 full offline test results. The
[acceptance record](../27-4-owner-approval-and-acceptance-2026-10-12.md) binds
retained source/contract/log bytes, with acceptance SHA-256
`c509977473f5b817c107e232da2f06774cfa1b74c41337d54bb9b97317fcac40`. Private archive mode `0700`, file mode
`0600` and exact-byte readback were verified. This is local owner acceptance,
not independent authentication or complete C1 evidence.

After that approval, bounded read-only readiness queries observed the existing
telemetry namespace/deployment/Pod and matching Dapr OCI index identity. The
Deployment already had one desired/ready replica. The isolated
`hexalith-memories-qualification` namespace lookup returned NotFound. Exact
commands, exits, projections and UTC observations are retained in the archive.
No Secret/credential value, logs, Pod exec, fault, mutation or live qualification
collector was invoked. The earlier no-target-contact statements describe their
earlier offline phases; this continuation contacted the API only for readiness.
The canonical C0–C6 verdicts, open A41/action and Production configuration remain
unchanged. Missing tooling and isolated test setup remain implementation work.

</details>

## Current R2 custody clarification — 2026-10-18

Current Platform **`902200ee82f706d90637867617d3b0295370beeb`** and the R2 **`final-*`** records supersede the historical `7f2394256c876688aaaee764b933e1cf8c1b9e94` binding and its source-admission description. R2 `source-admission.json` is the **retained failed admission** after external HEAD/source drift; it is not the current final admission. The current source-observation record is `final-source-admission.json`. All earlier prose and refusal records remain historical evidence.

For R2 `post-drift-model-build/source-mapping/mapping.json`, the semantic mapping **contentHash** is **`734b88a96ebc4f2277050dcbf6704eb953decbd5457ff61dc78f1e044e8de93d`**. The serialized mapping file's **SHA-256** is **`8c0b7bdb2fc51ceb1cff1c3ade8e7fd9d01f8fa340d22f2cb4f67b2586c751e6`**. These identify different things.

Exact private index anchors under R2 **`/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/approved-catalog-repair-jwz1ir3f/source-drift-resume-20261017-f50jvn7r/source-refresh-20261018-53vfi0yw/metadata-refresh-r2-_4u_999v`**:

| Filename | SHA-256 of file bytes |
| --- | --- |
| `final-preparation-summary.json` | `369ada95b96cc9737e439cd5747b3499392fcc85f356ec7edf8016d19e6f218a` |
| `final-artifact-binding.json` | `b15f9addb83b6faf04f1c0be8151ed42cf56b5970f2e2dff54b5b74916622fdd` |
| `final-source-admission.json` | `b37fc41108f1031f4bfd8f96bb492551b102c7e13f5e10978f6352edde55bc00` |
