// <copyright file="RediSearchHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.HealthChecks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using StackExchange.Redis;

/// <summary>Instance-scoped RediSearch readiness probe (Story 8.1). Executes
/// <c>FT._LIST</c> against the shared <see cref="IConnectionMultiplexer"/> keyed
/// <c>"redis"</c>; reports <see cref="HealthStatus.Healthy"/> when the module
/// responds and the configured <c>failureStatus</c> (expected
/// <see cref="HealthStatus.Degraded"/>) on connectivity or server failure. Never throws.</summary>
public sealed class RediSearchHealthCheck([FromKeyedServices("redis")] IConnectionMultiplexer redis) : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis = redis
        ?? throw new ArgumentNullException(nameof(redis));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            IDatabase db = _redis.GetDatabase();
            RedisResult result = await db.ExecuteAsync("FT._LIST")
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is null || result.IsNull || result.Resp2Type != ResultType.Array)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    $"RediSearch probe returned unexpected response type: {result?.Resp2Type}");
            }

            int indexCount = ((RedisResult[])result!).Length;

            return HealthCheckResult.Healthy(
                $"RediSearch module reachable; {indexCount} indexes loaded.");
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "RediSearch probe timed out.",
                exception: ex);
        }
        catch (RedisConnectionException ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"RediSearch unreachable: {ex.GetType().Name}",
                exception: ex);
        }
        catch (RedisServerException ex) when (
            ex.Message.Contains("LOADING", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("BUSY", StringComparison.OrdinalIgnoreCase))
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"RediSearch temporarily unavailable ({ex.GetType().Name}): {ex.Message}",
                exception: ex);
        }
        catch (RedisServerException ex) when (
            ex.Message.Contains("unknown command", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("ERR unknown", StringComparison.OrdinalIgnoreCase))
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"RediSearch module missing: {ex.Message}",
                exception: ex);
        }
        catch (RedisException ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"RediSearch probe failed ({ex.GetType().Name}): {ex.Message}",
                exception: ex);
        }
    }
}
