// <copyright file="TenantIsolationVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using System.Diagnostics;
using System.Net;

using Dapr.Actors;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Diagnostic tool that verifies tenant data isolation across all storage backends.
/// Confirms that architectural isolation guarantees hold — does not enforce isolation at runtime.</summary>
public sealed partial class TenantIsolationVerifier
{
    // Index positions into both _semanticDiscriminatorFields and the HashGetAsync result it drives; keep the
    // field order and these constants in lock-step.
    private const int MemoryUnitIdFieldIndex = 0;
    private const int TenantIdFieldIndex = 1;
    private const int ChunkSequenceFieldIndex = 2;
    private const int ChunkStartOffsetFieldIndex = 3;
    private const int ChunkEndOffsetFieldIndex = 4;

    private static readonly RedisValue[] _semanticDiscriminatorFields =
    [
        "memoryUnitId",
        "tenantId",
        "chunkSequence",
        "chunkStartOffset",
        "chunkEndOffset",
    ];

    private readonly TenantRegistryService _registry;
    private readonly ITenantEmbeddingConfigProvider _embeddingConfigProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly ILogger<TenantIsolationVerifier> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantIsolationVerifier"/> class.</summary>
    /// <param name="registry">The tenant registry service.</param>
    /// <param name="embeddingConfigProvider">The requested-tenant embedding configuration provider.</param>
    /// <param name="redis">The Redis connection multiplexer for RediSearch and Redis Vector.</param>
    /// <param name="falkorDb">The FalkorDB connection multiplexer for graph database.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantIsolationVerifier(
        TenantRegistryService registry,
        ITenantEmbeddingConfigProvider embeddingConfigProvider,
        IConnectionMultiplexer redis,
        IConnectionMultiplexer falkorDb,
        ILogger<TenantIsolationVerifier> logger)
    {
        ArgumentNullException.ThrowIfNull(embeddingConfigProvider);
        _registry = registry;
        _embeddingConfigProvider = embeddingConfigProvider;
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
        List<TenantInfo> skippedTenants = allTenants
            .Where(t => !string.Equals(t.Id, tenantId, StringComparison.Ordinal) && t.Status != TenantStatus.Active)
            .ToList();

        List<TenantIsolationCheckResult> checks = [];

        // Core checks
        checks.Add(await CheckIndexExistenceAsync(tenantId, ct).ConfigureAwait(false));
        checks.Add(await CheckSyntacticIsolationAsync(tenantId, ct).ConfigureAwait(false));
        checks.Add(await CheckSemanticIsolationAsync(tenantId, ct).ConfigureAwait(false));
        checks.Add(await CheckGraphIsolationAsync(tenantId, ct).ConfigureAwait(false));

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
            string naturalLanguageSemanticIndex = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId);

            List<string> missing = [];

            _ = await TryGetIndexInfoAsync(db, syntacticIndex, missing).ConfigureAwait(false);
            _ = await TryGetIndexInfoAsync(db, semanticIndex, missing).ConfigureAwait(false);
            _ = await TryGetIndexInfoAsync(db, naturalLanguageSemanticIndex, missing).ConfigureAwait(false);

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
        CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            IDatabase db = _redis.GetDatabase();
            string targetIndex = IndexSchemaDefinitions.GetSyntacticIndexName(tenantId);
            string targetPrefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId);
            List<string> missing = [];
            (bool found, RedisResult info) = await TryGetIndexInfoAsync(db, targetIndex, missing).ConfigureAwait(false);
            if (!found)
            {
                sw.Stop();
                return CreateMissingResourcesResult("SyntacticIsolation", missing, tenantId, sw.Elapsed.TotalMilliseconds);
            }

            int? targetDocumentCount = GetIndexDocumentCount(info);
            List<string> problems = [];
            AppendIndexMetadataProblems(
                problems,
                info,
                targetIndex,
                targetPrefix,
                IndexSchemaDefinitions.GetSyntacticFieldIdentifiers());

            (IReadOnlyList<string> mismatches, int scannedCount) = await ScanHashPrefixForTenantFieldMismatchesAsync(
                    "syntactic",
                    targetPrefix,
                    tenantId,
                    ct)
                .ConfigureAwait(false);
            problems.AddRange(mismatches);

            sw.Stop();
            if (problems.Count > 0)
            {
                return new TenantIsolationCheckResult("SyntacticIsolation", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = string.Join("; ", problems),
                    Remediation = "Repair or re-provision the tenant RediSearch index and remove mismatched target-prefix hashes",
                };
            }

            return new TenantIsolationCheckResult("SyntacticIsolation", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = targetDocumentCount == 0 && scannedCount == 0
                    ? "Tenant has zero syntactic hashes/indexed memory units — isolation checks are vacuously true"
                    : $"Target syntactic index metadata and {scannedCount} target-prefix hash(es) verified; indexed docs: {FormatDocumentCount(targetDocumentCount)}",
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
        CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();

        TenantEmbeddingConfig? embeddingConfig;
        try
        {
            Task<TenantEmbeddingConfig>? lookup = _embeddingConfigProvider.GetAsync(tenantId, ct);
            if (lookup is null)
            {
                sw.Stop();
                LogEmbeddingConfigurationLookupFailed(_logger, tenantId, "NullLookupTask");
                return CreateEmbeddingConfigurationUnavailableResult(tenantId, sw.Elapsed.TotalMilliseconds);
            }

            embeddingConfig = await lookup
                .WaitAsync(ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            sw.Stop();
            LogEmbeddingConfigurationLookupFailed(_logger, tenantId, ex.GetType().Name);
            return CreateEmbeddingConfigurationUnavailableResult(tenantId, sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex) when (IsEmbeddingConfigurationUnavailable(ex))
        {
            sw.Stop();
            LogEmbeddingConfigurationLookupFailed(_logger, tenantId, ex.GetType().Name);
            return CreateEmbeddingConfigurationUnavailableResult(tenantId, sw.Elapsed.TotalMilliseconds);
        }

        if (embeddingConfig is null)
        {
            sw.Stop();
            LogEmbeddingConfigurationLookupFailed(_logger, tenantId, "NullConfigurationResult");
            return CreateEmbeddingConfigurationUnavailableResult(tenantId, sw.Elapsed.TotalMilliseconds);
        }

        try
        {
            EmbeddingProviderDefaults.Validate(embeddingConfig);
        }
        catch (ArgumentException ex)
        {
            sw.Stop();
            return new TenantIsolationCheckResult("SemanticIsolation", false, sw.Elapsed.TotalMilliseconds)
            {
                Details = $"Embedding configuration for requested tenant '{tenantId}' is invalid in field '{GetEmbeddingConfigurationValidationField(ex)}' with actual configured dimensions {embeddingConfig.Dimensions}",
                Remediation = $"Correct the embedding configuration for tenant '{tenantId}' and retry verification; no indexes were changed",
            };
        }

        try
        {
            IDatabase db = _redis.GetDatabase();
            string rawIndex = IndexSchemaDefinitions.GetSemanticIndexName(tenantId);
            string rawPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId);
            string naturalLanguageIndex = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId);
            string naturalLanguagePrefix = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId);

            List<string> missing = [];
            (bool rawFound, RedisResult rawInfo) = await TryGetIndexInfoAsync(db, rawIndex, missing).ConfigureAwait(false);
            (bool naturalLanguageFound, RedisResult naturalLanguageInfo) = await TryGetIndexInfoAsync(db, naturalLanguageIndex, missing).ConfigureAwait(false);
            if (!rawFound || !naturalLanguageFound)
            {
                sw.Stop();
                return CreateMissingResourcesResult("SemanticIsolation", missing, tenantId, sw.Elapsed.TotalMilliseconds);
            }

            int? rawDocumentCount = GetIndexDocumentCount(rawInfo);
            int? naturalLanguageDocumentCount = GetIndexDocumentCount(naturalLanguageInfo);
            List<string> problems = [];
            int? rawDimensions = AppendVectorIndexMetadataProblems(
                problems,
                rawInfo,
                rawIndex,
                rawPrefix,
                IndexSchemaDefinitions.GetSemanticFieldIdentifiers());
            int? naturalLanguageDimensions = AppendVectorIndexMetadataProblems(
                problems,
                naturalLanguageInfo,
                naturalLanguageIndex,
                naturalLanguagePrefix,
                IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers());
            AppendConfiguredDimensionProblem(
                problems,
                rawIndex,
                tenantId,
                embeddingConfig.Dimensions,
                rawDimensions);
            AppendConfiguredDimensionProblem(
                problems,
                naturalLanguageIndex,
                tenantId,
                embeddingConfig.Dimensions,
                naturalLanguageDimensions);
            if (rawDimensions is not null
                && naturalLanguageDimensions is not null
                && rawDimensions.Value != naturalLanguageDimensions.Value)
            {
                problems.Add(
                    $"Raw semantic index '{rawIndex}' has {rawDimensions.Value} dimensions but natural-language semantic index '{naturalLanguageIndex}' has {naturalLanguageDimensions.Value}");
            }

            (
                IReadOnlyList<MarkerMismatchEvidence> rawMarkerMismatches,
                IReadOnlyList<string> rawClassificationGaps,
                int rawActiveCount,
                int rawExcludedCount) = await ScanSemanticHashPrefixForTenantEvidenceAsync(
                    rawPrefix,
                    tenantId,
                    ct)
                .ConfigureAwait(false);
            (
                IReadOnlyList<MarkerMismatchEvidence> naturalLanguageMarkerMismatches,
                IReadOnlyList<string> naturalLanguageClassificationGaps,
                int naturalLanguageActiveCount,
                int naturalLanguageExcludedCount) = await ScanSemanticHashPrefixForTenantEvidenceAsync(
                    naturalLanguagePrefix,
                    tenantId,
                    ct)
                .ConfigureAwait(false);
            // Snapshot before classification gaps and marker mismatches are appended below: this is the exact
            // set of non-marker problems (index prefix/field metadata and configured-dimension mismatches)
            // BuildSemanticIsolationRemediation needs, computed directly from its source rather than inferred
            // by subtracting the marker count from the final combined total.
            bool hasNonMarkerProblem = problems.Count > 0;

            List<MarkerMismatchEvidence> allMarkerMismatches =
                [.. rawMarkerMismatches, .. naturalLanguageMarkerMismatches];
            problems.AddRange(rawClassificationGaps);
            problems.AddRange(naturalLanguageClassificationGaps);
            problems.AddRange(allMarkerMismatches.Select(m => m.Detail));
            bool hasClassificationGap = rawClassificationGaps.Count > 0
                || naturalLanguageClassificationGaps.Count > 0;

            sw.Stop();
            if (problems.Count > 0)
            {
                return new TenantIsolationCheckResult("SemanticIsolation", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = string.Join("; ", problems),
                    Remediation = hasClassificationGap
                        ? $"Register or migrate the reported semantic key family for tenant '{tenantId}', then retry verification; no data or indexes were changed"
                        : BuildSemanticIsolationRemediation(
                            tenantId,
                            embeddingConfig.Dimensions,
                            hasNonMarkerProblem,
                            allMarkerMismatches),
                };
            }

            int totalActive = rawActiveCount + naturalLanguageActiveCount;
            int totalExcluded = rawExcludedCount + naturalLanguageExcludedCount;
            return new TenantIsolationCheckResult("SemanticIsolation", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = rawDocumentCount == 0 && naturalLanguageDocumentCount == 0 && totalActive == 0
                    ? $"Tenant has zero active vector hashes/indexed memory units across raw and natural-language semantic indexes; excluded {totalExcluded} proven non-active hash(es); both indexes match the requested tenant's validated {embeddingConfig.Dimensions}-dimension configuration — isolation checks are vacuously true"
                    : $"Target raw and natural-language vector index metadata verified against the requested tenant's validated {embeddingConfig.Dimensions}-dimension configuration; active marker evidence covered {rawActiveCount} raw base/chunk and {naturalLanguageActiveCount} current natural-language hash(es), excluding {totalExcluded} proven non-active hash(es); indexed docs: raw={FormatDocumentCount(rawDocumentCount)}, nl={FormatDocumentCount(naturalLanguageDocumentCount)}",
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
        CancellationToken ct)
    {
        // This check is deliberately structural-only. Content isolation is proven by the manifest-bound,
        // real-backend collision test cited in the successful result below, not by a production graph scan.
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            ct.ThrowIfCancellationRequested();
            IDatabase falkorDb = _falkorDb.GetDatabase();

            // Get all graph databases
            RedisResult graphListResult = await falkorDb.ExecuteAsync("GRAPH.LIST").ConfigureAwait(false);
            HashSet<string> graphDatabases = ParseGraphList(graphListResult);

            List<string> structuralProblems = [];

            if (!graphDatabases.Contains(tenantId))
            {
                structuralProblems.Add($"Tenant '{tenantId}' graph database is missing from GRAPH.LIST");
            }

            sw.Stop();
            if (structuralProblems.Count > 0)
            {
                return new TenantIsolationCheckResult("GraphIsolation", false, sw.Elapsed.TotalMilliseconds)
                {
                    Details = $"Structural database-existence evidence only: {string.Join("; ", structuralProblems)}",
                    Remediation = "Re-provision the missing FalkorDB databases for the affected tenants",
                };
            }

            return new TenantIsolationCheckResult("GraphIsolation", true, sw.Elapsed.TotalMilliseconds)
            {
                Details = $"Structural database-existence evidence only: target graph database '{tenantId}' appears in GRAPH.LIST ({graphDatabases.Count} graph database(s)); independent execution of the real-backend proof TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes is required for content-isolation evidence",
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

    /// <summary>Scans target tenant keys and flags hashes whose optional tenantId field contradicts the target tenant.</summary>
    private async Task<(IReadOnlyList<string> Mismatches, int ScannedCount)> ScanHashPrefixForTenantFieldMismatchesAsync(
        string storageName,
        string keyPrefix,
        string tenantId,
        CancellationToken ct)
    {
        IReadOnlyList<IServer> servers = GetConnectedServers(_redis);
        if (servers.Count == 0)
        {
            throw CreateNoConnectedRedisServerException("tenant key cursor scan");
        }

        IDatabase db = _redis.GetDatabase();
        List<string> mismatches = [];
        HashSet<string> scannedKeys = new(StringComparer.Ordinal);
        int scannedCount = 0;
        foreach (IServer server in servers)
        {
            await foreach (RedisKey key in server.KeysAsync(pattern: keyPrefix + "*", pageSize: 250).WithCancellation(ct))
            {
                string? keyText = key.ToString();
                if (string.IsNullOrWhiteSpace(keyText) || !scannedKeys.Add(keyText))
                {
                    continue;
                }

                scannedCount++;
                RedisValue storedTenantId = await db.HashGetAsync(key, "tenantId").ConfigureAwait(false);
                if (storedTenantId.IsNullOrEmpty)
                {
                    mismatches.Add(
                        $"{storageName} key '{key}' under tenant '{tenantId}' is missing tenantId field");
                    continue;
                }

                string actualTenantId = storedTenantId.ToString();
                if (!string.Equals(actualTenantId, tenantId, StringComparison.Ordinal))
                {
                    mismatches.Add(
                        $"{storageName} key '{key}' under tenant '{tenantId}' has tenantId field '{actualTenantId}'");
                }
            }
        }

        return (mismatches, scannedCount);
    }

    /// <summary>Classifies semantic hashes before evaluating tenant-marker evidence.</summary>
    private async Task<(
        IReadOnlyList<MarkerMismatchEvidence> MarkerMismatches,
        IReadOnlyList<string> ClassificationGaps,
        int ActiveCount,
        int ExcludedCount)> ScanSemanticHashPrefixForTenantEvidenceAsync(
        string keyPrefix,
        string tenantId,
        CancellationToken ct)
    {
        IReadOnlyList<IServer> servers = GetConnectedServers(_redis);
        if (servers.Count == 0)
        {
            throw CreateNoConnectedRedisServerException("semantic tenant key cursor scan");
        }

        IDatabase db = _redis.GetDatabase();
        List<MarkerMismatchEvidence> markerMismatches = [];
        List<string> classificationGaps = [];
        HashSet<string> scannedKeys = new(StringComparer.Ordinal);
        int activeCount = 0;
        int excludedCount = 0;
        foreach (IServer server in servers)
        {
            await foreach (RedisKey key in server.KeysAsync(pattern: keyPrefix + "*", pageSize: 250).WithCancellation(ct))
            {
                string? keyText = key.ToString();
                if (string.IsNullOrWhiteSpace(keyText) || !scannedKeys.Add(keyText))
                {
                    continue;
                }

                RedisValue[] discriminatorValues;
                bool hasNaturalLanguageDescription;
                try
                {
                    discriminatorValues = await db
                        .HashGetAsync(key, _semanticDiscriminatorFields)
                        .WaitAsync(ct)
                        .ConfigureAwait(false);
                    hasNaturalLanguageDescription = await db
                        .HashExistsAsync(key, "naturalLanguageDescription")
                        .WaitAsync(ct)
                        .ConfigureAwait(false);
                }
                catch (RedisServerException ex) when (IsWrongType(ex))
                {
                    classificationGaps.Add(
                        $"Semantic key '{key}' under tenant '{tenantId}' has an evidence-classification gap (wrong Redis value type)");
                    continue;
                }

                SemanticKeyFamily family = SemanticKeyFamilyClassifier.Classify(
                    tenantId,
                    key,
                    discriminatorValues[MemoryUnitIdFieldIndex],
                    discriminatorValues[ChunkSequenceFieldIndex],
                    discriminatorValues[ChunkStartOffsetFieldIndex],
                    discriminatorValues[ChunkEndOffsetFieldIndex],
                    hasNaturalLanguageDescription);

                if (family is SemanticKeyFamily.Unknown or SemanticKeyFamily.Ambiguous)
                {
                    classificationGaps.Add(
                        $"Semantic key '{key}' under tenant '{tenantId}' has an evidence-classification gap ({family.ToString().ToLowerInvariant()})");
                    continue;
                }

                if (!SemanticKeyFamilyClassifier.IsActiveMarkerEvidenceFamily(family))
                {
                    excludedCount++;
                    continue;
                }

                activeCount++;
                RedisValue storedTenantId = discriminatorValues[TenantIdFieldIndex];
                string storageName = family switch
                {
                    SemanticKeyFamily.ActiveRawBase => "raw semantic base",
                    SemanticKeyFamily.ActiveRawChunk => "raw semantic chunk",
                    SemanticKeyFamily.ActiveNaturalLanguage => "natural-language semantic",
                    _ => throw new InvalidOperationException($"Active marker family '{family}' has no storage label."),
                };
                if (storedTenantId.IsNullOrEmpty)
                {
                    markerMismatches.Add(new MarkerMismatchEvidence(
                        MarkerDefectKind.Missing,
                        keyText,
                        $"{storageName} key '{keyText}' under tenant '{tenantId}' is missing its tenantId marker: incomplete evidence, not confirmed cross-tenant leakage — expected tenant '{tenantId}'"));
                    continue;
                }

                string actualTenantId = storedTenantId.ToString();
                if (!string.Equals(actualTenantId, tenantId, StringComparison.Ordinal))
                {
                    markerMismatches.Add(new MarkerMismatchEvidence(
                        MarkerDefectKind.Foreign,
                        keyText,
                        $"{storageName} key '{keyText}' under tenant '{tenantId}' has a foreign tenantId marker '{actualTenantId}': confirmed marker mismatch (possible contamination) — expected tenant '{tenantId}', observed tenant '{actualTenantId}'"));
                }
            }
        }

        return (markerMismatches, classificationGaps, activeCount, excludedCount);
    }

    /// <summary>Distinguishes a missing tenant marker (incomplete evidence) from a foreign tenant marker
    /// (confirmed mismatch/possible contamination) on a proven-active semantic hash. This enum and
    /// <see cref="MarkerMismatchEvidence"/> are internal types only — they never appear in the public V1
    /// contract — but the diagnostic wording they select for is embedded directly into the public
    /// <see cref="TenantIsolationCheckResult.Details"/> and <see cref="TenantIsolationCheckResult.Remediation"/>
    /// string fields, so that wording is contract-visible and must stay backward-compatible in shape.</summary>
    private enum MarkerDefectKind
    {
        /// <summary>The proven-active hash has no <c>tenantId</c> field: incomplete evidence, not confirmed leakage.</summary>
        Missing,

        /// <summary>The proven-active hash has a non-empty <c>tenantId</c> field that differs from the requested
        /// tenant: a confirmed marker mismatch and possible contamination.</summary>
        Foreign,
    }

    /// <summary>One classified tenant-marker defect captured while scanning a proven-active semantic hash
    /// prefix. Internal to <see cref="TenantIsolationVerifier"/> only.</summary>
    /// <param name="Kind">Whether the marker was missing or foreign.</param>
    /// <param name="Key">The exact Redis key the defect was observed on.</param>
    /// <param name="Detail">The payload-safe, human-readable diagnostic text for this entry.</param>
    private readonly record struct MarkerMismatchEvidence(MarkerDefectKind Kind, string Key, string Detail);

    /// <summary>Builds non-destructive, per-defect-kind remediation guidance for a failed <c>SemanticIsolation</c>
    /// check, naming the exact affected key(s) and never recommending blanket prefix/hash deletion.</summary>
    /// <param name="tenantId">The requested tenant identifier.</param>
    /// <param name="expectedDimensions">The tenant's validated configured embedding dimension count.</param>
    /// <param name="hasNonMarkerProblem">Whether the check also reported a non-marker problem (index prefix/field
    /// metadata or configured-dimension mismatch), computed by the caller directly from that problem source.</param>
    /// <param name="markerMismatches">The classified missing/foreign marker defects for this check.</param>
    private static string BuildSemanticIsolationRemediation(
        string tenantId,
        int expectedDimensions,
        bool hasNonMarkerProblem,
        IReadOnlyList<MarkerMismatchEvidence> markerMismatches)
    {
        bool hasForeignMarker = false;
        bool hasMissingMarker = false;
        foreach (MarkerMismatchEvidence mismatch in markerMismatches)
        {
            hasForeignMarker |= mismatch.Kind == MarkerDefectKind.Foreign;
            hasMissingMarker |= mismatch.Kind == MarkerDefectKind.Missing;
        }

        List<string> sentences = [];
        if (hasNonMarkerProblem)
        {
            sentences.Add(
                $"Repair or re-provision tenant '{tenantId}' Redis Vector indexes and reindex its semantic data using the validated {expectedDimensions}-dimension embedding configuration.");
        }

        if (hasForeignMarker)
        {
            string foreignKeys = string.Join(
                ", ",
                markerMismatches.Where(m => m.Kind == MarkerDefectKind.Foreign).Select(m => $"'{m.Key}'"));
            sentences.Add(
                $"For the confirmed marker mismatch (possible contamination) on {foreignKeys}, inspect and quarantine the named key(s), then run tenant-scoped marker repair or reindex for tenant '{tenantId}' only after provenance is verified — never delete the prefix.");
        }

        if (hasMissingMarker)
        {
            string missingKeys = string.Join(
                ", ",
                markerMismatches.Where(m => m.Kind == MarkerDefectKind.Missing).Select(m => $"'{m.Key}'"));
            sentences.Add(
                $"For the incomplete evidence (missing marker, not confirmed leakage) on {missingKeys}, inspect and quarantine the named key(s) before any tenant-scoped marker repair or reindex for tenant '{tenantId}', applied only after provenance is verified — never delete the prefix.");
        }

        return string.Join(" ", sentences);
    }

    private static async Task<(bool Found, RedisResult Info)> TryGetIndexInfoAsync(
        IDatabase db,
        string indexName,
        List<string> missing)
    {
        try
        {
            return (true, await db.ExecuteAsync("FT.INFO", indexName).ConfigureAwait(false));
        }
        catch (RedisServerException ex) when (IsUnknownIndex(ex))
        {
            missing.Add(indexName);
            return (false, default!);
        }
    }

    private static bool IsUnknownIndex(RedisServerException ex)
        => ex.Message.Contains("Unknown index name", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("no such index", StringComparison.OrdinalIgnoreCase);

    private static bool IsWrongType(RedisServerException ex)
        => ex.Message.Contains("WRONGTYPE", StringComparison.OrdinalIgnoreCase);

    private static TenantIsolationCheckResult CreateMissingResourcesResult(
        string checkName,
        IReadOnlyList<string> missing,
        string tenantId,
        double durationMs)
        => new(checkName, false, durationMs)
        {
            Details = $"Missing tenant isolation resources: {string.Join(", ", missing)}",
            Remediation = $"Run tenant provisioning for '{tenantId}' to create or repair missing resources",
        };

    private static void AppendIndexMetadataProblems(
        List<string> problems,
        RedisResult info,
        string indexName,
        string expectedPrefix,
        IReadOnlyCollection<string> expectedFields)
    {
        IReadOnlyList<string> prefixes = IndexSchemaDefinitions.GetIndexPrefixes(info);
        if (prefixes.Count != 1 || !string.Equals(prefixes[0], expectedPrefix, StringComparison.Ordinal))
        {
            problems.Add($"Index '{indexName}' expected prefix '{expectedPrefix}' but found [{string.Join(", ", prefixes)}]");
        }

        HashSet<string> actualFields = new(IndexSchemaDefinitions.GetAttributeIdentifiers(info), StringComparer.OrdinalIgnoreCase);
        HashSet<string> expected = new(expectedFields, StringComparer.OrdinalIgnoreCase);
        if (!actualFields.SetEquals(expected))
        {
            problems.Add(
                $"Index '{indexName}' expected fields [{string.Join(", ", expected.OrderBy(v => v))}] but found [{string.Join(", ", actualFields.OrderBy(v => v))}]");
        }
    }

    private static int? AppendVectorIndexMetadataProblems(
        List<string> problems,
        RedisResult info,
        string indexName,
        string expectedPrefix,
        IReadOnlyCollection<string> expectedFields)
    {
        AppendIndexMetadataProblems(problems, info, indexName, expectedPrefix, expectedFields);
        if (IndexSchemaDefinitions.TryGetVectorDimensions(info, "embedding", out int dimensions))
        {
            return dimensions;
        }

        problems.Add($"Index '{indexName}' embedding vector dimensions are missing from FT.INFO");
        return null;
    }

    private static int? GetIndexDocumentCount(RedisResult info)
        => IndexSchemaDefinitions.TryGetDocumentCount(info, out int documentCount)
            ? documentCount
            : null;

    private static void AppendConfiguredDimensionProblem(
        List<string> problems,
        string indexName,
        string tenantId,
        int expectedDimensions,
        int? actualDimensions)
    {
        if (actualDimensions is not null && actualDimensions.Value != expectedDimensions)
        {
            problems.Add(
                $"Index '{indexName}' for tenant '{tenantId}' expected {expectedDimensions} dimensions from embedding configuration but found {actualDimensions.Value}");
        }
    }

    private static bool IsEmbeddingConfigurationUnavailable(Exception ex)
        => ex is ActorMethodInvocationException
            or Dapr.DaprException
            or TimeoutException
            or HttpRequestException;

    private static TenantIsolationCheckResult CreateEmbeddingConfigurationUnavailableResult(
        string tenantId,
        double durationMs)
        => new("SemanticIsolation", false, durationMs)
        {
            Details = $"Embedding configuration is unavailable for requested tenant '{tenantId}'",
            Remediation = $"Check the DAPR tenant-configuration backend for tenant '{tenantId}' and retry verification",
        };

    private static string GetEmbeddingConfigurationValidationField(ArgumentException exception)
        => exception.ParamName switch
        {
            nameof(TenantEmbeddingConfig.Provider) => "provider",
            nameof(TenantEmbeddingConfig.Model) => "model",
            nameof(TenantEmbeddingConfig.Dimensions) => "dimensions",
            nameof(TenantEmbeddingConfig.RateLimitPerMinute) => "rateLimitPerMinute",
            nameof(TenantEmbeddingConfig.ApiSecretKeyName) => "apiSecretKeyName",
            nameof(TenantEmbeddingConfig.BaseUrl) => "baseUrl",
            nameof(TenantEmbeddingConfig.AuthMode) => "authMode",
            nameof(TenantEmbeddingConfig.OidcTokenEndpoint) => "oidcTokenEndpoint",
            nameof(TenantEmbeddingConfig.OidcClientId) => "oidcClientId",
            _ => "configuration",
        };

    private static string FormatDocumentCount(int? documentCount)
        => documentCount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unknown";

    private static IReadOnlyList<IServer> GetConnectedServers(IConnectionMultiplexer redis)
    {
        List<IServer> servers = [];
        foreach (EndPoint endpoint in redis.GetEndPoints())
        {
            IServer server = redis.GetServer(endpoint);
            if (server.IsConnected)
            {
                servers.Add(server);
            }
        }

        return servers;
    }

    private static RedisConnectionException CreateNoConnectedRedisServerException(string scanName)
        => new(
            ConnectionFailureType.UnableToConnect,
            CommandFlags.None,
            $"No connected Redis server endpoint is available for {scanName}.",
            null);

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

    private static TenantIsolationCheckResult CreateBackendUnavailableResult(string checkName, Exception ex, double durationMs)
    {
        string details = $"Backend unavailable: {ex.Message}";
        if (string.Equals(checkName, "GraphIsolation", StringComparison.Ordinal))
        {
            details = $"Structural database-existence evidence only: {details}";
        }

        return new(checkName, false, durationMs)
        {
            Details = details,
            Remediation = "Check Redis/FalkorDB connectivity and retry",
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting tenant isolation verification for tenant '{TenantId}'")]
    private static partial void LogVerificationStarted(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant isolation verification completed for '{TenantId}': AllPassed={AllPassed}, Checks={CheckCount}")]
    private static partial void LogVerificationCompleted(ILogger logger, string tenantId, bool allPassed, int checkCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Embedding configuration lookup failed for requested tenant '{TenantId}' with {FailureType}; semantic isolation verification will fail closed")]
    private static partial void LogEmbeddingConfigurationLookupFailed(
        ILogger logger,
        string tenantId,
        string failureType);
}
