// <copyright file="MemoriesFormRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// A proposed contract-aware form submission, carrying both the requested scope and the current scope so
/// the validator can detect dangerous tenant/case scope changes before dispatch.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — the request is the unit the <see cref="ContractAwareFormValidator"/> validates. It
/// never bypasses the existing command lifecycle, authorization, or tenant context; it only describes the
/// proposed change so the UI can validate it and gate dangerous changes behind acknowledgement.
/// </remarks>
/// <param name="FormKind">The configuration surface being changed.</param>
/// <param name="RequestedTenantId">The tenant the submission targets.</param>
/// <param name="RequestedCaseId">The case the submission targets, or null for tenant-wide scope.</param>
/// <param name="CurrentTenantId">The tenant scope the user is currently in.</param>
/// <param name="CurrentCaseId">The case scope the user is currently in, or null for tenant-wide scope.</param>
/// <param name="IsolationStatus">The authorization/isolation status of the requested scope.</param>
/// <param name="Fields">The declared fields to validate, in author-defined order.</param>
/// <param name="Acknowledged">Whether the user has explicitly acknowledged any dangerous change.</param>
public sealed record MemoriesFormRequest(
    MemoriesFormKind FormKind,
    string RequestedTenantId,
    string? RequestedCaseId,
    string CurrentTenantId,
    string? CurrentCaseId,
    EvidencePacketIsolationStatus IsolationStatus,
    IReadOnlyList<MemoriesFormField> Fields,
    bool Acknowledged);
