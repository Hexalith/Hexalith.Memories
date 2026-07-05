// <copyright file="EvidencePacket.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Canonical cross-surface evidence packet shared by CLI, MCP, diagnostics, and UI consumers.</summary>
/// <param name="Scope">Tenant and case boundary for the packet.</param>
/// <param name="Result">Query-result summary.</param>
/// <param name="Sources">Ranked source evidence exposed in the current response budget.</param>
/// <param name="Evidence">Evidence strength, caveat, and retrieval-axis metadata.</param>
/// <param name="Graph">Graph traversal summary when graph evidence is available.</param>
/// <param name="State">Trust and availability state for the packet.</param>
/// <param name="OmittedDetails">Details omitted from the current response and how to retrieve them.</param>
/// <param name="Recovery">Safe next actions for the caller.</param>
/// <param name="Metadata">Optional cross-surface metadata for freshness, benchmark, and MCP schema consumers.</param>
public sealed record EvidencePacket(
    EvidencePacketScope Scope,
    EvidencePacketResultSummary Result,
    IReadOnlyList<EvidencePacketSource> Sources,
    EvidencePacketEvidence Evidence,
    EvidencePacketGraphSummary Graph,
    EvidencePacketState State,
    EvidencePacketOmittedDetails OmittedDetails,
    IReadOnlyList<EvidencePacketRecoveryAction> Recovery,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacketMetadata? Metadata = null);

/// <summary>Tenant and case boundary for an evidence packet.</summary>
/// <param name="TenantId">Requested tenant identifier.</param>
/// <param name="CaseId">Requested case identifier, or null for tenant-wide scope.</param>
/// <param name="IsolationStatus">Authorization and isolation status of the scope.</param>
/// <param name="PermissionsContext">Machine-readable description of the permission context used to compose the packet.</param>
public sealed record EvidencePacketScope(
    string TenantId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CaseId,
    EvidencePacketIsolationStatus IsolationStatus,
    string PermissionsContext);

/// <summary>Query-result summary for an evidence packet.</summary>
/// <param name="Query">Original query string, or an empty string when no query applies.</param>
/// <param name="TotalCount">Total matching item count reported by the lower-level response.</param>
/// <param name="ReturnedCount">Number of ranked sources included in this packet.</param>
/// <param name="HasIndexedMemoryUnits">Whether the tenant had indexed memory units, when known.</param>
/// <param name="Summary">Optional bounded textual summary. Null when no summary was produced.</param>
public sealed record EvidencePacketResultSummary(
    string Query,
    long TotalCount,
    int ReturnedCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? HasIndexedMemoryUnits,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Summary);

/// <summary>Single ranked source included in an evidence packet.</summary>
/// <param name="Rank">One-based source rank in the packet.</param>
/// <param name="MemoryUnitId">Memory unit identifier.</param>
/// <param name="SourceUri">Source URI from the lower-level search result.</param>
/// <param name="SourceType">Source type from the lower-level search result.</param>
/// <param name="Snippet">Bounded source snippet.</param>
/// <param name="Score">Relevance score for the source, when known.</param>
/// <param name="CaseId">Case identifier for the memory unit, when known.</param>
/// <param name="CaseName">Case display name for the memory unit, when known.</param>
/// <param name="AnnotationsCount">Number of annotations linked to the memory unit.</param>
/// <param name="Timestamp">Chronological activity timestamp for this source, when the producer can justify one.</param>
/// <param name="Freshness">Freshness metadata for this source, when known.</param>
/// <param name="Ingestion">Ingestion lifecycle metadata for this source, when known.</param>
public sealed record EvidencePacketSource(
    int Rank,
    string MemoryUnitId,
    string SourceUri,
    SourceType SourceType,
    string Snippet,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Score,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CaseId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CaseName,
    int AnnotationsCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? Timestamp = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacketFreshness? Freshness = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacketIngestionMetadata? Ingestion = null);

