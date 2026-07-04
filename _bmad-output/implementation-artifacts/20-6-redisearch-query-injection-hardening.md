---
baseline_commit: d9420585b56b
---

# Story 20.6: RediSearch Query-Injection Hardening

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want one shared, complete RediSearch escaper on all axes,
so that user input cannot break query syntax or cause query-shaped denial of service.

## Acceptance Criteria

1. Given syntactic and semantic search currently use separate RediSearch escaping implementations, when this story is complete, then all RediSearch query construction in the Server search path uses one shared helper with context-specific methods for TEXT/free-text clauses, TAG filters, and KNN pre-filters.

2. Given user-controlled values flow into search query syntax through `query`, `metadataQuery`, `subject`, `caseId`, `sourceType`, and `attributeFilters`, when those values contain RediSearch operators, punctuation, backslashes, quotes, wildcard/negation syntax, field modifiers, vector-separator syntax, or pathological punctuation-only strings, then the generated dialect-2 query remains syntactically valid and cannot inject extra clauses, change axes, broaden to wildcard-only search, or trigger purely negative/full-index query behavior.

3. Given the semantic axis builds a hybrid vector query using a RediSearch filter before `=>[KNN ...]`, when `caseId`, `sourceType`, or `subject` contains adversarial TAG syntax, then the shared helper escapes those values consistently with the syntactic axis and Redis syntax errors are not mislabeled as `SemanticSearchDimensionMismatchException`.

4. Given adversarial inputs are sent through `/api/search` for `axis=syntactic`, `axis=semantic`, `axis=hybrid`, and graph-scoped syntactic/semantic search, when RediSearch receives the request, then the API returns successful empty/typed results or existing typed validation/degradation responses without leaking raw Redis parser messages, returning unhandled 500s, or converting user-shaped syntax failures into 503 backend outages.

5. Given Epic 20 is security remediation, when implementation finishes, then focused unit and endpoint tests prove shared escaper coverage, replacement of both old private regex helpers, safe behavior for the subject filter on both axes, metadata/attribute escaping on syntactic search, semantic KNN pre-filter escaping, hybrid/graph-scoped reuse, and no regression in existing search degradation handling.

## Tasks / Subtasks

- [x] Task 1 - Re-run audit-anchor preflight before editing (AC: 1-5)
  - [x] Confirm `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs` still has `EscapeRedisQuery` and a private `EscapeRegex` around lines 261 and 302.
  - [x] Confirm `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs` still has `EscapeTagValue` and a private `EscapeRegex` around lines 193 and 293.
  - [x] Confirm semantic search still catches all non-missing `RedisServerException` from the FT.SEARCH call as a dimension mismatch around lines 98-112.
  - [x] Confirm `/api/search` still catches only transient Redis server errors for direct syntactic search and graph/traverse paths, so parser errors from incomplete escaping can surface incorrectly if not prevented.
  - [x] Record the current commit, moved anchors, and any adaptation in the Dev Agent Record before implementation.

- [x] Task 2 - Add one shared RediSearch query escaping helper (AC: 1, 2, 3)
  - [x] Add a focused helper near the Server search code, preferably `src/Hexalith.Memories.Server/Search/RediSearchQueryEscaper.cs`, with one C# type in the file and the standard ITANEO copyright header.
  - [x] Provide context-specific APIs rather than one ambiguous method. Suggested shape: `EscapeText(...)` for TEXT/free-text terms, `EscapeTag(...)` for TAG filter values, and `EscapeTagComposite(...)` or explicit caller composition for attribute `key=value` tags.
  - [x] Cover the dialect-2 query syntax operator set used by Redis Search: field modifiers (`@`, `:`), grouping (`(`, `)`, `[`, `]`, `{`, `}`), union/negation/optional/wildcard/fuzzy/query-attributes operators (`|`, `-`, `~`, `*`, `%`, `=>`, `$`), quotes/backslash, comma, punctuation that RediSearch tokenizes as syntax, and current repo-observed operators (`=`, `!`, `^`, `?`, `"`, `'`).
  - [x] Treat whitespace intentionally. Preserve spaces where the current query semantics require multiword TEXT input, but make TAG behavior explicit for dialect 2 and avoid whitespace-driven syntax errors in tag values.
  - [x] Make the helper deterministic and allocation-conscious; do not add new package dependencies.

