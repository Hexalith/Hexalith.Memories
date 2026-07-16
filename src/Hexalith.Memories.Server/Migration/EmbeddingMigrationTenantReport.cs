// <copyright file="EmbeddingMigrationTenantReport.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using Hexalith.Memories.Contracts.V1;

/// <summary>Dry-run or live migration report for a single tenant.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Affected">Whether the tenant differs from the target or has stale vector state.</param>
/// <param name="CurrentConfig">The old tenant embedding configuration.</param>
/// <param name="TargetConfig">The target tenant embedding configuration.</param>
/// <param name="Counts">The inventory counts.</param>
/// <param name="IndexInfo">The active semantic index dimensions.</param>
/// <param name="DimensionMismatch">Whether any active semantic index dimension differs from the target.</param>
/// <param name="Raw">The raw semantic counters.</param>
/// <param name="NaturalLanguage">The natural-language semantic counters.</param>
/// <param name="ManualFollowUpRequired">Whether manual operator follow-up is required.</param>
public sealed record EmbeddingMigrationTenantReport(
    string TenantId,
    bool Affected,
    TenantEmbeddingConfig CurrentConfig,
    TenantEmbeddingConfig TargetConfig,
    EmbeddingMigrationTenantCounts Counts,
    EmbeddingMigrationIndexInfo IndexInfo,
    bool DimensionMismatch,
    EmbeddingMigrationUnitCounters Raw,
    EmbeddingMigrationUnitCounters NaturalLanguage,
    bool ManualFollowUpRequired);
