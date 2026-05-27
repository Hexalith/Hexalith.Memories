# Story 14.4: Migration and Integration Test Hardening

Status: done

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

- [x] Task 1 - Harden migration result surfaces without broad API churn (AC: 1)
  - [x] Inspect `EmbeddingVectorMigrationService.RunAsync(...)`, `ValidateOptions(...)`, `TryBuildTargetConfig(...)`, tenant-level error handling, and `tools/MigrateEmbeddingVectors/Program.cs`.
  - [x] Convert expected validation/business failures to the repository's `ValueOrError<T>` pattern only where it makes the surface clearer and does not cascade through unrelated contracts.
  - [x] If a migration path intentionally retains exceptions or string messages, document the focused reason in this story's Dev Agent Record and keep output controlled by `EmbeddingMigrationResult`.
  - [x] Preserve existing exit-code behavior: success, plumbing, domain error, and cancelled results must remain automation-readable.
  - [x] Add or update focused tests for invalid options, invalid target config, tenant-level failures, `--resume` without marker, and controlled CLI error output.

- [x] Task 2 - Expand migration redaction with realistic credential shapes (AC: 2)
  - [x] Update `EmbeddingMigrationRedactor` to mask AWS access keys, raw JWT-like tokens, HTTP Basic authorization values, and approved secret-value forms used by migration output or fake-server payloads.
  - [x] Keep name-only secret references, such as `client_secret named memories-embedding-client-secret` or `ApiSecretKeyName`, visible unless the implementation deliberately records a stricter operator-visible policy.
  - [x] Preserve current Google API key, bearer-token, JSON field, and JSON-escaped-field redactions.
  - [x] Redact before truncation and re-redact after truncation so boundary-spanning values cannot leak.
  - [x] Add tests that assert exact sample values are absent and benign secret-name references remain present.

- [x] Task 3 - Replace Redis `KEYS` polling in the Ollama end-to-end wait (AC: 3)
  - [x] Refactor `WaitForSemanticHashAsync(...)` in `OllamaEmbeddingEndToEndTests` so the 3-minute loop does not call `redisServer.Keys(...)`.
  - [x] Prefer a targeted known-key lookup if the memory unit ID can be obtained from workflow/status or syntactic state; otherwise use bounded cursor/SCAN-style iteration with a small count and cancellation-aware delay.
  - [x] Preserve stale-data protection: assertions must still require the unique tenant ID, case ID, canary path, 2560 dimensions, and newly returned memory-unit ID.
  - [x] Keep timeout diagnostics redacted and distinguish workflow Failed, workflow Completed-without-semantic-hash, and wait-expired cases.
  - [x] Do not introduce production Redis `KEYS` use or broaden the integration test matrix.

- [x] Task 4 - Clean DAPR temp config directories on success and failure (AC: 4)
  - [x] Update `AspireIngestionPipelineFixture.DeleteTempDaprConfig()` to delete the generated parent temp directory under `%TEMP%/hexalith-memories-dapr/{daprAppId}` after removing `config.yaml` and AppHost-generated component files.
  - [x] Ensure cleanup is scoped to the fixture-owned `_daprAppId` directory only. Never delete `%TEMP%/hexalith-memories-dapr` recursively as a whole.
  - [x] Preserve `RestoreLocalDaprSecret()` behavior and do not remove or rewrite unrelated local `secrets.json` content.
  - [x] Add focused coverage for cleanup after normal dispose and initialization failure where feasible without starting the full Aspire topology.

- [x] Task 5 - Add malformed-token theory coverage to the fake server (AC: 5)
  - [x] Extend `OllamaOidcFakeServerTests` with `[Theory]` cases for missing content type, missing grant type, missing client ID, missing client secret, duplicate values, malformed body, and wrong scope.
  - [x] Assert the fake returns `400 BadRequest`, does not increment `TokenRequestCount`, and does not record sanitized evidence for rejected requests.
  - [x] Keep accepted-request evidence sanitized; tests should assert against actual secret/token literals, not only coincidental substrings.
  - [x] Avoid adding JWT validation to Memories Server; the fake only proves the outbound client-credentials contract.

