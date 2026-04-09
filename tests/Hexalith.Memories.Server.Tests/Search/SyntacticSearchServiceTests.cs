namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using NRedisStack.Search;

using Shouldly;

using StackExchange.Redis;

public class SyntacticSearchServiceTests
{
    [Fact]
    public void MapDocumentToScoredResult_ShouldExtractMemoryUnitId()
    {
        Document doc = CreateDocument("tenant1:mu:abc123", 5.0);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "tenant1");

        result.MemoryUnitId.ShouldBe("abc123");
    }

    [Fact]
    public void MapDocumentToScoredResult_ShouldPreserveBm25Score()
    {
        Document doc = CreateDocument("tenant1:mu:mu-001", 12.5);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "tenant1");

        result.Score.ShouldBe(12.5);
    }

    [Fact]
    public void MapDocumentToScoredResult_ShouldSetAxisToSyntactic()
    {
        Document doc = CreateDocument("tenant1:mu:mu-001", 1.0);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "tenant1");

        result.Axis.ShouldBe("syntactic");
    }

    [Theory]
    [InlineData("file", SourceType.File)]
    [InlineData("File", SourceType.File)]
    [InlineData("event", SourceType.Event)]
    [InlineData("Event", SourceType.Event)]
    [InlineData("url", SourceType.Url)]
    public void MapDocumentToScoredResult_ShouldParseSourceTypeCaseInsensitive(string sourceTypeValue, SourceType expected)
    {
        Document doc = CreateDocument("t:mu:id", 1.0, sourceType: sourceTypeValue);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.SourceType.ShouldBe(expected);
    }

    [Fact]
    public void MapDocumentToScoredResult_UnknownSourceType_ShouldFallbackToFile()
    {
        Document doc = CreateDocument("t:mu:id", 1.0, sourceType: "unknownType");

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.SourceType.ShouldBe(SourceType.File);
    }

    [Fact]
    public void MapDocumentToScoredResult_LongContent_ShouldTruncateAtWordBoundary()
    {
        string longContent = string.Join(' ', Enumerable.Repeat("word", 100));
        Document doc = CreateDocument("t:mu:id", 1.0, content: longContent);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.ContentSnippet.Length.ShouldBeLessThanOrEqualTo(203); // 200 + "..."
        result.ContentSnippet.ShouldEndWith("...");
    }

    [Fact]
    public void MapDocumentToScoredResult_ShortContent_ShouldNotTruncate()
    {
        string shortContent = "This is a short snippet.";
        Document doc = CreateDocument("t:mu:id", 1.0, content: shortContent);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.ContentSnippet.ShouldBe(shortContent);
        result.ContentSnippet.ShouldNotContain("...");
    }

    [Fact]
    public void EscapeRedisQuery_ShouldEscapeHyphens()
    {
        string result = SyntacticSearchService.EscapeRedisQuery("claim-denied");

        result.ShouldBe(@"claim\-denied");
    }

    [Fact]
    public void EscapeRedisQuery_ShouldEscapeAtSymbol()
    {
        string result = SyntacticSearchService.EscapeRedisQuery("@admin");

        result.ShouldBe(@"\@admin");
    }

    [Fact]
    public void EscapeRedisQuery_ShouldEscapeCurlyBraces()
    {
        string result = SyntacticSearchService.EscapeRedisQuery("{value}");

        result.ShouldBe(@"\{value\}");
    }

    [Fact]
    public void EscapeRedisQuery_ShouldEscapePipe()
    {
        string result = SyntacticSearchService.EscapeRedisQuery("a|b");

        result.ShouldBe(@"a\|b");
    }

    [Fact]
    public void EscapeRedisQuery_QueryInjection_ShouldEscapeFieldFilter()
    {
        string result = SyntacticSearchService.EscapeRedisQuery("@sourceType:{file}");

        result.ShouldBe(@"\@sourceType\:\{file\}");
    }

    [Fact]
    public void EscapeRedisQuery_AllSpecialChars_ShouldEscapeAll()
    {
        string result = SyntacticSearchService.EscapeRedisQuery("---");

        result.ShouldBe(@"\-\-\-");
    }

    [Fact]
    public void EscapeRedisQuery_PlainText_ShouldNotEscape()
    {
        string result = SyntacticSearchService.EscapeRedisQuery("hello world");

        result.ShouldBe("hello world");
    }

    [Fact]
    public void BuildSearchTermsQuery_SingleTerm_ShouldReturnEscapedTerm()
    {
        string result = SyntacticSearchService.BuildSearchTermsQuery("claim-denied");

        result.ShouldBe(@"claim\-denied");
    }

    [Fact]
    public void BuildSearchTermsQuery_KeywordPhrase_ShouldPreservePhraseSemantics()
    {
        string result = SyntacticSearchService.BuildSearchTermsQuery("payment processing outage");

        result.ShouldBe("payment processing outage");
    }

    [Fact]
    public void BuildSearchTermsQuery_NaturalLanguageQuestion_ShouldUseOrSemantics()
    {
        string result = SyntacticSearchService.BuildSearchTermsQuery("what caused the payment outage in march?");

        result.ShouldBe("(what | caused | the | payment | outage | in | march\\?)");
    }

    [Fact]
    public void BuildSearchTermsQuery_NaturalLanguagePromptWithDuplicateTerms_ShouldDeduplicate()
    {
        string result = SyntacticSearchService.BuildSearchTermsQuery("show show the outage findings now");

        result.ShouldBe("(show | the | outage | findings | now)");
    }

    [Fact]
    public void BuildQueryString_WithoutCaseId_ShouldReturnSearchTermsOnly()
    {
        string result = SyntacticSearchService.BuildQueryString("escaped terms", null);

        result.ShouldBe("escaped terms");
    }

    [Fact]
    public void BuildQueryString_WithCaseId_ShouldAddTagFilter()
    {
        string result = SyntacticSearchService.BuildQueryString(@"claim\-denied", "case-1");

        result.ShouldBe(@"@caseId:{case\-1} claim\-denied");
    }

    [Fact]
    public void BuildQueryString_WithEmptyCaseId_ShouldReturnSearchTermsOnly()
    {
        string result = SyntacticSearchService.BuildQueryString("terms", "");

        result.ShouldBe("terms");
    }

    [Fact]
    public void BuildQueryString_CaseIdInjection_ShouldEscapeSpecialChars()
    {
        string result = SyntacticSearchService.BuildQueryString("terms", "} @content:{secret");

        result.ShouldNotContain("@content:{secret");
        result.ShouldContain(@"\}");
        result.ShouldContain(@"\@content");
    }

    private static Document CreateDocument(
        string id,
        double score,
        string content = "Test content for search",
        string sourceUri = "file:///test.pdf",
        string sourceType = "file")
    {
        var fields = new Dictionary<string, RedisValue>
        {
            ["content"] = content,
            ["sourceUri"] = sourceUri,
            ["sourceType"] = sourceType,
            ["metadataJson"] = "{}",
            ["ingestedBy"] = "user@test.com",
            ["ingestedAt"] = "2026-03-31T10:00:00+00:00",
        };

        return new Document(id, fields, score);
    }
}
