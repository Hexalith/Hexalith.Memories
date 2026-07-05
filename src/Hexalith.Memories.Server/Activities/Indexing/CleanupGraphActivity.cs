// <copyright file="CleanupGraphActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Server.Activities;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Compensation activity that removes a memory unit node from FalkorDB.</summary>
public sealed class CleanupGraphActivity : WorkflowTraceLinkedActivity<CleanupInput, bool>
{
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<CleanupGraphActivity> _logger;

    public CleanupGraphActivity(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<CleanupGraphActivity> logger)
    {
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<bool> RunActivityAsync(
        WorkflowActivityContext context,
        CleanupInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildDeleteMemoryUnitNode(input.MemoryUnitId);

        await falkor.QueryAsync(input.TenantId, query, parameters).ConfigureAwait(false);

        _logger.LogWarning(
            "Compensation: cleaned up FalkorDB node for {MemoryUnitId} in tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return true;
    }
}
