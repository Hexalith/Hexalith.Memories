// <copyright file="ReIngestedUnitInfo.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Per-unit outcome inside <see cref="BulkReIngestionResponse"/>.</summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="NewWorkflowInstanceId">The newly-scheduled workflow id when <paramref name="Outcome"/>=<c>"scheduled"</c>; otherwise null.</param>
/// <param name="Outcome">One of <c>"scheduled"</c>, <c>"not-found"</c>, <c>"conflict"</c>, <c>"error"</c>.</param>
/// <param name="ErrorMessage">Populated when <paramref name="Outcome"/>=<c>"error"</c>.</param>
public sealed record ReIngestedUnitInfo(
    string MemoryUnitId,
    string? NewWorkflowInstanceId,
    string Outcome,
    string? ErrorMessage);
