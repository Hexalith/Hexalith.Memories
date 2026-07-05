---
baseline_commit: 1501a51
---

# Story 23.7: Index-Provisioning Ownership

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want index existence verified once per tenant, not per document,
so that ingestion does not block threads or spam warnings.

Story 23.7 follows Story 23.9, Story 23.1, Story 23.2, Story 23.3, Story 23.4, Story 23.5, and Story 23.6 in `story_execution_order.epic-23`. Provider strategy, chunked batch embedding, claim-check payloads, durable provider 429 retry, non-URL re-ingestion, single-operation embedding admission, and directory batch scalability are already done. This story closes A34 by moving per-document index readiness work out of the hot path while preserving tenant-provisioning ownership.

## Acceptance Criteria

1. Indexing activities stop creating indexes per document. Given `IndexSyntacticActivity.RunAsync`, `IndexSemanticActivity.RunAsync`, `IndexSemanticChunksActivity.RunAsync`, and `IndexNaturalLanguageSemanticActivity.RunAsync` currently call `FT.CREATE` in the write path and catch "Index already exists", when documents are indexed, then these activities no longer issue `FT.CREATE` on every run. `TenantProvisioningWorkflow` remains the owner of RediSearch, raw semantic Redis Vector, and natural-language semantic index creation.

2. Readiness is memoized per tenant, index family, expected schema, and process. Given one process can index many documents for the same tenant, when the first document for a tenant/index family is written, then a readiness component verifies the existing index once and caches success for that tenant/index/schema tuple. Subsequent writes in the same process skip readiness I/O and go straight to hash writes unless the cache entry is invalidated or the process restarts.

3. The readiness check validates schema, not only existence. Given existing indexes can drift in prefix, field list, or vector dimensions, when readiness is checked, then syntactic prefix/fields, raw semantic prefix/fields/dimensions, and natural-language semantic prefix/fields/dimensions are validated using `IndexSchemaDefinitions` helpers. Safe in-place upgrades for known additive TAG fields (`cloudeventSubject`, `attributeTags`) may remain, but incompatible schema drift still fails before any hash/vector write.

4. Blocking sleep is removed. Given `IndexSyntacticActivity.EnsureSyntacticIndexReady` currently retries incomplete `FT.INFO` metadata with `Thread.Sleep(IndexInfoRetryDelay)`, when transient incomplete metadata is handled, then the implementation uses asynchronous `Task.Delay(..., cancellationToken)` or a non-blocking equivalent. No production indexing readiness path may call `Thread.Sleep`.

5. Per-ingest warning noise is removed. Given the current hot path logs a Warning whenever `FT.CREATE` reports "Index already exists", when indexing a correctly provisioned active tenant, then repeated document indexing does not emit "index already exists" warnings. Successful first readiness checks should be silent or low-cardinality Debug/Information logs; incompatible schema remains Error/exception-worthy.

6. Missing index behavior is explicit and tenant-safe. Given ingestion should run only for active, provisioned tenants, when a required tenant index is missing at readiness time, then the activity fails with a clear, structured exception message naming the tenant and index family without creating the index on demand. Do not hide a missing provisioning step by creating indexes from ingestion code.

7. Curated EventStore search maintenance is reconciled. Given `RedisSearchIndexMaintenanceAdapter.EnsureSyntacticIndexExists` also performs create-if-missing before every curated search-index upsert, when this story completes, then either this adapter reuses the same readiness/memoization policy or the story documents and tests why its active-tenant safety-net behavior is intentionally excluded. Do not leave an unexamined duplicate A34 pattern.

8. Tenant lifecycle and migration semantics are preserved. Given tenant provisioning, tenant deletion, embedding migration, and migration-marker write-block checks already own lifecycle and write safety, when readiness memoization is added, then it does not cache across tenants, does not survive process restart, does not bypass active migration marker checks in semantic activities, and does not prevent deletion/re-provisioning from failing clearly if an old process still sees a stale cache entry.

9. Existing hash/vector write contracts remain unchanged. Given prior Epic 23 stories changed workflow payloads and semantic chunk storage, when this story completes, then syntactic hashes, semantic chunk hashes, natural-language semantic hashes, field names, source-byte claim-check resolution, migration marker enforcement, and search behavior remain compatible.

