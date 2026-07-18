// <copyright file="MarkerKeyRotationCoordinator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Pure staged/acknowledge/drain/activate marker-key rotation protocol.</summary>
internal static class MarkerKeyRotationCoordinator
{
    private static readonly TimeSpan OldKeyRetention =
        TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1) + TimeSpan.FromMinutes(15);

    /// <summary>Stages a new generation and freezes the current live writer snapshot.</summary>
    public static MarkerKeyRotationState Stage(
        MarkerKeyRotationState current,
        string newGeneration,
        IReadOnlyList<WriterHeartbeat> writers,
        long nowUnixMilliseconds)
    {
        if (current.Phase != MarkerKeyRotationPhase.Stable ||
            string.Equals(current.ActiveGeneration, newGeneration, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Marker-key rotation cannot be staged from the current phase.");
        }

        return current with
        {
            Phase = MarkerKeyRotationPhase.Staged,
            StagedGeneration = newGeneration,
            FrozenOldGeneration = current.ActiveGeneration,
            RequiredWriterKeys = writers
                .Where(writer => writer.LeaseExpiresAtUnixMilliseconds > nowUnixMilliseconds)
                .Select(GetWriterKey)
                .ToHashSet(StringComparer.Ordinal),
            AcknowledgedWriterKeys = new HashSet<string>(StringComparer.Ordinal),
        };
    }

    /// <summary>Records one live writer acknowledgement of the staged generation.</summary>
    public static MarkerKeyRotationState Acknowledge(
        MarkerKeyRotationState state,
        WriterHeartbeat heartbeat,
        long nowUnixMilliseconds)
    {
        if (state.Phase != MarkerKeyRotationPhase.Staged ||
            heartbeat.LeaseExpiresAtUnixMilliseconds <= nowUnixMilliseconds ||
            !string.Equals(heartbeat.MarkerKeyGeneration, state.StagedGeneration, StringComparison.Ordinal))
        {
            return state;
        }

        HashSet<string> acknowledgements = state.AcknowledgedWriterKeys.ToHashSet(StringComparer.Ordinal);
        _ = acknowledgements.Add(GetWriterKey(heartbeat));
        return state with { AcknowledgedWriterKeys = acknowledgements };
    }

    /// <summary>Begins draining only after every still-live staging writer acknowledges.</summary>
    public static bool TryBeginDrain(
        MarkerKeyRotationState state,
        IReadOnlyList<WriterHeartbeat> writers,
        long nowUnixMilliseconds,
        out MarkerKeyRotationState updated)
    {
        Dictionary<string, WriterHeartbeat> current = writers.ToDictionary(GetWriterKey, StringComparer.Ordinal);
        bool ready = state.Phase == MarkerKeyRotationPhase.Staged && state.RequiredWriterKeys.All(key =>
            state.AcknowledgedWriterKeys.Contains(key) ||
            !current.TryGetValue(key, out WriterHeartbeat? writer) ||
            writer.LeaseExpiresAtUnixMilliseconds <= nowUnixMilliseconds);
        updated = ready ? state with { Phase = MarkerKeyRotationPhase.Draining } : state;
        return ready;
    }

    /// <summary>Activates after every still-live old-generation queue drains.</summary>
    public static bool TryActivate(
        MarkerKeyRotationState state,
        IReadOnlyList<WriterHeartbeat> writers,
        long nowUnixMilliseconds,
        long finalOldKeyWriteUnixMilliseconds,
        out MarkerKeyRotationState updated)
    {
        bool drained = state.Phase == MarkerKeyRotationPhase.Draining && writers.All(writer =>
            writer.LeaseExpiresAtUnixMilliseconds <= nowUnixMilliseconds || writer.OldKeyQueueCount == 0);
        if (!drained || state.StagedGeneration is null)
        {
            updated = state;
            return false;
        }

        updated = state with
        {
            Phase = MarkerKeyRotationPhase.Stable,
            ActiveGeneration = state.StagedGeneration,
            StagedGeneration = null,
            FinalOldKeyWriteUnixMilliseconds = finalOldKeyWriteUnixMilliseconds,
            OldGenerationRetireAfterUnixMilliseconds =
                finalOldKeyWriteUnixMilliseconds + (long)OldKeyRetention.TotalMilliseconds,
            RequiredWriterKeys = new HashSet<string>(StringComparer.Ordinal),
            AcknowledgedWriterKeys = new HashSet<string>(StringComparer.Ordinal),
        };
        return true;
    }

    private static string GetWriterKey(WriterHeartbeat heartbeat)
        => $"{heartbeat.ServiceInstanceId}/{heartbeat.ProcessEpoch}";
}
