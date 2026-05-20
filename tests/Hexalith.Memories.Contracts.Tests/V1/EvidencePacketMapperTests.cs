// <copyright file="EvidencePacketMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public sealed class EvidencePacketMapperTests
{
    [Fact]
    public void FromSearchResult_ShouldMapScopeSourcesEvidenceAndTokenBudgetOmissions()
    {
        var result = new SearchResult
        {
            Results =
            [
                new ScoredResult
                {
                    MemoryUnitId = "mu-001",
                    Score = 0.91,
                    ContentSnippet = "Claim denial language",
                    SourceUri = "mem://tenant-a/case-a/mu-001",
                    SourceType = SourceType.File,
                    Axis = "semantic",
                    CaseId = "case-a",
                    CaseName = "Case A",
                    AnnotationsCount = 2,
                },
            ],
            TotalCount = 4,
            HasIndexedMemoryUnits = true,
            Query = "claim denied",
            Explanation = BuildExplanation(),
            OmittedCount = 3,
            EstimatedTokensTotal = 1_024,
            OmittedReason = OmittedReason.TokenBudget,
            AxesUsed = ["semantic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"));

        packet.Scope.TenantId.ShouldBe("tenant-a");
        packet.Scope.CaseId.ShouldBe("case-a");
        packet.Result.TotalCount.ShouldBe(4);
        packet.Sources[0].Rank.ShouldBe(1);
        packet.Sources[0].Score.ShouldBe(0.91);
        packet.Evidence.Caveat.ShouldBe("Scores measure relevance, not factual accuracy.");
        packet.Evidence.AxesUsed.ShouldBe(["semantic"]);
        packet.Evidence.AxisEvidence[0].Axis.ShouldBe("semantic");
        packet.State.ShouldBe(EvidencePacketState.PendingExpansion);
        packet.OmittedDetails.ExpansionHandles.ShouldHaveSingleItem();
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.IncreaseTokenBudget);
    }

    [Fact]
    public void FromHybridSearchResult_ShouldMapHybridScoresAndDegradationPrecedence()
    {
        var result = new HybridSearchResult
        {
            Results =
            [
                new FusedScoredResult
                {
                    MemoryUnitId = "mu-001",
                    CompositeScore = 0.82,
                    ContentSnippet = "Hybrid evidence",
                    SourceUri = "mem://tenant-a/case-a/mu-001",
                    SourceType = SourceType.Url,
                    SyntacticScore = 0.51,
                    SemanticScore = 0.82,
                    GraphScore = null,
                    CaseId = "case-a",
                    CaseName = "Case A",
                },
            ],
            TotalCount = 1,
            Degraded = true,
            UnavailableAxes = ["graph"],
            Query = "claim denied",
            Explanation = BuildExplanation(),
            OmittedReason = OmittedReason.BackendDegraded,
            AxesUsed = ["semantic", "syntactic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromHybridSearchResult(
            result,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"));

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.Evidence.Degraded.ShouldBeTrue();
        packet.Evidence.UnavailableAxes.ShouldBe(["graph"]);
        packet.Evidence.AxisEvidence.ShouldContain(axis => axis.Axis == "semantic" && axis.Score == 0.82);
        packet.Evidence.AxisEvidence.ShouldContain(axis => axis.Axis == "syntactic" && axis.Score == 0.51);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.BackendUnavailable);
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.Retry);
    }

    [Fact]
    public void FromError_Unauthorized_ShouldSuppressExpansionGuidanceAndSanitizeRecoveryText()
    {
        var error = new ErrorResponse(
            "TENANT_FORBIDDEN",
            "Denied for tenant-b at C:\\secret\\trace.txt with Bearer abc.",
            "Use tenant-b redis://backend-key or Bearer abc.");

        EvidencePacket packet = EvidencePacketMapper.FromError(
            error,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"),
            query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Unauthorized);
        packet.Scope.IsolationStatus.ShouldBe(EvidencePacketIsolationStatus.Unauthorized);
        packet.Sources.ShouldBeEmpty();
        packet.OmittedDetails.ExpansionHandles.ShouldBeEmpty();
        packet.Recovery.ShouldHaveSingleItem();
        packet.Recovery[0].Kind.ShouldBe(EvidencePacketRecoveryKind.CheckAuthorization);

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain("C:\\secret", Shouldly.Case.Sensitive);
        json.ShouldNotContain("Bearer abc", Shouldly.Case.Sensitive);
        json.ShouldNotContain("redis://backend-key", Shouldly.Case.Sensitive);
        json.ShouldNotContain("tenant-b", Shouldly.Case.Sensitive);
    }

    private static SearchExplanation BuildExplanation() => new()
    {
        Caveat = "Scores measure relevance, not factual accuracy.",
        AxisDetails = new Dictionary<string, AxisExplanation>
        {
            ["semantic"] = new() { NormalizationMethod = "cosine", Description = "cosine similarity" },
            ["syntactic"] = new() { NormalizationMethod = "bm25_saturation", Description = "BM25 saturation" },
        },
    };
}

