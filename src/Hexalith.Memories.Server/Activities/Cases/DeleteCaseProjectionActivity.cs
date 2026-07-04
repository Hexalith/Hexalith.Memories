// <copyright file="DeleteCaseProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;

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
            string muKey = $"{input.TenantId}:mu:{memoryUnitId}";
            string vecKey = $"{input.TenantId}:vec:{memoryUnitId}";
            string nlVecKey = $"{input.TenantId}:vec:nl:{memoryUnitId}";
            (string graphQuery, IDictionary<string, object> graphParams) = graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);
            await Task.WhenAll(
                db.KeyDeleteAsync(muKey),
                db.KeyDeleteAsync(vecKey),
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
}
