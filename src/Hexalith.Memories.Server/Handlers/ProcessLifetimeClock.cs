// <copyright file="ProcessLifetimeClock.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Handlers;

using System;

/// <summary>Captures the process start timestamp once and exposes a stable uptime calculation to scoped
/// services. Story 9.3 uses this for handler-subscription startup-grace inference; keeping the clock as
/// a singleton avoids resetting the grace window on every request.</summary>
public sealed class ProcessLifetimeClock
{
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _startedAt;

    /// <summary>Initializes a new instance of the <see cref="ProcessLifetimeClock"/> class.</summary>
    /// <param name="timeProvider">The wall-clock source used for both capture and later uptime reads.</param>
    public ProcessLifetimeClock(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        _startedAt = timeProvider.GetUtcNow();
    }

    /// <summary>Gets the captured process-start timestamp.</summary>
    public DateTimeOffset StartedAt => _startedAt;

    /// <summary>Gets the current process uptime.</summary>
    /// <returns>The elapsed wall-clock time since <see cref="StartedAt"/>.</returns>
    public TimeSpan GetUptime() => _timeProvider.GetUtcNow() - _startedAt;
}