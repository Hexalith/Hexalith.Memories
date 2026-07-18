// <copyright file="AccessTelemetryAuthority.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Separated access-telemetry authorities.</summary>
internal enum AccessTelemetryAuthority
{
    /// <summary>Server write/heartbeat authority.</summary>
    ServerWriter,

    /// <summary>Fixed actor/state mutation authority.</summary>
    LifecycleService,

    /// <summary>Independent time-signing authority.</summary>
    Clock,

    /// <summary>Sanitized operations-only read authority.</summary>
    Inspector,

    /// <summary>External adapter evidence authority.</summary>
    AdapterEvidence,
}
