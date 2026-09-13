---
lens: technology-version-reality
kind: reviewer-gate-pass4
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
scope: 'Stack table (spine:245-265), the caveat paragraph (spine:266), the Structural Seed partition sentence (spine:314), the Direct Redis Exception Registry (spine:235-243), AD-19 (spine:188-192), AD-21 (spine:200-205), and every file/line/code claim in the 44-row alignment ledger (spine:350-407)'
reviewed: '2026-09-12'
supersedes: 'reviews/review-version-pass3-2026-09-12.md'
mode: read-only
---

# Reviewer Gate Pass 4 — Technology / Version Reality (2026-09-12)

**Verdict: REVISE.**

**Counts:** CONFIRMED 48 · NEWLY-WRONG 8 · STILL-WRONG 1 · OVERSTATED 6 · UNVERIFIED 1.

The pass re-derived the EventStore source-lane gitlink exactly as AD-19 requires and got it right to the
commit. It did not re-derive the other gitlink in the same sentence. `references/Hexalith.Builds` moved from
`a32cb422` to `cf52f74c` in commit `878b397f` — the commit immediately *before* the EventStore bump this pass
did catch — and the Stack table is therefore read from a superseded catalog. Five Stack facts are stale as a
result, and one security conclusion inverts: the `Microsoft.AspNetCore.*` package bump the caveat says has not
landed is pinned at `10.0.12` in the catalog the repository actually consumes.

Every one of the four code/git claims this pass added is confirmed, three of them to the exact cited line.
That half of the work is clean. The verdict is REVISE because the caveat paragraph's factual base is a
superseded revision, not because the pass reasoned badly from it.

---

## 1. Verification table

Method: every version row was read from the file the caveat names as its source — the committed
`references/Hexalith.Builds` catalog — and from the repository's own manifests; every upstream claim was
re-fetched. Repository authority for a *pin* is the committed gitlink, per the caveat's own rule.

### 1.1 Stack table (spine:247-264)

| # | Claim | Verdict | Evidence |
| --- | --- | --- | --- |
| 1 | .NET SDK `10.0.400` / `net10.0` / C# 14 | **CONFIRMED** | `global.json:3` = `10.0.400`; `Directory.Build.props:3` = `net10.0`; `:4` = `LangVersion 14` |
| 2 | Aspire AppHost SDK `13.5.3` | **CONFIRMED** | `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj:1` = `Sdk="Aspire.AppHost.Sdk/13.5.3"`; catalog `Aspire.Hosting` 13.5.3 (`:113`) |
| 3 | CommunityToolkit Aspire Dapr hosting `13.5.0-preview.1.260825-0345` | **NEWLY-WRONG** | Committed catalog `references/Hexalith.Builds/Props/Directory.Packages.props:136` = `13.5.1-beta.751`. The cited value is what `a32cb422` carried (`git show a32cb422:Props/Directory.Packages.props`) |
| 4 | Dapr .NET SDK `1.18.5` | **NEWLY-WRONG** | Committed catalog `:139-146` = `1.18.7` for all eight Dapr packages. `a32cb422` carried `1.18.5` |
| 5 | Dapr CI runtime `1.18.2` | **CONFIRMED** | `.github/workflows/ci.yml:16` `DAPR_RUNTIME_VERSION: '1.18.2'`; `nightly.yml:13` same |
| 6 | Hexalith.EventStore package lane `3.103.0` | **CONFIRMED** | catalog `:8` `HexalithEventStoreVersion` = `3.103.0` |
| 7 | EventStore source-lane gitlink `fe05a796…` / `v3.103.0-43-gfe05a796` / moved from `6b0247ac` (40) in `a51080a5` | **CONFIRMED** | see §2.1 — confirmed four independent ways |
| 8 | Redis Stack Server `7.4.0-v8` | **CONFIRMED** | `deploy/kubernetes/base/redis-statefulset.yaml:37` = `redis/redis-stack-server:7.4.0-v8@sha256:798ab84d…`, digest matches Docker Hub's tag digest exactly |
| 9 | NRedisStack `1.7.4` | **CONFIRMED** | catalog `:258` |
| 10 | StackExchange.Redis `3.1.31` | **NEWLY-WRONG** | Committed catalog `:188` = `3.2.0`. `a32cb422` carried `3.1.31` |
| 11 | FalkorDB `4.12.0` | **CONFIRMED** | `deploy/kubernetes/base/falkordb-statefulset.yaml:37` = `falkordb/falkordb:v4.12.0@sha256:7927eb19…` |
| 12 | NFalkorDB `1.2.0` | **CONFIRMED** | catalog `:257` |
| 13 | PostgreSQL `18.4-trixie`, digest-pinned, behind 18.6 | **CONFIRMED** | `deploy/kubernetes/base/access-telemetry-postgresql.yaml:144` = `postgres:18.4-trixie@sha256:3a82e1f5…`; 18.6 released 2026-08-13 |
| 14 | OpenBao image `2.6.0` / chart `0.28.5` | **CONFIRMED** | `deploy/openbao/values.yaml:27` tag `2.6.0@sha256:900bb64d…`; `:2` comment `version 0.28.5`; `deploy/openbao/smoke-test.yaml:32` same image |
| 15 | Embedding Google / `gemini-embedding-001` / `768` | **CONFIRMED** | `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:23` model name, `:59` `Dimensions = 768`; `src/Hexalith.Memories.Contracts/V1/TenantProvisioningInput.cs:12` default 768 |
| 16 | Model Context Protocol SDK `2.2.0` | **CONFIRMED** | catalog `:248`; and 2.2.0 is the current NuGet stable (2026-08-13) |
| 17 | Kreuzberg `4.10.2` | **NEWLY-WRONG** | Committed catalog `:168` = `4.10.3`. `a32cb422` carried `4.10.2` |
| 18 | Hexalith.FrontComposer `4.4.0` / Fluent UI Blazor `5.0.0-rc.5-26219.1` | **CONFIRMED** | catalog `:9` and `:226-227`; and rc.5 is the newest published prerelease, no 5.0.0 GA |
| 19 | OpenTelemetry `1.18.0` | **CONFIRMED** | catalog `:266-273` (two instrumentation packages are `1.18.0-beta.1`, which the table does not claim otherwise) |

