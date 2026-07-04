namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Search;

using NRedisStack.Search;

using Shouldly;

using StackExchange.Redis;

public class SyntacticSearchServiceTests
{
    [Fact]
    public void MapDocumentToScoredResult_ShouldExtractMemoryUnitId()
    {
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("tenant1", "abc123"), 5.0);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "tenant1");

        result.MemoryUnitId.ShouldBe("abc123");
    }

    [Fact]
    public void MapDocumentToScoredResult_ShouldPreserveBm25Score()
    {
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("tenant1", "mu-001"), 12.5);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "tenant1");

        result.Score.ShouldBe(12.5);
    }

    [Fact]
    public void MapDocumentToScoredResult_ShouldSetAxisToSyntactic()
    {
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("tenant1", "mu-001"), 1.0);

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
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("t", "id"), 1.0, sourceType: sourceTypeValue);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.SourceType.ShouldBe(expected);
    }

    [Fact]
    public void MapDocumentToScoredResult_UnknownSourceType_ShouldFallbackToFile()
    {
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("t", "id"), 1.0, sourceType: "unknownType");

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.SourceType.ShouldBe(SourceType.File);
    }

    [Fact]
    public void MapDocumentToScoredResult_LongContent_ShouldTruncateAtWordBoundary()
    {
        string longContent = string.Join(' ', Enumerable.Repeat("word", 100));
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("t", "id"), 1.0, content: longContent);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.ContentSnippet.Length.ShouldBeLessThanOrEqualTo(203); // 200 + "..."
        result.ContentSnippet.ShouldEndWith("...");
    }

    [Fact]
    public void MapDocumentToScoredResult_ShortContent_ShouldNotTruncate()
    {
        string shortContent = "This is a short snippet.";
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("t", "id"), 1.0, content: shortContent);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "t");

        result.ContentSnippet.ShouldBe(shortContent);
        result.ContentSnippet.ShouldNotContain("...");
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

    [Fact]
    public void BuildQueryString_WithSourceTypeFilter_ShouldAddTagFilter()
    {
        string result = SyntacticSearchService.BuildQueryString("terms", null, "file");

        result.ShouldBe("@sourceType:{file} terms");
    }

    [Fact]
    public void BuildQueryString_WithAdversarialSourceType_ShouldEscapeTagOperators()
    {
        string result = SyntacticSearchService.BuildQueryString(
            "terms",
            null,
            "file} @caseId:{other}|*");

        result.ShouldBe(@"@sourceType:{file\} \@caseId\:\{other\}\|\*} terms");
    }

    [Fact]
    public void BuildQueryString_WithMetadataQuery_ShouldAddTextFilter()
    {
        string result = SyntacticSearchService.BuildQueryString("terms", null, null, "important");

        result.ShouldBe("@metadataText:(important) terms");
    }

    [Fact]
    public void BuildQueryString_WithCloudEventSubject_ShouldAddExactMatchTagFilter()
    {
        string result = SyntacticSearchService.BuildQueryString("terms", null, null, null, "claim-42");

        result.ShouldBe(@"@cloudeventSubject:{claim\-42} terms");
    }

    [Fact]
    public void BuildQueryString_WithAttributeFilters_ShouldAddExactMatchTagFilters()
    {
        string result = SyntacticSearchService.BuildQueryString(
            "terms",
            null,
            attributeFilters: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["status"] = "Active",
                ["tier"] = "Gold",
            });

        result.ShouldContain(@"@attributeTags:{status\=Active}");
        result.ShouldContain(@"@attributeTags:{tier\=Gold}");
        result.ShouldContain("terms");
    }

    [Fact]
    public void BuildQueryString_WithAdversarialMetadataQuery_ShouldEscapeTextOperators()
    {
        string result = SyntacticSearchService.BuildQueryString(
            "terms",
            null,
            metadataQuery: "@content:{secret} | * -");

        result.ShouldBe(@"@metadataText:(\@content\:\{secret\} \| \* \-) terms");
    }

    [Fact]
    public void BuildQueryString_WithAdversarialAttributeFilters_ShouldEscapeTagComposite()
    {
        string result = SyntacticSearchService.BuildQueryString(
            "terms",
            null,
            attributeFilters: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["role@field"] = "admin|*",
            });

        result.ShouldBe(@"@attributeTags:{role\@field\=admin\|\*} terms");
    }

    [Fact]
    public void BuildQueryString_WithAdversarialSubject_ShouldEscapeTagOperators()
    {
        string result = SyntacticSearchService.BuildQueryString(
            "terms",
            null,
            cloudEventSubject: "claim} @content:{secret}");

        result.ShouldBe(@"@cloudeventSubject:{claim\} \@content\:\{secret\}} terms");
    }

    [Fact]
    public void BuildSearchTermsQuery_WithNegationOnlyText_ShouldNotEmitNegativeClause()
    {
        string result = SyntacticSearchService.BuildSearchTermsQuery("-");

        result.ShouldBe(@"\-");
    }

    [Fact]
    public void BuildSearchTermsQuery_WithWildcardOnlyText_ShouldNotEmitWildcardClause()
    {
        string result = SyntacticSearchService.BuildSearchTermsQuery("*");

        result.ShouldBe(@"\*");
    }

    [Fact]
    public void BuildQueryString_WithAllFilters_ShouldCombineWithAnd()
    {
        string result = SyntacticSearchService.BuildQueryString(
            "terms",
            "case-1",
            "file",
            "important",
            "claim-42",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "Active" });

        result.ShouldContain(@"@caseId:{case\-1}");
        result.ShouldContain("@sourceType:{file}");
        result.ShouldContain(@"@cloudeventSubject:{claim\-42}");
        result.ShouldContain(@"@attributeTags:{status\=Active}");
        result.ShouldContain("@metadataText:(important)");
        result.ShouldContain("terms");
    }

    [Fact]
    public void BuildQueryString_WithCaseIdAndSourceType_ShouldCombineBoth()
    {
        string result = SyntacticSearchService.BuildQueryString("terms", "case-1", "url");

        result.ShouldContain(@"@caseId:{case\-1}");
        result.ShouldContain("@sourceType:{url}");
        result.ShouldContain("terms");
    }

    [Fact]
    public void BuildQueryString_WithEmptySourceTypeFilter_ShouldIgnore()
    {
        string result = SyntacticSearchService.BuildQueryString("terms", null, "");

        result.ShouldBe("terms");
    }

    [Fact]
    public void MapDocumentToScoredResult_WithCaseId_ShouldSetCaseIdProperty()
    {
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("tenant1", "abc123"), 5.0, caseId: "case-abc");

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "tenant1");

        result.CaseId.ShouldBe("case-abc");
    }

    [Fact]
    public void MapDocumentToScoredResult_WithoutCaseId_ShouldHaveNullCaseId()
    {
        Document doc = CreateDocument(IndexSchemaDefinitions.BuildSyntacticKey("tenant1", "abc123"), 5.0);

        ScoredResult result = SyntacticSearchService.MapDocumentToScoredResult(doc, "tenant1");

        result.CaseId.ShouldBeNull();
    }

    private static Document CreateDocument(
        string id,
        double score,
        string content = "Test content for search",
        string sourceUri = "file:///test.pdf",
        string sourceType = "file",
        string? caseId = null)
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

        if (caseId is not null)
        {
            fields["caseId"] = caseId;
        }

        return new Document(id, fields, score);
    }
}
