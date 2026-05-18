// <copyright file="ProgramHealthCheckRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 8.1 Task 4.4 + AC #9 — asserts the health-check registrations match the
/// contract documented in AC #4 / AC #8: five registrations, the Dapr sidecar carries
/// both <c>live</c> and <c>ready</c> tags, the three backend checks use
/// <see cref="HealthStatus.Degraded"/> as their failure status, and Dapr checks keep
/// <see cref="HealthStatus.Unhealthy"/> (so orchestrators pull the pod on sidecar loss).
/// </summary>
public class ProgramHealthCheckRegistrationTests
{
    [Fact]
    public void AddHealthChecks_RegistersExpectedNames()
    {
        using HealthCheckWebAppFactory factory = new();
        _ = factory.Server;
        HealthCheckServiceOptions options = factory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        string[] registeredNames = [.. options.Registrations.Select(r => r.Name)];

        registeredNames.ShouldContain("self");
        registeredNames.ShouldContain("redis-ping");
        registeredNames.ShouldContain("dapr-sidecar");
        registeredNames.ShouldContain("dapr-statestore");
        registeredNames.ShouldContain("redisearch");
        registeredNames.ShouldContain("redis-vector");
        registeredNames.ShouldContain("falkordb");
    }

    [Fact]
    public void DaprSidecarRegistration_HasLiveAndReadyTags()
    {
        HealthCheckRegistration registration = GetRegistration("dapr-sidecar");

        registration.Tags.ShouldContain("live");
        registration.Tags.ShouldContain("ready");
        registration.FailureStatus.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public void DaprStateStoreRegistration_IsReadyOnlyAndUnhealthyOnFail()
    {
        HealthCheckRegistration registration = GetRegistration("dapr-statestore");

        registration.Tags.ShouldContain("ready");
        registration.Tags.ShouldNotContain("live");
        registration.FailureStatus.ShouldBe(HealthStatus.Unhealthy);
    }

    [Theory]
    [InlineData("redisearch")]
    [InlineData("redis-vector")]
    [InlineData("falkordb")]
    public void BackendRegistration_TaggedReadyAndFailsDegraded(string name)
    {
        HealthCheckRegistration registration = GetRegistration(name);

        registration.Tags.ShouldContain("ready");
        registration.Tags.ShouldNotContain("live");
        registration.FailureStatus.ShouldBe(
            HealthStatus.Degraded,
            $"Backend check '{name}' must fail as Degraded so /ready aggregates to 200 OK (Risk #1 mitigation).");
        registration.Timeout.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void SelfRegistration_IsLiveOnly()
    {
        HealthCheckRegistration registration = GetRegistration("self");

        registration.Tags.ShouldContain("live");
        registration.Tags.ShouldNotContain("ready");
    }

    [Fact]
    public async Task ReadyEndpoint_BackendDegraded_StillMapsTo200Ok()
    {
        using HealthCheckWebAppFactory factory = new(services =>
        {
            OverrideCheck(services, "redisearch", HealthStatus.Healthy, "RediSearch reachable.");
            OverrideCheck(services, "redis-vector", HealthStatus.Healthy, "Redis Vector reachable.");
            OverrideCheck(services, "falkordb", HealthStatus.Degraded, "FalkorDB unreachable: simulated");
            OverrideCheck(services, "dapr-sidecar", HealthStatus.Healthy, "Dapr sidecar is responsive.");
            OverrideCheck(services, "dapr-statestore", HealthStatus.Healthy, "Dapr state store is accessible.");
        });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement root = await response.Content.ReadFromJsonAsync<JsonElement>();
        root.GetProperty("status").GetString().ShouldBe("Degraded");
    }

    private static HealthCheckRegistration GetRegistration(string name)
    {
        using HealthCheckWebAppFactory factory = new();
        _ = factory.Server;
        HealthCheckServiceOptions options = factory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        return options.Registrations.SingleOrDefault(r => r.Name == name)
            ?? throw new Xunit.Sdk.XunitException(
                $"Expected registration '{name}' was not found. Got: {string.Join(", ", options.Registrations.Select(r => r.Name))}");
    }

    private static void OverrideCheck(IServiceCollection services, string name, HealthStatus status, string description)
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
                    _ => new StubHealthCheck(status, description),
                    existing.FailureStatus,
                    existing.Tags,
                    existing.Timeout);

                options.Registrations.Remove(existing);
                options.Registrations.Add(replacement);
                return;
            }
        });
    }

    private sealed class StubHealthCheck(HealthStatus status, string description) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(status switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(description),
                HealthStatus.Degraded => HealthCheckResult.Degraded(description),
                _ => HealthCheckResult.Unhealthy(description),
            });
    }
}
