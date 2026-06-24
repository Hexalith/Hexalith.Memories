// <copyright file="RecoveryPacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Recovery;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Tests.Components.Evidence;

/// <summary>
/// Story 17.2 — recovery-specific Evidence Packet fixtures built on the canonical Story 2.7-aligned
/// fixtures from Story 17.1. Each fixture targets one derived <c>RecoveryStateKind</c> or precedence path.
/// </summary>
internal static class RecoveryPacketFixtures
{
    /// <summary>A confident, complete answer that needs no recovery action.</summary>
    public static EvidencePacket Supported() => EvidencePacketFixtures.CompletePacket();

    /// <summary>An answer exists but backend health is degraded, so the answer is conflicting.</summary>
    public static EvidencePacket ConflictingViaDegraded() => EvidencePacketFixtures.DegradedPacket();

    /// <summary>A complete answer with an unavailable axis must not render as confident.</summary>
    public static EvidencePacket ConflictingViaUnavailableAxes()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        return packet with
        {
            Evidence = packet.Evidence with { UnavailableAxes = ["graph"], Degraded = false },
        };
    }

    /// <summary>Sources present, degraded state, but no backend-health flag: a degraded backend.</summary>
    public static EvidencePacket DegradedBackendWithSources()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        return packet with
        {
            State = EvidencePacketState.Degraded,
            Evidence = packet.Evidence with { Degraded = false, UnavailableAxes = [] },
        };
    }

    /// <summary>No results because retrieval backends were degraded.</summary>
    public static EvidencePacket DegradedBackendNoSources()
    {
        EvidencePacket packet = EvidencePacketFixtures.EmptyPacket();
        return packet with
        {
            Evidence = packet.Evidence with { Degraded = true },
        };
    }

    /// <summary>No results because matching knowledge is not ingested yet.</summary>
    public static EvidencePacket NotIngestedYet()
    {
        EvidencePacket packet = EvidencePacketFixtures.EmptyPacket();
        return packet with
        {
            Result = packet.Result with { HasIndexedMemoryUnits = false },
        };
    }

    /// <summary>No results, but graph context exposes gap markers.</summary>
    public static EvidencePacket GraphGapNoSources()
    {
        EvidencePacket packet = EvidencePacketFixtures.EmptyPacket();
        return packet with
        {
            Graph = packet.Graph with { Available = false, GapMarkers = ["missing-parent"] },
        };
    }

    /// <summary>Search ran in scope and matched nothing.</summary>
    public static EvidencePacket NoMatch() => EvidencePacketFixtures.EmptyPacket();

    /// <summary>Stale memory with sources present.</summary>
    public static EvidencePacket StaleMemory() => EvidencePacketFixtures.StalePacket();

    /// <summary>Weak evidence with sources present.</summary>
    public static EvidencePacket Weak() => EvidencePacketFixtures.WeakPacket();

    /// <summary>Partial evidence maps to insufficient support.</summary>
    public static EvidencePacket InsufficientFromPartial() => EvidencePacketFixtures.PartialPacket();

    /// <summary>No results and no safe signal to distinguish a cause.</summary>
    public static EvidencePacket InsufficientNoSignal()
    {
        EvidencePacket packet = EvidencePacketFixtures.EmptyPacket();
        return packet with
        {
            Result = packet.Result with { HasIndexedMemoryUnits = null },
        };
    }

    /// <summary>Token-budget compressed packet.</summary>
    public static EvidencePacket Compressed() => EvidencePacketFixtures.CompressedPacket();

    /// <summary>Unauthorized packet with a single safe recovery action.</summary>
    public static EvidencePacket Unauthorized() => EvidencePacketFixtures.UnauthorizedPacket();

    /// <summary>Unknown isolation status is treated as restrictively as unauthorized.</summary>
    public static EvidencePacket UnknownScope() => EvidencePacketFixtures.UnknownScopePacket();

    /// <summary>An out-of-range state value falls back to a safe unknown state.</summary>
    public static EvidencePacket UnknownState()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        return packet with { State = (EvidencePacketState)999 };
    }

    /// <summary>Weak evidence that is also token-budget compressed, to exercise secondary risk markers.</summary>
    public static EvidencePacket WeakAndCompressed()
    {
        EvidencePacket packet = EvidencePacketFixtures.WeakPacket();
        return packet with
        {
            OmittedDetails = new EvidencePacketOmittedDetails(
                OmittedCount: 2,
                EstimatedTokensTotal: 800,
                Reason: EvidencePacketOmissionReason.TokenBudget,
                FieldNames: ["sources"],
                DetailGroups: ["rankedResults"],
                ExpansionHandles: []),
        };
    }

    /// <summary>
    /// Stale evidence that is also token-budget compressed. The primary state stays StaleMemory while the
    /// compression remains visible as a secondary risk marker (stale/compressed combination, Task 5).
    /// </summary>
    public static EvidencePacket StaleAndCompressed()
    {
        EvidencePacket packet = EvidencePacketFixtures.StalePacket();
        return packet with
        {
            OmittedDetails = new EvidencePacketOmittedDetails(
                OmittedCount: 4,
                EstimatedTokensTotal: 900,
                Reason: EvidencePacketOmissionReason.TokenBudget,
                FieldNames: ["sources"],
                DetailGroups: ["rankedResults"],
                ExpansionHandles: []),
        };
    }

    /// <summary>
    /// Stale evidence with a degraded backend and sources present. Conflict precedence wins (the answer
    /// must not look confident) while staleness remains visible as a secondary risk marker
    /// (stale/conflict combination, Task 5).
    /// </summary>
    public static EvidencePacket StaleDegradedWithSources()
    {
        EvidencePacket packet = EvidencePacketFixtures.StalePacket();
        return packet with
        {
            Evidence = packet.Evidence with { Degraded = true },
        };
    }

    /// <summary>A no-match packet offering several actions to verify the safest one becomes primary.</summary>
    public static EvidencePacket MultiActionNoMatch()
    {
        EvidencePacket packet = EvidencePacketFixtures.EmptyPacket();
        return packet with
        {
            Recovery =
            [
                new EvidencePacketRecoveryAction(EvidencePacketRecoveryKind.BroadenScope, "broadenScope", "Broaden the case scope.", "scope"),
                new EvidencePacketRecoveryAction(EvidencePacketRecoveryKind.IncreaseMaxResults, "increaseMaxResults", "Return more results.", "results"),
                new EvidencePacketRecoveryAction(EvidencePacketRecoveryKind.FetchMemoryUnit, "fetchMemoryUnit", "Fetch a known memory unit.", "unit"),
            ],
        };
    }

    /// <summary>Unauthorized packet that also offers a scope-expanding action, which must render disabled.</summary>
    public static EvidencePacket UnauthorizedWithExpandingActions()
    {
        EvidencePacket packet = EvidencePacketFixtures.UnauthorizedPacket();
        return packet with
        {
            Recovery =
            [
                new EvidencePacketRecoveryAction(EvidencePacketRecoveryKind.BroadenScope, "broadenScope", "Broaden the case scope.", "scope"),
                new EvidencePacketRecoveryAction(EvidencePacketRecoveryKind.CheckAuthorization, "checkAuthorization", "Use an authorized tenant and case scope.", "auth"),
            ],
        };
    }

    /// <summary>Unauthorized packet with a large result count, to prove counts never leak in the clue.</summary>
    public static EvidencePacket UnauthorizedHighCount()
    {
        EvidencePacket packet = EvidencePacketFixtures.UnauthorizedPacket();
        return packet with
        {
            Result = packet.Result with { TotalCount = 999, ReturnedCount = 5, HasIndexedMemoryUnits = true },
        };
    }

    /// <summary>Unauthorized packet with no results, paired with <see cref="UnauthorizedHighCount"/>.</summary>
    public static EvidencePacket UnauthorizedZeroCount()
    {
        EvidencePacket packet = EvidencePacketFixtures.UnauthorizedPacket();
        return packet with
        {
            Result = packet.Result with { TotalCount = 0, ReturnedCount = 0, HasIndexedMemoryUnits = false },
        };
    }

    /// <summary>Compressed packet whose recovery action carries sensitive text that must be redacted.</summary>
    public static EvidencePacket SensitiveRecoveryAction()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompressedPacket();
        return packet with
        {
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.IncreaseTokenBudget,
                    "increaseTokenBudget",
                    "Retry with Bearer abc.def.ghi from C:\\Users\\Jerome\\secret.txt",
                    "/home/jerome/file"),
            ],
        };
    }

    /// <summary>Compressed packet with sensitive tenant/case identifiers to verify scope redaction.</summary>
    public static EvidencePacket SensitiveScopeRecovery()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompressedPacket();
        return packet with
        {
            Scope = new EvidencePacketScope(
                TenantId: "tenant Bearer leaked-token",
                CaseId: "C:\\Users\\Jerome\\case.txt",
                IsolationStatus: EvidencePacketIsolationStatus.Authorized,
                PermissionsContext: "tenant-case"),
        };
    }

    /// <summary>A malformed-but-safe packet: empty strings, null optionals, empty collections.</summary>
    public static EvidencePacket MalformedButSafe()
        => new(
            Scope: new EvidencePacketScope(
                TenantId: " ",
                CaseId: null,
                IsolationStatus: EvidencePacketIsolationStatus.Authorized,
                PermissionsContext: " "),
            Result: new EvidencePacketResultSummary(
                Query: string.Empty,
                TotalCount: 0,
                ReturnedCount: 0,
                HasIndexedMemoryUnits: null,
                Summary: null),
            Sources: [],
            Evidence: new EvidencePacketEvidence(
                EvidenceStrength: EvidencePacketEvidenceStrength.None,
                Caveat: string.Empty,
                AxesUsed: [],
                UnavailableAxes: [],
                Degraded: false,
                AllEnabledAxesUnavailable: null,
                AxisEvidence: []),
            Graph: new EvidencePacketGraphSummary(
                Available: false,
                RelatedPath: [],
                EdgeTypes: [],
                GapMarkers: []),
            State: EvidencePacketState.Empty,
            OmittedDetails: new EvidencePacketOmittedDetails(
                OmittedCount: 0,
                EstimatedTokensTotal: 0,
                Reason: EvidencePacketOmissionReason.None,
                FieldNames: [],
                DetailGroups: [],
                ExpansionHandles: []),
            Recovery: []);
}
