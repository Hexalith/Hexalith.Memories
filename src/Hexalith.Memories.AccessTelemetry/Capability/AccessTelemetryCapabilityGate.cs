// <copyright file="AccessTelemetryCapabilityGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Evaluates every mandatory behavior for the exact configured profile.</summary>
internal static class AccessTelemetryCapabilityGate
{
    /// <summary>Evaluates a profile without inferring any capability from component type/name.</summary>
    public static AccessTelemetryCapabilityGateResult Evaluate(
        AccessTelemetryCapabilityProfile profile,
        string expectedProfileHash,
        bool production,
        bool allowAlpha,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(profile);
        bool passed = string.Equals(profile.ComponentProfileHash, expectedProfileHash, StringComparison.Ordinal) &&
            profile.ValidUntilUtc > now &&
            profile.ExactVersionPinned &&
            profile.DaprOnlyBoundary &&
            profile.StrongCrudAndEtags &&
            profile.MultiKeyTransactionsAndConflicts &&
            profile.ActorReactivationFailoverAndReminders &&
            profile.EffectivePerRecordTtl &&
            profile.RecordAndRequestBounds &&
            profile.TwoWriterThroughputDuringPurge &&
            profile.DeclaredDurabilityAndFailureBehavior &&
            profile.TenantIsolationAndEncryption &&
            profile.PhysicalCapacityEvidence &&
            profile.ReclamationEvidenceHooks &&
            !string.IsNullOrWhiteSpace(profile.CapacityEvidenceId) &&
            !string.IsNullOrWhiteSpace(profile.PhysicalReclamationEvidenceId) &&
            (!production || !profile.IsAlpha || allowAlpha);
        return passed
            ? new AccessTelemetryCapabilityGateResult(true, true, AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None)
            : new AccessTelemetryCapabilityGateResult(false, true, AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.CapabilityUnproven);
    }
}
