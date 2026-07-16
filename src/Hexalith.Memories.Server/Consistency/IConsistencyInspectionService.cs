// <copyright file="IConsistencyInspectionService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Testability seam over <see cref="ConsistencyInspectionService"/>. Consumers that
/// need to mock the probe (<c>RepairUnitActivity</c>, the repair workflow tests) depend
/// on this interface; production DI registers the concrete class as
/// <see cref="IConsistencyInspectionService"/>.
/// </summary>
public interface IConsistencyInspectionService
{
    /// <inheritdoc cref="ConsistencyInspectionService.InspectAsync"/>
    Task<ConsistencyInspectionResult> InspectAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken ct);
}
