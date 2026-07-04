---
baseline_commit: 1b072f4de56f8d7ab2d256eb1ce9bce650c817c8
---

# Story 21.4: Key-Schema Single Source of Truth

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want all Redis key/index names built through `IndexSchemaDefinitions`,
so that a schema rename cannot silently orphan search, consistency, or migration.

## Acceptance Criteria

1. Given >=12 hand-interpolated `:mu:`/`:vec:` sites bypass the declared single source of truth, when this story completes, then `Build{Syntactic,Semantic,NlSemantic}Key` helpers exist, all production sites use them, and a CI grep guard fails on raw `:mu:`/`:vec:` literals. Closes A44.

2. Given Story 21.3 introduced the disjoint natural-language semantic prefix and retained the legacy nested NL prefix only for migration, when this story completes, then new helpers preserve `{tenant}:vecnl:{memoryUnitId}` for current NL hashes and keep `{tenant}:vec:nl:{memoryUnitId}` reachable only through explicitly named legacy migration helpers.

3. Given search, repair, export, metrics, migration, and case workflows derive memory-unit IDs from Redis keys, when this story completes, then key parsing uses centralized helpers instead of ad hoc `IndexOf(":mu:")`, substring offsets, or duplicated prefix-length logic.

4. Given tests and integration fixtures currently hard-code production key/index names, when this story completes, then assertions either use `IndexSchemaDefinitions` helpers or are covered by a deliberate allowlist because the test is pinning an external contract string.

## Tasks / Subtasks

- [x] Task 1 - Re-run the key-literal audit before editing (AC: 1, 2, 3)
  - [x] Run a focused `rg` scan for production `":mu:"`, `":vec:"`, `":vecnl:"`, `":memories:idx"`, `":memories:vec"`, and `":memories:vec:nl"` literals under `src/`.
  - [x] Record the current commit and any moved anchors in the Dev Agent Record before changing code.
  - [x] Classify each hit as one of: replace with key builder, replace with index helper, parsing helper, migration-only legacy helper, non-memory-key namespace, documentation/comment, or allowed test contract.
  - [x] Do not modify `dedup:*`, `case:*`, `failed-unit:*`, `eventstore:*`, or `embedding-migration:*` key schemes unless required by a touched helper boundary; Stories 21.5, 21.7, 21.8, and 21.9 own adjacent namespaces.

- [x] Task 2 - Extend `IndexSchemaDefinitions` into a complete key schema API (AC: 1, 2, 3)
  - [x] Add `BuildSyntacticKey(string tenantId, string memoryUnitId)`, `BuildSemanticKey(...)`, and `BuildNaturalLanguageSemanticKey(...)`.
  - [x] Add `BuildLegacyNaturalLanguageSemanticKey(...)` or an equivalently explicit migration-only helper; keep it clearly named so new runtime writes cannot use it accidentally.
  - [x] Add `TryParseSyntacticMemoryUnitId(string tenantId, RedisKey key, out string memoryUnitId)` and `TryParseSemanticMemoryUnitId(...)` helpers, or a shared parse helper that is axis-specific at the call site.
  - [x] Ensure helpers validate/guard tenant and memory-unit input consistently with existing `TenantIdGuard` / memory-unit validation expectations without changing accepted legacy GUID lookup behavior.
  - [x] Add unit tests in `IndexSchemaDefinitionsTests` for exact key shapes, prefix non-collision, legacy-NL migration-only shape, parse success, parse rejection for foreign-tenant keys, and parse rejection for prefix-only keys.

