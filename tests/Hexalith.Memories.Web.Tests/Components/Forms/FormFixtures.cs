// <copyright file="FormFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Forms;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// Story 17.3 — reusable contract-aware form fixtures covering valid, invalid, dangerous, cross-tenant,
/// and unauthorized submissions.
/// </summary>
internal static class FormFixtures
{
    public static MemoriesFormField Tenant(string? value = "tenant-a")
        => new("tenant", FormResourceKeys.TenantLabel, MemoriesFormFieldKind.TenantScope, value, Required: true);

    public static MemoriesFormField Case(string? value = "case-a", bool required = true)
        => new("case", FormResourceKeys.CaseLabel, MemoriesFormFieldKind.CaseScope, value, Required: required);

    public static MemoriesFormField RequiredText(string key = "query", string? value = "policy context")
        => new(key, "Form_Tenant_Label", MemoriesFormFieldKind.RequiredText, value);

    public static MemoriesFormField EnumField(string key, string? value, params string[] allowed)
        => new(key, "Form_Tenant_Label", MemoriesFormFieldKind.ContractEnum, value, AllowedTokens: allowed);

    public static MemoriesFormField Range(string key, string? value, double min, double max)
        => new(key, "Form_Tenant_Label", MemoriesFormFieldKind.NumericRange, value, Minimum: min, Maximum: max);

    public static MemoriesFormField Toggle(string key, bool on)
        => new(key, "Form_Tenant_Label", MemoriesFormFieldKind.DangerousToggle, on ? "true" : "false");

    public static MemoriesFormRequest Request(
        IReadOnlyList<MemoriesFormField>? fields = null,
        string requestedTenant = "tenant-a",
        string? requestedCase = "case-a",
        string currentTenant = "tenant-a",
        string? currentCase = "case-a",
        EvidencePacketIsolationStatus isolation = EvidencePacketIsolationStatus.Authorized,
        bool acknowledged = false,
        MemoriesFormKind kind = MemoriesFormKind.Search)
        => new(
            kind,
            requestedTenant,
            requestedCase,
            currentTenant,
            currentCase,
            isolation,
            fields ?? [Tenant(), Case(), RequiredText()],
            acknowledged);
}
