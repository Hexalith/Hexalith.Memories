// <copyright file="HealthProbe.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

using System.Diagnostics;

using Hexalith.Memories.Client.Rest;

/// <summary>
/// Polls the server's <c>/health</c> endpoint until it returns 2xx or a total timeout elapses.
/// Wizard step 3. Uses <see cref="MemoriesClient.ProbeHealthAsync(CancellationToken)"/> which
/// swallows transport failures and returns <see langword="false"/> — the probe treats those as
/// "not yet ready" and retries rather than propagating.
/// </summary>
public sealed class HealthProbe
{
    private readonly MemoriesClient _client;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="HealthProbe"/> class.</summary>
    /// <param name="client">The REST client.</param>
    /// <param name="timeProvider">Time provider abstraction — inject a fake in tests to avoid real wall-clock delays.</param>
    public HealthProbe(MemoriesClient client, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _client = client;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Polls <see cref="MemoriesClient.ProbeHealthAsync(CancellationToken)"/> every
    /// <paramref name="pollInterval"/> until it returns <see langword="true"/> or
    /// <paramref name="totalTimeout"/> elapses.
    /// </summary>
    /// <param name="totalTimeout">Maximum total wait.</param>
    /// <param name="pollInterval">Interval between probes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The probe result.</returns>
    public async Task<HealthProbeResult> WaitForReadyAsync(
        TimeSpan totalTimeout,
        TimeSpan pollInterval,
        CancellationToken ct)
    {
        long startTimestamp = _timeProvider.GetTimestamp();
        string? lastError = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            TimeSpan elapsed = _timeProvider.GetElapsedTime(startTimestamp);
            if (elapsed >= totalTimeout)
            {
                return new HealthProbeResult(
                    Ready: false,
                    Elapsed: elapsed,
                    LastError: lastError ?? "Timeout elapsed without a successful probe.");
            }

            bool healthy;
            try
            {
                healthy = await _client.ProbeHealthAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                healthy = false;
            }

            if (healthy)
            {
                return new HealthProbeResult(
                    Ready: true,
                    Elapsed: _timeProvider.GetElapsedTime(startTimestamp),
                    LastError: null);
            }

            TimeSpan remaining = totalTimeout - _timeProvider.GetElapsedTime(startTimestamp);
            if (remaining <= TimeSpan.Zero)
            {
                return new HealthProbeResult(
                    Ready: false,
                    Elapsed: _timeProvider.GetElapsedTime(startTimestamp),
                    LastError: lastError ?? "Timeout elapsed without a successful probe.");
            }

            TimeSpan delay = pollInterval < remaining ? pollInterval : remaining;
            try
            {
                await Task.Delay(delay, _timeProvider, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
        }
    }
}

/// <summary>Outcome of a <see cref="HealthProbe.WaitForReadyAsync(TimeSpan, TimeSpan, CancellationToken)"/> call.</summary>
/// <param name="Ready">True when the server returned 2xx before the timeout.</param>
/// <param name="Elapsed">Wall-clock time spent probing.</param>
/// <param name="LastError">Last probe exception message (null on immediate success; "Cancelled." on cancellation).</param>
public sealed record HealthProbeResult(bool Ready, TimeSpan Elapsed, string? LastError);
