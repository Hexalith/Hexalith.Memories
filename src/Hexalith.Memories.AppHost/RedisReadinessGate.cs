// <copyright file="RedisReadinessGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Shares preparation and probing of the current Redis allocation with all startup consumers.</summary>
/// <param name="startTimeout">The maximum time to wait for Aspire to start Redis.</param>
internal sealed class RedisReadinessGate(TimeSpan startTimeout)
{
    private readonly Lock _sync = new();
    private TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource _readiness = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Starts a fresh readiness task and invalidates work from an earlier Redis start.</summary>
    /// <param name="waitForRunning">Waits for the current Redis resource to reach Running.</param>
    /// <param name="prepareAndProbe">Installs components for the allocation and starts the bounded probe.</param>
    /// <param name="applicationStopping">Cancellation token for the AppHost lifetime.</param>
    /// <returns>The preparation and probe worker task.</returns>
    internal Task StartGenerationAsync(
        Func<CancellationToken, Task> waitForRunning,
        Func<CancellationToken, Task> prepareAndProbe,
        CancellationToken applicationStopping)
    {
        lock (_sync)
        {
            if (_started.Task.IsCompleted)
            {
                _readiness.TrySetCanceled();
                _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _readiness = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            _started.SetResult();
            return CompleteReadinessAsync(_readiness, waitForRunning, prepareAndProbe, applicationStopping);
        }
    }

    /// <summary>Waits for the latest Redis readiness outcome with independent caller cancellation.</summary>
    /// <param name="cancellationToken">Cancellation token for this consumer only.</param>
    /// <returns>A task that completes when the Redis endpoint is ready.</returns>
    internal async Task WaitAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task started;
            Task readiness;
            lock (_sync)
            {
                started = _started.Task;
                readiness = _readiness.Task;
            }

            await WaitForStartAsync(started, cancellationToken).ConfigureAwait(false);
            await readiness.WaitAsync(cancellationToken).ConfigureAwait(false);

            // A successful earlier probe may have completed just before its start was superseded.
            lock (_sync)
            {
                if (ReferenceEquals(readiness, _readiness.Task))
                {
                    return;
                }
            }
        }
    }

    private async Task WaitForStartAsync(Task started, CancellationToken cancellationToken)
    {
        try
        {
            await started.WaitAsync(startTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException($"Redis resource 'memories-vectors' did not start within {startTimeout}.", ex);
        }
    }

    private async Task CompleteReadinessAsync(
        TaskCompletionSource readiness,
        Func<CancellationToken, Task> waitForRunning,
        Func<CancellationToken, Task> prepareAndProbe,
        CancellationToken applicationStopping)
    {
        using CancellationTokenSource probeCancellation = CancellationTokenSource.CreateLinkedTokenSource(applicationStopping);
        try
        {
            Task work = PrepareAndProbeAsync(readiness, waitForRunning, prepareAndProbe, probeCancellation.Token);
            if (await Task.WhenAny(work, readiness.Task).ConfigureAwait(false) == readiness.Task)
            {
                await probeCancellation.CancelAsync().ConfigureAwait(false);
            }

            await work.ConfigureAwait(false);
            readiness.TrySetResult();
        }
        catch (OperationCanceledException ex)
        {
            readiness.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            if (readiness.TrySetException(ex))
            {
                // The consumers await this outcome. Observe it here as well if none are starting.
                _ = readiness.Task.Exception;
            }
        }
    }

    private async Task PrepareAndProbeAsync(
        TaskCompletionSource readiness,
        Func<CancellationToken, Task> waitForRunning,
        Func<CancellationToken, Task> prepareAndProbe,
        CancellationToken cancellationToken)
    {
        await waitForRunning(cancellationToken).ConfigureAwait(false);

        Task probe;
        lock (_sync)
        {
            if (!ReferenceEquals(readiness, _readiness))
            {
                return;
            }

            // Component preparation runs synchronously before the probe task is returned.
            probe = prepareAndProbe(cancellationToken);
        }

        await probe.ConfigureAwait(false);
    }
}