10. Tests prove A34 is closed. Given A34 names per-document `FT.CREATE`, warning spam, and `Thread.Sleep`, when the story completes, then focused tests prove one readiness check per tenant/index family per process, no second-document `FT.CREATE`, no Warning on existing healthy indexes, async retry/no `Thread.Sleep` hot-path behavior, missing-index failure, incompatible-schema failure, additive-field upgrade behavior where retained, and unchanged hash writes after readiness passes.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A34 and current index hot paths before editing (AC: 1-10)
  - [x] Read `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs` completely and confirm the current `FT.CREATE` catch/warning and `Thread.Sleep` retry behavior.
  - [x] Read `IndexSemanticActivity.cs`, `IndexSemanticChunksActivity.cs`, and `IndexNaturalLanguageSemanticActivity.cs` completely and identify every per-run `FT.CREATE` / "Index already exists" path.
  - [x] Read `ProvisionRediSearchActivity.cs`, `ProvisionRedisVectorActivity.cs`, `TenantProvisioningWorkflow.cs`, and `IndexSchemaDefinitions.cs` to preserve tenant-provisioning ownership and schema helpers.
  - [x] Read `RedisSearchIndexMaintenanceAdapter.cs` and decide whether its curated syntactic index safety net must be brought under the same memoized readiness policy.
  - [x] Read existing tests for indexing/provisioning: `IndexSyntacticActivityTests`, `IndexSemanticActivityTests`, `IndexNaturalLanguageSemanticActivityTests`, `ProvisionRediSearchActivityTests`, `ProvisionRedisVectorActivityTests`, and Redis-backed indexing integration tests.

- [x] Task 2 - Add a focused index-readiness abstraction (AC: 1-6, 8, 10)
  - [x] Introduce a small singleton service such as `ITenantIndexReadinessVerifier` / `TenantIndexReadinessVerifier` under `Infrastructure` or `Activities/Indexing`; keep one C# type per file.
  - [x] Key memoization by tenant id, index family (`syntactic`, `semantic`, `semantic-nl`), and schema-sensitive values such as vector dimensions where applicable.
  - [x] Use `ConcurrentDictionary` or an equivalent concurrency-safe cache so parallel first writes for the same tenant/index family coalesce or remain safe without unbounded duplicate checks.
  - [x] Keep cache entries process-local only. Do not persist readiness state to Redis, Dapr state, actors, or static cross-test state.
  - [x] Register the verifier in `Program.cs` through existing DI patterns. Do not add packages or package versions.

- [x] Task 3 - Move schema validation into the readiness verifier (AC: 2-6, 10)
  - [x] For syntactic readiness, use `FT.INFO` and `IndexSchemaDefinitions.GetIndexPrefixes`, `GetAttributeIdentifiers`, and `GetSyntacticFieldIdentifiers` to validate the existing index.
  - [x] For semantic and natural-language readiness, use `IndexSchemaDefinitions.DescribeVectorSchemaProblems(...)` or equivalent shared helpers to validate prefix, field list, and dimensions.
  - [x] Preserve the safe additive TAG-field upgrade behavior currently used for `cloudeventSubject` and `attributeTags` only if the existing tests prove it is still required. Keep any upgrade before the cache entry is marked ready.
  - [x] Replace incomplete `FT.INFO` metadata retry with an asynchronous retry helper using `Task.Delay` and a cancellation token. Do not use `Thread.Sleep`.
  - [x] Create a narrow exception type or structured message for missing/incompatible indexes so workflow failures remain actionable and non-secret.

- [x] Task 4 - Remove per-document index creation from indexing activities (AC: 1, 4-6, 8-10)
  - [x] Update `IndexSyntacticActivity` to call the readiness verifier before the hash write and delete the direct `ft.Create(...)` / "Index already exists" warning path.
  - [x] Update `IndexSemanticActivity` similarly while preserving active migration marker reads and vector dimension validation.
  - [x] Update `IndexSemanticChunksActivity` similarly for raw semantic chunk writes; verify it keeps chunk ordering, claim-check reads, and per-chunk hash fields unchanged.
  - [x] Update `IndexNaturalLanguageSemanticActivity` similarly for the NL semantic index and key family.
  - [x] Ensure all activities continue to validate tenant id, memory-unit id, case id, payload references, vectors, and migration markers as they do today.

