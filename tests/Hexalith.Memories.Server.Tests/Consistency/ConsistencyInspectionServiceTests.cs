// <copyright file="ConsistencyInspectionServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Consistency;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — AC #3 (per-unit inspection) + Risk #4 (Cypher-injection guard via
/// parameterized graph queries). Covers the full 6-test inventory in AC #9.
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
    public async Task InspectAsync_BlankMemoryUnitId_ThrowsArgumentExceptionWithoutProbing(string blankId)
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        ConsistencyInspectionService service = CreateService(builder: builder);

        await Should.ThrowAsync<ArgumentException>(
            () => service.InspectAsync(TestTenantId, blankId, CancellationToken.None));

        builder.DidNotReceive().BuildCheckMemoryUnitExists(Arg.Any<string>());
    }

    [Fact]
    public async Task InspectAsync_OpaqueExactHit_ProbesAndReturnsExactIdentifier()
    {
        const string opaqueId = "wf-file-instance-7";
        IDatabase redisDb = Substitute.For<IDatabase>();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns([]);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, opaqueId)),
                Arg.Any<CommandFlags>())
            .Returns(CreateSyntacticEntries());

        IGraphQueryBuilder builder = CreateMockBuilder();
        ConsistencyInspectionService service = CreateService(redisDb, builder);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, opaqueId, CancellationToken.None);

        result.MemoryUnitId.ShouldBe(opaqueId);
        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, opaqueId)),
            Arg.Any<CommandFlags>());
        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSemanticKey(TestTenantId, opaqueId)),
            Arg.Any<CommandFlags>());
        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(TestTenantId, opaqueId)),
            Arg.Any<CommandFlags>());
        builder.Received(1).BuildCheckMemoryUnitExists(opaqueId);
    }

    [Fact]
    public async Task InspectAsync_OpaqueMiss_ProbesExactIdentifierOnceAndThrowsKeyNotFoundException()
    {
        const string opaqueId = "missing-opaque-unit";
        IDatabase redisDb = Substitute.For<IDatabase>();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns([]);
        IGraphQueryBuilder builder = CreateMockBuilder();
        ConsistencyInspectionService service = CreateService(redisDb, builder);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => service.InspectAsync(TestTenantId, opaqueId, CancellationToken.None));

        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, opaqueId)),
            Arg.Any<CommandFlags>());
        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSemanticKey(TestTenantId, opaqueId)),
            Arg.Any<CommandFlags>());
        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(TestTenantId, opaqueId)),
            Arg.Any<CommandFlags>());
        builder.Received(1).BuildCheckMemoryUnitExists(opaqueId);
    }

    [Fact]
    public async Task InspectAsync_OpaqueIdCollidingWithAnotherUnitsChunk_DoesNotReportForeignSemanticData()
    {
        const string opaqueId = "wf-file:0";
        const string collidingBaseId = "wf-file";
        IDatabase redisDb = Substitute.For<IDatabase>();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns([]);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSemanticKey(TestTenantId, opaqueId)),
                Arg.Any<CommandFlags>())
            .Returns(CreateSemanticEntries(collidingBaseId));
        IGraphQueryBuilder builder = CreateMockBuilder();
        ConsistencyInspectionService service = CreateService(redisDb, builder);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => service.InspectAsync(TestTenantId, opaqueId, CancellationToken.None));
    }

    [Theory]
    [InlineData("e8b1d6c2-7e3f-4a21-9f2d-3c4e5a6b7c80")] // hyphenated GUID (D format) — legacy ingest path
    [InlineData("E8B1D6C27E3F4A219F2D3C4E5A6B7C80")] // 32-hex GUID (N format)
    public static void ValidateMemoryUnitIdFormat_LegacyGuid_DoesNotThrow(string legacyId)
    {
        // Policy (review fix pass 2): ULID is preferred but the operator-facing API accepts
        // any legacy GUID-shaped ID so existing units ingested via the pre-ULID fallback
        // remain inspectable + repairable.
        Should.NotThrow(() => ConsistencyInspectionService.ValidateMemoryUnitIdFormat(legacyId));
    }

    [Fact]
    public static void ValidateMemoryUnitIdFormat_ValidUlid_DoesNotThrow()
    {
        Should.NotThrow(() => ConsistencyInspectionService.ValidateMemoryUnitIdFormat(ValidUlid));
    }

    [Fact]
    public async Task InspectAsync_LegacyGuidNFormat_ExactHitWinsWithoutAliasProbe()
    {
        const string exactGuidN = "E8B1D6C27E3F4A219F2D3C4E5A6B7C80";
        const string guidDAlias = "e8b1d6c2-7e3f-4a21-9f2d-3c4e5a6b7c80";

        IDatabase redisDb = Substitute.For<IDatabase>();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns([]);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, exactGuidN)),
                Arg.Any<CommandFlags>())
            .Returns(CreateSyntacticEntries());

        IGraphQueryBuilder builder = CreateMockBuilder();
        ConsistencyInspectionService service = CreateService(redisDb, builder);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, exactGuidN, CancellationToken.None);

        result.MemoryUnitId.ShouldBe(exactGuidN);
        await redisDb.DidNotReceive().HashGetAllAsync(
            Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, guidDAlias)),
            Arg.Any<CommandFlags>());
        builder.Received(1).BuildCheckMemoryUnitExists(exactGuidN);
        builder.DidNotReceive().BuildCheckMemoryUnitExists(guidDAlias);
    }

    [Fact]
    public async Task InspectAsync_LegacyGuidNFormat_ExactMissFallsBackToStoredDFormat()
    {
        const string storedGuid = "e8b1d6c2-7e3f-4a21-9f2d-3c4e5a6b7c80";
        const string legacyAlias = "E8B1D6C27E3F4A219F2D3C4E5A6B7C80";

        IDatabase redisDb = Substitute.For<IDatabase>();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns([]);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, storedGuid)),
                Arg.Any<CommandFlags>())
            .Returns(CreateSyntacticEntries());
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSemanticKey(TestTenantId, storedGuid)),
                Arg.Any<CommandFlags>())
            .Returns([]);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        IGraphQueryBuilder builder = CreateMockBuilder();
        ConsistencyInspectionService service = new(
            redis,
            CreateFalkorMultiplexer(nodeExists: false),
            builder,
            Substitute.For<ILogger<ConsistencyInspectionService>>());

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, legacyAlias, CancellationToken.None);

        result.MemoryUnitId.ShouldBe(storedGuid);
        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, legacyAlias)),
            Arg.Any<CommandFlags>());
        await redisDb.Received(1).HashGetAllAsync(
            Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, storedGuid)),
            Arg.Any<CommandFlags>());
        builder.Received(1).BuildCheckMemoryUnitExists(legacyAlias);
        builder.Received(1).BuildCheckMemoryUnitExists(storedGuid);
    }

    [Fact]
    public static void GraphQueryBuilder_AdversarialOpaqueIdentifier_UsesParameterMapWithoutChangingQueryText()
    {
        const string adversarialId = "wf-file-instance-7'}) MATCH (n) RETURN n";
        var builder = new GraphQueryBuilder();

        (string opaqueQuery, IDictionary<string, object> opaqueParameters) =
            builder.BuildCheckMemoryUnitExists(adversarialId);
        (string baselineQuery, _) = builder.BuildCheckMemoryUnitExists("baseline-id");

        opaqueQuery.ShouldBe(baselineQuery);
        opaqueQuery.ShouldNotContain(adversarialId, Shouldly.Case.Sensitive);
        opaqueParameters["id"].ShouldBe(adversarialId);
    }

    [Fact]
    public async Task InspectAsync_AdversarialOpaqueIdentifier_BackendFailurePropagatesUnchanged()
    {
        const string adversarialId = "wf-file-instance-7'}) MATCH (n) RETURN n";
        var expected = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "backend unavailable");
        IDatabase redisDb = Substitute.For<IDatabase>();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns([]);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(key => key.ToString() == IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, adversarialId)),
                Arg.Any<CommandFlags>())
            .Returns(Task.FromException<HashEntry[]>(expected));
        IGraphQueryBuilder builder = CreateMockBuilder();
        ConsistencyInspectionService service = CreateService(redisDb, builder);

        RedisConnectionException actual = await Should.ThrowAsync<RedisConnectionException>(
            () => service.InspectAsync(TestTenantId, adversarialId, CancellationToken.None));

        actual.ShouldBeSameAs(expected);
        builder.Received(1).BuildCheckMemoryUnitExists(adversarialId);
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
    public async Task InspectAsync_QueuedNaturalLanguageMissing_ReturnsInformationalNoteWithoutViolation()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: true,
            semanticPresent: true,
            graphPresent: true,
            naturalLanguageSemanticPresent: false,
            naturalLanguageEmbeddingStatus: NaturalLanguageEmbeddingStatus.Queued);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, ValidUlid, CancellationToken.None);

        result.Recommendation.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        result.NaturalLanguageSemanticPresent.ShouldBeFalse();
        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Queued);
        result.ConsistencyNote.ShouldNotBeNull();
        result.ConsistencyNote.ShouldContain("queued retry");
    }

    [Fact]
    public async Task InspectAsync_IndexedNaturalLanguageMissing_SurfacesGapNote()
    {
        ConsistencyInspectionService service = CreateService(
            syntacticPresent: true,
            semanticPresent: true,
            graphPresent: true,
            naturalLanguageSemanticPresent: false,
            naturalLanguageEmbeddingStatus: NaturalLanguageEmbeddingStatus.Indexed);

        ConsistencyInspectionResult result = await service.InspectAsync(TestTenantId, ValidUlid, CancellationToken.None);

        result.Recommendation.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        result.NaturalLanguageSemanticPresent.ShouldBeFalse();
        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Indexed);
        result.ConsistencyNote.ShouldNotBeNull();
        result.ConsistencyNote.ShouldContain("semantic-nl");
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
        bool naturalLanguageSemanticPresent = false,
        NaturalLanguageEmbeddingStatus? naturalLanguageEmbeddingStatus = null,
        IGraphQueryBuilder? builder = null)
    {
        IDatabase redisDb = Substitute.For<IDatabase>();

        HashEntry[] syntacticEntries = syntacticPresent
            ? CreateSyntacticEntries(naturalLanguageEmbeddingStatus)
            : [];
        HashEntry[] semanticEntries = semanticPresent
            ? CreateSemanticEntries()
            : [];
        HashEntry[] naturalLanguageSemanticEntries = naturalLanguageSemanticPresent
            ? CreateNaturalLanguageSemanticEntries()
            : [];

        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(key => IsTenantSyntacticKey(key)),
                Arg.Any<CommandFlags>())
            .Returns(syntacticEntries);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(key => IsTenantNaturalLanguageSemanticKey(key)),
                Arg.Any<CommandFlags>())
            .Returns(naturalLanguageSemanticEntries);
        redisDb.HashGetAllAsync(
                Arg.Is<RedisKey>(key => IsTenantSemanticKey(key)),
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

    private static ConsistencyInspectionService CreateService(IDatabase redisDb, IGraphQueryBuilder builder)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        return new ConsistencyInspectionService(
            redis,
            CreateFalkorMultiplexer(nodeExists: false),
            builder,
            Substitute.For<ILogger<ConsistencyInspectionService>>());
    }

    private static bool IsTenantSyntacticKey(RedisKey key)
        => IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(TestTenantId, key, out string _);

    private static bool IsTenantSemanticKey(RedisKey key)
        => IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(TestTenantId, key, out string _);

    private static bool IsTenantNaturalLanguageSemanticKey(RedisKey key)
        => IndexSchemaDefinitions.TryParseNaturalLanguageSemanticMemoryUnitId(TestTenantId, key, out string _);

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

    private static HashEntry[] CreateSyntacticEntries(NaturalLanguageEmbeddingStatus? naturalLanguageEmbeddingStatus = null)
    {
        List<HashEntry> entries =
        [
            new("contentHash", "abc123"),
            new("ingestedAt", "2026-04-20T10:00:00+00:00"),
            new("sourceUri", "file:///sample.md"),
            new("sourceType", "file"),
            new("caseId", "case-1"),
            new("embeddingProvider", "gemini"),
            new("embeddingModel", "gemini-embedding-001"),
        ];

        if (naturalLanguageEmbeddingStatus.HasValue)
        {
            Dictionary<string, MetadataField> metadata = new(StringComparer.Ordinal)
            {
                [NaturalLanguageConsistencyState.EmbeddingStatusMetadataKey] = new(
                    naturalLanguageEmbeddingStatus.Value.ToString(),
                    MetadataOrigin.Ai,
                    1.0f),
            };
            entries.Add(new HashEntry("metadataJson", JsonSerializer.Serialize(metadata, MemoriesJsonContext.Options)));
        }

        return [.. entries];
    }

    private static HashEntry[] CreateSemanticEntries(string memoryUnitId = ValidUlid) =>
    [
        new("embedding", new byte[1536 * sizeof(float)]),
        new("memoryUnitId", memoryUnitId),
        new("caseId", "case-1"),
    ];

    private static HashEntry[] CreateNaturalLanguageSemanticEntries() =>
    [
        new("embedding", new byte[1536 * sizeof(float)]),
        new("memoryUnitId", ValidUlid),
        new("caseId", "case-1"),
        new("naturalLanguageDescription", "A business action happened."),
    ];
}
