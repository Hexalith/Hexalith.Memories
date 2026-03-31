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

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google() with { Model = "text-embedding-004" };

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

        TenantEmbeddingConfig newConfig = EmbeddingProviderDefaults.Google() with { Model = "different-model" };

        // Act
        await actor.SetEmbeddingConfigAsync(newConfig, forceReindex: true);

        // Assert
        await stateManager.Received().SetStateAsync(
            "embeddingConfig",
            Arg.Is<TenantEmbeddingConfig>(c => c.ReindexRequired && c.Model == "different-model"),
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
}
