# Story 13.6: Vector Migration Tool

Status: done

**Effort estimate:** ~1.5-2.0 working days. Breakdown:

- **0.15 day - Task 0:** Verify prerequisite Epic 13 stories are done and inspect current Redis / ingestion surfaces.
- **0.25 day - Task 1:** Add migration command surface and dry-run inventory/report model.
- **0.55 day - Task 2:** Add live Path A migration orchestration: update tenant config, drop/recreate semantic indexes, re-embed raw and NL vectors, and emit progress.
- **0.25 day - Task 3:** Add idempotent resume behavior and interruption-safe markers.
- **0.20 day - Task 4:** Add rollback guardrails for explicitly retained Path B indexes, without building Path B coexistence.
- **0.25 day - Task 5:** Add focused unit/tooling tests and record validation.

**HARD prerequisite:** Stories 13.2, 13.3, 13.4, and 13.5 must be `done` before implementation starts. This story depends on `IOidcTokenProvider`, Ollama `EmbeddingClient` dispatch, additive `TenantEmbeddingConfig` OIDC fields, and `TenantConfigurationActor` write/read surfaces. If any prerequisite is still `ready-for-dev`, `in-progress`, or `review`, stop before editing code.

**SOFT prerequisite:** Story 13.7 is intentionally not required. Do not write the operator deployment guide in this story; Story 13.7 owns `docs/operations/embedding-providers.md` and the final runbook entry.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

Build the operator migration surface for Path A: identify Google-backed tenants, update the tenant's embedding config to the committed Ollama target, drop and recreate both semantic Redis Vector indexes at 2560 dimensions, then re-embed persisted units through the same provider/client path used by ingestion.

Do not schedule ordinary `IngestionWorkflow` replay as the migration mechanism. Existing dedup keys can make the workflow return duplicate without re-embedding, and `SemanticIndexer.ReIndexFromSyntacticAsync(...)` currently throws `NotSupportedException`. This story must either complete that reusable re-embedding helper or add a migration-specific re-embedding service that reconstructs from `{tenantId}:mu:{memoryUnitId}` hashes, calls `EmbeddingClient.GenerateAsync(...)`, and writes the existing raw / natural-language semantic hash shapes.

The live command must be resume-safe: already-migrated units are skipped based on persisted provider/model/dimension metadata, progress is durable enough to survive Ctrl-C, and the final summary reports processed, skipped, failed, elapsed time, old config, and target config.

## Story

As an **operator**,
I want a migration tool that can dry-run, execute, resume, and summarize Redis Vector migration from Google 768-dimension vectors to Ollama 2560-dimension vectors,
so that existing tenants can move to the self-hosted provider without ad-hoc Redis CLI operations or silent partial state.

## Acceptance Criteria

1. **AC1 - Dry-run inventories affected tenants without writes.** `--dry-run` lists every affected tenant whose current `TenantEmbeddingConfig.Provider` or `Model` differs from the target provider/model, the current provider/model/dimensions, the target provider/model/dimensions, raw semantic unit count, NL semantic unit count, syntactic memory-unit count, and whether the tenant has a dimension mismatch today. No actor state, Redis index, Redis hash, DAPR secret, dedup key, or graph state is modified.

2. **AC2 - Live migration requires explicit tenant and confirmation.** `--live --tenant <tenantId>` is required for mutation. Non-interactive execution requires `--yes`; interactive execution prompts before dropping indexes. A live all-tenants mode is out of scope unless a separate product decision is recorded.

3. **AC3 - Tenant config is updated through the actor surface.** The live flow updates the tenant's `TenantEmbeddingConfig` to the committed Ollama target using `TenantConfigurationActor.SetEmbeddingConfigAsync(config, forceReindex: true)` or the already-committed equivalent surface from Story 13.5. It does not write DAPR actor state directly and does not store raw OIDC `client_secret` values.

4. **AC4 - Path A drops and recreates both active semantic indexes.** The live flow drops `{tenantId}:memories:vec` and `{tenantId}:memories:vec:nl` and recreates them using `IndexSchemaDefinitions.CreateSemanticSchema(2560)` and `CreateNaturalLanguageSemanticSchema(2560)`, preserving the existing key prefixes `{tenantId}:vec:` and `{tenantId}:vec:nl:`. Syntactic RediSearch, graph data, failed-unit data, dedup keys, tenant registry data, and case data are not deleted.

5. **AC5 - Re-embedding does not rely on ordinary ingestion replay.** The migration reconstructs raw semantic input from persisted syntactic hashes (`{tenantId}:mu:{memoryUnitId}`), calls the committed provider-aware `EmbeddingClient.GenerateAsync(...)` path, and writes the same Redis hash fields that `IndexSemanticActivity` writes today. It must not call `IngestionWorkflow` as the primary replay path because dedup can skip the work.

6. **AC6 - Natural-language semantic vectors are migrated when source data exists.** For units with `{tenantId}:vec:nl:{memoryUnitId}` carrying a non-empty `naturalLanguageDescription`, the migration re-embeds that text and writes the same fields as `IndexNaturalLanguageSemanticActivity`. If the NL hash is absent or lacks a description, the unit is counted as NL skipped, not failed.

