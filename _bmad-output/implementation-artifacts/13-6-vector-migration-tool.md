# Story 13.6: Vector Migration Tool

Status: blocked

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

10. **AC10 - Rollback is guarded, not invented.** `--rollback --tenant <tenantId>` is available only when explicitly retained Path B previous-version indexes can be detected. If no retained previous-version index exists, the command fails closed with a clear message. This story does not implement read-side fan-out, dual-write, or automatic versioned-index coexistence.

11. **AC11 - Secret and token discipline is preserved.** Command output, logs, errors, result artifacts, and tests never include raw OIDC `client_secret`, Google API key values, or Bearer tokens. It is acceptable to show `ApiSecretKeyName` as a secret-name reference.

12. **AC12 - Story 13.7 scope remains untouched.** This story does not add Aspire fixtures, Keycloak/Ollama integration tests, AppHost gateway wiring, or the final operator deployment guide. It may add command help text and test fixtures needed to prove this tool.

## Tasks / Subtasks

- [ ] Task 0 - Verify prerequisites and current implementation state (AC: #1-#12)
  - [ ] Confirm `13-2-implement-oidc-token-provider`, `13-3-extend-embedding-client-to-support-ollama`, `13-4-extend-tenant-embedding-config-with-additive-oidc-fields`, and `13-5-surface-new-fields-via-tenant-configuration-actor` are `done`; if not, stop.
  - [ ] Read `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`; reuse it for index names, prefixes, schemas, and dimension validation.
  - [ ] Read `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` and `IndexNaturalLanguageSemanticActivity.cs`; preserve their Redis hash field shapes.
  - [ ] Read `src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs`; either complete this helper or avoid using it. Do not leave migration blocked by its current `NotSupportedException`.
  - [ ] Read `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs`; reuse its cursor-based scan pattern and avoid blocking `KEYS`.
  - [ ] Read `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` if extending CLI, or `tools/GenerateBenchmarkVectors` if creating a standalone tool project.

- [ ] Task 1 - Choose and add the operator command surface (AC: #1, #2, #8, #11)
  - [ ] Prefer an extension to `Hexalith.Memories.Cli` only if the migration can be executed through committed server endpoints/workflows. Otherwise create a dedicated `tools/MigrateEmbeddingVectors/` console tool so direct Redis/DAPR dependencies do not leak into the normal CLI.
  - [ ] Define options: `--dry-run`, `--live`, `--tenant`, `--target-provider`, `--target-model`, `--target-dimensions`, `--batch-size`, `--yes`, `--resume`, `--rollback`, and `--format`.
  - [ ] Default target values to the committed Story 13.4/13.5 Ollama config: provider `ollama`, model `qwen3-embedding:4b`, dimensions `2560`.
  - [ ] Reject mutation when `--dry-run` and `--live` are both present, when neither is present, when `--live` lacks `--tenant`, or when non-interactive mutation lacks `--yes`.
  - [ ] Register human and JSON output shapes so automation can parse dry-run and final summaries.

- [ ] Task 2 - Implement dry-run inventory without writes (AC: #1, #11)
  - [ ] Enumerate tenants via the committed tenant registry/listing surface, not by guessing Redis key prefixes.
  - [ ] For each tenant, read current embedding config through `TenantConfigurationActor.GetEmbeddingConfigAsync()` or the committed REST/config surface.
  - [ ] Count syntactic units from `{tenantId}:mu:*`, raw semantic units from `{tenantId}:vec:*`, and NL semantic units from `{tenantId}:vec:nl:*` with cursor-based scanning.
  - [ ] Read `FT.INFO` for both semantic indexes when present and report current dimensions using `IndexSchemaDefinitions.TryGetVectorDimensions(...)`.
  - [ ] Mark a tenant affected when provider/model/dimensions differ from target, when raw/NL index dimensions differ from target, or when hashes carry stale provider/model metadata.
  - [ ] Prove by test that dry-run does not call `SetEmbeddingConfigAsync`, `FT.DROPINDEX`, `FT.CREATE`, `HashSet`, `KeyDelete`, or DAPR secret reads.

- [ ] Task 3 - Implement live Path A migration (AC: #3-#6, #8, #11)
  - [ ] Capture and report the old tenant config before mutation.
  - [ ] Update tenant config via the actor/config endpoint with `forceReindex: true`; do not edit actor state directly.
  - [ ] Drop raw and NL semantic indexes with `FT.DROPINDEX` without the `DD` flag for the Path A active-index reset. Then explicitly delete old raw/NL semantic hashes only after the replacement plan is ready, or rewrite hashes as units are successfully migrated so interruption remains resumable.
  - [ ] Recreate both indexes through `IndexSchemaDefinitions.CreateSemanticSchema(targetDimensions)` and `CreateNaturalLanguageSemanticSchema(targetDimensions)`.
  - [ ] For raw payload migration, read `content`, `caseId`, `sourceUri`, `sourceType`, `metadataJson`, `embeddingProvider`, and `embeddingModel` from syntactic hashes; validate required fields before embedding.
  - [ ] Generate raw vectors through `EmbeddingClient.GenerateAsync(content, tenantId, targetConfig, ct)` so Story 13.3 auth, retry, dimension validation, and redaction behavior are reused.
  - [ ] Write raw semantic hashes with fields `embedding`, `memoryUnitId`, `caseId`, and optional `cloudeventSubject`, plus target `embeddingProvider`, `embeddingModel`, and `embeddingDimensions` metadata needed for resume detection.
  - [ ] For NL migration, read the existing NL hash's `naturalLanguageDescription`; generate a vector from that text and write the existing NL fields plus target provider/model/dimensions.
  - [ ] Do not regenerate natural-language descriptions in this story. Story 9.2's NL retry service owns missing descriptions.

- [ ] Task 4 - Add interruption-safe resume behavior (AC: #7, #9)
  - [ ] Define a durable migration marker shape, for example `{tenantId}:embedding-migration:{targetProvider}:{targetModel}` or a local JSON result file when running as a tool. It must track started/completed timestamps, target config, failed units, and last completed batch.
  - [ ] Before embedding a raw unit, skip it when `{tenantId}:vec:{memoryUnitId}` already carries target provider, target model, and target dimensions.
  - [ ] Before embedding an NL unit, skip it when `{tenantId}:vec:nl:{memoryUnitId}` already carries target provider, target model, and target dimensions.
  - [ ] On Ctrl-C, stop after the current unit or batch, flush the marker/result artifact, and return the existing cancelled exit code when available.
  - [ ] On rerun with `--resume`, continue remaining units and keep previous failure records unless `--retry-failed` or equivalent is intentionally added.

- [ ] Task 5 - Add rollback guardrails (AC: #10)
  - [ ] Detect retained previous-version indexes only through explicit names from the committed Path B convention, not by guessing arbitrary backup keys.
  - [ ] If retained indexes do not exist, return a fail-closed error that explains rollback is unavailable for Path A-only migrations.
  - [ ] If retained indexes exist, require `--yes` and restore only the active semantic index aliases/names documented by the committed migration implementation.
  - [ ] Do not implement dual-write, search fan-out across old/new indexes, or automatic backup retention in this story.

- [ ] Task 6 - Add focused tests and validation (AC: #1-#12)
  - [ ] Add dry-run tests for affected/unaffected tenant detection, counts, index-dimension reporting, and no writes.
  - [ ] Add option-validation tests for invalid mode combinations and confirmation requirements.
  - [ ] Add live-flow tests using fake Redis/embedding/actor boundaries or small adapters so unit tests do not require Docker, DAPR sidecars, Keycloak, or Ollama.
  - [ ] Add resume tests proving already-migrated raw and NL hashes are skipped.
  - [ ] Add failure tests proving per-unit errors are recorded and the run continues.
  - [ ] Add secret-redaction tests that scan output/log/result payloads for sample secret/token values and assert they are absent.
  - [ ] Run focused tests for the new tool/CLI/server slice.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` if the local SDK allows it.
  - [ ] Record exact commands and outcomes in the Dev Agent Record.

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

### Completion Notes List

- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-6-vector-migration-tool`.
- Implementation is explicitly gated on Stories 13.2, 13.3, 13.4, and 13.5 reaching `done`.

### File List

- `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

| Date       | Change                                                                                                                                                                                                                         | Author |
|------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-02 | Story 13.6 context created: Path A Redis Vector migration tool with dry-run, live confirmation, actor-based config update, raw/NL re-embedding, resume behavior, guarded rollback, secret discipline, and Story 13.7 boundaries. | Codex |
