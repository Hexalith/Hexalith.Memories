// <copyright file="IClockAttestationSigner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

/// <summary>Independent clock-only signing authority.</summary>
internal interface IClockAttestationSigner
{
    /// <summary>Gets the bounded signer/key epoch.</summary>
    string KeyEpoch { get; }

    /// <summary>Signs canonical attestation bytes.</summary>
    byte[] Sign(ReadOnlySpan<byte> payload);
}
