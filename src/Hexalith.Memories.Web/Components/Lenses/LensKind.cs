// <copyright file="LensKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// The five Story 17.4 role-specific inspection lenses. Each lens is an evidence-density camera angle
/// over the same canonical <c>Contracts.V1</c> Evidence Packet, not a separate evidence, authorization,
/// recovery, benchmark, or MCP schema model.
/// </summary>
public enum LensKind
{
    /// <summary>Case Activity Trail — continuity of how a case's memory changed over time (AC1).</summary>
    CaseActivityTrail = 0,

    /// <summary>Ingestion Lifecycle Tracker — pipeline recoverability for ingestion units (AC2).</summary>
    IngestionLifecycleTracker,

    /// <summary>Operator Health Matrix — operational risk across tenant, backend, and index checks (AC3).</summary>
    OperatorHealthMatrix,

    /// <summary>Benchmark Result Comparator — thesis validation of hybrid-vs-single-axis retrieval (AC4).</summary>
    BenchmarkResultComparator,

    /// <summary>Agent Packet Inspector — MCP request/response trust inspection (AC5).</summary>
    AgentPacketInspector,
}
