// <copyright file="DeleteCaseProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Options;
using Hexalith.Memories.Server.NaturalLanguage;
using StackExchange.Redis;

/// <summary>Deletes case and memory-unit projections idempotently.</summary>
internal sealed class DeleteCaseProjectionActivity(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder, SourceInventoryStore? inventory = null, IWorkflowPayloadStore? payloadStore = null, IDaprWorkflowClient? workflowClient = null,
    IOptions<NaturalLanguageDescriptionOptions>? retryOptions = null) : WorkflowActivity<CaseDeletionProjectionInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, CaseDeletionProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = redis.GetDatabase();
        NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());

        foreach (string memoryUnitId in input.MemoryUnitIds)
        {
            await new DeleteMemoryUnitProjectionActivity(redis, falkorDb, graphQueryBuilder, inventory,
                payloadStore: payloadStore, workflowClient: workflowClient, retryOptions: retryOptions)
                .DeleteOneAsync(db, falkor, input.TenantId, memoryUnitId).ConfigureAwait(false);
        }

        (string caseDelQuery, IDictionary<string, object> caseDelParams) = graphQueryBuilder.BuildDeleteCaseNode(input.CaseId);
        await falkor.SelectGraph(input.TenantId).QueryAsync(caseDelQuery, caseDelParams).ConfigureAwait(false);
        await Task.WhenAll(
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}:members"),
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}:activity"),
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}")).ConfigureAwait(false);
        return true;
    }

}
