---
baseline_commit: 3676ad0
---

# Story 21.9: Blue/Green Embedding Migration

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want embedding-vector migration to be non-destructive with a real rollback and a locked marker,
so that a mid-run failure cannot strand a tenant with broken search and blocked writes.

## Acceptance Criteria

1. Given live migration currently calls `StartMigrationMarkerAsync`, `DropAndRecreateSemanticIndexesAsync`, and `SetEmbeddingConfigAsync` before re-embedding, when a live migration starts, then the current active raw and natural-language semantic indexes and hashes remain queryable while new vectors are generated under staging prefixes and staging indexes. No active index is dropped or recreated before all staging vectors and index metadata are verified. Closes A5.

2. Given raw semantic and natural-language semantic indexes use tenant-scoped names from `IndexSchemaDefinitions`, when staging migration runs, then all staging index names, aliases, prefixes, and hash keys are produced through new `IndexSchemaDefinitions` helpers and covered by tests. Do not reintroduce raw `:vec:` or `:vecnl:` string construction outside the schema helper and approved tests.

3. Given cutover must be atomic from the search path's perspective, when staging verification succeeds, then cutover switches both raw and natural-language semantic search targets together using a single approved Redis/RediSearch indirection, transaction, or alias update strategy. Searches must see either the full old vector set or the full new vector set, never a partially rebuilt tenant.

4. Given rollback is currently a stub that returns `DomainError` even when `:previous` indexes exist, when rollback is requested after cutover or after a failed cutover, then the previous active raw and natural-language search targets are retained and can be restored. Rollback must restore tenant embedding config and marker state consistently, and tests must prove search/index metadata points back to the previous version.

5. Given the existing marker is a durable active hash without owner locking, TTL, or heartbeat, when a migration is started, resumed, heartbeated, completed, aborted, or rolled back, then marker ownership is guarded by `SET NX` semantics with a unique owner id, TTL, and heartbeat renewal. Concurrent live/resume/rollback/abort attempts must fail closed unless they own the lock or satisfy an explicit stale-lock recovery rule.

6. Given Story 19.4 carried the migration-marker target-consistency and operator-recovery cluster into Epic 21, when this story completes, then resume verifies the active marker target matches the requested provider/model/dimensions, completion verifies the active marker is still owned by the current run, and `--abort` exists for operator recovery without manually editing Redis.

7. Given tenant deletion now sweeps `embedding-migration:*` keys, when blue/green migration introduces versioned indexes, staging keys, lock keys, or retained previous keys, then tenant deletion cleanup, orphan-index reconciliation, and any migration failure retention windows are updated so deleted tenants do not leave write-blocking locks or orphaned staging indexes.

8. Given the tool is operator-facing and can expose provider failures, when CLI output, docs, failures, logs, or marker fields are changed, then secret redaction remains intact, JSON/human output remains automation-safe, and `docs/operations/embedding-providers.md` documents live, resume, abort, rollback, retention, heartbeat, and failure semantics.

9. Given Story 21.10 will add broader migration subsystem integration coverage, when Story 21.9 completes, then it still includes focused unit and store-level tests for non-destructive staging, cutover ordering, rollback restoration, marker ownership/TTL/heartbeat, concurrent-run rejection, `--abort`, and no active-index drop before staging verification.

## Tasks / Subtasks

- [x] Task 1 - Re-run the A5 anchor preflight before editing (AC: 1, 4, 5)
  - [x] Confirm `EmbeddingVectorMigrationService.LiveAsync` still starts a marker, calls `DropAndRecreateSemanticIndexesAsync`, then updates config before vector generation.
  - [x] Confirm `EmbeddingVectorMigrationService.RollbackAsync` still only checks `HasRetainedPreviousVersionIndexesAsync` and returns `DomainError`.
  - [x] Confirm `RedisEmbeddingMigrationStore.StartMigrationMarkerAsync` writes hashes but has no `SET NX` owner lock, TTL, or heartbeat.
  - [x] Confirm `RedisEmbeddingMigrationStore.HasRetainedPreviousVersionIndexesAsync` still checks `GetSemanticIndexName(...) + ":previous"` and `GetNaturalLanguageSemanticIndexName(...) + ":previous"` without a real restore path.
  - [x] Confirm no existing story file for 21.9 was implemented before this artifact; if code has already changed, reconcile the story against current code before continuing.

