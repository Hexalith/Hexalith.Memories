// <copyright file="DeleteMemoryUnitProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

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
        string vecKey = IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId);
        string nlVecKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId);
        (string graphQuery, IDictionary<string, object> graphParams) = graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);

        await Task.WhenAll(
            db.KeyDeleteAsync(vecKey),
            db.KeyDeleteAsync(nlVecKey),
            falkor.QueryAsync(tenantId, graphQuery, graphParams)).ConfigureAwait(false);
        await db.KeyDeleteAsync(muKey).ConfigureAwait(false);
    }
}
