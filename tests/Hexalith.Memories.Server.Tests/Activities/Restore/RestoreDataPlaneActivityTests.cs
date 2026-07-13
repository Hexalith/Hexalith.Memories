// <copyright file="RestoreDataPlaneActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Restore;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Restore;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Import;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Workflows.Contracts;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 26.2 (AC2/AC3) — Docker-free coverage for the data-plane restore activity: edges are reconstructed
/// from <c>(source, target, type)</c> (never the exported edge id), dangling endpoints get stub nodes, the
/// syntactic hash is written, and a re-run converges (idempotent).
/// </summary>
public class RestoreDataPlaneActivityTests
{
    private const string TenantEnvelope = """
    {
      "manifest": { "schemaVersion": 1, "scope": "tenant", "tenantId": "acme", "caseId": null, "exportedAt": "2026-07-13T00:00:00+00:00", "snapshotAt": "2026-07-13T00:00:00+00:00" },
      "cases": [
        { "id": "case-1", "tenantId": "acme", "name": "Case One", "status": "active", "createdAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-02T00:00:00+00:00", "memoryUnitCount": 2,
          "members": [ { "memberId": "user-1", "memberType": "user", "addedAt": "2026-07-01T00:00:00+00:00" } ] }
      ],
      "memoryUnits": [
        { "unit": { "id": "mu-1", "tenantId": "acme", "caseId": "case-1", "content": "hello", "contentHash": "h1", "sourceUri": "file:///a.txt", "sourceType": "file", "ingestedBy": "tester", "ingestedAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-01T00:00:00+00:00", "status": "indexed", "metadata": {}, "embeddingProvider": "google:text-embedding-004", "embeddingModel": "text-embedding-004", "embeddingDimensions": 768 }, "annotationTargets": [] },
        { "unit": { "id": "mu-2", "tenantId": "acme", "caseId": "case-1", "content": "world", "contentHash": "h2", "sourceUri": "file:///b.txt", "sourceType": "file", "ingestedBy": "tester", "ingestedAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-01T00:00:00+00:00", "status": "indexed", "metadata": {}, "embeddingProvider": "google:text-embedding-004", "embeddingModel": "text-embedding-004", "embeddingDimensions": 768 }, "annotationTargets": [] }
      ],
      "edges": [
        { "id": "9999", "sourceId": "mu-1", "targetId": "mu-outside", "edgeType": "causedBy", "confidence": 0.9, "origin": "inferred", "createdAt": "2026-07-03T00:00:00+00:00", "verifiedBy": "reviewer-1", "previousConfidence": 0.5 }
      ],
      "statistics": { "memoryUnitCount": 2, "edgeCount": 1, "caseCount": 1 }
    }
    """;

    [Fact]
    public async Task RunAsync_ReconstructsEdgeIdentityAndRestoresDataPlane()
    {
        Fixture fixture = Fixture.Create(TenantEnvelope);

        RestoreDataPlaneResult result = await fixture.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreDataPlaneInput("acme", null, "acme:import:staging:instance-1", "operator"));

        result.MemoryUnitIds.ShouldBe(["mu-1", "mu-2"]);
        result.RestoredCaseCount.ShouldBe(1);
        result.RestoredEdgeCount.ShouldBe(1);

        // Edge identity is reconstructed from (source, target, type) with the full audit trail — the exported
        // graph-instance id "9999" is never used.
        fixture.Builder.Received(1).BuildRestoreEdge(
            "mu-1",
            "mu-outside",
            EdgeType.CausedBy,
            0.9f,
            EdgeOrigin.Inferred,
            Arg.Any<DateTimeOffset>(),
            "reviewer-1",
            0.5f);

