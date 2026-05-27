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
            $"/api/search?tenantId={tenantId}&query=test&axis=not-a-real-axis");

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
            $"/api/search?tenantId={tenantId}&query=routing default axis content");

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
            $"/api/search?tenantId={tenantId}&query=routing semantic axis content&axis=semantic");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Results[0].Axis.ShouldBe("semantic");
    }

    private async Task SeedDocumentAsync(string tenantId, string memoryUnitId, string content)
    {
        float[] vector = await _embeddingClient.GenerateAsync(
            content,
            tenantId,
            _embeddingConfig,
            CancellationToken.None);

        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: "default-case",
            embeddingVector: vector,
            embeddingDimensions: _embeddingConfig.Dimensions);

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
