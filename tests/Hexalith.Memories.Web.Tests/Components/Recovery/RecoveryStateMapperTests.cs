// <copyright file="RecoveryStateMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Recovery;

using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Recovery;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

public sealed partial class RecoveryStateMapperTests
{
    public static TheoryData<string, RecoveryStateKind> StateMatrix() => new()
    {
        { nameof(RecoveryPacketFixtures.Supported), RecoveryStateKind.Supported },
        { nameof(RecoveryPacketFixtures.Weak), RecoveryStateKind.Weak },
        { nameof(RecoveryPacketFixtures.StaleMemory), RecoveryStateKind.StaleMemory },
        { nameof(RecoveryPacketFixtures.DegradedBackendWithSources), RecoveryStateKind.DegradedBackend },
        { nameof(RecoveryPacketFixtures.DegradedBackendNoSources), RecoveryStateKind.DegradedBackend },
        { nameof(RecoveryPacketFixtures.Unauthorized), RecoveryStateKind.Unauthorized },
        { nameof(RecoveryPacketFixtures.UnknownScope), RecoveryStateKind.Unauthorized },
        { nameof(RecoveryPacketFixtures.Compressed), RecoveryStateKind.Compressed },
        { nameof(RecoveryPacketFixtures.ConflictingViaDegraded), RecoveryStateKind.Conflicting },
        { nameof(RecoveryPacketFixtures.ConflictingViaUnavailableAxes), RecoveryStateKind.Conflicting },
        { nameof(RecoveryPacketFixtures.NoMatch), RecoveryStateKind.NoMatch },
        { nameof(RecoveryPacketFixtures.NotIngestedYet), RecoveryStateKind.NotIngestedYet },
        { nameof(RecoveryPacketFixtures.GraphGapNoSources), RecoveryStateKind.GraphGap },
        { nameof(RecoveryPacketFixtures.InsufficientFromPartial), RecoveryStateKind.InsufficientEvidence },
        { nameof(RecoveryPacketFixtures.InsufficientNoSignal), RecoveryStateKind.InsufficientEvidence },
        { nameof(RecoveryPacketFixtures.UnknownState), RecoveryStateKind.Unknown },
    };

    [Theory]
    [MemberData(nameof(StateMatrix))]
    public void Map_Packet_ProducesExpectedStateKind(string fixtureName, RecoveryStateKind expected)
    {
        EvidencePacket packet = Resolve(fixtureName);

        RecoveryStateMapper.Map(packet).StateKind.ShouldBe(expected);
    }

