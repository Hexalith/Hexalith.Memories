// <copyright file="ObservedFakeTimeProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Microsoft.Extensions.Time.Testing;

/// <summary>Trusted fake time that exposes when the real worker schedules its next timer.</summary>
internal sealed class ObservedFakeTimeProvider : TimeProvider
{
    private readonly FakeTimeProvider _inner;
    private readonly Lock _timerGate = new();
    private readonly List<(DateTimeOffset CreatedAt, TimeSpan DueTime, TimeSpan Period)> _timerRequests = [];
    private readonly SemaphoreSlim _timerCreations = new(initialCount: 0);
    private int _observedTimerCount;

    /// <summary>Initializes trusted fake time at an exact UTC instant.</summary>
    /// <param name="start">The initial UTC time.</param>
    public ObservedFakeTimeProvider(DateTimeOffset start)
    {
        _inner = new FakeTimeProvider(start);
    }

    /// <inheritdoc/>
    public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;

    /// <inheritdoc/>
    public override long TimestampFrequency => _inner.TimestampFrequency;

    /// <summary>Advances trusted time and synchronously fires every due fake timer.</summary>
    /// <param name="delta">The exact amount to advance.</param>
    public void Advance(TimeSpan delta) => _inner.Advance(delta);

    /// <summary>Sets trusted UTC time to a later exact instant.</summary>
    /// <param name="utcNow">The new trusted UTC time.</param>
    public void SetUtcNow(DateTimeOffset utcNow) => _inner.SetUtcNow(utcNow);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

    /// <inheritdoc/>
    public override long GetTimestamp() => _inner.GetTimestamp();

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ITimer timer = _inner.CreateTimer(callback, state, dueTime, period);
        lock (_timerGate)
        {
            _timerRequests.Add((_inner.GetUtcNow(), dueTime, period));
        }

        _timerCreations.Release();
        return timer;
    }

    /// <summary>Waits until the real worker creates its next fake-time timer and returns its exact request.</summary>
    /// <param name="cancellationToken">Cancels the observation.</param>
    /// <returns>The trusted creation time, requested due time, and requested period.</returns>
    public async Task<(DateTimeOffset CreatedAt, TimeSpan DueTime, TimeSpan Period)> WaitForTimerCreationAsync(
        CancellationToken cancellationToken)
    {
        await _timerCreations.WaitAsync(cancellationToken).ConfigureAwait(false);
        lock (_timerGate)
        {
            return _timerRequests[_observedTimerCount++];
        }
    }
}
