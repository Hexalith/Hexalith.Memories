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
    private int _failureCount;

    /// <summary>Initializes the delivery worker.</summary>
    public AccessTelemetryDeliveryWorker(
        BoundedAccessTelemetryQueue queue,
        IAccessTelemetryDeliveryClient client,
        TimeProvider timeProvider)
    {
        _queue = queue;
        _client = client;
        _timeProvider = timeProvider;
    }

    /// <summary>Attempts one bounded delivery pass. Records remain queued unless fully acknowledged.</summary>
    public async Task DrainOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AccessTelemetryRecord> batch = _queue.PeekBatch(
            AccessTelemetryOptions.MaximumBatchRecords,
            AccessTelemetryOptions.MaximumBatchBytes);
        if (batch.Count == 0)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int expiredPrefix = batch.TakeWhile(record =>
            ParseUtc(record.ExpiresAtUtc) <= now ||
            ParseUtc(record.EmittedAtUtc).Add(AccessTelemetryOptions.MaximumRetryAge) <= now).Count();
        if (expiredPrefix > 0)
        {
            _queue.Acknowledge(expiredPrefix);
            ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Dropped, AccessTelemetryReason.Expired);
            ServerAccessTelemetryLifecycleMetrics.RecordQueueBytes(_queue.ByteCount);
            return;
        }

        try
        {
            AccessTelemetryWriteBatchResponse response = await _client.SendAsync(batch, cancellationToken).ConfigureAwait(false);
            if (response.Rejected == 0 && response.Accepted == batch.Count && response.Reason == AccessTelemetryReason.None)
            {
                _queue.Acknowledge(batch.Count);
                ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Persisted, AccessTelemetryReason.None);
                ServerAccessTelemetryLifecycleMetrics.RecordQueueBytes(_queue.ByteCount);
                _failureCount = 0;
            }
            else if (response.Reason is AccessTelemetryReason.ConfigurationInvalid or AccessTelemetryReason.RecordIdConflict)
            {
                _failureCount = int.MaxValue;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
        {
            _failureCount = Math.Min(_failureCount + 1, 16);
            ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Retried, AccessTelemetryReason.DependencyUnavailable);
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DrainOnceAsync(stoppingToken).ConfigureAwait(false);
            int upperMilliseconds = _failureCount == 0
                ? 100
                : Math.Min(5000, 100 * (1 << Math.Min(_failureCount, 6)));
            int delayMilliseconds = _failureCount == 0 ? upperMilliseconds : Random.Shared.Next(100, upperMilliseconds + 1);
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
            await DrainOnceAsync(flush.Token).ConfigureAwait(false);
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DateTimeOffset ParseUtc(string value)
        => DateTimeOffset.ParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
}
