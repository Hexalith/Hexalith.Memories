---
stepsCompleted:
  - step-01-preflight-and-context
  - step-02-generation-mode
  - step-03-test-strategy
  - step-04-generate-tests
lastStep: step-04-generate-tests
lastSaved: '2026-03-29'
storyId: '1-7'
detectedStack: backend
generationMode: ai-generation
inputDocuments:
  - _bmad-output/implementation-artifacts/1-7-embedding-provider-configuration.md
  - _bmad/tea/testarch/knowledge/data-factories.md
  - _bmad/tea/testarch/knowledge/test-quality.md
  - _bmad/tea/testarch/knowledge/test-levels-framework.md
  - _bmad/tea/testarch/knowledge/test-priorities-matrix.md
---

# ATDD Checklist — Story 1.7: Embedding Provider Configuration

## Summary

- **Total ATDD tests generated:** 30
- **Test files created:** 6
- **TDD Phase:** RED (all tests Skip or throw NotImplementedException)
- **Coverage:** All 4 acceptance criteria mapped

## Acceptance Criteria → Test Mapping

### AC #1: Provider configuration stored per tenant

| ID | Test | File | Priority |
|----|------|------|----------|
| 1.7-UNIT-001 | RoundTrip_AllFieldsPopulated_ShouldProduceIdenticalJson | TenantEmbeddingConfigSerializationTests.cs | P0 |
| 1.7-UNIT-002 | RoundTrip_ReindexRequiredTrue_ShouldPreserve | TenantEmbeddingConfigSerializationTests.cs | P0 |
| 1.7-UNIT-003 | PropertyNames_ShouldBeCamelCase | TenantEmbeddingConfigSerializationTests.cs | P1 |
| 1.7-UNIT-004 | GetEmbeddingConfigAsync_UnconfiguredTenant_ShouldReturnGoogleDefaults | TenantConfigurationActorTests.cs | P0 |
| 1.7-UNIT-005 | SetEmbeddingConfigAsync_NewConfig_ShouldPersistToActorState | TenantConfigurationActorTests.cs | P0 |
| 1.7-UNIT-006 | GetEmbeddingConfig_UnconfiguredTenant_ShouldReturnDefaultConfig | TenantEmbeddingConfigEndpointTests.cs | P1 |

### AC #2: Activity reads tenant config

| ID | Test | File | Priority |
|----|------|------|----------|
| 1.7-UNIT-007 | Google_ShouldReturnCorrectDefaults | EmbeddingProviderDefaultsTests.cs | P0 |
| 1.7-UNIT-008 | Validate_ValidConfig_ShouldNotThrow | EmbeddingProviderDefaultsTests.cs | P1 |
| 1.7-UNIT-009 | RunAsync_ShouldReadConfigFromTenantConfigurationActor | GenerateEmbeddingActivityConfigTests.cs | P0 |
| 1.7-UNIT-010 | RunAsync_ShouldPassConfigToEmbeddingClient | GenerateEmbeddingActivityConfigTests.cs | P0 |
| 1.7-UNIT-011 | RunAsync_ShouldSetRateLimiterCeilingFromConfig | GenerateEmbeddingActivityConfigTests.cs | P1 |
| 1.7-UNIT-012 | RunAsync_ShouldReturnDynamicProviderAndDimensions | GenerateEmbeddingActivityConfigTests.cs | P0 |
| 1.7-UNIT-013 | GenerateAsync_ShouldIncludeOutputDimensionalityInRequest | EmbeddingClientConfigTests.cs | P0 |
| 1.7-UNIT-014 | GenerateAsync_ShouldUseConfiguredEndpointUrl | EmbeddingClientConfigTests.cs | P0 |
| 1.7-UNIT-015 | GenerateAsync_ShouldValidateResponseDimensionsFromConfig | EmbeddingClientConfigTests.cs | P0 |
| 1.7-UNIT-016 | GenerateAsync_TwoConcurrentTenants_ShouldRetrieveCorrectApiKeys | EmbeddingClientConfigTests.cs | P0 |
| 1.7-UNIT-017 | GenerateAsync_ShouldCacheApiKeyBySecretKeyName | EmbeddingClientConfigTests.cs | P1 |

### AC #3: Extensible provider pattern

