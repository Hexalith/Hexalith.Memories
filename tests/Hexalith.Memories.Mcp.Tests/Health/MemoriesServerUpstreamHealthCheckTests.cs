// <copyright file="MemoriesServerUpstreamHealthCheckTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests.Health;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Mcp.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 26.1 — guards the immediate fail-closed behavior of the MCP-to-Server upstream health check.
/// The former 3-strike degraded window was removed so MCP leaves rotation the moment its upstream is
/// unavailable; a regression reintroducing a degraded window would keep MCP serving without its upstream.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MemoriesServerUpstreamHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenUpstreamProbeFails_IsImmediatelyUnhealthyNotDegraded()
    {
        // No MemoriesClient is resolvable, so ProbeHealthAsync resolution throws and is treated as a
        // failed probe -- exactly the single-failure case that must map straight to Unhealthy.
        IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
        IServiceScope scope = Substitute.For<IServiceScope>();
        IServiceProvider provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(MemoriesClient)).Returns((object?)null);

        var check = new MemoriesServerUpstreamHealthCheck(scopeFactory);

        HealthCheckResult result = await check.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Status.ShouldNotBe(HealthStatus.Degraded);
        result.Data["upstream"].ShouldBe(MemoriesServerUpstreamHealthCheck.UpstreamIdentifier);
    }
}