### 1.2 The caveat paragraph (spine:266)

| # | Claim | Verdict | Evidence |
| --- | --- | --- | --- |
| 20 | "exact repository pins read from the **committed** `references/Hexalith.Builds` gitlink `a32cb422`" | **NEWLY-WRONG** | `git ls-tree HEAD references/Hexalith.Builds` = `cf52f74c983bf88496cf0280cd9788b5ebcf50de`; `git submodule status --cached` = `cf52f74c… (v4.27.3-3-gcf52f74)`. `a32cb422` was correct at pass 3 and was superseded by commit `878b397f` |
| 21 | the source-lane gitlink "is re-derived from `git submodule status --cached references/Hexalith.EventStore` at each revision" | **CONFIRMED** | that command returns `fe05a796… (v3.103.0-43-gfe05a796)`; the sentence's method is exactly reproducible |
| 22 | "**Six preconditions** stand before Production qualification, not four" and "Each carries a ledger row with a convergence path or a dated exception" | **NEWLY-WRONG** | Seven ledger rows carry `blocks Production` (spine:360, 361, 368, 369, 373, 377, 390). The six-item list omits two of them — EventStore tenant-key crypto-shredding completion (spine:361) and demonstrative erasure verification (spine:390) — while the item it does add, the external EventStore gateway, maps to spine:367, which carries **no** Production marker. And the second clause contradicts the ledger's own preamble: "no row is in either" (spine:401) |
| 23 | .NET `10.0.400` carries runtime `10.0.11` | **CONFIRMED** | SDK 10.0.400 ships runtime 10.0.11, released 2026-08-11 |
| 24 | runtime 10.0.11 "predates the 2026-09-08 security release `10.0.12` / SDK `10.0.401` fixing six CVEs" | **CONFIRMED** | 10.0.12 released 2026-09-08 with SDKs 10.0.401 and 10.0.112, six CVEs: CVE-2026-69439, -71328, -69522, -69304, -58649, -69806 |
| 25 | "`rollForward: latestFeature` floats the resolved SDK across feature bands rather than within one" | **OVERSTATED** | Policy claim correct (`latestFeature` = highest installed feature band and patch ≥ requested, per global.json docs), but 10.0.401 sits in the *same* `4xx` band as 10.0.400, so `latestPatch` would reach it too; the band argument does no work for this bump |
| 26 | "the pinned `Microsoft.AspNetCore.*` packages do not roll forward, so the package-side exposure is real until the bump lands" | **NEWLY-WRONG** | The committed catalog pins `Microsoft.AspNetCore.*` at **`10.0.12`** (`:172-186`ff). The diff `a32cb422..cf52f74c` shows the whole family moving `10.0.11` → `10.0.12`. The package-side exposure is closed at the revision the spine is describing; what remains is `global.json`'s SDK `10.0.400` and the floating base image |
| 27 | container base image: `Directory.Build.targets:3` floating `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` for all four production images | **CONFIRMED** | `Directory.Build.targets:3` is exactly that string, and it is the only `ContainerBaseImage` in the repo; exactly four projects set `EnableContainer=true` (Server, Mcp, AccessTelemetry, AccessTelemetry.Clock). **Pass 3's STILL-WRONG VER3-02 is resolved** |
| 28 | "the remediation already sits in the checked-out Builds worktree, making this a one-bump fix rather than an upstream wait" | **OVERSTATED** | The Builds worktree is clean at the committed gitlink (`git status --short` empty; HEAD = `cf52f74c` = the gitlink). The remediation is *committed*, not sitting uncommitted in a worktree; and Builds' own `global.json:3` is already `10.0.401`. The one bump left is this repo's `global.json` |
| 29 | OpenBao "must be upgraded … to at least the security-fixed `2.6.2` line" | **CONFIRMED** | v2.6.2 (2026-08-18) is the latest stable and is a security release: GHSA-rh46-vc3j-w2w3 (inline-auth token creation) and GHSA-g892-p242-8g86 (PKI `allowed_ip_sans_cidr`) |
| 30 | "the current chart line (`0.29.x`, presently `0.29.4`) assessed rather than the superseded `0.28.6`" | **CONFIRMED** | openbao-helm releases: 0.29.4 newest, 0.29.0-0.29.4 exist, and 0.28.6 exists above the pinned 0.28.5 |
| 31 | PostgreSQL 18.6 "(2026-08-13; 18.5 was skipped), which fixes 28 CVEs, fourteen of them at CVSS 8.8 and seventeen at 8.0 or above, including RCE and SQL-injection classes" | **CONFIRMED** | Release announcement: 2026-08-13, 18.5 skipped, 28 security issues. Counted from the published CVSS v3.1 scores: exactly **14** at 8.8 and **17** at ≥8.0. **Pass 3's VER3-01 is resolved in this paragraph** (but not in the ledger — see §1.4) |
| 32 | Redis Stack `7.4.0-v8` "is the terminal tag of its ended-maintenance line … maintenance ended December 2025 … not been rebuilt since 2025-11-03 … roughly ten months" | **CONFIRMED** | Docker Hub tag API: `7.4.0-v8` `tag_last_pushed` = `2025-11-03T13:49:49Z`, no newer 7.4 tag, `latest` points at the same push. Redis Stack 6.2/7.2/7.4 maintenance ended December 2025. 2025-11-03 → 2026-09-12 is ten months |
| 33 | "The 2026-11-30 date sometimes cited is Redis Software 7.4's EOL, a different product" | **CONFIRMED** | Redis Software product lifecycle: 7.4 EOL 2026-11-30; that is Redis Enterprise/Software, not the Redis Stack OSS image |
| 34 | FalkorDB `4.12.0` "is four released minor lines behind `v4.20.4` — FalkorDB ships even-numbered minors" | **CONFIRMED** | Newest release v4.20.4 (2026-08-20); release list shows 4.20.x then 4.18.x with no odd minors, so 4.14/4.16/4.18/4.20 = four lines |
| 35 | CommunityToolkit prerelease accepted: "no stable 13.5.x is published, though 13.0.0 is stable and 13.5.1-beta is newer than the pin" | **CONFIRMED** (upstream) / superseded in fact | NuGet: latest stable 13.0.0 (2025-11-25), latest prerelease 13.5.1-beta.752 (2026-09-12), no stable 13.5.x. The reasoning holds; the pin it reasons about is stale (row 3) — the repository now pins 13.5.1-beta.751 |
| 36 | Fluent UI Blazor `5.0.0-rc.5` accepted, "no 5.0.0 GA is published, though 4.x is stable" | **CONFIRMED** | NuGet: newest prerelease 5.0.0-rc.5-26219.1 (2026-08-09), newest stable 4.14.4 (2026-07-30), no 5.0.0 GA |
| 37 | "floating AppHost/Aspire Redis and Falkor defaults are alignment gaps" | **CONFIRMED** | `src/Hexalith.Memories.AppHost/Program.cs:137,278` and `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:102,114` add `redis/redis-stack` and `falkordb/falkordb` with no tag |
| 38 | GO-2026-5970 sweep (ledger spine:369) | **CONFIRMED** | GO-2026-5970 published 2026-07-22, modified 2026-09-07; `openbao/dependency-vulnerabilities` issue #231 records it against `release/2.6.x` with a govulncheck-reachable call path |

