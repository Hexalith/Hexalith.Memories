// <copyright file="VerifyConsistencyActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class VerifyConsistencyActivityTests
{
    [Fact]
    public async Task RunAsync_AllBackendsPresent_ShouldReturnAllTrue()
    {
        (VerifyConsistencyActivity activity, _, _) = CreateActivity(
            syntacticExists: true,
            semanticExists: true,
            graphExists: true);

        ConsistencyResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        result.SyntacticExists.ShouldBeTrue();
        result.SemanticExists.ShouldBeTrue();
        result.GraphExists.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_SyntacticMissing_ShouldReturnSyntacticExistsFalse()
    {
        (VerifyConsistencyActivity activity, _, _) = CreateActivity(
            syntacticExists: false,
            semanticExists: true,
            graphExists: true);

        ConsistencyResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        result.SyntacticExists.ShouldBeFalse();
        result.SemanticExists.ShouldBeTrue();
        result.GraphExists.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_SemanticMissing_ShouldReturnSemanticExistsFalse()
    {
        (VerifyConsistencyActivity activity, _, _) = CreateActivity(
            syntacticExists: true,
            semanticExists: false,
            graphExists: true);

        ConsistencyResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        result.SyntacticExists.ShouldBeTrue();
        result.SemanticExists.ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_GraphMissing_ShouldReturnGraphExistsFalse()
    {
        (VerifyConsistencyActivity activity, _, _) = CreateActivity(
            syntacticExists: true,
            semanticExists: true,
            graphExists: false);

        ConsistencyResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        result.GraphExists.ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_ShouldUseTenantNamespacedKeys()
    {
        (VerifyConsistencyActivity activity, IDatabase redisDb, _) = CreateActivity(
            syntacticExists: true,
            semanticExists: true,
            graphExists: true);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        await redisDb.Received(1).KeyExistsAsync((RedisKey)"tenant-1:mu:mu-001", Arg.Any<CommandFlags>());
        await redisDb.Received(1).KeyExistsAsync((RedisKey)"tenant-1:vec:mu-001", Arg.Any<CommandFlags>());
        await redisDb.Received(1).KeyExistsAsync((RedisKey)"tenant-1:vec:nl:mu-001", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_ShouldCallGraphQueryBuilder()
    {
        (VerifyConsistencyActivity activity, _, IGraphQueryBuilder builder) = CreateActivity(
            syntacticExists: true,
            semanticExists: true,
            graphExists: true);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        builder.Received(1).BuildCheckMemoryUnitExists("mu-001");
    }

    [Fact]
    public async Task RunAsync_RedisUnavailable_ShouldPropagateException()
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb(true);
        IGraphQueryBuilder builder = CreateMockBuilder();
        VerifyConsistencyActivity activity = new(redis, falkorDb, builder, Substitute.For<ILogger<VerifyConsistencyActivity>>());

        await Should.ThrowAsync<RedisConnectionException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                new ConsistencyInput("mu-001", "tenant-1")));
    }

    [Fact]
    public async Task RunAsync_QueuedNaturalLanguageMissing_SurfacesInformationalNote()
    {
        (VerifyConsistencyActivity activity, _, _) = CreateActivity(
            syntacticExists: true,
            semanticExists: true,
            graphExists: true,
            naturalLanguageSemanticExists: false,
            naturalLanguageEmbeddingStatus: NaturalLanguageEmbeddingStatus.Queued);

        ConsistencyResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        result.NaturalLanguageSemanticExists.ShouldBeFalse();
        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Queued);
        result.ConsistencyNote.ShouldNotBeNull();
        result.ConsistencyNote.ShouldContain("queued retry");
    }

    [Fact]
    public async Task RunAsync_IndexedNaturalLanguageMissing_SurfacesGapNote()
    {
        (VerifyConsistencyActivity activity, _, _) = CreateActivity(
            syntacticExists: true,
            semanticExists: true,
            graphExists: true,
            naturalLanguageSemanticExists: false,
            naturalLanguageEmbeddingStatus: NaturalLanguageEmbeddingStatus.Indexed);

        ConsistencyResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-1"));

        result.NaturalLanguageSemanticExists.ShouldBeFalse();
        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Indexed);
        result.ConsistencyNote.ShouldNotBeNull();
        result.ConsistencyNote.ShouldContain("semantic-nl");
    }

    private static (VerifyConsistencyActivity Activity, IDatabase RedisDb, IGraphQueryBuilder Builder) CreateActivity(
        bool syntacticExists,
        bool semanticExists,
        bool graphExists,
        bool naturalLanguageSemanticExists = false,
        NaturalLanguageEmbeddingStatus? naturalLanguageEmbeddingStatus = null)
    {
        IDatabase redisDb = Substitute.For<IDatabase>();
        redisDb.KeyExistsAsync(Arg.Is<RedisKey>(k => k.ToString()!.Contains(":mu:")), Arg.Any<CommandFlags>())
            .Returns(syntacticExists);
        redisDb.KeyExistsAsync(Arg.Is<RedisKey>(k => k.ToString()!.Contains(":vec:nl:")), Arg.Any<CommandFlags>())
            .Returns(naturalLanguageSemanticExists);
        redisDb.KeyExistsAsync(Arg.Is<RedisKey>(k => k.ToString()!.Contains(":vec:") && !k.ToString()!.Contains(":vec:nl:")), Arg.Any<CommandFlags>())
            .Returns(semanticExists);

        Dictionary<string, MetadataField> metadata = new(StringComparer.Ordinal)
        {
            [NaturalLanguageConsistencyState.EmbeddingStatusMetadataKey] = new(
                (naturalLanguageEmbeddingStatus ?? NaturalLanguageEmbeddingStatus.NotApplicable).ToString(),
                MetadataOrigin.Ai,
                1.0f),
        };
        redisDb.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString()!.Contains(":mu:")), "metadataJson", Arg.Any<CommandFlags>())
            .Returns(JsonSerializer.Serialize(metadata, MemoriesJsonContext.Options));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        (IConnectionMultiplexer falkorMux, _) = CreateMockFalkorDb(graphExists);
        IGraphQueryBuilder builder = CreateMockBuilder();

        VerifyConsistencyActivity activity = new(
            redis, falkorMux, builder, Substitute.For<ILogger<VerifyConsistencyActivity>>());

        return (activity, redisDb, builder);
    }

    private static IGraphQueryBuilder CreateMockBuilder()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        builder.BuildCheckMemoryUnitExists(Arg.Any<string>())
            .Returns(("MATCH (m:MemoryUnit {id: $id}) RETURN m.id", new Dictionary<string, object> { ["id"] = "mock" }));
        return builder;
    }

    private static (IConnectionMultiplexer Mux, IDatabase Db) CreateMockFalkorDb(bool nodeExists)
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer mux = Substitute.For<IConnectionMultiplexer>();
        mux.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        // GRAPH.QUERY returns [headers, data_rows, statistics]
        RedisResult[] dataRows = nodeExists
            ? [RedisResult.Create(new RedisValue[] { "mu-001" })]
            : [];

        RedisResult fakeResult = RedisResult.Create(
        [
            RedisResult.Create(Array.Empty<RedisResult>()), // headers
            RedisResult.Create(dataRows),                   // data rows
            RedisResult.Create(                             // statistics
            [
                RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
            ]),
        ]);

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(fakeResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(fakeResult);

        return (mux, db);
    }
}
