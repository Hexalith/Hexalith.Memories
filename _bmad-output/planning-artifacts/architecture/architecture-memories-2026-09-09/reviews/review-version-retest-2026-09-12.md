---
lens: technology-version-reality
kind: reviewer-gate-retest
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
scope: '## Stack table and the caveat paragraph immediately following it (both rewritten 2026-09-12)'
reviewed: '2026-09-12'
supersedes: 'reviews/review-version-reality.md'
mode: read-only
---

# Reviewer Gate Re-run — Technology / Version Reality (2026-09-12)

**Verdict: PASS-WITH-FINDINGS.**

The two gitlink corrections and the .NET and OpenBao blocker statements hold up under live
verification. Every pin in the `## Stack` table matches the committed `references/Hexalith.Builds`
catalog exactly, and the new "committed gitlink" disambiguation is not merely correct but
load-bearing. However, of the four genuinely new assertions added in this revision, **one is
factually wrong against the repository and names a model Google decommissioned eight months ago**,
and **one adds a component carrying 28 unfixed CVEs to the Stack table with no risk annotation** —
which makes the caveat's own "two security blockers, not one" headline an understatement for the
second revision running. Two further quantifications overshoot.

---

## Verification Table

| # | Claim | Spine now says | Verified reality | Status | Source |
| --- | --- | --- | --- | --- | --- |
| 1 | EventStore source-lane gitlink | `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20` | `git ls-tree HEAD references/Hexalith.EventStore` returns `160000 commit 6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20`. Prior `b1c00a79…` is gone. | **CONFIRMED** | `git ls-tree HEAD references/Hexalith.EventStore` |
| 1b | Caveat wording "re-derived from `git ls-tree HEAD …` at each revision" | Process statement | Accurate as written and true today. Matches AD-19's re-derivation duty (spine:176). | **CONFIRMED** | ARCHITECTURE-SPINE.md:232, :176 |
| 1c | Gitlink ↔ package-lane correspondence (AD-19) | Table pairs `3.103.0` with `6b0247ac` | `git submodule status --cached` describes `6b0247ac` as `v3.103.0-40-g6b0247ac` — **40 commits ahead** of tag `v3.103.0`. Not strict correspondence. | **OVERSTATED** | `git submodule status --cached references/Hexalith.EventStore` |
| 2 | Pins read from **committed** `references/Hexalith.Builds` gitlink `a32cb422` | Disambiguation claim | `git ls-tree HEAD references/Hexalith.Builds` → `a32cb422749352cce8dec948aa3e78c8f00eb4cf` (`v4.27.2-10-ga32cb42`). | **CONFIRMED** | `git ls-tree HEAD references/Hexalith.Builds` |
| 2b | Every table pin matches that catalog | 12 pins | All verified against `git show a32cb422:Props/Directory.Packages.props`: Aspire `13.5.3`; CT Dapr `13.5.0-preview.1.260825-0345`; Dapr `1.18.5`; EventStore `3.103.0`; NRedisStack `1.7.4`; StackExchange.Redis `3.1.31`; NFalkorDB `1.2.0`; MCP `2.2.0`; Kreuzberg `4.10.2`; FrontComposer `4.4.0`; FluentUI `5.0.0-rc.5-26219.1`; OpenTelemetry `1.18.0`. Zero mismatches. | **CONFIRMED** | `references/Hexalith.Builds` @ `a32cb422`, `Props/Directory.Packages.props` |
| 2c | Dirty worktree submodule is pre-existing/expected | Implicit | Worktree is at `fa647278` (`v4.27.3`). The disambiguation **matters**: that revision carries `Microsoft.AspNetCore.* 10.0.12`, `Dapr 1.18.7`, `StackExchange.Redis 3.2.0`, `Kreuzberg 4.10.3`. 75 pin lines differ. | **CONFIRMED (and materially load-bearing)** | `git diff a32cb422 fa647278 -- Props/Directory.Packages.props` |
| 3 | .NET `10.0.400` carries runtime `10.0.11` | Security-blocker premise | `releases.json`: SDK `10.0.400` ↔ release-version `10.0.11`, released 2026-08-11. | **CONFIRMED** | https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/releases.json |
| 3b | `10.0.12` / SDK `10.0.401` released 2026-09-08 fixing **six** CVEs | Six CVEs | Release notes list exactly six: CVE-2026-69439, -71328, -69522, -69304, -58649, -69806. Date 2026-09-08. SDKs `10.0.401` and `10.0.112`. (A secondary blog claimed seven by folding in CVE-2026-66822, which is not in the 10.0.12 list — the spine's count is the correct one.) | **CONFIRMED** | https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md |
| 3c | `rollForward: latestFeature` "floats the resolved SDK **within the 10.0.4xx band**" | Mechanism | Wrong policy described. MS docs: `latestFeature` = "the **highest installed feature band** and patch level that matches the requested major and minor" — it floats **across** feature bands (10.0.5xx would be selected). "Within the 4xx band" is `latestPatch` behaviour. Conclusion unaffected. | **NEWLY-WRONG** | https://learn.microsoft.com/dotnet/core/tools/global-json#globaljson-schema |
| 3d | Container base images and pinned `Microsoft.AspNetCore.*` do **not** roll forward | Exposure claim | Committed catalog pins `Microsoft.AspNetCore.* 10.0.11` throughout. Correct — central package management fixes them; no roll-forward applies. | **CONFIRMED** | `Props/Directory.Packages.props` @ `a32cb422` (Authorization/JwtBearer/OpenApi/… all `10.0.11`) |
| 3e | Remediation availability (unstated) | — | The already-checked-out Builds worktree revision `fa647278` (`v4.27.3`) **already carries `10.0.12`**. Remediation is one submodule bump, not an upstream wait. | **CONFIRMED (omission)** | `git show fa647278:Props/Directory.Packages.props` |
| 4 | OpenBao remediation "at least `2.6.2`" | 2.6.2 is the security-fixed line | `v2.6.2` released 2026-08-18 and is the newest **stable**; the only newer tag is `v2.7.0-beta20260909` (prerelease). Not moved. | **CONFIRMED** | https://github.com/openbao/openbao/releases |
| 4b | Current chart line "`0.29.x`, presently `0.29.4`" | 0.29.4 newest chart | `openbao-0.29.4` is still marked Latest; no 0.29.5+. Chart `0.29.2` is the one that bumped the app to `v2.6.2`. | **CONFIRMED** | https://github.com/openbao/openbao-helm/releases |
| 4c | Repository pins `2.6.0` / chart `0.28.5` | Ledger row premise | `deploy/openbao/values.yaml:2` (chart `0.28.5`, digest-pinned) and `:27` (image tag `2.6.0@sha256:900bb64d…`); `deploy/openbao/smoke-test.yaml:32` same image. | **CONFIRMED** | `deploy/openbao/values.yaml:2,27` |
| 4d | Sweep GO-2026-5970 | Advisory is real and relevant | Real: "Infinite loop on invalid input in golang.org/x/text", tracked for `openbao/openbao release/2.6.x` in openbao/dependency-vulnerabilities#231 (opened 2026-07-22, now closed), fixed in `golang.org/x/text v0.39.0`. Sweep instruction is sound. | **CONFIRMED** | https://github.com/openbao/dependency-vulnerabilities/issues/231 |
| 5 | FalkorDB "eight minor lines behind `v4.20.4`" | Eight lines | `v4.20.4` is latest (2026-08-20) — confirmed. But FalkorDB ships **even-numbered** stable minor lines: tags show 4.0, 4.2, 4.3, 4.4, 4.6, 4.8, 4.10, 4.12, 4.14, 4.16, 4.18, 4.20. Between the pin and head there are **four** released minor lines (4.14, 4.16, 4.18, 4.20). Eight is the version-number distance, not a count of lines. | **OVERSTATED (2×)** | GitHub API `/repos/FalkorDB/FalkorDB/tags`; `deploy/kubernetes/base/falkordb-statefulset.yaml:37` |
| 5b | Redis Stack `7.4.0-v8` "unrebuilt since 2025-11-03" | Last rebuild date | GitHub API: `v7.4.0-v8` `published_at: 2025-11-03T13:45:04Z`, and it is the newest release in the repo. Terminal-tag claim also holds. | **CONFIRMED** | GitHub API `/repos/redis-stack/redis-stack/releases` |
| 5c | Redis Stack "reaches formal EOL **2026-11-30**" | Hard EOL date | **Wrong product.** 2026-11-30 is the EOL of **Redis Software (Enterprise) cluster version 7.4** in redis.io's *Redis Software product lifecycle* table. It is not a Redis Stack date. Redis Stack maintenance releases **ended December 2025**; the pinned OSS image (`redis/redis-stack-server:7.4.0-v8`) is already unmaintained. | **NEWLY-WRONG** | https://redis.io/docs/latest/operate/rs/installing-upgrading/product-lifecycle/ |
| 6 | CT Aspire Dapr hosting preview: "no stable 13.5.x is published" | Prerelease verdict | Correct. NuGet stable list ends at `13.0.0` (then `9.2.0`–`9.9.0`); the entire 13.5 line is preview/beta. | **CONFIRMED** | https://api.nuget.org/v3-flatcontainer/communitytoolkit.aspire.hosting.dapr/index.json |
| 6b | Same, "accepted because no stable alternative exists upstream" | Justification | Loose: a stable `13.0.0` exists, and newer prereleases than the pin exist (`13.5.1-beta.752`). The pin is not the newest prerelease in its own line. | **OVERSTATED** | same index |
| 6c | Fluent UI Blazor `5.0.0-rc.5`: "no GA is published" | Prerelease verdict | Correct. Newest 5.x is exactly `5.0.0-rc.5-26219.1` (= the pin); no `5.0.0` GA. | **CONFIRMED** | https://api.nuget.org/v3-flatcontainer/microsoft.fluentui.aspnetcore.components/index.json |
| 6d | Same, "no stable alternative exists upstream" | Justification | Loose: stable `4.14.4` exists and is maintained. The accurate statement is "no GA **in the 5.x line**". | **OVERSTATED** | same index |
| 7 | **New row** — PostgreSQL `18.4-trixie`, digest-pinned, access-telemetry store | Repository fact | Exact: `image: docker.io/library/postgres:18.4-trixie@sha256:3a82e1f5…`. Role confirmed (schema `access_telemetry`, dedicated runtime role, TLS-only `pg_hba`). | **CONFIRMED** | `deploy/kubernetes/base/access-telemetry-postgresql.yaml:144` |
| 7b | Same row — currency / risk | Silent (no caveat, no ledger row) | **18.4 is superseded by 18.6** (2026-08-13; 18.5 was skipped). 18.6 fixes **28 CVEs affecting all supported versions**, including multiple CVSS 8.8 arbitrary-code-execution defects (CVE-2026-14664 regexp, CVE-2026-14669 `to_char`, CVE-2026-14670 plperl, CVE-2026-15741 EXTRACT SQL injection). | **NEWLY-WRONG (omission)** | https://www.postgresql.org/about/news/postgresql-186-1711-1615-1519-1424-and-19-beta-3-released-3365/ |
| 7c | **New row** — Embedding provider `Google` | Repository fact | Correct: `GoogleProviderName = "google"`, and the Google registry entry is the MVP default config. | **CONFIRMED** | `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:18,55-62` |
| 7d | **New row** — Embedding model `text-embedding-004` | Repository fact | **False.** The repository's Google model constant is `gemini-embedding-001`: `public const string GoogleModelName = "gemini-embedding-001";`. `text-embedding-004` appears nowhere in the repository. Independently, Google **shut down `text-embedding-004` on 2026-01-14**; it is a decommissioned model. | **NEWLY-WRONG** | `EmbeddingProviderDefaults.cs:23`; https://developers.googleblog.com/gemini-embedding-available-gemini-api/ |
| 7e | **New row** — Embedding dimension `768` | Repository fact | Correct: default `Dimensions = 768`; supported set `[768, 1536, 3072]`; `TenantProvisioningInput.VectorDimensions` defaults to `768`. | **CONFIRMED** | `EmbeddingProviderDefaults.cs:53,59`; `src/Hexalith.Memories.Contracts/V1/TenantProvisioningInput.cs:12` |
| 7f | Same row presented as a single global stack pin | Table framing | The repository registers **two** providers with per-tenant configuration: `google`/`gemini-embedding-001`/768 and `ollama`/`qwen3-embedding:4b`/2560. AD-14 makes provider/model/dimension a **tenant-lifecycle domain fact**, not a build pin. | **OVERSTATED** | `EmbeddingProviderDefaults.cs:21,25,64-71`; spine AD-14 (:146) |
| 8 | "Two security blockers stand before Production qualification, not one." | Headline count | Understated. With PostgreSQL 18.4 carrying 28 unfixed CVEs (several 8.8), the count is **three**. The previous revision understated at one; this revision corrects to two and still misses one. | **OVERSTATED (in the sense of still-understated)** | 7b above |
| — | Dapr CI runtime `1.18.2` | Table pin | `DAPR_RUNTIME_VERSION: '1.18.2'` in both `.github/workflows/ci.yml:16` and `nightly.yml:13`. | **CONFIRMED** | `.github/workflows/ci.yml:16` |
| — | Redis Stack image pin | Implied by table | `redis/redis-stack-server:7.4.0-v8@sha256:798ab84d…`, digest-pinned. | **CONFIRMED** | `deploy/kubernetes/base/redis-statefulset.yaml:37` |
| — | FalkorDB image pin | `4.12.0` | `falkordb/falkordb:v4.12.0@sha256:7927eb19…`, digest-pinned. | **CONFIRMED** | `deploy/kubernetes/base/falkordb-statefulset.yaml:37` |
| — | .NET SDK / TFM / language row | `10.0.400` / `net10.0` / C# 14 | `global.json` `"version": "10.0.400"`, `"rollForward": "latestFeature"`; `Directory.Build.props` `<TargetFramework>net10.0</TargetFramework>`, `<LangVersion>14</LangVersion>`. | **CONFIRMED** | `global.json:3-4`; `Directory.Build.props:3-4` |

**Counts:** CONFIRMED 19 · NEWLY-WRONG 4 · OVERSTATED 6 · STILL-WRONG 0 · UNVERIFIED 0.
Every check reached a live source; nothing was answered from memory.

---

## Findings

### VER2-01 — The new embedding row names a model the repository does not use and Google decommissioned. **HIGH**

**Evidence.** The Stack table's new row reads `Embedding provider / model / dimension | Google / text-embedding-004 / 768` (ARCHITECTURE-SPINE.md:226). The repository's actual value is
`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:23`:

```csharp
/// <summary>The default Google embedding model used in the MVP.</summary>
public const string GoogleModelName = "gemini-embedding-001";
```

A repo-wide grep finds **zero** occurrences of `text-embedding-004`. Separately, Google shut the
`text-embedding-004` endpoint down on **2026-01-14**, superseded by the `gemini-embedding-001`
series; the two models are not interchangeable (004 emits fixed 768-dim vectors, `gemini-embedding-001`
emits 3072 by default with Matryoshka down-scaling to 1536 or 768 — which is why the repo's
supported set is `[768, 1536, 3072]`).

This is the precise failure mode this lens exists to catch: a plausible-sounding name recalled from
training data, written into a table row whose entire purpose is to record verified repository pins,
in the same paragraph that asserts these are "exact repository pins". The task brief flagged this row
as never previously reviewed, and the flag was warranted.

The consequence is not cosmetic. AD-14 binds "embedding provider/model/dimension" as the trigger for
versioned create-backfill-verify-switch-retire migrations, and AD-3 keys every projection checkpoint
on `embeddingConfigurationEpoch`. A wrong model identifier in the governing document is the kind of
value an implementer or migration author would copy.

**Recommended action.** Correct the row to `Google / gemini-embedding-001 / 768`. Add a footnote that
`768` is the configured output dimension of a model whose native width is 3072 (MRL truncation), since
that is a non-obvious fact a reindex planner needs. See also VER2-05 on where this row belongs.

---

### VER2-02 — PostgreSQL 18.4 was added to the Stack table with no security annotation; it carries 28 unfixed CVEs. **HIGH**

**Evidence.** The new row `PostgreSQL (access-telemetry store) | 18.4-trixie, digest-pinned` is
accurate as a repository fact (`deploy/kubernetes/base/access-telemetry-postgresql.yaml:144`). But
PostgreSQL **18.6** shipped 2026-08-13 (18.5 was skipped for a regression) fixing **28 security
vulnerabilities affecting all supported versions**, including four CVSS 8.8 arbitrary-code-execution
or SQL-injection defects: CVE-2026-14664 (regexp heap overflow), CVE-2026-14669 (`to_char` heap
overflow), CVE-2026-14670 (plperl heap overflow), CVE-2026-15741 (EXTRACT deparse SQL injection),
plus CVE-2026-6464 (8.1, `COPY FROM STDIN`).

The rewritten caveat names .NET (six CVEs) and OpenBao as blockers and opens with "Two security
blockers stand before Production qualification, not one." A component with 28 unfixed CVEs was added
to the same table in the same revision and drew neither a caveat sentence nor a Current Alignment
Gaps row. Both existing security rows (.NET at :332, OpenBao at :333) are marked *blocks Production*;
by the same standard this one qualifies.

This also directly answers the brief's item 8. The previous revision understated risk in two places;
the correction moved the count from one to two — and still lands short of three.

**Recommended action.** Bump the digest pin to `18.6-trixie`, or add a Current Alignment Gaps row
("PostgreSQL `18.4` precedes the 2026-08-13 28-CVE release; bump to `18.6` or record a dated exception
naming the 8.8 defects", violated rule AD-19, *Dated exception or bump owed; blocks Production*,
active-foundation critical: Yes) and change the caveat's opening to "Three security blockers".

---

### VER2-03 — The Redis Stack EOL date belongs to a different Redis product; the real position is worse. **MEDIUM**

**Evidence.** The caveat states Redis Stack `7.4.0-v8` "reaches formal EOL 2026-11-30, which dates the
Redis 8 migration", and the Deferred table repeats it ("Before the 2026-11-30 Redis Stack EOL",
spine:350). **2026-11-30 is the end-of-life of Redis *Software* (Redis Enterprise) cluster version
7.4**, taken from redis.io's *Redis Software product lifecycle* table — a commercial cluster product,
not the `redis/redis-stack-server` OSS distribution this repository pins.

