// <copyright file="AccessTelemetryCapabilityOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Immutable, secret-free result or pending state of one capability.</summary>
/// <param name="Outcome">The fixed outcome, with cancellation distinct from a false return.</param>
/// <param name="FailureCategory">The fixed exception category, or None.</param>
public sealed record AccessTelemetryCapabilityOutcome(string Outcome, string FailureCategory)
{
    private string _outcome = AccessTelemetryCapabilityDiagnosticValues.Bound(Outcome, AccessTelemetryCapabilityDiagnosticValues.Outcomes);
    private string _failureCategory = AccessTelemetryCapabilityDiagnosticValues.Bound(FailureCategory, AccessTelemetryCapabilityDiagnosticValues.FailureCategories);

    /// <summary>Gets the bounded outcome, including after deserialization or a with-expression.</summary>
    public string Outcome { get => _outcome; init => _outcome = AccessTelemetryCapabilityDiagnosticValues.Bound(value, AccessTelemetryCapabilityDiagnosticValues.Outcomes); }

    /// <summary>Gets the bounded failure category, never exception text.</summary>
    public string FailureCategory { get => _failureCategory; init => _failureCategory = AccessTelemetryCapabilityDiagnosticValues.Bound(value, AccessTelemetryCapabilityDiagnosticValues.FailureCategories); }
}
