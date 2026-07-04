---
baseline_commit: c2bfe91
---

# Story 22.3: Graph-Scoped & Hybrid Pagination Correctness

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want scoped and hybrid searches to paginate honestly,
so that clients can page results and deep results are reachable or explicitly capped.

## Acceptance Criteria

1. Given `GraphScopedSearch.SearchWithinGraphScopeAsync` currently pages inner syntactic/semantic search in batches of 100 and filters the returned batches in memory, when graph-scoped syntactic or semantic search runs, then the graph scope is pushed into the inner RediSearch query using tenant-scoped memory-unit keys (`INKEYS`) or equivalent indexed TAG pre-filtering before `LIMIT`, and the old growing-offset post-filter scan is removed. Closes A29.

2. Given graph-scoped searches can return fewer results than the traversed graph when hashes are missing, source type filters do not match, or metadata filters exclude enriched units, when graph-scoped pure traversal or inner-search mode returns a page, then `TotalCount` reflects the real number of matching, enrichable, filtered results before pagination, not only the returned page size.

3. Given hybrid search currently requests at most rank 100 per axis through `CreateAxisQuery`, when `Offset + MaxResults` is within the supported fusion window, then each enabled axis receives a deterministic candidate window large enough to support the requested fused page, fusion runs once over that window, and `HybridSearchResult.TotalCount` is honest for the fused candidate set used to serve the request.

4. Given A29 explicitly calls out unreachable deep results, when a graph-scoped or hybrid request asks for a page beyond the supported candidate window, then the request fails with a structured `ErrorResponse` such as `PAGINATION_LIMIT_EXCEEDED` instead of silently clamping, returning duplicates, returning an empty page with a fake total, or marking a backend unavailable.

5. Given Redis `FT.SEARCH` supports `INKEYS`, `LIMIT`, `PARAMS`, `DIALECT`, and `SORTBY`, and filtered vector examples place metadata filters before the `KNN` clause, when this story changes RediSearch query construction, then tenant/user/query/filter values remain escaped or parameterized through existing helpers; only validated tenant-scoped Redis keys may be supplied to `INKEYS`.

6. Given Stories 22.1 and 22.2 already fixed semantic offset pagination and bounded graph traversal, when this story is complete, then those behaviors remain intact: semantic search still fetches `offset + maxResults` KNN candidates, graph traversal still passes FalkorDB server timeout and bounded limits, graph backend degradation mapping is preserved, and Story 22.4 score calibration/case attribution, Story 22.5 all-path case integrity, Story 22.6 post-filter recall, and Story 22.7 NL/reranking work remain out of scope.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A29 and current pagination failure modes (AC: 1-4)
  - [x] Inspect `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs` around `SearchWithinGraphScopeAsync`, `MaxInnerSearchPageSize`, and pure graph traversal `TotalCount`.
  - [x] Inspect `src/Hexalith.Memories.Server/Search/HybridSearchService.cs` around `CreateAxisQuery`, `ExecuteAxisAsync`, fusion pagination, and `TotalCount`.
  - [x] Inspect `/api/search` handling in `src/Hexalith.Memories.Server/Program.cs` for graph-scoped syntactic/semantic, pure graph, and hybrid error mapping.
  - [x] Add red tests that prove current hybrid deep offsets are silently capped at rank 100 and current graph-scoped inner search still scans growing offsets instead of pushing graph scope into the inner query.

- [x] Task 2 - Introduce one explicit pagination policy boundary (AC: 3, 4, 6)
  - [x] Add a small internal options/helper type in `src/Hexalith.Memories.Server/Search/` for supported retrieval candidate windows, for example `SearchPaginationOptions`.
  - [x] Define a named maximum candidate window for graph-scoped and hybrid deep paging. Recommended starting point: `1000`, unless existing tests or benchmarks justify a lower value.
  - [x] Use checked arithmetic for `Offset + MaxResults`; negative offsets remain normalized consistently with existing search services.
  - [x] Add an internal typed exception or result path, for example `SearchPaginationLimitExceededException`, for requests exceeding the candidate window. Do not model this as backend degradation.
  - [x] Map the exception at the `/api/search` endpoint to a structured `ErrorResponse` with code `PAGINATION_LIMIT_EXCEEDED`, an actionable message, and HTTP 400 unless local endpoint conventions require a different client-error status.

