// <copyright file="RestoreAcceptedResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Response body returned with <c>202 Accepted</c> when a tenant or case import/restore
/// (Story 26.2) is scheduled as a durable restore workflow.
/// </summary>
/// <param name="InstanceId">The restore workflow instance identifier (poll the status location with this id).</param>
/// <param name="TenantId">The target tenant the export is being restored into.</param>
/// <param name="CaseId">The target case for a case-scoped restore; <see langword="null"/> for a tenant-scoped restore.</param>
/// <param name="Scope">The scope of the accepted restore, echoed from the export manifest.</param>
/// <param name="StatusLocation">The absolute status location (also sent in the <c>Location</c> header) to poll for completion.</param>
public sealed record RestoreAcceptedResponse(
    string InstanceId,
    string TenantId,
    string? CaseId,
    ExportScope Scope,
    string StatusLocation);
