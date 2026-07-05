// <copyright file="TenantConfigurationActorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Actors;

using System.Reflection;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Runtime;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public class TenantConfigurationActorTests
{
    private const string TenantId = "test-tenant";

    [Fact]
    public async Task GetEmbeddingConfigAsync_UnconfiguredTenant_ShouldReturnGoogleDefaults()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        // Act
        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();

        // Assert
        config.Provider.ShouldBe("google");
        config.Model.ShouldBe("gemini-embedding-001");
        config.Dimensions.ShouldBe(768);
        config.RateLimitPerMinute.ShouldBe(1500);
        config.ApiSecretKeyName.ShouldBe("google-embedding-api-key");
        config.ReindexRequired.ShouldBeFalse();
        await stateManager.DidNotReceive().SetStateAsync(
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_NewConfig_ShouldPersistToActorState()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google();

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.Provider == "google" && c.Model == "gemini-embedding-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFusionWeightsAsync_UnconfiguredTenant_ShouldReturnDefaults()
    {
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        stateManager.TryGetStateAsync<FusionWeights>("fusionWeights", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<FusionWeights>(false, default!));

        FusionWeights weights = await actor.GetFusionWeightsAsync();

        weights.ShouldBe(new FusionWeights());
        await stateManager.DidNotReceive().SetStateAsync(
            Arg.Any<string>(),
            Arg.Any<FusionWeights>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetFusionWeightsAsync_ValidWeights_ShouldPersistSeparateStateKey()
    {
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        var weights = new FusionWeights
        {
            SyntacticWeight = 0.2,
            SemanticWeight = 0.3,
            GraphWeight = 0.1,
            NlWeight = 0.4,
        };

        await actor.SetFusionWeightsAsync(weights);

        await stateManager.Received().SetStateAsync(
            "fusionWeights",
            weights,
            Arg.Any<CancellationToken>());
        await stateManager.DidNotReceive().SetStateAsync(
            "embeddingConfig",
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFusionWeightsAsync_StoredWeights_ShouldReturnStoredValue()
    {
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        var stored = new FusionWeights
        {
            SyntacticWeight = 0.1,
            SemanticWeight = 0.2,
            GraphWeight = 0.3,
            NlWeight = 0.4,
        };
        stateManager.TryGetStateAsync<FusionWeights>("fusionWeights", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<FusionWeights>(true, stored));

        FusionWeights weights = await actor.GetFusionWeightsAsync();

        weights.ShouldBe(stored);
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_FirstCustomDimensions_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google() with { Dimensions = 1536 };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.Dimensions == 1536 && !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_ModelChanged_WithoutForceReindex_ShouldThrow()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, EmbeddingProviderDefaults.Google());

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Ollama();

        // Act & Assert
        EmbeddingConfigChangeException ex = await Should.ThrowAsync<EmbeddingConfigChangeException>(
            () => actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false));
        ex.AffectedFields.ShouldContain("model");
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_DimensionsChanged_WithoutForceReindex_ShouldThrow()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, EmbeddingProviderDefaults.Google());

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google() with { Dimensions = 3072 };

        // Act & Assert
        EmbeddingConfigChangeException ex = await Should.ThrowAsync<EmbeddingConfigChangeException>(
            () => actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false));
        ex.AffectedFields.ShouldContain("dimensions");
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_ForceReindex_ShouldSaveAndSetReindexRequired()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, EmbeddingProviderDefaults.Google());

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Ollama();

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: true);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.ReindexRequired && c.Provider == "ollama" && c.Model == "qwen3-embedding:4b"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_RateLimitOnlyChange_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, EmbeddingProviderDefaults.Google());

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 1000 };

        // Act — should NOT throw
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.RateLimitPerMinute == 1000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OllamaBaseUrlChanged_WithoutForceReindex_ShouldThrow()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig() with { BaseUrl = "https://llm.tache.ai/" });

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { BaseUrl = "https://other-llm.tache.ai" };

        // Act & Assert
        EmbeddingConfigChangeException ex = await Should.ThrowAsync<EmbeddingConfigChangeException>(
            () => actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false));
        ex.AffectedFields.ShouldContain("baseUrl");
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OllamaBaseUrlChanged_WithForceReindex_ShouldSaveAndSetReindexRequired()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig() with { BaseUrl = "https://llm.tache.ai" });

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { BaseUrl = "https://other-llm.tache.ai" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: true);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.BaseUrl == "https://other-llm.tache.ai" && c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    // BaseUrl normalization is split into three isolated cases so that a regression dropping
    // any single rule (whitespace trim, trailing-slash trim, ordinal-ignore-case) is localized.
    [Fact]
    public async Task SetEmbeddingConfigAsync_OllamaBaseUrl_WhitespaceOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig() with { BaseUrl = " https://llm.tache.ai " });

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { BaseUrl = "https://llm.tache.ai" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.BaseUrl == "https://llm.tache.ai" && !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OllamaBaseUrl_TrailingSlashOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig() with { BaseUrl = "https://llm.tache.ai/" });

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { BaseUrl = "https://llm.tache.ai" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.BaseUrl == "https://llm.tache.ai" && !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OllamaBaseUrl_CasingOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig() with { BaseUrl = "https://llm.tache.ai" });

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { BaseUrl = "https://LLM.TACHE.AI" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.BaseUrl == "https://LLM.TACHE.AI" && !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    // Each AC5 non-BaseUrl OIDC field is exercised in isolation so that a regression which
    // started forcing reindex for one specific field is not masked by the others.
    [Fact]
    public async Task SetEmbeddingConfigAsync_AuthModeOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig());

        // OIDC-CLIENT-CREDENTIALS exercises the validation case-insensitivity contract too.
        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { AuthMode = "OIDC-CLIENT-CREDENTIALS" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.AuthMode == "OIDC-CLIENT-CREDENTIALS" && !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OidcTokenEndpointOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig());

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with
        {
            OidcTokenEndpoint = "https://auth2.tache.ai/realms/tache/protocol/openid-connect/token",
        };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c =>
                c.OidcTokenEndpoint == "https://auth2.tache.ai/realms/tache/protocol/openid-connect/token" &&
                !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OidcClientIdOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig());

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { OidcClientId = "other-client" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.OidcClientId == "other-client" && !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_ApiSecretKeyNameOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig());

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with
        {
            ApiSecretKeyName = "memories-embedding-client-secret-2",
        };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c =>
                c.ApiSecretKeyName == "memories-embedding-client-secret-2" &&
                !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OidcScopeOnlyDelta_ShouldNotRequireForceReindex()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig());

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { OidcScope = "openid profile" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.OidcScope == "openid profile" && !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    // Story 5.5 Task 5.3 — protect AC3's rate-limit update path by asserting that the breaking-
    // fields contract does not include rateLimitPerMinute. If it ever did, PUT /embedding-config
    // rate-limit-only updates would start throwing 409 by accident.
    [Fact]
    public void GetBreakingChangeFields_RateLimitOnlyDelta_ShouldReturnEmptyList()
    {
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig proposed = current with { RateLimitPerMinute = current.RateLimitPerMinute + 500 };

        string[] affected = EmbeddingProviderDefaults.GetBreakingChangeFields(current, proposed);

        affected.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_NonBreakingChange_ShouldPreserveExistingReindexFlag()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, EmbeddingProviderDefaults.Google() with { ReindexRequired = true });

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google() with
        {
            RateLimitPerMinute = 1000,
            ReindexRequired = false,
        };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.RateLimitPerMinute == 1000 && c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmbeddingConfigAsync_CorruptedState_ShouldReturnDefault()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        stateManager.TryGetStateAsync<TenantEmbeddingConfig>("embeddingConfig", Arg.Any<CancellationToken>())
            .ThrowsAsync(new JsonException("corrupted state"));

        // Act
        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();

        // Assert — returns Google default, does NOT throw
        config.Provider.ShouldBe("google");
        config.Model.ShouldBe("gemini-embedding-001");
        await stateManager.DidNotReceive().SetStateAsync(
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmbeddingConfigAsync_InvalidStoredConfig_ShouldReturnDefault()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        TenantEmbeddingConfig invalidConfig = EmbeddingProviderDefaults.Google() with { ApiSecretKeyName = "invalid/key" };
        SetupExistingState(stateManager, invalidConfig);

        // Act
        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();

        // Assert
        config.Provider.ShouldBe("google");
        config.ApiSecretKeyName.ShouldBe("google-embedding-api-key");
        await stateManager.DidNotReceive().SetStateAsync(
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmbeddingConfigAsync_LegacyGoogleState_ShouldDefaultNewFields()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        TenantEmbeddingConfig legacyConfig = DeserializeLegacyGoogleConfig();
        SetupExistingState(stateManager, legacyConfig);

        // Act
        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();

        // Assert
        config.Provider.ShouldBe("google");
        config.Model.ShouldBe("gemini-embedding-001");
        config.Dimensions.ShouldBe(768);
        config.RateLimitPerMinute.ShouldBe(1500);
        config.ApiSecretKeyName.ShouldBe("google-embedding-api-key");
        config.BaseUrl.ShouldBeNull();
        config.AuthMode.ShouldBe("api-key");
        config.OidcTokenEndpoint.ShouldBeNull();
        config.OidcClientId.ShouldBeNull();
        config.OidcScope.ShouldBeNull();
    }

    [Fact]
    public async Task GetEmbeddingConfigAsync_LegacyState_ShouldNotWriteReplacementState()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, DeserializeLegacyGoogleConfig());

        // Act
        _ = await actor.GetEmbeddingConfigAsync();

        // Assert
        await stateManager.DidNotReceive().SetStateAsync(
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_FirstWrite_ShouldIgnoreClientSuppliedReindexFlag()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google() with { ReindexRequired = true };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: true);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig() with { ReindexRequired = true };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: true);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c =>
                c.Provider == "ollama" &&
                c.AuthMode == "oidc-client-credentials" &&
                !c.ReindexRequired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEmbeddingConfigAsync_OllamaOidcConfig_ShouldPersistAllMetadataFields()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        TenantEmbeddingConfig newConfig = CreateOllamaOidcConfig();

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: false);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => HasOllamaOidcMetadata(c)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmbeddingConfigAsync_OllamaOidcState_ShouldReturnAllMetadataFields()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupExistingState(stateManager, CreateOllamaOidcConfig());

        // Act
        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();

        // Assert
        HasOllamaOidcMetadata(config).ShouldBeTrue();
    }

    [Fact]
    public async Task GetEmbeddingConfigAsync_ExistingConfig_ShouldReturnStoredConfig()
    {
        // Arrange
        (TenantConfigurationActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        TenantEmbeddingConfig storedConfig = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 500 };
        SetupExistingState(stateManager, storedConfig);

        // Act
        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();

        // Assert
        config.RateLimitPerMinute.ShouldBe(500);
    }

    private static (TenantConfigurationActor Actor, IActorStateManager StateManager) CreateActorWithMockState()
    {
        IActorStateManager stateManager = Substitute.For<IActorStateManager>();

        ActorHost host = ActorHost.CreateForTest<TenantConfigurationActor>(
            new ActorTestOptions { ActorId = new ActorId(TenantId) });

        TenantConfigurationActor actor = new(host, NullLogger<TenantConfigurationActor>.Instance);

        PropertyInfo? prop = typeof(Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance);
        prop?.SetValue(actor, stateManager);

        return (actor, stateManager);
    }

    private static void SetupEmptyState(IActorStateManager stateManager)
    {
        stateManager.TryGetStateAsync<TenantEmbeddingConfig>("embeddingConfig", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<TenantEmbeddingConfig>(false, default!));
    }

    private static void SetupExistingState(IActorStateManager stateManager, TenantEmbeddingConfig config)
    {
        stateManager.TryGetStateAsync<TenantEmbeddingConfig>("embeddingConfig", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<TenantEmbeddingConfig>(true, config));
    }

    private static TenantEmbeddingConfig CreateOllamaOidcConfig() => EmbeddingProviderDefaults.Ollama() with
    {
        BaseUrl = "https://llm.tache.ai",
        AuthMode = "oidc-client-credentials",
        OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
        OidcClientId = "memories-embedding",
        OidcScope = "openid",
        ApiSecretKeyName = "memories-embedding-client-secret",
    };

    private static bool HasOllamaOidcMetadata(TenantEmbeddingConfig config) =>
        config.Provider == "ollama" &&
        config.Model == "qwen3-embedding:4b" &&
        config.Dimensions == 2560 &&
        config.RateLimitPerMinute == 6000 &&
        config.BaseUrl == "https://llm.tache.ai" &&
        config.AuthMode == "oidc-client-credentials" &&
        config.OidcTokenEndpoint == "https://auth.tache.ai/realms/tache/protocol/openid-connect/token" &&
        config.OidcClientId == "memories-embedding" &&
        config.OidcScope == "openid" &&
        config.ApiSecretKeyName == "memories-embedding-client-secret";

    private static TenantEmbeddingConfig DeserializeLegacyGoogleConfig()
    {
        const string Json = """
            {
              "provider": "google",
              "model": "gemini-embedding-001",
              "dimensions": 768,
              "rateLimitPerMinute": 1500,
              "apiSecretKeyName": "google-embedding-api-key",
              "reindexRequired": false
            }
            """;

        TenantEmbeddingConfig? config = JsonSerializer.Deserialize<TenantEmbeddingConfig>(Json, MemoriesJsonContext.Options);
        config.ShouldNotBeNull();
        return config;
    }
}
