// <copyright file="DeleteRediSearchActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Tenant deletion activity that drops a RediSearch index with the DD flag (deletes associated document hashes).</summary>
public sealed partial class DeleteRediSearchActivity : WorkflowActivity<TenantDeletionInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DeleteRediSearchActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteRediSearchActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteRediSearchActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<DeleteRediSearchActivity> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, TenantDeletionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = _redis.GetDatabase();
        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(input.TenantId);

        try
        {
            // DD flag deletes associated document hashes under the index prefix
            await db.ExecuteAsync("FT.DROPINDEX", indexName, "DD").ConfigureAwait(false);
            LogIndexDropped(_logger, indexName, input.TenantId);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
            LogIndexNotFound(_logger, indexName, input.TenantId);
        }

        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "RediSearch index '{IndexName}' dropped with DD for tenant '{TenantId}'")]
    private static partial void LogIndexDropped(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch index '{IndexName}' not found for tenant '{TenantId}' — nothing to drop")]
    private static partial void LogIndexNotFound(ILogger logger, string indexName, string tenantId);
}
