// <copyright file="IAccessTelemetryCapabilityProbe.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Behavioral probe against the exact configured Dapr component profile.</summary>
internal interface IAccessTelemetryCapabilityProbe
{
    /// <summary>Runs one bounded behavior check.</summary>
    Task<AccessTelemetryCapabilityProbeResult> ProbeAsync(CancellationToken cancellationToken);
}