- [x] Task 5 - Reconcile curated EventStore index maintenance (AC: 7, 9-10)
  - [x] If `RedisSearchIndexMaintenanceAdapter` remains in scope, inject/reuse the readiness verifier before curated hash upserts and remove per-entry create-if-missing.
  - [x] If intentionally excluded, add a short story note in the Dev Agent Record explaining the different lifecycle/routing guarantee and add or preserve tests that make the exclusion explicit.
  - [x] Keep curated search writes using `IndexSchemaDefinitions.BuildSyntacticKey` and the same field set consumed by `SyntacticSearchService`.

- [x] Task 6 - Update tests for memoization, schema validation, and logging (AC: 1-10)
  - [x] Add unit tests for the readiness verifier: first check validates with `FT.INFO`, second same tenant/index skips Redis calls, different tenant checks separately, different dimensions check separately, missing index fails, incompatible schema fails, and allowed additive fields are upgraded before cache.
  - [x] Update `IndexSyntacticActivityTests`, `IndexSemanticActivityTests`, `IndexSemanticChunksActivity` coverage, and `IndexNaturalLanguageSemanticActivityTests` so they assert readiness verifier use and unchanged `HashSetAsync` writes.
  - [x] Add a test that repeated healthy-index writes do not call `FT.CREATE` and do not log Warning-level "already exists" messages.
  - [x] Add or update a source-level guard test that production code under `src/Hexalith.Memories.Server/Activities/Indexing` does not contain `Thread.Sleep`.
  - [x] Preserve Redis-backed integration tests for syntactic and semantic indexing; update setup to provision indexes explicitly before activity writes if needed.

- [x] Task 7 - Focused validation evidence (AC: 1-10)
  - [x] Run `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 tests for indexing activities, readiness verifier, provisioning activities, and EventStore maintenance adapter tests if touched. If VSTest is blocked by the known sandbox TCP-listener issue, use the established `DiffEngine_Disabled=true dotnet exec ...Hexalith.Memories.Server.Tests.dll` fallback and record exact counts.
  - [x] Run Redis-backed indexing integration tests if Docker/RedisStack is available; otherwise record the exact blocker and rely on unit-level `FT.INFO`/schema behavior tests.
  - [x] Run `git diff --check`.

## Dev Notes

### Current State and Code Anchors

`IndexSyntacticActivity.RunAsync` currently calls `db.FT().Create(IndexSchemaDefinitions.GetSyntacticIndexName(...), ...)` on every indexed document. On "Index already exists" it calls `EnsureSyntacticIndexReady(...)` and logs a Warning for an expected healthy tenant state. That is the primary A34 hot path. [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`; `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A34`]

`IndexSyntacticActivity.EnsureSyntacticIndexReady` retries incomplete `FT.INFO` metadata up to 10 times with `Thread.Sleep(100ms)`. Replace this with an async delay or remove the retry if the new verifier design makes it unnecessary. Blocking worker threads during ingestion is the exact failure mode A34 calls out. [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`]

`IndexSemanticActivity`, `IndexSemanticChunksActivity`, and `IndexNaturalLanguageSemanticActivity` each perform `FT.CREATE` in the indexing path and catch "Index already exists". The post-23.1 production workflow writes raw payload vectors through `IndexSemanticChunksActivity`; `IndexSemanticActivity` remains used by tests, search setup, older paths, or compatibility callers. Treat all of them deliberately rather than only fixing syntactic indexing. [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`; `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs`; `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`]

`TenantProvisioningWorkflow` provisions tenant backends in order: `ProvisionRediSearchActivity`, `ProvisionRedisVectorActivity`, `ProvisionFalkorDbActivity`, then `VerifyTenantActivity`, then marks the tenant Active. Ingestion/indexing should assume this lifecycle instead of creating infrastructure on demand. [Source: `src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs`; `_bmad-output/planning-artifacts/prd.md#Implementation-Sequencing`]

