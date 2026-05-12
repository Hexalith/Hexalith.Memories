# Story 15.3: Live Migration Coordination Policy

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want live embedding-vector migration to coordinate with concurrent ingestion,
so that a tenant cannot finish migration with mixed provider/model vector state.

## Acceptance Criteria

1. Given migration currently updates tenant config before enumerating syntactic units, when ingestion starts or resumes during migration, then deferred ID `13.6-RV1` is resolved by a defined coordination policy such as tenant migration lock, ingestion pause/drain, migration-aware ingestion routing, or a deliberately accepted operational constraint.

2. Given the migration service exposes operator-visible failures, when expected business failures are represented, then deferred ID `13.6-RV3` is resolved with `ValueOrError<T>` or equivalent project-approved result semantics, or accepted with a specific architectural rationale.

3. Given migration coordination changes runtime or operator behavior, when tests run, then coverage proves no new old-provider vectors are written after the migration cutover point, or the accepted policy is enforced and documented.

4. Given operator guidance is part of the safety contract, when the story completes, then `docs/operations/embedding-providers.md` or the migration runbook documents the coordination policy, abort/resume expectations, and any ingestion downtime requirement.

## Tasks / Subtasks

- [ ] Task 0 - Verify migration and ingestion state before choosing policy (AC: 1-4)
  - [ ] Read `EmbeddingVectorMigrationService.cs`, `IEmbeddingMigrationStore.cs`, `RedisEmbeddingMigrationStore.cs`, `GenerateEmbeddingActivity.cs`, `EmbeddingVectorMigrationServiceTests.cs`, `docs/operations/embedding-providers.md`, and the `13.6-RV1` / `13.6-RV3` entries in `deferred-work.md` before editing.
  - [ ] Confirm Stories 13.6, 13.7, 14.4, and 15.2 are not actively `in-progress` or `review`; if another migration/provider story is active, stop and record the exact status.
  - [ ] Identify the current cutover order in live migration: marker start, semantic index drop/recreate, tenant config write, raw migration, natural-language migration, marker completion.
  - [ ] Identify all ingestion paths that can write raw or natural-language semantic vectors during that window, including DAPR workflow retries and `GenerateEmbeddingActivity` config reads.

- [ ] Task 1 - Choose and document one coordination policy (AC: 1, 3, 4)
  - [ ] Select one explicit policy: tenant migration lock, ingestion pause/drain, migration-aware ingestion routing, or accepted operator downtime constraint.
  - [ ] Prefer the smallest policy that prevents mixed-provider state without redesigning the ingestion workflow. If accepting a downtime/operator constraint, make the enforcement and evidence requirements concrete.
  - [ ] Define the cutover point in one sentence. For example: "after target config is visible to ingestion, no new old-provider semantic writes may be accepted for the tenant."
  - [ ] Record what happens to in-flight ingestion work that started before the cutover: allowed to complete, blocked/retried, drained before mutation, or detected and remigrated.
  - [ ] Record how resume behaves if a migration is interrupted while the coordination policy is active.

- [ ] Task 2 - Implement the selected runtime or operational guard (AC: 1, 3)
  - [ ] If implementing a tenant migration lock, store it through a durable tenant-scoped boundary that both migration and ingestion can check. Do not use static in-memory flags as the source of truth.
  - [ ] If implementing ingestion pause/drain, make the pause tenant-scoped and bounded; do not globally stop ingestion for every tenant.
  - [ ] If implementing migration-aware ingestion routing, ensure new embeddings use the target config and target index dimensions after cutover and that stale old-config retries cannot write old-provider vectors.
  - [ ] If accepting an operator downtime constraint, add a preflight or dry-run evidence path that tells the operator what must be quiesced and how to verify no active ingestion remains.
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
  - [ ] Add unit tests proving the selected coordination policy prevents or explicitly handles an ingestion write racing after the migration cutover.
  - [ ] Add a resume/interruption test for the selected coordination policy.
  - [ ] Add a negative test showing the old race either fails closed, retries, is drained, or is documented as operator-blocked with explicit evidence.
  - [ ] Run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingVectorMigrationServiceTests"`.
  - [ ] If ingestion activity code changes, run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~GenerateEmbeddingActivity"`.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` when the local SDK permits it.
  - [ ] Run `git diff --check -- src/Hexalith.Memories.Server/Migration src/Hexalith.Memories.Server/Activities/Ingestion src/Hexalith.Memories.Server/Actors tests/Hexalith.Memories.Server.Tests/Migration tests/Hexalith.Memories.Server.Tests/Activities/Ingestion docs/operations/embedding-providers.md _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md`.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` - UPDATE. Coordination policy enforcement, tenant-level migration ordering, result-surface adjustments, and resume behavior.
- `src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs` - UPDATE only if the selected policy needs a durable store boundary for migration lock, pause state, or active-ingestion evidence.
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs` - UPDATE only with the matching durable Redis/DAPR/actor implementation for the selected policy.
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs` - UPDATE. Coordination, race, resume, expected-failure, and documentation-evidence tests.
- `docs/operations/embedding-providers.md` - UPDATE. Operator coordination policy, downtime/drain expectations, abort/resume instructions, and verification steps.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Structured dispositions for `13.6-RV1` and `13.6-RV3`.
- `_bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Possible files only if the selected policy proves they are necessary:

- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - UPDATE only if ingestion must check a tenant migration lock/pause or refresh target config at write time.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs` - UPDATE only with matching ingestion coordination coverage.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` - UPDATE only with matching activity behavior coverage.
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` - UPDATE only if the policy belongs in tenant configuration state and cannot be represented through the migration store boundary.
- `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs` - UPDATE only if actor contract expansion is required and documented.
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
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`

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

`EmbeddingMigrationResult` is the automation-readable result surface for the migration tool. `ValidateOptions(...)` and `TryBuildTargetConfig(...)` use local nullable string/error returns, and `RunAsync(...)` maps them to `EmbeddingMigrationExitCodes.Plumbing`; tenant-level and per-unit failures map to domain-error results. Story 14.4 carried `13.6-RV3` forward because adopting `ValueOrError<T>` from `Hexalith.Commons` would have required broader references and contract churn.

### Coordination Policy Guidance

Do not treat this as a broad migration redesign. Pick one policy and make it observable:

- A tenant-scoped migration lock is strongest if ingestion can cheaply check it before generating/writing semantic vectors and return a retryable/domain-specific failure.
- An ingestion pause/drain policy is acceptable if it is tenant-scoped, documented, and testable. It must not require globally stopping all tenants.
- A migration-aware routing policy is acceptable if it proves post-cutover writes use target provider/model/dimensions and cannot write old-provider vectors from stale workflow state.
- An accepted operator downtime constraint is acceptable only if docs and tooling tell the operator exactly how to quiesce ingestion, verify no active work remains, run migration, and resume safely.

The story should make the final policy obvious to both a maintainer reading code and an operator following the runbook.

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

### Completion Notes List

- Story context created on 2026-05-12.
- Scope is limited to live migration coordination policy, expected-failure result semantics, targeted tests, operator guidance, and deferred-work dispositions for `13.6-RV1` and `13.6-RV3`.
- Provider registry work, token transport policy, dual-write/search fan-out, broad rollback, integration fixture expansion, CI/release tooling, and submodules are forbidden by default.
- No submodule state was touched.

### File List

- `_bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-12: Created Story 15.3 and promoted it from `backlog` to `ready-for-dev`.

## Story Completion Status

Story context created and ready for implementation. Status set to `ready-for-dev`.