### 1.3 Code, path, and line-number claims outside the Stack

Every line number cited anywhere in the spine was opened and read. All three code line citations are exact.

| # | Claim | Verdict | Evidence |
| --- | --- | --- | --- |
| 39 | `EventStoreDedupKey.cs:17` = `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` | **CONFIRMED** | `src/Hexalith.Memories.EventStore/EventStoreDedupKey.cs:17` is `=> $"dedup:{tenantId}:{caseId}:{ComputeHash(sourceUri)}";`, and `:19-20` defines `ComputeHash` as lowercase hex SHA-256 |
| 40 | `TenantEventRoutingOptions.cs:21` matches `source` prefixes case-insensitively | **CONFIRMED** | line 21 is `public Dictionary<string, string> SourceToTenantMap { get; init; } = new(StringComparer.OrdinalIgnoreCase);` |
| 41 | `Directory.Build.targets:3` (cited twice, spine:266 and spine:380) | **CONFIRMED** | line 3 exactly; see row 27 |
| 42 | `prd.md:1234` carries the `[DERIVED]` 2026-10-31 G1-prerequisite checkpoint | **CONFIRMED** | `_bmad-output/planning-artifacts/prd.md:1234` is the `[DERIVED]` G1-prerequisite sprint-selection date line |
| 43 | `tools/release-packages.json` is the publication inventory | **CONFIRMED** | file present; `tools/release-packages.schema.json` and `tools/validate-release-packages.ps1` alongside it |
| 44 | `Contracts.V1` `EvidencePacketState` ships exactly the eight listed values | **CONFIRMED** | `src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs:178-203`: Complete, Partial, Weak, Empty, Stale, Degraded, Unauthorized, PendingExpansion |
| 45 | `Contracts.V1` has no wire-name register file (spine:378) | **CONFIRMED** | `src/Hexalith.Memories.Contracts/` holds only the csproj, `Placeholder.cs`, `README.md`, and `V1/`; no register file by any name |
| 46 | "105 files under `Hexalith.Memories.Server`" use provider SDKs | **CONFIRMED** | `grep -rl "StackExchange\.Redis\|NRedisStack\|NFalkorDB\|FalkorDB" src/Hexalith.Memories.Server --include=*.cs \| wc -l` = **105** (448 .cs files total) |
| 47 | `Adapters.Redis` / `Adapters.FalkorDb` namespaces do not exist | **CONFIRMED** | no match for either namespace anywhere under `src/` |
| 48 | `FusionEngine.ScoresTie` treats `NaN == NaN` as a tie | **CONFIRMED** | `src/Hexalith.Memories.Server/Search/FusionEngine.cs:253-254`: `=> left.Equals(right) \|\| (double.IsNaN(left) && double.IsNaN(right));` |
| 49 | `Fuse_Bm25NaN_*` / `Fuse_Bm25Infinity_*` tests lock the behaviour | **CONFIRMED** | `tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs:359` and `:374` |
| 50 | "the read path defaults an unpersisted status to `Indexed`" | **CONFIRMED** | `src/Hexalith.Memories.Server/Ingestion/DirectoryBatchStatusMapper.cs:66` returns `"indexed"` on a null workflow output and `:78` returns `"indexed"` from the catch block |
| 51 | "`Indexed` is inferred from syntactic-hash presence alone" | **UNVERIFIED** | The default-to-indexed half is proven (row 50); I did not trace a syntactic-hash-presence inference to a specific site. Carried forward, not disputed |
| 52 | Kubernetes has no EventStore gateway workload | **CONFIRMED** | `deploy/kubernetes/base/` contains server, mcp, access-telemetry (+PostgreSQL), redis, falkordb, dapr, services, RBAC only |
| 53 | MCP deployment runs `replicas: 2` with no gate flag | **CONFIRMED** | `deploy/kubernetes/base/mcp-deployment.yaml:9` `replicas: 2`; no ingress manifest exists |
| 54 | `tools/` utilities outside the inventory gate; `MigrateEmbeddingVectors` takes a direct provider-SDK dependency | **CONFIRMED** | `tools/MigrateEmbeddingVectors/MigrateEmbeddingVectors.csproj:20` `<PackageReference Include="StackExchange.Redis" />`; `Program.cs:21` `using StackExchange.Redis;`; `tools/GenerateBenchmarkVectors` also present |
| 55 | Web specimen host exists; `src/Hexalith.Memories.Web` non-packable; the "no runnable host yet" comment is stale | **CONFIRMED** | `tests/Hexalith.Memories.Web.SpecimenHost/` exists; CI job `web-e2e-specimen` at `.github/workflows/ci.yml:335-336`; the stale comment is still at `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj:5` |
| 56 | Route families: `/api/v1`, `/events/ingest`, `/health` `/alive` `/ready`, `/v1/access-telemetry/*`, `/v1/time/attest` | **CONFIRMED** | `CloudEventEnvelopeCaptureMiddleware.cs:75` `/events/ingest`; `ServiceDefaults/Health/HealthEndpointPaths.cs:16,19,22`; `Telemetry/AccessTelemetryLifecycle/*` uses `v1/access-telemetry/write|heartbeat|validate` and `v1/time/attest`; `/api/v1` throughout `Contracts/V1` |
| 57 | "the current N=8 diagnostic cannot pass G1" | **CONFIRMED** | `prd.md:99`, `:173`, `:666` all record the shipped N=8 benchmark as diagnostic-only against a G1 protocol requiring N ≥ 50 |
| 58 | 44 ledger rows, every Disposition beginning `blocker` | **CONFIRMED** | 44 data rows between the header at spine:354 and spine:399; `` `blocker` `` occurs 46 times = 44 rows + 2 preamble mentions |
| 59 | "digest guard, which currently covers Kubernetes manifests only" (spine:380) | **OVERSTATED** | The guard also pins `deploy/openbao/values.yaml` (`ProductionDeploymentArtifactsTests.cs:329`) and the CI `OPENBAO_IMAGE` env var (`CiTestInventoryTests.cs:842`). The substance — no coverage of `ContainerBaseImage` — holds |

