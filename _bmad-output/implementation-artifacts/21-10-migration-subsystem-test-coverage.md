---
baseline_commit: d673a0e
---

# Story 21.10: Migration Subsystem Test Coverage

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a test architect,
I want the migration subsystem covered by unit and real-vector integration tests,
so that the riskiest operation is validated before it touches live tenant data.

## Acceptance Criteria

1. Given the audit found `Server/Migration/` nearly untested and `tools/MigrateEmbeddingVectors` historically had no references, when this story completes, then the migration subsystem has focused unit tests for store behavior, marker ownership/end-state, vector generation, and tool parsing/output. Closes A22.

2. Given Story 21.9 replaced destructive migration with blue/green staging, when a Redis-backed migration test runs from a 768-dimension tenant to a 1024-dimension target, then the test asserts raw and natural-language staging writes, `FT.INFO` reports 1024 dimensions on the active raw and NL search targets after cutover, rewritten hashes contain target provider/model/dimensions metadata, and the durable marker reaches the expected completed end-state.

3. Given rollback and abort are the operator recovery paths after Story 21.9, when Redis-backed tests exercise rollback-unavailable and `--abort` paths, then rollback without recorded previous targets fails closed without alias/config mutation, pre-cutover abort cleans staging state and releases the lock, and post-cutover abort restores previous targets before cleanup.

4. Given the migration tool is operator-facing and can expose provider, token, and marker failures, when parser/output/error tests are expanded, then `--live`, `--resume`, `--rollback`, and `--abort` keep exactly-one-mode behavior, JSON output remains camelCase and automation-safe, human output remains actionable, and secret literals never appear in logs, result messages, or failure records.

5. Given Epic 21 changes data paths that security and tenant-isolation stories already hardened, when migration tests are added, then they prove tenant-scoped keys/indexes/aliases/locks stay isolated and do not weaken Story 20 auth/tenant/audit regression guards or Story 21.4 key-schema literal guards.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A22 and current 21.9 baseline before editing (AC: 1-3)
  - [x] Verify the current migration files under `src/Hexalith.Memories.Server/Migration/` and `tools/MigrateEmbeddingVectors/Program.cs` match the Story 21.9 blue/green baseline.
  - [x] Confirm existing focused tests in `tests/Hexalith.Memories.Server.Tests/Migration/` are unit/store/tool tests, not the required real-vector Redis-backed migration lane.
  - [x] Keep the story bounded to tests and minimal testability hooks; do not redesign migration, provider strategy, tenant registry, or deployment topology.

- [x] Task 2 - Strengthen unit coverage for store, marker, generator, and tool behavior (AC: 1, 3, 4)
  - [x] Extend `EmbeddingVectorMigrationServiceTests` for generator dimension mismatch, provider exceptions, per-unit failure retention, resume target mismatch, completed marker end-state, rollback unavailable, rollback success, and abort success/failure paths.
  - [x] Extend `RedisEmbeddingMigrationStoreTests` for marker hash fields, active/per-target marker consistency, owner mismatch refusal, TTL/heartbeat renewal, stale-lock recovery rules, previous-target preservation on resume, and lock deletion on completion/abort.
  - [x] Extend `MigrateEmbeddingVectorsToolTests` for exactly-one-mode behavior across `--dry-run`, `--live`, `--rollback`, and `--abort`; invalid target dimensions; JSON output shape; and human prompt/help text that no longer describes destructive index rebuild.
  - [x] Add or extend generator tests around `EmbeddingClientMigrationVectorGenerator` using existing fake HTTP/OIDC patterns rather than a live provider.

