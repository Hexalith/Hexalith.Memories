# Review — Brownfield Code Reality lens

**Verdict: PASS-WITH-FINDINGS**

**Target:** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` (status `final`, created/updated 2026-09-09)
**Lens:** brownfield code reality — does the spine ratify what the code shows, and is its "Current Alignment Gaps" table true today?
**Intent:** Validate (read-only). No repository file other than this review was modified. No build, test, or restore was run.
**Date:** 2026-09-12

## Method and a controlling fact

Evidence is `grep`/`find`/`read` over the working tree plus read-only `git`. Nothing was built.

One fact frames every judgement below:

```
git diff --name-only 3644ef63..HEAD -- src/ tests/ deploy/ tools/   →  (empty)
```

`3644ef63` is the commit that added the spine. **Not one file under `src/`, `tests/`, `deploy/`, or `tools/` has changed since.** The only post-spine commits (`e18f51a9`, `c4e01fe8`, `42dfa26b`) touch `_bmad-output/` and submodule gitlinks. Therefore no row in the gap table can be "stale by drift" — a row that does not match the code was **wrong on arrival**, and a row that matches still matches. This makes the three MISSTATED rows below authoring defects rather than decay, which raises their severity: they were falsifiable against the same tree the spine was written from.

---

## Part A — Current Alignment Gaps (spine lines 292–308)

| # | Gap (short) | Spine's claim | Code evidence | Status |
| --- | --- | --- | --- | --- |
| 1 | Ingestion scheduled before EventStore acceptance | AD-2/AD-4 violated; file, URL, MCP ingestion schedule work first | `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:56-63` (handler injects `IIngestionWorkflowScheduler`, `TenantStatusGuard`, `IngestDedupReservation` — no command store), ordering validate `:92` → tenant guard `:100` → Redis preflight `:116-134` → `.ScheduleAsync(...)` `:139-141` → `Results.Accepted` `:142`; URL `:319` `ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), ...)`; directory `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs:272-273`; MCP `src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs:124-131` → `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:454-460` (same REST route). No ingestion command exists in `src/Hexalith.Memories.EventStore/Domain/Commands/` (only CreateCase, DeleteCase, DeleteMemoryUnit, RegisterTenant, RequestAnnotation, UpdateTenantLifecycleStatus). Contrast the accept-first paths: `src/Hexalith.Memories.Server/Cases/CaseService.cs:167` `AcceptAsync(new RequestAnnotationCommand(...))` **before** `:182` `ScheduleAsync(nameof(AnnotationProjectionWorkflow), ...)`; same shape at `:89/:95`, `:669/:675`, `:717/:723` | **STILL-OPEN** |
| 2 | Missing axes still produce `Indexed`; no AD-3 checkpoint | AD-3 violated | `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:436-441` calls `VerifyConsistencyActivity`; `:442-465` turns a missing axis into a **string note plus a warning log** (`consistencyNote = $"Missing backends: {...}"`), then execution falls through to `:608` `currentStatus = TransitionStatus(..., MemoryUnitStatus.Indexed);` — `Indexed` is returned with `syntactic\|semantic\|graph` known-missing. Two further unconditional `Indexed` paths at `:96` and `:913`. Read path is worse: `SyntacticHashProjection.cs:66-92` never writes a `status` field, and `src/Hexalith.Memories.Server/Cases/CaseService.cs:995-997` defaults an absent/unparseable status to `MemoryUnitStatus.Indexed`. Repo-wide grep for `schemaGeneration`, `embeddingConfigurationEpoch`, `ProjectionCoordinator` returns zero hits in `src/`; the only version fence is `DerivedStores/RedisDerivedStoreService.cs:142-147,243,304` keyed `(tenantId, associationId, intakeId)` | **STILL-OPEN** (understated — see COD-12) |
| 3 | Repair reads Redis syntactic artifacts; no EventStore replay | AD-2/AD-3 violated | `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs:82-90` (key-prefix + graph scan, not an event stream); `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs:112-119` (`HashGetAllAsync(syntacticKey)`); `Consistency/GraphNodeMerger.cs:69-74` re-derives from `BuildSyntacticKey(...)`; `Consistency/SemanticIndexer.cs:52-53` calls the syntactic hash "authoritative" and `:86-88` `throw new NotSupportedException("Semantic re-index ... deferred to Story 8.2 Phase C")`; `Activities/Restore/RestoreReindexUnitActivity.cs:22-33` defers to an "event replay" that exists only in docs | **STILL-OPEN** |
| 4 | Caller-controlled `IngestedBy` | AD-5/AD-13 violated | `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs:27`, `UrlIngestionRequest.cs:17`, `DirectoryIngestionRequest.cs:17` all `public required string IngestedBy { get; init; }` on the request body. Assignments are caller-sourced: `IngestionEndpoints.cs:63` binds and `:140` forwards unchanged; `:311` `IngestedBy = request.IngestedBy`; `DirectoryIngestionService.cs:256`; `IngestContentTool.cs:121,130`. Validation is emptiness-only (`IngestionEndpoints.cs:584-586`, `:645-647`; `Activities/Ingestion/IngestionInputValidator.cs:26`). The authenticated subject **is** resolved but only for audit (`Telemetry/AuditPrincipalResolver.cs:26-28`, consumed at `Endpoints/EndpointTelemetryHelpers.cs:38`) and never assigned to `IngestedBy`. The caller string persists (`SyntacticHashProjection.cs:88`). Server-set attribution exists only on the event path (`src/Hexalith.Memories.EventStore/CloudEventToIngestionInputMapper.cs:35,82`) | **STILL-OPEN** |
| 5 | Shared Redis/Falkor credentials via K8s Secrets; no tenant backend principals | AD-6/AD-15 violated; converge by provisioning per-tenant Redis ACL identities **and separate Falkor graphs** | Credentials half confirmed: `deploy/kubernetes/base/server-deployment.yaml:46-59` injects `REDIS_PASSWORD` and `FALKORDB_PASSWORD` from the same `redis-secret`, composing one process-wide connection string per backend; servers are password-only with no ACL users (`redis-statefulset.yaml:42-48`, `falkordb-statefulset.yaml:47-53`, `config/redis-stack.conf:1-8` has no `user`/`aclfile`); no Redis `ACL`/`SETUSER` anywhere in `src/`. **But the "separate Falkor graphs" convergence is already satisfied**: `Activities/Tenants/ProvisionFalkorDbActivity.cs:41-46` `string graphId = input.TenantId; falkor.SelectGraph(graphId)`, and every call site follows (`Activities/Indexing/IndexGraphActivity.cs:50`, `Search/GraphScopedSearch.cs:92`, `Consistency/GraphNodeMerger.cs:107`) | **STILL-OPEN**, convergence column **MISSTATED** (COD-09) |
| 6 | Tenant deletion does not crypto-shred, enforce non-reuse, or quarantine on restore | AD-16 violated | Case-insensitive grep for `shred` across `src/`, `tests/`, `deploy/` returns **zero hits**; the only `Cryptography` uses are SHA-256 dedup hashing (`src/Hexalith.Memories.EventStore/EventStoreDedupKey.cs:8`, `Activities/Ingestion/DedupKeyBuilder.cs:8`) — no tenant data key exists to destroy. `Workflows/TenantDeletionWorkflow.cs:19-21` documents the whole flow as projection purge; `Activities/Tenants/DeleteTenantDataKeysActivity.cs:62-64` explicitly leaves EventStore keys behind. Reuse: `Activities/Tenants/RemoveTenantRegistryActivity.cs:28` hard-deletes the row and `Tenants/TenantRegistryService.cs:108-146` re-registers via the plain not-found path; no `Tombstone` type exists. Restore aborts wholesale instead of quarantining (`Import/RestoreTargetGuard.cs:63-65` `RESTORE_TARGET_NOT_CLEAN`); `Workflows/RestoreWorkflow.cs:72` only counts `SkippedRecords` | **STILL-OPEN** |
| 7 | Provider SDK spread through Server; five direct-Redis coordination uses outside the registry | AD-8 violated | **105 files** under `src/Hexalith.Memories.Server/` reference `NRedisStack`/`StackExchange.Redis`/`NFalkorDB`/`IConnectionMultiplexer` — Activities 42, Endpoints 11, Search 7, Workflows 5, Ingestion 4, Infrastructure 4, Graph 4, Migration 3, Import 3, HealthChecks 3, EventStoreIntegration 3, Consistency 3, then Tenants/Hosting/Cases/Actors 2 each (121 files repo-wide under `src/`). All five named coordination uses confirmed direct-Redis, none on Dapr state: permanent dedup `Activities/Ingestion/SaveDedupKeyActivity.cs:43-48` (`expiry: null`); failed-unit registry `Ingestion/FailedUnitsRegistry.cs:20-27` (hash + sorted set + Lua claim); import lease `Import/RedisImportStagingStore.cs:279,307-314`; derived-store fence `DerivedStores/RedisDerivedStoreService.cs:299-304,447-459`; migration state `Migration/RedisEmbeddingMigrationStore.cs:210-274,286-297`. `IPreflightDedupStore` (`src/Hexalith.Memories.EventStore/IPreflightDedupStore.cs:11-32`) has **two** direct-Redis implementations (`src/Hexalith.Memories.EventStore/RedisPreflightDedupStore.cs:47-49`, `src/Hexalith.Memories.Server/EventStoreIntegration/RedisPreflightDedupStore.cs:19`). `Hexalith.Memories.Redis` is genuinely a facade (2 files: `RedisPlaceholder.cs`, `FalkorDbCompatibilityExtensions.cs`) | **STILL-OPEN** (understated) |
| 8 | Fusion lacks AD-9 blank/non-finite/duplicate preprocessing before competition ranks | AD-9 violated | `src/Hexalith.Memories.Server/Search/FusionEngine.cs` is the single implementation, called only from `Search/HybridSearchService.cs:212-219` with raw axis results. Contribution `1/(10+rank)` is correct (`:18`, `:246`) and **competition ranks 1,1,3 ARE implemented** (`:144-150`). Violations: blank IDs not dropped (`GetOrAddAccumulator` `:219-237` keys on `result.MemoryUnitId` with no `IsNullOrWhiteSpace` guard — that guard is used only for `CaseId`/`CaseName` at `:258-259`); non-finite scores not dropped and NaN is an **explicit tie** (`:253` `left.Equals(right) \|\| (double.IsNaN(left) && double.IsNaN(right))`; `double.IsFinite` appears nowhere in the file); duplicates keep first score but still consume rank slots (`currentRank = i + 1` over the raw index). Passing tests **lock the anti-AD-9 behavior**: `tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs:358-370` and `:373-385` assert a NaN/Infinity input is retained and ranked #1. No blank-ID or duplicate-ID test exists in 513 lines | **MISSTATED** (COD-03) |
| 9 | Graph skipped without a start node; no case-partition merge, no kill switch, N=8 cannot pass G1 | AD-9/AD-11 violated | Skip confirmed: `Search/HybridSearchService.cs:168-176` logs `graphStartNodeId is null` and omits the axis; start node comes only from query params (`Endpoints/SearchEndpoints.cs:414-416`). No auto-seeding — grep for `autoseed\|auto-seed\|seedFrom\|top5` in `src/Hexalith.Memories.Server/` returns zero. No case-partition fan-out: `Search/GraphScopedSearch.cs:82-85` uses one graph per tenant with an optional `CaseId` filter. No kill switch — grep for `GraphEnabled\|EnableGraph\|KillSwitch\|DisableGraph` returns zero; `Graph/GraphQueryExecutionOptions.cs:16` holds only a row limit. G1 harness: `tests/Hexalith.Memories.Benchmarks/Data/ground-truth.json` is **8 queries** over a 35-unit synthetic corpus, and the control is best-single-axis, not a BM25+semantic fused arm (`BenchmarkSuiteTests.cs:178-186` `bestSingleNdcg = Math.Max(syntacticNdcg, semanticNdcg)`), asserted as a win-rate at `:88-98` — 8 queries move the metric in 12.5% steps | **STILL-OPEN** |
| 10 | Kubernetes has no EventStore gateway workload | AD-2 violated | Complete workload set in `deploy/kubernetes/base/kustomization.yaml:3-22`: `server-deployment.yaml:2`, `mcp-deployment.yaml:2`, `access-telemetry-deployments.yaml:2,141`, `redis-statefulset.yaml:16`, `falkordb-statefulset.yaml:16`, `access-telemetry-postgresql.yaml:105`. Services are `memories`, `memories-mcp`, and the two access-telemetry ones (`services.yaml:4,17,30,43`). No EventStore Deployment or Service. Supporting scaffolding for a pod that does not exist: `service-accounts-rbac.yaml:16-18` ServiceAccount `eventstore` + `:92-113` `eventstore-dapr-secret-reader`, and Dapr scopes at `dapr/pubsub.yaml:26`, `dapr/secretstore.yaml:32`; `serviceAccountName: eventstore` is bound by nothing in base or either overlay. Local AppHost **does** compose it (`src/Hexalith.Memories.AppHost/Program.cs:290` `.AddHexalithEventStoreGatewayProject()`) | **STILL-OPEN** |
| 11 | Source and package modes lack independent contract/integration lanes | AD-19 violated | Mode switch is `UseHexalithProjectReferences` (`Directory.Build.props:41-46`, default false). Only the `build` job exercises both: `.github/workflows/ci.yml:189-196` (Release/package restore+build) and `:199-208` (`-p:Configuration=Debug -p:UseHexalithProjectReferences=true` restore+build) — compile only, no tests. Every test lane is package mode: `test-unit-contract` builds Release at `ci.yml:255-256` and runs at `:320-321 --configuration Release --no-build`; `integration-fast` at `:441-445`; `nightly.yml:53,99,144` all Release. `UseHexalithProjectReferences` appears in workflows only at `ci.yml:201,205,208` | **STILL-OPEN** |
| 12 | AppHost and Aspire defaults use floating Redis/Falkor images | AD-19 violated | `src/Hexalith.Memories.AppHost/Program.cs:137` `.AddContainer("memories-vectors", "redis/redis-stack")` and `:278` `.AddContainer("memories-graphs", "falkordb/falkordb")` — no tag, no digest; same two at `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:102,114`. No `WithImageTag`/`WithImageSHA256` in either file. OpenBao in the same AppHost **is** digest-pinned (`Program.cs:50` via `OpenBaoDevelopmentProfile.cs:15,18`), so the omission is specific. Kubernetes is fully pinned by tag **and** digest (`deploy/kubernetes/base/redis-statefulset.yaml:37`, `falkordb-statefulset.yaml:37`, guarded by `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs:111,117`) — the row correctly scopes itself to AppHost/Aspire | **STILL-OPEN** |
| 13 | OpenBao `2.6.0` / chart `0.28.5` precede security patches | AD-15/AD-19 violated | Pins unchanged and mutually consistent: image `quay.io/openbao/openbao:2.6.0@sha256:900bb64d…` at `.github/workflows/ci.yml:20`, `deploy/openbao/smoke-test.yaml:32`, `src/Hexalith.Memories.AppHost/OpenBaoDevelopmentProfile.cs:15,18`; chart `0.28.5` at `deploy/openbao/values.yaml:2` (digest restated and self-flagged "not measured" at `docs/operations/openbao.md:29`). Pins are test-guarded at `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:842,868`. The repo already records 2.6.2 as current stable in a vendored review under `references/Hexalith.EventStore/` | **STILL-OPEN** (the "precede available security patches" clause is UNVERIFIABLE offline; settling it needs the OpenBao advisory feed for 2.6.0→2.6.2 and chart 0.28.6) |
| 14 | Web is a non-runnable RCL while the PRD requires a runnable conformance specimen | AD-12 violated | First clause is **false**. A runnable conformance host exists: `tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj:1` is `Microsoft.NET.Sdk.Web` and references the specimen library and the Web RCL (`:9-10`); `tests/Hexalith.Memories.Web.SpecimenHost/Program.cs:10-27` is a real `WebApplication` with `AddRazorComponents().AddInteractiveServerComponents()`, `MapRazorComponents<App>()`, `app.Run()`, serving `/__memories/specimens/{SurfaceSlug}`. Playwright drives it: `tests/Hexalith.Memories.Web.E2E/playwright.config.ts:39-49` launches the host on `:5177`, four spec files, CI job `web-e2e-specimen` at `.github/workflows/ci.yml:335,368-369,383-384,387-388`. `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj:1` is indeed `Microsoft.NET.Sdk.Razor` with no `OutputType`, and its comment at `:4-6` ("has no runnable host yet") is stale. Second clause holds: only `tests/` projects consume the RCL | **MISSTATED** (COD-02) |
| 15 | Kubernetes assets include MCP before Phase 1.5 gates | AD-12 violated; keep public ingress/publication disabled | MCP is shipped ungated: `deploy/kubernetes/base/kustomization.yaml:7,14` and `mcp-deployment.yaml:8` `replicas: 2`, with RBAC at `service-accounts-rbac.yaml:11,71-89`; neither overlay disables it (production only remaps the image tag, `overlays/production/kustomization.yaml:19-21`). **But there is no public ingress to disable**: the Service is `ClusterIP` (`services.yaml:14-25`), a repo-wide grep for `kind: Ingress`/`Gateway` under `deploy/` returns nothing, and Dapr access control is `defaultAction: deny` with one narrow allow (`dapr/config.yaml:23-35`) | **MISSTATED** (COD-08) — presence claim true, exposure framing overstated |

### Part A counts

| Status | Count | Rows |
| --- | --- | --- |
| STILL-OPEN | **12** | 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13 |
| CLOSED | **0** | — |
| MISSTATED | **3** | 8, 14, 15 |
| UNVERIFIABLE | **0** | (row 13 carries an unverifiable sub-clause; row 5 carries a misstated convergence column) |

The table is directionally sound: 12 of 15 obligations are real and none has been silently closed. Its defects are accuracy, not direction.

---

## Part B — Structural Seed and Consistency Conventions

### B.1 Structural Seed (spine lines 218–239) vs. `Hexalith.Memories.slnx`

Every project named in the seed exists with that name and role. Verified against `Hexalith.Memories.slnx` and `ls src/ tests/ deploy/`.

| Seed entry | Real? | Note |
| --- | --- | --- |
| `Hexalith.Memories.Contracts/` | yes | Namespaces are exactly `Hexalith.Memories.Contracts`, `.V1`, `.V1.DerivedStores` |
| `Hexalith.Memories.EventStore/` "mixed; pure `Domain/` subtree" | yes — **verified accurate** | Assembly references `StackExchange.Redis`, `Dapr.*`; but every non-`System` `using` under `Domain/` resolves to `Hexalith.Memories.Contracts.V1` or its own `Domain.*` namespaces. The "only its `Domain/` namespace is the dependency-pure domain center" claim (line 39) holds |
| `Hexalith.Memories.Server/` "plus **target** internal Adapters.Redis/FalkorDb" | project yes; adapters **no** | The word "target" is the only place the spine hedges this. See COD-01 |
| `Hexalith.Memories.Client*/` "Consumer ports and transport adapters" | partly | Only `Client.Rest` exists; it contains no port interfaces — `MemoriesClient` is a concrete class and the sole `public interface` in Contracts is `IWorkflowTraceContextCarrier`. "ports" overstates (COD-11) |
| `Hexalith.Memories.Redis/` "compatibility facade only" | yes — **verified accurate** | Exactly two files; `RedisPlaceholder.cs:20,23` are two port constants self-documented as unreferenced; `FalkorDbCompatibilityExtensions.cs` is one global-namespace shim |
| `Hexalith.Memories.Telemetry/` | yes | 5 files: meters, activity sources, collectors |
| `Hexalith.Memories.AccessTelemetry*/` | yes | `.AccessTelemetry`, `.Contracts`, `.Clock` all present |
| `Hexalith.Memories.Cli/` "incomplete commands fail explicitly" | yes | `Commands/NotImplementedCommand.cs`, wired at `Commands/RootCommandFactory.cs:162` |
| `Hexalith.Memories.Mcp/` | yes | |
| `Hexalith.Memories.Web/` "current RCL" | yes, but see row 14 | |
| `Hexalith.Memories.Aspire/` | yes | 8 files of hosting extensions |
| `Hexalith.Memories.AppHost/` | yes | |
| `Hexalith.Memories.ServiceDefaults/` | yes | |
| `deploy/kubernetes/`, `deploy/openbao/` | yes | Seed omits `deploy/dapr/`, `deploy/grafana/`, `deploy/redis/` (minor) |
| `tools/release-packages.json` | yes | |

**Missing from the tree:** `tools/MigrateEmbeddingVectors/` — a real solution project (`Hexalith.Memories.slnx`, folder `/tools/`) that `ProjectReference`s `Hexalith.Memories.Server` and takes a direct `StackExchange.Redis` dependency, i.e. an AD-8 surface the seed does not acknowledge. `tools/GenerateBenchmarkVectors/` also exists on disk. See COD-06.

**Missing from the deployment narrative and Stack:** `deploy/kubernetes/base/access-telemetry-postgresql.yaml:105` deploys a PostgreSQL StatefulSet (`docker.io/library/postgres:18.4-trixie@sha256:3a82e1f5…`, line 144) as the access-telemetry store. The spine mentions PostgreSQL **nowhere** — not in the Stack table, not in the seed's `kubernetes/` gloss, not in the composition sentence at line 259. See COD-04.

### B.2 Claimed namespace and route conventions

| Claim (spine line) | Real? | Evidence |
| --- | --- | --- |
| Public APIs live in `Contracts/V1` (174) | **yes** | `src/Hexalith.Memories.Contracts/V1/` is the only public surface; namespaces `Hexalith.Memories.Contracts.V1[.DerivedStores]` |
| Provider adapters use the AD-8 `Adapters.Redis` / `Adapters.FalkorDb` namespaces (102, 174, 222) | **NO — does not exist** | `grep -rn "namespace .*Adapters" src/` → **zero hits**; `find src -type d -name "Adapters*"` → **zero hits**. The Server's 30+ namespaces are feature-shaped (`.Activities.*`, `.Search`, `.Graph`, `.DerivedStores`, `.Consistency`, …), not adapter-shaped |
| Projects use `Hexalith.Memories.*` (174) | **yes** | All 15 `src/` projects |
| Route constants live in `MemoriesRoutes` (176) | **yes** | `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs:27`, consumed by both Server endpoints and the REST client |
| Product HTTP routes under `/api/v1` (176) | **yes for MemoriesRoutes**, incomplete as stated | `MemoriesRoutes.cs:30` `ApiPrefix = "/api/v1"`, and `tests/…/MemoriesRoutesTests.cs:41-57` reflects over every public literal and asserts it starts with `/api`. But `src/Hexalith.Memories.AccessTelemetry/Program.cs:87,98,109,125,137` and `src/Hexalith.Memories.AccessTelemetry.Clock/Program.cs:80` map `/v1/access-telemetry/*` and `/v1/time/attest` — outside `/api/v1`, outside `MemoriesRoutes`. Health paths `/health`, `/alive`, `/ready` likewise (`ServiceDefaults/Health/HealthEndpointPaths.cs:16,19,22`). See COD-07 |
| `/events/ingest` is the named infrastructure exception (176) | **yes** | `src/Hexalith.Memories.EventStore/EventIngestionController.cs:33` `[Route("events")]` + `:57` `[HttpPost("ingest")]`; middleware match at `CloudEventEnvelopeCaptureMiddleware.cs:75`; pinned by `tests/…/Deployment/RouteSurfaceContractTests.cs:143-155` |

### B.3 V1 status wire contract (spine line 177)

**Accurate — verified.** `src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs:7-15` declares exactly `Queued, Extracting, Embedding, Indexing, Indexed, Failed` under `[JsonConverter(typeof(CamelCaseStringEnumConverter<MemoryUnitStatus>))]` (`:6`), producing the six lowercase wire values. `tests/Hexalith.Memories.Contracts.Tests/V1/EnumSerializationTests.cs:25-28` pins `Queued → "queued"` and `Failed → "failed"`, and `:30-34` rejects integer tokens; `MemoryUnitSerializationTests.cs:110,137` pins `"status": "indexed"` in golden JSON.

The `pending`→`queued` / `projecting`→`indexing` mapping is a **product-label** statement, and the spine states it correctly ("displays may use product terms"). `projecting` appears nowhere in `src/` — only in `_bmad-output/planning-artifacts/prd.md:98,829,838,843,1013`, which states the same mapping in the same direction. No contradiction.

### B.4 Architecture-test enforcement: asserted vs. actually existing

The spine says "enforced by architecture tests" (AD-8, line 102) and "architecture guards enforce dependency and literal ownership" (line 183). There is **no** `NetArchTest` or `ArchUnitNET` dependency anywhere (`grep -rIl "NetArchTest\|ArchUnitNET" src/ tests/` → zero). Every guard is a hand-rolled source-text or reflection test. Inventory:

| Spine invariant | Would be enforced by | Exists today? |
| --- | --- | --- |
| AD-8 — provider SDK confined to composition roots and `Adapters.Redis`/`Adapters.FalkorDb` | *(nothing)* | **NO** — no test, and the namespaces do not exist |
| AD-8 — Direct Redis Exception Registry admits only `IPreflightDedupStore` | *(nothing)* | **NO** — five coordination uses sit outside it unguarded |
| AD-1 — dependency-pure domain namespaces (EventStore `Domain/`) | *(nothing)* | **NO** — true in fact, unguarded; nothing stops the next `using StackExchange.Redis;` under `Domain/` |
| AD-1 — consumers never reference AppHost or Server | *(nothing)* | **NO** |
| AD-3 — all-axis acknowledgement gate before `Indexed` | *(nothing)* | **NO** |
| AD-9 — fusion canonicalization (blank/non-finite/duplicate) | `Search/FusionEngineTests.cs` | **NEGATIVE** — tests exist and assert the **opposite** (`:358-370`, `:373-385` keep NaN/Infinity and rank them #1) |
| AD-12 — Contracts carry no persistence/impl leakage and no Hexalith deps | `Contracts.Tests/V1/ContractPersistenceSeparationTests.cs:104-121` | **YES** — reflection over public types plus `GetReferencedAssemblies()` asserting no `Hexalith.*` reference |
| Routes — every constant under `/api`, constants own the surface | `Contracts.Tests/V1/MemoriesRoutesTests.cs:41-57`; `Server.Tests/Deployment/RouteSurfaceContractTests.cs:52,79,122` | **YES** |
| V1 status wire values | `Contracts.Tests/V1/EnumSerializationTests.cs:25-34` | **YES** |
| AD-4 — workflow determinism and the reviewed start inventory | `Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs:17-292` | **YES** — strongest guard in the repo; pins four exact start sites by file:line and forbids ambient trace capture |
| Literal ownership of index/key schema | `Server.Tests/Architecture/IndexSchemaLiteralGuardTests.cs:29-61` | **YES** — scans all of `src/` for six forbidden key fragments |
| Indexing hot path (no on-demand `FT.CREATE`, no `Thread.Sleep`) | `Server.Tests/Architecture/IndexingHotPathGuardTests.cs:29-70` | **YES** |
| AD-11 — graph labels restricted to the contract enum | `Contracts.Tests/V1/EdgeTypeTaxonomyTests.cs:33-73` over `V1/EdgeType.cs:9-13` | **YES** |
| Endpoint telemetry centralization | `Server.Tests/Architecture/EndpointCentralizationGuardTests.cs:17-69` | **YES** |
| AD-19 — container digest pinning | `Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs:111,117` | **YES for Kubernetes only** — AppHost/Aspire are unguarded, which is exactly why gap 12 survives |
| AD-19 — release inventory is the sole publication decider | `tools/validate-release-packages.ps1` | **YES for `src/` only** — `:259` enumerates `src/**/*.csproj` and nothing else, so `tools/MigrateEmbeddingVectors` is outside the gate |
| Testing convention — "tests state tier and boundary" | `[Trait(...)]` | **PARTIAL** — 118 classes carry a `[Trait("Category", …)]`; most do not |

Six of the seventeen invariants the spine leans on are unenforced, one is enforced backwards, and two are enforced only over a subset. **AD-8 is the worst case: its Rule is the only one that names architecture tests explicitly, and it has no test and no namespaces.**

### B.5 Other conventions spot-checked (all accurate)

- **Errors** (178): `src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs:4` `sealed record ErrorResponse(string Code, string Message, string Suggestion)`.
- **Configuration** (179): `src/Hexalith.Memories.Cli/CliServices.cs:56` — "registration ORDER is the precedence (flag > env > file > default)"; `Configuration/IConfigurationSource.cs:18`.
- **Logging** (182): 62 files use source-generated `LoggerMessage`; **zero** interpolated `Log*($"…")` calls in `src/`.
- **Health** (181): `ServiceDefaults/Health/HealthEndpointPaths.cs:16-25`; readiness-tagged checks registered at `Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:171-194` and `ServiceDefaults/Extensions.cs:633,667`.
- **Packaging** (184): `.slnx` confirmed; `tools/release-packages.json` lists 9 packages and 6 non-packable, including `Hexalith.Memories.Redis` as packable — consistent with the facade role.
- **Stack table** (198–212): **accurate against the committed pins.** Compared against the *committed* `references/Hexalith.Builds` gitlink `a32cb422` (the working-tree submodule is dirty at `fa647278` and would falsely suggest drift): `HexalithEventStoreVersion 3.103.0`, `HexalithFrontComposerVersion 4.4.0`, `CommunityToolkit.Aspire.Hosting.Dapr 13.5.0-preview.1.260825-0345`, `Dapr.Client 1.18.5`, `Kreuzberg 4.10.2`, `StackExchange.Redis 3.1.31`, `NFalkorDB 1.2.0`, `NRedisStack 1.7.4`, `ModelContextProtocol 2.2.0`, `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.5-26219.1` — every one matches. `global.json` = `10.0.400`; `Directory.Build.props:3-4` = `net10.0` / C# 14. **One exception:** the EventStore source-lane gitlink (COD-05).

---

## Findings

### COD-01 — AD-8's enforcement mechanism does not exist, and two of its three statements are present-tense
**Severity: critical**

AD-8 (line 102) reads: "provider SDK references are confined to composition roots and named internal `Adapters.Redis` or `Adapters.FalkorDb` namespaces **and enforced by architecture tests**." The Consistency Conventions row (line 174) restates it as settled practice: "provider adapters use the AD-8 namespaces."

Neither is true. `grep -rn "namespace .*Adapters" src/` and `find src -type d -name "Adapters*"` both return **zero**. No test in `tests/` references `NRedisStack`, `StackExchange.Redis`, or `NFalkorDB` as a forbidden symbol; there is no `NetArchTest`/`ArchUnitNET` dependency in the repo. Meanwhile 105 files under `src/Hexalith.Memories.Server/` and 121 under `src/` hold those SDKs, spread across Endpoints (11), Activities (42), Search (7), Workflows (5) and more.

This is the single largest gap between the spine's implied state and reality. Only the Structural Seed hedges ("**target** internal Adapters.Redis/FalkorDb", line 222); AD-8's Rule and the conventions table do not, and gap row 7 asks to "enforce both boundaries with architecture tests" as future work — three parts of one document disagreeing about whether the boundary is present, target, or pending. Per the lens brief: an AD whose Rule is enforceable only by a test that does not exist is a weaker invariant than the spine implies, and AD-8 is the clearest instance.

**Recommended spine edit:** (a) in AD-8's Rule, change "are confined to … and enforced by architecture tests" to "**must be** confined to … **enforced by an architecture test to be added** (see Current Alignment Gaps)"; (b) change the Consistency Conventions "Names and dependencies" row from "provider adapters use the AD-8 namespaces" to "provider adapters **will use** the AD-8 namespaces (`Adapters.Redis` / `Adapters.FalkorDb`); today no such namespace exists"; (c) in gap row 7, add the namespace creation itself to the Required convergence, and state the current spread as "105 files under `src/Hexalith.Memories.Server/`" so the size of the obligation is visible.

### COD-02 — Gap row 14 is factually wrong: a runnable conformance host exists and is CI-gated
**Severity: high**

The row states Web is "a non-runnable RCL while the PRD requires a runnable conformance specimen." The RCL half is true (`src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj:1` is `Microsoft.NET.Sdk.Razor`), but the obligation it derives is already met by `tests/Hexalith.Memories.Web.SpecimenHost/`: a `Microsoft.NET.Sdk.Web` project (`…csproj:1`) referencing the Web RCL (`:9-10`), whose `Program.cs:10-27` builds a real `WebApplication` with interactive server components and `app.Run()`. Playwright launches it at `tests/Hexalith.Memories.Web.E2E/playwright.config.ts:39-49` and CI runs it as job `web-e2e-specimen` (`.github/workflows/ci.yml:335,368-369,383-384`).

Since `src/`, `tests/`, and CI are byte-identical to the spine commit, this was verifiable on 2026-09-09. The spine is understating the project's true state, and an implementation team reading this row would rebuild something that exists.

**Recommended spine edit:** replace the row with one that names what actually remains — e.g. "The conformance specimen host lives under `tests/Hexalith.Memories.Web.SpecimenHost` rather than a shipped surface, and no product host consumes the `Hexalith.Memories.Web` RCL; the stale 'no runnable host yet' comment at `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj:4-6` should be corrected." Keep "product Web remains future" and the AD-12 binding.

### COD-03 — Gap row 8 understates fusion by one dimension and misses a harder obligation: tests lock the anti-AD-9 behavior
**Severity: high**

The row says fusion "does not perform AD-9's canonical blank/non-finite/duplicate preprocessing before competition-rank allocation." Three of four sub-requirements are indeed violated, but **competition ranking is implemented correctly** (`src/Hexalith.Memories.Server/Search/FusionEngine.cs:144-150` produces 1,1,3; `:246` gives `1/(10+rank)`), so the row as phrased is imprecise.

More importantly, it treats this as a pure omission. It is not. `FusionEngine.cs:253` deliberately encodes `double.IsNaN(left) && double.IsNaN(right)` as a **tie**, and `tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs:358-370` / `:373-385` are passing tests asserting that a NaN or Infinity input is retained and ranked #1. AD-9 requires the exact opposite ("drop … non-finite raw scores"; ties only when *finite* `Double.Equals`). Converging therefore requires **deleting or inverting green tests**, which is materially different work from adding a missing preprocessing pass and should be flagged as such.

**Recommended spine edit:** rewrite the row's claim to "Fusion allocates competition ranks correctly but skips AD-9's blank-ID and non-finite drops and lets duplicate IDs consume rank slots; `FusionEngine.ScoresTie` additionally treats `NaN == NaN` as a tie, a behavior currently locked by passing tests." Extend Required convergence with "retire or invert `FusionEngineTests.Fuse_Bm25NaN_*` / `Fuse_Bm25Infinity_*` as part of the change."

### COD-04 — PostgreSQL is an unlisted production dependency
**Severity: medium**

`deploy/kubernetes/base/access-telemetry-postgresql.yaml` deploys a ServiceAccount, ConfigMap, two Services, a PodDisruptionBudget, and a StatefulSet (`:105`) running `docker.io/library/postgres:18.4-trixie@sha256:3a82e1f5…` (`:144`), wired into `kustomization.yaml`. The spine never mentions PostgreSQL: it is absent from the Stack table (so AD-19's "pin qualified container defaults" and the Deferred row on backend upgrades silently exclude it), from the Structural Seed's `kubernetes/` gloss (line 235), and from the composition sentence at line 259.

The image *is* digest-pinned, so this is a documentation completeness defect rather than a risk in itself — but a Stack table that omits a stateful production datastore cannot support the qualification claims built on it.

**Recommended spine edit:** add a `PostgreSQL (access telemetry store)` row to `## Stack` with `18.4-trixie` + digest; add PostgreSQL to the Structural Seed's `deploy/kubernetes/` comment and to the deployment sentence at line 259.