Redis Stack's own position: Redis folded the Stack modules into core Redis at 8.0 and **stopped
shipping Redis Stack maintenance releases in December 2025**. The pinned image's terminal build is
`v7.4.0-v8`, published `2025-11-03T13:45:04Z` (GitHub API) — a date the spine gets exactly right.

So the two verifiable halves of the sentence are correct and the inference drawn from them is not.
The effect is an *understatement*: the text implies roughly two and a half months of remaining
supported runway when in fact the component has been receiving no security patches for ten months.
A migration deadline derived from another product's calendar is worse than no deadline, because it
reads as researched.

**Recommended action.** Replace with: "Redis Stack `7.4.0-v8` is the terminal build of a line whose
maintenance ended December 2025 (last rebuild 2025-11-03); it receives no further security patches,
and Redis Open Source 8 — which absorbs Search, JSON, TimeSeries and Bloom into core — is the
successor. The migration is overdue rather than scheduled." Update the Deferred row (:350) in the
same edit, since it carries the same date. If a dated deadline is wanted for planning, it must be one
the team sets, not one attributed to Redis.

---

### VER2-04 — "Eight minor lines behind" overstates the FalkorDB gap by 2×. **LOW**

**Evidence.** `v4.20.4` (2026-08-20) is confirmed latest and the repository is confirmed at
`v4.12.0`. But FalkorDB releases on **even-numbered** stable minor lines — the tag list shows
4.0, 4.2, 4.3, 4.4, 4.6, 4.8, 4.10, 4.12, 4.14, 4.16, 4.18, 4.20. Between the pin and head, the
actually released minor lines are **four**: 4.14, 4.16, 4.18, 4.20. "Eight" is `20 − 12`, the
version-number distance, presented as a count of release lines.

