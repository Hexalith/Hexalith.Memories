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

/// <summary>Contains the fast integration cases for HealthEndpointIntegrationTests.</summary>
public sealed partial class HealthEndpointIntegrationTests
{
    [Fact]
    public async Task ReadyEndpoint_AllHealthy_Returns200WithFiveEntries()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        JsonElement root = await response.Content.ReadFromJsonAsync<JsonElement>();
        root.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        root.GetProperty("status").GetString().ShouldBe("Healthy");

        JsonElement entries = root.GetProperty("entries");
        foreach (string name in new[] { "dapr-sidecar", "dapr-statestore", "redisearch", "redis-vector", "falkordb" })
        {
            entries.GetProperty(name).GetProperty("status").GetString().ShouldBe("Healthy");
        }
    }

    [Fact]
    public async Task AliveEndpoint_Default_Returns200WithSidecarAndSelfOnly()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement root = await response.Content.ReadFromJsonAsync<JsonElement>();
        root.GetProperty("status").GetString().ShouldBe("Healthy");

        JsonElement entries = root.GetProperty("entries");
        entries.TryGetProperty("self", out _).ShouldBeTrue();
        entries.TryGetProperty("dapr-sidecar", out _).ShouldBeTrue();
        entries.TryGetProperty("redisearch", out _).ShouldBeFalse();
        entries.TryGetProperty("redis-vector", out _).ShouldBeFalse();
        entries.TryGetProperty("falkordb", out _).ShouldBeFalse();
    }
}