### COD-05 — The EventStore source-lane gitlink in the Stack table matches no commit in this history
**Severity: medium**

Stack line 205 pins `Hexalith.EventStore source lane gitlink` = `b1c00a79d1d34aa7ba3f58046a7844e8b3d57fd6`. Gitlink history:

- `4e564e43` (2026-09-09) moved it *to* `b1c00a79…`
- `3644ef63` (2026-09-10) — **the commit that added the spine** — moved it `b1c00a79…` → `2dd7ebfb…`
- `e18f51a9` (2026-09-12) moved it `2dd7ebfb…` → `6b0247ac…` (HEAD today)

So the pin was already superseded by the very commit that published the spine, and is now two moves behind. (The working tree is further dirty at `a568af4e`, consistent with pre-existing `references/` gitlink drift; the committed value is authoritative.) Every other Stack pin verified clean against the committed `Hexalith.Builds` catalog `a32cb422`, so this is an isolated error — but AD-19 makes source-lane identity an evidence artifact, and a wrong hash defeats it.

**Recommended spine edit:** update line 205 to `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20`, and note that this pin must be re-derived (`git ls-tree HEAD references/Hexalith.EventStore`) whenever the spine is revised.

### COD-06 — Structural Seed omits `tools/` projects, and the release-inventory gate does not cover them
**Severity: medium**

