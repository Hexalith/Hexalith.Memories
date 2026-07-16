// <copyright file="DaprStateStoreHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.HealthChecks;

using Dapr.Client;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Health check that verifies the Dapr state store is accessible.</summary>
public sealed class DaprStateStoreHealthCheck(DaprClient daprClient, string storeName) : IHealthCheck
{
    private const string ProbeKey = "__health_probe__";
    private readonly DaprClient _daprClient = daprClient
        ?? throw new ArgumentNullException(nameof(daprClient));
    private readonly string _storeName = storeName
        ?? throw new ArgumentNullException(nameof(storeName));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            _ = await _daprClient.GetStateAsync<byte[]>(
                _storeName,
                ProbeKey,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy($"Dapr state store '{_storeName}' is accessible.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Dapr state store '{_storeName}' is not accessible: {ex.GetType().Name}",
                exception: ex);
        }
    }
}
