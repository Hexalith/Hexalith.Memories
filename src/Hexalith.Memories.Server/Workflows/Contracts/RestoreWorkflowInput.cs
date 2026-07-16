// <copyright file="RestoreWorkflowInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>
/// Input to the Story 26.2 <c>RestoreWorkflow</c>. Deliberately small — the (potentially very large) export
/// payload is staged out-of-band and referenced by <see cref="StagingKey"/> so it never becomes durable
/// workflow state. A non-null <see cref="CaseId"/> denotes a case-scoped restore; <see langword="null"/>
/// denotes a tenant-scoped restore.
/// </summary>
/// <param name="TenantId">The target tenant the export is restored into (same-tenant-id DR, decision D2).</param>
/// <param name="CaseId">The target case for a case-scoped restore; <see langword="null"/> for tenant scope.</param>
/// <param name="StagingKey">The key of the staged export payload in the import staging store.</param>
/// <param name="RequestedBy">The authenticated principal that requested the restore (for audit).</param>
public sealed record RestoreWorkflowInput(
    string TenantId,
    string? CaseId,
    string StagingKey,
    string RequestedBy);
