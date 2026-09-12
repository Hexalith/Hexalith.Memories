---
lens: technology-version-reality
kind: reviewer-gate-pass3
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
scope: '## Stack table (spine:212-230), the caveat paragraph (spine:232), the PostgreSQL ledger row (spine:337), and the Deferred Redis row (spine:356)'
reviewed: '2026-09-12'
supersedes: 'reviews/review-version-retest-2026-09-12.md'
mode: read-only
---

# Reviewer Gate Pass 3 — Technology / Version Reality (2026-09-12)

**Verdict: PASS-WITH-FINDINGS.**

Seven of the eight corrections requested in pass 2 landed and are themselves correct. The
embedding row, the Redis Stack rewrite, the FalkorDB recount, the `rollForward` mechanism, and both
prerelease verdicts are now accurate against live sources. **One correction overshot into a new wrong
number** — the PostgreSQL caveat converted pass 2's illustrative "including four CVSS 8.8 defects"
into an exhaustive count, and the real figure is **fourteen** at 8.8 (seventeen at ≥8.0), so the
amendment understates the release it just added as a blocker. **One requested correction was not
applied at all** (the AD-19 gitlink/package-lane divergence, `v3.103.0+40`). And the sweep for
newly-asserted-but-unchecked facts surfaces a claim that has survived three revisions unchallenged
and is false: **the container base image is a floating tag, not a pinned one**, which makes the
caveat's ".NET" sentence wrong in one direction and hides an unrecorded AD-19 pinning gap in the
other. I mis-cleared that claim myself in pass 2 by verifying only its package half; this pass
verifies both.

---

## Verification Table

