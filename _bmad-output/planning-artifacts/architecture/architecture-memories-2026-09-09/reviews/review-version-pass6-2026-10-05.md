---
lens: technology-version-reality
kind: reviewer-gate-pass6
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
reviewed: '2026-10-05'
supersedes: 'reviews/review-version-pass5-2026-09-13.md'
mode: read-only-spine
---

# Reviewer Gate Pass 6 — Technology and Version Reality

**Verdict: REVISE.** The spine's AD-19 Stack and security-gap evidence is no longer a current description of the committed repository. This is a present-tense source contradiction, not simply a newer upstream release. No spine or source file was edited in this review.

**Retest update (2026-10-05):** The author corrected the Stack, narrative, and three security-gap rows after this initial verdict. The retest at the end of this file supersedes the stale-fact findings above: no high-severity version/repository-fact mismatch remains. AD-19 qualification evidence and the old OpenBao smoke-test asset remain open.

## Method and scope

I read the current spine, its latest export-custody edits, the committed root and submodule pins, deployment manifests, and the previous version review. The export-custody edits add no technology or version assertion. I used the prior review only to identify claims for a fresh retest. Local evidence comes from `git submodule status --cached`, `global.json`, `references/Hexalith.Builds/Props/Directory.Packages.props`, the AppHost project, Kubernetes and OpenBao manifests, and CI variables. Official release pages were checked for time-sensitive upstream claims. `HEAD` was `ec3601a5` during the review; the spine itself is a modified working-tree file.

## Critical and high findings

### VER6-01 — AD-19's supposedly re-derived Stack pins and gitlinks are stale. **HIGH** — autofix in the spine and memlog

`ARCHITECTURE-SPINE.md:266-282` asserts that the table contains exact current repository pins and that every fact-source gitlink was re-derived. The current committed reality is:

| Claim | Spine | Current committed source |
| --- | --- | --- |
| Builds gitlink | `bf8adb73` | `11ae79eb141593deb964297920456addce79a4a3`, `v4.29.1-18-g11ae79e` |
| EventStore source gitlink | `4502913c`, exact `v3.104.0` | `7dcc4756a6d04ccb46cbf7bc847ff8f4340f1cc7`, `v3.113.0-7-g7dcc4756` |
| EventStore package lane | `3.103.0` | `3.112.0` in the Builds catalog; source/package correspondence is still unmet, but the actual mismatch is now different |
| .NET SDK / AppHost SDK | `10.0.400` / `13.5.3` | `10.0.401` / `13.6.0` |
| CommunityToolkit Aspire Dapr / Dapr .NET SDK | `13.5.1-beta.751` / `1.18.7` | `13.6.0-preview.1.261001-0243` / `1.18.10`; CI Dapr runtime remains `1.18.2` |
| Redis clients | NRedisStack `1.7.4`, StackExchange.Redis `3.2.0` | `1.8.0`, `3.3.1` |
| Extraction / UI / telemetry | Kreuzberg `4.10.3`; FrontComposer `4.4.0`; Fluent UI `5.0.0-rc.5-26219.1`; OpenTelemetry `1.18.0` | `4.10.4`; `4.5.0`; `5.0.0`; core OpenTelemetry `1.19.1` (instrumentation pins vary) |
| PostgreSQL / OpenBao | `18.4-trixie`; OpenBao `2.6.0`, chart `0.28.5` | `18.6-trixie`; OpenBao `2.6.4`, chart `0.29.6` |

The Redis Stack `7.4.0-v8` and FalkorDB `4.12.0` Kubernetes tags, NFalkorDB `1.2.0`, MCP SDK `2.2.0`, and Google `gemini-embedding-001` / 768 configuration remain consistent with the inspected sources. The FrontComposer source gitlink is `3019dd30` (`v4.5.0-137-g3019dd30`), so its package/source evidence should be treated separately from the package pin. Re-derive every row from the current committed authorities, record the current gitlinks and lane correspondence, and append the reality check to `.memlog.md` before rendering the corrected spine.

### VER6-02 — Three Production security-gap rows describe upgrades already made. **HIGH** — autofix disposition after checking present evidence

