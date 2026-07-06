---
title: '25.1 Program.cs Decomposition'
type: 'refactor'
created: '2026-07-06'
status: 'done'
baseline_revision: 'c7f4e07cd26d2adc795597d3d6b7765ff4a78637'
final_revision: '2294eea8aac6668a10e955fb7a014e0d2cca0da4'
review_loop_iteration: 0
followup_review_recommended: false
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-25-context.md']
warnings: ['oversized']
---

<intent-contract>

## Intent

**Problem:** `src/Hexalith.Memories.Server/Program.cs` is a 4,745-line composition root with most API route behavior embedded as inline lambdas, making route review, merge safety, and focused endpoint testing difficult. Story 25.1 must reduce the composition root to startup orchestration and route registration without changing the public API or observable behavior.

**Approach:** Extract the existing service/workflow/actor wiring into hosting extensions and the Minimal API mappings into focused endpoint classes for ingestion, tenant lifecycle, cases, search, graph, consistency, and export. Move endpoint-only helpers with their owning endpoint group or into a shared endpoint helper, update the route-surface drift guard to scan extracted endpoint files, and preserve middleware order, filters, telemetry, validation, error envelopes, route literals, and streaming behavior.

## Boundaries & Constraints

**Always:** Preserve existing route templates, HTTP verbs, service lifetimes, hosted-service/workflow/actor registrations, middleware order, endpoint filters, auth/rate-limit behavior, telemetry event IDs, structured `ErrorResponse` payloads, tenant-active versus tenant-exists distinctions, search degradation semantics, export response-start timing, and `public partial class Program` for `WebApplicationFactory<Program>`. New C# files must use the ITANEO copyright header, file-scoped namespaces, one type per file, `ConfigureAwait(false)` in awaited library/helper code, and central package conventions.

**Block If:** Implementation discovers a required route rename, new `/api/v1` route versioning, public contract shape change, endpoint behavior rewrite, or deletion of an existing endpoint. Those belong to later Epic 25 stories, not this decomposition.