/// <summary>Evidence-strength and retrieval-axis metadata for a packet.</summary>
/// <param name="EvidenceStrength">Strength of query-result evidence, not factual accuracy.</param>
/// <param name="Caveat">Neutral caveat explaining that scores represent retrieval relevance.</param>
/// <param name="AxesUsed">Retrieval axes that contributed to the packet.</param>
/// <param name="UnavailableAxes">Axes that were unavailable or degraded.</param>
/// <param name="Degraded">Whether expected retrieval infrastructure was degraded.</param>
/// <param name="AllEnabledAxesUnavailable">Whether all enabled axes were unavailable, when known.</param>
/// <param name="AxisEvidence">Per-axis score and explanation summary.</param>
public sealed record EvidencePacketEvidence(
    EvidencePacketEvidenceStrength EvidenceStrength,
    string Caveat,
    IReadOnlyList<string> AxesUsed,
    IReadOnlyList<string> UnavailableAxes,
    bool Degraded,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? AllEnabledAxesUnavailable,
    IReadOnlyList<EvidencePacketAxisEvidence> AxisEvidence);

/// <summary>Per-axis evidence metadata.</summary>
/// <param name="Axis">Axis name, such as syntactic, semantic, or graph.</param>
/// <param name="Score">Best score observed for this axis, when known.</param>
/// <param name="NormalizationMethod">Normalization method reported by explain metadata, when known.</param>
/// <param name="Description">Explain metadata description, when known.</param>
public sealed record EvidencePacketAxisEvidence(
    string Axis,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Score,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NormalizationMethod,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description);

/// <summary>Graph evidence summary.</summary>
/// <param name="Available">Whether graph evidence is available in this packet.</param>
/// <param name="RelatedPath">Related memory-unit path identifiers, when available.</param>
/// <param name="EdgeTypes">Graph edge types included in the summary.</param>
/// <param name="GapMarkers">Graph gap markers included in the summary.</param>
public sealed record EvidencePacketGraphSummary(
    bool Available,
    IReadOnlyList<string> RelatedPath,
    IReadOnlyList<string> EdgeTypes,
    IReadOnlyList<string> GapMarkers);

/// <summary>Details omitted from an evidence packet and deterministic expansion guidance.</summary>
/// <param name="OmittedCount">Count of omitted results or detail items, when known.</param>
/// <param name="EstimatedTokensTotal">Estimated token count before response-budget compression, when known.</param>
/// <param name="Reason">Machine-readable omission reason.</param>
/// <param name="FieldNames">Packet field names with omitted details.</param>
/// <param name="DetailGroups">Machine-readable detail groups with omitted data.</param>
/// <param name="ExpansionHandles">Scoped expansion handles or equivalent retrieval guidance.</param>
public sealed record EvidencePacketOmittedDetails(
    int OmittedCount,
    long EstimatedTokensTotal,
    EvidencePacketOmissionReason Reason,
    IReadOnlyList<string> FieldNames,
    IReadOnlyList<string> DetailGroups,
    IReadOnlyList<EvidencePacketExpansionHandle> ExpansionHandles);

/// <summary>Scoped deterministic handle or retrieval guidance for omitted detail groups.</summary>
/// <param name="Handle">Opaque deterministic handle safe to serialize.</param>
/// <param name="Kind">Recovery action kind that can expand the detail group.</param>
/// <param name="TargetDetailGroup">Machine-readable detail group targeted by the handle.</param>
/// <param name="TenantId">Tenant scope for the handle.</param>
/// <param name="CaseId">Case scope for the handle, or null for tenant-wide scope.</param>
/// <param name="Guidance">Safe caller guidance for using the handle.</param>
public sealed record EvidencePacketExpansionHandle(
    string Handle,
    EvidencePacketRecoveryKind Kind,
    string TargetDetailGroup,
    string TenantId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CaseId,
    string Guidance);

