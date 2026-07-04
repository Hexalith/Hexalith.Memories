// <copyright file="CheckMemoryUnitExistsActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Server.Infrastructure;

using StackExchange.Redis;

/// <summary>Workflow activity that checks whether the authoritative syntactic memory-unit hash
/// <c>{tenant}:mu:{memoryUnitId}</c> still exists. The NL retry workflow uses this guard to avoid
/// recreating orphan NL semantic hashes after delete/rollback.</summary>
public sealed class CheckMemoryUnitExistsActivity : WorkflowActivity<ConsistencyInput, bool>
{
    private readonly IConnectionMultiplexer _redis;

    public CheckMemoryUnitExistsActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        ArgumentNullException.ThrowIfNull(redis);
        _redis = redis;
    }

    /// <inheritdoc/>
    public override Task<bool> RunAsync(WorkflowActivityContext context, ConsistencyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);

        IDatabase db = _redis.GetDatabase();
        return db.KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId));
    }
}