### 1.4 Carried STILL-WRONG

| # | Claim | Verdict | Evidence |
| --- | --- | --- | --- |
| 60 | PostgreSQL ledger row (spine:373): "the 18.6 security release fixing 28 CVEs, **four** at CVSS 8.8" | **STILL-WRONG** | Fourteen at 8.8, seventeen at ≥8.0. The Stack paragraph was corrected to fourteen (row 31) and this row was not, so the same document now states both numbers. Third pass carrying this figure |

### 1.5 Resolved since pass 3

- **VER3-02 resolved.** The base-image sentence is now factually right and carries its own AD-19 ledger row (spine:380).
- **VER3-03 resolved.** The gitlink/package-lane divergence is annotated in the Stack row and AD-19's Rule now states the per-revision re-derivation obligation, which is what made §2.1 checkable.
- **Upstream `prd.md` embedding model resolved.** `prd.md:799` reads `gemini-embedding-001` / 768, matching `EmbeddingProviderDefaults.cs:23,59`.

---

## 2. The four claims this pass added from code or git

### 2.1 EventStore source-lane gitlink — **CONFIRMED, all four sub-claims**

```
git submodule status --cached references/Hexalith.EventStore
  fe05a796faa46f6486f615c119809148347c9a25 references/Hexalith.EventStore (v3.103.0-43-gfe05a796)
git -C references/Hexalith.EventStore describe --tags fe05a796…   → v3.103.0-43-gfe05a796
git -C references/Hexalith.EventStore describe --tags 6b0247ac…   → v3.103.0-40-g6b0247ac
git -C references/Hexalith.EventStore rev-list --count v3.103.0..fe05a796…  → 43
git ls-tree a51080a5  references/Hexalith.EventStore → fe05a796…
git ls-tree a51080a5^ references/Hexalith.EventStore → 6b0247ac…
```

