// <copyright file="FormFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Forms;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Forms;
using Hexalith.Memories.Web.Specimens;

internal static class FormFixtures
{
    public static MemoriesFormField Tenant(string? value = "tenant-a") => Epic17FormFixtures.Tenant(value);

    public static MemoriesFormField Case(string? value = "case-a", bool required = true) => Epic17FormFixtures.Case(value, required);

    public static MemoriesFormField RequiredText(string key = "query", string? value = "policy context") => Epic17FormFixtures.RequiredText(key, value);

    public static MemoriesFormField EnumField(string key, string? value, params string[] allowed) => Epic17FormFixtures.EnumField(key, value, allowed);

    public static MemoriesFormField Range(string key, string? value, double min, double max) => Epic17FormFixtures.Range(key, value, min, max);

    public static MemoriesFormField Toggle(string key, bool on) => Epic17FormFixtures.Toggle(key, on);

    public static MemoriesFormRequest Request(
        IReadOnlyList<MemoriesFormField>? fields = null,
        string requestedTenant = "tenant-a",
        string? requestedCase = "case-a",
        string currentTenant = "tenant-a",
        string? currentCase = "case-a",
        EvidencePacketIsolationStatus isolation = EvidencePacketIsolationStatus.Authorized,
        bool acknowledged = false,
        MemoriesFormKind kind = MemoriesFormKind.Search)
        => Epic17FormFixtures.Request(
            fields,
            requestedTenant,
            requestedCase,
            currentTenant,
            currentCase,
            isolation,
            acknowledged,
            kind);
}
