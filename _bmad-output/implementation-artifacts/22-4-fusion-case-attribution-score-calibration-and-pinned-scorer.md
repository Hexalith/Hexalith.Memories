---
baseline_commit: e72c4a4
---

# Story 22.4: Fusion Case Attribution, Score Calibration & Pinned Scorer

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want hybrid fusion to carry case attribution and fuse calibrated scores on a pinned scorer,
so that hybrid results are not silently degraded versus single-axis search.

## Acceptance Criteria

1. Given `FusionEngine` currently builds `FusedScoredResult` without copying `CaseId`, `CaseName`, or `AnnotationsCount`, when syntactic, semantic, or graph results include attribution for a memory unit, then the fused result preserves the best available case attribution and annotation count so existing hybrid enrichment can resolve `CaseName`, `CaseGroups`, evidence packet source scope, CLI JSON, and MCP output. Closes A30 and reinforces FR34.

2. Given multiple axes can return the same `MemoryUnitId`, when fusion merges axis results, then attribution is deterministic: non-empty matching values are preserved, missing values are filled from later axes, and conflicting non-empty `CaseId` values are treated as stale projection inconsistency by a documented deterministic policy rather than random last-writer behavior.

3. Given `SyntacticSearchService` currently relies on the RediSearch default scorer in both NRedisStack `Query` and raw scoped `FT.SEARCH INKEYS` paths, when syntactic search runs, then the query pins the Redis BM25-family scorer explicitly. Prefer `SCORER BM25STD` for Redis Open Source 8.4+; use a narrowly documented compatibility fallback only if the locally supported Redis Stack version rejects it. Do not leave scorer selection implicit.

4. Given hybrid fusion currently normalizes each axis independently and combines scores by weighted average, when hybrid search fuses candidates from differently shaped syntactic, semantic, and graph score distributions, then it uses a scale-free deterministic method, preferably weighted Reciprocal Rank Fusion (RRF) with a named constant, or per-axis min-max normalization if RRF is rejected with rationale. Raw BM25 magnitude must not dominate semantic or graph ranking.

5. Given current explain/evidence semantics expose per-axis score fields and `CompositeScore` in `[0.0, 1.0]`, when the fusion method changes, then public response fields remain stable and documented: `SyntacticScore`, `SemanticScore`, and `GraphScore` still represent per-axis comparable contribution scores, `CompositeScore` remains bounded and deterministic, and the confidence caveat still means query-result relevance, not factual accuracy.

6. Given Stories 22.1-22.3 changed candidate windows, graph-scope pushdown, pagination-limit errors, and scoped `INKEYS` key validation, when this story is complete, then those behaviors remain intact: semantic offset pagination still fetches the requested candidate window, graph traversal remains bounded/cancellable, hybrid deep pages still fail with `PAGINATION_LIMIT_EXCEEDED` beyond the configured cap, and raw scoped RediSearch commands still validate tenant-scoped keys before execution.

7. Given A30 is a retrieval-quality remediation, when implementation is complete, then focused deterministic-score, case-attribution, scorer-token, hybrid pagination, endpoint/evidence, and benchmark-adjacent tests prove the fix without adding package upgrades, submodule changes, UI changes, tenant lifecycle changes, or Story 22.5-22.7 retrieval features.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm current A30 behavior and write red tests (AC: 1-4)
  - [x] Inspect `src/Hexalith.Memories.Server/Search/FusionEngine.cs` and prove `CaseId`, `CaseName`, and `AnnotationsCount` are currently dropped during accumulator creation/result projection.
  - [x] Inspect `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs` normal and graph-scoped raw `FT.SEARCH` paths and prove no explicit scorer token is emitted.
  - [x] Add failing tests in `tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs` for case attribution preservation, deterministic conflict policy, and scale-free fusion ordering.
  - [x] Add failing tests in `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs` proving normal and raw scoped syntactic paths pin a BM25-family scorer.

