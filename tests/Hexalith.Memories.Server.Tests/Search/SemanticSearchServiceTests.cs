namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Search;

using Shouldly;

using StackExchange.Redis;

public class SemanticSearchServiceTests
{
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(0.3, 0.7)]
    [InlineData(2.0, 0.0)]
    public void ConvertDistanceToSimilarity_ShouldConvertCorrectly(double distance, double expectedSimilarity)
    {
        double result = SemanticSearchService.ConvertDistanceToSimilarity(distance);

        result.ShouldBe(expectedSimilarity, 0.0001);
    }

    [Fact]
    public void ConvertDistanceToSimilarity_NegativeDistance_ShouldClampToMax()
    {
        double result = SemanticSearchService.ConvertDistanceToSimilarity(-0.5);

        result.ShouldBe(1.0);
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithoutCaseId_ShouldReturnWildcardKnn()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, null);

        result.ShouldBe("*=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithEmptyCaseId_ShouldReturnWildcardKnn()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, "");

        result.ShouldBe("*=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithCaseId_ShouldAddTagFilter()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, "case-1");

        result.ShouldBe(@"@caseId:{case\-1}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithCaseIdContainingSpecialChars_ShouldEscapeProperly()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(5, "case@special|value");

        result.ShouldContain(@"\@");
        result.ShouldContain(@"\|");
        result.ShouldContain("=>[KNN 5 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_DifferentCandidateCount_ShouldReflectInQuery()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(25, null);

        result.ShouldBe("*=>[KNN 25 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void HasRequiredEnrichmentFields_WithAllRequiredValues_ShouldReturnTrue()
    {
        bool result = SemanticSearchService.HasRequiredEnrichmentFields(
            [new RedisValue("content"), new RedisValue("file:///doc.txt"), new RedisValue("file")]);

        result.ShouldBeTrue();
    }

    [Fact]
    public void HasRequiredEnrichmentFields_WithMissingSourceUri_ShouldReturnFalse()
    {
        bool result = SemanticSearchService.HasRequiredEnrichmentFields(
            [new RedisValue("content"), RedisValue.Null, new RedisValue("file")]);

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasRequiredEnrichmentFields_WithMissingSourceType_ShouldReturnFalse()
    {
        bool result = SemanticSearchService.HasRequiredEnrichmentFields(
            [new RedisValue("content"), new RedisValue("file:///doc.txt"), RedisValue.Null]);

        result.ShouldBeFalse();
    }

    [Fact]
    public void BuildKnnCandidateQueryString_ShouldNotEmitSourceTypePreFilter()
    {
        // Story 22.6 (A49): sourceType is not indexed on the raw semantic vector hash, so the builder
        // must never emit a @sourceType KNN pre-filter (it would silently drop matches). Source-type
        // recall is handled by a bounded service-side post-filter during enrichment instead.
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, "case-1", "claim-42");

        result.ShouldNotContain("@sourceType");
        result.ShouldBe(@"@caseId:{case\-1} @cloudeventSubject:{claim\-42}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithCloudEventSubject_ShouldAddTagFilter()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, null, "claim-42");

        result.ShouldBe(@"@cloudeventSubject:{claim\-42}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithCaseIdAndCloudEventSubject_ShouldCombineFilters()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, "case-1", "claim-42");

        result.ShouldContain(@"@caseId:{case\-1}");
        result.ShouldContain(@"@cloudeventSubject:{claim\-42}");
        result.ShouldNotContain("@sourceType");
        result.ShouldContain("=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithEmptyCloudEventSubject_ShouldIgnore()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, null, "");

        result.ShouldBe("*=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithAdversarialSubject_ShouldEscapeTagOperatorsBeforeKnnClause()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(
            10,
            null,
            "claim} @content:{secret}|*");

        result.ShouldBe(@"@cloudeventSubject:{claim\} \@content\:\{secret\}\|\*}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithAdversarialCaseAndSubject_ShouldEscapeTagOperatorsBeforeKnnClause()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(
            10,
            "case} | @sourceType:{event}",
            "subject=>[KNN 100 @embedding $query_vec]");

        result.ShouldContain(@"@caseId:{case\} \| \@sourceType\:\{event\}}");
        result.ShouldContain(@"@cloudeventSubject:{subject\=\>\[KNN 100 \@embedding \$query_vec\]}");
        result.ShouldContain("=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Theory]
    [InlineData("ERR Syntax error at offset 12 near '@content:{secret}'")]
    [InlineData("Syntax error")]
    [InlineData("Could not parse query")]
    public void IsQuerySyntaxError_WithParserMessages_ShouldReturnTrue(string message)
    {
        RediSearchErrorClassifier.IsQuerySyntaxError(new RedisServerException(message)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("ERR blob size does not match index vector dimension")]
    [InlineData("Vector dimension mismatch")]
    public void IsVectorDimensionMismatchError_WithDimensionMessages_ShouldReturnTrue(string message)
    {
        RediSearchErrorClassifier.IsVectorDimensionMismatchError(new RedisServerException(message)).ShouldBeTrue();
    }

    [Fact]
    public void IsVectorDimensionMismatchError_WithParserMessage_ShouldReturnFalse()
    {
        RediSearchErrorClassifier.IsVectorDimensionMismatchError(
            new RedisServerException("ERR Syntax error at offset 12")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 2, 2)]
    [InlineData(2, 2, 4)]
    [InlineData(-5, 2, 2)]
    [InlineData(150, 10, 160)]
    [InlineData(900, 100, 1000)]
    public void CalculateKnnCandidateCount_WithOffset_ShouldReturnCandidateWindow(
        int offset,
        int maxResults,
        int expectedCandidateCount)
    {
        int result = SemanticSearchService.CalculateKnnCandidateCount(offset, maxResults);

        result.ShouldBe(expectedCandidateCount);
    }

    [Fact]
    public void CalculateKnnCandidateCount_WhenWindowExceedsLimit_ShouldThrowArgumentOutOfRange()
    {
        Should.Throw<SearchPaginationLimitExceededException>(
            () => SemanticSearchService.CalculateKnnCandidateCount(901, 100));
    }

    [Fact]
    public void CalculateKnnCandidateCount_WhenOffsetAdditionOverflows_ShouldThrowArgumentOutOfRange()
    {
        Should.Throw<SearchPaginationLimitExceededException>(
            () => SemanticSearchService.CalculateKnnCandidateCount(int.MaxValue, 100));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(20, 30)]
    [InlineData(900, 100)]
    public void CalculateKnnCandidateCount_WithServiceSidePostFilter_ShouldExpandToMaxCandidateWindow(
        int offset,
        int maxResults)
    {
        // Story 22.6 (A49): when a metadata/source-type post-filter is present, over-fetch up to the
        // bounded cap so filtered recall survives beyond the initial offset+max window.
        int result = SemanticSearchService.CalculateKnnCandidateCount(offset, maxResults, hasServiceSidePostFilter: true);

        result.ShouldBe(SearchPaginationOptions.MaxCandidateWindow);
    }

    [Fact]
    public void CalculateKnnCandidateCount_WithoutServiceSidePostFilter_ShouldReturnBaseWindow()
    {
        int result = SemanticSearchService.CalculateKnnCandidateCount(20, 30, hasServiceSidePostFilter: false);

        result.ShouldBe(50);
    }

    [Fact]
    public void CalculateKnnCandidateCount_WithServiceSidePostFilter_ShouldNotExceedMaxCandidateWindow()
    {
        int result = SemanticSearchService.CalculateKnnCandidateCount(0, 100, hasServiceSidePostFilter: true);

        result.ShouldBeLessThanOrEqualTo(SearchPaginationOptions.MaxCandidateWindow);
    }

    [Fact]
    public void CalculateKnnCandidateCount_WithServiceSidePostFilterBeyondCap_ShouldThrowPaginationLimit()
    {
        // AC5: expansion must not bypass the bounded candidate window; an offset page that cannot be
        // served within the cap still raises the established PAGINATION_LIMIT_EXCEEDED behaviour.
        Should.Throw<SearchPaginationLimitExceededException>(
            () => SemanticSearchService.CalculateKnnCandidateCount(901, 100, hasServiceSidePostFilter: true));
    }

    [Fact]
    public void RequiresServiceSidePostFilter_WithMetadataQuery_ShouldReturnTrue()
    {
        SemanticSearchService.RequiresServiceSidePostFilter(
            new SearchQuery { TenantId = "tenant-a", Query = "q", MetadataQuery = "acme" }).ShouldBeTrue();
    }

    [Fact]
    public void RequiresServiceSidePostFilter_WithSourceTypeFilter_ShouldReturnTrue()
    {
        SemanticSearchService.RequiresServiceSidePostFilter(
            new SearchQuery { TenantId = "tenant-a", Query = "q", SourceTypeFilter = "url" }).ShouldBeTrue();
    }

    [Fact]
    public void RequiresServiceSidePostFilter_WithCaseAndSubjectOnly_ShouldReturnFalse()
    {
        // caseId and cloudeventSubject are indexed TAG pre-filters, not service-side post-filters,
        // so they must not trigger candidate-window expansion.
        SemanticSearchService.RequiresServiceSidePostFilter(
            new SearchQuery { TenantId = "tenant-a", Query = "q", CaseId = "case-1", CloudEventSubject = "claim-42" }).ShouldBeFalse();
    }

    [Fact]
    public void ValidateGraphScopeKeys_WithTenantScopedSemanticKeys_ShouldReturnDistinctKeys()
    {
        RedisKey[] keys = SemanticSearchService.ValidateGraphScopeKeys(
            "tenant-a",
            [
                IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-1"),
                IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-1"),
                IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-2"),
            ]);

        keys.ShouldBe(
        [
            IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-1"),
            IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-2"),
        ]);
    }

    [Fact]
    public void ValidateGraphScopeKeys_WithForeignTenantSemanticKey_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => SemanticSearchService.ValidateGraphScopeKeys(
            "tenant-a",
            [IndexSchemaDefinitions.BuildSemanticKey("tenant-b", "mu-1")]));
    }

    [Fact]
    public void HasRequiredEnrichmentFields_WithExtraFields_ShouldStillReturnTrue()
    {
        bool result = SemanticSearchService.HasRequiredEnrichmentFields(
            [new RedisValue("content"), new RedisValue("file:///doc.txt"), new RedisValue("file"), new RedisValue("case-1"), new RedisValue("metadata text")]);

        result.ShouldBeTrue();
    }
}
