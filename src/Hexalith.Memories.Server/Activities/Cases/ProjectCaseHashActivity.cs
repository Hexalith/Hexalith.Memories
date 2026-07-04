// <copyright file="ProjectCaseHashActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using StackExchange.Redis;

/// <summary>Projects a case aggregate event into the Redis case hash read model.</summary>
internal sealed class ProjectCaseHashActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    : WorkflowActivity<ProjectCaseCreatedInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, ProjectCaseCreatedInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = redis.GetDatabase();
        string redisKey = $"{input.TenantId}:case:{input.CaseId}";
        await db.HashSetAsync(
            redisKey,
            [
                new HashEntry("id", input.CaseId),
                new HashEntry("tenantId", input.TenantId),
                new HashEntry("name", input.Name),
                new HashEntry("description", input.Description ?? string.Empty),
                new HashEntry("status", "active"),
                new HashEntry("createdAt", input.CreatedAt.ToString("o")),
                new HashEntry("lastUpdated", input.CreatedAt.ToString("o")),
            ]).ConfigureAwait(false);

        return true;
    }
}
