// <copyright file="AccessTelemetryCapabilityDiagnosticValues.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Collections.Frozen;

/// <summary>Fixed secret-free vocabulary for observational capability diagnostics.</summary>
public static class AccessTelemetryCapabilityDiagnosticValues
{
    /// <summary>Gets the ten allowed capability names, independently of local admission requirements.</summary>
    public static IReadOnlySet<string> Capabilities { get; } = new[]
    {
        "StrongCrudAndEtags",
        "MultiKeyTransactionsAndConflicts",
        "ActorReactivationFailoverAndReminders",
        "EffectivePerRecordTtl",
        "RecordAndRequestBounds",
        "TwoWriterThroughputDuringPurge",
        "DeclaredDurabilityAndFailureBehavior",
        "TenantIsolationAndEncryption",
        "PhysicalCapacityEvidence",
        "ReclamationEvidenceHooks",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Gets the bounded runner dispositions.</summary>
    public static IReadOnlySet<string> Dispositions { get; } = new[] { "NotStarted", "PreflightRefused", "Running", "Passed", "Failed", "Timeout", "Canceled" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Gets the bounded preflight refusal reasons; no binding material is included.</summary>
    public static IReadOnlySet<string> PreflightReasons { get; } = new[] { "None", "ContextChanged", "LocalModeRequired", "Production", "VersionUnpinned", "BoundaryInvalid", "Expired", "LocalProfileInvalid" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Gets the bounded per-capability outcomes.</summary>
    public static IReadOnlySet<string> Outcomes { get; } = new[] { "NotStarted", "Running", "Passed", "ReturnedFalse", "Exception", "Canceled" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Gets bounded failure categories, without exception detail.</summary>
    public static IReadOnlySet<string> FailureCategories { get; } = new[] { "None", "Timeout", "Canceled", "RemoteDenied", "Transport", "DaprApi", "Format", "Other" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Replaces an unrecognized value with a fixed incomplete-evidence marker.</summary>
    public static string Bound(string? value, IReadOnlySet<string> allowed)
        => value is not null && allowed.Contains(value) ? value : "Unobserved";
}
