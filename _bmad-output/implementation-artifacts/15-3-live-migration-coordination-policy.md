# Story 15.3: Live Migration Coordination Policy

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want live embedding-vector migration to coordinate with concurrent ingestion,
so that a tenant cannot finish migration with mixed provider/model vector state.

## Acceptance Criteria

1. Given migration currently updates tenant config before enumerating syntactic units, when migration cutover begins for a tenant, then deferred ID `13.6-RV1` is resolved by the selected policy: a durable tenant-scoped migration marker that is visible to ingestion and semantic vector write activities and prevents old-provider/model/dimension vector writes for that tenant while the marker is active.

2. Given the migration service exposes operator-visible failures, when expected business failures are represented, then deferred ID `13.6-RV3` is resolved with `ValueOrError<T>` or equivalent project-approved result semantics, or accepted with a specific architectural rationale.

3. Given migration coordination changes runtime behavior, when tests run, then coverage proves both stale-config ingestion and post-marker ingestion cannot persist old-provider vectors after the migration cutover point.

4. Given operator guidance is part of the safety contract, when the story completes, then `docs/operations/embedding-providers.md` or the migration runbook documents the coordination policy, abort/resume expectations, and any ingestion downtime requirement.

## Tasks / Subtasks

- [ ] Task 0 - Verify migration and ingestion state before choosing policy (AC: 1-4)
  - [ ] Read `EmbeddingVectorMigrationService.cs`, `IEmbeddingMigrationStore.cs`, `RedisEmbeddingMigrationStore.cs`, `GenerateEmbeddingActivity.cs`, `EmbeddingVectorMigrationServiceTests.cs`, `docs/operations/embedding-providers.md`, and the `13.6-RV1` / `13.6-RV3` entries in `deferred-work.md` before editing.
  - [ ] Confirm Stories 13.6, 13.7, 14.4, and 15.2 are not actively `in-progress` or `review`; if another migration/provider story is active, stop and record the exact status.
  - [ ] Identify the current cutover order in live migration: marker start, semantic index drop/recreate, tenant config write, raw migration, natural-language migration, marker completion.
  - [ ] Identify all ingestion paths that can write raw or natural-language semantic vectors during that window, including DAPR workflow retries and `GenerateEmbeddingActivity` config reads.

- [ ] Task 1 - Implement the selected coordination policy (AC: 1, 3, 4)
  - [ ] Use the selected policy: durable tenant-scoped migration marker enforcement. Do not switch to operator downtime, global pause, dual-write, or search fan-out unless the story is explicitly corrected again before development.
  - [ ] Treat cutover as beginning immediately after `StartMigrationMarkerAsync(...)` succeeds and before semantic indexes are dropped/recreated or tenant config is written.
  - [ ] While the active marker exists for a tenant, semantic vector writes for that tenant must not persist a vector whose provider/model/dimensions do not match the marker target.
  - [ ] In-flight ingestion work that read old config before cutover must be blocked, retried, or fail closed at the semantic write boundary; it must not silently write stale provider/model metadata.
  - [ ] Ingestion work that starts after the marker is active must observe the marker before expensive provider calls where practical, and must also be protected at the final write boundary.
  - [ ] Resume must keep the active marker authoritative until a clean migration completion stamps the marker complete; interrupted runs must not clear the protection early.

- [ ] Task 2 - Add the durable marker read/write guard (AC: 1, 3)
  - [ ] Extend the migration store boundary, or add a narrow read-gate service, so runtime ingestion/indexing code can read an active tenant migration marker and target config without depending on static process memory.
  - [ ] Ensure `StartMigrationMarkerAsync(...)` records enough target data for the runtime gate: tenant ID, target provider, target model, target dimensions, and active/completed status.
  - [ ] Guard `GenerateEmbeddingActivity` or the closest practical pre-provider-call boundary so post-marker ingestion avoids avoidable old-config provider calls.
  - [ ] Guard both semantic write activities: `IndexSemanticActivity` for raw payload vectors and `IndexNaturalLanguageSemanticActivity` for natural-language vectors. These are mandatory because stale workflows may already hold an old `EmbeddingResult` before migration cutover.
  - [ ] The write guard must compare the vector write input's provider, model, and dimensions against the active marker target. If they differ, fail closed with a retryable or automation-readable failure that preserves existing workflow retry/compensation semantics.
  - [ ] Do not block unrelated tenants. Do not make the marker a global ingestion pause.
  - [ ] Keep secret and provider error redaction behavior from Stories 14.3 and 14.4 unchanged.

