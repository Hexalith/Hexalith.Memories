// <copyright file="ReadyEndpointAggregationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

/// <summary>
/// Story 8.1 AC #9 — Tier-1 in-memory end-to-end test of the <c>/ready</c>
/// aggregate. Substitutes one backend <see cref="IHealthCheck"/> with a fake
/// that returns <see cref="HealthStatus.Degraded"/>, hits the live endpoint
/// through the in-memory TestServer, and asserts: aggregate status
/// <c>Degraded</c>, HTTP <c>200 OK</c>, per-backend capability array, healthy
/// peers unaffected. This test is the definitive runtime guarantee that Risk #1's
/// mitigation (Degraded ≠ 503) actually works wire-to-wire — independent of the
/// Aspire fixture's CS0311 build issue from Story 5.6.
/// </summary>
public class ReadyEndpointAggregationTests
{
    [Fact]
    public async Task ReadyEndpoint_AllBackendsHealthy_Returns200Healthy()
    {
        using HealthCheckWebAppFactory factory = new(OverrideAll(HealthStatus.Healthy));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("Healthy");
        JsonElement entries = root.GetProperty("entries");
        foreach (string name in new[] { "redisearch", "redis-vector", "falkordb", "dapr-sidecar", "dapr-statestore" })
        {
            entries.GetProperty(name).GetProperty("status").GetString().ShouldBe("Healthy");
        }
    }

    [Fact]
    public async Task ReadyEndpoint_OneBackendDegraded_Returns200DegradedWithCapabilities()
    {
        using HealthCheckWebAppFactory factory = new(services =>
        {
            OverrideCheck(services, "redisearch", HealthStatus.Healthy);
            OverrideCheck(services, "redis-vector", HealthStatus.Healthy);
            OverrideCheck(services, "falkordb", HealthStatus.Degraded, "FalkorDB unreachable: simulated");
            OverrideCheck(services, "dapr-sidecar", HealthStatus.Healthy);
            OverrideCheck(services, "dapr-statestore", HealthStatus.Healthy);
        });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("Degraded");

        JsonElement falkor = root.GetProperty("entries").GetProperty("falkordb");
        falkor.GetProperty("status").GetString().ShouldBe("Degraded");
        string[] caps = [.. falkor.GetProperty("affectedCapabilities").EnumerateArray().Select(e => e.GetString()!)];
        caps.ShouldContain("graph-traversal");
        caps.ShouldContain("graph-scoped-search");

        root.GetProperty("entries").GetProperty("redisearch")
            .GetProperty("status").GetString().ShouldBe("Healthy");
        root.GetProperty("entries").GetProperty("redisearch")
            .GetProperty("affectedCapabilities").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task ReadyEndpoint_SidecarUnhealthy_Returns503Unhealthy()
    {
        using HealthCheckWebAppFactory factory = new(services =>
        {
            OverrideCheck(services, "redisearch", HealthStatus.Healthy);
            OverrideCheck(services, "redis-vector", HealthStatus.Healthy);
            OverrideCheck(services, "falkordb", HealthStatus.Healthy);
            OverrideCheck(services, "dapr-sidecar", HealthStatus.Unhealthy, "Dapr sidecar is not responsive.");
            OverrideCheck(services, "dapr-statestore", HealthStatus.Healthy);
        });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        JsonElement root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("Unhealthy");
        JsonElement sidecar = root.GetProperty("entries").GetProperty("dapr-sidecar");
        sidecar.GetProperty("affectedCapabilities").EnumerateArray()
            .Select(e => e.GetString()!)
            .ShouldContain("workflow-orchestration");
    }

    [Fact]
    public async Task AliveEndpoint_ExcludesBackendChecks()
    {
        using HealthCheckWebAppFactory factory = new(OverrideAll(HealthStatus.Healthy));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement root = await ReadJsonAsync(response);
        JsonElement entries = root.GetProperty("entries");

        // Only self + dapr-sidecar (live-tagged); backend checks are ready-only and must be absent.
        entries.TryGetProperty("self", out _).ShouldBeTrue();
        entries.TryGetProperty("dapr-sidecar", out _).ShouldBeTrue();
        entries.TryGetProperty("redisearch", out _).ShouldBeFalse();
        entries.TryGetProperty("redis-vector", out _).ShouldBeFalse();
        entries.TryGetProperty("falkordb", out _).ShouldBeFalse();
    }

    private static Action<IServiceCollection> OverrideAll(HealthStatus status) => services =>
    {
        OverrideCheck(services, "redisearch", status);
        OverrideCheck(services, "redis-vector", status);
        OverrideCheck(services, "falkordb", status);
        OverrideCheck(services, "dapr-sidecar", status);
        OverrideCheck(services, "dapr-statestore", status);
    };

    private static void OverrideCheck(IServiceCollection services, string name, HealthStatus status, string? description = null)
    {
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            for (int i = 0; i < options.Registrations.Count; i++)
            {
                HealthCheckRegistration existing = options.Registrations.ElementAt(i);
                if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
                {
                    continue;
                }

                HealthCheckRegistration replacement = new(
                    existing.Name,
                    _ => new StubHealthCheck(status, description ?? $"Stubbed {status}"),
                    existing.FailureStatus,
                    existing.Tags,
                    existing.Timeout);

                options.Registrations.Remove(existing);
                options.Registrations.Add(replacement);
                return;
            }
        });
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        JsonElement element = await response.Content.ReadFromJsonAsync<JsonElement>();
        return element;
    }

    private sealed class StubHealthCheck(HealthStatus status, string description) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(status switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(description),
                HealthStatus.Degraded => HealthCheckResult.Degraded(description),
                _ => HealthCheckResult.Unhealthy(description),
            });
        }
    }
}
