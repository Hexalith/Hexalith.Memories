// <copyright file="IndexNaturalLanguageSemanticActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class IndexNaturalLanguageSemanticActivityTests
{
    [Fact]
    public async Task RunAsync_WritesDistinctHashKey_FromSemanticActivity()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        IndexNaturalLanguageSemanticActivity activity =
            new(redis, Substitute.For<ILogger<IndexNaturalLanguageSemanticActivity>>());

        IndexResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            CreateInput());

        result.Backend.ShouldBe("semantic-nl");
        result.MemoryUnitId.ShouldBe("mu-001");
        result.TenantId.ShouldBe("tenant-a");

        await db.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k!.ToString() == IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-001")),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_WritesConfidenceSourceField()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        HashEntry[]? captured = null;
        await db.HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Do<HashEntry[]>(entries => captured = entries),
            Arg.Any<CommandFlags>());

        IndexNaturalLanguageSemanticActivity activity =
            new(redis, Substitute.For<ILogger<IndexNaturalLanguageSemanticActivity>>());

        _ = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            CreateInput() with
            {
                DescriptionConfidence = 0.87f,
                ConfidenceSource = ConfidenceSource.Logprobs,
            });

        captured.ShouldNotBeNull();
        captured.ShouldContain(e => e.Name == "descriptionOrigin" && e.Value.ToString() == "ai");
        captured.ShouldContain(e => e.Name == "tenantId" && e.Value.ToString() == "tenant-a");
        captured.ShouldContain(e => e.Name == "descriptionConfidenceSource" && e.Value.ToString() == "logprobs");
        captured.ShouldContain(e => e.Name == "descriptionConfidence" && e.Value.ToString()!.StartsWith("0.87"));
        captured.ShouldContain(e => e.Name == "naturalLanguageDescription" && e.Value.ToString() == "User opened a support claim.");
        captured.ShouldContain(e => e.Name == "embeddingDimensions" && (int)e.Value == 3);
    }

    [Fact]
    public async Task RunAsync_NullConfidence_PersistsEmptyString()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        HashEntry[]? captured = null;
        await db.HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Do<HashEntry[]>(entries => captured = entries),
            Arg.Any<CommandFlags>());

        IndexNaturalLanguageSemanticActivity activity =
            new(redis, Substitute.For<ILogger<IndexNaturalLanguageSemanticActivity>>());

        _ = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            CreateInput() with
            {
                DescriptionConfidence = null,
                ConfidenceSource = ConfidenceSource.Constant,
            });

        captured.ShouldNotBeNull();
        captured.ShouldContain(e => e.Name == "descriptionConfidence" && e.Value.ToString() == string.Empty);
        captured.ShouldContain(e => e.Name == "descriptionConfidenceSource" && e.Value.ToString() == "constant");
    }

    [Fact]
    public async Task RunAsync_EmptyEmbeddingVector_Throws()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        IndexNaturalLanguageSemanticActivity activity =
            new(redis, Substitute.For<ILogger<IndexNaturalLanguageSemanticActivity>>());

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                CreateInput() with { EmbeddingVector = [] }));
    }

    [Fact]
    public async Task RunAsync_ActiveMigrationMarkerWithOldProvider_ShouldFailBeforeHashWrite()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(CreateActiveMarkerEntries("tenant-a")));
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        IndexNaturalLanguageSemanticActivity activity =
            new(redis, Substitute.For<ILogger<IndexNaturalLanguageSemanticActivity>>());

        EmbeddingMigrationWriteBlockedException ex = await Should.ThrowAsync<EmbeddingMigrationWriteBlockedException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                CreateInput()));

        ex.Message.ShouldContain("active tenant migration marker");
        await db.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_DimensionsMismatch_Throws()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        IndexNaturalLanguageSemanticActivity activity =
            new(redis, Substitute.For<ILogger<IndexNaturalLanguageSemanticActivity>>());

        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                CreateInput() with { EmbeddingDimensions = 99 }));
    }

    [Fact]
    public async Task RunAsync_ExistingIndexWithDifferentDimensions_ThrowsInvalidOperationException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.CREATE"), Arg.Any<object[]>())
            .Returns(_ => throw Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Index already exists"));
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.CREATE"), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Index already exists"));
        RedisResult info = CreateNaturalLanguageIndexInfo(99);
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Any<object[]>())
            .Returns(info);
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(info);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        IndexNaturalLanguageSemanticActivity activity =
            new(redis, Substitute.For<ILogger<IndexNaturalLanguageSemanticActivity>>());

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                CreateInput()));

        ex.Message.ShouldContain("does not match the expected tenant schema");
        ex.Message.ShouldContain("expected 3 dimensions but found 99");
    }

    private static IConnectionMultiplexer CreateMockMultiplexer(IDatabase db)
    {
        // Story 23.7 (A34): the NL index is now provisioned by TenantProvisioningWorkflow, so the activity verifies
        // an already-existing, schema-matching index (dim 3) via FT.INFO instead of creating it per document.
        RedisResult Execute(string command)
            => command == "FT.INFO"
                ? CreateNaturalLanguageIndexInfo(3)
                : RedisResult.Create(new RedisValue("OK"));

        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(call => Execute(call.ArgAt<string>(0)));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call => Execute(call.ArgAt<string>(0)));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static HashEntry[] CreateActiveMarkerEntries(string tenantId) =>
    [
        new HashEntry("tenantId", tenantId),
        new HashEntry("targetProvider", EmbeddingProviderDefaults.OllamaProviderName),
        new HashEntry("targetModel", EmbeddingProviderDefaults.OllamaModelName),
        new HashEntry("targetDimensions", EmbeddingProviderDefaults.Ollama().Dimensions),
        new HashEntry("status", "started"),
    ];

    private static NaturalLanguageIndexInput CreateInput() => new()
    {
        MemoryUnitId = "mu-001",
        TenantId = "tenant-a",
        CaseId = "case-001",
        EmbeddingVector = [0.1f, 0.2f, 0.3f],
        EmbeddingProvider = "openai",
        EmbeddingModel = "text-embedding-3-small",
        EmbeddingDimensions = 3,
        NaturalLanguageDescription = "User opened a support claim.",
        DescriptionConfidence = 0.85f,
        ConfidenceSource = ConfidenceSource.Logprobs,
    };

    private static RedisResult CreateNaturalLanguageIndexInfo(int dimensions) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("num_docs")),
        RedisResult.Create(new RedisValue("0")),
            RedisResult.Create(new RedisValue("index_definition")),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("prefixes")),
                RedisResult.Create([RedisResult.Create(new RedisValue(IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a")))]),
            ]),
        RedisResult.Create(new RedisValue("attributes")),
        RedisResult.Create(
        [
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("identifier")),
                RedisResult.Create(new RedisValue("embedding")),
                RedisResult.Create(new RedisValue("attribute")),
                RedisResult.Create(new RedisValue("embedding")),
                RedisResult.Create(new RedisValue("type")),
                RedisResult.Create(new RedisValue("VECTOR")),
                RedisResult.Create(new RedisValue("dim")),
                RedisResult.Create(new RedisValue(dimensions.ToString())),
            ]),
            CreateTagAttribute("memoryUnitId"),
            CreateTagAttribute("caseId"),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("identifier")),
                RedisResult.Create(new RedisValue("naturalLanguageDescription")),
                RedisResult.Create(new RedisValue("attribute")),
                RedisResult.Create(new RedisValue("naturalLanguageDescription")),
                RedisResult.Create(new RedisValue("type")),
                RedisResult.Create(new RedisValue("TEXT")),
            ]),
        ]),
    ]);

    private static RedisResult CreateTagAttribute(string identifier) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("identifier")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("attribute")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("type")),
        RedisResult.Create(new RedisValue("TAG")),
    ]);
}
