// <copyright file="FormValidationTraceability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

using Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Field-level traceability table from every <see cref="FormValidationCode"/> to its dispatch
/// classification, display severity, localization key, and the named Evidence Packet contract fields that
/// justify it.
/// </summary>
/// <remarks>
/// Story 17.3 (Task 0, AC1) — this table is the single source of truth consumed by
/// <see cref="ContractAwareFormValidator"/>. Every produced message resolves to exactly one row here, so a
/// rendered validation error always traces back to a named contract source instead of a web-only rule.
/// </remarks>
public static class FormValidationTraceability
{
    /// <summary>Gets the traceability rows, one per validation code.</summary>
    public static IReadOnlyList<FormValidationTrace> Entries { get; } =
    [
        Trace(
            FormValidationCode.TenantRequired,
            FormMessageClassification.Blocking,
            InteractionSeverity.Critical,
            "EvidencePacketScope.TenantId"),
        Trace(
            FormValidationCode.CaseRequired,
            FormMessageClassification.Blocking,
            InteractionSeverity.Warning,
            "EvidencePacketScope.CaseId"),
        Trace(
            FormValidationCode.FieldRequired,
            FormMessageClassification.Blocking,
            InteractionSeverity.Warning,
            "MemoriesFormField.Value"),
        Trace(
            FormValidationCode.UnknownEnumValue,
            FormMessageClassification.Blocking,
            InteractionSeverity.Warning,
            "Contracts.V1 enum tokens"),
        Trace(
            FormValidationCode.ValueOutOfRange,
            FormMessageClassification.Blocking,
            InteractionSeverity.Warning,
            "MemoriesFormField.Minimum",
            "MemoriesFormField.Maximum"),
        Trace(
            FormValidationCode.UnauthorizedScope,
            FormMessageClassification.Blocking,
            InteractionSeverity.Critical,
            "EvidencePacketScope.IsolationStatus"),
        Trace(
            FormValidationCode.TenantChange,
            FormMessageClassification.Acknowledgement,
            InteractionSeverity.Warning,
            "EvidencePacketScope.TenantId"),
        Trace(
            FormValidationCode.ScopeBroadened,
            FormMessageClassification.Acknowledgement,
            InteractionSeverity.Caution,
            "EvidencePacketScope.CaseId"),
        Trace(
            FormValidationCode.DangerousChange,
            FormMessageClassification.Acknowledgement,
            InteractionSeverity.Warning,
            "MemoriesFormRequest.FormKind"),
    ];

    /// <summary>Gets the traceability row for a validation code.</summary>
    /// <param name="code">The validation code.</param>
    /// <returns>The matching <see cref="FormValidationTrace"/>.</returns>
    public static FormValidationTrace For(FormValidationCode code)
        => Entries.Single(e => e.Code == code);

    private static FormValidationTrace Trace(
        FormValidationCode code,
        FormMessageClassification classification,
        InteractionSeverity severity,
        params string[] contractSources)
        => new(code, classification, severity, FormResourceKeys.Message(code), contractSources);
}
