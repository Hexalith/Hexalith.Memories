// <copyright file="ConsistencyModelDecisionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>
/// Story 21.1 documentation guard for the ratified consistency model. These tests keep the architecture
/// decision, operator consistency guide, and story record aligned so Epic 21 implementation stories cannot
/// silently fall back to direct multi-backend writes as the domain source of truth.
/// </summary>
public sealed partial class ConsistencyModelDecisionTests
{
    [Fact]
    public void ArchitectureD3_RatifiesEventStoreAggregatesWithRebuildableProjections()
    {
        string architecture = NormalizeWhitespace(ReadRepoFile("_bmad-output", "planning-artifacts", "architecture.md"));

        architecture.ShouldContain(
            "| D3 | EventStore aggregate source of truth + rebuildable projections + DAPR Workflow projection compensation |",
            Case.Sensitive,
            "Decision D3 must name the ratified EventStore/projection model, not only the older ingestion saga model.");
        architecture.ShouldContain(
            "Story 21.1 ratifies the EventStore aggregate model as the consistency target for `Case`, `MemoryUnit`, and `Tenant`",
            Case.Sensitive,
            "The Multi-Backend Consistency section must bind the decision to all three affected aggregates.");
        architecture.ShouldContain(
            "domain state is sourced from Hexalith.EventStore events",
            Case.Sensitive,
            "Architecture must state the durable domain source of truth.");
        architecture.ShouldContain(
            "RediSearch syntactic hashes, Redis Vector entries, FalkorDB nodes/edges, case activity streams, and tenant registry/read records are rebuildable projections/read models",
            Case.Sensitive,
            "Architecture must identify backend records as projections/read models.");
        architecture.ShouldContain(
            "Story 21.2 implements the command-acceptance boundary for case, annotation, memory-unit deletion, case deletion, and tenant lifecycle mutation paths",
            Case.Sensitive,
            "Architecture must describe the implemented Story 21.2 command boundary.");
        architecture.ShouldContain(
            "new domain mutation paths must follow that boundary instead of treating direct Redis/FalkorDB or Dapr state writes as authoritative",
            Case.Sensitive,
            "Architecture must preserve the post-21.2 guard against authoritative direct projection writes.");
        architecture.ShouldContain(
            "| EventStore source of truth + projection compensation | Gate 1 | MVP |",
            Case.Sensitive,
            "The gate summary must not preserve the older eventual-consistency-only label.");
    }

    [Fact]
    public void ConsistencyGuide_DistinguishesCurrentRepairInputFromTargetSourceOfTruth()
    {
        string consistency = NormalizeWhitespace(ReadRepoFile("docs", "dev", "consistency.md"));

        consistency.ShouldContain(
            "Story 21.2 routes case, annotation, memory-unit deletion, and case deletion mutations through a durable EventStore command boundary before projection fan-out",
            Case.Sensitive,
            "The operator guide must describe the post-21.2 durable write boundary.");
        consistency.ShouldContain(
            "The EventStore command/event stream is the authoritative write record for these domain mutations",
            Case.Sensitive,
            "The operator guide must state the EventStore source-of-truth rule.");
        consistency.ShouldContain(
            "Redis case hashes, RediSearch syntactic memory-unit hashes, Redis Vector entries, FalkorDB nodes/edges, case activity streams, and tenant registry/read records are projections/read models",
            Case.Sensitive,
            "The operator guide must identify every backend/read surface as rebuildable.");
        consistency.ShouldContain(
            "The syntactic hash `{tenantId}:mu:{memoryUnitId}` remains the operational input for the existing consistency repair workflow",
            Case.Sensitive,
            "The operator guide must keep current repair workflow behavior separate from source-of-truth semantics.");
        consistency.ShouldNotContain(
            "authoritative syntactic record",
            Case.Sensitive,
            "The operator guide must not keep stale wording that treats syntactic hashes as the target authoritative source.");
        consistency.ShouldNotContain(
            "Until Story 21.2 changes the mutation path",
            Case.Sensitive,
            "The operator guide must not retain pre-implementation transitional wording after Story 21.2.");
    }

    /// <summary>
    /// Story 21.2 code guard: `CaseService` mutations must stay routed through the EventStore
    /// command boundary plus workflow-owned projection fan-out. Reintroducing the pre-21.2 direct
    /// multi-backend write helpers would silently revert audit finding A3.
    /// </summary>
    [Fact]
    public void CaseServiceMutations_RouteThroughEventStoreCommandBoundary()
    {
        string source = ReadRepoFile("src", "Hexalith.Memories.Server", "Cases", "CaseService.cs");

        Regex.Matches(source, @"_commandStore\.AcceptAsync\(").Count.ShouldBeGreaterThanOrEqualTo(
            4,
            "Case create, annotation, memory-unit deletion, and case deletion must each accept an EventStore command before projection fan-out.");
        Regex.Matches(source, @"_projectionWorkflowScheduler\.ScheduleAsync\(").Count.ShouldBeGreaterThanOrEqualTo(
            4,
            "Each accepted mutation command must schedule its workflow-owned projection fan-out.");
        source.ShouldNotContain(
            "DeleteMemoryUnitKeepingHashForRetryAsync",
            Case.Sensitive,
            "The pre-21.2 direct multi-backend delete helper must not be reintroduced as authoritative persistence.");
        source.ShouldNotContain(
            "DeleteMemoryUnitKeepingGraphForRetryAsync",
            Case.Sensitive,
            "The pre-21.2 direct multi-backend delete helper must not be reintroduced as authoritative persistence.");
    }

    [Fact]
    public void StoryRecord_CapturesRejectedAlternativeAndStoryTwentyOneTwoGate()
    {
        string story = NormalizeWhitespace(ReadRepoFile("_bmad-output", "implementation-artifacts", "21-1-consistency-model-decision.md"));

        story.ShouldContain(
            "selected EventStore aggregates with rebuildable projections as the ratified model",
            Case.Sensitive,
            "Story record must capture the chosen model.");
        story.ShouldContain(
            "Workflow-wrapped compensated multi-writes was not selected because it would make a silent exception to Hexalith state rules",
            Case.Sensitive,
            "Story record must explain why the alternate compensated-multi-write model was rejected.");
        story.ShouldContain(
            "Story 21.2 minimum requirements before A3 can be claimed closed: introduce EventStore command/event handling for `Case`, `MemoryUnit`, and `Tenant` mutation semantics",
            Case.Sensitive,
            "Story record must keep A3 closure assigned to concrete Story 21.2 implementation work.");
        story.ShouldContain(
            "add failure-injection tests proving no permanent divergence after Redis, Redis Vector, FalkorDB, registry, or activity-recording failures",
            Case.Sensitive,
            "Story record must preserve the critical failure-injection test requirement for Story 21.2.");
        story.ShouldContain(
            "Stories 21.3-21.10 may perform scoped remediation only when their changes do not depend on unresolved source-of-truth semantics",
            Case.Sensitive,
            "Story record must keep later Epic 21 stories behind the source-of-truth gate.");
        story.ShouldContain(
            "Any source-of-truth-dependent work waits for 21.2",
            Case.Sensitive,
            "Story record must make the 21.2 dependency explicit.");
    }

    private static string ReadRepoFile(params string[] segments)
    {
        string path = Path.Combine([ResolveRepoRoot(), .. segments]);
        File.Exists(path).ShouldBeTrue($"Required Story 21.1 artifact not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }

    private static string NormalizeWhitespace(string value)
        => WhitespaceRegex().Replace(value, " ");

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
