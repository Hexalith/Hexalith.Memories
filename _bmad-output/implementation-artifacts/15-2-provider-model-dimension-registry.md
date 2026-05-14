# Story 15.2: Provider Model Dimension Registry

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want provider, model, and vector-dimension validation to use a centralized registry,
so that invalid or cross-pollinated embedding configurations fail before tenant state or indexes drift.

## Acceptance Criteria

1. Given provider/model/dimension combinations are validated, when `EmbeddingProviderDefaults.Validate(...)` receives Google, Ollama, or future provider input, then validation is driven by one provider-to-model registry that owns allowed models, dimensions, provider-specific rate-limit ceilings, and default values.

2. Given dimension values can be unbounded today, when a proposed config uses `Dimensions = int.MaxValue` or another out-of-policy dimension, then deferred ID `13.1-RV6` is resolved by a shared upper bound and tests that fail fast at config time.

3. Given cross-provider model names can accidentally validate, when configurations mix provider and model families such as Google with `qwen3-embedding:4b` or Ollama with `gemini-embedding-001`, then deferred ID `13.1-RV11` is resolved with provider/model negative tests and no special-case downstream parser assumptions.

4. Given casing and persistence can affect comparisons, when provider/model values round-trip through tenant configuration or persisted `{provider}:{model}` strings, then deferred IDs `13.1-RV10` and `13.3-RV8` are either resolved by documented normalization/equality semantics or explicitly accepted with rationale.

5. Given this story touches tenant configuration validation, when it completes, then contract/server tests cover success, invalid provider, invalid model, invalid dimension, max-dimension boundary, cross-provider negative paths, rate-limit ceiling lookup, and deferred-work dispositions for all targeted IDs.

6. Given tenant embedding configuration is persisted or used to create/update indexes, when provider, model, dimension, or rate-limit values are invalid for the tenant's selected provider/model pair, then validation fails before tenant config persistence and before any index creation/update path can use cross-provider or default-fallback values.

## Tasks / Subtasks

- [x] Task 0 - Verify current registry target and active deferred IDs (AC: 1-5)
  - [x] Read `EmbeddingProviderDefaults.cs`, `TenantEmbeddingConfig.cs`, `EmbeddingProviderDefaultsTests.cs`, `TenantEmbeddingConfigSerializationTests.cs`, and the `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` entries in `deferred-work.md` before editing.
  - [x] Confirm Stories 13.1, 13.3, 13.4, 13.5, 13.6, 13.7, 14.3, and 14.5 are `done`; if any prerequisite is not done, stop and record the exact status.
  - [x] Preserve the committed Google and Ollama runtime behavior unless an acceptance criterion explicitly changes validation rejection timing.

- [x] Task 1 - Introduce one provider/model registry in `EmbeddingProviderDefaults` (AC: 1, 2, 3)
  - [x] Replace scattered provider/model/dimension/rate-limit checks with a single local registry owned by `EmbeddingProviderDefaults`.
  - [x] Keep the public constants `GoogleProviderName`, `OllamaProviderName`, `GoogleModelName`, `OllamaModelName`, `ApiKeyAuthMode`, and `OidcClientCredentialsAuthMode` source-compatible.
  - [x] Model registry entries must include allowed model names, allowed dimensions per model, provider max rate limit, and the provider default config shape.
  - [x] Add a shared maximum dimension policy for unknown or future entries. Start from the deferred recommendation of `16_384` unless code analysis proves a different explicit bound is safer; record the chosen value in XML docs or a short code comment.
  - [x] Reject provider/model pairs that do not exist in the registry even if the model name matches the generic regex.
  - [x] Treat the registry as a closed allowlist for this story: custom/unregistered models are rejected unless a future story explicitly adds a validated extension point.
  - [x] Ensure provider/model lookup cannot fall back to another provider's defaults, dimensions, or rate-limit ceiling when input is missing, mixed case, or partially matched.
  - [x] Keep error messages actionable and field-specific. Unsupported provider messages list supported providers; unsupported model messages list models for that provider; unsupported dimension messages list allowed dimensions and/or the upper bound.

