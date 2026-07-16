// <copyright file="GraphQueryExecutionOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Graph;

/// <summary>
/// Central graph query execution bounds shared by traversal callers and query builders.
/// </summary>
internal static class GraphQueryExecutionOptions
{
    /// <summary>
    /// Default maximum traversal rows returned by FalkorDB for bounded graph traversal queries.
    /// </summary>
    public const int DefaultTraversalResultLimit = 1_000;

    /// <summary>
    /// Converts a local graph operation timeout into the positive millisecond value expected by NFalkorDB.
    /// </summary>
    /// <param name="timeout">The local operation timeout.</param>
    /// <returns>A positive timeout in milliseconds.</returns>
    public static long ToServerTimeoutMilliseconds(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Graph operation timeout must be positive.");
        }

        double totalMilliseconds = Math.Ceiling(timeout.TotalMilliseconds);
        if (totalMilliseconds > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Graph operation timeout is too large.");
        }

        long milliseconds = checked((long)totalMilliseconds);
        if (milliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Graph operation timeout must resolve to positive milliseconds.");
        }

        return milliseconds;
    }
}
