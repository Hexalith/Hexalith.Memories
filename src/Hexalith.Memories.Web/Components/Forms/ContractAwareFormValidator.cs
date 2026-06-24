// <copyright file="ContractAwareFormValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

using System.Globalization;

using Hexalith.Memories.Web.Components.Evidence;

/// <summary>
/// Pure, deterministic contract-aware validation for Story 17.3 forms.
/// </summary>
/// <remarks>
/// <para>Story 17.3 (AC1) — validates a <see cref="MemoriesFormRequest"/> against typed contract concerns:
/// tenant/case scope presence, authorization/isolation status, contract enum token membership, numeric
/// range bounds, and dangerous tenant/case scope transitions. It never bypasses the command lifecycle,
/// authorization, or tenant context; it only decides whether a submission is consistent and whether a
/// dangerous-but-valid change must be acknowledged first.</para>
/// <para>Every message traces to a <see cref="FormValidationTraceability"/> row, so validation stays bound
/// to named <c>Contracts.V1</c> fields. Messages carry localization keys only — no raw field values — so
/// they are safe across visible text, accessible names, copied text, diagnostics, logs, and snapshots.</para>
/// </remarks>
public static class ContractAwareFormValidator
{
    /// <summary>Validates a form request.</summary>
    /// <param name="request">The proposed form submission.</param>
    /// <returns>The contract-aware validation result, including the dispatch gate.</returns>
    public static FormValidationResult Validate(MemoriesFormRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<FormValidationMessage> messages = [];
        IReadOnlyList<MemoriesFormField> ordered = OrderScopeFirst(request.Fields);

        // 1. Authorization outranks everything: an unauthorized or untrusted isolation status means the UI
        //    must not let the change reach the command lifecycle at all.
        if (EvidenceDisplay.IsRestrictiveScope(request.IsolationStatus))
        {
            messages.Add(Message(string.Empty, FormValidationCode.UnauthorizedScope));
        }

        // 2. Tenant scope is mandatory for every form. The case may be tenant-wide (null).
        if (string.IsNullOrWhiteSpace(request.RequestedTenantId))
        {
            messages.Add(Message(ScopeFieldKey(ordered, MemoriesFormFieldKind.TenantScope), FormValidationCode.TenantRequired));
        }

        // 3. Dangerous scope transitions relative to the current scope require explicit acknowledgement.
        if (IsTenantChange(request))
        {
            messages.Add(Message(ScopeFieldKey(ordered, MemoriesFormFieldKind.TenantScope), FormValidationCode.TenantChange));
        }

        if (IsScopeBroadened(request))
        {
            messages.Add(Message(ScopeFieldKey(ordered, MemoriesFormFieldKind.CaseScope), FormValidationCode.ScopeBroadened));
        }

        // 4. Field-level validation against typed contract constraints.
        foreach (MemoriesFormField field in request.Fields)
        {
            ValidateField(field, messages);
        }

        bool hasErrors = messages.Any(static m => m.Classification == FormMessageClassification.Blocking);
        bool requiresAck = messages.Any(static m => m.Classification == FormMessageClassification.Acknowledgement);
        bool canDispatch = !hasErrors && (!requiresAck || request.Acknowledged);

        IReadOnlyList<string> contractSources =
        [
            .. messages
                .SelectMany(static m => FormValidationTraceability.For(m.Code).ContractSources)
                .Distinct(StringComparer.Ordinal),
        ];

        return new FormValidationResult(ordered, messages, requiresAck, hasErrors, canDispatch, contractSources);
    }