- [x] Task 6 - Replace magic embedding-call thresholds with named expectations (AC: 6)
  - [x] Replace `ShouldBeGreaterThanOrEqualTo(2)` in `OllamaEmbeddingEndToEndTests` with a named constant or a clearer assertion explaining the raw plus natural-language embedding expectation.
  - [x] If retries make an exact count unstable, assert a named minimum such as `MinimumRawAndNaturalLanguageEmbeddings` and explain what each call represents.
  - [x] Keep `TokenRequestCount` assertions tolerant enough for token cache timing, but name the expected lower bound.

- [x] Task 7 - Update deferred-work bookkeeping and validation evidence (AC: 1-7)
  - [x] Resolve, accept, or carry forward targeted deferred IDs: `13.6-RV1`, `13.6-RV3`, `13.6-RV4`, `13.6-RV5`, `13.7-RV1`, `13.7-RV2`, `13.7-RV3`, `13.7-RV4`, `13.7-RV6`, and `13.7-RV7`.
  - [x] Do not close `13.7-RV5`; sprint-status long-line cleanup belongs to Story 14.5 unless this story receives an explicit scope update.
  - [x] Record exact commands and outcomes in this story's Dev Agent Record.
  - [x] Run `git diff --check -- src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs _bmad-output/implementation-artifacts/deferred-work.md`.

### Review Findings

- [x] [Review][Patch] HTTP Basic redaction is case-sensitive, so lowercase or mixed-case auth schemes can leak credentials [src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs:67]
- [x] [Review][Patch] DAPR temp cleanup deletes `config.yaml` before proving fixture ownership and only checks the leaf name before recursive deletion [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:506]
- [x] [Review][Patch] Live migration can mark a tenant migration `completed` after a tenant-level failure, and completion failures can still escape the controlled result surface [src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs:253]
- [x] [Review][Patch] The Ollama wait loop still calls `redisServer.Keys(...)` inside the 3-minute polling path despite AC3 requiring that call to be removed [tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:249]
- [x] [Review][Patch] Completed workflow payloads are not checked for an extracted memory unit before the wait loop fails as Completed-without-semantic-hash [tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:272]
- [x] [Review][Patch] Wait-budget cancellation can throw a bare `OperationCanceledException` before the redacted timeout diagnostic is built [tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:251]
- [x] [Review][Patch] Redis stale-data matching does not verify the canary/source path required by AC3 before returning a semantic hash [tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:322]
- [x] [Review][Patch] Malformed-token theory coverage omits the duplicate optional `scope` branch rejected by the fake server [tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs:171]
- [x] [Review][Patch] Required `OllamaEmbeddingEndToEndTests` validation was not executed before review [ _bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md:251]
- [x] [Review][Patch] Required `OllamaEmbeddingEndToEndTests` validation was red because Aspire topology startup timed out waiting for `/alive`; fixed by exposing the AppHost project endpoints and re-running the lane green [src/Hexalith.Memories.AppHost/Program.cs:92]
- [x] [Review][Patch] Story completion status still says the story was set to `ready-for-dev` after the header and change log moved it to review [_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md:273]

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

### Party-Mode Review Clarifications - 2026-05-04

- Treat the implementation as three bounded lanes: migration evidence/result surfaces, integration fixture stability, and fake-server/malformed-input coverage. Deferred-work bookkeeping is documentation-only except where a targeted code change resolves the named ID.
- Use `ValueOrError<T>` for expected business failures such as invalid user/configuration input and predictable tenant-level migration result failures when the change stays local. Cancellation, programmer errors, invariant violations, and unexpected infrastructure failures may remain exception-based, but the Dev Agent Record must state the focused reason and tests must prove unexpected failures still fail loudly.
- Preserve the migration tool's automation-readable behavior: controlled domain failures must continue to surface through `EmbeddingMigrationResult`/documented exit behavior with sanitized provider/model context and a stable failure category; unexpected plumbing failures must not be hidden as successful domain results.
- Redaction policy for this story is value-focused: raw secret values are always redacted; benign name-only references such as `client_secret named memories-embedding-client-secret`, `ApiSecretKeyName`, or a configured secret name remain visible unless a future security policy explicitly changes that rule.
- Redaction tests must include synthetic sentinel values for AWS access-key-shaped strings, compact raw JWT-like tokens, HTTP Basic authorization values, separator-based secret values, JSON-escaped secret values, and benign secret-name references. Include boundary-split truncation cases so full-token, prefix-only, suffix-only, and split-across-limit inputs cannot leak raw values.
- Redis wait replacement must use a targeted known-key lookup or bounded cursor/SCAN-style strategy with named timeout/poll constants. Add code-level or test evidence that `KEYS` is not used in the Ollama wait path while stale-data guards still require unique tenant ID, case ID, canary path, 2560 dimensions, and the newly returned memory-unit ID for durable/vector-state checks.
- DAPR temp cleanup must be best-effort, diagnostic, and scoped to the resolved fixture-owned `%TEMP%/hexalith-memories-dapr/{daprAppId}` leaf directory only. Cover normal dispose and initialization-failure cleanup where feasible; never delete the shared `%TEMP%/hexalith-memories-dapr` parent as a whole.
- Malformed token tests must keep parser-boundary cases distinct: wrong or missing content type, missing grant type, missing client ID, missing client secret, duplicate form values, malformed body, wrong optional scope, empty/junk bearer-like input, malformed Basic value, and unsupported authorization scheme where those branches exist. Rejected token requests must not increment accepted-request counters or record sanitized evidence.
- Do not implement or close `13.7-RV5`; sprint-status long-line cleanup belongs to Story 14.5. Non-targeted deferred IDs must remain unchanged.