| # | Claim | Spine now says | Verified reality | Status | Source |
| --- | --- | --- | --- | --- | --- |
| 1 | Embedding model corrected | `gemini-embedding-001` (spine:226) | `public const string GoogleModelName = "gemini-embedding-001";` — exact match. `text-embedding-004` now appears only in test fixtures, nowhere in `src/`. | **CONFIRMED** | `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:23` |
| 1b | Embedding dimension | `768` | Registry default `Dimensions = 768`; supported set `[768, 1536, 3072]`. | **CONFIRMED** | `EmbeddingProviderDefaults.cs:53,57` |
| 1c | Model still exists and fits | Implied by the pin | `gemini-embedding-001` is GA on the Gemini API and Vertex AI today; MRL truncation makes 768 an officially recommended output width (3072 default → 1536 / 768 / 256). | **CONFIRMED** | https://ai.google.dev/gemini-api/docs/embeddings ; https://developers.googleblog.com/gemini-embedding-available-gemini-api/ |
| 1d | New parenthetical "a per-tenant AD-14 fact, recorded here because AD-14 binds it" | Framing correction | Accurate. AD-14's **Binds** line opens with "Embedding provider/model/dimension" (spine:144), and the value is carried per tenant on `TenantEmbeddingConfig`, not globally. | **CONFIRMED** | spine:144; `EmbeddingProviderDefaults.cs:55-62` |
| 1e | Same row — completeness | Names only `Google` | The registry holds **two** providers: `google`/`gemini-embedding-001`/768 and `ollama`/`qwen3-embedding:4b`/2560. The parenthetical now signals per-tenancy but the row still reads as the only configuration and is not marked "MVP default". | **OVERSTATED** (residual) | `EmbeddingProviderDefaults.cs:20,26,64-71` |
| 2 | PostgreSQL row annotation | "behind the 18.6 security release" (spine:224) | Correct and now visible in the table itself. Repository pin unchanged and still digest-pinned. | **CONFIRMED** | `deploy/kubernetes/base/access-telemetry-postgresql.yaml:144` |
| 2b | 18.6 release date | `2026-08-13` | Announcement is "Posted on 2026-08-13". | **CONFIRMED** | https://www.postgresql.org/about/news/postgresql-186-1711-1615-1519-1424-and-19-beta-3-released-3365/ |
| 2c | 18.5 skipped | "18.5 was skipped" | Verbatim: "This release skips PostgreSQL 18 versions from PostgreSQL 18.4 to 18.6. 18.5 was not shipped due to a regression." | **CONFIRMED** | same announcement |
| 2d | CVE count | `28 CVEs` | "This release fixes 28 security vulnerabilities" — and 28 unique `CVE-2026-*` ids parse out of the raw page. | **CONFIRMED** | same announcement (raw HTML parse) |
| 2e | Severity count | "**four** of them CVSS 8.8 RCE or SQL-injection" (spine:232, repeated at spine:337) | **Fourteen** carry CVSS v3.1 8.8: CVE-2026-14662, -14664, -14669, -14670, -14671, -14676, -14677, -14680, -15741, -15742, -16238, -16239, -18408, -19385. Seventeen score ≥ 8.0 (adding 8.1 ×2, 8.2 ×1). Pass 2 wrote "including four … defects" as an illustration; the amendment read it as a total. | **NEWLY-WRONG** | raw parse of the announcement; cross-checked against "seventeen of the 28 carry a CVSS score of 8.0 or higher" |
| 2f | Scope of exposure | "28 CVEs **affecting all supported versions**" | The *release* covers all supported versions; the per-CVE affected-version sets differ (CVE-2026-16238 and CVE-2026-14676 are PostgreSQL-18-only; others reach back to 14). The announcement itself says "more details about the vulnerabilities and **their affected versions** in the links below". | **OVERSTATED** (minor) | announcement text; https://www.postgresql.org/support/security/ |
| 2g | PostgreSQL as a blocker | New ledger row at spine:337, `blocks Production` | Justified. A digest-pinned image one release behind a 28-CVE, 14×8.8 release is a defensible Production blocker under AD-19. | **CONFIRMED** | spine:337 |
| 3 | Blocker count | "**Four** items block Production qualification, not one." | Exactly four ledger rows carry `blocks Production`: spine:324 (per-tenant backend principals), :332 (.NET), :333 (OpenBao), :337 (PostgreSQL). The count matches the document. | **CONFIRMED** | `grep -n "blocks Production"` over the spine |
| 3b | Each is genuinely a blocker | .NET / OpenBao / PostgreSQL / per-tenant principals | All four hold. The fourth is not merely a ledger convention: AD-15's rule text states "**Production qualification is blocked** until per-tenant backend principals replace it or a dated, time-bounded security exception is approved". | **CONFIRMED** | spine:152 (AD-15 Rule) |
| 3c | Count read as exhaustive | Unqualified "Four items block Production qualification" | The sentence scopes to nothing. The Deferred row "Production Dapr actor/workflow state store — **Before Production launch or SLO approval**, qualify or replace" (spine:351) is also a launch precondition, and the same paragraph describes Redis Stack as ten months unpatched without counting it. The number is right for the ledger, not for the document. | **OVERSTATED** (minor) | spine:351; spine:232 |
| 4 | Redis Stack maintenance ended | "maintenance ended December 2025" | Official: the redis-stack repository states maintenance for Redis Stack 6.2, 7.2 and 7.4 ceases December 2025, and directs users to Redis Open Source 8, which absorbs every Stack module. | **CONFIRMED** | https://github.com/redis-stack/redis-stack (README) |
| 4b | Last rebuild | "not been rebuilt since 2025-11-03" | `v7.4.0-v8`, `published_at 2025-11-03T13:45:04Z`, still the newest release in the repository. | **CONFIRMED** | GitHub API `/repos/redis-stack/redis-stack/releases` |
| 4c | Unpatched duration | "roughly ten months" | 2025-11-03 → 2026-09-12 = 10 months 9 days. "Roughly ten" is exact enough and does not overshoot. | **CONFIRMED** | arithmetic on 4b |
| 4d | EOL disclaimer | "The 2026-11-30 date sometimes cited is Redis Software 7.4's EOL, a different product, and must not be read as headroom here." | Exactly right. redis.io's *Redis Software product lifecycle* table lists cluster version "7.4 – February 2024 → November 30, 2026". That table covers Redis Software (Enterprise) clusters, not the `redis/redis-stack-server` OSS image. | **CONFIRMED** | https://redis.io/docs/latest/operate/rs/installing-upgrading/product-lifecycle/ |
| 4e | Deferred row corrected too | spine:356 now reads "the Redis Stack line being already out of maintenance since December 2025" | Corrected. `2026-11-30` now occurs **once** in the whole spine — inside the disclaimer that debunks it. | **CONFIRMED** | `grep -c "2026-11-30"` = 1, at spine:232 |
| 5 | FalkorDB gap | "four released minor lines behind `v4.20.4`" | Correct. Tags between the pin and head: 4.14, 4.16, 4.18, 4.20 — four lines. `v4.20.4` is still latest (`prerelease: false`, 2026-08-20). Repository pin `v4.12.0` digest-pinned, unchanged. | **CONFIRMED** | GitHub API `/repos/FalkorDB/FalkorDB/tags` and `/releases/latest`; `deploy/kubernetes/base/falkordb-statefulset.yaml:37` |
| 5b | Supporting rule | "FalkorDB ships even-numbered minors" | Almost, not quite: **`v4.3.0` exists** alongside 4.2 and 4.4. The pattern holds from 4.4 onward but is stated as a property of the project. The conclusion (four lines) is unaffected. | **OVERSTATED** (minor) | tag list: … v4.4.1, v4.4.0, **v4.3.0**, v4.2.2 … |
| 6 | `rollForward` mechanism | "floats the resolved SDK **across feature bands rather than within one**" | Correct now. `latestFeature` = "the highest installed feature band and patch level that matches the requested major and minor"; the doc's own example allows 8.0.302 → 8.0.402. The pass-2 `latestPatch` conflation is gone. | **CONFIRMED** | https://learn.microsoft.com/dotnet/core/tools/global-json#rollforward |
| 6b | ".NET" premises | runtime `10.0.11` under SDK `10.0.400`; `10.0.12` / SDK `10.0.401` on 2026-09-08 fixing six CVEs | `releases.json` latest-release `10.0.12`, latest-release-date `2026-09-08`; SDK 10.0.400 ↔ 10.0.11, SDK 10.0.401 ↔ 10.0.12. Release notes list exactly six CVEs (CVE-2026-69439, -71328, -69522, -69304, -58649, -69806). Nothing newer than 10.0.12 as of today. | **CONFIRMED** | https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/releases.json ; .../10.0.12/10.0.12.md |
| 6c | "**container base images** and the pinned `Microsoft.AspNetCore.*` packages do not roll forward" | Exposure premise | Half false. The packages half is right (`Microsoft.AspNetCore.* 10.0.11` throughout the committed catalog). The **container half is wrong**: every owned image is built from `<ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:10.0-alpine</ContainerBaseImage>` — a **floating** tag with no digest, which Microsoft re-points at the newest 10.0.x. On the next build/pull the shipped ASP.NET Core shared framework becomes 10.0.12 without any bump. Four projects inherit it: Server, Mcp, AccessTelemetry, AccessTelemetry.Clock. | **STILL-WRONG** (carried; I mis-cleared it in pass 2 by checking only the package half) | `Directory.Build.targets:3`; `src/*/**.csproj:5` (`<EnableContainer>true</EnableContainer>` ×4) |
| 6d | "the remediation already sits in the checked-out Builds worktree, making this a **one-bump fix** rather than an upstream wait" | New claim | Substance holds, framing overshoots. `fa647278` = `v4.27.3` carries `Microsoft.AspNetCore.* 10.0.12` and **is a published upstream tag** (`ls-remote` confirms `refs/tags/v4.27.3 → fa647278`), so "not an upstream wait" is right. But (i) it points at a *dirty, uncommitted* submodule worktree rather than the released tag — a reader on a clean checkout sees nothing there; (ii) it is not one bump — the spine's own ledger convergence (spine:332) also demands SDK `10.0.401` while `global.json` pins `10.0.400`, and 6c's floating base tag is a third, unowned surface. | **OVERSTATED** | `git ls-remote --tags` in `references/Hexalith.Builds`; `global.json:3`; spine:332 |
| 7 | Prerelease verdict rewording | "no stable release exists **in the pinned line**" | Correct and precisely scoped now. | **CONFIRMED** | below |
| 7b | CT Aspire Dapr detail | "no stable 13.5.x is published, though 13.0.0 is stable and 13.5.1-beta is newer than the pin" | Stable list ends at `13.0.0`; no stable 13.5.x exists; newest 13.5.x prerelease is `13.5.1-beta.752`, ahead of the pinned `13.5.0-preview.1.260825-0345`. Every clause holds. | **CONFIRMED** | https://api.nuget.org/v3-flatcontainer/communitytoolkit.aspire.hosting.dapr/index.json |
| 7c | Fluent UI detail | "no 5.0.0 GA is published, though 4.x is stable" | No `5.0.0` GA; highest 4.x stable is `4.14.4`; the pin `5.0.0-rc.5-26219.1` is the newest 5.x. | **CONFIRMED** | https://api.nuget.org/v3-flatcontainer/microsoft.fluentui.aspnetcore.components/index.json |
| 8 | EventStore gitlink still current | `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20` | `git ls-tree HEAD` returns exactly that. Unmoved since pass 2. | **CONFIRMED** | `git ls-tree HEAD references/Hexalith.EventStore` |
| 8b | Builds gitlink still current | `a32cb422` | `git ls-tree HEAD` returns `a32cb422749352cce8dec948aa3e78c8f00eb4cf` (`v4.27.2-10-ga32cb42`). Unmoved. | **CONFIRMED** | `git ls-tree HEAD references/Hexalith.Builds` |
| 8c | AD-19 package-lane correspondence acknowledged? | Nothing added | **Not applied.** `git submodule status --cached` still describes the gitlink as `v3.103.0-40-g6b0247ac` — 40 commits past tag `v3.103.0`, which the table pairs it with. No `v3.103.0+40` annotation appears in the table, the caveat, or the ledger, and the row that owes the correspondence evidence (spine:330) is still classified active-foundation critical **No**. | **STILL-WRONG** | `git submodule status --cached references/Hexalith.EventStore`; spine:220-221, :330 |
| 8d | Re-derivation duty against a moving target | "re-derived … at each revision" | The **worktree** EventStore has moved to `a568af4e` (`v3.103.0-42`) since pass 2, two commits past the committed gitlink. The spine cites the committed gitlink and is therefore still correct — but the duty now has a live drift underneath it. | **CONFIRMED** (informational) | `git submodule status references/Hexalith.EventStore` |
| — | Dapr CI runtime | `1.18.5` / `1.18.2` | `DAPR_RUNTIME_VERSION: '1.18.2'` in `ci.yml:16` and `nightly.yml:13`; catalog SDK `1.18.5`. Unchanged. (The uncommitted Builds worktree carries `Dapr.Client 1.18.7`.) | **CONFIRMED** | `.github/workflows/ci.yml:16` |
| — | OpenBao remediation targets | `2.6.2`, chart line `0.29.x` presently `0.29.4`, superseded `0.28.6` | `v2.6.2` (2026-08-18) is still `releases/latest`, `prerelease: false`; the only newer tag remains `v2.7.0-beta20260909`. Chart `openbao-0.29.4` (2026-09-03) is newest; `openbao-0.28.6` (2026-07-22) is real and superseded. Repository still at image `2.6.0` / chart `0.28.5`. | **CONFIRMED** | GitHub API openbao & openbao-helm; `deploy/openbao/values.yaml:2,27` |
| — | Frontmatter `sources:` provenance | Lists OpenBao ×2 and redis-stack as the only upstream sources | The caveat now makes dated, numeric CVE claims about **.NET** and **PostgreSQL** and version claims about **FalkorDB**, **NuGet prereleases** and the **k8s manifests**, none of which appear in `sources:`. A checkable claim with no recorded source is exactly what this lens exists to prevent. | **OVERSTATED** (provenance gap) | spine:20-32 |
| — | Upstream consistency of the embedding correction | Spine corrected | `prd.md:799` **still** reads `| Google | text-embedding-004 | 768 | 1500 req/min |`, and `prd.md:805` names OpenAI `text-embedding-3-small` as the deferred provider while the repository registers `ollama`/`qwen3-embedding:4b`. The PRD is a declared `source:` of the spine. | **STILL-WRONG** (upstream) | `prd.md:799,805`; `EmbeddingProviderDefaults.cs:26` |

