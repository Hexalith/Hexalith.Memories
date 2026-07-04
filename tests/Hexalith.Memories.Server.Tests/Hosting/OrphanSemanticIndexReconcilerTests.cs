// <copyright file="OrphanSemanticIndexReconcilerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class OrphanSemanticIndexReconcilerTests
{
    [Fact]
    public async Task NLIndexWithoutRawSibling_IsDropped()
    {
        RecordingReconciler rig = CreateReconciler(
            IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("orphan-tenant"));

        await rig.Reconciler.ReconcileAsync(CancellationToken.None);

        rig.DropIndexCalls.ShouldContain(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("orphan-tenant"));
        rig.DropIndexCalls.ShouldNotContain(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")); // raw sibling exists — retained
    }

    [Fact]
    public async Task RawIndexWithNLSibling_BothRetained()
    {
        RecordingReconciler rig = CreateReconciler(
            IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"));

        await rig.Reconciler.ReconcileAsync(CancellationToken.None);

        rig.DropIndexCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReconcilerIdempotent_MultipleStartupsDoNotDoubleAct()
    {
        RecordingReconciler rig = CreateReconciler(
            IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"));

        await rig.Reconciler.ReconcileAsync(CancellationToken.None);
        await rig.Reconciler.ReconcileAsync(CancellationToken.None);

        // Idempotent — no orphans in either run.
        rig.DropIndexCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task EmptyRedis_NoIndexes_ReturnsCleanly()
    {
        RecordingReconciler rig = CreateReconciler();

        await rig.Reconciler.ReconcileAsync(CancellationToken.None);

        rig.DropIndexCalls.ShouldBeEmpty();
    }

    private static RecordingReconciler CreateReconciler(params string[] existingIndexes)
    {
        List<string> dropCalls = [];
        IDatabase db = Substitute.For<IDatabase>();

        RedisResult listResult = RedisResult.Create(
            [.. existingIndexes.Select(name => RedisResult.Create(new RedisValue(name)))]);

        db.ExecuteAsync("FT._LIST").Returns(listResult);
        db.ExecuteAsync(
                Arg.Is<string>(c => c == "FT.DROPINDEX"),
                Arg.Do<object[]>(args => dropCalls.Add(args[0]?.ToString() ?? string.Empty)))
            .Returns(RedisResult.Create(new RedisValue("OK")));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        OrphanSemanticIndexReconciler reconciler = new(redis, Substitute.For<ILogger<OrphanSemanticIndexReconciler>>());
        return new RecordingReconciler(reconciler, dropCalls);
    }

    private sealed record RecordingReconciler(OrphanSemanticIndexReconciler Reconciler, List<string> DropIndexCalls);
}
