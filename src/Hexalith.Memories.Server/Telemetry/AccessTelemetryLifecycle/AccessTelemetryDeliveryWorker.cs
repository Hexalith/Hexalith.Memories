// <copyright file="AccessTelemetryDeliveryWorker.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;

using Hexalith.Memories.AccessTelemetry.Contracts;

using Microsoft.Extensions.Hosting;

/// <summary>Hosted bounded-batch delivery worker with capped full-jitter recovery.</summary>
internal sealed class AccessTelemetryDeliveryWorker : BackgroundService
{
    private readonly BoundedAccessTelemetryQueue _queue;
    private readonly IAccessTelemetryDeliveryClient _client;
    private readonly TimeProvider _timeProvider;
    private readonly AccessTelemetryOptions _options;
    private readonly AccessTelemetryLifecycleStatus _status;
    private int _failureCount;
    private bool _terminal;

    /// <summary>Initializes the delivery worker.</summary>
    public AccessTelemetryDeliveryWorker(
        BoundedAccessTelemetryQueue queue,
        IAccessTelemetryDeliveryClient client,
        TimeProvider timeProvider,
        AccessTelemetryOptions options,
        AccessTelemetryLifecycleStatus status)
    {
        _queue = queue;
        _client = client;
        _timeProvider = timeProvider;
        _options = options;
        _status = status;
    }

    /// <summary>Attempts one bounded delivery pass. Records remain queued unless fully acknowledged.</summary>
    public async Task DrainOnceAsync(CancellationToken cancellationToken)
    {
        if (_terminal)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int expired = _queue.RemoveWhere(record =>
            ParseUtc(record.ExpiresAtUtc) <= now ||
            ParseUtc(record.EmittedAtUtc).Add(AccessTelemetryOptions.MaximumRetryAge) <= now);
        if (expired > 0)
        {
            ServerAccessTelemetryLifecycleMetrics.Record(expired, AccessTelemetryRecordState.Dropped, AccessTelemetryReason.Expired);
            ServerAccessTelemetryLifecycleMetrics.RecordQueueBytes(_queue.ByteCount);
        }

        IReadOnlyList<AccessTelemetryRecord> batch = _queue.PeekBatch(
            _options.BatchRecordLimit,
            _options.BatchByteLimit);
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            AccessTelemetryWriteBatchResponse response = await _client.SendAsync(batch, cancellationToken).ConfigureAwait(false);
            if (response.Accepted is < 0 || response.Accepted > batch.Count || response.Rejected < 0 || response.Accepted + response.Rejected != batch.Count)
            {
                throw new InvalidOperationException("The lifecycle service returned an invalid bounded acknowledgement.");
            }

            if (response.Accepted > 0)
            {
                _queue.Acknowledge(response.Accepted);
                ServerAccessTelemetryLifecycleMetrics.Record(response.Accepted, AccessTelemetryRecordState.Persisted, AccessTelemetryReason.None);
                _status.RecordActivity(now);
            }

            if (response.Rejected == 0 && response.Accepted == batch.Count && response.Reason == AccessTelemetryReason.None)
            {
                ServerAccessTelemetryLifecycleMetrics.RecordQueueBytes(_queue.ByteCount);
                _failureCount = 0;
                _status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
            }
            else if (response.Reason is AccessTelemetryReason.ConfigurationInvalid or AccessTelemetryReason.RecordIdConflict)
            {
                _terminal = true;
                _status.RecordActivity(now);
                _status.PublishTerminal(response.Reason);
            }
            else if (response.Reason is AccessTelemetryReason.SchemaMismatch or AccessTelemetryReason.Expired)
            {
                if (response.Rejected > 0)
                {
                    _queue.Acknowledge(1);
                    ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Dropped, response.Reason);
                    _status.RecordActivity(now);
                }

                _failureCount = 0;
            }
            else
            {
                _failureCount = Math.Min(_failureCount + 1, 16);
                _status.Publish(AccessTelemetryHealthState.Degraded, response.Reason);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            RegisterRetry();
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
        {
            RegisterRetry();
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DrainOnceAsync(stoppingToken).ConfigureAwait(false);
            int initialMilliseconds = checked((int)_options.RetryInitialDelay.TotalMilliseconds);
            int maximumMilliseconds = checked((int)_options.RetryMaximumDelay.TotalMilliseconds);
            int upperMilliseconds = _failureCount == 0
                ? initialMilliseconds
                : Math.Min(maximumMilliseconds, initialMilliseconds * (1 << Math.Min(_failureCount, 6)));
            int delayMilliseconds = _failureCount == 0 ? upperMilliseconds : Random.Shared.Next(initialMilliseconds, upperMilliseconds + 1);
            await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds), _timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        using var flush = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        flush.CancelAfter(AccessTelemetryOptions.ShutdownFlushTimeout);
        while (_queue.Count > 0 && !flush.IsCancellationRequested)
        {
            if (_terminal)
            {
                break;
            }

            int countBeforeDrain = _queue.Count;
            try
            {
                await DrainOnceAsync(flush.Token).ConfigureAwait(false);
                if (_queue.Count >= countBeforeDrain && !flush.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(25), flush.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (flush.IsCancellationRequested)
            {
                break;
            }
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DateTimeOffset ParseUtc(string value)
        => DateTimeOffset.ParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private void RegisterRetry()
    {
        _failureCount = Math.Min(_failureCount + 1, 16);
        _status.Publish(AccessTelemetryHealthState.Degraded, AccessTelemetryReason.DependencyUnavailable);
        ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Retried, AccessTelemetryReason.DependencyUnavailable);
    }
}