**Counts:** CONFIRMED 24 · NEWLY-WRONG 1 · STILL-WRONG 3 · OVERSTATED 6 · UNVERIFIED 0.
Every check reached a live source or the working tree; nothing was answered from memory.

---

## Findings

### VER3-01 — The PostgreSQL severity count overshot in the wrong direction: fourteen CVEs at 8.8, not four. **HIGH**

**Evidence.** The caveat (spine:232) and the new ledger row (spine:337) both state that the 18.6
release fixes "28 CVEs … four of them CVSS 8.8" / "four at CVSS 8.8". Parsing the release
announcement directly gives **fourteen** entries at CVSS v3.1 8.8:

```
CVE-2026-14662  14664  14669  14670  14671  14676  14677  14680
CVE-2026-15741  15742  16238  16239  18408  19385
```

and **seventeen** at ≥ 8.0 (adding CVE-2026-6464 8.1, -14668 8.1, -14679 8.2). Ten of the fourteen
carry "executes arbitrary code" in the title.

The provenance of the error is instructive and worth naming, because it is a failure mode this gate
will meet again. Pass 2 wrote "**including** four CVSS 8.8 arbitrary-code-execution or SQL-injection
defects" and then listed four exemplars. The amendment lifted the exemplar count and wrote it into
the spine as a total. A reviewer's illustrative sample became a governing document's quantification.

