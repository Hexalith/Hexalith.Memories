namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;

using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;

using Shouldly;

public class SearchEndpointErrorResponseFactoryTests
{
    [Fact]
    public void CreateEmbeddingUnavailable_WithEmbeddingApiException_ShouldReturnStructuredError()
    {
        EmbeddingApiException exception = new("Embedding provider returned HTTP 500.", "tenant-123");

        ErrorResponse response = SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(exception);

        response.Code.ShouldBe("EMBEDDING_UNAVAILABLE");
        response.Message.ShouldBe(exception.Message);
        response.Suggestion.ShouldBe("Check embedding provider configuration or retry later.");
    }

    [Fact]
    public void CreateEmbeddingUnavailable_WithRateLimitException_ShouldReturnStructuredError()
    {
        EmbeddingRateLimitException exception = new("tenant-123");

        ErrorResponse response = SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(exception);

        response.Code.ShouldBe("EMBEDDING_UNAVAILABLE");
        response.Message.ShouldBe(exception.Message);
        response.Suggestion.ShouldBe("Check embedding provider configuration or retry later.");
    }

    [Fact]
    public void CreateDimensionMismatch_ShouldReturnStructuredError()
    {
        SemanticSearchDimensionMismatchException exception = new(384, 768);

        ErrorResponse response = SearchEndpointErrorResponseFactory.CreateDimensionMismatch(exception);

        response.Code.ShouldBe("DIMENSION_MISMATCH");
        response.Message.ShouldContain("384 dimensions");
        response.Message.ShouldContain("768");
        response.Suggestion.ShouldBe("Align the tenant embedding configuration with the indexed vector dimensions, then reindex and retry.");
    }
}