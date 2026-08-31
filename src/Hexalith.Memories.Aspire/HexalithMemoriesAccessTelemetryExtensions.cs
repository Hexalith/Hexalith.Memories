// <copyright file="HexalithMemoriesAccessTelemetryExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.Memories.Aspire;

/// <summary>Composes the fixed, disabled-by-default access-telemetry lifecycle resources.</summary>
public static class HexalithMemoriesAccessTelemetryExtensions
{
    /// <summary>Adds the portable topology without certifying or enabling a Production adapter.</summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="server">The Memories server project resource that also references the access-telemetry secrets and configuration components.</param>
    /// <param name="stateStoreComponentPath">Path to the access-telemetry state-store component YAML (consumer-owned).</param>
    /// <param name="secretStore">
    /// The externally-provisioned DAPR secret-store component the access-telemetry lifecycle, clock, and server
    /// reference (for example an OpenBao-backed <c>secretstores.hashicorp.vault</c> component the consumer owns
    /// and seeds). This helper never creates its own secret-store component; the consumer is responsible for
    /// provisioning, scoping, and authenticating it.
    /// </param>
    /// <param name="configurationStoreComponentPath">Path to the access-telemetry configuration-store component YAML (consumer-owned).</param>
    /// <param name="daprConfigurationPath">Optional DAPR sidecar configuration path applied to the lifecycle and clock sidecars.</param>
    /// <param name="daprPlacementHostAddress">Optional DAPR placement service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <param name="daprSchedulerHostAddress">Optional DAPR scheduler service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <returns>The lifecycle, clock, server and component resource builders for further customization.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="server"/> or <paramref name="secretStore"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when a required path is <see langword="null"/> or whitespace.</exception>
    public static HexalithMemoriesAccessTelemetryResources AddHexalithMemoriesAccessTelemetry(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> server,
        string stateStoreComponentPath,
        IResourceBuilder<IDaprComponentResource> secretStore,
        string configurationStoreComponentPath,
        string? daprConfigurationPath = null,
        string? daprPlacementHostAddress = null,
        string? daprSchedulerHostAddress = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateStoreComponentPath);
        ArgumentNullException.ThrowIfNull(secretStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationStoreComponentPath);

        IResourceBuilder<IDaprComponentResource> stateStore = builder.AddDaprComponent(
            "access-telemetry-store",
            "state.redis",
            new DaprComponentOptions { LocalPath = stateStoreComponentPath });

        // The secret store is never created here -- it is externally provisioned and passed in by the consumer
        // (Story 29.2), e.g. an OpenBao-backed secretstores.hashicorp.vault component.
        IResourceBuilder<IDaprComponentResource> configurationStore = builder.AddDaprComponent(
            "access-telemetry-config",
            "configuration.redis",
            new DaprComponentOptions { LocalPath = configurationStoreComponentPath });

        IResourceBuilder<ProjectResource> clock = builder
            .AddProject<MemoriesAccessTelemetryClockProjectMetadata>(
                "memories-access-telemetry-clock",
                launchProfileName: "http")
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(CreateSidecarOptions(
                    "memories-access-telemetry-clock",
                    3800,
                    50301,
                    daprConfigurationPath,
                    daprPlacementHostAddress,
                    daprSchedulerHostAddress))
                .WithReference(secretStore))
            .WaitFor(secretStore);

        IResourceBuilder<ProjectResource> lifecycle = builder
            .AddProject<MemoriesAccessTelemetryProjectMetadata>(
                "memories-access-telemetry",
                launchProfileName: "http")
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(CreateSidecarOptions(
                    "memories-access-telemetry",
                    3700,
                    50201,
                    daprConfigurationPath,
                    daprPlacementHostAddress,
                    daprSchedulerHostAddress))
                .WithReference(stateStore)
                .WithReference(secretStore)
                .WithReference(configurationStore))
            .WithEnvironment("AccessTelemetryLifecycle__Enabled", "false")
            .WaitFor(stateStore)
            .WaitFor(secretStore)
            .WaitFor(configurationStore);

#pragma warning disable CS0618 // Dapr hosting reads project-level component references.
        clock = clock.WithReference(secretStore);
        lifecycle = lifecycle
            .WithReference(stateStore)
            .WithReference(secretStore)
            .WithReference(configurationStore);
        server = server
            .WithReference(secretStore)
            .WithReference(configurationStore)
            .WithEnvironment("AccessTelemetryLifecycle__Enabled", "false");
#pragma warning restore CS0618

        return new HexalithMemoriesAccessTelemetryResources(
            server,
            lifecycle,
            clock,
            stateStore,
            secretStore,
            configurationStore);
    }

    private static DaprSidecarOptions CreateSidecarOptions(
        string appId,
        int httpPort,
        int grpcPort,
        string? configPath,
        string? placementHostAddress,
        string? schedulerHostAddress)
        => new()
        {
            AppId = appId,
            DaprHttpPort = httpPort,
            DaprGrpcPort = grpcPort,
            Config = configPath,
            PlacementHostAddress = placementHostAddress,
            SchedulerHostAddress = schedulerHostAddress,
        };
}