7. **AC7 - Resume skips already-migrated units.** When rerun for the same tenant after interruption, the tool skips raw units whose semantic hash already carries the target provider, model, and dimensions, and skips NL units whose NL semantic hash already carries the target provider, model, and dimensions. It does not re-embed already-migrated units.

8. **AC8 - Progress and summary are operator-visible.** The live flow emits per-batch progress with tenant ID, batch number, processed count, skipped count, failed count, total count, percent, and elapsed time. The final summary includes tenant ID, raw units processed/skipped/failed, NL units processed/skipped/failed, elapsed time, old provider/model/dimensions, target provider/model/dimensions, and whether manual follow-up is required.

9. **AC9 - Failures are bounded and resumable.** A per-unit embedding or write failure records that unit ID, content kind (`payload` or `naturalLanguageDescription`), error category, and truncated message in a migration result artifact or Redis marker. The run continues to the next unit unless the failure is a tenant-level configuration/index error. A non-zero failed count exits with the existing domain-error exit code or equivalent.

10. **AC10 - Rollback is guarded, not invented.** `--rollback --tenant <tenantId>` is available only when explicitly retained Path B previous-version indexes can be detected. The command always fails closed under Path A: when no retained previous-version index exists it returns a clear "rollback unavailable" message; when retained indexes are detected it returns a distinct "no committed Path B restore convention" message. This story does not implement read-side fan-out, dual-write, automatic versioned-index coexistence, or any actual restore action — Task 5.3 was struck during code review on 2026-05-03 because Path A explicitly does not retain previous-version indexes.

11. **AC11 - Secret and token discipline is preserved.** Command output, logs, errors, result artifacts, and tests never include raw OIDC `client_secret`, Google API key values, or Bearer tokens. It is acceptable to show `ApiSecretKeyName` as a secret-name reference.

12. **AC12 - Story 13.7 scope remains untouched.** This story does not add Aspire fixtures, Keycloak/Ollama integration tests, AppHost gateway wiring, or the final operator deployment guide. It may add command help text and test fixtures needed to prove this tool.

## Tasks / Subtasks