`tools/MigrateEmbeddingVectors/MigrateEmbeddingVectors.csproj` is a solution project (`Hexalith.Memories.slnx`, folder `/tools/`) with `OutputType=Exe`, `IsPackable=false`, a `ProjectReference` to `Hexalith.Memories.Server`, and a direct `StackExchange.Redis` dependency — i.e. an executable that both violates the AD-8 direction and is invisible in the seed. `tools/GenerateBenchmarkVectors/` also exists. Neither appears in `tools/release-packages.json` (`packages` or `nonPackableProjects`), and they cannot: `tools/validate-release-packages.ps1:259` enumerates `src/**/*.csproj` only. AD-19's "`tools/release-packages.json` alone decides what is packaged" is therefore true only within `src/`.

**Recommended spine edit:** add a `tools/` line to the Structural Seed listing `MigrateEmbeddingVectors` and `GenerateBenchmarkVectors` as non-packable operator utilities; qualify the Packaging convention (line 184) with "over `src/`" or add a gap row to extend `validate-release-packages.ps1` to every solution project.

### COD-07 — The routes convention names one exception; the code has three families outside `/api/v1`
**Severity: medium**

Line 176 states: "Product HTTP routes are under `/api/v1`; the Dapr subscription adapter `/events/ingest` is the named infrastructure exception… Route constants live in `MemoriesRoutes`." In fact three surfaces sit outside both:

