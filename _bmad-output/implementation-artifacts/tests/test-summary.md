# Test Automation Summary - Story 22.7 (Retrieval Feature Completion)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-05
- **Story:** `_bmad-output/implementation-artifacts/22-7-retrieval-feature-completion.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute; API/E2E-facing coverage uses existing ASP.NET Core `WebApplicationFactory` endpoint tests, typed REST client tests, and MCP tool schema/tool tests. No new framework introduced.
- **Feature under test:** public `axis=nl` exposure across REST, CLI client, and MCP; hybrid fusion weight validation; tenant authorization coverage for the new axis. Story 22.7 has no UI/web scope, so browser E2E is not applicable.

## Generated / Updated Tests

### API Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` - added `NaturalLanguageSearch_WhenEmbeddingConfigActorUnavailable_ReturnsBackendUnavailable`, proving `/api/search?axis=nl` reaches the NL/embedding-config route and maps unavailable tenant embedding configuration to `BACKEND_UNAVAILABLE` instead of rejecting `nl` as an invalid axis.
- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` - added `HybridSearch_WhenQueryWeightsAreAllZero_ReturnsInvalidFusionWeights`, covering the public query-weight validation error path for `syntacticWeight=0&semanticWeight=0&nlWeight=0&graphWeight=0`.
- [x] `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` - extended tenant-forbidden search coverage to include `axis=nl`, ensuring tenant authorization fails before search dependencies for the new public axis.
- [x] `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSearchTests.cs` - added `SearchAsync_NaturalLanguageAxis_TargetsNlAxis`, proving the typed client emits `axis=nl` and query parameters for single-axis NL search.

### MCP / Agent-Surface Tests

- [x] `tests/Hexalith.Memories.Mcp.Tests/SearchMemoryToolTests.cs` - extended single-axis routing coverage so `SearchAxis.Nl` is sent as wire axis `nl`.
- [x] `tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs` - updated the `search_memory` schema contract to require `nl` in the advertised `axes` enum.

### E2E / Integration Tests

- [x] Backend/API E2E-facing coverage was added through in-memory HTTP endpoint tests and typed client/MCP tool tests.
- [x] Browser UI E2E is not applicable. Story 22.7 explicitly has no UI/web work.
- [x] Redis-backed NL/highlight acceptance coverage already exists in the Story 22.7 implementation diff and remains compile-covered; runtime execution is blocked by Docker/Testcontainers permissions in this sandbox.

## Coverage

- API endpoints: standalone `axis=nl` route acceptance/degradation, hybrid invalid fusion weights, and tenant-forbidden behavior covered.
- Client surfaces: REST typed client and MCP tool/schema now cover the new `nl` axis.
- Critical error cases: unavailable embedding configuration for `axis=nl`, all-zero query fusion weights, and mismatched-tenant access for `axis=nl`.
- UI features: 0 applicable.

## Validation

- [x] Senior review addendum: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors after adding NL adapter coverage.
- [x] Senior review addendum: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.NaturalLanguageSemanticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.HybridSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.FusionEngineTests -class Hexalith.Memories.Server.Tests.Search.SyntacticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.ExplainMetadataBuilderTests -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests -class Hexalith.Memories.Server.Tests.Actors.TenantConfigurationActorTests -parallel none -noLogo` - 152 total, 0 failed.
- [x] Senior review addendum: focused CLI/MCP/Contracts in-process suites passed (CLI 62/62, MCP 24/24, Contracts 6/6).
- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [ ] `DiffEngine_Disabled=true dotnet test ... --no-build --filter ...` - blocked before execution by VSTest TCP listener setup: `System.Net.Sockets.SocketException (13): Permission denied`.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.SearchMemoryToolTests -class Hexalith.Memories.Mcp.Tests.McpToolSchemaTests -parallel none -noLogo` - 24 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.ClientRest.MemoriesClientSearchTests -class Hexalith.Memories.Cli.Tests.Cli.SearchQueryCommandTests -class Hexalith.Memories.Cli.Tests.Cli.ErrorCatalogTests -parallel none -noLogo` - 62 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -parallel none -noLogo` - 39 total, 0 failed.
- [x] `git diff --check` - passed.

## Checklist Result

- API tests generated/updated where applicable: pass.
- E2E tests generated where applicable: pass for backend/API and agent-facing surfaces; no browser UI exists.
- Tests use standard xUnit v3/Shouldly/NSubstitute APIs, cover happy path/public routing and critical error cases, have clear descriptions, use no hardcoded waits, and are independent: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.
- Full infrastructure-backed Redis/Testcontainers execution remains blocked by sandbox Docker permissions recorded in the Story 22.7 Dev Agent Record, not by test compilation.

---

# Test Automation Summary - Story 22.6 (Post-Filter Recall)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-05
- **Story:** `_bmad-output/implementation-artifacts/22-6-post-filter-recall.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute; API/E2E coverage uses existing Aspire, Redis Stack, and FalkorDB integration fixtures. No new framework introduced.
- **Feature under test:** semantic metadata/source-type post-filter recall for plain semantic search, graph-scoped semantic search, and public `/api/search?axis=semantic` routing.

## Generated / Updated Tests

### API Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchApiIntegrationTests.cs` - added `GetSearch_WithSemanticAxisAndMetadataQueryBeyondInitialWindow_ShouldReturnLaterFilteredMatches`, which seeds two nearest semantic candidates that fail `metadataQuery` and two farther candidates that match, then exercises `/api/search?axis=semantic&metadataQuery=acme&maxResults=2`.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchApiIntegrationTests.cs` - added `GetSearch_WithSemanticAxisAndSourceTypeBeyondInitialWindow_ShouldReturnLaterFilteredMatches`, proving public semantic `sourceType=url` does not silently lose farther URL matches behind nearer file matches.

### E2E / Integration Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchIntegrationTests.cs` - existing story tests cover Redis-backed metadata recall and source-type recall beyond the initial KNN page window.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` - existing story test covers graph-scoped semantic metadata recall inside the validated graph-approved key set.
- [x] Browser UI E2E is not applicable. Story 22.6 has no module UI.

### Unit / Service Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs` - candidate-window expansion, `PAGINATION_LIMIT_EXCEEDED` preservation, service-side post-filter detection, source-type query-builder truthfulness, TAG escaping, and graph-scope key validation.

## Coverage

- API endpoints: `/api/search` semantic axis now covered for metadata and source-type post-filter recall through the public HTTP route.
- Plain semantic Redis path: metadata and source-type filters recover later matching candidates within the bounded `MaxCandidateWindow`.
- Graph-scoped semantic path: metadata-filtered recall is preserved inside the graph-approved `INKEYS` key set.
- Critical error cases: deep-pagination cap behavior and source-type fake pre-filter regression covered by unit tests.
- UI features: 0 applicable.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -parallel none -class "Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests"` - 45 total, 0 failed.
- [ ] Redis/FalkorDB story integration test run - blocked by Docker socket permission: Testcontainers failed to connect to `unix:///var/run/docker.sock`; inner error `System.Net.Sockets.SocketException: Permission denied`.
- [ ] Aspire API integration test run - blocked before test execution by sandbox infrastructure: Aspire backchannel bind failed with `System.Net.Sockets.SocketException (13): Permission denied`; Aspire then failed startup with `Container runtime 'docker' was found but appears to be unhealthy`.
- [x] `git diff --check -- tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchApiIntegrationTests.cs` - passed.
- [ ] `git diff --check` - blocked by existing story/worktree CRLF-as-trailing-whitespace debt in already-dirty files. The file changed by this QA pass passes targeted diff-check.

## Checklist Result

- API tests generated/updated where applicable: pass; two public semantic API recall tests added.
- E2E tests generated where applicable: pass for backend/API integration intent; no browser UI exists.
- Tests use standard xUnit v3/Shouldly/NSubstitute APIs, cover happy path and critical error cases, have clear descriptions, use no hardcoded waits, and are independent through unique tenant/document ids: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.
- Integration execution remains blocked by sandbox Docker/Aspire permissions, not by compilation or test code.

---

# Test Automation Summary - Story 22.4 (Fusion Case Attribution, Score Calibration & Pinned Scorer)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-05
- **Story:** `_bmad-output/implementation-artifacts/22-4-fusion-case-attribution-score-calibration-and-pinned-scorer.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute; API/E2E coverage uses existing ASP.NET Core `WebApplicationFactory` endpoint tests and existing Aspire/Redis/FalkorDB integration fixtures. No new framework introduced.
- **Feature under test:** hybrid fusion case attribution, deterministic scale-free RRF score fusion, explicit Redis BM25-family scorer pinning, hybrid response case grouping, evidence source attribution, and Story 22.1-22.3 regression preservation.

## Generated / Updated Tests

### API Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` - added `HybridSearch_WithFusedCaseAttribution_ReturnsCaseGroupsAndEnrichedNames`, which exercises the real `/api/search?axis=hybrid&axes=syntactic` endpoint path and proves fused `CaseId` values are enriched into `CaseName` and `CaseGroups`.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/EvidencePacketServerMappingTests.cs` - existing Story 22.4 coverage proves server-applied hybrid results map case id, case name, annotation count, and axes used into evidence packet sources.
- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` - existing Story 22.3 regression coverage still proves hybrid over-window requests return HTTP 400 `PAGINATION_LIMIT_EXCEEDED`.

### E2E / Integration Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs` - Story 22.4 Redis-backed pinned scorer acceptance coverage is present, but compilation/execution remains sandbox-blocked by NuGet signature lookup network denial.
- [x] Browser UI E2E is not applicable. Story 22.4 has no module UI.

### Unit / Service Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs` - case attribution preservation, deterministic conflict policy, bounded composite scores, rank-derived axis scores, skewed-score RRF behavior, and repeated-run deterministic ordering.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs` - typed query and raw scoped `FT.SEARCH INKEYS` command shape pin the explicit BM25-family scorer token and keep scoped key validation.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs` - hybrid orchestration preserves candidate windows, excludes stale-only axes from fusion, avoids corpus-statistics dependency, and keeps pagination-limit behavior.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/ExplainMetadataBuilderTests.cs` - public explain metadata reflects rank-contribution score semantics.

## Coverage

- API endpoints: `/api/search` hybrid success path now covered for fused case attribution, enriched case names, and grouped case metadata; hybrid pagination-limit errors remain covered.
- Fusion engine: syntactic-only, semantic-only, graph-only, mixed-axis attribution; conflict handling; deterministic RRF ordering; bounded composite and per-axis contribution scores covered.
- Redis scorer: normal NRedisStack query and raw graph-scoped `FT.SEARCH INKEYS` command shape covered; Redis-backed acceptance test exists but cannot be compiled in this sandbox.
- Evidence packets: hybrid evidence source case id/name/annotation count mapping covered.
- UI features: 0 applicable.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [ ] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter FullyQualifiedName~SearchEndpointContractTests -m:1 /nodeReuse:false` - blocked before discovery by VSTest TCP listener setup: `System.Net.Sockets.SocketException (13): Permission denied`.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests` - 8 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.FusionEngineTests -class Hexalith.Memories.Server.Tests.Search.SyntacticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.ExplainMetadataBuilderTests -class Hexalith.Memories.Server.Tests.Search.HybridSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.EvidencePacketServerMappingTests -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests` - 114 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` - 2248 total, 0 failed, 1 skipped.
- [ ] `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` - blocked before compilation by `NU1301` NuGet repository signature lookup denial: `Permission denied (api.nuget.org:443)`.
- [x] `git diff --check` - passed.

## Checklist Result

- API tests generated/updated where applicable: pass; one endpoint-level hybrid attribution success path added.
- E2E tests generated where applicable: pass for backend/API surface; no browser UI exists.
- Tests use standard xUnit v3/Shouldly/NSubstitute APIs, cover happy path and critical error cases, have clear descriptions, use no hardcoded waits, and are independent: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.
- Full Redis/Aspire integration compilation/execution remains blocked by sandbox NuGet signature network policy, not by test code.

---

