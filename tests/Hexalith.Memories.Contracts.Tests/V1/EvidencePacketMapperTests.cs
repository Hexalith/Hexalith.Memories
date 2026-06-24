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
    public void FromSearchResult_EmptyAuthorizedScope_ShouldEmitEmptyPacketShape()
    {
        var result = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "missing phrase",
            AxesUsed = ["semantic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"));

        packet.State.ShouldBe(EvidencePacketState.Empty);
        packet.Result.TotalCount.ShouldBe(0);
        packet.Result.ReturnedCount.ShouldBe(0);
        packet.Sources.ShouldBeEmpty();
        packet.Evidence.EvidenceStrength.ShouldBe(EvidencePacketEvidenceStrength.None);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.None);
        packet.OmittedDetails.ExpansionHandles.ShouldBeEmpty();
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.BroadenScope);
    }

    [Fact]
    public void FromSearchResult_DegradedTokenBudget_ShouldCombineOmissionMetadata()
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
                    Axis = "syntactic",
                    CaseId = "case-a",
                    CaseName = "Case A",
                },
            ],
            TotalCount = 3,
            HasIndexedMemoryUnits = true,
            Query = "claim denied",
            Degraded = true,
            UnavailableAxes = ["syntactic"],
            OmittedCount = 2,
            EstimatedTokensTotal = 2_048,
            OmittedReason = OmittedReason.TokenBudget,
            AxesUsed = ["syntactic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"));

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.Evidence.Degraded.ShouldBeTrue();
        packet.Evidence.UnavailableAxes.ShouldBe(["syntactic"]);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.Combined);
        packet.OmittedDetails.FieldNames.ShouldContain("sources");
        packet.OmittedDetails.FieldNames.ShouldContain("evidence.unavailableAxes");
        packet.OmittedDetails.DetailGroups.ShouldContain("rankedResults");
        packet.OmittedDetails.DetailGroups.ShouldContain("backendDiagnostics");
        EvidencePacketExpansionHandle handle = packet.OmittedDetails.ExpansionHandles.ShouldHaveSingleItem();
        handle.Handle.ShouldStartWith("ep:v1:");
        handle.TargetDetailGroup.ShouldBe("rankedResults");
        handle.TenantId.ShouldBe("tenant-a");
        handle.CaseId.ShouldBe("case-a");
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.Retry);
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.InspectBackendHealth);
    }

    [Fact]
    public void FromSearchResult_TenantWideScope_ShouldKeepScopeSeparateFromSourceCase()
    {
        var result = new SearchResult
        {
            Results =
            [
                new ScoredResult
                {
                    MemoryUnitId = "mu-001",
                    Score = 0.79,
                    ContentSnippet = "Tenant-wide match",
                    SourceUri = "mem://tenant-a/case-visible/mu-001",
                    SourceType = SourceType.File,
                    Axis = "semantic",
                    CaseId = "case-visible",
                    CaseName = "Visible Case",
                },
            ],
            TotalCount = 1,
            HasIndexedMemoryUnits = true,
            Query = "tenant wide",
            AxesUsed = ["semantic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-a", null, EvidencePacketIsolationStatus.Authorized, "tenant"));

        packet.Scope.CaseId.ShouldBeNull();
        packet.Scope.PermissionsContext.ShouldBe("tenant");
        packet.Sources[0].CaseId.ShouldBe("case-visible");
        packet.Sources[0].CaseName.ShouldBe("Visible Case");
    }

    [Fact]
    public void FromSearchResult_AxisEvidence_ShouldBeNormalizedDeduplicatedAndSorted()
    {
        var result = new SearchResult
        {
            Results =
            [
                new ScoredResult
                {
                    MemoryUnitId = "mu-001",
                    Score = 0.71,
                    ContentSnippet = "Semantic match",
                    SourceUri = "mem://tenant-a/case-a/mu-001",
                    SourceType = SourceType.File,
                    Axis = "Semantic",
                    CaseId = "case-a",
                    CaseName = "Case A",
                },
            ],
            TotalCount = 1,
            HasIndexedMemoryUnits = true,
            Query = "claim denied",
            AxesUsed = ["Syntactic", " semantic ", "syntactic"],
            Explanation = new SearchExplanation
            {
                Caveat = "Scores measure relevance, not factual accuracy.",
                AxisDetails = new Dictionary<string, AxisExplanation>
                {
                    ["Graph"] = new() { NormalizationMethod = "path_decay", Description = "graph proximity" },
                    ["semantic"] = new() { NormalizationMethod = "cosine", Description = "cosine similarity" },
                },
            },
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"));

        packet.Evidence.AxesUsed.ShouldBe(["semantic", "syntactic"]);
        packet.Evidence.AxisEvidence.Select(axis => axis.Axis).ToArray().ShouldBe(["graph", "semantic", "syntactic"]);
        packet.Evidence.AxisEvidence.Single(axis => axis.Axis == "semantic").Score.ShouldBe(0.71);
        packet.Evidence.AxisEvidence.Single(axis => axis.Axis == "graph").NormalizationMethod.ShouldBe("path_decay");
    }

    [Theory]
    [InlineData("TENANT_FORBIDDEN", EvidencePacketState.Unauthorized, EvidencePacketOmissionReason.Authorization, EvidencePacketRecoveryKind.CheckAuthorization)]
    [InlineData("CASE_UNAUTHORIZED", EvidencePacketState.Unauthorized, EvidencePacketOmissionReason.Authorization, EvidencePacketRecoveryKind.CheckAuthorization)]
    [InlineData("BACKEND_DEGRADED", EvidencePacketState.Degraded, EvidencePacketOmissionReason.BackendUnavailable, EvidencePacketRecoveryKind.Retry)]
    public void FromError_StateMapping_ShouldPreserveSanitizedPacketShape(
        string code,
        EvidencePacketState expectedState,
        EvidencePacketOmissionReason expectedReason,
        EvidencePacketRecoveryKind expectedRecoveryKind)
    {
        var error = new ErrorResponse(
            code,
            "Sensitive backend failure at C:\\secret\\trace.txt.",
            "Retry after Bearer abc123def456ghi789jkl012mno345pqr678 reconnects to redis://backend-key/0.");

        EvidencePacket packet = EvidencePacketMapper.FromError(
            error,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"),
            query: "claim denied");

        packet.State.ShouldBe(expectedState);
        packet.OmittedDetails.Reason.ShouldBe(expectedReason);
        packet.Recovery.ShouldContain(action => action.Kind == expectedRecoveryKind);

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain("C:\\secret", Shouldly.Case.Sensitive);
        json.ShouldNotContain("Bearer abc123def456ghi789jkl012mno345pqr678", Shouldly.Case.Sensitive);
        json.ShouldNotContain("redis://backend-key", Shouldly.Case.Sensitive);
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