The hash, the describe string, the 43-commit distance, the prior `6b0247ac` / 40, and the attribution to commit
`a51080a5` are all exactly right. "Second consecutive revision at which re-derivation changed it" is consistent
with pass 3 having recorded `6b0247ac`.

The failure is the *other* gitlink in the same caveat sentence. `878b397f` — the commit directly before
`a51080a5` — moved Builds from `a32cb422` (`v4.27.2-10-ga32cb42`) to `cf52f74c` (`v4.27.3-3-gcf52f74`). One
gitlink was re-derived and its neighbour was not, and the one that was not is the one the whole Stack table
reads from. See §1.2 rows 20, 26 and §1.1 rows 3, 4, 10, 17.

### 2.2 `TenantStatus` and `TenantLifecycleStatusUpdatedEvent` — **CONFIRMED**

`src/Hexalith.Memories.Contracts/V1/TenantStatus.cs:12-27` declares exactly five members, in the spine's order:
`Provisioning`, `Active`, `Deleting`, `Failed`, `CompensationFailed`. No sixth member, and no parallel
vocabulary elsewhere — so AD-6's ratification of this enum as the single definition site is sound, and the
ledger row's "`Deactivated` and `Erased` are absent" is right.

The event is real and is on the claimed partition:
`src/Hexalith.Memories.EventStore/Domain/Events/TenantLifecycleStatusUpdatedEvent.cs:11` defines it;
`Domain/Aggregates/MemoriesTenantAggregate.cs:37` emits it; `Domain/Commands/UpdateTenantLifecycleStatusCommand.cs:18`
binds `Domain => MemoriesDomain.Tenants`; `Domain/MemoriesDomain.cs:18` resolves that to `"memories-tenants"`.
`Domain/States/MemoriesTenantAggregateState.cs:33` applies it.

