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
    private readonly AccessTelemetryRuntimeOptionsProvider _optionsProvider;
    private readonly ILifecycleClockEvidenceProvider _clockEvidence;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes the fixed lifecycle actor.</summary>
    public AccessTelemetryLifecycleActor(
        ActorHost host,
        AccessTelemetryLifecycleProcessor processor,
        IAccessTelemetryClockGate clockGate,
        IAccessTelemetryRuntimeGate runtimeGate,
        AccessTelemetryRuntimeOptionsProvider optionsProvider,
        ILifecycleClockEvidenceProvider clockEvidence,
        TimeProvider timeProvider)
        : base(host)
    {
        _processor = processor;
        _clockGate = clockGate;
        _runtimeGate = runtimeGate;
        _optionsProvider = optionsProvider;
        _clockEvidence = clockEvidence;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryWriteBatchResponse> WriteBatchAsync(AccessTelemetryWriteBatchRequest request)
    {
        EnsureGlobalActor();
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<AccessTelemetryRecord> records = request.Records;
        if (!_optionsProvider.IsReady || !_runtimeGate.Current.AllowsWrites)
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = _runtimeGate.Current.Reason };
        }
        AccessTelemetryOptions options = _optionsProvider.Current;
        ClockAttestationValidationResult clock = _clockGate.Validate(
            request.ClockAttestation,
            "memories",
            request.RequestingProcessEpoch,
            request.RequestingServiceInstanceId);
        if (!clock.IsValid)
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = clock.Reason };
        }

        if (!string.Equals(request.ConfigurationEpoch, options.ConfigurationEpoch, StringComparison.Ordinal) ||
            !string.Equals(request.ComponentProfileHash, options.ComponentProfileHash, StringComparison.Ordinal) ||
            !string.Equals(request.ComponentProfileHash, request.ClockAttestation.ComponentProfileHash, StringComparison.Ordinal))
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.ConfigurationInvalid };
        }

        AccessTelemetryLifecycleActorState initialState = await GetStateAsync().ConfigureAwait(false);
        if (initialState.Health == AccessTelemetryHealthState.Unhealthy || initialState.Configuration is null ||
            !string.Equals(initialState.Configuration.Epoch, request.ConfigurationEpoch, StringComparison.Ordinal) ||
            !string.Equals(initialState.Configuration.ComponentProfileHash, request.ComponentProfileHash, StringComparison.Ordinal))
        {
            return new AccessTelemetryWriteBatchResponse
            {
                Accepted = 0,
                Rejected = records.Count,
                Reason = initialState.HealthReason == AccessTelemetryReason.None
                    ? AccessTelemetryReason.ConfigurationInvalid
                    : initialState.HealthReason,
            };
        }
        if (initialState.MarkerKeyRotation is { ActiveGeneration: string activeGeneration } &&
            records.Any(record => !string.Equals(record.MarkerKeyId, activeGeneration, StringComparison.Ordinal) &&
                !string.Equals(record.MarkerKeyId, initialState.MarkerKeyRotation.StagedGeneration, StringComparison.Ordinal)))
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.ConfigurationInvalid };
        }
        try
        {
            if (records.Count < 1 || records.Count > options.BatchRecordLimit ||
                records.Sum(static record => AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length) > options.BatchByteLimit)
            {
                return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.SchemaMismatch };
            }
        }
        catch (Exception exception) when (exception is AccessTelemetryContractException or InvalidOperationException or OverflowException)
        {
            return new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = records.Count, Reason = AccessTelemetryReason.SchemaMismatch };
        }

        int accepted = 0;
        int inserted = 0;
        AccessTelemetryReason reason = AccessTelemetryReason.None;
        DateTimeOffset trustedNow = DateTimeOffset.FromUnixTimeMilliseconds(
            clock.TrustedUnixMilliseconds ?? request.ClockAttestation.NotBeforeUnixMilliseconds);
        foreach (AccessTelemetryRecord record in records)
        {
            if (_timeProvider.GetUtcNow().ToUnixTimeMilliseconds() >= request.ClockAttestation.ExpiresAtUnixMilliseconds)
            {
                reason = AccessTelemetryReason.ClockUntrusted;
                break;
            }

            AccessTelemetryPersistenceResult result = await _processor.PersistAsync(record, trustedNow, CancellationToken.None).ConfigureAwait(false);
            if (result.Status is AccessTelemetryPersistenceStatus.Inserted or AccessTelemetryPersistenceStatus.Idempotent)
            {
                accepted++;
                if (result.Status == AccessTelemetryPersistenceStatus.Inserted)
                {
                    inserted++;
                }
            }
            else
            {
                reason = result.Reason;
                break;
            }
        }

        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        AccessTelemetryLifecycleActorState updated = state with
        {
            RetainedRecordCount = state.RetainedRecordCount + inserted,
            Health = reason == AccessTelemetryReason.RecordIdConflict ? AccessTelemetryHealthState.Unhealthy : state.Health,
            HealthReason = reason == AccessTelemetryReason.RecordIdConflict ? reason : state.HealthReason,
        };
        await StateManager.SetStateAsync(StateName, updated).ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordCapacity(updated.RetainedRecordCount);
        return new AccessTelemetryWriteBatchResponse { Accepted = accepted, Rejected = records.Count - accepted, Reason = reason };
    }

    /// <inheritdoc/>
    public async Task<WriterHeartbeatResponse> HeartbeatAsync(WriterHeartbeatRequest request)
    {
        EnsureGlobalActor();
        ArgumentNullException.ThrowIfNull(request);
        WriterHeartbeat heartbeat = request.Heartbeat;
        AccessTelemetryOptions options = _optionsProvider.Current;
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        string activeGeneration = state.ActiveMarkerKeyGeneration ?? options.MarkerKeyGeneration;
        if (!_optionsProvider.IsReady || !_runtimeGate.Current.AllowsWrites || state.Health == AccessTelemetryHealthState.Unhealthy)
        {
            return new WriterHeartbeatResponse
            {
                Accepted = false,
                Reason = state.HealthReason != AccessTelemetryReason.None ? state.HealthReason : _runtimeGate.Current.Reason,
                ActiveGeneration = activeGeneration,
                StagedGeneration = state.StagedMarkerKeyGeneration,
            };
        }

        ClockAttestationValidationResult clock = _clockGate.Validate(
            request.ClockAttestation,
            "memories",
            heartbeat.ProcessEpoch,
            heartbeat.ServiceInstanceId);
        if (!clock.IsValid || !IsValidHeartbeat(heartbeat, options, clock.TrustedUnixMilliseconds))
        {
            return new WriterHeartbeatResponse
            {
                Accepted = false,
                Reason = clock.IsValid ? AccessTelemetryReason.SchemaMismatch : clock.Reason,
                ActiveGeneration = activeGeneration,
                StagedGeneration = state.StagedMarkerKeyGeneration,
            };
        }

        Dictionary<string, WriterHeartbeat> writers = new(state.Writers, StringComparer.Ordinal)
        {
            [$"{heartbeat.ServiceInstanceId}/{heartbeat.ProcessEpoch}"] = heartbeat,
        };
        long now = clock.TrustedUnixMilliseconds!.Value;
        foreach (string expired in writers.Where(pair => pair.Value.LeaseExpiresAtUnixMilliseconds <= now).Select(static pair => pair.Key).ToArray())
        {
            _ = writers.Remove(expired);
        }

        string writerKey = $"{heartbeat.ServiceInstanceId}/{heartbeat.ProcessEpoch}";
        if (!state.Writers.ContainsKey(writerKey) && writers.Count > 256)
        {
            return new WriterHeartbeatResponse
            {
                Accepted = false,
                Reason = AccessTelemetryReason.ConfigurationInvalid,
                ActiveGeneration = activeGeneration,
                StagedGeneration = state.StagedMarkerKeyGeneration,
            };
        }

        MarkerKeyRotationState? rotation = state.MarkerKeyRotation;
        if (rotation is not null)
        {
            rotation = MarkerKeyRotationCoordinator.Acknowledge(rotation, heartbeat, now);
            IReadOnlyList<WriterHeartbeat> currentWriters = writers.Values.ToArray();
            if (rotation.Phase == MarkerKeyRotationPhase.Staged)
            {
                _ = MarkerKeyRotationCoordinator.TryBeginDrain(rotation, currentWriters, now, out rotation);
            }

            if (rotation.Phase == MarkerKeyRotationPhase.Draining)
            {
                _ = MarkerKeyRotationCoordinator.TryActivate(rotation, currentWriters, now, now, out rotation);
            }
        }

        AccessTelemetryLifecycleActorState updated = state with
        {
            Writers = writers,
            MarkerKeyRotation = rotation,
            ActiveMarkerKeyGeneration = rotation?.ActiveGeneration ?? activeGeneration,
            StagedMarkerKeyGeneration = rotation?.StagedGeneration,
        };
        await StateManager.SetStateAsync(StateName, updated).ConfigureAwait(false);
        return new WriterHeartbeatResponse
        {
            Accepted = true,
            Reason = AccessTelemetryReason.None,
            ActiveGeneration = updated.ActiveMarkerKeyGeneration!,
            StagedGeneration = updated.StagedMarkerKeyGeneration,
        };
    }

    /// <inheritdoc/>
    public async Task PurgeAsync()
    {
        EnsureGlobalActor();
        DateTimeOffset trustedNow = await GetTrustedNowAsync().ConfigureAwait(false);
        AccessTelemetryPurgeResult result = await _processor.PurgeAsync(trustedNow, CancellationToken.None).ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordReminder(succeeded: true);
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        AccessTelemetryLifecycleActorState updated = state with
        {
            LastPurgeUnixMilliseconds = trustedNow.ToUnixTimeMilliseconds(),
            RetainedRecordCount = Math.Max(0, state.RetainedRecordCount - result.VerifiedAbsent),
            ReminderSequence = state.ReminderSequence + 1,
            ExpiryMinuteCursor = result.LastExpiryMinute ?? state.ExpiryMinuteCursor,
            ExpiryShardCursor = result.LastExpiryShard ?? state.ExpiryShardCursor,
            Health = _processor.Health,
            HealthReason = _processor.HealthReason,
        };
        await StateManager.SetStateAsync(StateName, updated).ConfigureAwait(false);
        AccessTelemetryOptions options = _optionsProvider.Current;
        _ = await RegisterReminderAsync(
            PurgeReminderName,
            [],
            result.HasMore ? TimeSpan.FromMilliseconds(Random.Shared.Next(25, 101)) : options.PurgeInterval,
            options.PurgeInterval).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryInspectionResponse> InspectAsync()
    {
        EnsureGlobalActor();
        _ = await GetTrustedNowAsync().ConfigureAwait(false);
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordPhysicalEvidence(present: false);
        return new AccessTelemetryInspectionResponse
        {
            Health = state.Health == AccessTelemetryHealthState.Unhealthy ? state.Health : _processor.Health,
            Reason = state.HealthReason != AccessTelemetryReason.None ? state.HealthReason : _processor.HealthReason,
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
        long now = (await GetTrustedNowAsync().ConfigureAwait(false)).ToUnixTimeMilliseconds();
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
        DateTimeOffset trustedNow = await GetTrustedNowAsync().ConfigureAwait(false);
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        if (state.MarkerKeyRotation is null)
        {
            return;
        }

        MarkerKeyRotationState acknowledged = MarkerKeyRotationCoordinator.Acknowledge(
            state.MarkerKeyRotation,
            heartbeat,
            trustedNow.ToUnixTimeMilliseconds());
        await StateManager.SetStateAsync(StateName, state with { MarkerKeyRotation = acknowledged }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> TryActivateMarkerKeyAsync(long finalOldKeyWriteUnixMilliseconds)
    {
        EnsureGlobalActor();
        DateTimeOffset trustedNow = await GetTrustedNowAsync().ConfigureAwait(false);
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        if (state.MarkerKeyRotation is null)
        {
            return false;
        }

        long now = trustedNow.ToUnixTimeMilliseconds();
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
        DateTimeOffset trustedNow = await GetTrustedNowAsync().ConfigureAwait(false);
        AccessTelemetryOptions options = _optionsProvider.Current;
        AccessTelemetryLifecycleActorState state = await GetStateAsync().ConfigureAwait(false);
        int retentionSeconds = checked((int)(options.Retention ?? AccessTelemetryOptions.DefaultRetention).TotalSeconds);
        LifecycleConfigurationEpoch configuration = new()
        {
            Epoch = options.ConfigurationEpoch,
            SchemaVersion = options.SchemaVersion,
            ComponentProfileHash = options.ComponentProfileHash,
            RetentionSeconds = retentionSeconds,
            MarkerKeyGeneration = options.MarkerKeyGeneration,
        };
        bool configurationChanged = state.Configuration is null ||
            !string.Equals(state.Configuration.Epoch, configuration.Epoch, StringComparison.Ordinal) ||
            state.Configuration.SchemaVersion != configuration.SchemaVersion ||
            !string.Equals(state.Configuration.ComponentProfileHash, configuration.ComponentProfileHash, StringComparison.Ordinal) ||
            state.Configuration.RetentionSeconds != configuration.RetentionSeconds ||
            !string.Equals(state.Configuration.MarkerKeyGeneration, configuration.MarkerKeyGeneration, StringComparison.Ordinal);
        string activeGeneration = state.ActiveMarkerKeyGeneration ?? options.MarkerKeyGeneration;
        MarkerKeyRotationState? rotation = state.MarkerKeyRotation;
        if (!string.Equals(activeGeneration, options.MarkerKeyGeneration, StringComparison.Ordinal))
        {
            MarkerKeyRotationState current = rotation ?? new MarkerKeyRotationState { ActiveGeneration = activeGeneration };
            rotation = MarkerKeyRotationCoordinator.Stage(
                current,
                options.MarkerKeyGeneration,
                state.Writers.Values.ToArray(),
                trustedNow.ToUnixTimeMilliseconds());
        }

        await StateManager.SetStateAsync(
            StateName,
            state with
            {
                Configuration = configuration,
                ActiveMarkerKeyGeneration = activeGeneration,
                StagedMarkerKeyGeneration = rotation?.StagedGeneration,
                MarkerKeyRotation = rotation,
                PhysicalReclamationEvidenceId = options.PhysicalReclamationEvidenceId,
                Health = configurationChanged ? AccessTelemetryHealthState.Healthy : state.Health,
                HealthReason = configurationChanged ? AccessTelemetryReason.None : state.HealthReason,
            }).ConfigureAwait(false);

        _ = await RegisterReminderAsync(
            PurgeReminderName,
            [],
            options.PurgeInterval,
            options.PurgeInterval).ConfigureAwait(false);
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

    private async Task<DateTimeOffset> GetTrustedNowAsync()
    {
        LifecycleClockEvidence evidence = await _clockEvidence.GetAsync(CancellationToken.None).ConfigureAwait(false);
        ClockAttestationValidationResult validation = _clockGate.Validate(
            evidence.Attestation,
            _optionsProvider.Current.LifecycleAppId,
            evidence.RequestingProcessEpoch,
            evidence.RequestingServiceInstanceId);
        if (!validation.IsValid || validation.TrustedUnixMilliseconds is null)
        {
            throw new AccessTelemetryContractException("clock_untrusted");
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(validation.TrustedUnixMilliseconds.Value);
    }

    private static bool IsValidHeartbeat(
        WriterHeartbeat heartbeat,
        AccessTelemetryOptions options,
        long? trustedUnixMilliseconds)
    {
        if (trustedUnixMilliseconds is null ||
            !string.Equals(heartbeat.DeploymentId, options.DeploymentId, StringComparison.Ordinal) ||
            !IsUlid(heartbeat.ServiceInstanceId) || !IsUlid(heartbeat.ProcessEpoch) ||
            !IsMarkerKeyGeneration(heartbeat.MarkerKeyGeneration) ||
            heartbeat.OldKeyQueueCount is < 0 or > AccessTelemetryOptions.MaximumQueueRecords)
        {
            return false;
        }

        long leaseDuration = heartbeat.LeaseExpiresAtUnixMilliseconds - trustedUnixMilliseconds.Value;
        return leaseDuration is > 0 and <= 30_000;
    }

    private static bool IsUlid(string value)
        => value.Length == 26 && value.All(static character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'H' or >= 'J' and <= 'N' or >= 'P' and <= 'T' or >= 'V' and <= 'Z');

    private static bool IsMarkerKeyGeneration(string value)
        => value.Length is >= 1 and <= 32 &&
            (value[0] is >= 'a' and <= 'z' or >= '0' and <= '9') &&
            value.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
}
