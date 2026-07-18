// <copyright file="IAccessTelemetryClockEvidenceProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Obtains freshly signed and locally verified clock evidence.</summary>
internal interface IAccessTelemetryClockEvidenceProvider
{
    /// <summary>Gets a new single-use attestation after every delivery/reconnect context.</summary>
    Task<SignedClockAttestation> GetAsync(CancellationToken cancellationToken);
}