The substantive conclusion — a multi-line upgrade with data-format and query-behaviour risk, not a
minor lag — survives intact and is well judged. Only the number is wrong, and it is wrong in the
direction that makes the gap look worse than it is. This is the second half of the brief's item 8
question: the correction did overshoot here.

**Recommended action.** "FalkorDB `4.12.0` is four released minor lines behind `v4.20.4`".

---

### VER2-05 — Three smaller precision defects in the rewritten caveat. **LOW**

**(a) `rollForward: latestFeature` is described as `latestPatch`.** The caveat says it "means the
resolved SDK floats within the 10.0.4xx band". Per the `global.json` schema, `latestFeature` uses
"the highest installed feature band and patch level that matches the requested major and minor" —
it floats **across** feature bands within 10.0, so an installed 10.0.5xx SDK would be selected.
Confinement to the 4xx band is what `latestPatch` does. The conclusion the sentence supports (the
SDK floats, the packages and images do not) is unaffected; only the mechanism is misnamed.
Fix: "floats to the highest installed 10.0.x SDK".

**(b) "no stable alternative exists upstream" is too strong for both prereleases.** For Fluent UI, a
maintained stable `4.14.4` exists — the accurate claim is "no GA in the 5.x line". For CommunityToolkit
Aspire Dapr hosting, a stable `13.0.0` exists, and the pin is not even the newest prerelease in its
own line (`13.5.1-beta.752` is available). Both verdicts are defensible on compatibility grounds
against Aspire `13.5.3`; state that reason rather than the absolute one, which is checkable and false.

