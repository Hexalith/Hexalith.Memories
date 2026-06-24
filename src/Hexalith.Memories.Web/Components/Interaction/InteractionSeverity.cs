// <copyright file="InteractionSeverity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Shared severity scale for Story 17.3 contract-aware interaction surfaces (forms, filters, navigation,
/// confirmations, and commands).
/// </summary>
/// <remarks>
/// Mirrors the recovery-grammar severity tiers from Story 17.2 so badges, message bars, and accessible
/// announcements stay visually and semantically consistent across every interaction family. Higher values
/// are more severe; presentation maps each tier to a Fluent badge slot or message-bar intent.
/// </remarks>
public enum InteractionSeverity
{
    /// <summary>No risk; the interaction is safe and consistent.</summary>
    None = 0,

    /// <summary>Informational; surfaced for awareness without implying risk.</summary>
    Info,

    /// <summary>Caution; the interaction changes meaning or scope and deserves attention.</summary>
    Caution,

    /// <summary>Warning; the interaction is inconsistent, degraded, or scope-affecting.</summary>
    Warning,

    /// <summary>Critical; the interaction is unsafe or unauthorized and must be blocked or acknowledged.</summary>
    Critical,
}
