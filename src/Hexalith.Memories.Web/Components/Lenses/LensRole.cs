// <copyright file="LensRole.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// Role-density profile for a lens audience.
/// </summary>
/// <remarks>
/// Story 17.4 — a role is an evidence-density profile over the same canonical packet. It changes
/// ordering, grouping, and default expansion ONLY. It never changes the underlying packet fields, state
/// grammar, recovery affordances, authorization model, benchmark threshold, or MCP schema meaning, and it
/// never broadens authorization or exposes restricted fields. <see cref="LensRoleDensity"/> proves the
/// difference is presentational by construction.
/// </remarks>
public enum LensRole
{
    /// <summary>Developer audience: prefers maximal field detail and expanded technical rows.</summary>
    Developer = 0,

    /// <summary>Operator audience: prefers health/severity-first ordering and recovery affordances.</summary>
    Operator,

    /// <summary>Team-lead audience: prefers summarized, collapsed-by-default density.</summary>
    TeamLead,

    /// <summary>LLM-agent integrator audience: prefers schema, token-budget, and packet-identity detail.</summary>
    AgentIntegrator,
}
