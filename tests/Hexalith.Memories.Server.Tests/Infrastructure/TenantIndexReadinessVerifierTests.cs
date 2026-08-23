// <copyright file="TenantIndexReadinessVerifierTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Infrastructure;

using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Story 23.7 (A34): unit coverage for <see cref="TenantIndexReadinessVerifier"/> — one FT.INFO check
/// per tenant/index family/process, per-tenant and per-dimension isolation, missing-index and schema-mismatch
/// failures, additive TAG-field upgrade before caching, thundering-herd coalescing, and non-caching of failures.</summary>
public sealed class TenantIndexReadinessVerifierTests
{
    private static TenantIndexReadinessVerifier CreateVerifier()
        => new(NullLogger<TenantIndexReadinessVerifier>.Instance);

    [Fact]
    public async Task EnsureReadyAsync_HealthySyntacticIndex_VerifiesViaFtInfo()
    {
        IDatabase db = MockDb(indexName => SyntacticInfo(SyntacticTenant(indexName)));

        await CreateVerifier().EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None);

        db.Received(1).Execute("FT.INFO", Arg.Any<object[]>());
        db.DidNotReceive().Execute("FT.CREATE", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_SecondCallSameTenantFamily_SkipsRedis()
    {
        IDatabase db = MockDb(indexName => SyntacticInfo(SyntacticTenant(indexName)));
        TenantIndexReadinessVerifier verifier = CreateVerifier();

        await verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None);
        await verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None);