### 2.3 `SourceToTenantMap` case-insensitivity and `AutoProvisionRoutedTenants` — **CONFIRMED / OVERSTATED**

The comparison claim is exact at the cited line (§1.3 row 40), and `:19` documents "Longest-prefix wins,
case-insensitive" in prose as well, so the contradiction with AD-5's ordinal rule and AD-23's lowercase grammar
is real.

`AutoProvisionRoutedTenants` exists at `TenantEventRoutingOptions.cs:29` (default `false`) and does cause tenant
creation: `src/Hexalith.Memories.Server/EventStoreIntegration/RoutedTenantProvisioningStartupService.cs:67`
gates on it, `:88-99` calls `TenantExistsAsync` then schedules `TenantProvisioningInput` and waits for `Active`;
`EventStoreRoutingConfigValidator.cs:113` also treats `IsDevelopment()` as equivalent, which widens the path
beyond the flag.

The wording is where it overshoots. The ledger says the flag "can create tenants **outside AD-6**", but the
startup service creates them *through* the tenant-provisioning workflow — i.e. through AD-6's own mechanism.
The real defect is narrower and sharper: a startup hot path initiates provisioning with **no operator
principal**, which is AD-6's `Prevents` clause and AD-5's authorization requirement, not an end-run around the
lifecycle workflow. As written the row overstates the mechanism and understates the authorization defect.

### 2.4 Preflight reservation key family — **CONFIRMED, both halves**

`EventStoreDedupKey.cs:17` is exactly the claimed family, and `:11-13` documents that the Server's
`DedupKeyBuilder` is deliberately duplicated here, which corroborates the ledger row about one `dedup:` prefix
serving two mechanisms. `grep -rn "memories:preflight" src/ | wc -l` = **0**; the string appears only in this
architecture folder's own memlog and reviews. Recording the shipped family instead of the invented prefix is
the right call and AD-23's "already does for `source`" follows from `:19-20`.

---

## 3. Live upstream re-check

All of §1.1/§1.2's upstream rows were re-fetched today rather than recalled. Sources:

- .NET 10.0.12 / SDK 10.0.401, 2026-09-08, six CVEs; SDK 10.0.400 ↔ runtime 10.0.11 (2026-08-11) — <https://versionsof.net/core/10.0/10.0.12/>, <https://versionsof.net/core/10.0/10.0.11/>
- `rollForward` semantics — <https://learn.microsoft.com/dotnet/core/tools/global-json#globaljson-schema>
- OpenBao v2.6.2 (2026-08-18, security) — <https://github.com/openbao/openbao/releases>; chart 0.29.4 newest, 0.28.6 exists — <https://github.com/openbao/openbao-helm/releases>; GO-2026-5970 — <https://github.com/openbao/dependency-vulnerabilities/issues/231>, <https://pkg.go.dev/vuln/GO-2026-5970>
- PostgreSQL 18.6, 2026-08-13, 28 issues, per-CVE CVSS v3.1 — <https://www.postgresql.org/about/news/postgresql-186-1711-1615-1519-1424-and-19-beta-3-released-3365/>
- Redis Stack `7.4.0-v8` last pushed 2025-11-03 and terminal — <https://hub.docker.com/v2/repositories/redis/redis-stack-server/tags/7.4.0-v8>; maintenance ended December 2025; Redis Software 7.4 EOL 2026-11-30 — <https://redis.io/docs/latest/operate/rs/installing-upgrading/product-lifecycle/>
- FalkorDB v4.20.4 (2026-08-20), even-minor cadence — <https://github.com/FalkorDB/FalkorDB/releases>
- CommunityToolkit.Aspire.Hosting.Dapr — stable 13.0.0 (2025-11-25), newest prerelease 13.5.1-beta.752 (2026-09-12), no stable 13.5.x — <https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Dapr>
- Microsoft.FluentUI.AspNetCore.Components — newest prerelease 5.0.0-rc.5-26219.1 (2026-08-09), newest stable 4.14.4, no 5.0.0 GA — <https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components>
- ModelContextProtocol 2.2.0 current stable (2026-08-13) — <https://www.nuget.org/packages/ModelContextProtocol>

