// <copyright file="EmbeddingMigrationMarker.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Durable tenant-scoped embedding migration marker visible to runtime ingestion guards.</summary>
/// <param name="TenantId">The tenant currently under embedding vector migration.</param>
/// <param name="TargetProvider">The provider that semantic vector writes must use while the marker is active.</param>
/// <param name="TargetModel">The model that semantic vector writes must use while the marker is active.</param>
/// <param name="TargetDimensions">The dimensions that semantic vector writes must use while the marker is active.</param>
/// <param name="Status">The durable marker status.</param>
public sealed record EmbeddingMigrationMarker(
    string TenantId,
    string TargetProvider,
    string TargetModel,
    int TargetDimensions,
    string Status)
{
    /// <summary>Gets a value indicating whether the marker still protects runtime writes.</summary>
    public bool IsActive
        => string.Equals(Status, "started", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Status, "resumed", StringComparison.OrdinalIgnoreCase);
}
