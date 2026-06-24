// <copyright file="InteractionFamily.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Story 17.3 interaction families that must trace back to upstream contracts, FrontComposer state, and
/// unavailable fallbacks.
/// </summary>
public enum InteractionFamily
{
    /// <summary>Contract-aware forms and field validation.</summary>
    Forms = 0,

    /// <summary>Inspectable search and grid filters.</summary>
    Filters,

    /// <summary>Context-preserving navigation from evidence into details.</summary>
    Navigation,

    /// <summary>Evidence-preserving detail overlays and panels.</summary>
    Overlays,

    /// <summary>Safety confirmations for destructive or diagnostic actions.</summary>
    Confirmations,

    /// <summary>Command palette and command-surface actions.</summary>
    Commands,

    /// <summary>Trust-preserving evidence grids.</summary>
    Grids,
}
