// <copyright file="AccessTelemetryLifecycleActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Dapr.Actors.Runtime;

using Hexalith.Memories.AccessTelemetry.Capability;
using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Observability;

/// <summary>Dapr actor hosted only as <c>AccessTelemetryLifecycleActor/global</c>.</summary>
internal sealed class AccessTelemetryLifecycleActor : Actor, IAccessTelemetryLifecycleActor, IRemindable
{
    private const string PurgeReminderName = "bounded-purge";
    private const string StateName = "lifecycle-control";
    private readonly AccessTelemetryLifecycleProcessor _processor;
    private readonly IAccessTelemetryClockGate _clockGate;
    private readonly IAccessTelemetryRuntimeGate _runtimeGate;
    private readonly AccessTelemetryOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes the fixed lifecycle actor.</summary>
    public AccessTelemetryLifecycleActor(
        ActorHost host,
        AccessTelemetryLifecycleProcessor processor,
        IAccessTelemetryClockGate clockGate,
        IAccessTelemetryRuntimeGate runtimeGate,
        AccessTelemetryOptions options,
        TimeProvider timeProvider)
        : base(host)
    {
        _processor = processor;
        _clockGate = clockGate;
        _runtimeGate = runtimeGate;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryWriteBatchResponse> WriteBatchAsync(AccessTelemetryWriteBatchRequest request)
    {
        EnsureGlobalActor();
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<AccessTelemetryRecord> records = request.Records;
        if (!_runtimeGate.Current.AllowsWrites)
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = _runtimeGate.Current.Reason };
        }
        ClockAttestationValidationResult clock = _clockGate.Validate(request.ClockAttestation);
        if (!clock.IsValid)
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = clock.Reason };
        }

        if (!string.Equals(request.ConfigurationEpoch, _options.ConfigurationEpoch, StringComparison.Ordinal) ||
            !string.Equals(request.ComponentProfileHash, _options.ComponentProfileHash, StringComparison.Ordinal) ||
            !string.Equals(request.ComponentProfileHash, request.ClockAttestation.ComponentProfileHash, StringComparison.Ordinal))
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.ConfigurationInvalid };
        }

        AccessTelemetryLifecycleActorState initialState = await GetStateAsync().ConfigureAwait(false);
        if (initialState.Configuration is null ||
            !string.Equals(initialState.Configuration.Epoch, request.ConfigurationEpoch, StringComparison.Ordinal) ||
            !string.Equals(initialState.Configuration.ComponentProfileHash, request.ComponentProfileHash, StringComparison.Ordinal))
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.ConfigurationInvalid };
        }
        if (initialState.MarkerKeyRotation is { ActiveGeneration: string activeGeneration } &&
            records.Any(record => !string.Equals(record.MarkerKeyId, activeGeneration, StringComparison.Ordinal) &&
                !string.Equals(record.MarkerKeyId, initialState.MarkerKeyRotation.StagedGeneration, StringComparison.Ordinal)))
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.ConfigurationInvalid };
        }
        if (records.Count is < 1 or > AccessTelemetryOptions.MaximumBatchRecords ||
            records.Sum(static record => AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length) > AccessTelemetryOptions.MaximumBatchBytes)
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.SchemaMismatch };
        }

        int accepted = 0;
        AccessTelemetryReason reason = AccessTelemetryReason.None;
        foreach (AccessTelemetryRecord record in records)
        {
            AccessTelemetryPersistenceResult result = await _processor.PersistAsync(record, CancellationToken.None).ConfigureAwait(false);
            if (result.Status is AccessTelemetryPersistenceStatus.Inserted or AccessTelemetryPersistenceStatus.Idempotent)
            {
                accepted++;
            }
            else
            {
                reason = result.Reason;
            }
        }

        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        await StateManager.SetStateAsync(StateName, state with { RetainedRecordCount = state.RetainedRecordCount + accepted }).ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordCapacity(state.RetainedRecordCount + accepted);
        return new AccessTelemetryWriteBatchResponse { Accepted = accepted, Rejected = records.Count - accepted, Reason = reason };
    }

    /// <inheritdoc/>
    public async Task HeartbeatAsync(WriterHeartbeat heartbeat)
    {
        EnsureGlobalActor();
        ArgumentNullException.ThrowIfNull(heartbeat);
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        Dictionary<string, WriterHeartbeat> writers = new(state.Writers, StringComparer.Ordinal)
        {
            [$"{heartbeat.ServiceInstanceId}/{heartbeat.ProcessEpoch}"] = heartbeat,
        };
        long now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        foreach (string expired in writers.Where(pair => pair.Value.LeaseExpiresAtUnixMilliseconds <= now).Select(static pair => pair.Key).ToArray())
        {
            _ = writers.Remove(expired);
        }

        await StateManager.SetStateAsync(StateName, state with { Writers = writers }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task PurgeAsync()
    {
        EnsureGlobalActor();
        bool hasMore;
        do
        {
            AccessTelemetryPurgeResult result = await _processor.PurgeAsync(CancellationToken.None).ConfigureAwait(false);
            AccessTelemetryLifecycleMetrics.RecordReminder(succeeded: true);
            AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
            long now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            await StateManager.SetStateAsync(
                StateName,
                state with
                {
                    LastPurgeUnixMilliseconds = now,
                    RetainedRecordCount = Math.Max(0, state.RetainedRecordCount - result.Purged),
                    ReminderSequence = state.ReminderSequence + 1,
                }).ConfigureAwait(false);
            hasMore = result.HasMore;
            if (hasMore)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(25, 101))).ConfigureAwait(false);
            }
        }
        while (hasMore);
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryInspectionResponse> InspectAsync()
    {
        EnsureGlobalActor();
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordPhysicalEvidence(present: false);
        return new AccessTelemetryInspectionResponse
        {
            Health = _processor.Health,
            Reason = _processor.Health == AccessTelemetryHealthState.Unhealthy
                ? AccessTelemetryReason.RecordIdConflict
                : AccessTelemetryReason.None,
            RetainedRecordCount = state.RetainedRecordCount,
            OldestExpiryMinute = state.ExpiryMinuteCursor == 0 ? null : state.ExpiryMinuteCursor,
            LastPurgeUnixMilliseconds = state.LastPurgeUnixMilliseconds,
            ConfigurationEpoch = state.Configuration?.Epoch ?? "unconfigured",
            PhysicalReclamationEvidencePending = true,
        };
    }

    /// <inheritdoc/>
    public async Task StageMarkerKeyAsync(string newGeneration)
    {
        EnsureGlobalActor();
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        long now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        MarkerKeyRotationState current = state.MarkerKeyRotation ?? new MarkerKeyRotationState
        {
            ActiveGeneration = state.ActiveMarkerKeyGeneration ?? throw new InvalidOperationException("Active marker-key generation is unconfigured."),
        };
        MarkerKeyRotationState staged = MarkerKeyRotationCoordinator.Stage(current, newGeneration, state.Writers.Values.ToArray(), now);
        await StateManager.SetStateAsync(
            StateName,
            state with { MarkerKeyRotation = staged, StagedMarkerKeyGeneration = newGeneration }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AcknowledgeMarkerKeyAsync(WriterHeartbeat heartbeat)
    {
        EnsureGlobalActor();
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        if (state.MarkerKeyRotation is null)
        {
            return;
        }

        MarkerKeyRotationState acknowledged = MarkerKeyRotationCoordinator.Acknowledge(
            state.MarkerKeyRotation,
            heartbeat,
            _timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        await StateManager.SetStateAsync(StateName, state with { MarkerKeyRotation = acknowledged }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> TryActivateMarkerKeyAsync(long finalOldKeyWriteUnixMilliseconds)
    {
        EnsureGlobalActor();
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        if (state.MarkerKeyRotation is null)
        {
            return false;
        }

        long now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        IReadOnlyList<WriterHeartbeat> writers = state.Writers.Values.ToArray();
        MarkerKeyRotationState rotation = state.MarkerKeyRotation;
        if (rotation.Phase == MarkerKeyRotationPhase.Staged &&
            !MarkerKeyRotationCoordinator.TryBeginDrain(rotation, writers, now, out rotation))
        {
            return false;
        }

        if (!MarkerKeyRotationCoordinator.TryActivate(rotation, writers, now, finalOldKeyWriteUnixMilliseconds, out rotation))
        {
            await StateManager.SetStateAsync(StateName, state with { MarkerKeyRotation = rotation }).ConfigureAwait(false);
            return false;
        }

        await StateManager.SetStateAsync(
            StateName,
            state with
            {
                MarkerKeyRotation = rotation,
                ActiveMarkerKeyGeneration = rotation.ActiveGeneration,
                StagedMarkerKeyGeneration = null,
            }).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc/>
    public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        => string.Equals(reminderName, PurgeReminderName, StringComparison.Ordinal)
            ? PurgeAsync()
            : Task.CompletedTask;

    /// <inheritdoc/>
    protected override async Task OnActivateAsync()
    {
        EnsureGlobalActor();
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        if (state.Configuration is null)
        {
            int retentionSeconds = checked((int)(_options.Retention ?? AccessTelemetryOptions.DefaultRetention).TotalSeconds);
            await StateManager.SetStateAsync(
                StateName,
                state with
                {
                    Configuration = new LifecycleConfigurationEpoch
                    {
                        Epoch = _options.ConfigurationEpoch,
                        SchemaVersion = _options.SchemaVersion,
                        ComponentProfileHash = _options.ComponentProfileHash,
                        RetentionSeconds = retentionSeconds,
                        MarkerKeyGeneration = _options.MarkerKeyGeneration,
                    },
                    ActiveMarkerKeyGeneration = _options.MarkerKeyGeneration,
                    PhysicalReclamationEvidenceId = _options.PhysicalReclamationEvidenceId,
                }).ConfigureAwait(false);
        }

        _ = await RegisterReminderAsync(
            PurgeReminderName,
            [],
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5)).ConfigureAwait(false);
    }

    private void EnsureGlobalActor()
    {
        if (!string.Equals(Id.GetId(), "global", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AccessTelemetryLifecycleActor is restricted to the fixed global actor ID.");
        }
    }

    private async Task<AccessTelemetryLifecycleActorState> GetStateAsync()
    {
        ConditionalValue<AccessTelemetryLifecycleActorState> state = await StateManager.TryGetStateAsync<AccessTelemetryLifecycleActorState>(StateName).ConfigureAwait(false);
        return state.HasValue ? state.Value : new AccessTelemetryLifecycleActorState();
    }
}