        db.Received(1).Execute("FT.INFO", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_DifferentTenants_VerifySeparately()
    {
        IDatabase db = MockDb(indexName => SyntacticInfo(SyntacticTenant(indexName)));
        TenantIndexReadinessVerifier verifier = CreateVerifier();

        await verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None);
        await verifier.EnsureReadyAsync(db, "tenant-b", TenantIndexFamily.Syntactic, null, CancellationToken.None);

        db.Received(2).Execute("FT.INFO", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_SameTenantDifferentDimensions_VerifySeparately()
    {
        // The dim-3 success must not authorize a dim-4 write: the dim-4 key re-verifies and fails against the real
        // dim-3 index, proving the two dimensions are cached independently.
        IDatabase db = MockDb(_ => SemanticInfo("tenant-a", dimensions: 3));
        TenantIndexReadinessVerifier verifier = CreateVerifier();

        await verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Semantic, 3, CancellationToken.None);
        await Should.ThrowAsync<TenantIndexSchemaMismatchException>(
            () => verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Semantic, 4, CancellationToken.None));

        db.Received(2).Execute("FT.INFO", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_MissingIndex_ThrowsNotProvisioned()
    {
        IDatabase db = MockDb(_ => throw Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Unknown index name"));

        TenantIndexNotProvisionedException ex = await Should.ThrowAsync<TenantIndexNotProvisionedException>(
            () => CreateVerifier().EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None));

        ex.TenantId.ShouldBe("tenant-a");
        ex.Family.ShouldBe(TenantIndexFamily.Syntactic);
        ex.Message.ShouldContain("is missing");
        ex.Message.ShouldContain("does not create indexes on demand");
        db.DidNotReceive().Execute("FT.CREATE", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_IncompatibleDimensions_ThrowsSchemaMismatch()
    {
        IDatabase db = MockDb(_ => SemanticInfo("tenant-a", dimensions: 99));

        TenantIndexSchemaMismatchException ex = await Should.ThrowAsync<TenantIndexSchemaMismatchException>(
            () => CreateVerifier().EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Semantic, 3, CancellationToken.None));

        ex.Message.ShouldContain("does not match the expected tenant schema");
        ex.Message.ShouldContain("expected 3 dimensions but found 99");
    }

    [Fact]
    public async Task EnsureReadyAsync_IncompatiblePrefix_ThrowsSchemaMismatch()
    {
        // A wrong prefix is not an additive-upgradeable difference; it must fail before any write.
        IDatabase db = MockDb(_ => SyntacticInfo("other-tenant"));

        await Should.ThrowAsync<TenantIndexSchemaMismatchException>(
            () => CreateVerifier().EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureReadyAsync_MissingAdditiveTagField_UpgradesInPlaceBeforeCaching()
    {
        IDatabase db = MockDb(indexName => SyntacticInfo(SyntacticTenant(indexName), includeCloudEventSubject: false));

        await CreateVerifier().EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None);

        db.Received().Execute(
            "FT.ALTER",
            Arg.Is<object[]>(args => args!.Length == 5
                && args[1].ToString() == "SCHEMA"
                && args[2].ToString() == "ADD"
                && args[3].ToString() == "cloudeventSubject"
                && args[4].ToString() == "TAG"));
    }

    [Fact]
    public async Task EnsureReadyAsync_ConcurrentFirstWrites_VerifyOnce()
    {
        // Story 23.6 bounded parallelism can drive many concurrent first writes for the same tenant: they must
        // coalesce into a single FT.INFO rather than a thundering herd of duplicate checks.
        IDatabase db = MockDb(indexName => SyntacticInfo(SyntacticTenant(indexName)));
        TenantIndexReadinessVerifier verifier = CreateVerifier();

        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ =>
            verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None)));

        db.Received(1).Execute("FT.INFO", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_FailedCheck_IsNotCached_AndReVerifiesOnRetry()
    {
        // AC8: a failed readiness check must never be cached as success, and a stale failure must not wedge the
        // tenant — a later write re-verifies once the index is provisioned.
        int calls = 0;
        IDatabase db = MockDb(indexName =>
        {
            calls++;
            return calls == 1
                ? throw Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Unknown index name")
                : SyntacticInfo(SyntacticTenant(indexName));
        });
        TenantIndexReadinessVerifier verifier = CreateVerifier();

        await Should.ThrowAsync<TenantIndexNotProvisionedException>(
            () => verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None));
        await verifier.EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Syntactic, null, CancellationToken.None);

        db.Received(2).Execute("FT.INFO", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_HealthyNaturalLanguageIndex_Verifies()
    {
        IDatabase db = MockDb(_ => NaturalLanguageInfo("tenant-a", dimensions: 3));

        await CreateVerifier().EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.NaturalLanguageSemantic, 3, CancellationToken.None);

        db.Received(1).Execute("FT.INFO", Arg.Any<object[]>());
    }

    [Fact]
    public async Task EnsureReadyAsync_VectorFamilyWithoutDimensions_Throws()
    {
        IDatabase db = MockDb(_ => SemanticInfo("tenant-a", dimensions: 3));

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => CreateVerifier().EnsureReadyAsync(db, "tenant-a", TenantIndexFamily.Semantic, null, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureReadyAsync_InvalidTenant_Throws()
    {
        IDatabase db = MockDb(_ => SyntacticInfo("tenant-a"));

        await Should.ThrowAsync<ArgumentException>(
            () => CreateVerifier().EnsureReadyAsync(db, "bad tenant; DROP", TenantIndexFamily.Syntactic, null, CancellationToken.None));
    }

    private static IDatabase MockDb(Func<string, RedisResult> onInfo)
    {
        IDatabase db = Substitute.For<IDatabase>();

        RedisResult Execute(string command, IReadOnlyList<object> args)
            => command == "FT.INFO"
                ? onInfo(args.Count > 0 ? args[0]?.ToString() ?? string.Empty : string.Empty)
                : RedisResult.Create(new RedisValue("OK"));

        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(call => Execute(call.ArgAt<string>(0), call.ArgAt<object[]>(1)));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call => Execute(call.ArgAt<string>(0), [.. call.ArgAt<ICollection<object>>(1)]));
        return db;
    }

    private static string SyntacticTenant(string indexName)
        => indexName.Replace(IndexSchemaDefinitions.SyntacticIndexSuffix, string.Empty, StringComparison.Ordinal);

    private static RedisResult SyntacticInfo(string tenantId, bool includeCloudEventSubject = true)
    {
        List<(string Id, string Type, int? Dim)> attributes =
        [
            ("content", "TEXT", null),
            ("sourceUriText", "TEXT", null),
            ("sourceTypeText", "TEXT", null),
            ("metadataText", "TEXT", null),
            ("sourceUri", "TAG", null),
            ("sourceType", "TAG", null),
            ("contentHash", "TAG", null),
            ("caseId", "TAG", null),
            ("attributeTags", "TAG", null),
            ("embeddingProvider", "TAG", null),
        ];

        if (includeCloudEventSubject)
        {
            attributes.Add(("cloudeventSubject", "TAG", null));
        }

        return Info(IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId), attributes);
    }

    private static RedisResult SemanticInfo(string tenantId, int dimensions)
        => Info(
            IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId),
            [
                ("embedding", "VECTOR", dimensions),
                ("memoryUnitId", "TAG", null),
                ("caseId", "TAG", null),
                ("cloudeventSubject", "TAG", null),
            ]);

    private static RedisResult NaturalLanguageInfo(string tenantId, int dimensions)
        => Info(
            IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId),
            [
                ("embedding", "VECTOR", dimensions),
                ("memoryUnitId", "TAG", null),
                ("caseId", "TAG", null),
                ("naturalLanguageDescription", "TEXT", null),
            ]);

    private static RedisResult Info(string prefix, IReadOnlyList<(string Id, string Type, int? Dim)> attributes)
    {
        List<RedisResult> attributeResults = [];
        foreach ((string id, string type, int? dim) in attributes)
        {
            List<RedisResult> keyValues =
            [
                RedisResult.Create(new RedisValue("identifier")),
                RedisResult.Create(new RedisValue(id)),
                RedisResult.Create(new RedisValue("attribute")),
                RedisResult.Create(new RedisValue(id)),
                RedisResult.Create(new RedisValue("type")),
                RedisResult.Create(new RedisValue(type)),
            ];

            if (dim is int dimensions)
            {
                keyValues.Add(RedisResult.Create(new RedisValue("dim")));
                keyValues.Add(RedisResult.Create(new RedisValue(dimensions.ToString())));
            }

            attributeResults.Add(RedisResult.Create([.. keyValues]));
        }

        return RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("index_definition")),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("prefixes")),
                RedisResult.Create([RedisResult.Create(new RedisValue(prefix))]),
            ]),
            RedisResult.Create(new RedisValue("attributes")),
            RedisResult.Create([.. attributeResults]),
        ]);
    }
}