| ID | Test | File | Priority |
|----|------|------|----------|
| 1.7-UNIT-018 | Validate_DimensionsZero_ShouldThrow | EmbeddingProviderDefaultsTests.cs | P0 |
| 1.7-UNIT-019 | Validate_NegativeDimensions_ShouldThrow | EmbeddingProviderDefaultsTests.cs | P0 |
| 1.7-UNIT-020 | Validate_RateLimitExceedsMaximum_ShouldThrow | EmbeddingProviderDefaultsTests.cs | P0 |
| 1.7-UNIT-021 | Validate_RateLimitZero_ShouldThrow | EmbeddingProviderDefaultsTests.cs | P0 |
| 1.7-UNIT-022 | Validate_ApiSecretKeyNameWithSpecialChars_ShouldThrow | EmbeddingProviderDefaultsTests.cs | P0 |
| 1.7-UNIT-023 | Validate_InvalidApiSecretKeyNames_ShouldThrow (Theory x5) | EmbeddingProviderDefaultsTests.cs | P0 |
| 1.7-UNIT-024 | Validate_EmptyProvider_ShouldThrow (Theory x3) | EmbeddingProviderDefaultsTests.cs | P1 |
| 1.7-UNIT-025 | Validate_EmptyModel_ShouldThrow (Theory x3) | EmbeddingProviderDefaultsTests.cs | P1 |

### AC #4: Reindex warning on config change

| ID | Test | File | Priority |
|----|------|------|----------|
| 1.7-UNIT-026 | SetEmbeddingConfigAsync_ProviderChanged_WithoutForceReindex_ShouldThrow | TenantConfigurationActorTests.cs | P0 |
| 1.7-UNIT-027 | SetEmbeddingConfigAsync_ModelChanged_WithoutForceReindex_ShouldThrow | TenantConfigurationActorTests.cs | P0 |
| 1.7-UNIT-028 | SetEmbeddingConfigAsync_DimensionsChanged_WithoutForceReindex_ShouldThrow | TenantConfigurationActorTests.cs | P0 |
| 1.7-UNIT-029 | SetEmbeddingConfigAsync_ForceReindex_ShouldSaveAndSetReindexRequired | TenantConfigurationActorTests.cs | P0 |
| 1.7-UNIT-030 | SetEmbeddingConfigAsync_RateLimitOnlyChange_ShouldNotRequireForceReindex | TenantConfigurationActorTests.cs | P1 |
| 1.7-UNIT-031 | GetEmbeddingConfigAsync_CorruptedState_ShouldReturnDefault | TenantConfigurationActorTests.cs | P0 |
| 1.7-UNIT-032 | PutEmbeddingConfig_ConfigChangeWithoutForceReindex_ShouldReturn409Conflict | TenantEmbeddingConfigEndpointTests.cs | P0 |
| 1.7-UNIT-033 | PutEmbeddingConfig_WithForceReindex_ShouldReturn200 | TenantEmbeddingConfigEndpointTests.cs | P1 |
| 1.7-UNIT-034 | PutEmbeddingConfig_RateLimitOnlyChange_ShouldReturn200WithoutForceReindex | TenantEmbeddingConfigEndpointTests.cs | P1 |

## Test Files Generated

| File | Tests | AC Coverage |
|------|-------|-------------|
| `tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs` | 3 | AC #1, #4 |
| `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` | 11 | AC #2, #3 |
| `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs` | 8 | AC #1, #4 |
| `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs` | 4 | AC #2 |
| `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientConfigTests.cs` | 5 | AC #2, #3 |
| `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs` | 4 | AC #1, #4 |

## Cross-Story Coverage Summary (Stories 1-1 through 1-7)

| Story | Status | Tests | Coverage |
|-------|--------|-------|----------|
| 1-1 Scaffolding | done | 12 | Smoke + health checks |
| 1-2 Domain Model | done | 51 | Full serialization + enum |
| 1-3 Extraction | done | 20 | Activity + client + contracts |
| 1-4 Embedding | done | 43 | Client + actor + rate limiter |
| 1-5 Indexing | done | 55 | 3 backends + graph builder + integration |
| 1-6 Workflow | in-progress | 55 | Orchestration + compensation + all activities |
| 1-7 Provider Config | **TDD Red** | **35** | **ATDD tests generated** |
| **Total** | | **271 existing + 35 new** | |

## Implementation Checklist

When implementing Story 1.7, use these tests as your red-green-refactor guide:

1. [ ] Create `TenantEmbeddingConfig` sealed record → serialization tests go green
2. [ ] Create `EmbeddingProviderDefaults` static class → defaults + validation tests go green
3. [ ] Create `TenantConfigurationActor` → actor tests go green
4. [ ] Refactor `EmbeddingClient` for config → client config tests go green
5. [ ] Refactor `GenerateEmbeddingActivity` → activity config tests go green
6. [ ] Add REST endpoints → endpoint tests go green
7. [ ] Update existing tests for new constructor shapes