The consequence is that the amendment **understates the very release it just promoted to a blocker**,
by 3.5×, in the two places a reader would check. It also creates an internal oddity: the ledger asks
the exception-writer to "record a dated exception **naming the CVEs**", and the number they will find
when they go to name them is not the number the row told them to expect.

**Recommended action.** In both spine:232 and spine:337, replace "four" with "fourteen at CVSS 8.8
(seventeen at 8.0 or higher)", or drop the count and write "including multiple CVSS 8.8
arbitrary-code-execution defects". Do not carry an exemplar count forward as a total.

---

### VER3-02 — The container base image is a floating tag, which makes the .NET exposure sentence wrong and hides an unrecorded AD-19 gap. **HIGH**

**Evidence.** The caveat asserts that "container base images and the pinned `Microsoft.AspNetCore.*`
packages do not roll forward, so the exposure is real". The second clause is true. The first is not:

```xml
<!-- Directory.Build.targets:3 -->
<ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:10.0-alpine</ContainerBaseImage>
```

That is a **floating** tag with no digest. Microsoft re-points `10.0-alpine` at each patch release, so
the ASP.NET Core shared framework inside every rebuilt image becomes 10.0.12 with no bump, no review
and no record. Four owned projects set `<EnableContainer>true</EnableContainer>` and inherit it:
`Hexalith.Memories.Server`, `.Mcp`, `.AccessTelemetry`, `.AccessTelemetry.Clock` — i.e. the whole
production image set published to `registry.hexalith.com`.