`ProvisionRediSearchActivity` and `ProvisionRedisVectorActivity` remain the index creation owners. They are intentionally idempotent on "Index already exists" during tenant provisioning and validate matching schema before returning success. Do not remove their provisioning-time `FT.CREATE` behavior. [Source: `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRediSearchActivity.cs`; `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs`]

`IndexSchemaDefinitions` is the single source of truth for index names, prefixes, key shapes, syntactic field identifiers, semantic field identifiers, natural-language field identifiers, vector dimensions parsing, and allowed additive tag upgrades. Reuse it; do not reintroduce string literals for `:mu:`, `:vec:`, `:vecnl:`, or `:memories:*` names. [Source: `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`]

`RedisSearchIndexMaintenanceAdapter` is a second syntactic hot path for curated EventStore/Tenants search-index entries. It currently has a create-if-missing safety net in `EnsureSyntacticIndexExists` before each upsert. This may share the same A34 symptoms even though it is not the normal ingestion workflow. Reconcile it explicitly. [Source: `src/Hexalith.Memories.Server/EventStoreIntegration/RedisSearchIndexMaintenanceAdapter.cs`]

Existing tests are built around the old create-if-exists behavior. `IndexSyntacticActivityTests.RunAsync_IndexInfoTemporarilyIncomplete_ShouldRetryBeforeHashWrite` currently exercises the `Thread.Sleep` retry path; `IndexSemanticActivityTests` and `IndexNaturalLanguageSemanticActivityTests` set up `FT.CREATE` throwing "Index already exists"; `Provision*ActivityTests` pin provisioning-time idempotency. Update activity tests without weakening provisioning tests. [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`; `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs`; `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs`; `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRediSearchActivityTests.cs`; `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRedisVectorActivityTests.cs`]

### Architecture Constraints

- Tenant infrastructure lifecycle belongs to tenant provisioning, not document indexing. Missing indexes after a tenant is Active are an operational inconsistency; fail clearly rather than silently creating indexes from ingestion. [Source: `_bmad-output/planning-artifacts/prd.md#Implementation-Sequencing`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04-rerun.md#Resource-creation-timing`]
- Tenant isolation remains physical and prefix/index-scoped. Readiness cache keys must include tenant id and index family and must never allow a successful tenant A readiness check to authorize tenant B writes. [Source: `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Workflow orchestration remains replay-safe. This story should touch activities/services and DI; do not add Redis/Dapr state reads or mutable process cache reads inside `IngestionWorkflow`. [Source: `_bmad-output/project-context.md#Framework-Specific-Rules`]
- Active embedding migration marker checks stay in semantic indexing activities. Readiness memoization is only about index existence/schema; it must not certify provider/model/dimension write safety. [Source: `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs`; `_bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md`]
- No dependency upgrade is required. Use .NET 10/C# 14, existing StackExchange.Redis/NRedisStack APIs, central package management, xUnit v3, Shouldly, and NSubstitute. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`]

### Previous Story Intelligence

Story 23.9 is done. Provider-specific transport/auth/format behavior is behind `EmbeddingClient` provider strategies; index readiness work must not move provider logic into indexing activities. [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]

Story 23.1 is done. Raw payload semantic writes now use `IndexSemanticChunksActivity` and chunk keys built with `IndexSchemaDefinitions.BuildSemanticChunkKey(...)`; do not collapse chunked writes back to one vector or change search dedupe contracts. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`]

Story 23.2 is done. `IndexSyntacticActivity` resolves extracted text from `WorkflowPayloadReference` when present, and `IndexSemanticChunksActivity` resolves chunk text/vector references inside the activity. Readiness refactoring must not re-inline large payloads into workflow history or bypass payload-store validation. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]

Story 23.3 is done. Provider 429s use workflow-owned durable timers, and local rate-limit window math was corrected. Index readiness must not add blocking waits or host-local retry loops that interfere with durable retry semantics. [Source: `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md`]

Story 23.4 is done. Failed non-URL re-ingestion relies on retained source-byte payload references and clear unsupported legacy errors. Readiness failures should be surfaced as indexing/provisioning problems, not as content-source failures. [Source: `_bmad-output/implementation-artifacts/23-4-non-url-re-ingestion.md`]

Story 23.5 is done. Embedding admission now uses a single actor operation per provider call/batch plus a tenant embedding config cache. Do not couple the index-readiness cache to embedding config caching; they have different invalidation and safety semantics. [Source: `_bmad-output/implementation-artifacts/23-5-rate-limiter-admission-simplification.md`]

Story 23.6 is done. Directory ingestion now uses bounded parallel scheduling and checkpointed state. This can increase concurrent first writes for the same tenant, so the readiness verifier must be concurrency-safe and avoid a thundering herd of duplicate `FT.INFO` checks. [Source: `_bmad-output/implementation-artifacts/23-6-directory-batch-scalability.md`]

Carry-forward from Epic 22 still applies: retrieval and search tests are sensitive to Redis response parsing and key-shape behavior. Keep raw Redis response parsing on the review checklist for this story because it changes readiness around RediSearch/Redis Vector indexes. [Source: `_bmad-output/implementation-artifacts/epic-22-retro-2026-07-05.md`; `_bmad-output/implementation-artifacts/sprint-status.yaml#action_items`]

### Git Intelligence

Recent commits before story creation:

- `1501a51 feat(story-23.6): Directory-Batch Scalability`
- `8eaae08 feat: Implement directory-batch scalability and enhance Evidence Packet metadata`
- `8dced2b feat(story-23.5): Rate-Limiter Admission Simplification`
- `54f6292 feat(story-23.4): Non-URL Re-Ingestion`
- `1ef8a18 feat(references): update Hexalith.FrontComposer subproject commit`

Pattern: Epic 23 stories are source-anchored, tightly scoped to one audit finding, reuse existing seams, and validate with focused server tests plus xUnit v3 fallback when VSTest is blocked. Continue that pattern.

### Latest Technical / Library Notes

- No external package/API research changes this story. The repository-pinned stack is authoritative: .NET 10/C# 14, Dapr 1.18.4, StackExchange.Redis/NRedisStack, central package management, xUnit v3, Shouldly, and NSubstitute. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`]
- Use asynchronous BCL primitives already available in .NET for non-blocking retry. Do not add Polly, TPL Dataflow, or new caching packages for this narrow readiness cache unless there is a repo-approved reason. [Source: `_bmad-output/project-context.md#Code-Quality-Style-Rules`]

### Scope Boundaries

In scope:

- Memoized per-process readiness verification for syntactic, raw semantic, and natural-language semantic indexes.
- Removing per-document `FT.CREATE`, "Index already exists" warning noise, and `Thread.Sleep` from indexing activities.
- Schema validation using `IndexSchemaDefinitions`.
- Concurrency-safe first-write behavior for the same tenant/index family.
- Reconciling or explicitly excluding `RedisSearchIndexMaintenanceAdapter`.
- Focused tests and Redis-backed validation where available.

Out of scope:

- Tenant provisioning/deletion workflow redesign.
- Changing index names, key prefixes, field sets, vector dimensions, semantic chunk key shape, natural-language key shape, migration marker semantics, or search ranking.
- Provider strategy, chunking, claim-check payload design, durable 429 timers, re-ingestion source retention, rate-limiter admission, directory scheduling, workflow config determinism, MCP/CLI/Web work, package upgrades, and submodule updates.
- Persisting readiness cache state or using it as tenant active-status authorization.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`.
- Tests belong under matching folders: `Activities/Indexing`, `Activities/Tenants`, `Infrastructure`, and `EventStoreIntegration` if that adapter changes.
- Unit tests should not require live Redis; mock `IDatabase.Execute`/`HashSetAsync` and logger behavior where possible.
- Redis-backed integration tests should use existing `RedisStack` fixtures and remain explicit about infrastructure assumptions.
- If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the established xUnit v3 in-process fallback and record exact commands/counts.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-23.7` - story statement and A34 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A34` - finding: per-document `FT.CREATE`, Warning log, `Thread.Sleep`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23` - approved A34 remediation scope]
- [Source: `_bmad-output/planning-artifacts/prd.md#Implementation-Sequencing` - tenant provisioning owns infrastructure before ingestion/indexing writes]
- [Source: `_bmad-output/project-context.md` - .NET 10/C# 14, Dapr, Redis/FalkorDB, testing, workflow, and tenant-isolation rules]
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs` - current syntactic hot path and blocking retry]
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` - compatibility raw semantic activity]
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs` - current post-23.1 raw chunk semantic write path]
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs` - NL semantic write path]
- [Source: `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRediSearchActivity.cs` - RediSearch provisioning owner]
- [Source: `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs` - raw/NL Redis Vector provisioning owner]
- [Source: `src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs` - tenant backend lifecycle]
- [Source: `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` - schema/key helper source of truth]
- [Source: `src/Hexalith.Memories.Server/EventStoreIntegration/RedisSearchIndexMaintenanceAdapter.cs` - curated syntactic index maintenance path]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs` - current syntactic activity tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs` - current semantic activity tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs` - current NL activity tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRediSearchActivityTests.cs` - provisioning idempotency tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRedisVectorActivityTests.cs` - vector provisioning idempotency tests]
- [Source: `tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSyntacticIntegrationTests.cs` - Redis-backed syntactic indexing proof]
- [Source: `tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSemanticIntegrationTests.cs` - Redis-backed semantic/chunk indexing proof]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (create-story) · Claude Opus 4.8 (dev-story)

### Debug Log References

- 2026-07-05: Loaded repository AGENTS instructions from user prompt and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.
- 2026-07-05: Used `.agents/skills/bmad-create-story/SKILL.md`; loaded `discover-inputs.md`, `template.md`, and `checklist.md`.
- 2026-07-05: Resolved workflow customization with `_bmad/scripts/resolve_customization.py`; activation prepend/append steps were empty, persistent facts were `file:{project-root}/**/project-context.md`, and `workflow.on_complete` was empty.
- 2026-07-05: Loaded BMM config: user `Jerome`, project `memories`, planning artifacts `_bmad-output/planning-artifacts`, implementation artifacts `_bmad-output/implementation-artifacts`, English communication/output.
- 2026-07-05: Target story supplied by user as `23.7`; selected story key `23-7-index-provisioning-ownership`.
- 2026-07-05: Confirmed sprint status before creation: `epic-23: in-progress`; `23-1`, `23-2`, `23-3`, `23-4`, `23-5`, `23-6`, and `23-9` done; `23-7` backlog.
- 2026-07-05: Loaded project context plus root-declared reference project contexts, Epic 23 source, A34 audit finding, sprint-change proposal, PRD/readiness notes for tenant provisioning ownership, architecture index/provisioning sections, current indexing/provisioning source files, current indexing/provisioning tests, previous Epic 23 story files, and recent git commits.
- 2026-07-05: Discovery results: no sharded planning directories were present; loaded relevant sections from `_bmad-output/planning-artifacts/epics.md`, `architecture.md`, `prd.md`, `sprint-change-proposal-2026-07-04.md`, `implementation-readiness-report-2026-07-04-rerun.md`, and `research/architecture-audit-2026-07-04.md`, plus `_bmad-output/project-context.md` and prior story files.
- 2026-07-05: Validation pass applied checklist concerns: preserved tenant provisioning as the creation owner, included all indexing siblings rather than syntactic only, required async replacement for `Thread.Sleep`, required warning-noise removal, required schema validation before cache success, required concurrency-safe memoization after Story 23.6 bounded parallel scheduling, and forced a decision on the duplicate curated EventStore maintenance path.
- 2026-07-05 (dev-story): Read all A34 anchors — the four indexing activities, `IndexSchemaDefinitions`, `Provision{RediSearch,RedisVector}Activity`, `TenantProvisioningWorkflow`, `RedisSearchIndexMaintenanceAdapter`, and their tests — before editing.
- 2026-07-05 (dev-story): `dotnet build src/Hexalith.Memories.Server` → Build succeeded, 0 Warnings, 0 Errors.
- 2026-07-05 (dev-story): `dotnet build tests/Hexalith.Memories.Server.Tests` → Build succeeded, 0 Warnings, 0 Errors.
- 2026-07-05 (dev-story): VSTest is blocked by the known sandbox TCP-listener issue, so ran the established xUnit v3 in-process fallback `DiffEngine_Disabled=true dotnet exec Hexalith.Memories.Server.Tests.dll`. Focused readiness/indexing/adapter/guard classes: 47 passed, 0 failed. Full Server.Tests suite: Total 2404, Failed 0, Errors 0, Skipped 1 (pre-existing `SubmoduleGuardTests` skip, unrelated).
- 2026-07-05 (dev-story): `git diff --check` → clean (exit 0). Note: this repo's default git config flags every CR-at-EOL as trailing whitespace (a consistent-CRLF probe file trips it too, and several committed files such as `Program.cs` are already pure-LF), so all touched `.cs` files were normalized to LF to keep the whitespace check clean.
- 2026-07-05 (dev-story): Redis-backed integration tests were NOT run — exact blocker: the `Hexalith.Memories.IntegrationTests` project transitively builds `Hexalith.Memories.AppHost`, which fails to compile with a pre-existing `CS0234: 'EventStore' does not exist in the namespace 'Hexalith'` (`Hexalith.EventStore.Aspire`) error unrelated to Story 23.7; Docker/RedisStack is also unavailable in the sandbox. Per Task 7, relied on unit-level `FT.INFO`/schema behavior tests.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story is ready for dev-story implementation.
- ✅ AC1/AC5: All four indexing activities (`IndexSyntacticActivity`, `IndexSemanticActivity`, `IndexSemanticChunksActivity`, `IndexNaturalLanguageSemanticActivity`) no longer issue `FT.CREATE` or the "index already exists" Warning on the write path. Creation stays owned by `TenantProvisioningWorkflow` / `Provision{RediSearch,RedisVector}Activity` (their provisioning-time idempotent `FT.CREATE` is untouched).
- ✅ AC2/AC8: Added `ITenantIndexReadinessVerifier` / `TenantIndexReadinessVerifier` (singleton, `ConcurrentDictionary<Lazy<Task>>`) memoizing readiness per `(tenantId, family, dimensions)`. A `Lazy<Task>` coalesces concurrent first writes (Story 23.6 parallelism) into one `FT.INFO`. Cache is process-local only — no Redis/Dapr/actor/static state — and failed checks are evicted so a later write re-verifies (stale-entry safety). Semantic activities still run the embedding migration-marker check on every invocation, before and independent of the readiness cache.
- ✅ AC3/AC4: Schema validation reuses `IndexSchemaDefinitions` helpers (prefix/fields for syntactic, `TryGetVectorDimensions` + fields/prefix for vector families). Safe additive TAG-field upgrades preserved (`cloudeventSubject` + `attributeTags` for syntactic, `cloudeventSubject` for raw semantic) before caching. The blocking `Thread.Sleep` incomplete-`FT.INFO` retry is replaced with async `Task.Delay(..., cancellationToken)`.
- ✅ AC6: Missing index at readiness time throws the structured `TenantIndexNotProvisionedException` (names tenant + family, no secrets) instead of creating on demand; incompatible drift throws `TenantIndexSchemaMismatchException : InvalidOperationException` (message retains "does not match the expected tenant schema").
- ✅ AC7 (Task 5): `RedisSearchIndexMaintenanceAdapter` reconciled onto the same verifier — its per-upsert create-if-missing safety net was removed; it now verifies readiness once (memoized) before curated hash upserts.
- ✅ AC9: Hash/vector write contracts unchanged — field sets, keys, migration-marker enforcement, and claim-check resolution are byte-identical; only the create/verify seam changed.
- ✅ AC10 (Task 6): New `TenantIndexReadinessVerifierTests` (13 cases: verify-once, per-tenant/per-dimension isolation, missing → NotProvisioned, incompatible → SchemaMismatch, additive upgrade before cache, thundering-herd single check, failure-not-cached retry, invalid inputs). Activity tests assert no second-document `FT.CREATE` + single readiness check; adapter tests assert no create-if-missing + missing-index failure; `IndexingHotPathGuardTests` guards against reintroducing `Thread.Sleep` / per-document `FT.CREATE` in the hot path.
- ⚠️ Integration tests: `SemanticSearchIntegrationTests` and `GraphScopedSearchIntegrationTests` (raw-Redis) were updated to explicitly provision tenant indexes before activity writes (mirroring `TenantProvisioningWorkflow`); `AspireEndToEndTraceTests` needs no change (it provisions via the real `POST /api/tenants` workflow). These edits could NOT be compiled or run here due to the pre-existing AppHost `Hexalith.EventStore.Aspire` build blocker + Docker unavailability, and are therefore unvalidated in this environment — they must be verified where the IntegrationTests project builds and RedisStack is available.

### File List

- `_bmad-output/implementation-artifacts/23-7-index-provisioning-ownership.md`
- `src/Hexalith.Memories.Server/Infrastructure/TenantIndexFamily.cs` (new)
- `src/Hexalith.Memories.Server/Infrastructure/ITenantIndexReadinessVerifier.cs` (new)
- `src/Hexalith.Memories.Server/Infrastructure/TenantIndexReadinessVerifier.cs` (new)
- `src/Hexalith.Memories.Server/Infrastructure/TenantIndexReadinessException.cs` (new)
- `src/Hexalith.Memories.Server/Infrastructure/TenantIndexNotProvisionedException.cs` (new)
- `src/Hexalith.Memories.Server/Infrastructure/TenantIndexSchemaMismatchException.cs` (new)
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/RedisSearchIndexMaintenanceAdapter.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/TenantIndexReadinessVerifierTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Architecture/IndexingHotPathGuardTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticChunksActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/RedisSearchIndexMaintenanceAdapterTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSyntacticIntegrationTests.cs` (unvalidated — see Completion Notes)
- `tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSemanticIntegrationTests.cs` (unvalidated — see Completion Notes)
- `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchIntegrationTests.cs` (unvalidated — see Completion Notes)
- `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` (unvalidated — see Completion Notes)

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-05

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- [Medium] `IndexSemanticChunksActivity` was marked covered, but Server.Tests had no focused unit test proving readiness verification before chunk hash writes or missing-index failure before writes. Added `IndexSemanticChunksActivityTests` with ready-index and missing-index cases.
- [Medium] The story File List omitted changed integration indexing files discovered by git (`IndexSyntacticIntegrationTests.cs`, `IndexSemanticIntegrationTests.cs`). Added them to the File List so review evidence matches the actual diff.
- [Low] Touched syntactic source/test files lacked the standard ITANEO copyright header required by project context. Added headers to `IndexSyntacticActivity.cs` and `IndexSyntacticActivityTests.cs`.
- [Low] `RedisSearchIndexMaintenanceAdapterTests` retained a stale helper comment describing the removed create-if-missing behavior. Updated it to describe FT.INFO readiness verification.

Validation:

- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -> passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -> passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class ...` focused readiness/indexing/adapter/guard slice -> 49 passed, 0 failed, 0 skipped.
- MCP resource search returned no configured resources; no external package/API research was required for this repository-pinned implementation review.
- Redis-backed integration tests were not run in this review environment; the story already records the AppHost `Hexalith.EventStore.Aspire` build blocker and Docker/RedisStack unavailability.
- `git diff --check` was rerun after normalizing modified sprint-status line endings and passed.

## Change Log

- 2026-07-05: Created Story 23.7 via BMAD create-story workflow and set status to `ready-for-dev`.
- 2026-07-05: Implemented A34 index-provisioning ownership — introduced memoized `ITenantIndexReadinessVerifier`, removed per-document `FT.CREATE`/warning-noise/`Thread.Sleep` from the four indexing activities and the curated EventStore maintenance adapter, added structured missing/incompatible-index exceptions, and added verifier + activity + adapter + source-guard tests. Server + Server.Tests build clean; 2404 Server.Tests pass. Status set to `review`.
- 2026-07-05: Senior developer review completed with automatic fixes (chunk activity unit coverage, File List hygiene, touched-file headers, stale adapter test comment). Story status set to `done`.
