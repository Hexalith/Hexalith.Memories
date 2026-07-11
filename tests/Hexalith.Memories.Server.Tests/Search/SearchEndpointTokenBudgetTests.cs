// <copyright file="SearchEndpointTokenBudgetTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Shouldly;

/// <summary>
/// Story 10.2 Task 13.1 — Tier-2 endpoint-wiring coverage for the token-budget plumbing
/// applied between <c>/api/v1/search</c> and the search services. Exercises
/// <see cref="SearchResponseMetadataApplier"/> directly because that class is the in-memory
/// surface the search endpoint defers to once a search service has produced a result.
/// </summary>
public sealed class SearchEndpointTokenBudgetTests
{
    [Fact]
    public void SyntacticAxis_TruncatesByRank_AndEmitsOmittedCount()
    {
        SearchResult source = BuildSearchResult(resultCount: 5);

        SearchResult applied = SearchResponseMetadataApplier.ApplySearch(source, "syntactic", budget: 80);

        applied.Results.Count.ShouldBeLessThan(source.Results.Count);
        applied.OmittedCount.ShouldBe(source.Results.Count - applied.Results.Count);
        applied.OmittedReason.ShouldBe(OmittedReason.TokenBudget);
        applied.AxesUsed.ShouldNotBeNull();
        applied.AxesUsed.ShouldBe(["syntactic"]);
        applied.EstimatedTokensTotal.ShouldBeGreaterThan(0);
        applied.Degraded.ShouldBeFalse();
        applied.UnavailableAxes.ShouldBeNull();
    }

    [Fact]
    public void SemanticAxis_TruncatesByRank_AndPreservesRankOrder()
    {
        SearchResult source = BuildSearchResult(resultCount: 4);

        SearchResult applied = SearchResponseMetadataApplier.ApplySearch(source, "semantic", budget: 60);

        applied.Results
            .Select(static result => result.MemoryUnitId)
            .ShouldBe(source.Results
                .Take(applied.Results.Count)
                .Select(static result => result.MemoryUnitId));
        applied.AxesUsed.ShouldBe(["semantic"]);
    }

    [Fact]
    public void HybridAxis_TruncatesFusedResults_AndPopulatesAxesUsed()
    {
        HashSet<string> enabledAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
        TenantEmbeddingConfig embeddingConfig = new()
        {
            Provider = "test",
            Model = "test-embedding",
            Dimensions = 8,
            RateLimitPerMinute = 60,
            ApiSecretKeyName = "test-secret",
        };

        HybridSearchResult source = BuildHybridResult(
            resultCount: 5,
            unavailableAxes: []);

        HybridSearchResult applied = SearchResponseMetadataApplier.ApplyHybrid(
            source,
            budget: 80,
            enabledAxes,
            embeddingConfig,
            graphStart: "mu-start");

        applied.Results.Count.ShouldBeLessThan(source.Results.Count);
        applied.OmittedCount.ShouldBe(source.Results.Count - applied.Results.Count);
        applied.OmittedReason.ShouldBe(OmittedReason.TokenBudget);
        applied.AxesUsed.ShouldNotBeNull();
        applied.AxesUsed.ShouldBe(["graph", "semantic", "syntactic"]);
    }

    [Fact]
    public void MissingTokenBudget_ReturnsAllResults_AndDoesNotEmitOmissions()
    {
        SearchResult singleAxisSource = BuildSearchResult(resultCount: 4);
        HybridSearchResult hybridSource = BuildHybridResult(resultCount: 4, unavailableAxes: []);
        HashSet<string> enabledAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic" };

        SearchResult singleAxisApplied = SearchResponseMetadataApplier.ApplySearch(singleAxisSource, "syntactic", budget: null);
        HybridSearchResult hybridApplied = SearchResponseMetadataApplier.ApplyHybrid(
            hybridSource,
            budget: null,
            enabledAxes,
            embeddingConfig: null,
            graphStart: null);

        singleAxisApplied.Results.Count.ShouldBe(singleAxisSource.Results.Count);
        singleAxisApplied.OmittedCount.ShouldBe(0);
        singleAxisApplied.OmittedReason.ShouldBe(OmittedReason.None);
        singleAxisApplied.AxesUsed.ShouldBe(["syntactic"]);

        hybridApplied.Results.Count.ShouldBe(hybridSource.Results.Count);
        hybridApplied.OmittedCount.ShouldBe(0);
        hybridApplied.OmittedReason.ShouldBe(OmittedReason.None);
        hybridApplied.AxesUsed.ShouldBe(["syntactic"]);
    }