This cuts twice, in opposite directions:

1. **The stated exposure is overstated.** The runtime the containers actually ship *does* roll forward.
   The residual real exposure is the compile-time `Microsoft.AspNetCore.*` package references and the
   build SDK, not the container runtime.
2. **A larger AD-19 violation is unrecorded.** AD-19 requires "pin qualified container defaults", and
   the caveat's own closing sentence declares "Image digests remain mandatory in production" — while
   naming only "floating AppHost/Aspire Redis and Falkor defaults" as the gap. The four *application*
   images are built from a floating base and no ledger row covers them. The spine states a rule and,
   two sentences later, describes the pinning posture as if the rule were met.

I must record that I cleared this claim in pass 2 (row 3d, "CONFIRMED"). I verified the package half
and inferred the container half. That was the wrong shape of check for a claim with two independent
subjects, and it let a false premise survive a second revision.

**Recommended action.** Split the sentence: "the pinned `Microsoft.AspNetCore.*` packages do not roll
forward, so build-time exposure is real; the container base image `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`
is a *floating* tag, so image contents change unreviewed between rebuilds — itself an AD-19 pinning
violation." Add a ledger row under AD-19 for the unpinned application base image, and extend the
floating-image row at spine:335 beyond AppHost/Aspire Redis and Falkor.

