// <copyright file="CleanupCaseProjectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;

using StackExchange.Redis;

/// <summary>Compensates case projections across Redis and FalkorDB.</summary>
internal sealed class CleanupCaseProjectionActivity(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder) : WorkflowActivity<CaseProjectionCleanupInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, CaseProjectionCleanupInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = redis.GetDatabase();
        NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = graphQueryBuilder.BuildDeleteCaseNode(input.CaseId);
        await Task.WhenAll(
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}:members"),
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}:activity"),
            db.KeyDeleteAsync($"{input.TenantId}:case:{input.CaseId}"),
            falkor.SelectGraph(input.TenantId).QueryAsync(query, parameters)).ConfigureAwait(false);
        return true;
    }
}
