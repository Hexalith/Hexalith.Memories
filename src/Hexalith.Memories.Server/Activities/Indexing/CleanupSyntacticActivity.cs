// <copyright file="CleanupSyntacticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Compensation activity that removes a memory unit from RediSearch.</summary>
public sealed class CleanupSyntacticActivity : WorkflowActivity<CleanupInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CleanupSyntacticActivity> _logger;

    public CleanupSyntacticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<CleanupSyntacticActivity> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        CleanupInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        string hashKey = IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId);
        IDatabase db = _redis.GetDatabase();
        bool deleted = await db.KeyDeleteAsync(hashKey).ConfigureAwait(false);

        _logger.LogWarning(
            "Compensation: cleaned up RediSearch key {Key} for {MemoryUnitId} (deleted={Deleted})",
            hashKey,
            input.MemoryUnitId,
            deleted);

        return deleted;
    }
}