- `src/Hexalith.Memories.AccessTelemetry/Program.cs:87,98,109,125,137` → `/v1/access-telemetry/{write,heartbeat,validate,inspect,physical-reclamation-evidence}`
- `src/Hexalith.Memories.AccessTelemetry.Clock/Program.cs:80` → `/v1/time/attest`
- `src/Hexalith.Memories.ServiceDefaults/Health/HealthEndpointPaths.cs:16,19,22` → `/health`, `/alive`, `/ready`

None uses `MemoriesRoutes`. The access-telemetry ones are defensible under AD-17 (telemetry is not product truth) and the health ones under the Health convention, but the conventions row asserts a single exception and is used as a completeness statement.

**Recommended spine edit:** amend line 176 to enumerate the exception families — Dapr subscription (`/events/ingest`), ServiceDefaults health probes (`/health`, `/alive`, `/ready`, plus `/api/v1/health` for Dapr invocation), and the AD-17 access-telemetry/clock plane (`/v1/access-telemetry/*`, `/v1/time/attest`) — and state that `MemoriesRoutes` owns the *product* surface only.

### COD-08 — Gap row 15 overstates MCP exposure
**Severity: low**

The Required convergence says "Keep public ingress/publication/announcement disabled until L1-L3 pass." There is no ingress to keep disabled: `grep -rn "kind: Ingress\|kind: Gateway" deploy/` returns nothing, the MCP Service is `ClusterIP` (`deploy/kubernetes/base/services.yaml:14-25`), and Dapr access control is `defaultAction: deny` with one narrow allow (`deploy/kubernetes/base/dapr/config.yaml:23-35`). The true residue is that `mcp-deployment.yaml:8` runs `replicas: 2` unconditionally in base with no overlay or flag gating it.