**Never:** Do not centralize errors or tenant filters beyond mechanical helper moves, do not introduce controllers, OpenAPI, new packages, route versioning, or reusable framework abstractions, do not touch submodules, and do not edit generated `bin/` or `obj/` artifacts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Startup wiring | Existing service, workflow, actor, JSON, health, authentication, authorization, rate-limit, EventStore, and telemetry setup | Same registrations and middleware order after extraction; `Program.cs` calls hosting/endpoint extensions and remains thin | Missing registration fails build, startup, or existing focused tests |
| Route surface | Existing documented 46 `/api/*` routes plus `/events/ingest`, `/dapr/subscribe`, `/mcp` | Same route templates and verbs after extraction; route-surface test scans endpoint files, not only `Program.cs` | Missing or phantom route fails route contract tests |
| Ingestion dedup | Concurrent duplicate `POST /api/ingest` with preflight dedup enabled | Losing request returns accepted winner instance without scheduling duplicate workflow | Reservation release still runs on scheduling failure |
| Search variants | `/api/search` with syntactic, semantic, nl, graph, hybrid, graph-scoped axes | Same validation order, clamping, degradation, enrichment, explain, token-budget, metrics, and status codes | Existing `ErrorResponse` and degraded result shapes preserved |
| Export streaming | Tenant or case export request with invalid tenant/case/backend unavailable | Validation and snapshot capture happen before `Response.StartAsync`; successful exports keep headers and streamed JSON | Pre-start failures return JSON 400/404/503; mid-stream behavior unchanged |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Server/Program.cs` -- composition root to slim down; must retain builder/app orchestration, middleware order, meter/gauge setup, route registration calls, `app.Run()`, and empty `partial class Program`.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` -- new hosting extension for existing service, health, authentication, authorization, HTTP client, keyed Redis/FalkorDB, workflow, actor, JSON, and EventStore registrations.
- `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs` -- new helper for existing rate-limit partition/key/tag logic used by service registration.
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs` -- new route class for `/api/ingest*` mappings plus ingestion validation/audit helpers.
- `src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs` -- new route class for tenant provisioning, deletion, configuration, verification, telemetry, and handler diagnostic routes.
- `src/Hexalith.Memories.Server/Endpoints/CasesEndpoints.cs` -- new route class for `/api/tenants/{tenantId}/cases*`, failed-unit, member, memory-unit, annotation, activity, and re-ingestion mappings.
- `src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs` -- new route class for `/api/search` plus search helper methods and result enrichment.
- `src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs` -- new route class for traversal and edge confidence promotion.
- `src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs` -- new route class for consistency verify/status/inspect/repair routes.
- `src/Hexalith.Memories.Server/Endpoints/ExportEndpoints.cs` -- new route class for tenant and case export streaming routes.
- `src/Hexalith.Memories.Server/Endpoints/EndpointTelemetryHelpers.cs` -- optional shared helper for audit user, scope creation, audit query params, result marking, and identifier prefixes used by multiple endpoint classes.
- `src/Hexalith.Memories.Server/Endpoints/EndpointValidationHelpers.cs` -- optional shared helper for tenant validation and small JSON request-body parsing helpers used by multiple endpoint classes.
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` -- update extraction to scan `Program.cs` and endpoint files so the route/doc guard survives decomposition.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` -- move current service, health, workflow, actor, JSON, EventStore, HTTP client, keyed multiplexer, hosted-service, and options registration behind `AddMemoriesServerServices(...)`; preserve all lifetimes and registration order-sensitive behavior.
- [x] `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs` -- move current inbound rate-limit partition helpers and rejection response setup without changing partition keys, retry headers, metrics, or excluded ingestion paths.
- [x] `src/Hexalith.Memories.Server/Program.cs` -- replace inline service registration and route lambdas with hosting extension calls and calls to `MapIngestionEndpoints`, `MapTenantLifecycleEndpoints`, `MapExportEndpoints`, `MapConsistencyEndpoints`, `MapCasesEndpoints`, `MapSearchEndpoints`, and `MapGraphEndpoints`; preserve middleware order and startup orchestration.
- [x] `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs` -- move the four ingestion and batch status mappings, request size limit, tenant authorization/rate-limit filters, ingestion validation, dedup reservation behavior, and status tenant authorization logic.
- [x] `src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs` -- move tenant create/list/detail/config/update/delete/status/verify/telemetry/handler mappings while preserving workflow instance prefixes, tenant status checks, `HXL002` headers, and embedding-config conflict response.
- [x] `src/Hexalith.Memories.Server/Endpoints/CasesEndpoints.cs` -- move all case subtree mappings while preserving `by-source-uri` literal route precedence, member JSON parsing, failed-unit synthesized memory units, re-ingestion outcomes, deletion and annotation audit scopes.
- [x] `src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs` -- move `/api/search` and all search-only helpers while preserving axis validation order, graph query-not-required behavior, hybrid fallback/degradation, token-budget metadata, explain metadata, case attribution, annotation counts, and rolling counter telemetry.
- [x] `src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs` -- move traversal and confidence promotion mappings while preserving edge-type parsing, degraded graph responses, token-budget traversal metadata, and confidence validation.
- [x] `src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs` -- move consistency routes while preserving batch-size bounds, tenant-exists diagnostic behavior, instance-id prefix checks, and backend error envelopes.
- [x] `src/Hexalith.Memories.Server/Endpoints/ExportEndpoints.cs` -- move export routes while preserving pre-stream validation/snapshot capture, response headers, schema version header, and streaming writer calls.
- [x] `src/Hexalith.Memories.Server/Endpoints/EndpointTelemetryHelpers.cs` and `src/Hexalith.Memories.Server/Endpoints/EndpointValidationHelpers.cs` -- add only if needed to avoid duplicate copied helper logic; keep helpers internal and endpoint-specific.
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` -- update mapped-route extraction to read all endpoint mapping files plus `Program.cs` and keep the documented-route count and per-route assertions.
- [x] `docs/operations/route-surface.md` -- inspect and leave unchanged unless route-surface tests prove the existing documented route count is stale; this story should not add or remove rows.