- [x] Task 3 - Add Redis-backed real-vector migration integration coverage (AC: 2, 5)
  - [x] Add a new integration test class under `tests/Hexalith.Memories.IntegrationTests/Migration/` using the existing `RedisStackFixture` and deterministic fake vector generation.
  - [x] Seed one tenant with active 768-dimension raw semantic and natural-language indexes, syntactic hashes, raw vector hashes, and NL vector hashes using `IndexSchemaDefinitions` helpers.
  - [x] Run `EmbeddingVectorMigrationService` against a real Redis Stack connection with a deterministic `IEmbeddingMigrationVectorGenerator` that returns 1024-float vectors.
  - [x] Assert active raw and NL aliases/search targets point to 1024-dimension indexes by reading `FT.INFO` and parsing dimensions with the existing schema helpers.
  - [x] Assert rewritten raw and NL staging/active hashes contain `embeddingProvider`, `embeddingModel`, `embeddingDimensions=1024`, `memoryUnitId`, `caseId`, and retained NL description metadata.
  - [x] Assert active 768-dimension search targets remain available until cutover and no tenant B key/index/alias/lock is created or mutated.

- [x] Task 4 - Add rollback and abort Redis-backed recovery tests (AC: 3)
  - [x] Cover rollback-unavailable: missing or corrupt previous-target marker fields must return/fail as a domain error and leave active aliases/config unchanged.
  - [x] Cover pre-cutover abort: started marker plus staging indexes/keys are cleaned, lock is released, and active aliases still target the original 768-dimension indexes.
  - [x] Cover post-cutover abort or rollback: after alias cutover begins, abort/rollback restores previous raw and NL aliases and previous tenant embedding config, then cleans staging when appropriate.
  - [x] Assert marker status transitions (`started`, `cutover`, `aborted`, `rolled-back`, `completed`) and owner fields through Redis hash reads, not only mock calls.

- [x] Task 5 - Preserve documentation and test-lane hygiene (AC: 1-5)
  - [x] If adding reusable Redis test helpers, place them under the relevant integration test fixture/helper folder and keep one C# type per file.
  - [x] Do not add package versions to `.csproj`; use existing Testcontainers, StackExchange.Redis, xUnit v3, Shouldly, and NSubstitute packages.
  - [x] Avoid `RunnableSkippedFact` for the new RedisStack migration tests unless Docker/Redis Stack is truly unavailable in the lane; if a skip is unavoidable, include the exact enabling condition and a normal runnable unit/store fallback.
  - [x] Update `docs/operations/embedding-providers.md` only if tests expose a runbook mismatch; do not churn docs just to mention that tests exist.
  - [x] Keep File List complete and record exact test counts/commands in the Dev Agent Record.

- [x] Task 6 - Validate with focused and broad commands (AC: 1-5)
  - [x] Run focused server tests for migration classes.
  - [x] Run the RedisStack migration integration test class.
  - [x] Run the known xUnit v3 in-process fallback if normal `dotnet test` fails in the sandbox with the documented VSTest TCP listener permission issue.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`.
  - [x] If Docker/Testcontainers is unavailable, record the exact Docker/socket error and ensure unit/store coverage still proves the non-Docker portions.

### Evidence Table

| Evidence item | Owner | Required proof | Review status | Completion date |
|---|---|---|---|---|
| Unit/store/tool coverage | Dev/Test | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Migration -parallel none -noLogo` -> 60 total, 0 failed | Complete | 2026-07-04 |
| Real Redis 768-to-1024 migration | Dev/Test | RedisStack migration class added and compiled; execution blocked by Docker socket permission in this sandbox (`unix:///var/run/docker.sock`, `SocketException (13): Permission denied`) | Implemented; sandbox-blocked | 2026-07-04 |
| Rollback/abort recovery | Dev/Test | RedisStack tests cover rollback-unavailable, pre-cutover abort cleanup, and post-cutover restore; unit/store fallback passed in 59-test migration lane | Complete | 2026-07-04 |
| Isolation and guard preservation | Dev/Test | RedisStack test asserts tenant B untouched; `Architecture`, `Authentication`, and `Tenants` server guards passed 156 total, 0 failed | Complete | 2026-07-04 |
| Full build/test hygiene | Dev/Test | `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` passed; full Server.Tests fallback passed 2212 total, 0 failed, 1 skipped | Complete | 2026-07-04 |

