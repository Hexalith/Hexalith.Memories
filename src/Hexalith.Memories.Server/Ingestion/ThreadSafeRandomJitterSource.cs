// <copyright file="ThreadSafeRandomJitterSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Thread-safe production implementation of <see cref="IJitterSource"/> backed by
/// <see cref="Random.Shared"/>.</summary>
public sealed class ThreadSafeRandomJitterSource : IJitterSource
{
    /// <inheritdoc/>
    public int NextMilliseconds(int maxExclusive = 500)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return Random.Shared.Next(0, maxExclusive);
    }
}
