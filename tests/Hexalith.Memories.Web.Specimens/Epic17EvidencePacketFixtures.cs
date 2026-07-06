// <copyright file="Epic17EvidencePacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Specimens;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Shared Story 17 Evidence Packet fixtures consumed by bUnit tests and browser specimens.
/// </summary>
public static class Epic17EvidencePacketFixtures
{
    private static readonly DateTimeOffset ProducedAt = new(2026, 7, 5, 6, 58, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LastCheckedAt = new(2026, 7, 5, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SourceTimestamp = new(2026, 7, 5, 6, 55, 0, TimeSpan.Zero);

    public static EvidencePacket CompletePacket()
        => Packet(
            EvidencePacketState.Complete,
            [Source(1, "memory-a", "https://docs.example/source-a", 0.91d)],
            [Axis("semantic", 0.91d, "cosine", "semantic vector match")]);

    public static EvidencePacket CompressedPacket()
        => CompletePacket() with
        {
            State = EvidencePacketState.PendingExpansion,
            OmittedDetails = new EvidencePacketOmittedDetails(
                OmittedCount: 3,
                EstimatedTokensTotal: 1200,
                Reason: EvidencePacketOmissionReason.TokenBudget,
                FieldNames: ["sources"],
                DetailGroups: ["rankedResults"],
                ExpansionHandles:
                [
                    new EvidencePacketExpansionHandle(
                        "ep:v1:abc:rankedResults",
                        EvidencePacketRecoveryKind.IncreaseTokenBudget,
                        "rankedResults",
                        "tenant-a",
                        "case-a",
                        "Re-run with a larger tokenBudget."),
                ]),
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.IncreaseTokenBudget,
                    "increaseTokenBudget",
                    "Re-run with a larger tokenBudget.",
                    "rankedResults"),
            ],
        };