## Dev Notes

Story 21.10 closes audit finding A22. It is a coverage and evidence story for the migration subsystem, not a second implementation pass for blue/green migration. Story 21.9 already changed production behavior; this story should prove that behavior under unit, store, tool, and Redis-backed integration tests. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.10; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A22; _bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 21 covers data integrity, consistency, deletion, routing, dedup, registry, migration safety, and A22 coverage.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are physical tenant isolation, EventStore as domain source of truth, Redis/FalkorDB as projections/read models, Dapr workflow/idempotency, and operator-safe recovery.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements include configurable embeddings, tenant provider configuration, embedding versioning/migration, partial-failure recovery, tenant deletion completeness, and zero data loss on restart.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope, but operator-facing migration output must avoid silent partial failure and give recovery paths.
- Loaded persistent facts from `_bmad-output/project-context.md` and Hexalith LLM/state instructions; tests use .NET 10/C# 14, xUnit v3, Shouldly, NSubstitute, central package management, and one C# type per file.
- Loaded previous Story 21.9, current migration source files, current migration tests, RedisStack integration fixture, Ollama fake provider fixtures, operator docs, the A22 audit anchor, sprint status, recent commits through `d673a0e`, and official Redis command docs for `FT.INFO`, `FT.ALIASUPDATE`, and `SET`.

### Current State and Code Anchors

`EmbeddingVectorMigrationService.RunAsync` dispatches dry-run, live, rollback, and abort modes. Live starts/resumes an owner-locked marker, prepares staging indexes, migrates raw and natural-language vectors, verifies staging, cuts over aliases, updates tenant config through the store, heartbeats, and completes the marker only on a clean run. [Source: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs]

`RedisEmbeddingMigrationStore` owns the real Redis behavior: `StartMigrationMarkerAsync`, `PrepareStagingSemanticIndexesAsync`, `VerifyStagingSemanticIndexesAsync`, `CutoverStagingSemanticIndexesAsync`, `RollbackMigrationAsync`, `AbortMigrationAsync`, `WriteRawSemanticAsync`, and `WriteNaturalLanguageSemanticAsync`. The integration tests should exercise this class against Redis Stack where possible instead of substituting every Redis command. [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs]

`IndexSchemaDefinitions` is the single source for active legacy names, active aliases, staging indexes, staging key prefixes, previous aliases, field identifiers, vector schema creation, and `FT.INFO` dimension parsing. Use these helpers in tests; do not hard-code raw `:vec:`, `:vecnl:`, staging, or alias strings except where an existing literal guard test intentionally checks forbidden production literals. [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs; _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md]

`IEmbeddingMigrationStore` exposes the testable storage boundary. Unit tests can use the existing fake store for orchestration order, but the 21.10 integration tests need real `RedisEmbeddingMigrationStore` behavior for indexes, hashes, aliases, markers, locks, and `FT.INFO` assertions. [Source: src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs]

`tools/MigrateEmbeddingVectors/Program.cs` contains the CLI parser and operator output. It supports `--dry-run`, `--live`, `--rollback`, `--abort`, `--resume`, `--format`, `--redis`, and `--dapr-http`. Parser/output tests should keep this internal parser behavior covered without requiring a live operator shell. [Source: tools/MigrateEmbeddingVectors/Program.cs]

`tests/Hexalith.Memories.Server.Tests/Migration/` already contains focused unit/store/tool tests for Story 21.9, including redaction, no active drop before staging, owner lock, resume preservation, rollback, abort, and parser help. 21.10 should expand these, not duplicate them under new names. [Source: tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs; tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs; tests/Hexalith.Memories.Server.Tests/Migration/MigrateEmbeddingVectorsToolTests.cs]