---

### VER3-03 — The AD-19 gitlink/package-lane divergence was requested in pass 2 and was not applied. **MEDIUM**

**Evidence.** The Stack table still pairs, on adjacent lines with nothing between them:

```
| Hexalith.EventStore package lane        | `3.103.0` |
| Hexalith.EventStore source lane gitlink | `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20` |
```

`git submodule status --cached references/Hexalith.EventStore` still resolves that gitlink to
`v3.103.0-40-g6b0247ac` — **forty commits past** the tag the row above it names. AD-19 (spine:176)
requires the source-lane gitlink to "correspond to the package-lane version; that correspondence is
part of lane evidence."

Two things make this worth re-raising rather than dropping. First, the amendment pass explicitly
claimed to have applied this reviewer's corrections, and this one is absent from both the table and
the caveat — the memlog's own second-pass entry lists four corrections and this is not among them.
Second, the caveat's newly confident framing ("These are **exact** repository pins", "re-derived from
`git ls-tree HEAD` at each revision") now invites the reader to read the pairing as *verified*, which
is precisely what it is not. The ledger row that owes this evidence (spine:330) remains classified
active-foundation critical **No**, so nothing in the document forces the divergence to surface.

Separately, the *worktree* EventStore has moved to `a568af4e` (`v3.103.0-42`) since pass 2. The spine
cites the committed gitlink and is correct as written, but the re-derivation duty AD-19 imposes is now
sitting over live drift.

**Recommended action.** Annotate the row `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20` *(`v3.103.0+40` —
correspondence evidence owed, see the AD-19 lane row)*, and reconsider the `No` classification on
spine:330 now that the distance is measurable.

---

### VER3-04 — "One-bump fix" and the caveat's blocker count each overstate slightly. **LOW**

**(a) The .NET remediation is not one bump.** "The remediation already sits in the checked-out Builds
worktree, making this a one-bump fix rather than an upstream wait" — the *upstream-wait* half is
confirmed and is a genuinely useful addition: `v4.27.3` = `fa647278` carries `10.0.12` and
`git ls-remote --tags` shows it published upstream. But the claim points at a **dirty, uncommitted
submodule worktree**, which a clean checkout does not have and which this repository's own convention
treats as inherited drift to be reverted. And it is not one bump: the spine's own ledger convergence
(spine:332) also requires SDK `10.0.401` while `global.json:3` pins `10.0.400`, and VER3-02's floating
base tag is a third surface nobody owns. (Note that `rollForward: latestFeature` would in fact select
an installed 10.0.401 on its own, and CI has no `setup-dotnet` step — so the SDK half is less fixed
than the sentence implies, in the opposite direction.) Cite the **released tag** `v4.27.3`, not the
worktree, and say "a submodule bump plus a `global.json` edit".