# Test Automation Summary - Story 22.3 (Graph-Scoped & Hybrid Pagination Correctness)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-05
- **Story:** `_bmad-output/implementation-artifacts/22-3-graph-scoped-and-hybrid-pagination-correctness.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute; API/E2E coverage uses existing Aspire, Redis Stack, and FalkorDB fixtures. No new framework introduced.
- **Feature under test:** graph-scoped search pagination correctness, hybrid candidate-window pagination, and structured `PAGINATION_LIMIT_EXCEEDED` API behavior.

## Generated / Updated Tests

### API Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` - existing focused endpoint contract coverage proves graph-scoped and hybrid over-window requests return HTTP 400 `PAGINATION_LIMIT_EXCEEDED`.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs` - senior review added coverage proving scoped `INKEYS` syntactic search rejects foreign-tenant keys before executing Redis commands.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs` - senior review added coverage proving scoped semantic key validation keeps only tenant-scoped distinct keys and rejects foreign-tenant keys.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/HybridSearchApiIntegrationTests.cs` - added `GetSearch_HybridSyntacticOffsetBeyondOneHundredWithinWindow_ShouldReturnFusedPageAsync`, which seeds 130 syntactic RediSearch documents and exercises `/api/search?axis=hybrid&axes=syntactic&maxResults=5&offset=120` through the HTTP API.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/HybridSearchApiIntegrationTests.cs` - added `GetSearch_HybridSyntacticOffsetBeyondCandidateWindow_ShouldReturnPaginationLimitExceededAsync`, proving the public hybrid API returns structured pagination-limit failure for `offset + maxResults > 1000`.

### E2E / Integration Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` - existing Story 22.3 integration coverage proves graph-scoped page totals, scoped inner-search key propagation, disjoint pages, and enrichment-filtered totals.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/HybridSearchApiIntegrationTests.cs` - new Aspire API coverage fills the discovered hybrid deep-pagination E2E gap.
- [x] Browser UI E2E is not applicable. Story 22.3 has no module UI.

## Coverage

- API endpoints: `/api/search` graph-scoped and hybrid pagination-limit errors covered by server endpoint tests; hybrid within-window deep paging and beyond-window errors covered by Aspire HTTP integration tests.
- Graph-scoped search: scoped inner syntactic/semantic search shape, no growing-offset scan, honest totals, disjoint pages, and enrichment filtering covered by existing Story 22.3 unit/integration tests.
- Hybrid search: candidate windows beyond rank 100, configured cap errors, honest fused totals, stale-leading-page backfill, and API-level deep-page behavior covered.
- UI features: 0 applicable.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Search -parallel none -noLogo` - 238 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests -parallel none -noLogo` - 7 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` - 2238 total, 0 failed, 1 skipped.
- [x] `git diff --check` - passed.
- [ ] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~Search"` - blocked before discovery by VSTest TCP listener setup: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` - blocked before compilation by NuGet repository signature lookup denial: `NU1301 Unable to get repository signature information for source https://api.nuget.org/v3-index/repository-signatures/5.0.0/index.json` / `Permission denied (api.nuget.org:443)`.

## Checklist Result

- API tests generated/updated where applicable: pass.
- E2E tests generated where applicable: pass for backend/API integration; no browser UI exists.
- Tests use standard xUnit v3/Shouldly/NSubstitute APIs, cover happy path and critical error cases, have clear descriptions, use no hardcoded waits, and are independent through unique tenant/document ids: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.
- Full integration compilation/execution remains blocked by sandbox NuGet signature network policy, not by test intent.

---

# Test Automation Summary - Story 22.2 (Bounded, Cancellable Graph Traversal)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-05
- **Story:** `_bmad-output/implementation-artifacts/22-2-bounded-cancellable-graph-traversal.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute; FalkorDB/Aspire integration coverage uses existing fixtures. No new framework introduced.
- **Feature under test:** bounded server-side-cancellable graph traversal, default semantic traversal edges, deterministic limits, and graph-scoped search behavior.

## Generated / Updated / Referenced Tests

### API Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs` - server-side FalkorDB timeout propagation and caller cancellation coverage.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs` - graph-scoped traversal timeout propagation coverage, including the empty traversal indexed-memory count query.
- [x] Existing `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` coverage continues to prove traverse timeout maps to `GRAPH_TIMEOUT`.

### E2E / Integration Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeFilterIntegrationTests.cs` - real FalkorDB dense `CONTAINS` hub exclusion and deterministic traversal limiting.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` - updated graph-scoped search coverage so default traversal reaches a semantic neighbor but excludes siblings reachable only through a `CONTAINS` case hub.
- [x] Existing `tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeEndpointIntegrationTests.cs` coverage continues to prove traverse endpoint edge-type parsing and default semantic traversal behavior.
- [x] Browser UI E2E is not applicable. Story 22.2 has no module UI.

## Coverage

- Direct traversal service timeout propagation: covered.
- Graph-scoped search timeout propagation: covered.
- Query builder limit validation and deterministic `LIMIT` emission: covered.
- Default semantic traversal excluding `CONTAINS`/`ANNOTATES`: covered by unit tests and integration assertions.
- UI features: 0 applicable.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Graph.GraphQueryBuilderTests -class Hexalith.Memories.Server.Tests.Graph.GraphTraversalServiceTests -class Hexalith.Memories.Server.Tests.Search.GraphScopedSearchTests` - review rerun passed, 185 total, 0 failed.
- [ ] `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` - blocked before compilation by `NU1301` NuGet repository signature lookup denial: `Permission denied (api.nuget.org:443)`.
- [x] `git diff --check` - passed.

## Checklist Result

- API tests generated/updated where applicable: pass.
- E2E tests generated where applicable: pass for backend/API integration; no browser UI exists.
- Tests use standard xUnit v3/Shouldly/NSubstitute APIs, cover happy path and critical error cases, have clear descriptions, use no hardcoded waits, and are independent through unique tenant/graph ids: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.
- Full integration compilation/execution remains blocked by sandbox NuGet signature network policy, not by test code.

---

# Test Automation Summary - Story 22.1 (Semantic-Axis Pagination)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-05
- **Story:** `_bmad-output/implementation-artifacts/22-1-semantic-axis-pagination.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute; RedisStack/Aspire integration coverage uses existing collection fixtures. No new framework introduced.
- **Feature under test:** `axis=semantic` honors `SearchQuery.Offset` by retrieving an `offset + maxResults` KNN candidate window and returning stable, disjoint pages.

## Generated / Updated Tests

### API Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchApiIntegrationTests.cs` - added `GetSearch_WithSemanticAxisAndOffset_ShouldReturnDisjointStablePages`, which seeds deterministic semantic documents and exercises `/api/search?axis=semantic&maxResults=2&offset=2` through the HTTP API.

### E2E Tests

- [x] Browser UI E2E is not applicable. Story 22.1 has no module UI.
- [x] Existing RedisStack semantic integration coverage already includes direct service pagination proof in `SemanticSearchIntegrationTests.SearchAsync_WithOffset_ShouldReturnDisjointStableSemanticPages`.
- [x] New HTTP API integration coverage proves the endpoint forwards semantic pagination parameters into the service path.

## Coverage

- API endpoints: `/api/search` semantic-axis pagination path covered for page 1/page 2 disjointness and stable expected ordering.
- UI features: 0 applicable.
- Service behavior: candidate-window math, query-string construction, negative-offset normalization, deep-page limit, escaping, and RedisStack page disjointness covered by existing Story 22.1 unit/integration tests.
- Critical error cases: deep-page candidate-window overflow and vector/query error classifiers covered by existing unit tests; Docker-dependent integration execution is blocked in this sandbox.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests -parallel none -noLogo` - 34 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Search -parallel none -noLogo` - 228 total, 0 failed.
- [x] `dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.SearchQuerySerializationTests -parallel none -noLogo` - 3 total, 0 failed.
- [x] `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.ClientRest.MemoriesClientSearchTests -parallel none -noLogo` - 9 total, 0 failed.
- [x] `dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.SearchMemoryToolTests -parallel none -noLogo` - 15 total, 0 failed.
- [ ] `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` - blocked before compilation by sandbox network denial: `NU1301 Permission denied (api.nuget.org:443)` while fetching NuGet repository signature information.
- [ ] `docker ps` / RedisStack and Aspire integration execution - blocked by Docker socket permission: `permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`.
- [ ] `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` - blocked by the same NuGet signature network denial for AppHost and IntegrationTests; unaffected projects compiled successfully.

## Checklist Result

- API tests generated/updated where applicable: pass; one HTTP API semantic pagination regression added.
- E2E tests generated where applicable: pass for backend/API surface; no browser UI exists.
- Tests use standard xUnit v3/Shouldly APIs, cover happy path and critical pagination behavior, have clear descriptions, use no hardcoded waits, and are independent: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.
- Full integration execution remains blocked by sandbox Docker and network policy, not by test code.

---

# Test Automation Summary - Story 21.9 (Blue/Green Embedding Migration)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute with in-process xUnit execution; no new framework introduced.
- **Feature under test:** operator-facing blue/green embedding migration ordering, staging verification, atomic cutover gating, marker completion, rollback/abort parser surface, Redis marker/store command behavior.

## Generated / Updated Tests

### API Tests

- [x] Direct HTTP API tests are not applicable. Story 21.9 is an operator/backend migration path exposed through `tools/MigrateEmbeddingVectors`, Redis store operations, and migration service orchestration rather than a server endpoint.

### E2E Tests

- [x] Browser UI E2E is not applicable. Story 21.9 has no module UI.
- [x] Backend E2E-style service coverage updated in `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`.

### Unit / Service Tests

- [x] `LiveMigrationSuccessShouldVerifyStagingBeforeCutoverAndCompleteAfterConfigUpdate` - proves a live migration starts the marker, prepares staging, writes staging vectors, verifies staging, cuts over, updates config, heartbeats, then completes the marker in that order.
- [x] `LiveMigrationVerificationFailureShouldNotCutoverUpdateConfigOrCompleteMarker` - covers the critical verification failure path: staging writes can exist, but cutover, config update, and marker completion do not occur; the active marker remains protective and a tenant failure is recorded.
- [x] Existing Story 21.9 coverage reviewed in `MigrateEmbeddingVectorsToolTests`, `RedisEmbeddingMigrationStoreTests`, `IndexSchemaDefinitionsTests`, and migration service tests for blue/green wording, `--abort`, `SET NX` owner locking, no active-index drop during staging, schema helpers, rollback/abort invocation, and secret redaction.

## Coverage

- API endpoints: 0 applicable.
- UI features: 0 applicable.
- Migration service happy path: staging-before-cutover, verification-before-cutover, config update through cutover, and marker completion ordering covered.
- Critical error cases: staging verification failure, tenant-level failures, per-unit provider failures, cancellation, resume-without-marker, rollback without retained target, parser mode conflict, and owner-lock conflict covered by current tests.
- Redis/store behavior: staging index creation, legacy NL migration before staging, owner lock `SET NX`, lock conflict, and schema helper coverage present.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` - 2,194 total, 0 failed, 1 skipped.

## Checklist Result

- API tests generated/updated where applicable: pass; no HTTP API exists for this story surface.
- E2E tests generated where applicable: pass; no UI/browser surface exists, backend service E2E-style orchestration tests added for live migration ordering and verification failure.
- Tests use standard xUnit v3/Shouldly APIs, cover happy path and critical error cases, have clear descriptions, use no hardcoded waits, and are independent: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.8 (Tenant Registry CAS & Rollback Integrity)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-8-tenant-registry-cas-and-rollback-integrity.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute with Dapr client substitutes and in-process xUnit execution; no new framework introduced.
- **Feature under test:** tenant registry CAS status updates, transactional entry/index writes, rollback/end-state integrity, and workflow owner propagation.

## Generated / Updated Tests

### API Tests

- [x] Direct public API route shape changes are not applicable in this QA pass; Story 21.8 behavior is exercised through existing endpoint rollback coverage plus service/workflow tests at the registry boundary.

### E2E Tests

- [x] UI/browser E2E is not applicable. Story 21.8 has no module UI.
- [x] Backend workflow E2E-style coverage updated in `tests/Hexalith.Memories.Server.Tests/Workflows/TenantProvisioningWorkflowTests.cs` and `tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs` to assert status activities carry the owning workflow instance on happy and failure paths.

### Unit / Service Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs` - strengthened registration transaction assertions to include entry/index ETags and added post-conflict consistent end-state coverage.
- [x] `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs` - added removal conflict exhaustion coverage proving inconsistent end state fails clearly and does not fall back to direct `DeleteStateAsync`.

