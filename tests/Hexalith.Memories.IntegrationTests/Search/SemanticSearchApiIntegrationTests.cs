namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

/// <summary>HTTP integration tests for the search endpoint running inside the Aspire topology.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class SemanticSearchApiIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly EmbeddingClient _embeddingClient;
    private readonly TenantEmbeddingConfig _embeddingConfig;

    public SemanticSearchApiIntegrationTests(AspireIngestionPipelineFixture fixture)
    {
        _fixture = fixture;
        _embeddingClient = new EmbeddingClient(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<DaprClient>(),
            CreateFakeEmbeddingConfiguration(),
            CreateDevelopmentHostEnvironment());
        _embeddingConfig = EmbeddingProviderDefaults.Google();
    }

    [Fact]
    public async Task GetSearch_WithInvalidAxis_ShouldReturnBadRequestErrorResponse()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=test&axis=not-a-real-axis");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_AXIS");
        error.Message.ShouldContain("not supported");
    }

    [Fact]
    public async Task GetSearch_WithoutAxis_ShouldDefaultToSyntacticResults()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        await SeedDocumentAsync(tenantId, "mu-default", "routing default axis content");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=routing default axis content");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Results[0].Axis.ShouldBe("syntactic");
    }

    [Fact]
    public async Task GetSearch_WithSemanticAxis_ShouldReturnSemanticResults()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        await SeedDocumentAsync(tenantId, "mu-semantic", "routing semantic axis content");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=routing semantic axis content&axis=semantic");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Results[0].Axis.ShouldBe("semantic");
    }

    [Fact]
    public async Task GetSearch_WithSemanticAxisAndOffset_ShouldReturnDisjointStablePages()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        const string Query = "story 22 semantic api pagination query";

        await SeedDocumentAsync(tenantId, "mu-api-page-01", "story 22 semantic api pagination rank 32");
        await SeedDocumentAsync(tenantId, "mu-api-page-02", "story 22 semantic api pagination rank 99");
        await SeedDocumentAsync(tenantId, "mu-api-page-03", "story 22 semantic api pagination rank 90");
        await SeedDocumentAsync(tenantId, "mu-api-page-04", "story 22 semantic api pagination rank 62");

        string path = $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(Query)}&axis=semantic&maxResults=2";

        using HttpResponseMessage firstResponse = await _fixture.MemoriesClient.GetAsync(path);
        using HttpResponseMessage secondResponse = await _fixture.MemoriesClient.GetAsync($"{path}&offset=2");
        using HttpResponseMessage secondRepeatResponse = await _fixture.MemoriesClient.GetAsync($"{path}&offset=2");

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondRepeatResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? firstPage = await firstResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        SearchResult? secondPage = await secondResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        SearchResult? secondRepeatPage = await secondRepeatResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);

        firstPage.ShouldNotBeNull();
        secondPage.ShouldNotBeNull();
        secondRepeatPage.ShouldNotBeNull();
        firstPage.Results.Count.ShouldBe(2);
        secondPage.Results.Count.ShouldBe(2);
        firstPage.Results.Select(static r => r.MemoryUnitId)
            .Intersect(secondPage.Results.Select(static r => r.MemoryUnitId))
            .ShouldBeEmpty();
        secondRepeatPage.Results.Select(static r => r.MemoryUnitId)
            .ShouldBe(secondPage.Results.Select(static r => r.MemoryUnitId));
        firstPage.Results.Concat(secondPage.Results).ShouldAllBe(static r => r.Axis == "semantic");
    }

    [Fact]
    public async Task GetSearch_WithSemanticAxisAndMetadataQueryBeyondInitialWindow_ShouldReturnLaterFilteredMatches()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        const string Query = "story 22 semantic api metadata recall probe";

        float[] queryVector = await _embeddingClient.GenerateAsync(
            Query,
            tenantId,
            _embeddingConfig,
            CancellationToken.None);
        float[] farVector = NegateVector(queryVector);

        Dictionary<string, MetadataField> nonMatching = new() { ["customer"] = new("globex", MetadataOrigin.Ai, 1.0f) };
        Dictionary<string, MetadataField> matching = new() { ["customer"] = new("acme", MetadataOrigin.Ai, 1.0f) };

        await SeedDocumentAsync(tenantId, "mu-api-near-1", "api nearest metadata miss 1", embeddingVector: queryVector, metadata: nonMatching);
        await SeedDocumentAsync(tenantId, "mu-api-near-2", "api nearest metadata miss 2", embeddingVector: queryVector, metadata: nonMatching);
        await SeedDocumentAsync(tenantId, "mu-api-far-1", "api farther metadata match 1", embeddingVector: farVector, metadata: matching);
        await SeedDocumentAsync(tenantId, "mu-api-far-2", "api farther metadata match 2", embeddingVector: farVector, metadata: matching);

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(Query)}&axis=semantic&metadataQuery=acme&maxResults=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Results.Select(static r => r.MemoryUnitId).ShouldBe(["mu-api-far-1", "mu-api-far-2"]);
        result.Results.ShouldAllBe(static r => r.Axis == "semantic");
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetSearch_WithSemanticAxisAndSourceTypeBeyondInitialWindow_ShouldReturnLaterFilteredMatches()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        const string Query = "story 22 semantic api source recall probe";

        float[] queryVector = await _embeddingClient.GenerateAsync(
            Query,
            tenantId,
            _embeddingConfig,
            CancellationToken.None);
        float[] farVector = NegateVector(queryVector);

        await SeedDocumentAsync(tenantId, "mu-api-file-1", "api nearest source miss 1", sourceType: SourceType.File, embeddingVector: queryVector);
        await SeedDocumentAsync(tenantId, "mu-api-file-2", "api nearest source miss 2", sourceType: SourceType.File, embeddingVector: queryVector);
        await SeedDocumentAsync(tenantId, "mu-api-url-1", "api farther source match 1", sourceType: SourceType.Url, embeddingVector: farVector);
        await SeedDocumentAsync(tenantId, "mu-api-url-2", "api farther source match 2", sourceType: SourceType.Url, embeddingVector: farVector);

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(Query)}&axis=semantic&sourceType=url&maxResults=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Results.Select(static r => r.MemoryUnitId).ShouldBe(["mu-api-url-1", "mu-api-url-2"]);
        result.Results.ShouldAllBe(static r => r.Axis == "semantic" && r.SourceType == SourceType.Url);
        result.TotalCount.ShouldBe(2);
    }

    private async Task SeedDocumentAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        SourceType sourceType = SourceType.File,
        float[]? embeddingVector = null,
        Dictionary<string, MetadataField>? metadata = null)
    {
        float[] vector = embeddingVector ?? await _embeddingClient.GenerateAsync(
            content,
            tenantId,
            _embeddingConfig,
            CancellationToken.None);

        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: "default-case",
            sourceType: sourceType,
            embeddingVector: vector,
            embeddingDimensions: _embeddingConfig.Dimensions)
            with
        {
            Metadata = metadata ?? [],
        };

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();

        IndexSyntacticActivity syntacticActivity = new(
            _fixture.RedisConnection,
            NullLogger<IndexSyntacticActivity>.Instance);
        await syntacticActivity.RunAsync(context, input);

        IndexSemanticActivity semanticActivity = new(
            _fixture.RedisConnection,
            NullLogger<IndexSemanticActivity>.Instance);
        await semanticActivity.RunAsync(context, input);
    }

    private static float[] NegateVector(float[] vector)
    {
        float[] negated = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            negated[i] = -vector[i];
        }

        return negated;
    }

    private static IConfiguration CreateFakeEmbeddingConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = "true",
            })
            .Build();

    private static IHostEnvironment CreateDevelopmentHostEnvironment()
    {
        IHostEnvironment hostEnv = Substitute.For<IHostEnvironment>();
        hostEnv.EnvironmentName.Returns("Development");
        return hostEnv;
    }
}
