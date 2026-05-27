// <copyright file="DaprSidecarHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.HealthChecks;

using Dapr.Client;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class DaprSidecarHealthCheck(DaprClient daprClient) : IHealthCheck
{
    private readonly DaprClient _daprClient = daprClient
        ?? throw new ArgumentNullException(nameof(daprClient));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            bool isHealthy = await _daprClient.CheckHealthAsync(cancellationToken)
                .ConfigureAwait(false);

            return isHealthy
                ? HealthCheckResult.Healthy("Dapr sidecar is responsive.")
                : new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "Dapr sidecar is not responsive.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Dapr sidecar health check failed: {ex.GetType().Name}",
                exception: ex);
        }
    }
}
