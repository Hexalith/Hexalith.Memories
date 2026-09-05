// <copyright file="IAccessTelemetryLifecycleActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Dapr.Actors;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Fixed-ID serialized lifecycle mutation authority.</summary>
public interface IAccessTelemetryLifecycleActor : IActor
{
    /// <summary>Writes one bounded batch.</summary>
    Task<AccessTelemetryWriteBatchResponse> WriteBatchAsync(AccessTelemetryWriteBatchRequest request);

    /// <summary>Records one bounded writer heartbeat.</summary>
    Task<WriterHeartbeatResponse> HeartbeatAsync(WriterHeartbeatRequest request);

    /// <summary>Executes one bounded purge turn.</summary>
    Task PurgeAsync();

    /// <summary>Returns sanitized operations-only lifecycle evidence.</summary>
    Task<AccessTelemetryInspectionResponse> InspectAsync();

    /// <summary>Records verified physical-reclamation evidence from the adapter authority.</summary>
    Task RecordPhysicalReclamationEvidenceAsync(AccessTelemetryPhysicalReclamationEvidence evidence);

    /// <summary>Stages a new marker-key generation against the live writer snapshot.</summary>
    Task StageMarkerKeyAsync(string newGeneration);

    /// <summary>Acknowledges that one live writer loaded the staged generation.</summary>
    Task AcknowledgeMarkerKeyAsync(WriterHeartbeat heartbeat);

    /// <summary>Attempts to drain and activate the staged generation.</summary>
    Task<bool> TryActivateMarkerKeyAsync(long finalOldKeyWriteUnixMilliseconds);
}