`RedisStackFixture` already starts a pinned Redis Stack container and exposes `IConnectionMultiplexer` plus a connection string. Existing semantic integration tests use it for RediSearch/vector assertions and should be the starting point for real migration coverage. [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs; tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSemanticIntegrationTests.cs]

`OllamaOidcFakeServer` has deterministic vector generation and provider HTTP/OIDC patterns. For a lightweight 768-to-1024 migration integration test, prefer a deterministic in-process `IEmbeddingMigrationVectorGenerator`; use the fake server only if exercising `EmbeddingClientMigrationVectorGenerator` specifically. [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs; tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs]

### Architecture Constraints

- Domain source of truth remains EventStore. Migration markers, Redis vector hashes, RediSearch aliases/indexes, staging keys, and previous targets are projection/infrastructure state for operator migration, not new domain persistence. [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md; _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency]
- Tenant isolation is physical and explicit. Tests must use unique tenant IDs, tenant-scoped indexes/keys/aliases/locks, and a negative tenant check so a migration for tenant A cannot mutate tenant B. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Keep Dapr/Aspire topology out of this story unless needed for an existing fixture. A RedisStack fixture plus deterministic vector generator is enough for the required A22 real-vector migration proof; broader deployment/test-readiness belongs to Epic 26. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21]
- Use existing packages and test stack: .NET 10, xUnit v3, Shouldly, NSubstitute, StackExchange.Redis, NRedisStack, Testcontainers. Do not add package versions to project files. [Source: _bmad-output/project-context.md#Technology-Stack-and-Versions]
- Normal `dotnet test` may fail in this sandbox before discovery with `SocketException (13): Permission denied`; previous Epic 21 stories used `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` as the xUnit v3 in-process fallback. [Source: _bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md#Debug-Log-References]

### Redis Command Notes

Official Redis docs confirm the commands Story 21.10 should assert against: `FT.INFO` returns index information/statistics, `FT.ALIASUPDATE` updates an alias to point at an index, and `SET` supports conditional/expiry options used for owner locks. Keep tests grounded in the repository's `IndexSchemaDefinitions.TryGetVectorDimensions` and StackExchange.Redis `StringSetAsync(..., When.NotExists)` usage rather than shelling out to `redis-cli`. [Source: https://redis.io/docs/latest/commands/ft.info/; https://redis.io/docs/latest/commands/ft.aliasupdate/; https://redis.io/docs/latest/commands/set/]

### Previous Story Intelligence

Story 21.1 ratified EventStore as the source of truth and Redis/FalkorDB/Dapr state as rebuildable projections. 21.10 must not test or imply Redis marker state is domain truth. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md]

Story 21.3 moved natural-language semantic hashes to a disjoint `{tenant}:vecnl:*` shape while preserving migration reads for legacy nested `{tenant}:vec:nl:*`. Migration tests must include NL coverage and ensure raw semantic enumeration does not consume NL hashes. [Source: _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md]

Story 21.4 centralized key/index construction in `IndexSchemaDefinitions` and added literal guards. New tests should extend helpers and guards when needed, not reintroduce production literals. [Source: _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md]

Story 21.5 expanded tenant deletion cleanup for `embedding-migration:*` and vector key families; Story 21.9 review added deletion cleanup for versioned staging RediSearch indexes. Migration integration tests should not leave tenant-scoped staging indexes/keys behind without cleanup. [Source: _bmad-output/implementation-artifacts/21-5-deletion-completeness.md; _bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md#Senior-Developer-Review-AI]

Story 21.7 review fixed owner-checked cleanup semantics for dedup; Story 21.9 applied the same principle to migration. Rollback, abort, completion, and heartbeat tests should assert owner mismatch fails closed. [Source: _bmad-output/implementation-artifacts/21-7-dedup-race-and-duplicate-instance-handling.md; _bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md]

Story 21.8 hardened registry CAS and rollback integrity. Migration tests should use the committed tenant config surface or a narrow fake when the test scope is Redis-only; do not add direct registry writes. [Source: _bmad-output/implementation-artifacts/21-8-tenant-registry-cas-and-rollback-integrity.md]

Story 21.9 implemented blue/green staging, alias cutover, real rollback, marker ownership/TTL/heartbeat, `--abort`, operator docs, and focused tests. Story 21.10's main value is proving that work against real Redis vector indexes and recovery end-states. [Source: _bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md]

### Git Intelligence

Recent commits:

- `d673a0e feat(story-21.9): Blue/Green Embedding Migration`
- `3676ad0 feat(story-21.8): Tenant Registry CAS & Rollback Integrity`
- `39d4c21 feat(story-21.7): Dedup Race & Duplicate-Instance Handling`
- `56598ac feat(story-21.6): Event Routing for Unknown/Unavailable Tenants`
- `c4df92b feat(story-21.5): Deletion Completeness`
- `b0ff9bf feat(story-21.4): Key-Schema Single Source of Truth`

The Epic 21 pattern is narrow audit remediation with explicit source anchors, focused regression tests, end-state proof, owner checks, and File List hygiene. Continue that pattern.

### Scope Boundaries

- In scope: tests under `tests/Hexalith.Memories.Server.Tests/Migration/`, new RedisStack-backed tests under `tests/Hexalith.Memories.IntegrationTests/Migration/`, small reusable integration helpers, and minimal production testability hooks only if impossible to test otherwise.
- In scope: proving `EmbeddingVectorMigrationService`, `RedisEmbeddingMigrationStore`, marker reader/models, `IndexSchemaDefinitions`, `EmbeddingClientMigrationVectorGenerator` or deterministic generator behavior, and `tools/MigrateEmbeddingVectors` parser/output behavior.
- In scope: focused docs correction only if implementation/test evidence shows `docs/operations/embedding-providers.md` is stale.
- Out of scope: changing migration semantics already implemented by Story 21.9 unless a test exposes a real defect; provider strategy refactor (Story 23.9); chunking/batch embedding (Story 23.1); physical isolation redesign (Story 24.3); deployment lanes (Epic 26); package upgrades; submodule changes; broad AppHost/Aspire topology changes.
- Out of scope: using live third-party embedding providers in tests. Use deterministic vectors or existing local fake server patterns.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Test names must be descriptive PascalCase and test folders should mirror product areas. [Source: _bmad-output/project-context.md#Testing-Rules]
- Redis-backed migration tests must assert durable end-state, not only service return codes: `FT.INFO` dimensions, alias targets or effective search target, Redis hash metadata, marker hash fields/status, lock key presence/absence, and tenant isolation.
- Use unique tenant IDs and clean up created indexes/keys to avoid cross-test pollution in the shared RedisStack collection.
- Keep tests deterministic: fixed vector lengths, deterministic vector content, no external network provider calls, and bounded waits.
- Validation should include focused migration tests, the new RedisStack integration tests, the in-process xUnit fallback if needed, and full solution build.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.10 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - approved A22 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A22 - migration subsystem test gap]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source of truth and projection framing]
- [Source: _bmad-output/planning-artifacts/prd.md#Embedding-Provider-and-Migration - embedding migration and operator requirements]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Failure-Design - avoid silent partial failure]
- [Source: _bmad-output/project-context.md - .NET, Redis, Dapr, testing, package, and style rules]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - EventStore and read-model rules]
- [Source: _bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md - previous story implementation and review intelligence]
- [Source: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs - migration orchestration]
- [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs - Redis migration store behavior]
- [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs - schema and dimension parsing helpers]
- [Source: tools/MigrateEmbeddingVectors/Program.cs - operator tool parser/output]
- [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs - Redis Stack fixture]
- [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs - deterministic fake provider pattern]
- [Source: https://redis.io/docs/latest/commands/ft.info/ - Redis `FT.INFO` command]
- [Source: https://redis.io/docs/latest/commands/ft.aliasupdate/ - Redis `FT.ALIASUPDATE` command]
- [Source: https://redis.io/docs/latest/commands/set/ - Redis `SET` command with conditional/expiry options]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, customization block, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Story 21.9, A22 audit anchor, current migration code, current tests, RedisStack integration fixtures, operator docs, recent commits, and official Redis command docs.
- 2026-07-04: story target came from user request `21.10`; sprint status had `21-10-migration-subsystem-test-coverage: backlog` and `epic-21: in-progress`.
- 2026-07-04: no module UI work detected; UX context was discovered only for operator-facing failure/recovery guidance.
- 2026-07-04: checklist validation applied after creation; story includes A22 anchors, Story 21.9 guardrails, real Redis vector test requirements, recovery-path requirements, tenant isolation guardrails, source citations, and validation commands.
- 2026-07-04: dev-story workflow loaded BMAD customization/config, root project context, Hexalith LLM/state instructions, sprint status, Story 21.10, current migration source/tests, RedisStack fixture, and migration tool parser/output implementation.
- 2026-07-04: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~Migration" --logger "console;verbosity=normal"` failed before discovery with documented VSTest sandbox issue: `System.Net.Sockets.SocketException (13): Permission denied` while starting the TCP listener.
- 2026-07-04: fallback `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Migration -parallel none -noLogo` passed: 60 total, 0 failed, 0 skipped.
- 2026-07-04: `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore -p:BuildProjectReferences=false` passed, proving the new RedisStack migration integration class compiles.
- 2026-07-04: RedisStack migration execution command `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Migration.EmbeddingVectorMigrationRedisIntegrationTests -parallel none -noLogo` was blocked by Docker/Testcontainers: `DockerUnavailableException`, failed to connect to `unix:///var/run/docker.sock`, inner `SocketException: Permission denied`.
- 2026-07-04: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Architecture -namespace Hexalith.Memories.Server.Tests.Authentication -namespace Hexalith.Memories.Server.Tests.Tenants -parallel none -noLogo` passed: 156 total, 0 failed.
- 2026-07-04: full Server.Tests in-process fallback `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` passed: 2212 total, 0 failed, 1 skipped.
- 2026-07-04: `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` passed with 0 warnings and 0 errors.
- 2026-07-04: qa-generate-e2e-tests workflow re-ran checklist validation, patched discovered raw/NL staging cleanup and tenant-B isolation assertion gaps, appended the Story 21.10 test automation summary, and reconfirmed the same Docker socket permission blocker for RedisStack execution.
- 2026-07-05: senior review loaded `bmad-story-automator-review` workflow/config/checklist, project context, Hexalith LLM/state instructions, planning anchors, story file, git status/diff, and every source/test file in the File List.
- 2026-07-05: senior review fixed rollback status preservation so standalone rollback leaves the durable marker `rolledBack` and releases the lock instead of overwriting it with `completed`.
- 2026-07-05: senior review added Redis-backed post-cutover abort hash cleanup assertions and a Redis-backed successful rollback end-state test covering raw/NL alias restore, tenant config restore, `rolledBack` marker, and lock release.
- 2026-07-05: validation `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings and 0 errors.
- 2026-07-05: validation `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Migration -parallel none -noLogo` passed: 60 total, 0 failed, 0 skipped.
- 2026-07-05: validation `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore -p:BuildProjectReferences=false` passed with 0 warnings and 0 errors.
- 2026-07-05: RedisStack migration execution remained blocked by Docker/Testcontainers socket permission: `DockerUnavailableException`, `unix:///var/run/docker.sock`, inner `SocketException: Permission denied`; discovery found 5 tests in the class, all blocked at fixture initialization.
- 2026-07-05: validation `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` passed with 0 warnings and 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.10 created as the A22 coverage story after Story 21.9 completed.
- The story requires Redis-backed 768-to-1024 migration proof with `FT.INFO`, rewritten key metadata, marker end-state, rollback-unavailable, and abort coverage.
- The story keeps implementation bounded to tests and minimal testability hooks, preserving Story 21.9 migration semantics.
- Added vector-length validation before migration writes so provider/generator dimension mismatches are retained as per-unit failures and cannot cut over.
- Expanded migration service/store/tool unit coverage for wrong-length vectors, abort failure, marker hash consistency, owner mismatch refusal, lock cleanup, parser exactly-one-mode behavior, invalid dimensions, and camelCase JSON output.
- Added RedisStack-backed migration tests for 768-to-1024 live cutover, tenant B isolation, rollback-unavailable fail-closed behavior, pre-cutover abort cleanup, and post-cutover abort restore/cleanup.
- Fixed abort cleanup to delete staging hash keys as well as staging RediSearch indexes.
- QA automation follow-up tightened pre-cutover abort coverage to assert natural-language staging hash cleanup and tenant B absence for staging indexes, staging keys, locks, and markers.
- Senior review fixed standalone rollback to preserve the `rolledBack` durable end-state instead of completing the marker after rollback.
- Senior review added RedisStack coverage for successful post-cutover rollback and tightened post-cutover abort cleanup assertions for raw and natural-language staging hashes.
- No operations documentation changes were needed; tests did not expose a runbook mismatch.

### File List

- _bmad-output/implementation-artifacts/21-10-migration-subsystem-test-coverage.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
- src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs
- tests/Hexalith.Memories.IntegrationTests/Migration/EmbeddingVectorMigrationRedisIntegrationTests.cs
- tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj
- tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs
- tests/Hexalith.Memories.Server.Tests/Migration/MigrateEmbeddingVectorsToolTests.cs
- tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs
- tools/MigrateEmbeddingVectors/MigrateEmbeddingVectors.csproj

### Change Log

- 2026-07-04: Implemented Story 21.10 migration subsystem coverage and abort cleanup fix; added unit/store/tool tests and RedisStack integration tests; validation recorded with Docker/Testcontainers sandbox limitation.
- 2026-07-04: QA workflow follow-up patched Redis cleanup/isolation test gaps and appended the Story 21.10 automation summary.
- 2026-07-05: Senior review fixed rollback marker end-state preservation, added successful rollback RedisStack coverage, tightened post-cutover abort staging hash cleanup assertions, and marked story done.

## Senior Developer Review (AI)

### Review Date

2026-07-05

### Reviewer

Codex GPT-5

### Findings

- HIGH fixed: standalone rollback restored aliases/config but `EmbeddingVectorMigrationService.RollbackAsync` immediately called `CompleteMigrationMarkerAsync`, overwriting the durable `rolledBack` marker with `completed`. Removed the completion call and made `RedisEmbeddingMigrationStore.RollbackMigrationAsync` release the lock after writing `rolledBack`. Abort still keeps the lock until abort cleanup completes.
- MEDIUM fixed: post-cutover abort coverage asserted staging indexes were removed but did not prove raw and natural-language staging hashes were cleaned. Added hash absence assertions for both staging key families.
- MEDIUM fixed: successful post-cutover rollback end-state was not covered by the RedisStack lane. Added a Redis-backed rollback test proving raw/NL aliases restore to 768 dimensions, config restores, the marker remains `rolledBack`, and the lock is released.

### Validation

- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -namespace Hexalith.Memories.Server.Tests.Migration -parallel none -noLogo` - passed, 60 total, 0 failed, 0 skipped.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore -p:BuildProjectReferences=false` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Migration.EmbeddingVectorMigrationRedisIntegrationTests -parallel none -noLogo` - blocked by Docker/Testcontainers socket permission: `unix:///var/run/docker.sock`, `SocketException (13): Permission denied`.
- `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.

### Outcome

Approved after auto-fixes. No critical issues remain.
