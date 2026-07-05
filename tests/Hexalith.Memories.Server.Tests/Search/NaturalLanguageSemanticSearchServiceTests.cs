namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Shouldly;

using StackExchange.Redis;

public class NaturalLanguageSemanticSearchServiceTests
{
    [Fact]
    public void HasRequiredEnrichmentFields_AllRequiredFieldsPresent_ShouldReturnTrue()
    {
        RedisValue[] fields = ["content", "file:///source.md", "file"];

        NaturalLanguageSemanticSearchService.HasRequiredEnrichmentFields(fields).ShouldBeTrue();
    }

    [Fact]
    public void HasRequiredEnrichmentFields_MissingSyntacticHashFields_ShouldReturnFalse()
    {
        RedisValue[] fields = [RedisValue.Null, "file:///source.md", "file"];

        NaturalLanguageSemanticSearchService.HasRequiredEnrichmentFields(fields).ShouldBeFalse();
    }

    [Fact]
    public void TryBuildScoredResult_ValidBackingHash_ShouldAdaptNaturalLanguageHit()
    {
        var hit = new NaturalLanguageSemanticSearchHit(
            "mu-001",
            0.87,
            "LLM-authored incident description",
            0.91f,
            "logprobs");

        bool mapped = NaturalLanguageSemanticSearchService.TryBuildScoredResult(
            hit,
            "Stored source content that should become the snippet.",
            "file:///source.md",
            "file",
            "case-123",
            out ScoredResult? result);

        mapped.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.MemoryUnitId.ShouldBe("mu-001");
        result.Score.ShouldBe(0.87);
        result.Axis.ShouldBe("nl");
        result.SourceUri.ShouldBe("file:///source.md");
        result.SourceType.ShouldBe(SourceType.File);
        result.CaseId.ShouldBe("case-123");
        result.ContentSnippet.ShouldBe("Stored source content that should become the snippet.");
    }

    [Fact]
    public void TryBuildScoredResult_InvalidSourceType_ShouldDropHit()
    {
        var hit = new NaturalLanguageSemanticSearchHit(
            "mu-002",
            0.5,
            "Description",
            null,
            "unknown");

        bool mapped = NaturalLanguageSemanticSearchService.TryBuildScoredResult(
            hit,
            "content",
            "file:///source.md",
            "not-a-source-type",
            null,
            out ScoredResult? result);

        mapped.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
