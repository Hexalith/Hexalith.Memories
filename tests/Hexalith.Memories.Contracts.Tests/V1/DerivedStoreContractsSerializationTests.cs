// <copyright file="DerivedStoreContractsSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;

using Shouldly;

public class DerivedStoreContractsSerializationTests
{
    [Fact]
    public void CorrectionStatus_RoundTripsCamelCaseEnumsAndMetadataOnlyEvidence()
    {
        var expected = new DerivedStoreCorrectionStatus(
            "derived-correction-abc",
            DerivedStoreCorrectionState.Succeeded,
            "association-1",
            "intake-1",
            "correction-1",
            7,
            "case-prior",
            "case-corrected",
            2,
            2,
            false,
            DateTimeOffset.Parse("2026-08-28T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-08-28T11:10:00Z", System.Globalization.CultureInfo.InvariantCulture),
            null);

        string json = JsonSerializer.Serialize(expected, MemoriesJsonContext.Options);
        json.ShouldContain("\"state\":\"succeeded\"");
        json.ShouldNotContain("content", Shouldly.Case.Insensitive);
        JsonSerializer.Deserialize<DerivedStoreCorrectionStatus>(json, MemoriesJsonContext.Options).ShouldBe(expected);
    }

    [Fact]
    public void FinalizeBindingRequest_RoundTripsOrderedManifestKinds()
    {
        var expected = new FinalizeDerivedStoreBindingRequest(
            "association-1",
            "intake-1",
            4,
            "case-1",
            1,
            [
                new DerivedStoreBindingEntry(DerivedStoreRecordKind.Message, 0, "unit-message"),
                new DerivedStoreBindingEntry(DerivedStoreRecordKind.Attachment, 1, "unit-attachment"),
            ]);

        string json = JsonSerializer.Serialize(expected, MemoriesJsonContext.Options);
        json.ShouldContain("\"recordKind\":\"message\"");
        json.ShouldContain("\"recordKind\":\"attachment\"");
        FinalizeDerivedStoreBindingRequest? actual = JsonSerializer.Deserialize<FinalizeDerivedStoreBindingRequest>(json, MemoriesJsonContext.Options);
        actual.ShouldNotBeNull();
        actual.AssociationId.ShouldBe(expected.AssociationId);
        actual.Entries.ShouldBe(expected.Entries);
    }

    [Fact]
    public void DiagnosticRoutes_AreTenantFirstAndOutsideCanonicalNamespaces()
    {
        string ownerPath = MemoriesRoutes.DerivedStoreDiagnosticPath("tenant-owner", "vectorIndex", "probe-1");
        string intruderPath = MemoriesRoutes.DerivedStoreDiagnosticPath("tenant-intruder", "vectorIndex", "probe-1");

        ownerPath.ShouldNotBe(intruderPath);
        ownerPath.ShouldStartWith("api/v1/tenants/tenant-owner/diagnostics/derived-stores/", Shouldly.Case.Sensitive);
        ownerPath.ShouldNotContain("/cases/", Shouldly.Case.Sensitive);
        ownerPath.ShouldNotContain("memories:vec", Shouldly.Case.Sensitive);
    }
}
