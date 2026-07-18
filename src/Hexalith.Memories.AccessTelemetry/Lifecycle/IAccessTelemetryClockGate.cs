// <copyright file="IAccessTelemetryClockGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Fail-closed trusted-clock gate evaluated inside every actor mutation.</summary>
internal interface IAccessTelemetryClockGate
{
    /// <summary>Validates one context-bound single-use attestation.</summary>
    ClockAttestationValidationResult Validate(SignedClockAttestation attestation);
}
