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
    public void FromError_Unauthorized_ShouldUseHardcodedRecoveryWithoutCopyingErrorFields()
    {
        // Sensitive fragments live in error.Message and error.Suggestion. The unauthorized branch must not
        // copy either field into the packet — recovery is hardcoded and the scope echo is the caller's,
        // not the error's. The assertion proves the bypass invariant, not that SanitizeGuidance ran.
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
        packet.Scope.TenantId.ShouldBe("tenant-a");
        packet.Sources.ShouldBeEmpty();
        packet.OmittedDetails.ExpansionHandles.ShouldBeEmpty();
        packet.Recovery.ShouldHaveSingleItem();
        packet.Recovery[0].Kind.ShouldBe(EvidencePacketRecoveryKind.CheckAuthorization);
        packet.Recovery[0].Guidance.ShouldBe("Use an authorized tenant and case scope.");

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain("tenant-b", Shouldly.Case.Sensitive);
        json.ShouldNotContain("C:\\secret", Shouldly.Case.Sensitive);
        json.ShouldNotContain("Bearer abc", Shouldly.Case.Sensitive);
        json.ShouldNotContain("redis://backend-key", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void FromError_NonUnauthorized_ShouldReplaceSensitiveSuggestionWithFallback()
    {
        // BACKEND_DEGRADED routes to the Retry branch where SanitizeGuidance actually runs against
        // error.Suggestion. Sensitive payload in the suggestion must be replaced with the fallback string.
        var error = new ErrorResponse(
            "BACKEND_DEGRADED",
            "Backend is degraded.",
            "Retry after Bearer abc123def456ghi789jkl012mno345pqr678 reconnects to redis://backend-key/0.");

        EvidencePacket packet = EvidencePacketMapper.FromError(
            error,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"),
            query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.Recovery.ShouldHaveSingleItem();
        packet.Recovery[0].Kind.ShouldBe(EvidencePacketRecoveryKind.Retry);
        packet.Recovery[0].Guidance.ShouldBe("Retry the authorized request or inspect service health.");

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain("Bearer abc123def456ghi789jkl012mno345pqr678", Shouldly.Case.Sensitive);
        json.ShouldNotContain("redis://backend-key", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void FromError_NonUnauthorized_ShouldPreserveBenignTokenBudgetGuidance()
    {
        // Regression for the over-broad sanitization regex: prose like "token budget" must survive.
        var error = new ErrorResponse(
            "BACKEND_DEGRADED",
            "Backend is degraded.",
            "Increase the token budget and retry.");

        EvidencePacket packet = EvidencePacketMapper.FromError(
            error,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"),
            query: "claim denied");

        packet.Recovery[0].Guidance.ShouldBe("Increase the token budget and retry.");
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

