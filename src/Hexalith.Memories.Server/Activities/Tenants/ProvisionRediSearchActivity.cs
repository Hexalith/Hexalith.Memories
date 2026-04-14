// <copyright file="ProvisionRediSearchActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using System.Diagnostics;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that creates a RediSearch index for a tenant.</summary>
public sealed partial class ProvisionRediSearchActivity : WorkflowActivity<TenantProvisioningInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ProvisionRediSearchActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProvisionRediSearchActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public ProvisionRediSearchActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<ProvisionRediSearchActivity> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task<bool> RunAsync(WorkflowActivityContext context, TenantProvisioningInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        long startTimestamp = Stopwatch.GetTimestamp();
        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();

        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(input.TenantId);

        try
        {
            ft.Create(
                indexName,
                IndexSchemaDefinitions.CreateSyntacticParams(input.TenantId),
                IndexSchemaDefinitions.CreateSyntacticSchema());
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            EnsureSyntacticSchemaMatches(db, indexName, input.TenantId);
            LogIndexAlreadyExists(_logger, indexName, input.TenantId);
        }

        LogIndexCreated(_logger, indexName, input.TenantId);
        LogActivityAudit(
            _logger,
            input.TenantId,
            nameof(ProvisionRediSearchActivity),
            "success",
            Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
            DateTimeOffset.UtcNow);
        return Task.FromResult(true);
    }

    private static void EnsureSyntacticSchemaMatches(IDatabase db, string indexName, string tenantId)
    {
        RedisResult info = db.Execute("FT.INFO", indexName);
        List<string> problems = [];

        IReadOnlyList<string> prefixes = IndexSchemaDefinitions.GetIndexPrefixes(info);
        string expectedPrefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId);
        if (prefixes.Count != 1 || !string.Equals(prefixes[0], expectedPrefix, StringComparison.Ordinal))
        {
            problems.Add($"expected prefix '{expectedPrefix}' but found [{string.Join(", ", prefixes)}]");
        }

        HashSet<string> actualFields = new(IndexSchemaDefinitions.GetAttributeIdentifiers(info), StringComparer.OrdinalIgnoreCase);
        HashSet<string> expectedFields = new(IndexSchemaDefinitions.GetSyntacticFieldIdentifiers(), StringComparer.OrdinalIgnoreCase);
        if (!actualFields.SetEquals(expectedFields))
        {
            problems.Add($"expected fields [{string.Join(", ", expectedFields.OrderBy(v => v))}] but found [{string.Join(", ", actualFields.OrderBy(v => v))}]");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Existing RediSearch index '{indexName}' does not match the expected tenant schema: {string.Join("; ", problems)}.");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "RediSearch index '{IndexName}' created for tenant '{TenantId}'")]
    private static partial void LogIndexCreated(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch index '{IndexName}' already exists for tenant '{TenantId}' — returning success (idempotent)")]
    private static partial void LogIndexAlreadyExists(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant provisioning activity {ActivityName} completed for tenant '{TenantId}' with result {Result} in {DurationMs} ms at {Timestamp:O}")]
    private static partial void LogActivityAudit(
        ILogger logger,
        string tenantId,
        string activityName,
        string result,
        double durationMs,
        DateTimeOffset timestamp);
}
