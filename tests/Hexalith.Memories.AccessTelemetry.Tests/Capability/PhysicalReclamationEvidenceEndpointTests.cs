// <copyright file="PhysicalReclamationEvidenceEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Capability;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;
using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

/// <summary>Verifies the Dapr-authenticated adapter-only physical-evidence ingress.</summary>
[Collection(PhysicalEvidenceEndpointTestCollection.Name)]
public sealed class PhysicalReclamationEvidenceEndpointTests
{
    private const string AppToken = "physical-evidence-test-app-token";

    [Fact]
    public async Task PhysicalEvidence_WithAuthenticatedDaprRequest_ReachesGlobalLifecycleActor()
    {
        IAccessTelemetryLifecycleActor actor = Substitute.For<IAccessTelemetryLifecycleActor>();
        IActorProxyFactory proxies = Substitute.For<IActorProxyFactory>();
        proxies.CreateActorProxy<IAccessTelemetryLifecycleActor>(
                Arg.Any<ActorId>(),
                Arg.Any<string>())
            .Returns(actor);
        string? previous = Environment.GetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable);
        Environment.SetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
            AppToken);
        try
        {
            await using var factory = new AccessTelemetryWebAppFactory(proxies);
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Add(DaprApplicationTokenMiddleware.DaprApiTokenHeader, AppToken);
            AccessTelemetryPhysicalReclamationEvidence evidence = Evidence();

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/v1/access-telemetry/physical-reclamation-evidence",
                evidence,
                TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            using JsonDocument receipt = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);
            JsonElement root = receipt.RootElement;
            root.EnumerateObject().Count().ShouldBe(5);
            root.GetProperty("status").GetString().ShouldBe("accepted");
            root.GetProperty("evidenceId").GetString().ShouldBe(evidence.EvidenceId);
            root.GetProperty("componentProfileHash").GetString().ShouldBe(evidence.ComponentProfileHash);
            root.GetProperty("artifactSha256").GetString().ShouldBe(evidence.ArtifactSha256);
            root.GetProperty("observedAtUnixMilliseconds").GetInt64().ShouldBe(evidence.ObservedAtUnixMilliseconds);
            await actor.Received(1).RecordPhysicalReclamationEvidenceAsync(evidence);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                previous);
        }
    }

    [Fact]
    public async Task PhysicalEvidence_WithoutAuthenticatedDaprRequest_IsRejectedBeforeActorAccess()
    {
        IAccessTelemetryLifecycleActor actor = Substitute.For<IAccessTelemetryLifecycleActor>();
        IActorProxyFactory proxies = Substitute.For<IActorProxyFactory>();
        proxies.CreateActorProxy<IAccessTelemetryLifecycleActor>(
                Arg.Any<ActorId>(),
                Arg.Any<string>())
            .Returns(actor);
        string? previous = Environment.GetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable);
        Environment.SetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
            AppToken);
        try
        {
            await using var factory = new AccessTelemetryWebAppFactory(proxies);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/v1/access-telemetry/physical-reclamation-evidence",
                Evidence(),
                TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await actor.DidNotReceiveWithAnyArgs().RecordPhysicalReclamationEvidenceAsync(default!);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                previous);
        }
    }

    [Fact]
    public void DaprPolicy_GrantsPhysicalEvidenceOnlyToAdapterIdentity()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        string policy = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml"));
        string adapterPolicy = policy[(policy.IndexOf("- appId: access-telemetry-adapter", StringComparison.Ordinal))..];

        policy.ShouldContain("defaultAction: deny", Case.Sensitive);
        adapterPolicy.ShouldContain("namespace: hexalith-memories-qualification", Case.Sensitive);
        adapterPolicy.ShouldContain("- name: /v1/access-telemetry/physical-reclamation-evidence", Case.Sensitive);
        adapterPolicy.ShouldContain("httpVerb: [\"POST\"]", Case.Sensitive);
        policy[..policy.IndexOf("- appId: access-telemetry-adapter", StringComparison.Ordinal)]
            .ShouldNotContain("/v1/access-telemetry/physical-reclamation-evidence", Case.Sensitive);
    }

    private static AccessTelemetryPhysicalReclamationEvidence Evidence()
        => new()
        {
            EvidenceId = "physical-evidence-27-4",
            ComponentProfileHash = new string('a', 64),
            ArtifactSha256 = new string('b', 64),
            ObservedAtUnixMilliseconds = 1_785_227_200_000,
        };

    private sealed class AccessTelemetryWebAppFactory(IActorProxyFactory proxies)
        : WebApplicationFactory<AccessTelemetryService>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IActorProxyFactory>();
                services.AddSingleton(proxies);
            });
        }
    }
}

/// <summary>Serializes tests that mutate the process-wide Dapr application token.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PhysicalEvidenceEndpointTestCollection
{
    /// <summary>Collection name.</summary>
    public const string Name = "PhysicalReclamationEvidenceEndpoint";
}
