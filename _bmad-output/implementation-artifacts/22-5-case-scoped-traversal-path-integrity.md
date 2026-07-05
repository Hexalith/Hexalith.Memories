---
baseline_commit: 14c1942
---

# Story 22.5: Case-Scoped Traversal Path Integrity

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want case-scoped traversal to constrain every path node to the case,
so that in-case results are not reachable only via other cases and hop scores do not leak cross-case structure.

## Acceptance Criteria

1. Given `GraphQueryBuilder.BuildTraverseFromNode(startNodeId, depth, caseId, limit)` currently constrains only the terminal matched memory unit with `WHERE n.caseId = $caseId`, when a case-scoped graph search runs, then the matched path is valid only if the start node and every `MemoryUnit` node in `nodes(p)` belongs to the requested case, except existing stub/gap-marker nodes with missing content may remain allowed under the same rule already used by `BuildTraverseWithEdges`. Closes A48.

2. Given `BuildTraverseWithEdges` already applies an all-path-nodes case predicate with `start.caseId = $caseId` and `ALL(node IN nodes(p) WHERE ...)`, when implementing this story, then `BuildTraverseFromNode` reuses the same case-boundary semantics rather than inventing a weaker or divergent predicate.

3. Given graph-scoped search uses `BuildTraverseFromNode` before pure graph enrichment and before scoped syntactic/semantic inner search key pushdown, when a graph contains a same-tenant cross-case bridge such as `case-a/start -> case-b/bridge -> case-a/target`, then a case-scoped search for `case-a` does not return `target`, does not pass `target` into scoped RediSearch `INKEYS`, and does not compute hop distance through the cross-case bridge.

4. Given Story 22.2 made `BuildTraverseFromNode` semantic-edge-only, bounded, ordered, and server-timeout-backed, when the case predicate changes, then it still uses only `CAUSED_BY|CORRELATED_WITH|REFERENCES` by default, still validates depth and limit, still emits deterministic `ORDER BY hopDistance ASC, nodeId ASC LIMIT`, and `GraphScopedSearch` still passes the positive FalkorDB timeout.

5. Given Story 22.3 fixed graph-scoped pagination and Story 22.4 fixed fusion attribution/scoring, when this story completes, then graph-scoped pure traversal, graph-scoped syntactic/semantic, and hybrid flows preserve honest totals, disjoint pages, `PAGINATION_LIMIT_EXCEEDED` behavior, validated tenant-scoped Redis keys, case attribution through fusion, and pinned syntactic scoring.

6. Given A48 is a same-tenant cross-case leakage and ranking-integrity defect, when implementation is complete, then focused unit tests prove the new query shape and at least one FalkorDB-backed integration test proves the cross-case bridge negative case. If Docker/Testcontainers or NuGet signature lookup blocks execution, the integration test must still be committed or compile-proven where possible, and the exact blocker must be recorded.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A48 and current code shape (AC: 1-3)
  - [x] Inspect `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` around `BuildTraverseFromNode` and `BuildTraverseWithEdges`.
  - [x] Confirm `BuildTraverseFromNode(..., caseId)` still emits only terminal-node case filtering and does not require every node in `nodes(p)` to satisfy the case boundary.
  - [x] Confirm `GraphScopedSearch.SearchAsync` still calls `BuildTraverseFromNode(startNodeId, depth, normalizedQuery.CaseId)` for pure graph traversal and scoped inner search modes.
  - [x] Add or update a red unit test that proves the current `BuildTraverseFromNode` case-scoped query lacks the all-path `ALL(node IN nodes(p) ...)` predicate and `start.caseId = $caseId` guard.

- [x] Task 2 - Harden `BuildTraverseFromNode` case scoping (AC: 1, 2, 4)
  - [x] Replace the case-scoped `whereClause` for `BuildTraverseFromNode` with the same path-wide semantics used by `BuildTraverseWithEdges`: terminal `n` is in case or an allowed stub, `start.caseId = $caseId`, and every node in `nodes(p)` is either a case-owned `MemoryUnit` or an allowed case boundary node.
  - [x] Keep `startId` and `caseId` parameterized. Do not concatenate tenant, case, memory-unit, user, query, or metadata input into Cypher.
  - [x] Keep relationship labels derived from `EdgeTypeTaxonomy.SemanticTypes` and `ToUpperSnakeCase`; do not accept raw relationship labels.
  - [x] Keep depth and limit interpolation only for already-validated numeric values.
  - [x] Do not change `BuildTraverseWithEdges` unless a shared helper is introduced to remove duplication without changing its existing query semantics.