- [x] Task 3 - Replace production raw key/index literals with schema helpers (AC: 1, 2, 3)
  - [x] Update indexing and compensation paths: `IndexSyntacticActivity`, `IndexSemanticActivity`, `CleanupSyntacticActivity`, `CheckMemoryUnitExistsActivity`, `RepairUnitActivity`, and any remaining `CleanupSemanticActivity`/NL cleanup string concatenation.
  - [x] Update consistency and repair helpers: `EnumerateMemoryUnitIdsActivity`, `ConsistencyInspectionService`, `SemanticIndexer`, `GraphNodeMerger`, `VerifyConsistencyActivity`, and graph/semantic repair code.
  - [x] Update search enrichment paths: `SyntacticSearchService`, `SemanticSearchService`, `GraphScopedSearch`, `GraphTraversalService`, and `CorpusStatisticsActor` where they address syntactic hashes or indexes.
  - [x] Update case and projection paths: `CaseService`, `DeleteMemoryUnitProjectionActivity`, `DeleteCaseProjectionActivity`, and any direct indexed-state existence checks.
  - [x] Update export and tenant metrics paths: `TenantExportService` and `TenantMetricsService`.
  - [x] Update migration paths: `RedisEmbeddingMigrationStore` and `RedisNaturalLanguageNamespaceMigrator`, preserving the legacy NL prefix only as migration input.
  - [x] Leave comments/docs readable, but avoid comments that hard-code new runtime key strings where a helper name would communicate the boundary better.

- [x] Task 4 - Add the CI-visible guard (AC: 1, 4)
  - [x] Prefer a Docker-free unit/architecture test under `tests/Hexalith.Memories.Server.Tests/Architecture/` that scans production `src/**/*.cs` for forbidden raw memory/index key literals.
  - [x] Fail on raw `:mu:`, `:vec:`, `:vecnl:`, `:memories:idx`, `:memories:vec`, and `:memories:vec:nl` literals outside `IndexSchemaDefinitions` and documented migration-only/contract exceptions.
  - [x] Include a narrow allowlist for non-memory-key namespaces or external-contract tests only; each allowlist entry must name why it is safe.
  - [x] Ensure the guard runs in the existing `test-unit-contract` CI lane via normal `dotnet test`; no new GitHub Actions job is required unless the architecture test cannot express the rule cleanly.

- [x] Task 5 - Reconcile tests and contract-pinning fixtures (AC: 1, 2, 4)
  - [x] Update focused unit tests to call key/index helpers rather than retyping production key strings.
  - [x] Keep explicit string assertions only where the test intentionally pins a public/operator-facing key shape; document that intent in the assertion message.
  - [x] Update integration tests and benchmark fixtures only as far as needed to compile and preserve existing behavior; do not broaden this story into performance work.
  - [x] Preserve Story 21.3 tests proving raw semantic prefixes do not match NL hashes and legacy NL hashes migrate before raw/NL index rebuild.

