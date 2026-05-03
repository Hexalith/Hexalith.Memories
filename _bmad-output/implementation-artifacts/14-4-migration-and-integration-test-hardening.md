# Story 14.4: Migration and Integration Test Hardening

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator and maintainer,
I want migration and Aspire integration tests hardened,
so that provider migration evidence remains stable under CI pressure and malformed fake-server input cannot weaken coverage silently.

## Acceptance Criteria

1. Given migration service expected-failure paths are refactored, when options or tenant migration results are invalid, then business failures use `ValueOrError<T>` where appropriate or retain exceptions with a documented, focused reason.

2. Given migration redaction is expanded, when AWS access keys, raw JWTs, HTTP Basic auth, and approved secret-value shapes appear in captured payloads, then the redactor masks them without masking benign secret-name references unless the story explicitly chooses stricter behavior.

3. Given the Ollama integration test waits for Redis state, when a bounded targeted alternative exists, then it no longer uses Redis `KEYS` polling in the 3-minute loop.

4. Given Aspire fixture DAPR config files are created in temp directories, when the fixture disposes or initialization fails, then generated config files and parent temp directories are cleaned up.

5. Given the Ollama OIDC fake server rejects malformed token requests, when tests run, then dedicated theory cases cover missing content type, missing grant type, missing client ID, missing client secret, duplicate form values, malformed body branches, and wrong optional scope.

6. Given provider integration assertions depend on expected embedding call counts, when the raw plus natural-language embedding path is asserted, then magic numeric thresholds are replaced with named constants or clearer assertions.

7. Given targeted Epic 13 deferred IDs are addressed, when the story moves to review, then `deferred-work.md` records each targeted ID as resolved, accepted, or carried forward with validation evidence.

## Tasks / Subtasks

- [ ] Task 1 - Harden migration result surfaces without broad API churn (AC: 1)
  - [ ] Inspect `EmbeddingVectorMigrationService.RunAsync(...)`, `ValidateOptions(...)`, `TryBuildTargetConfig(...)`, tenant-level error handling, and `tools/MigrateEmbeddingVectors/Program.cs`.
  - [ ] Convert expected validation/business failures to the repository's `ValueOrError<T>` pattern only where it makes the surface clearer and does not cascade through unrelated contracts.
  - [ ] If a migration path intentionally retains exceptions or string messages, document the focused reason in this story's Dev Agent Record and keep output controlled by `EmbeddingMigrationResult`.
  - [ ] Preserve existing exit-code behavior: success, plumbing, domain error, and cancelled results must remain automation-readable.
  - [ ] Add or update focused tests for invalid options, invalid target config, tenant-level failures, `--resume` without marker, and controlled CLI error output.

- [ ] Task 2 - Expand migration redaction with realistic credential shapes (AC: 2)
  - [ ] Update `EmbeddingMigrationRedactor` to mask AWS access keys, raw JWT-like tokens, HTTP Basic authorization values, and approved secret-value forms used by migration output or fake-server payloads.
  - [ ] Keep name-only secret references, such as `client_secret named memories-embedding-client-secret` or `ApiSecretKeyName`, visible unless the implementation deliberately records a stricter operator-visible policy.
  - [ ] Preserve current Google API key, bearer-token, JSON field, and JSON-escaped-field redactions.
  - [ ] Redact before truncation and re-redact after truncation so boundary-spanning values cannot leak.
  - [ ] Add tests that assert exact sample values are absent and benign secret-name references remain present.

- [ ] Task 3 - Replace Redis `KEYS` polling in the Ollama end-to-end wait (AC: 3)
  - [ ] Refactor `WaitForSemanticHashAsync(...)` in `OllamaEmbeddingEndToEndTests` so the 3-minute loop does not call `redisServer.Keys(...)`.
  - [ ] Prefer a targeted known-key lookup if the memory unit ID can be obtained from workflow/status or syntactic state; otherwise use bounded cursor/SCAN-style iteration with a small count and cancellation-aware delay.
  - [ ] Preserve stale-data protection: assertions must still require the unique tenant ID, case ID, canary path, 2560 dimensions, and newly returned memory-unit ID.
  - [ ] Keep timeout diagnostics redacted and distinguish workflow Failed, workflow Completed-without-semantic-hash, and wait-expired cases.
  - [ ] Do not introduce production Redis `KEYS` use or broaden the integration test matrix.

