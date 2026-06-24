// <copyright file="LensRoleDensity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

/// <summary>Detail density a role-density profile requests for a lens.</summary>
public enum LensDetailLevel
{
    /// <summary>Summarized rows; secondary detail collapsed.</summary>
    Compact = 0,

    /// <summary>Balanced rows with primary detail expanded.</summary>
    Standard,

    /// <summary>Maximal field detail expanded.</summary>
    Detailed,
}

/// <summary>
/// Presentational density profile for a <see cref="LensRole"/>.
/// </summary>
/// <remarks>
/// Story 17.4 — these flags drive ordering, grouping, and default expansion ONLY. They never gate
/// authorization, alter packet semantics, change recovery grammar, move a benchmark threshold, or change
/// an MCP schema meaning. Two roles applied to the same packet must yield identical fields, states, and
/// recovery affordances; only their presentation density differs.
/// </remarks>
/// <param name="Role">The role this profile describes.</param>
/// <param name="ExpandedByDefault">Whether secondary detail rows start expanded.</param>
/// <param name="DetailLevel">The requested detail density.</param>
public sealed record LensRoleDensityProfile(
    LensRole Role,
    bool ExpandedByDefault,
    LensDetailLevel DetailLevel);

/// <summary>Pure resolver from a role to its presentational density profile.</summary>
public static class LensRoleDensity
{
    /// <summary>Resolves the density profile for a role.</summary>
    /// <param name="role">The role-density profile.</param>
    /// <returns>The presentational density profile.</returns>
    public static LensRoleDensityProfile For(LensRole role) => role switch
    {
        LensRole.Developer => new(role, true, LensDetailLevel.Detailed),
        LensRole.Operator => new(role, true, LensDetailLevel.Standard),
        LensRole.TeamLead => new(role, false, LensDetailLevel.Compact),
        LensRole.AgentIntegrator => new(role, true, LensDetailLevel.Detailed),

        // Unknown/future role values fail closed to the safest, least-revealing density.
        _ => new(role, false, LensDetailLevel.Compact),
    };
}
