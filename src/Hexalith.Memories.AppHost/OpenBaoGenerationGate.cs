// <copyright file="OpenBaoGenerationGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Tracks the refreshable readiness signal for disposable OpenBao generations.</summary>
internal sealed class OpenBaoGenerationGate
{
    private readonly object _sync = new();
    private OpenBaoGenerationLease? _activeLease;
    private int _generationNumber;
    private TaskCompletionSource _nextReadiness = CreateCompletionSource();

    /// <summary>Attempts to acquire exclusive ownership of generation initialization.</summary>
    /// <param name="lease">The active generation lease.</param>
    /// <returns><see langword="true"/> only for the callback that owns initialization.</returns>
    internal bool TryBeginGeneration(out OpenBaoGenerationLease lease)
    {
        lock (_sync)
        {
            if (_activeLease is not null)
            {
                lease = _activeLease;
                return false;
            }

            if (_nextReadiness.Task.IsCompleted)
            {
                _nextReadiness = CreateCompletionSource();
            }

            lease = new OpenBaoGenerationLease(++_generationNumber, _nextReadiness);
            _activeLease = lease;
            return true;
        }
    }

    /// <summary>Gets the readiness task belonging to the current or next generation.</summary>
    /// <returns>The generation readiness task.</returns>
    internal Task SnapshotReadiness()
    {
        lock (_sync)
        {
            return (_activeLease?.Readiness ?? _nextReadiness).Task;
        }
    }

    /// <summary>Atomically installs artifacts and publishes readiness for the current generation.</summary>
    /// <param name="lease">The lease that produced the artifacts.</param>
    /// <param name="install">The protected artifact installation action.</param>
    /// <returns><see langword="false"/> when the lease became stale before installation.</returns>
    internal bool TryInstallCurrent(OpenBaoGenerationLease lease, Action install)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(install);

        lock (_sync)
        {
            if (!ReferenceEquals(_activeLease, lease))
            {
                return false;
            }

            install();
            lease.Readiness.TrySetResult();
            return true;
        }
    }

    /// <summary>Publishes an initialization failure only when the lease is still current.</summary>
    /// <param name="lease">The lease that failed.</param>
    /// <param name="exception">The initialization failure.</param>
    /// <returns><see langword="true"/> when the failure belonged to the current generation.</returns>
    internal bool TryFailCurrent(OpenBaoGenerationLease lease, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(exception);

        lock (_sync)
        {
            return ReferenceEquals(_activeLease, lease) && lease.Readiness.TrySetException(exception);
        }
    }

    /// <summary>Closes the active generation so the next start obtains a fresh readiness signal.</summary>
    internal void MarkStopped()
    {
        lock (_sync)
        {
            if (_activeLease is null)
            {
                return;
            }

            _activeLease.Cancel();
            _activeLease.Readiness.TrySetException(
                new OperationCanceledException("The OpenBao generation stopped before initialization completed."));
            _activeLease = null;
            _nextReadiness = CreateCompletionSource();
        }
    }

    private static TaskCompletionSource CreateCompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
