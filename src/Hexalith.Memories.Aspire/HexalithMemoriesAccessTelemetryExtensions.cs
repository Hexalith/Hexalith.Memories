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
    public static HexalithMemoriesAccessTelemetryResources AddHexalithMemoriesAccessTelemetry(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> server,
        string stateStoreComponentPath,
        string secretStoreComponentPath,
        string configurationStoreComponentPath,
        string? daprConfigurationPath = null,
        string? daprPlacementHostAddress = null,
        string? daprSchedulerHostAddress = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateStoreComponentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretStoreComponentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationStoreComponentPath);

        IResourceBuilder<IDaprComponentResource> stateStore = builder.AddDaprComponent(
            "access-telemetry-store",
            "state.redis",
            new DaprComponentOptions { LocalPath = stateStoreComponentPath });
        IResourceBuilder<IDaprComponentResource> secretStore = builder.AddDaprComponent(
            "access-telemetry-secrets",
            "secretstores.local.file",
            new DaprComponentOptions { LocalPath = secretStoreComponentPath });
        IResourceBuilder<IDaprComponentResource> configurationStore = builder.AddDaprComponent(
            "access-telemetry-config",
            "configuration.redis",
            new DaprComponentOptions { LocalPath = configurationStoreComponentPath });

        IResourceBuilder<ProjectResource> clock = builder
            .AddProject<MemoriesAccessTelemetryClockProjectMetadata>(
                "memories-access-telemetry-clock",
                launchProfileName: "http")
            .WithDaprSidecar(sidecar => sidecar.WithOptions(CreateSidecarOptions(
                "memories-access-telemetry-clock",
                3800,
                50301,
                daprConfigurationPath,
                daprPlacementHostAddress,
                daprSchedulerHostAddress)));

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
