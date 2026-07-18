// <copyright file="LifecycleClockEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Trusted-clock evidence paired with the locally constructed expected caller context.</summary>
internal sealed record LifecycleClockEvidence(
    SignedClockAttestation Attestation,
    string RequestingProcessEpoch,
    string RequestingServiceInstanceId);