/// <summary>Safe next action that does not bypass authorization or leak hidden scope existence.</summary>
/// <param name="Kind">Machine-readable recovery action kind.</param>
/// <param name="Label">Stable short label for the action.</param>
/// <param name="Guidance">Safe human-readable guidance.</param>
/// <param name="Target">Machine-readable target detail group or surface.</param>
public sealed record EvidencePacketRecoveryAction(
    EvidencePacketRecoveryKind Kind,
    string Label,
    string Guidance,
    string Target);

/// <summary>Authorization and isolation status of an evidence packet scope.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EvidencePacketIsolationStatus>))]
public enum EvidencePacketIsolationStatus
{
    /// <summary>The scope is authorized and tenant isolated.</summary>
    Authorized = 0,

    /// <summary>The scope authorization status is unknown to the packet producer.</summary>
    Unknown,

    /// <summary>The caller is not authorized for the requested scope.</summary>
    Unauthorized,
}

/// <summary>State describing evidence availability and trust semantics.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EvidencePacketState>))]
public enum EvidencePacketState
{
    /// <summary>The packet is complete for the current request and response budget.</summary>
    Complete = 0,

    /// <summary>The packet contains partial evidence.</summary>
    Partial,

    /// <summary>The packet contains weak evidence.</summary>
    Weak,

    /// <summary>The packet found no evidence in the authorized scope.</summary>
    Empty,

    /// <summary>The packet may be stale.</summary>
    Stale,

    /// <summary>The packet was produced while a backend or axis was degraded.</summary>
    Degraded,

    /// <summary>The caller is not authorized for the requested scope.</summary>
    Unauthorized,

    /// <summary>The packet omitted details that can be expanded through authorized retrieval.</summary>
    PendingExpansion,
}

/// <summary>Evidence strength for query-result relevance, not factual truth.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EvidencePacketEvidenceStrength>))]
public enum EvidencePacketEvidenceStrength
{
    /// <summary>No evidence was available.</summary>
    None = 0,

    /// <summary>Evidence strength cannot be inferred from available scores.</summary>
    Unknown,

    /// <summary>Evidence is weak.</summary>
    Weak,

    /// <summary>Evidence is moderate.</summary>
    Moderate,

    /// <summary>Evidence is strong.</summary>
    Strong,
}

/// <summary>Reason packet details were omitted.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EvidencePacketOmissionReason>))]
public enum EvidencePacketOmissionReason
{
    /// <summary>No details were omitted.</summary>
    None = 0,

    /// <summary>Details were omitted because of a token budget.</summary>
    TokenBudget,

    /// <summary>Details were omitted because of response-density limits.</summary>
    Density,

    /// <summary>Details were omitted because they were redacted.</summary>
    Redaction,

    /// <summary>Details were omitted by policy.</summary>
    Policy,

    /// <summary>Details were omitted because the caller lacks authorization.</summary>
    Authorization,

    /// <summary>Details were omitted because a backend or axis was unavailable.</summary>
    BackendUnavailable,

    /// <summary>Details are absent in the authorized data, rather than omitted.</summary>
    TrueAbsence,

    /// <summary>Multiple omission reasons apply.</summary>
    Combined,
}

/// <summary>Machine-readable recovery action kind.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EvidencePacketRecoveryKind>))]
public enum EvidencePacketRecoveryKind
{
    /// <summary>No recovery action is needed.</summary>
    None = 0,

    /// <summary>Retry the same authorized request.</summary>
    Retry,

    /// <summary>Increase the token budget for the authorized request.</summary>
    IncreaseTokenBudget,

    /// <summary>Increase the maximum result count for the authorized request.</summary>
    IncreaseMaxResults,

    /// <summary>Fetch a memory unit by its authorized identifier.</summary>
    FetchMemoryUnit,

    /// <summary>Broaden the authorized query or case scope.</summary>
    BroadenScope,

    /// <summary>Check tenant or case authorization.</summary>
    CheckAuthorization,

    /// <summary>Inspect backend health or retry after recovery.</summary>
    InspectBackendHealth,

    /// <summary>Use graph traversal or a related authorized traversal command.</summary>
    UseTraversal,
}

