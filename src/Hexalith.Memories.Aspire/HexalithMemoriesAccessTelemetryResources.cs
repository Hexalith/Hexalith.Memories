// <copyright file="HexalithMemoriesAccessTelemetryResources.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.Memories.Aspire;

/// <summary>Resources created for the portable, fail-closed access-telemetry topology.</summary>
public sealed record HexalithMemoriesAccessTelemetryResources(
    IResourceBuilder<ProjectResource> Server,
    IResourceBuilder<ProjectResource> Lifecycle,
    IResourceBuilder<ProjectResource> Clock,
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> SecretStore,
    IResourceBuilder<IDaprComponentResource> ConfigurationStore);
