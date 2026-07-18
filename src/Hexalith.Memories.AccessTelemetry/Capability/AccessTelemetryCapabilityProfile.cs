// <copyright file="AccessTelemetryCapabilityProfile.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Behavioral proof for one exact configured Dapr component profile.</summary>
internal sealed record AccessTelemetryCapabilityProfile
{
    /// <summary>Gets the exact lowercase component-profile SHA-256.</summary>
    public required string ComponentProfileHash { get; init; }

    /// <summary>Gets whether the component version is pinned exactly.</summary>
    public required bool ExactVersionPinned { get; init; }

    /// <summary>Gets whether application code remained behind Dapr APIs.</summary>
    public required bool DaprOnlyBoundary { get; init; }

    /// <summary>Gets strong CRUD and ETag proof.</summary>
    public required bool StrongCrudAndEtags { get; init; }

    /// <summary>Gets multi-key transaction and conflict proof.</summary>
    public required bool MultiKeyTransactionsAndConflicts { get; init; }

    /// <summary>Gets actor lifecycle and durable reminder proof.</summary>
    public required bool ActorReactivationFailoverAndReminders { get; init; }

    /// <summary>Gets observed effective per-record TTL proof.</summary>
    public required bool EffectivePerRecordTtl { get; init; }

    /// <summary>Gets 1,024-byte record and 256-record/one-MiB request proof.</summary>
    public required bool RecordAndRequestBounds { get; init; }

    /// <summary>Gets two-writer throughput proof while purge runs.</summary>
    public required bool TwoWriterThroughputDuringPurge { get; init; }

    /// <summary>Gets declared durability and failure-behavior proof.</summary>
    public required bool DeclaredDurabilityAndFailureBehavior { get; init; }

    /// <summary>Gets tenant isolation and encryption proof.</summary>
    public required bool TenantIsolationAndEncryption { get; init; }

    /// <summary>Gets physical capacity evidence.</summary>
    public required bool PhysicalCapacityEvidence { get; init; }

    /// <summary>Gets reclamation evidence-hook availability without claiming reclamation.</summary>
    public required bool ReclamationEvidenceHooks { get; init; }

    /// <summary>Gets whether the exact component is alpha.</summary>
    public bool IsAlpha { get; init; }

    /// <summary>Gets the capacity evidence ID.</summary>
    public required string CapacityEvidenceId { get; init; }

    /// <summary>Gets the physical-reclamation evidence hook ID.</summary>
    public required string PhysicalReclamationEvidenceId { get; init; }

    /// <summary>Gets when this exact proof becomes stale.</summary>
    public required DateTimeOffset ValidUntilUtc { get; init; }

    /// <summary>Returns a copy with one named capability set for table-driven tests/probes.</summary>
    public AccessTelemetryCapabilityProfile WithCapability(string property, bool value)
        => property switch
        {
            nameof(DaprOnlyBoundary) => this with { DaprOnlyBoundary = value },
            nameof(StrongCrudAndEtags) => this with { StrongCrudAndEtags = value },
            nameof(MultiKeyTransactionsAndConflicts) => this with { MultiKeyTransactionsAndConflicts = value },
            nameof(ActorReactivationFailoverAndReminders) => this with { ActorReactivationFailoverAndReminders = value },
            nameof(EffectivePerRecordTtl) => this with { EffectivePerRecordTtl = value },
            nameof(RecordAndRequestBounds) => this with { RecordAndRequestBounds = value },
            nameof(TwoWriterThroughputDuringPurge) => this with { TwoWriterThroughputDuringPurge = value },
            nameof(DeclaredDurabilityAndFailureBehavior) => this with { DeclaredDurabilityAndFailureBehavior = value },
            nameof(TenantIsolationAndEncryption) => this with { TenantIsolationAndEncryption = value },
            nameof(PhysicalCapacityEvidence) => this with { PhysicalCapacityEvidence = value },
            nameof(ReclamationEvidenceHooks) => this with { ReclamationEvidenceHooks = value },
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, "Unknown capability property."),
        };
}