- [x] Task 3 - Replace both divergent search-service escapers (AC: 1, 2, 3)
  - [x] Replace `SyntacticSearchService.EscapeRedisQuery` call sites in `BuildSearchTermsQuery`, `BuildQueryString`, metadata text filtering, subject filtering, case/source filters, and attribute tag filtering with the shared helper.
  - [x] Replace `SemanticSearchService.EscapeTagValue` call sites in `BuildKnnQueryString` with the shared helper.
  - [x] Delete the old private `EscapeRegex` implementations from both services after all call sites move.
  - [x] Preserve existing natural-language OR token behavior in `BuildSearchTermsQuery`; only the escaping implementation should change.
  - [x] Preserve existing `Query(...).Dialect(2)` and vector parameterization. Do not interpolate vectors, tenant IDs, or embedding bytes into query strings.

- [x] Task 4 - Harden RediSearch parser-error behavior without hiding real backend outages (AC: 4)
  - [x] Add a narrow classifier for RediSearch query syntax/parser errors if tests prove any syntax-shaped Redis error can still occur after escaping.
  - [x] For user-shaped syntax/parser failures, return safe empty/typed search results or existing typed validation responses according to the path. Do not map these to 503 `BACKEND_UNAVAILABLE`.
  - [x] Keep transient infrastructure errors (`LOADING`, `BUSY`, `OOM`, connection failure, timeout) on the existing degradation path with retry guidance.
  - [x] Fix semantic search's broad non-missing `RedisServerException` catch so only actual vector dimension failures become `SemanticSearchDimensionMismatchException`.
  - [x] Do not log raw query text, bearer tokens, JWT payloads, source payloads, provider credentials, raw parser messages containing user input, or unbounded exception messages.

