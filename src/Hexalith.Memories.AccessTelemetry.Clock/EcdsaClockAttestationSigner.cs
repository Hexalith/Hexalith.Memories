// <copyright file="EcdsaClockAttestationSigner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

using System.Security.Cryptography;

/// <summary>ECDSA P-256 clock signer backed by clock-only key material.</summary>
internal sealed class EcdsaClockAttestationSigner : IClockAttestationSigner
{
    private readonly ECDsa _key;

    /// <summary>Initializes a signer without taking ownership of the key.</summary>
    public EcdsaClockAttestationSigner(ECDsa key, string keyEpoch)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        KeyEpoch = keyEpoch ?? throw new ArgumentNullException(nameof(keyEpoch));
    }

    /// <inheritdoc/>
    public string KeyEpoch { get; }

    /// <inheritdoc/>
    public byte[] Sign(ReadOnlySpan<byte> payload)
        => _key.SignData(payload, HashAlgorithmName.SHA256);
}