- [x] Task 3 - Preserve graph-scoped search behavior outside the case-boundary fix (AC: 3-5)
  - [x] Verify pure graph traversal still enriches results through Redis hashes after traversal and still reports `TotalCount` after enrichment/source/metadata filtering but before pagination.
  - [x] Verify scoped syntactic and semantic inner-search paths still receive only traversal-approved node ids converted to validated tenant-scoped Redis keys.
  - [x] Verify an empty or fully excluded traversal returns the user search query for scoped inner-search modes, preserving the Story 22.3 review fix.
  - [x] Do not change public `SearchQuery`, REST, CLI, MCP, evidence packet, or hybrid response JSON shapes.

- [x] Task 4 - Add focused unit tests (AC: 1, 2, 4, 5)
  - [x] Extend `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` so `BuildTraverseFromNode_WithCaseId` asserts `start.caseId = $caseId`, `ALL(node IN nodes(p) WHERE ...)`, the existing allowed-stub predicate, the semantic-only edge labels, deterministic ordering, and `LIMIT`.
  - [x] Add a negative query-shape test proving raw case id input appears only in parameters, not in the generated query string.
  - [x] Preserve existing tests for depth validation, limit validation, semantic-only traversal labels, and no `CONTAINS`/`ANNOTATES` default traversal.
  - [x] Extend `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs` only if a service-level seam can prove traversal-approved ids are the sole source of scoped inner-search keys without duplicating the integration test.

- [x] Task 5 - Add FalkorDB integration proof (AC: 3, 6)
  - [x] Extend `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` or the nearest existing FalkorDB graph integration test with a same-tenant, cross-case bridge:
    - `mu-a-start` in `case-a`
    - `mu-b-bridge` in `case-b`
    - `mu-a-target` in `case-a`
    - semantic edges from start to bridge and bridge to target
  - [x] Seed Redis syntactic hashes for the nodes so pure graph enrichment can return eligible results.
  - [x] Assert `SearchAsync(new SearchQuery { TenantId = tenantId, CaseId = "case-a", ... }, "mu-a-start", depth: 2)` returns the start node and any same-case reachable nodes that have same-case-only paths, but does not return `mu-a-target` through the `case-b` bridge.
  - [x] Add a scoped inner-search variant if practical, proving the excluded target is not passed to the scoped RediSearch key set.
  - [x] Keep the test in existing Redis/FalkorDB fixtures. Do not add new Docker/Testcontainers infrastructure.

