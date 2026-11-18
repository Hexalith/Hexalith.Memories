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
    private TaskCompletionSource _readiness = CreateCompletionSource();

    /// <summary>Starts a generation and invalidates any earlier lease.</summary>
    /// <returns>The lease that owns initialization for this start.</returns>
    internal OpenBaoGenerationLease BeginGeneration()
    {
        lock (_sync)
        {
            if (_activeLease is { } previous)
            {
                previous.Cancel();
                previous.Readiness.TrySetException(
                    new OperationCanceledException("OpenBao started another generation before initialization completed."));
                _readiness = CreateCompletionSource();
            }

            return _activeLease = new OpenBaoGenerationLease(++_generationNumber, _readiness);
        }
    }

    /// <summary>Gets the readiness task belonging to the current or next generation.</summary>
    /// <returns>The generation readiness task.</returns>
    internal Task SnapshotReadiness()
    {
        lock (_sync)
        {
            return _readiness.Task;
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
            if (!ReferenceEquals(_activeLease, lease))
            {
                return false;
            }

            lease.Readiness.TrySetException(exception);
            return true;
        }
    }

    private static TaskCompletionSource CreateCompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