Every named technology still exists, is still the product the spine thinks it is, and still fits its stated
role. No dependency in the table has been renamed, withdrawn, or absorbed into another product since the last
pass — with the standing exception the spine already states correctly, Redis Stack, whose function has moved
into Redis Open Source 8.x.

---

## 4. New claims

### 4.1 `memories-platform` and "EventStore carries four partitions" — **OVERSTATED**

`src/Hexalith.Memories.EventStore/Domain/MemoriesDomain.cs` declares exactly three constants: `Cases`
(`:12`), `MemoryUnits` (`:15`), `Tenants` (`:18`). `grep -rn "memories-platform" src tests deploy | wc -l` =
**0**.

The spine gets this right in two places and wrong in one. AD-21 (spine:204) is a Rule, and the preamble
declares Rules to be adopted target — fine. The ledger row at spine:377 says outright that the partition and
its bootstrap step "have no implementation" and must be created — fine, and correctly `blocker`.

But the Structural Seed (spine:314) says "EventStore **carries** four partitions: … plus AD-21's
`memories-platform`, which … **is created** by the platform-bootstrap step before any tenant exists." Present
indicative, no target qualifier, in a section that elsewhere annotates current reality explicitly ("Mixed
EventStore/CloudEvent integration", "no port interface today") and whose sibling Conventions row is careful to
say the adapter namespaces "**will use**" / "do not exist today". A reader taking the Seed at its word would
conclude the partition is live. One clause — "will carry four partitions; three exist today" — fixes it.

### 4.2 Six preconditions — **NEWLY-WRONG**, see §1.2 row 22

Seven ledger rows carry `blocks Production`; the enumeration names six preconditions; the two sets differ by
three items in total. The claim "not four" is directionally right and the correction was worth making — the
count it lands on is not the one the ledger supports.

### 4.3 AD-19's digest obligation vs the Stack table — **NEWLY-WRONG, and the rule loses**

AD-19 (spine:192) now states two new things:

1. "**every consumer of a container default resolves images from that one digest set** — AppHost,
   `deploy/kubernetes`, CI, integration harnesses, and the `ContainerBaseImage` of every owned image alike".
2. "**The Stack table records the qualified digest alongside the tag for every image the platform runs.**"

The second is false of the document that states it. The Stack table records **zero** digests; `sha256` occurs
twice in the entire spine and both occurrences are the `sha256(sourceUri)` component of the dedup key. Five
images the platform runs are digest-pinned in the repository — redis-stack-server `@sha256:798ab84d…`,
falkordb `@sha256:7927eb19…`, postgres `@sha256:3a82e1f5…`, openbao `@sha256:900bb64d…` — and not one of those
digests reached the table. A rule that obliges a table the same revision leaves empty is self-refuting, and by
the spine's own preamble ("a rule that names a mechanism the architecture has not yet defined carries a ledger
row") this needs either the digests or a row.

The first clause creates a second, unledgered obligation. The integration harnesses resolve a **different**
image and digest set from `deploy/kubernetes`:

| Consumer | Redis | FalkorDB |
| --- | --- | --- |
| `deploy/kubernetes/base/*-statefulset.yaml` | `redis/redis-stack-server:7.4.0-v8@sha256:798ab84d…` | `falkordb/falkordb:v4.12.0@sha256:7927eb19…` |
| `tests/…IntegrationTests/Fixtures/RedisStackFixture.cs:14`, `CompositeSearchFixture.cs:14-15`, `DaprStateSidecarFixture.cs:23`, `FalkorDbFixture.cs:14`, `tests/…Benchmarks/Fixtures/BenchmarkFixture.cs:28-29` | `redis/redis-stack:latest@sha256:880df9c2…` | `falkordb/falkordb:latest@sha256:4b7c7990…` |

Different repository (`redis-stack` vs `redis-stack-server`), floating `latest` tag, and a different digest.
So the integration evidence AD-19 requires "against that recorded digest set" is today produced against a
different one. The existing ledger row (spine:363) names only "AppHost and Aspire defaults"; nothing names the
harness divergence, and AD-19 acquired that obligation in this revision.

---

## 5. Findings

### VER4-01 — The Builds gitlink was not re-derived, and five Stack facts plus one security conclusion are stale. **HIGH**

The caveat's sourcing sentence names `a32cb422`; the committed gitlink is
`cf52f74c983bf88496cf0280cd9788b5ebcf50de`, moved by commit `878b397f`. Consequences, each independently
confirmed against `references/Hexalith.Builds/Props/Directory.Packages.props` at the real gitlink: Dapr .NET SDK
is `1.18.7` not `1.18.5`; CommunityToolkit Aspire Dapr is `13.5.1-beta.751` not `13.5.0-preview.1.260825-0345`;
Kreuzberg is `4.10.3` not `4.10.2`; StackExchange.Redis is `3.2.0` not `3.1.31`; and — the one that changes a
security conclusion — `Microsoft.AspNetCore.*` is pinned at `10.0.12`, so the caveat's "the package-side
exposure is real until the bump lands" is no longer true. Builds' own `global.json` is already `10.0.401` too.
What survives of the .NET row is the repository's own `global.json:3` = `10.0.400` (SDK-bundled runtime
`10.0.11`) and the floating base image at `Directory.Build.targets:3`. Fix: re-derive both gitlinks in the same
sentence, restate the five rows, and narrow the .NET exposure to the SDK pin and the base image.

