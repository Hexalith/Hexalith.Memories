// <copyright file="RecoveryExpansionView.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// A deterministic expansion handle projected for safe rendering of compressed/omitted evidence.
/// </summary>
/// <remarks>
/// Story 17.2 — surfaces the contract's omitted detail group and its expansion guidance so compressed
/// packets are announced as omitted-and-expandable, not absent. The opaque handle value itself is never
/// rendered; only the sanitized target detail group and guidance are shown, preserving redaction parity.
/// </remarks>
/// <param name="Kind">The recovery kind that can expand the omitted detail group.</param>
/// <param name="TargetDetailGroup">Sanitized machine-readable detail group targeted by the handle.</param>
/// <param name="Guidance">Sanitized caller guidance for expanding the detail group.</param>
public sealed record RecoveryExpansionView(
    EvidencePacketRecoveryKind Kind,
    string TargetDetailGroup,
    string Guidance);