- [x] Task 3 - Push graph scope into RediSearch inner searches (AC: 1, 2, 5)
  - [x] Convert traversed graph node ids into tenant-scoped Redis hash keys using `IndexSchemaDefinitions.BuildSyntacticKey` for syntactic inner search and `IndexSchemaDefinitions.BuildSemanticKey` or active semantic key helpers for semantic inner search.
  - [x] Add a narrow, testable way for inner search services to accept graph-scope keys before `LIMIT`. Prefer an internal scoped query path over public Contracts V1 changes.
  - [x] For syntactic search, apply `INKEYS` to the tenant's syntactic index and preserve existing query-string filters for case, source type, metadata, CloudEvent subject, and attribute tags.
  - [x] For semantic search, apply the graph scope before KNN ranking using Redis-supported key or TAG filtering. If `INKEYS` cannot be combined safely with the current NRedisStack KNN path, document the selected equivalent indexed filter and prove it avoids the old growing-offset scan.
  - [x] Keep all user-controlled query/filter values behind `RediSearchQueryEscaper`, Redis `PARAMS`, or existing service helpers. Do not concatenate raw case/source/subject/metadata/attribute values.
  - [x] Remove or bypass the old `while` loop that repeatedly increases `innerOffset` to find in-graph hits after the inner search has already ranked the full tenant corpus.

- [x] Task 4 - Make graph-scoped totals honest (AC: 2, 6)
  - [x] In pure graph traversal mode, compute `TotalCount` before applying `Offset`/`Take`, after enrichment and source/metadata filtering, so page 2 does not report only the page size.
  - [x] In graph-scoped inner-search mode, use the scoped RediSearch total for `TotalCount`; do not count only filtered returned batches.
  - [x] Preserve `HasIndexedMemoryUnits` semantics: missing graph remains `false`, empty graph with existing indexed units remains discoverable through the existing count path, and stale/enrichment-skipped entries do not become fake matches.
  - [x] Preserve result ordering: pure graph by hop distance then memory unit id; scoped syntactic/semantic by their axis ranking; no unstable unsorted Redis page iteration.

- [x] Task 5 - Fix hybrid candidate-window and deep-page behavior (AC: 3, 4, 6)
  - [x] Replace the hard `Math.Clamp(offset + maxResults, 1, 100)` behavior in `CreateAxisQuery` with policy-backed checked window calculation.
  - [x] If `Offset + MaxResults` is within the supported window, request that candidate count from each axis with `Offset = 0`, then paginate after fusion.
  - [x] If the requested fused page exceeds the supported window, throw the explicit pagination-limit signal before executing axis searches.
  - [x] Keep stale-leading-page backfill coverage from current `ExecuteAxisAsync`; do not regress behavior where an axis returns an empty page with `TotalCount > 0` because stale/un-enrichable candidates were skipped.
  - [x] Keep all-enabled-axes unavailable semantics unchanged: pagination-limit errors are validation errors, not axis failures.
  - [x] Do not implement Story 22.4's fusion algorithm changes. `FusionEngine` and `ScoreNormalizer` may be touched only if pagination correctness cannot be expressed without a tiny local adjustment.

- [x] Task 6 - Add focused unit coverage (AC: 1-6)
  - [x] Extend `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs` for scoped inner-search query shape, no growing-offset scan, honest totals, and pagination-limit behavior.
  - [x] Extend `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs` for candidate windows above 100, explicit limit errors above the configured cap, honest fused totals, and preservation of stale-leading-page backfill.
  - [x] Add endpoint contract coverage in `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` or the nearest existing endpoint test for `PAGINATION_LIMIT_EXCEEDED` on graph-scoped and hybrid requests.
  - [x] Preserve adversarial filter tests so graph-scope plumbing cannot reintroduce RediSearch query injection.

