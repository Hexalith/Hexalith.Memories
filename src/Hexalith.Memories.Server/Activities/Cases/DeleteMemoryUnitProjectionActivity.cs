// <copyright file="DeleteMemoryUnitProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Hexalith.Memories.Server.NaturalLanguage;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.DerivedStores;

using Microsoft.Extensions.Options;
using StackExchange.Redis;

/// <summary>Deletes memory-unit read-model projections idempotently.</summary>
internal sealed class DeleteMemoryUnitProjectionActivity(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder,
    SourceInventoryStore? inventory = null, IConfiguration? configuration = null, IHostEnvironment? environment = null,
    IWorkflowPayloadStore? payloadStore = null, IDaprWorkflowClient? workflowClient = null,
    IOptions<NaturalLanguageDescriptionOptions>? retryOptions = null) : WorkflowActivity<MemoryUnitDeletionProjectionInput, bool>
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

    internal async Task DeleteOneAsync(IDatabase db, NFalkorDB.FalkorDB falkor, string tenantId, string memoryUnitId)
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
        await FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenantId, memoryUnitId, CancellationToken.None).ConfigureAwait(false);
        await RetryWorkflowErasure.PurgeAsync(workflowClient, tenantId, memoryUnitId, CancellationToken.None,
            retryOptions?.Value.LegacyRetrySchedulersDrainedForErasure == true).ConfigureAwait(false);
        string muKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);
        RedisKey[] semanticKeys = await SemanticKeyDiscovery.FindAsync(db, tenantId, memoryUnitId, CancellationToken.None).ConfigureAwait(false);
        string nlVecKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId);
        string sourceArtifactKey = RedisDerivedStoreService.BuildSourceArtifactKey(tenantId, memoryUnitId);
        (string graphQuery, IDictionary<string, object> graphParams) = graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);

        if (environment?.IsProduction() == false && configuration?.GetValue("Memories:Testing:EnableSourceInventoryQualification", false) == true
            && configuration.GetValue("Memories:Testing:UseFakeEmbedding", false))
        {
            int delay = Math.Clamp(configuration.GetValue("Memories:Testing:SourceInventoryDeletionDelayMilliseconds", 0), 0, 5000);
            if (delay > 0)
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
        await Task.WhenAll(
            db.KeyDeleteAsync(semanticKeys),
            db.KeyDeleteAsync(new RedisKey[] { nlVecKey, IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenantId, memoryUnitId) }),
            db.KeyDeleteAsync(sourceArtifactKey),
            falkor.SelectGraph(tenantId).QueryAsync(graphQuery, graphParams)).ConfigureAwait(false);
        await db.KeyDeleteAsync(muKey).ConfigureAwait(false);
        if (inventory is not null)
        {
            await inventory.MarkDeletedAsync(tenantId, memoryUnitId, CancellationToken.None).ConfigureAwait(false);
        }
    }

}