- [x] Task 6 - Validate the refactor (AC: 1, 2, 3, 4)
  - [x] Run focused tests for `IndexSchemaDefinitionsTests`, key-literal architecture guard, indexing activities, consistency inspection/repair, search enrichment, case projection deletion, tenant vector provisioning/deletion, migration store, export, and tenant metrics.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false`.
  - [x] If `dotnet test` is blocked by the sandbox TCP listener issue seen in Story 21.3, use the in-process xUnit runner for focused test classes and record the blocked VSTest command.
  - [x] Update this story's Dev Agent Record, File List, and Completion Notes.

## Dev Notes

Story 21.4 closes audit finding A44, the root cause behind the A4 natural-language prefix collision fixed by Story 21.3. This is a production key-schema consolidation story, not a key rename story: runtime key shapes should remain compatible unless a named migration path already exists. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.4; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A44]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 21 owns data integrity, consistency, deletion, migration safety, and key-schema cleanup.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are D3 EventStore source-of-truth, rebuildable Redis/FalkorDB projections, Dapr workflow compensation, physical tenant isolation, RediSearch/Vector schema evolution constraints, and testing standards.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR13, FR39, FR43, FR70, FR73, FR74, NFR8, NFR15, and NFR16-NFR19.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope, but operator-facing absence/degradation states must remain honest if a backend/index is missing.
- Loaded persistent facts from `_bmad-output/project-context.md` and root-declared reference project-context files under `references/`.
- Loaded Hexalith state instructions because this story touches persisted read-model keys; domain source of truth remains Hexalith.EventStore, while Redis/FalkorDB records are projections/read models.
- Loaded previous Stories 21.1, 21.2, and 21.3 plus recent commits `53cc9c2`, `c350b7a`, `95048df`, and `1b072f4`.

### Current State and Code Anchors

`IndexSchemaDefinitions` already owns index suffixes, key-prefix suffixes, field identifiers, FT.CREATE params, and vector schema validation. It does not yet expose full key builders or parse helpers, so callers still assemble keys by appending IDs to prefixes or retyping string templates. [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs]

Story 21.3 changed current NL semantic hashes to `{tenant}:vecnl:{memoryUnitId}` and retained `{tenant}:vec:nl:{memoryUnitId}` only as legacy migration input. The public NL index name remains `{tenant}:memories:vec:nl`. Do not collapse those distinctions. [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs; _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md]

Remaining production raw literals found during story creation include:

- `IndexSyntacticActivity` writes `"{tenant}:mu:{id}"`; `IndexSemanticActivity` writes `"{tenant}:vec:{id}"`. [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs; src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs]
- `CheckMemoryUnitExistsActivity` and `CleanupSyntacticActivity` still build syntactic keys directly. [Source: src/Hexalith.Memories.Server/Activities/Indexing/CheckMemoryUnitExistsActivity.cs; src/Hexalith.Memories.Server/Activities/Indexing/CleanupSyntacticActivity.cs]
- `EnumerateMemoryUnitIdsActivity` scans raw syntactic and semantic prefixes and slices memory-unit IDs using local prefix lengths. Replace with schema-owned prefix/parse helpers while preserving cursor-based `KeysAsync` and the graph union behavior. [Source: src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs]
- `RepairUnitActivity.DeleteVectorAsync` deletes `"{tenant}:vec:{id}"` directly. [Source: src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs]
- `SemanticIndexer` and `GraphNodeMerger` read syntactic hashes by direct string interpolation for repair re-index/re-merge. [Source: src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs; src/Hexalith.Memories.Server/Consistency/GraphNodeMerger.cs]
- `SyntacticSearchService` constructs the syntactic index name directly and strips document IDs with a local `"{tenant}:mu:"` prefix. [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs]
- `SemanticSearchService` constructs the semantic index name directly and enriches KNN results from direct syntactic hash keys. [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs]
- `GraphScopedSearch` and `GraphTraversalService` load fallback/enrichment data from direct syntactic hash keys. [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs; src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs]
- `CaseService` uses direct syntactic keys for memory-unit reads/deletion validation and direct semantic keys for indexed-state checks, even after Story 21.2 moved mutation acceptance through EventStore command boundaries and projection workflows. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs; _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]
- `TenantExportService` scans `"{tenant}:mu:*"`, parses with `IndexOf(":mu:")`, and reloads direct syntactic keys. [Source: src/Hexalith.Memories.Server/Export/TenantExportService.cs]
- `TenantMetricsService` scans direct syntactic keys for memory-unit count; `CorpusStatisticsActor` builds the syntactic index name directly. [Source: src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs; src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs]
- `RedisEmbeddingMigrationStore` still has migration marker/failure namespaces that are not part of the `:mu:`/`:vec:` story scope, while raw/NL semantic writes already mostly use `IndexSchemaDefinitions`. Do not accidentally move marker namespaces into `IndexSchemaDefinitions`. [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs]

Several 21.3-touched files already use `IndexSchemaDefinitions.Get*KeyPrefix(...) + memoryUnitId`, including `CleanupSemanticActivity`, `VerifyConsistencyActivity`, `ConsistencyInspectionService`, `DeleteMemoryUnitProjectionActivity`, `DeleteCaseProjectionActivity`, `DeleteRedisVectorActivity`, `DeleteRediSearchActivity`, `DeleteRedisVectorIndexActivity`, and `DeleteRediSearchIndexActivity`. Convert these to full key/index helpers where appropriate but preserve behavior. [Source: src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs; src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs; src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs; src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs]

### Architecture Constraints

- Domain state is sourced from Hexalith.EventStore events; Redis syntactic hashes, Redis Vector entries, FalkorDB nodes/edges, case activity streams, and tenant registry/read records are rebuildable projections/read models. Do not use this refactor to reintroduce direct Redis/FalkorDB writes as the domain source of truth. [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency; references/Hexalith.AI.Tools/hexalith-state-instructions.md]
- RediSearch and Redis Vector index schemas are deployment-sensitive. Existing data/index names should not be renamed by this story unless the migration path already exists and tests prove old data remains reachable or migrated. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints-Dependencies]
- Tenant isolation is physical and prefix/index based. Key builders must always carry the tenant ID; filters alone are not an acceptable substitute. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Dapr workflow logic must remain replay-safe. This story should only change activity/service key construction and tests; no workflow orchestration side effects or nondeterministic logic belong in workflow bodies. [Source: _bmad-output/project-context.md#Framework-Specific-Rules]
- Keep package versions centralized and unchanged. No Redis/NRedisStack, Dapr, or EventStore upgrade is in scope. [Source: _bmad-output/project-context.md#Technology-Stack-Versions]

### Previous Story Intelligence

Story 21.1 selected EventStore aggregates with rebuildable projections as the ratified consistency model. Redis keys are projection/read-model implementation details, not domain authority. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md]

Story 21.2 routed case, annotation, memory-unit deletion, and case deletion mutations through an EventStore command boundary before workflow-owned projection fan-out. Do not bypass `_commandStore.AcceptAsync(...)` or `_projectionWorkflowScheduler.ScheduleAsync(...)` while replacing keys in `CaseService` or projection activities. [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]

Story 21.3 fixed A4 by moving current NL hashes from `{tenant}:vec:nl:*` to `{tenant}:vecnl:*`, adding a legacy NL namespace migrator, and proving raw semantic scans/counts do not treat legacy NL hashes as raw semantic units. Story 21.4 must preserve those tests and migrate their literals to helpers only where doing so does not weaken the asserted external shape. [Source: _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md]

Story 21.3 validation reported that normal `dotnet test`/VSTest can be blocked in this sandbox by TCP listener permissions, while the in-process xUnit runner worked for focused classes. Use the normal test command first; if blocked, record the failure and run the in-process runner for focused classes. [Source: _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md#Debug-Log-References]

### Git Intelligence

Recent commits:

- `1b072f4 feat(story-21.3): Natural-Language Vector Namespace Separation`
- `95048df feat: Implement natural-language vector namespace separation`
- `c350b7a feat(story-21.2): Transactional Multi-Backend Mutation`
- `53cc9c2 feat(story-21.1): Consistency Model Decision`
- `8a37253 docs(epic-20): close retrospective and sync operations docs`

The current Epic 21 pattern is narrow audit remediation with explicit source anchors, focused regression tests, architecture guard tests, and story File List hygiene. Continue that pattern; do not turn this into route consolidation, Redis scanner abstraction, export pagination, or migration blue/green work.

### Latest Technical Notes

No external technical research is required for this story. It is a local schema-helper refactor on the repo-pinned stack (.NET 10, Dapr 1.18.4, NRedisStack/StackExchange.Redis through central package management). Redis `FT.CREATE PREFIX` behavior was already accounted for by Story 21.3's prefix non-collision tests; this story should preserve those guards rather than changing Redis APIs.

### Scope Boundaries

- In scope: memory-unit projection keys, raw/NL semantic keys, syntactic/semantic/NL index names, helper-owned parsing of those keys, production call-site replacement, and a CI-visible guard.
- In scope: tests and fixtures needed to preserve compilation, behavior, and external key-shape contracts.
- Out of scope: changing runtime key shapes, adding a new migration, changing dedup/sourceUri key stability contracts, changing case/activity/member key namespaces, changing EventStore aggregate-case-map cleanup, changing tenant deletion sweep coverage, or implementing `IRedisKeyScanner`.
- Out of scope: Story 21.5 deletion completeness (`eventstore:*`, `embedding-migration:*`, defensive `mu:*`/`vec:*` sweeps), Story 21.7 dedup race handling, Story 21.8 tenant registry CAS, Story 21.9 blue/green migration, Story 22 retrieval behavior, and Story 25 route/client consolidation.
- Do not initialize or update nested submodules.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Keep tests Docker-free unless they are existing integration tests already in the integration lane. [Source: _bmad-output/project-context.md#Testing-Rules]
- Add focused helper tests for exact key/index shapes and parse behavior.
- Add an architecture guard test that fails in the normal unit/contract lane if production code reintroduces raw memory/index literals outside `IndexSchemaDefinitions` and named exceptions.
- Keep external contract string assertions only when they are deliberately proving a public/operator-visible key shape.
- Run focused test classes first, then `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.4 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A44 - key literals bypassing schema single source of truth]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - approved sequencing and A44 coverage]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source-of-truth and projection model]
- [Source: _bmad-output/project-context.md - repo-wide C#, Dapr workflow, testing, package, tenant isolation, and submodule rules]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - Hexalith.EventStore persistence and projection rules]
- [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md - ratified consistency model]
- [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md - EventStore command boundary and projection workflow patterns]
- [Source: _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md - disjoint NL prefix, legacy migration, validation notes]
- [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs - current schema/prefix/index source]
- [Source: tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs - existing prefix non-collision tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs - precedent for source-scanning architecture guard tests]
- [Source: .github/workflows/ci.yml - test-unit-contract lane runs Docker-free tests]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: create-story workflow loaded BMAD skill, discovery protocol, template, checklist, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Stories 21.1-21.3, architecture audit A44, current code anchors, CI workflow, recent commits, and existing architecture guard test patterns.
- 2026-07-04: story target came from user request `21.4`; sprint status had `21-4-key-schema-single-source-of-truth: backlog` and `epic-21: in-progress`.
- 2026-07-04: no module UI work detected; UX context loaded only for operator trust/degradation constraints.
- 2026-07-04: dev-story activation loaded BMAD workflow, project-context facts, Hexalith LLM/state instructions, story 21.4, sprint status, and persisted baseline commit `1b072f4de56f8d7ab2d256eb1ce9bce650c817c8`; existing `baseline_commit` was preserved.
- 2026-07-04: pre-edit production literal audit command found raw memory/index literals in indexing activities, consistency/repair, search enrichment, case/projection paths, export, metrics, graph fallback reads, and migration; all hits were classified for builder, index helper, parse helper, or explicit legacy migration helper replacement. Adjacent `dedup:*`, `case:*`, `failed-unit:*`, `eventstore:*`, and `embedding-migration:*` schemes were not moved.
- 2026-07-04: red-phase `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~IndexSchemaDefinitionsTests --no-restore` was blocked before compile by MSBuild named-pipe/socket permission (`SocketException 13`); retry with `-m:1 /nodeReuse:false` compiled but VSTest was blocked by the sandbox TCP listener.
- 2026-07-04: focused xUnit in-process validation passed for `IndexSchemaDefinitionsTests` after helper implementation: 19 tests, 0 failed.
- 2026-07-04: focused xUnit in-process validation passed for 21 key-schema-related classes, including schema helpers, literal guard, indexing, consistency, search, graph, case, projection deletion, export, tenant metrics, and migration: 279 tests, 0 failed.
- 2026-07-04: `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` passed with 0 warnings and 0 errors.
- 2026-07-04: normal `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build -m:1 /nodeReuse:false` was blocked by VSTest TCP listener permission (`SocketException 13`); full in-process xUnit fallback passed for `Hexalith.Memories.Server.Tests`: 2,143 total, 0 failed, 1 skipped.
- 2026-07-04: final production literal audit reports only `IndexSchemaDefinitions` constants for `:mu:`, `:vec:`, `:vecnl:`, `:memories:idx`, `:memories:vec`, and `:memories:vec:nl`.
- 2026-07-04: story-automator review loaded review workflow/checklist, compared story File List to git changes, verified production source literal guard scope, and auto-fixed all confirmed review findings without prompting.
- 2026-07-04: review validation fixed helper validation consistency, expanded the architecture guard string-literal scanner for interpolated-verbatim/raw-string forms, and removed non-contract test key/index literals in favor of `IndexSchemaDefinitions` helpers.
- 2026-07-04: review `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build -m:1 /nodeReuse:false --filter "FullyQualifiedName~IndexSchemaDefinitionsTests|FullyQualifiedName~IndexSchemaLiteralGuardTests|FullyQualifiedName~EnumerateMemoryUnitIdsActivityTests|FullyQualifiedName~TenantMetricsServiceTests|FullyQualifiedName~CaseServiceTests|FullyQualifiedName~ConsistencyInspectionServiceTests|FullyQualifiedName~RedisEmbeddingMigrationStoreTests|FullyQualifiedName~RedisNaturalLanguageNamespaceMigratorTests"` was blocked by VSTest TCP listener permission (`SocketException 13`).
- 2026-07-04: review validation passed `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` with 0 warnings and 0 errors.
- 2026-07-04: review in-process xUnit fallback passed full `Hexalith.Memories.Server.Tests`: 2,158 total, 0 failed, 1 skipped.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.4 created as the A44 implementation story after Stories 21.1, 21.2, and 21.3 completed.
- Added full key-builder and parse-helper API to `IndexSchemaDefinitions`, including explicit current NL and legacy migration-only NL helpers.
- Replaced production memory-unit key/index construction across indexing, cleanup, consistency, repair, search, graph fallback, case/projection, export, metrics, EventStore integration, and embedding migration paths.
- Preserved current NL hash shape `{tenant}:vecnl:{memoryUnitId}` and kept legacy `{tenant}:vec:nl:{memoryUnitId}` reachable only through explicitly named legacy migration helpers.
- Added a Docker-free architecture guard that fails on raw production memory/index key literals outside `IndexSchemaDefinitions`.
- Updated focused tests to use schema helpers for changed key assertions while keeping exact shape checks centralized in `IndexSchemaDefinitionsTests`.
- Normal VSTest execution remains blocked in this sandbox by TCP listener permissions; in-process xUnit fallback passed the focused and full server test runs.
- Senior review auto-fixes tightened tenant validation on all schema index/prefix helpers, expanded the literal guard to catch more C# string literal forms, and reconciled non-contract test fixtures to use schema helpers.

### Senior Developer Review (AI)

Review completed with automatic fixes. Findings fixed:

- HIGH: `Get*IndexName` and `Get*KeyPrefix` helpers accepted invalid tenant IDs while full key builders validated them. Fixed by applying `TenantIdGuard` through all schema index/prefix helpers and adding validation tests.
- MEDIUM: the production literal guard missed `$@"..."` interpolated-verbatim and raw string literal forms. Fixed the scanner regex and added guard self-tests for supported C# string forms.
- MEDIUM: non-contract tests still duplicated production memory/index key strings after the refactor. Replaced those fixtures/assertions with `IndexSchemaDefinitions` helpers, leaving exact shape pins centralized in `IndexSchemaDefinitionsTests` and guard self-tests.

Outcome: approved; no critical issues remain.

### File List

- `_bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/CheckMemoryUnitExistsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/CleanupSyntacticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs`
- `src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs`
- `src/Hexalith.Memories.Server/Cases/CaseService.cs`
- `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs`
- `src/Hexalith.Memories.Server/Consistency/GraphNodeMerger.cs`
- `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/RedisSearchIndexMaintenanceAdapter.cs`
- `src/Hexalith.Memories.Server/Export/TenantExportService.cs`
- `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs`
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`
- `src/Hexalith.Memories.Server/Migration/RedisNaturalLanguageNamespaceMigrator.cs`
- `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs`
- `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs`
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Cases/DeleteMemoryUnitProjectionActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/CheckMemoryUnitExistsActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/CleanupActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/VerifyConsistencyActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteRediSearchActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteRedisVectorActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRediSearchActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRedisVectorActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/VerifyTenantActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Architecture/IndexSchemaLiteralGuardTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/RedisSearchIndexMaintenanceAdapterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Export/TenantExportServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/RediSearchHealthCheckTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Hosting/OrphanSemanticIndexReconcilerTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisNaturalLanguageNamespaceMigratorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantMetricsServiceTests.cs`

### Change Log

- 2026-07-04: Implemented key-schema single source of truth for memory-unit and vector keys, added parsing helpers and production literal guard, replaced production raw key/index literals, reconciled focused tests, and validated with solution build plus xUnit in-process fallback.
- 2026-07-04: Senior developer review auto-fixes applied for schema helper tenant validation, architecture guard scanner coverage, non-contract test literal cleanup, story status, and sprint status sync.
