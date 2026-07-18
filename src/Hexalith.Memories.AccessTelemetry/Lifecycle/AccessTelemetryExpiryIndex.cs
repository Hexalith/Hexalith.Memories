// <copyright file="AccessTelemetryExpiryIndex.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Security.Cryptography;
using System.Text;

/// <summary>Deterministic 64-shard minute expiry index.</summary>
internal static class AccessTelemetryExpiryIndex
{
    /// <summary>Gets the deterministic shard for a record ID.</summary>
    public static int GetShard(string recordId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);
        Span<byte> hash = stackalloc byte[32];
        _ = SHA256.TryHashData(Encoding.ASCII.GetBytes(recordId), hash, out _);
        return hash[0] & 63;
    }

    /// <summary>Gets the absolute Unix minute for a canonical timestamp.</summary>
    public static long GetExpiryMinute(DateTimeOffset expiresAt)
        => expiresAt.ToUnixTimeSeconds() / 60;
}
