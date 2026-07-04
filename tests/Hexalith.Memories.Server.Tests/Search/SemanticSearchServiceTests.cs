namespace Hexalith.Memories.Server.Tests.Search;

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
    public void BuildKnnQueryString_WithoutCaseId_ShouldReturnWildcardKnn()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, null);

        result.ShouldBe("*=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithEmptyCaseId_ShouldReturnWildcardKnn()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, "");

        result.ShouldBe("*=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithCaseId_ShouldAddTagFilter()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, "case-1");

        result.ShouldBe(@"@caseId:{case\-1}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithCaseIdContainingSpecialChars_ShouldEscapeProperly()
    {
        string result = SemanticSearchService.BuildKnnQueryString(5, "case@special|value");

        result.ShouldContain(@"\@");
        result.ShouldContain(@"\|");
        result.ShouldContain("=>[KNN 5 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_DifferentMaxResults_ShouldReflectInQuery()
    {
        string result = SemanticSearchService.BuildKnnQueryString(25, null);

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
    public void BuildKnnQueryString_WithSourceTypeFilter_ShouldAddTagFilter()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, null, "file");

        result.ShouldBe("@sourceType:{file}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithCloudEventSubject_ShouldAddTagFilter()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, null, null, "claim-42");

        result.ShouldBe(@"@cloudeventSubject:{claim\-42}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithCaseIdAndSourceType_ShouldCombineFilters()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, "case-1", "file", "claim-42");

        result.ShouldContain(@"@caseId:{case\-1}");
        result.ShouldContain("@sourceType:{file}");
        result.ShouldContain(@"@cloudeventSubject:{claim\-42}");
        result.ShouldContain("=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithEmptySourceType_ShouldIgnore()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, null, "");

        result.ShouldBe("*=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithSourceTypeContainingSpecialChars_ShouldEscape()
    {
        string result = SemanticSearchService.BuildKnnQueryString(10, null, "file-type");

        result.ShouldContain(@"@sourceType:{file\-type}");
    }

    [Fact]
    public void BuildKnnQueryString_WithAdversarialSubject_ShouldEscapeTagOperatorsBeforeKnnClause()
    {
        string result = SemanticSearchService.BuildKnnQueryString(
            10,
            null,
            null,
            "claim} @content:{secret}|*");

        result.ShouldBe(@"@cloudeventSubject:{claim\} \@content\:\{secret\}\|\*}=>[KNN 10 @embedding $query_vec AS __vector_score]");
    }

    [Fact]
    public void BuildKnnQueryString_WithAdversarialCaseAndSource_ShouldEscapeTagOperatorsBeforeKnnClause()
    {
        string result = SemanticSearchService.BuildKnnQueryString(
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

    [Fact]
    public void HasRequiredEnrichmentFields_WithExtraFields_ShouldStillReturnTrue()
    {
        bool result = SemanticSearchService.HasRequiredEnrichmentFields(
            [new RedisValue("content"), new RedisValue("file:///doc.txt"), new RedisValue("file"), new RedisValue("case-1"), new RedisValue("metadata text")]);

        result.ShouldBeTrue();
    }
}