**Recommended spine edit:** restate the claim as "Kubernetes base runs the MCP deployment at `replicas: 2` with no launch-gate flag, though no ingress or public route exists; convergence = gate the workload (or zero its replicas) until L1-L3 pass, and keep publication/announcement disabled."

### COD-09 — Gap row 5's convergence asks for something already built
**Severity: low**

The row's Required convergence is "Provision and resolve per-tenant Redis ACL identities/**separate Falkor graphs** and route their secrets through the adopted boundary." Separate Falkor graphs already exist: `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionFalkorDbActivity.cs:41-46` selects `graphId = input.TenantId`, and every read/write path follows (`Activities/Indexing/IndexGraphActivity.cs:50`, `Search/GraphScopedSearch.cs:92`, `Consistency/GraphNodeMerger.cs:107`). What is missing is the *credential/principal* dimension: one shared password per backend from one Secret (`deploy/kubernetes/base/server-deployment.yaml:46-59`) with `--requirepass` and no ACL users (`redis-statefulset.yaml:42-48`, `falkordb-statefulset.yaml:47-53`, `config/redis-stack.conf:1-8`). Isolation today is enforced entirely in application code over a shared authenticated connection.

**Recommended spine edit:** drop "separate Falkor graphs" from the convergence and replace it with "per-tenant Redis ACL principals and per-tenant FalkorDB credentials; graph-per-tenant selection already exists, so the residual risk is that tenant isolation rests on application code over one shared connection."

