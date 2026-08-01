// <copyright file="AccessTelemetryOperationRendezvous.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

/// <summary>Forces a fixed number of independent test operations to be active at the same rendezvous.</summary>
internal sealed class AccessTelemetryOperationRendezvous
{
    private readonly TaskCompletionSource _allEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int _participantCount;
    private int _entered;
    private int _overlapObserved;

    /// <summary>Initializes a rendezvous for the exact participant count.</summary>
    /// <param name="participantCount">The number of operations that must enter before any may continue.</param>
    public AccessTelemetryOperationRendezvous(int participantCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(participantCount, 2);
        _participantCount = participantCount;
    }

    /// <summary>Gets whether all required operations were observed at the rendezvous simultaneously.</summary>
    public bool OverlapObserved => Volatile.Read(ref _overlapObserved) != 0;

    /// <summary>Enters the rendezvous and waits until every participant has entered.</summary>
    /// <param name="cancellationToken">Cancels the wait.</param>
    public async Task EnterAsync(CancellationToken cancellationToken)
    {
        int entered = Interlocked.Increment(ref _entered);
        if (entered > _participantCount)
        {
            throw new InvalidOperationException("More operations entered the lifecycle test rendezvous than declared.");
        }

        if (entered == _participantCount)
        {
            Volatile.Write(ref _overlapObserved, 1);
            _allEntered.TrySetResult();
        }

        await _allEntered.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
