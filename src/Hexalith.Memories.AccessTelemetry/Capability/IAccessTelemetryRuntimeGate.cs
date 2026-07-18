// <copyright file="IAccessTelemetryRuntimeGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Exact-profile gate checked before every lifecycle write.</summary>
internal interface IAccessTelemetryRuntimeGate
{
    /// <summary>Gets the current immutable capability decision.</summary>
    AccessTelemetryCapabilityGateResult Current { get; }
}
