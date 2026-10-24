// <copyright file="AccessTelemetryCapabilityDiagnostics.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Publishes immutable observations and ignores all completion callbacks after a terminal snapshot.</summary>
internal sealed class AccessTelemetryCapabilityDiagnostics
{
    private readonly object _sync = new();
    private AccessTelemetryCapabilityDiagnosticSnapshot _current = Initial("NotStarted", "None");
    private long _attempt;

    /// <summary>Gets the current process-local observation.</summary>
    public AccessTelemetryCapabilityDiagnosticSnapshot Current => Volatile.Read(ref _current);

    /// <summary>Begins a diagnostic generation without changing probe admission.</summary>
    public long Begin()
    {
        lock (_sync)
        {
            _current = Initial("Running", "None");
            return ++_attempt;
        }
    }

    /// <summary>Records a preflight refusal; already completed outcomes remain visible.</summary>
    public void Refuse(string reason)
    {
        lock (_sync)
        {
            _current = _current with { Disposition = "PreflightRefused", PreflightReason = reason };
        }
    }

    /// <summary>Updates only a known capability in the active nonterminal generation.</summary>
    public void Observe(long attempt, string name, string outcome, string category = "None")
    {
        lock (_sync)
        {
            if (attempt != _attempt || _current.Disposition != "Running" || !AccessTelemetryCapabilityDiagnosticValues.Capabilities.Contains(name)) return;
            var capabilities = new Dictionary<string, AccessTelemetryCapabilityOutcome>(_current.Capabilities, StringComparer.Ordinal)
            {
                [name] = new(outcome, category),
            };
            _current = _current with { Capabilities = capabilities };
        }
    }

    /// <summary>Freezes terminal evidence; pending operations remain Running rather than being described as false returns.</summary>
    public void Complete(long attempt, string disposition)
    {
        lock (_sync)
        {
            if (attempt == _attempt && _current.Disposition == "Running") _current = _current with { Disposition = disposition };
        }
    }

    private static AccessTelemetryCapabilityDiagnosticSnapshot Initial(string disposition, string reason)
        => new(disposition, reason, AccessTelemetryCapabilityDiagnosticValues.Capabilities.ToDictionary(
            name => name, _ => new AccessTelemetryCapabilityOutcome("NotStarted", "None"), StringComparer.Ordinal));
}
