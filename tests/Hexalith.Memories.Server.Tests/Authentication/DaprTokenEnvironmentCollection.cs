// <copyright file="DaprTokenEnvironmentCollection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

/// <summary>
/// Serializes tests that mutate process-wide <c>APP_API_TOKEN</c> / <c>DAPR_API_TOKEN</c>
/// so they cannot race each other. Development WebApplicationFactory hosts that overlay an empty
/// <c>Hexalith:Dapr:ApplicationTokenOverride</c> (the EventStore and health-check factories) ignore
/// process-wide <c>APP_API_TOKEN</c> and do not need this collection; factories without that overlay
/// still read it. Production hosts ignore the override and refuse to start while it is set.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DaprTokenEnvironmentCollection
{
    /// <summary>Collection name applied via <c>[Collection(DaprTokenEnvironmentCollection.Name)]</c>.</summary>
    public const string Name = "DaprTokenEnvironment";
}
