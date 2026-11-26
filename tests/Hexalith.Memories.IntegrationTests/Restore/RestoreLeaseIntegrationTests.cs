// <copyright file="RestoreLeaseIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Restore;

using System.Text;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Import;

using Shouldly;

[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class RestoreLeaseIntegrationTests(RedisStackFixture fixture)
{
    [Fact]
    public async Task ConcurrentUploads_CoalesceOnlyWithinTenantAndKeepTheWinningLease()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RedisImportStagingStore store = new(fixture.Connection);
        string tenant = $"tenant-restore-{Guid.NewGuid():N}";
        string otherTenant = tenant + "-other";
        List<string> stagingKeys = [];
        try
        {
            string first = await StageAsync(tenant, "first", "same");
            string second = await StageAsync(tenant, "second", "same");
            string different = await StageAsync(tenant, "different", "different");
            string other = await StageAsync(otherTenant, "other", "same");

            RestoreLeaseResult[] attempts = await Task.WhenAll(
                store.AcquireRestoreLeaseAsync(first, tenant, null, "first", cancellationToken),
                store.AcquireRestoreLeaseAsync(second, tenant, null, "second", cancellationToken));
            RestoreLeaseResult winner = attempts.Single(attempt => attempt.Acquired);
            RestoreLeaseResult duplicate = attempts.Single(attempt => !attempt.Acquired);
            duplicate.ShouldBe(new RestoreLeaseResult(false, winner.InstanceId, true));

            (await store.AcquireRestoreLeaseAsync(different, tenant, "case-1", "different", cancellationToken))
                .ShouldBe(new RestoreLeaseResult(false, winner.InstanceId, false));
            (await store.AcquireRestoreLeaseAsync(other, otherTenant, null, "other", cancellationToken))
                .ShouldBe(new RestoreLeaseResult(true, "other", false));

            string winnerKey = winner.InstanceId == "first" ? first : second;
            string duplicateKey = winner.InstanceId == "first" ? second : first;
            await store.DeleteAsync(duplicateKey, cancellationToken);
            (await store.OwnsRestoreLeaseAsync(winnerKey, cancellationToken)).ShouldBeTrue();
            (await store.OwnsRestoreLeaseAsync(other, cancellationToken)).ShouldBeTrue();

            await store.DeleteAsync(winnerKey, cancellationToken);
            (await store.AcquireRestoreLeaseAsync(different, tenant, "case-1", "different", cancellationToken))
                .ShouldBe(new RestoreLeaseResult(true, "different", false));
        }
        finally
        {
            foreach (string key in stagingKeys)
            {
                await store.DeleteAsync(key, CancellationToken.None);
            }
        }

        async Task<string> StageAsync(string target, string instance, string content)
        {
            using MemoryStream input = new(Encoding.UTF8.GetBytes(content));
            string key = await store.StageAsync(target, instance, input, 1024, cancellationToken);
            stagingKeys.Add(key);
            return key;
        }
    }
}