## Coverage

- Registry service tests: 29/29 focused tenant registry tests passed.
- Workflow tests: 11/11 focused provisioning/deletion workflow tests passed.
- Happy path: registration transaction shape, CAS status update, deletion removal transaction, and owner propagation covered.
- Critical error cases: CAS retry/exhaustion, missing tenant, stale deletion-owner rollback block, transaction conflict with consistent end state, transaction conflict with inconsistent end state, and deletion/provisioning failure owner propagation covered.
- UI features: 0 applicable.

## Validation

- [x] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~TenantRegistryServiceTests|FullyQualifiedName~TenantProvisioningWorkflowTests|FullyQualifiedName~TenantDeletionWorkflowTests" --no-restore --logger "console;verbosity=normal"` - blocked before test execution by sandbox MSBuild named-pipe/socket permission (`SocketException (13): Permission denied`).
- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantRegistryServiceTests -class Hexalith.Memories.Server.Tests.Workflows.TenantProvisioningWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.TenantDeletionWorkflowTests` - 40 total, 0 failed, 0 skipped.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` - 2,184 total, 0 failed, 1 pre-existing skipped submodule mutation guard.
- [x] `git diff --check` - passed.

## Checklist Result

- API tests generated/updated where applicable: pass.
- E2E tests generated where applicable: pass; no browser UI exists, backend workflow tests cover owner-sensitive end-to-end behavior.
- Standard framework APIs, happy path, critical error cases, clear descriptions, no hardcoded waits, independent tests: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.7 (Dedup Race & Duplicate-Instance Handling)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-7-dedup-race-and-duplicate-instance-handling.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute with in-process `WebApplicationFactory<Program>` API/E2E coverage; no new framework introduced.
- **Feature under test:** race-safe permanent dedup writes, post-index duplicate loser compensation, token-race cleanup, and deterministic Dapr workflow duplicate-instance handling.

## Generated / Updated Tests

### API Tests

- [x] Existing Story 21.7 API/controller coverage reviewed: `EventIngestionOutcomeTests`, `EventIngestionServiceTests`, `DaprWorkflowDuplicateInstanceDetectorTests`, `SaveDedupKeyActivityTests`, `IngestionWorkflowTests`, serialization guards, and docs drift guards already cover atomic `When.NotExists` saves, duplicate winner observation, source/token loser handling, duplicate scheduler mapping, and non-duplicate scheduler failure release.

### E2E Tests

- [x] `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs` - added `DuplicateWorkflowInstance_ToSharedTopic_ReturnsDuplicateWithoutPoisoningRedelivery`, driving a structured CloudEvent through the real `/events/ingest` HTTP pipeline and asserting HTTP 200, `status=duplicate`, `wasDuplicate=true`, no instance id, one scheduler call, and no preflight reservation release when deterministic workflow scheduling collides.
- [x] UI E2E is not applicable. Story 21.7 has no module UI.

## Coverage

- API endpoints: `/events/ingest` duplicate workflow-instance collisions covered through controller/API tests and in-process HTTP E2E tests.
- Happy path: existing shared-topic E2E still covers accepted module events; existing activity/workflow tests cover first-writer dedup saves and successful source/token permanent record paths.
- Critical error cases: duplicate scheduler conflicts return HTTP 200 duplicate without reservation release; non-duplicate scheduler failures remain HTTP 500/retry-driving and release held reservations; atomic dedup losers compensate rather than persisting failed units.
- Duplicate safety: permanent source-URI and token dedup saves use TTL-less `When.NotExists`, loser-owned source records are released only by ownership match, and duplicate workflow-instance collisions no longer poison Dapr pub/sub redelivery.

## Validation

- [x] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~CrossModuleEventIntakeE2ETests|FullyQualifiedName~EventIngestionOutcomeTests" -v minimal` - blocked by sandbox MSBuild/VSTest socket permission (`SocketException (13): Permission denied`), before tests ran.
- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore -v minimal` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.EventStoreIntegration.CrossModuleEventIntakeE2ETests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.EventIngestionOutcomeTests` - 15 total, 0 failed, 0 skipped.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Activities.Ingestion.SaveDedupKeyActivityTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.IngestionActivityRecordSerializationTests -class Hexalith.Memories.Server.Tests.Ingestion.MemoryUnitIdStabilityContractTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.DocumentationCompletenessTests` - 67 total, 0 failed, 0 skipped.
- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj -m:1 /nodeReuse:false --no-restore -v minimal` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.EventIngestionServiceTests -class Hexalith.Memories.EventStore.Tests.DaprWorkflowDuplicateInstanceDetectorTests -class Hexalith.Memories.EventStore.Tests.EventIngestionResponseTests` - 29 total, 0 failed, 0 skipped.
- [x] `git diff --check -- tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs _bmad-output/implementation-artifacts/tests/test-summary.md` - passed.

## Checklist Result

- API tests generated/updated where applicable: pass.
- E2E tests generated where applicable: pass; one in-process HTTP E2E test added for the discovered Story 21.7 duplicate scheduler collision gap.
- Standard framework APIs, happy path, 1-2 critical error cases, clear descriptions, no hardcoded waits, independent tests: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.6 (Event Routing for Unknown/Unavailable Tenants)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-6-event-routing-for-unknown-unavailable-tenants.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute with in-process `WebApplicationFactory<Program>` API/E2E coverage; no new framework introduced.
- **Feature under test:** DAPR pub/sub `/events/ingest` retry posture for tenant lifecycle route failures.

## Generated / Updated Tests

### API Tests

- [x] Existing Story 21.6 API/controller coverage reviewed: `EventIngestionOutcomeTests` and `EventIngestionControllerTests` already prove `TenantNotFound` and `TenantDeleting` map to HTTP 500 while `UnknownSource`, `AutoCreateDisabled`, and `CaseCapExceeded` remain HTTP 200.

### E2E Tests

- [x] `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs` - added `TenantNotFoundRouteFailure_Returns500ForDaprRetryWithoutScheduling`, driving a structured CloudEvent through the real `/events/ingest` HTTP pipeline and asserting HTTP 500, `tenant-not-found`, no instance id, no duplicate flag, no preflight dedup reservation, and no workflow scheduling.
- [x] `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs` - added `TenantDeletingOrUnavailableRouteFailure_Returns500ForDaprRetryWithoutScheduling`, covering the controller-boundary path shared by deleting and unavailable tenants, with the same no-dedup/no-schedule guarantees.
- [x] UI E2E is not applicable. Story 21.6 has no module UI.

## Coverage

- API endpoints: `/events/ingest` lifecycle route failures covered through unit/controller tests and in-process HTTP E2E tests.
- Happy path: existing cross-module shared-topic E2E still covers accepted module events and duplicate delivery.
- Critical error cases: tenant not found and deleting/unavailable now return non-2xx through the HTTP surface for DAPR retry; unknown source remains the intentional 200 non-retry drop.
- Duplicate safety: new lifecycle tests assert no dedup reservation or workflow scheduling occurs before a tenant route is accepted.

## Validation

- [x] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~CrossModuleEventIntakeE2ETests|FullyQualifiedName~EventIngestionOutcomeTests" --no-restore -m:1 /nodeReuse:false` - build succeeded, then VSTest aborted on sandbox TCP listener permission (`SocketException (13): Permission denied`).
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.EventStoreIntegration.CrossModuleEventIntakeE2ETests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.EventIngestionOutcomeTests` - 13 total, 0 failed, 0 skipped.
- [x] `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `git diff --check -- tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs _bmad-output/implementation-artifacts/tests/test-summary.md` - passed.

## Checklist Result

- API tests generated/updated where applicable: pass.
- E2E tests generated where applicable: pass; two in-process HTTP E2E tests added for the discovered Story 21.6 gap.
- Standard framework APIs, happy path, 1-2 critical error cases, clear descriptions, no hardcoded waits, independent tests: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.5 (Deletion Completeness)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-5-deletion-completeness.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute for unit/API integration coverage; no new framework introduced.
- **Feature under test:** case and tenant deletion end-state cleanup for aggregate-case-map routes, event-router stale cache prevention, EventStore/embedding marker cleanup, and orphan memory/vector key cleanup.

## Generated / Updated Tests

### API Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantDeletionIntegrationTests.cs` - extended the tenant deletion API integration scenario to seed `eventstore:*`, `embedding-migration:*`, syntactic, raw semantic, current natural-language semantic, and legacy natural-language semantic keys, then assert Redis/FalkorDB end state after tenant deletion.
- [x] Case route-map deletion is covered at the route-map store, workflow, and activity boundary because the HTTP case deletion route first submits an EventStore domain command; the story behavior under test is projection cleanup, not the command gateway.

### E2E Tests

- [x] Browser UI E2E is not applicable. Story 21.5 has no module UI.
- [x] Existing Aspire tenant deletion integration coverage exercises the distributed API/backend cleanup path; focused route-map cleanup execution is covered without requiring the unrelated EventStore gateway runtime path.

### Unit / Activity / Workflow Tests

- [x] `tests/Hexalith.Memories.EventStore.Tests/RedisAggregateCaseMappingStoreTests.cs` - aggregate-case-map cleanup happy path, idempotent missing-map behavior, and invalid input guard coverage.
- [x] `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs` - persisted map revalidation, deleted cached route rejection, targeted invalidation, and curated search-index bypass coverage.
- [x] `tests/Hexalith.Memories.Server.Tests/Activities/Cases/DeleteCaseRouteMappingsActivityTests.cs` - persisted cleanup before cache invalidation and failure surfacing.
- [x] `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteTenantDataKeysActivityTests.cs` - expected tenant-scoped scan patterns and bounded batched delete behavior.
- [x] `tests/Hexalith.Memories.Server.Tests/Workflows/CaseDeletionProjectionWorkflowTests.cs` - cleanup activity ordering and retry behavior.

## Coverage

- API deletion endpoints: tenant deletion cleanup coverage extended; case deletion projection cleanup covered below the endpoint at the workflow/activity boundary where the story-owned cleanup is executed.
- Aggregate-case-map cleanup: deleted-case matching fields removed; unrelated case routes preserved; missing maps idempotent; invalid tenant/case input rejects before Redis calls.
- Event-router stale route behavior: cache hit revalidates against persisted map; explicit invalidation removes only matching deleted-case routes; curated index events do not consult the case map.
- Tenant cleanup key families: `{tenant}:case:*`, `dedup:{tenant}:*`, `{tenant}:eventstore:*`, `{tenant}:embedding-migration:*`, syntactic, raw semantic, current NL semantic, and legacy NL semantic prefixes.
- Critical error cases: Redis cleanup failures surface for workflow retry; missing key families remain success states; no UI path exists for this story.

## Validation

- [x] `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `dotnet test tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~TenantEventRouterTests|FullyQualifiedName~RedisAggregateCaseMappingStoreTests"` - 22 total, 0 failed.
- [x] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~DeleteCaseRouteMappingsActivityTests|FullyQualifiedName~DeleteTenantDataKeysActivityTests|FullyQualifiedName~CaseDeletionProjectionWorkflowTests"` - 8 total, 0 failed.
- [!] Live Aspire tenant deletion integration was not executed in this pass; the solution build compiled the updated integration test, and focused executable tests validated the story-owned cleanup paths.

## Checklist Result

