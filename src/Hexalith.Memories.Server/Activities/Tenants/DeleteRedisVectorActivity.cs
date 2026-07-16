// <copyright file="DeleteRedisVectorActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Tenant deletion activity that drops a Redis Vector index with the DD flag (deletes associated vector hashes).</summary>
public sealed partial class DeleteRedisVectorActivity : WorkflowActivity<TenantDeletionInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DeleteRedisVectorActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteRedisVectorActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteRedisVectorActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<DeleteRedisVectorActivity> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, TenantDeletionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = _redis.GetDatabase();
        string indexName = IndexSchemaDefinitions.GetSemanticIndexName(input.TenantId);

        try
        {
            // DD flag deletes associated vector hashes under the index prefix
            await db.ExecuteAsync("FT.DROPINDEX", indexName, "DD").ConfigureAwait(false);
            LogIndexDropped(_logger, indexName, input.TenantId);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
            LogIndexNotFound(_logger, indexName, input.TenantId);
        }

        // Story 9.2 Task 4.6: drop the sibling natural-language semantic index and all associated
        // {tenant}:vecnl:* hashes so tenant deletion leaves no orphan NL vectors behind.
        string nlIndexName = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(input.TenantId);

        try
        {
            await db.ExecuteAsync("FT.DROPINDEX", nlIndexName, "DD").ConfigureAwait(false);
            LogIndexDropped(_logger, nlIndexName, input.TenantId);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
            LogIndexNotFound(_logger, nlIndexName, input.TenantId);
        }

        foreach (string stagingIndexName in await ListTenantStagingIndexesAsync(db, input.TenantId).ConfigureAwait(false))
        {
            try
            {
                await db.ExecuteAsync("FT.DROPINDEX", stagingIndexName, "DD").ConfigureAwait(false);
                LogIndexDropped(_logger, stagingIndexName, input.TenantId);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
            {
                LogIndexNotFound(_logger, stagingIndexName, input.TenantId);
            }
        }

        return true;
    }

    private static async Task<IReadOnlyList<string>> ListTenantStagingIndexesAsync(IDatabase db, string tenantId)
    {
        RedisResult listResult = await db.ExecuteAsync("FT._LIST").ConfigureAwait(false);
        RedisResult[]? items;
        try
        {
            items = (RedisResult[]?)listResult;
        }
        catch (InvalidCastException)
        {
            return [];
        }

        if (items is null || items.Length == 0)
        {
            return [];
        }

        string rawPrefix = tenantId + IndexSchemaDefinitions.StagingSemanticIndexSuffixPrefix;
        string nlPrefix = tenantId + IndexSchemaDefinitions.StagingNaturalLanguageSemanticIndexSuffixPrefix;
        return items
            .Select(static item => item.ToString() ?? string.Empty)
            .Where(name => name.StartsWith(rawPrefix, StringComparison.Ordinal) || name.StartsWith(nlPrefix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Redis Vector index '{IndexName}' dropped with DD for tenant '{TenantId}'")]
    private static partial void LogIndexDropped(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis Vector index '{IndexName}' not found for tenant '{TenantId}' — nothing to drop")]
    private static partial void LogIndexNotFound(ILogger logger, string indexName, string tenantId);
}
