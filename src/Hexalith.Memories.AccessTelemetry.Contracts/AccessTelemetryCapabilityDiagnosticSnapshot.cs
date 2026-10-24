// <copyright file="AccessTelemetryCapabilityDiagnosticSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Collections.ObjectModel;

/// <summary>Immutable process-local observation; it carries no authority or admission decision.</summary>
/// <param name="Disposition">The bounded runner disposition.</param>
/// <param name="PreflightReason">The bounded refusal reason, or None.</param>
/// <param name="Capabilities">The owned fixed-name outcomes; unknown names are discarded.</param>
public sealed record AccessTelemetryCapabilityDiagnosticSnapshot(
    string Disposition,
    string PreflightReason,
    IReadOnlyDictionary<string, AccessTelemetryCapabilityOutcome> Capabilities)
{
    private string _disposition = AccessTelemetryCapabilityDiagnosticValues.Bound(Disposition, AccessTelemetryCapabilityDiagnosticValues.Dispositions);
    private string _preflightReason = AccessTelemetryCapabilityDiagnosticValues.Bound(PreflightReason, AccessTelemetryCapabilityDiagnosticValues.PreflightReasons);
    private IReadOnlyDictionary<string, AccessTelemetryCapabilityOutcome> _capabilities = Copy(Capabilities);

    /// <summary>Gets the bounded runner disposition.</summary>
    public string Disposition { get => _disposition; init => _disposition = AccessTelemetryCapabilityDiagnosticValues.Bound(value, AccessTelemetryCapabilityDiagnosticValues.Dispositions); }

    /// <summary>Gets the bounded preflight refusal reason.</summary>
    public string PreflightReason { get => _preflightReason; init => _preflightReason = AccessTelemetryCapabilityDiagnosticValues.Bound(value, AccessTelemetryCapabilityDiagnosticValues.PreflightReasons); }

    /// <summary>Gets an owned immutable dictionary, including after deserialization or with-expressions.</summary>
    public IReadOnlyDictionary<string, AccessTelemetryCapabilityOutcome> Capabilities { get => _capabilities; init => _capabilities = Copy(value); }

    private static IReadOnlyDictionary<string, AccessTelemetryCapabilityOutcome> Copy(IReadOnlyDictionary<string, AccessTelemetryCapabilityOutcome>? source)
    {
        var owned = new SortedDictionary<string, AccessTelemetryCapabilityOutcome>(StringComparer.Ordinal);
        foreach (string name in AccessTelemetryCapabilityDiagnosticValues.Capabilities)
        {
            if (source is not null && source.TryGetValue(name, out AccessTelemetryCapabilityOutcome? outcome) && outcome is not null)
            {
                owned[name] = outcome with { };
            }
        }

        return new ReadOnlyDictionary<string, AccessTelemetryCapabilityOutcome>(owned);
    }
}
