// <copyright file="RecoveryActionView.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// A single recovery action projected from <see cref="EvidencePacketRecoveryAction"/> for safe rendering.
/// </summary>
/// <param name="Kind">Machine-readable recovery kind from the Evidence Packet contract.</param>
/// <param name="Label">Sanitized stable label from the packet action.</param>
/// <param name="Guidance">Sanitized human-readable guidance from the packet action.</param>
/// <param name="Target">Sanitized machine-readable target detail group or surface.</param>
/// <param name="IsPrimary">Whether this is the single safest primary action for the state.</param>
/// <param name="Availability">Whether the action is safe to emit in the current authorization context.</param>
/// <param name="DisabledReasonKey">Localization key explaining why an unavailable action is disabled, or null.</param>
public sealed record RecoveryActionView(
    EvidencePacketRecoveryKind Kind,
    string Label,
    string Guidance,
    string Target,
    bool IsPrimary,
    RecoveryActionAvailability Availability,
    string? DisabledReasonKey);
