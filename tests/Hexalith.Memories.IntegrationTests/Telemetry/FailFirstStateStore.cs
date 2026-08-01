// <copyright file="FailFirstStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Test state-store decorator that fails its first atomic write.</summary>
internal sealed class FailFirstStateStore(IAccessTelemetryStateStore inner) : IAccessTelemetryStateStore
{
    private bool _failed;

    /// <inheritdoc/>
    public Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
        AccessTelemetryRecord record,
        AccessTelemetryExpiryEntry expiryEntry,
        int ttlInSeconds,
        CancellationToken cancellationToken)
    {
        if (!_failed)
        {
            _failed = true;
            throw new InvalidOperationException("transient transaction failure");
        }

        return inner.WriteRecordAndIndexAsync(record, expiryEntry, ttlInSeconds, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Entries, bool HasMoreDueEntries)> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken)
        => inner.GetDueEntriesAsync(dueMinute, limit, cancellationToken);

    /// <inheritdoc/>
    public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
        => inner.DeleteAndVerifyAsync(entry, cancellationToken);
}
