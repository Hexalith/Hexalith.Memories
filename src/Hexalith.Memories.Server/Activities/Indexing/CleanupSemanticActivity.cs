// <copyright file="CleanupSemanticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Compensation activity that removes a memory unit from Redis Vector.</summary>
/// <remarks>Story 9.2 Task 4.7 extended this to delete the natural-language hash alongside the raw one.
/// <para><b>Return-value contract (note the semantic shift from pre-9.2):</b> returns <see langword="true"/>
/// when <b>either</b> the raw hash or the NL hash was deleted. Returns <see langword="false"/> only when
/// <b>neither</b> hash existed (a no-op cleanup, e.g., compensation for an event that failed before any
/// indexing took place). Callers that branched on the pre-9.2 "raw hash deleted" semantic must be audited
/// — consider reading the per-hash flags from the accompanying log instead.</para></remarks>
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

        string hashKey = IndexSchemaDefinitions.GetSemanticKeyPrefix(input.TenantId) + input.MemoryUnitId;

        // Story 9.2 Task 4.7: extend compensation to delete the NL semantic hash alongside the raw one.
        // Semantic cleanup is transactionally coupled for dual-embedding events — forking a second
        // activity would add dispatch complexity without isolating a failure mode. Idempotent: DEL is a
        // no-op on missing keys (the NL hash does not exist for SourceType != Event).
        string nlHashKey = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(input.TenantId) + input.MemoryUnitId;

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