- [ ] Task 4 - Clean DAPR temp config directories on success and failure (AC: 4)
  - [ ] Update `AspireIngestionPipelineFixture.DeleteTempDaprConfig()` to delete the generated parent temp directory under `%TEMP%/hexalith-memories-dapr/{daprAppId}` after removing `config.yaml` and AppHost-generated component files.
  - [ ] Ensure cleanup is scoped to the fixture-owned `_daprAppId` directory only. Never delete `%TEMP%/hexalith-memories-dapr` recursively as a whole.
  - [ ] Preserve `RestoreLocalDaprSecret()` behavior and do not remove or rewrite unrelated local `secrets.json` content.
  - [ ] Add focused coverage for cleanup after normal dispose and initialization failure where feasible without starting the full Aspire topology.

- [ ] Task 5 - Add malformed-token theory coverage to the fake server (AC: 5)
  - [ ] Extend `OllamaOidcFakeServerTests` with `[Theory]` cases for missing content type, missing grant type, missing client ID, missing client secret, duplicate values, malformed body, and wrong scope.
  - [ ] Assert the fake returns `400 BadRequest`, does not increment `TokenRequestCount`, and does not record sanitized evidence for rejected requests.
  - [ ] Keep accepted-request evidence sanitized; tests should assert against actual secret/token literals, not only coincidental substrings.
  - [ ] Avoid adding JWT validation to Memories Server; the fake only proves the outbound client-credentials contract.

- [ ] Task 6 - Replace magic embedding-call thresholds with named expectations (AC: 6)
  - [ ] Replace `ShouldBeGreaterThanOrEqualTo(2)` in `OllamaEmbeddingEndToEndTests` with a named constant or a clearer assertion explaining the raw plus natural-language embedding expectation.
  - [ ] If retries make an exact count unstable, assert a named minimum such as `MinimumRawAndNaturalLanguageEmbeddings` and explain what each call represents.
  - [ ] Keep `TokenRequestCount` assertions tolerant enough for token cache timing, but name the expected lower bound.

- [ ] Task 7 - Update deferred-work bookkeeping and validation evidence (AC: 1-7)
  - [ ] Resolve, accept, or carry forward targeted deferred IDs: `13.6-RV1`, `13.6-RV3`, `13.6-RV4`, `13.6-RV5`, `13.7-RV1`, `13.7-RV2`, `13.7-RV3`, `13.7-RV4`, `13.7-RV6`, and `13.7-RV7`.
  - [ ] Do not close `13.7-RV5`; sprint-status long-line cleanup belongs to Story 14.5 unless this story receives an explicit scope update.
  - [ ] Record exact commands and outcomes in this story's Dev Agent Record.
  - [ ] Run `git diff --check -- src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs _bmad-output/implementation-artifacts/deferred-work.md`.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` - UPDATE. Expected-failure/result-surface hardening only.
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` - UPDATE. Credential-shape expansion and truncation safety.
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationResult.cs` - UPDATE only if result typing is needed for controlled failure output.
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationUnitFailure.cs` - UPDATE only if redaction/result metadata requires a small contract clarification.
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs` - UPDATE. Migration result and redaction coverage.
- `tools/MigrateEmbeddingVectors/Program.cs` - UPDATE only if CLI-controlled error handling must align with service result changes.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` - UPDATE. Fixture-owned temp config cleanup only.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs` - UPDATE. Malformed token request handling only if needed for testability.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs` - UPDATE. Dedicated token rejection theory coverage.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs` - UPDATE. Redis wait strategy and named provider-call assertions.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Targeted deferred ID status/evidence.
- `_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md` - UPDATE. Implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md`
- `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md`
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md`
- `src/Hexalith.Memories.Server/Migration/IEmbeddingMigrationStore.cs`
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/ScriptedHttpServer.cs`
- `docs/operations/embedding-providers.md`

