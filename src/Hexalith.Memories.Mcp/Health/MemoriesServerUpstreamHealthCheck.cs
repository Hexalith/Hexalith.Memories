// <copyright file="MemoriesServerUpstreamHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Health;

using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Probes the upstream Memories Server through the ACL-compatible DAPR health operation. Any failure is
/// immediately unhealthy because MCP cannot serve safely without its upstream.
/// </summary>
internal sealed class MemoriesServerUpstreamHealthCheck : IHealthCheck
{
    /// <summary>The upstream service identifier surfaced via <c>data["upstream"]</c>.</summary>
    internal const string UpstreamIdentifier = "memories";

    private readonly IServiceScopeFactory _scopeFactory;

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
            return HealthCheckResult.Healthy("Memories Server upstream is responsive.");
        }

        var data = new Dictionary<string, object> { ["upstream"] = UpstreamIdentifier };
        return new HealthCheckResult(
            HealthStatus.Unhealthy,
            "Memories Server upstream is unavailable.",
            data: data);
    }
}
