// <copyright file="InteractionContextSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// Tenant/case/search-scoped snapshot captured before executing navigation, overlay, command, grid-row, or
/// confirmation work.
/// </summary>
/// <param name="TenantId">The tenant captured when the interaction was built.</param>
/// <param name="CaseId">The case captured when the interaction was built, or null for tenant-wide.</param>
/// <param name="Query">The search query captured when the interaction was built.</param>
/// <param name="PacketState">The packet state captured when the interaction was built.</param>
/// <param name="ContractVersion">The contract version captured when the interaction was built.</param>
/// <param name="ReturnRoute">A safe internal route or route-like token used to return to the originating packet/grid.</param>
/// <param name="TargetKind">The kind of target captured for the interaction.</param>
/// <param name="TargetId">The specific target id, when applicable.</param>
/// <param name="Filters">The active filters captured with the interaction.</param>
public sealed record InteractionContextSnapshot(
    string TenantId,
    string? CaseId,
    string Query,
    EvidencePacketState PacketState,
    string ContractVersion,
    string ReturnRoute,
    InteractionTargetKind TargetKind,
    string? TargetId,
    IReadOnlyList<MemoriesFilter> Filters)
{
    /// <summary>Canonical Story 2.7 contract version consumed by this web slice.</summary>
    public const string SupportedContractVersion = "v1";
}
