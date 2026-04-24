// <copyright file="CleanupSemanticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Dapr.Workflow;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Compensation activity that removes a memory unit from Redis Vector.</summary>
public sealed class CleanupSemanticActivity : WorkflowActivity<CleanupInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CleanupSemanticActivity> _logger;

    public CleanupSemanticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<CleanupSemanticActivity> logger)
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

        string hashKey = $"{input.TenantId}:vec:{input.MemoryUnitId}";

        // Story 9.2 Task 4.7: extend compensation to delete the NL semantic hash alongside the raw one.
        // Semantic cleanup is transactionally coupled for dual-embedding events — forking a second
        // activity would add dispatch complexity without isolating a failure mode. Idempotent: DEL is a
        // no-op on missing keys (the NL hash does not exist for SourceType != Event).
        string nlHashKey = $"{input.TenantId}:vec:nl:{input.MemoryUnitId}";

        IDatabase db = _redis.GetDatabase();
        bool deleted = await db.KeyDeleteAsync(hashKey).ConfigureAwait(false);
        bool nlDeleted = await db.KeyDeleteAsync(nlHashKey).ConfigureAwait(false);

        _logger.LogWarning(
            "Compensation: cleaned up Redis Vector keys {Key} (deleted={Deleted}) and {NlKey} (deleted={NlDeleted}) for {MemoryUnitId}",
            hashKey,
            deleted,
            nlHashKey,
            nlDeleted,
            input.MemoryUnitId);

        return deleted || nlDeleted;
    }
}
