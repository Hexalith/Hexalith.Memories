// <copyright file="EvidencePacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Evidence;

using Hexalith.Memories.Contracts.V1;

internal static class EvidencePacketFixtures
{
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
                3,
                1200,
                EvidencePacketOmissionReason.TokenBudget,
                ["sources"],
                ["rankedResults"],
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
            [Source(1, "memory-secret", "https://docs.example/restricted", 0.99d)],
            []) with
        {
            Scope = new EvidencePacketScope(
                "tenant-a",
                "case-a",
                EvidencePacketIsolationStatus.Unauthorized,
                "tenant-case"),
            Sources = [],
            OmittedDetails = new EvidencePacketOmittedDetails(
                0,
                0,
                EvidencePacketOmissionReason.Authorization,
                ["sources", "evidence"],
                ["authorization"],
                []),
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
                Source(1, "memory-a", "C:\\Users\\Jerome\\secret.txt", 0.81d) with { Snippet = "Bearer abc.def.ghi redis://localhost:6379 /home/jerome/file" },
            ],
            [Axis("semantic", 0.81d, "cosine", "semantic vector match")]);

    private static EvidencePacket Packet(
        EvidencePacketState state,
        IReadOnlyList<EvidencePacketSource> sources,
        IReadOnlyList<EvidencePacketAxisEvidence> axisEvidence)
        => new(
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"),
            new EvidencePacketResultSummary("find policy context", sources.Count, sources.Count, true, "Answer summary"),
            sources,
            new EvidencePacketEvidence(
                EvidencePacketEvidenceStrength.Strong,
                "Scores measure query-result relevance, not factual accuracy or data completeness.",
                axisEvidence.Select(static x => x.Axis).ToArray(),
                [],
                false,
                null,
                axisEvidence),
            new EvidencePacketGraphSummary(
                true,
                ["memory-a", "memory-b"],
                ["supports"],
                ["missing-parent"]),
            state,
            new EvidencePacketOmittedDetails(0, 0, EvidencePacketOmissionReason.None, [], [], []),
            []);

    private static EvidencePacketSource Source(int rank, string memoryUnitId, string sourceUri, double score)
        => new(
            rank,
            memoryUnitId,
            sourceUri,
            SourceType.File,
            "Relevant source snippet",
            score,
            "case-a",
            "Case A",
            0);

    private static EvidencePacketAxisEvidence Axis(string axis, double score, string method, string description)
        => new(axis, score, method, description);
}