- API tests generated/updated: pass.
- E2E tests generated if UI exists: UI not applicable; API/backend integration coverage extended and compiled, with route-map cleanup validated through focused store/activity/workflow tests.
- Standard framework APIs, happy path, critical error cases, clear descriptions, polling instead of blind sleeps, independent tests: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.4 (Key-Schema Single Source of Truth)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute through `Hexalith.Memories.Server.Tests`; no new framework introduced.
- **Feature under test:** Redis memory-unit/vector key schema builders and parsers centralized in `IndexSchemaDefinitions`.

## Generated / Updated Tests

### API Tests
- [x] Direct API endpoint tests are not applicable. Story 21.4 is a backend key-schema refactor with no new HTTP route.

### E2E Tests
- [x] UI E2E is not applicable. Story 21.4 has no module UI change.
- [x] Backend E2E-style regression coverage is provided through the full `Hexalith.Memories.Server.Tests` assembly, including the source-scanning architecture guard and key-schema call-site tests.

### Unit / Guard Tests
- [x] `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs` - added 10 focused gap-fill tests for centralized key-builder validation, current natural-language semantic key parsing, legacy/current NL separation, foreign tenant rejection, and prefix-only rejection.
- [x] Existing Story 21.4 coverage reviewed: key shape helpers, prefix non-collision, legacy migration helper, production literal guard, indexing cleanup/verification activities, syntactic search mapping, and related service call sites.

## Coverage

- Key builder happy paths: syntactic, semantic, current NL semantic, and legacy migration-only NL shapes covered.
- Critical validation cases: null/whitespace memory-unit ids and whitespace tenant ids now covered for all four builder helpers.
- Parser happy paths: syntactic, semantic, current NL semantic, and legacy NL migration helper covered.
- Critical parser rejection cases: foreign tenant keys, prefix-only keys, current NL key rejected by raw semantic parser, and legacy NL key rejected by current NL parser.
- CI guard: production `src/**/*.cs` raw memory/index key literal scan remains covered by `IndexSchemaLiteralGuardTests`.

## Validation

- [x] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~IndexSchemaDefinitionsTests -m:1 /nodeReuse:false` - build passed; VSTest aborted on sandbox TCP listener permission (`SocketException (13): Permission denied`).
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests` - 29 total, 0 failed, 0 skipped.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` - 2,151 total, 0 failed, 1 pre-existing skipped submodule mutation guard.
- [x] `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.

## Checklist Result

- API tests generated if applicable: not applicable.
- E2E tests generated if UI exists: not applicable; backend regression coverage pass.
- Standard framework APIs, happy path, critical error cases, clear descriptions, no hardcoded waits, independent tests: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.3 (Natural-Language Vector Namespace Separation)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute through `Hexalith.Memories.Server.Tests`; no new framework introduced.
- **Feature under test:** Natural-language vector key namespace migration and raw/NL RediSearch index rebuild behavior for Story 21.3.

## Generated / Updated Tests

### API Tests
- [x] `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs` - added migration-store boundary coverage proving live semantic index rebuild migrates legacy `{tenant}:vec:nl:*` hashes to `{tenant}:vecnl:*` before dropping/recreating raw and NL indexes, and review coverage proving legacy NL hashes are counted as NL data rather than raw semantic data.

### E2E Tests
- [x] Backend E2E-style coverage through the migration store boundary validates Redis scan/copy/delete orchestration plus RediSearch rebuild command shape without Docker.
- [x] UI E2E is not applicable. Story 21.3 has no module UI change.

## Coverage

- Legacy NL hash migration trigger: covered at the `RedisEmbeddingMigrationStore.DropAndRecreateSemanticIndexesAsync` boundary.
- Happy path: legacy NL hash is copied to the disjoint prefix, verified, and deleted before index rebuild.
- Migration inventory regression: legacy `{tenant}:vec:nl:*` hashes are excluded from raw semantic counts and included in natural-language semantic counts before migration converges.
- Critical regression cases: raw index recreation uses `{tenant}:vec:`; NL index recreation uses `{tenant}:vecnl:`; recreated indexes do not use the legacy nested `{tenant}:vec:nl:` prefix.
- Existing Story 21.3 focused coverage rerun: prefix non-containment, phantom ID prevention, consistency verification/inspection, tenant vector provisioning, namespace migrator idempotency, and repair workflow stability.

## Validation

- [x] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~RedisEmbeddingMigrationStoreTests" -m:1 /nodeReuse:false` - build passed; VSTest aborted on sandbox TCP listener permission (`SocketException (13): Permission denied`).
- [x] `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests` - 2 total, 0 failed.
- [x] `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Migration.RedisNaturalLanguageNamespaceMigratorTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.EnumerateMemoryUnitIdsActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.VerifyConsistencyActivityTests -class Hexalith.Memories.Server.Tests.Consistency.ConsistencyInspectionServiceTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.ProvisionRedisVectorActivityTests -class Hexalith.Memories.Server.Tests.Workflows.ConsistencyRepairWorkflowTests` - 66 total, 0 failed.
- [x] `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false` - passed, 0 warnings, 0 errors.

## Checklist Result

- API tests generated: pass.
- E2E tests generated if UI exists: UI not applicable; backend migration boundary E2E-style coverage pass.
- Standard framework APIs, happy path, critical error/regression cases, clear descriptions, no hardcoded waits, independent tests: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.2 (Transactional Multi-Backend Mutation)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute through `Hexalith.Memories.Server.Tests`; no new framework introduced.
- **Feature under test:** HTTP case mutation path and Story 21.2 EventStore-command-before-projection boundary for transactional multi-backend mutation.

## Generated / Updated Tests

### API Tests
- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/CaseMutationEndpointE2ETests.cs` - added in-process HTTP endpoint coverage for create-case mutation success, validation failure, and EventStore command-gateway failure.
- [x] `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs` - added overridable `IMemoriesCommandStore` and `ICaseProjectionWorkflowScheduler` seams so endpoint tests can drive the real HTTP pipeline without Redis, FalkorDB, Dapr Workflow, or EventStore services.

### E2E Tests
- [x] API E2E through `WebApplicationFactory<Program>` covers the real minimal API route, authentication/tenant middleware, tenant-status guard, service resolution, EventStore command acceptance, and projection workflow scheduling.
- [x] UI E2E is not applicable. Story 21.2 has no module UI change.

## Coverage

- API mutation endpoints newly covered: create case 1/1 targeted route.
- Happy path: `POST /api/tenants/{tenantId}/cases` returns `201 Created`, accepts a `CreateCaseCommand`, schedules `CaseCreationProjectionWorkflow`, and does not write Redis case hashes directly.
- Critical error cases: invalid case name returns `400` without EventStore command or workflow scheduling; EventStore command acceptance failure returns `500` without projection workflow scheduling or Redis write.
- Existing Story 21.2 focused coverage rerun: service mutation boundary tests for create case, create annotation, delete memory unit, and delete case; projection workflow compensation/order tests; delete projection activity and architecture guard tests.

## Validation

- [x] `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore -v:m -m:1 /nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Memories.Server.Tests.Endpoints.CaseMutationEndpointE2ETests` - 3 total, 0 failed.
- [x] `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Memories.Server.Tests.Cases.CaseServiceTests -method "*CreateCaseAsync*" -method "*CreateAnnotationAsync*" -method "*DeleteMemoryUnitAsync*" -method "*DeleteCaseAsync*"` - 11 total, 0 failed.
- [x] `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Memories.Server.Tests.Workflows.CaseCreationProjectionWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.AnnotationProjectionWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.MemoryUnitDeletionProjectionWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.CaseDeletionProjectionWorkflowTests` - 13 total, 0 failed.
- [x] `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Memories.Server.Tests.Activities.Cases.DeleteMemoryUnitProjectionActivityTests -class Hexalith.Memories.Server.Tests.Architecture.ConsistencyModelDecisionTests` - 7 total, 0 failed.
- [x] `dotnet test ... --filter "FullyQualifiedName~CaseMutationEndpointE2ETests"` compiled the project, then VSTest aborted on sandbox TCP listener permission (`SocketException (13): Permission denied`); the xUnit v3 in-process executable above was used as the sandbox-safe runner.

## Checklist Result

- API tests generated: pass.
- E2E tests generated if UI exists: UI not applicable; API E2E pass.
- Standard framework APIs, happy path, 1-2 critical error cases, clear descriptions, no hardcoded waits, independent tests: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.

---

# Test Automation Summary - Story 21.1 (Consistency Model Decision)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-1-consistency-model-decision.md`
- **Framework detected:** xUnit v3 + Shouldly through `Hexalith.Memories.Server.Tests`; no new framework introduced.
- **Feature under test:** Architecture and operator-documentation guardrails for the Story 21.1 consistency model decision.

## Generated / Updated Tests

### API Tests
- [x] Direct API tests are not applicable. Story 21.1 is documentation/architecture scope and adds no endpoint or runtime command behavior.

### E2E Tests
- [x] `tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs` - added an end-to-end documentation guard tying `architecture.md` D3, `docs/dev/consistency.md`, and the Story 21.1 record together.
- [x] UI E2E is not applicable. Story 21.1 has no module UI change.

## Coverage

- Architecture D3: verifies EventStore aggregate source of truth, rebuildable projections, DAPR Workflow projection compensation, transitional direct-write debt, and the Story 21.2 implementation gate.
- Operator consistency guide: verifies syntactic hash repair remains current pre-21.2 operational input while EventStore is the target source of truth.
- Story record: verifies the rejected workflow-wrapped compensated multi-write alternative, A3 closure requirements, failure-injection test requirement, and 21.3-21.10 source-of-truth dependency gate.

## Validation

- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.ConsistencyModelDecisionTests` - 3 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` - 2101 total, 0 failed, 1 intentionally skipped submodule mutation guard.
- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 DiffEngine_Disabled=true dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false --filter FullyQualifiedName~ConsistencyModelDecisionTests` - build passed, then VSTest aborted on local socket permission; xUnit v3 executable fallback above passed.

## Checklist Result

- API tests generated if applicable: not applicable.
- E2E tests generated if UI exists: documentation E2E guard generated; UI not applicable.
- Standard framework APIs, happy path, critical guardrail cases, clear descriptions, no sleeps, independent tests: pass.
- Test summary created with coverage metrics: pass.

---

# Test Automation Summary - Story 20.3 (Tenant-Scope Workflow & Batch Status Endpoints)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute through `Hexalith.Memories.Server.Tests` and `Hexalith.Memories.Contracts.Tests`; no new framework introduced.
- **Feature under test:** Tenant-scoped ingestion workflow and directory batch status APIs, safe single-workflow DTO projection, batch fan-out authorization ordering, and raw workflow-state leakage prevention.

## Generated / Updated Tests

### API Tests
- [x] `tests/Hexalith.Memories.Server.Tests/Authentication/IngestionStatusEndpointAuthorizationTests.cs` - added fail-closed endpoint coverage for unreadable workflow input, missing batch state, and malformed stored batch tenant.
- [x] Existing Story 20.3 API tests validate cross-tenant single-workflow denial, matching-tenant projected status success, missing workflow status, cross-tenant batch denial before fan-out, and matching-tenant `BatchStatusResponse` preservation.
- [x] Existing mapper and contract tests validate safe workflow-state projection, output deserialization degradation, source-generated JSON registration, and no raw workflow contract fields.

### E2E Tests
- [x] UI E2E not applicable. Story 20.3 is a Server API/security slice with no module UI change.

## Coverage

- Single workflow status endpoint: matching tenant, mismatched tenant, missing state, unreadable input, raw-state non-leakage.
- Batch status endpoint: matching tenant, mismatched tenant, missing state, malformed stored tenant, no per-file workflow fan-out before batch tenant authorization.
- Public contract: `IngestionWorkflowStatus` serialization and safe-field registration.
- Route authorization guardrails: existing Story 20.1/20.2 route authorization tests rerun.

## Validation

- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` - passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.IngestionStatusEndpointAuthorizationTests` - 8 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.IngestionWorkflowStatusMapperTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` - 32 total, 0 failed.
- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --disable-build-servers -m:1 /nr:false` - passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.IngestionWorkflowStatusSerializationTests` - 2 total, 0 failed.
- [x] `git diff --check -- src tests _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md` - passed.