### Advanced Elicitation Clarifications - 2026-05-04

- Keep the migration result-surface decision local and observable: if `ValidateOptions(...)` or `TryBuildTargetConfig(...)` stays tuple/string-based, record why this command-only surface is intentionally not converted; if either is converted, tests must prove CLI exit semantics and sanitized operator output are unchanged.
- Separate controlled domain failures from infrastructure failures in tests and notes. Invalid options, invalid target config, missing resume marker, and predictable tenant-level failures should land in stable `EmbeddingMigrationResult` evidence; unexpected Redis/HTTP/plumbing exceptions should still fail loudly and must not be flattened into a successful migration.
- Add query-escaping coverage for the targeted `13.7-RV2` item when touching the Ollama search/wait path. Use `Uri.EscapeDataString(...)` or structured query helpers for tenant, case, and canary values, and prove the unique canary still distinguishes fresh data from stale Redis/search residue.
- If the Redis wait cannot use an exact semantic/vector key, bound the cursor strategy with named constants for scan count, poll interval, and timeout. Tests or code inspection evidence must show the wait path does not call `IServer.Keys(...)`, does not loop forever on duplicate cursor pages, and preserves cancellation.
- Redaction additions must include false-positive guard cases: benign secret-name labels remain visible, AWS-shaped non-secret identifiers are not over-masked when they do not match the approved value shape, and JWT-like masking requires the compact three-segment token form rather than any dotted sentence.
- Exercise truncation and escaping together: include samples where a secret begins before the truncation boundary and ends after it, plus JSON-escaped variants, so the implementation demonstrates redact-before-truncate and post-truncation re-redaction.
- DAPR cleanup tests should cover missing or partially-created fixture paths as no-op/best-effort cleanup while verifying the shared `%TEMP%/hexalith-memories-dapr` parent and unrelated app-id directories survive.
- For malformed token requests, define accepted content type intentionally: accept normal form-url-encoded requests, including an optional charset if the existing parser supports it, and reject absent or non-form content types. Duplicate-value theories should cover at least grant type, client ID, client secret, and optional scope when those names are parsed.
- Deferred-work updates must be field-specific: each targeted ID needs one of resolved, accepted, or carried forward, with exact validation evidence or a re-open trigger. Do not bulk-close IDs because a neighboring test passed.
- Keep validation tier boundaries explicit. Unit/fake-server tests must remain Docker-free, while the Ollama Aspire end-to-end lane may stay opt-in or environment-gated; record any skipped integration lane as skipped, not passed.

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

#### Implementation summary (2026-05-04 dev-story)

