---
baseline_commit: 20d3525
---

# Story 22.2: Bounded, Cancellable Graph Traversal

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want graph traversals bounded and server-side cancellable,
so that a dense graph cannot exhaust FalkorDB CPU after the client gives up.

## Acceptance Criteria

1. Given `GraphTraversalService.TraverseAsync` and `GraphScopedSearch.SearchAsync` currently call `falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout, cancellationToken)`, when a traversal runs, then the FalkorDB query receives a non-zero server-side timeout through the existing NFalkorDB compatibility shim's `timeout` argument, while the caller cancellation token is still honored locally. Closes A9.

2. Given `BuildTraverseFromNode` currently emits an unrestricted undirected `[*0..depth]` traversal and is used by graph-scoped search, when this story completes, then `BuildTraverseFromNode` restricts traversal relationships to the semantic edge set `CAUSED_BY|CORRELATED_WITH|REFERENCES` by default and does not traverse `CONTAINS` or `ANNOTATES` structural edges.

3. Given dense graphs can return unbounded node sets, when either direct traversal or graph-scoped traversal query builders emit FalkorDB Cypher, then the query includes a validated result limit and deterministic ordering before `LIMIT`; invalid or non-positive limits are rejected before query construction.

4. Given current graph traversal supports depth clamping, optional case scoping, explicit edge-type filters on `BuildTraverseWithEdges`, stub/gap-marker detection, edge metadata, timeout/degraded REST responses, MCP traversal, and token-budget pruning, when bounding is added, then those behaviors are preserved and existing direct traversal, graph-scoped search, REST, client, and MCP tests remain green.

5. Given Story 22.5 owns all-path-nodes case predicate hardening for `BuildTraverseFromNode`, when Story 22.2 completes, then it does not widen into the A48 case-scope path-integrity fix except for preserving current case filters and avoiding regressions.

6. Given A9 is a performance and cancellation failure mode, when this story completes, then focused unit tests prove server timeout propagation, semantic-only `BuildTraverseFromNode` edges, positive limit validation, `LIMIT` emission, and cancellation/degraded behavior; FalkorDB integration coverage proves a dense structural edge hub is not reachable by default traversal.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A9 and current graph query boundaries (AC: 1-3)
  - [x] Inspect `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` around `BuildTraverseFromNode` and `BuildTraverseWithEdges`.
  - [x] Inspect `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs` and `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs` for every FalkorDB traversal call and current `.WaitAsync(GraphOperationTimeout, cancellationToken)` usage.
  - [x] Inspect `src/Hexalith.Memories.Redis/FalkorDbCompatibilityExtensions.cs` and the local NFalkorDB XML/README docs to confirm `timeout` is milliseconds.
  - [x] Add or update a failing test that demonstrates current traversal query execution leaves the shim timeout at `0`.

- [x] Task 2 - Pass server-side timeout to FalkorDB without losing caller cancellation (AC: 1, 4)
  - [x] Introduce a single helper or constant for converting `GraphOperationTimeout` to a positive millisecond `long`; do not hardcode unrelated timeout values in multiple files.
  - [x] Update `GraphTraversalService.TraverseAsync` to call `falkor.QueryAsync(graphId, query, parameters, timeout: <ms>)` and keep `.WaitAsync(GraphOperationTimeout, cancellationToken)` as the caller-side guard.
  - [x] Update `GraphScopedSearch.SearchAsync` traversal stage the same way; A9 is not closed if graph-scoped search still leaves server timeout at `0`.
  - [x] Preserve graph-not-found empty results and REST degradation/error mapping for `RedisConnectionException`, `RedisTimeoutException`, transient `RedisServerException`, and `TimeoutException`.