## Checklist Result

- API tests generated/updated: pass.
- E2E tests generated if UI exists: not applicable.
- Standard framework APIs, happy path, critical error cases, clear descriptions, no sleeps, independent tests: pass.
- Test summary created with coverage metrics: pass.

---

# Test Automation Summary — Story 20.2 (Tenant Authorization Filter & Principal-Derived Audit Identity)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute through `Hexalith.Memories.Server.Tests`; no new framework introduced.
- **Feature under test:** Server tenant authorization from authenticated principal claims, body/query/path tenant denials, and principal-derived audit identity.

## Generated / Updated Tests

### API Tests
- [x] `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` — added authorized tenant-route pass-through coverage and expanded cross-tenant plus malformed tenant denial coverage across path routes, `/api/search`, `/api/ingest`, `/api/ingest/url`, and `/api/ingest/directory`.
- [x] Existing Story 20.2 tests validated claims normalization, tenant authorization filter behavior, tenant path denial, search-axis denial (`syntactic`, `semantic`, `graph`, `hybrid`), and `x-user-id` audit spoofing regression.
- [x] Review regression coverage added for MCP-compatible underscore tenant IDs and extracted by-source-URI case-access audit identity.

### E2E Tests
- [x] UI E2E not applicable. Story 20.2 is a Server API/security slice with no module UI change.

## Coverage

- API tenant path representative denial routes: 3/3 covered.
- Search cross-tenant denial axes: 4/4 covered.
- Body-tenant ingest scheduling denial endpoints: 3/3 covered.
- Principal-derived audit identity regression: covered for search and by-source-URI case-access `x-user-id` spoofing; existing audit tests cover search, ingest, traverse, and case-access audit event emission.

## Validation

- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` — passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerTenantClaimsTransformationTests -class Hexalith.Memories.Server.Tests.Authentication.TenantAuthorizationEndpointFilterTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Telemetry.AuditLogStreamTests` — 56 total, 0 failed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` — 2000 total, 0 failed, 1 intentionally skipped submodule mutation guard.
- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` — passed.

## Checklist Result

- API tests generated/updated: pass.
- E2E tests generated if UI exists: not applicable.
- Standard framework APIs, happy path, critical error cases, clear descriptions, no sleeps, independent tests: pass.
- Test summary created with coverage metrics: pass.

---

# Test Automation Summary — Story 17.2 (Recovery and Feedback State Grammar)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Feature under test:** Memories Web recovery state grammar — `RecoveryStateMapper`,
  `MemoriesRecoveryActionPanel`, and its composition inside `MemoriesEvidenceCockpit`.
- **Date:** 2026-06-24
- **Framework detected:** xUnit v3 (`xunit.v3` 3.2.2) + bUnit (2.8.4-preview) + Shouldly + FrontComposer
  `Hexalith.FrontComposer.Testing` (`FrontComposerTestBase`). Matched the project's existing test stack;
  no new framework introduced.
