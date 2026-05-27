// <copyright file="MemoriesServerUpstreamHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Health;

using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Story 10.1 AC #7 — rolling-window 3-strike health check that probes the upstream Memories Server
/// via the existing <see cref="MemoriesClient.ProbeHealthAsync"/> 5-second timeout. Single transient
/// failures degrade the row to <see cref="HealthStatus.Degraded"/> so the MCP Aspire Dashboard
/// indicator does not flap; sustained failure (3 consecutive misses) escalates to
/// <see cref="HealthStatus.Unhealthy"/> with a diagnostic data entry pointing at the failing
/// upstream.
/// </summary>
internal sealed class MemoriesServerUpstreamHealthCheck : IHealthCheck
{
    /// <summary>Number of consecutive failed probes before the check returns Unhealthy.</summary>
    internal const int FailureStrikeThreshold = 3;

    /// <summary>The upstream service identifier surfaced via <c>data["upstream"]</c>.</summary>
    internal const string UpstreamIdentifier = "memories-server";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Lock _lock = new();
    private int _consecutiveFailures;

    /// <summary>Initializes a new instance of the <see cref="MemoriesServerUpstreamHealthCheck"/> class.</summary>
    /// <param name="scopeFactory">The scope factory used to resolve the scoped Memories REST client.</param>
    public MemoriesServerUpstreamHealthCheck(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool healthy;
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            MemoriesClient client = scope.ServiceProvider.GetRequiredService<MemoriesClient>();
            healthy = await client.ProbeHealthAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            healthy = false;
        }

        if (healthy)
        {
            ResetFailures();
            return HealthCheckResult.Healthy("Memories Server upstream is responsive.");
        }

        int strikes = IncrementFailures();
        var data = new Dictionary<string, object> { ["upstream"] = UpstreamIdentifier };

        return strikes < FailureStrikeThreshold
            ? new HealthCheckResult(
                HealthStatus.Degraded,
                $"Memories Server upstream missed {strikes} consecutive probe(s) (threshold: {FailureStrikeThreshold}).",
                data: data)
            : new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"Memories Server upstream missed {strikes} consecutive probes (threshold: {FailureStrikeThreshold}).",
                data: data);
    }

    /// <summary>Test-only — reports the current consecutive-failure counter.</summary>
    internal int CurrentFailureCount
    {
        get
        {
            lock (_lock)
            {
                return _consecutiveFailures;
            }
        }
    }

    private void ResetFailures()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
        }
    }

    private int IncrementFailures()
    {
        lock (_lock)
        {
            return ++_consecutiveFailures;
        }
    }
}
