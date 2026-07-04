// <copyright file="RedisNaturalLanguageNamespaceMigrator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using Hexalith.Memories.Server.Infrastructure;

using StackExchange.Redis;

/// <summary>Migrates legacy nested natural-language semantic hashes to the disjoint Story 21.3 prefix.</summary>
internal static class RedisNaturalLanguageNamespaceMigrator
{
    private const int ScanPageSize = 1000;

    /// <summary>Copies verified legacy NL hashes to the disjoint prefix and deletes only after target verification.</summary>
    /// <param name="db">The Redis database.</param>
    /// <param name="server">The Redis server used for cursor scanning.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that completes when migration has converged for the tenant.</returns>
    public static async Task MigrateAsync(IDatabase db, IServer server, string tenantId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string legacyPrefix = IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix(tenantId);
        string targetPrefix = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId);

        await foreach (RedisKey legacyKey in server.KeysAsync(pattern: legacyPrefix + "*", pageSize: ScanPageSize).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();

            string? legacyKeyText = legacyKey.ToString();
            if (string.IsNullOrEmpty(legacyKeyText) || legacyKeyText.Length <= legacyPrefix.Length)
            {
                continue;
            }

            string memoryUnitId = legacyKeyText[legacyPrefix.Length..];
            RedisKey targetKey = targetPrefix + memoryUnitId;

            HashEntry[] targetEntries = await db.HashGetAllAsync(targetKey).WaitAsync(ct).ConfigureAwait(false);
            if (!HasRequiredFields(targetEntries))
            {
                HashEntry[] legacyEntries = await db.HashGetAllAsync(legacyKey).WaitAsync(ct).ConfigureAwait(false);
                if (!HasRequiredFields(legacyEntries))
                {
                    continue;
                }

                await db.HashSetAsync(targetKey, legacyEntries).WaitAsync(ct).ConfigureAwait(false);
                targetEntries = await db.HashGetAllAsync(targetKey).WaitAsync(ct).ConfigureAwait(false);
            }

            if (HasRequiredFields(targetEntries))
            {
                await db.KeyDeleteAsync(legacyKey).WaitAsync(ct).ConfigureAwait(false);
            }
        }
    }

    private static bool HasRequiredFields(HashEntry[] entries)
        => entries.Any(e => e.Name == "embedding" && !e.Value.IsNullOrEmpty)
            && entries.Any(e => e.Name == "memoryUnitId" && !e.Value.IsNullOrEmpty)
            && entries.Any(e => e.Name == "caseId" && !e.Value.IsNullOrEmpty)
            && entries.Any(e => e.Name == "naturalLanguageDescription" && !e.Value.IsNullOrEmpty);
}
