// <copyright file="EvidencePacketCanonicalParityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.TestHelpers.EvidencePackets;

using Shouldly;

/// <summary>
/// Anchors the cross-surface canonical JSON (Story 2.7 / CR1). The shared fixtures in
/// <see cref="EvidencePacketCanonicalFixtures"/> are the single source of truth the CLI, MCP, and server
/// tests compare against; this suite proves the contract layer itself round-trips that JSON without drift
/// and that the <c>Canonicalize</c>/<c>CanonicalizeEmbedded</c> normalizers are well-founded.
/// </summary>
public sealed class EvidencePacketCanonicalParityTests
{
    public static TheoryData<string, EvidencePacketState> CanonicalPackets() => new()
    {
        { nameof(EvidencePacketCanonicalFixtures.HybridCompletePacket), EvidencePacketState.Complete },
        { nameof(EvidencePacketCanonicalFixtures.SingleCompletePacket), EvidencePacketState.Complete },
        { nameof(EvidencePacketCanonicalFixtures.HybridDegradedPacket), EvidencePacketState.Degraded },
        { nameof(EvidencePacketCanonicalFixtures.SingleTokenBudgetPacket), EvidencePacketState.PendingExpansion },
        { nameof(EvidencePacketCanonicalFixtures.SingleEmptyPacket), EvidencePacketState.Empty },
    };

    [Theory]
    [MemberData(nameof(CanonicalPackets))]
    public void CanonicalPacket_ShouldHaveExpectedStateAndRoundTrip(string fixtureName, EvidencePacketState expectedState)
    {
        EvidencePacket packet = Resolve(fixtureName);

        packet.State.ShouldBe(expectedState);

        string json = EvidencePacketCanonicalFixtures.Canonicalize(packet);
        EvidencePacket? deserialized = JsonSerializer.Deserialize<EvidencePacket>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.State.ShouldBe(expectedState);

        // Canonicalize is idempotent: re-normalizing the same packet JSON yields byte-identical output, so
        // any surface comparison against it is order- and formatting-independent.
        EvidencePacketCanonicalFixtures.Canonicalize(json).ShouldBe(json);
    }

    [Fact]
    public void CanonicalizeEmbedded_ShouldExtractPacketFromNestedSurfaceJson()
    {
        EvidencePacket packet = EvidencePacketCanonicalFixtures.HybridCompletePacket();
        string canonical = EvidencePacketCanonicalFixtures.Canonicalize(packet);

        // Simulate an arbitrary surface envelope that nests the packet under data.evidencePacket.
        string surfaceJson = $$"""
        { "schemaVersion": 1, "command": "search query", "data": { "results": [], "evidencePacket": {{canonical}} } }
        """;

        EvidencePacketCanonicalFixtures.CanonicalizeEmbedded(surfaceJson).ShouldBe(canonical);
    }

    [Fact]
    public void DegradedCanonicalPacket_ShouldExposeUnavailableAxisAndRetryRecovery()
    {
        EvidencePacket packet = EvidencePacketCanonicalFixtures.HybridDegradedPacket();

        packet.Evidence.Degraded.ShouldBeTrue();
        packet.Evidence.UnavailableAxes.ShouldBe(["graph"]);
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.Retry);
    }

    [Fact]
    public void TokenBudgetCanonicalPacket_ShouldExposeScopedExpansionHandle()
    {
        EvidencePacket packet = EvidencePacketCanonicalFixtures.SingleTokenBudgetPacket();

        packet.State.ShouldBe(EvidencePacketState.PendingExpansion);
        EvidencePacketExpansionHandle handle = packet.OmittedDetails.ExpansionHandles.ShouldHaveSingleItem();
        handle.TenantId.ShouldBe("tenant-a");
        handle.CaseId.ShouldBe("case-a");
        handle.Handle.ShouldStartWith("ep:v1:");
    }

    private static EvidencePacket Resolve(string fixtureName) => fixtureName switch
    {
        nameof(EvidencePacketCanonicalFixtures.HybridCompletePacket) => EvidencePacketCanonicalFixtures.HybridCompletePacket(),
        nameof(EvidencePacketCanonicalFixtures.SingleCompletePacket) => EvidencePacketCanonicalFixtures.SingleCompletePacket(),
        nameof(EvidencePacketCanonicalFixtures.HybridDegradedPacket) => EvidencePacketCanonicalFixtures.HybridDegradedPacket(),
        nameof(EvidencePacketCanonicalFixtures.SingleTokenBudgetPacket) => EvidencePacketCanonicalFixtures.SingleTokenBudgetPacket(),
        nameof(EvidencePacketCanonicalFixtures.SingleEmptyPacket) => EvidencePacketCanonicalFixtures.SingleEmptyPacket(),
        _ => throw new ArgumentOutOfRangeException(nameof(fixtureName), fixtureName, "Unknown canonical fixture."),
    };
}