    public static EvidencePacket UnauthorizedPacket()
        => Packet(
            EvidencePacketState.Unauthorized,
            // Producer left sources populated. UI must scrub them — the test relies on this
            // fixture preserving "memory-secret" so a missing scope guard fails the test.
            [Source(1, "memory-secret", "https://docs.example/restricted", 0.99d)],
            [Axis("semantic", 0.91d, "cosine", "secret-axis-evidence")]) with
        {
            Scope = new EvidencePacketScope(
                TenantId: "tenant-a",
                CaseId: "case-a",
                IsolationStatus: EvidencePacketIsolationStatus.Unauthorized,
                PermissionsContext: "tenant-case"),
            Result = new EvidencePacketResultSummary(
                Query: "find policy context",
                TotalCount: 0,
                ReturnedCount: 0,
                HasIndexedMemoryUnits: true,
                Summary: null),
            Graph = new EvidencePacketGraphSummary(
                Available: true,
                RelatedPath: ["memory-secret-a", "memory-secret-b"],
                EdgeTypes: ["secret-supports"],
                GapMarkers: ["secret-gap"]),
            OmittedDetails = new EvidencePacketOmittedDetails(
                OmittedCount: 0,
                EstimatedTokensTotal: 0,
                Reason: EvidencePacketOmissionReason.Authorization,
                FieldNames: ["sources", "evidence"],
                DetailGroups: ["authorization"],
                ExpansionHandles: []),
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.CheckAuthorization,
                    "checkAuthorization",
                    "Use an authorized tenant and case scope.",
                    "auth"),
            ],
        };

    public static EvidencePacket MultiSourcePacket()
        => Packet(
            EvidencePacketState.Complete,
            [
                Source(1, "memory-a", "https://docs.example/source-a", 0.81d),
                Source(2, "memory-b", "https://docs.example/source-b", 0.79d),
            ],
            [
                Axis("semantic", 0.81d, "cosine", "semantic vector match"),
                Axis("syntactic", 0.79d, "bm25", "term match"),
            ]);

    public static EvidencePacket SensitivePacket()
        => Packet(
            EvidencePacketState.Complete,
            [
                Source(1, "memory-a", "C:\\Users\\Jerome\\secret.txt", 0.81d)
                    with { Snippet = "Bearer abc.def.ghi redis://localhost:6379 /home/jerome/file" },
            ],
            [Axis("semantic", 0.81d, "cosine", "semantic vector match")]);

    public static EvidencePacket TenantCaseSensitivePacket()
        => Packet(
            EvidencePacketState.Complete,
            [Source(1, "memory-a", "https://docs.example/source-a", 0.81d)],
            [Axis("semantic", 0.81d, "cosine", "semantic vector match")]) with
        {
            Scope = new EvidencePacketScope(
                TenantId: "tenant Bearer leaked-token",
                CaseId: "C:\\Users\\Jerome\\case.txt",
                IsolationStatus: EvidencePacketIsolationStatus.Authorized,
                PermissionsContext: "tenant-case"),
        };

    public static EvidencePacket EmptyPacket()
        => Packet(
            EvidencePacketState.Empty,
            [],
            []) with
        {
            Result = new EvidencePacketResultSummary(
                Query: "find policy context",
                TotalCount: 0,
                ReturnedCount: 0,
                HasIndexedMemoryUnits: true,
                Summary: null),
            Evidence = new EvidencePacketEvidence(
                EvidenceStrength: EvidencePacketEvidenceStrength.None,
                Caveat: "Scores measure query-result relevance, not factual accuracy.",
                AxesUsed: [],
                UnavailableAxes: [],
                Degraded: false,
                AllEnabledAxesUnavailable: null,
                AxisEvidence: []),
            Graph = new EvidencePacketGraphSummary(
                Available: false,
                RelatedPath: [],
                EdgeTypes: [],
                GapMarkers: []),
        };

    public static EvidencePacket DegradedPacket()
        => CompletePacket() with
        {
            State = EvidencePacketState.Degraded,
            Evidence = new EvidencePacketEvidence(
                EvidenceStrength: EvidencePacketEvidenceStrength.Weak,
                Caveat: "Backend degraded; partial evidence only.",
                AxesUsed: ["semantic"],
                UnavailableAxes: ["graph"],
                Degraded: true,
                AllEnabledAxesUnavailable: false,
                AxisEvidence:
                [
                    new EvidencePacketAxisEvidence("semantic", 0.6d, "cosine", "degraded semantic axis"),
                ]),
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.InspectBackendHealth,
                    "inspectBackendHealth",
                    "Inspect retrieval backend health and retry.",
                    "health"),
            ],
        };

    public static EvidencePacket PartialPacket()
        => CompletePacket() with
        {
            State = EvidencePacketState.Partial,
            Evidence = new EvidencePacketEvidence(
                EvidenceStrength: EvidencePacketEvidenceStrength.Moderate,
                Caveat: "Some axes returned partial scores.",
                AxesUsed: ["semantic"],
                UnavailableAxes: ["syntactic"],
                Degraded: false,
                AllEnabledAxesUnavailable: false,
                AxisEvidence:
                [
                    new EvidencePacketAxisEvidence("semantic", 0.72d, "cosine", "partial semantic axis"),
                ]),
        };

    public static EvidencePacket WeakPacket()
        => CompletePacket() with
        {
            State = EvidencePacketState.Weak,
            Evidence = new EvidencePacketEvidence(
                EvidenceStrength: EvidencePacketEvidenceStrength.Weak,
                Caveat: "Weak evidence; verify with additional sources.",
                AxesUsed: ["semantic"],
                UnavailableAxes: [],
                Degraded: false,
                AllEnabledAxesUnavailable: false,
                AxisEvidence:
                [
                    new EvidencePacketAxisEvidence("semantic", 0.42d, "cosine", "weak semantic axis"),
                ]),
        };

    public static EvidencePacket StalePacket()
        => CompletePacket() with
        {
            State = EvidencePacketState.Stale,
            Evidence = new EvidencePacketEvidence(
                EvidenceStrength: EvidencePacketEvidenceStrength.Moderate,
                Caveat: "Evidence may be stale.",
                AxesUsed: ["semantic"],
                UnavailableAxes: [],
                Degraded: false,
                AllEnabledAxesUnavailable: false,
                AxisEvidence:
                [
                    new EvidencePacketAxisEvidence("semantic", 0.68d, "cosine", "stale semantic axis"),
                ]),
        };

    public static EvidencePacket RedactedPacket()
        => CompletePacket() with
        {
            State = EvidencePacketState.Complete,
            OmittedDetails = new EvidencePacketOmittedDetails(
                OmittedCount: 1,
                EstimatedTokensTotal: 0,
                Reason: EvidencePacketOmissionReason.Redaction,
                FieldNames: ["snippet"],
                DetailGroups: ["redactedContent"],
                ExpansionHandles: []),
        };

    public static EvidencePacket UnknownScopePacket()
        => Packet(
            EvidencePacketState.Complete,
            [Source(1, "memory-a", "https://docs.example/source-a", 0.81d)],
            [Axis("semantic", 0.81d, "cosine", "semantic vector match")]) with
        {
            Scope = new EvidencePacketScope(
                TenantId: "tenant-a",
                CaseId: "case-a",
                IsolationStatus: EvidencePacketIsolationStatus.Unknown,
                PermissionsContext: "tenant-case"),
        };

    private static EvidencePacket Packet(
        EvidencePacketState state,
        IReadOnlyList<EvidencePacketSource> sources,
        IReadOnlyList<EvidencePacketAxisEvidence> axisEvidence)
        => new(
            Scope: new EvidencePacketScope(
                TenantId: "tenant-a",
                CaseId: "case-a",
                IsolationStatus: EvidencePacketIsolationStatus.Authorized,
                PermissionsContext: "tenant-case"),
            Result: new EvidencePacketResultSummary(
                Query: "find policy context",
                TotalCount: sources.Count,
                ReturnedCount: sources.Count,
                HasIndexedMemoryUnits: true,
                Summary: "Answer summary"),
            Sources: sources,
            Evidence: new EvidencePacketEvidence(
                EvidenceStrength: EvidencePacketEvidenceStrength.Strong,
                Caveat: "Scores measure query-result relevance, not factual accuracy or data completeness.",
                AxesUsed: axisEvidence.Select(static x => x.Axis).ToArray(),
                UnavailableAxes: [],
                Degraded: false,
                AllEnabledAxesUnavailable: null,
                AxisEvidence: axisEvidence),
            Graph: new EvidencePacketGraphSummary(
                Available: true,
                RelatedPath: ["memory-a", "memory-b"],
                EdgeTypes: ["supports"],
                GapMarkers: ["missing-parent"]),
            State: state,
            OmittedDetails: new EvidencePacketOmittedDetails(
                OmittedCount: 0,
                EstimatedTokensTotal: 0,
                Reason: EvidencePacketOmissionReason.None,
                FieldNames: [],
                DetailGroups: [],
                ExpansionHandles: []),
            Recovery: [],
            Metadata: new EvidencePacketMetadata(
                Freshness: Freshness(),
                Benchmark: new EvidencePacketBenchmarkEvidence(
                    HybridNdcg10: 0.875d,
                    SyntacticNdcg10: 0.625d,
                    SemanticNdcg10: 0.75d,
                    GraphNdcg10: 0.5d,
                    Threshold: 0.8d,
                    ThresholdPassed: true,
                    CorpusId: "synthetic-corpus-v1",
                    RunId: "benchmark-run-2026-07-05",
                    RunAt: LastCheckedAt,
                    PerQuery:
                    [
                        new EvidencePacketBenchmarkQuery(
                            QueryId: "q-claim-denied",
                            HybridNdcg10: 0.91d,
                            BestSingleAxisNdcg10: 0.72d,
                            ThresholdPassed: true),
                    ],
                    EvidenceUri: "docs://benchmarks/benchmark-run-2026-07-05"),
                McpSchema: new EvidencePacketMcpSchema(
                    ToolName: "search_memory",
                    SchemaName: "memories.search_memory.result",
                    SchemaVersion: "v1",
                    Transport: "streamable-http")));

    private static EvidencePacketSource Source(int rank, string memoryUnitId, string sourceUri, double score)
        => new(
            Rank: rank,
            MemoryUnitId: memoryUnitId,
            SourceUri: sourceUri,
            SourceType: SourceType.File,
            Snippet: "Relevant source snippet",
            Score: score,
            CaseId: "case-a",
            CaseName: "Case A",
            AnnotationsCount: 0,
            Timestamp: SourceTimestamp.AddMinutes(rank),
            Freshness: Freshness(),
            Ingestion: new EvidencePacketIngestionMetadata(
                Stage: EvidencePacketIngestionStage.Completed,
                StageDetail: "indexed",
                UpdatedAt: SourceTimestamp.AddMinutes(rank),
                RetryCount: 0));

    private static EvidencePacketAxisEvidence Axis(string axis, double score, string method, string description)
        => new(axis, score, method, description);

    private static EvidencePacketFreshness Freshness()
        => new(
            EvidencePacketFreshnessState.Current,
            ProducedAt: ProducedAt,
            LastCheckedAt: LastCheckedAt,
            ExpiresAt: LastCheckedAt.AddMinutes(15),
            AgeSeconds: 120);
}