        // Case + memory-unit nodes merged.
        fixture.Builder.Received(1).BuildMergeCaseNode("case-1", "Case One", "acme", Arg.Any<DateTimeOffset>());
        fixture.Builder.Received(1).BuildMergeMemoryUnitNode(
            "mu-1", "case-1", "hello", "h1", "file:///a.txt", SourceType.File,
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), "tester", Arg.Any<DateTimeOffset>(), Arg.Any<string>());

        // Syntactic hashes written under the tenant-prefixed key.
        await fixture.RedisDatabase.Received().HashSetAsync("acme:mu:mu-1", Arg.Any<HashEntry[]>());
        await fixture.RedisDatabase.Received().HashSetAsync("acme:mu:mu-2", Arg.Any<HashEntry[]>());
        await fixture.RedisDatabase.Received().HashSetAsync("acme:case:case-1", Arg.Any<HashEntry[]>());
    }

    [Fact]
    public async Task RunAsync_DanglingEdgeEndpoint_CreatesStubNode()
    {
        Fixture fixture = Fixture.Create(TenantEnvelope);

        await fixture.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreDataPlaneInput("acme", null, "acme:import:staging:instance-1", "operator"));

        // "mu-outside" is not among the exported memory units (case-scope dangling endpoint pattern) — restore
        // must stub it rather than fail.
        fixture.Builder.Received().BuildMergeStubNode("mu-outside", Arg.Any<DateTimeOffset>());
        fixture.Builder.Received().BuildMergeStubNode("mu-1", Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task RunAsync_RunTwice_ConvergesToSameResult()
    {
        Fixture fixture = Fixture.Create(TenantEnvelope);
        RestoreDataPlaneInput input = new("acme", null, "acme:import:staging:instance-1", "operator");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        RestoreDataPlaneResult first = await fixture.Activity.RunAsync(context, input);
        RestoreDataPlaneResult second = await fixture.Activity.RunAsync(context, input);

        second.MemoryUnitIds.ShouldBe(first.MemoryUnitIds);
        second.RestoredCaseCount.ShouldBe(first.RestoredCaseCount);
        second.RestoredEdgeCount.ShouldBe(first.RestoredEdgeCount);
    }

    [Fact]
    public async Task RunAsync_StagingExpired_Throws()
    {
        Fixture fixture = Fixture.Create(TenantEnvelope);
        fixture.StagingStore.RetrieveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        ImportEnvelopeException ex = await Should.ThrowAsync<ImportEnvelopeException>(() => fixture.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreDataPlaneInput("acme", null, "acme:import:staging:instance-1", "operator")));

        ex.Code.ShouldBe("IMPORT_STAGING_EXPIRED");
    }

    [Fact]
    public async Task RunAsync_EdgeWithInvalidConfidence_SkipsEdgeAndReports()
    {
        Fixture fixture = Fixture.Create(TenantEnvelope);

        // Simulate the real GraphQueryBuilder rejecting an out-of-range / non-finite confidence (proved in
        // GraphQueryBuilderRestoreEdgeTests). Story 26.2 review decision D4: the activity must skip that edge
        // best-effort and report it via the skipped count, NOT abort the whole restore.
        fixture.Builder
            .BuildRestoreEdge("mu-1", "mu-outside", EdgeType.CausedBy, Arg.Any<float>(), Arg.Any<EdgeOrigin>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<float?>())
            .Throws(new ArgumentOutOfRangeException("confidence"));

        RestoreDataPlaneResult result = await fixture.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreDataPlaneInput("acme", null, "acme:import:staging:instance-1", "operator"));

        result.RestoredEdgeCount.ShouldBe(0);
        result.SkippedRecords.ShouldBe(1);

        // The data-plane units are still restored — one corrupt edge does not abort the whole restore.
        result.MemoryUnitIds.ShouldBe(["mu-1", "mu-2"]);
    }

    [Fact]
    public async Task RunAsync_MemoryUnitWithBlankCaseId_SkipsUnitAndReports()
    {
        const string envelope = """
        {
          "manifest": { "schemaVersion": 1, "scope": "tenant", "tenantId": "acme", "caseId": null, "exportedAt": "2026-07-13T00:00:00+00:00", "snapshotAt": "2026-07-13T00:00:00+00:00" },
          "cases": [],
          "memoryUnits": [
            { "unit": { "id": "mu-ok", "tenantId": "acme", "caseId": "case-1", "content": "hello", "contentHash": "h1", "sourceUri": "file:///a.txt", "sourceType": "file", "ingestedBy": "tester", "ingestedAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-01T00:00:00+00:00", "status": "indexed", "metadata": {}, "embeddingProvider": "google:text-embedding-004", "embeddingModel": "text-embedding-004", "embeddingDimensions": 768 }, "annotationTargets": [] },
            { "unit": { "id": "mu-blank", "tenantId": "acme", "caseId": "", "content": "orphan", "contentHash": "h2", "sourceUri": "file:///b.txt", "sourceType": "file", "ingestedBy": "tester", "ingestedAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-01T00:00:00+00:00", "status": "indexed", "metadata": {}, "embeddingProvider": "google:text-embedding-004", "embeddingModel": "text-embedding-004", "embeddingDimensions": 768 }, "annotationTargets": [] }
          ],
          "edges": [],
          "statistics": { "memoryUnitCount": 2, "edgeCount": 0, "caseCount": 0 }
        }
        """;

        Fixture fixture = Fixture.Create(envelope);

        RestoreDataPlaneResult result = await fixture.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreDataPlaneInput("acme", null, "acme:import:staging:instance-1", "operator"));

        // The blank-caseId unit is corrupt (ingestion always sets a caseId) — skipped + reported, not fatal,
        // and its syntactic hash is never written (Story 26.2 review decision D4).
        result.MemoryUnitIds.ShouldBe(["mu-ok"]);
        result.SkippedRecords.ShouldBe(1);
        await fixture.RedisDatabase.DidNotReceive().HashSetAsync("acme:mu:mu-blank", Arg.Any<HashEntry[]>());
    }

    private sealed class Fixture
    {
        public required RestoreDataPlaneActivity Activity { get; init; }

        public required IGraphQueryBuilder Builder { get; init; }

        public required IDatabase RedisDatabase { get; init; }

        public required IImportStagingStore StagingStore { get; init; }

        public static Fixture Create(string envelopeJson)
        {
            byte[] payload = Encoding.UTF8.GetBytes(envelopeJson);

            IImportStagingStore stagingStore = Substitute.For<IImportStagingStore>();
            stagingStore.RetrieveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(payload);

            IDatabase redisDb = Substitute.For<IDatabase>();
            IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
            redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

            (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
            IGraphQueryBuilder builder = CreateMockBuilder();
            ITenantIndexReadinessVerifier readiness = Substitute.For<ITenantIndexReadinessVerifier>();

            RestoreDataPlaneActivity activity = new(
                stagingStore,
                redis,
                falkorDb,
                builder,
                Substitute.For<ILogger<RestoreDataPlaneActivity>>(),
                readiness);

            return new Fixture
            {
                Activity = activity,
                Builder = builder,
                RedisDatabase = redisDb,
                StagingStore = stagingStore,
            };
        }

        private static IGraphQueryBuilder CreateMockBuilder()
        {
            IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
            (string, IDictionary<string, object>) dummy = ("RETURN 1", new Dictionary<string, object>());

            builder.BuildMergeCaseNode(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>())
                .Returns(dummy);
            builder.BuildMergeMemoryUnitNode(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<SourceType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(),
                    Arg.Any<DateTimeOffset>(), Arg.Any<string>())
                .Returns(dummy);
            builder.BuildMergeEdge(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EdgeType>(), Arg.Any<float>(), Arg.Any<EdgeOrigin>())
                .Returns(dummy);
            builder.BuildMergeStubNode(Arg.Any<string>(), Arg.Any<DateTimeOffset>())
                .Returns(dummy);
            builder.BuildRestoreEdge(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EdgeType>(), Arg.Any<float>(), Arg.Any<EdgeOrigin>(),
                    Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<float?>())
                .Returns(dummy);
            return builder;
        }

        private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
        {
            IDatabase db = Substitute.For<IDatabase>();
            IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
            falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

            // FalkorDB.QueryAsync calls db.ExecuteAsync("GRAPH.QUERY", ...); it must return a 3-element array.
            RedisResult fakeGraphResult = RedisResult.Create(
            [
                RedisResult.Create(Array.Empty<RedisResult>()),
                RedisResult.Create(Array.Empty<RedisResult>()),
                RedisResult.Create(
                [
                    RedisResult.Create(new RedisValue("Nodes created: 0")),
                    RedisResult.Create(new RedisValue("Relationships created: 0")),
                    RedisResult.Create(new RedisValue("Cached execution: 0")),
                    RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
                ]),
            ]);

            db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(fakeGraphResult);
            db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>()).Returns(fakeGraphResult);
            return (falkorDb, db);
        }
    }
}
