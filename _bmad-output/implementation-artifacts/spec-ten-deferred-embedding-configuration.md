---
title: 'Close ten deferred embedding configuration findings'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close ten ledger entries about selecting, validating, and consuming tenant embedding configuration.

- Given a registered provider name with surrounding whitespace, when validating it, then return a provider-field diagnostic naming the whitespace issue while retaining case-insensitive lookup for unpadded names (DW-51, DW-138).
- Given an embedding migration request, when selecting target defaults, then use the configuration registry for supported targets, keep Ollama as the omitted target, and report an invalid target for unknown providers (DW-50).
- Given mixed-case provider and model names, when serializing, saving a casing-only update, or parsing an existing identifier, then preserve caller spelling in the config, avoid a reindex, and preserve the model suffix while normalizing the parsed provider (DW-42, DW-45, DW-113, DW-144).
- Given an omitted or blank tenant OIDC scope, when requesting a token, then omit the form field and reuse the unscoped cache key (DW-122).
- Given the maximum Ollama rate limit, when the one-minute budget expires, then replenish it and debit one token without overflow. Document why the default rate is lower than the cap (DW-121, DW-143).

</frozen-after-approval>

## Implementation Notes

- Baseline `60c1754ef4a2041872c579785f729d7eb392fe70` on clean and synchronized `main`. The local AppHost started and Aspire enumerated its resources during startup. The AppHost was stopped after inspection.
- Reuse the configuration registry in `EmbeddingProviderDefaults` and the existing target error path in `EmbeddingVectorMigrationService`. Exercise the existing casing, OIDC form/cache, and fixed-window limiter behavior in the relevant contract and server test classes.

- The rate path passes the validated ceiling directly from both embedding activities to the actor. The limiter applies it with a minimum clamp, refills from the configured ceiling once per minute, and subtracts one from a positive balance.
- Tenant OIDC scope normalization already maps omitted and blank values to the same key and omits the outbound scope field. Test a blank first request so cached results cannot mask a malformed form. The existing parser test covers mixed-case provider names and a model suffix containing a colon.

- Debug builds with source project references passed. Six focused Server.Tests classes passed 448 cases and the focused Contracts.Tests class passed 8 cases.
- Contracts.Tests passed all 602 cases. The Debug full Server.Tests run encountered the package-only EventStore asset guard under source project references. A Release rebuild with package references passed the full Server.Tests suite with 3,376 passes and one pre-existing skip.
- The ledger check verified exactly ten changed statuses. BMAD's dry-run count fell from 356 to 346 open numbered entries.
- After review, the Release package-reference build passed with zero warnings or errors. The focused Server run passed 460 cases. The full Server suite passed 3,378 cases with one pre-existing skip.

## Review Triage Log

- Low, rejected. The tool opens its Redis connection before service validation. The user sees the backend failure first when both Redis is unavailable and the provider input is invalid. Changing startup order would move validation across the CLI composition boundary.
- Medium, patched. The actor test verifies the state key and stored data, then reads the captured configuration back and checks both original reindex flag values.
- Medium, patched. The parser tests cover identifier parsing in isolation. The index activity test separately proves the runtime migration guard accepts mixed-case provider and model values with a tagged model and preserves both stored fields.
- Medium, patched. The rate limiter test checks the updated window start and a second debit at the same timestamp to detect repeated refills.
- Medium, patched. The dry run pins a mixed-case model override and valid nondefault dimensions alongside the existing registry default and invalid input tests.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-embedding-configuration.md`
