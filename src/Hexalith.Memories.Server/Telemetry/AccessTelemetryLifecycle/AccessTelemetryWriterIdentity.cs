// <copyright file="AccessTelemetryWriterIdentity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Process-local identities used for uniqueness and leases, never metric labels.</summary>
internal sealed class AccessTelemetryWriterIdentity
{
    /// <summary>Initializes fresh identities for one Server process.</summary>
    public AccessTelemetryWriterIdentity(MonotonicRecordIdGenerator recordIds)
    {
        ArgumentNullException.ThrowIfNull(recordIds);
        ServiceInstanceId = recordIds.NewId();
        ProcessEpoch = recordIds.NewId();
    }

    /// <summary>Gets the service-instance ULID.</summary>
    public string ServiceInstanceId { get; }

    /// <summary>Gets the process-epoch ULID.</summary>
    public string ProcessEpoch { get; }
}
