// <copyright file="IEmbeddingMigrationStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using Hexalith.Memories.Contracts.V1;

/// <summary>Storage boundary for tenant config, Redis indexes, vector hashes, and migration markers.</summary>
public interface IEmbeddingMigrationStore
{
    /// <summary>Lists registered tenant identifiers.</summary>
    Task<IReadOnlyList<string>> ListTenantIdsAsync(CancellationToken ct);

    /// <summary>Reads the current tenant embedding configuration through the committed actor/config surface.</summary>
    Task<TenantEmbeddingConfig> GetEmbeddingConfigAsync(string tenantId, CancellationToken ct);

    /// <summary>Writes the target tenant embedding configuration through the committed actor/config surface.</summary>
    Task SetEmbeddingConfigAsync(string tenantId, TenantEmbeddingConfig config, bool forceReindex, CancellationToken ct);

    /// <summary>Gets counts for syntactic, raw semantic, and natural-language semantic units.</summary>
    Task<EmbeddingMigrationTenantCounts> GetCountsAsync(string tenantId, TenantEmbeddingConfig targetConfig, CancellationToken ct);

    /// <summary>Gets active semantic index dimension information.</summary>
    Task<EmbeddingMigrationIndexInfo> GetIndexInfoAsync(string tenantId, CancellationToken ct);

    /// <summary>Drops and recreates both active semantic indexes for Path A migration.</summary>
    Task DropAndRecreateSemanticIndexesAsync(string tenantId, int dimensions, CancellationToken ct);

    /// <summary>Starts or resumes a durable migration marker.</summary>
    Task StartMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, bool resume, CancellationToken ct);

    /// <summary>Reads the durable active migration marker for a tenant when one protects runtime writes.</summary>
    Task<EmbeddingMigrationMarker?> GetActiveMigrationMarkerAsync(string tenantId, CancellationToken ct);

    /// <summary>Marks a durable migration marker complete.</summary>
    Task CompleteMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, CancellationToken ct);

    /// <summary>Records a durable per-unit failure.</summary>
    Task RecordFailureAsync(EmbeddingMigrationUnitFailure failure, CancellationToken ct);

    /// <summary>Enumerates syntactic units using cursor-based Redis scanning.</summary>
    IAsyncEnumerable<SyntacticMigrationUnit> EnumerateSyntacticUnitsAsync(string tenantId, int pageSize, CancellationToken ct);

    /// <summary>Reads target-detection metadata from a raw semantic hash.</summary>
    Task<SemanticMigrationState?> GetRawSemanticStateAsync(string tenantId, string memoryUnitId, CancellationToken ct);

    /// <summary>Reads a natural-language semantic hash when it exists.</summary>
    Task<NaturalLanguageMigrationUnit?> GetNaturalLanguageSemanticUnitAsync(string tenantId, string memoryUnitId, CancellationToken ct);

    /// <summary>Writes a raw semantic hash using the existing indexing shape plus target metadata.</summary>
    Task WriteRawSemanticAsync(string tenantId, TenantEmbeddingConfig targetConfig, RawSemanticMigrationWrite write, CancellationToken ct);

    /// <summary>Writes a natural-language semantic hash using the existing indexing shape.</summary>
    Task WriteNaturalLanguageSemanticAsync(string tenantId, TenantEmbeddingConfig targetConfig, NaturalLanguageSemanticMigrationWrite write, CancellationToken ct);

    /// <summary>Returns whether explicitly retained previous-version Path B indexes exist.</summary>
    Task<bool> HasRetainedPreviousVersionIndexesAsync(string tenantId, CancellationToken ct);
}
