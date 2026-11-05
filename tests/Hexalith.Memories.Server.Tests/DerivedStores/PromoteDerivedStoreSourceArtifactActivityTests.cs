namespace Hexalith.Memories.Server.Tests.DerivedStores;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.DerivedStores;
using Hexalith.Memories.Server.Ingestion;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

/// <summary>Exact source promotion refuses unrelated dedup owners before payload I/O or artifact persistence.</summary>
public sealed class PromoteDerivedStoreSourceArtifactActivityTests
{
    [Theory]
    [InlineData("tenant")]
    [InlineData("case")]
    [InlineData("source")]
    [InlineData("owner")]
    [InlineData("kind")]
    public async Task ForeignSourceScopeCannotPromoteBytes(string fault)
    {
        string sourceOwner = DedupKeyBuilder.BuildKey("tenant", "case", "event-source");
        var reference = new WorkflowPayloadReference("opaque-source", new string('a', 64), 3,
            WorkflowPayloadKind.SourceBytes, "tenant", sourceOwner);
        var input = new PromoteDerivedStoreSourceArtifactInput("tenant", "output", "case", "event-source", SourceType.Event,
            "application/json", [], reference, "provider", "model", 3, [], "operator", null, null, "{}", DateTimeOffset.UtcNow);
        input = fault switch
        {
            "tenant" => input with { TenantId = "foreign" },
            "case" => input with { CaseId = "foreign" },
            "source" => input with { SourceUri = "foreign" },
            "owner" => input with { SourceReference = reference with { MemoryUnitId = "foreign" } },
            _ => input with { SourceReference = reference with { ContentKind = WorkflowPayloadKind.ChunkText } },
        };
        var payloads = Substitute.For<IWorkflowPayloadStore>();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var activity = new PromoteDerivedStoreSourceArtifactActivity(payloads, new RedisDerivedStoreService(redis, redis, TimeProvider.System));
        var refused = await Should.ThrowAsync<DerivedStoreStateException>(() => activity.RunAsync(null!, input));
        refused.Code.ShouldBe("SOURCE_ARTIFACT_SCOPE_INVALID");
        payloads.ReceivedCalls().ShouldBeEmpty();
        redis.ReceivedCalls().ShouldBeEmpty();
    }
}