    [Fact]
    public void Map_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => RecoveryStateMapper.Map(null!));

    [Fact]
    public void Map_NeverEmitsWrongCase_BecauseNoSideChannelSafeSignalExists()
    {
        // Wrong-case cannot be derived without revealing cross-case existence, so the mapper must never
        // produce it. Sweep every fixture and assert the value is unreachable.
        foreach (EvidencePacket packet in AllFixtures())
        {
            RecoveryStateMapper.Map(packet).StateKind.ShouldNotBe(RecoveryStateKind.WrongCase);
        }
    }

    [Fact]
    public void Map_UnauthorizedScope_IgnoresResultCounts_NoSideChannel()
    {
        RecoveryStateViewModel high = RecoveryStateMapper.Map(RecoveryPacketFixtures.UnauthorizedHighCount());
        RecoveryStateViewModel low = RecoveryStateMapper.Map(RecoveryPacketFixtures.UnauthorizedZeroCount());

        high.StateKind.ShouldBe(RecoveryStateKind.Unauthorized);
        low.StateKind.ShouldBe(RecoveryStateKind.Unauthorized);

        // The diagnostic clue must be identical regardless of result counts, so it cannot signal whether
        // matching evidence exists beyond the authorization boundary.
        high.DiagnosticClueCode.ShouldBe(low.DiagnosticClueCode);
        high.DiagnosticClueCode.ShouldNotContain("999");
        high.DiagnosticClueCode.ShouldNotContain("omittedCount");
        high.DiagnosticClueCode.ShouldNotContain("unavailableAxes");
    }

    [Theory]
    [InlineData(nameof(RecoveryPacketFixtures.ConflictingViaDegraded))]
    [InlineData(nameof(RecoveryPacketFixtures.ConflictingViaUnavailableAxes))]
    [InlineData(nameof(RecoveryPacketFixtures.InsufficientFromPartial))]
    public void Map_DisagreementSignalsWithSources_NeverRenderConfidentAnswer(string fixtureName)
    {
        // AC3 — when sources exist but evidence disagrees (degraded or unavailable axes), the result must
        // never be the confident Supported state.
        RecoveryStateMapper.Map(Resolve(fixtureName)).StateKind.ShouldNotBe(RecoveryStateKind.Supported);
    }

    [Fact]
    public void Map_UnauthorizedOutranksCompressedAndDegraded()
    {
        // Unauthorized scope must win even when compression and degradation signals are also present.
        EvidencePacket packet = RecoveryPacketFixtures.Unauthorized() with
        {
            Evidence = RecoveryPacketFixtures.Unauthorized().Evidence with { Degraded = true },
            OmittedDetails = new EvidencePacketOmittedDetails(
                OmittedCount: 5,
                EstimatedTokensTotal: 1000,
                Reason: EvidencePacketOmissionReason.TokenBudget,
                FieldNames: ["sources"],
                DetailGroups: ["rankedResults"],
                ExpansionHandles: []),
        };

        RecoveryStateViewModel view = RecoveryStateMapper.Map(packet);
        view.StateKind.ShouldBe(RecoveryStateKind.Unauthorized);

        // Risk markers are suppressed when unauthorized so they cannot leak detail about restricted content.
        view.RiskMarkers.ShouldBeEmpty();
    }

    [Fact]
    public void Map_RestrictiveScope_DisablesScopeExpandingActionsWithReason()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.UnauthorizedWithExpandingActions());

        // CheckAuthorization is the only safe action under a restrictive scope, so it becomes the primary.
        view.PrimaryAction.ShouldNotBeNull();
        view.PrimaryAction!.Kind.ShouldBe(EvidencePacketRecoveryKind.CheckAuthorization);
        view.PrimaryAction.Availability.ShouldBe(RecoveryActionAvailability.Available);

        // The scope-expanding action renders disabled with a localized reason instead of being hidden.
        RecoveryActionView broaden = view.SecondaryActions.ShouldHaveSingleItem();
        broaden.Kind.ShouldBe(EvidencePacketRecoveryKind.BroadenScope);
        broaden.Availability.ShouldBe(RecoveryActionAvailability.Unavailable);
        broaden.DisabledReasonKey.ShouldBe(RecoveryResourceKeys.DisabledAuthRequired);
    }

    [Fact]
    public void Map_MultipleActions_SelectsSafestAsPrimary()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.MultiActionNoMatch());

        // FetchMemoryUnit is safer (inspection) than IncreaseMaxResults or the scope-expanding BroadenScope.
        view.PrimaryAction.ShouldNotBeNull();
        view.PrimaryAction!.Kind.ShouldBe(EvidencePacketRecoveryKind.FetchMemoryUnit);
        view.PrimaryAction.IsPrimary.ShouldBeTrue();
        view.SecondaryActions.Count.ShouldBe(2);
        view.SecondaryActions.ShouldContain(a => a.Kind == EvidencePacketRecoveryKind.BroadenScope);
        view.SecondaryActions.ShouldContain(a => a.Kind == EvidencePacketRecoveryKind.IncreaseMaxResults);
    }

    [Fact]
    public void Map_NoRecoveryActions_LeavesPrimaryNull()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.NoMatch());

        view.PrimaryAction.ShouldBeNull();
        view.SecondaryActions.ShouldBeEmpty();
    }

    [Fact]
    public void Map_WeakAndCompressed_KeepsCompressedAsSecondaryRiskMarker()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.WeakAndCompressed());

        view.StateKind.ShouldBe(RecoveryStateKind.Weak);
        view.RiskMarkers.ShouldContain(m => m.Code == "compressed");
    }

    [Fact]
    public void Map_MalformedButSafePacket_DoesNotThrowAndStaysSafe()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.MalformedButSafe());

        view.StateKind.ShouldBe(RecoveryStateKind.InsufficientEvidence);
        view.TenantId.ShouldBe("unknown tenant");
        view.CaseId.ShouldBeNull();
    }

    [Fact]
    public void Map_DiagnosticClue_UsesOnlyWhitelistedCodesAndCounts()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.Compressed());

        // Compression must be announced as omitted/expandable, not proven absent.
        view.DiagnosticClueCode.ShouldContain("omission=tokenBudget");
        view.DiagnosticClueCode.ShouldContain("omittedCount=");

        // The clue is whitelisted code tokens and counts only — no free-form punctuation beyond the
        // documented separators, identifiers, or sensitive markers.
        ClueShapeRegex().IsMatch(view.DiagnosticClueCode).ShouldBeTrue($"Unexpected clue shape: {view.DiagnosticClueCode}");
    }

    [Fact]
    public void Map_CompressedPacket_SurfacesOmittedDetailNamesAndExpansionHandles()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.Compressed());

        view.OmittedDetailNames.ShouldContain("rankedResults");
        view.OmittedDetailNames.ShouldContain("sources");
        view.Expansions.ShouldContain(e =>
            e.Kind == EvidencePacketRecoveryKind.IncreaseTokenBudget && e.TargetDetailGroup == "rankedResults");
    }

    [Fact]
    public void Map_Unauthorized_SuppressesOmittedDetailsForRedactionParity()
    {
        RecoveryStateViewModel view = RecoveryStateMapper.Map(RecoveryPacketFixtures.Unauthorized());

        view.OmittedDetailNames.ShouldBeEmpty();
        view.Expansions.ShouldBeEmpty();
    }

    [Fact]
    public void Map_MappedContractSources_MatchTraceabilityTable()
    {
        foreach (EvidencePacket packet in AllFixtures())
        {
            RecoveryStateViewModel view = RecoveryStateMapper.Map(packet);
            RecoveryStateTrace trace = RecoveryStateTraceability.For(view.StateKind);

            view.ContractSources.ShouldBe(trace.ContractSources);
            view.ContractSources.ShouldNotBeEmpty();
            view.TitleKey.ShouldBe(trace.TitleKey);
            view.ExplanationKey.ShouldBe(trace.ExplanationKey);
            view.AffectedCapabilityKey.ShouldBe(trace.AffectedCapabilityKey);
            view.Severity.ShouldBe(trace.Severity);
        }
    }

    [Fact]
    public void Traceability_HasExactlyOneEntryPerStateKindWithNamedSources()
    {
        RecoveryStateKind[] kinds = Enum.GetValues<RecoveryStateKind>();

        foreach (RecoveryStateKind kind in kinds)
        {
            RecoveryStateTrace trace = RecoveryStateTraceability.For(kind);
            trace.Kind.ShouldBe(kind);
            trace.ContractSources.ShouldNotBeEmpty();
            trace.ContractSources.ShouldAllBe(static s => !string.IsNullOrWhiteSpace(s));
            trace.TitleKey.ShouldBe(RecoveryResourceKeys.Title(kind));
            trace.ExplanationKey.ShouldBe(RecoveryResourceKeys.Explanation(kind));
        }

        RecoveryStateTraceability.Entries.Count.ShouldBe(kinds.Length);
    }

    private static EvidencePacket Resolve(string fixtureName) => fixtureName switch
    {
        nameof(RecoveryPacketFixtures.Supported) => RecoveryPacketFixtures.Supported(),
        nameof(RecoveryPacketFixtures.Weak) => RecoveryPacketFixtures.Weak(),
        nameof(RecoveryPacketFixtures.StaleMemory) => RecoveryPacketFixtures.StaleMemory(),
        nameof(RecoveryPacketFixtures.DegradedBackendWithSources) => RecoveryPacketFixtures.DegradedBackendWithSources(),
        nameof(RecoveryPacketFixtures.DegradedBackendNoSources) => RecoveryPacketFixtures.DegradedBackendNoSources(),
        nameof(RecoveryPacketFixtures.Unauthorized) => RecoveryPacketFixtures.Unauthorized(),
        nameof(RecoveryPacketFixtures.UnknownScope) => RecoveryPacketFixtures.UnknownScope(),
        nameof(RecoveryPacketFixtures.Compressed) => RecoveryPacketFixtures.Compressed(),
        nameof(RecoveryPacketFixtures.ConflictingViaDegraded) => RecoveryPacketFixtures.ConflictingViaDegraded(),
        nameof(RecoveryPacketFixtures.ConflictingViaUnavailableAxes) => RecoveryPacketFixtures.ConflictingViaUnavailableAxes(),
        nameof(RecoveryPacketFixtures.NoMatch) => RecoveryPacketFixtures.NoMatch(),
        nameof(RecoveryPacketFixtures.NotIngestedYet) => RecoveryPacketFixtures.NotIngestedYet(),
        nameof(RecoveryPacketFixtures.GraphGapNoSources) => RecoveryPacketFixtures.GraphGapNoSources(),
        nameof(RecoveryPacketFixtures.InsufficientFromPartial) => RecoveryPacketFixtures.InsufficientFromPartial(),
        nameof(RecoveryPacketFixtures.InsufficientNoSignal) => RecoveryPacketFixtures.InsufficientNoSignal(),
        nameof(RecoveryPacketFixtures.UnknownState) => RecoveryPacketFixtures.UnknownState(),
        _ => throw new InvalidOperationException($"Unsupported fixture '{fixtureName}'."),
    };

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
        yield return RecoveryPacketFixtures.MultiActionNoMatch();
        yield return RecoveryPacketFixtures.UnauthorizedWithExpandingActions();
        yield return RecoveryPacketFixtures.MalformedButSafe();
    }

    [GeneratedRegex("^[a-zA-Z0-9]+=[a-zA-Z0-9]+(; [a-zA-Z0-9]+=[a-zA-Z0-9]+)*$")]
    private static partial Regex ClueShapeRegex();
}