- [x] Task 2 - Pin casing and normalization semantics (AC: 4)
  - [x] Decide and implement whether `Validate(...)` only accepts canonical provider/model casing or accepts case-insensitive input but preserves original casing.
  - [x] Do not lowercase Ollama model tags blindly. Ollama tags may be case-sensitive; if canonicalization is used, prove it is safe for committed models only.
  - [x] Make the compatibility behavior explicit for existing tenant configs that currently validate only because provider/model checks are loose: reject on the next validation/write or document any detect-only path with a clear operator remediation message.
  - [x] Do not add tenant-specific operator overrides for model dimensions or provider rate-limit ceilings in this story. If override support is judged necessary, record it as a deferred decision rather than adding a dynamic registry.
  - [x] Add tests covering `Provider = "Ollama"` and mixed-case model strings. The tests must document whether this is accepted, normalized, or rejected.
  - [x] Review `EmbeddingClient` provider/model parser behavior for `GOOGLE:Gemini-Embedding-001` and `ollama:qwen3-embedding:4b`. If parser behavior changes, keep first-colon splitting and preserve model tags with embedded colons.
  - [x] Resolve or explicitly accept `13.1-RV10` and `13.3-RV8` with evidence/rationale in `deferred-work.md`.

- [x] Task 3 - Update focused validation tests (AC: 1-5)
  - [x] Add negative tests for Google with `qwen3-embedding:4b`, Ollama with `gemini-embedding-001`, Ollama with an unknown model, Google with an unknown model, punctuation-only models, and `Dimensions = int.MaxValue`.
  - [x] Add positive tests for every registry-supported provider/model/dimension combination currently committed: Google `gemini-embedding-001` with `768`, `1536`, `3072`; Ollama `qwen3-embedding:4b` with `2560`.
  - [x] Add rate-limit tests that prove provider ceiling lookup comes from the registry and cannot silently fall back to Google's ceiling for a future provider.
  - [x] Add null-config and malformed provider/model tests if absent, without weakening existing Google/Ollama behavior.
  - [x] Add or update serialization/round-trip tests only if Task 2 changes casing or canonicalization semantics.

- [x] Task 4 - Update deferred-work dispositions (AC: 2-5)
  - [x] Add a Story 15.2 rollup section to `_bmad-output/implementation-artifacts/deferred-work.md`.
  - [x] Mark `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` as `resolved`, `accepted`, or `carried-forward` using the Story 14.5 structured fields: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [x] Do not sweep adjacent IDs such as `13.1-RV1` through `13.1-RV5`, `13.1-RV7`, `13.1-RV8`, `13.1-RV9`, `13.3-RV9`, or migration coordination entries unless implementation genuinely resolves them and the story records why they became in scope.
  - [x] Preserve historical context; add structured disposition blocks rather than deleting original review notes.

- [x] Task 5 - Validate and record completion (AC: 1-5)
  - [x] Run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"`.
  - [x] If serialization/casing semantics changed, run `dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --filter "FullyQualifiedName~TenantEmbeddingConfigSerializationTests"`.
  - [x] If the `EmbeddingClient` parser changes, run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingClientTests"`.
  - [x] Run `dotnet build Hexalith.Memories.slnx` when the local SDK permits it.
  - [x] Run `git diff --check -- src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md`.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` - UPDATE. Central provider/model/dimension/rate-limit registry, validation, error messages, and defaults.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` - UPDATE. Registry success, invalid-provider, invalid-model, invalid-dimension, max-dimension, cross-provider, casing, and rate-limit coverage.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Structured dispositions for targeted deferred IDs.
- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Possible files only if Task 2 proves they are necessary:

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` - UPDATE only if provider/model casing canonicalization must be documented at the contract boundary. Do not change JSON property names.
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs` - UPDATE only if contract-level casing or round-trip behavior changes.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` - UPDATE only if resolving `13.3-RV8` requires parser normalization or comparison changes. Preserve first-colon model parsing.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` - UPDATE only with matching parser/casing coverage if `EmbeddingClient.cs` changes.
- `docs/operations/embedding-providers.md` - UPDATE only if the final registry semantics change operator-facing provider/model/dimension rules.
- `docs/dev/embedding-providers.md` - UPDATE only if developer-facing registry semantics need a short cross-reference.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/13-1-extend-embedding-provider-defaults-to-accept-ollama.md`
- `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`
- `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md`
- `_bmad-output/implementation-artifacts/13-5-surface-new-fields-via-tenant-configuration-actor.md`
- `_bmad-output/implementation-artifacts/14-3-oidc-and-embedding-security-hardening.md`
- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md`
- `docs/operations/embedding-providers.md`
- `docs/dev/embedding-providers.md`

Forbidden by default:

- `.github/**`
- `tools/MigrateEmbeddingVectors/**`
- `src/Hexalith.Memories.Server/Migration/**`
- `src/Hexalith.Memories.Server/Activities/**`
- `src/Hexalith.Memories.Server/Workflows/**`
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` unless a compile-only registry call-site adjustment is unavoidable and explicitly recorded.
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

`EmbeddingProviderDefaults` currently validates providers and models through separate checks:

- Supported providers are hard-coded by `IsSupportedProvider(...)`.
- `ModelNamePattern()` enforces shape only. It no longer accepts punctuation-only values, but it does not prove the model belongs to the selected provider.
- Google dimensions are checked only when `Model == "gemini-embedding-001"`.
- Ollama dimensions are checked only when `Model == "qwen3-embedding:4b"`.
- `Dimensions <= 0` is rejected, but no upper bound exists for unknown accepted models.
- Rate-limit ceiling selection is currently provider ternary logic, not registry lookup.

That means a cross-pollinated config can still validate if its model-specific dimension rule is satisfied. The highest-risk examples are:

- `Provider = "google", Model = "qwen3-embedding:4b", Dimensions = 2560`.
- `Provider = "ollama", Model = "gemini-embedding-001", Dimensions = 768`.
- `Provider = "ollama", Model = "totally-fake", Dimensions = 1`.

Story 13.4 partially closed `13.1-RV11` by tightening the model regex and rejecting non-Ollama `oidc-client-credentials`, but it deliberately left the provider/model/dimension allowlist for a dedicated registry story. This is that story.

### Registry Shape Guidance

Keep the registry local and boring. A private immutable array or dictionary in `EmbeddingProviderDefaults` is enough. Do not introduce a plugin system, service-provider abstraction, live model discovery, remote model capability probing, or dynamic operator-editable registry in this story.

The registry should make these questions answerable from one place:

- Which providers are supported?
- Which models belong to each provider?
- Which dimensions are valid for each model?
- What is the provider rate-limit ceiling?
- What default `TenantEmbeddingConfig` should `Google()` and `Ollama()` return?

Prefer small helper methods such as `FindProvider(...)`, `FindModel(...)`, `GetSupportedProviderNames()`, and `GetSupportedModelNames(provider)` over broad abstractions. Keep allocations and error-message formatting simple; this path runs during config validation, not per vector element.

The registry contract is closed and authoritative for this story:

- Provider lookup may normalize for comparison, but must not produce ambiguous or provider-crossing matches.
- Model lookup is scoped to the selected provider; a model valid for another provider must fail even when dimensions match.
- Each registry entry owns the allowed dimensions, default dimension, provider rate-limit ceiling, and default `TenantEmbeddingConfig` shape.
- Unknown providers, unknown models, unsupported dimensions, and impossible rate-limit/default lookups fail before tenant config persistence or index creation/update can consume the config.
- Error messages should name the field and list supported providers/models or allowed dimensions, while tests should assert stable fragments rather than full prose.

### Casing and Persistence Decision

Do not assume case-insensitive validation means persisted values are canonical. Today validation uses `OrdinalIgnoreCase`, while persisted strings can keep caller casing. `EmbeddingClient` lowercases the provider when parsing persisted `{provider}:{model}` strings but preserves model casing. That may be correct for Ollama tags, but it must be intentional and tested.

Acceptable outcomes:

- **Resolve:** enforce canonical provider/model values in `Validate(...)` and tests, or normalize them at the write boundary with documented behavior.
- **Accept:** preserve case-insensitive validation and original casing because model tags may be case-sensitive, but add tests and rationale that downstream comparisons use `OrdinalIgnoreCase` where required and first-colon parsing preserves the model verbatim.

Whichever path is chosen, record the disposition for `13.1-RV10` and `13.3-RV8` in `deferred-work.md`.

Compatibility behavior must also be explicit. Existing tenant configs that only worked because validation was loose should fail on the next validation/write with actionable provider/model/dimension guidance; automatic data migration or tenant repair is out of scope unless a later story owns it.

### Deferred IDs Targeted

This story is the normal lifecycle home for:

- `13.1-RV6`: `Dimensions = int.MaxValue` accepted by validation.
- `13.1-RV10`: mixed-case provider/model strings persist verbatim.
- `13.1-RV11`: tolerant defaults / cross-pollinated provider-model-dimension configs validate.
- `13.3-RV8`: persisted provider/model parser lowercases provider while preserving model casing.

Do not close these by assertion only. Each disposition needs code, tests, documentation, or explicit accepted rationale.

### Implementation Guardrails

- Preserve existing public constants and default factory methods for source compatibility.
- Do not change default Google or Ollama values unless the registry implementation would otherwise duplicate them.
- Do not broaden Ollama runtime support for `api-key` mode. `EmbeddingClient` still accepts Ollama only with `oidc-client-credentials` unless a separate product decision changes it.
- Do not change tenant actor reindex behavior except through validation. `GetBreakingChangeFields(...)` should still report provider, model, dimensions, and Ollama `baseUrl` changes exactly as before.
- Do not weaken URL, auth-mode, or secret-name validation added by Story 14.3.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.

### Testing Requirements

Use xUnit, Shouldly, and the existing test files. Add targeted tests instead of replacing the existing suite.

Minimum focused test additions:

- Google default model allows `768`, `1536`, and `3072`.
- Ollama default model allows exactly `2560`.
- `Dimensions = int.MaxValue` fails at validation time.
- `Provider = "google", Model = "qwen3-embedding:4b", Dimensions = 2560` fails.
- `Provider = "ollama", Model = "gemini-embedding-001", Dimensions = 768` fails.
- `Provider = "ollama", Model = "totally-fake", Dimensions = 1` fails.
- Unsupported-provider messages list every registry provider.
- Unsupported-model messages list the selected provider's supported models.
- Rate-limit ceiling lookup is keyed by provider and cannot silently fall back to Google's ceiling for another supported provider.
- Casing tests document the chosen behavior for provider/model input and persisted parser output.
- Public-boundary tests prove invalid configs fail before tenant config persistence and before index creation/update paths can use a mismatched provider/model pair.
- Tenant-isolation-oriented tests prove one tenant's provider/model/default selection cannot inherit or reuse another provider's defaults or rate-limit ceiling.
- Deferred-work dispositions cite automation-readable evidence: named tests, structured story sections, or tracked artifacts rather than free-text assertions alone.

## Project Structure Notes

This is a validation and governance story. The expected runtime code change is limited to `EmbeddingProviderDefaults`, with possible parser/casing adjustments only if needed to resolve `13.3-RV8`. Migration tooling, ingestion workflows, Redis index creation, OIDC token acquisition, and operator deployment guides are out of scope unless the final registry semantics force a narrow documentation correction.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 15 and Story 15.2 acceptance criteria.
- `_bmad-output/implementation-artifacts/deferred-work.md` - target deferred IDs `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8`.
- `_bmad-output/implementation-artifacts/13-1-extend-embedding-provider-defaults-to-accept-ollama.md` - provider-defaults foundation and original deferred validation risks.
- `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md` - first-colon persisted provider/model parser and `13.3-RV8` casing risk.
- `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md` - OIDC config, URL validation, and partial regex/cross-provider review decisions.
- `_bmad-output/implementation-artifacts/14-3-oidc-and-embedding-security-hardening.md` - URL, auth, and redaction hardening that must not regress.
- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md` - structured deferred-work schema required for dispositions.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` - current provider defaults, validation, URL checks, and breaking-change detection.
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` - tenant config record and JSON constructor semantics.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` - provider dispatch and persisted `{provider}:{model}` parser behavior.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` - current validation test suite.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` - parser tests if `13.3-RV8` is touched.
- `docs/operations/embedding-providers.md` - operator-facing provider/model/dimension matrix.
- `docs/dev/embedding-providers.md` - developer-facing provider surface summary.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight JSON timestamp `2026-05-12T17:55:50Z` passed all checks with `working tree cleanliness` reporting `0 dirty paths`.
- Story selection chose `15-2-provider-model-dimension-registry` because `ready_count` was `1`, below the target of `5`, and this was the first backlog story in sprint-status order.
- `/bmad-create-story 15-2-provider-model-dimension-registry` context gathering loaded Epic 15 planning, sprint status, root project context, Stories 13.1, 13.3, 13.4, 14.3, 14.5, current deferred-work entries, provider operations docs, current `EmbeddingProviderDefaults`, `TenantEmbeddingConfig`, focused tests, and recent git history.
- No external technology research was needed for this story. The implementation surface is repository-owned validation logic, provider metadata, and test coverage.
- Party-mode review ran on 2026-05-12 after preflight JSON timestamp `2026-05-12T20:03:09Z` passed all checks with `working tree cleanliness` reporting `0 dirty paths`.
- 2026-05-13 dev-story start: story and sprint status moved `ready-for-dev` -> `in-progress`.
- Task 0 verification loaded the allowed implementation files plus the targeted deferred entries. Stories 13.1, 13.3, 13.4, 13.5, 13.6, 13.7, 14.3, and 14.5 are all `done` in `sprint-status.yaml`.
- Red phase: `EmbeddingProviderDefaultsTests` failed 15 new registry assertions before implementation, covering cross-provider model pairs, unknown models, syntactically valid but unregistered models, and `Dimensions = int.MaxValue`.
- Validation: `EmbeddingProviderDefaultsTests` 141/141 PASS; `EmbeddingClientTests` 64/64 PASS; `TenantConfigurationActorTests` 28/28 PASS; `TenantEmbeddingConfigSerializationTests` 6/6 PASS; full `Hexalith.Memories.Server.Tests` 1763/1763 PASS on rerun after one order-sensitive metric test passed in isolation.
- Validation: `dotnet build Hexalith.Memories.slnx` PASS with 0 warnings and 0 errors; `git diff --check` PASS with only expected LF-to-CRLF notices.

### Completion Notes List

- Story context created on 2026-05-12.
- Scope is limited to central provider/model/dimension/rate-limit validation, casing/persistence decision evidence, focused tests, and targeted deferred-work dispositions.
- Runtime dispatch, OIDC token acquisition, migration coordination, AppHost/integration topology, package metadata, CI workflows, release tooling, and submodules are forbidden by default.
- No submodule state was touched.
- Party-mode review hardened the story with closed-registry semantics, fail-before-persistence/index-use boundaries, casing/persistence compatibility guidance, tenant-isolation expectations, and automation-readable deferred-work evidence requirements.
- Task 0 confirmed the current validation target: provider support, model shape, model-specific dimensions, and provider rate-limit ceilings are still separate checks in `EmbeddingProviderDefaults`.
- Implemented a closed local provider/model registry in `EmbeddingProviderDefaults` with provider-scoped models, dimensions, default config factories, and rate-limit ceilings. Google and Ollama public constants and defaults remain source-compatible.
- Preserved compatibility casing semantics: validation is case-insensitive and preserves caller casing; persisted provider/model parsing lowercases the provider key and preserves the post-first-colon model string verbatim. `13.1-RV10` and `13.3-RV8` are accepted with explicit rationale.
- Resolved `13.1-RV6` and `13.1-RV11` with config-time dimension upper-bound and provider-scoped closed-registry validation. Invalid provider/model/dimension/rate-limit values fail before tenant config persistence or index update paths can consume them.
- Updated two actor tests that previously used invalid fake model names for reindex behavior; they now use the supported Google-to-Ollama transition to preserve the original behavior under the closed registry.

### File List

- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`