- [x] Task 0 - Verify prerequisites and current implementation state (AC: #1-#12)
  - [x] Confirm `13-2-implement-oidc-token-provider`, `13-3-extend-embedding-client-to-support-ollama`, `13-4-extend-tenant-embedding-config-with-additive-oidc-fields`, and `13-5-surface-new-fields-via-tenant-configuration-actor` are `done`; if not, stop.
  - [x] Read `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`; reuse it for index names, prefixes, schemas, and dimension validation.
  - [x] Read `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` and `IndexNaturalLanguageSemanticActivity.cs`; preserve their Redis hash field shapes.
  - [x] Read `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs`; either complete this helper or avoid using it. Do not leave migration blocked by its current `NotSupportedException`.
  - [x] Read `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs`; reuse its cursor-based scan pattern and avoid blocking `KEYS`.
  - [x] Read `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` if extending CLI, or `tools/GenerateBenchmarkVectors` if creating a standalone tool project.

- [x] Task 1 - Choose and add the operator command surface (AC: #1, #2, #8, #11)
  - [x] Prefer an extension to `Hexalith.Memories.Cli` only if the migration can be executed through committed server endpoints/workflows. Otherwise create a dedicated `tools/MigrateEmbeddingVectors/` console tool so direct Redis/DAPR dependencies do not leak into the normal CLI.
  - [x] Define options: `--dry-run`, `--live`, `--tenant`, `--target-provider`, `--target-model`, `--target-dimensions`, `--batch-size`, `--yes`, `--resume`, `--rollback`, and `--format`.
  - [x] Default target values to the committed Story 13.4/13.5 Ollama config: provider `ollama`, model `qwen3-embedding:4b`, dimensions `2560`.
  - [x] Reject mutation when `--dry-run` and `--live` are both present, when neither is present, when `--live` lacks `--tenant`, or when non-interactive mutation lacks `--yes`.
  - [x] Register human and JSON output shapes so automation can parse dry-run and final summaries.

- [x] Task 2 - Implement dry-run inventory without writes (AC: #1, #11)
  - [x] Enumerate tenants via the committed tenant registry/listing surface, not by guessing Redis key prefixes.
  - [x] For each tenant, read current embedding config through `TenantConfigurationActor.GetEmbeddingConfigAsync()` or the committed REST/config surface.
  - [x] Count syntactic units from `{tenantId}:mu:*`, raw semantic units from `{tenantId}:vec:*`, and NL semantic units from `{tenantId}:vec:nl:*` with cursor-based scanning.
  - [x] Read `FT.INFO` for both semantic indexes when present and report current dimensions using `IndexSchemaDefinitions.TryGetVectorDimensions(...)`.
  - [x] Mark a tenant affected when provider/model/dimensions differ from target, when raw/NL index dimensions differ from target, or when hashes carry stale provider/model metadata.
  - [x] Prove by test that dry-run does not call `SetEmbeddingConfigAsync`, `FT.DROPINDEX`, `FT.CREATE`, `HashSet`, `KeyDelete`, or DAPR secret reads.

- [x] Task 3 - Implement live Path A migration (AC: #3-#6, #8, #11)
  - [x] Capture and report the old tenant config before mutation.
  - [x] Update tenant config via the actor/config endpoint with `forceReindex: true`; do not edit actor state directly. Order: drop+recreate first (atomic — second create rolls back the first), then `SetEmbeddingConfigAsync`, then migrate. This minimizes the window where ingestion sees a config-vs-index dimension mismatch.
  - [x] Drop raw and NL semantic indexes with `FT.DROPINDEX` without the `DD` flag for the Path A active-index reset. Per-unit migration writes use a Redis transaction that `KEY DELETE`s the prior hash before `HSET` of the new generation, so old fields cannot leak into the new document and old vectors are evicted as units are migrated.
  - [x] Recreate both indexes through `IndexSchemaDefinitions.CreateSemanticSchema(targetDimensions)` and `CreateNaturalLanguageSemanticSchema(targetDimensions)`. Wrap the pair in a try/catch so that if the NL `FT.CREATE` throws after the raw `FT.CREATE` succeeded, the raw index is dropped to keep the tenant in a recoverable state.
  - [x] For raw payload migration, read `content`, `caseId`, and `cloudeventSubject` from syntactic hashes; validate that `content` and `caseId` are non-empty before embedding. **Spec amendment 2026-05-03:** the original wording also listed `sourceUri`, `sourceType`, `metadataJson`, `embeddingProvider`, and `embeddingModel`; verification against `IndexSemanticActivity.cs` confirmed those fields are not written into the raw semantic hash, so loading them is unnecessary. `cloudeventSubject` is read as a top-level hash field because `IndexSyntacticActivity.cs` already parses `cloudevent.subject` from `metadataJson` and persists it as a top-level field; the migration honors that committed contract rather than re-parsing JSON.
  - [x] Generate raw vectors through `EmbeddingClient.GenerateAsync(content, tenantId, targetConfig, ct)` so Story 13.3 auth, retry, dimension validation, and redaction behavior are reused.
  - [x] Write raw semantic hashes with fields `embedding`, `memoryUnitId`, `caseId`, and optional `cloudeventSubject`, plus target `embeddingProvider`, `embeddingModel`, and `embeddingDimensions` metadata needed for resume detection.
  - [x] For NL migration, read the existing NL hash's `naturalLanguageDescription`; generate a vector from that text and write the existing NL fields plus target provider/model/dimensions. Preserve missing-as-missing for `descriptionOrigin`, `descriptionConfidence`, and `descriptionConfidenceSource` — never substitute fabricated defaults like `"ai"`/`"unknown"`/`""` when the source field is absent.
  - [x] Do not regenerate natural-language descriptions in this story. Story 9.2's NL retry service owns missing descriptions.

- [x] Task 4 - Add interruption-safe resume behavior (AC: #7, #9)
  - [x] Define a durable migration marker shape, for example `{tenantId}:embedding-migration:{targetProvider}:{targetModel}` or a local JSON result file when running as a tool. It must track started/completed timestamps, target config, failed units, and last completed batch.
  - [x] Before embedding a raw unit, skip it when `{tenantId}:vec:{memoryUnitId}` already carries target provider, target model, and target dimensions.
  - [x] Before embedding an NL unit, skip it when `{tenantId}:vec:nl:{memoryUnitId}` already carries target provider, target model, and target dimensions.
  - [x] On Ctrl-C, stop after the current unit or batch, flush the marker/result artifact, and return the existing cancelled exit code when available.
  - [x] On rerun with `--resume`, continue remaining units and keep previous failure records unless `--retry-failed` or equivalent is intentionally added.

- [x] Task 5 - Add rollback guardrails (AC: #10)
  - [x] Detect retained previous-version indexes only through explicit names from the committed Path B convention, not by guessing arbitrary backup keys.
  - [x] If retained indexes do not exist, return a fail-closed error that explains rollback is unavailable for Path A-only migrations.
  - [x] **Struck 2026-05-03 in code review:** "If retained indexes exist, require `--yes` and restore only the active semantic index aliases/names documented by the committed migration implementation." — Path A explicitly does not retain previous-version indexes per the sprint-change-proposal, so no committed Path B restore convention exists for this story to call. The code returns a distinct `DomainError` message when retained indexes are unexpectedly detected, but does not perform any restore action.
  - [x] Do not implement dual-write, search fan-out across old/new indexes, or automatic backup retention in this story.

- [x] Task 6 - Add focused tests and validation (AC: #1-#12)
  - [x] Add dry-run tests for affected/unaffected tenant detection, counts, index-dimension reporting, and no writes.
  - [x] Add option-validation tests for invalid mode combinations and confirmation requirements.
  - [x] Add live-flow tests using fake Redis/embedding/actor boundaries or small adapters so unit tests do not require Docker, DAPR sidecars, Keycloak, or Ollama.
  - [x] Add resume tests proving already-migrated raw and NL hashes are skipped.
  - [x] Add failure tests proving per-unit errors are recorded and the run continues.
  - [x] Add secret-redaction tests that scan output/log/result payloads for sample secret/token values and assert they are absent.
  - [x] Run focused tests for the new tool/CLI/server slice.
  - [x] Run `dotnet build Hexalith.Memories.slnx` if the local SDK allows it.
  - [x] Record exact commands and outcomes in the Dev Agent Record.

## Dev Notes

### Current Implementation State

- `IndexSchemaDefinitions` is the single source of truth for current Redis index names, prefixes, and schemas. Active raw semantic index name is `{tenantId}:memories:vec`; raw semantic hash prefix is `{tenantId}:vec:`. Active NL semantic index name is `{tenantId}:memories:vec:nl`; NL semantic hash prefix is `{tenantId}:vec:nl:`.
- `ProvisionRedisVectorActivity` already creates both raw and NL semantic indexes at `TenantProvisioningInput.VectorDimensions`. Reuse the same schema helpers for migration to prevent provisioning/migration drift.
- `DeleteRedisVectorIndexActivity` drops indexes without `DD` for provisioning compensation. `DeleteRedisVectorActivity` uses `FT.DROPINDEX ... DD` for full tenant deletion. Migration should not reuse tenant deletion semantics blindly.
- `IndexSemanticActivity` currently writes `embedding`, `memoryUnitId`, `caseId`, and optional `cloudeventSubject`. It does not yet persist provider/model/dimensions on the raw semantic hash, so this story must add enough metadata for resume detection or use a durable marker keyed by memory unit.
- `IndexNaturalLanguageSemanticActivity` already writes `embeddingProvider`, `embeddingModel`, and `embeddingDimensions`; preserve that shape and use it for NL resume detection.
- `IndexSyntacticActivity` stores the authoritative content, case, source, metadata JSON, `embeddingProvider`, and `embeddingModel` in `{tenantId}:mu:{memoryUnitId}`. That syntactic hash is the correct source for raw re-embedding; dedup keys are not.
- `SemanticIndexer.ReIndexFromSyntacticAsync(...)` was intentionally deferred and currently throws `NotSupportedException`. If the migration chooses to reuse it, this story must implement the missing `EmbeddingClient` + actor/rate-limit wiring and tests.
- The normal `IngestionWorkflow` first runs idempotency. Replaying it for an already-indexed source can return duplicate and skip embedding/indexing. Do not use it as the primary migration mechanism.
- `GenerateEmbeddingActivity` already gets tenant config from `TenantConfigurationActor`, primes provider credentials, enforces the per-tenant `EmbeddingRateLimiterActor`, calls `EmbeddingClient.GenerateAsync(...)`, and returns `provider:model` plus dimensions. Reuse this behavior directly only if it can be invoked without DAPR workflow replay hazards; otherwise reuse `EmbeddingClient` and explicitly decide whether rate limiting applies.

### File Scope

**Expected edited files if implementing as a dedicated tool:**

- `tools/MigrateEmbeddingVectors/MigrateEmbeddingVectors.csproj`
- `tools/MigrateEmbeddingVectors/Program.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/*` or a new focused tooling test project if the repo already has one
- `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md`

**Expected edited files if implementing through server workflow plus CLI command:**

- `src/Hexalith.Memories.Contracts/V1/*EmbeddingMigration*.cs`
- `src/Hexalith.Memories.Server/Workflows/*EmbeddingMigration*.cs`
- `src/Hexalith.Memories.Server/Activities/*EmbeddingMigration*.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `src/Hexalith.Memories.Cli/Commands/*EmbeddingMigration*.cs`
- `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`
- focused contract/server/client/CLI tests

**Possible edited files:**

- `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs`
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` only for small helper extraction needed by tests/migration
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` only to add resume metadata in the same shape migration writes

**Do not edit in this story:**

- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` except for prerequisite compile fallout.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` except for prerequisite compile fallout.
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` except for prerequisite compile fallout.
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` except for prerequisite compile fallout.
- `src/Hexalith.Memories.AppHost/Program.cs`, AppHost settings, Keycloak fixtures, Ollama test fixture wiring, or `docs/operations/embedding-providers.md` (Story 13.7).
- `Hexalith.EventStore`, `Hexalith.Commons`, or nested submodule contents.

### Implementation Guidance

- Keep the migration state machine explicit: `dry-run -> confirmed live -> config updated -> indexes recreated -> raw batch loop -> NL batch loop -> summary`. Tests should pin transitions and failure behavior.
- Prefer cursor-based Redis scans (`IServer.KeysAsync` with page size) or index queries already used in the codebase. Do not use blocking `KEYS`.
- Build the target config from committed `EmbeddingProviderDefaults.Ollama()` after Story 13.4, then allow explicit command overrides only for provider/model/dimensions and documented metadata. Do not hard-code secrets or endpoints in tests.
- Use `MemoryMarshal.AsBytes(float[].AsSpan()).ToArray()` for vector bytes, matching existing indexing activities.
- Preserve CloudEvent subject tagging by reading `cloudevent.subject` from parsed `metadataJson` and writing `cloudeventSubject` on raw semantic hashes when present.
- Preserve NL-specific fields: `naturalLanguageDescription`, `descriptionOrigin`, `descriptionConfidence`, `descriptionConfidenceSource`, `embeddingProvider`, `embeddingModel`, and `embeddingDimensions`.
- Treat a missing syntactic hash as a per-unit failure, not as a reason to delete graph or case data.
- If the tool writes local result artifacts, put them under a deliberate ignored or documented location, not under `_bmad-output/implementation-artifacts`, unless BMAD traceability is intentionally required.

### Security Requirements

- Never log, serialize, or display raw `client_secret`, Google API key values, or Bearer tokens.
- `ApiSecretKeyName` is safe to show because it is a secret-name reference.
- Do not read DAPR secret values during dry-run.
- During live migration, secret reads should happen only through the committed embedding client/provider path.
- Truncate provider response bodies and exception messages before writing result artifacts.

### Testing Requirements

- Use xUnit + Shouldly for .NET tests and existing CLI test conventions for command-output assertions.
- Unit tests should use fakes/adapters around Redis, actor/config, and embedding generation. Docker/real Redis belongs to optional integration coverage only.
- Include a fixture with one Google 768 raw unit, one already-migrated Ollama 2560 raw unit, one NL description unit, and one malformed unit to prove processed/skipped/failed counts.
- Add JSON output tests so automation can consume dry-run and final summary.
- Add redaction tests with sample values such as `super-secret-client-secret`, `AIzaFake`, and `Bearer eyJ...`; assert they are absent from output and artifacts.
- Focused validation should include the new migration/tool tests and any touched contract/server/CLI tests.

### Previous Story Intelligence

- Story 13.1 delivered the `ollama` provider, `qwen3-embedding:4b`, 2560 dimensions, and provider validation. Its review deferred stricter provider/model/dimension registry work to Story 13.4.
- Story 13.2 owns OIDC token acquisition, cache, forced refresh, typed failures, and redaction. This story consumes that behavior and should not reimplement token logic.
- Story 13.3 owns provider dispatch in `EmbeddingClient`, including Ollama request shape and 401/403 retry. This story should call that client path rather than issuing raw Ollama HTTP requests.
- Story 13.4 owns additive config fields, OIDC validation, URL validation, and `BaseUrl` breaking-change detection.
- Story 13.5 owns actor/API exposure for the new config fields. This migration should update config through that committed surface.
- Story 9.2 added the sibling NL semantic index. A complete migration must handle both raw payload and NL semantic indexes.
- Stories 12.3 through 12.6 reinforced strict file-scope discipline and release-lane proof. Keep this story's implementation focused on migration surfaces and tests.

### Anti-Patterns to Avoid

- Do not use normal ingestion replay as the migration path; dedup can make it a no-op.
- Do not drop syntactic indexes, graph databases, case data, tenant registry data, failed-unit data, or dedup keys.
- Do not treat Path B versioned-index coexistence as built. Rollback can only use retained previous-version indexes when they actually exist.
- Do not hide partial failures behind a successful exit code.
- Do not create a separate provider/token implementation inside the migration tool.
- Do not require real Keycloak/Ollama/DAPR sidecars for unit tests.
- Do not broaden Story 13.7 documentation, AppHost, or integration fixture scope.

## References

- [Source: `_bmad-output/planning-artifacts/epics.md` Epic 13 Story 13.6] - Dry-run, live migration, interrupt/resume, rollback, and runbook expectations.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md` Sections 2.5, 3, 4.4, 5] - Path A default, Path B as opt-in, and operator acceptance of one-shot reindex.
- [Source: `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md`] - Token-provider dependency and redaction behavior.
- [Source: `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`] - Provider-aware embedding client behavior this migration must reuse.
- [Source: `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md`] - Target Ollama config fields, defaults, and validation.
- [Source: `_bmad-output/implementation-artifacts/13-5-surface-new-fields-via-tenant-configuration-actor.md`] - Actor/config surface for reading and writing embedding config.
- [Source: `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`] - Current Redis index names, prefixes, schemas, and dimension helpers.
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`] - Raw semantic hash write shape.
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`] - NL semantic hash write shape and existing provider/model/dimension metadata.
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`] - Authoritative syntactic hash fields used for re-embedding.
- [Source: `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs`] - Existing deferred semantic re-index helper that currently throws.
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs`] - Cursor-based key enumeration pattern.
- [Source: `src/Hexalith.Memories.Cli/Commands/ConsistencyRepairCommand.cs`] - Existing CLI pattern for mutating command confirmation and wait behavior.

## Project Context Reference

The BMad persistent-facts glob found `Hexalith.Commons/_bmad-output/project-context.md` but no Memories-local `project-context.md`. Treat the Commons context as general Hexalith ecosystem guidance only. Repository-specific constraints in this story and the Memories planning artifacts take precedence.

## Party-Mode Review

- **Date/time:** 2026-05-02T13:44:02Z
- **Selected story key:** `13-6-vector-migration-tool`
- **Command/skill invocation used:** `/bmad-party-mode 13-6-vector-migration-tool; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- **Findings summary:** The story direction is valid, but implementation is blocked by unmet hard prerequisites: Stories 13.3 and 13.5 remain `ready-for-dev`, so the required 13.2-13.5 implementation chain is not complete. The review also identified decision-budget risk around migration state identity, resume markers, raw semantic provider/model/dimension metadata, tenant config update verification, per-unit failure bounds, exact live confirmation behavior, and whether `SemanticIndexer.ReIndexFromSyntacticAsync(...)` should be completed or bypassed by a migration-local path.
- **Changes applied:** Recorded this canonical party-mode trace and moved the story status to `blocked` so it is not handed to `bmad-dev-story` before prerequisite stories complete.
- **Findings deferred:** Resolve whether raw semantic hashes gain provider/model/dimension metadata as a platform schema contract or whether the migration relies on durable migration markers; define the migration attempt id / per-tenant / per-index / per-unit checkpoint shape; define retry and failure-retention limits; confirm the exact command/output format; decide whether tenant config is updated before index work or after readiness proof with a documented recovery path; decide whether to complete the shared `SemanticIndexer` helper or keep the implementation migration-local.
- **Final recommendation:** `blocked`

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- Story authored on 2026-05-02 by the recurring pre-dev hardening automation after preflight JSON timestamp `2026-05-02T08:10:53Z`.
- Preflight result was `pass` with `working tree cleanliness` reporting `0 dirty paths`.
- No code implementation was performed in this run; this is a create-story artifact only.
- 2026-05-03: Verified sprint prerequisites 13.2, 13.3, 13.4, and 13.5 are all `done`; story status was stale `blocked`, but the hard prerequisite gate is satisfied.
- 2026-05-03: Implemented a migration-local service path instead of completing `SemanticIndexer.ReIndexFromSyntacticAsync(...)`, preserving the existing repair helper boundary.
- 2026-05-03: Validation commands:
  - `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --filter EmbeddingVectorMigrationServiceTests --no-restore` -> Passed 4/4.
  - `dotnet restore tools\MigrateEmbeddingVectors\MigrateEmbeddingVectors.csproj` -> Passed.
  - `dotnet build Hexalith.Memories.slnx --no-restore` -> Passed 0 warnings / 0 errors.
  - `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --filter EmbeddingVectorMigrationServiceTests --no-build` -> Passed 4/4.
  - `dotnet tools\MigrateEmbeddingVectors\bin\Debug\net10.0\MigrateEmbeddingVectors.dll --help` -> Passed.
  - `dotnet test Hexalith.Memories.slnx --no-build` -> Timed out after 10 minutes in the integration lane; leftover `testhost` was stopped.
  - `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build` -> Passed 1687/1687.

### Completion Notes List

- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-6-vector-migration-tool`.
- Implementation is explicitly gated on Stories 13.2, 13.3, 13.4, and 13.5 reaching `done`.
- Implemented dedicated `tools/MigrateEmbeddingVectors` operator surface with dry-run, live, resume, rollback, target override, batch, confirmation, and JSON/human output options.
- Added server-owned migration orchestration with fakeable Redis/actor/embedding boundaries, tenant inventory, Path A index recreation, raw and natural-language re-embedding, durable Redis marker/failure recording, per-batch progress, fail-closed rollback, and redaction.
- Added raw semantic provider/model/dimension metadata stamping to the normal indexing activity so new raw vectors match the migration resume contract.
- Added focused migration unit tests proving dry-run no-write behavior, option validation, live resume skip behavior, per-unit failure continuation, secret/token redaction, and rollback fail-closed behavior.
- Full solution build passed; full solution test run timed out locally in the integration lane, while full `Hexalith.Memories.Server.Tests` passed.

### File List

- `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `Hexalith.Memories.slnx`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingClientMigrationVectorGenerator.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationExitCodes.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationIndexInfo.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMode.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationOptions.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationProgress.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationResult.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationTenantCounts.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationTenantReport.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationUnitCounters.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationUnitFailure.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`
- `src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs`
- `src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationVectorGenerator.cs`
- `src/Hexalith.Memories.Server/Migration/NaturalLanguageMigrationUnit.cs`
- `src/Hexalith.Memories.Server/Migration/NaturalLanguageSemanticMigrationWrite.cs`
- `src/Hexalith.Memories.Server/Migration/RawSemanticMigrationWrite.cs`
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`
- `src/Hexalith.Memories.Server/Migration/SemanticMigrationState.cs`
- `src/Hexalith.Memories.Server/Migration/SyntacticMigrationUnit.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`
- `tools/MigrateEmbeddingVectors/MigrateEmbeddingVectors.csproj`
- `tools/MigrateEmbeddingVectors/Program.cs`

### Review Findings

Adversarial review on 2026-05-03 across three layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor). Diff snapshot at `_bmad-output/implementation-artifacts/13-6-review-diff.patch`.

#### Decision-needed (resolved 2026-05-03)

- [x] [Review][Decision] Confirmation gate inversion lets unconfirmed mutations through — **Resolved (a):** dropped the `!options.Interactive` predicate from `ValidateOptions`. `--yes` is now always required for mutations; the interactive prompt only promotes to `Yes=true` on explicit `y`/`yes`, and the CLI returns `Plumbing` exit code "Aborted by operator." otherwise. Test added: `LiveWithoutYesShouldReturnPlumbingError`.
- [x] [Review][Decision] Live migration mutates tenant config before any embedding work succeeds — **Resolved (b, modified):** reordered to `StartMarker → DropAndRecreateSemanticIndexesAsync → SetEmbeddingConfigAsync → MigrateRaw → MigrateNaturalLanguage → CompleteMarker`, with the entire mutation block wrapped in a tenant-level try/catch that records a `tenant`-kind failure and returns `DomainError` rather than throwing. The drop+recreate is atomic (see DN4); config update lands immediately after so ingestion sees consistent dimensions, and per-batch failures don't roll back tenant config. Test added: `TenantLevelErrorShouldRecordFailureAndReturnDomainError`.
- [x] [Review][Decision] `FT.DROPINDEX` invoked without `DD` — **Resolved (b):** kept `FT.DROPINDEX` without `DD` to preserve hash data for resume detection, but `WriteRawSemanticAsync` and `WriteNaturalLanguageSemanticAsync` now use a Redis transaction that `KeyDelete`s the prior hash before `HSET`-ing the new generation. Old fields cannot leak into the new document, and stale-dimension hashes are removed atomically as the migration progresses.
- [x] [Review][Decision] Rollback Task 5.3 not implemented — **Resolved (b):** Task 5.3 struck from the spec because Path A explicitly does not retain previous-version indexes per `sprint-change-proposal-2026-04-29.md`. AC10 wording amended to clarify rollback always fails closed with two distinct messages depending on whether retained indexes are unexpectedly detected. Test added: `RollbackWithRetainedPreviousIndexesShouldStillFailClosed`.
- [x] [Review][Decision] Drop-then-create atomicity — **Resolved (a):** `DropAndRecreateSemanticIndexesAsync` wraps the two `FT.CREATE` calls in a try/catch; if the NL create throws after the raw create succeeded, the raw index is dropped to leave the tenant in a recoverable state and the original exception is rethrown into the tenant-level error path.
- [x] [Review][Decision] Silent NL default substitution — **Resolved (b):** `MigrateNaturalLanguageAsync` no longer substitutes `"ai"`/`"unknown"`/`""` for missing fields; `NaturalLanguageSemanticMigrationWrite` now accepts `string?` for `DescriptionOrigin`/`DescriptionConfidence`/`DescriptionConfidenceSource`, and `WriteNaturalLanguageSemanticAsync` only emits the corresponding hash fields when the source value is non-empty. Test added: `LiveMigrationShouldNotInventNaturalLanguageDefaults`.
- [x] [Review][Decision] AC5 syntactic enumeration field set narrower than spec — **Resolved (b):** spec Task 3.5 amended to drop `sourceUri`, `sourceType`, `metadataJson`, `embeddingProvider`, `embeddingModel` from the required-read list. `IndexSemanticActivity` does not persist those fields into the raw semantic hash, so loading them was dead weight. The amended task now requires `content`, `caseId`, and `cloudeventSubject` only.
- [x] [Review][Decision] CloudEvent subject sourcing — **Resolved (b):** verified against `IndexSyntacticActivity.cs:47,92` that `cloudevent.subject` is parsed from `Metadata` and persisted as a top-level `cloudeventSubject` hash field. Migration's direct read honors the committed contract; the spec's "from parsed metadataJson" wording was describing where the value originates upstream, not the runtime field shape. Implementation Guidance amended in Task 3.

#### Patch

- [x] [Review][Patch] Stream syntactic units instead of buffering the entire enumeration into `List<SyntacticMigrationUnit>` [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`]
- [x] [Review][Patch] `HashSetAsync` overwrites only enumerated entries, retaining stale fields from the prior generation (e.g., legacy `cloudeventSubject` on a unit whose subject is now empty); `KeyDelete` then `HashSet`, or write a complete field set including explicit empty values [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs` `WriteRawSemanticAsync`, `WriteNaturalLanguageSemanticAsync`]
- [x] [Review][Patch] Vector byte conversion via `MemoryMarshal.AsBytes` is host-endian; add an explicit little-endian guard or convert via `BinaryPrimitives.WriteSingleLittleEndian` [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`]
- [x] [Review][Patch] `CancellationToken` not propagated into per-key `HashGetAsync` calls inside `GetCountsAsync` and `ReadSemanticStateAsync`, and `DropAndRecreateSemanticIndexesAsync` discards `ct` with `_ = ct;`; thread the token through [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`]
- [x] [Review][Patch] `int.Parse` on `--target-dimensions` and `--batch-size` throws on non-numeric input, escaping the parser; use `int.TryParse` and return a `Plumbing` error [`tools/MigrateEmbeddingVectors/Program.cs`]
- [x] [Review][Patch] `ReadValue` rejects any value starting with `--` and throws `ArgumentException`, blocking legitimate values and crashing the tool with a stack trace; treat the next arg as a value when the current option requires one [`tools/MigrateEmbeddingVectors/Program.cs`]
- [x] [Review][Patch] `SimpleHttpClientFactory.CreateClient` returns a fresh `HttpClient` every call, never disposed; reuse a single instance per factory or use `IHttpClientFactory` [`tools/MigrateEmbeddingVectors/Program.cs`]
- [x] [Review][Patch] `EmbeddingMigrationResult` is JSON-serialized via reflection-based `JsonSerializer.Serialize` instead of `MemoriesJsonContext`; under trimming/AOT this fails at runtime — wire the source-generated context [`tools/MigrateEmbeddingVectors/Program.cs`]
- [x] [Review][Patch] NL skip when description is missing is indistinguishable from "already migrated" — separate counters or status (e.g., `NlMissing` vs `NlSkipped`) so operators can investigate [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`, `EmbeddingMigrationUnitCounters.cs`]
- [x] [Review][Patch] `ErrorCategory` is set to `ex.GetType().Name`, leaking provider-specific exception types (e.g., `OllamaApiException`) past the redactor; normalize to a fixed taxonomy or pass through the redactor [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`]
- [x] [Review][Patch] Final progress reporter fires twice when `units.Count == 0` (post-loop branch with `total=0 percent=100`) and emits asymmetric batch numbers across runs; rewrite the predicate to suppress empty-batch reports [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`]
- [x] [Review][Patch] `GetMarkerKey` only sanitizes `:` in the model name; sanitize a broader set (`/`, whitespace, control chars) or hash the model identifier [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`]
- [x] [Review][Patch] `RedisValue.ToString()` on a missing hash field returns `""`, so resume detection cannot distinguish "field absent" from "field empty"; use `RedisValue.IsNull` checks before stamping `SemanticMigrationState` [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`]
- [x] [Review][Patch] `IsTargetState` compares `Provider` with `OrdinalIgnoreCase` but `Model` with case-sensitive `string.Equals`; pick one and apply consistently [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`]
- [x] [Review][Patch] `--batch-size` is unbounded — a huge value defeats per-batch progress and marker flushing; cap at a sensible upper bound (e.g., 10_000) [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` `ValidateOptions`]
- [x] [Review][Patch] `BuildTargetConfig`'s `EmbeddingProviderDefaults.Validate` throws on invalid target dimensions / unknown auth-mode combinations; `RunAsync` only catches `OperationCanceledException`, so the tool exits with a stack trace — wrap in a controlled `Plumbing` error path [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`]
- [x] [Review][Patch] `--resume` without a prior marker silently treats the run as fresh; emit a warning or fail with a clear message [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs` `StartMigrationMarkerAsync`]
- [x] [Review][Patch] Failure-list Redis key is unbounded across repeated `--resume` runs; add TTL, rotation, or a documented cleanup step [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs` `RecordFailureAsync`]
- [x] [Review][Patch] Redactor truncates after redaction but secret patterns spanning the truncation boundary may survive when the regex did not match; redact, then truncate, then redact-again on the truncated tail [`src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs`]
- [x] [Review][Patch] Redactor `SecretFieldRegex` does not catch JSON-encoded secrets in escaped exception strings (`\"client_secret\":\"...\"`); add a JSON-escaped variant or normalize before matching [`src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs`]
- [x] [Review][Patch] Tenant-level errors (`SetEmbeddingConfigAsync`, `DropAndRecreateSemanticIndexesAsync` exceptions) escape `RunAsync` instead of producing a controlled `EmbeddingMigrationResult` with `manualFollowUp = true`; wrap the orchestration in a tenant-scoped try/catch [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` `LiveAsync`]
- [x] [Review][Patch] Public method boundaries lack `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace` validation per Hexalith convention [`src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`, `EmbeddingClientMigrationVectorGenerator.cs`]
- [x] [Review][Patch] `EmbeddingMigrationUnitFailure` (and other migration records serialized to Redis / JSON output) lack `[DataContract]` / `[DataMember]` / `JsonPropertyOrder` per Hexalith convention [`src/Hexalith.Memories.Server/Migration/EmbeddingMigrationUnitFailure.cs` and siblings]
- [x] [Review][Patch] Add tests: cancellation mid-batch, invalid `--target-provider`/`--target-dimensions`, stale-metadata-only resume detection (target provider but stale dimensions, target model with stale provider), batch-size boundary (exact multiple, last partial), `--resume` without prior marker, rollback with retained indexes present [`tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`]

#### Deferred

- [x] [Review][Defer] Concurrent ingestion racing the migration (a separate writer with cached old config writes a fresh hash with old provider during enumeration) [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`] — deferred, broader ingestion-vs-migration concurrency is out of scope for this tool
- [x] [Review][Defer] Pre-existing missing copyright header [`src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`] — deferred, pre-existing
- [x] [Review][Defer] Migration tool surfaces use ad-hoc `string` error returns + exit codes rather than `ValueOrError<T>` [`src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`] — deferred, refactor scope
- [x] [Review][Defer] Redactor does not match AWS access keys, raw JWT signatures without `Bearer ` prefix, or HTTP Basic auth headers [`src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs`] — deferred, low likelihood for embedding-provider error surfaces
- [x] [Review][Defer] Redactor skips `client_secret named foo` style strings without `:` or `=` separator [`src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs`] — deferred, pattern correct in typical case (separator-bound value), name-only references are not credential exposure

### Change Log

| Date       | Change                                                                                                                                                                                                                         | Author |
|------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-02 | Story 13.6 context created: Path A Redis Vector migration tool with dry-run, live confirmation, actor-based config update, raw/NL re-embedding, resume behavior, guarded rollback, secret discipline, and Story 13.7 boundaries. | Codex |
| 2026-05-03 | Implemented Path A vector migration tool, migration service, Redis/DAPR adapter, raw metadata stamping, focused tests, and validation evidence; moved story to review.                                                          | Codex |
| 2026-05-03 | Adversarial code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) appended Review Findings: 8 decision-needed, 23 patches, 5 deferred.                                                                            | Claude |
| 2026-05-03 | Resolved all 8 decision-needed items, applied all 23 patches, amended Task 3 / Task 5 / AC10 wording, expanded migration test coverage to 13 tests (was 4); full Server.Tests suite green at 1696/1696. Story moved to `done`. | Claude |
