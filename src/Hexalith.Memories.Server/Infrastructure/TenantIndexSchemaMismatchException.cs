// <copyright file="TenantIndexSchemaMismatchException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

/// <summary>Story 23.7 (A34) AC3/AC6: raised when an existing tenant index drifts from the expected schema
/// (prefix, field list, or vector dimensions) in a way that safe additive TAG-field upgrades cannot reconcile.
/// The message preserves the historical "does not match the expected tenant schema" phrasing so incompatible
/// drift still fails before any hash/vector write.</summary>
public sealed class TenantIndexSchemaMismatchException : TenantIndexReadinessException
{
    /// <summary>Initializes a new instance of the <see cref="TenantIndexSchemaMismatchException"/> class.</summary>
    /// <param name="tenantId">The tenant whose index schema is incompatible.</param>
    /// <param name="family">The index family whose schema is incompatible.</param>
    /// <param name="indexName">The fully qualified index name.</param>
    /// <param name="problems">The specific, non-secret schema problems detected.</param>
    public TenantIndexSchemaMismatchException(
        string tenantId,
        TenantIndexFamily family,
        string indexName,
        IReadOnlyList<string> problems)
        : base(
            tenantId,
            family,
            $"Existing {family} index '{indexName}' for tenant '{tenantId}' does not match the expected tenant schema: "
            + $"{string.Join("; ", problems ?? [])}.")
    {
        IndexName = indexName;
        Problems = problems ?? [];
    }

    /// <summary>Gets the fully qualified index name.</summary>
    public string IndexName { get; }

    /// <summary>Gets the specific schema problems detected.</summary>
    public IReadOnlyList<string> Problems { get; }
}