**(c) The embedding row is a per-tenant fact wearing a build-pin costume.** Beyond the wrong model
name (VER2-01), the repository registers two providers — `google`/`gemini-embedding-001`/768 and
`ollama`/`qwen3-embedding:4b`/2560 — selected per tenant, and AD-14 (:146) explicitly makes "the active
schema generation and embedding-configuration epoch … tenant-lifecycle domain facts committed through
AD-2". A single row in a table of build-time pins implies a global constant the architecture
deliberately does not have. Either annotate the row "MVP default; per-tenant under AD-14" or move it
out of `## Stack`.

---

### VER2-06 — The gitlink/package-lane correspondence AD-19 requires is asserted but not met. **LOW**

**Evidence.** The table pairs package lane `3.103.0` with source-lane gitlink `6b0247ac`, and AD-19
(:176) requires the source-lane gitlink to "correspond to the package-lane version". `git submodule
status --cached` describes the gitlink as `v3.103.0-40-g6b0247ac` — **40 commits past** tag `v3.103.0`.
The two rows sit adjacently in the table with nothing marking the distance.

This is not a new error (the ledger row at :330 already owes "gitlink/package-version correspondence"
evidence), but the corrected gitlink now makes the divergence precisely measurable for the first time,
and the caveat's new confident framing invites the reader to treat the pairing as verified.

