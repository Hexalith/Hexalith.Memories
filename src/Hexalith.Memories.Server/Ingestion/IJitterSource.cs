// <copyright file="IJitterSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Abstraction for jittered retry delays (Story 6.2, NFR22). Injectable so tests can supply
/// deterministic values and avoid reliance on process-global <see cref="Random.Shared"/>.</summary>
public interface IJitterSource
{
    /// <summary>Returns a uniform-random integer in <c>[0, maxExclusive)</c> milliseconds.</summary>
    /// <param name="maxExclusive">Upper (exclusive) bound — must be positive.</param>
    /// <returns>Jitter delay in milliseconds.</returns>
    int NextMilliseconds(int maxExclusive = 500);
}
