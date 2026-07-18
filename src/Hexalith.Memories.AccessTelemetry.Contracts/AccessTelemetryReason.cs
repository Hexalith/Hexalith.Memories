// <copyright file="AccessTelemetryReason.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded lifecycle reason catalog.</summary>
public enum AccessTelemetryReason
{
    /// <summary>No failure.</summary>
    None,

    /// <summary>Configuration is invalid.</summary>
    ConfigurationInvalid,

    /// <summary>Remote validation has not completed.</summary>
    RemoteValidationPending,

    /// <summary>The bounded queue is full.</summary>
    QueueFull,

    /// <summary>The source event does not match the ratified schema.</summary>
    SchemaMismatch,

    /// <summary>The source event has expired.</summary>
    Expired,

    /// <summary>The record identifier conflicts with different content.</summary>
    RecordIdConflict,

    /// <summary>Clock evidence is stale or untrusted.</summary>
    ClockUntrusted,

    /// <summary>A dependency is temporarily unavailable.</summary>
    DependencyUnavailable,

    /// <summary>A capability probe did not pass.</summary>
    CapabilityUnproven,
}
