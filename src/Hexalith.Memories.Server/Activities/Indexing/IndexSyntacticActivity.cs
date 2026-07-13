// <copyright file="IndexSyntacticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Server.Activities;

using System.Globalization;

using Dapr.Workflow;

using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit in RediSearch for full-text search.</summary>
public sealed class IndexSyntacticActivity : WorkflowTraceLinkedActivity<IndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexSyntacticActivity> _logger;
    private readonly IWorkflowPayloadStore? _payloadStore;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;

    public IndexSyntacticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexSyntacticActivity> logger,
        IWorkflowPayloadStore? payloadStore = null,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        _redis = redis;
        _logger = logger;
        _payloadStore = payloadStore;
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
        string content = await ResolveContentAsync(input).ConfigureAwait(false);

        IDatabase db = _redis.GetDatabase();

        string hashKey = IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId);

        // Story 23.7 (A34): TenantProvisioningWorkflow owns index creation. Ingestion only verifies the tenant's
        // syntactic index exists and matches the expected schema, memoized once per tenant/index family/process —
        // no per-document FT.CREATE, no "index already exists" warning, no blocking Thread.Sleep retry.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Syntactic, null, CancellationToken.None)
            .ConfigureAwait(false);

        // Story 26.2: the syntactic hash field contract is factored into SyntacticHashProjection so ingest and
        // restore write byte-identical hashes. Ingest stamps ingestedAt into both ingestedAt/lastUpdated.
        List<HashEntry> hashEntries = SyntacticHashProjection.BuildEntries(
            input.MemoryUnitId,
            input.TenantId,
            content,
            input.SourceUri,
            input.SourceType,
            input.Metadata,
            input.ContentHash,
            input.CaseId,
            input.EmbeddingProvider,
            input.EmbeddingModel,
            input.IngestedBy,
            input.IngestedAt,
            input.IngestedAt);

        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        // Story 5.5 AC1 / Amendment A + L + T: stamp last-activity AFTER the hash write succeeds
        // (ordering L: never advertise activity that never happened). Fire-and-forget because a
        // stale timestamp is acceptable; a failed ingest is not. Deploy-doc TODO: the
        // {tenantId}:metadata hash field requires a noeviction (or volatile-*) maxmemory-policy
        // so it is not silently lost under memory pressure (Amendment T).
        try
        {
            _ = db.HashSetAsync(
                $"{input.TenantId}:metadata",
                "lastActivityAt",
                input.IngestedAt.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture),
                flags: CommandFlags.FireAndForget);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to stamp lastActivityAt for tenant {TenantId}; ingest continues",
                input.TenantId);
        }

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in RediSearch for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("syntactic", input.MemoryUnitId, input.TenantId);
    }

    private async Task<string> ResolveContentAsync(IndexInput input)
    {
        if (input.ContentReference is null)
        {
            return input.Content;
        }

        byte[] contentBytes = await RequirePayloadStore()
            .ReadAsync(
                input.ContentReference,
                input.TenantId,
                input.MemoryUnitId,
                WorkflowPayloadKind.ExtractedText,
                CancellationToken.None)
            .ConfigureAwait(false);
        return System.Text.Encoding.UTF8.GetString(contentBytes);
    }

    private IWorkflowPayloadStore RequirePayloadStore()
        => _payloadStore ?? throw new WorkflowPayloadException("PAYLOAD_STORE_UNAVAILABLE", "index-content");
}
