// <copyright file="PersistFailedUnitActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

using System.Globalization;
using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that durably persists a failed memory unit to Redis (Story 6.3 NFR19/FR11).
/// Hash <c>{tenantId}:failed-unit:{memoryUnitId}</c> + sorted-set <c>{tenantId}:case:{caseId}:failed-units</c>
/// scored by <c>FailedAt</c> unix-ms — written atomically via Lua.</summary>
internal sealed class PersistFailedUnitActivity : WorkflowTraceLinkedActivity<FailedUnitInput, bool>
{
    /// <summary>Redis hash field names. Centralized to keep the registry reader and writer in lockstep.</summary>
    internal const string FieldTenantId = "tenantId";
    internal const string FieldCaseId = "caseId";
    internal const string FieldSourceUri = "sourceUri";
    internal const string FieldSourceType = "sourceType";
    internal const string FieldIngestedBy = "ingestedBy";
    internal const string FieldContentType = "contentType";
    internal const string FieldStage = "stage";
    internal const string FieldErrorCode = "errorCode";
    internal const string FieldErrorMessage = "errorMessage";
    internal const string FieldRetryCount = "retryCount";
    internal const string FieldLastRetryAt = "lastRetryAt";
    internal const string FieldFailedAt = "failedAt";
    internal const string FieldFailureDetailsJson = "failureDetailsJson";
    internal const string FieldSourcePayloadReferenceJson = "sourcePayloadReferenceJson";
    internal const string FieldMetadataJson = "metadataJson";
    internal const string FieldCausationId = "causationId";
    internal const string FieldCorrelationId = "correlationId";

    /// <summary>Lua: HSET hash + ZADD sorted-set as one atomic round-trip.
    /// KEYS[1]=hash, KEYS[2]=sorted-set; ARGV[1..N-2]=field/value pairs; ARGV[N-1]=score; ARGV[N]=member.</summary>
    internal const string PersistScript = """
        redis.call('HSET', KEYS[1], unpack(ARGV, 1, #ARGV - 2))
        redis.call('ZADD', KEYS[2], ARGV[#ARGV - 1], ARGV[#ARGV])
        return 1
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<PersistFailedUnitActivity> _logger;

    public PersistFailedUnitActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<PersistFailedUnitActivity> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<bool> RunActivityAsync(WorkflowActivityContext context, FailedUnitInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        IDatabase db = _redis.GetDatabase();
        string hashKey = BuildHashKey(input.TenantId, input.MemoryUnitId);
        string zKey = BuildSortedSetKey(input.TenantId, input.CaseId);
        long failedAtMs = input.FailedAt.ToUnixTimeMilliseconds();

        FailureDetails details = new(
            input.Stage,
            input.ErrorCode,
            input.RetryCount,
            input.ErrorMessage,
            input.LastRetryAt);
        string detailsJson = JsonSerializer.Serialize(details, MemoriesJsonContext.Options);

        RedisValue[] argv =
        [
            FieldTenantId, input.TenantId,
            FieldCaseId, input.CaseId,
            FieldSourceUri, input.SourceUri,
            FieldSourceType, input.SourceType.ToString(),
            FieldIngestedBy, input.IngestedBy,
            FieldContentType, input.ContentType ?? string.Empty,
            FieldStage, input.Stage,
            FieldErrorCode, input.ErrorCode,
            FieldErrorMessage, input.ErrorMessage ?? string.Empty,
            FieldRetryCount, input.RetryCount.ToString(CultureInfo.InvariantCulture),
            FieldLastRetryAt, input.LastRetryAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            FieldFailedAt, input.FailedAt.ToString("O", CultureInfo.InvariantCulture),
            FieldFailureDetailsJson, detailsJson,
            FieldSourcePayloadReferenceJson, input.SourcePayloadReference is null
                ? string.Empty
                : JsonSerializer.Serialize(input.SourcePayloadReference, MemoriesJsonContext.Options),
            FieldMetadataJson, input.Metadata is { Count: > 0 }
                ? JsonSerializer.Serialize(new Dictionary<string, MetadataField>(input.Metadata, StringComparer.Ordinal), MemoriesJsonContext.Options)
                : string.Empty,
            FieldCausationId, input.CausationId ?? string.Empty,
            FieldCorrelationId, input.CorrelationId ?? string.Empty,
            failedAtMs.ToString(CultureInfo.InvariantCulture),
            input.MemoryUnitId,
        ];

        await db.ScriptEvaluateAsync(
            PersistScript,
            [hashKey, zKey],
            argv).ConfigureAwait(false);

        RetryFailureLog.LogFailedUnitPersisted(
            _logger, input.TenantId, input.MemoryUnitId, input.Stage, input.ErrorCode);
        return true;
    }

    /// <summary>Builds the Redis hash key for a failed unit.</summary>
    public static string BuildHashKey(string tenantId, string memoryUnitId)
        => $"{tenantId}:failed-unit:{memoryUnitId}";

    /// <summary>Builds the Redis sorted-set key for a case's failed units.</summary>
    public static string BuildSortedSetKey(string tenantId, string caseId)
        => $"{tenantId}:case:{caseId}:failed-units";
}
