// <copyright file="FormResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// Stable localization key conventions for the contract-aware form surface.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — every user-facing form string (section labels, scope labels, validation messages,
/// acknowledgement, and submit state) resolves through a key defined here so the form uses the same
/// localization path as the surrounding FrontComposer UI instead of component-side string building.
/// </remarks>
public static class FormResourceKeys
{
    /// <summary>Accessible label for the form region.</summary>
    public const string PanelLabel = "Form_Panel_Label";

    /// <summary>Heading for the scope-first section.</summary>
    public const string ScopeSectionLabel = "Form_ScopeSection_Label";

    /// <summary>Label preceding the tenant scope value.</summary>
    public const string TenantLabel = "Form_Tenant_Label";

    /// <summary>Label preceding the case scope value.</summary>
    public const string CaseLabel = "Form_Case_Label";

    /// <summary>Fallback shown when the case identifier is absent (tenant-wide scope).</summary>
    public const string TenantScope = "Form_TenantScope";

    /// <summary>Accessible label for the validation summary region.</summary>
    public const string ValidationSummaryLabel = "Form_ValidationSummary_Label";

    /// <summary>Label for the dangerous-change acknowledgement control.</summary>
    public const string AcknowledgeLabel = "Form_Acknowledge_Label";

    /// <summary>Label for the submit control.</summary>
    public const string SubmitLabel = "Form_Submit_Label";

    /// <summary>Reason shown when dispatch is blocked by validation errors.</summary>
    public const string DispatchBlockedLabel = "Form_DispatchBlocked_Label";

    /// <summary>Status shown when the form is ready to dispatch.</summary>
    public const string DispatchReadyLabel = "Form_DispatchReady_Label";

    /// <summary>Shown when there are no validation messages.</summary>
    public const string NoMessages = "Form_NoMessages";

    /// <summary>Builds the message key for a validation code.</summary>
    /// <param name="code">The validation code.</param>
    /// <returns>The localization key.</returns>
    public static string Message(FormValidationCode code) => $"Form_Msg_{code}";
}
