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
        List<string> problems = [];

        IReadOnlyList<string> prefixes = IndexSchemaDefinitions.GetIndexPrefixes(info);
        string expectedPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId);
        if (prefixes.Count != 1 || !string.Equals(prefixes[0], expectedPrefix, StringComparison.Ordinal))
        {
            problems.Add($"expected prefix '{expectedPrefix}' but found [{string.Join(", ", prefixes)}]");
        }

        HashSet<string> actualFields = new(IndexSchemaDefinitions.GetAttributeIdentifiers(info), StringComparer.OrdinalIgnoreCase);
        HashSet<string> expectedFields = new(IndexSchemaDefinitions.GetSemanticFieldIdentifiers(), StringComparer.OrdinalIgnoreCase);
        if (!actualFields.SetEquals(expectedFields))
        {
            problems.Add($"expected fields [{string.Join(", ", expectedFields.OrderBy(v => v))}] but found [{string.Join(", ", actualFields.OrderBy(v => v))}]");
        }

        if (!IndexSchemaDefinitions.TryGetVectorDimensions(info, "embedding", out int actualDimensions))
        {
            problems.Add("embedding vector dimensions are missing from FT.INFO");
        }
        else if (actualDimensions != expectedDimensions)
        {
            problems.Add($"expected {expectedDimensions} dimensions but found {actualDimensions}");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Existing Redis Vector index '{indexName}' does not match the expected tenant schema: {string.Join("; ", problems)}.");
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