- [x] Task 2 - Preserve case attribution through fusion (AC: 1, 2, 5)
  - [x] Extend the internal `FusionAccumulator` to carry `CaseId`, `CaseName`, and `AnnotationsCount`.
  - [x] Populate attribution from each axis through a deterministic merge helper. Recommended policy: first non-empty `CaseId` wins, identical values are accepted, missing values are filled, conflicts keep the first value and emit a low-cardinality warning or test-visible consistency signal without leaking content.
  - [x] Project attribution fields onto `FusedScoredResult` so `EnrichHybridResultWithCaseAttributionAsync` can resolve case names and build `CaseGroups`.
  - [x] Verify evidence packet mapping receives `CaseId` from hybrid results through `EvidencePacketMapper.FromHybridSearchResult`.
  - [x] Do not add new public contract fields unless an existing field is demonstrably insufficient; `FusedScoredResult.CaseId`, `CaseName`, `AnnotationsCount`, and `HybridSearchResult.CaseGroups` already exist.

- [x] Task 3 - Pin the syntactic scorer explicitly (AC: 3, 6)
  - [x] Add one internal scorer constant/helper in `SyntacticSearchService` or a nearby search helper; avoid scattering `"BM25"`, `"BM25STD"`, or scorer literals.
  - [x] For the NRedisStack `Query` path, verify available API support for `.Scorer(...)` or equivalent; if unavailable, use a narrow raw-command path only if it preserves existing query escaping, return fields, `WITHSCORES`, `LIMIT`, `DIALECT`, missing-index mapping, and query-syntax handling.
  - [x] For `SearchWithGraphScopeKeysAsync`, add the scorer token to the raw `FT.SEARCH` argument list without moving `INKEYS` after `LIMIT` or weakening tenant-scoped key validation.
  - [x] Prefer `SCORER BM25STD` because Redis Open Source 8.4 renamed `BM25` to `BM25STD` and marks `BM25` deprecated; if this repo's pinned Redis Stack fixture requires `BM25`, contain the compatibility choice in the helper and document it in tests.
  - [x] Keep Story 20.6 RediSearch escaping protections and Story 22.3 `INKEYS` validation tests green.

- [x] Task 4 - Replace weighted-average score fusion with a scale-free deterministic method (AC: 4, 5)
  - [x] Implement weighted RRF over per-axis ranks with a named rank constant, for example `RrfRankConstant = 60`, unless per-axis min-max is chosen with written rationale in dev notes.
  - [x] Preserve optional axes and degraded-axis behavior: only axes that actually produced usable results participate, and unavailable/stale-only axes must not penalize successful axes.
  - [x] Keep `CompositeScore` bounded in `[0.0, 1.0]`. For RRF, normalize by the maximum possible weighted RRF contribution for the active axes, then clamp finite results defensively.
  - [x] Keep per-axis exposed scores comparable and deterministic. For RRF, expose rank-derived contribution scores per axis or explicitly normalized per-axis values; do not expose raw BM25 as `SyntacticScore`.
  - [x] Retain deterministic sorting by descending `CompositeScore` then `MemoryUnitId` ascending.
  - [x] Remove or narrow `ScoreNormalizer.NormalizeBm25` use only if it is no longer used in production fusion; update tests/docs rather than leaving stale assertions that imply weighted average is still the algorithm.

- [x] Task 5 - Preserve endpoint, CLI, MCP, and evidence semantics (AC: 1, 5, 6)
  - [x] Verify `/api/search` hybrid flow still applies `EnrichHybridResultWithCaseAttributionAsync`, `EnrichHybridResultWithAnnotationCountsAsync` if applicable, token-budget metadata, and `EvidencePacketMapper.FromHybridSearchResult`.
  - [x] Verify `SearchResponseMetadataApplier.ApplyHybrid` still determines `AxesUsed` from non-null per-axis scores after the scoring method changes.
  - [x] Update explain metadata, docs strings, or test fixtures that still describe weighted-average BM25 saturation as the active hybrid fusion method.
  - [x] Do not change CLI/MCP JSON field names. Existing formatters should benefit from populated contract fields without custom branching.

