// <copyright file="HealthEndpointIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Health;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

/// <summary>Contains the nightly integration cases for HealthEndpointIntegrationTests.</summary>
public sealed partial class HealthEndpointIntegrationTests
{
    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task ReadyEndpoint_FalkorDbDown_ReturnsDegradedWithCapabilities()
    {
        try
        {
            using CancellationTokenSource stopCts = new(HealthTransitionTimeout);
            await _fixture.StopFalkorDbContainerAsync(stopCts.Token);

            JsonElement root = await WaitForEndpointAsync(
                "/ready",
                HttpStatusCode.OK,
                "Degraded");

            JsonElement falkor = root.GetProperty("entries").GetProperty("falkordb");
            falkor.GetProperty("status").GetString().ShouldBe("Degraded");
            falkor.GetProperty("affectedCapabilities").EnumerateArray()
                .Select(e => e.GetString()!)
                .ShouldContain("graph-traversal");
        }
        finally
        {
            _ = await _fixture.RestartTopologyAsync();
        }
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task AliveEndpoint_DaprSidecarDown_Returns503Unhealthy()
    {
        try
        {
            using CancellationTokenSource stopCts = new(HealthTransitionTimeout);
            await _fixture.StopDaprSidecarAsync(stopCts.Token);

            JsonElement root = await WaitForEndpointAsync(
                "/alive",
                HttpStatusCode.ServiceUnavailable,
                "Unhealthy");

            JsonElement sidecar = root.GetProperty("entries").GetProperty("dapr-sidecar");
            sidecar.GetProperty("status").GetString().ShouldBe("Unhealthy");
        }
        finally
        {
            _ = await _fixture.RestartTopologyAsync();
        }
    }
}
