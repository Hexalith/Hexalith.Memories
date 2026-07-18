// <copyright file="MonotonicRecordIdGenerator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using BaUlid = ByteAether.Ulid.Ulid;

/// <summary>Generates process-monotonic uppercase Crockford ULIDs.</summary>
public sealed class MonotonicRecordIdGenerator
{
    private static readonly BaUlid.GenerationOptions Options = new()
    {
        Monotonicity = BaUlid.GenerationOptions.MonotonicityOptions.MonotonicIncrement,
    };

    private readonly Lock _gate = new();

    /// <summary>Creates the next monotonic record ID.</summary>
    /// <returns>An uppercase 26-character ULID.</returns>
    public string NewId()
    {
        lock (_gate)
        {
            return BaUlid.New(Options).ToString().ToUpperInvariant();
        }
    }
}
