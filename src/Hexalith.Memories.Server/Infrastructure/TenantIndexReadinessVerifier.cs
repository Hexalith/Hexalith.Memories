// <copyright file="TenantIndexReadinessVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

using System.Collections.Concurrent;

using Hexalith.Memories.Server.Activities.Indexing;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Story 23.7 (A34): process-local, concurrency-safe implementation of
/// <see cref="ITenantIndexReadinessVerifier"/>. Verifies an existing tenant index once with a single
/// <c>FT.INFO</c> call and caches success per (tenant, index family, expected vector dimensions) so subsequent
/// writes in the same process skip readiness I/O. Registered as a singleton so the cache is shared across activity
/// invocations; the cache is never persisted and does not survive a process restart.</summary>
internal sealed partial class TenantIndexReadinessVerifier : ITenantIndexReadinessVerifier
{
    private static readonly TimeSpan IndexInfoRetryDelay = TimeSpan.FromMilliseconds(100);
    private const int IndexInfoRetryAttempts = 10;

    private static readonly string[] SyntacticAdditiveFields = ["cloudeventSubject", "attributeTags"];
    private static readonly string[] SemanticAdditiveFields = ["cloudeventSubject"];

    private readonly ILogger<TenantIndexReadinessVerifier> _logger;
    private readonly ConcurrentDictionary<ReadinessKey, Lazy<Task>> _verified = new();

    /// <summary>Initializes a new instance of the <see cref="TenantIndexReadinessVerifier"/> class.</summary>
    /// <param name="logger">The logger used for low-cardinality additive-field upgrade notices.</param>
    public TenantIndexReadinessVerifier(ILogger<TenantIndexReadinessVerifier> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task EnsureReadyAsync(
        IDatabase database,
        string tenantId,
        TenantIndexFamily family,
        int? expectedDimensions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);
        TenantIdGuard.Validate(tenantId);

        int dimensions = 0;
        if (RequiresVectorDimensions(family))
        {
            if (expectedDimensions is null or <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedDimensions),
                    "Vector index families require positive expected dimensions.");
            }