The narrative at line 282 and ledger rows at lines 386-387 and 391 state that `global.json` still pins .NET SDK `10.0.400`, PostgreSQL is still `18.4`, and OpenBao still runs `2.6.0` with chart `0.28.5`. Their proposed upgrades are already present in the committed files: SDK `10.0.401`, PostgreSQL `18.6-trixie` with a digest, OpenBao image `2.6.4` with a digest and chart `0.29.6`. Microsoft's [.NET 10 page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) identifies `10.0.401` / runtime `10.0.12` as the September security release; [PostgreSQL security information](https://www.postgresql.org/support/security/) lists `18.6` as the current 18 security release; [OpenBao v2.6.4](https://github.com/openbao/openbao/releases/tag/v2.6.4) contains the October security fixes. Thus the rows' *pin-upgrade* claims are false now. Requalification or rerunnable evidence may still be owed under AD-20: inspect the current evidence paths and keep a blocker only for the precise unmet check, with no implied proof that a version bump alone passes Production. The floating .NET container base image and unqualified Redis Stack image remain separate live gaps.

## Medium and low findings

### VER6-03 — Upstream-current and prerelease descriptions have aged out. **MEDIUM** — update the dated context; do not force a blind major upgrade

The line-282 assertion that Fluent UI v5 has no GA is false: [Fluent UI Blazor v5.0.0](https://github.com/microsoft/fluentui-blazor/releases/tag/v5.0.0) shipped 2026-09-28 and the catalog now pins it. The CommunityToolkit pin is now a 13.6 preview; the old discussion of a 13.5 beta is no longer relevant. OpenBao's [chart releases](https://github.com/openbao/openbao-helm/releases) now include `0.30.0`, while the deployment uses `0.29.6`; the 0.30 release notes name a standalone-storage change, so 0.29.6 is a qualification decision rather than proof of being latest. [FalkorDB releases](https://github.com/FalkorDB/FalkorDB/releases) now include `v4.22.0` and `v6.0.1`; the spine's `v4.20.4` comparison and “four released minor lines” count are stale. Its pinned `4.12.0` remains real, and the v6 engine and replication change makes a major upgrade a separate compatibility decision. The [Redis Stack maintenance statement](https://github.com/redis-stack/redis-stack/blob/master/README.md) still supports the ended-maintenance finding; retain that finding without converting it to evidence of tested Redis 8 compatibility.

### VER6-04 — OpenBao smoke-test image still uses the old security release. **MEDIUM** — inspect test ownership and align the image

`deploy/openbao/smoke-test.yaml:32` pins `quay.io/openbao/openbao:2.6.0@sha256:900bb64d…` while `deploy/openbao/values.yaml:28`, CI `OPENBAO_IMAGE`, and production verification scripts pin `2.6.4@sha256:cf2340fc…`. The old image is part of a tracked deployment artifact, even though a search found no direct CI invocation of that YAML. Do not cite the smoke test as current `2.6.4` qualification evidence. Establish whether it is deployed; align it to the qualified image or explicitly archive it, then update any artifact-hash inventory that binds it.

## Retested findings and limits

- Pass-5's stale Builds/EventStore gitlinks remain a real issue but have been rechecked above against **new** commits and package values. The prior pass's exact hashes and version counts must not be copied forward.
- Pass-5's digest-guard and startup-auto-provision wording defects have been corrected in the current spine. The floating `ContainerBaseImage`, nonshared image digest set, and publisher-controlled routing problems remain ledgered; this review does not re-audit their security semantics.
- The current export-custody text in AD-16/AD-21 has no external version binding. It does not change this version verdict.
- This was a document reality review, not a build, runtime qualification, vulnerability reachability analysis, or approval of any release gate.

## Retest — 2026-10-05, after Stack and ledger correction

**Retest verdict for this lens: no remaining high stale-fact mismatch.** VER6-01, VER6-02, and VER6-03 are closed as document-reality findings. VER6-04 remains an acknowledged medium implementation/evidence gap. The spine still has active AD-19 blockers; this finding closure does not qualify Production or turn an evidence-owed row into `evidenced`.

| Initial finding | Current retest |
| --- | --- |
| VER6-01 — stale Stack and gitlinks | **Closed.** Lines 274-291 now match the committed `global.json`, AppHost SDK `13.6.0`, Builds gitlink `11ae79eb`, EventStore source gitlink `7dcc4756`, Builds package catalog (`EventStore 3.112.0`, Dapr .NET `1.18.10`, CommunityToolkit `13.6.0-preview.1.261001-0243`, Redis clients `1.8.0` / `3.3.1`, Kreuzberg `4.10.4`, FrontComposer `4.5.0`, Fluent UI `5.0.0`, OpenTelemetry core `1.19.1`), and the inspected deployment pins. The source/package mismatch is stated without claiming correspondence. |
| VER6-02 — obsolete security-pin rows | **Closed as stale facts.** The .NET, PostgreSQL, and OpenBao rows now describe the upgraded pins and precisely claim that current same-profile qualification evidence is still owed. Their `blocker` dispositions are consistent with AD-20 until rerunnable evidence is recorded. |
| VER6-03 — aged upstream-current wording | **Closed.** The current prose calls Fluent UI v5 GA, the CommunityToolkit 13.6 pin a preview, and FalkorDB a migration candidate; it no longer makes an old “latest” chart or Falkor release-count assertion. |
| VER6-04 — smoke-test on OpenBao 2.6.0 | **Open, acknowledged.** Both Stack and ledger name `deploy/openbao/smoke-test.yaml` as old, and the OpenBao gap requires alignment or retirement before citing same-profile evidence. The artifact itself has not changed. |

I re-ran the source comparisons against the same committed gitlinks and manifests, searched the current spine for the obsolete `10.0.400`, `18.4-trixie`, `0.28.5`, `v4.20.4`, `13.5.3`, `1.18.7`, and old gitlink claims, and found no stale hit. The unchanged Redis Stack `7.4.0-v8`, FalkorDB `4.12.0`, CI Dapr runtime `1.18.2`, NFalkorDB `1.2.0`, and MCP SDK `2.2.0` claims still match inspected pins. No spine file was edited by this reviewer.

## Final handoff retest — 2026-10-05, after grammar and G6 updates

**Scope:** current working-tree `ARCHITECTURE-SPINE.md`, `prd.md`, and `addendum.md` against committed pins and gitlinks. The new identifier grammar, export, and evidence-only G6 language adds no technology-version assertion. The spine's Stack and source-lane rows remain accurate: Builds `11ae79eb`, EventStore `7dcc4756`, FrontComposer `3019dd30`, package EventStore `3.112.0`, root SDK `10.0.401`, AppHost `13.6.0`, PostgreSQL `18.6`, and main OpenBao `2.6.4` / chart `0.29.6` still match the committed files. No new high spine pin mismatch was found.

**New planning-doc reality finding — VER6-05, MEDIUM; correct before final handoff.** The PRD Release decision record (`prd.md:218` at this retest) still cites DW-728 as a present L2 blocker because the `eventstore` resource “cannot start under SDK 10.0.400-only environments.” That is the historical environment from the still-skipped test at `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventStoreAppHostResourceGraphTests.cs:126-145`, whose skip text says the EventStore submodule requires SDK `10.0.302` while the root requires `10.0.400`. Both **committed** `global.json` files now pin SDK `10.0.401`, and `dotnet --version` returns `10.0.401` from both repository roots on this machine. The stated mismatch is no longer a current reason for the L2 no-go; the skipped test must be re-evaluated before anyone claims the resource is healthy. The PRD can preserve DW-728 as a historical unresolved test/evidence item and keep L2 no-go on the independently current grounds: no full-stack publish proof, no sample, and no current L2 run. The addendum's `10.0.302` versus `10.0.400` example is labelled as historical in its Language/SDK section (`addendum.md:99`), so it does not create another current-pin claim.

**Closure retest, later on 2026-10-05:** The PRD L2 row now calls DW-728's SDK-pin mismatch historical, accurately states that current root and EventStore `global.json` both pin `10.0.401`, cites the still-skipped test, and leaves EventStore startup/L2 proof unverified. VER6-05's stale planning wording is **closed**. The skipped test still needs a fresh run or scoped outcome before it can provide evidence; no such run was part of this review.

**Remaining finality blockers for this lens:** the acknowledged OpenBao `2.6.0` smoke-test mismatch and the exact-profile AD-19 qualification gaps remain open. These are evidence/implementation obligations, not stale statements in the final spine or PRD. This review supplies no runtime or release-gate evidence. No source or planning document was edited by this reviewer.
