// <copyright file="BoundedNonceReplayCache.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Thread-safe bounded FIFO replay cache for verified attestation nonces.</summary>
public sealed class BoundedNonceReplayCache
{
    private readonly int _capacity;
    private readonly HashSet<string> _nonces = new(StringComparer.Ordinal);
    private readonly Queue<string> _order = [];
    private readonly Lock _gate = new();

    /// <summary>Initializes a cache with an exact positive capacity.</summary>
    public BoundedNonceReplayCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    /// <summary>Adds a single-use nonce, returning false for replay.</summary>
    public bool TryAdd(string nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        lock (_gate)
        {
            if (!_nonces.Add(nonce))
            {
                return false;
            }

            _order.Enqueue(nonce);
            while (_order.Count > _capacity)
            {
                _ = _nonces.Remove(_order.Dequeue());
            }

            return true;
        }
    }
}
