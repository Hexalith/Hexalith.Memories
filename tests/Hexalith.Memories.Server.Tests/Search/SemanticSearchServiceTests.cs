namespace Hexalith.Memories.Server.Tests.Search;

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
    public void BuildKnnCandidateQueryString_WithSourceTypeFilter_ShouldAddTagFilter()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, null, "file");

        result.ShouldBe("@sourceType:{file}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithCloudEventSubject_ShouldAddTagFilter()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, null, null, "claim-42");

        result.ShouldBe(@"@cloudeventSubject:{claim\-42}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithCaseIdAndSourceType_ShouldCombineFilters()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, "case-1", "file", "claim-42");

        result.ShouldContain(@"@caseId:{case\-1}");
        result.ShouldContain("@sourceType:{file}");
        result.ShouldContain(@"@cloudeventSubject:{claim\-42}");
        result.ShouldContain("=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithEmptySourceType_ShouldIgnore()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, null, "");

        result.ShouldBe("*=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithSourceTypeContainingSpecialChars_ShouldEscape()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(10, null, "file-type");

        result.ShouldContain(@"@sourceType:{file\-type}");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithAdversarialSubject_ShouldEscapeTagOperatorsBeforeKnnClause()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(
            10,
            null,
            null,
            "claim} @content:{secret}|*");

        result.ShouldBe(@"@cloudeventSubject:{claim\} \@content\:\{secret\}\|\*}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnCandidateQueryString_WithAdversarialCaseAndSource_ShouldEscapeTagOperatorsBeforeKnnClause()
    {
        string result = SemanticSearchService.BuildKnnCandidateQueryString(
            10,
            "case} | @sourceType:{event}",
            "file=>[KNN 100 @embedding $query_vec]");

        result.ShouldContain(@"@caseId:{case\} \| \@sourceType\:\{event\}}");
        result.ShouldContain(@"@sourceType:{file\=\>\[KNN 100 \@embedding \$query_vec\]}");
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
