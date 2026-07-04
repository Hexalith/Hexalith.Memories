---
baseline_commit: c350b7ab420d
---

# Story 21.3: Natural-Language Vector Namespace Separation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want NL vectors on a disjoint key namespace,
so that consistency verification, repair, and raw KNN search stop being corrupted by nested prefixes.

## Acceptance Criteria

1. Given `SemanticKeyPrefixSuffix = ":vec:"` and `NaturalLanguageSemanticKeyPrefixSuffix = ":vec:nl:"` in `IndexSchemaDefinitions`, when NL hashes are stored and a tenant is verified/repaired, then NL keys live under a disjoint prefix, the raw index is rebuilt with a non-overlapping prefix, existing data is migrated, and a regression test enumerating a tenant with NL hashes shows zero phantom discrepancies and no repair-workflow crash. Closes A4.

## Tasks / Subtasks

- [x] Task 1 - Re-run the A4 anchor preflight before editing (AC: 1)
  - [x] Confirm `IndexSchemaDefinitions.SemanticKeyPrefixSuffix` is still `":vec:"` and `NaturalLanguageSemanticKeyPrefixSuffix` is still nested under it.
  - [x] Confirm `EnumerateMemoryUnitIdsActivity` still scans raw semantic keys using the raw semantic prefix and would treat `tenant:vec:nl:<id>` as a raw ID of `nl:<id>`.
  - [x] Confirm `VerifyConsistencyActivity`, `ConsistencyInspectionService`, cleanup/projection deletion activities, and migration code still reference the old NL prefix directly or through `IndexSchemaDefinitions`.
  - [x] Confirm existing raw semantic search, NL semantic search, tenant provisioning, tenant deletion, consistency repair, and embedding migration tests still compile around the current key scheme.
  - [x] Record current anchors and any moved paths in this story's Dev Agent Record before changing code.

- [x] Task 2 - Define and apply a disjoint NL semantic key namespace (AC: 1)
  - [x] Change the NL semantic hash prefix to a tenant-local prefix that does not start with the raw semantic key prefix. Recommended shape: `{tenantId}:vecnl:{memoryUnitId}` via `NaturalLanguageSemanticKeyPrefixSuffix = ":vecnl:"`.
  - [x] Keep the public NL index name stable unless a deliberate migration reason requires changing it: `{tenantId}:memories:vec:nl`.
  - [x] Add or update `IndexSchemaDefinitions` helpers/tests so `GetNaturalLanguageSemanticKeyPrefix(tenant)` does not start with `GetSemanticKeyPrefix(tenant)`, and raw semantic keys do not start with the NL prefix.
  - [x] Do not solve Story 21.4's full key-builder sweep here. It is acceptable to update A4-owned call sites to use `IndexSchemaDefinitions`; leave the repo-wide grep guard and complete `Build{Syntactic,Semantic,NlSemantic}Key` helper rollout to 21.4 unless a local helper is needed to make 21.3 safe.

- [x] Task 3 - Update all A4-owned read/write/delete paths to the new NL prefix (AC: 1)
  - [x] Update NL writes in `IndexNaturalLanguageSemanticActivity` and `RedisEmbeddingMigrationStore.WriteNaturalLanguageSemanticAsync`.
  - [x] Update NL reads/counts in `NaturalLanguageSemanticSearchService`, `VerifyConsistencyActivity`, `ConsistencyInspectionService`, and `RedisEmbeddingMigrationStore.GetNaturalLanguageSemanticUnitAsync` / count logic.
  - [x] Update NL cleanup/deletion in `CleanupSemanticActivity`, `DeleteMemoryUnitProjectionActivity`, `DeleteCaseProjectionActivity`, and tenant deletion paths.
  - [x] Keep tenant isolation strict: no tenant-wide scan, migration, deletion, or search may touch another tenant's key namespace.
  - [x] Preserve Story 21.2's EventStore command boundary and projection workflow compensation model; Redis vector hashes remain rebuildable projections, not source-of-truth domain state.

