// <copyright file="RecoveryStateMapperGapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Recovery;

using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Recovery;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

/// <summary>
/// QA gap coverage for <see cref="RecoveryStateMapper"/>: an exhaustive state/isolation precedence and
/// side-channel-safety sweep, stale/compressed/conflict risk-marker combinations, a sanitization sweep
/// over every fixture, and a whitelisted diagnostic-clue shape check across every fixture.
/// </summary>
public sealed partial class RecoveryStateMapperGapTests
{
    // Patterns that must never appear in any rendered or copied recovery string (secrets, bearer tokens,
    // raw payloads, local absolute paths, connection strings, JWTs). Mirrors the EvidenceDisplay sanitizer.
    private static readonly string[] _sensitiveMarkers =
    [
        "Bearer ",
        "C:\\Users\\Jerome",
        "/home/jerome",
        "redis://",
        "falkor",
        "eyJ",
        "sk_live_",
        "sk_test_",
        "ghp_",
        "leaked-token",
        ".def.ghi",
    ];

    [Fact]
    public void Map_EveryStateAndIsolationCombination_IsDeterministicAndSideChannelSafe()
    {
        // Exhaustive matrix over the known contract enums (Task 5): the mapper must never throw, must
        // always produce a state with named contract sources, must collapse every restrictive isolation
        // or unauthorized state to Unauthorized, and must never leak result counts in that clue.
        foreach (EvidencePacketState state in Enum.GetValues<EvidencePacketState>())
        {
            foreach (EvidencePacketIsolationStatus isolation in Enum.GetValues<EvidencePacketIsolationStatus>())
            {
                EvidencePacket basePacket = EvidencePacketFixtures.CompletePacket();
                EvidencePacket packet = basePacket with
                {
                    State = state,
                    Scope = basePacket.Scope with { IsolationStatus = isolation },
                };

                RecoveryStateViewModel view = RecoveryStateMapper.Map(packet);
                string because = $"state={state}, isolation={isolation}";

                // Determinism: identical inputs map to the identical state kind.
                RecoveryStateMapper.Map(packet).StateKind.ShouldBe(view.StateKind, because);

                // Every emitted state traces back to named contract fields.
                RecoveryStateTrace trace = RecoveryStateTraceability.For(view.StateKind);
                view.ContractSources.ShouldBe(trace.ContractSources, because);
                view.ContractSources.ShouldNotBeEmpty(because);

                // Wrong-case is never derivable side-channel-safely, so it must never be emitted.
                view.StateKind.ShouldNotBe(RecoveryStateKind.WrongCase, because);

                bool restrictive = isolation is EvidencePacketIsolationStatus.Unauthorized
                        or EvidencePacketIsolationStatus.Unknown
                    || state == EvidencePacketState.Unauthorized;

                if (restrictive)
                {
                    view.StateKind.ShouldBe(RecoveryStateKind.Unauthorized, because);

                    // The clue must not leak any count-bearing axis that could reveal whether matching
                    // evidence exists beyond the authorization boundary.
                    view.DiagnosticClueCode.ShouldNotContain("omittedCount");
                    view.DiagnosticClueCode.ShouldNotContain("unavailableAxes");
                    view.DiagnosticClueCode.ShouldNotContain("graphGaps");

                    // Risk markers and omitted-detail hints are suppressed under restrictive scope.
                    view.RiskMarkers.ShouldBeEmpty(because);
                    view.OmittedDetailNames.ShouldBeEmpty(because);
                    view.Expansions.ShouldBeEmpty(because);
                }

                // The clue is always present and built only from whitelisted code tokens and counts.
                view.DiagnosticClueCode.ShouldNotBeNullOrWhiteSpace(because);
                ClueShapeRegex().IsMatch(view.DiagnosticClueCode)
                    .ShouldBeTrue($"Unexpected clue shape '{view.DiagnosticClueCode}' ({because}).");
            }
        }
    }

