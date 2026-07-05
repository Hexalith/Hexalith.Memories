// <copyright file="IndexSemanticActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class IndexSemanticActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldStoreVectorWithCorrectKey()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        IndexResult result = await activity.RunAsync(context, input);

        result.Backend.ShouldBe("semantic");
        result.MemoryUnitId.ShouldBe(input.MemoryUnitId);
        result.TenantId.ShouldBe(input.TenantId);

        await db.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSemanticKey("test-tenant", "test-mu-001")),
            Arg.Is<HashEntry[]>(entries => HasEntry(entries, "tenantId", "test-tenant")),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public void VectorByteConversion_KnownGoldValues_ShouldBeExact()
    {
        float[] vector = [1.0f, 0.0f, -1.0f];
        byte[] vectorBytes = MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();

        vectorBytes.Length.ShouldBe(12);
        vectorBytes[0..4].ShouldBe(BitConverter.GetBytes(1.0f));
        vectorBytes[4..8].ShouldBe(BitConverter.GetBytes(0.0f));
        vectorBytes[8..12].ShouldBe(BitConverter.GetBytes(-1.0f));
    }

    [Fact]
    public async Task RunAsync_ShouldUseTenantNamespacedKey()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        await activity.RunAsync(context, input);

        await db.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(key => IsTestTenantSemanticKey(key)),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_WhenRedisConnectionFails_ShouldPropagateException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        await Should.ThrowAsync<RedisConnectionException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_ActiveMigrationMarkerWithOldProvider_ShouldFailBeforeHashWrite()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(CreateActiveMarkerEntries("test-tenant")));
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        EmbeddingMigrationWriteBlockedException ex = await Should.ThrowAsync<EmbeddingMigrationWriteBlockedException>(
            () => activity.RunAsync(context, input));

        ex.Message.ShouldContain("active tenant migration marker");
        await db.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_InvalidTenantId_ShouldThrow()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput() with { TenantId = "bad tenant; DROP" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_IndexAlreadyExistsWithDifferentDimensions_ShouldThrow()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db, existingIndexDimensions: 4);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_NullEmbeddingVector_ShouldThrow()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput() with { EmbeddingVector = null! };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        await Should.ThrowAsync<ArgumentNullException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_EmptyEmbeddingVector_ShouldThrow()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput() with { EmbeddingVector = [], EmbeddingDimensions = 0 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_IndexMissingCloudEventSubjectField_ShouldAlterSchemaBeforeHashWrite()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ConfigureExistingIndex(db, existingIndexDimensions: 3, includeSubjectField: false);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        IndexInput input = CreateTestInput() with
        {
            Metadata = new Dictionary<string, MetadataField>
            {
                ["cloudevent.subject"] = new("claim-42", MetadataOrigin.Ai, 1.0f),
            },
        };

        IndexSemanticActivity activity = new(redis, Substitute.For<ILogger<IndexSemanticActivity>>());

        _ = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        db.Received().Execute(
            "FT.ALTER",
            Arg.Is<object[]>(args => args.Length == 5
                && args[0].ToString() == IndexSchemaDefinitions.GetSemanticIndexName("test-tenant")
                && args[1].ToString() == "SCHEMA"
                && args[2].ToString() == "ADD"
                && args[3].ToString() == "cloudeventSubject"
                && args[4].ToString() == "TAG"));
    }

    private static IConnectionMultiplexer CreateMockMultiplexer(IDatabase db, int existingIndexDimensions = 3)
    {
        ConfigureExistingIndex(db, existingIndexDimensions, includeSubjectField: true);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static bool IsTestTenantSemanticKey(RedisKey key)
        => IndexSchemaDefinitions.TryParseSemanticMemoryUnitId("test-tenant", key, out string _);

    private static bool HasEntry(IEnumerable<HashEntry> entries, string name, string value)
        => entries.Any(entry => entry.Name == name && entry.Value.ToString() == value);

    private static HashEntry[] CreateActiveMarkerEntries(string tenantId) =>
    [
        new HashEntry("tenantId", tenantId),
        new HashEntry("targetProvider", EmbeddingProviderDefaults.OllamaProviderName),
        new HashEntry("targetModel", EmbeddingProviderDefaults.OllamaModelName),
        new HashEntry("targetDimensions", EmbeddingProviderDefaults.Ollama().Dimensions),
        new HashEntry("status", "started"),
    ];

    private static void ConfigureExistingIndex(IDatabase db, int existingIndexDimensions, bool includeSubjectField)
    {
        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(call =>
            {
                string command = call.ArgAt<string>(0);
                return command switch
                {
                    "FT.CREATE" => throw new RedisServerException("Index already exists"),
                    "FT.INFO" => CreateExistingIndexInfoResult(existingIndexDimensions, includeSubjectField),
                    "FT.ALTER" => RedisResult.Create(new RedisValue("OK")),
                    _ => RedisResult.Create(new RedisValue("OK")),
                };
            });
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call =>
            {
                string command = call.ArgAt<string>(0);
                return command switch
                {
                    "FT.CREATE" => throw new RedisServerException("Index already exists"),
                    "FT.INFO" => CreateExistingIndexInfoResult(existingIndexDimensions, includeSubjectField),
                    "FT.ALTER" => RedisResult.Create(new RedisValue("OK")),
                    _ => RedisResult.Create(new RedisValue("OK")),
                };
            });
    }

    private static RedisResult CreateExistingIndexInfoResult(int dimensions, bool includeSubjectField)
    {
        List<RedisResult> attributes =
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
        ];

        if (includeSubjectField)
        {
            attributes.Add(CreateTagAttribute("cloudeventSubject"));
        }

        return RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("index_definition")),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("prefixes")),
                RedisResult.Create([RedisResult.Create(new RedisValue(IndexSchemaDefinitions.GetSemanticKeyPrefix("test-tenant")))]),
            ]),
            RedisResult.Create(new RedisValue("attributes")),
            RedisResult.Create([.. attributes]),
        ]);
    }

    private static RedisResult CreateTagAttribute(string identifier) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("identifier")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("attribute")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("type")),
        RedisResult.Create(new RedisValue("TAG")),
    ]);

    private static IndexInput CreateTestInput() => new()
    {
        MemoryUnitId = "test-mu-001",
        TenantId = "test-tenant",
        CaseId = "test-case-001",
        Content = "Test content",
        ContentHash = "abc123",
        SourceUri = "file:///test.txt",
        SourceType = SourceType.File,
        IngestedBy = "test-user@example.com",
        IngestedAt = DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
        EmbeddingVector = new float[] { 0.1f, 0.2f, 0.3f },
        EmbeddingProvider = "google:text-embedding-004",
        EmbeddingModel = "gemini-embedding-001",
        EmbeddingDimensions = 3,
    };
}
