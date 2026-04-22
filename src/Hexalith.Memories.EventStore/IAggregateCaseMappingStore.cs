// <copyright file="IAggregateCaseMappingStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Shared storage contract for the tenant+aggregateType → caseId routing map. A Redis-backed
/// implementation lets multiple server instances and post-restart processes converge on the same case ids
/// instead of relying on process-local memory.</summary>
public interface IAggregateCaseMappingStore
{
    Task<string?> GetCaseIdAsync(string tenantId, string aggregateType, CancellationToken cancellationToken);

    Task<long> GetAggregateCountAsync(string tenantId, CancellationToken cancellationToken);

    Task<bool> TryAcquireCreationLockAsync(string tenantId, string aggregateType, TimeSpan leaseTtl, CancellationToken cancellationToken);

    Task ReleaseCreationLockAsync(string tenantId, string aggregateType, CancellationToken cancellationToken);

    Task<bool> TryStoreCaseIdAsync(string tenantId, string aggregateType, string caseId, CancellationToken cancellationToken);
}
