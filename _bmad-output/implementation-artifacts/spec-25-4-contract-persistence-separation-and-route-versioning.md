---
title: 'Story 25.4: Contract/Persistence Separation & Route Versioning'
type: 'refactor'
created: '2026-07-11T00:00:00+02:00'
status: 'blocked'
baseline_revision: 'ad7cb31f66238bfa2107288886e2924044274bcb'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
warnings: [oversized, multiple-goals]
---

<intent-contract>

## Intent

**Problem:** Public V1 CLR contracts expose Redis/FalkorDB terminology, all REST routes are unversioned, and the public Contracts package owns JSON metadata and DTOs used only for durable server state. This makes a storage swap, V2 introduction, or stored-state evolution unnecessarily breaking.

**Approach:** Make public CLR names retrieval-axis-oriented while pinning the existing V1 JSON property shapes, cut the canonical REST surface over to `/api/v1/` through `MemoriesRoutes`, and move server-only durable payload models and serialization metadata into the Server boundary.

## Boundaries & Constraints

**Always:** Preserve every existing JSON property name, value type, default, enum spelling, and backward deserialization behavior; preserve all 46 HTTP verbs, authentication/authorization, tenant isolation, rate limiting, telemetry, response bodies/statuses, and Dapr infrastructure routes; preserve historical Redis, actor, state-store, and Dapr workflow payload readability; use one C# type per file and source-generated JSON metadata at the owning boundary.

**Block If:** A stored payload cannot be dual-read without destructive migration, a supposedly server-only DTO is consumed by a published client/API surface, or versioning requires changing `/events/ingest`, `/dapr/subscribe`, `/mcp`, health routes, or external Ollama `/api/embed`.

**Never:** Keep backend-named CLR members in public Contracts as aliases; redirect or silently serve legacy `/api/*` REST routes; change legacy V1 JSON field names to axis names; add package dependencies to Contracts; edit submodules; broaden into Story 25.5 CLI transport consolidation or Story 25.6 MCP executor work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Canonical REST | Any of the 46 operations at `/api/v1/...` | Same verb, security, result, headers, and telemetry as before | Existing structured errors unchanged |
| Legacy REST | Representative ingest, search, and tenant request at unversioned `/api/...` | Route is not mapped | `404`; no redirect or duplicate handler |
| Contract JSON | Legacy backend-named V1 JSON | Deserializes into axis-named CLR members and serializes with identical legacy names | Malformed payload behavior unchanged |
| Stored state | Existing retry, actor, registry, case-member, failed-unit, or workflow JSON | New server-owned persistence model/context reads it and rewrites an equivalent shape | Corrupt-state behavior remains current behavior |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Contracts/V1/{TenantIndexSizes,TenantIndexStatus,ConsistencySemanticDetail}.cs` -- backend-named public CLR members whose V1 JSON names must remain pinned.
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` -- single source for 37 templates, 19 builders, 46 server mappings, client paths, auth, and rate-limit prefix checks.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` -- public wire-contract context currently mixed with durable server payload registrations.
- `src/Hexalith.Memories.Server/Serialization/` -- new owner for persistence JSON metadata and compatibility mappings.
- `src/Hexalith.Memories.Server/{NaturalLanguage,Activities,Workflows,Actors,Tenants,Cases,Ingestion}/` -- durable payload producers/consumers and generated response links.
- `docs/operations/route-surface.md` -- 46-route and downstream Dapr ACL mapping contract.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Memories.Contracts/V1/TenantIndexSizes.cs`, `TenantIndexStatus.cs`, and `ConsistencySemanticDetail.cs` -- rename CLR members to syntactic/semantic/graph concepts, pin their legacy JSON names explicitly, and remove backend-specific public documentation; update all source consumers and serialization fixtures without adding backend-named aliases.
- `src/Hexalith.Memories.Contracts/V1/{TenantProvisioningResult,TenantDeletionResult,ConsistencyDiscrepancy,ConsistencyInspectionResult,ConsistencyGraphDetail,ConsistencySyntacticDetail,ConsistencyRepairRecommendation,IndexHealth}.cs` -- replace backend terminology in public descriptions and emit axis-oriented provisioning/deletion step values while retaining legacy payload-read compatibility.
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` -- set `ApiPrefix` to `/api/v1`, version all templates/builders, and add escaped builders for every server-generated `Location`/status/resource link.
- `src/Hexalith.Memories.Server/Endpoints/{Ingestion,TenantLifecycle,Cases,Consistency,Export,Search}Endpoints.cs`, `Endpoints/ErrorResults.cs`, `Tenants/TenantStatusGuard.cs`, and `EventStoreIntegration/EventStoreRoutingConfigValidator.cs` -- derive mappings, returned links, operation labels, and recovery guidance from the V1 route table; preserve endpoint behavior.
- `src/Hexalith.Memories.Contracts/V1/{BatchedGraphDeletionInput,BatchedGraphDeletionResult,CounterTransitionInput,ExtractionInput,ExtractionResult,FailedUnitInput,FetchUrlInput,UrlFetchResult,IndexInput,IndexResult,NaturalLanguageDescriptionInput,NaturalLanguageDescriptionResult,NaturalLanguageIndexInput,QueueNaturalLanguageEmbeddingRetryInput,FailedNaturalLanguageEmbeddingRecord,NaturalLanguageEmbeddingRetryInput,NaturalLanguageEmbeddingRetryResult}.cs` -- move server-only durable workflow/activity/retry models into focused files under `src/Hexalith.Memories.Server/Workflows/Contracts/` or the owning server feature; preserve serialized member names/defaults and update generic workflow/activity consumers.
- `src/Hexalith.Memories.Server/Serialization/MemoriesPersistenceJsonContext.cs` and focused stored-model/mapping files under `Serialization/` -- own source-generated metadata for moved payloads and explicit stored representations of tenant registry/config, fusion weights, case members, failed-unit details/references/metadata; map to public contracts while preserving historical stored JSON.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`, `src/Hexalith.Memories.Server/{Actors/TenantConfigurationActor.cs,Tenants/TenantRegistryService.cs,Cases/CaseService.cs,Ingestion/FailedUnitsRegistry.cs,Activities/Ingestion/PersistFailedUnitActivity.cs,NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs,Activities/Indexing/IndexSyntacticActivity.cs,Activities/Indexing/IndexGraphActivity.cs,EventStoreIntegration/RedisSearchIndexMaintenanceAdapter.cs}` -- remove persistence-only registrations from the public context and route durable serialization through the server context/mappings; keep HTTP serialization on the Contracts context.
- `src/Hexalith.Memories.Cli/{Commands/TenantListCommand.cs,Commands/StatusTelemetryCommand.cs,Commands/QuickstartCommand.cs,Errors/ErrorMessageCatalog.cs}`, `docs/operations/{route-surface,rate-limiting,failure-recovery}.md`, and `docs/dev/{consistency,eventstore-integration,export,health-checks,ingest-contract,mcp-server,memory-unit-id-stability,quickstart,telemetry}.md` -- update route-bearing guidance to `/api/v1/` without changing CLI command behavior or external Ollama paths.
- `tests/Hexalith.Memories.Contracts.Tests/V1/`, `tests/Hexalith.Memories.Server.Tests/{Serialization,Deployment,Endpoints,Tenants,Actors,Cases,Ingestion,NaturalLanguage,Workflows}/`, `tests/Hexalith.Memories.Cli.Tests/ClientRest/`, and `tests/Hexalith.Memories.IntegrationTests/` -- move server-only serialization tests, add golden legacy JSON/stored-state round trips, pin V1 routes/links/ACL mapping, assert representative unversioned routes return 404, and update the Aspire bearer-path parser.