### Change Log

- 2026-05-12: Created Story 15.2 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-12: Party-mode review completed; added registry contract, casing, failure-boundary, tenant-isolation, compatibility, and evidence clarifications.
- 2026-05-13: Started implementation and completed Task 0 verification.
- 2026-05-13: Implemented provider/model/dimension registry, focused tests, deferred-work dispositions, and moved story to review.

### Party-Mode Review

- Date/time: `2026-05-12T22:23:26+02:00`
- Selected story key: `15-2-provider-model-dimension-registry`
- Command/skill invocation used: `/bmad-party-mode 15-2-provider-model-dimension-registry; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - The story needed an explicit closed-registry contract for provider keys, model keys, dimension lists, default dimensions, rate-limit ceilings, and default config shape.
  - Provider/model casing and persistence behavior needed to be testable before development, especially for mixed-case values and persisted `{provider}:{model}` parsing.
  - Cross-provider validation had to fail by construction, not by ad hoc dimension checks or fallback defaults.
  - Invalid provider/model/dimension/rate-limit combinations needed to fail before tenant config persistence or index creation/update paths can consume the config.
  - Existing loose configs needed a deliberate compatibility behavior, while automatic tenant repair and data migration should stay out of scope.
  - Deferred-work dispositions needed automation-readable evidence such as named tests, structured story sections, or tracked artifacts.
- Changes applied:
  - Added AC #6 to require fail-before-persistence and fail-before-index-use behavior for invalid provider/model/dimension/rate-limit combinations.
  - Hardened registry tasks with closed-allowlist behavior, no cross-provider fallback, and no dynamic tenant-specific overrides in this story.
  - Added registry contract guidance for scoped model lookup, entry-owned dimensions/defaults/rate limits, and stable error-message fragments.
  - Added compatibility guidance for existing tenant configs that currently pass because validation is loose.
  - Expanded testing requirements for public-boundary validation, tenant-isolation/default leakage prevention, and automation-readable deferred-work evidence.
- Findings deferred:
  - Data migration or automatic tenant repair for already-persisted invalid configs remains out of scope.
  - Dynamic provider discovery, live provider API verification, dynamic rate-limit discovery, and tenant-specific override support remain out of scope.
  - OIDC/token acquisition, migration/backfill, AppHost/integration topology, release tooling, broad documentation refreshes, and submodule work remain out of scope.
- Final recommendation: `ready-for-dev`

## Story Completion Status

Story context created and ready for implementation. Status set to `ready-for-dev`.
