// <copyright file="IsStubBackfillMigration.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migrations;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>Story 9.2 Task 7.6 — one-shot migration that sets <c>m.isStub = false</c> on pre-9.2
/// <c>MemoryUnit</c> nodes that have <c>content</c> populated. After the migration runs, the
/// content-absent fallback in <see cref="Graph.GraphTraversalService"/> becomes redundant for that
/// database (tracked in <c>deferred-work.md</c> as a post-migration cleanup).</summary>
public sealed partial class IsStubBackfillMigration
{
    internal const string MigrationId = "9.2-isStub-backfill";

    private readonly IConnectionMultiplexer _falkorDb;
    private readonly ILogger<IsStubBackfillMigration> _logger;

    public IsStubBackfillMigration(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        ILogger<IsStubBackfillMigration> logger)
    {
        ArgumentNullException.ThrowIfNull(falkorDb);
        ArgumentNullException.ThrowIfNull(logger);
        _falkorDb = falkorDb;
        _logger = logger;
    }

    /// <summary>Runs the migration for a single tenant graph. Idempotent: a second run is a no-op
    /// because the migration gate node <c>(:SchemaMigration {id: "9.2-isStub-backfill"})</c>
    /// already exists.</summary>
    /// <param name="tenantId">The tenant graph identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of nodes that were backfilled (0 if the migration had already run).</returns>
    public async Task<long> RunAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        FalkorDB falkor = new(_falkorDb.GetDatabase());

        // Gate: check whether the migration ran previously for this graph.
        ResultSet gateCheck = await falkor.SelectGraph(tenantId).QueryAsync(
            "MATCH (s:SchemaMigration {id: $id}) RETURN s.id AS id",
            new Dictionary<string, object> { ["id"] = MigrationId }).ConfigureAwait(false);

        if (gateCheck.Count > 0)
        {
            LogAlreadyRan(_logger, tenantId);
            return 0;
        }

        // Backfill: for every MemoryUnit lacking an isStub flag but WITH content, set isStub = false.
        ResultSet backfill = await falkor.SelectGraph(tenantId).QueryAsync(
            "MATCH (m:MemoryUnit) WHERE m.isStub IS NULL AND m.content IS NOT NULL SET m.isStub = false RETURN count(m) AS backfilled",
            new Dictionary<string, object>()).ConfigureAwait(false);

        long backfilled = TryGetCount(backfill);

        // Record the migration run.
        await falkor.SelectGraph(tenantId).QueryAsync(
            "MERGE (s:SchemaMigration {id: $id}) SET s.ranAt = $ranAt",
            new Dictionary<string, object>
            {
                ["id"] = MigrationId,
                ["ranAt"] = DateTimeOffset.UtcNow.ToString("o"),
            }).ConfigureAwait(false);

        LogBackfillCompleted(_logger, tenantId, backfilled);
        return backfilled;
    }

    private static long TryGetCount(ResultSet result)
    {
        if (result is null || result.Count == 0)
        {
            return 0;
        }

        try
        {
            foreach (Record r in result)
            {
                return r.GetValue<long>("backfilled");
            }
        }
        catch
        {
            // Ignore — partial result handling; treat as 0.
        }

        return 0;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "IsStub backfill already ran for tenant {TenantId} — no-op.")]
    private static partial void LogAlreadyRan(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "IsStub backfill for tenant {TenantId} set {Backfilled} nodes to isStub=false.")]
    private static partial void LogBackfillCompleted(ILogger logger, string tenantId, long backfilled);
}
