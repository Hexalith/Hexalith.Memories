// <copyright file="ClockAttestationException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded fail-closed trusted-clock exception.</summary>
internal sealed class ClockAttestationException : Exception
{
    /// <summary>Initializes a clock exception with no source details.</summary>
    public ClockAttestationException(AccessTelemetryReason reason)
        : base("Trusted clock evidence is unavailable.")
    {
        Reason = reason;
    }

    /// <summary>Gets the bounded reason.</summary>
    public AccessTelemetryReason Reason { get; }
}
