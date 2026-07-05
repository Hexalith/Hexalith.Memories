// <copyright file="LensFieldTraceability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// The Story 17.4 shared lens field-trace table: for every lens, which canonical Evidence Packet field
/// (or upstream 17.1/17.2/17.3 component) drives each displayed field, how it renders when absent or
/// restricted, the test level, and the evidence artifact.
/// </summary>
/// <remarks>
/// <para>This table is the single source of truth that justifies every rendered lens field. A lens may
/// only display a field that traces back to a named contract/component source here. Fields the canonical
/// <c>Contracts.V1</c> contract still does not expose record <see cref="NoContractSource"/> and render a
/// documented unavailable/insufficient-evidence boundary.</para>
/// <para>Story 2.7 now exposes optional metadata for timestamps, freshness, benchmark evidence, ingestion
/// stages, and MCP schema. This slice remains consume-only over that contract metadata.</para>
/// </remarks>
public static class LensFieldTraceability
{
    /// <summary>Sentinel recorded when the canonical contract does not yet expose a source field.</summary>
    public const string NoContractSource = "(none — no canonical contract source; deferred to Story 2.7)";

    private const string Unit = "unit";
    private const string Bunit = "bUnit";

    /// <summary>Gets every field-trace row across all five lenses.</summary>
    public static IReadOnlyList<LensFieldTrace> Entries { get; } =
    [
        // ----- Case Activity Trail (AC1) -----
        new(LensKind.CaseActivityTrail, "activity.scope", "EvidencePacket.Scope.TenantId / Scope.CaseId", "unknown tenant / tenant scope", Bunit, "CaseActivityTrailMapperTests"),
        new(LensKind.CaseActivityTrail, "activity.sourceLink", "EvidencePacket.Sources[].SourceUri / MemoryUnitId", "source unavailable / redacted link state", Bunit, "CaseActivityTrailMapperTests"),
        new(LensKind.CaseActivityTrail, "activity.relationship", "EvidencePacket.Graph.RelatedPath / EdgeTypes", "no traversal path", Unit, "CaseActivityTrailMapperTests"),
        new(LensKind.CaseActivityTrail, "activity.status", "EvidencePacket.State (via RecoveryStateMapper)", "insufficient evidence label", Bunit, "CaseActivityTrailMapperTests"),
        new(LensKind.CaseActivityTrail, "activity.timestamp", "EvidencePacket.Sources[].Timestamp", "timestamp unavailable; deterministic by rank", Unit, "CaseActivityTrailMapperTests"),
        new(LensKind.CaseActivityTrail, "activity.return", "InteractionContextSnapshot.ReturnRoute (Story 17.3)", "return unavailable", Bunit, "MemoriesCaseActivityTrailTests"),

        // ----- Ingestion Lifecycle Tracker (AC2) -----
        new(LensKind.IngestionLifecycleTracker, "ingestion.unit", "EvidencePacket.Sources[].MemoryUnitId", "memory unit unavailable", Bunit, "IngestionLifecycleMapperTests"),
        new(LensKind.IngestionLifecycleTracker, "ingestion.stage", "EvidencePacket.Sources[].Ingestion.Stage", "stage unavailable", Unit, "IngestionLifecycleMapperTests"),
        new(LensKind.IngestionLifecycleTracker, "ingestion.outcome", "EvidencePacket.Result.HasIndexedMemoryUnits / State", "outcome unavailable", Unit, "IngestionLifecycleMapperTests"),
        new(LensKind.IngestionLifecycleTracker, "ingestion.degradation", "EvidencePacket.Evidence.Degraded / OmittedDetails.Reason", "no degradation signal", Unit, "IngestionLifecycleMapperTests"),
        new(LensKind.IngestionLifecycleTracker, "ingestion.recovery", "EvidencePacket.Recovery[] (via RecoveryStateMapper)", "no recovery action; disabled when restrictive", Bunit, "MemoriesIngestionLifecycleTrackerTests"),

        // ----- Operator Health Matrix (AC3) -----
        new(LensKind.OperatorHealthMatrix, "health.tenantIsolation", "EvidencePacket.Scope.IsolationStatus", "unknown status (trust-blocking)", Bunit, "OperatorHealthMatrixMapperTests"),
        new(LensKind.OperatorHealthMatrix, "health.retrieval", "EvidencePacket.Evidence.Degraded / UnavailableAxes", "unavailable", Unit, "OperatorHealthMatrixMapperTests"),
        new(LensKind.OperatorHealthMatrix, "health.graph", "EvidencePacket.Graph.Available / GapMarkers", "unavailable", Unit, "OperatorHealthMatrixMapperTests"),
        new(LensKind.OperatorHealthMatrix, "health.authorization", "EvidencePacket.State / OmittedDetails.Reason", "trust-blocking unauthorized state", Bunit, "OperatorHealthMatrixMapperTests"),
        new(LensKind.OperatorHealthMatrix, "health.affectedCapability", "RecoveryStateTraceability (Story 17.2)", "answer-support fallback", Unit, "OperatorHealthMatrixMapperTests"),
        new(LensKind.OperatorHealthMatrix, "health.lastChecked", "EvidencePacket.Metadata.Freshness.LastCheckedAt", "last-checked unavailable", Unit, "OperatorHealthMatrixMapperTests"),
        new(LensKind.OperatorHealthMatrix, "health.queueBacklog", NoContractSource, "unavailable; live probes out of scope", Unit, "OperatorHealthMatrixMapperTests"),

        // ----- Benchmark Result Comparator (AC4) -----
        new(LensKind.BenchmarkResultComparator, "benchmark.axisEvidence", "EvidencePacket.Evidence.AxisEvidence[] (retrieval relevance, not NDCG)", "axis unavailable", Bunit, "BenchmarkResultComparatorMapperTests"),
        new(LensKind.BenchmarkResultComparator, "benchmark.unavailableAxes", "EvidencePacket.Evidence.UnavailableAxes", "no unavailable axes", Unit, "BenchmarkResultComparatorMapperTests"),
        new(LensKind.BenchmarkResultComparator, "benchmark.ndcg", "EvidencePacket.Metadata.Benchmark.*Ndcg10", "benchmark evidence unavailable", Bunit, "BenchmarkResultComparatorMapperTests"),
        new(LensKind.BenchmarkResultComparator, "benchmark.threshold", "EvidencePacket.Metadata.Benchmark.Threshold / ThresholdPassed", "threshold status unavailable", Unit, "BenchmarkResultComparatorMapperTests"),
        new(LensKind.BenchmarkResultComparator, "benchmark.perQuery", "EvidencePacket.Metadata.Benchmark.PerQuery", "per-query breakdown unavailable", Unit, "BenchmarkResultComparatorMapperTests"),
        new(LensKind.BenchmarkResultComparator, "benchmark.evidenceLink", "EvidencePacket.Metadata.Benchmark.EvidenceUri", "reproducible evidence link unavailable", Unit, "BenchmarkResultComparatorMapperTests"),

        // ----- Agent Packet Inspector (AC5) -----
        new(LensKind.AgentPacketInspector, "packet.requestSummary", "EvidencePacket.Result.Query / TotalCount / ReturnedCount", "no query / counts unavailable", Bunit, "AgentPacketInspectorMapperTests"),
        new(LensKind.AgentPacketInspector, "packet.responseSchema", "EvidencePacket field structure (sanitized)", "schema unavailable", Bunit, "AgentPacketInspectorMapperTests"),
        new(LensKind.AgentPacketInspector, "packet.tokenBudget", "EvidencePacket.OmittedDetails.EstimatedTokensTotal / Reason", "token budget unavailable", Unit, "AgentPacketInspectorMapperTests"),
        new(LensKind.AgentPacketInspector, "packet.omittedFields", "EvidencePacket.OmittedDetails.FieldNames / DetailGroups", "no omitted fields", Unit, "AgentPacketInspectorMapperTests"),
        new(LensKind.AgentPacketInspector, "packet.expansionHandles", "EvidencePacket.OmittedDetails.ExpansionHandles", "no expansion handles", Bunit, "AgentPacketInspectorMapperTests"),
        new(LensKind.AgentPacketInspector, "packet.structuredError", "EvidencePacket.State / Recovery (via RecoveryStateMapper)", "no error state", Unit, "AgentPacketInspectorMapperTests"),
        new(LensKind.AgentPacketInspector, "packet.toolName", "EvidencePacket.Metadata.McpSchema.ToolName", "tool/resource name unavailable", Unit, "AgentPacketInspectorMapperTests"),
    ];

    /// <summary>Gets the field-trace rows for a single lens.</summary>
    /// <param name="lens">The lens.</param>
    /// <returns>The field-trace rows for the lens, in declared order.</returns>
    public static IReadOnlyList<LensFieldTrace> For(LensKind lens)
        => Entries.Where(e => e.Lens == lens).ToArray();
}