- [ ] Task 3 - Resolve migration result-surface policy for `13.6-RV3` (AC: 2)
  - [ ] Reassess `ValidateOptions(...)`, `TryBuildTargetConfig(...)`, tenant-level errors, and CLI exit behavior.
  - [ ] Convert expected failures to `ValueOrError<T>` only if it can be done locally without adding broad `Hexalith.Commons` references or changing public result contracts unnecessarily.
  - [ ] If retaining local string/tuple result helpers, document why `EmbeddingMigrationResult` plus stable exit codes is the approved equivalent for this command surface.
  - [ ] Add focused tests for any changed result path so invalid options, invalid target config, tenant-level coordination failures, cancellation, and resume failures remain automation-readable.
  - [ ] Mark `13.6-RV3` resolved, accepted, or carried-forward in `deferred-work.md` with the exact rationale/evidence.

- [ ] Task 4 - Update operator guidance and deferred-work dispositions (AC: 1-4)
  - [ ] Update `docs/operations/embedding-providers.md` with the final coordination policy, required operator steps, abort/resume behavior, and whether ingestion downtime is required.
  - [ ] Add a Story 15.3 rollup section to `_bmad-output/implementation-artifacts/deferred-work.md`.
  - [ ] Mark `13.6-RV1` and `13.6-RV3` as `resolved`, `accepted`, or `carried-forward` using the Story 14.5 structured fields: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [ ] Do not sweep adjacent migration IDs such as `13.6-RV2`, `13.6-RV4`, `13.6-RV5`, or provider-registry IDs unless implementation genuinely resolves them and the story records why they became in scope.
  - [ ] Preserve historical context; add structured disposition blocks rather than deleting original review notes.

- [ ] Task 5 - Validate coordination behavior (AC: 1-4)
  - [ ] Add a deterministic stale-config race test: ingestion reads or produces an old-provider embedding result, migration marker becomes active, then the raw semantic write attempts to persist; expected result is no old-provider raw semantic hash persisted.
  - [ ] Add the same deterministic race coverage for the natural-language semantic write path.
  - [ ] Add a post-marker ingestion test showing new ingestion observes the active marker before or during generation and does not persist old-provider vectors.
  - [ ] Add a resume/interruption test proving the active marker remains protective until clean completion and is not cleared by tenant-level errors, per-unit failures, or cancellation.
  - [ ] Run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingVectorMigrationServiceTests"`.
  - [ ] If ingestion activity code changes, run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~GenerateEmbeddingActivity"`.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` when the local SDK permits it.
  - [ ] Run `git diff --check -- src/Hexalith.Memories.Server/Migration src/Hexalith.Memories.Server/Activities/Ingestion src/Hexalith.Memories.Server/Actors tests/Hexalith.Memories.Server.Tests/Migration tests/Hexalith.Memories.Server.Tests/Activities/Ingestion docs/operations/embedding-providers.md _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md`.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` - UPDATE. Coordination policy enforcement, tenant-level migration ordering, result-surface adjustments, and resume behavior.
- `src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs` - UPDATE. Add or expose the durable active-marker read boundary if this remains the narrowest project-local home.
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs` - UPDATE. Persist/read active marker target state and status through Redis/DAPR without process-local coordination.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - UPDATE. Add the post-marker pre-provider-call guard if the runtime marker reader can be injected cleanly.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` - UPDATE. Mandatory raw semantic write guard so stale old-provider embedding results cannot be persisted after cutover.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs` - UPDATE. Mandatory natural-language semantic write guard so stale old-provider embedding results cannot be persisted after cutover.
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs` - UPDATE. Coordination, race, resume, expected-failure, and documentation-evidence tests.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs` - UPDATE only with matching pre-provider-call marker coverage.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` - UPDATE only with matching activity behavior coverage.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs` - UPDATE or CREATE if no focused raw semantic indexing test file exists, covering stale marker write rejection and unchanged normal writes.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs` - UPDATE or CREATE if no focused NL semantic indexing test file exists, covering stale marker write rejection and unchanged normal writes.
- `docs/operations/embedding-providers.md` - UPDATE. Operator coordination policy, downtime/drain expectations, abort/resume instructions, and verification steps.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Structured dispositions for `13.6-RV1` and `13.6-RV3`.
- `_bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Possible files only if the selected policy proves they are necessary:

- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` - UPDATE only if the policy belongs in tenant configuration state and cannot be represented through the migration store boundary.
- `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs` - UPDATE only if actor contract expansion is required and documented.
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarker*.cs` or equivalent NEW small record/service file - CREATE only if needed to keep marker read semantics typed and testable.
- `tools/MigrateEmbeddingVectors/Program.cs` - UPDATE only if CLI flags/help/output must expose the selected coordination policy.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md`
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md`
- `_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md`
- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationResult.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationOptions.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationExitCodes.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`

Forbidden by default:

- `.github/**`
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `tests/Hexalith.Memories.IntegrationTests/**`
- `Directory.Packages.props`
- `Directory.Build.props`
- `NuGet.config`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`
- Any submodule pointer change

## Dev Notes

### Current Implementation State

`EmbeddingVectorMigrationService.LiveAsync(...)` currently starts a migration marker, drops and recreates semantic indexes, writes the target tenant embedding config with `forceReindex: true`, then enumerates syntactic units for raw and natural-language re-embedding. This order reduces dimension mismatch between config and indexes, but it leaves a real race window: an ingestion workflow that already read the old tenant config can still write an old-provider semantic hash after the migration cutover, and that fresh unit may not be picked up by the current enumeration.

`GenerateEmbeddingActivity` reads `TenantConfigurationActor.GetEmbeddingConfigAsync()`, primes credentials, sets the per-tenant rate limiter ceiling, and calls `EmbeddingClient.GenerateAsync(...)`. It returns `provider:model` and `Dimensions` from the config it read. It does not currently check migration marker state or a tenant pause/lock before generating or writing embeddings.

`RedisEmbeddingMigrationStore` already has durable migration marker methods, but the marker is local to migration and is not checked by ordinary ingestion. It stores target provider/model/dimensions and started/completed status in Redis. The marker key sanitizes provider/model segments and lowercases them.

`IndexSemanticActivity` and `IndexNaturalLanguageSemanticActivity` are the final Redis write boundaries for raw and natural-language semantic vectors. Both accept provider/model/dimensions from workflow activity input and write them to Redis hash metadata. A stale workflow can therefore bypass a generation-only guard if it generated an old-provider vector before cutover and reaches indexing after the marker becomes active. Story 15.3 must protect these write boundaries.

`EmbeddingMigrationResult` is the automation-readable result surface for the migration tool. `ValidateOptions(...)` and `TryBuildTargetConfig(...)` use local nullable string/error returns, and `RunAsync(...)` maps them to `EmbeddingMigrationExitCodes.Plumbing`; tenant-level and per-unit failures map to domain-error results. Story 14.4 carried `13.6-RV3` forward because adopting `ValueOrError<T>` from `Hexalith.Commons` would have required broader references and contract churn.

### Selected Coordination Policy

Use a durable tenant-scoped migration marker gate. This is the selected policy for development, not a menu.

Cutover begins when `StartMigrationMarkerAsync(...)` succeeds. From that point until clean completion, any semantic vector write for the tenant must either match the marker's target provider/model/dimensions or fail closed before Redis hash persistence. The marker remains protective across resume/interruption and is marked complete only after raw and natural-language re-embedding finish without tenant-level or per-unit failures.

Implementation should add a cheap pre-provider-call check where practical, but the correctness gate is the write boundary in `IndexSemanticActivity` and `IndexNaturalLanguageSemanticActivity`. This avoids the stale-config trap where a workflow generated an old-provider vector before cutover and only reaches indexing after migration is active.

Expected failure behavior: prefer the existing workflow retry/compensation style already used by these activities. The error must be bounded, sanitized, and automation-readable enough for tests and operators to distinguish "blocked by active embedding migration" from provider failure.

Do not treat this as a broad migration redesign. No global ingestion pause, no accepted downtime-only policy, no dual-write, no read-side fan-out, and no provider-registry work.

### Party-Mode Review Clarifications - 2026-05-12

- The primary coordination policy is now chosen: durable tenant-scoped migration marker enforcement visible to ingestion and semantic vector write activities.
- `GenerateEmbeddingActivity` can avoid unnecessary old-config provider calls after the marker is active, but the write-boundary checks in `IndexSemanticActivity` and `IndexNaturalLanguageSemanticActivity` are mandatory because they catch stale workflows that already generated vectors before cutover.
- The cutover invariant must be explicit before dev starts: once tenant migration cutover begins, no semantic vector write for that tenant may persist the old provider/model. The allowed behavior must be one of: block/retry, fail fast with an automation-readable result, drain before cutover, or route to the active migration target.
- Stale-config ingestion is the critical race. Required acceptance evidence should include a deterministic test where ingestion reads the old config, migration reaches cutover, then the ingestion write attempts to persist; the expected result must prove no old-provider semantic vector is stored.
- A second concurrency test should cover ingestion started after the durable migration marker is active. It must observe the selected policy rather than relying on migration-service sequencing alone.
- Assertions should inspect persisted vector metadata/provider/model or the blocking/retry/failure evidence, not only workflow completion status.
- Abort/resume expectations need minimum durable-state coverage: before cutover, after marker creation, after index recreation, after config write, during re-embedding, and after interruption. A richer state machine can remain deferred, but the selected policy must say how the marker is cleared, retained, resumed, or reported.
- AC2 should stay local to migration orchestration/result boundaries touched by this story. If `ValueOrError<T>` is not adopted, the story must name the approved equivalent, such as `EmbeddingMigrationResult` plus stable exit codes and sanitized operator messages, and record why broad contract normalization is deferred.
- Operator documentation must include a compact behavior matrix for active migration, blocked/retried/failed ingestion, abort, resume, and whether tenant-specific ingestion downtime is required.
- Explicit non-goals remain: dual-write, search fan-out, provider-registry changes, token-transport policy, broad rollback design, global ingestion pause, integration fixture expansion, CI/release tooling, and submodule changes.

### Deferred IDs Targeted

This story is the normal lifecycle home for:

- `13.6-RV1`: concurrent ingestion racing migration can leave mixed-provider vector state.
- `13.6-RV3`: migration service expected failures use local string/error returns rather than `ValueOrError<T>`.

Do not close either item by assertion only. `13.6-RV1` needs code/tests or an explicit, enforceable operator policy. `13.6-RV3` needs code/tests or a precise accepted rationale tied to `EmbeddingMigrationResult` and exit-code stability.

### Implementation Guardrails

- Preserve Path A migration semantics unless the chosen coordination policy explicitly changes them.
- Do not add dual-write, search fan-out, versioned-index coexistence, or automatic rollback in this story.
- Do not introduce static in-memory coordination as the source of truth; migration and ingestion can run in separate processes.
- Keep tenant isolation physical and tenant-scoped. Never pause or lock unrelated tenants.
- Keep cancellation behavior controlled: caller cancellation should produce the existing cancelled result path where applicable.
- Preserve redaction for `client_secret`, Google API keys, bearer tokens, AWS keys, JWT-like values, and Basic credentials.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.

### Testing Requirements

Use focused unit tests before considering integration lanes. Minimum evidence:

- A live migration race fixture showing an old-config ingestion write after cutover cannot create a mixed-provider final state, or is blocked by the selected policy.
- A resume fixture proving the coordination state is cleared, retained, or retried correctly after interruption.
- A result-surface fixture proving expected coordination failures are automation-readable and sanitized.
- A documentation assertion or focused text check is optional but useful if the policy is primarily operational.

## Project Structure Notes

This is a migration safety and operations story. Expected implementation stays around migration orchestration, a narrow ingestion check only if needed, focused migration/activity tests, operator documentation, and deferred-work bookkeeping. Provider registry, token endpoint transport rules, AppHost topology, integration fixture expansion, CI workflows, release tooling, and submodules are out of scope.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 15 and Story 15.3 acceptance criteria.
- `_bmad-output/implementation-artifacts/deferred-work.md` - target deferred IDs `13.6-RV1` and `13.6-RV3`.
- `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md` - original Path A migration implementation, live ordering, and deferred race/result findings.
- `_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md` - current migration hardening evidence and carried-forward rationale.
- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md` - adjacent provider validation story; avoid overlapping provider-registry scope.
- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` - current live/dry-run/resume orchestration and expected-failure result paths.
- `src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs` - migration storage boundary.
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs` - Redis/DAPR actor migration store and marker implementation.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - current ingestion config read and embedding generation path.
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs` - current migration service fake-store test harness.
- `docs/operations/embedding-providers.md` - operator migration runbook.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight JSON timestamp `2026-05-12T18:01:03Z` passed all checks with `working tree cleanliness` reporting `0 dirty paths`.
- Story selection chose `15-3-live-migration-coordination-policy` after the repository already contained Story 15.2, leaving `ready_count` at `2`, below the target of `5`, and making this the first backlog story in sprint-status order.
- `/bmad-create-story 15-3-live-migration-coordination-policy` context gathering loaded Epic 15 planning, sprint status, root project context, Story 15.2, Stories 13.6 and 14.4, current deferred-work entries, migration service/store/activity source, migration tests, operator docs, and recent git history.
- No external technology research was needed for this story. The implementation surface is repository-owned migration coordination, ingestion behavior, result semantics, and operator documentation.
- 2026-05-14 create-story update reloaded sprint status, Epic 15 planning, project-context facts, current migration service/store/activity/indexing code, migration tests, operator docs, deferred-work entries `13.6-RV1` and `13.6-RV3`, Stories 13.6/14.4/15.2, and recent git history.

### Completion Notes List

- Story context created on 2026-05-12.
- Scope is limited to live migration coordination policy, expected-failure result semantics, targeted tests, operator guidance, and deferred-work dispositions for `13.6-RV1` and `13.6-RV3`.
- Provider registry work, token transport policy, dual-write/search fan-out, broad rollback, integration fixture expansion, CI/release tooling, and submodules are forbidden by default.
- No submodule state was touched.
- 2026-05-14 update selected the concrete coordination policy: durable tenant-scoped migration marker enforcement visible to ingestion and mandatory at both semantic write boundaries. Story promoted back to `ready-for-dev`.

### File List

- `_bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Party-Mode Review

- Date/time: `2026-05-12T23:04:12+02:00`
- Selected story key: `15-3-live-migration-coordination-policy`
- Command/skill invocation used: `/bmad-party-mode 15-3-live-migration-coordination-policy; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Historical finding, resolved by the 2026-05-14 create-story update: AC1 was too open-ended because the selected coordination policy was still a developer-time choice.
  - The stale-config race is the primary implementation trap: ingestion that read old config before cutover must not persist old-provider vectors after cutover.
  - Existing durable migration markers are insufficient unless ingestion reads or is otherwise governed by them.
  - AC2 risks broad result-contract churn unless the accepted migration-local result semantics are named.
  - Abort/resume behavior needs testable durable-state expectations.
- Changes applied:
  - Added `Party-Mode Review Clarifications - 2026-05-12` with the required pre-dev policy decision, cutover invariant, stale-config race tests, result-surface boundary, operator-doc matrix, and explicit non-goals.
  - Moved story status from `ready-for-dev` to `backlog` because the review recommendation requires a story update before implementation.
- Findings deferred:
  - Product/architecture decision resolved by the 2026-05-14 create-story update: durable tenant-scoped migration marker enforcement with mandatory semantic write-boundary checks is the exact live migration coordination policy.
  - Whether future work should add richer operator pause/drain controls, migration-aware zero-downtime routing, or marker inspection/clearance commands.
  - Whether `ValueOrError<T>` becomes a broader architectural standard beyond migration services.
  - Whether old-provider vector cleanup/enumeration gaps require a follow-up story outside 15.3.
- Final recommendation at review time: `needs-story-update`. Current disposition after 2026-05-14 create-story update: `ready-for-dev`.

### Change Log

- 2026-05-12: Created Story 15.3 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-12: Party-mode review completed; moved story back to `backlog` pending an explicit migration coordination policy decision.
- 2026-05-14: Create-story update selected the durable tenant-scoped migration marker write-gate policy, expanded file scope/tests for raw and natural-language semantic write boundaries, and promoted status to `ready-for-dev`.

## Story Completion Status

Story context updated after party-mode review. The explicit coordination policy is selected, stale-config and write-boundary test requirements are captured, and status is `ready-for-dev`.