### VER4-02 — The PostgreSQL 8.8 count is corrected in the Stack paragraph and still wrong in the ledger. **HIGH**

spine:266 now says "fourteen of them at CVSS 8.8 and seventeen at 8.0 or above", which is exactly right;
spine:373 still says "four at CVSS 8.8". The document asserts both. Third pass carrying the wrong figure, now
with the additional cost of an internal contradiction. Fix: `four` → `fourteen` in the ledger row.

### VER4-03 — AD-19's digest rule contradicts the Stack table it obliges, and its new integration-harness clause has no ledger row. **HIGH**

See §4.3. The Stack table records no digests at all while AD-19 says it records one per image; and the
harnesses pin `redis/redis-stack:latest@sha256:880df9c2…` / `falkordb/falkordb:latest@sha256:4b7c7990…`
against Kubernetes' `redis-stack-server:7.4.0-v8@sha256:798ab84d…` / `falkordb:v4.12.0@sha256:7927eb19…`.
Fix: add the four known digests to the Stack table (or soften the rule to "records the qualified digest where
one is recorded" and ledger the gap), and add a row for the harness digest divergence.

### VER4-04 — "EventStore carries four partitions" states a target as current reality. **MEDIUM**

See §4.1. Three partitions exist; `memories-platform` has zero occurrences in `src/`, `tests/`, or `deploy/`.
The ledger is right and the Seed is not. Fix: one qualifying clause in spine:314.

### VER4-05 — The six-precondition count does not match the ledger's seven Production-blocking rows. **MEDIUM**

See §1.2 row 22. Also, "Each carries a ledger row with a convergence path or a dated exception" reads against
the ledger's own "no row is in either" (spine:401). Fix: derive the count from the `blocks Production` rows, or
say explicitly that two erasure-completion rows are folded into one precondition and that the EventStore
gateway row needs the marker added.

### VER4-06 — Three smaller overstatements. **LOW**

(a) "the remediation already sits in the checked-out Builds worktree" — the worktree is clean at the committed
gitlink; the remediation is committed. (b) "`rollForward: latestFeature` floats … across feature bands rather
than within one" — true as policy, but 10.0.401 is in the same `4xx` band, so the argument is inert here.
(c) "digest guard, which currently covers Kubernetes manifests only" — it also covers
`deploy/openbao/values.yaml` and the CI `OPENBAO_IMAGE` variable; the `ContainerBaseImage` gap is the real
point.

### VER4-07 — "`AutoProvisionRoutedTenants` can create tenants outside AD-6" misdescribes the mechanism. **LOW**

The startup service schedules the tenant-provisioning workflow
(`RoutedTenantProvisioningStartupService.cs:88-99`), so creation goes through AD-6's mechanism with no operator
principal — an AD-5 authorization defect and an AD-6 `Prevents` violation, not a bypass of the lifecycle
workflow. `EventStoreRoutingConfigValidator.cs:113` widening the path to any `IsDevelopment()` host is worth
naming too. Fix: restate the row as "initiates provisioning from a startup path with no operator principal".

---

## 6. What this lens verified and did not

Verified by command or fetch: 19 Stack rows, 19 caveat assertions, 21 code/path/line claims, 4 new-claim
checks. Read-only throughout — no spine, memlog, sprint-status, or source file was modified, nothing was
staged or committed, no submodule pointer was touched, and no build or restore was run.

Not verified: the "Indexed is inferred from syntactic-hash presence alone" half of spine:357 (§1.3 row 51). The
memlog was treated as claim throughout; all four of its code/git assertions independently checked out, and the
one thing it got wrong is what it did *not* claim — that the Builds gitlink had also moved.
