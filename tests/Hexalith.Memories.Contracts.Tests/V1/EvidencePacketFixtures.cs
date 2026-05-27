// <copyright file="EvidencePacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using Hexalith.Memories.Contracts.V1;

internal static class EvidencePacketFixtures
{
    public static EvidencePacket Complete()
        => new(
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"),
            new EvidencePacketResultSummary("claim denied", 1, 1, true, null),
            [
                new EvidencePacketSource(
                    1,
                    "mu-001",
                    "mem://tenant-a/case-a/mu-001",
                    SourceType.File,
                    "The claim was denied...",
                    0.91,
                    "case-a",
                    "Case A",
                    0),
            ],
            new EvidencePacketEvidence(
                EvidencePacketEvidenceStrength.Strong,
                "Scores measure relevance, not factual accuracy.",
                ["semantic"],
                [],
                false,
                null,
                [new EvidencePacketAxisEvidence("semantic", 0.91, "cosine", "cosine similarity")]),
            new EvidencePacketGraphSummary(false, [], [], []),
            EvidencePacketState.Complete,
            new EvidencePacketOmittedDetails(0, 0, EvidencePacketOmissionReason.None, [], [], []),
            []);

    public static EvidencePacket Degraded()
        => Complete() with
        {
            Evidence = Complete().Evidence with
            {
                Degraded = true,
                UnavailableAxes = ["graph"],
            },
            State = EvidencePacketState.Degraded,
            OmittedDetails = new EvidencePacketOmittedDetails(
                0,
                0,
                EvidencePacketOmissionReason.BackendUnavailable,
                ["evidence.unavailableAxes"],
                ["backendDiagnostics"],
                []),
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.Retry,
                    "retry",
                    "Retry after the unavailable axis recovers.",
                    "search"),
            ],
        };

    public static EvidencePacket Empty()
        => Complete() with
        {
            Result = new EvidencePacketResultSummary("claim denied", 0, 0, true, null),
            Sources = [],
            Evidence = Complete().Evidence with { EvidenceStrength = EvidencePacketEvidenceStrength.None, AxisEvidence = [] },
            State = EvidencePacketState.Empty,
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.BroadenScope,
                    "broadenScope",
                    "Retry with a broader query or without a case filter.",
                    "search"),
            ],
        };

    public static EvidencePacket Unauthorized()
        => Complete() with
        {
            Scope = new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Unauthorized, "tenant-case"),
            Result = new EvidencePacketResultSummary("claim denied", 0, 0, null, null),
            Sources = [],
            State = EvidencePacketState.Unauthorized,
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

    public static EvidencePacket TokenBudgetCompressed()
        => Complete() with
        {
            State = EvidencePacketState.PendingExpansion,
            OmittedDetails = new EvidencePacketOmittedDetails(
                3,
                1_024,
                EvidencePacketOmissionReason.TokenBudget,
                ["sources"],
                ["rankedResults"],
                [
                    new EvidencePacketExpansionHandle(
                        "ep:v1:8F9E2A1D:rankedResults",
                        EvidencePacketRecoveryKind.IncreaseTokenBudget,
                        "rankedResults",
                        "tenant-a",
                        "case-a",
                        "Re-run the authorized search with a larger tokenBudget."),
                ]),
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.IncreaseTokenBudget,
                    "increaseTokenBudget",
                    "Re-run the authorized search with a larger tokenBudget.",
                    "rankedResults"),
            ],
        };
}