            dimensions = expectedDimensions.Value;
        }

        // Key on tenant + family + dimensions: a different tenant, family, or vector width is a distinct index and
        // must be verified independently — a successful tenant-A check never authorizes tenant-B writes.
        ReadinessKey key = new(tenantId, family, dimensions);

        // Lazy<Task> coalesces a thundering herd of concurrent first writes (Story 23.6 bounded parallelism) into a
        // single FT.INFO verification instead of one check per document.
        Lazy<Task> pending = _verified.GetOrAdd(
            key,
            _ => new Lazy<Task>(() => VerifyAsync(database, family, tenantId, dimensions, cancellationToken)));

        return AwaitAndInvalidateOnFailureAsync(key, pending);
    }

    private static bool RequiresVectorDimensions(TenantIndexFamily family)
        => family is TenantIndexFamily.Semantic or TenantIndexFamily.NaturalLanguageSemantic;

    private static string GetIndexName(TenantIndexFamily family, string tenantId)
        => family switch
        {
            TenantIndexFamily.Syntactic => IndexSchemaDefinitions.GetSyntacticIndexName(tenantId),
            TenantIndexFamily.Semantic => IndexSchemaDefinitions.GetSemanticIndexName(tenantId),
            TenantIndexFamily.NaturalLanguageSemantic => IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId),
            _ => throw new ArgumentOutOfRangeException(nameof(family)),
        };

    private static string GetExpectedPrefix(TenantIndexFamily family, string tenantId)
        => family switch
        {
            TenantIndexFamily.Syntactic => IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId),
            TenantIndexFamily.Semantic => IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId),
            TenantIndexFamily.NaturalLanguageSemantic => IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId),
            _ => throw new ArgumentOutOfRangeException(nameof(family)),
        };

    private static IReadOnlyList<string> GetExpectedFields(TenantIndexFamily family)
        => family switch
        {
            TenantIndexFamily.Syntactic => IndexSchemaDefinitions.GetSyntacticFieldIdentifiers(),
            TenantIndexFamily.Semantic => IndexSchemaDefinitions.GetSemanticFieldIdentifiers(),
            TenantIndexFamily.NaturalLanguageSemantic => IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers(),
            _ => throw new ArgumentOutOfRangeException(nameof(family)),
        };

    private static IReadOnlyCollection<string> GetAllowedAdditiveFields(TenantIndexFamily family)
        => family switch
        {
            TenantIndexFamily.Syntactic => SyntacticAdditiveFields,
            TenantIndexFamily.Semantic => SemanticAdditiveFields,
            TenantIndexFamily.NaturalLanguageSemantic => [],
            _ => throw new ArgumentOutOfRangeException(nameof(family)),
        };

    private static RedisResult ReadIndexInfo(IDatabase database, TenantIndexFamily family, string tenantId, string indexName)
    {
        try
        {
            return database.Execute("FT.INFO", indexName);
        }
        catch (RedisServerException ex) when (IsUnknownIndex(ex))
        {
            // AC6: a missing index after a tenant is Active is a provisioning inconsistency — fail clearly and
            // tenant-safely, never create the index on demand from the ingestion path.
            throw new TenantIndexNotProvisionedException(tenantId, family, indexName);
        }
    }

    private static bool IsUnknownIndex(RedisServerException ex)
        => ex.Message.Contains("Unknown index name", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("no such index", StringComparison.OrdinalIgnoreCase);

    private async Task AwaitAndInvalidateOnFailureAsync(ReadinessKey key, Lazy<Task> pending)
    {
        try
        {
            await pending.Value.ConfigureAwait(false);
        }
        catch
        {
            // Never cache a failed readiness check: drop the entry so a later write can re-verify after the index
            // is provisioned or repaired (AC8 — a stale entry must not wedge the tenant, and readiness never
            // certifies active status). The value-matched remove avoids evicting a fresh retry started concurrently.
            _verified.TryRemove(new KeyValuePair<ReadinessKey, Lazy<Task>>(key, pending));
            throw;
        }
    }

    private async Task VerifyAsync(
        IDatabase database,
        TenantIndexFamily family,
        string tenantId,
        int expectedDimensions,
        CancellationToken cancellationToken)
    {
        string indexName = GetIndexName(family, tenantId);
        IReadOnlyList<string> lastProblems = [];

        for (int attempt = 1; attempt <= IndexInfoRetryAttempts; attempt++)
        {
            RedisResult info = ReadIndexInfo(database, family, tenantId, indexName);
            VerificationOutcome outcome = Describe(database, family, tenantId, indexName, expectedDimensions, info);
            if (outcome.Problems.Count == 0)
            {
                return;
            }

            lastProblems = outcome.Problems;

            // AC4: retry only transient incomplete FT.INFO metadata, and do it with a non-blocking async delay —
            // never Thread.Sleep. A genuine schema mismatch fails immediately without waiting.
            if (!outcome.IncompleteMetadata || attempt == IndexInfoRetryAttempts)
            {
                break;
            }

            await Task.Delay(IndexInfoRetryDelay, cancellationToken).ConfigureAwait(false);
        }

        throw new TenantIndexSchemaMismatchException(tenantId, family, indexName, lastProblems);
    }

    private VerificationOutcome Describe(
        IDatabase database,
        TenantIndexFamily family,
        string tenantId,
        string indexName,
        int expectedDimensions,
        RedisResult info)
    {
        List<string> problems = [];

        IReadOnlyList<string> prefixes = IndexSchemaDefinitions.GetIndexPrefixes(info);
        HashSet<string> actualFields = new(IndexSchemaDefinitions.GetAttributeIdentifiers(info), StringComparer.OrdinalIgnoreCase);
        bool incompleteMetadata = prefixes.Count == 0 || actualFields.Count == 0;

        string expectedPrefix = GetExpectedPrefix(family, tenantId);
        if (prefixes.Count != 1 || !string.Equals(prefixes[0], expectedPrefix, StringComparison.Ordinal))
        {
            problems.Add($"expected prefix '{expectedPrefix}' but found [{string.Join(", ", prefixes)}]");
        }

        HashSet<string> expectedFields = new(GetExpectedFields(family), StringComparer.OrdinalIgnoreCase);
        IReadOnlyCollection<string> additiveFields = GetAllowedAdditiveFields(family);
        if (!actualFields.SetEquals(expectedFields) && additiveFields.Count > 0)
        {
            // AC3: safe in-place upgrade of known additive TAG fields (cloudeventSubject, attributeTags) before the
            // entry is marked ready — incompatible drift still fails below.
            foreach (string upgradedField in IndexSchemaDefinitions.TryUpgradeMissingTagFields(
                database,
                indexName,
                actualFields,
                expectedFields,
                additiveFields))
            {
                actualFields.Add(upgradedField);
                LogFieldUpgraded(_logger, tenantId, indexName, upgradedField);
            }
        }

        if (!actualFields.SetEquals(expectedFields))
        {
            problems.Add($"expected fields [{string.Join(", ", expectedFields.OrderBy(v => v))}] but found [{string.Join(", ", actualFields.OrderBy(v => v))}]");
        }

        if (RequiresVectorDimensions(family))
        {
            if (!IndexSchemaDefinitions.TryGetVectorDimensions(info, "embedding", out int actualDimensions))
            {
                problems.Add("embedding vector dimensions are missing from FT.INFO");
            }
            else if (actualDimensions != expectedDimensions)
            {
                problems.Add($"expected {expectedDimensions} dimensions but found {actualDimensions}");
            }
        }

        return new VerificationOutcome(problems, incompleteMetadata);
    }

    [LoggerMessage(
        EventId = 2370,
        Level = LogLevel.Information,
        Message = "Added missing {FieldName} field to index {IndexName} for tenant {TenantId} during readiness verification.")]
    private static partial void LogFieldUpgraded(ILogger logger, string tenantId, string indexName, string fieldName);

    private readonly record struct ReadinessKey(string TenantId, TenantIndexFamily Family, int Dimensions);

    private readonly record struct VerificationOutcome(IReadOnlyList<string> Problems, bool IncompleteMetadata);
}
