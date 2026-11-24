// <copyright file="RedisReadinessBackoff.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Computes the delay between AppHost Redis readiness probe attempts.</summary>
/// <remarks>
/// The delay doubles from <see cref="InitialDelay"/> up to <see cref="MaximumDelay"/>. Equal jitter keeps each
/// delay between half and all of that value so parallel AppHosts do not probe in lockstep, and no delay is
/// shorter than the former fixed 500 ms cadence. A delay never extends past the probe deadline.
/// </remarks>
internal static class RedisReadinessBackoff
{
    /// <summary>The delay after the first failed attempt, before jitter.</summary>
    internal static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(1);

    /// <summary>The largest delay between attempts, before jitter.</summary>
    internal static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(4);

    /// <summary>Computes the delay before the next readiness attempt.</summary>
    /// <param name="failedAttempts">The number of failed attempts so far; 1 after the first failure.</param>
    /// <param name="jitterSample">A uniformly distributed sample in the range [0, 1).</param>
    /// <param name="remaining">The time left before the probe deadline.</param>
    /// <returns>The delay, never negative and never longer than <paramref name="remaining"/>.</returns>
    internal static TimeSpan GetDelay(int failedAttempts, double jitterSample, TimeSpan remaining)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failedAttempts, 1);
        if (double.IsNaN(jitterSample) || jitterSample < 0 || jitterSample >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(jitterSample), jitterSample, "The jitter sample must be in the range [0, 1).");
        }

        if (remaining <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        int doublings = Math.Min(failedAttempts - 1, 16);
        double backoffMilliseconds = Math.Min(
            InitialDelay.TotalMilliseconds * Math.Pow(2, doublings),
            MaximumDelay.TotalMilliseconds);
        TimeSpan delay = TimeSpan.FromMilliseconds(backoffMilliseconds * (0.5 + (jitterSample / 2)));
        return delay < remaining ? delay : remaining;
    }
}