Forbidden by default:

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`
- `src/Hexalith.Memories.Server/Ingestion/**`
- `src/Hexalith.Memories.Server/Actors/**`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `docs/**` except deferred-work bookkeeping or a narrow validation note explicitly required by review
- `.github/**`
- `deploy/**`
- `Directory.Packages.props`
- `Directory.Build.props`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`

## Dev Notes

### Current Implementation State

`EmbeddingVectorMigrationService` already returns `EmbeddingMigrationResult` for invalid mode/options, invalid target config, cancellation, rollback fail-closed behavior, and tenant-level migration errors. `ValidateOptions(...)` and `TryBuildTargetConfig(...)` currently use nullable string/error tuples rather than `ValueOrError<T>`. That is acceptable only if the implementation records why this local command surface should stay string-based. If refactoring to `ValueOrError<T>`, keep it tightly scoped and do not require unrelated migration records or CLI call sites to change shape.

`EmbeddingMigrationRedactor` currently masks bearer tokens, Google API keys, secret-like `key=value` or JSON fields, and JSON-escaped secret fields. It does not yet match AWS access keys, JWT-like compact tokens without a `Bearer` prefix, or HTTP Basic authorization values. Existing behavior intentionally preserves secret-name references unless there is a separator-bound value to redact.

`OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync(...)` currently uses `redisServer.Keys(pattern: $"{tenantId}:vec:*")` inside the 3-minute wait loop, then filters out `:vec:nl:`. This is acceptable as a bounded test helper only in the old state, but Story 14.4 should remove that broad scan because Redis documents `SCAN` as the cursor-based alternative and warns that `KEYS` can block large databases.

`AspireIngestionPipelineFixture` creates a fixture-owned temp directory at `%TEMP%/hexalith-memories-dapr/{daprAppId}` and writes `config.yaml` there. Current cleanup deletes only `config.yaml` and leaves the parent directory, plus any generated component files, behind. Cleanup must only remove the `_daprAppId` directory that this fixture created.

`OllamaOidcFakeServer` already rejects several malformed token requests at runtime and rejects duplicate form values through `TryReadSingle(...)`, but tests only prove the happy path and wrong embed path. Dedicated branch tests should prevent future weakening of the fake without changing production gateway responsibility.

### Deferred IDs Targeted

This story is the normal lifecycle home for:

- `13.6-RV1`: concurrent ingestion racing migration. Reassess whether Story 13.7 integration evidence can close, accept, or carry it forward with a clearer trigger.
- `13.6-RV3`: migration service ad-hoc string errors instead of `ValueOrError<T>`.
- `13.6-RV4`: migration redactor does not match AWS access keys, raw JWT signatures, or HTTP Basic auth.
- `13.6-RV5`: migration redactor skips `client_secret named foo` style strings. Preserve name-only references unless policy changes.
- `13.7-RV1`: Redis `KEYS` in the Ollama end-to-end polling loop.
- `13.7-RV2`: URL-escape `tenantId` and `canary` in search query interpolation, even though current GUID-derived values are safe.
- `13.7-RV3`: clean up parent temp DAPR config directory.
- `13.7-RV4`: duplicate `ResolveRepositoryRoot` helpers. Carry forward unless this story naturally touches a reusable helper with no new shared-project churn.
- `13.7-RV6`: missing malformed-token-form rejection branch tests.
- `13.7-RV7`: replace `EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2)` magic threshold.

Out of scope unless explicitly approved:

- `13.7-RV5` sprint-status long-line cleanup; Story 14.5 owns sprint-status hygiene.
- Production ingestion-vs-migration locking or dual-write/fan-out redesign.
- Real Keycloak, real Ollama, real gateway certification, Kubernetes/GPU/cert-manager deployment automation.
- Broad repo-wide secret scanning beyond committed docs/test evidence touched by this story.

### Implementation Guardrails

- Keep this as hardening for existing migration and integration-test surfaces. Do not redesign embedding provider dispatch, tenant configuration contracts, actor state, AppHost topology, or the migration command model.
- Do not add real infrastructure dependencies to routine unit/Tier-2 validation. Fake-server branch tests must run without Docker, DAPR sidecars, Keycloak, Ollama, or Aspire.
- Preserve the default `EmbeddingProviderTestMode.GoogleFake` fixture path. Ollama fake mode must remain opt-in.
- Do not log or assert raw `client_secret`, Google API keys, bearer tokens, AWS keys, Basic credentials, or full upstream payloads.
- When building query strings in tests, use structured URI/query helpers or `Uri.EscapeDataString(...)`; do not concatenate unescaped arbitrary values into URLs.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.

### Technical Constraints and References

- Redis `SCAN` is the cursor-based keyspace iteration command; Redis documentation describes it as the production-safe alternative to blocking broad key discovery. Source: https://redis.io/docs/latest/commands/scan/
- Redis keyspace documentation warns that `KEYS` should be used with extreme care and that `SCAN` can incrementally iterate without the same blocking downside. Source: https://redis.io/docs/latest/develop/using-commands/keyspace/
- `Directory.Delete(path, recursive: true)` can remove a directory tree, but cleanup must pass a verified fixture-owned path, not a computed parent shared by other tests. Source: https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.delete
- Hexalith project context prefers `ValueOrError<T>` for expected business failures and exceptions for exceptional/programmer errors. Apply this selectively so the migration tool remains stable and automation-readable.

### Testing Requirements

Minimum validation before review:

```powershell
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingVectorMigrationServiceTests"
dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --filter "FullyQualifiedName~OllamaOidcFakeServerTests"
dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --filter "FullyQualifiedName~OllamaEmbeddingEndToEndTests"
git diff --check -- src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs _bmad-output/implementation-artifacts/deferred-work.md
```

Additional probes to record when relevant:

- Token fake rejection theory covers every branch and leaves counts/evidence unchanged.
- Temp DAPR config cleanup removes the fixture-owned leaf directory after both dispose and initialization failure.
- Ollama end-to-end wait no longer uses Redis `KEYS`; if exact-key lookup is impossible, document the bounded cursor/SCAN approach.
- Redaction tests include AWS key, raw JWT-like token, Basic authorization, `client_secret=value`, JSON-escaped secret value, and benign secret-name reference.
- Existing default Google/fake ingestion regression still passes if fixture initialization or provider mode code is touched.

## Project Structure Notes

- This is a migration and integration-test hardening story. Expected implementation stays under `src/Hexalith.Memories.Server/Migration`, focused migration tests, Ollama integration fixture/fake tests, and BMAD deferred-work bookkeeping.
- Use existing C# conventions: copyright header on new files, XML documentation on public/internal members, nullable-safe validation, xUnit + Shouldly tests, and no package versions in project files.
- The `Hexalith.Commons` project context discovered by the persistent-facts glob is background Hexalith guidance only. Repository-specific Memories story scope and current code are authoritative.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 14 and Story 14.4 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md` - approved Epic 14 grouping and targeted deferred IDs.
- `_bmad-output/implementation-artifacts/deferred-work.md` - source of targeted `13.6` and `13.7` deferred IDs.
- `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md` - migration tool implementation record and deferred review findings.
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md` - Aspire fixture/fake implementation record and deferred review findings.
- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` - current migration orchestration and expected-failure result paths.
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` - current migration redaction helper.
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs` - current migration service test harness.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` - current DAPR temp config and local secret handling.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs` - current fake token/embed server.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs` - current fake-server Tier-2 tests.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs` - current Ollama Aspire end-to-end wait and assertions.
- Redis `SCAN` docs: https://redis.io/docs/latest/commands/scan/
- Redis keyspace guidance: https://redis.io/docs/latest/develop/using-commands/keyspace/
- Microsoft `Directory.Delete` docs: https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.delete

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight JSON timestamp `2026-05-03T11:31:50Z` passed all checks with `working tree cleanliness` reporting `0 dirty paths`.
- Story selection chose `14-4-migration-and-integration-test-hardening` because `ready_count` was `3`, below the target of `5`, and this was the first backlog story in sprint-status order.
- `/bmad-create-story 14-4-migration-and-integration-test-hardening` context gathering loaded Epic 14 planning, the approved 2026-05-03 sprint-change proposal, Stories 13.6, 13.7, and 14.3 context, current migration/integration source and tests, targeted deferred-work entries, recent git history, and current Redis/Microsoft documentation.

### Completion Notes List

- Story context created on 2026-05-03.
- Scope is limited to migration expected-failure/result-surface hardening, migration redaction expansion, Ollama integration wait strategy, DAPR temp cleanup, fake-token rejection branch tests, named embedding-call assertions, and targeted deferred-work closure.
- Sprint-status long-line cleanup and broad deferred-register governance remain for Story 14.5.
- No submodule state was touched.

### File List

- `_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-03: Created Story 14.4 and promoted it from `backlog` to `ready-for-dev`.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Status set to `ready-for-dev`.
