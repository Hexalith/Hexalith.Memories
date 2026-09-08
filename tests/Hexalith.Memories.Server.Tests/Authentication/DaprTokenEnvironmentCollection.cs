// <copyright file="DaprTokenEnvironmentCollection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

/// <summary>
/// Serializes tests that mutate process-wide <c>APP_API_TOKEN</c> / <c>DAPR_API_TOKEN</c>
/// so they cannot race each other. Host-level WebApplicationFactory tests overlay an empty
/// <c>APP_API_TOKEN</c> in configuration and therefore do not need this collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DaprTokenEnvironmentCollection
{
    /// <summary>Collection name applied via <c>[Collection(DaprTokenEnvironmentCollection.Name)]</c>.</summary>
    public const string Name = "DaprTokenEnvironment";
}
