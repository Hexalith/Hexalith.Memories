// <copyright file="RetryBacklogDiscoveryIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using StackExchange.Redis;

/// <summary>Verifies retry discovery reconciles legacy backlog tenants in Redis.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class RetryBacklogDiscoveryIntegrationTests(RedisStackFixture redis)
{
    [Fact]
    public async Task FirstPollRepairsLegacyMembershipAlongsideAnIndexedTenant()
    {
        string scope = $"retry-discovery-{Guid.NewGuid():N}";
        string indexed = $"{scope}-indexed";
        string legacy = $"{scope}-legacy";
        string fenced = $"{scope}-fenced";
        string wrongType = $"{scope}-wrong-type";
        IDatabase db = redis.Connection.GetDatabase();
        RedisKey index = FailedNaturalLanguageEmbeddingRegistry.TenantBacklogKey;
        foreach (string tenant in new[] { indexed, legacy, fenced })
        {
            _ = await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), "pending-retry", 1);
        }

        _ = await db.SetAddAsync(index, indexed);
        _ = await db.StringSetAsync(TenantIngestionOwnership.Fence(fenced), "1");
        _ = await db.StringSetAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(wrongType), "other-data");

        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        List<string> listed = [];
        await foreach (string tenant in registry.ListTenantsWithBacklogAsync(TestContext.Current.CancellationToken))
        {
            if (tenant.StartsWith(scope, StringComparison.Ordinal))
            {
                listed.Add(tenant);
            }
        }

        listed.Order(StringComparer.Ordinal).ShouldBe(new[] { indexed, legacy });
        (await db.SetContainsAsync(index, legacy)).ShouldBeTrue();
        (await db.SetContainsAsync(index, fenced)).ShouldBeFalse();
        (await db.SetContainsAsync(index, wrongType)).ShouldBeFalse();
        (await db.SortedSetLengthAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(fenced))).ShouldBe(1);
    }
}
