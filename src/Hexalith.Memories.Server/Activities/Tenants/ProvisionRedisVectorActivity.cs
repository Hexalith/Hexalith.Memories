// <copyright file="ProvisionRedisVectorActivity.cs" company="ITANEO">
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

/// <summary>DAPR Workflow activity that creates a Redis Vector index for a tenant.</summary>
public sealed partial class ProvisionRedisVectorActivity : WorkflowActivity<TenantProvisioningInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ProvisionRedisVectorActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProvisionRedisVectorActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public ProvisionRedisVectorActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<ProvisionRedisVectorActivity> logger)
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

        string indexName = IndexSchemaDefinitions.GetSemanticIndexName(input.TenantId);

        try
        {
            ft.Create(
                indexName,
                IndexSchemaDefinitions.CreateSemanticParams(input.TenantId),
                IndexSchemaDefinitions.CreateSemanticSchema(input.VectorDimensions));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            EnsureSemanticSchemaMatches(db, indexName, input.TenantId, input.VectorDimensions);
            LogIndexAlreadyExists(_logger, indexName, input.TenantId);
        }

        LogIndexCreated(_logger, indexName, input.TenantId, input.VectorDimensions);
        EnsureAlias(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(input.TenantId), indexName);

        // Story 9.2 Task 4.5: create the sibling natural-language semantic index. Same HNSW/FLOAT32/COSINE
        // schema shape at the same dimensions (Risk #5 — schema drift prevented by shared core helper).
        // Idempotent on "Index already exists" matching the raw-index pattern above.
        string nlIndexName = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(input.TenantId);

        try
        {
            ft.Create(
                nlIndexName,
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(input.TenantId),
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(input.VectorDimensions));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            EnsureNaturalLanguageSemanticSchemaMatches(db, nlIndexName, input.TenantId, input.VectorDimensions);
            LogIndexAlreadyExists(_logger, nlIndexName, input.TenantId);
        }

        LogIndexCreated(_logger, nlIndexName, input.TenantId, input.VectorDimensions);
        EnsureAlias(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(input.TenantId), nlIndexName);

        LogActivityAudit(
            _logger,
            input.TenantId,
            nameof(ProvisionRedisVectorActivity),
            "success",
            Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
            DateTimeOffset.UtcNow);
        return Task.FromResult(true);
    }

    private static void EnsureSemanticSchemaMatches(IDatabase db, string indexName, string tenantId, int expectedDimensions)
    {
        RedisResult info = db.Execute("FT.INFO", indexName);
        IReadOnlyList<string> problems = IndexSchemaDefinitions.DescribeVectorSchemaProblems(
            info,
            IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId),
            IndexSchemaDefinitions.GetSemanticFieldIdentifiers(),
            expectedDimensions);

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Existing Redis Vector index '{indexName}' does not match the expected tenant schema: {string.Join("; ", problems)}.");
        }
    }

    private static void EnsureNaturalLanguageSemanticSchemaMatches(IDatabase db, string indexName, string tenantId, int expectedDimensions)
    {
        RedisResult info = db.Execute("FT.INFO", indexName);
        IReadOnlyList<string> problems = IndexSchemaDefinitions.DescribeVectorSchemaProblems(
            info,
            IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers(),
            expectedDimensions);

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Existing Redis Vector index '{indexName}' does not match the expected tenant schema: {string.Join("; ", problems)}.");
        }
    }

    private static void EnsureAlias(IDatabase db, string aliasName, string indexName)
    {
        try
        {
            _ = db.Execute("FT.ALIASADD", aliasName, indexName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("alias already exists", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Redis Vector index '{IndexName}' created for tenant '{TenantId}' with {Dimensions} dimensions")]
    private static partial void LogIndexCreated(ILogger logger, string indexName, string tenantId, int dimensions);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis Vector index '{IndexName}' already exists for tenant '{TenantId}' — returning success (idempotent)")]
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
