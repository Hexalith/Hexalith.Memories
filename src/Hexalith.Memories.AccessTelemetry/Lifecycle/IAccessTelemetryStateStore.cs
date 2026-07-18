// <copyright file="IAccessTelemetryStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Dapr-only strong-state boundary owned exclusively by the fixed actor.</summary>
internal interface IAccessTelemetryStateStore
{
    /// <summary>Atomically writes one record and its minute/shard index.</summary>
    Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
        AccessTelemetryRecord record,
        AccessTelemetryExpiryEntry expiryEntry,
        int ttlInSeconds,
        CancellationToken cancellationToken);

    /// <summary>Reads at most the bounded number of due expiry entries.</summary>
    Task<IReadOnlyList<AccessTelemetryExpiryEntry>> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>Deletes a record, verifies strong absence, and removes its expiry entry.</summary>
    Task<bool> DeleteAndVerifyAsync(AccessTelemetryExpiryEntry entry, CancellationToken cancellationToken);
}
