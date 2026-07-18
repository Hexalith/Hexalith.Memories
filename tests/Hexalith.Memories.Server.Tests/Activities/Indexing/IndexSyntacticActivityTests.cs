// <copyright file="IndexSyntacticActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Text;
using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class IndexSyntacticActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldStoreHashWithCorrectKeyAndFields()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act
        IndexResult result = await activity.RunAsync(context, input);

        // Assert
        result.Backend.ShouldBe("syntactic");
        result.MemoryUnitId.ShouldBe(input.MemoryUnitId);
        result.TenantId.ShouldBe(input.TenantId);

        await db.Received(1).HashSetAsync(
            IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId),
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries!, "content", input.Content)
                && HasEntry(entries!, "sourceUri", input.SourceUri)
                && HasEntry(entries!, "sourceUriText", input.SourceUri)
                && HasEntry(entries!, "sourceType", "file")
                && HasEntry(entries!, "sourceTypeText", "file")
                && HasEntry(entries!, "metadataText", "priority urgent human")
                && HasEntry(entries!, "attributeTags", "priority=urgent")
                && HasEntry(entries!, "metadataJson", JsonSerializer.Serialize(input.Metadata, MemoriesJsonContext.Options))
                && HasEntry(entries!, "ingestedBy", input.IngestedBy)
                && HasEntry(entries!, "ingestedAt", input.IngestedAt.ToString("o"))),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_ShouldUseTenantNamespacedKey()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        await db.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k!.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("test-tenant", "test-mu-001")),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_WithContentReference_StoresResolvedContent()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        WorkflowPayloadReference contentReference = new(
            "test-mu-001:extractedtext:hash",
            "hash",
            16,
            WorkflowPayloadKind.ExtractedText,
            "test-tenant",
            "test-mu-001");
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        payloadStore
            .ReadAsync(contentReference, "test-tenant", "test-mu-001", WorkflowPayloadKind.ExtractedText, Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes("resolved content"));
        IndexInput input = CreateTestInput() with
        {
            Content = string.Empty,
            ContentReference = contentReference,
        };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger, payloadStore);

        await activity.RunAsync(context, input);

        await db.Received(1).HashSetAsync(
            IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId),
            Arg.Is<HashEntry[]>(entries => HasEntry(entries!, "content", "resolved content")),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_WhenRedisConnectionFails_ShouldPropagateException()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        db.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<RedisConnectionException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_InvalidTenantId_ShouldThrow()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput() with { TenantId = "bad tenant; DROP" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    // Story 5.5 AC6 / FR70 — persist the embedding model alongside the existing provider entry.
    [Fact]
    public async Task RunAsync_ShouldPersistEmbeddingModelHashField()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput() with { EmbeddingModel = "gemini-embedding-001" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        await activity.RunAsync(context, input);

        await db.Received(1).HashSetAsync(
            IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId),
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries!, "embeddingModel", "gemini-embedding-001")
                && HasEntry(entries!, "embeddingProvider", input.EmbeddingProvider)),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_IndexMissingCloudEventSubjectField_ShouldAlterSchemaBeforeHashWrite()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ConfigureExistingIndex(db, includeSubjectField: false);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        IndexInput input = CreateTestInput() with
        {
            Metadata = new Dictionary<string, MetadataField>
            {
                ["cloudevent.subject"] = new("claim-42", MetadataOrigin.Ai, 1.0f),
            },
        };

        IndexSyntacticActivity activity = new(redis, Substitute.For<ILogger<IndexSyntacticActivity>>());

        _ = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        db.Received().Execute(
            "FT.ALTER",
            Arg.Is<object[]>(args => args!.Length == 5
                && args[0].ToString() == IndexSchemaDefinitions.GetSyntacticIndexName("test-tenant")
                && args[1].ToString() == "SCHEMA"
                && args[2].ToString() == "ADD"
                && args[3].ToString() == "cloudeventSubject"
                && args[4].ToString() == "TAG"));
    }

    [Fact]
    public async Task RunAsync_IndexInfoTemporarilyIncomplete_ShouldRetryBeforeHashWrite()
    {
        IDatabase db = Substitute.For<IDatabase>();
        int infoCalls = 0;
        ConfigureExistingIndexWithTransientEmptyInfo(db, () => ++infoCalls);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        IndexInput input = CreateTestInput();
        IndexSyntacticActivity activity = new(redis, Substitute.For<ILogger<IndexSyntacticActivity>>());

        _ = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        infoCalls.ShouldBeGreaterThanOrEqualTo(2);
        await db.Received(1).HashSetAsync(
            IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    // Story 23.7 (A34) AC1/AC10: the activity never issues FT.CREATE — creation is owned by
    // TenantProvisioningWorkflow. The shared readiness verifier performs a single FT.INFO for the tenant/index
    // family and every subsequent document write reuses the cached result (no per-document FT.CREATE, no
    // "already exists" warning).
    [Fact]
    public async Task RunAsync_RepeatedWrites_NeverCreateIndex_AndVerifyReadinessOnce()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ITenantIndexReadinessVerifier verifier =
            new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, Substitute.For<ILogger<IndexSyntacticActivity>>(), payloadStore: null, verifier);

        await activity.RunAsync(context, CreateTestInput() with { MemoryUnitId = "mu-1" });
        await activity.RunAsync(context, CreateTestInput() with { MemoryUnitId = "mu-2" });

        db.DidNotReceive().Execute("FT.CREATE", Arg.Any<object[]>());
        db.Received(1).Execute("FT.INFO", Arg.Any<object[]>());
        await db.Received(2).HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>());
    }

    private static IConnectionMultiplexer CreateMockMultiplexer(IDatabase db)
    {
        ConfigureExistingIndex(db, includeSubjectField: true);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static void ConfigureExistingIndex(IDatabase db, bool includeSubjectField)
    {
        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(call =>
            {
                string command = call.ArgAt<string>(0);
                return command switch
                {
                    "FT.CREATE" => throw new RedisServerException("Index already exists"),
                    "FT.INFO" => CreateExistingIndexInfoResult(includeSubjectField),
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
                    "FT.INFO" => CreateExistingIndexInfoResult(includeSubjectField),
                    "FT.ALTER" => RedisResult.Create(new RedisValue("OK")),
                    _ => RedisResult.Create(new RedisValue("OK")),
                };
            });
    }

    private static void ConfigureExistingIndexWithTransientEmptyInfo(IDatabase db, Func<int> nextInfoCall)
    {
        RedisResult Execute(string command)
            => command switch
            {
                "FT.CREATE" => throw new RedisServerException("Index already exists"),
                "FT.INFO" => nextInfoCall() == 1
                    ? CreateIncompleteIndexInfoResult()
                    : CreateExistingIndexInfoResult(includeSubjectField: true),
                _ => RedisResult.Create(new RedisValue("OK")),
            };

        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(call => Execute(call.ArgAt<string>(0)));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call => Execute(call.ArgAt<string>(0)));
    }

    private static bool HasEntry(IEnumerable<HashEntry> entries, string name, string value)
    {
        foreach (HashEntry entry in entries)
        {
            if (entry.Name == name && entry.Value.ToString() == value)
            {
                return true;
            }
        }

        return false;
    }

    private static IndexInput CreateTestInput() => new()
    {
        MemoryUnitId = "test-mu-001",
        TenantId = "test-tenant",
        CaseId = "test-case-001",
        Content = "Test content for indexing",
        ContentHash = "abc123hash",
        SourceUri = "file:///test.txt",
        SourceType = SourceType.File,
        IngestedBy = "test-user@example.com",
        IngestedAt = DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
        EmbeddingVector = new float[] { 0.1f, 0.2f, 0.3f },
        EmbeddingProvider = "google:text-embedding-004",
        EmbeddingModel = "gemini-embedding-001",
        EmbeddingDimensions = 3,
        Metadata = new Dictionary<string, MetadataField>
        {
            ["priority"] = new("urgent", MetadataOrigin.Human, 1.0f),
        },
    };

    private static RedisResult CreateExistingIndexInfoResult(bool includeSubjectField)
    {
        List<RedisResult> attributes =
        [
            CreateAttribute("content", "TEXT"),
            CreateAttribute("sourceUriText", "TEXT"),
            CreateAttribute("sourceTypeText", "TEXT"),
            CreateAttribute("metadataText", "TEXT"),
            CreateAttribute("sourceUri", "TAG"),
            CreateAttribute("sourceType", "TAG"),
            CreateAttribute("contentHash", "TAG"),
            CreateAttribute("caseId", "TAG"),
            CreateAttribute("attributeTags", "TAG"),
            CreateAttribute("embeddingProvider", "TAG"),
        ];

        if (includeSubjectField)
        {
            attributes.Add(CreateAttribute("cloudeventSubject", "TAG"));
        }

        return RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("index_definition")),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("prefixes")),
                RedisResult.Create([RedisResult.Create(new RedisValue(IndexSchemaDefinitions.GetSyntacticKeyPrefix("test-tenant")))]),
            ]),
            RedisResult.Create(new RedisValue("attributes")),
            RedisResult.Create([.. attributes]),
        ]);
    }

    private static RedisResult CreateIncompleteIndexInfoResult() => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("num_docs")),
        RedisResult.Create(0),
    ]);

    private static RedisResult CreateAttribute(string identifier, string type) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("identifier")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("attribute")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("type")),
        RedisResult.Create(new RedisValue(type)),
    ]);
}