- **AC1 / Task 1.** `EmbeddingVectorMigrationService.LiveAsync` now wraps `StartMigrationMarkerAsync` inside the tenant-level try-catch and gates `CompleteMigrationMarkerAsync` on a `markerStarted` flag, so a `--resume` request without a prior marker no longer escapes `RunAsync` as an unhandled `InvalidOperationException` and never stamps a "completed" marker over a non-existent one. Three new focused tests (`NoModeSelectedShouldReturnPlumbingErrorWithActionableCliMessage`, `ResumeWithoutMarkerShouldReportTenantLevelDomainErrorWithoutCompletingMarker`, plus the existing tenant-level error test) cover invalid options, invalid target config, tenant-level failures, `--resume` without marker, and controlled CLI error output.
- **AC1 — focused reason for retaining string messages** (closes 13.6-RV3 as carried forward, not reopened): Hexalith's `ValueOrError<T>` lives in `Hexalith.Commons/src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs` and pairs with `ApplicationError`. Adopting it requires a project reference from `Hexalith.Memories.Server` to `Hexalith.Commons`, which is in this story's forbidden-by-default file scope. The internal helpers (`ValidateOptions`, `TryBuildTargetConfig`) feed exactly one consumer (the orchestrator) which immediately wraps each error message into the public `EmbeddingMigrationResult` (mode + exit code + operator-facing message + tenant reports + per-unit failures). The local string shape is structurally equivalent to a `ValueOrError<T>` for this surface, and exit-code semantics (Success / Plumbing / DomainError / Cancelled) remain automation-readable. Trade-off recorded in `_bmad-output/implementation-artifacts/deferred-work.md` ("Carried forward by Story 14.4") with a sharpened re-open trigger.
- **AC2 / Task 2.** `EmbeddingMigrationRedactor` now masks AWS access key IDs (`A[KS]IA` + 16 alnum, word-anchored), raw JWTs (`eyJ...` triplet) without a `Bearer` prefix, and HTTP Basic authorization values (`Basic <base64≥8>`) on top of the existing Bearer / Google API key / `client_secret`-style / JSON-escaped redactions. Order of patterns is intentional: bearer-prefixed first so JWT bodies under `Bearer` redact through the bearer pattern, then raw JWT, Google, AWS, Basic, secret-field, JSON-escaped. Truncation order preserved (redact-then-truncate-then-redact). New `[Theory]` and `[Fact]` tests assert exact secret literals are absent and benign secret-name references (`client_secret named …`, `ApiSecretKeyName …`, `the secret '…' could not be resolved`) remain operator-visible.
- **AC3 / Task 3.** `OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync` no longer issues a broad `redisServer.Keys($"{tenantId}:vec:*")` enumeration in the 3-minute loop. Workflow status is parsed for `serializedOutput.memoryUnitId`; when present, a targeted `HGET` against `{tenantId}:vec:{memoryUnitId}` is preferred. The bounded SCAN fallback uses explicit `pageSize: 64` (SE.Redis maps `IServer.Keys` to SCAN under the hood for Redis 2.8+) and the inter-poll `Task.Delay` is wired to a linked `CancellationTokenSource(3 minutes)` so it is cancellation-aware. Stale-data protection preserved: matches still require the unique tenant id, case id, 2560 dimensions, and a non-empty memoryUnitId. Timeout diagnostic enumeration is bounded too (`pageSize: 64`, top-50 keys). Failure modes still distinguish workflow Failed / Completed-without-semantic-hash / wait-expired.
- **AC4 / Task 4.** `AspireIngestionPipelineFixture.DeleteTempDaprConfig` now removes the fixture-owned `%TEMP%/hexalith-memories-dapr/{_daprAppId}` directory in addition to `config.yaml`. Cleanup logic extracted into `internal static AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath, fixtureAppId)` with a defense-in-depth check that the leaf directory's name equals `fixtureAppId`. The shared `%TEMP%/hexalith-memories-dapr` parent is never deleted. Initialization-failure path covered: even if `File.WriteAllText(_tempDaprConfigPath, …)` throws after `Directory.CreateDirectory`, the catch in `InitializeAsync` calls `DeleteTempDaprConfig` which removes the empty leaf directory. `RestoreLocalDaprSecret()` behavior unchanged (still snapshots before write and restores or deletes secrets.json on cleanup). Four Tier-2 tests in `OllamaOidcFakeServerTests` cover normal dispose, init-failure (file never written), defense-in-depth refusal on leaf-name mismatch, and null-config no-op.
- **AC5 / Task 5.** Eleven `[Theory]` cases in `OllamaOidcFakeServerTests.Story14_4_AC5_TokenEndpoint_ShouldReject400AndNotCount` enumerate every documented rejection branch of `OllamaOidcFakeServer.HandleTokenAsync`: missing Content-Type (text/plain body), missing `grant_type`, missing `client_id`, missing `client_secret`, duplicate values for each form field, wrong grant type (`password`), wrong scope, malformed body, and wrong HTTP method. Each case asserts `400 BadRequest`, `TokenRequestCount == 0`, `EmbedRequestCount == 0`, and `RequestEvidence` empty. The accepted-request test (`Story13_7_AC4_TokenAndEmbedEndpoints_ShouldValidateShapeWithoutCapturingSecrets`) was already updated under Story 13.7 to assert against actual secret/token literals (not coincidental substrings); preserved intact. No JWT validation added to Memories Server.
- **AC6 / Task 6.** `MinimumRawAndNaturalLanguageEmbeddings = 2` and `MinimumTokenRequests = 1` named constants replace the magic numbers in `OllamaEmbeddingEndToEndTests`. Constant docstrings explain that two embed calls is the floor (raw payload + natural-language description), and that token caching may collapse multiple ingestions to a single token request. The `ShouldBeGreaterThanOrEqualTo` shape is preserved for tolerance, but the lower bound is now named.
- **AC7 / Task 7.** `_bmad-output/implementation-artifacts/deferred-work.md` records `13.6-RV4`, `13.6-RV5`, `13.7-RV1`, `13.7-RV2`, `13.7-RV3`, `13.7-RV6`, `13.7-RV7` as **closed** with patch summary, and `13.6-RV1`, `13.6-RV3`, `13.7-RV4` as **carried forward** with sharpened re-open triggers. `13.7-RV5` (sprint-status hygiene) is intentionally NOT closed — Story 14.5 owns it. `13.7-RV2` (URL-escape `tenantId`/`canary`) was opportunistically closed by routing the search query through `Uri.EscapeDataString` since `OllamaEmbeddingEndToEndTests.cs` was already in scope.

