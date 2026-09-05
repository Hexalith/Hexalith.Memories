// <copyright file="EventStoreAppHostResourceGraphTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.EventStoreIntegration;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Memories.TestHelpers.Process;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Story 28.1 code-review finding: a typo'd Dapr AppId, a dropped port, or a dropped
/// <c>WithReference</c> on the <c>eventstore</c> resource would still compile and build green --
/// nothing previously inspected the actual Aspire resource graph. These tests build (never start)
/// the real <c>Hexalith.Memories.AppHost</c> distributed-application model and assert on the
/// <c>eventstore</c> resource's Dapr sidecar options and its <c>stateStore</c>/<c>pubSub</c>
/// references, plus the single-ownership invariant for the <c>statestore</c>/<c>pubsub</c>
/// components that <c>src/Hexalith.Memories.AppHost/Program.cs</c>'s boundaries exist to protect.
/// Building the model executes the AppHost's Program.cs top-level statements (same as
/// <see cref="Fixtures.AspireIngestionPipelineFixture"/>) but the test never calls
/// <c>StartAsync</c>, so no container/process/Dapr sidecar is actually launched -- this stays
/// fast and Docker-free, unlike the fixtures elsewhere in this project that do start the topology.
/// Placed in this project (not alongside <c>AppHostSecurityConfigurationTests</c> in
/// <c>Hexalith.Memories.Server.Tests</c>) because referencing the AppHost project from
/// <c>Hexalith.Memories.Server.Tests</c> makes the bare <c>Program</c> type ambiguous with
/// <c>Hexalith.Memories.Server</c>'s own <c>Program</c> (used throughout that project's
/// <c>WebApplicationFactory&lt;Program&gt;</c> tests) -- confirmed by attempting it (CS0433 across
/// six existing files). This project already references both projects successfully.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EventStoreAppHostResourceGraphTests
{
    private const string EventStoreResourceName = "eventstore";
    private const string StateStoreComponentName = "statestore";
    private const string PubSubComponentName = "pubsub";

    [Fact]
    public async Task EventStoreResource_HasExpectedDaprSidecarAndComponentReferences()
    {
        using EnvVarScope aspNetCoreEnvironment = EnvVarScope.Set("ASPNETCORE_ENVIRONMENT", "Development");
        using EnvVarScope dotNetEnvironment = EnvVarScope.Set("DOTNET_ENVIRONMENT", "Development");
        using EnvVarScope enableKeycloak = EnvVarScope.Set("EnableKeycloak", "false");
        using EnvVarScope randomizePorts = EnvVarScope.Set("MEMORIES_ASPIRE_RANDOMIZE_PROJECT_PORTS", "true");

        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>();
        await using DistributedApplication app = await builder.BuildAsync();

        DistributedApplicationModel model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Exactly one `eventstore` resource -- satisfies the "AppHost adds exactly one `eventstore`
        // resource" boundary and rules out AddHexalithEventStore(...) also having been called with a
        // different resource name for the gateway itself.
        IResource eventStoreResource = model.Resources
            .Where(r => r.Name == EventStoreResourceName)
            .ShouldHaveSingleItem();

        // Exactly one `statestore` and one `pubsub` Dapr component in the whole model. If
        // AddHexalithEventStore(...) had also been called (the "Never" boundary this story
        // forbids), it hardcodes components of these exact names -- Aspire would either reject
        // the duplicate resource name at build time (this test would then fail with a build
        // exception, not silently pass) or, were that ever relaxed, this count assertion would
        // catch the resulting double ownership directly.
        model.Resources.OfType<IDaprComponentResource>()
            .Count(r => r.Name == StateStoreComponentName)
            .ShouldBe(1, $"exactly one '{StateStoreComponentName}' Dapr component (single ownership)");
        model.Resources.OfType<IDaprComponentResource>()
            .Count(r => r.Name == PubSubComponentName)
            .ShouldBe(1, $"exactly one '{PubSubComponentName}' Dapr component (single ownership)");

        // The `eventstore` resource's Dapr sidecar: AppId/ports must match Program.cs exactly --
        // Memories Server reaches this resource via Dapr *service invocation* to app-id
        // "eventstore" (MemoriesServerServiceCollectionExtensions.cs), so a typo'd AppId here
        // would silently break that call path without any build-time signal.
        DaprSidecarAnnotation sidecarAnnotation = eventStoreResource.Annotations
            .OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem();
        IDaprSidecarResource sidecar = sidecarAnnotation.Sidecar;

        DaprSidecarOptionsAnnotation optionsAnnotation = sidecar.Annotations
            .OfType<DaprSidecarOptionsAnnotation>()
            .ShouldHaveSingleItem();
        optionsAnnotation.Options.AppId.ShouldBe("eventstore");
        optionsAnnotation.Options.DaprHttpPort.ShouldBe(3501);
        optionsAnnotation.Options.DaprGrpcPort.ShouldBe(50002);

        // Sidecar-level component references (set inside the WithDaprSidecar(sidecar => ...)
        // lambda in Program.cs).
        string[] sidecarComponentNames = [.. sidecar.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .Select(a => a.Component.Name)];
        sidecarComponentNames.ShouldContain(StateStoreComponentName);
        sidecarComponentNames.ShouldContain(PubSubComponentName);

        // Project-level component references (the CS0618-suppressed
        // `eventStoreGateway.WithReference(stateStore).WithReference(pubSub)` pattern mirrored
        // from the `memories` resource in Program.cs).
        string[] projectComponentNames = [.. eventStoreResource.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .Select(a => a.Component.Name)];
        projectComponentNames.ShouldContain(StateStoreComponentName);
        projectComponentNames.ShouldContain(PubSubComponentName);
    }

    /// <summary>
    /// Story 28.1 code-review finding (verification-gap layer): the resource-graph test above only
    /// builds the model -- it never proves the <c>eventstore</c> gateway project actually starts
    /// successfully. Every fixture-based integration test in this project waits only for
    /// <c>"memories"</c> to become healthy and forces
    /// <c>Memories:Testing:UseInMemoryCommandStore=true</c>, so none of them route through, or
    /// observe, the real <c>eventstore</c> resource either.
    ///
    /// <para><b>Running this test (skip removed) surfaced a real, previously-undetected defect,
    /// confirmed 2026-09-05:</b> <see cref="HexalithEventStorePlatformExtensions.AddHexalithEventStoreGatewayProject"/>
    /// always adds the gateway as a project resource (<c>builder.AddProject&lt;EventStoreProjectMetadata&gt;</c>),
    /// which Aspire always launches via <c>dotnet run</c> against the EventStore submodule's own
    /// project file -- regardless of Memories' own <c>UseHexalithProjectReferences</c> package/source
    /// mode. .NET SDK resolution walks up from that project's own directory and finds
    /// <c>references/Hexalith.EventStore/global.json</c> (pinned to SDK <c>10.0.302</c>,
    /// <c>rollForward: latestPatch</c>) before it ever reaches Memories' root <c>global.json</c>
    /// (SDK <c>10.0.400</c>). Any environment with only the mandated SDK 10.0.400 installed --
    /// which this repo requires everywhere else -- cannot launch the <c>eventstore</c> project at
    /// all: <c>dotnet run</c> fails with "Install the [10.0.302] .NET SDK or update [...] to match
    /// an installed SDK", and the resource never becomes healthy. The two obvious fixes are both
    /// out of this story's scope: installing SDK 10.0.302 alongside 10.0.400 is an environment
    /// change, and editing the submodule's <c>global.json</c> is forbidden by this spec's own
    /// "submodule content is not edited" boundary. Filed as deferred work (<c>DW-728</c>); skipped
    /// rather than left red so this defect is visible without breaking every run of this suite
    /// pending a human decision on the fix.</para>
    /// </summary>
    [Fact(Skip = @"28.1-DW-728: eventstore gateway project resource cannot start under SDK 10.0.400-only environments -- references/Hexalith.EventStore/global.json pins 10.0.302 and Aspire always launches the gateway via dotnet run against that project regardless of package/source mode. Owner: Administrator. Unskip when: SDK 10.0.302 is installed alongside 10.0.400 in the target environment, or an approved change to how the gateway project is launched removes the dependency on that submodule's own global.json.")]
    public async Task EventStoreResource_StartsAndBecomesHealthy()
    {
        using EnvVarScope aspNetCoreEnvironment = EnvVarScope.Set("ASPNETCORE_ENVIRONMENT", "Development");
        using EnvVarScope dotNetEnvironment = EnvVarScope.Set("DOTNET_ENVIRONMENT", "Development");
        using EnvVarScope enableKeycloak = EnvVarScope.Set("EnableKeycloak", "false");
        using EnvVarScope randomizePorts = EnvVarScope.Set("MEMORIES_ASPIRE_RANDOMIZE_PROJECT_PORTS", "true");

        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>();
        await using DistributedApplication app = await builder.BuildAsync();
        await app.StartAsync();

        using CancellationTokenSource healthyCts = new(TimeSpan.FromMinutes(3));
        _ = await app.ResourceNotifications
            .WaitForResourceHealthyAsync(EventStoreResourceName, healthyCts.Token);
    }
}
