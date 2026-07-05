// <copyright file="CaseActivityTrailViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.CaseActivity;

/// <summary>
/// Typed, pure projection of an Evidence Packet into the Case Activity Trail lens (AC1).
/// </summary>
/// <remarks>
/// Story 17.4 — produced by <see cref="CaseActivityTrailMapper.Map"/>. Rows are deterministically ordered;
/// timestamp metadata is rendered when supplied by the canonical packet. The shared scope/trust context
/// lives in the lens shell, so this model carries only the trail itself.
/// </remarks>
/// <param name="Rows">The deterministically ordered, sanitized activity rows.</param>
/// <param name="TimestampsAvailable">Whether at least one rendered row has a chronological timestamp.</param>
/// <param name="OrderingBasisKey">Localization key for the deterministic ordering-basis note.</param>
/// <param name="IsEmpty">Whether the trail has no source, annotation, relationship, or gap activity.</param>
/// <param name="EmptyReasonKey">Localization key shown when the trail is empty.</param>
public sealed record CaseActivityTrailViewModel(
    IReadOnlyList<CaseActivityRow> Rows,
    bool TimestampsAvailable,
    string OrderingBasisKey,
    bool IsEmpty,
    string EmptyReasonKey);