**Acceptance Criteria:**
- Given the server starts, when `Program.cs` is inspected, then it contains builder creation, service extension calls, app build/init, middleware, meter setup, endpoint extension calls, `app.Run()`, and `Program` sentinel only, with total line count no greater than 200.
- Given the route-surface contract tests run, when mapped routes are extracted from the decomposed endpoint files, then every mapped `/api/*` route remains documented and the documented route count still matches code.
- Given focused endpoint tests run, when ingestion, tenant lifecycle, cases, search, graph, consistency, export, telemetry, authorization, and rate-limit scenarios execute, then existing status codes, response bodies, headers, filters, and telemetry side effects remain unchanged.
- Given the solution builds with warnings as errors, when `dotnet build Hexalith.Memories.slnx` runs, then no warnings or errors are introduced.

## Spec Change Log

## Review Triage Log

### 2026-07-06 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 3: (high 0, medium 1, low 2)
- defer: 11: (high 0, medium 9, low 2)
- reject: 7
- addressed_findings:
  - `[medium]` `[patch]` Route-source scanning could pass even if `Program.cs` stopped invoking a decomposed endpoint registration; added `Program_InvokesAllDecomposedEndpointRegistrations` to the route-surface contract tests.
  - `[low]` `[patch]` `InboundRateLimitPartitionFactory` depended on an endpoint helper for principal resolution; added neutral `AuditPrincipalResolver` under telemetry and used it from both hosting and endpoint helpers.
  - `[low]` `[patch]` stale tenant-validation TODO comments survived the move into `CasesEndpoints`; removed the obsolete comments while preserving behavior.

## Design Notes

Keep route registration extension methods on `IEndpointRouteBuilder` so `Program.cs` reads as startup orchestration rather than endpoint implementation:

```csharp
app.MapIngestionEndpoints();
app.MapTenantLifecycleEndpoints();
app.MapExportEndpoints();
app.MapConsistencyEndpoints();
app.MapCasesEndpoints();
app.MapSearchEndpoints();
app.MapGraphEndpoints();
```

Do not try to make route groups change URLs in this story. If a group is used, it must combine to the exact existing template; otherwise keep exact `MapGet`/`MapPost` templates inside the endpoint class.

## Verification

**Commands:**
- `dotnet build Hexalith.Memories.slnx` -- expected: succeeds with zero warnings.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~RouteSurfaceContractTests|FullyQualifiedName~DeploymentConfigurationContractTests|FullyQualifiedName~DocumentationCompletenessTests"` -- expected: route/doc/deployment contract tests pass.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~ServerEndpointAuthorizationTests|FullyQualifiedName~ServerEndpointRateLimitTests|FullyQualifiedName~TenantAuthorizationEndpointFilterTests|FullyQualifiedName~ProgramHealthCheckRegistrationTests|FullyQualifiedName~ReadyEndpointAggregationTests|FullyQualifiedName~MiddlewareOrderTests|FullyQualifiedName~EventIngestionOutcomeTests|FullyQualifiedName~CrossModuleEventIntakeE2ETests"` -- expected: auth, middleware, health, rate-limit, and event-ingest tests pass.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~SearchEndpointContractTests|FullyQualifiedName~SearchEndpointDegradationTests|FullyQualifiedName~SearchEndpointTokenBudgetTests|FullyQualifiedName~TraverseEndpointTokenBudgetTests|FullyQualifiedName~IngestionEndpointE2ETests|FullyQualifiedName~DirectoryIngestionEndpointE2ETests|FullyQualifiedName~ReIngestionEndpointE2ETests|FullyQualifiedName~CaseMutationEndpointE2ETests|FullyQualifiedName~ConsistencyEndpointTests|FullyQualifiedName~TenantConfigurationEndpointTests|FullyQualifiedName~TenantEmbeddingConfigEndpointTests|FullyQualifiedName~MemoryUnitLookupEndpointTests|FullyQualifiedName~TelemetrySummaryEndpointTests"` -- expected: focused endpoint behavior tests pass.
- `dotnet test tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --filter "FullyQualifiedName~McpEndpointAllowAnonymousPathsTests|FullyQualifiedName~McpEndpointChallengeBodyTests|FullyQualifiedName~McpCompositionRootTests|FullyQualifiedName~McpToolSchemaTests"` -- expected: MCP endpoint/auth/schema tests pass.
- `git diff --check -- src/Hexalith.Memories.Server/Program.cs src/Hexalith.Memories.Server/Endpoints tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` -- expected: no whitespace errors.

