// <copyright file="DeleteMemoryUnitProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using System.Net;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.DerivedStores;

using StackExchange.Redis;

/// <summary>Deletes memory-unit read-model projections idempotently.</summary>
internal sealed class DeleteMemoryUnitProjectionActivity(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder,
    SourceInventoryStore? inventory = null, IConfiguration? configuration = null, IHostEnvironment? environment = null,
    IWorkflowPayloadStore? payloadStore = null) : WorkflowActivity<MemoryUnitDeletionProjectionInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, MemoryUnitDeletionProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = redis.GetDatabase();
        NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());

        foreach (string annotationId in input.AnnotationMemoryUnitIds)
        {
            await DeleteOneAsync(db, falkor, input.TenantId, annotationId).ConfigureAwait(false);
        }

        await DeleteOneAsync(db, falkor, input.TenantId, input.MemoryUnitId).ConfigureAwait(false);
        return true;
    }

    private async Task DeleteOneAsync(IDatabase db, NFalkorDB.FalkorDB falkor, string tenantId, string memoryUnitId)
    {
        // Tombstones precede all deletion and survive it. Retried/delayed staged writes
        // cannot recreate personal content after a terminal absence observation.
        await db.StringSetAsync(SourcePublicationVisibility.Tombstone(tenantId, memoryUnitId), "1").ConfigureAwait(false);
        await falkor.SelectGraph(tenantId).QueryAsync("MERGE (t:MemoryUnitTombstone {id:$id})",
            new Dictionary<string, object> { ["id"] = memoryUnitId }).ConfigureAwait(false);
        if (payloadStore is not null)
        {
            await new WorkflowPayloadLedger(redis).PurgeAsync(tenantId, memoryUnitId, payloadStore, CancellationToken.None).ConfigureAwait(false);
        }
        string muKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);
        RedisKey[] semanticKeys = await FindSemanticKeysAsync(tenantId, memoryUnitId).ConfigureAwait(false);
        string nlVecKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId);
        string sourceArtifactKey = RedisDerivedStoreService.BuildSourceArtifactKey(tenantId, memoryUnitId);
        (string graphQuery, IDictionary<string, object> graphParams) = graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);

        if (environment?.IsProduction() == false && configuration?.GetValue("Memories:Testing:EnableSourceInventoryQualification", false) == true
            && configuration.GetValue("Memories:Testing:UseFakeEmbedding", false))
        {
            int delay = Math.Clamp(configuration.GetValue("Memories:Testing:SourceInventoryDeletionDelayMilliseconds", 0), 0, 5000);
            if (delay > 0) { await Task.Delay(delay).ConfigureAwait(false); }
        }
        await Task.WhenAll(
            db.KeyDeleteAsync(semanticKeys),
            db.KeyDeleteAsync(nlVecKey),
            db.KeyDeleteAsync(sourceArtifactKey),
            falkor.SelectGraph(tenantId).QueryAsync(graphQuery, graphParams)).ConfigureAwait(false);
        await db.KeyDeleteAsync(muKey).ConfigureAwait(false);
        if (inventory is not null)
        {
            await inventory.MarkDeletedAsync(tenantId, memoryUnitId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<RedisKey[]> FindSemanticKeysAsync(string tenantId, string memoryUnitId)
    {
        List<RedisKey> keys = [IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId)];
        foreach (EndPoint endpoint in redis.GetEndPoints())
        {
            IServer server = redis.GetServer(endpoint);
            if (!server.IsConnected)
            {
                throw new InvalidOperationException("source-publication-derived-store-unavailable");
            }

            await foreach (RedisKey key in server.KeysAsync(pattern: IndexSchemaDefinitions.BuildSemanticChunkKeyPattern(tenantId, memoryUnitId), pageSize: 100))
            {
                if (IndexSchemaDefinitions.TryParseSemanticChunkKey(tenantId, key, out string parsedId, out _)
                    && string.Equals(parsedId, memoryUnitId, StringComparison.Ordinal))
                {
                    if (keys.Count >= SourcePublicationVisibility.KeyCapacity) { throw new InvalidOperationException("source-publication-key-capacity"); }
                    keys.Add(key);
                }
            }
        }

        return [.. keys];
    }
}
