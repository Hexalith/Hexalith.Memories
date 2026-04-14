// <copyright file="GenerateEmbeddingActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

/// <summary>DAPR Workflow activity that generates embeddings via configurable provider API with per-tenant rate limiting.</summary>
public sealed class GenerateEmbeddingActivity : WorkflowActivity<EmbeddingInput, EmbeddingResult>
{
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly EmbeddingClient _embeddingClient;

    /// <summary>Initializes a new instance of the <see cref="GenerateEmbeddingActivity"/> class.</summary>
    /// <param name="embeddingClient">The embedding client for provider API calls.</param>
    /// <param name="actorProxyFactory">The factory for creating DAPR actor proxies.</param>
    public GenerateEmbeddingActivity(EmbeddingClient embeddingClient, IActorProxyFactory actorProxyFactory)
    {
        _embeddingClient = embeddingClient;
        _actorProxyFactory = actorProxyFactory;
    }

    /// <inheritdoc/>
    public override async Task<EmbeddingResult> RunAsync(
        WorkflowActivityContext context,
        EmbeddingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ContentText);

        ITenantConfigurationActor tenantConfigActor = _actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(
                new ActorId(input.TenantId),
                nameof(TenantConfigurationActor));

        TenantEmbeddingConfig config = await tenantConfigActor
            .GetEmbeddingConfigAsync()
            .ConfigureAwait(false);

        await _embeddingClient
            .PrimeApiKeyAsync(input.TenantId, config, CancellationToken.None)
            .ConfigureAwait(false);

        IEmbeddingRateLimiterActor rateLimiter = _actorProxyFactory
            .CreateActorProxy<IEmbeddingRateLimiterActor>(
                new ActorId(input.TenantId),
                nameof(EmbeddingRateLimiterActor));

        await rateLimiter.SetCeilingAsync(config.RateLimitPerMinute).ConfigureAwait(false);

        bool allowed = await rateLimiter.TryConsumeAsync().ConfigureAwait(false);
        if (!allowed)
        {
            throw new EmbeddingRateLimitException(input.TenantId);
        }

        float[] vector = await _embeddingClient
            .GenerateAsync(input.ContentText, input.TenantId, config, CancellationToken.None)
            .ConfigureAwait(false);

        return new EmbeddingResult(vector, $"{config.Provider}:{config.Model}", config.Dimensions)
        {
            Model = config.Model,
        };
    }
}