    [Fact]
    public void DegradedSingleAxis_PopulatesUnavailableAxes_AndCombinesOmittedReason()
    {
        SearchResult source = BuildSearchResult(resultCount: 4);

        SearchResult applied = SearchResponseMetadataApplier.ApplySearch(
            source,
            "graph",
            budget: 60,
            degraded: true,
            unavailableAxes: ["semantic"]);

        applied.Degraded.ShouldBeTrue();
        applied.UnavailableAxes.ShouldNotBeNull();
        applied.UnavailableAxes.ShouldBe(["semantic"]);
        applied.OmittedReason.ShouldBe(OmittedReason.Combined);
        applied.AxesUsed.ShouldBe(["graph"]);
    }

    [Fact]
    public void HybridResult_DegradedAxis_IsRemovedFromAxesUsed()
    {
        HashSet<string> enabledAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
        TenantEmbeddingConfig embeddingConfig = new()
        {
            Provider = "test",
            Model = "test-embedding",
            Dimensions = 8,
            RateLimitPerMinute = 60,
            ApiSecretKeyName = "test-secret",
        };

        HybridSearchResult source = BuildHybridResult(
            resultCount: 3,
            unavailableAxes: ["graph"],
            degraded: true);

        HybridSearchResult applied = SearchResponseMetadataApplier.ApplyHybrid(
            source,
            budget: null,
            enabledAxes,
            embeddingConfig,
            graphStart: "mu-start");

        applied.AxesUsed.ShouldNotBeNull();
        applied.AxesUsed.ShouldBe(["semantic", "syntactic"]);
        applied.Degraded.ShouldBeTrue();
        applied.UnavailableAxes.ShouldBe(["graph"]);
        applied.OmittedReason.ShouldBe(OmittedReason.BackendDegraded);
    }

    [Fact]
    public void HybridResult_HealthyButZeroHitAxis_IsNotReportedAsUsed()
    {
        HashSet<string> enabledAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
        TenantEmbeddingConfig embeddingConfig = new()
        {
            Provider = "test",
            Model = "test-embedding",
            Dimensions = 8,
            RateLimitPerMinute = 60,
            ApiSecretKeyName = "test-secret",
        };

        HybridSearchResult source = BuildHybridResult(
            resultCount: 3,
            unavailableAxes: [],
            graphScores: false);

        HybridSearchResult applied = SearchResponseMetadataApplier.ApplyHybrid(
            source,
            budget: null,
            enabledAxes,
            embeddingConfig,
            graphStart: "mu-start");

        applied.AxesUsed.ShouldNotBeNull();
        applied.AxesUsed.ShouldBe(["semantic", "syntactic"]);
    }

    private static SearchResult BuildSearchResult(int resultCount)
    {
        ScoredResult[] results = Enumerable.Range(0, resultCount)
            .Select(index => new ScoredResult
            {
                MemoryUnitId = $"mu-{index}",
                Score = 1.0 - (index * 0.1),
                ContentSnippet = new string('x', 80),
                SourceUri = $"file:///mu-{index}.txt",
                SourceType = SourceType.File,
            })
            .ToArray();

        return new SearchResult
        {
            Results = results,
            TotalCount = resultCount,
            HasIndexedMemoryUnits = true,
            Query = "demo",
        };
    }

    private static HybridSearchResult BuildHybridResult(
        int resultCount,
        IReadOnlyList<string> unavailableAxes,
        bool degraded = false,
        bool syntacticScores = true,
        bool semanticScores = true,
        bool graphScores = true)
    {
        FusedScoredResult[] results = Enumerable.Range(0, resultCount)
            .Select(index => new FusedScoredResult
            {
                MemoryUnitId = $"mu-{index}",
                CompositeScore = 1.0 - (index * 0.1),
                ContentSnippet = new string('x', 80),
                SourceUri = $"file:///mu-{index}.txt",
                SourceType = SourceType.File,
                SyntacticScore = syntacticScores ? 0.9 - (index * 0.1) : null,
                SemanticScore = semanticScores ? 0.8 - (index * 0.1) : null,
                GraphScore = graphScores ? 0.7 - (index * 0.1) : null,
            })
            .ToArray();

        return new HybridSearchResult
        {
            Results = results,
            TotalCount = resultCount,
            Degraded = degraded || unavailableAxes.Count > 0,
            UnavailableAxes = unavailableAxes,
            Query = "demo",
        };
    }
}
