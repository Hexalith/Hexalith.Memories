# Reviewer Gate — Technology / Version Reality Lens

**VERDICT: PASS-WITH-FINDINGS**

- Lens: Technology / Version Reality
- Intent: Validate (read-only critique; no spine or repository file modified)
- Target: `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` — "## Stack" table (lines 196–212) and the caveat paragraph (line 214)
- Review date: 2026-09-12 · Spine finalized: 2026-09-09
- Severity counts: **1 critical · 3 high · 4 medium · 2 low**

## Scope and method

Two-sided check as mandated:

- **(A) Repository reality** — every Stack row compared against `global.json`, `Directory.Build.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`, `tools/release-packages.json`, `deploy/**`, `src/Hexalith.Memories.AppHost/**`, `.github/workflows/{ci,nightly}.yml`, and read-only `git ls-tree` / `git -C … rev-parse`. No submodule was initialized or updated; no build, restore, or test was run.
- **(B) Upstream currentness** — every named technology checked against live upstream sources (GitHub Releases API, GitHub Security Advisories, `api.nuget.org` flat-container index, `raw.githubusercontent.com/dotnet/core` release notes, Docker Hub tags API). Every claim below carries a source. Nothing in this review is answered from training data; there were **no** web-access failures, so there are **no** UNVERIFIED rows.

