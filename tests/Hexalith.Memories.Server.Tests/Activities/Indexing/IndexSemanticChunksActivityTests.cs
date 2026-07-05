// <copyright file="IndexSemanticChunksActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public sealed class IndexSemanticChunksActivityTests
{
    [Fact]
    public async Task RunAsync_WithReadyIndex_VerifiesReadinessOnceAndWritesChunkHashes()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<HashEntry>()));
        IConnectionMultiplexer redis = CreateMultiplexer(db);
        ITenantIndexReadinessVerifier verifier = Substitute.For<ITenantIndexReadinessVerifier>();
        verifier
            .EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Semantic, 3, CancellationToken.None)
            .Returns(Task.CompletedTask);

        IndexSemanticChunksActivity activity = new(
            redis,
            NullLogger<IndexSemanticChunksActivity>.Instance,
            payloadStore: null,
            verifier);

        SemanticChunkIndexInput input = CreateInput();

        IndexResult result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        result.Backend.ShouldBe("semantic");
        result.MemoryUnitId.ShouldBe("mu-1");
        result.TenantId.ShouldBe("tenant-a");

        await verifier.Received(1)
            .EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Semantic, 3, CancellationToken.None);
        db.DidNotReceive().Execute("FT.CREATE", Arg.Any<object[]>());
        await db.Received(1).HashSetAsync(
            IndexSchemaDefinitions.BuildSemanticChunkKey("tenant-a", "mu-1", 0),
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries, "tenantId", "tenant-a")
                && HasEntry(entries, "memoryUnitId", "mu-1")
                && HasEntry(entries, "caseId", "case-1")
                && HasEntry(entries, "chunkSequence", 0)
                && HasEntry(entries, "chunkText", "first chunk")),
            Arg.Any<CommandFlags>());
        await db.Received(1).HashSetAsync(
            IndexSchemaDefinitions.BuildSemanticChunkKey("tenant-a", "mu-1", 1),
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries, "tenantId", "tenant-a")
                && HasEntry(entries, "chunkSequence", 1)
                && HasEntry(entries, "chunkText", "second chunk")),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_MissingIndex_FailsBeforeWritingChunkHashes()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<HashEntry>()));
        IConnectionMultiplexer redis = CreateMultiplexer(db);
        ITenantIndexReadinessVerifier verifier = Substitute.For<ITenantIndexReadinessVerifier>();
        verifier
            .EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Semantic, 3, CancellationToken.None)
            .Returns(Task.FromException(new TenantIndexNotProvisionedException(
                "tenant-a",
                TenantIndexFamily.Semantic,
                IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"))));

        IndexSemanticChunksActivity activity = new(
            redis,
            NullLogger<IndexSemanticChunksActivity>.Instance,
            payloadStore: null,
            verifier);

        await Should.ThrowAsync<TenantIndexNotProvisionedException>(
            () => activity.RunAsync(Substitute.For<WorkflowActivityContext>(), CreateInput()));

        await db.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    private static IConnectionMultiplexer CreateMultiplexer(IDatabase db)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetDatabase().Returns(db);
        return redis;
    }

    private static SemanticChunkIndexInput CreateInput() => new()
    {
        TenantId = "tenant-a",
        MemoryUnitId = "mu-1",
        CaseId = "case-1",
        EmbeddingProvider = "google",
        EmbeddingModel = "gemini-embedding-001",
        EmbeddingDimensions = 3,
        Chunks =
        [
            new ChunkEmbeddingResult
            {
                Sequence = 1,
                Text = "second chunk",
                StartOffset = 20,
                EndOffset = 32,
                EstimatedTokens = 3,
                Vector = [0.0f, 1.0f, 0.0f],
            },
            new ChunkEmbeddingResult
            {
                Sequence = 0,
                Text = "first chunk",
                StartOffset = 0,
                EndOffset = 11,
                EstimatedTokens = 3,
                Vector = [1.0f, 0.0f, 0.0f],
            },
        ],
    };

    private static bool HasEntry(IEnumerable<HashEntry> entries, string name, string value)
        => entries.Any(entry => entry.Name == name && entry.Value.ToString() == value);

    private static bool HasEntry(IEnumerable<HashEntry> entries, string name, int value)
        => entries.Any(entry => entry.Name == name && (int)entry.Value == value);
}
