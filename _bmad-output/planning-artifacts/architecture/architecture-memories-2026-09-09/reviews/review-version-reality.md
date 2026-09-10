# Version and Brownfield Reality Review

Date: 2026-09-09  
Reviewer lens: repository reality, dependency/currentness, and implementation-gap candor  
Artifact reviewed: `ARCHITECTURE-SPINE.md`

## Verdict

**CHANGES REQUIRED — do not finalize this spine yet.**

The spine is mechanically sound (`lint_spine.py`: 0 findings), correctly replaces most obsolete legacy assumptions, and candidly records six important implementation gaps. It nevertheless presents several target rules as adopted/current without recording all material brownfield divergences. One production security pin is below the current security patch, one primary datastore family is past upstream maintenance, and several structural/convention statements directly contradict current projects or public contracts.

Finding count: **1 critical, 8 high, 4 medium**.

## Critical finding

### C1 — OpenBao is pinned below a published security patch

**Spine:** Stack pins OpenBao `2.6.0` and Helm chart `0.28.5` (`ARCHITECTURE-SPINE.md:166`).  
**Repository:** The same image and digest are used by local AppHost and production values (`src/Hexalith.Memories.AppHost/OpenBaoDevelopmentProfile.cs:15-18`, `deploy/openbao/values.yaml:1-3,24-28`), so the row is repository-accurate.  
**Current upstream reality:** OpenBao `2.6.2` contains security fixes, including GHSA-rh46-vc3j-w2w3 and GHSA-g892-p242-8g86, according to the [official OpenBao changelog](https://github.com/openbao/openbao/blob/main/CHANGELOG.md). The [official Helm releases](https://github.com/openbao/openbao-helm/releases) also show `0.28.6` after `0.28.5`.

**Why this blocks finalization:** The system’s secret authority is being bound to a version with published security fixes available, while AD-13 asserts a least-privileged security boundary. An exact pin is not sufficient when its security status is stale.

**Required resolution:** Update and re-qualify the image/digest at least to the patched 2.6 line, and assess the chart patch. If immediate update is impossible, record a time-bounded security exception, affected-advisory assessment, owner, and mandatory revisit trigger; do not describe `2.6.0` as verified-current.

## High findings

### H1 — The public ingestion status convention names states that do not exist in V1

**Spine:** The mutation/status convention says public states remain `Pending`, `Extracting`, `Embedding`, `Projecting`, `Indexed`, and `Failed` (`ARCHITECTURE-SPINE.md:150`).  
**Code:** `MemoryUnitStatus` is `Queued`, `Extracting`, `Embedding`, `Indexing`, `Indexed`, and `Failed` (`src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs:7-15`). `IngestionWorkflowStatus` exposes that enum rather than a renamed status contract (`src/Hexalith.Memories.Contracts/V1/IngestionWorkflowStatus.cs:18-27`).

**Impact:** This is a direct public-contract contradiction and appears to carry PRD/legacy vocabulary forward as current reality. A builder following the spine would create an incompatible V1 surface.

**Required resolution:** Use the current V1 names (`Queued`/`Indexing`) or explicitly adopt a future breaking/additive contract change and list it as an alignment gap. The present claim that internal states map “without renaming V1” is false.

### H2 — The structural seed misrepresents the EventStore project as a pure domain boundary

**Spine:** The seed labels `Hexalith.Memories.EventStore` “Pure aggregate handlers, domain commands/events” (`ARCHITECTURE-SPINE.md:179`), and AD-1 says domain modules remain free of orchestration dependencies (`:57`).  
**Code:** `Hexalith.Memories.EventStore` is a host-integration package whose description is Dapr pub/sub ingestion. Its project references `Dapr.AspNetCore`, `Dapr.Client`, `Dapr.Workflow`, `StackExchange.Redis`, and `Microsoft.AspNetCore.App` (`src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj:4-15,30-39`). Its composition root registers controllers, workflow scheduling, Dapr state, and direct Redis deduplication (`src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs:13-28,76-94`). Pure aggregate handlers are only a folder within that assembly.

**Impact:** The diagram can be read as a logical layer, but the project tree claims a compile-time boundary that does not exist. Independently built units could incorrectly depend on the assembly believing it is domain-only.

**Required resolution:** Describe the project as EventStore/CloudEvent integration plus a pure `Domain/` namespace, or split the domain types into a dependency-pure project before preserving the current wording. Keep logical layers distinct from physical projects.

### H3 — AD-7’s “one named atomic Redis operation” rule contradicts many adopted code paths

**Spine:** AD-7 limits direct atomic Redis use to “the named reservation operation with `SET NX` plus TTL semantics” (`ARCHITECTURE-SPINE.md:93`).  
**Code:** Beyond ingress reservation (`src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs:81,168`), direct Redis atomicity is used for EventStore preflight dedup (`EventStoreIntegration/RedisPreflightDedupStore.cs:45`), permanent dedup promotion (`Activities/Ingestion/SaveDedupKeyActivity.cs:43-63`), failed-unit registries (`Ingestion/FailedUnitsRegistry.cs:119,178`), import leases and Lua cleanup (`Import/RedisImportStagingStore.cs:279,405`), derived-store fences/idempotence (`DerivedStores/RedisDerivedStoreService.cs:321-324,459-586`), and embedding migration locks/transactions (`Migration/RedisEmbeddingMigrationStore.cs:337-366,782`). Case membership also uses `HashSet(..., When.NotExists)` (`Cases/CaseService.cs:503,521`).

**Impact:** The rule is not enforceable against the current architecture and would classify necessary idempotency, migration, and fencing behavior as violations.

**Required resolution:** Enumerate approved categories/owners of direct Redis atomic operations and forbid them outside adapters, or add every nonconforming path to the gap ledger. Do not retain the singular-reservation claim.

### H4 — AD-8 promises automatic graph seeding, but current hybrid search silently skips graph

**Spine:** AD-8 prevents missing starts from silently disabling graph and requires seeds from the top five syntactic plus top five semantic hits (`ARCHITECTURE-SPINE.md:98-99`).  
**Code:** `HybridSearchService` logs and skips the graph axis whenever `graphStartNodeId` is null (`src/Hexalith.Memories.Server/Search/HybridSearchService.cs:168-186`). The endpoint removes graph from explanation axes when no start node exists (`Endpoints/SearchEndpoints.cs:983-1003`). No top-five seed derivation is present. The RRF details themselves are correct: `k=10`, deterministic ordinal `MemoryUnitId` ties, and the stated weights are implemented (`Search/FusionEngine.cs:17-22,126-133,245-246`; `Contracts/V1/FusionWeights.cs:23-44`).

**Impact:** A load-bearing retrieval behavior is target-only but missing from `Current Alignment Gaps`; the rule’s stated prevention is exactly what current code does.

**Required resolution:** Add the missing graph-seed behavior as an explicit gap or change AD-8 to ratify explicit-start-only graph retrieval. Keep the verified RRF constants.

### H5 — AD-2 overstates authoritative replay/rebuild capability

**Spine:** AD-2 requires all derived stores to rebuild from authoritative EventStore events (`ARCHITECTURE-SPINE.md:63`), and the operational consistency boundary repeats that projections are replayable (`:219`).  
**Code/docs:** The current repair workflow still uses `{tenantId}:mu:{memoryUnitId}` syntactic hashes as its operational input (`docs/dev/consistency.md:145-150`; guarded by `tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs:55-83`). Restore uses staged export payloads (`src/Hexalith.Memories.Server/Workflows/RestoreWorkflow.cs:39-61`). There is no general EventStore replay-to-all-projections implementation. The existing gap table admits the ordinary-ingestion command-boundary problem but not the rebuild-source problem (`ARCHITECTURE-SPINE.md:246`).

**Impact:** “Rebuildable” is a target data-ownership decision, not a fully available recovery mechanism. Operators/builders could infer recovery guarantees the code does not provide.

**Required resolution:** Add authoritative replay/rebuild plumbing as a gap, distinguishing the current syntactic-input repair and export restore mechanisms from the target EventStore replay path.

### H6 — AD-13 says application secrets come through Dapr/OpenBao, but backend secrets are injected directly

**Spine:** Applications request named secrets through Dapr secret stores (`ARCHITECTURE-SPINE.md:125-129`), and the security boundary says secrets come through Dapr/OpenBao (`:220`).  
**Production manifest:** Redis and FalkorDB passwords are read from Kubernetes Secrets into Server environment variables, then embedded in direct connection strings (`deploy/kubernetes/base/server-deployment.yaml:46-69`). Provider credentials do use Dapr (`src/Hexalith.Memories.Server/Ingestion/EmbeddingSecretStore.cs:35-45`), and Dapr/OpenBao scopes are real, but the universal application-secret wording is not.

**Impact:** This hides a material exception at the data-plane credential boundary, adjacent to the already acknowledged shared-credential gap.

**Required resolution:** Either route Redis/Falkor credentials through the decided secret abstraction or explicitly state that Dapr/OpenBao governs provider/application secrets while Kubernetes bootstrap/data-plane credentials remain a sanctioned exception pending tenant credential work.

### H7 — AD-15 overstates independent source/package test evidence

**Spine:** AD-15 requires independent pristine restore, build, contract, and integration evidence for every supported source and package lane (`ARCHITECTURE-SPINE.md:137-141`).  
**CI:** Release/package and Debug/source graphs receive separate restore/build steps (`.github/workflows/ci.yml:189-208`), but unit/contract tests and integration jobs build/test the Release/package graph (`:248-321` and later integration jobs). No corresponding source-mode contract/integration test lane is defined.

**Impact:** Two build graphs are proven, but two full evidence lanes are not. The invariant is a valid target, yet its missing test half is absent from the gap ledger.

**Required resolution:** Add source-mode contract/integration execution or record this as a release-evidence alignment gap. “Pristine” should also identify how cross-step build artifacts are isolated if it remains binding language.

### H8 — Local/Aspire image resolution is floating despite the pinned-dependency invariant

**Spine:** AD-15 requires pinned dependencies, the Stack presents exact Redis/Falkor versions, and the prose says image digests remain mandatory where present (`ARCHITECTURE-SPINE.md:137-141,164-172`).  
**Code:** Production Kubernetes images are digest-pinned (`deploy/kubernetes/base/redis-statefulset.yaml:37`; `falkordb-statefulset.yaml:37`), but AppHost uses untagged `redis/redis-stack` and `falkordb/falkordb` (`src/Hexalith.Memories.AppHost/Program.cs:136-140,277-280`). The published Aspire integration does the same (`src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:100-115`).

**Impact:** Local/integration and consumer-hosted topologies can resolve versions different from the Stack table and production, defeating reproducibility and parity.

**Required resolution:** Pin AppHost and Aspire defaults to qualified tags/digests, expose an explicit consumer override where appropriate, or qualify the Stack row as production-only and record floating local/consumer images as a gap.

## Medium findings

### M1 — Redis Stack is exact but no longer a current supported platform choice

The production pin `7.4.0-v8` is exact (`deploy/kubernetes/base/redis-statefulset.yaml:37`) and matches the Stack (`ARCHITECTURE-SPINE.md:164`). However, the [official Redis Stack repository](https://github.com/redis-stack/redis-stack) states that new users should use Redis rather than Redis Stack and that maintenance releases for 7.4 stopped in December 2025. Its [official releases](https://github.com/redis-stack/redis-stack/releases) identify `7.4.0-v8` as the final maintenance line.

This is a lifecycle risk, not merely an alternate-backend question. Add a concrete Redis 8 migration/qualification trigger under Deferred or explain why the EOL pin is temporarily accepted and bounded.

### M2 — FalkorDB pin is exact but materially behind upstream

The production pin `4.12.0` and digest match the Stack (`deploy/kubernetes/base/falkordb-statefulset.yaml:37`; `ARCHITECTURE-SPINE.md:165`), but the [official FalkorDB repository](https://github.com/FalkorDB/FalkorDB) reports `4.18.8` as its latest release. An older qualified pin can be intentional, but the spine provides no compatibility reason or upgrade trigger. Record the qualification basis and revisit condition rather than implying “verified-current.”

### M3 — The graph rule includes an unimplemented kill switch and ambiguous obsolete phase language

AD-9 requires a graph kill switch and reserves EventStore-derived causal edges for “P1.5” (`ARCHITECTURE-SPINE.md:101-105`). Current graph endpoints enforce depth 0-10 and 10-second timeouts (`src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs:129-162`; `Graph/GraphTraversalService.cs:25-27`), but no graph kill switch exists. `IndexGraphActivity` already builds `CAUSED_BY` edges from `CausationId` and marks them `Explicit` (`Activities/Indexing/IndexGraphActivity.cs:91-105`). Clarify whether these are the supposedly deferred causal edges, replace legacy phase labels with capability/revisit language, and record the missing kill switch as a gap if retained.

### M4 — Packaging convention incorrectly says service/application hosts are non-packable

The spine accurately states there are nine release-manifest packages (`ARCHITECTURE-SPINE.md:153`; `tools/release-packages.json:3-39`), but then says service/application hosts remain non-packable. MCP is both an ASP.NET host/container and a NuGet package (`src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj:1-10`) and CLI is an executable packaged global tool (`src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj:9-17`). Name the actual non-packable set from `tools/release-packages.json:41-47` or say selected hosts are intentionally dual-published.

## Decision-by-decision reality matrix

| Decision | Result | Repository reality |
| --- | --- | --- |
| AD-1 platform/dependencies | **Partial** | Consumer-facing Contracts/Client/Aspire direction is real, but the EventStore project is not a pure domain assembly (H2). |
| AD-2 EventStore truth | **Partial target** | Implemented for case, annotation, deletion, and tenant lifecycle; ordinary ingestion gap is candid. General EventStore replay/rebuild is not implemented or listed (H5). |
| AD-3 three-axis completion | **Accurate target + candid gap** | Workflow still marks `Indexed` after a missing-axis note (`IngestionWorkflow.cs:431-465,608-620`), correctly disclosed at spine lines 247. Same-source-version acknowledgements remain convergence work. |
| AD-4 workflow/activity/actor roles | **Pass** | Workflows hold orchestration, activities I/O, and registered actors are non-reentrant (`MemoriesServerServiceCollectionExtensions.cs:375-499`). Determinism has architecture guards. |
| AD-5 tenant authority | **Accurate target + candid gaps** | Exact normalized claim checks occur before infrastructure (`TenantAuthorizationEndpointFilter.cs:43-87`; authorization tests at `ServerEndpointAuthorizationTests.cs:75-248`). Provenance and tenant backend principals are correctly disclosed as gaps. |
| AD-6 lifecycle ownership | **Pass as target** | Tenant provisioning owns index/graph create/verify/compensation (`TenantProvisioningWorkflow.cs:18-20,88-170`); hot-path creation is prohibited by `IndexingHotPathGuardTests.cs:48-69`. Backend-principal work is candidly incomplete. |
| AD-7 adapter boundary | **Fail** | Direct providers stay mostly below endpoints, but the singular atomic-Redis restriction is false (H3). |
| AD-8 deterministic fusion | **Partial** | RRF constants/weights/tie-break are exact; automatic graph seeding is absent and undisclosed (H4). |
| AD-9 graph bounds/phasing | **Partial** | Query parameterization, enum labels, tenant-selected graphs, depth, and time limits exist. Kill switch and phase statement need correction (M3). |
| AD-10 shared semantics | **Pass with convention correction** | Contracts and Evidence Packet are shared; MCP uses Client.Rest and four approved tools. The status-name convention is wrong (H1). |
| AD-11 provenance/opaque IDs | **Accurate target + candid gap** | Opaque string identity is consistent; caller-controlled `IngestedBy` is candidly disclosed. |
| AD-12 staged migrations | **Pass for implemented semantic migration** | Active/staging aliases, backfill verification, atomic alias cutover, rollback, and provider strategies exist (`IndexSchemaDefinitions.cs:89-104`; `RedisEmbeddingMigrationStore.cs:337-439`). Backend replacement remains a target. |
| AD-13 Dapr/OpenBao secrets | **Partial** | Provider secrets and access telemetry use Dapr/OpenBao; Redis/Falkor application credentials bypass it (H6). |
| AD-14 telemetry separation | **Pass** | Separate services/contracts, fail-closed production activation, retention/delivery qualification, and non-blocking product outcomes are reflected in code/tests/manifests. |
| AD-15 pins/two lanes | **Partial** | Central versions and release inventory are real; source tests and local image pins are not (H7/H8). |

## Stack verification

| Stack entry | Repository pin | Currentness check | Result |
| --- | --- | --- | --- |
| .NET SDK / TFM / C# | `global.json:3`, `Directory.Build.props:3-4` | [Microsoft .NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) lists SDK `10.0.400`. | Pass |
| Aspire | AppHost SDK and catalog at `13.5.3` | [NuGet Aspire.Hosting 13.5.3](https://www.nuget.org/packages/Aspire.Hosting/13.5.3) confirms the current package. | Pass |
| CommunityToolkit Dapr hosting | Catalog `13.5.0-preview.1.260825-0345` | [NuGet package history](https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Dapr/) confirms this exact prerelease. | Pass; prerelease risk is visible in version |
| Dapr .NET SDK | Catalog `1.18.5` | [Official SDK releases](https://github.com/dapr/dotnet-sdk/releases) mark `1.18.5` latest stable. | Pass |
| Hexalith.EventStore | Catalog `3.103.0` | Internal package/source pin; repository verified. | Pass, internal-currentness not externally asserted |
| Redis clients | NRedisStack `1.7.4`, StackExchange.Redis `3.1.31` | Current package registries confirm [NRedisStack](https://www.nuget.org/packages/NRedisStack/) and [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis/). | Pass |
| Redis Stack Server | Manifest `7.4.0-v8` | Official project declares Redis Stack maintenance ended and points users to Redis 8. | Lifecycle finding M1 |
| FalkorDB / NFalkorDB | Manifest `4.12.0`; catalog `1.2.0` | Server is behind official `4.18.8`; client pin is repository-exact. | Finding M2 |
| OpenBao / chart | Image `2.6.0`; chart `0.28.5` | Security-patched core `2.6.2` and chart `0.28.6` exist. | Critical C1 |
| MCP SDK | Catalog `2.2.0` | [Official C# SDK releases](https://github.com/modelcontextprotocol/csharp-sdk/releases) mark `2.2.0` latest. | Pass |
| Kreuzberg | Catalog `4.10.2` | Repository-exact; no stronger currentness claim required by the spine. | Pass |
| FrontComposer / Fluent UI | Internal `4.4.0`; catalog Fluent UI `5.0.0-rc.5-26219.1` | [Fluent UI NuGet profile](https://www.nuget.org/profiles/fluentui-blazor) confirms RC5 as latest v5 prerelease. | Pass; prerelease is visible |
| OpenTelemetry | Catalog `1.18.0` | [NuGet OpenTelemetry](https://www.nuget.org/packages/OpenTelemetry/) confirms `1.18.0`; Redis/gRPC instrumentation are separately pinned beta builds in catalog lines 271/275. | Pass, but consider showing beta instrumentation explicitly |

Operationally relevant pins omitted from the spine are Dapr CLI `1.18.0`, Dapr runtime `1.18.2`, kubectl `1.35.0`, kind `0.31.0`, and kind node `1.35.0` (`.github/workflows/ci.yml:14-20`). They need not all be architecture decisions, but Dapr runtime should be distinguished from the .NET SDK because workflows/actors depend on both.

## What the spine gets right

- It removes the legacy C# 13 / Aspire 13.1.3 / Dapr 1.17.6 / old Redis-client assumptions and uses exact repository pins.
- It no longer presents Google-only embeddings as current; Google and Ollama strategy implementations are reflected by AD-12.
- It correctly keeps the Python/Dapr Agents sidecar deferred rather than claiming it exists.
- It candidly records ordinary-ingestion EventStore bypass, false-positive `Indexed`, caller-controlled provenance, missing tenant principals, missing Kubernetes EventStore workload, CLI stubs, and non-hosted Web.
- It accurately describes local AppHost versus Kubernetes/OpenBao topology, including the production EventStore dependency gap.
- The mechanics and most load-bearing boundaries are concise, enforceable, and supported by architecture/contract/integration guards.

## Gate recommendation

Resolve C1 and H1-H4 before handoff. Add or amend the gap ledger for H5-H8 and M3, then qualify the Redis/Falkor lifecycle posture. Re-run the deterministic lint and this repository-reality lens after those edits. No spine or memlog files were modified by this review.

## Second Pass — 2026-09-09

### Verdict: PASS

The revised spine resolves every prior critical/high documentation-reality finding. Where the adopted architecture is not implemented, it now labels the rule as target architecture and records the concrete implementation obligation in **Current Alignment Gaps**. This is a documentation/reality pass, not a claim that the open implementation work or product gates are complete (`ARCHITECTURE-SPINE.md:52,278-299`).

### Prior critical/high findings

| Prior finding | Second-pass result | Revised evidence |
| --- | --- | --- |
| C1 OpenBao security qualification | **Resolved** | The Stack retains the repository-exact `2.6.0` image and `0.28.5` chart (`ARCHITECTURE-SPINE.md:198`) while explicitly blocking Production qualification until at least `2.6.2` is requalified and chart `0.28.6` is assessed or a time-bounded exception is approved (`:204`). The obligation is also in the gap register (`:295`). This matches the official [OpenBao changelog](https://github.com/openbao/openbao/blob/main/CHANGELOG.md) and [Helm releases](https://github.com/openbao/openbao-helm/releases). |
| H1 V1 statuses | **Resolved** | The convention now preserves `queued`, `extracting`, `embedding`, `indexing`, `indexed`, and `failed`, and explicitly treats `pending`/`projecting` as product-language mappings (`ARCHITECTURE-SPINE.md:177`). This matches `src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs:7-15` and its serialization guard at `tests/Hexalith.Memories.Contracts.Tests/V1/EnumSerializationTests.cs:25`. |
| H2 EventStore project purity | **Resolved** | The spine now states that the physical EventStore assembly is mixed and only its `Domain/` namespace is dependency-pure (`ARCHITECTURE-SPINE.md:39,60,211`). That matches the Dapr/Redis dependencies and registrations in `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj:4-39` and `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs:13-94`. |
| H3 Redis/provider boundary | **Resolved as target + gap** | AD-8 now permits the existing categories of direct Redis atomic operations only behind ports, names the target adapter boundary, and recognizes the Redis project as a compatibility facade (`ARCHITECTURE-SPINE.md:98-102`). The current spread of provider SDK use is candidly recorded for convergence (`:290`). |
| H4 automatic graph seeding | **Resolved as target + gap** | The top-five syntactic/semantic union and depth-two rule is explicit (`ARCHITECTURE-SPINE.md:104-108`), while the current no-start-node skip and missing G1 implementation are disclosed (`:291`), consistent with `src/Hexalith.Memories.Server/Search/HybridSearchService.cs:168-186`. |
| H5 EventStore replay/rebuild | **Resolved as target + gap** | EventStore-authoritative replay is now described as target design (`ARCHITECTURE-SPINE.md:62-72,255`) and the current Redis-input repair/no-all-projections replay limitation is explicit (`:286`). |
| H6 secrets path | **Resolved as target + gap** | AD-15 explicitly calls direct Kubernetes-secret injection of Redis/Falkor credentials temporary alignment debt (`ARCHITECTURE-SPINE.md:140-144`); the gap register names the current shared credentials and required Dapr/OpenBao/per-tenant convergence (`:288`). This matches `deploy/kubernetes/base/server-deployment.yaml:46-69`. |
| H7 release evidence lanes | **Resolved as target + gap** | AD-19 requires restore/build/contract/integration evidence in both lanes (`ARCHITECTURE-SPINE.md:164-168`), while the current source lane's missing contract/integration evidence is disclosed (`:293`). |
| H8 floating local images | **Resolved as target + gap** | The stack qualification text and gap register explicitly identify floating AppHost/Aspire Redis and Falkor defaults (`ARCHITECTURE-SPINE.md:204,294`), matching `src/Hexalith.Memories.AppHost/Program.cs:136-140,277-280` and `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:100-115`. Production's digest requirement remains unambiguous. |

### Remaining medium findings and expanded decisions

- Redis Stack's ended-maintenance status and the Redis 8 migration/qualification obligation are now explicit (`ARCHITECTURE-SPINE.md:204,310`), consistent with the official [Redis Stack repository](https://github.com/redis-stack/redis-stack).
- FalkorDB `4.12.0` is accurately characterized as qualified-but-behind with a scheduled review/upgrade trigger (`ARCHITECTURE-SPINE.md:197,204,310`), rather than as upstream-current.
- AD-11 separates Phase 1 metadata-supplied causal edges from Phase 1.5 automatic EventStore causal population and retains the graph kill-switch target; the missing kill switch is recorded (`ARCHITECTURE-SPINE.md:116-120,291`).
- Packaging now defers exclusively to `tools/release-packages.json`, correctly allowing intentionally packaged executable/host projects (`ARCHITECTURE-SPINE.md:184,228`).
- The added erasure rule is explicitly a target and its missing EventStore crypto-shredding/non-reuse/restore behavior is recorded (`ARCHITECTURE-SPINE.md:146-150,289`). Capacity/fairness remains an adopted cross-cutting rule with numeric gates owned by the PRD (`:158-162,258`), not a claim that G1-G5 have passed (`:299`).
- MCP deployment assets are distinguished from Phase 1.5 activation, with public ingress/publication/announcement gated until L1-L3 pass (`ARCHITECTURE-SPINE.md:122-126,249,297`). The Web RCL-versus-runnable-specimen difference is likewise explicit (`:220,296`).
- Local AppHost and Kubernetes topology now make the Production EventStore dependency candid: Kubernetes has no owned EventStore gateway and must provide an external compatible gateway or add one before qualification (`ARCHITECTURE-SPINE.md:249,292`). OpenBao is correctly qualified as three voters on one node, not node/site HA (`:261,312`).

### Stack and decision gate

All Stack values remain exact repository pins, and the revision no longer equates exactness with safety/currentness (`ARCHITECTURE-SPINE.md:186-204`). AD-1 through AD-19 are consistently presented as adopted target rules (`:52-168`); implemented behavior is not overstated where the repository differs, and the material divergences are listed at `:278-299`. No obsolete legacy assumption identified in the first pass remains presented as current reality.

**Unresolved blockers:** None for architecture handoff. The Production/security/release/product-launch blockers remain implementation obligations exactly as declared in the spine; this PASS does not waive them.
