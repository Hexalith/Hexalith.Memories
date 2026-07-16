// <copyright file="RecoveryActionInvocation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// A recovery intent emitted by the recovery panel for the host to route through existing command,
/// navigation, or handler conventions.
/// </summary>
/// <remarks>
/// Story 17.2 — the panel emits intents only. It never retries ingestion, mutates authorization,
/// refreshes tenant scope, repairs consistency, executes retrieval, or launches backend recovery
/// directly; the host owns lifecycle, authorization, diagnostics, and tenant context.
/// </remarks>
/// <param name="Kind">The recovery kind the operator chose.</param>
/// <param name="Target">Sanitized machine-readable target detail group or surface.</param>
/// <param name="TenantId">Sanitized tenant identifier the action applies to.</param>
/// <param name="CaseId">Sanitized case identifier the action applies to, or null for tenant scope.</param>
public sealed record RecoveryActionInvocation(
    EvidencePacketRecoveryKind Kind,
    string Target,
    string TenantId,
    string? CaseId);