- [x] Task 3 - Bound traversal result size in query builders (AC: 2, 3, 5)
  - [x] Add validated limit support to `IGraphQueryBuilder`/`GraphQueryBuilder` traversal methods. Preserve existing overloads or provide a narrow migration path so call sites remain clear.
  - [x] Apply the limit to `BuildTraverseFromNode` and `BuildTraverseWithEdges`; if FalkorDB cannot parameterize `LIMIT`, interpolate only a server-validated positive integer and document why this is safe.
  - [x] Add deterministic ordering before the limit: hop distance then node id for `BuildTraverseFromNode`; existing `BuildTraverseWithEdges` ordering may keep `n.ingestedAt` but must be stable for null timestamps, for example with node id tie-breaks.
  - [x] Keep depth validation at `0..10`; this story does not change public depth semantics.

- [x] Task 4 - Restrict `BuildTraverseFromNode` to semantic edges by default (AC: 2, 4, 5)
  - [x] Replace unrestricted `[*0..depth]` with semantic edge labels derived from `EdgeTypeTaxonomy.SemanticTypes`.
  - [x] Keep edge label interpolation closed over the `EdgeType` enum and `ToUpperSnakeCase`; do not accept raw edge labels from callers.
  - [x] Preserve `startId` and `caseId` as parameters; no tenant, case, memory-unit, or user input may be concatenated into Cypher.
  - [x] Do not implement Story 22.5's all-path-nodes case predicate for `BuildTraverseFromNode` unless preserving this story's behavior makes a tiny local adjustment unavoidable; if touched, document the boundary.

- [x] Task 5 - Add focused unit coverage (AC: 1-5)
  - [x] Extend `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` to assert `BuildTraverseFromNode` contains `CAUSED_BY|CORRELATED_WITH|REFERENCES`, excludes `CONTAINS`/`ANNOTATES`, includes `LIMIT`, validates positive limits, and preserves injection-prevention parameters.
  - [x] Extend `BuildTraverseWithEdges` tests to assert its bounded query shape and deterministic ordering.
  - [x] Extend `tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs` to verify `QueryAsync` receives the server timeout argument and cancellation still propagates as `OperationCanceledException`.
  - [x] Add equivalent timeout propagation coverage for `GraphScopedSearch` if it can be tested with the existing substitutes; otherwise add the smallest test seam needed without broad refactoring.

- [x] Task 6 - Add FalkorDB integration proof where infrastructure is available (AC: 2, 3, 6)
  - [x] Extend `tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeFilterIntegrationTests.cs` or `GraphQueryBuilderIntegrationTests.cs` with a dense `Case -[:CONTAINS]-> MemoryUnit` hub and one semantic chain.
  - [x] Assert default `BuildTraverseFromNode` reaches the semantic chain but not sibling nodes reachable only through `CONTAINS`.
  - [x] Assert the limit caps returned rows deterministically.
  - [x] Keep integration tests in the existing FalkorDB collection; do not introduce new Docker/Testcontainers fixtures.