## Auto Run Result

Status: done

Summary: Decomposed the Memories Server composition root into focused hosting and endpoint files while preserving existing routes and behavior. `Program.cs` is now 86 lines and contains startup orchestration, middleware, meter setup, endpoint registration calls, `app.Run()`, and the `Program` sentinel.

Files changed:
- `src/Hexalith.Memories.Server/Program.cs` -- reduced to orchestration and endpoint extension calls.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` -- moved existing service, workflow, actor, health, JSON, EventStore, hosted-service, keyed backend, and option registrations.
- `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs` -- moved inbound rate-limit partitioning and rejection response behavior.
- `src/Hexalith.Memories.Server/Telemetry/AuditPrincipalResolver.cs` -- added neutral audit-principal resolver shared by hosting and endpoints.
- `src/Hexalith.Memories.Server/Endpoints/*Endpoints.cs` -- moved ingestion, tenant lifecycle, cases, search, graph, consistency, and export route mappings into resource-specific endpoint registration classes.
- `src/Hexalith.Memories.Server/Endpoints/EndpointTelemetryHelpers.cs` and `src/Hexalith.Memories.Server/Endpoints/EndpointValidationHelpers.cs` -- added shared endpoint-local helpers for moved behavior.
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` -- updated route-source scanning and added Program endpoint-registration assertions.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- recorded pre-existing edge cases surfaced by review.

Review findings breakdown: patched 3 findings (1 medium, 2 low), deferred 11 findings (9 medium, 2 low), rejected 7 findings. Follow-up review recommended: false.

Verification performed:
- `dotnet build Hexalith.Memories.slnx` -- passed, 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~RouteSurfaceContractTests|FullyQualifiedName~DeploymentConfigurationContractTests|FullyQualifiedName~DocumentationCompletenessTests"` -- passed, 18 tests.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~ServerEndpointAuthorizationTests|FullyQualifiedName~ServerEndpointRateLimitTests|FullyQualifiedName~TenantAuthorizationEndpointFilterTests|FullyQualifiedName~ProgramHealthCheckRegistrationTests|FullyQualifiedName~ReadyEndpointAggregationTests|FullyQualifiedName~MiddlewareOrderTests|FullyQualifiedName~EventIngestionOutcomeTests|FullyQualifiedName~CrossModuleEventIntakeE2ETests"` -- passed, 72 tests.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~SearchEndpointContractTests|FullyQualifiedName~SearchEndpointDegradationTests|FullyQualifiedName~SearchEndpointTokenBudgetTests|FullyQualifiedName~TraverseEndpointTokenBudgetTests|FullyQualifiedName~IngestionEndpointE2ETests|FullyQualifiedName~DirectoryIngestionEndpointE2ETests|FullyQualifiedName~ReIngestionEndpointE2ETests|FullyQualifiedName~CaseMutationEndpointE2ETests|FullyQualifiedName~ConsistencyEndpointTests|FullyQualifiedName~TenantConfigurationEndpointTests|FullyQualifiedName~TenantEmbeddingConfigEndpointTests|FullyQualifiedName~MemoryUnitLookupEndpointTests|FullyQualifiedName~TelemetrySummaryEndpointTests"` -- passed, 112 tests.
- `dotnet test tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --filter "FullyQualifiedName~McpEndpointAllowAnonymousPathsTests|FullyQualifiedName~McpEndpointChallengeBodyTests|FullyQualifiedName~McpCompositionRootTests|FullyQualifiedName~McpToolSchemaTests"` -- passed, 17 tests.
- `git diff --check -- src/Hexalith.Memories.Server/Program.cs src/Hexalith.Memories.Server/Endpoints src/Hexalith.Memories.Server/Hosting src/Hexalith.Memories.Server/Telemetry tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md _bmad-output/implementation-artifacts/deferred-work.md` -- passed.

Residual risks: Full Docker/Aspire integration lanes were not run; review-surfaced pre-existing endpoint edge cases were deferred rather than folded into this behavior-preserving decomposition.
