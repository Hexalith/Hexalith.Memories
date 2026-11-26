// <copyright file="RestoreStagingBoundaryIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Integration.NightlyTests.Restore;

using System.Security.Cryptography;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Import;

using Shouldly;

using StackExchange.Redis;

[Collection("RedisStack")]
[Trait("Category", "Integration")]
[Trait("Category", "IntegrationSlow")]
public sealed class RestoreStagingBoundaryIntegrationTests(RedisStackFixture fixture)
{
    [Fact]
    public async Task StageAsync_At512MiB_RetainsAllBytesAndReindexIdsThroughRenewal()
    {
        const int ChunkCount = 512;
        const long MaxBytes = (long)ChunkCount * 1024 * 1024;
        const string Instance = "boundary";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tenant = $"restore-boundary-{Guid.NewGuid():N}";
        string key = RedisImportStagingStore.BuildKey(tenant, Instance);
        RedisImportStagingStore store = new(fixture.Connection);
        IDatabase redis = fixture.Connection.GetDatabase();

        string reindexKey = key + ":reindex";
        string leaseKey = tenant + ":restore:lease";
        RedisKey[] retainedKeys = [key, reindexKey, leaseKey, .. Enumerable.Range(0, ChunkCount)
            .Select(chunk => (RedisKey)RedisImportStagingStore.BuildChunkKey(key, chunk))];
        DirectoryInfo directory = Directory.CreateTempSubdirectory("memories-restore-");
        try
        {
            await using FileStream payload = File.Create(Path.Combine(directory.FullName, "upload.json"));
            payload.SetLength(MaxBytes);
            payload.WriteByte(0x42);
            payload.Position = MaxBytes / 2;
            payload.WriteByte(0x17);
            payload.Position = MaxBytes - 1;
            payload.WriteByte(0x63);
            payload.Position = 0;
            byte[] expectedHash = await SHA256.HashDataAsync(payload, cancellationToken);
            payload.Position = 0;

            (await store.StageAsync(tenant, Instance, payload, MaxBytes, cancellationToken)).ShouldBe(key);
            ((long)await redis.HashGetAsync(key, "length")).ShouldBe(MaxBytes);
            ((int)await redis.HashGetAsync(key, "chunkCount")).ShouldBe(ChunkCount);
            (await store.AcquireRestoreLeaseAsync(key, tenant, null, Instance, cancellationToken)).Acquired.ShouldBeTrue();

            string[] ids = Enumerable.Range(0, 100_003).Select(index => $"unit-{index:D6}").ToArray();
            await store.AppendReindexIdsAsync(key, ids, cancellationToken);
            (await redis.ListLengthAsync(reindexKey)).ShouldBe(ids.LongLength);
            (await store.ReadReindexIdsAsync(key, 990, 100, cancellationToken)).ShouldBe(ids[990..1_090]);
            (await store.ReadReindexIdsAsync(key, 100_000, 100, cancellationToken)).ShouldBe(ids[100_000..]);

            await redis.HashSetAsync(key, "lastRenewed", DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds());
            bool[] shortened = await Task.WhenAll(retainedKeys.Select(k => redis.KeyExpireAsync(k, TimeSpan.FromMinutes(10))));
            shortened.ShouldAllBe(exists => exists);
            await store.RenewAsync(key, cancellationToken);
            TimeSpan?[] renewed = await Task.WhenAll(retainedKeys.Select(k => redis.KeyTimeToLiveAsync(k)));
            renewed.ShouldAllBe(ttl => ttl > TimeSpan.FromHours(11) && ttl <= RedisImportStagingStore.StagingTtl);
            (await store.OwnsRestoreLeaseAsync(key, cancellationToken)).ShouldBeTrue();

            await using (Stream staged = (await store.OpenReadAsync(key, cancellationToken)).ShouldNotBeNull())
            {
                (await SHA256.HashDataAsync(staged, cancellationToken)).ShouldBe(expectedHash);
            }

            await store.DeleteAsync(key, cancellationToken);
            (await redis.KeyExistsAsync(retainedKeys)).ShouldBe(0);

            payload.SetLength(MaxBytes + 1);
            payload.Position = 0;
            await Should.ThrowAsync<InvalidDataException>(
                () => store.StageAsync(tenant, Instance, payload, MaxBytes, cancellationToken));
            (await redis.KeyExistsAsync(retainedKeys)).ShouldBe(0);
        }
        finally
        {
            try
            {
                await store.DeleteAsync(key, CancellationToken.None);
            }
            finally
            {
                directory.Delete(recursive: true);
            }
        }

        (await redis.KeyExistsAsync(retainedKeys)).ShouldBe(0);
    }
}
