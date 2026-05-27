// <copyright file="TenantIsolationVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using System.Diagnostics;
using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Diagnostic tool that verifies tenant data isolation across all storage backends.
/// Confirms that architectural isolation guarantees hold — does not enforce isolation at runtime.</summary>
public sealed partial class TenantIsolationVerifier
{
    private readonly TenantRegistryService _registry;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly ILogger<TenantIsolationVerifier> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantIsolationVerifier"/> class.</summary>
    /// <param name="registry">The tenant registry service.</param>
    /// <param name="redis">The Redis connection multiplexer for RediSearch and Redis Vector.</param>
    /// <param name="falkorDb">The FalkorDB connection multiplexer for graph database.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantIsolationVerifier(
        TenantRegistryService registry,
        IConnectionMultiplexer redis,
        IConnectionMultiplexer falkorDb,
        ILogger<TenantIsolationVerifier> logger)
    {
        _registry = registry;
        _redis = redis;
        _falkorDb = falkorDb;
        _logger = logger;
    }

    /// <summary>Runs all tenant isolation verification checks.</summary>
    /// <param name="tenantId">The tenant identifier to verify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The aggregated verification result.</returns>
    public async Task<TenantIsolationVerificationResult> VerifyAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        LogVerificationStarted(_logger, tenantId);

        IReadOnlyList<TenantInfo> allTenants = await _registry.ListTenantsAsync(ct).ConfigureAwait(false);
        List<TenantInfo> otherActiveTenants = allTenants
            .Where(t => !string.Equals(t.Id, tenantId, StringComparison.Ordinal) && t.Status == TenantStatus.Active)
            .ToList();

        List<TenantInfo> skippedTenants = allTenants
            .Where(t => !string.Equals(t.Id, tenantId, StringComparison.Ordinal) && t.Status != TenantStatus.Active)
            .ToList();

        List<TenantIsolationCheckResult> checks = [];

        // Core checks
        checks.Add(await CheckIndexExistenceAsync(tenantId, ct).ConfigureAwait(false));
        checks.Add(await CheckSyntacticIsolationAsync(tenantId, otherActiveTenants, ct).ConfigureAwait(false));
        checks.Add(await CheckSemanticIsolationAsync(tenantId, otherActiveTenants, ct).ConfigureAwait(false));
        checks.Add(await CheckGraphIsolationAsync(tenantId, otherActiveTenants, ct).ConfigureAwait(false));
        checks.Add(await CheckInputValidationAsync(tenantId, ct).ConfigureAwait(false));

        // Enhancement checks
        checks.Add(await CheckOrphanedDatabasesAsync(ct).ConfigureAwait(false));

        // Skipped tenant reports
        foreach (TenantInfo skipped in skippedTenants)
        {
            checks.Add(new TenantIsolationCheckResult(
                $"CrossCheck-{skipped.Id}",
                true,
                0.0)
            {
                Details = $"Skipped \u2014 tenant status: {skipped.Status}",
            });
        }

        bool allPassed = checks.All(c => c.Passed);
        List<string> failedNames = checks.Where(c => !c.Passed).Select(c => c.CheckName).ToList();
        int passedCount = checks.Count(c => c.Passed);
        string summary = failedNames.Count == 0
            ? $"{passedCount} of {checks.Count} checks passed"
            : $"{failedNames.Count} of {checks.Count} checks failed: {string.Join(", ", failedNames)}";

        LogVerificationCompleted(_logger, tenantId, allPassed, checks.Count);

        return new TenantIsolationVerificationResult(
            tenantId,
            DateTimeOffset.UtcNow,
            allPassed,
            summary,
            checks);
    }

    private async Task<TenantIsolationCheckResult> CheckIndexExistenceAsync(string tenantId, CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            IDatabase db = _redis.GetDatabase();
            string syntacticIndex = IndexSchemaDefinitions.GetSyntacticIndexName(tenantId);
            string semanticIndex = IndexSchemaDefinitions.GetSemanticIndexName(tenantId);

            List<string> missing = [];

            try
            {
                _ = await db.ExecuteAsync("FT.INFO", syntacticIndex).ConfigureAwait(false);
            }
            catch (RedisServerException)
            {
                missing.Add(syntacticIndex);
            }

            try
            {
                _ = await db.ExecuteAsync("FT.INFO", semanticIndex).ConfigureAwait(false);
            }
            catch (RedisServerException)
            {
                missing.Add(semanticIndex);
            }

            // Check FalkorDB database exists via GRAPH.LIST
            IDatabase falkorDb = _falkorDb.GetDatabase();
            RedisResult graphListResult = await falkorDb.ExecuteAsync("GRAPH.LIST").ConfigureAwait(false);
            HashSet<string> graphDatabases = ParseGraphList(graphListResult);
            if (!graphDatabases.Contains(tenantId))
            {
                missing.Add($"FalkorDB database '{tenantId}'");
            }

            sw.Stop();
            if (missing.Count > 0)
            {
                return new TenantIsolationCheckResult("IndexExistence", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = $"Missing indexes/databases: {string.Join(", ", missing)}",
                    Remediation = $"Run tenant provisioning for '{tenantId}' to create missing indexes",
                };
            }

            return new TenantIsolationCheckResult("IndexExistence", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = "All indexes and databases exist",
            };
        }
        catch (RedisConnectionException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("IndexExistence", ex, sw.Elapsed.TotalMilliseconds);
        }
        catch (RedisServerException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("IndexExistence", ex, sw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<TenantIsolationCheckResult> CheckSyntacticIsolationAsync(
        string tenantId,
        List<TenantInfo> otherActiveTenants,
        CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            if (otherActiveTenants.Count == 0)
            {
                sw.Stop();
                return new TenantIsolationCheckResult("SyntacticIsolation", true, sw.Elapsed.TotalMilliseconds)
                {
                    Details = "Skipped \u2014 no other tenants to check against",
                };
            }

            IDatabase db = _redis.GetDatabase();
            string targetIndex = IndexSchemaDefinitions.GetSyntacticIndexName(tenantId);
            int? targetDocumentCount = await GetIndexDocumentCountAsync(db, targetIndex).ConfigureAwait(false);
            List<string> leaks = [];

            foreach (TenantInfo other in otherActiveTenants)
            {
                string otherKeyPrefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix(other.Id);
                string otherIndex = IndexSchemaDefinitions.GetSyntacticIndexName(other.Id);

                // Check: does target tenant's index contain keys with other tenant's prefix?
                long countInTarget = await SearchIndexForForeignKeysAsync(db, targetIndex, otherKeyPrefix, ct).ConfigureAwait(false);
                if (countInTarget > 0)
                {
                    leaks.Add($"Tenant '{other.Id}' data found in '{tenantId}' syntactic index ({countInTarget} entries)");
                }

                // Check: does other tenant's index contain keys with target tenant's prefix?
                long countInOther = await SearchIndexForForeignKeysAsync(db, otherIndex, IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId), ct).ConfigureAwait(false);
                if (countInOther > 0)
                {
                    leaks.Add($"Tenant '{tenantId}' data found in '{other.Id}' syntactic index ({countInOther} entries)");
                }
            }

            sw.Stop();
            if (leaks.Count > 0)
            {
                return new TenantIsolationCheckResult("SyntacticIsolation", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = string.Join("; ", leaks),
                    Remediation = "Investigate cross-tenant data leakage in RediSearch indexes",
                };
            }

            return new TenantIsolationCheckResult("SyntacticIsolation", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = targetDocumentCount == 0
                    ? "Tenant has zero indexed memory units — isolation checks are vacuously true"
                    : $"No cross-tenant data detected across {otherActiveTenants.Count} other tenant(s)",
            };
        }
        catch (RedisConnectionException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("SyntacticIsolation", ex, sw.Elapsed.TotalMilliseconds);
        }
        catch (RedisServerException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("SyntacticIsolation", ex, sw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<TenantIsolationCheckResult> CheckSemanticIsolationAsync(
        string tenantId,
        List<TenantInfo> otherActiveTenants,
        CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            if (otherActiveTenants.Count == 0)
            {
                sw.Stop();
                return new TenantIsolationCheckResult("SemanticIsolation", true, sw.Elapsed.TotalMilliseconds)
                {
                    Details = "Skipped \u2014 no other tenants to check against",
                };
            }

            IDatabase db = _redis.GetDatabase();
            string targetIndex = IndexSchemaDefinitions.GetSemanticIndexName(tenantId);
            int? targetDocumentCount = await GetIndexDocumentCountAsync(db, targetIndex).ConfigureAwait(false);
            List<string> leaks = [];

            foreach (TenantInfo other in otherActiveTenants)
            {
                string otherKeyPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix(other.Id);
                string otherIndex = IndexSchemaDefinitions.GetSemanticIndexName(other.Id);

                // Check: does target tenant's index contain keys with other tenant's prefix?
                long countInTarget = await SearchIndexForForeignKeysAsync(db, targetIndex, otherKeyPrefix, ct).ConfigureAwait(false);
                if (countInTarget > 0)
                {
                    leaks.Add($"Tenant '{other.Id}' data found in '{tenantId}' semantic index ({countInTarget} entries)");
                }

                // Check: does other tenant's index contain keys with target tenant's prefix?
                long countInOther = await SearchIndexForForeignKeysAsync(db, otherIndex, IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId), ct).ConfigureAwait(false);
                if (countInOther > 0)
                {
                    leaks.Add($"Tenant '{tenantId}' data found in '{other.Id}' semantic index ({countInOther} entries)");
                }
            }

            sw.Stop();
            if (leaks.Count > 0)
            {
                return new TenantIsolationCheckResult("SemanticIsolation", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = string.Join("; ", leaks),
                    Remediation = "Investigate cross-tenant data leakage in Redis Vector indexes",
                };
            }

            return new TenantIsolationCheckResult("SemanticIsolation", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = targetDocumentCount == 0
                    ? "Tenant has zero indexed memory units — isolation checks are vacuously true"
                    : $"No cross-tenant data detected across {otherActiveTenants.Count} other tenant(s)",
            };
        }
        catch (RedisConnectionException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("SemanticIsolation", ex, sw.Elapsed.TotalMilliseconds);
        }
        catch (RedisServerException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("SemanticIsolation", ex, sw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<TenantIsolationCheckResult> CheckGraphIsolationAsync(
        string tenantId,
        List<TenantInfo> otherActiveTenants,
        CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            if (otherActiveTenants.Count == 0)
            {
                sw.Stop();
                return new TenantIsolationCheckResult("GraphIsolation", true, sw.Elapsed.TotalMilliseconds)
                {
                    Details = "Skipped \u2014 no other tenants to check against",
                };
            }

            IDatabase falkorDb = _falkorDb.GetDatabase();

            // Get all graph databases
            RedisResult graphListResult = await falkorDb.ExecuteAsync("GRAPH.LIST").ConfigureAwait(false);
            HashSet<string> graphDatabases = ParseGraphList(graphListResult);

            List<string> structuralProblems = [];

            if (!graphDatabases.Contains(tenantId))
            {
                structuralProblems.Add($"Tenant '{tenantId}' graph database is missing from GRAPH.LIST");
            }

            foreach (TenantInfo other in otherActiveTenants)
            {
                if (!graphDatabases.Contains(other.Id))
                {
                    structuralProblems.Add($"Active tenant '{other.Id}' graph database is missing from GRAPH.LIST");
                }
            }

            sw.Stop();
            if (structuralProblems.Count > 0)
            {
                return new TenantIsolationCheckResult("GraphIsolation", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = string.Join("; ", structuralProblems),
                    Remediation = "Re-provision the missing FalkorDB databases for the affected tenants",
                };
            }

            return new TenantIsolationCheckResult("GraphIsolation", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = $"Structural isolation verified — database '{tenantId}' and {otherActiveTenants.Count} peer database(s) were found",
            };
        }
        catch (RedisConnectionException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("GraphIsolation", ex, sw.Elapsed.TotalMilliseconds);
        }
        catch (RedisServerException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("GraphIsolation", ex, sw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<TenantIsolationCheckResult> CheckOrphanedDatabasesAsync(CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            IDatabase falkorDb = _falkorDb.GetDatabase();
            RedisResult graphListResult = await falkorDb.ExecuteAsync("GRAPH.LIST").ConfigureAwait(false);
            HashSet<string> graphDatabases = ParseGraphList(graphListResult);

            IReadOnlyList<TenantInfo> registeredTenants = await _registry.ListTenantsAsync(ct).ConfigureAwait(false);
            HashSet<string> registeredIds = new(registeredTenants.Select(t => t.Id), StringComparer.Ordinal);

            List<string> orphaned = graphDatabases
                .Where(db => !registeredIds.Contains(db))
                .ToList();

            sw.Stop();
            if (orphaned.Count > 0)
            {
                return new TenantIsolationCheckResult("OrphanedDatabases", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = $"Orphaned FalkorDB databases not in tenant registry: {string.Join(", ", orphaned)}",
                    Remediation = $"Run `memories tenant delete --id {orphaned[0]}` to clean up orphaned database",
                };
            }

            return new TenantIsolationCheckResult("OrphanedDatabases", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = $"All {graphDatabases.Count} FalkorDB database(s) correspond to registered tenants",
            };
        }
        catch (RedisConnectionException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("OrphanedDatabases", ex, sw.Elapsed.TotalMilliseconds);
        }
        catch (RedisServerException ex)
        {
            sw.Stop();
            return CreateBackendUnavailableResult("OrphanedDatabases", ex, sw.Elapsed.TotalMilliseconds);
        }
    }

    private Task<TenantIsolationCheckResult> CheckInputValidationAsync(string tenantId, CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        string[] malformedIds = ["", " ", "../escape", "tenant with spaces", null!];
        List<string> failures = [];

        // Test TenantIdGuard rejects malformed IDs
        foreach (string badId in malformedIds)
        {
            try
            {
                TenantIdGuard.Validate(badId);
                failures.Add($"TenantIdGuard accepted malformed ID: '{badId ?? "(null)"}'");
            }
            catch (ArgumentException)
            {
                // Expected — validation working correctly
            }
        }

        // Test reserved names are rejected
        foreach (string reserved in TenantIdGuard.ReservedNames)
        {
            try
            {
                TenantIdGuard.Validate(reserved);
                failures.Add($"TenantIdGuard accepted reserved name: '{reserved}'");
            }
            catch (ArgumentException)
            {
                // Expected — validation working correctly
            }
        }

        sw.Stop();
        if (failures.Count > 0)
        {
            return Task.FromResult(new TenantIsolationCheckResult("InputValidation", false, sw.Elapsed.TotalMilliseconds)
            {
                Details = string.Join("; ", failures),
                Remediation = "Fix TenantIdGuard validation to reject all malformed and reserved tenant IDs",
            });
        }

        return Task.FromResult(new TenantIsolationCheckResult("InputValidation", true, sw.Elapsed.TotalMilliseconds)
        {
            Details = $"All {malformedIds.Length + TenantIdGuard.ReservedNames.Count} malformed/reserved IDs correctly rejected",
        });
    }

    /// <summary>Checks if a RediSearch index contains documents with a foreign key prefix by scanning all indexed keys.</summary>
    private static async Task<long> SearchIndexForForeignKeysAsync(IDatabase db, string indexName, string foreignKeyPrefix, CancellationToken ct)
    {
        const int PageSize = 250;
        long foreignCount = 0;
        long offset = 0;
        long totalCount;

        do
        {
            ct.ThrowIfCancellationRequested();

            RedisResult result = await db.ExecuteAsync(
                "FT.SEARCH",
                indexName,
                "*",
                "NOCONTENT",
                "LIMIT",
                offset.ToString(CultureInfo.InvariantCulture),
                PageSize.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            RedisResult[]? results = (RedisResult[]?)result;
            if (results is null || results.Length == 0)
            {
                return 0;
            }

            totalCount = ParseRedisLong(results[0]);
            if (results.Length == 1 || totalCount == 0)
            {
                return 0;
            }

            // results[0] = total count, results[1..] = document keys (NOCONTENT mode)
            for (int i = 1; i < results.Length; i++)
            {
                string? key = results[i].ToString();
                if (key is not null && key.StartsWith(foreignKeyPrefix, StringComparison.Ordinal))
                {
                    foreignCount++;
                }
            }

            offset += results.Length - 1;
        }

        while (offset < totalCount);

        return foreignCount;
    }

    private static async Task<int?> GetIndexDocumentCountAsync(IDatabase db, string indexName)
    {
        RedisResult info = await db.ExecuteAsync("FT.INFO", indexName).ConfigureAwait(false);
        return IndexSchemaDefinitions.TryGetDocumentCount(info, out int documentCount)
            ? documentCount
            : null;
    }

    private static HashSet<string> ParseGraphList(RedisResult result)
    {
        HashSet<string> databases = new(StringComparer.Ordinal);
        try
        {
            RedisResult[]? items = (RedisResult[]?)result;
            if (items is not null)
            {
                foreach (RedisResult item in items)
                {
                    string? name = item.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        databases.Add(name);
                    }
                }
            }
        }
        catch (InvalidCastException)
        {
            // Empty or unexpected format
        }

        return databases;
    }

    private static long ParseRedisLong(RedisResult result)
    {
        if (result.Resp2Type == ResultType.Integer)
        {
            return (long)result;
        }

        return long.TryParse(result.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
            ? parsed
            : 0;
    }

    private static TenantIsolationCheckResult CreateBackendUnavailableResult(string checkName, Exception ex, double durationMs)
        => new(checkName, false, durationMs)
        {
            Details = $"Backend unavailable: {ex.Message}",
            Remediation = "Check Redis/FalkorDB connectivity and retry",
        };

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting tenant isolation verification for tenant '{TenantId}'")]
    private static partial void LogVerificationStarted(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant isolation verification completed for '{TenantId}': AllPassed={AllPassed}, Checks={CheckCount}")]
    private static partial void LogVerificationCompleted(ILogger logger, string tenantId, bool allPassed, int checkCount);
}