**Acceptance Criteria:**
- Given legacy V1 JSON for tenant index sizes/status and semantic inspection, when it is deserialized and serialized through public contract metadata, then axis-named CLR members hold the values and the emitted property names/types remain byte-shape compatible.
- Given the server and REST client route inventories, when their routes are enumerated, then all 46 registrations and all client requests use `/api/v1/`, returned `Location` URLs are V1, and representative unversioned REST paths return 404.
- Given tenant authorization and rate-limit tests at V1 paths, when cross-tenant and quota scenarios execute, then denial, partitioning, structured errors, audit, and telemetry match the pre-versioning behavior.
- Given historical durable JSON already stored for every moved or mapped persistence shape, when the owning registry, actor, workflow, or service reads and rewrites it, then the asserted state-store/Redis end-state preserves every field/default/value and corrupt-payload handling remains unchanged.
- Given the Contracts assembly public types and JSON registrations are inspected, when the architecture guard runs, then server-only persistence/workflow DTOs and backend-named CLR members are absent and Contracts remains dependency-free.
- Given route documentation and the Dapr mapping guard, when verified, then exactly 46 `/api/v1/` rows exist and search maps to `method/api/v1/search`, while infrastructure and Ollama routes remain unchanged.

## Spec Change Log

## Review Triage Log

## Design Notes

This is an intentional breaking pre-GA route/CLR cutover and must be released as a breaking refactor. V1 payload compatibility means legacy JSON names remain authoritative even when CLR names become neutral; a future V2 may introduce neutral wire names. Legacy `/api/*` aliases are deliberately absent so there is one canonical operation family and downstream Dapr ACLs must update atomically.

## Verification

**Commands:**
- `dotnet restore Hexalith.Memories.slnx && dotnet build Hexalith.Memories.slnx --configuration Release -m:1` -- expected: 0 warnings and 0 errors.
- `dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -c Release -m:1 && dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release -m:1 && dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj -c Release -m:1 && dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj -c Release -m:1 && dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Release -m:1` -- expected: all test assemblies build; invoke each built xUnit v3 assembly with focused `-class` lanes for contract serialization, persistence compatibility, ClientRest, endpoint/auth/rate-limit, route-surface, workflow, and MCP before running each touched assembly without filters.
- `rg -n '/api/(?!v1(?:/|$))' src tests docs --pcre2` with explicit Ollama/infrastructure allowlist review -- expected: no stale Memories REST route.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.

## Auto Run Result

Status: blocked
Blocking condition: implementation verification failed

The full `Hexalith.Memories.IntegrationTests` assembly completed with 122 tests: 119 passed and 3 failed. The failures were outside the Story 25.4 behavior: `SyntacticSearchIntegrationTests.SearchAsync_OffsetPagination_ShouldSkipResults` observed unstable result ordering, `SemanticSearchIntegrationTests.SearchAsync_LatencySmokeTest_10ConcurrentQueries_ShouldBeFast` seeded without provisioning its required semantic index, and `PipelinePersistencePerformanceTests.RunPipelinePersistenceBenchmarks_ShouldMeetWarmRestartAndThroughputTargets` lost its Aspire server connection during teardown. Story-focused contract, persistence, route-surface, legacy-route, actor, registry, and case lanes passed; Contracts (568), CLI (424), MCP (90), and Server (2555 passed, 1 existing skip) broad assemblies passed; the Release solution and all required test projects built with 0 warnings/errors.
