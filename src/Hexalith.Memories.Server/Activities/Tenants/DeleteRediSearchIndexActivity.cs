// <copyright file="DeleteRediSearchIndexActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;

using StackExchange.Redis;

/// <summary>Compensation activity that drops a RediSearch index for a tenant.</summary>
public sealed partial class DeleteRediSearchIndexActivity : WorkflowActivity<TenantProvisioningInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DeleteRediSearchIndexActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteRediSearchIndexActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteRediSearchIndexActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<DeleteRediSearchIndexActivity> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task<bool> RunAsync(WorkflowActivityContext context, TenantProvisioningInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IDatabase db = _redis.GetDatabase();
        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(input.TenantId);

        try
        {
            db.FT().DropIndex(indexName);
            LogIndexDropped(_logger, indexName, input.TenantId);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
            LogIndexNotFound(_logger, indexName, input.TenantId);
        }

        return Task.FromResult(true);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "RediSearch index '{IndexName}' dropped for tenant '{TenantId}'")]
    private static partial void LogIndexDropped(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch index '{IndexName}' not found for tenant '{TenantId}' — nothing to drop")]
    private static partial void LogIndexNotFound(ILogger logger, string indexName, string tenantId);
}
