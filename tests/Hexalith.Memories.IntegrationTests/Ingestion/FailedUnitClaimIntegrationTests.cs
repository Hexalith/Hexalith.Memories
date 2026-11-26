// <copyright file="FailedUnitClaimIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using StackExchange.Redis;

[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class FailedUnitClaimIntegrationTests(RedisStackFixture fixture)
{
    [Fact]
    public async Task ClaimWriteFailurePreservesTheFailedUnitAndOnlyOneRetryCanClaimAfterRepair()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tenantId = $"tenant-claim-{Guid.NewGuid():N}";
        const string CaseId = "case-claim";
        const string UnitId = "unit-claim";
        const string SourceUri = "file:///test.txt";
        RedisKey hash = PersistFailedUnitActivity.BuildHashKey(tenantId, UnitId);
        RedisKey failedIndex = PersistFailedUnitActivity.BuildSortedSetKey(tenantId, CaseId);
        RedisKey dedup = DedupKeyBuilder.BuildKey(tenantId, CaseId, SourceUri);
        IDatabase redis = fixture.Connection.GetDatabase();
        var registry = new FailedUnitsRegistry(fixture.Connection, NullLogger<FailedUnitsRegistry>.Instance);

        try
        {
            await redis.HashSetAsync(hash, [new HashEntry("sourceUri", SourceUri)]);
            await redis.StringSetAsync(dedup, UnitId);
            await redis.StringSetAsync(failedIndex, "Test index key with an incompatible type");

            RedisServerException failure = await Should.ThrowAsync<RedisServerException>(ClaimAsync);
            failure.Message.ShouldContain("WRONGTYPE");
            (await redis.HashGetAsync(hash, "sourceUri")).ShouldBe((RedisValue)SourceUri);
            (await redis.StringGetAsync(dedup)).ShouldBe((RedisValue)UnitId);

            await redis.KeyDeleteAsync(failedIndex);
            await redis.SortedSetAddAsync(failedIndex, UnitId, 1);
            (await Task.WhenAll(ClaimAsync(), ClaimAsync())).Count(claimed => claimed).ShouldBe(1);
            (await redis.KeyExistsAsync(hash)).ShouldBeFalse();
            (await redis.KeyExistsAsync(dedup)).ShouldBeFalse();
            (await redis.SortedSetLengthAsync(failedIndex)).ShouldBe(0);
        }
        finally
        {
            await redis.KeyDeleteAsync([hash, failedIndex, dedup]);
        }

        Task<bool> ClaimAsync() => registry.RemoveAsync(tenantId, CaseId, UnitId, SourceUri, cancellationToken);
    }
}
