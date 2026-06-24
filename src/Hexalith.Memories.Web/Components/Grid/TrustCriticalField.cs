// <copyright file="TrustCriticalField.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Grid;

/// <summary>
/// The trust-critical fields a Story 17.3 data grid must keep visible or reachable even when compact.
/// </summary>
/// <remarks>
/// Story 17.3 (AC6) — tenant, case, confidence, freshness, evidence health, source count, recovery state,
/// and scope status must stay visible or reachable without horizontal-scroll-only access when a grid
/// renders in a compact form.
/// </remarks>
public enum TrustCriticalField
{
    /// <summary>Tenant scope.</summary>
    Tenant = 0,

    /// <summary>Case scope.</summary>
    Case,

    /// <summary>Confidence / evidence strength.</summary>
    Confidence,

    /// <summary>Source freshness.</summary>
    Freshness,

    /// <summary>Evidence health.</summary>
    EvidenceHealth,

    /// <summary>Source count.</summary>
    SourceCount,

    /// <summary>Recovery state.</summary>
    RecoveryState,

    /// <summary>Scope / isolation status.</summary>
    ScopeStatus,
}
