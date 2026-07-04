// <copyright file="MarkCaseDeletingActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using StackExchange.Redis;

/// <summary>Marks a case projection as deleting for observability and ingestion guards.</summary>
internal sealed class MarkCaseDeletingActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    : WorkflowActivity<CaseProjectionCleanupInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, CaseProjectionCleanupInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = redis.GetDatabase();
        await db.HashSetAsync(
            $"{input.TenantId}:case:{input.CaseId}",
            [
                new HashEntry("status", "deleting"),
                new HashEntry("deletionStartedAt", DateTimeOffset.UtcNow.ToString("o")),
            ]).ConfigureAwait(false);
        return true;
    }
}
