// <copyright file="TimedOutageAccessTelemetryDeliveryClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>Delivery seam that stays unavailable until an exact trusted-time recovery instant.</summary>
internal sealed class TimedOutageAccessTelemetryDeliveryClient(
    TimeProvider timeProvider,
    DateTimeOffset recoveryAt) : IAccessTelemetryDeliveryClient
{
    private readonly List<IReadOnlyList<string>> _attemptRecordIdBatches = [];
    private readonly List<DateTimeOffset> _attemptTimes = [];
    private readonly Lock _gate = new();
    private readonly SemaphoreSlim _attempts = new(initialCount: 0);
    private int _failedBatches;
    private int _successfulBatches;

    /// <summary>Gets the exact trusted-time instant of every delivery attempt.</summary>
    public IReadOnlyList<DateTimeOffset> AttemptTimes
    {
        get
        {
            lock (_gate)
            {
                return _attemptTimes.ToArray();
            }
        }
    }

    /// <summary>Gets the exact record identifiers carried by every attempted batch.</summary>
    public IReadOnlyList<IReadOnlyList<string>> AttemptRecordIdBatches
    {
        get
        {
            lock (_gate)
            {
                return _attemptRecordIdBatches.Select(static batch => (IReadOnlyList<string>)batch.ToArray()).ToArray();
            }
        }
    }

    /// <summary>Gets the number of batches rejected while the timed outage was active.</summary>
    public int FailedBatches => Volatile.Read(ref _failedBatches);

    /// <summary>Gets the number of batches accepted at or after recovery.</summary>
    public int SuccessfulBatches => Volatile.Read(ref _successfulBatches);

    /// <inheritdoc/>
    public Task<AccessTelemetryWriteBatchResponse> SendAsync(
        IReadOnlyList<AccessTelemetryRecord> records,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset attemptedAt = timeProvider.GetUtcNow();
        lock (_gate)
        {
            _attemptTimes.Add(attemptedAt);
            _attemptRecordIdBatches.Add(records.Select(static record => record.RecordId).ToArray());
        }

        _attempts.Release();
        if (attemptedAt < recoveryAt)
        {
            _ = Interlocked.Increment(ref _failedBatches);
            return Task.FromException<AccessTelemetryWriteBatchResponse>(new HttpRequestException("temporary timed outage"));
        }

        _ = Interlocked.Increment(ref _successfulBatches);
        return Task.FromResult(new AccessTelemetryWriteBatchResponse
        {
            Accepted = records.Count,
            Rejected = 0,
            Reason = AccessTelemetryReason.None,
        });
    }

    /// <summary>Waits for the next delivery attempt without sleeping or using a timing tolerance.</summary>
    /// <param name="cancellationToken">Cancels the observation.</param>
    public Task WaitForNextAttemptAsync(CancellationToken cancellationToken)
        => _attempts.WaitAsync(cancellationToken);
}
