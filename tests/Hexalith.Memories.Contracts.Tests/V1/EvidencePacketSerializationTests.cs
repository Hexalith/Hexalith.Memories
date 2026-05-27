// <copyright file="EvidencePacketSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public sealed class EvidencePacketSerializationTests
{
    [Fact]
    public void CompletePacket_ShouldRoundTripWithStableCamelCaseSections()
    {
        EvidencePacket original = EvidencePacketFixtures.Complete();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        EvidencePacket? deserialized = JsonSerializer.Deserialize<EvidencePacket>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Scope.TenantId.ShouldBe("tenant-a");
        deserialized.Result.Query.ShouldBe("claim denied");
        deserialized.Sources[0].MemoryUnitId.ShouldBe("mu-001");
        deserialized.Evidence.EvidenceStrength.ShouldBe(EvidencePacketEvidenceStrength.Strong);
        deserialized.Graph.Available.ShouldBeFalse();
        deserialized.State.ShouldBe(EvidencePacketState.Complete);
        deserialized.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.None);
        deserialized.Recovery.ShouldBeEmpty();
        json.ShouldContain("\"scope\":");
        json.ShouldContain("\"omittedDetails\":");
        json.ShouldContain("\"evidenceStrength\"", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"Scope\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void DegradedPacket_ShouldPreserveUnavailableAxesAndRecoveryActions()
    {
        EvidencePacket original = EvidencePacketFixtures.Degraded();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        EvidencePacket? deserialized = JsonSerializer.Deserialize<EvidencePacket>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.State.ShouldBe(EvidencePacketState.Degraded);
        deserialized.Evidence.Degraded.ShouldBeTrue();
        deserialized.Evidence.UnavailableAxes.ShouldBe(["graph"]);
        deserialized.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.BackendUnavailable);
        deserialized.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.Retry);
        json.ShouldContain("\"state\":\"degraded\"");
        json.ShouldContain("\"kind\":\"retry\"");
    }

    [Fact]
    public void EmptyPacket_ShouldKeepRequiredSectionsWithEmptyCollections()
    {
        EvidencePacket original = EvidencePacketFixtures.Empty();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        EvidencePacket? deserialized = JsonSerializer.Deserialize<EvidencePacket>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.State.ShouldBe(EvidencePacketState.Empty);
        deserialized.Sources.ShouldBeEmpty();
        deserialized.Graph.EdgeTypes.ShouldBeEmpty();
        deserialized.OmittedDetails.ExpansionHandles.ShouldBeEmpty();
        json.ShouldContain("\"sources\":[]");
        json.ShouldContain("\"recovery\":");
    }

    [Fact]
    public void UnauthorizedPacket_ShouldNotExposeExpansionHandles()
    {
        EvidencePacket original = EvidencePacketFixtures.Unauthorized();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        EvidencePacket? deserialized = JsonSerializer.Deserialize<EvidencePacket>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.State.ShouldBe(EvidencePacketState.Unauthorized);
        deserialized.Scope.IsolationStatus.ShouldBe(EvidencePacketIsolationStatus.Unauthorized);
        deserialized.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.Authorization);
        deserialized.OmittedDetails.ExpansionHandles.ShouldBeEmpty();
        deserialized.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.CheckAuthorization);
        json.ShouldNotContain("fetchMemoryUnit");
        json.ShouldNotContain("backend-key");
    }

    [Fact]
    public void TokenBudgetCompressedPacket_ShouldPreserveOmittedGroupsAndScopedExpansionHandle()
    {
        EvidencePacket original = EvidencePacketFixtures.TokenBudgetCompressed();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        EvidencePacket? deserialized = JsonSerializer.Deserialize<EvidencePacket>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.State.ShouldBe(EvidencePacketState.PendingExpansion);
        deserialized.OmittedDetails.OmittedCount.ShouldBe(3);
        deserialized.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.TokenBudget);
        deserialized.OmittedDetails.FieldNames.ShouldBe(["sources"]);
        deserialized.OmittedDetails.ExpansionHandles.ShouldHaveSingleItem();
        deserialized.OmittedDetails.ExpansionHandles[0].TenantId.ShouldBe("tenant-a");
        deserialized.OmittedDetails.ExpansionHandles[0].CaseId.ShouldBe("case-a");
        deserialized.OmittedDetails.ExpansionHandles[0].Handle.ShouldNotContain("claim denied");
        json.ShouldContain("\"reason\":\"tokenBudget\"");
    }
}