- [x] Task 6 - Validate and record evidence (AC: 1-6)
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 in-process graph/search tests if normal `dotnet test` is blocked by the known sandbox TCP listener issue.
  - [x] Run or compile the relevant Redis/FalkorDB integration tests. If blocked, record the exact Docker/socket/NuGet signature error.
  - [x] Run `git diff --check`.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`, or record the exact known AppHost/IntegrationTests sandbox blocker.
  - [x] Update the Dev Agent Record with exact commands, results, file list, and blockers.

### Evidence Table

| Evidence item | Owner | Required proof | Review status | Completion date |
|---|---|---|---|---|
| A48 current-state proof | Dev | Unit test or code proof showing `BuildTraverseFromNode` case scope filters only terminal `n` before the fix | Verified | 2026-07-05 |
| All-path case predicate | Dev | Query-shape tests prove `start.caseId = $caseId` and `ALL(node IN nodes(p) WHERE ...)` are emitted for case-scoped `BuildTraverseFromNode` | Verified | 2026-07-05 |
| Cross-case bridge blocked | Dev/Test | FalkorDB-backed negative test proves a same-tenant path through another case cannot make an in-case target reachable | Source committed; execution blocked by sandbox NuGet signature lookup | 2026-07-05 |
| Graph-scoped search regressions preserved | Dev | Focused tests preserve 22.2 timeout/bounds, 22.3 totals/pagination/key validation, and 22.4 attribution/fusion behavior where touched | Verified | 2026-07-05 |
| Validation hygiene | Dev | Build/test commands and any sandbox blockers recorded with exact output summary | Verified | 2026-07-05 |

## Dev Notes

Story 22.5 is the A48 retrieval-quality remediation story. Keep it narrow: harden case-scoped `BuildTraverseFromNode` so graph-scoped searches cannot route through another case inside the same tenant. Do not implement Story 22.6 post-filter recall, Story 22.7 NL/reranker/highlighting/weight tuning, package upgrades, submodule changes, tenant lifecycle, EventStore command changes, or UI work.

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 22 covers A8, A9, A29, A30, A48, A49, and A50 retrieval correctness. Story 22.5 closes A48 only.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are FalkorDB as graph backend, `IGraphQueryBuilder` as the Cypher injection-prevention boundary, physical tenant isolation at graph database level, and graph traversal as a retrieval axis.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements include FR16 graph traversal, FR20 case filtering, FR33 case-scoped graph edges, FR34 case attribution, FR48 edge-type filtering, FR63 per-axis evidence scores, and NFR8 zero cross-scope leakage.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI work is in scope, but wrong-scope evidence is a trust-blocking state. Search/evidence results must not present cross-case paths as verified case-scoped evidence.
- Loaded persistent project-context facts from `_bmad-output/project-context.md` and submodule project-context files. Implementation must use .NET 10/C# 14, centralized package versions, xUnit v3, Shouldly, NSubstitute, explicit cancellation where public async code changes, existing graph/query builders, and low-cardinality telemetry.
- Loaded previous Stories 22.1-22.4 and recent commits through `14c1942`; current remediation pattern is narrow audit closure, source-anchored tests, explicit sandbox blocker notes, and full File List hygiene.

### Current State and Code Anchors

`GraphScopedSearch.SearchAsync` normalizes pagination, calculates the candidate window, then calls `_graphQueryBuilder.BuildTraverseFromNode(startNodeId, depth, normalizedQuery.CaseId)` for the FalkorDB traversal stage. The returned node ids drive pure graph enrichment and scoped syntactic/semantic inner search. [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs]

`BuildTraverseFromNode(startNodeId, depth, caseId, limit)` validates `startNodeId`, depth, and limit. It restricts default traversal to `EdgeTypeTaxonomy.SemanticTypes`, emits deterministic ordering and `LIMIT`, and keeps `startId`/`caseId` parameterized. Preserve these Story 22.2 behaviors. [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs]

The current case-scoped `BuildTraverseFromNode` predicate is only `WHERE n.caseId = $caseId`. That means a path can start in the requested case, route through another case, and land back in the requested case while still satisfying the terminal-node filter. This is the A48 defect. [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A48]

`BuildTraverseWithEdges` already has the stronger all-path case predicate: terminal `n` is in case or an allowed stub, `start.caseId = $caseId`, and `ALL(node IN nodes(p) WHERE ...)` enforces the case boundary across path nodes. Treat this as the local reference implementation for `BuildTraverseFromNode`. [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs]

`GraphQueryBuilderTests` already covers query parameterization, semantic-only `BuildTraverseFromNode` labels, depth and limit validation, `BuildTraverseWithEdges` case scoping, and edge-type filtering. Extend these tests rather than creating a parallel test file. [Source: tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs]

`GraphScopedSearchIntegrationTests` already seeds FalkorDB nodes/edges and Redis syntactic hashes through `CompositeSearchFixture`, tests tenant isolation, default semantic traversal excluding `CONTAINS` hubs, graph-scoped inner search key propagation, and disjoint pagination. Extend this fixture for the cross-case bridge negative proof. [Source: tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs]

### Architecture Constraints

- Graph queries must stay behind `IGraphQueryBuilder`; never concatenate tenant, case, memory-unit, user, query, metadata, or other caller-controlled values into Cypher. Relationship labels are safe only when derived from the closed `EdgeType` enum, and depth/limit are safe only after validation. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules; src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs]
- Tenant isolation remains physical: FalkorDB graph id is the tenant id supplied by validated upstream paths. Story 22.5 is about case boundary integrity inside the tenant graph, not replacing physical tenant isolation. [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns]
- Case scope is part of the trust/evidence model. Do not return in-case results that are reachable only through another case because the hop score and evidence path would describe the wrong scope. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Core-User-Experience]
- FalkorDB is projection/read-model infrastructure. Do not introduce EventStore commands, domain persistence, workflow orchestration, tenant registry changes, or migration behavior in this story. [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency]
- Keep package versions centralized. Do not change `NFalkorDB` 1.0.6, `NRedisStack` 1.6.0, `StackExchange.Redis`, or project package references for this story. [Source: Directory.Packages.props]

### Previous Story Intelligence

Story 22.1 fixed semantic offset pagination by fetching an `offset + maxResults` KNN candidate window and preserving RediSearch escaping, active-alias fallback, missing-index behavior, and dimension-mismatch behavior. Story 22.5 must not alter semantic search. [Source: _bmad-output/implementation-artifacts/22-1-semantic-axis-pagination.md]

Story 22.2 added FalkorDB server-side timeout propagation, traversal limits, deterministic ordering, and semantic-only default traversal edges. The new all-path case predicate must preserve these constraints. [Source: _bmad-output/implementation-artifacts/22-2-bounded-cancellable-graph-traversal.md]

Story 22.3 pushed graph scope into scoped RediSearch searches, introduced `PAGINATION_LIMIT_EXCEEDED`, preserved honest totals, and added tenant-key validation before raw `FT.SEARCH INKEYS`. Story 22.5 must not weaken that key-validation or pagination behavior. [Source: _bmad-output/implementation-artifacts/22-3-graph-scoped-and-hybrid-pagination-correctness.md]

Story 22.4 preserved case attribution through fusion, pinned the RediSearch BM25-family scorer, and replaced weighted-average fusion with deterministic RRF. Story 22.5 should not touch fusion or syntactic scoring unless an unexpected compile-only dependency forces a narrow test update. [Source: _bmad-output/implementation-artifacts/22-4-fusion-case-attribution-score-calibration-and-pinned-scorer.md]

Story 20.6 consolidated RediSearch escaping and Story 21.4 centralized Redis key schema helpers. Although this story is graph-focused, do not reintroduce raw user input into graph or Redis query strings while validating regression paths. [Source: _bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md; _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md]

### Git Intelligence

Recent commits:

- `14c1942 feat(story-22.4): Fusion Case Attribution, Score Calibration & Pinned Scorer`
- `e72c4a4 feat(story-22.3): Graph-Scoped & Hybrid Pagination Correctness`
- `c2bfe91 feat(story-22.2): Bounded, Cancellable Graph Traversal`
- `20d3525 feat(story-22.1): Semantic-Axis Pagination`
- `c533874 feat(story-21.10): Migration Subsystem Test Coverage`

The current project pattern is small, reviewable audit remediation with exact source anchors, focused unit tests, integration proof where infrastructure allows, no dependency churn, and exact sandbox blocker notes.

### Latest Technical / Library Notes

No package upgrade is required. The repo pins `NFalkorDB` 1.0.6 and already uses Cypher-compatible path predicates through `BuildTraverseWithEdges`; reuse that existing syntax and prove it against the current FalkorDB fixture.

Cypher list functions document `nodes(path)` as returning the nodes in a path, in traversal order. Predicate functions document `all(variable IN list WHERE predicate)` as checking a predicate across every element in a list. This supports the all-path-node predicate already present in `BuildTraverseWithEdges`; the implementation should validate actual support through the existing FalkorDB integration fixture rather than relying only on string-shape tests. [Source: https://neo4j.com/docs/cypher-manual/current/functions/list/#functions-nodes; https://neo4j.com/docs/cypher-manual/current/functions/predicate/#functions-all]

### Scope Boundaries

- In scope: `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs`, focused tests in `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs`, and FalkorDB/Redis integration proof in `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` or the nearest existing graph integration test.
- In scope if needed: a small private helper inside `GraphQueryBuilder` to share case-boundary predicate fragments between traversal builders without changing public interfaces.
- In scope if needed: narrow `GraphScopedSearchTests` coverage to prove traversal-approved ids remain the sole source for scoped inner-search keys.
- Out of scope: public contract changes, endpoint route changes, CLI/MCP changes, fusion/RRF/scorer changes, semantic post-filter recall, NL axis/reranker/highlighting/weight tuning, EventStore command/persistence work, tenant lifecycle, package upgrades, submodule changes, and UI.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Keep graph unit tests in existing `tests/Hexalith.Memories.Server.Tests/Graph/` and search tests in `tests/Hexalith.Memories.Server.Tests/Search/`.
- Prefer unit tests for generated query shape, validation, and parameterization.
- Use Redis/FalkorDB integration tests for the actual cross-case bridge behavior because the bug is path semantics, not just string construction.
- Keep integration tests deterministic: unique tenant graph id, explicit case ids, explicit nodes/edges, fake content hashes, and no live embedding providers.
- If normal `dotnet test` is blocked by the sandbox TCP listener issue, use the established xUnit v3 in-process fallback and record the exact command/result.
- If Docker/Testcontainers or NuGet signature lookup is blocked, record the exact error rather than weakening or deleting integration coverage.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-22.5 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-22 - approved A48 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A48 - case-scoped traversal path integrity finding]
- [Source: _bmad-output/planning-artifacts/architecture.md#FalkorDB-Decision - `IGraphQueryBuilder` boundary]
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns - tenant isolation and graph query isolation]
- [Source: _bmad-output/planning-artifacts/prd.md#FR16 - graph traversal]
- [Source: _bmad-output/planning-artifacts/prd.md#FR20 - case filtering]
- [Source: _bmad-output/planning-artifacts/prd.md#FR33 - case-scoped graph edges]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Core-User-Experience - wrong-scope evidence as trust-blocking]
- [Source: _bmad-output/project-context.md - .NET, FalkorDB, graph query, testing, package, tenant-isolation, and style rules]
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs - traversal query construction]
- [Source: src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs - graph query interface]
- [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs - graph-scoped traversal execution]
- [Source: tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs - graph query unit tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs - graph-scoped unit tests]
- [Source: tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs - Redis/FalkorDB graph-scoped integration tests]
- [Source: https://neo4j.com/docs/cypher-manual/current/functions/list/#functions-nodes - Cypher `nodes(path)` list function]
- [Source: https://neo4j.com/docs/cypher-manual/current/functions/predicate/#functions-all - Cypher `all(... WHERE ...)` predicate function]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, customization block, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM instructions, previous Stories 22.1-22.4, recent commits, A48 audit anchors, current graph query/search code and tests, package pins, and Cypher `nodes(path)`/`all(...)` documentation.
- 2026-07-05: story target came from user request `22.5`; sprint status had `epic-22: in-progress` and `22-5-case-scoped-traversal-path-integrity: backlog`.
- 2026-07-05: no module UI work detected; UX context was discovered only for cross-surface evidence/search trust semantics.
- 2026-07-05: A48 reconfirmed in current code: `BuildTraverseFromNode(..., caseId)` uses only `WHERE n.caseId = $caseId`; `BuildTraverseWithEdges(..., caseId, ...)` already uses the stronger all-path-node predicate.
- 2026-07-05: checklist validation applied after creation; story includes A48 anchors, implementation path, code/test file locations, Epic 22 regression boundaries, previous-story guardrails, latest Cypher predicate notes, and validation commands.
- 2026-07-05: dev-story activation loaded BMAD workflow customization, BMAD config, root project context, Hexalith LLM/state instructions, sprint status, complete story file, current graph query/search code, unit tests, and integration fixture.
- 2026-07-05: red proof added to `BuildTraverseFromNode_WithCaseId_ShouldIncludeWhereClause`; normal `dotnet test` built the server test assembly but VSTest aborted before execution with `SocketException (13): Permission denied` from `TcpListener`.
- 2026-07-05: xUnit v3 in-process red proof passed as a failing test: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Graph.GraphQueryBuilderTests -parallel none -noLogo` -> 138 total, 1 failed before implementation.
- 2026-07-05: implemented path-wide case boundary in `BuildTraverseFromNode`: `(n.caseId = $caseId OR n.content IS NULL)`, `start.caseId = $caseId`, and `ALL(node IN nodes(p) WHERE ...)`; kept `startId`/`caseId` parameterized, semantic-only labels, depth validation, limit validation, ordering, and `BuildTraverseWithEdges` unchanged.
- 2026-07-05: added `SearchAsync_CaseScopedTraversal_ShouldNotReachTargetThroughCrossCaseBridge` to the existing Redis/FalkorDB integration fixture. The test seeds `case-a/start -> case-b/bridge -> case-a/target` plus a same-case direct neighbor, asserts pure graph traversal excludes the bridge/target, and asserts scoped inner-search keys exclude both.
- 2026-07-05: validation passed: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -> 0 warnings, 0 errors.
- 2026-07-05: validation passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Graph.GraphQueryBuilderTests -parallel none -noLogo` -> 138 total, 0 failed.
- 2026-07-05: validation passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` -> 2248 total, 0 failed, 1 skipped.
- 2026-07-05: validation passed: `git diff --check` -> no whitespace errors.
- 2026-07-05: integration compile/run blocked: `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` and retry with `NUGET_CERT_REVOCATION_MODE=offline DOTNET_NUGET_SIGNATURE_VERIFICATION=false` both failed with `NU1301: Unable to get repository signature information for source https://api.nuget.org/v3-index/repository-signatures/5.0.0/index.json` and `Permission denied (api.nuget.org:443)`.
- 2026-07-05: full solution build blocked by the same sandbox NuGet signature lookup for `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` and `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj`; other listed projects built before the failure.
- 2026-07-05: story-automator review loaded local review workflow, checklist, Hexalith LLM instructions, project context, architecture, story file, git status/diffs, implementation files, tests, sprint status, and official Cypher docs for `nodes(...)` and `all(...)`.
- 2026-07-05: review findings fixed automatically: evidence table statuses were stale, File List omitted the modified story-automator orchestration file, and sprint/story status needed final review sync.
- 2026-07-05: review validation passed: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -> 0 warnings, 0 errors.
- 2026-07-05: review validation passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Graph.GraphQueryBuilderTests -parallel none -noLogo` -> 138 total, 0 failed.
- 2026-07-05: review validation passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` -> 2248 total, 0 failed, 1 skipped.
- 2026-07-05: review validation blocked for integration compile: `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` failed with `NU1301: Unable to get repository signature information for source https://api.nuget.org/v3-index/repository-signatures/5.0.0/index.json` and `Permission denied (api.nuget.org:443)`.
- 2026-07-05: review validation blocked for full solution build: `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` failed with the same NuGet signature lookup blocker for AppHost and IntegrationTests; other projects built before failure.
- 2026-07-05: review validation passed: `git diff --check` -> no whitespace errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 22.5 created as the A48 case-scoped traversal path integrity remediation story.
- The story requires `BuildTraverseFromNode` to apply a path-wide case predicate so graph-scoped searches cannot route through another case inside the same tenant.
- The story explicitly preserves Story 22.2 graph timeout/bounds/semantic-only traversal behavior, Story 22.3 graph-scoped pagination/key-validation behavior, and Story 22.4 fusion/scorer behavior.
- The story keeps Story 22.6 post-filter recall, Story 22.7 retrieval feature completion, package upgrades, submodule changes, tenant lifecycle, EventStore command changes, and UI out of scope.
- Implemented the A48 fix by changing only `BuildTraverseFromNode` case-scoped query construction to use the existing all-path case-boundary semantics from `BuildTraverseWithEdges`.
- Strengthened `GraphQueryBuilderTests` so the case-scoped traversal query proves `start.caseId = $caseId`, `ALL(node IN nodes(p) WHERE ...)`, allowed stub/gap-marker behavior, semantic-only default edge labels, deterministic ordering, validated limit interpolation, and parameterized `caseId`.
- Added a Redis/FalkorDB integration regression source test for the same-tenant cross-case bridge and scoped inner-search key exclusion; execution is blocked in this sandbox by NuGet repository-signature network access.
- No public contracts, REST/CLI/MCP surfaces, fusion/scoring behavior, semantic search behavior, package references, submodules, EventStore commands, tenant lifecycle, or UI code were changed.
- Senior developer review completed with automatic fixes to story evidence/status/File List hygiene; no remaining critical issues found.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-05