**Recommended action.** Annotate the row `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20` *(v3.103.0+40)*
so the reader sees that source and package lanes are not at the same revision, and cross-reference
the ledger row that owes the correspondence evidence.

---

### VER2-07 — The .NET remediation is already sitting in the working tree. **INFORMATIONAL**

**Evidence.** The dirty `references/Hexalith.Builds` worktree revision `fa647278` (`v4.27.3`) already
carries `Microsoft.AspNetCore.* 10.0.12` throughout — along with `Dapr 1.18.7`, `StackExchange.Redis
3.2.0` and `Kreuzberg 4.10.3`. The committed gitlink `a32cb422` the spine reads from carries `10.0.11`.

Two consequences worth recording. First, the disambiguation added in this revision is **correct and
genuinely load-bearing** — 75 pin lines differ between the two revisions, so "committed gitlink" was
the right thing to specify and this reviewer confirms the choice. Second, the ledger row at :332
(".NET `10.0.400` / runtime `10.0.11` precedes the 2026-09-08 six-CVE security release") describes work
whose upstream half is already done: closing it is a submodule bump plus a `global.json` edit, not a
wait on Microsoft or on Hexalith.Builds. Recording that in the ledger's convergence column would make
the row actionable rather than open-ended.

---

## What this lens confirms without qualification

Worth stating plainly, since the findings above are all corrections: the **backbone of this revision
is sound and materially better than what it replaced**.

- Both gitlink corrections are exactly right, verified against `git ls-tree HEAD` today.
- All twelve catalog-sourced pins match the committed catalog with zero mismatches — a clean sweep.
- The "committed gitlink" disambiguation is correct *and* necessary, not defensive boilerplate.
- The .NET blocker is real, correctly dated, correctly counted at six CVEs, and correctly reasoned:
  the packages genuinely do not roll forward.
- The OpenBao remediation target (`2.6.2`), the current chart line (`0.29.4`), and the GO-2026-5970
  sweep are all still accurate as of today; neither version moved.
- The Redis Stack last-rebuild date `2025-11-03` is exact to the second against the GitHub API.
- Both prerelease verdicts are correct on their central claim: no stable `13.5.x`, no `5.0.0` GA.
- The PostgreSQL row's repository facts — tag, digest pinning, role as the access-telemetry store —
  are all exactly as stated.

The failures cluster in one place: **the two rows added without a repository read behind them**. The
embedding row was written from recall and is wrong; the PostgreSQL row was read from the manifest and
is right on its facts but was never currency-checked. Everything re-derived from the repository or
re-checked against upstream held.