- [x] Task 4 - Migrate existing nested NL hashes without data loss (AC: 1)
  - [x] Add a focused migration path that copies or atomically renames existing `{tenantId}:vec:nl:{memoryUnitId}` hashes to the new disjoint prefix, preserving `embedding`, `memoryUnitId`, `caseId`, `naturalLanguageDescription`, confidence fields, provider/model/dimensions, and any metadata added by Stories 13/15/21.2.
  - [x] Make migration idempotent: rerunning after a partial failure must converge without duplicating hashes, deleting the only good copy, or corrupting dimensions/provider metadata.
  - [x] Rebuild or recreate the raw semantic RediSearch index with a prefix that no longer includes NL hashes. Do not rely on `FT.ALTER`; prefix is part of `FT.CREATE` index definition, and changing it requires a rebuild path.
  - [x] Ensure the NL semantic index validates against the new prefix using `DescribeVectorSchemaProblems`.
  - [x] Document or expose the migration trigger in the existing operator/developer docs if it is not automatic during provisioning/startup.

- [x] Task 5 - Add regression coverage for phantom IDs and repair stability (AC: 1)
  - [x] Add a unit test proving `EnumerateMemoryUnitIdsActivity` with one raw key `tenant:vec:mu-1` and one NL key under the new prefix returns `mu-1` only once and never returns `nl:mu-1`.
  - [x] Add an integration or focused unit-level regression proving a tenant with syntactic + raw semantic + graph + NL hashes produces zero phantom consistency discrepancies.
  - [x] Add a repair-workflow regression where NL hashes exist but raw semantic/graph/syntactic state is otherwise consistent; the workflow must not dispatch repair for a phantom `nl:<id>` unit and must not crash.
  - [x] Update `IndexNaturalLanguageSemanticActivityTests`, `IndexSchemaDefinitionsTests`, `VerifyConsistencyActivityTests`, `ConsistencyInspectionServiceTests`, provisioning tests, tenant deletion tests, and migration tests for the new prefix.
  - [x] Add raw semantic KNN coverage or existing semantic search coverage proving the raw index does not return NL-only hashes after the migration/rebuild.

- [x] Task 6 - Validate and document completion (AC: 1)
  - [x] Update docs that publish the old NL key shape, especially `docs/dev/eventstore-integration.md`, `docs/governance/PII_ACKNOWLEDGMENT.md`, and operations/provider docs if they mention `{tenant}:vec:nl:*`.
  - [x] Run focused tests for indexing, consistency verification/repair, NL semantic indexing/search, tenant provisioning/deletion, and embedding migration.
  - [x] Run `dotnet build Hexalith.Memories.slnx` before handoff because this story changes shared server indexing and consistency behavior.
  - [x] Record migration behavior, validation commands, and any blocked tests in the Dev Agent Record.

## Dev Notes

Story 21.3 closes audit finding A4. The root problem is prefix containment: raw semantic keys use `{tenant}:vec:{memoryUnitId}`, while NL semantic keys use `{tenant}:vec:nl:{memoryUnitId}`. Any raw semantic prefix scan or RediSearch index that matches `{tenant}:vec:*` also sees NL hashes, producing phantom memory-unit IDs such as `nl:<memoryUnitId>`, raw KNN contamination, and repair-workflow instability. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.3; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A4]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key section is Epic 21 and Story 21.3.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant sections are RediSearch/Vector schema immutability, multi-backend consistency, D3, and tenant isolation.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR13, FR39, FR73, FR74, NFR8, NFR15, and NFR16-NFR19.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope, but consistency output must preserve trust/recovery semantics.
- Loaded persistent facts from `_bmad-output/project-context.md` and root-declared reference project contexts under `references/`.
- Loaded Hexalith state instructions because this story changes persisted projection/read-model keys and migration behavior.
- Loaded previous Stories 21.1 and 21.2, Epic 20 handoff notes, architecture audit A4, current code anchors, current tests, and official Redis Search command docs.

### Current Code Anchors

Re-verified during story creation on 2026-07-04 against `HEAD` `c350b7ab420d`:

- `IndexSchemaDefinitions` currently defines `SemanticKeyPrefixSuffix = ":vec:"` and `NaturalLanguageSemanticKeyPrefixSuffix = ":vec:nl:"`. `IndexSchemaDefinitionsTests.NaturalLanguageKeyPrefix_DoesNotCollideWithSemanticKeyPrefix` currently asserts `nl.ShouldStartWith(raw)`, which pins the broken containment behavior and must be inverted. [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs; tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs]
- `EnumerateMemoryUnitIdsActivity` scans syntactic, semantic, and graph sources. The semantic scan uses pattern `{tenant}:vec:*` and strips `{tenant}:vec:` from every match; with the old NL prefix, `tenant:vec:nl:mu-1` becomes phantom ID `nl:mu-1`. [Source: src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs]
- `IndexNaturalLanguageSemanticActivity` already writes through `IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix`, so it should follow the new prefix once the constant/helper changes. It also validates existing NL index prefix via `DescribeVectorSchemaProblems`. [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs]
- `ProvisionRedisVectorActivity` creates both raw and NL indexes and verifies their configured prefixes. Existing tenants with the old NL index prefix will fail validation unless a migration/rebuild path updates the index definition. [Source: src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs]
- `VerifyConsistencyActivity` and `ConsistencyInspectionService` probe NL hashes with the old direct `:vec:nl:` key shape; both need to follow the schema definition, not hard-coded strings. [Source: src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs; src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs]
- `CleanupSemanticActivity`, `DeleteMemoryUnitProjectionActivity`, and `DeleteCaseProjectionActivity` delete old NL keys through string interpolation. Update them to delete the new prefix and consider old-prefix cleanup during migration only. [Source: src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs; src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs; src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs]
- `RedisEmbeddingMigrationStore` already separates raw and NL scans and explicitly skips `nlPrefix` while scanning raw keys, which is a workaround for the broken nested prefix. After 21.3, the skip should remain harmless but should not be the only correctness guard. [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs]
- Story 21.2 added projection workflows and cleanup activities that delete NL keys as projection state. Keep those paths idempotent and compensation-friendly. [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md#Completion-Notes-List]

### Architecture Compliance

- RediSearch/Vector index schemas are effectively immutable after creation; architecture requires additive-only schema evolution or a create-backfill-switch migration pattern. Prefix changes are not an in-place schema alteration. [Source: _bmad-output/planning-artifacts/architecture.md#Technology-Risks]
- D3 says EventStore events are the domain source of truth and Redis/FalkorDB/activity records are rebuildable projections. This story must not introduce Redis vector hashes as authoritative domain data. [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency; _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md#Implementation-Notes]
- Dapr workflows remain the preferred mechanism for durable multi-step projection repair/migration. Keep workflow orchestration replay-safe and put Redis I/O in activities. [Source: _bmad-output/project-context.md#Framework-Specific-Rules]
- Tenant isolation is physical and prefix/index based. Any migration or scan must operate on a single tenant's declared prefixes and indexes only. [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns; _bmad-output/project-context.md#Critical-Dont-Miss-Rules]

### Implementation Guardrails

- Use `IndexSchemaDefinitions` for A4-owned prefixes and index-prefix validation. Do not add new raw `":vec:nl:"` literals.
- Treat old-prefix data as migration input only. New writes must go exclusively to the disjoint NL prefix.
- Do not change natural-language description prompt behavior, LLM/cache policy, metadata persistence policy, provider selection, or embedding dimensions. This story is key namespace separation plus migration/rebuild.
- Do not fold Story 21.4 into this story. If a small key helper is introduced for safety, keep it local to semantic/NL keys and document that the repo-wide raw literal purge remains Story 21.4.
- Preserve Epic 20 security guardrails: authenticated APIs, tenant authorization, principal-derived audit identity, rate limiting, and RediSearch escaping must not regress during indexing changes.
- Preserve Story 21.2 behavior: command accept happens before projection fan-out; projection cleanup remains idempotent; failed projection work remains retryable/rebuildable.
- Keep package versions centralized. No package upgrade is required.
- Do not initialize or update nested submodules.

### Migration Design Notes

The safest implementation shape is:

1. Define the new NL prefix in `IndexSchemaDefinitions`, update all new writes and reads to that prefix, and keep a named old-prefix helper only inside migration code.
2. For each tenant, scan old NL keys under `{tenant}:vec:nl:*`, copy/hash-set the same fields to `{tenant}:vecnl:*` or atomically rename where safe, verify the target hash, then delete the old key only after target verification.
3. Drop and recreate the NL semantic index or otherwise rebuild it so `FT.INFO` reports the new prefix. If raw semantic index creation previously indexed NL keys because of prefix containment, rebuild the raw semantic index as part of the migration path or provide an explicit operator rebuild step.
4. Re-run consistency verification and repair against the tenant; no phantom `nl:<id>` unit should be enumerated.

Official Redis Search docs confirm `FT.CREATE` uses `PREFIX` to select keys to index and `FT.DROPINDEX index DD` deletes indexed document keys; without `DD`, it drops only the index. Use this carefully during migration so the only valid copy of an NL hash is not deleted by a drop/rebuild operation. [Source: https://redis.io/docs/latest/commands/ft.create/; https://redis.io/docs/latest/commands/ft.dropindex/]

### Previous Story Intelligence

Story 21.1 ratified EventStore aggregates with rebuildable projections. Story 21.3 operates on Redis Vector projection keys and must not reopen source-of-truth semantics. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md]

Story 21.2 routed case and memory-unit mutations through an EventStore command boundary followed by workflow-owned projection fan-out. It added cleanup paths that explicitly delete raw and NL vector hashes; those paths are now part of the A4 blast radius. [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]

Epic 20 handoff says Epic 21 stories must keep audit-anchor preflight discipline, re-check moved line numbers, preserve security regression guards, and review documentation drift after each story. [Source: _bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md]

### Git Intelligence

Recent commits:

- `c350b7a feat(story-21.2): Transactional Multi-Backend Mutation`
- `53cc9c2 feat(story-21.1): Consistency Model Decision`
- `8a37253 docs(epic-20): close retrospective and sync operations docs`
- `5b2b117 feat(story-20.6): RediSearch Query-Injection Hardening`
- `d942058 feat(story-20.5): Inbound Rate Limiting, Quotas & Audit Completeness`

The current pattern is narrow audit-remediation work with explicit code anchors, focused regression tests, and documentation guardrails. Continue that pattern; do not turn 21.3 into a general indexing or search refactor.

### Latest Technical Notes

- Repo-pinned Redis/NRedisStack behavior should be used; no dependency upgrade is in scope. Redis Search docs still define `FT.CREATE ... PREFIX` as the way an index filters keys and show no in-place prefix mutation path. [Source: Directory.Packages.props; https://redis.io/docs/latest/commands/ft.create/]
- `FT.DROPINDEX index DD` deletes document keys as well as the index and is asynchronous. Do not use `DD` while the old prefix contains the only copy of NL hashes. Prefer copy/verify/delete or drop-without-DD plus explicit key cleanup once target keys are verified. [Source: https://redis.io/docs/latest/commands/ft.dropindex/]

### Project Structure Notes

Likely update paths:

- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorActivity.cs`
- `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs`
- `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs`
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/*`
- `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRedisVectorActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteRedisVectorActivityTests.cs`
- migration-focused tests under existing server/integration test folders
- docs publishing the old key shape under `docs/dev`, `docs/operations`, and `docs/governance`

Out of scope:

- Story 21.4's complete key-schema single source of truth and CI grep guard.
- Story 21.5 deletion completeness for aggregate-case-map/router cache and broader tenant key sweep.
- Story 21.9 and 21.10 blue/green embedding migration strategy beyond the minimum old-NL-key migration required here.
- Story 23 scalability/performance cleanup for directory scans.
- Story 24 physical isolation decisions.
- Wiring `axis=nl` into public hybrid search; audit A50 owns stranded NL search feature work.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Keep test names descriptive and behavior-focused.
- Unit tests should pin prefix non-containment, exact keys used by NL writes/reads/deletes, and phantom-ID enumeration prevention.
- Workflow tests should prove repair does not dispatch work for `nl:<id>` phantom units and still handles real raw semantic/graph discrepancies.
- Integration or end-state tests should assert persisted Redis key contents and RediSearch index prefixes where practical, not only mock calls.
- Run focused server tests for indexing, consistency, workflows, tenant vector provisioning/deletion, and migration. Then run `dotnet build Hexalith.Memories.slnx`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.3 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A4 - nested NL vector prefix finding]
- [Source: _bmad-output/planning-artifacts/architecture.md#Technology-Risks - RediSearch/Vector schema evolution constraints]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source-of-truth and rebuildable projections]
- [Source: _bmad-output/project-context.md - repo-wide C#, Dapr workflow, testing, package, tenant isolation, and submodule rules]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - Hexalith.EventStore persistence and projection rules]
- [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md - ratified consistency model]
- [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md - projection workflow and cleanup patterns]
- [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs - current raw and NL semantic prefixes]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs - phantom raw semantic prefix scan]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs - NL write and prefix validation]
- [Source: src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs - raw and NL index creation]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs - consistency NL key probe]
- [Source: src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs - per-unit inspection NL key probe]
- [Source: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs - embedding migration raw/NL scan and write paths]
- [Source: tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs - currently broken non-collision assertion]
- [Source: https://redis.io/docs/latest/commands/ft.create/ - Redis Search prefix behavior]
- [Source: https://redis.io/docs/latest/commands/ft.dropindex/ - Redis Search index drop and DD behavior]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-04: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Stories 21.1 and 21.2, audit A4, current code anchors, current tests, recent commits, and official Redis Search docs.
- 2026-07-04: story target came from user request `21.3`; sprint status has `21-3-natural-language-vector-namespace-separation: backlog` and `epic-21: in-progress`.
- 2026-07-04: current implementation anchors rechecked at `c350b7ab420d`. NL semantic keys still nest under raw semantic keys; `EnumerateMemoryUnitIdsActivity` still scans the raw semantic prefix; Story 21.2 cleanup paths still delete old NL key strings.
- 2026-07-04: no module UI work detected; Hexalith UX instructions were loaded only through planning UX context and no UI implementation is in scope.
- 2026-07-04: dev-story activation loaded BMAD workflow customization, BMAD config, root and reference project-context facts, Hexalith LLM/state instructions, the story, sprint status, and the enhanced DoD checklist.
- 2026-07-04: A4 preflight confirmed `SemanticKeyPrefixSuffix = ":vec:"`, `NaturalLanguageSemanticKeyPrefixSuffix = ":vec:nl:"`, raw enumeration still scanned `{tenant}:vec:*`, and direct old NL key usage existed in consistency/projection cleanup paths. Focused preflight compile succeeded after using `-m:1 /nodeReuse:false`; VSTest execution was blocked by sandbox TCP listener creation.
- 2026-07-04: implemented disjoint NL key prefix `:vecnl:` with legacy `:vec:nl:` retained only as a named migration input prefix.
- 2026-07-04: added `RedisNaturalLanguageNamespaceMigrator` and wired it into embedding migration index rebuild. It scans only a single tenant's legacy NL prefix, copies required hash fields to the disjoint prefix, verifies the target hash, then deletes the legacy key. Existing verified target hashes are not overwritten.
- 2026-07-04: updated A4-owned consistency, inspection, projection cleanup, tenant deletion comments, and schema validation tests to use the new prefix through `IndexSchemaDefinitions`.
- 2026-07-04: validation used in-process xUnit runner because `dotnet test`/VSTest starts a TCP listener blocked by this sandbox (`SocketException 13`).
- 2026-07-04: senior developer review loaded the story, sprint status, project context, Hexalith LLM/state instructions, Epic 21/A4 planning anchors, and changed implementation/test files. Review found and fixed a migration inventory bug where legacy `{tenant}:vec:nl:*` hashes were still counted as raw semantic hashes after the prefix change.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.3 created as the A4 implementation story after Story 21.1 and 21.2 completed.
- Story scope is natural-language vector namespace separation, existing-data migration, raw index rebuild/prefix correctness, and phantom consistency discrepancy regression coverage.
- No production code changes are part of this create-story run.
- Implemented new NL semantic key namespace `{tenant}:vecnl:{memoryUnitId}` while keeping the public NL index name `{tenant}:memories:vec:nl`.
- Existing direct NL key reads/deletes now route through `IndexSchemaDefinitions`; raw and NL prefixes are explicitly non-overlapping.
- Live embedding migration now migrates legacy `{tenant}:vec:nl:*` hashes before dropping/recreating raw and NL indexes without `DD`, preserving hash fields and avoiding deletion before target verification.
- Added regression coverage for prefix non-containment, phantom-ID prevention, repair workflow stability, migration idempotency/field preservation, and raw-index prefix exclusion.
- Senior review fixed migration inventory counting so legacy `{tenant}:vec:nl:*` hashes are excluded from raw semantic counts and included in natural-language semantic counts until namespace migration converges.
- Updated developer, governance, and operations docs to publish `{tenant}:vecnl:*` and the migration trigger.
- Validation passed for focused A4 tests, the full Server test assembly, Contracts, EventStore, MCP, Web tests, and `dotnet build Hexalith.Memories.slnx`.
- Senior review validation passed: migration-store focused tests (2 total), A4 focused server test set (66 total), and `dotnet build Hexalith.Memories.slnx`.
- Broader CLI test assembly has unrelated pre-existing failures: `RATE_LIMIT_EXCEEDED` is missing from the CLI error catalog, and quickstart port checks cannot bind sockets in this sandbox (`SocketException 13`).

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-04

Outcome: Approved after automatic fixes. Story 21.3 status set to `done`; sprint status synced to `done`.

Findings fixed:

- [x] [AI-Review][High] `RedisEmbeddingMigrationStore.GetCountsAsync` only skipped the new `:vecnl:` prefix after the namespace change. Legacy `{tenant}:vec:nl:*` hashes still matched the raw `{tenant}:vec:*` scan, so dry-run/live inventory could count old NL hashes as raw semantic data and miss them in NL counts. Fixed by skipping the legacy prefix during raw counting and counting both legacy and new NL prefixes by memory-unit ID. [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`]
- [x] [AI-Review][Medium] The story File List did not include the new `RedisEmbeddingMigrationStoreTests.cs` regression coverage referenced by the test summary. Fixed by adding the test file to the File List. [`tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs`]

Review validation:

- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~RedisEmbeddingMigrationStoreTests" -m:1 /nodeReuse:false` built successfully, then VSTest aborted on sandbox TCP listener permission (`SocketException 13`).
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests` - 2 total, 0 failed.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Migration.RedisNaturalLanguageNamespaceMigratorTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.EnumerateMemoryUnitIdsActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.VerifyConsistencyActivityTests -class Hexalith.Memories.Server.Tests.Consistency.ConsistencyInspectionServiceTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.ProvisionRedisVectorActivityTests -class Hexalith.Memories.Server.Tests.Workflows.ConsistencyRepairWorkflowTests` - 66 total, 0 failed.
- `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false` - passed, 0 warnings, 0 errors.

### File List

- `_bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/dev/eventstore-integration.md`
- `docs/governance/PII_ACKNOWLEDGMENT.md`
- `docs/operations/embedding-providers.md`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorActivity.cs`
- `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs`
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`
- `src/Hexalith.Memories.Server/Migration/RedisNaturalLanguageNamespaceMigrator.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Cases/DeleteMemoryUnitProjectionActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/CleanupActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/VerifyConsistencyActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRedisVectorActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisNaturalLanguageNamespaceMigratorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs`

### Change Log

- 2026-07-04: Implemented Story 21.3 natural-language vector namespace separation, legacy NL hash migration, raw/NL index rebuild safety, regression coverage, docs updates, and validation.
- 2026-07-04: Senior developer review auto-fixed legacy NL migration inventory counting, added focused regression coverage, updated File List, validated focused tests/build, and marked the story done.
