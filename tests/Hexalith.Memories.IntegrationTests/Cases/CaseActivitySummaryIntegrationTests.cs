// <copyright file="CaseActivitySummaryIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Cases;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Cases;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using StackExchange.Redis;

/// <summary>Verifies the retained activity stream and its Redis summary agree after legacy backfill.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class CaseActivitySummaryIntegrationTests(RedisStackFixture redis)
{
    [Theory]
    [InlineData(null, 10)]
    [InlineData("invalid-count", 10)]
    [InlineData(" 2 ", 10)]
    [InlineData("1", 10)]
    [InlineData("400", 408)]
    public async Task ConcurrentFailureWritesPreserveLegacyFailures(string? initialCount, int expectedCount)
    {
        string tenant = $"case-summary-{Guid.NewGuid():N}";
        const string CaseId = "legacy-case";
        RedisKey stream = $"{tenant}:case:{CaseId}:activity";
        RedisKey summary = $"{stream}:summary";
        IDatabase db = redis.Connection.GetDatabase();
        NameValueEntry[] oldFailure = [new("type", "ingestionFailed"), new("actor", "system"), new("description", "Old failure")];
        _ = await db.StreamAddAsync(stream, oldFailure);
        _ = await db.StreamAddAsync(stream, oldFailure);
        if (initialCount is not null)
        {
            _ = await db.HashSetAsync(summary, "failedCount", initialCount);
        }

        var service = new CaseActivityService(redis.Connection, NullLogger<CaseActivityService>.Instance);
        Task<bool>[] writes = Enumerable.Range(0, 8)
            .Select(index => service.RecordEventAsync(tenant, CaseId, CaseActivityEventType.IngestionFailed, "system", $"Failure {index}"))
            .ToArray();
        _ = await service.GetFailedCountAsync(tenant, CaseId);
        (await Task.WhenAll(writes)).ShouldAllBe(written => written);

        (await db.StreamLengthAsync(stream)).ShouldBe(10);
        ((int)await db.HashGetAsync(summary, "failedCount")).ShouldBe(expectedCount);
        (await db.HashGetAsync(summary, "failedCountReconciled")).ToString().ShouldBe("1");
        (await service.GetFailedCountAsync(tenant, CaseId)).ShouldBe(expectedCount);
    }
}