- [x] Task 5 - Add adversarial unit and endpoint coverage (AC: 1-5)
  - [x] Add or extend `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs` and `SemanticSearchServiceTests.cs` for the shared helper and both query builders.
  - [x] Include inputs containing at least: `$`, `=>`, `%`, `#`, `;`, `.`, `<`, `>`, `+`, `/`, `\`, quotes, parentheses/brackets/braces, pipe, wildcard, negation-only text, field filters such as `@content:{secret}`, tag unions, and punctuation-only strings.
  - [x] Add tests proving `subject` is escaped on both syntactic and semantic axes.
  - [x] Add tests proving `metadataQuery` and `attributeFilters` cannot inject extra fields or wildcard/negative clauses.
  - [x] Add endpoint or service-level tests for adversarial `/api/search` requests across direct syntactic, direct semantic, hybrid, and graph-scoped syntactic/semantic paths. Use existing TestServer/service test patterns where possible; avoid Docker/Aspire unless the test explicitly needs real RediSearch parser behavior.
  - [x] Extend degradation tests only where the error classification surface changes. Preserve the existing distinction between missing index as empty results and transient Redis as 503.

- [x] Task 6 - Validate focused scope (AC: 5)
  - [x] Run `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false`.
  - [x] Run focused xUnit classes with the sandbox-safe executable fallback if needed:

    ```bash
    DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.SyntacticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointDegradationTests -class Hexalith.Memories.Server.Tests.Search.HybridSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.GraphScopedSearchTests
    ```

  - [x] Run `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` or document the exact blocker.
  - [x] Run `git diff --check -- src tests _bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md _bmad-output/implementation-artifacts/sprint-status.yaml`.

## Dev Notes

Story 20.6 closes audit finding A31. The risk is not data exfiltration through Redis commands; it is user input being interpreted as RediSearch query syntax, causing parser failures, broadened/negative queries, or query-shaped denial of service that currently can escape as 503s or incorrect semantic dimension errors. The correct fix is one shared RediSearch query construction helper used by every Server search axis, with tests that make missed syntax characters obvious. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.6; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A31]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key section is Post-MVP Audit Remediation and Story 20.6 under Epic 20 API Security & Tenant Authorization.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are tenant isolation, RediSearch/Redis Vector query construction, backend portability, graceful degradation, search latency targets, and no deep Redis syntax leakage beyond explicit builders/helpers.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR14-FR23, FR34, FR63, FR66, FR67, NFR1-NFR3, NFR8, NFR15, and NFR18.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI implementation is in scope.
- Loaded persistent facts from `_bmad-output/project-context.md` and root-declared reference project-context files under `references/`.
- Loaded previous story `_bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md`.

### Audit-Anchor Preflight

Re-verified on 2026-07-04 against current `HEAD` `d9420585b56b`:

- A31 remains present in the audit evidence: two divergent, incomplete RediSearch escapers can convert user input into dialect-2 syntax errors and 503s. [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A31]
- The audit line anchors are stale because Epic 20 work moved code, but the implementation-state assumption still holds. `SyntacticSearchService` has `EscapeRedisQuery` and a private regex at current lines 261 and 302. [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs:261; src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs:302]
- `SemanticSearchService` has `EscapeTagValue` and a different private regex at current lines 193 and 293. The semantic regex omits at least the `=` character that syntactic currently escapes, and both omit `$` from the official TAG escaping set. [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs:193; src/Hexalith.Memories.Server/Search/SemanticSearchService.cs:293]
- Syntactic query construction includes user-controlled `query`, `metadataQuery`, `subject`, `caseId`, `sourceType`, and `attributeFilters` in the RediSearch query string. [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs:151-218]
- Semantic KNN query construction includes user-controlled `caseId`, `sourceType`, and `subject` before `=>[KNN ...]`. [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs:163-187]
- Semantic search currently catches every non-missing `RedisServerException` from `FT.SEARCH` and rethrows `SemanticSearchDimensionMismatchException`. This can misclassify parser errors or other RediSearch errors as vector dimension problems. [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs:98-112]
- The `/api/search` syntactic path catches only connection, timeout, and transient Redis server errors. Non-transient parser errors can still escape if the service does not prevent them. [Source: src/Hexalith.Memories.Server/Program.cs:3423-3444]

If any anchor moves before dev starts, update this section first. Epics 20-26 require current-code re-verification before implementation. [Source: _bmad-output/planning-artifacts/epics.md#Audit-anchor-preflight]

### Existing Patterns to Reuse

- Keep the helper in Server search scope for this story. `src/Hexalith.Memories.Redis` is still a placeholder and Story 25 owns broader factorization/topology cleanup; do not turn 20.6 into a project-boundary refactor. [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A45; _bmad-output/planning-artifacts/epics.md#Story-25.1]
- Reuse `SearchEndpointDegradationLog` and `SearchEndpointDegradationResponses` for typed backend failures; extend them only if the parser-error classification needs a named helper. [Source: src/Hexalith.Memories.Server/Search/SearchEndpointDegradationLog.cs; src/Hexalith.Memories.Server/Search/SearchEndpointDegradationResponses.cs]
- Reuse current search service unit test files for deterministic query-builder tests. Existing tests already cover basic escaping, subject filters, attribute filters, and semantic KNN filters; extend them rather than creating parallel test suites unless a new helper test file is clearer. [Source: tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs; tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs]
- Reuse `ErrorResponse` for any new typed endpoint response. Do not introduce anonymous or string-only error shapes. [Source: _bmad-output/project-context.md#Critical-Implementation-Rules]
- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`. [Source: _bmad-output/project-context.md#Testing-Rules]

### Architecture and Security Constraints

- Tenant isolation remains mandatory. Escaping must not remove tenant/case/subject/source filters, broaden search to wildcard-only results, or allow caller text to inject another field filter. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules; _bmad-output/planning-artifacts/prd.md#NFR8]
- Preserve RediSearch dialect 2 because current service queries explicitly call `.Dialect(2)` and query syntax semantics differ between dialects. [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs:84-87; src/Hexalith.Memories.Server/Search/SemanticSearchService.cs:83-85; Redis docs query syntax]
- Preserve graceful degradation semantics: missing indexes return empty results, transient Redis conditions return typed 503/degraded responses, and user-shaped parser input should not masquerade as infrastructure outage. [Source: tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs]
- Do not change score normalization, fusion ranking, semantic pagination, graph traversal, vector migration, or query-embedding caching in this story. Those are Epic 21/22/24/25 concerns.
- Do not log raw query values or raw Redis parser messages if they can echo user input. Audit/search telemetry must stay content-free and bounded. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules; _bmad-output/planning-artifacts/ux-design-specification.md#Security-and-privacy]
- Keep public contract JSON shapes additive-compatible. This story should not need contract changes.

### Latest Technical Notes

- Redis Search query syntax uses operators for OR (`|`), negation (`-`), wildcard (`*`), field modifiers (`@field:`), grouping, fuzzy matching (`%`), optional terms (`~`), and query attributes (`=>`). Dialect 2 changes parsing semantics, so the helper must be tested against dialect-2 query construction rather than assuming older behavior. [Source: https://redis.io/docs/latest/develop/ai/search-and-query/advanced-concepts/query_syntax/]
- Redis documents KNN vector queries as `{filter_query}=>[KNN ...]`, which is exactly the semantic path in this repo. User-controlled TAG filters before `=>` must be escaped before the vector clause is appended. [Source: https://redis.io/docs/latest/develop/ai/search-and-query/advanced-concepts/query_syntax/#vector-search]
- Redis TAG filter docs call out `$`, `{`, `}`, backslash, and `|` as characters that should be escaped in tag values; with dialect 2 or greater, spaces in TAG queries are supported, including stopwords. [Source: https://redis.io/docs/latest/develop/ai/search-and-query/advanced-concepts/query_syntax/#tag-filters]

### Expected File Touches

Likely production files:

- `src/Hexalith.Memories.Server/Search/RediSearchQueryEscaper.cs` - new shared helper for dialect-2 TEXT/TAG escaping.
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs` - replace private `EscapeRedisQuery` and `EscapeRegex` with shared helper calls.
- `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs` - replace private `EscapeTagValue` and `EscapeRegex`; narrow `RedisServerException` classification if needed.
- `src/Hexalith.Memories.Server/Search/SearchEndpointDegradationLog.cs` or `SearchEndpointDegradationResponses.cs` - update only if parser-error handling needs central classification/typed response support.
- `src/Hexalith.Memories.Server/Program.cs` - update only if endpoint-level parser-error mapping is required after service-level escaping/error classification.

Likely test files:

- `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs`
- A new helper-specific test file, for example `tests/Hexalith.Memories.Server.Tests/Search/RediSearchQueryEscaperTests.cs`, if it keeps the shared helper contract clearer.

### Scope Boundaries

- Do not implement semantic offset pagination; Story 22.1 owns A8.
- Do not implement bounded graph traversal or graph timeout changes; Story 22.2 owns A9.
- Do not implement fusion recalibration, RRF, scorer pinning, or case-attribution changes; Story 22.4 owns A30.
- Do not move search code into `Hexalith.Memories.Redis`; Story 25.8/25.1 own topology/code-health cleanup.
- Do not add Redis Stack integration tests unless needed to prove parser behavior. Prefer deterministic unit/query-builder tests plus existing TestServer endpoint patterns.
- Do not change MCP tool schemas or CLI request shapes; they already URI-escape HTTP parameters before reaching the server, but server-side RediSearch escaping remains the security boundary.

### Previous Story Intelligence

Story 20.5 completed inbound rate limiting and audit completeness, and reinforced these Epic 20 patterns:

- Re-run live-code anchors because audit line numbers are stale after each Epic 20 story.
- Keep remediation stories narrow. Do not pull Story 25 `Program.cs` decomposition or Redis project extraction into this story.
- Preserve typed `ErrorResponse` bodies and sanitized telemetry/logging. Security stories should fail closed without leaking secrets or raw user-controlled payloads.
- Use focused tests plus the `dotnet exec` xUnit v3 fallback when VSTest is blocked by sandbox TCP-listener limits.
- Keep the story File List complete during implementation; recent Epic 20 reviews caught omissions.

[Source: _bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md; git commit `d9420585b56b`]

### Git Intelligence

Recent commits show Epic 20 is in active security remediation:

- `d942058 feat(story-20.5): Inbound Rate Limiting, Quotas & Audit Completeness` added request-boundary quotas, new audit operation families, and focused Server tests.
- `e444331 feat(story-20.4): MCP Production Signing-Key Hardening` added production MCP auth validation and sanitized startup/tests.
- `ef57bd5 feat(story-20.3): Tenant-Scope Workflow & Batch Status Endpoints` added safe status projection and tenant-first status authorization.
- `ae9558f feat(story-20.2): Tenant Authorization Filter & Principal-Derived Audit Identity` added normalized tenant claims and cross-tenant denial tests.
- `b48a519 feat(story-20.1): Server Authentication Foundation` added bearer authentication, fallback authorization, and anonymous-route guardrails.

### Testing Standards

Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`. Test names should be behavior-focused PascalCase, and test folders should mirror product areas. [Source: _bmad-output/project-context.md#Testing-Rules]

For TestServer tests, keep `DiffEngine_Disabled=true` for xUnit executable runs. If `dotnet test` hits the known sandbox/VSTest TCP-listener limitation, use the built test assembly with `dotnet exec` and record the exact command and outcome. [Source: CONTRIBUTING.md#Sandbox-test-runner-workaround]

### Project Structure Notes

This story is Server search hardening only. Keep new helper code near the search services, one C# type per file, with no new package versions in `.csproj`. Preserve ITANEO copyright headers for Memories-owned `.cs` files. Do not initialize or update nested submodules.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-20.6 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A31 - RediSearch query escaping gap]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-20 - approved remediation scope]
- [Source: _bmad-output/planning-artifacts/prd.md#FR14-FR23-FR34-FR63-FR66-NFR1-NFR3-NFR8-NFR15-NFR18 - search, tenant isolation, degradation, and backend portability requirements]
- [Source: _bmad-output/planning-artifacts/architecture.md#NFR8-NFR15-RediSearch - tenant isolation and Redis-specific syntax boundaries]
- [Source: _bmad-output/project-context.md - C#, testing, package, telemetry, tenant isolation, and secrets rules]
- [Source: _bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md - previous story implementation and validation learnings]
- [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs - current syntactic query builder and escaper]
- [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs - current semantic KNN query builder and escaper]
- [Source: src/Hexalith.Memories.Server/Program.cs - current `/api/search` error routing]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs - current syntactic query builder tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs - current semantic query builder tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs - current search degradation classification]
- [Source: https://redis.io/docs/latest/develop/ai/search-and-query/advanced-concepts/query_syntax/ - Redis Search query syntax, dialect behavior, TAG filters, and vector KNN syntax]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: Audit-anchor preflight re-run against `d9420585b56b74361c1241541c07233ad2e87bea`; anchors still matched `SyntacticSearchService` old `EscapeRedisQuery`/`EscapeRegex`, `SemanticSearchService` old `EscapeTagValue`/`EscapeRegex`, broad semantic `RedisServerException` dimension mapping, and endpoint transient-only Redis catches.
- 2026-07-04: Red-phase build failed as expected before implementation because `RediSearchQueryEscaper` and `RediSearchErrorClassifier` did not exist.
- 2026-07-04: Focused Server test project build passed with `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false`.
- 2026-07-04: Focused xUnit executable run passed 164 tests, including `RediSearchQueryEscaperTests`, `SyntacticSearchServiceTests`, `SemanticSearchServiceTests`, `SearchEndpointDegradationTests`, `HybridSearchServiceTests`, and `GraphScopedSearchTests`.
- 2026-07-04: Full solution build passed with `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false`.
- 2026-07-04: `git diff --check -- src tests _bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md _bmad-output/implementation-artifacts/sprint-status.yaml` passed after removing one trailing-whitespace line.
- 2026-07-04: Solution-level `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet test Hexalith.Memories.slnx --no-build --disable-build-servers -m:1 /nr:false` was blocked by the known VSTest TCP listener sandbox error: `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-07-04: Full Server test assembly fallback passed with `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll`: 2088 total, 0 errors, 0 failed, 1 skipped.
- 2026-07-04: Story-automator senior review loaded official Redis query syntax documentation as web fallback for the checklist MCP/doc-search item; Redis docs confirmed dialect behavior, TAG escaping, and KNN filter syntax used by the implementation.
- 2026-07-04: Story-automator senior review verified AC1-AC5 against changed source/tests, found no critical code issues, and auto-fixed the incomplete Dev Agent File List.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added one shared RediSearch dialect-2 query escaper with context-specific TEXT, TAG, and attribute TAG composite methods.
- Replaced syntactic and semantic service-local query escapers and removed both old private regex helpers.
- Added bounded RediSearch error classification so parser-shaped errors return safe empty search results while transient Redis errors keep the existing 503 degradation path and only vector dimension messages map to `SemanticSearchDimensionMismatchException`.
- Added adversarial escaper/query-builder/degradation tests covering subject, metadata, attribute, KNN pre-filter, punctuation/operator, parser, and dimension-classification cases.
- Senior review confirmed the shared helper is used by syntactic and semantic query construction, vector bytes remain parameterized, parser errors are not treated as transient outages, and review-only metadata fixes were applied.

### File List

- `_bmad-output/implementation-artifacts/20-6-redisearch-query-injection-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-20-20260704-091304.md`
- `src/Hexalith.Memories.Server/Search/RediSearchErrorClassifier.cs`
- `src/Hexalith.Memories.Server/Search/RediSearchQueryEscaper.cs`
- `src/Hexalith.Memories.Server/Search/SearchEndpointDegradationLog.cs`
- `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs`
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/RediSearchQueryEscaperTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs`

### Senior Developer Review (AI)

Outcome: Approved after automatic fixes. No critical issues remain.

Issues found and fixed:

- MEDIUM: The Dev Agent File List omitted changed files that were present in git status: `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs`, `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs`, `_bmad-output/implementation-artifacts/tests/test-summary.md`, and `_bmad-output/story-automator/orchestration-20-20260704-091304.md`. Fixed by adding the missing entries to the File List.

Review notes:

- AC1-AC3 verified: `SyntacticSearchService` and `SemanticSearchService` now use `RediSearchQueryEscaper` for TEXT, TAG, attribute TAG composite, and semantic KNN pre-filter construction; the previous private regex helpers are removed.
- AC4 verified: parser-shaped Redis errors are classified separately from missing-index, transient Redis, and vector-dimension errors; sanitized parser failures return safe empty results at the service layer and are not mapped to 503 transient backend outages.
- AC5 verified: focused query-builder, escaper, hybrid, graph-scoped, and degradation tests cover the adversarial input classes required by the story.

Validation:

- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Search.RediSearchQueryEscaperTests -class Hexalith.Memories.Server.Tests.Search.SyntacticSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests -class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointDegradationTests -class Hexalith.Memories.Server.Tests.Search.HybridSearchServiceTests -class Hexalith.Memories.Server.Tests.Search.GraphScopedSearchTests` passed.
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` passed.

### Change Log

- 2026-07-04: Implemented Story 20.6 RediSearch query-injection hardening and moved story to review.
- 2026-07-04: Story-automator review verified AC1-AC5, fixed missing File List entries, and set status to done.
