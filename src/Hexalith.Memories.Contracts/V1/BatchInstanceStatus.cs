// <copyright file="BatchInstanceStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Per-workflow-instance status row inside a batch status response.</summary>
/// <param name="InstanceId">Workflow instance identifier.</param>
/// <param name="Status">User-facing status (mirrors MemoryUnitStatus values, lowercase).</param>
/// <param name="MemoryUnitId">Resolved memory unit id once indexed, null otherwise.</param>
/// <param name="SourceUri">The URI/path that was ingested.</param>
public sealed record BatchInstanceStatus(
    string InstanceId,
    string Status,
    string? MemoryUnitId,
    string SourceUri);