- [x] Task 2 - Introduce versioned blue/green schema helpers (AC: 2, 3, 7)
  - [x] Add raw and natural-language staging/active/previous naming helpers to `IndexSchemaDefinitions` or a narrowly named migration schema helper owned by the same infrastructure boundary.
  - [x] Keep active search/index naming backwards-compatible or provide an explicit first-cutover migration path for tenants whose current physical indexes use `GetSemanticIndexName` and `GetNaturalLanguageSemanticIndexName` directly.
  - [x] Add helpers for staging hash prefixes and version ids; version ids must be deterministic per migration run once created and safe for Redis key/index names.
  - [x] Update `IndexSchemaLiteralGuardTests` or equivalent guard coverage so future migration work cannot bypass the helper.

- [x] Task 3 - Replace destructive rebuild with staging generation (AC: 1, 2, 9)
  - [x] Replace `DropAndRecreateSemanticIndexesAsync` with explicit staging preparation methods that create raw and NL staging indexes without dropping active indexes.
  - [x] Run the Story 21.3 legacy NL namespace migration before staging writes, preserving its existing verification behavior.
  - [x] Write migrated raw vectors and NL vectors to staging keys only until verification succeeds.
  - [x] Preserve current resume detection by provider/model/dimensions, but apply it to staging state rather than active hashes.
  - [x] Keep progress output and failure recording behavior, including redaction and batch progress, while distinguishing staging, cutover, rollback, and abort failures.

- [x] Task 4 - Implement atomic cutover and real rollback (AC: 3, 4, 7)
  - [x] Choose and document the repo-approved active target indirection. Prefer RediSearch aliases if compatible with the current NRedisStack/StackExchange.Redis usage; if a different mechanism is used, tests must prove search paths cannot observe a mixed raw/NL cutover.
  - [x] Update `SemanticSearchService`, `NaturalLanguageSemanticSearchService`, tenant metrics, verifier, provisioning/deletion activities, and migration store code to use the active search target helper where required.
  - [x] Retain the previous active raw and NL targets with a bounded retention policy until rollback is impossible or explicitly cleaned by a successful migration close-out.
  - [x] Implement rollback so it restores both raw and NL active targets and the previous tenant embedding config as one coherent operation or fails without changing the active target.
  - [x] Ensure rollback and abort are idempotent: a repeated operator command returns the same safe end state, not a second mutation of active indexes.

- [x] Task 5 - Add owner-locked marker, TTL, heartbeat, resume, and abort semantics (AC: 5, 6, 8)
  - [x] Extend `EmbeddingMigrationOptions` and `tools/MigrateEmbeddingVectors/Program.cs` with `--abort`; preserve the exactly-one-mode parser rule across `--dry-run`, `--live`, `--rollback`, and `--abort`.
  - [x] Add marker fields for owner id, target provider/model/dimensions, migration version, status, created/updated/expires timestamps, active/staging/previous targets, and last heartbeat.
  - [x] Acquire the owner lock with Redis `SET` plus NX and expiry semantics before any mutable migration work. In StackExchange.Redis this should map to the pinned package's conditional string set APIs; do not upgrade packages.
  - [x] Heartbeat the lock and marker during long-running generation, before cutover, after cutover, during rollback, and before completion. If heartbeat renewal fails, fail closed and do not continue cutover.
  - [x] Make resume require a matching active marker target, existing staging state, and either current owner renewal or explicit stale-lock recovery.
  - [x] Make completion and rollback owner-checked. They must not complete, clear, or overwrite a marker owned by another active run.
  - [x] Implement `--abort` as an operator-safe recovery command: it may clear an owned/stale pre-cutover marker and staging resources, or restore previous active targets if cutover began; otherwise it must refuse with a precise message.

