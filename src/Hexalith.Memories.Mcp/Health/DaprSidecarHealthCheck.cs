// <copyright file="DaprSidecarHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Health;

using Dapr.Client;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Story 10.1 — mirrors <c>Hexalith.Memories.Server.HealthChecks.DaprSidecarHealthCheck</c>. The
/// MCP server cannot reference the Memories Server project (architecture boundary D6 / NFR11), so
/// this check is duplicated rather than imported. Same shape, same semantics.
/// </summary>
internal sealed class DaprSidecarHealthCheck : IHealthCheck
{
    private readonly DaprClient _daprClient;

    /// <summary>Initializes a new instance of the <see cref="DaprSidecarHealthCheck"/> class.</summary>
    /// <param name="daprClient">The DAPR client.</param>
    public DaprSidecarHealthCheck(DaprClient daprClient)
    {
        ArgumentNullException.ThrowIfNull(daprClient);
        _daprClient = daprClient;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            bool healthy = await _daprClient.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            return healthy
                ? HealthCheckResult.Healthy("DAPR sidecar is responsive.")
                : new HealthCheckResult(context.Registration.FailureStatus, "DAPR sidecar is not responsive.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"DAPR sidecar health check failed: {ex.GetType().Name}",
                exception: ex);
        }
    }
}
