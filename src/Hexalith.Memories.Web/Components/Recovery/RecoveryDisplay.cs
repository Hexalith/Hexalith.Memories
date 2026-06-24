// <copyright file="RecoveryDisplay.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Shared presentation helpers for the Story 17.2 recovery state grammar.
/// </summary>
/// <remarks>
/// Centralizes the mapping from <see cref="RecoverySeverity"/> to FrontComposer status-badge slots so every
/// surface that renders a recovery severity — the recovery panel and all Story 17.4 inspection lenses —
/// colours the same severity identically. This mirrors the <c>EvidenceDisplay</c> (Story 17.1) and
/// <c>InteractionDisplay</c> (Story 17.3) helper convention and keeps cross-lens severity rendering
/// consistent without re-declaring the switch in every component.
/// </remarks>
internal static class RecoveryDisplay
{
    /// <summary>Maps a recovery severity to a FrontComposer status-badge slot.</summary>
    /// <param name="severity">The severity tier.</param>
    /// <returns>The badge slot used to colour the badge.</returns>
    public static BadgeSlot SeveritySlot(RecoverySeverity severity)
        => severity switch
        {
            RecoverySeverity.Critical => BadgeSlot.Danger,
            RecoverySeverity.Warning => BadgeSlot.Warning,
            RecoverySeverity.Caution => BadgeSlot.Warning,
            RecoverySeverity.Info => BadgeSlot.Info,
            _ => BadgeSlot.Success,
        };
}
