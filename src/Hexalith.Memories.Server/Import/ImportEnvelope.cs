// <copyright file="ImportEnvelope.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// The fully parsed export envelope (Story 26.2), normalized across case-scope and tenant-scope exports:
/// a case-scope export's single <c>case</c> object is normalized into <see cref="Cases"/> with one element.
/// </summary>
internal sealed record ImportEnvelope
{
    /// <summary>Gets the export manifest (schema version, scope, tenant/case ids).</summary>
    public required ExportManifest Manifest { get; init; }

    /// <summary>Gets the tenant configuration for a tenant-scope export; <see langword="null"/> for case-scope.</summary>
    public ExportedTenantConfig? Tenant { get; init; }

    /// <summary>Gets the cases (one for case-scope, N for tenant-scope), each with its members.</summary>
    public IReadOnlyList<ImportedCase> Cases { get; init; } = [];

    /// <summary>Gets the exported memory units.</summary>
    public IReadOnlyList<ExportedMemoryUnit> MemoryUnits { get; init; } = [];

    /// <summary>Gets the exported graph edges (excludes CONTAINS, which is rebuilt from each unit's caseId).</summary>
    public IReadOnlyList<ExportedEdge> Edges { get; init; } = [];

    /// <summary>Gets the export statistics; <see langword="null"/> when absent.</summary>
    public ExportStatistics? Statistics { get; init; }
}
