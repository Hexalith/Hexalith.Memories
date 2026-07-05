// <copyright file="DeleteCaseProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using System.Net;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using StackExchange.Redis;

/// <summary>Deletes case and memory-unit projections idempotently.</summary>
internal sealed class DeleteCaseProjectionActivity(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder) : WorkflowActivity<CaseDeletionProjectionInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, CaseDeletionProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = redis.GetDatabase();
        NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());

        foreach (string memoryUnitId in input.MemoryUnitIds)
        {
            string muKey = IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, memoryUnitId);
            RedisKey[] semanticKeys = await FindSemanticKeysAsync(input.TenantId, memoryUnitId).ConfigureAwait(false);
            string nlVecKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(input.TenantId, memoryUnitId);
            (string graphQuery, IDictionary<string, object> graphParams) = graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);
            await Task.WhenAll(
                db.KeyDeleteAsync(muKey),
                db.KeyDeleteAsync(semanticKeys),
                db.KeyDeleteAsync(nlVecKey)).ConfigureAwait(false);
            await falkor.QueryAsync(input.TenantId, graphQuery, graphParams).ConfigureAwait(false);
        }

        (string caseDelQuery, IDictionary<string, object> caseDelParams) = graphQueryBuilder.BuildDeleteCaseNode(input.CaseId);
        await falkor.QueryAsync(input.TenantId, caseDelQuery, caseDelParams).ConfigureAwait(false);
        await Task.WhenAll(
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}:members"),
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}:activity"),
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}")).ConfigureAwait(false);
        return true;
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
