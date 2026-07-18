// <copyright file="AccessTelemetryHealthState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded lifecycle health states in increasing precedence order.</summary>
public enum AccessTelemetryHealthState
{
    /// <summary>Enabled and healthy, but no recent data.</summary>
    NoData,

    /// <summary>All gates and lifecycle operations are healthy.</summary>
    Healthy,

    /// <summary>Writes are temporarily degraded.</summary>
    Degraded,

    /// <summary>A fail-closed gate blocks lifecycle operations.</summary>
    Unhealthy,
}
