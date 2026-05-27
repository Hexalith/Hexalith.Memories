// <copyright file="ExportSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Export;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Pre-flight snapshot captured before the export writer commits response headers (Story 8.3 Task 1.2a).
/// The <see cref="SnapshotAt"/> timestamp is the <em>only</em> advisory-snapshot reference used by
/// the writer to filter memory units and edges; the tenant and case records are hydrated upfront so
/// the endpoint can fail fast with 404 before streaming begins.
/// </summary>
/// <param name="SnapshotAt">Snapshot timestamp (captured before any backend call).</param>
/// <param name="Tenant">Tenant info from the registry (never <see langword="null"/>).</param>
/// <param name="CaseRecord">Case record for case-scope exports; <see langword="null"/> for tenant-scope.</param>
/// <param name="Members">Case members for case-scope exports; <see langword="null"/> for tenant-scope.</param>
/// <param name="TenantConfig">Tenant configuration snapshot for tenant-scope exports; <see langword="null"/> for case-scope.</param>
public sealed record ExportSnapshot(
    DateTimeOffset SnapshotAt,
    TenantInfo Tenant,
    Case? CaseRecord,
    IReadOnlyList<CaseMember>? Members,
    ExportedTenantConfig? TenantConfig);