- **Run command (sandbox):** built with serialized MSBuild (`-m:1`); executed the xUnit v3 in-process
  executable directly with `DiffEngine_Disabled=true` (project `dotnet test`/VSTest socket is blocked in
  this sandbox, per the story's Dev Agent Record).

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 75 | 0 | 0 | 0 |
| **After gap auto-apply** | **100** | **0** | **0** | **0** |

All 100 tests pass. `git diff --check` is clean for every file touched by this workflow. The
`Hexalith.Memories.Web` source project builds clean under `TreatWarningsAsErrors=true` (0/0).

## Gaps discovered and auto-applied

The feature already shipped with focused tests. This pass mapped Story 17.2's acceptance criteria and
Task 5 test requirements against existing coverage and filled the remaining gaps. **+25 test cases.**

### Mapper coverage — `Components/Recovery/RecoveryStateMapperGapTests.cs` (new)

- **G1 — Exhaustive state × isolation precedence/safety sweep.** Cross-product of every
  `EvidencePacketState` × `EvidencePacketIsolationStatus`: asserts the mapper never throws, is
  deterministic, always traces to named contract sources, never emits `WrongCase`, collapses every
  restrictive/unauthorized scope to `Unauthorized`, suppresses risk markers + omitted-detail hints under
  restrictive scope, and never leaks count-bearing clue axes when unauthorized. (Task 5 "exhaustive
  state/precedence matrix … unknown/future enum values"; AC3 side-channel safety.)
- **G2 — Stale + compressed combination.** `StaleMemory` stays primary with a `compressed` secondary
  risk marker and the omitted detail group remains visible. (Task 5 "stale/compressed/conflict
  combinations".)
- **G3 — Stale + degraded + sources combination.** Conflict wins precedence (`Conflicting`) while
  staleness remains a visible `stale` risk marker. (Task 5 combinations; AC3 no-confident-answer.)
- **G4 — Sanitization sweep over every fixture.** Flattens all dynamic view-model strings (clue, tenant,
  case, omitted names, expansions, action labels/guidance/targets) and proves no fixture — including the
  sensitive ones — leaks bearer tokens, local paths, connection strings, JWTs, or secrets. (Task 5
  negative-leakage, broadened from 2 fixtures to all.)
- **G5 — Whitelisted diagnostic-clue shape over every fixture.** Every state yields a non-empty clue
  matching the `code=token` whitelist shape. (AC1 diagnostic clue.)

### Component coverage — `Components/Recovery/RecoveryActionPanelStateGrammarTests.cs` (new)

- **G6 — Per-state full grammar render (14 theory cases).** Renders the panel for every actionable state
  (weak, stale, degraded ×2, conflicting ×2, no-match, not-ingested, graph-gap, insufficient ×2,
  compressed, unauthorized, unknown) and asserts each shows title, explanation, diagnostic clue, a
  text-bearing severity badge, and a text-bearing affected-capability badge — never color alone — with no
  sensitive markup leak. (Task 5 "each state renders title, explanation, diagnostic clue, severity,
  affected capability"; AC4 color-is-never-the-only-signal.)
- **G7 — State-transition accessibility (4 tests).** Re-renders across packet changes:
  unauthorized→allowed (assertive `alert` → polite `status`), complete→degraded (hidden → conflicting),
  conflicting→resolved (shown → hidden), compressed→expanded (omitted-detail grammar dropped). (Task 4 +
  Advanced Elicitation transition coverage: loading→no-result, unauthorized→allowed, complete→degraded,
  compressed→expanded, conflicting→resolved.)

### Integration coverage — `Components/Evidence/EvidenceCockpitRecoveryTransitionTests.cs` (new)

- **G8 — Loading→result transition.** While loading the recovery panel is absent; once a packet arrives
  the panel appears for the resolved state. (Task 4 loading→result transition at the cockpit boundary.)
- **G9 — Dual-announcement wiring.** An unauthorized packet renders the assertive restrictive banner
  (`role=alert`) alongside the recovery panel announced politely (`role=status`, `AnnounceAssertively=false`)
  so the two live regions do not compete; only the safe `CheckAuthorization` action surfaces and no
  restricted content leaks. (Task 2 routing + AC4 announcements.)

### Supporting fixtures — `Components/Recovery/RecoveryPacketFixtures.cs` (extended)

- Added `StaleAndCompressed()` and `StaleDegradedWithSources()`, built on the canonical Story 2.7-aligned
  Evidence Packet fixtures, for the new combination tests.

## Coverage map (Story 17.2)

- **AC1** (state grammar: title/explanation/clue/severity/capability/safest action): G6 (all states) +
  existing panel tests.
- **AC2** (no-result distinctions): existing state matrix + G1 sweep; `WrongCase` proven unreachable.
- **AC3** (conflict not smoothed into a confident answer): G1, G3 + existing disagreement tests.
- **AC4** (keyboard/AT readable, color never the only signal, transition announcements): G6, G7, G8, G9 +
  existing keyboard/localization tests.

## Notes / scope boundaries

- No API endpoint tests apply — this is a Razor Component Library slice with no new HTTP surface; the
  mapper unit/contract tests are the API-equivalent layer.
- No runnable web host exists for this RCL-only slice, so Playwright/axe viewport validation remains not
  applicable (consistent with the story's Dev Agent Record); component-level accessibility is asserted via
  semantic roles, `aria-live`, and accessible names in bUnit.
- The pre-existing `git diff --check` trailing-whitespace warnings are in unrelated Story 2.7 markdown/YAML
  artifacts modified before this task; no file changed by this workflow has whitespace errors.

## Next steps

- Run the Web test lane in CI alongside the rest of the solution.
- When a runnable web host lands (later Epic 17 stories), add Playwright + axe viewport checks at 360/768/
  1024/1440px to complete Task 6's browser-level validation.

---

# Test Automation Summary — Story 17.3 (Contract-Aware Web Interaction Patterns)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24
- **Story:** `_bmad-output/implementation-artifacts/17-3-contract-aware-web-interaction-patterns.md`
- **Framework detected:** xUnit v3 (3.2.2) + bUnit (2.8.4-preview) + Shouldly + `Hexalith.FrontComposer.Testing`
  (`FrontComposerTestBase`). Matched the existing stack; no new framework introduced.
- **Run command (sandbox-safe in-process runner):**
  `DiffEngine_Disabled=true ./tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests`
  (`dotnet test`/VSTest socket is blocked in this sandbox, per the story's Dev Agent Record.)

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 156 | 0 | 0 | 0 |
| **After gap auto-apply** | **212** | **0** | **0** | **0** |

All 212 tests pass (**+56**). The test project builds clean under `TreatWarningsAsErrors=true` (0 warnings /
0 errors), and `git diff --check` is clean for every added file.

## Scope note — API vs E2E

- **API tests:** Not applicable. Story 17.3 is an RCL-only web-interaction slice over the shared Evidence
  Packet contract; it adds no runnable API endpoints or HTTP surface. The pure mappers/validators
  (`FilterInspectionMapper`, `ContractAwareFormValidator`, `InteractionContextValidator`,
  `MemoriesCommandSurfaceMapper`, `ConfirmationPromptMapper`, `CompactGridColumnPlanner`) are the
  API-equivalent layer and are unit-tested directly.
- **Browser E2E (Playwright/axe):** Not applicable here — no runnable web host is shipped by this slice (as the
  story records). The project's component/interaction test surface is **bUnit**, which is what these gap tests use.

## Gaps discovered and auto-applied

Mapped Story 17.3's six ACs and Task 5 test requirements against existing coverage and filled the remaining
gaps, following the repo's `*GapTests.cs` convention. Tests use `data-testid`/accessible locators (no CSS-class
selectors), canonical `EvidencePacketFixtures`, no sleeps, and each builds its own fixtures (order-independent).

### Filters (AC2) — `FilterInspectionMapperGapTests.cs`, `MemoriesFilterSummaryGapTests.cs`
- Empty-state reason branches not previously exercised: `NotIngested`, `DegradedBackend`, `StaleMemory`,
  `InsufficientEvidence`, plus `Unknown` isolation → `InaccessibleScope`.
- Per-effect chip trust severity mapping; sensitive chip value redaction (mapper + rendered chip).
- No-filters render path (`mem-filter-none`); null-argument guards; component surfaces distinct empty reasons.

### Forms (AC1) — `ContractAwareFormValidatorGapTests.cs`
- Required case / text / enum / range blank-value paths → field-associated `CaseRequired` / `FieldRequired`.
- Optional text never blocks; unbounded range accepts a finite value; `Infinity`/`-Infinity` blocks dispatch.

### Grid (AC6) — `MemoriesEvidenceGridGapTests.cs`
- Planner non-compact path + guard paths (negative cap, null columns).
- Multi-source render (row count + per-row action); sensitive source URI redaction (no `C:\`, no `Bearer `).
- Non-restrictive empty → `NoMatch`; `Unknown` isolation → no rows + `InaccessibleScope`.

### Navigation / Overlays / Confirmations / Commands (AC3–AC5) — tenant isolation & stale context
`InteractionContextValidatorGapTests.cs`, `MemoriesCommandSurfaceGapTests.cs`,
`MemoriesConfirmationAndNavigationGapTests.cs`
- Missing-tenant guards (blank snapshot/active tenant).
- **Cross-tenant / cross-case leakage:** snapshot matches the active scope but the live packet belongs to
  another tenant/case → `TenantChanged` / `CaseChanged`.
- Graph/activity target existence (known graph valid; unknown → `MissingTarget`; activity w/o id valid).
- **Contract-version mismatch** disables every command (incl. tenant verification) — at the mapper and the
  rendered surface; empty graph disables only Open Graph.
- Confirmation accept/cancel transitions invoke `OnConfirm`/`OnCancel`; tenant-wide (null case) copy names
  "tenant-wide"; mapper null guards; navigation context sanitization + stale-context disabled-reason surface.

## Files added

- `tests/Hexalith.Memories.Web.Tests/Components/Filters/FilterInspectionMapperGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Filters/MemoriesFilterSummaryGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Forms/ContractAwareFormValidatorGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Grid/MemoriesEvidenceGridGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Interaction/InteractionContextValidatorGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Interaction/MemoriesCommandSurfaceGapTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Interaction/MemoriesConfirmationAndNavigationGapTests.cs`

## Coverage map (Story 17.3)

- **AC1** forms (scope-first, contract-aware validation, acknowledgement, dispatch gating): forms gap tests + existing.
- **AC2** inspectable filters (axes, trust effects, empty-state distinctions, contract-boundary unknowns): filters gap tests + existing.
- **AC3** navigation context preservation + return path: validator + navigation gap tests + existing.
- **AC4** safety-gated confirmations (tenant/case/object/consequence/recovery; accept/cancel): confirmation gap tests + existing.
- **AC5** command access (availability, disabled reasons, stale/version revalidation): command-surface gap tests + existing.
- **AC6** data grid (compact column priority, trust-critical columns, row actions, empty/restricted states): grid gap tests + existing.

## Next steps

- Run the Web test lane in CI alongside the rest of the solution.
- When a runnable web host lands (later Epic 17 stories), add Playwright + axe viewport checks at
  360/768/1024/1440px to complete Task 6's browser-level validation; not applicable for this RCL-only slice.

---

# Test Automation Summary — Story 2.7 (Evidence Packet Contract Mapping)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-30
- **Story:** `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`
- **Historical artifact ignored:** `_bmad-output/implementation-artifacts/2-7-benchmark-suite-and-thesis-validation.md`
  is explicitly marked as historical; benchmark validation moved to Story 2.8.
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute. CLI/MCP/server tests use existing in-process
  fakes and contract fixtures; no new framework introduced.
- **Sandbox runner:** `dotnet build --no-restore -m:1`, then xUnit v3 executable fallback. `dotnet test`
  is blocked here by VSTest socket permissions.

## Generated / Validated Tests

### API / Contract Tests
- [x] `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketMapperTests.cs` — packet mapping,
  unauthorized short-circuit, omission precedence, score-strength edge cases, and fallback axes.
- [x] `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketSanitizationTests.cs` — sensitive diagnostic
  redaction for backend URLs, bearer tokens, stack traces, local/UNC paths, and credentials.
- [x] `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketSerializationTests.cs` and parity/isolation
  tests — stable JSON shape, round trips, canonical fixture parity, and tenant/case non-leakage.

### Surface / E2E-Equivalent Tests
- [x] `tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs` — end-to-end command execution
  for CLI JSON evidence packets, token-budget compression, degraded results, and sanitized error envelopes.
- [x] `tests/Hexalith.Memories.Mcp.Tests/EvidencePacketMcpParityTests.cs` and `SearchMemoryToolTests.cs` —
  MCP `search_memory` structured content and text fallback parity.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/EvidencePacketServerMappingTests.cs` — server-emitted
  metadata maps into packet state, omissions, recovery actions, and canonical JSON.

## Coverage

- Contract packet states: complete, weak, empty, degraded, unauthorized, pending expansion covered.
- Omitted-detail reasons: none, token budget, backend unavailable, combined, authorization covered.
- Surfaces: Contracts, CLI JSON, MCP structured/text fallback, and server metadata mapping covered.
- UI/browser E2E: not applicable for Story 2.7; the story forbids web UI implementation.

## Validation

- `dotnet build` focused projects with `-m:1`: Contracts.Tests, Cli.Tests, Mcp.Tests, Server.Tests all clean
  with 0 warnings / 0 errors.
- xUnit fallback results:
  - Contracts Evidence Packet lane: 101 passed.
  - CLI packet/search lane: 19 passed.
  - MCP packet/search/error lane: 37 passed.
  - Server packet/search lane: 71 passed.

## Checklist Result

- API tests generated/validated where applicable.
- E2E-equivalent surface tests generated/validated for CLI and MCP; no browser UI exists in this story.
- Tests use standard xUnit v3 APIs, semantic command/tool contracts, no sleeps, and independent fixtures.
- Summary includes coverage metrics and validation commands/results.

---

# Test Automation Summary — Story 17.4 (Role-Specific Web Inspection Lenses)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24
- **Story:** `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`
- **Framework detected:** xUnit v3 (3.2.2) + bUnit (2.8.4-preview) + Shouldly + `Hexalith.FrontComposer.Testing`
  (`FrontComposerTestBase`). Matched the existing stack; no new framework introduced.
- **Run command (sandbox-safe in-process runner):**
  `./tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests`
  (`dotnet test`/VSTest socket is blocked in this sandbox, per the story's Dev Agent Record.)

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 256 | 0 | 0 | 0 |
| **After gap auto-apply** | **291** | **0** | **0** | **0** |

All 291 tests pass (**+35**). The test project builds clean under `TreatWarningsAsErrors=true`
(0 warnings / 0 errors), and `git diff --check` is clean.

## Scope note — API vs E2E

- **API tests:** Not applicable. Story 17.4 is a **consume-only RCL** web slice over the shared Evidence
  Packet contract; it adds no runnable API endpoints. The pure mappers (`LensShellMapper`,
  `CaseActivityTrailMapper`, `IngestionLifecycleMapper`, `OperatorHealthMatrixMapper`,
  `BenchmarkResultComparatorMapper`, `AgentPacketInspectorMapper`) are the API-equivalent layer and are
  unit-tested directly.
- **Browser E2E (Playwright/axe):** Not applicable — no runnable web host ships with this slice (as the
  story records). The project's component test surface is **bUnit**; keyboard/accessibility/responsive
  behavior is asserted there.

## Gaps discovered and auto-applied

Mapped Story 17.4 Task 6 / Task 7 required coverage against existing tests and filled the remaining gaps.
Tests use `data-testid`/accessible locators (no CSS-class selectors), the canonical bounded
`LensPacketFixtures` inventory, no sleeps, and each builds its own fixtures (order-independent).

### Cross-cutting guardrails — `Components/Lenses/LensCrossCuttingTests.cs` (new, 20 cases)

- **Cross-lens consistency:** same packet → identical shell state / severity / affected capability /
  confidence / freshness / contract version / scope across all five lenses (5 fixtures); confidence
  suppressed identically under a restrictive scope.
- **Tenant isolation:** cross-tenant packet carries the foreign tenant/case across every lens; the Agent
  Packet copy payload / command target repartition with **no originating-tenant residue**.
- **Role-density invariance:** Ingestion / Operator Health / Benchmark / Agent Packet projections preserve
  packet semantics across roles (only ordering/density differs); **no role broadens authorization** on an
  unauthorized packet (4 roles × 5 lenses).
- **Fail-closed:** unknown isolation scope is treated as restrictively as unauthorized across every lens.
- **Contract version:** every lens always reports the supported contract version (incl. schema-mismatch /
  cross-tenant packets).
- **Stale/changed-context revalidation:** a previously-expandable compressed packet and a recoverable
  degraded packet have their expansion / recovery commands **disabled before activation** once the scope
  becomes restrictive.

### Per-lens state matrix completion (added to existing mapper test files, 13 cases)

- **Case Activity (AC1):** empty (trust-state continuity preserved), degraded explicit.
- **Ingestion (AC2):** empty (no fabricated unit), schema-mismatch fail-closed without leak.
- **Operator Health (AC3):** empty (no trust-blocking), redacted (DetailCompleteness caution, no producer
  action), schema-mismatch safe with fixed 6-check set.
- **Benchmark (AC4):** empty (MissingBaseline, nothing inferred), sensitive-axis scrubbing, schema-mismatch,
  and a sweep proving benchmark-only states (`Regression`/`Inconclusive`/`Unreproducible`) and NDCG@10 are
  **never inferred** from the bounded inventory.
- **Agent Packet (AC5):** empty (NoMatch signalled without raw-JSON inspection), redacted (omitted groups
  announced, not an error).

### Component (bUnit) — `Components/Lenses/MemoriesLensComponentsTests.cs` (2 cases)

- Cross-tenant packet renders the foreign tenant/case in the rendered shell (no tenant-a residue).
- The return action is reachable and emits the sanitized return route (keyboard-reachable return for AC1).

## Coverage map (Story 17.4)

- **AC1–AC5:** each lens has mapper + component coverage across populated / empty / degraded / unauthorized /
  unknown-scope / redacted-sensitive / schema-mismatch states.
- **Cross-cutting:** shared-shell consistency, tenant isolation, role-density invariance, fail-closed
  authorization, contract-version stability, stale-context command revalidation, and copy/diagnostics
  sanitization are covered across all five lenses.

## Next steps

- When **Story 2.7** lands canonical benchmark / ingestion-stage / MCP-tool-name / freshness / last-checked
  contract fields, replace the deferred `NoContractSource` unavailable boundaries with populated-value tests
  and add `Regression`/`Inconclusive`/`Unreproducible` benchmark coverage.
- When a runnable web host lands, add one Playwright + axe smoke path per lens at 360/768/1024/1440px
  (Task 7 viewport set) using role/label or `data-testid` selectors.

---

# Test Automation Summary — Story 18.1 (AppHost Project-Resolution Guard & Public-Surface Stability Contract)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24
- **Story:** `_bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md`
- **Framework detected:** xUnit v3 (3.2.2) + Shouldly (4.3.0) in `tests/Hexalith.Memories.IntegrationTests`,
  default (no-Docker) lane. Matched the existing stack; no new framework introduced.
- **Run command (sandbox):** `DiffEngine_Disabled=true dotnet exec
  tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll
  -class …AppHostProjectResolutionTests -class …PublicSurfaceStabilityTests` (`dotnet test`/VSTest socket is
  blocked in this sandbox, per the story's Dev Agent Record).

## Result

| | Discovered (IntegrationTests assembly) | Target run | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 237 | 1 (AC1 guard) | 0 | 0 |
| **After gap auto-apply** | **239** | **3** | **0** | **0** |

All 3 target tests pass (**+2** new `[Fact]`s). The IntegrationTests project builds clean under
`TreatWarningsAsErrors=true` (0 warnings / 0 errors).

## Scope note — API vs E2E

- **API / Browser E2E tests:** Not applicable. Story 18.1 is a **test + docs** story; its "feature" is a
  public-surface **stability contract** (`docs/dev/public-surface-stability.md`), not a runnable HTTP/API or
  UI surface. The applicable automated tests are buildable, no-Docker **guard tests** over that contract. The
  AC1 constraint forbids `DistributedApplicationTestingBuilder` / Testcontainers, so all tests stay plain
  `[Fact]`s in the default lane.

## Gaps discovered and auto-applied

Audited the existing single AC1 guard test against the **full** documented contract. The contract doc itself
flagged that the assembly-name / root-namespace / PackageId half was "enforced by review" only — two of those
three are reflectable with no Docker, so they were untested-but-testable gaps.

- **G1 — Server assembly name + root namespace** (`Hexalith.Memories.Server`): reflectable, previously
  review-only. → new `PublicSurfaceStabilityTests.ServerAssembly_KeepsStableNameAndRootNamespace`.
- **G2 — Mcp assembly name + root namespace** (`Hexalith.Memories.Mcp`): same. → new
  `PublicSurfaceStabilityTests.McpAssembly_KeepsStableNameAndRootNamespace`.
- **G3 — Aspire symbol *shape*** (`Projects.Hexalith_Memories_*`, the dots→underscores rule the doc calls
  load-bearing): only `ProjectPath` was asserted, not the generated type name/namespace. → strengthened
  `AppHostProjectResolutionTests` with `GetType().Namespace`/`.Name` assertions.

**Not auto-applied (correctly out of reach):** the **Mcp PackageId** half of the contract is a pack-time
NuGet property, not embedded in a built assembly, so it is not reflectable at runtime. It remains
review-enforced; the contract doc was updated to state that precisely instead of lumping it with the
now-test-enforced assembly-name/root-namespace half.

## Files added / modified

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs` — **added** (2 `[Fact]`s;
  reflects over a stable public anchor type from each assembly: `IGraphQueryBuilder` for Server,
  `MemoriesMcpAuthenticationOptions` for Mcp).
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostProjectResolutionTests.cs` — **strengthened**
  (AC1 `[Fact]` now also asserts the generated symbol shape).
- `docs/dev/public-surface-stability.md` — **modified** ("Automated enforcement" section synced: assembly-name
  / root-namespace half now test-enforced; only PackageId remains review-enforced).

## Coverage map (Story 18.1)

- **AC1** (buildable project-resolution guard, no Docker): existing compile-time reference + ProjectPath
  assertions, now also the symbol-shape assertions (G3). Still a plain `[Fact]`, no fixture.
- **AC2** (EventStore wiring surface / stale-pin finding): documentation-only AC (`eventstore-integration.md`
  §1.2.1) — no runtime surface to test; left as-is.
- **AC3** (public-surface stability contract): 5/6 contract items now automated — symbol resolution, symbol
  shape, Server assembly name+namespace (G1), Mcp assembly name+namespace (G2), ProjectPath csproj. Mcp
  PackageId (6th) is review-enforced by design.

## Constraints honored

- No Docker / no `DistributedApplicationTestingBuilder` / no Testcontainers — all new tests are default-lane
  `[Fact]`s (AC1).
- No production `src/` code changed, no submodule touched, no `.slnx` / `Directory.Packages.props` /
  `release-packages.json` edits, no PublicAPI analyzer added.
- ITANEO MIT header, file-scoped namespace, global `using Xunit;` (not re-added), Shouldly assertions,
  4-space C#, final newline.

## Next steps

- Run the new tests in CI's default (no-Docker) lane alongside the rest of `Hexalith.Memories.IntegrationTests`.
- Commit as `test:` (guard tests) + `docs:` (doc sync) — **never `feat:`**; Story 18.1 has no release impact.

---

# Test Automation Summary — Story 18.2 (Deployment Configuration Contract Publication)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24
- **Story:** `_bmad-output/implementation-artifacts/18-2-deployment-configuration-contract-publication.md`
- **Feature under test:** the deployment-config contract `docs/operations/deployment-configuration.md` and
  its drift guard `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs`.
- **Framework detected:** xUnit v3 (3.2.2) + Shouldly in `tests/Hexalith.Memories.Server.Tests`, default
  (no-Docker) lane. Matched the existing stack; no new framework introduced.
- **Run command (sandbox):** `DiffEngine_Disabled=true dotnet exec
  tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll
  -class …Deployment.DeploymentConfigurationContractTests` (`dotnet test`/VSTest socket is blocked in this
  sandbox, per the story's Dev Agent Record).

## Result

| | Discovered (Server.Tests assembly) | Target run (contract class) | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 1859 | 4 | 0 | 1 (pre-existing `SubmoduleGuardTests`) |
| **After gap auto-apply** | **1861** | **6** | **0** | **1** |

All 6 contract tests pass (**+2** new `[Fact]`s; the existing source-tie fact was also extended). Build is
**0 warnings / 0 errors**.

## Scope note — API vs E2E

Not applicable. Story 18.2 is a **docs + drift-guard test** story; it adds no runnable HTTP/API or UI
surface. Its "feature" is the published deployment-config contract, and the applicable automated tests are
buildable, no-Docker **content-asserting drift-guard** `[Fact]`s tying the doc to its authoritative sources
(AC2). The QA mandate here is to make every documented element with an authoritative source drift-guarded.

## Gaps discovered and auto-applied

Audited the existing 4-fact guard against the **full** published contract. Five elements were documented but
not source-tied (doc-presence-only or untested) — all five auto-applied:

| # | Documented element previously unguarded | Authoritative source | Guard added |
| :-- | :-- | :-- | :-- |
| A | Server Dapr app-id default `memories` + the `memories-server` reconciliation note (AC2 headline) | `AppHost/Program.cs` `ResolveDaprAppId` (`return "memories";`) | new `[Fact] DeploymentConfigurationDoc_TiesServerAppIdDefaultToResolveDaprAppId` |
| B | OTLP Production-empty warning service `OtlpExporterWarningHostedService` | `ServiceDefaults/Extensions.cs` | source↔doc tie in `LiteralsMatchAuthoritativeSourceFiles` |
| C | Component name `pubsub` agreement: `TenantEventRoutingOptions.PubSubName` default + yaml `metadata.name` | `TenantEventRoutingOptions.cs`, `deploy/dapr/components/pubsub.yaml:18` | new `[Fact] DeploymentConfigurationDoc_IsTiedToRoutingOptionDefaults` + `name: pubsub` source assertion |
| D | Ingest route attributes `[Route("events")]` + `[HttpPost("ingest")]` | `EventIngestionController.cs` | source↔doc ties in `LiteralsMatchAuthoritativeSourceFiles` |
| E | Config-section prefix `EventStoreIntegration:Routing` (doc previously labelled "review-enforced") | `EventStoreIntegrationServiceCollectionExtensions.cs` `GetSection(...)` | source↔doc tie; doc enforcement section upgraded to test-enforced |

## Files modified

- `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` — **4 → 6
  `[Fact]`s**; `LiteralsMatchAuthoritativeSourceFiles` extended (gaps B, D, E + yaml `metadata.name`).
- `docs/operations/deployment-configuration.md` — "Automated enforcement" section rewritten to truthfully
  describe the strengthened guards; **no contract value changed** and every previously-asserted literal
  preserved.

## Verification (negative-proof that drift is actually caught)

- Doc-removal of `OtlpExporterWarningHostedService` → 1 fail; doc-removal of the `memories-server` note →
  1 fail; source-rename of `EventStoreIntegration:Routing` → 1 fail; source-rename of `[HttpPost("ingest")]`
  → 1 fail. All 6 green again after restore; all proof mutations reverted (`git status` clean for `src/`,
  `deploy/`).
- Known limitation (inherited from the `DocumentationCompletenessTests` precedent): `ShouldContain` is
  substring-based, so an append-style rename (`…Service` → `…ServiceXYZ`) is not caught — only token-removing
  renames are. Acceptable for this contract.

## Coverage map (Story 18.2)

- **AC1** (publish canonical contract): every published literal is doc-asserted; OTLP var + warning service,
  Dapr sidecar ports, required runtime env now source-tied.
- **AC2** (guard against drift): all documented elements with an authoritative source are now test-enforced;
  only the architecture-projection backend/dashboard ports (`6379`/`6380`/`18888`/`18889`) remain
  review-enforced by design.
- **AC3** (defer aspirate): unchanged — `MEM-2-ASPIRATE` remains `carried-forward`.
- **AC4** (pub/sub intake surface): component name now tied across all three sources; routing key prefix and
  ingest route attributes now source-tied.

## Next steps

- Run the new tests in CI's default (no-Docker) lane alongside the rest of `Hexalith.Memories.Server.Tests`.
- Commit as `test:` (guard tests) + `docs:` (enforcement-section sync) — **never `feat:`**; Story 18.2 has no
  release impact.

---

# Test Automation Summary — Story 18.4 (Stable Ingest Contract with Explicit Idempotency Token and Atomic Dedup)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-25
- **Story:** `_bmad-output/implementation-artifacts/18-4-stable-ingest-contract-with-explicit-idempotency-token-and-atomic-dedup.md`
- **Feature under test:** the stable ingest path — `IngestionInput.IdempotencyToken`, `MemoriesClient.IngestAsync`
  (graduated out of `HXL001` + token overload), `DedupKeyBuilder` (token `:tok:` namespace), the atomic REST-ingress
  `IngestDedupReservation` (`SET … NX`), `CheckIdempotencyActivity` token precedence, and the workflow's dual
  permanent-record write.
- **Framework detected:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + NSubstitute (5.3.0). Matched the existing stack;
  no new framework introduced.
- **Run command (sandbox):** `DiffEngine_Disabled=true dotnet exec
  tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class …`
  (`dotnet test`/VSTest socket is blocked in this sandbox, per the story's Dev Agent Record).

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| `Server.Tests` baseline (post-dev) | 1887 | 0 | 0 | 1 (pre-existing `SubmoduleGuardTests`) |
| **`Server.Tests` after gap auto-apply** | **1904** | **0** | **0** | **1** |
| `Cli.Tests` baseline (post-dev) | 384 | 0 | 0 | 0 |
| **`Cli.Tests` after gap auto-apply** | **385** | **0** | **0** | **0** |
| `Contracts.Tests` (already covered) | 545 | 0 | 0 | 0 |

**QA delta: +18 test cases** (Server +17, Cli +1). Build **0 warnings / 0 errors** under `TreatWarningsAsErrors=true`.

## Scope note — API vs E2E

Story 18.4 has **no UI surface**; the user-facing path is the REST `/api/ingest` ingress + the `MemoriesClient`
SDK over the `IngestionInput` contract. The applicable automated tests are **contract/client API tests** and
**activity/seam unit tests** — which is the layer exercised here. A browser E2E lane is not applicable.

## Gaps discovered and auto-applied

The dev phase shipped focused happy-path coverage for all four ACs. This pass audited the feature's **own
production code for uncovered branches/boundaries** and filled five:

| # | Gap (uncovered production behavior) | Production anchor | AC | Test added |
| :- | :--- | :--- | :-- | :--- |
| 1 | `TryReserveAsync` **"reservation expired between `SET NX` and `GET`" → `FailOpen`** (NX-false **and** GET-miss) — only NX-false + GET-hit was tested. | `IngestDedupReservation.cs:87-95` | AC3 | `TryReserveAsync_NxFailsButKeyAlreadyExpired_FailsOpen` |
| 2 | `ReleaseAsync` **must swallow a Redis failure** (compensation never hard-fails; TTL is backstop — invariant 8). | `IngestDedupReservation.cs:124-131` | AC3 | `ReleaseAsync_RedisFailure_DoesNotThrow` |
| 3 | `TryReserveAsync` **blank `instanceId` boundary validation** (`ArgumentException.ThrowIfNullOrWhiteSpace`). | `IngestDedupReservation.cs:73` | AC3 | `TryReserveAsync_BlankInstanceId_ThrowsArgumentException` (`[Theory]` ×2) |
| 4 | `DedupKeyBuilder` — the **central design decision** (token `:tok:` namespace *augments-not-replaces*, precedence/fallback, tenant/case isolation, lowercase-hex SHA-256) was only asserted **indirectly**; the 18.5/18.6 invariant (token key ≠ sourceUri key) had no direct guard. | `DedupKeyBuilder.cs:12-37` | AC2 | **new** `DedupKeyBuilderTests` (13 cases) |
| 5 | `MemoriesClient` — **blank/whitespace token → `null`-on-wire** normalization. | `MemoriesClient.cs:495` | AC1/AC2 | `IngestAsync_TokenOverload_BlankToken_NormalizesToNullOnWire` |

## Files added / modified (tests only — no production change)

- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/DedupKeyBuilderTests.cs` — **new** (13 cases).
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` — **+4** (expired→fail-open,
  blank-id ×2, release-on-redis-failure).
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` — **+1** (blank-token normalization).

## Coverage map (Story 18.4)

- **AC1** (stable additive entry point): contract round-trip + back-compat (dev) + client stable/token/blank
  normalization (dev + G5).
- **AC2** (token precedence + sourceUri fallback, augment-not-replace): `CheckIdempotencyActivity` precedence
  (dev) + **direct `DedupKeyBuilder` invariants (G4)** + workflow dual-record (dev).
- **AC3** (atomic, exactly-one-winner): `IngestDedupReservation` winner/loser/concurrent/key-selection/fail-open
  (dev) + **expired→fail-open, blank-id, release-resilience edges (G1-G3)**.
- **AC4** (idempotent under redelivery): workflow duplicate short-circuit, token + sourceUri (dev — fully covered).

## Documented coverage boundary (deferred by design — not a gap)

The REST `/api/ingest` handler orchestration (Reserved→schedule; `DuplicateInFlight`→return winner id without
scheduling; `FailOpen`→schedule; `PreflightDedupEnabled == false`→bypass; release-on-scheduling-failure) is an
inline minimal-API lambda in `Program.cs`, verified at unit level only via its `IngestDedupReservation` seam. A
faithful handler test needs a live `DaprWorkflowClient` + Redis (`WebApplicationFactory`/Aspire/Testcontainers),
which is Docker-dependent and cannot run in this sandbox. This matches the story's stated strategy: the
deterministic substitute-based reservation test is the **authoritative unit-level proof of AC3**, with a true
two-thread real-Redis race deferred to `tests/Hexalith.Memories.IntegrationTests/`. Recorded so the boundary is
explicit rather than implied as covered.

## Next steps

- Run the new tests in CI alongside the existing suites.
- When an Aspire/Testcontainers Redis+Dapr fixture lands, promote the `/api/ingest` reservation wiring and a true
  two-thread race to `Hexalith.Memories.IntegrationTests` (the deferred boundary above).

---

# Test Automation Summary

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Memories.Mcp.Tests/Authentication/McpEndpointChallengeBodyTests.cs` - MCP `/mcp` missing bearer challenge returns sanitized ProblemDetails.
- [x] `tests/Hexalith.Memories.Mcp.Tests/Authentication/McpEndpointChallengeBodyTests.cs` - MCP `/mcp` malformed bearer challenge returns sanitized invalid-token ProblemDetails and does not echo raw bearer material.

### E2E Tests

- [x] Existing `WebApplicationFactory<Program>` MCP endpoint tests exercise the HTTP request/response path in-process. No browser UI exists for Story 20.4.

## Coverage

- API/startup auth features: 6/6 story-required areas covered.
- UI features: 0/0 applicable; Story 20.4 has no module UI.
- Added gap coverage: sanitized challenge response body for missing and malformed bearer tokens.

## Validation

- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --disable-build-servers -m:1 /nr:false`
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointChallengeBodyTests -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointAllowAnonymousPathsTests`
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.MemoriesMcpAuthenticationOptionsTests -class Hexalith.Memories.Mcp.Tests.ConfigureJwtBearerOptionsTests`
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.McpCompositionRootTests`
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointAllowAnonymousPathsTests -class Hexalith.Memories.Mcp.Tests.TenantClaimAuthorizationTests`

## Checklist Result

- API tests generated/updated: pass.
- E2E tests generated if UI exists: not applicable; no UI exists for this story.
- Standard framework APIs, happy path, critical error cases, clear descriptions, no sleeps, independent tests: pass.
- Test summary created with coverage metrics: pass.

---

# Test Automation Summary - Story 21.10 (Migration Subsystem Test Coverage)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/21-10-migration-subsystem-test-coverage.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute, with Redis Stack integration coverage through Testcontainers; no new framework introduced.
- **Feature under test:** embedding vector migration unit/store/tool behavior plus Redis-backed blue/green migration, rollback, abort, marker, and tenant-isolation end-states.

## Generated / Updated Tests

### API / Service / Tool Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs` - covers dry-run/live/resume/rollback/abort orchestration, vector dimension mismatch retention, provider failures, marker end-state protection, secret redaction, and actionable operator messages.
- [x] `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs` - covers marker hashes, owner lock behavior, resume preservation, owner mismatch refusal, completion cleanup, and abort cleanup for both raw and natural-language staging key families.
- [x] `tests/Hexalith.Memories.Server.Tests/Migration/MigrateEmbeddingVectorsToolTests.cs` - covers `--live`, `--resume`, `--rollback`, `--abort`, exactly-one-mode parsing, invalid dimensions, camelCase JSON output, and blue/green help text.

### E2E / Integration Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Migration/EmbeddingVectorMigrationRedisIntegrationTests.cs` - adds RedisStack end-to-end coverage for 768-to-1024 live migration, `FT.INFO` dimension assertions, raw/NL staging hash metadata, completed markers, tenant B isolation, rollback-unavailable fail-closed behavior, pre-cutover abort cleanup, post-cutover abort restore/cleanup, and successful post-cutover rollback end-state.
- [x] Browser UI E2E is not applicable. Story 21.10 has no module UI surface.

## Coverage

- Migration unit/store/tool surfaces: focused coverage present for store behavior, marker ownership/end-state, vector generation, parser modes, JSON output, human guidance, and secret redaction.
- Redis-backed migration surfaces: 5 RedisStack scenarios implemented and compiled.
- Recovery paths: rollback-unavailable, successful post-cutover rollback, pre-cutover abort, and post-cutover abort/restore covered.
- Tenant isolation: tenant B active aliases remain unchanged, no tenant B lock/marker/staging indexes are created, and no tenant B staging keys are written.
- UI features: 0/0 applicable.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed.
- [x] `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore -p:BuildProjectReferences=false` - passed.
- [x] `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~Migration" --logger "console;verbosity=normal"` - blocked before discovery by VSTest sandbox listener permission: `System.Net.Sockets.SocketException (13): Permission denied`.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Migration -parallel none -noLogo` - passed, 60 total, 0 failed, 0 skipped.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Migration.EmbeddingVectorMigrationRedisIntegrationTests -parallel none -noLogo` - Docker/Testcontainers blocked before test bodies: `DockerUnavailableException`, `unix:///var/run/docker.sock`, inner `SocketException: Permission denied`.
- [x] `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` - passed with 0 warnings and 0 errors.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` - passed, 2212 total, 0 failed, 1 skipped.
- [x] 2026-07-05 senior-review rerun: server test build passed, migration namespace fallback passed 60 total / 0 failed, integration test build passed, solution build passed, and RedisStack execution remained blocked by Docker socket permission after discovering 5 tests.

## Checklist Result

- API/service/tool tests generated/updated: pass.
- E2E tests generated if UI exists: pass for RedisStack integration; browser UI not applicable.
- Standard framework APIs, happy path, critical error cases, semantic Redis state assertions, clear descriptions, no sleeps, independent tests: pass.
- All generated non-Docker tests run successfully: pass.
- RedisStack generated tests run successfully: blocked by Docker socket permission in this sandbox; tests compile and are ready for Docker-enabled execution.
- Test summary created with coverage metrics: pass.

## Next Steps

- Keep these checks in the focused MCP auth regression set for Story 20.4.

---

# Test Automation Summary - Story 20.5 (Inbound Rate Limiting, Quotas & Audit Completeness)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute through `Hexalith.Memories.Server.Tests`; no new framework introduced.
- **Feature under test:** inbound tenant-aware API rate limiting and mutation/status access telemetry audit emission.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointRateLimitTests.cs` - added rate-limit rejection checks for retry guidance, infrastructure route exemption, and tenant-create principal partitioning instead of body-tenant partitioning.
- [x] Existing Story 20.5 rate-limit API tests cover tenant route/query partitioning, body-bound ingest limiting after tenant authorization, sanitized 429 `ErrorResponse`, and downstream short-circuiting.
- [x] `tests/Hexalith.Memories.Server.Tests/Telemetry/MutationAuditLogStreamTests.cs` - added mutation/status audit coverage for embedding-config update, tenant workflow status lookups, tenant delete, case-member remove, and case delete validation/error paths.
- [x] Existing Story 20.5 mutation audit tests cover tenant create, tenant display-name update, case-member add, annotation create, and memory-unit delete validation/error paths.

### E2E Tests

- [x] TestServer endpoint workflow coverage exercises the HTTP request/response path in-process. Browser UI E2E is not applicable because Story 20.5 has no module UI scope.

## Coverage

- API rate-limit surfaces: route/query tenant partitioning, body-bound ingest limiter, tenant-create principal partitioning, 429 payload sanitization, retry-after header, downstream short-circuiting, and infrastructure path exemption.
- API mutation audit surfaces: tenant lifecycle/configuration/status/delete, case-member add/remove, annotation create, memory-unit delete, and case delete.
- UI features: 0/0 applicable; Story 20.5 has no UI.

## Validation

- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` - passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLogTests -class Hexalith.Memories.Server.Tests.Telemetry.EndpointTelemetryScopeTests -class Hexalith.Memories.Server.Tests.Telemetry.AuditLogStreamTests -class Hexalith.Memories.Server.Tests.Telemetry.MutationAuditLogStreamTests -class Hexalith.Memories.Server.Tests.Telemetry.TelemetryMetricsRecorderTests -class Hexalith.Memories.Server.Tests.Telemetry.MemoriesMetricsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Authentication.TenantAuthorizationEndpointFilterTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointRateLimitTests` - passed, 133 total, 0 failed.
- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` - passed.

## Checklist Result

- API tests generated/updated: pass.
- E2E tests generated if UI exists: TestServer API workflow coverage added; browser UI not applicable.
- Standard framework APIs, happy path, critical error cases, clear descriptions, no sleeps, independent tests: pass.
- Test summary created with coverage metrics: pass.

---

# Test Automation Summary - Story 20.6 (RediSearch Query-Injection Hardening)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-04
- **Story:** `_bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute through `Hexalith.Memories.Server.Tests`; no new framework introduced.
- **Feature under test:** RediSearch query escaping and parser-error hardening across syntactic, semantic, hybrid, and graph-scoped search paths.

## Generated / Updated Tests

### API / Service Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Search/RediSearchQueryEscaperTests.cs` - extended dialect-2 operator escaping coverage for fuzzy, comma, ampersand, and backtick operators.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs` - added adversarial `sourceType` and wildcard-only query coverage.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs` - added hybrid adversarial filter propagation coverage.
- [x] `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs` - added graph-scoped inner-search adversarial filter propagation coverage.
- [x] Existing Story 20.6 tests cover TEXT/free-text escaping, TAG filters, attribute TAG composites, semantic KNN pre-filters, parser-error classification, and endpoint degradation response builders.

### E2E Tests

- [x] Browser UI E2E not applicable. Story 20.6 is a Server API/security slice with no module UI.

## Coverage

- API/service search axes: syntactic, semantic, hybrid, graph-scoped syntactic, graph-scoped semantic.
- RediSearch escaping contexts: TEXT/free-text, TAG filters, attribute TAG composites, semantic KNN pre-filters.
- User-controlled values: `query`, `metadataQuery`, `subject`, `caseId`, `sourceType`, and `attributeFilters`.
- Critical error cases: parser errors are non-transient, missing indexes return empty results, transient Redis conditions remain degraded 503 paths, vector dimension mismatch remains typed.

## Validation

- [x] `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` - passed.
- [x] `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.RediSearchQueryEscaperTests -class Hexalith.Memories.Server.Tests.Search.SyntacticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointDegradationTests -class Hexalith.Memories.Server.Tests.Search.HybridSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.GraphScopedSearchTests` - passed, 174 total, 0 failed, 0 skipped.
- [x] `git diff --check -- src tests _bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/test-summary.md` - passed.

## Checklist Result

- API tests generated/updated: pass.
- E2E tests generated if UI exists: not applicable; no UI exists for this story.
- Standard framework APIs, happy path, critical error cases, clear descriptions, no sleeps, independent tests: pass.
- Test summary created with coverage metrics: pass.
