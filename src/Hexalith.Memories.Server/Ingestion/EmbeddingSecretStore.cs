// <copyright file="EmbeddingSecretStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Collections.Concurrent;

using Dapr.Client;

/// <summary>Shared DAPR secret retrieval for embedding providers. Caches by secret key name and supports eviction so a
/// rotated secret is re-read after an authentication failure (Story 23.9, AC4). The cache is intentionally keyed by the
/// tenant's <c>ApiSecretKeyName</c> rather than the tenant id so distinct tenants with distinct secret keys never collide.</summary>
internal sealed class EmbeddingSecretStore
{
    private const string SecretStoreName = "secretstore";

    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly DaprClient _daprClient;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingSecretStore"/> class.</summary>
    /// <param name="daprClient">The DAPR client for secret retrieval.</param>
    public EmbeddingSecretStore(DaprClient daprClient)
    {
        ArgumentNullException.ThrowIfNull(daprClient);
        _daprClient = daprClient;
    }

    /// <summary>Gets the cached secret for the supplied key name, reading it from the DAPR secret store on a cache miss.</summary>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="apiSecretKeyName">The DAPR secret key name.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The secret value.</returns>
    public async Task<string> GetSecretAsync(string tenantId, string apiSecretKeyName, CancellationToken ct)
    {
        if (_cache.TryGetValue(apiSecretKeyName, out string? cachedKey))
        {
            return cachedKey;
        }

        try
        {
            Dictionary<string, string> secret = await _daprClient
                .GetSecretAsync(SecretStoreName, apiSecretKeyName, cancellationToken: ct)
                .ConfigureAwait(false);
            string apiKey = secret[apiSecretKeyName];
            _cache.TryAdd(apiSecretKeyName, apiKey);
            return apiKey;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new EmbeddingApiException(
                $"Failed to retrieve embedding credential secret from DAPR secret store '{SecretStoreName}'. " +
                "Ensure the DAPR sidecar is running and deploy/dapr/components/secretstore.yaml is configured.",
                tenantId,
                ex);
        }
    }

    /// <summary>Evicts the cached secret for the supplied key name so the next read comes from the DAPR secret store.</summary>
    /// <param name="apiSecretKeyName">The DAPR secret key name to evict.</param>
    public void Evict(string apiSecretKeyName) => _cache.TryRemove(apiSecretKeyName, out _);
}
