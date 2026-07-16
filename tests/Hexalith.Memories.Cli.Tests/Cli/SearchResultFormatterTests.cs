// <copyright file="SearchResultFormatterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

public sealed class SearchResultFormatterTests
{
    private const string PrdCaveat =
        "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness.";

    [Fact]
    public void HybridHuman_WithExplain_PrintsCaveatFirstAndOnce()
    {
        HybridSearchResult payload = BuildHybridResult(withExplain: true);
        using var writer = new StringWriter() { NewLine = "\n" };

        new HybridSearchResultHumanFormatter().Write(payload, writer);
        string output = writer.ToString();

        // Caveat must appear exactly once and before the first result line.
        int caveatIndex = output.IndexOf(PrdCaveat, StringComparison.Ordinal);
        int firstResultIndex = output.IndexOf("1. [", StringComparison.Ordinal);
        caveatIndex.ShouldBeGreaterThanOrEqualTo(0);
        firstResultIndex.ShouldBeGreaterThan(caveatIndex);

        int lastCaveatIndex = output.LastIndexOf(PrdCaveat, StringComparison.Ordinal);
        caveatIndex.ShouldBe(lastCaveatIndex);
    }

    [Fact]
    public void HybridHuman_Degraded_FormatterOmitsDegradationNotice_SurfaceMovedToHandlerInStory73()
    {
        // Story 7.3 Task 5.1: the 7.2 bridge line "Note: search degraded — axes unavailable: ..." was
        // deleted from the formatter. Per-axis degradation warnings are now emitted to stderr by
        // SearchQueryCommand BEFORE the formatter runs. The formatter itself is degradation-agnostic.
        HybridSearchResult payload = BuildHybridResult(withExplain: false, degraded: true, unavailable: new[] { "graph" });
        using var writer = new StringWriter() { NewLine = "\n" };

        new HybridSearchResultHumanFormatter().Write(payload, writer);
        string output = writer.ToString();

        output.ShouldNotContain("Note: search degraded");
        output.ShouldNotContain("axes unavailable");
    }

    [Fact]
    public void HybridJson_EmitsEnvelopeWithCaveatPath()
    {
        HybridSearchResult payload = BuildHybridResult(withExplain: true);
        using var writer = new StringWriter() { NewLine = "\n" };

        new JsonEnvelopeFormatter<HybridSearchResult>(SearchQueryCommand.CommandName).Write(payload, writer);
        using JsonDocument doc = JsonDocument.Parse(writer.ToString());

        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("command").GetString().ShouldBe("search query");
        doc.RootElement.GetProperty("data").GetProperty("explanation").GetProperty("caveat").GetString()
            .ShouldBe(PrdCaveat);
    }

    [Fact]
    public void HybridTable_WithExplain_HeadersIncludeAllAxes()
    {
        HybridSearchResult payload = BuildHybridResult(withExplain: true);
        using var writer = new StringWriter() { NewLine = "\n" };

        new HybridSearchResultTableFormatter().Write(payload, writer);
        string output = writer.ToString();

        output.ShouldContain("COMPOSITE");
        output.ShouldContain("SYNTACTIC");
        output.ShouldContain("SEMANTIC");
        output.ShouldContain("GRAPH");
        output.ShouldContain(PrdCaveat);
    }

    [Fact]
    public void HybridHuman_WithoutExplain_NoCaveat()
    {
        HybridSearchResult payload = BuildHybridResult(withExplain: false);
        using var writer = new StringWriter() { NewLine = "\n" };

        new HybridSearchResultHumanFormatter().Write(payload, writer);
        string output = writer.ToString();

        output.ShouldNotContain(PrdCaveat);
        output.ShouldContain("1. [0.750]");
    }

    [Fact]
    public void SingleAxisHuman_EmptyResults_NoOutputContent()
    {
        var payload = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "needle",
        };
        using var writer = new StringWriter() { NewLine = "\n" };

        new SearchResultHumanFormatter().Write(payload, writer);
        writer.ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void SingleAxisHuman_ExplainWithEmptyResults_PrintsCaveatAndPerAxisNormalizationOnly()
    {
        // The dangling-normalization edge: when --explain is set but the server returns zero rows, the
        // formatter still emits the caveat and per-axis normalization lines — it must not crash and it
        // must not emit a "(no results)" nudge (that is Story 7.3's scope, anti-pattern #3).
        var payload = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "needle",
            Explanation = new SearchExplanation
            {
                Caveat = PrdCaveat,
                AxisDetails = new Dictionary<string, AxisExplanation>
                {
                    ["syntactic"] = new() { NormalizationMethod = "bm25_saturation", Description = "BM25 saturation" },
                },
            },
        };
        using var writer = new StringWriter() { NewLine = "\n" };

        new SearchResultHumanFormatter().Write(payload, writer);
        string output = writer.ToString();

        output.ShouldStartWith(PrdCaveat);
        output.ShouldContain("(syntactic: bm25_saturation)");
        output.ShouldNotContain("No results");
        output.ShouldNotContain("1. [");
    }

    private static HybridSearchResult BuildHybridResult(
        bool withExplain,
        bool degraded = false,
        IReadOnlyList<string>? unavailable = null)
    {
        var result = new FusedScoredResult
        {
            MemoryUnitId = "mu-1",
            CompositeScore = 0.75d,
            ContentSnippet = "snippet content",
            SourceUri = "mem://case/mu-1",
            SourceType = SourceType.File,
            SyntacticScore = 0.60,
            SemanticScore = 0.85,
            GraphScore = null,
        };

        SearchExplanation? explanation = withExplain
            ? new SearchExplanation
            {
                Caveat = PrdCaveat,
                AxisDetails = new Dictionary<string, AxisExplanation>
                {
                    ["syntactic"] = new() { NormalizationMethod = "bm25_saturation", Description = "BM25 saturation" },
                    ["semantic"] = new() { NormalizationMethod = "cosine", Description = "cosine similarity" },
                },
            }
            : null;

        return new HybridSearchResult
        {
            Results = [result],
            TotalCount = 1,
            Degraded = degraded,
            UnavailableAxes = unavailable ?? [],
            Query = "needle",
            Explanation = explanation,
        };
    }
}