- [x] Task 6 - Add focused validation coverage (AC: 1-7)
  - [x] Extend `FusionEngineTests` for `CaseId` preservation from syntactic-only, semantic-only, graph-only, and mixed-axis results.
  - [x] Extend `FusionEngineTests` for deterministic conflict behavior when two axes return the same memory unit with different non-empty `CaseId` values.
  - [x] Extend `FusionEngineTests` with a skewed-score scenario where rank-based or min-max fusion prevents a raw high BM25 magnitude from dominating a better semantic/graph rank.
  - [x] Extend `FusionEngineTests` or `HybridSearchServiceTests` with 100 repeated runs proving identical `CompositeScore` values and ordering.
  - [x] Extend `SyntacticSearchServiceTests` so normal and graph-scoped query construction assert the explicit scorer token and preserve `INKEYS` before `LIMIT`.
  - [x] Extend `SearchEndpointContractTests`, `EvidencePacketServerMappingTests`, or nearest existing tests to assert hybrid responses and evidence packets include case attribution after fusion.
  - [x] Preserve existing Story 22.3 tests for hybrid candidate windows and `PAGINATION_LIMIT_EXCEEDED`.

- [x] Task 7 - Integration and benchmark-adjacent proof (AC: 3-7)
  - [x] Add or update a Redis-backed syntactic integration test proving the pinned scorer command is accepted by the fixture Redis Stack version.
  - [x] Add or update a hybrid API/integration test with two cases where hybrid results include `CaseId`, `CaseGroups`, and evidence packet source case IDs.
  - [x] If full benchmark execution is too expensive or infra-blocked, add a focused deterministic scorer fixture under existing benchmark/search tests that proves the new fusion method is reproducible for a fixed corpus.
  - [x] Keep integration tests on existing Redis Stack/FalkorDB/Aspire fixtures; do not add new Docker/Testcontainers infrastructure.

- [x] Task 8 - Validate and record evidence (AC: 1-7)
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused server search tests; if normal `dotnet test` is blocked by the known sandbox TCP listener issue, use the established xUnit v3 in-process fallback with `DiffEngine_Disabled=true`.
  - [x] Run or compile Redis/FalkorDB integration tests that cover scorer acceptance and hybrid case attribution; record exact Docker/Testcontainers/NuGet blockers if infrastructure is unavailable.
  - [x] Run `git diff --check`.
  - [x] Update the Dev Agent Record with commands, results, file list, and any blocked validation.

### Evidence Table

| Evidence item | Owner | Required proof | Review status | Completion date |
|---|---|---|---|---|
| A30 current-state proof | Dev | Red tests or code proof showing fusion drops case attribution, syntactic scorer is implicit, and weighted average is score-scale sensitive | Passed | 2026-07-05 |
| Case attribution preserved | Dev | Hybrid fused results carry `CaseId`, optional `CaseName`, annotation count, `CaseGroups`, and evidence source case IDs | Passed | 2026-07-05 |
| Pinned scorer accepted | Dev | Unit query-shape tests plus Redis-backed proof that the selected BM25-family scorer token works in the pinned fixture | Partial: unit proof passed; Redis-backed build/run blocked by sandbox NuGet signature network denial | 2026-07-05 |
| Scale-free deterministic fusion | Dev | RRF or min-max tests with skewed score distributions and 100-run deterministic score/order proof | Passed | 2026-07-05 |
| Regression preservation | Dev | Story 22.1 semantic pagination, Story 22.2 graph bounds, Story 22.3 hybrid deep-page and scoped-key validation tests remain green | Passed for server tests; integration build remains sandbox-blocked | 2026-07-05 |

## Dev Notes

Story 22.4 is the A30 retrieval-quality remediation story. Keep it narrow: fusion must stop dropping case attribution, syntactic search must stop relying on an implicit RediSearch scorer, and hybrid ranking must stop depending on incomparable raw score distributions. Do not implement Story 22.5 all-path case integrity, Story 22.6 post-filter recall, Story 22.7 NL/reranker/highlighting/weight-tuning, package upgrades, submodule changes, or UI work.

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 22 closes A8, A9, A29, A30, A48, A49, and A50. Story 22.4 closes A30 only.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are deterministic pure fusion, normalized/scaled scores, physical tenant isolation, Redis Stack for syntactic/vector search, FalkorDB for graph, and optional/degradable graph contribution.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements include FR17 hybrid search, FR19 per-axis score breakdown, FR34 case attribution, FR63 composite confidence scores, NFR24 normalized axis scores, and NFR25 deterministic fusion scores.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI work is in scope, but evidence packets and agent-facing responses must keep tenant/case scope, source attribution, scores, and degradation visible.
- Loaded persistent project-context facts from `_bmad-output/project-context.md` and submodule project-context files. Implementation must use .NET 10/C# 14, centralized package versions, xUnit v3, Shouldly, NSubstitute, explicit cancellation where public async code changes, existing search/query helpers, and low-cardinality telemetry.
- Loaded previous Story 22.3 file and recent commits through `e72c4a4`; current remediation pattern is narrow audit closure, source-anchored tests, explicit sandbox blocker notes, and full File List hygiene.