    [Fact]
    public void Map_StaleAndCompressed_KeepsStaleMemoryPrimaryWithCompressedRiskMarker()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.StaleAndCompressed());

        // Staleness owns the primary state; compression decorates it as a secondary risk marker, and the
        // omitted detail group stays visible so compression is announced as omitted, not absent.
        view.StateKind.ShouldBe(RecoveryStateKind.StaleMemory);
        view.RiskMarkers.ShouldContain(m => m.Code == "compressed");
        view.RiskMarkers.ShouldNotContain(m => m.Code == "stale");
        view.OmittedDetailNames.ShouldContain("rankedResults");
    }

    [Fact]
    public void Map_StaleDegradedWithSources_IsConflictingWithStaleRiskMarker()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.StaleDegradedWithSources());

        // A degraded backend with sources present must not look confident: conflict wins precedence while
        // staleness remains visible as a secondary risk marker.
        view.StateKind.ShouldBe(RecoveryStateKind.Conflicting);
        view.RiskMarkers.ShouldContain(m => m.Code == "stale");
    }

    [Fact]
    public void Map_EveryFixture_ProducesSanitizedViewModelWithNoSensitiveContent()
    {
        foreach (EvidencePacket packet in AllFixtures())
        {
            RecoveryStateViewModel view = RecoveryStateMapper.Map(packet);

            foreach (string value in FlattenStrings(view))
            {
                foreach (string marker in _sensitiveMarkers)
                {
                    value.ShouldNotContain(marker);
                }
            }
        }
    }

    [Fact]
    public void Map_EveryFixture_ProducesNonEmptyWhitelistedDiagnosticClue()
    {
        foreach (EvidencePacket packet in AllFixtures())
        {
            RecoveryStateViewModel view = RecoveryStateMapper.Map(packet);

            view.DiagnosticClueCode.ShouldNotBeNullOrWhiteSpace($"state {view.StateKind} produced an empty clue.");
            ClueShapeRegex().IsMatch(view.DiagnosticClueCode)
                .ShouldBeTrue($"Unexpected clue shape '{view.DiagnosticClueCode}' for state {view.StateKind}.");
        }
    }

    private static IEnumerable<string> FlattenStrings(RecoveryStateViewModel view)
    {
        yield return view.DiagnosticClueCode;
        yield return view.TenantId;
        if (view.CaseId is not null)
        {
            yield return view.CaseId;
        }

        foreach (string name in view.OmittedDetailNames)
        {
            yield return name;
        }

        foreach (RecoveryExpansionView expansion in view.Expansions)
        {
            yield return expansion.TargetDetailGroup;
            yield return expansion.Guidance;
        }

        foreach (RecoveryActionView action in Actions(view))
        {
            yield return action.Label;
            yield return action.Guidance;
            yield return action.Target;
        }
    }

    private static IEnumerable<RecoveryActionView> Actions(RecoveryStateViewModel view)
    {
        if (view.PrimaryAction is not null)
        {
            yield return view.PrimaryAction;
        }

        foreach (RecoveryActionView action in view.SecondaryActions)
        {
            yield return action;
        }
    }

    private static IEnumerable<EvidencePacket> AllFixtures()
    {
        yield return RecoveryPacketFixtures.Supported();
        yield return RecoveryPacketFixtures.Weak();
        yield return RecoveryPacketFixtures.StaleMemory();
        yield return RecoveryPacketFixtures.DegradedBackendWithSources();
        yield return RecoveryPacketFixtures.DegradedBackendNoSources();
        yield return RecoveryPacketFixtures.Unauthorized();
        yield return RecoveryPacketFixtures.UnknownScope();
        yield return RecoveryPacketFixtures.Compressed();
        yield return RecoveryPacketFixtures.ConflictingViaDegraded();
        yield return RecoveryPacketFixtures.ConflictingViaUnavailableAxes();
        yield return RecoveryPacketFixtures.NoMatch();
        yield return RecoveryPacketFixtures.NotIngestedYet();
        yield return RecoveryPacketFixtures.GraphGapNoSources();
        yield return RecoveryPacketFixtures.InsufficientFromPartial();
        yield return RecoveryPacketFixtures.InsufficientNoSignal();
        yield return RecoveryPacketFixtures.UnknownState();
        yield return RecoveryPacketFixtures.WeakAndCompressed();
        yield return RecoveryPacketFixtures.StaleAndCompressed();
        yield return RecoveryPacketFixtures.StaleDegradedWithSources();
        yield return RecoveryPacketFixtures.MultiActionNoMatch();
        yield return RecoveryPacketFixtures.UnauthorizedWithExpandingActions();
        yield return RecoveryPacketFixtures.UnauthorizedHighCount();
        yield return RecoveryPacketFixtures.UnauthorizedZeroCount();
        yield return RecoveryPacketFixtures.SensitiveRecoveryAction();
        yield return RecoveryPacketFixtures.SensitiveScopeRecovery();
        yield return RecoveryPacketFixtures.MalformedButSafe();
    }

    [GeneratedRegex("^[a-zA-Z0-9]+=[a-zA-Z0-9]+(; [a-zA-Z0-9]+=[a-zA-Z0-9]+)*$")]
    private static partial Regex ClueShapeRegex();
}