- [x] Task 6 - Update cleanup, orphan handling, and docs (AC: 7, 8)
  - [x] Update `DeleteTenantDataKeysActivity` patterns for any new staging, lock, and retained previous keys.
  - [x] Update `OrphanSemanticIndexReconciler` or add a migration-specific reconciler path so orphaned staging/previous indexes are either retained intentionally or cleaned by documented policy.
  - [x] Update `docs/operations/embedding-providers.md` to replace Path A "rollback unavailable" language with the new blue/green runbook.
  - [x] Update tool help and human prompts so operators are no longer told that live migration drops and recreates active indexes.
  - [x] Keep secret redaction coverage for all new error/status/output fields.

- [x] Task 7 - Add focused validation and evidence (AC: 1-9)
  - [x] Add unit tests in `EmbeddingVectorMigrationServiceTests` for staging-before-cutover, no active drop on generation failure, cutover success, rollback success, rollback refusal when previous target is unavailable, and abort behavior.
  - [x] Add store tests in `RedisEmbeddingMigrationStoreTests` for staging index creation, alias/indirection commands, marker `SET NX` conflict, TTL/heartbeat renewal, owner mismatch refusal, stale-lock recovery, and cleanup of staging keys.
  - [x] Add search/verification/provisioning tests for any active-target helper change touching `SemanticSearchService`, `NaturalLanguageSemanticSearchService`, `ProvisionRedisVectorActivity`, `VerifyTenantActivity`, `TenantMetricsService`, or `TenantIsolationVerifier`.
  - [x] Add CLI parser/output tests for `--abort`, new help text, and no secret leakage in new marker/output fields.
  - [x] Run focused xUnit v3 in-process tests if normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`.
  - [x] Run a Redis-backed migration smoke/integration test if Docker/Dapr permissions allow; otherwise document the exact blocker and leave Story 21.10's real-vector integration lane explicit.

### Checkpoint Evidence Table

| Checkpoint | Owner | Required evidence | Review status | Completion date |
|---|---|---|---|---|
| A - Preflight and schema helpers | Dev | A5 anchors confirmed; helper tests prove versioned/staging names and no literal drift | Complete | 2026-07-04 |
| B - Staging generation | Dev | Service/store tests prove active indexes remain untouched on generation failure | Complete | 2026-07-04 |
| C - Cutover and rollback | Dev/Test | Tests prove raw and NL active targets switch together and rollback restores previous targets/config | Complete | 2026-07-04 |
| D - Marker ownership and abort | Dev/Test | Tests prove `SET NX` lock, TTL/heartbeat, owner mismatch refusal, stale recovery, and `--abort` | Complete | 2026-07-04 |
| E - Cleanup, docs, validation | Dev/Test | Deletion/reconciler/docs/tool-help updated; focused tests/build recorded | Complete | 2026-07-04 |

## Dev Notes

Story 21.9 closes audit finding A5. It is a migration-safety implementation story, not a provider-registry redesign, not a broader migration test epic, and not a retrieval-quality story. The goal is to replace destructive Path A semantics with blue/green staging, atomic cutover, owner-locked markers, and real rollback. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.9; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A5]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 21 covers consistency, namespace, deletion, routing, dedup, registry, and migration-safety remediation.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are tenant isolation, Dapr workflow/idempotency, EventStore as source of truth for domain state, and rebuildable Redis/FalkorDB projections.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are partial backend failure recovery, tenant deletion, restart durability, tenant isolation, and operator-safe migration behavior.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI is in scope, but destructive operations require clear operator confirmation and scope visibility.
- Loaded persistent facts from `_bmad-output/project-context.md`, referenced Hexalith project-context files, Hexalith LLM instructions, and Hexalith state instructions.
- Loaded prior Story 21.8, Story 19.4 migration-marker decision sweep, the A5 audit anchor, current migration code, current migration tests, current operator docs, tool parser, and recent commits through `3676ad0`.

### Current State and Code Anchors

`EmbeddingVectorMigrationService.LiveAsync` reads the current tenant config/counts/index info, starts a marker, calls `DropAndRecreateSemanticIndexesAsync`, writes the target embedding config with `forceReindex: true`, then migrates raw and natural-language vectors. A tenant-level failure leaves the marker active, but the active indexes may already have been dropped/recreated before replacement vectors exist. [Source: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs]

`EmbeddingVectorMigrationService.RollbackAsync` only calls `HasRetainedPreviousVersionIndexesAsync` and returns `DomainError` in both branches. There is no path that restores active search targets or tenant embedding config. [Source: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs]

`RedisEmbeddingMigrationStore.DropAndRecreateSemanticIndexesAsync` first runs `RedisNaturalLanguageNamespaceMigrator.MigrateAsync`, then drops active raw and NL indexes and creates new active indexes at the target dimension. This method must be replaced or narrowed so staging work never destroys active search before verification. [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs]

`RedisEmbeddingMigrationStore.StartMigrationMarkerAsync` writes per-target and active marker hashes atomically, but the marker is not an ownership lock: no unique owner id, no Redis `SET NX`, no TTL, no heartbeat, and no owner-checked completion. Concurrent runs can both believe they own the migration. [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs]

`EmbeddingMigrationMarkerReader.ReadActiveMarkerAsync` fails closed on malformed active-marker hashes and `EnsureWriteMatchesMarker` blocks stale raw/NL semantic writes when the active marker target does not match the attempted provider/model/dimensions. Preserve this fail-closed guard while adding ownership and TTL semantics. [Source: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs]

`RedisEmbeddingMigrationStore.WriteRawSemanticAsync` and `WriteNaturalLanguageSemanticAsync` write directly to active keys from `IndexSchemaDefinitions.BuildSemanticKey` and `BuildNaturalLanguageSemanticKey`. Story 21.9 must route migration writes to staging keys until cutover without changing normal ingestion writes to staging. [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs; src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs]

`SemanticSearchService`, `NaturalLanguageSemanticSearchService`, `ProvisionRedisVectorActivity`, `VerifyTenantActivity`, `TenantMetricsService`, and `TenantIsolationVerifier` read active index names directly through `IndexSchemaDefinitions.GetSemanticIndexName` or `GetNaturalLanguageSemanticIndexName`. If cutover uses aliases or another indirection, these paths must be updated together and tested. [Source: rg output for `GetSemanticIndexName` and `GetNaturalLanguageSemanticIndexName`]

`tools/MigrateEmbeddingVectors/Program.cs` supports `--dry-run`, `--live`, `--rollback`, and `--resume`. It prompts that live migration "drops and recreates active semantic indexes" and help says rollback fails closed unless previous indexes exist. Story 21.9 must update parser, prompt, help, and output semantics. [Source: tools/MigrateEmbeddingVectors/Program.cs]

`docs/operations/embedding-providers.md` currently documents Path A live migration, marker protection, resume, final verification, and rollback as unavailable unless retained previous-version indexes exist. This runbook must be updated so operators do not follow stale destructive-migration guidance. [Source: docs/operations/embedding-providers.md#Migration-Runbook]

### Architecture Constraints

- Domain source of truth remains EventStore. Migration markers, Redis vector hashes, RediSearch indexes, aliases, staging state, and previous-index retention are infrastructure/projection state, not new domain persistence. Do not introduce a new authoritative tenant or memory-unit store. [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md; _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency]
- Tenant isolation is physical where possible and tenant id must stay explicit through migration, search, indexes, keys, telemetry, CLI, and docs. New staging/previous keys must remain tenant-scoped and must not be visible to other tenant searches. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Dapr Workflow owns durable orchestration for system workflows, but `tools/MigrateEmbeddingVectors` is an operator tool that talks to Redis and Dapr actor/config surfaces directly. Keep migration idempotent and operator-recoverable; do not invent a custom background queue. [Source: _bmad-output/project-context.md#Framework-Specific-Rules; tools/MigrateEmbeddingVectors/Program.cs]
- Use pinned repository packages. StackExchange.Redis and NRedisStack are already available; use direct `db.Execute(...)` only where the wrapper lacks a needed RediSearch command. Do not add package versions to `.csproj` files. [Source: _bmad-output/project-context.md#Technology-Stack-and-Versions]
- Keep one C# type per file, copyright headers on Memories `.cs` files, nullable/warnings-as-errors clean, and async cancellation tokens propagated. [Source: _bmad-output/project-context.md#CSharp-Language-Specific-Rules]

### Previous Story Intelligence

Story 21.1 ratified EventStore as source of truth and Redis/FalkorDB/Dapr state as projections/read models. Story 21.9 must not turn Redis migration marker state into domain truth. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md]

Story 21.3 made natural-language semantic keys disjoint from raw semantic keys. Blue/green staging must preserve that disjointness and must not resurrect the legacy nested `{tenant}:vec:nl:` shape except through migration-read helpers. [Source: _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md]

Story 21.4 centralized Redis key/index names in `IndexSchemaDefinitions` and added guard coverage against raw literals. All new staging, previous, alias, and lock names need the same treatment. [Source: _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md]

Story 21.5 extended tenant deletion sweeps to include `embedding-migration:*` and defensive vector keys. New blue/green keys and retained previous indexes must be included in deletion/reconciler cleanup. [Source: _bmad-output/implementation-artifacts/21-5-deletion-completeness.md]

Story 21.7's review fixed owner-checked dedup release. Apply the same rule here: abort, rollback, and completion must only clear or mutate migration state owned by the current run unless an explicit stale-lock recovery rule is satisfied. [Source: _bmad-output/implementation-artifacts/21-7-dedup-race-and-duplicate-instance-handling.md]

Story 21.8 hardened tenant registry CAS and rollback integrity. Do not bypass `TenantRegistryService` or introduce direct registry writes from the migration tool. [Source: _bmad-output/implementation-artifacts/21-8-tenant-registry-cas-and-rollback-integrity.md]

Story 19.4 classified migration-marker target-consistency (`15.3-RV15`, `15.3-RV16`, `15.3-RV27`) as mandatory before the next provider migration investment and operator recovery (`15.3-RV18`, `15.3-RV25`) as requiring reassessment before production migration claims. Story 21.9 is that migration investment; include target matching, stale marker recovery, TTL, heartbeat, and operator copy now. [Source: _bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md]

### Git Intelligence

Recent commits:

- `3676ad0 feat(story-21.8): Tenant Registry CAS & Rollback Integrity`
- `33b99f5 feat(story-21.8): Update orchestration state and progress for story 21.8`
- `39d4c21 feat(story-21.7): Dedup Race & Duplicate-Instance Handling`
- `56598ac feat(story-21.6): Event Routing for Unknown/Unavailable Tenants`
- `e64459b chore(story-automator): record story 21.5 completion`
- `c4df92b feat(story-21.5): Deletion Completeness`
- `b0ff9bf feat(story-21.4): Key-Schema Single Source of Truth`
- `1b072f4 feat(story-21.3): Natural-Language Vector Namespace Separation`

The pattern in recent Epic 21 work is focused remediation with owner checks, end-state tests, file-list discipline, and no broad package or topology changes. Follow that pattern.

### Scope Boundaries

- In scope: `EmbeddingVectorMigrationService`, `IEmbeddingMigrationStore`, `RedisEmbeddingMigrationStore`, marker models/reader, migration result/options if needed, `IndexSchemaDefinitions`, semantic/NL search active-target helpers if cutover requires them, tenant vector provisioning/deletion/verification/metrics paths touched by active target naming, `tools/MigrateEmbeddingVectors`, focused migration/search/provisioning tests, and `docs/operations/embedding-providers.md`.
- In scope: small helper records/classes for migration version identity, marker ownership, retained targets, staging targets, and rollback state. Keep one C# type per file.
- In scope: cleanup/reconciler updates for staging, previous, lock, and marker keys.
- Out of scope: provider plugin/strategy refactor (Story 23.9), migration subsystem broad real-vector integration expansion (Story 21.10), content chunking/batch embedding (Story 23.1), physical tenant isolation decision (Story 24.3), retrieval ranking changes (Epic 22), and general Program.cs decomposition (Epic 25).
- Out of scope: changing public tenant embedding config JSON shape unless an additive field is essential and covered by contract serialization tests.
- Out of scope: changing normal ingestion write behavior to staging outside an active migration run.
- Out of scope: submodule changes, package version upgrades, Docker/K8s/deployment artifacts.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. [Source: _bmad-output/project-context.md#Testing-Rules]
- Unit tests must verify both operation order and durable end state: active index target unchanged on pre-cutover failure, staging target exists before cutover, previous target retained after cutover, rollback restores active target, marker lock has TTL, and owner mismatch refuses mutation.
- Store-level tests should assert exact Redis/RediSearch command shape where practical: conditional string set for `SET NX` lock, expiry/heartbeat renewal, alias/active-target update, staging index creation, previous-retention naming, and no `FT.DROPINDEX` on active indexes before verification.
- CLI tests must cover parser exclusivity, `--abort`, human help/prompt text, JSON output shape where changed, and secret redaction.
- Any search/provisioning path changed for active-target indirection needs focused tests in the matching test folders.
- Integration tests using Redis/Testcontainers are valuable but may be blocked by Docker socket permission. If blocked, record the exact blocker and keep unit/store tests strong. Story 21.10 remains the broader real-vector integration coverage story.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.9 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - approved A5 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A5 - destructive migration finding]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04-rerun.md#Checkpoint-heavy-stories - evidence table requirement]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source of truth and projection framing]
- [Source: _bmad-output/planning-artifacts/prd.md#FR13-and-FR39 - partial failure recovery and tenant deletion]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Dialogs-and-drawers - destructive operation confirmation guidance]
- [Source: _bmad-output/project-context.md - Dapr, Redis, workflow, testing, package, and style rules]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - EventStore persistence and read-model rules]
- [Source: _bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md - migration-marker target-consistency and operator-recovery carry-forward]
- [Source: _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md - NL prefix disjointness]
- [Source: _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md - key helper guardrails]
- [Source: _bmad-output/implementation-artifacts/21-5-deletion-completeness.md - tenant deletion cleanup precedent]
- [Source: _bmad-output/implementation-artifacts/21-7-dedup-race-and-duplicate-instance-handling.md - owner-checked cleanup precedent]
- [Source: _bmad-output/implementation-artifacts/21-8-tenant-registry-cas-and-rollback-integrity.md - latest Epic 21 implementation pattern]
- [Source: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs - A5 service anchor]
- [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs - destructive index rebuild and marker storage anchor]
- [Source: src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs - migration storage contract]
- [Source: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs - fail-closed marker reader and write guard]
- [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs - key/index naming source of truth]
- [Source: tools/MigrateEmbeddingVectors/Program.cs - operator tool parser, prompt, and help]
- [Source: docs/operations/embedding-providers.md#Migration-Runbook - current Path A operator docs]
- [Source: tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs - current migration service coverage]
- [Source: tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs - current Redis migration store coverage]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, customization block, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Story 21.8, Story 19.4 migration residual sweep, A5 audit anchor, current migration code, current tests, tool parser, operator docs, and recent commits.
- 2026-07-04: story target came from user request `21.9`; sprint status had `21-9-blue-green-embedding-migration: backlog` and `epic-21: in-progress`.
- 2026-07-04: no module UI work detected; UX context was discovered only for operator destructive-action confirmation guidance.
- 2026-07-04: checklist validation applied after creation; story includes A5 anchors, previous-story guardrails, checkpoint evidence table, anti-reinvention guidance, marker ownership requirements, and focused validation requirements.
- 2026-07-04: dev-story workflow loaded BMAD customization, project context, Hexalith LLM/state rules, sprint status, and the complete story file; baseline commit `3676ad0` preserved.
- 2026-07-04: A5 preflight confirmed the destructive live path, fail-closed rollback stub, unlocked marker, and previous-index check anchors before implementation edits.
- 2026-07-04: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --logger "console;verbosity=normal"` was attempted and failed before discovery with `SocketException (13): Permission denied`; the documented xUnit v3 in-process runner was used instead.
- 2026-07-04: focused xUnit v3 in-process lane passed for migration, schema, tenant cleanup, and reconciler classes: 74 tests, 0 failed.
- 2026-07-04: full validation passed with `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` and `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll`: 2192 tests, 0 failed, 1 skipped.
- 2026-07-04: Redis-backed Docker smoke was not run because Docker socket access failed with `permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`; Story 21.10 remains the broader real-vector integration lane.
- 2026-07-04: senior developer review workflow found and auto-fixed two issues: resume/rollback marker refresh overwrote previous rollback fields, and tenant deletion did not drop versioned staging RediSearch indexes.
- 2026-07-04: review validation reran with `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`, `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll`, and `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`; all passed. Normal `dotnet test` still fails before discovery with `SocketException (13): Permission denied`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.9 created as the A5 implementation story after Story 21.8 completed.
- The story requires non-destructive staging, atomic cutover, real rollback, owner-locked marker semantics, and an operator-safe abort path.
- The story explicitly carries forward Story 19.4 migration-marker target-consistency and operator-recovery clusters.
- The story keeps broader migration integration coverage visible as Story 21.10 while requiring focused A5 tests in this story.
- Implemented versioned blue/green schema helpers for active aliases, staging indexes, staging key prefixes, previous aliases, and migration version validation.
- Replaced destructive active-index rebuild with staging index creation and staging vector writes; legacy NL namespace migration still runs before staging preparation.
- Added owner-locked marker semantics using Redis `SET NX` with TTL, heartbeat renewal, owner-checked completion/cutover/rollback/abort, target-match resume, and stale-lock recovery hooks.
- Implemented RediSearch alias-based cutover and rollback path; active search falls back to legacy physical indexes for existing tenants that do not yet have aliases.
- Added `--abort` mode, updated operator prompts/help, and replaced destructive Path A documentation with a blue/green runbook.
- Added focused service, store, schema, and tool parser tests; full server test assembly passed via the in-process xUnit v3 runner.
- Review fixed resume so owner/status/heartbeat refresh preserves original previous config and target fields needed for rollback.
- Review fixed tenant deletion cleanup so versioned staging RediSearch indexes for the deleted tenant are discovered through `FT._LIST` and dropped with `DD`.

