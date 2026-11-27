// <copyright file="DaprTokenEnvironmentCollection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

/// <summary>
/// Runs tests that temporarily change process-wide Dapr settings, including <c>APP_API_TOKEN</c>
/// and <c>DAPR_API_TOKEN</c>, apart from all other collections. Development test hosts can isolate
/// app-token checks from the process environment with an empty <c>Hexalith:Dapr:ApplicationTokenOverride</c>.
/// Qualification hosts that exercise token enforcement leave the override unset and use a scoped
/// <c>APP_API_TOKEN</c>. Production hosts enforce the process token and reject the override at startup.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DaprTokenEnvironmentCollection
{
    /// <summary>Collection name applied via <c>[Collection(DaprTokenEnvironmentCollection.Name)]</c>.</summary>
    public const string Name = "DaprTokenEnvironment";
}