    private static void ValidateField(MemoriesFormField field, List<FormValidationMessage> messages)
    {
        switch (field.Kind)
        {
            case MemoriesFormFieldKind.TenantScope:
                if (field.Required && string.IsNullOrWhiteSpace(field.Value))
                {
                    messages.Add(Message(field.FieldKey, FormValidationCode.TenantRequired));
                }

                break;

            case MemoriesFormFieldKind.CaseScope:
                if (field.Required && string.IsNullOrWhiteSpace(field.Value))
                {
                    messages.Add(Message(field.FieldKey, FormValidationCode.CaseRequired));
                }

                break;

            case MemoriesFormFieldKind.RequiredText:
                if (string.IsNullOrWhiteSpace(field.Value))
                {
                    messages.Add(Message(field.FieldKey, FormValidationCode.FieldRequired));
                }

                break;

            case MemoriesFormFieldKind.ContractEnum:
                ValidateEnum(field, messages);
                break;

            case MemoriesFormFieldKind.NumericRange:
                ValidateRange(field, messages);
                break;

            case MemoriesFormFieldKind.DangerousToggle:
                if (IsToggledOn(field.Value))
                {
                    messages.Add(Message(field.FieldKey, FormValidationCode.DangerousChange));
                }

                break;

            case MemoriesFormFieldKind.OptionalText:
            default:
                break;
        }
    }

    private static void ValidateEnum(MemoriesFormField field, List<FormValidationMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(field.Value))
        {
            if (field.Required)
            {
                messages.Add(Message(field.FieldKey, FormValidationCode.FieldRequired));
            }

            return;
        }

        IReadOnlyList<string> allowed = field.AllowedTokens ?? [];

        // Unknown or future contract tokens are rejected as a contract-boundary error rather than coerced
        // into a known value. The offending value is never echoed back into the message.
        bool known = allowed.Any(token => string.Equals(token, field.Value, StringComparison.OrdinalIgnoreCase));
        if (!known)
        {
            messages.Add(Message(field.FieldKey, FormValidationCode.UnknownEnumValue));
        }
    }

    private static void ValidateRange(MemoriesFormField field, List<FormValidationMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(field.Value))
        {
            if (field.Required)
            {
                messages.Add(Message(field.FieldKey, FormValidationCode.FieldRequired));
            }

            return;
        }

        bool parsed = double.TryParse(
            field.Value,
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out double value);

        if (!parsed
            || !double.IsFinite(value)
            || (field.Minimum is double min && value < min)
            || (field.Maximum is double max && value > max))
        {
            messages.Add(Message(field.FieldKey, FormValidationCode.ValueOutOfRange));
        }
    }

    private static bool IsTenantChange(MemoriesFormRequest request)
        => !string.IsNullOrWhiteSpace(request.RequestedTenantId)
            && !string.IsNullOrWhiteSpace(request.CurrentTenantId)
            && !string.Equals(request.RequestedTenantId, request.CurrentTenantId, StringComparison.Ordinal);

    private static bool IsScopeBroadened(MemoriesFormRequest request)
        => !string.IsNullOrWhiteSpace(request.CurrentCaseId)
            && string.IsNullOrWhiteSpace(request.RequestedCaseId);

    private static bool IsToggledOn(string? value)
        => bool.TryParse(value, out bool result) && result;

    private static IReadOnlyList<MemoriesFormField> OrderScopeFirst(IReadOnlyList<MemoriesFormField> fields)
    {
        List<MemoriesFormField> tenant = [];
        List<MemoriesFormField> @case = [];
        List<MemoriesFormField> rest = [];

        foreach (MemoriesFormField field in fields)
        {
            switch (field.Kind)
            {
                case MemoriesFormFieldKind.TenantScope:
                    tenant.Add(field);
                    break;
                case MemoriesFormFieldKind.CaseScope:
                    @case.Add(field);
                    break;
                default:
                    rest.Add(field);
                    break;
            }
        }

        return [.. tenant, .. @case, .. rest];
    }

    private static string ScopeFieldKey(IReadOnlyList<MemoriesFormField> fields, MemoriesFormFieldKind kind)
    {
        foreach (MemoriesFormField field in fields)
        {
            if (field.Kind == kind)
            {
                return field.FieldKey;
            }
        }

        return string.Empty;
    }

    private static FormValidationMessage Message(string fieldKey, FormValidationCode code)
    {
        FormValidationTrace trace = FormValidationTraceability.For(code);
        return new FormValidationMessage(fieldKey, code, trace.Classification, trace.Severity, trace.MessageKey);
    }
}
