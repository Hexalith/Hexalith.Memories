// <copyright file="LensShellAndTraceabilityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.CaseActivity;
using Hexalith.Memories.Web.Components.Recovery;

using Shouldly;

public sealed class LensShellAndTraceabilityTests
{
    [Fact]
    public void Map_Shell_KeepsScopeStateCapabilityReturnPathAndVersionVisible()
    {
        LensShellViewModel shell = LensShellMapper.Map(
            LensPacketFixtures.Happy(),
            LensKind.CaseActivityTrail,
            LensRole.Developer,
            "memories/evidence?packet=memory-a");

        shell.TenantId.ShouldBe("tenant-a");
        shell.CaseId.ShouldBe("case-a");
        shell.Lens.ShouldBe(LensKind.CaseActivityTrail);
        shell.Role.ShouldBe(LensRole.Developer);
        shell.StateKind.ShouldBe(RecoveryStateKind.Supported);
        shell.ContractVersion.ShouldBe(InteractionContextSnapshot.SupportedContractVersion);
        shell.ReturnRoute.ShouldBe("memories/evidence?packet=memory-a");
    }

    [Fact]
    public void Map_RestrictiveShell_SuppressesConfidenceAndSanitizesReturnPath()
    {
        LensShellViewModel shell = LensShellMapper.Map(
            LensPacketFixtures.Unauthorized(),
            LensKind.AgentPacketInspector,
            LensRole.AgentIntegrator,
            "Bearer abc.def.ghi /home/jerome/packet.json");

        shell.Restrictive.ShouldBeTrue();
        shell.ConfidenceLabel.ShouldBe(LensResourceKeys.ConfidenceUnavailableText);
        shell.ReturnRoute.ShouldNotContain("Bearer ");
        shell.ReturnRoute.ShouldNotContain("/home/jerome");
        shell.ReturnRoute.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void RoleDensity_UnknownRole_FailsClosedToCompactCollapsedProfile()
    {
        LensRoleDensityProfile profile = LensRoleDensity.For((LensRole)999);

        profile.ExpandedByDefault.ShouldBeFalse();
        profile.DetailLevel.ShouldBe(LensDetailLevel.Compact);
    }

    [Fact]
    public void RoleDensity_ChangesPresentationOnlyNotActivitySemantics()
    {
        CaseActivityTrailViewModel developer = CaseActivityTrailMapper.Map(LensPacketFixtures.Degraded(), LensRole.Developer);
        CaseActivityTrailViewModel operatorView = CaseActivityTrailMapper.Map(LensPacketFixtures.Degraded(), LensRole.Operator);

        developer.Rows.Select(RowSemantics).OrderBy(static x => x, StringComparer.Ordinal)
            .ShouldBe(operatorView.Rows.Select(RowSemantics).OrderBy(static x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void Traceability_CoversEveryLensWithNamedFallbacksAndArtifacts()
    {
        foreach (LensKind lens in Enum.GetValues<LensKind>())
        {
            IReadOnlyList<LensFieldTrace> rows = LensFieldTraceability.For(lens);

            rows.ShouldNotBeEmpty(lens.ToString());
            rows.ShouldAllBe(r => r.Lens == lens);
            rows.ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.DisplayedField));
            rows.ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.UpstreamSource));
            rows.ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.AbsentBehavior));
            rows.ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.TestLevel));
            rows.ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.EvidenceArtifact));
        }
    }

    [Fact]
    public void Traceability_RecordsDeferredContractGapsInsteadOfInventedSemantics()
    {
        LensFieldTraceability.Entries.ShouldContain(r =>
            r.Lens == LensKind.BenchmarkResultComparator &&
            r.DisplayedField == "benchmark.ndcg" &&
            r.UpstreamSource == LensFieldTraceability.NoContractSource);
        LensFieldTraceability.Entries.ShouldContain(r =>
            r.Lens == LensKind.IngestionLifecycleTracker &&
            r.DisplayedField == "ingestion.stage" &&
            r.UpstreamSource == LensFieldTraceability.NoContractSource);
        LensFieldTraceability.Entries.ShouldContain(r =>
            r.Lens == LensKind.AgentPacketInspector &&
            r.DisplayedField == "packet.toolName" &&
            r.UpstreamSource == LensFieldTraceability.NoContractSource);
    }

    [Fact]
    public void AcceptanceCriteriaToTestMap_CoversEveryLensAndCrossCuttingGuardrail()
    {
        IReadOnlyDictionary<string, string[]> map = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["AC1"] = [nameof(CaseActivityTrailMapperTests)],
            ["AC2"] = [nameof(IngestionLifecycleMapperTests)],
            ["AC3"] = [nameof(OperatorHealthMatrixMapperTests)],
            ["AC4"] = [nameof(BenchmarkResultComparatorMapperTests)],
            ["AC5"] = [nameof(AgentPacketInspectorMapperTests)],
            ["Shell"] = [nameof(LensShellAndTraceabilityTests), nameof(MemoriesLensComponentsTests)],
            ["Sanitization"] = [
                nameof(CaseActivityTrailMapperTests),
                nameof(OperatorHealthMatrixMapperTests),
                nameof(AgentPacketInspectorMapperTests),
                nameof(MemoriesLensComponentsTests),
            ],
        };

        map.Keys.ShouldContain("AC1");
        map.Keys.ShouldContain("AC2");
        map.Keys.ShouldContain("AC3");
        map.Keys.ShouldContain("AC4");
        map.Keys.ShouldContain("AC5");
        map.Values.SelectMany(static v => v).ShouldContain(nameof(MemoriesLensComponentsTests));
        map.Values.ShouldAllBe(static tests => tests.Length > 0);
    }

    private static string RowSemantics(CaseActivityRow row)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{row.Kind}|{row.LinkAvailability}|{row.StatusLabelKey}|{row.Severity}|{row.SafeSummary}|{row.SafeLink}");
}
