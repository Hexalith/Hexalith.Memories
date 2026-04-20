// <copyright file="ConsistencyInspectionServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Consistency;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — AC #3 (per-unit inspection) + Risk #4 (Cypher-injection guard via ULID regex
/// validation). Covers the full 6-test inventory in AC #9.
/// </summary>
public class ConsistencyInspectionServiceTests
{
    private const string TestTenantId = "tenant-1";
    private const string ValidUlid = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9";

    [Fact]
    public async Task InspectAsync_AllBackendsPresent_ReturnsInspectionResultWithNoOp()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: true,
            semanticPresent: true,
            graphPresent: true);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, ValidUlid, CancellationToken.None);

        result.Recommendation.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        result.SyntacticPresent.ShouldBeTrue();
        result.SemanticPresent.ShouldBeTrue();
        result.GraphPresent.ShouldBeTrue();
        result.SyntacticDetail.ShouldNotBeNull();
        result.SemanticDetail.ShouldNotBeNull();
        result.GraphDetail.ShouldNotBeNull();
        result.MemoryUnitId.ShouldBe(ValidUlid);
        result.TenantId.ShouldBe(TestTenantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-ulid")]
    [InlineData("01HM5Q9WXGK6T8Q4Z5Y6V7W8X")] // 25 chars
    [InlineData("01HM5Q9WXGK6T8Q4Z5Y6V7W8X9A")] // 27 chars
    [InlineData("01HM5Q9WXGK6T8Q4Z5Y6V7W8X9; DROP GRAPH")] // injection attempt
    [InlineData("01HM5Q9WXGK6T8Q4Z5Y6V7I8U9")] // contains I and U which are excluded
    public async Task InspectAsync_MalformedMemoryUnitId_ThrowsArgumentException(string malformedId)
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        ConsistencyInspectionService service = CreateService(builder: builder);

        await Should.ThrowAsync<ArgumentException>(
            () => service.InspectAsync(TestTenantId, malformedId, CancellationToken.None));

        // Risk #4 guard: the query builder must NOT be called before regex validation passes.
        builder.DidNotReceive().BuildCheckMemoryUnitExists(Arg.Any<string>());
    }

    [Fact]
    public async Task InspectAsync_AllBackendsMissing_ThrowsKeyNotFoundException()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: false,
            semanticPresent: false,
            graphPresent: false);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => service.InspectAsync(TestTenantId, ValidUlid, CancellationToken.None));
    }

    [Fact]
    public async Task InspectAsync_SemanticMissing_ReturnsReIndexSemanticRecommendation()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: true,
            semanticPresent: false,
            graphPresent: true);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, ValidUlid, CancellationToken.None);

        result.Recommendation.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemantic);
        result.SyntacticPresent.ShouldBeTrue();
        result.SemanticPresent.ShouldBeFalse();
        result.GraphPresent.ShouldBeTrue();
        result.SemanticDetail.ShouldBeNull();
        result.SyntacticDetail.ShouldNotBeNull();
        result.GraphDetail.ShouldNotBeNull();
    }

    [Fact]
    public async Task InspectAsync_GraphMissing_ReturnsReIndexGraphRecommendation()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: true,
            semanticPresent: true,
            graphPresent: false);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, ValidUlid, CancellationToken.None);

        result.Recommendation.ShouldBe(ConsistencyRepairRecommendation.ReIndexGraph);
        result.SyntacticPresent.ShouldBeTrue();
        result.SemanticPresent.ShouldBeTrue();
        result.GraphPresent.ShouldBeFalse();
        result.GraphDetail.ShouldBeNull();
    }

    [Fact]
    public async Task InspectAsync_SyntacticMissingOthersPresent_ReturnsRemoveOrphanedRecommendation()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: false,
            semanticPresent: true,
            graphPresent: true);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, ValidUlid, CancellationToken.None);

        result.Recommendation.ShouldBe(ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph);
        result.SyntacticPresent.ShouldBeFalse();
        result.SyntacticDetail.ShouldBeNull();
    }

    [Fact]
    public async Task InspectAsync_CancelledBeforeProbe_ThrowsOperationCanceledException()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: true,
            semanticPresent: true,
            graphPresent: true);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.InspectAsync(TestTenantId, ValidUlid, cts.Token));
    }

    private static ConsistencyInspectionService CreateService(
        bool syntacticPresent = false,
        bool semanticPresent = false,
        bool graphPresent = false,
        IGraphQueryBuilder? builder = null)
    {
        IDatabase redisDb = Substitute.For<IDatabase>();

        HashEntry[] syntacticEntries = syntacticPresent
            ? CreateSyntacticEntries()
            : [];
        HashEntry[] semanticEntries = semanticPresent
            ? CreateSemanticEntries()
            : [];

        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(k => k.ToString()!.Contains(":mu:")),
                Arg.Any<CommandFlags>())
            .Returns(syntacticEntries);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(k => k.ToString()!.Contains(":vec:")),
                Arg.Any<CommandFlags>())
            .Returns(semanticEntries);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        IConnectionMultiplexer falkorMux = CreateFalkorMultiplexer(graphPresent);
        IGraphQueryBuilder effectiveBuilder = builder ?? CreateMockBuilder();

        return new ConsistencyInspectionService(
            redis,
            falkorMux,
            effectiveBuilder,
            Substitute.For<ILogger<ConsistencyInspectionService>>());
    }

    private static IGraphQueryBuilder CreateMockBuilder()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        builder.BuildCheckMemoryUnitExists(Arg.Any<string>())
            .Returns(("MATCH (m:MemoryUnit {id: $id}) RETURN m.id", (IDictionary<string, object>)new Dictionary<string, object> { ["id"] = "mock" }));
        builder.BuildCountMemoryUnitEdges(Arg.Any<string>())
            .Returns(("MATCH (m:MemoryUnit {id: $id}) RETURN 0,0,0", (IDictionary<string, object>)new Dictionary<string, object> { ["id"] = "mock" }));
        return builder;
    }

    private static IConnectionMultiplexer CreateFalkorMultiplexer(bool nodeExists)
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer mux = Substitute.For<IConnectionMultiplexer>();
        mux.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisResult existsResponse = nodeExists
            ? Activities.Indexing.VerifyConsistencyActivityTestsFactory.BuildStringIdRows(["node-id"])
            : Activities.Indexing.VerifyConsistencyActivityTestsFactory.BuildStringIdRows([]);
        RedisResult edgeCountsResponse =
            Activities.Indexing.VerifyConsistencyActivityTestsFactory.BuildEdgeCountsResponse(2, 3, 1);

        // First call returns exists-check; second call returns edge counts.
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(existsResponse, edgeCountsResponse);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(existsResponse, edgeCountsResponse);

        return mux;
    }

    private static HashEntry[] CreateSyntacticEntries() =>
    [
        new("contentHash", "abc123"),
        new("ingestedAt", "2026-04-20T10:00:00+00:00"),
        new("sourceUri", "file:///sample.md"),
        new("sourceType", "file"),
        new("caseId", "case-1"),
        new("embeddingProvider", "gemini"),
        new("embeddingModel", "gemini-embedding-001"),
    ];

    private static HashEntry[] CreateSemanticEntries() =>
    [
        new("embedding", new byte[1536 * sizeof(float)]),
        new("memoryUnitId", ValidUlid),
        new("caseId", "case-1"),
    ];
}
