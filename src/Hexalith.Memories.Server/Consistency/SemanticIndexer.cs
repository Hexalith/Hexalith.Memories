// <copyright file="SemanticIndexer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>
/// Story 8.2: semantic (Redis Vector) re-index helper. Owns the write-to-<c>:vec:</c>-hash
/// path that was originally inlined in <c>IndexSemanticActivity</c>. Used by
/// <c>RepairUnitActivity</c> when a unit's semantic entry has to be re-created.
/// </summary>
/// <remarks>
/// <para>
/// <b>Phase B scope:</b> the <see cref="ReIndexFromSyntacticAsync"/> path reads the
/// <c>{tenantId}:mu:{id}</c> syntactic hash and attempts to reconstruct the vector by
/// regenerating the embedding. Embedding regeneration requires <c>EmbeddingClient</c> +
/// <c>EmbeddingRateLimiterActor</c> plumbing; that wiring is deferred to Phase C of
/// Story 8.2 (Tasks 4-6 landing). Until then, re-index-semantic throws
/// <see cref="NotSupportedException"/> with a message referencing the follow-up.
/// </para>
/// <para>
/// <b>Factoring rationale:</b> repair must reuse the exact same Redis write logic or we
/// risk write-path drift between ingest and repair. The activity's current HashSet call
/// should be moved here in Phase C's Task 3.8 refactor.
/// </para>
/// </remarks>
public partial class SemanticIndexer : ISemanticIndexer
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SemanticIndexer> _logger;

    /// <summary>Initializes a new instance of the <see cref="SemanticIndexer"/> class.</summary>
    /// <param name="redis">The Redis multiplexer (keyed <c>"redis"</c>).</param>
    /// <param name="logger">Logger.</param>
    public SemanticIndexer(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<SemanticIndexer> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Re-creates the semantic entry for a memory unit by reading the authoritative
    /// syntactic hash, regenerating the embedding, and writing the vector.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the syntactic hash is absent.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown in Phase B — embedding regeneration wiring (EmbeddingClient + rate-limiter actor)
    /// lands in Phase C of Story 8.2.
    /// </exception>
    public virtual async Task ReIndexFromSyntacticAsync(string tenantId, string memoryUnitId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string syntacticKey = $"{tenantId}:mu:{memoryUnitId}";

        HashEntry[] entries = await db.HashGetAllAsync(syntacticKey).WaitAsync(ct).ConfigureAwait(false);
        if (entries.Length == 0)
        {
            throw new KeyNotFoundException(
                $"Cannot re-index semantic: syntactic hash '{syntacticKey}' is absent. "
                + "Unit is classified Unrepairable by the repair workflow.");
        }

        LogReIndexStarted(_logger, tenantId, memoryUnitId);

        // Embedding regeneration is deferred to Phase C (Story 8.2 Task 3.8 follow-up): wiring
        // EmbeddingClient + IActorProxyFactory into SemanticIndexer so we can re-generate
        // the embedding through the same rate-limiter actor path ingestion uses. Until then,
        // this branch surfaces as a repair failure (Succeeded=false) in RepairActionRecord.
        throw new NotSupportedException(
            $"Semantic re-index for unit '{memoryUnitId}' is deferred to Story 8.2 Phase C "
            + "(EmbeddingClient + rate-limiter actor wiring in SemanticIndexer).");
    }

    [LoggerMessage(
        EventId = 8210,
        Level = LogLevel.Information,
        Message = "SemanticReIndexStarted tenant '{TenantId}' unit '{MemoryUnitId}'")]
    private static partial void LogReIndexStarted(ILogger logger, string tenantId, string memoryUnitId);
}