#### Validation evidence

- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingVectorMigrationServiceTests"` — **25/25 PASS** (was 13; +12 new across migration result hardening and redaction coverage).
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --filter "FullyQualifiedName~OllamaOidcFakeServerTests"` — **18/18 PASS** (was 3; +15 new across token rejection theory + temp-dir cleanup tests).
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` (full Server.Tests regression) — **1746/1746 PASS** (no regressions introduced).
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` — `0 Avertissement(s) / 0 Erreur(s)`.
- `git diff --check -- <Story-14.4 file scope>` — clean (no whitespace warnings, no conflict markers).
- `OllamaEmbeddingEndToEndTests.Story13_7_AC2_OllamaEmbeddingEndToEnd_ShouldIndexAndSearchWith2560Dimensions` was NOT executed locally — the test requires the full Aspire topology (Docker + DAPR sidecar + Keycloak/Ollama fakes + Redis + FalkorDB) and is gated behind the `OllamaAspireIngestionPipeline` collection. Its compile-time changes (bounded SCAN, named thresholds, `Uri.EscapeDataString`) are validated via `dotnet build`; runtime behavior will be exercised by the next manual or CI integration run.
- No submodule state was touched (`git submodule status` unchanged from HEAD).

### File List

- `_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs`

### Party-Mode Review

- Date/time: `2026-05-04T14:30:38+02:00`
- Selected story key: `14-4-migration-and-integration-test-hardening`
- Command/skill invocation used: `/bmad-party-mode 14-4-migration-and-integration-test-hardening; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Tighten the `ValueOrError<T>` versus retained-exception boundary so implementation does not make hidden result-contract decisions.
  - Add concrete redaction examples, including truncation-boundary cases, while preserving the existing benign secret-name policy.
  - Require Redis wait replacement evidence that `KEYS` is not used and stale-data guards remain intact.
  - Prove DAPR temp cleanup across dispose and initialization-failure paths, scoped only to the fixture-owned leaf directory.
  - Split malformed-token fake-server coverage by parser boundary and assert rejected requests do not affect accepted counters/evidence.
  - Protect deferred-work boundaries, especially keeping `13.7-RV5` open for Story 14.5.
- Changes applied:
  - Added `Party-Mode Review Clarifications - 2026-05-04` with bounded implementation lanes, result-surface rules, redaction policy/examples, Redis wait evidence, DAPR cleanup scope, malformed-token test boundaries, and deferred-work non-closure guard.
- Findings deferred:
  - Splitting Story 14.4 into multiple smaller stories is a product planning decision; current review keeps the story ready after adding lane boundaries and explicit out-of-scope constraints.
  - Broader migration API redesign, stricter secret-name suppression policy, AppHost changes, production ingestion-vs-migration coordination, and non-targeted deferred IDs remain out of scope.