### COD-10 — "Tests state tier and boundary" is aspirational
**Severity: low**

Line 183 states tests "state tier and boundary". Only 118 test classes carry `[Trait("Category", …)]` across a suite spanning 13 test projects; there is no guard requiring it and `tests/README.md` defines no tier vocabulary.

**Recommended spine edit:** soften to "tests **should** state tier and boundary" or add a gap row for a trait-completeness guard.

### COD-11 — `Client*/` "Consumer ports" names something that does not exist
**Severity: low**

Structural Seed line 223 reads `Hexalith.Memories.Client*/  # Consumer ports and transport adapters`. Only `Hexalith.Memories.Client.Rest` exists, and it exposes no port abstraction — `MemoriesClient` is a concrete sealed class; the sole `public interface` in Contracts is `IWorkflowTraceContextCarrier` (`src/Hexalith.Memories.Contracts/V1/IWorkflowTraceContextCarrier.cs:9`). AD-1 tells consumers to depend on "published contracts, clients"; with no interface, a consumer binds to the concrete HTTP client.

**Recommended spine edit:** change the comment to `# Transport adapter (REST); no port interface today` or add the port extraction as a gap row under AD-1.

### COD-12 — Gap row 2 omits the read-path defect, which is the more dangerous half
**Severity: medium**

