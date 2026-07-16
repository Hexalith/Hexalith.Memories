// <copyright file="RecoveryResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Stable localization key conventions for the recovery panel.
/// </summary>
/// <remarks>
/// Story 17.2 — every user-facing recovery string resolves through a key defined here so titles,
/// explanations, diagnostic labels, severity labels, affected-capability labels, action labels, and
/// assistive labels come from localization resources instead of component-side string building.
/// </remarks>
public static class RecoveryResourceKeys
{
    /// <summary>Accessible label for the recovery panel region.</summary>
    public const string PanelLabel = "Recovery_Panel_Label";

    /// <summary>Label preceding the diagnostic clue code.</summary>
    public const string DiagnosticClueLabel = "Recovery_DiagnosticClue_Label";

    /// <summary>Column header / accessible prefix for the severity badge.</summary>
    public const string SeverityLabel = "Recovery_Severity_Label";

    /// <summary>Column header / accessible prefix for the affected-capability badge.</summary>
    public const string CapabilityLabel = "Recovery_Capability_Label";

    /// <summary>Heading for the primary recovery action.</summary>
    public const string PrimaryActionLabel = "Recovery_PrimaryAction_Label";

    /// <summary>Heading for the secondary recovery actions group.</summary>
    public const string SecondaryActionsLabel = "Recovery_SecondaryActions_Label";

    /// <summary>Heading for the secondary risk markers group.</summary>
    public const string RiskMarkersLabel = "Recovery_RiskMarkers_Label";

    /// <summary>Shown when the packet exposes no recovery action.</summary>
    public const string NoAction = "Recovery_NoAction";

    /// <summary>Reason shown on actions disabled because the current scope is unauthorized.</summary>
    public const string DisabledAuthRequired = "Recovery_Disabled_AuthRequired";

    /// <summary>Label preceding the tenant identifier in action context.</summary>
    public const string TenantLabel = "Recovery_Tenant_Label";

    /// <summary>Label preceding the case identifier in action context.</summary>
    public const string CaseLabel = "Recovery_Case_Label";

    /// <summary>Label preceding the action target in action context.</summary>
    public const string TargetLabel = "Recovery_Target_Label";

    /// <summary>Fallback shown when the case identifier is absent (tenant-wide scope).</summary>
    public const string TenantScope = "Recovery_TenantScope";

    /// <summary>Label preceding the list of omitted detail groups for compressed evidence.</summary>
    public const string OmittedDetailsLabel = "Recovery_OmittedDetails_Label";

    /// <summary>Label preceding an expansion-handle guidance entry.</summary>
    public const string ExpansionLabel = "Recovery_Expansion_Label";

    /// <summary>Builds the title key for a recovery state.</summary>
    /// <param name="kind">The recovery state.</param>
    /// <returns>The localization key.</returns>
    public static string Title(RecoveryStateKind kind) => $"Recovery_{kind}_Title";

    /// <summary>Builds the explanation key for a recovery state.</summary>
    /// <param name="kind">The recovery state.</param>
    /// <returns>The localization key.</returns>
    public static string Explanation(RecoveryStateKind kind) => $"Recovery_{kind}_Explanation";

    /// <summary>Builds the affected-capability key.</summary>
    /// <param name="capability">The capability token.</param>
    /// <returns>The localization key.</returns>
    public static string Capability(RecoveryCapability capability) => $"Recovery_Capability_{capability}";

    /// <summary>Builds the severity label key.</summary>
    /// <param name="severity">The severity.</param>
    /// <returns>The localization key.</returns>
    public static string Severity(RecoverySeverity severity) => $"Recovery_Severity_{severity}";

    /// <summary>Builds the action label key for a recovery kind.</summary>
    /// <param name="kind">The recovery kind.</param>
    /// <returns>The localization key.</returns>
    public static string Action(EvidencePacketRecoveryKind kind) => $"Recovery_Action_{kind}";

    /// <summary>Builds the risk-marker label key for a marker code.</summary>
    /// <param name="code">The marker code (for example <c>stale</c>).</param>
    /// <returns>The localization key.</returns>
    public static string RiskMarker(string code) => $"Recovery_RiskMarker_{code}";
}
