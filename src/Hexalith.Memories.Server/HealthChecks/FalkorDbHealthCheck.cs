// <copyright file="FalkorDbHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.HealthChecks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using StackExchange.Redis;

/// <summary>Instance-scoped FalkorDB readiness probe (Story 8.1). Uses the
/// keyed <see cref="IConnectionMultiplexer"/> <c>"falkordb"</c> — distinct from
/// the Redis Stack multiplexer — and executes <c>GRAPH.LIST</c>. Missing-graph
/// server responses are treated as Healthy (an empty instance is still healthy);
/// connectivity / server / driver failures map to the registration
/// <c>failureStatus</c> (expected <see cref="HealthStatus.Degraded"/>). Never throws.</summary>
public sealed class FalkorDbHealthCheck([FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb) : IHealthCheck
{
    private readonly IConnectionMultiplexer _falkorDb = falkorDb
        ?? throw new ArgumentNullException(nameof(falkorDb));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            IDatabase db = _falkorDb.GetDatabase();
            RedisResult result = await db.ExecuteAsync("GRAPH.LIST")
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is null || result.IsNull || result.Resp2Type != ResultType.Array)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    $"FalkorDB probe returned unexpected response type: {result?.Resp2Type}");
            }

            int graphCount = ((RedisResult[])result!).Length;

            return HealthCheckResult.Healthy(
                $"FalkorDB reachable; {graphCount} graphs.");
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "FalkorDB probe timed out.",
                exception: ex);
        }
        catch (RedisServerException ex) when (
            ex.Message.Contains("no such graph", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("unknown graph", StringComparison.OrdinalIgnoreCase))
        {
            // Empty instance still counts as healthy — the server responded.
            return HealthCheckResult.Healthy("FalkorDB reachable; 0 graphs.");
        }
        catch (RedisConnectionException ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"FalkorDB unreachable: {ex.GetType().Name}",
                exception: ex);
        }
        catch (RedisServerException ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"FalkorDB server error ({ex.GetType().Name}): {ex.Message}",
                exception: ex);
        }
        catch (Exception ex)
        {
            // NFalkorDB / protocol-parsing failures can surface as driver-level exceptions.
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"FalkorDB probe failed ({ex.GetType().Name}): {ex.Message}",
                exception: ex);
        }
    }
}
