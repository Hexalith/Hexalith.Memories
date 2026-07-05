// <copyright file="TenantConfigurationActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using System.Text.Json;

using Dapr.Actors.Runtime;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>DAPR Actor that stores per-tenant embedding provider configuration. Actor ID = tenant ID.</summary>
internal sealed class TenantConfigurationActor : Actor, ITenantConfigurationActor
{
    private const string StateName = "embeddingConfig";
    private const string FusionWeightsStateName = "fusionWeights";

    private readonly ILogger<TenantConfigurationActor> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantConfigurationActor"/> class.</summary>
    /// <param name="host">The actor host provided by the DAPR runtime.</param>
    /// <param name="logger">The logger.</param>
    public TenantConfigurationActor(ActorHost host, ILogger<TenantConfigurationActor> logger)
        : base(host)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TenantEmbeddingConfig> GetEmbeddingConfigAsync()
    {
        TenantEmbeddingConfig? storedConfig = await TryGetStoredEmbeddingConfigAsync().ConfigureAwait(false);
        return storedConfig ?? EmbeddingProviderDefaults.Google();
    }

    /// <inheritdoc/>
    public async Task<FusionWeights> GetFusionWeightsAsync()
    {
        FusionWeights? storedWeights = await TryGetStoredFusionWeightsAsync().ConfigureAwait(false);
        return storedWeights ?? new FusionWeights();
    }

    /// <inheritdoc/>
    public async Task SetEmbeddingConfigAsync(TenantEmbeddingConfig config, bool forceReindex)
    {
        ArgumentNullException.ThrowIfNull(config);
        EmbeddingProviderDefaults.Validate(config);

        TenantEmbeddingConfig? current = await TryGetStoredEmbeddingConfigAsync().ConfigureAwait(false);
        if (current is null)
        {
            TenantEmbeddingConfig initialConfig = config with { ReindexRequired = false };
            await StateManager.SetStateAsync(StateName, initialConfig).ConfigureAwait(false);
            return;
        }

        string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(current, config);

        if (affectedFields.Length > 0)
        {
            if (!forceReindex)
            {
                throw new EmbeddingConfigChangeException(
                    Id.GetId(),
                    current,
                    config,
                    affectedFields);
            }
        }

        bool reindexRequired = current.ReindexRequired || (affectedFields.Length > 0 && forceReindex);
        TenantEmbeddingConfig configToStore = config with { ReindexRequired = reindexRequired };
        await StateManager.SetStateAsync(StateName, configToStore).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetFusionWeightsAsync(FusionWeights weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        weights.Validate();
        await StateManager.SetStateAsync(FusionWeightsStateName, weights).ConfigureAwait(false);
    }

    private async Task<TenantEmbeddingConfig?> TryGetStoredEmbeddingConfigAsync()
    {
        try
        {
            ConditionalValue<TenantEmbeddingConfig> result = await StateManager
                .TryGetStateAsync<TenantEmbeddingConfig>(StateName)
                .ConfigureAwait(false);

            if (!result.HasValue)
            {
                return null;
            }

            try
            {
                EmbeddingProviderDefaults.Validate(result.Value);
                return result.Value;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid embedding config state for tenant '{TenantId}'. Returning Google defaults.",
                    Id.GetId());
                return null;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Corrupted embedding config state for tenant '{TenantId}'. Returning Google defaults.",
                Id.GetId());
            return null;
        }
    }

    private async Task<FusionWeights?> TryGetStoredFusionWeightsAsync()
    {
        try
        {
            ConditionalValue<FusionWeights> result = await StateManager
                .TryGetStateAsync<FusionWeights>(FusionWeightsStateName)
                .ConfigureAwait(false);

            if (!result.HasValue)
            {
                return null;
            }

            try
            {
                result.Value.Validate();
                return result.Value;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid fusion weights state for tenant '{TenantId}'. Returning default fusion weights.",
                    Id.GetId());
                return null;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Corrupted fusion weights state for tenant '{TenantId}'. Returning default fusion weights.",
                Id.GetId());
            return null;
        }
    }
}