- [x] Task 7 - Add Redis/FalkorDB integration proof (AC: 1-5)
  - [x] Extend `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` with a corpus where many top tenant-wide syntactic/semantic results are outside the graph scope and in-graph matches are reachable without scanning growing offsets.
  - [x] Assert graph-scoped page 1 and page 2 are disjoint, ordered by the inner axis, and report an honest `TotalCount`.
  - [x] Extend `tests/Hexalith.Memories.IntegrationTests/Search/HybridSearchApiIntegrationTests.cs` or existing service-level tests with `offset > 100` within the new cap and one beyond-cap request.
  - [x] Keep integration tests on existing `CompositeSearchFixture`, Redis Stack, and FalkorDB fixtures. Do not add new Docker/Testcontainers infrastructure.

- [x] Task 8 - Validate and record evidence (AC: 1-6)
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 in-process search tests if normal `dotnet test` is blocked by the known sandbox TCP listener issue.
  - [x] Run or compile Redis/FalkorDB integration tests; if Docker/Testcontainers or NuGet signature lookup is blocked, record the exact blocker.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`, or record the exact known AppHost/IntegrationTests sandbox blocker.
  - [x] Update the Dev Agent Record with commands, results, file list, and sandbox blockers.

### Evidence Table

| Evidence item | Owner | Required proof | Review status | Completion date |
|---|---|---|---|---|
| A29 current-state proof | Dev | Red test or code proof showing graph-scoped inner search scans increasing offsets and hybrid silently caps rank 100 | Complete | 2026-07-05 |
| Graph scope pushed down | Dev | Scoped syntactic/semantic search applies graph memory-unit scope before Redis `LIMIT`/KNN ranking | Complete | 2026-07-05 |
| Honest graph-scoped totals | Dev | Pure graph and graph-scoped inner-search totals reflect matching results before pagination | Complete | 2026-07-05 |
| Hybrid deep-page policy | Dev | Requests within the configured window are served; requests beyond it return `PAGINATION_LIMIT_EXCEEDED` | Complete | 2026-07-05 |
| Regression preservation | Dev | 22.1 semantic pagination, 22.2 graph timeout/bounds, degradation mapping, and stale-page backfill tests remain green | Complete | 2026-07-05 |

## Dev Notes

Story 22.3 is the A29 retrieval-correctness remediation story. Keep it narrow: graph-scoped searches must stop ranking the full tenant corpus and then filtering pages in memory, and hybrid search must stop silently pretending rank 100 is the universe. Do not redesign score calibration, case attribution, all-path case scoping, semantic post-filter recall, NL search, reranking, package versions, submodules, or UI.

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 22 covers A8, A9, A29, A30, A48, A49, and A50. Story 22.3 closes A29 only.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are RediSearch/Redis Vector as tenant-scoped search backends, FalkorDB as graph-scope source, deterministic fusion/search behavior, and physical tenant isolation.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements include FR17 hybrid search, FR22 pagination, FR34 case attribution as a later adjacent concern, and NFR search latency targets.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI work is in scope, but search responses must remain inspectable and must not hide partial, capped, or degraded retrieval behavior.
- Loaded persistent project-context facts from `_bmad-output/project-context.md`; implementation must use .NET 10/C# 14, central package management, xUnit v3, Shouldly, NSubstitute, explicit cancellation tokens, existing search/query helpers, and low-cardinality telemetry.
- Loaded previous Story 22.1 and Story 22.2 files plus recent commits through `c2bfe91`; current remediation pattern is narrow audit closure, source-anchored tests, explicit sandbox blocker notes, and full File List hygiene.

### Current State and Code Anchors

`GraphScopedSearch.SearchAsync` normalizes `MaxResults` to `1..100` and `Offset` to non-negative values, traverses FalkorDB with `BuildTraverseFromNode`, and then either enriches the graph traversal directly or enters Mode 2 through `SearchWithinGraphScopeAsync`. [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs]

`SearchWithinGraphScopeAsync` builds a `HashSet<string>` from traversed node ids, repeatedly invokes the inner search with `Offset = innerOffset` and `MaxResults = 100`, filters each page with `FilterToGraphScope`, and increases `innerOffset` until it has enough window results or exhausts `innerResult.TotalCount`. This is A29's growing-offset scan. [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs]

Pure graph traversal currently applies `Skip(normalizedQuery.Offset).Take(normalizedQuery.MaxResults)` before enrichment and returns `TotalCount = results.Count`. That makes page totals equal the current page's surviving enrichment count instead of the graph-scoped match count before pagination. [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs]

`HybridSearchService.CreateAxisQuery` computes `axisMaxResults = Math.Clamp(offset + maxResults, 1, 100)` and sets `Offset = 0`. Any request beyond rank 100 is silently capped before axis execution, then `TotalCount = fusedResults.Count`. This creates unreachable deep results and fake totals. [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs]

`ExecuteAxisAsync` currently has a useful stale-page backfill loop: if an axis returns empty/stale pages with `TotalCount > 0`, it can request later pages up to `maxPageIterations`. Preserve this behavior while changing the supported candidate window and explicit cap handling. [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs]

The `/api/search` endpoint has separate branches for pure graph, hybrid, graph-scoped semantic, graph-scoped syntactic, and default syntactic/semantic. Pagination-limit errors must be caught in the relevant branches and returned as client input errors rather than graph/Redis degradation. [Source: src/Hexalith.Memories.Server/Program.cs]

`IndexSchemaDefinitions` is the single source of truth for tenant-scoped syntactic and semantic Redis key names. Use `BuildSyntacticKey`, `BuildSemanticKey`, and parse helpers instead of rebuilding `:mu:` or `:vec:` literals. [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs]

### Architecture Constraints

- Tenant isolation remains physical: RediSearch and Redis Vector indexes are tenant-scoped, and FalkorDB graph id remains the tenant id. Do not replace this with only post-query filtering. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Search behavior belongs in `src/Hexalith.Memories.Server/Search/`; externally visible request/response contract changes belong under `Contracts.V1` only if unavoidable. Prefer internal overloads/options for graph-scoped key filters. [Source: _bmad-output/planning-artifacts/architecture.md#FR-Category-to-Structure-Mapping]
- RediSearch syntax must remain injection-safe. Use `RediSearchQueryEscaper`, `PARAMS`, NRedisStack APIs, and validated Redis keys. Do not concatenate user-controlled tenant, case, source, subject, metadata, attribute, or query text into raw RediSearch syntax. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- EventStore remains the domain source of truth; Redis/FalkorDB are rebuildable projections. This story must not introduce domain persistence, workflow orchestration, tenant lifecycle changes, or migration behavior. [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md; _bmad-output/project-context.md#Framework-Specific-Rules]
- Keep package versions centralized. Do not upgrade NRedisStack, StackExchange.Redis, or NFalkorDB for this story. [Source: Directory.Packages.props]

### Previous Story Intelligence

Story 22.1 fixed semantic-axis offset pagination by fetching an `offset + maxResults` KNN candidate window, preserving active-alias fallback, query syntax handling, dimension mismatch behavior, and RediSearch escaping. Story 22.3 must build on that behavior, not duplicate a second semantic search path that ignores the 22.1 fix. [Source: _bmad-output/implementation-artifacts/22-1-semantic-axis-pagination.md]

Story 22.2 fixed graph traversal bounds by passing FalkorDB server timeouts, adding traversal limits, and keeping default `BuildTraverseFromNode` semantic-only. Story 22.3 must preserve those query execution options and not widen into Story 22.5's all-path-node case predicate hardening. [Source: _bmad-output/implementation-artifacts/22-2-bounded-cancellable-graph-traversal.md]

Story 21.4 centralized key/index construction. Do not add production string literals such as `":mu:"`, `":vec:"`, or index suffixes outside `IndexSchemaDefinitions`. [Source: _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md]

Story 20.6 consolidated RediSearch escaping. The new graph-scope query path must preserve adversarial case/source/subject/attribute tests and avoid raw query concatenation. [Source: _bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md]

### Git Intelligence

Recent commits:

- `c2bfe91 feat(story-22.2): Bounded, Cancellable Graph Traversal`
- `20d3525 feat(story-22.1): Semantic-Axis Pagination`
- `c533874 feat(story-21.10): Migration Subsystem Test Coverage`
- `d673a0e feat(story-21.9): Blue/Green Embedding Migration`
- `3676ad0 feat(story-21.8): Tenant Registry CAS & Rollback Integrity`

The current project pattern is small, reviewable audit remediation with explicit regression tests, no package churn, and exact notes for sandbox-blocked integration infrastructure.

### Latest Technical / Library Notes

The installed repo package is `NRedisStack` 1.6.0 and existing services already use `NRedisStack.Search.Query.Limit`, `ReturnFields`, `SetSortBy`, `Dialect`, and Redis `PARAMS` patterns. Local package XML does not expose a high-level `Query.InKeys` helper, so the dev agent should verify the available API before choosing between NRedisStack helpers and a tiny raw `FT.SEARCH` builder around validated key lists. [Source: Directory.Packages.props; ~/.nuget/packages/nredisstack/1.6.0/lib/net10.0/NRedisStack.xml]

Official Redis `FT.SEARCH` syntax includes `INKEYS count key [key ...]`, `LIMIT offset num`, `PARAMS`, `DIALECT`, `SORTBY`, and `TIMEOUT`. Redis also documents that `LIMIT` offsets are zero-indexed and that deterministic paging needs sorting or cursor/aggregate semantics when result order is otherwise unstable. [Source: https://redis.io/docs/latest/commands/ft.search/]

Official Redis vector examples show filtered KNN queries as `<primary_filter_query>=>[KNN ...]`, use `PARAMS` with `DIALECT 2`, and sort by `__vector_score` or a named vector-distance field. This supports pushing graph/case/source filters before KNN instead of post-filtering only the returned top-K page. [Source: https://redis.io/docs/latest/develop/ai/search-and-query/vectors/]

### Scope Boundaries

- In scope: `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs`, `src/Hexalith.Memories.Server/Search/HybridSearchService.cs`, likely small internal search option/exception files, endpoint error mapping in `src/Hexalith.Memories.Server/Program.cs`, and focused search tests.
- In scope if needed: narrow internal overloads in `SyntacticSearchService` and `SemanticSearchService` to accept validated graph-scope keys before paging/ranking.
- In scope: preserving existing endpoint query parameters and Contracts V1 JSON shapes unless a typed error already uses existing `ErrorResponse`.
- Out of scope: Story 22.4 score calibration/RRF/case attribution, Story 22.5 all-path case integrity, Story 22.6 metadata/source-type post-filter recall redesign, Story 22.7 natural-language axis/reranker/highlighting, tenant lifecycle, EventStore commands, package upgrades, submodule changes, and UI.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Keep unit tests under `tests/Hexalith.Memories.Server.Tests/Search/` and endpoint tests under existing server endpoint test folders.
- Prefer unit tests for candidate-window math, cap validation, exception mapping, query-scope propagation, and total-count semantics.
- Use Redis/FalkorDB integration tests only where real `FT.SEARCH`, KNN, `INKEYS`, or graph traversal behavior must be proven.
- If normal `dotnet test` is blocked by the sandbox TCP listener issue, use the established xUnit v3 in-process fallback and record exact command output.
- If Docker/Testcontainers or NuGet signature lookup is blocked, record the exact error rather than weakening integration coverage.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-22.3 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-22 - approved A29 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A29 - graph-scoped and hybrid pagination finding]
- [Source: _bmad-output/planning-artifacts/architecture.md#Performance - search latency targets]
- [Source: _bmad-output/planning-artifacts/prd.md#FR17 - hybrid search]
- [Source: _bmad-output/planning-artifacts/prd.md#FR22 - pagination]
- [Source: _bmad-output/project-context.md - .NET, Redis, graph, testing, package, tenant-isolation, and style rules]
- [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs - graph-scoped search implementation]
- [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs - hybrid search implementation]
- [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs - RediSearch query construction]
- [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs - KNN candidate-window implementation from Story 22.1]
- [Source: src/Hexalith.Memories.Server/Program.cs - search endpoint routing/error mapping]
- [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs - Redis key/index helpers]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs - graph-scoped unit tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs - hybrid unit tests]
- [Source: tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs - Redis/FalkorDB graph-scoped integration tests]
- [Source: tests/Hexalith.Memories.IntegrationTests/Search/HybridSearchApiIntegrationTests.cs - hybrid endpoint integration tests]
- [Source: https://redis.io/docs/latest/commands/ft.search/ - Redis `FT.SEARCH` options]
- [Source: https://redis.io/docs/latest/develop/ai/search-and-query/vectors/ - Redis vector KNN with filters]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, customization block, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM instructions, previous Story 22.1/22.2 files, recent commits, A29 audit anchors, current graph-scoped/hybrid search code/tests, and official Redis search/vector documentation.
- 2026-07-05: story target came from user request `22.3`; sprint status had `epic-22: in-progress` and `22-3-graph-scoped-and-hybrid-pagination-correctness: backlog`.
- 2026-07-05: no module UI work detected; UX context was discovered only for cross-surface evidence/search semantics.
- 2026-07-05: checklist validation applied after creation; story includes A29 anchors, implementation path, code/test file locations, Epic 22 scope boundaries, Redis `INKEYS`/filter specifics, previous-story guardrails, and validation commands.
- 2026-07-05: dev-story workflow loaded BMAD dev-story skill/checklist, project context, sprint status, and story context; `baseline_commit: c2bfe91` was preserved.
- 2026-07-05: inspected graph-scoped, syntactic, semantic, hybrid, endpoint, and existing search test code. Confirmed A29 failure modes: graph-scoped inner search used growing post-filter offsets and hybrid clamped candidate windows to rank 100.
- 2026-07-05: implemented shared `SearchPaginationOptions` and `SearchPaginationLimitExceededException`; graph-scoped/hybrid requests over `offset + maxResults > 1000` now fail as validation instead of backend degradation.
- 2026-07-05: implemented scoped syntactic and semantic internal search paths using validated tenant-scoped Redis keys with raw `FT.SEARCH INKEYS`; existing query/filter escaping and Redis `PARAMS`/KNN pre-filter behavior were preserved.
- 2026-07-05: removed graph-scoped growing-offset inner scan, made pure graph totals count enriched/filtered matches before pagination, and made hybrid axis candidate windows use the checked policy.
- 2026-07-05: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings and 0 errors.
- 2026-07-05: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~Search"` was blocked before discovery by `System.Net.Sockets.SocketException (13): Permission denied` from VSTest TCP listener setup.
- 2026-07-05: xUnit v3 in-process fallback passed for search tests: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Search -parallel none -noLogo` -> 235 total, 0 failed.
- 2026-07-05: xUnit v3 in-process fallback passed for endpoint contract tests: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests -parallel none -noLogo` -> 7 total, 0 failed.
- 2026-07-05: full server xUnit v3 in-process regression passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` -> 2235 total, 0 failed, 1 skipped.
- 2026-07-05: integration test compile was blocked by NuGet signature lookup despite `--no-restore`: `NU1301 Unable to get repository signature information for source https://api.nuget.org/v3-index/repository-signatures/5.0.0/index.json` / `Permission denied (api.nuget.org:443)`.
- 2026-07-05: solution build was blocked by the same NuGet signature lookup for `Hexalith.Memories.AppHost` and `Hexalith.Memories.IntegrationTests`; other listed projects, including `Hexalith.Memories.Server` and `Hexalith.Memories.Server.Tests`, built before the failure.
- 2026-07-05: senior developer review workflow loaded local review skill, workflow, instructions, checklist, BMAD config, project-context, architecture, story file, git status/diff, and all changed source/test files for Story 22.3.
- 2026-07-05: review found and auto-fixed missing tenant-scoped validation before raw RediSearch `INKEYS` calls in syntactic and semantic scoped search paths.
- 2026-07-05: review found and auto-fixed empty scoped graph responses returning the start node as `Query` instead of the user search query.
- 2026-07-05: review validation rerun: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings and 0 errors.
- 2026-07-05: review validation rerun: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~Search"` remained blocked by `System.Net.Sockets.SocketException (13): Permission denied` from VSTest TCP listener setup.
- 2026-07-05: review validation rerun: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Search -parallel none -noLogo` -> 238 total, 0 failed.
- 2026-07-05: review validation rerun: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests -parallel none -noLogo` -> 7 total, 0 failed.
- 2026-07-05: review validation rerun: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` -> 2238 total, 0 failed, 1 skipped.
- 2026-07-05: review validation rerun: `git diff --check` passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 22.3 created as the A29 graph-scoped and hybrid pagination correctness remediation story.
- The story requires graph-scope pushdown before RediSearch paging/ranking, honest totals, explicit deep-pagination cap errors, and preservation of Story 22.1/22.2 behavior.
- The story explicitly keeps fusion calibration, case attribution, all-path case-scope integrity, post-filter recall redesign, NL/reranking completion, package upgrades, submodule changes, and UI out of scope.
- Added one shared search pagination policy boundary with checked candidate-window arithmetic and a typed pagination-limit exception.
- Graph-scoped inner syntactic/semantic endpoint paths now convert traversed nodes to tenant-scoped syntactic/semantic Redis keys and pass them to scoped RediSearch paths before `LIMIT`/KNN ranking.
- Syntactic scoped search uses raw `FT.SEARCH` with `INKEYS`, escaped query strings, `WITHSCORES`, `RETURN`, `LIMIT`, and `DIALECT 2`; semantic scoped search uses raw `FT.SEARCH` with `INKEYS`, `PARAMS`, `SORTBY __vector_score`, `LIMIT`, and `DIALECT 2`.
- Pure graph traversal now enriches and applies source/metadata filters before pagination so `TotalCount` reflects matching enrichable results, not only the returned page.
- Hybrid search now requests `offset + maxResults` candidates per axis up to the 1000-candidate policy window, preserves stale-leading-page backfill, and throws the typed pagination limit before axis execution when the window is exceeded.
- `/api/search` now maps graph-scoped, hybrid, and semantic pagination-limit exceptions to HTTP 400 `ErrorResponse` code `PAGINATION_LIMIT_EXCEEDED`.
- Focused and full server test validation passed through the xUnit v3 in-process runner; IntegrationTests and full solution build are blocked in this sandbox by NuGet signature network access to `api.nuget.org`.
- Senior developer review auto-fixes added tenant-scoped graph-scope key validation before raw `FT.SEARCH INKEYS` command construction for syntactic and semantic scoped searches.
- Senior developer review auto-fix preserves the user search query in empty graph-scoped syntactic/semantic responses.

### File List

- _bmad-output/implementation-artifacts/22-3-graph-scoped-and-hybrid-pagination-correctness.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.Memories.Server/Program.cs
- src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs
- src/Hexalith.Memories.Server/Search/HybridSearchService.cs
- src/Hexalith.Memories.Server/Search/SearchEndpointErrorResponseFactory.cs
- src/Hexalith.Memories.Server/Search/SearchPaginationLimitExceededException.cs
- src/Hexalith.Memories.Server/Search/SearchPaginationOptions.cs
- src/Hexalith.Memories.Server/Search/SemanticSearchService.cs
- src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs
- tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs
- tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/SearchEndpointErrorResponseFactoryTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-05

Outcome: Approved after automatic fixes.

Findings fixed:

- HIGH: Scoped RediSearch paths accepted arbitrary internal `graphScopeKeys` and passed them directly to raw `FT.SEARCH INKEYS`. This violated AC5's requirement that only validated tenant-scoped Redis keys be supplied to `INKEYS`. Fixed by validating syntactic keys with `IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId` and semantic keys with `IndexSchemaDefinitions.TryParseSemanticMemoryUnitId` before raw command construction, with regression tests covering valid, duplicate, and foreign-tenant keys.
- LOW: Empty graph-scoped syntactic/semantic searches returned the graph start node in the response `Query` field because only the legacy `innerSearch` delegate was considered. Fixed by treating `scopedInnerSearch` as an inner search mode for empty traversal responses.

Residual risk:

- Redis/FalkorDB integration execution and full solution build remain blocked in this sandbox by NuGet signature/network access to `api.nuget.org`; the existing integration coverage is present but was not re-executed during review.

### Change Log

- 2026-07-05: Created Story 22.3 context artifact and marked sprint status ready-for-dev.
- 2026-07-05: Implemented graph-scoped and hybrid pagination correctness, scoped RediSearch key pushdown, honest graph totals, explicit pagination-limit errors, and focused regression coverage; marked story ready for review.
- 2026-07-05: Senior developer review completed with automatic fixes for scoped `INKEYS` key validation and empty scoped graph response query preservation; marked story done.