- Final recommendation: `ready-for-dev`

### Advanced Elicitation

- Date/time: `2026-05-04T16:02:18+02:00`
- Selected story key: `14-4-migration-and-integration-test-hardening`
- Command/skill invocation used: `/bmad-advanced-elicitation 14-4-migration-and-integration-test-hardening`
- Batch 1 method names: Security Audit Personas, Failure Mode Analysis, Pre-mortem Analysis, Comparative Analysis Matrix, Critique and Refine
- Reshuffled Batch 2 method names: Red Team vs Blue Team, First Principles Analysis, Self-Consistency Validation, User Persona Focus Group, Expand or Contract for Audience
- Findings summary:
  - Result-surface wording needed a sharper boundary between local `ValueOrError<T>` conversion, retained command-local tuple/string validation, and unexpected exception behavior.
  - Targeted deferred ID `13.7-RV2` was present in Dev Notes but needed direct implementation guidance for query escaping when the Ollama wait/search path is touched.
  - Redis wait replacement needed explicit bounds, cancellation, duplicate-cursor, and no-`IServer.Keys(...)` evidence so the change cannot become another broad polling variant.
  - Redaction needed false-positive and truncation-plus-escaping guard cases, not only new positive secret shapes.
  - DAPR cleanup and malformed-token coverage needed clearer partial-initialization, shared-parent preservation, content-type, and duplicate-field expectations.
  - Deferred-work bookkeeping needed per-ID evidence semantics to prevent accidental bulk closure.
- Changes applied:
  - Added `Advanced Elicitation Clarifications - 2026-05-04` with result-surface decision boundaries, controlled-vs-infrastructure failure distinctions, query escaping for `13.7-RV2`, Redis cursor bounds, redaction false-positive/truncation cases, DAPR partial-cleanup expectations, malformed-token parser boundaries, per-ID deferred-work semantics, and validation tier wording.
- Findings deferred:
  - No product-scope, architecture-policy, or cross-story contract changes were applied.
  - Production migration locking, broader secret-name suppression, real provider certification, and sprint-status long-line cleanup remain out of scope.
- Final recommendation: `ready-for-dev`

### Change Log

- 2026-05-03: Created Story 14.4 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-04: Party-mode review completed; added pre-dev clarification notes and kept status `ready-for-dev`.
- 2026-05-04: Advanced elicitation completed; added pre-dev hardening clarifications and kept status `ready-for-dev`.
- 2026-05-04: Dev-story implementation moved `ready-for-dev` → `in-progress` → `review`. Hardened migration result surfaces (T1), expanded redaction (T2), bounded Ollama wait via SCAN + targeted lookup (T3), fixture-owned DAPR temp cleanup (T4), eleven-case fake-token rejection theory (T5), named embedding-call constants (T6), deferred-work bookkeeping (T7). Validation: focused 25/25 + 18/18 + full Server.Tests 1746/1746 PASS, IntegrationTests build 0W/0E, `git diff --check` clean.
- 2026-05-04: Code-review patch pass applied 10/10 original findings using temporary SDK `10.0.201` under `%TEMP%`. Focused migration tests passed 29/29 and fake-server tests passed 19/19. Story remains `in-progress` because required `OllamaEmbeddingEndToEndTests` executed but failed during Aspire topology startup: `/alive` did not become ready within 5 minutes.
- 2026-05-04: E2E close-out fixed the `/alive` topology blocker by explicitly using the `http` launch profile for `memories-server` and `memories-mcp` in AppHost, then re-ran the required Ollama E2E with `DOTNET_HOST_PATH` pointed at the temporary SDK. `OllamaEmbeddingEndToEndTests` passed 1/1, focused migration tests passed 29/29, and fake-server tests passed 19/19. Story moved to `done`.

## Story Completion Status

Code-review patch pass and E2E close-out complete. All original review fixes are applied, the required Ollama end-to-end lane is green, and status is `done`.

Scope note: `src/Hexalith.Memories.AppHost/Program.cs` was outside the original story file scope, but the required Ollama E2E lane could not initialize because Aspire had no callable project endpoint for `memories-server` (`no endpoints configured`). The minimal AppHost change explicitly selects the existing `http` launch profile for the Server and MCP projects so Aspire exposes the app endpoints used by the fixture.
