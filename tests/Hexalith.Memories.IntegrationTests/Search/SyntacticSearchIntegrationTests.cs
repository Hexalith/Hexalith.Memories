namespace Hexalith.Memories.IntegrationTests.Search;

using System.Diagnostics;

using DotNet.Testcontainers.Builders;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging.Abstractions;

using NRedisStack.RedisStackCommands;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests verifying SyntacticSearchService against a real Redis Stack instance.
/// Validates BM25 ranking, tenant isolation, case scoping, pagination, and latency.
/// </summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public partial class SyntacticSearchIntegrationTests
{
    private readonly RedisStackFixture _redis;

    public SyntacticSearchIntegrationTests(RedisStackFixture redis) => _redis = redis;




























    private static SyntacticSearchService CreateForwardedService(
        IConnectionMultiplexer connection,
        Func<string, List<object>, Task<RedisResult>> execute)
    {
        IDatabase db = connection.GetDatabase();
        IDatabase forwardedDb = Substitute.For<IDatabase>();
        forwardedDb.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(call => db.StringGetAsync(call.Arg<RedisKey>(), call.Arg<CommandFlags>()));
        forwardedDb.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call => execute(call.Arg<string>(), [.. call.Arg<ICollection<object>>() ]));
        IConnectionMultiplexer forwarded = Substitute.For<IConnectionMultiplexer>();
        forwarded.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(forwardedDb);
        return new SyntacticSearchService(forwarded, NullLogger<SyntacticSearchService>.Instance);
    }

    private static RedisResult ProfileQueryResult(RedisResult profile, bool resp3)
        => resp3 ? profile : ((RedisResult[])profile!)[0];

    private static RedisResult WithTimeoutWarning(RedisResult profile, bool resp3)
    {
        RedisResult[] values = (RedisResult[])profile!;
        if (!resp3)
        {
            return RedisResult.Create([values[0], RedisResult.Create([
                RedisResult.Create([RedisResult.Create((RedisValue)"Warning"), RedisResult.Create((RedisValue)"Timeout limit was reached")])])]);
        }

        int profileIndex = Array.FindIndex(values, value => value.ToString() == "profile");
        RedisResult[] details = (RedisResult[])values[profileIndex + 1]!;
        int warningIndex = Array.FindIndex(details, value => value.ToString() == "Warning");
        details[warningIndex + 1] = RedisResult.Create((RedisValue)"Timeout limit was reached");
        values[profileIndex + 1] = RedisResult.Create(details);
        return RedisResult.Create(values);
    }

    private static int ReadRawRows(RedisResult result, bool withScores = false)
    {
        RedisResult[] entries = (RedisResult[]?)result ?? [];
        if (long.TryParse(entries[0].ToString(), out _))
        {
            return withScores ? (entries.Length - 1) / 3 : entries.Length - 1;
        }

        for (int i = 0; i + 1 < entries.Length; i += 2)
        {
            if (entries[i].ToString() == "results")
            {
                return ((RedisResult[]?)entries[i + 1] ?? []).Length;
            }
        }

        throw new InvalidOperationException("Missing raw Redis rows.");
    }

    private static long ReadRawTotal(RedisResult result)
    {
        RedisResult[] entries = (RedisResult[]?)result ?? [];
        if (long.TryParse(entries[0].ToString(), out long total))
        {
            return total;
        }

        for (int i = 0; i + 1 < entries.Length; i += 2)
        {
            if (entries[i].ToString() == "total_results")
            {
                return long.Parse(entries[i + 1].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException("Missing raw Redis total.");
    }

    private static Task SeedRawDocumentAsync(IDatabase db, string tenantId, string memoryUnitId, string content)
        => db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId),
        [
            new HashEntry("content", content),
            new HashEntry("sourceUri", $"tenant:{memoryUnitId}"),
            new HashEntry("sourceType", "file"),
        ]);

    private Task<ConnectionMultiplexer> ConnectAsync(bool resp3)
        => RedisStackFixture.ConnectWhenReadyAsync(
            _redis.ConnectionString,
            resp3 ? RedisProtocol.Resp3 : RedisProtocol.Resp2);

    private static string RankedContent(int index)
        => string.Join(' ', Enumerable.Repeat("boundaryrank", index % 3 + 1)
            .Concat(Enumerable.Repeat("filler", 2 - index % 3)));

    private SyntacticSearchService CreateService()
        => new(_redis.Connection, NullLogger<SyntacticSearchService>.Instance);

    private static void AssertHighlightedSnippet(ScoredResult result)
    {
        result.ContentSnippet.ShouldContain("<b>payment</b>");
        result.ContentSnippet.ShouldContain("<b>outage</b>");
        result.ContentSnippet.Length.ShouldBeLessThanOrEqualTo(SearchSnippetBuilder.MaxSnippetLength + 3);
    }

    private static async Task<(SearchResult Result, long ElapsedMilliseconds)> MeasureSearchAsync(
        SyntacticSearchService service,
        SearchQuery query)
    {
        Stopwatch sw = Stopwatch.StartNew();
        SearchResult result = await service.SearchAsync(query);
        sw.Stop();
        return (result, sw.ElapsedMilliseconds);
    }

    private async Task SeedDocumentAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string? caseId = null,
        SourceType sourceType = SourceType.File,
        Dictionary<string, MetadataField>? metadata = null)
    {
        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: caseId ?? "default-case",
            sourceType: sourceType)
            with
        {
            Metadata = metadata ?? [],
        };

        IndexSyntacticActivity activity = new(
            _redis.Connection,
            NullLogger<IndexSyntacticActivity>.Instance);

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionSyntacticIndex(_redis.Connection.GetDatabase(), tenantId);
        await activity.RunAsync(context, input);
    }

    private static void ProvisionSyntacticIndex(IDatabase db, string tenantId)
        => TryCreateIndex(() => db.FT().Create(
            IndexSchemaDefinitions.GetSyntacticIndexName(tenantId),
            IndexSchemaDefinitions.CreateSyntacticParams(tenantId),
            IndexSchemaDefinitions.CreateSyntacticSchema()));

    private static void TryCreateIndex(Action create)
    {
        try
        {
            create();
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase))
        {
        }
    }
}
