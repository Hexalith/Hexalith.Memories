// <copyright file="LifecycleProcessBoundaryDeliveryClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>Test client that models one independent writer context crossing the delivery seam.</summary>
internal sealed class LifecycleProcessBoundaryDeliveryClient(
    string boundaryId,
    AccessTelemetryLifecycleProcessor processor) : IAccessTelemetryDeliveryClient
{
    private IReadOnlyList<AccessTelemetryRecord> _receivedRecords = [];

    /// <summary>Gets the test writer-context identity.</summary>
    public string BoundaryId { get; } = boundaryId;

    /// <summary>Gets the immutable record batch observed at this delivery boundary.</summary>
    public IReadOnlyList<AccessTelemetryRecord> ReceivedRecords => Volatile.Read(ref _receivedRecords);

    /// <inheritdoc/>
    public async Task<AccessTelemetryWriteBatchResponse> SendAsync(
        IReadOnlyList<AccessTelemetryRecord> records,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);
        Volatile.Write(ref _receivedRecords, records.ToArray());

        int accepted = 0;
        foreach (AccessTelemetryRecord record in records)
        {
            AccessTelemetryPersistenceResult result = await processor.PersistAsync(record, cancellationToken).ConfigureAwait(false);
            if (result.Status is AccessTelemetryPersistenceStatus.Inserted or AccessTelemetryPersistenceStatus.Idempotent)
            {
                accepted++;
            }
        }

        return new AccessTelemetryWriteBatchResponse
        {
            Accepted = accepted,
            Rejected = records.Count - accepted,
            Reason = accepted == records.Count ? AccessTelemetryReason.None : AccessTelemetryReason.DependencyUnavailable,
        };
    }
}
