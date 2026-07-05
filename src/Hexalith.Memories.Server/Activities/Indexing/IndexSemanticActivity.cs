// <copyright file="IndexSemanticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Server.Activities;

using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit embedding in Redis Vector Search.</summary>
public sealed class IndexSemanticActivity : WorkflowTraceLinkedActivity<IndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexSemanticActivity> _logger;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;

    public IndexSemanticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexSemanticActivity> logger,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        _redis = redis;
        _logger = logger;
        _readinessVerifier = readinessVerifier
            ?? new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
    }

    /// <inheritdoc/>
    protected override async Task<IndexResult> RunActivityAsync(
        WorkflowActivityContext context,
        IndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        ArgumentNullException.ThrowIfNull(input.EmbeddingVector);
        if (input.EmbeddingVector.Length == 0)
        {
            throw new ArgumentException("EmbeddingVector must not be empty.", nameof(input));
        }

        byte[] vectorBytes = MemoryMarshal.AsBytes(input.EmbeddingVector.AsSpan()).ToArray();
        if (vectorBytes.Length != input.EmbeddingDimensions * sizeof(float))
        {
            throw new InvalidOperationException(
                $"Vector byte length {vectorBytes.Length} does not match expected {input.EmbeddingDimensions * sizeof(float)} bytes for {input.EmbeddingDimensions} dimensions");
        }

        IDatabase db = _redis.GetDatabase();

        // Migration marker safety is per-write and independent of index-readiness memoization (AC8): it must run on
        // every invocation before any hash write, never skipped by the readiness cache.
        EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader
            .ReadActiveMarkerAsync(db, input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);
        EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker(
            marker,
            input.EmbeddingProvider,
            input.EmbeddingModel,
            input.EmbeddingDimensions);

        string? cloudEventSubject = TryGetMetadataValue(input.Metadata, "cloudevent.subject");
        string hashKey = IndexSchemaDefinitions.BuildSemanticKey(input.TenantId, input.MemoryUnitId);

        // Story 23.7 (A34): verify the tenant's raw semantic index once per process (existence + prefix + fields +
        // vector dimensions) instead of issuing FT.CREATE per document. TenantProvisioningWorkflow owns creation.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Semantic, input.EmbeddingDimensions, CancellationToken.None)
            .ConfigureAwait(false);

        List<HashEntry> hashEntries =
        [
            new HashEntry("embedding", vectorBytes),
            new HashEntry("memoryUnitId", input.MemoryUnitId),
            new HashEntry("caseId", input.CaseId),
            new HashEntry("embeddingProvider", input.EmbeddingProvider),
            new HashEntry("embeddingModel", input.EmbeddingModel),
            new HashEntry("embeddingDimensions", input.EmbeddingDimensions),
        ];

        if (!string.IsNullOrWhiteSpace(cloudEventSubject))
        {
            hashEntries.Add(new HashEntry("cloudeventSubject", cloudEventSubject));
        }

        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in Redis Vector for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("semantic", input.MemoryUnitId, input.TenantId);
    }

    private static string? TryGetMetadataValue(IReadOnlyDictionary<string, MetadataField> metadata, string key)
        => metadata.TryGetValue(key, out MetadataField? field) && !string.IsNullOrWhiteSpace(field.Value)
            ? field.Value
            : null;
}