Row 2 correctly says missing axes can still produce `Indexed`. It does not mention that the status is never persisted and is *defaulted* to `Indexed` on read: `src/Hexalith.Memories.Server/Activities/Indexing/SyntacticHashProjection.cs:66-92` writes no `status` field, and `src/Hexalith.Memories.Server/Cases/CaseService.cs:995-997` resolves an absent or unparseable status to `MemoryUnitStatus.Indexed`. A `GET` on a memory unit therefore reports `Indexed` from **syntactic-hash presence alone**, with vector and graph unexamined — a stronger violation of AD-3 than "the completion gate is missing", because it means the status API cannot report `Indexing` or `Failed` for these units at all. Two further unconditional transitions exist at `Workflows/IngestionWorkflow.cs:96` and `:913`.

**Recommended spine edit:** extend row 2's claim with "and the memory-unit read path defaults an unpersisted status to `Indexed` (`CaseService.cs:995-997`), so `Indexed` is inferred from syntactic-hash presence alone"; add "persist the status field on the projection" to the Required convergence.

---

## What the spine gets right

Recorded because a brownfield spine's job is to ratify sound existing convention, and most of this one does:

- The `Contracts/V1` boundary is real **and** mechanically defended — `ContractPersistenceSeparationTests.cs:104-121` asserts both that no persistence-shaped type leaks and that the assembly references no other `Hexalith.*` assembly.
- `MemoriesRoutes` + `/api/v1` is real and reflection-guarded for completeness (`MemoriesRoutesTests.cs:41-57`), with the Server↔client single-source-of-truth rationale documented in the file header.
- The V1 status wire contract is exactly as described, down to integer-token rejection, and matches the PRD's product-label mapping without contradiction.
- The EventStore `Domain/` purity claim (line 39) is verified true: every non-`System` `using` under `src/Hexalith.Memories.EventStore/Domain/` resolves to `Contracts.V1` or its own subtree.
- The `Hexalith.Memories.Redis` "compatibility facade, not the adapter owner" characterisation (line 102) is exactly right, and the package README says the same thing.
- The Stack table's pins are correct against the committed `Hexalith.Builds` catalog — a check that requires comparing against the committed gitlink `a32cb422` rather than the dirty working-tree submodule, which would falsely suggest six version drifts.
- `IngestionWorkflowDeterminismGuardTests` is a genuinely strong AD-4 guard, pinning four exact workflow-start sites by file:line and forbidding ambient trace capture in orchestration code.
- The gap table's direction is sound: 12 of 15 obligations are real today, and **none** has been silently closed.

## Suggested triage order

1. **COD-01** (critical) — reword AD-8 and the conventions row so the namespace/test boundary is stated as target, and add the namespaces + guard to gap row 7's convergence.
2. **COD-02** (high) — correct gap row 14; the specimen host exists and is CI-gated.
3. **COD-03** (high) — correct gap row 8 and flag that convergence requires retiring green tests.
4. **COD-04, COD-05, COD-06, COD-07, COD-12** (medium) — Stack/Seed/convention completeness and accuracy.
5. **COD-08, COD-09, COD-10, COD-11** (low) — phrasing precision.