### Change Log

- 2026-07-04: Implemented blue/green embedding migration with staging indexes/keys, alias cutover, rollback, owner lock/TTL/heartbeat marker, abort mode, docs, and focused tests.
- 2026-07-04: Senior developer review auto-fixes preserved rollback marker fields on resume and added tenant staging-index deletion coverage.

### File List

- `_bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/embedding-providers.md`
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs`
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationLease.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarker.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMode.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationOptions.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`
- `src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs`
- `src/Hexalith.Memories.Server/Migration/MigrationMarkerStatus.cs`
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`
- `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs`
- `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteRedisVectorActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/MigrateEmbeddingVectorsToolTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs`
- `tools/MigrateEmbeddingVectors/Program.cs`

## Senior Developer Review (AI)

Reviewer: Codex GPT-5 on 2026-07-04

Outcome: Approved after automatic fixes. Critical issues remaining: 0.

Findings fixed:

- HIGH: `RedisEmbeddingMigrationStore.StartMigrationMarkerAsync` rewrote full marker contents on resume, including previous provider/config and previous raw/NL targets. A rollback command starts through the resume path, so this could replace the original rollback snapshot with the current target before `RollbackMigrationAsync` reads it. Fixed by writing only owner, status, migration version, heartbeat, and expiry fields during resume; added `StartMigrationMarkerAsync_ResumePreservesPreviousRollbackFields`.
- MEDIUM: tenant deletion swept staging vector hashes but did not drop versioned staging RediSearch indexes. Deleted tenants could leave orphaned `*:memories:vec:staging:*` and `*:memories:vec:nl:staging:*` indexes. Fixed `DeleteRedisVectorActivity` to discover tenant staging indexes through `FT._LIST` and drop them with `DD`; added `RunAsync_DeletesTenantStagingIndexes`.

Validation:

- MCP doc search was not available in this session; web fallback checked official Redis docs for `SET` with `NX`/expiry and `FT.ALIASUPDATE`.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~DeleteRedisVectorActivityTests|FullyQualifiedName~RedisEmbeddingMigrationStoreTests" --logger "console;verbosity=normal"` failed before discovery with `SocketException (13): Permission denied`.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` passed: 2196 total, 0 failed, 1 skipped.
- `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` passed.
