// <copyright file="ExportManifest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Identifies an export file (Story 8.3). Emitted as the first top-level field in the export
/// envelope so streaming parsers can dispatch on <see cref="SchemaVersion"/> after reading only
/// the first KB. Additive schema changes keep <see cref="SchemaVersion"/> at <c>1</c>; breaking
/// changes bump the version per the policy documented in <c>docs/dev/export.md</c>.
/// </summary>
/// <param name="SchemaVersion">Export schema version. <c>1</c> for the MVP shape.</param>
/// <param name="Scope">Scope of this export (case or tenant).</param>
/// <param name="TenantId">Tenant the export was produced from.</param>
/// <param name="CaseId">Case identifier when <see cref="Scope"/> is <see cref="ExportScope.Case"/>; <see langword="null"/> for tenant-scope exports.</param>
/// <param name="ExportedAt">Wall-clock time the server began writing the response (advisory).</param>
/// <param name="SnapshotAt">
/// Server-captured snapshot timestamp. Memory units with <c>IngestedAt &lt;= SnapshotAt</c> and edges
/// with <c>CreatedAt &lt;= SnapshotAt</c> are included; newer records are excluded. See Risk #2 in
/// <c>_bmad-output/implementation-artifacts/8-3-data-export.md</c> for snapshot-isolation semantics.
/// </param>
public sealed record ExportManifest(
    int SchemaVersion,
    ExportScope Scope,
    string TenantId,
    string? CaseId,
    DateTimeOffset ExportedAt,
    DateTimeOffset SnapshotAt);
