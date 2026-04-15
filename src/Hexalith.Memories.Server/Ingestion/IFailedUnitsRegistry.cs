// <copyright file="IFailedUnitsRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Internal abstraction over the failed-units registry to keep re-ingestion orchestration testable.</summary>
internal interface IFailedUnitsRegistry
{
    Task<FailedUnitsPage> ListAsync(string tenantId, string caseId, int limit, int offset, CancellationToken cancellationToken);

    Task<FailedUnitRecord?> GetAsync(string tenantId, string memoryUnitId, CancellationToken cancellationToken);

    Task<FailedUnitSummary?> GetSummaryAsync(string tenantId, string memoryUnitId, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(string tenantId, string caseId, string memoryUnitId, string sourceUri, CancellationToken cancellationToken);

    Task RestoreAsync(FailedUnitRecord record, CancellationToken cancellationToken);
}