**(b) "Four items block Production qualification" is exhaustive-sounding but scoped to the ledger.**
The count is exactly right against the four rows marked `blocks Production` (spine:324, :332, :333,
:337) and each is genuinely a blocker — AD-15's rule text (spine:152) makes the per-tenant-principals
one normative, not merely a ledger convention. But the sentence scopes to nothing, while the Deferred
row at spine:351 ("**Before Production launch or SLO approval**, qualify or replace the current Redis
component") is also a launch precondition. Write "Four ledger rows block Production qualification".

**(c) "28 CVEs affecting all supported versions."** The *release* covers all supported versions; the
per-CVE affected sets differ (CVE-2026-16238 and CVE-2026-14676 are PostgreSQL-18-only). Say "28 CVEs
across all supported versions".

**(d) "FalkorDB ships even-numbered minors."** Nearly true and load-bearing for the recount, but
**`v4.3.0` exists**. True from 4.4 onward; state it as an observed pattern above 4.4, not a project rule.

---

### VER3-05 — The caveat's own risk ordering is now internally inconsistent. **LOW**

**Evidence.** In one paragraph the spine says PostgreSQL `18.4` — **one release** behind, remediated by
a digest bump — blocks Production, and that Redis Stack `7.4.0-v8` — the terminal build of a line whose
maintenance **ended**, unrebuilt for **ten months**, with no future patch of any kind — is not a
blocker but a Deferred "Redis 8 and FalkorDB upgrades" item to revisit "before the next Production
support/security window".

Both statements are individually correct and I confirmed both. But an unmaintained component
accumulating ten months of unpatched base-OS and server CVEs is not obviously a lower Production risk
than a maintained component one release behind, and the document offers no reason for the asymmetry.
This is not a version error — it is the version facts, now finally accurate, no longer matching the
disposition attached to them. Pass 2's correction made the Redis position *worse* than the spine
previously believed, and the disposition did not move with it.

**Recommended action.** Either state the reason the Redis exposure is acceptable (network isolation,
no untrusted input, a scheduled migration with a team-set date), or classify it alongside the other
four. Silence reads as an oversight rather than a judgement.

---

### VER3-06 — The corrected facts have no recorded sources, and the embedding correction did not reach the PRD. **LOW**

**(a) Provenance.** The `sources:` frontmatter (spine:20-32) lists only two upstream URLs — the OpenBao
changelog and helm releases — plus the redis-stack repository. The caveat now asserts dated, numeric
claims about **.NET** (six CVEs, 2026-09-08), **PostgreSQL** (28 CVEs, 2026-08-13, 18.5 skipped),
**FalkorDB** (`v4.20.4`), and **two NuGet prerelease lines**, and reads the Redis/Falkor/Postgres pins
from `deploy/kubernetes/base/*.yaml` — none of which appear as sources. For a lens whose mandate is
"flag anything that could be out of date and wasn't confirmed against the web", a governing document
that asserts eight upstream version facts and records three sources is one revision away from being
unauditable. Add `https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/releases.json`,
the PostgreSQL 18.6 announcement, the FalkorDB releases API, the two NuGet flat-container indexes, and
`deploy/kubernetes/base/`.

**(b) The correction stopped at the spine.** `prd.md:799` still carries
`| Google | text-embedding-004 | 768 | 1500 req/min |` — a decommissioned Google model (shut down
2026-01-14) in a document the spine declares as a `source:`. The memlog acknowledges this debt
("prd.md still carries the stale text-embedding-004 model and owes that correction") and it is still
owed. `prd.md:805` additionally names OpenAI `text-embedding-3-small` as the deferred second provider
while the repository registers `ollama`/`qwen3-embedding:4b`/2560. A spine that is right while its
declared source is wrong will lose that argument the next time someone reads bottom-up.

---

## What this pass confirms without qualification

- **The embedding row is now exactly right** — model, dimension, and the AD-14 per-tenant framing —
  and the model is live and GA, with 768 an officially supported MRL width. The worst defect pass 2
  found is fully closed.
- **The Redis Stack rewrite is the strongest correction in the amendment.** Every clause checks out:
  December 2025 end of maintenance (official repo), 2025-11-03 last rebuild (API, exact), ~ten months
  (arithmetic), and the disclaimer correctly attributes 2026-11-30 to Redis Software 7.4. The stale
  date is gone from the Deferred row and now appears once in the document — inside the sentence that
  debunks it. That is the right way to retire a wrong fact.
- **`rollForward: latestFeature`** is now described with the correct mechanism.
- **Both prerelease verdicts** are precisely scoped and every supporting sub-claim (13.0.0 stable,
  13.5.1-beta newer than the pin, 4.14.4 stable, no 5.0.0 GA, pin = newest 5.x) verifies.
- **The FalkorDB recount to four lines** is correct, and `v4.20.4` is still head.
- **Both gitlinks, all twelve catalog pins, the Dapr CI runtime, and the OpenBao targets** are
  unmoved and still exact.

The amendment closed six of eight requested items cleanly. What remains is one number that overshot
by taking a reviewer's example for a total, one requested annotation that never landed, and one claim
that has now survived three revisions because it was only ever half-checked — including by me.
