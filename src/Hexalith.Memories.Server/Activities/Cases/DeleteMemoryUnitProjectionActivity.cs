// <copyright file="DeleteMemoryUnitProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using System.Net;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.DerivedStores;

using StackExchange.Redis;

/// <summary>Deletes memory-unit read-model projections idempotently.</summary>
internal sealed class DeleteMemoryUnitProjectionActivity(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder) : WorkflowActivity<MemoryUnitDeletionProjectionInput, bool>
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
        string muKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);
        RedisKey[] semanticKeys = await FindSemanticKeysAsync(tenantId, memoryUnitId).ConfigureAwait(false);
        string nlVecKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId);
        string sourceArtifactKey = RedisDerivedStoreService.BuildSourceArtifactKey(tenantId, memoryUnitId);
        (string graphQuery, IDictionary<string, object> graphParams) = graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);

        await Task.WhenAll(
            db.KeyDeleteAsync(semanticKeys),
            db.KeyDeleteAsync(nlVecKey),
            db.KeyDeleteAsync(sourceArtifactKey),
            falkor.SelectGraph(tenantId).QueryAsync(graphQuery, graphParams)).ConfigureAwait(false);
        await db.KeyDeleteAsync(muKey).ConfigureAwait(false);
    }

    private async Task<RedisKey[]> FindSemanticKeysAsync(string tenantId, string memoryUnitId)
    {
        List<RedisKey> keys = [IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId)];
        foreach (EndPoint endpoint in redis.GetEndPoints())
        {
            IServer server = redis.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            await foreach (RedisKey key in server.KeysAsync(pattern: IndexSchemaDefinitions.BuildSemanticChunkKeyPattern(tenantId, memoryUnitId), pageSize: 100))
            {
                if (IndexSchemaDefinitions.TryParseSemanticChunkKey(tenantId, key, out string parsedId, out _)
                    && string.Equals(parsedId, memoryUnitId, StringComparison.Ordinal))
                {
                    keys.Add(key);
                }
            }
        }

        return [.. keys];
    }
}