- [x] Task 7 - Validate and record evidence (AC: 1-6)
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run the focused xUnit v3 in-process graph test classes if normal `dotnet test` is blocked by the sandbox listener issue.
  - [x] Run or compile the FalkorDB integration tests if Docker/Testcontainers are available; if blocked, record the exact Docker/socket/NuGet blocker.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`, or record the exact known sandbox blocker.
  - [x] Update the Dev Agent Record with commands, results, file list, and any sandbox blockers.

### Evidence Table

| Evidence item | Owner | Required proof | Review status | Completion date |
|---|---|---|---|---|
| A9 current-state proof | Dev | Test or code proof showing traversal query calls pass shim timeout `0` before the fix | Pending | |
| Server-side timeout propagation | Dev | `GraphTraversalService` and `GraphScopedSearch` pass positive timeout milliseconds into FalkorDB `QueryAsync` | Pending | |
| Bounded traversal query | Dev | `BuildTraverseFromNode` and `BuildTraverseWithEdges` emit validated `LIMIT` and deterministic ordering | Pending | |
| Semantic-only default `BuildTraverseFromNode` | Dev | Unit and FalkorDB integration tests show default traversal excludes structural `CONTAINS`/`ANNOTATES` paths | Pending | |
| Existing traversal semantics preserved | Dev | Direct traversal, edge-type filters, gap markers, REST degradation, MCP/client behavior, and token-budget pruning tests remain green | Pending | |

## Dev Notes

Story 22.2 is the A9 retrieval-quality remediation story. Keep the implementation narrow: bound graph work and make FalkorDB receive a real server-side timeout. Do not redesign graph-scoped pagination (22.3), hybrid fusion (22.4), case-scoped path integrity (22.5), semantic post-filter recall (22.6), NL axis/reranking seams (22.7), tenant lifecycle, EventStore command boundaries, package versions, or UI.

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 22 covers A8, A9, A29, A30, A48, A49, and A50 retrieval correctness. Story 22.2 closes A9 only.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are FalkorDB as the graph backend, `IGraphQueryBuilder` as the Cypher injection-prevention/extraction boundary, physical tenant isolation at graph database level, and graph traversal as a dual direct-traversal/graph-scoped-search capability.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements include FR16 graph traversal, FR48 edge-type filtering, FR49 gap markers, FR54 MCP access, and NFR4 graph traversal p95 latency.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI work is in scope, but traversal degradation and omitted/partial graph state must continue to support trust-visible search/explain surfaces.
- Loaded persistent project-context facts from `_bmad-output/project-context.md` and referenced Hexalith contexts; implementation must use .NET 10/C# 14, central package management, xUnit v3, Shouldly, NSubstitute, explicit cancellation tokens, one C# type per file, and existing graph/query builders.
- Loaded previous Story 22.1 and recent git history through `20d3525`; the current remediation pattern is narrow audit closure with focused tests, source anchors, exact sandbox blocker notes, and file-list hygiene.

### Current State and Code Anchors

`GraphTraversalService.TraverseAsync` builds `BuildTraverseWithEdges(startNodeId, depth, caseId, edgeTypes)`, then calls `falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout, cancellationToken)`. This only bounds the local wait/caller cancellation. The FalkorDB server is still called through the compatibility shim with `timeout = 0`. [Source: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:62; src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:69]

`GraphScopedSearch.SearchAsync` uses `BuildTraverseFromNode(startNodeId, depth, normalizedQuery.CaseId)` for graph-scoped search, then also calls `falkor.QueryAsync(graphId, cypherQuery, parameters).WaitAsync(GraphOperationTimeout, cancellationToken)`. A9 must update this path too because it is the production caller of `BuildTraverseFromNode`. [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs:79; src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs:85]

`FalkorDbCompatibilityExtensions.QueryAsync` already accepts `long timeout = 0` and passes it to `falkorDb.SelectGraph(graphId).QueryAsync(query, parameters, flags, timeout)`. Local NFalkorDB 1.0.6 docs state `QueryAsync` timeout is in milliseconds. [Source: src/Hexalith.Memories.Redis/FalkorDbCompatibilityExtensions.cs:37; ~/.nuget/packages/nfalkordb/1.0.6/lib/netstandard2.0/NFalkorDB.xml#QueryAsync]

`BuildTraverseFromNode` currently validates depth `0..10`, interpolates depth as a validated integer, and emits `MATCH p = (start:MemoryUnit {id: $startId})-[*0..{depth}]-(n:MemoryUnit)... RETURN DISTINCT n.id AS nodeId, min(length(p)) AS hopDistance`. It has no edge labels and no `LIMIT`. [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:315]

`BuildTraverseWithEdges` already defaults null/empty filters to `EdgeTypeTaxonomy.SemanticTypes`, supports explicit structural edge filters when callers ask for them, preserves gap-marker fields, and has stronger all-path-node case scoping. Do not regress these Story 4.2/9.2 behaviors. [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:356; src/Hexalith.Memories.Contracts/V1/EdgeTypeTaxonomy.cs:9]

The direct REST traversal endpoint clamps depth to `0..10`, parses optional edge types, passes the request cancellation token into `GraphTraversalService`, applies token-budget metadata, and maps graph backend failures to degraded traversal results or `GRAPH_TIMEOUT`. Preserve this contract. [Source: src/Hexalith.Memories.Server/Program.cs:3496]

`MemoriesClient.TraverseAsync` and MCP `traverse_relations` already forward cancellation tokens and optional edge types to the REST endpoint. This story should not add new public traversal parameters unless absolutely required. [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:894; src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs:64]

### Architecture Constraints

- Graph queries must stay behind `IGraphQueryBuilder`; never concatenate tenant, case, memory-unit, user, query, or metadata input into Cypher. Edge labels and traversal depth are the existing exceptions only because they are closed enum/integer values validated before interpolation. [Source: _bmad-output/project-context.md#Framework-Specific-Rules; src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs]
- Tenant isolation is physical: FalkorDB graph id remains the tenant id supplied by validated upstream paths. Do not replace this with label-only tenant filters. [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns]
- FalkorDB is projection/read-model infrastructure. Do not introduce domain persistence, EventStore commands, workflow orchestration, or tenant registry changes in this story. [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency]
- `CONTAINS` and `ANNOTATES` are structural edge types. Default traversal for retrieval quality should use semantic edges (`CausedBy`, `CorrelatedWith`, `References`) unless a caller explicitly asks for structural edges through `BuildTraverseWithEdges`/REST edge filters. [Source: src/Hexalith.Memories.Contracts/V1/EdgeTypeTaxonomy.cs]
- Keep package versions centralized. Do not change NFalkorDB, StackExchange.Redis, or project package references for this story. [Source: _bmad-output/project-context.md#Technology-Stack-and-Versions]

### Previous Story Intelligence

Story 22.1 completed semantic pagination as a narrow A8 fix and explicitly left graph traversal bounding to Story 22.2. Continue the same pattern: fix the exact audit finding, add focused tests, and document sandbox blockers rather than widening into adjacent Epic 22 stories. [Source: _bmad-output/implementation-artifacts/22-1-semantic-axis-pagination.md]

Story 20.6 and the project context emphasize query-builder boundaries and escaping. For graph, this means all user-controlled values remain parameters and only closed enum labels/validated numeric literals are interpolated. [Source: _bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md; _bmad-output/project-context.md]

Story 21.1 and 21.2 ratified EventStore as domain source of truth and Redis/FalkorDB as rebuildable projections. Traversal bounding is read-model behavior only. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md; _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]

### Git Intelligence

Recent commits:

- `20d3525 feat(story-22.1): Semantic-Axis Pagination`
- `c533874 feat(story-21.10): Migration Subsystem Test Coverage`
- `d673a0e feat(story-21.9): Blue/Green Embedding Migration`
- `3676ad0 feat(story-21.8): Tenant Registry CAS & Rollback Integrity`
- `33b99f5 feat(story-21.8): Update orchestration state and progress for story 21.8`

The current project pattern is small, reviewable remediation with explicit tests and no dependency churn.

### Latest Technical / Library Notes

No package upgrade is required for this story. The installed NFalkorDB 1.0.6 package already exposes `Graph.QueryAsync(..., timeout)` and documents that the timeout value is in milliseconds. Use that existing API through `FalkorDbCompatibilityExtensions.QueryAsync`; do not bypass the shim or migrate call sites to a new graph-selection API as part of A9. [Source: ~/.nuget/packages/nfalkordb/1.0.6/README.md; ~/.nuget/packages/nfalkordb/1.0.6/lib/netstandard2.0/NFalkorDB.xml]

### Scope Boundaries

- In scope: `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs`, `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs`, `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs`, `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs`, `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs`, `tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs`, `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs`, and existing FalkorDB graph integration tests.
- In scope if needed: endpoint contract/degradation tests proving existing REST timeout/degraded semantics remain stable.
- Out of scope: graph-scoped/hybrid pagination correctness (22.3), fusion case attribution and score calibration (22.4), all-path-node case predicate hardening for `BuildTraverseFromNode` (22.5), semantic post-filter recall (22.6), retrieval feature completion/NL/reranking (22.7), tenant provisioning/deletion, package upgrades, submodule changes, and UI.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Keep graph unit tests in existing `tests/Hexalith.Memories.Server.Tests/Graph/` and search tests in `tests/Hexalith.Memories.Server.Tests/Search/`.
- Prefer unit tests for generated query shape, timeout propagation, and validation; use FalkorDB integration tests only for behavior that requires a real graph engine.
- Keep integration tests deterministic: unique graph id, explicit nodes/edges, no live embedding providers, no new infrastructure fixture.
- If `dotnet test` is blocked by the known sandbox TCP listener issue, use the established xUnit v3 in-process fallback and record the exact command/result.
- If Docker/Testcontainers or NuGet signature lookup is blocked, record the exact error rather than weakening or deleting integration coverage.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-22.2 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-22 - approved A9 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A9 - unbounded graph traversal finding]
- [Source: _bmad-output/planning-artifacts/architecture.md#Graph-Axis-Architecture-Decision - direct traversal and graph-scoped search roles]
- [Source: _bmad-output/planning-artifacts/architecture.md#FalkorDB-Decision - IGraphQueryBuilder boundary]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR4 - graph traversal latency]
- [Source: _bmad-output/project-context.md - .NET, FalkorDB, graph query, testing, package, and style rules]
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs - traversal query construction]
- [Source: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs - direct traversal execution]
- [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs - graph-scoped traversal execution]
- [Source: src/Hexalith.Memories.Redis/FalkorDbCompatibilityExtensions.cs - graph-id compatibility shim and timeout parameter]
- [Source: ~/.nuget/packages/nfalkordb/1.0.6/lib/netstandard2.0/NFalkorDB.xml - `QueryAsync` timeout is milliseconds]
- [Source: tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs - graph query unit tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs - traversal service unit tests]
- [Source: tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeFilterIntegrationTests.cs - FalkorDB edge filter integration tests]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red proof: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Graph.GraphTraversalServiceTests -class Hexalith.Memories.Server.Tests.Search.GraphScopedSearchTests` failed before implementation on the new timeout assertions for both direct traversal and graph-scoped search.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~GraphQueryBuilderTests|FullyQualifiedName~GraphTraversalServiceTests|FullyQualifiedName~GraphScopedSearchTests" --no-restore -m:1 /nodeReuse:false` compiled but was blocked by the known VSTest TCP listener sandbox failure: `System.Net.Sockets.SocketException (13): Permission denied`.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Graph.GraphQueryBuilderTests -class Hexalith.Memories.Server.Tests.Graph.GraphTraversalServiceTests -class Hexalith.Memories.Server.Tests.Search.GraphScopedSearchTests` passed: 185 total, 0 failed.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed: 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` passed: 2227 total, 0 failed, 1 skipped.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --no-restore -m:1 /nodeReuse:false` blocked before Docker/FalkorDB execution by NuGet signature lookup: `NU1301: Permission denied (api.nuget.org:443)`.
- `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` blocked by the same NuGet signature lookup for `Hexalith.Memories.AppHost` and `Hexalith.Memories.IntegrationTests`: `NU1301: Permission denied (api.nuget.org:443)`.
- `git diff --check` passed.
- Senior review fix validation: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed: 0 warnings, 0 errors.
- Senior review fix validation: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Graph.GraphQueryBuilderTests -class Hexalith.Memories.Server.Tests.Graph.GraphTraversalServiceTests -class Hexalith.Memories.Server.Tests.Search.GraphScopedSearchTests` passed: 185 total, 0 failed.

### Implementation Plan

- Keep A9 scoped to traversal query execution and query builder bounds without changing public REST/client/MCP request parameters.
- Reuse the existing NFalkorDB compatibility shim and pass a positive server timeout while retaining local `WaitAsync(GraphOperationTimeout, cancellationToken)` cancellation behavior.
- Preserve existing traversal overloads, add bounded overloads for tests and future callers, and default existing call sites to a validated bounded limit.
- Keep Story 22.5 case-path integrity out of scope; `BuildTraverseFromNode` preserves current case filtering only.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 22.2 created as the A9 bounded, cancellable graph traversal remediation story.
- The story requires server-side FalkorDB timeout propagation, query-level limits, and semantic-only default `BuildTraverseFromNode` traversal.
- The story explicitly keeps graph-scoped/hybrid pagination, fusion calibration, all-path case integrity, post-filter recall, NL/reranking completion, package upgrades, and UI out of scope.
- Implemented `GraphQueryExecutionOptions` to centralize graph traversal default result limit and `TimeSpan` to positive millisecond timeout conversion.
- Updated `GraphTraversalService.TraverseAsync` and the `GraphScopedSearch.SearchAsync` traversal stage to pass `timeout: 10000` through the FalkorDB compatibility shim while preserving caller-side `WaitAsync(..., cancellationToken)`.
- Added bounded traversal overloads on `IGraphQueryBuilder`/`GraphQueryBuilder`; existing overloads now default to a validated `LIMIT 1000`.
- Restricted `BuildTraverseFromNode` to semantic edge labels `CAUSED_BY|CORRELATED_WITH|REFERENCES`, with deterministic `ORDER BY hopDistance ASC, nodeId ASC LIMIT`.
- Bounded `BuildTraverseWithEdges` with validated `LIMIT` and stable `ORDER BY coalesce(n.ingestedAt, ''), nodeId ASC`.
- Added focused unit coverage for timeout propagation, cancellation propagation, semantic-only traversal labels, positive limit validation, bounded query shape, and preserved parameterization.
- Added FalkorDB integration test coverage for dense `CONTAINS` hub exclusion and deterministic result limiting; compile/run is blocked in this sandbox by denied NuGet signature network access.
- Senior review fixed the remaining graph-scoped indexed-memory count query to pass the same positive FalkorDB server timeout as traversal queries.
- Senior review corrected graph-scoped traversal latency logging to use total elapsed milliseconds rather than the millisecond component.
- Senior review normalized changed C# files to the repository CRLF setting and updated automation/test-summary documentation for File List accuracy.

### File List

- _bmad-output/implementation-artifacts/22-2-bounded-cancellable-graph-traversal.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- _bmad-output/story-automator/orchestration-20-20260704-091304.md
- src/Hexalith.Memories.Server/Graph/GraphQueryExecutionOptions.cs
- src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs
- src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs
- src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs
- src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs
- tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeFilterIntegrationTests.cs
- tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs
- tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs
- tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-05.

Outcome: Approved after automatic fixes.

Findings fixed:

- MEDIUM: `GraphScopedSearch.HasIndexedMemoryUnitsAsync` still used `falkor.QueryAsync(graphId, countQuery, countParameters).WaitAsync(...)` without the NFalkorDB `timeout` argument. Fixed by passing `timeout: GraphOperationTimeoutMilliseconds` and extending `GraphScopedSearchTests.SearchAsync_PassesServerSideTimeoutToTraversalQuery` to assert both the traversal query and empty-result count query carry `timeout 10000`.
- MEDIUM: `GraphScopedSearch.SearchAsync` logged traversal latency with `Stopwatch.GetElapsedTime(...).Milliseconds`, which reports the millisecond component instead of total elapsed milliseconds. Fixed to use `TotalMilliseconds`.
- LOW: The changed C# files had LF/mixed line-ending churn against the repo CRLF convention, making the diff much larger than the actual code change. Normalized changed C# files to CRLF.
- LOW: File List/test-summary documentation missed changed automation artifacts and referenced existing endpoint coverage as generated/updated coverage. Updated the story File List and test summary wording.

Checklist:

- Story Status verified as reviewable: pass.
- Acceptance Criteria cross-checked against implementation: pass after fixes.
- File List reviewed and validated for completeness: pass after fixes.
- Tests identified and mapped to ACs: pass.
- Code quality and security review performed on changed source files: pass.
- Sprint status sync required: yes, set to done.

### Change Log

- 2026-07-05: Created Story 22.2 context artifact and marked sprint status ready-for-dev.
- 2026-07-05: Implemented bounded, semantic-only, server-timeout-backed graph traversal and added focused unit plus FalkorDB integration coverage. Story marked ready for review.
- 2026-07-05: Senior developer review completed with automatic fixes for graph-scoped count-query server timeout, latency logging, line-ending hygiene, and review documentation. Story marked done.
