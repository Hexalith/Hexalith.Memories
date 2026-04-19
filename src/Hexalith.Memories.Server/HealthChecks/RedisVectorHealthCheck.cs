// <copyright file="RedisVectorHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.HealthChecks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using StackExchange.Redis;

/// <summary>Instance-scoped Redis Vector readiness probe (Story 8.1). Redis Stack
/// bundles RediSearch + Vector into the <c>search</c> module — this check executes
/// <c>MODULE LIST</c> against the shared <see cref="IConnectionMultiplexer"/> keyed
/// <c>"redis"</c> and verifies that module is loaded. Ambiguous / unparseable
/// responses are classified as Healthy with a clarifying description (Risk #3
/// mitigation); connectivity failures map to the registration
/// <c>failureStatus</c> (expected <see cref="HealthStatus.Degraded"/>).</summary>
public sealed class RedisVectorHealthCheck([FromKeyedServices("redis")] IConnectionMultiplexer redis) : IHealthCheck
{
    private const string RequiredModuleName = "search";

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
            RedisResult result = await db.ExecuteAsync("MODULE", "LIST")
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "Redis Vector probe returned no response.");
            }

            ModuleSearchOutcome outcome = FindSearchModule(result);
            return outcome switch
            {
                ModuleSearchOutcome.Found => HealthCheckResult.Healthy(
                    "Redis Vector capability reachable."),
                ModuleSearchOutcome.Absent => new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "Vector module absent from MODULE LIST response."),
                _ => HealthCheckResult.Healthy(
                    "Redis Vector reachable; module presence unverified."),
            };
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Redis Vector probe timed out.",
                exception: ex);
        }
        catch (RedisConnectionException ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Redis Vector unreachable: {ex.GetType().Name}",
                exception: ex);
        }
        catch (RedisServerException ex) when (
            ex.Message.Contains("LOADING", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("BUSY", StringComparison.OrdinalIgnoreCase))
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Redis Vector temporarily unavailable ({ex.GetType().Name}): {ex.Message}",
                exception: ex);
        }
        catch (RedisServerException ex) when (
            ex.Message.Contains("unknown command", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("ERR unknown", StringComparison.OrdinalIgnoreCase))
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Redis Vector module missing: {ex.Message}",
                exception: ex);
        }
        catch (RedisException ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Redis Vector probe failed ({ex.GetType().Name}): {ex.Message}",
                exception: ex);
        }
    }

    private enum ModuleSearchOutcome
    {
        Ambiguous,
        Absent,
        Found,
    }

    private static ModuleSearchOutcome FindSearchModule(RedisResult result)
    {
        if (result.IsNull || result.Resp2Type != ResultType.Array)
        {
            return ModuleSearchOutcome.Ambiguous;
        }

        RedisResult[] modules = (RedisResult[])result!;
        if (modules.Length == 0)
        {
            return ModuleSearchOutcome.Absent;
        }

        foreach (RedisResult module in modules)
        {
            if (module.IsNull || module.Resp2Type != ResultType.Array)
            {
                continue;
            }

            RedisResult[] kv = (RedisResult[])module!;

            // MODULE LIST returns an array of key/value pairs per module: [name <name> ver <ver> ...].
            for (int i = 0; i + 1 < kv.Length; i += 2)
            {
                string? key = (string?)kv[i];
                if (string.Equals(key, "name", StringComparison.OrdinalIgnoreCase))
                {
                    string? value = (string?)kv[i + 1];
                    if (string.Equals(value, RequiredModuleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return ModuleSearchOutcome.Found;
                    }
                }
            }
        }

        // We successfully parsed the array but did not find the "search" entry.
        return ModuleSearchOutcome.Absent;
    }
}
