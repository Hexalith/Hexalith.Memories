// <copyright file="IGraphNodeMerger.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

/// <summary>
/// Testability seam over <see cref="GraphNodeMerger"/>. Injected into
/// <c>RepairUnitActivity</c>; mocked in unit tests.
/// </summary>
public interface IGraphNodeMerger
{
    /// <inheritdoc cref="GraphNodeMerger.ReMergeFromSyntacticAsync"/>
    Task ReMergeFromSyntacticAsync(string tenantId, string memoryUnitId, CancellationToken ct);
}