Outcome: Approved after automatic fixes.

Findings fixed:

- [Medium] Evidence Table still showed all evidence items as `Pending` despite implementation and validation records proving completion or sandbox-blocked integration execution. Updated evidence statuses and completion dates.
- [Medium] File List omitted `_bmad-output/story-automator/orchestration-20-20260704-091304.md`, which is modified in git. Added it for File List hygiene.
- [Medium] Story and sprint status were still at review state after successful source review. Updated story status and sprint tracking to `done`.

Review evidence:

- `BuildTraverseFromNode` now uses the same path-wide case-boundary predicate shape as `BuildTraverseWithEdges`: terminal `n` case/stub allowance, `start.caseId = $caseId`, and `ALL(node IN nodes(p) WHERE ...)`.
- `startId` and `caseId` remain parameterized; edge labels remain closed-enum semantic labels; depth and limit remain validated before interpolation.
- `GraphScopedSearch` still calls `BuildTraverseFromNode(startNodeId, depth, normalizedQuery.CaseId)` and still propagates the positive FalkorDB timeout.
- The integration test source covers the same-tenant cross-case bridge and scoped inner-search key exclusion, but execution/compile remains blocked by sandbox NuGet signature network denial.

### File List

- _bmad-output/implementation-artifacts/22-5-case-scoped-traversal-path-integrity.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/story-automator/orchestration-20-20260704-091304.md
- src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs
- tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs
- tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs

### Change Log

- 2026-07-05: Hardened case-scoped `BuildTraverseFromNode` with path-wide case-boundary semantics, added unit and integration regression coverage, recorded sandbox validation blockers, and moved story to review.
- 2026-07-05: Senior developer review completed with automatic fixes to evidence table, File List, and status sync; moved story to done.