**A structural caveat that shapes every "Repo reality" cell.** `references/Hexalith.Builds` is a *dirty gitlink*: `HEAD` records `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, but the checked-out worktree is at `fa6472788c14301c2c91c6beb85a8215aae1022c` ("build(deps): update packages and .NET SDK"). The spine's pins were transcribed from the **committed** gitlink and match it faithfully. They do **not** match the tree a developer or build resolves on this machine today. Both values are reported below; a "DRIFT" caused only by this uncommitted delta is labelled *(worktree)*.

## Stack table verification

| Technology | Spine pin | Repo reality | Latest upstream | Status | Source |
| --- | --- | --- | --- | --- | --- |
| .NET SDK / target / language | `10.0.400` / `net10.0` / C# 14 | `10.0.400` (`rollForward: latestFeature`); `net10.0`; `LangVersion 14` | SDK **10.0.401** / runtime **10.0.12**, 2026-09-08, **security** (6 CVEs) | **SECURITY** | `global.json:3`; `Directory.Build.props:4-5`; [dotnet/core releases.json](https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/releases.json) |
| Aspire AppHost SDK | `13.5.3` | `Aspire.AppHost.Sdk/13.5.3`; `Aspire.Hosting` 13.5.3 | 13.5.3 (newest stable) | MATCH | `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj:1`; `Directory.Packages.props:113`; [nuget](https://api.nuget.org/v3-flatcontainer/aspire.hosting/index.json) |
| CommunityToolkit Aspire Dapr hosting | `13.5.0-preview.1.260825-0345` | same (committed **and** worktree) | **13.5.1-beta.751** newest prerelease; newest **stable** in the 13.x line is **13.0.0** — no stable 13.5.x exists | **DRIFT** (preview) | `Directory.Packages.props:136`; [nuget](https://api.nuget.org/v3-flatcontainer/communitytoolkit.aspire.hosting.dapr/index.json) |
| Dapr .NET SDK | `1.18.5` | committed `1.18.5`; **worktree `1.18.7`** | 1.18.7 (newest stable) | **DRIFT** *(worktree)* | `Directory.Packages.props:139-146`; [nuget](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json) |
| Dapr CI runtime | `1.18.2` | `1.18.2` (CLI `1.18.0`) | **1.18.4** (2026-09-09); 1.18.3 (2026-08-14) | **DRIFT** | `.github/workflows/ci.yml:16`; `.github/workflows/nightly.yml:13`; [dapr releases](https://github.com/dapr/dapr/releases/tag/v1.18.4) |
| Hexalith.EventStore package lane | `3.103.0` | `3.103.0` (committed and worktree) | 3.103.0 (newest) | MATCH | `Directory.Packages.props:8`; [nuget](https://api.nuget.org/v3-flatcontainer/hexalith.eventstore.gateway/index.json) |
| Hexalith.EventStore source-lane gitlink | `b1c00a79d1d34aa7ba3f58046a7844e8b3d57fd6` | `HEAD` = **`6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20`**; worktree = **`a568af4ec963d017ed4343a518d4fbd7d444ad84`** | n/a | **DRIFT** | `git ls-tree HEAD references/Hexalith.EventStore` |
| Redis Stack Server | `7.4.0-v8` | digest-pinned `redis/redis-stack-server:7.4.0-v8@sha256:798ab84d…` | `7.4.0-v8` is the **terminal** 7.4 tag, image not rebuilt since **2025-11-03**; Redis Stack maintenance ended Dec 2025; successor is Redis 8 (Redis Open Source) | **EOL** | `deploy/kubernetes/base/redis-statefulset.yaml:37`; [Docker Hub tags](https://hub.docker.com/v2/repositories/redis/redis-stack-server/tags) |
| NRedisStack | `1.7.4` | `1.7.4` | 1.7.4 (newest) | MATCH | `Directory.Packages.props:258`; [nuget](https://api.nuget.org/v3-flatcontainer/nredisstack/index.json) |
| StackExchange.Redis | `3.1.31` | committed `3.1.31`; **worktree `3.2.0`** | 3.2.0 (newest stable) | **DRIFT** *(worktree)* | `Directory.Packages.props:188`; [nuget](https://api.nuget.org/v3-flatcontainer/stackexchange.redis/index.json) |
| FalkorDB | `4.12.0` | digest-pinned `falkordb/falkordb:v4.12.0@sha256:7927eb19…` | **v4.20.4** (2026-08-20) | **DRIFT** | `deploy/kubernetes/base/falkordb-statefulset.yaml:37`; [FalkorDB releases](https://api.github.com/repos/FalkorDB/FalkorDB/releases) |
| NFalkorDB | `1.2.0` | `1.2.0` | 1.2.0 (newest) | MATCH | `Directory.Packages.props:257`; [nuget](https://api.nuget.org/v3-flatcontainer/nfalkordb/index.json) |
| OpenBao image | `2.6.0` | digest-pinned `quay.io/openbao/openbao:2.6.0@sha256:900bb64d…` in **both** k8s and AppHost | **2.6.2** (2026-08-18, security). **GHSA-rh46-vc3j-w2w3**, Critical, **CVSS 9.2**, affects `< 2.6.2` → **2.6.0 is affected** | **SECURITY** | `deploy/openbao/smoke-test.yaml:32`; `src/Hexalith.Memories.AppHost/OpenBaoDevelopmentProfile.cs:18`; [GHSA-rh46-vc3j-w2w3](https://github.com/openbao/openbao/security/advisories/GHSA-rh46-vc3j-w2w3) |
| OpenBao production Helm chart | `0.28.5` | `0.28.5` | **0.29.4** (2026-09-03); 0.28.6 (2026-07-22) | **DRIFT** | `deploy/openbao/values.yaml:2`; [openbao-helm releases](https://api.github.com/repos/openbao/openbao-helm/releases) |
| Model Context Protocol SDK | `2.2.0` | `2.2.0` | 2.2.0 (newest) | MATCH | `Directory.Packages.props:248,250`; [nuget](https://api.nuget.org/v3-flatcontainer/modelcontextprotocol/index.json) |
| Kreuzberg | `4.10.2` | committed `4.10.2`; **worktree `4.10.3`** | 4.10.3 newest stable (5.0.0-rc.35 is preview-only) | **DRIFT** *(worktree)* | `Directory.Packages.props:168`; [nuget](https://api.nuget.org/v3-flatcontainer/kreuzberg/index.json) |
| Hexalith.FrontComposer | `4.4.0` | `4.4.0` | 4.4.0 (newest) | MATCH | `Directory.Packages.props:9`; [nuget](https://api.nuget.org/v3-flatcontainer/hexalith.frontcomposer.shell/index.json) |
| Fluent UI Blazor | `5.0.0-rc.5-26219.1` | `5.0.0-rc.5-26219.1` | `5.0.0-rc.5-26219.1` is the newest published version — **no 5.0.0 GA exists** | MATCH (RC) | `Directory.Packages.props:226-227`; [nuget](https://api.nuget.org/v3-flatcontainer/microsoft.fluentui.aspnetcore.components/index.json) |
| OpenTelemetry | `1.18.0` | `1.18.0` (instrumentation for gRPC/StackExchangeRedis on `1.18.0-beta.1`) | 1.18.0 (newest stable) | MATCH | `Directory.Packages.props:266-275`; [nuget](https://api.nuget.org/v3-flatcontainer/opentelemetry/index.json) |
| AppHost Redis container (caveat's "floating" claim) | claimed as alignment gap | `AddContainer("memories-vectors", "redis/redis-stack")` — **no tag, no digest** | n/a | claim CONFIRMED | `src/Hexalith.Memories.AppHost/Program.cs:137` |
| AppHost FalkorDB container (caveat's "floating" claim) | claimed as alignment gap | `AddContainer("memories-graphs", "falkordb/falkordb")` — **no tag, no digest** | n/a | claim CONFIRMED | `src/Hexalith.Memories.AppHost/Program.cs:278` |

**Provenance rows checked and clean:** `tools/release-packages.json` lists 9 packable projects and 6 non-packable ones consistent with the spine's Structural Seed; no version literals live there, so it contributes no pin to verify. All three `registry.hexalith.com/*` images carry the CI-substituted `0.0.0` placeholder tag, which is the expected pre-release form and not a floating-tag defect.

## Findings

### VER-01 — critical — .NET runtime/SDK pin predates a same-week security release, and the caveat does not mention it

**Evidence.** `global.json:3` pins SDK `10.0.400`, which is the SDK shipped with **runtime 10.0.11** (2026-08-11). On **2026-09-08 — one day before the spine was finalized** — Microsoft shipped **10.0.12 / SDK 10.0.401**, flagged `"security": true`, fixing **CVE-2026-69439, CVE-2026-71328, CVE-2026-69522, CVE-2026-69304, CVE-2026-58649, CVE-2026-69806** ([dotnet/core releases.json](https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/releases.json)). The committed Builds gitlink correspondingly pins the whole `Microsoft.AspNetCore.*` family at `10.0.11`; the uncommitted worktree already moves them to `10.0.12`.

This is the single most consequential gap in the lens. The spine's caveat paragraph enumerates exactly three security/EOL concerns — OpenBao, Redis Stack, FalkorDB — and asserts that "Production qualification is blocked until OpenBao is upgraded". A reader is entitled to conclude that OpenBao is the *only* security blocker. It is not: the platform runtime itself is one patch behind a six-CVE security release. Row 200 of the Stack table presents `10.0.400` with no annotation at all, which is precisely the "asserted rather than reality-checked" pattern the mandate targets.

Mitigating but not exculpating: `rollForward: latestFeature` means a machine with 10.0.401 installed will silently use it, so the *effective* SDK may already be patched. That makes the recorded pin misleading in the other direction — it neither guarantees 10.0.400 nor documents that the resolved version floats. Container base images and the NuGet `Microsoft.AspNetCore.*` pins do **not** roll forward, so the 10.0.11 exposure is real for the committed tree.

**Action.** Add .NET to the caveat's security list. Bump `global.json` to `10.0.401` and land the worktree's `10.0.11 → 10.0.12` ASP.NET Core package bump, or record a time-bounded exception naming the six CVEs on the same footing as the OpenBao exception. State explicitly whether `rollForward: latestFeature` is intended to be the patch mechanism; if it is, the Stack table should say the SDK floats within the 10.0.4xx band rather than presenting `10.0.400` as an exact pin.

### VER-02 — high — the OpenBao verdict is correct, but the remediation target it names was already two minor lines stale on the day it was written

**Evidence.** The substance of the spine's OpenBao claim **verifies**: `2.6.0` is confirmed affected by **GHSA-rh46-vc3j-w2w3** ("Internal Operation Dispatching Leads to Token Creation"), severity **Critical**, **CVSS v4 9.2**, affected range `< 2.6.2`, patched in **2.6.2** (2026-08-18). Calling production "blocked" is proportionate, and 2.6.2 remains the newest stable (`v2.6.3` → HTTP 404; only `v2.7.0-beta20260909` is newer, a prerelease).

The **chart** half is stale. The spine says "the `0.28.6` chart is assessed". Chart `0.28.6` shipped 2026-07-22, but the 0.29 line opened 2026-08-10 and **0.29.4 shipped 2026-09-03 — six days before the spine was finalized** ([openbao-helm releases](https://api.github.com/repos/openbao/openbao-helm/releases)). Naming 0.28.6 as the assessment target was already wrong at authoring time, not merely overtaken since. Line 306's Alignment Gaps row repeats the same 0.28.5-centric framing.

Note also an open upstream dependency-vulnerability tracker against the 2.6.x branch, **GO-2026-5970** ([openbao/dependency-vulnerabilities#231](https://github.com/openbao/dependency-vulnerabilities/issues/231)), which the requalification should sweep in.

**Action.** Restate the target as "upgrade to OpenBao ≥ 2.6.2 and assess the current chart line (0.29.x, presently 0.29.4)". Do not re-pin to 0.28.6. Verify GO-2026-5970 status as part of the same requalification. Keep the digest pin discipline — it is correctly applied in both `deploy/openbao/smoke-test.yaml:32` and `OpenBaoDevelopmentProfile.cs:18`.

### VER-03 — high — the recorded EventStore source-lane gitlink matches nothing in the repository

**Evidence.** The spine records `b1c00a79d1d34aa7ba3f58046a7844e8b3d57fd6`. Read-only git says otherwise:

```
$ git ls-tree HEAD references/Hexalith.EventStore
160000 commit 6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20  references/Hexalith.EventStore
$ git -C references/Hexalith.EventStore rev-parse HEAD
a568af4ec963d017ed4343a518d4fbd7d444ad84
```

Three distinct SHAs. The spine's value is neither the committed gitlink nor the checked-out worktree commit. Unlike the package drifts in VER-07, this cannot be explained by the dirty-worktree delta — the spine value appears nowhere. Either it was transcribed from a superseded state, from a different checkout, or asserted. A source-lane SHA whose whole purpose is byte-exact reproducibility is worthless if it does not resolve, and it silently defeats any later attempt to reproduce the qualification baseline.

**Action.** Re-derive the SHA with `git ls-tree HEAD references/Hexalith.EventStore` and correct the Stack table to `6b0247ac…`. State in the row which side is authoritative (committed gitlink vs. worktree), because the two currently disagree. Separately, resolve the ` M references/Hexalith.EventStore` dirty gitlink before any qualification run so the recorded value stays true.

### VER-04 — medium — "qualified-but-behind" materially understates the FalkorDB gap

**Evidence.** The deployment digest-pins `falkordb/falkordb:v4.12.0`. Upstream stable is **v4.20.4** (2026-08-20) — **eight minor lines** ahead, with 4.20.x alone carrying replica-crash fixes and graph-loading performance work ([FalkorDB releases](https://api.github.com/repos/FalkorDB/FalkorDB/releases)). "Qualified-but-behind … needs a scheduled compatibility/security review" reads as a minor lag; an eight-minor-line gap on a stateful graph store is a migration project with real data-format and query-behavior risk, and it belongs in the same tier as the Redis 8 plan the spine already schedules at line 321.

On advisories: the only FalkorDB-vendor CVE surfaced is **CVE-2026-10130** (CVSS 8.8), which affects **QueryWeaver**, a separate FalkorDB product, not the graph-engine image this repository deploys ([OpenCVE](https://app.opencve.io/cve/CVE-2026-10130)). No engine CVE was found against 4.12.0 — so the spine is not wrong to omit a security flag here, only to underweight the version distance.

**Action.** Quantify the gap in the caveat ("4.12.0 → current 4.20.4, eight minor lines") and give the review a date rather than leaving it "scheduled". Confirm CVE-2026-10130 non-applicability explicitly so the exclusion is recorded rather than assumed.

### VER-05 — medium — a preview and an RC sit in a production-bound stack with no verdict attached

**Evidence.** Two prerelease dependencies are pinned, and the caveat paragraph is silent on both:

- `CommunityToolkit.Aspire.Hosting.Dapr` `13.5.0-preview.1.260825-0345` (`Directory.Packages.props:136`). Upstream has moved on to `13.5.1-beta.751`, and — importantly — the flat-container index shows the **only** stable release in the entire 13.x line is `13.0.0`. There is no stable 13.5.x to upgrade to. This is a genuine constraint, not an oversight in dependency hygiene, but that is exactly what makes it worth stating: the stack has a production-bound component with no supported stable option, and Dapr hosting is on the critical path for the workflow architecture the spine builds on.
- `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1` (`Directory.Packages.props:226-227`). Verified as the newest published version — no 5.0.0 GA exists — so the pin is current, but it is still an RC.

The mandate asks for a verdict on preview dependencies in a production-bound stack. The spine offers none. Mitigating for Fluent UI: the spine itself scopes `Hexalith.Memories.Web` as a "conformance specimen, not product UI", which materially lowers the blast radius — but that reasoning is not written down next to the pin.

**Action.** Add one sentence to the caveat: the Dapr hosting integration is on a preview with no stable 13.5.x available upstream, and Fluent UI is on an RC with no GA available; both are accepted because no stable alternative exists, with the Fluent UI exposure bounded by the Web project's specimen-only scope. Decide separately whether to track `13.5.1-beta.751`, since the pinned preview is now superseded even within the prerelease channel.

### VER-06 — medium — the Dapr CI runtime pin is two patch releases behind

**Evidence.** `.github/workflows/ci.yml:16` and `.github/workflows/nightly.yml:13` both set `DAPR_RUNTIME_VERSION: '1.18.2'`. Upstream on the 1.18 line: **1.18.3** (2026-08-14) and **1.18.4** (2026-09-09) both exist and are stable (`v1.18.5`+ → HTTP 404, so 1.18.4 is the head of the line). 1.18.3's notes describe a CloudEvent trace-field handling defect where a publisher-controlled non-string `traceid` survives to delivery — a correctness/trust-boundary bug rather than a filed CVE.

Calibration note: 1.18.4 landed 2026-09-09, so it is arguably contemporaneous with finalization; **1.18.3 (2026-08-14) is not** and was available for nearly a month. Also worth recording explicitly, because it looks like an inconsistency and is not one: Dapr .NET SDK `1.18.7` legitimately outruns runtime `1.18.4` — the SDK and runtime patch on independent cadences.

**Action.** Bump `DAPR_RUNTIME_VERSION` to `1.18.4` in both workflows, or record why the CI runtime is deliberately held at 1.18.2. Given the architecture's dependence on Dapr workflows and W3C trace context across durable boundaries (Operational Boundaries, Observability row), the 1.18.3 trace-field fix is directly relevant.

### VER-07 — medium — four Stack rows are true of `HEAD` but false of the working tree

**Evidence.** `references/Hexalith.Builds` is a dirty gitlink: `HEAD` = `a32cb422…`, worktree = `fa647278…` ("build(deps): update packages and .NET SDK"). Diffing the two `Props/Directory.Packages.props`:

| Pin | Committed (`a32cb42`) = spine value | Worktree (`fa64727`) |
| --- | --- | --- |
| `Dapr.*` (8 packages) | 1.18.5 | **1.18.7** |
| `StackExchange.Redis` | 3.1.31 | **3.2.0** |
| `Kreuzberg` | 4.10.2 | **4.10.3** |
| `Microsoft.AspNetCore.*` (~20 packages) | 10.0.11 | **10.0.12** |

The spine is internally honest here — it says "These are exact repository pins", and against the committed gitlink every one of these four is exact. But the assertion is ambiguous about *which* repository state it describes, and on this machine the two states disagree on ~30 package versions including the security-relevant ASP.NET Core bump behind VER-01. A qualification evidence trail that cannot say which side it measured is not reproducible.

**Action.** State in the caveat that the pins are read from the **committed** `references/Hexalith.Builds` gitlink `a32cb422…`, and name that SHA in the Stack table the way the EventStore gitlink is named. Then either commit or revert the dirty gitlink before qualification, so "repository pins" has one unambiguous referent.

### VER-08 — low — the Redis Stack EOL claim verifies, and is if anything understated

**Evidence.** The spine's claim is **correct**: Redis Stack maintenance releases for 6.2/7.2/7.4 ended **December 2025**, and Redis 8 (Redis Open Source) folded Search/JSON/TimeSeries/Bloom into the core distribution, making it the successor. Two details sharpen it: `7.4.0-v8` is the **terminal** 7.4 tag, and the image has not been rebuilt since **2025-11-03** ([Docker Hub tags](https://hub.docker.com/v2/repositories/redis/redis-stack-server/tags)) — roughly ten months of accumulated unpatched base-OS CVEs on top of the frozen application line. Redis Software 7.4 additionally reaches formal EOL **2026-11-30**, which puts a hard external date on the migration the spine currently schedules only as "before the next Production support/security window" (line 321).

Recording this as *low* rather than higher because the spine already identifies the problem, mandates a migration plan, and digest-pins the image — the finding is a sharpening, not a correction.

**Action.** Attach the 2026-11-30 EOL date to the line-321 schedule row so the Redis 8 migration has an external deadline rather than a relative one, and note the ten-month image-rebuild freeze as the base-image CVE exposure it is.

### VER-09 — low — the caveat's "floating defaults" claim is accurate, and both instances are confirmed

**Evidence.** The caveat asserts "floating AppHost/Aspire Redis and Falkor defaults are alignment gaps". Both verify exactly as described: `Program.cs:137` calls `AddContainer("memories-vectors", "redis/redis-stack")` and `Program.cs:278` calls `AddContainer("memories-graphs", "falkordb/falkordb")` — neither carries a tag or a digest. Conversely, every production-path image *is* digest-pinned (`redis-statefulset.yaml:37`, `falkordb-statefulset.yaml:37`, `access-telemetry-postgresql.yaml:144`, `smoke-test.yaml:32`), and the AppHost's OpenBao resource pins a digest too (`OpenBaoDevelopmentProfile.cs:18`) — so the claim "Image digests remain mandatory in production" holds. No image the spine claims is pinned was found floating, and no floating image was found that the spine failed to claim as a gap.

**Action.** None required for accuracy. Optionally tag the two AppHost containers to match the production digests so local composition and production do not silently diverge on engine version — a real risk given VER-04 and VER-08 put both engines mid-migration.

## Assessment of the caveat paragraph

The mandate asks whether the caveat "accurately and completely describes the risk". Split verdict:

**Accurate where it speaks.** Every affirmative claim in the paragraph verifies against live upstream sources. OpenBao 2.6.0 really is behind a Critical CVSS 9.2 advisory; Redis Stack 7.4.0-v8 really is an ended-maintenance line; FalkorDB really is behind; the floating AppHost defaults really are floating; digests really are mandatory in production and really are applied. The opening disclaimer — "These are exact repository pins, not claims that every pin is the newest safe release" — is an unusually honest framing and is doing genuine work. This paragraph was not asserted from training data.

**Incomplete in three specific ways.**

1. **A security blocker is missing entirely (VER-01).** By naming OpenBao as *the* thing blocking production qualification and listing only three concerns, the paragraph implies the rest of the stack is not security-relevant. The .NET runtime was one patch behind a six-CVE security release the day before finalization. That omission is the reason this review is PASS-WITH-FINDINGS rather than PASS.
2. **A remediation target was stale at authoring time (VER-02).** "the `0.28.6` chart is assessed" pointed at a chart line already superseded by 0.29.4 six days earlier. The security *judgment* was researched; the *remediation target* was not re-checked.
3. **Prerelease dependencies get no verdict (VER-05).** A preview Dapr hosting package and an RC UI library sit in a production-bound stack, and the paragraph — which is the spine's designated place for version-risk commentary — does not mention either.

**Two calibration errors, both in the direction of understatement.** "Qualified-but-behind" for an eight-minor-line FalkorDB gap (VER-04), and a Redis Stack framing that omits both the ten-month image-rebuild freeze and the hard 2026-11-30 EOL date (VER-08). Nothing in the paragraph **overstates** risk.

**Changed since 2026-09-09?** Very little, which is itself the point — most of what this review flags was already true when the spine was finalized. The only genuinely post-finalization movement is Dapr runtime **1.18.4** (2026-09-09, same day) and Dapr **1.17.14** (2026-09-10, an unrelated LTS line). The .NET security release (09-08), OpenBao chart 0.29.4 (09-03), OpenBao 2.6.2 (08-18), FalkorDB 4.20.4 (08-20), and Dapr 1.18.3 (08-14) all predate finalization. The caveat does not "now miss" these — it missed them then.

## Why PASS-WITH-FINDINGS rather than FAIL

No named technology has ceased to exist, been renamed out from under the spine, or been found unfit for its role. The two rows most likely to be stale — OpenBao and Redis Stack — were demonstrably reality-checked, and their verdicts hold. Eleven of twenty rows are exact MATCHes against both the repository and the newest upstream release. Web access succeeded for every item, so nothing is UNVERIFIED.

What keeps it from PASS is VER-01: an unflagged six-CVE security gap in the platform runtime, inside a paragraph whose explicit job is to enumerate the security gaps. VER-02 and VER-03 compound it — one remediation target that was stale on arrival, and one recorded SHA that resolves to nothing. Those are corrections to the record, not architecture defects, which is why they do not carry the document to FAIL.

## Verification commands used (all read-only)

```
git ls-tree HEAD references/Hexalith.EventStore
git ls-tree HEAD references/Hexalith.Builds
git -C references/Hexalith.EventStore rev-parse HEAD
git -C references/Hexalith.Builds rev-parse HEAD
git -C references/Hexalith.Builds diff a32cb42 fa64727 -- Props/Directory.Packages.props
git status --short references/
```

No build, restore, test, submodule init/update, or write to any file other than this review.