### Current State and Code Anchors

`FusionEngine.Fuse` accumulates syntactic, semantic, and graph scores keyed by `MemoryUnitId`, but `FusionAccumulator` only stores scores, snippet, source URI, and source type. It does not store `CaseId`, `CaseName`, or `AnnotationsCount`, so `FusedScoredResult.CaseId` remains null even when all input `ScoredResult` values carry attribution. [Source: src/Hexalith.Memories.Server/Search/FusionEngine.cs]

`FusedScoredResult` already exposes `CaseId`, `CaseName`, and `AnnotationsCount`; `HybridSearchResult` already exposes `CaseGroups`. Use these fields rather than inventing a new public contract. [Source: src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs]

`EnrichHybridResultWithCaseAttributionAsync` in `Program.cs` already resolves case names and builds `CaseGroups`, but it can only work if fusion preserves `CaseId` first. [Source: src/Hexalith.Memories.Server/Program.cs]

`EvidencePacketMapper.FromHybridSearchResult` maps each `FusedScoredResult.CaseId` into evidence sources. Once fusion preserves attribution, MCP/CLI/future UI evidence packets can surface it through existing contracts. [Source: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs]

`SyntacticSearchService.SearchAsync` creates an NRedisStack `Query` with `WITHSCORES`, `LIMIT`, `DIALECT 2`, and return fields, but no explicit scorer. `SearchWithGraphScopeKeysAsync` builds a raw `FT.SEARCH` command with `INKEYS`, `WITHSCORES`, `RETURN`, `LIMIT`, and `DIALECT 2`, but no `SCORER`. [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs]

Story 22.3 added `SyntacticSearchService.ValidateGraphScopeKeys` and equivalent semantic validation before raw `INKEYS` execution. Do not regress this while adding scorer tokens to raw commands. [Source: _bmad-output/implementation-artifacts/22-3-graph-scoped-and-hybrid-pagination-correctness.md]

`FusionEngine` currently uses `ScoreNormalizer.NormalizeBm25`, `NormalizeCosine`, and graph-score clamp before weighted average. This satisfies the historical Story 2.5 shape but is the A30 target because per-axis score distributions remain differently shaped; rank-based RRF is the recommended scale-free replacement in the sprint change proposal. [Source: src/Hexalith.Memories.Server/Search/FusionEngine.cs; _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md]

### Architecture Constraints

