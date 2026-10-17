// <copyright file="LocalPurgeTestClockGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Supplies fixed authenticated time at the actor's existing clock seam.</summary>
internal sealed class LocalPurgeTestClockGate(DateTimeOffset now) : IAccessTelemetryClockGate
{
    /// <inheritdoc/>
    public ClockAttestationValidationResult Validate(SignedClockAttestation attestation, string expectedAppId, string expectedProcessEpoch, string expectedServiceInstanceId)
        => new(true, AccessTelemetryReason.None, now.ToUnixTimeMilliseconds());
}
