// <copyright file="TelemetryAsserts.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using OpenTelemetry.Trace;

/// <summary>
/// Story 8.5 Task 3.1.1 — bounded polling helper for Tier-3 span-presence assertions.
/// <para>
/// <see cref="TracerProvider.ForceFlushAsync(int)"/> flushes the tracer-level batch processor but
/// does NOT accelerate the Redis instrumentation's internal <c>DrainThread</c> (which runs on its
/// own <see cref="Extensions.RedisInstrumentationFlushInterval"/> cadence — 100ms per ADR-8.5-001
/// (e)). A hard <see cref="Task.Delay(TimeSpan, CancellationToken)"/> after <c>ForceFlush</c> is a
/// documented flake source: either too short (span not yet drained) or too long (slow tests).
/// </para>
/// <para>
/// This helper replaces the <c>ForceFlush + Task.Delay</c> smell with a bounded poll loop. Each
/// iteration calls <see cref="TracerProvider.ForceFlushAsync(int)"/> (best-effort tracer-level
/// flush) then re-scans the collector for the predicate match; returns <see langword="true"/> on
/// first hit, <see langword="false"/> on timeout. The existing 2-second tracer-level
/// <c>ForceFlushAsync</c> call in <c>AspireEndToEndTraceTests</c> is unchanged — this helper is
/// ADDITIVE and used only for Redis-span presence predicates where DrainThread cadence matters.
/// </para>
/// </summary>
internal static class TelemetryAsserts
{
    /// <summary>Default timeout before declaring the predicate unmet.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Default poll interval between scans.</summary>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Polls <paramref name="collector"/> until an item matching <paramref name="predicate"/> is
    /// observed or <paramref name="timeout"/> elapses. Each iteration invokes
    /// <see cref="TracerProvider.ForceFlushAsync(int)"/> on <paramref name="tracerProvider"/>
    /// (best-effort tracer flush) before rescanning.
    /// </summary>
    /// <typeparam name="T">Activity collection element type.</typeparam>
    /// <param name="tracerProvider">Tracer provider whose batch processor the poll loop flushes.
    /// May be <see langword="null"/>; in that case no flush is attempted per iteration.</param>
    /// <param name="collector">Collection scanned each iteration.</param>
    /// <param name="predicate">Predicate matched against each collected item.</param>
    /// <param name="cancellationToken">Caller cancellation token; propagated.</param>
    /// <param name="timeout">Overall timeout. <see cref="TimeSpan.Zero"/> defaults to
    /// <see cref="DefaultTimeout"/>.</param>
    /// <param name="pollInterval">Wait between scans. <see cref="TimeSpan.Zero"/> defaults to
    /// <see cref="DefaultPollInterval"/>.</param>
    /// <returns><see langword="true"/> if any element matches before timeout; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> WaitForActivityAsync<T>(
        TracerProvider? tracerProvider,
        IReadOnlyCollection<T> collector,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default,
        TimeSpan timeout = default,
        TimeSpan pollInterval = default)
    {
        ArgumentNullException.ThrowIfNull(collector);
        ArgumentNullException.ThrowIfNull(predicate);

        TimeSpan effectiveTimeout = timeout == TimeSpan.Zero ? DefaultTimeout : timeout;
        TimeSpan effectivePoll = pollInterval == TimeSpan.Zero ? DefaultPollInterval : pollInterval;

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(effectiveTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (tracerProvider is not null)
            {
                _ = tracerProvider.ForceFlush(500);
            }

            if (collector.Any(predicate))
            {
                return true;
            }

            try
            {
                await Task.Delay(effectivePoll, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        // One final flush + scan after deadline in case the last iteration slept past the
        // deadline with data already present.
        if (tracerProvider is not null)
        {
            _ = tracerProvider.ForceFlush(500);
        }

        return collector.Any(predicate);
    }
}
