// <copyright file="InteractionDisplay.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Memories.Web.Components.Evidence;

using Microsoft.FluentUI.AspNetCore.Components;

/// <summary>
/// Shared presentation helpers for Story 17.3 interaction surfaces.
/// </summary>
/// <remarks>
/// Centralizes the mapping from <see cref="InteractionSeverity"/> to Fluent badge slots and message-bar
/// intents, and re-exports the single sanitization path so every interaction family redacts secrets,
/// tokens, paths, and exception text identically to the visible Evidence Cockpit (Story 17.1).
/// </remarks>
internal static class InteractionDisplay
{
    /// <summary>Maps an interaction severity to a FrontComposer status-badge slot.</summary>
    /// <param name="severity">The severity tier.</param>
    /// <returns>The badge slot used to color the badge.</returns>
    public static BadgeSlot SeveritySlot(InteractionSeverity severity)
        => severity switch
        {
            InteractionSeverity.Critical => BadgeSlot.Danger,
            InteractionSeverity.Warning => BadgeSlot.Warning,
            InteractionSeverity.Caution => BadgeSlot.Warning,
            InteractionSeverity.Info => BadgeSlot.Info,
            _ => BadgeSlot.Success,
        };

    /// <summary>Maps an interaction severity to a Fluent message-bar intent.</summary>
    /// <param name="severity">The severity tier.</param>
    /// <returns>The message-bar intent.</returns>
    public static MessageBarIntent MessageIntent(InteractionSeverity severity)
        => severity switch
        {
            InteractionSeverity.Critical => MessageBarIntent.Error,
            InteractionSeverity.Warning => MessageBarIntent.Warning,
            InteractionSeverity.Caution => MessageBarIntent.Warning,
            InteractionSeverity.Info => MessageBarIntent.Info,
            _ => MessageBarIntent.Success,
        };

    /// <summary>Sanitizes a dynamic, contract-derived string for safe display, copy, and diagnostics.</summary>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The fallback used when the value is null or whitespace.</param>
    /// <returns>The sanitized text.</returns>
    public static string SafeText(string? value, string fallback = "unavailable")
        => EvidenceDisplay.SafeText(value, fallback);
}