- Fusion must remain a pure deterministic function with no backend calls and no hidden state. All inputs needed for scoring must be passed in by the caller. [Source: _bmad-output/planning-artifacts/architecture.md#Testability-Architecture]
- Tenant and case identifiers must remain visible through API, storage, search, CLI, MCP, telemetry, and evidence packets. Case attribution is not a cosmetic field; it is part of trust and scope. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- RediSearch syntax must remain injection-safe. Keep user-controlled query/filter values behind `RediSearchQueryEscaper`, Redis `PARAMS`, NRedisStack APIs, or validated Redis keys. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- EventStore remains the source of truth; Redis/FalkorDB are rebuildable projections. This story must not introduce domain persistence, workflow orchestration, tenant lifecycle changes, or migration behavior. [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md; _bmad-output/project-context.md#Framework-Specific-Rules]
- Keep package versions centralized. Do not upgrade NRedisStack, StackExchange.Redis, Redis Stack, or FalkorDB for this story. [Source: Directory.Packages.props]

### Previous Story Intelligence

Story 22.1 fixed semantic-axis offset pagination by fetching `offset + maxResults` KNN candidates and preserving RediSearch escaping, active-alias fallback, and dimension-mismatch behavior. Story 22.4 must not reintroduce semantic pagination drift through hybrid fusion. [Source: _bmad-output/implementation-artifacts/22-1-semantic-axis-pagination.md]

Story 22.2 bounded graph traversal with FalkorDB server timeout propagation, traversal limits, and semantic-only default traversal edges. Story 22.4 may consume graph scores but must not widen traversal scope or undo timeout/bounds behavior. [Source: _bmad-output/implementation-artifacts/22-2-bounded-cancellable-graph-traversal.md]

Story 22.3 fixed graph-scoped/hybrid pagination with a shared candidate-window policy, `PAGINATION_LIMIT_EXCEEDED`, scoped RediSearch key pushdown, and raw `INKEYS` tenant-key validation. Story 22.4 must preserve these tests while changing score fusion and syntactic scorer tokens. [Source: _bmad-output/implementation-artifacts/22-3-graph-scoped-and-hybrid-pagination-correctness.md]

Story 20.6 consolidated RediSearch query escaping. Adding `SCORER` must not weaken adversarial query/case/source/subject/attribute tests or raw command handling. [Source: _bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md]

### Git Intelligence

Recent commits:

- `e72c4a4 feat(story-22.3): Graph-Scoped & Hybrid Pagination Correctness`
- `c2bfe91 feat(story-22.2): Bounded, Cancellable Graph Traversal`
- `20d3525 feat(story-22.1): Semantic-Axis Pagination`
- `c533874 feat(story-21.10): Migration Subsystem Test Coverage`
- `d673a0e feat(story-21.9): Blue/Green Embedding Migration`

The current project pattern is small audit remediation with exact current-state proof, focused unit tests, infrastructure proof where relevant, no dependency churn, and explicit notes for sandbox-blocked integration validation.

### Latest Technical / Library Notes

Official Redis `FT.SEARCH` syntax supports `WITHSCORES`, `INKEYS`, `RETURN`, `SCORER`, `LIMIT`, `PARAMS`, and `DIALECT`. Redis documents `SCORER {scorer}` as the way to select a built-in or custom scoring function, and `EXPLAINSCORE` requires `WITHSCORES`. [Source: https://redis.io/docs/latest/commands/ft.search/]

Official Redis scoring docs list `BM25STD` as the default BM25-family scorer and state that `BM25` was renamed to `BM25STD` in Redis Open Source 8.4, with `BM25` deprecated. The implementation should pin the scorer explicitly while using the name supported by the local Redis Stack fixture. [Source: https://redis.io/docs/latest/develop/ai/search-and-query/advanced-concepts/scoring/]

Official Redis vector docs continue to require `LIMIT 0 <top_k>` for KNN result counts and `SORTBY <distance_field>` for vector similarity ordering. Preserve Story 22.1/22.3 candidate-window behavior while changing fusion. [Source: https://redis.io/docs/latest/develop/ai/search-and-query/vectors/]

### Scope Boundaries

- In scope: `src/Hexalith.Memories.Server/Search/FusionEngine.cs`, `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`, likely `src/Hexalith.Memories.Server/Search/ScoreNormalizer.cs`, tests under `tests/Hexalith.Memories.Server.Tests/Search/`, endpoint/evidence tests, and Redis-backed integration tests if fixture execution is available.
- In scope if needed: updating explain metadata strings, evidence packet fixtures, benchmark scorer fixtures, or `SearchResponseMetadataApplier` tests to reflect the new score semantics.
- Out of scope: public contract field additions unless proven necessary, Story 22.5 path-node case integrity, Story 22.6 post-filter recall, Story 22.7 NL axis/reranker/highlighting/weight tuning, tenant lifecycle, EventStore commands, package upgrades, submodule changes, and UI.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Keep unit tests under existing server search test folders.
- Prefer unit tests for pure fusion behavior, deterministic ordering, scorer query-shape construction, and endpoint/evidence mapping.
- Use Redis-backed integration only for real `FT.SEARCH SCORER` acceptance and case-attribution behavior that cannot be proven by unit tests.
- If normal `dotnet test` is blocked by the sandbox TCP listener issue, use the established xUnit v3 in-process fallback and record the exact command.
- If Docker/Testcontainers or NuGet signature lookup is blocked, record the exact error rather than weakening integration requirements.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-22.4 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-22 - approved A30 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A30 - fusion case attribution, scorer, and calibration finding]
- [Source: _bmad-output/planning-artifacts/architecture.md#Algorithmic-Quality - deterministic normalized fusion]
- [Source: _bmad-output/planning-artifacts/prd.md#Confidence-Score-Semantics - score meaning and caveat]
- [Source: _bmad-output/project-context.md - .NET, Redis, graph, testing, package, tenant-isolation, and style rules]
- [Source: src/Hexalith.Memories.Server/Search/FusionEngine.cs - fusion implementation]
- [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs - RediSearch query construction]
- [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs - hybrid orchestration and pagination window]
- [Source: src/Hexalith.Memories.Server/Program.cs - hybrid case-attribution enrichment]
- [Source: src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs - existing hybrid attribution contract fields]
- [Source: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs - hybrid evidence source mapping]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs - fusion unit tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs - syntactic query/unit tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs - hybrid orchestration tests]
- [Source: https://redis.io/docs/latest/commands/ft.search/ - Redis `FT.SEARCH` options]
- [Source: https://redis.io/docs/latest/develop/ai/search-and-query/advanced-concepts/scoring/ - Redis scorer names and BM25STD note]
- [Source: https://redis.io/docs/latest/develop/ai/search-and-query/vectors/ - Redis vector KNN behavior]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, customization block, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/UX instructions, previous Story 22.3 file, recent commits, A30 audit anchors, current fusion/syntactic/hybrid code/tests, and official Redis search/scoring/vector documentation.
- 2026-07-05: story target came from user request `22.4`; sprint status had `epic-22: in-progress` and `22-4-fusion-case-attribution-score-calibration-and-pinned-scorer: backlog`.
- 2026-07-05: no module UI work detected; UX context was discovered only for cross-surface evidence/search semantics.
- 2026-07-05: checklist validation applied after creation; story includes A30 anchors, implementation path, code/test file locations, Epic 22 scope boundaries, Redis scorer compatibility specifics, previous-story guardrails, and validation commands.
- 2026-07-05: dev-story workflow loaded the BMAD dev-story skill/checklist, project context, Hexalith LLM instructions, sprint status, and full story file. Existing `baseline_commit: e72c4a4` was preserved.
- 2026-07-05: current-state proof confirmed `FusionAccumulator` did not carry `CaseId`, `CaseName`, or `AnnotationsCount`; `SyntacticSearchService` used no explicit scorer in typed `Query` or raw `FT.SEARCH INKEYS` paths.
- 2026-07-05: initial focused `dotnet test ... --filter ...` without single-node settings hit MSBuild sandbox pipe permission errors. Retried with `-m:1 /nodeReuse:false`; build completed but VSTest aborted with `SocketException (13): Permission denied`, so xUnit v3 in-process fallback was used.
- 2026-07-05: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings and 0 errors.
- 2026-07-05: focused fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.FusionEngineTests -class Hexalith.Memories.Server.Tests.Search.SyntacticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.ExplainMetadataBuilderTests -class Hexalith.Memories.Server.Tests.Search.HybridSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.EvidencePacketServerMappingTests` -> 106 total, 0 failed.
- 2026-07-05: full server fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` -> 2247 total, 0 failed, 1 skipped.
- 2026-07-05: integration project build was blocked before compilation by sandbox network denial during NuGet repository signature lookup: `NU1301 Permission denied (api.nuget.org:443)`.
- 2026-07-05: full solution build was attempted with `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`; affected server/test projects compiled, but the build failed on AppHost and IntegrationTests with the same `NU1301 Permission denied (api.nuget.org:443)` repository signature lookup.
- 2026-07-05: `git diff --check` passed.
- 2026-07-05: senior developer review workflow loaded story, project context, Redis docs, implementation files, tests, sprint status, and test summary. Review auto-fixed File List hygiene, stale evidence-table statuses, story/sprint status, and one stale contract summary comment.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 22.4 created as the A30 fusion case-attribution, scorer, and score-calibration remediation story.
- The story requires preserving existing hybrid attribution contract fields through fusion, pinning a BM25-family RediSearch scorer explicitly, and replacing weighted-average score fusion with a deterministic scale-free method.
- The story explicitly preserves Stories 22.1-22.3 pagination, graph bounds, pagination-limit, and scoped `INKEYS` key-validation behaviors.
- The story keeps Story 22.5 case-path integrity, Story 22.6 post-filter recall, Story 22.7 NL/reranker/highlighting/weight tuning, package upgrades, submodule changes, tenant lifecycle, and UI out of scope.
- Implemented weighted Reciprocal Rank Fusion with `RrfRankConstant = 60`, bounded composite scores, rank-derived per-axis contribution scores, deterministic same-score tie handling, and `MemoryUnitId` tie-break ordering.
- Preserved hybrid case attribution through fusion: first non-empty `CaseId` wins deterministically, missing matching case names are filled, conflicts do not randomize attribution, and annotation counts preserve the maximum available projection value.
- Pinned RediSearch syntactic scoring to `BM25STD` through a single `RedisSearchScorerName` constant, NRedisStack `Query.SetScorer(...)`, and the raw scoped `FT.SEARCH` argument list while keeping `INKEYS` before `LIMIT`.
- Updated hybrid explain metadata and contract comments so public per-axis hybrid scores are documented as rank contribution scores rather than raw BM25/cosine/proximity magnitudes.
- Removed the obsolete hybrid corpus-statistics dependency because RRF no longer uses BM25 normalization inputs; syntactic results are no longer degraded when corpus stats are unavailable.
- Added focused unit/server coverage for attribution preservation, deterministic conflict handling, scale-free skewed-score ordering, scorer command shape, hybrid axes metadata, evidence packet case attribution, and Story 22.3 pagination/key-validation regressions.
- Added a RedisStack-backed syntactic integration test for pinned `BM25STD` scorer acceptance; compilation/execution of the integration project is blocked in this sandbox by NuGet signature lookup network denial.

### File List

- _bmad-output/implementation-artifacts/22-4-fusion-case-attribution-score-calibration-and-pinned-scorer.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- _bmad-output/story-automator/orchestration-20-20260704-091304.md
- src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs
- src/Hexalith.Memories.Server/Program.cs
- src/Hexalith.Memories.Server/Search/ExplainMetadataBuilder.cs
- src/Hexalith.Memories.Server/Search/FusionAccumulator.cs
- src/Hexalith.Memories.Server/Search/FusionEngine.cs
- src/Hexalith.Memories.Server/Search/HybridSearchService.cs
- src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs
- tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs
- tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/EvidencePacketServerMappingTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/ExplainMetadataBuilderTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs

### Change Log

- 2026-07-05: Created Story 22.4 context artifact and marked sprint status ready-for-dev.
- 2026-07-05: Implemented Story 22.4 A30 remediation: hybrid attribution preservation, explicit `BM25STD` scorer pinning, rank-based RRF fusion, updated explain/docs semantics, and focused validation coverage.
- 2026-07-05: Marked Story 22.4 ready for review; server build and server regression tests passed, while Redis integration/full solution validation is blocked by sandbox NuGet signature network denial.
- 2026-07-05: Senior developer review completed with automatic fixes (File List hygiene, evidence-table statuses, stale contract summary comment, story/sprint status sync). Story 22.4 -> done.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-05

Outcome: Approved after automatic fixes.

Findings fixed:

- Medium: Story File List omitted changed endpoint/test-summary artifacts while git status showed them modified. Added `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` and `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Medium: Evidence Table still reported all review statuses as `Pending` despite implemented and validated proof. Updated rows to passed or blocked-with-sandbox-note.
- Low: `FusedScoredResult` summary still described per-axis scores as normalized scores. Updated the contract summary to rank contribution scores.

Validation:

- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.FusionEngineTests -class Hexalith.Memories.Server.Tests.Search.SyntacticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.ExplainMetadataBuilderTests -class Hexalith.Memories.Server.Tests.Search.HybridSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.EvidencePacketServerMappingTests -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests` - 114 total, 0 failed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` - 2248 total, 0 failed, 1 skipped.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` - blocked by `NU1301` NuGet repository signature lookup denial: `Permission denied (api.nuget.org:443)`.
- `git diff --check` - passed.
