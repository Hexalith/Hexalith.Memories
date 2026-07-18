// <copyright file="AccessTelemetryInspectionResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Sanitized, bounded operations-only lifecycle inspection response.</summary>
public sealed record AccessTelemetryInspectionResponse
{
    /// <summary>Gets the lifecycle health state.</summary>
    public required AccessTelemetryHealthState Health { get; init; }

    /// <summary>Gets the bounded cause.</summary>
    public required AccessTelemetryReason Reason { get; init; }

    /// <summary>Gets the bounded retained record count.</summary>
    public required long RetainedRecordCount { get; init; }

    /// <summary>Gets the oldest unpurged expiry minute, if any.</summary>
    public long? OldestExpiryMinute { get; init; }

    /// <summary>Gets the last successful purge time.</summary>
    public long? LastPurgeUnixMilliseconds { get; init; }

    /// <summary>Gets the configuration epoch.</summary>
    public required string ConfigurationEpoch { get; init; }

    /// <summary>Gets whether physical reclamation evidence remains pending Story 27.3.</summary>
    public required bool PhysicalReclamationEvidencePending { get; init; }
}
