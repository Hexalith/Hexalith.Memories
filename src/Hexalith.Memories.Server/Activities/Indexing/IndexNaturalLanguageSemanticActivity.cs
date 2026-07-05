// <copyright file="IndexNaturalLanguageSemanticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Server.Activities;

using System.Globalization;
using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

/// <summary>Story 9.2 Task 4.3 — DAPR Workflow activity that indexes the natural-language (LLM-authored)
/// embedding for a memory unit in Redis Vector Search under the sibling <c>:memories:vec:nl</c> index.
/// Structural clone of <see cref="IndexSemanticActivity"/> with two line-level differences: index name
/// and hash-key prefix. Keeping the two activities separate preserves per-activity telemetry, retry, and
/// cleanup granularity.</summary>
public sealed class IndexNaturalLanguageSemanticActivity : WorkflowTraceLinkedActivity<NaturalLanguageIndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexNaturalLanguageSemanticActivity> _logger;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;

    public IndexNaturalLanguageSemanticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexNaturalLanguageSemanticActivity> logger,
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
        NaturalLanguageIndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        ArgumentNullException.ThrowIfNull(input.EmbeddingVector);
        if (input.EmbeddingVector.Length == 0)
        {
            throw new ArgumentException("EmbeddingVector must not be empty.", nameof(input));
        }

        // Review P5: the NL description is the business-meaning signal — indexing an empty or null
        // value writes a degenerate hash whose `naturalLanguageDescription` field is absent/empty,
        // and a subsequent KNN search may return false-positive matches for zero-vector-adjacent
        // neighbours. Fail fast so the retry workflow / compensation path can react.
        ArgumentException.ThrowIfNullOrWhiteSpace(input.NaturalLanguageDescription);

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

        string hashKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(input.TenantId, input.MemoryUnitId);

        // Story 23.7 (A34): verify the tenant's natural-language semantic index once per process instead of issuing
        // FT.CREATE per document. TenantProvisioningWorkflow owns creation of the sibling NL index.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.NaturalLanguageSemantic, input.EmbeddingDimensions, CancellationToken.None)
            .ConfigureAwait(false);

        string confidenceValue = input.DescriptionConfidence?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty;
        string confidenceSourceValue = input.ConfidenceSource.ToString().ToLowerInvariant();

        List<HashEntry> hashEntries =
        [
            new HashEntry("embedding", vectorBytes),
            new HashEntry("memoryUnitId", input.MemoryUnitId),
            new HashEntry("caseId", input.CaseId),
            new HashEntry("naturalLanguageDescription", input.NaturalLanguageDescription),
            new HashEntry("descriptionOrigin", "ai"),
            new HashEntry("descriptionConfidence", confidenceValue),
            new HashEntry("descriptionConfidenceSource", confidenceSourceValue),
            new HashEntry("embeddingProvider", input.EmbeddingProvider),
            new HashEntry("embeddingModel", input.EmbeddingModel),
            new HashEntry("embeddingDimensions", input.EmbeddingDimensions),
        ];

        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in NL semantic index for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("semantic-nl", input.MemoryUnitId, input.TenantId);
    }
}
