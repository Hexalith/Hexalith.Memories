// <copyright file="Epic17SpecimenManifest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Specimens;

/// <summary>
/// Stable Story 17 specimen route manifest shared by the Blazor host, bUnit inventory, and Playwright.
/// </summary>
public static class Epic17SpecimenManifest
{
    /// <summary>The non-product route prefix for Memories web specimens.</summary>
    public const string RoutePrefix = "/__memories/specimens";

    /// <summary>The durable committed evidence summary for the transient browser artifact set.</summary>
    public const string EvidenceSummaryPath = "_bmad-output/implementation-artifacts/tests/test-summary-17-7-browser-at-gap-closure.md";

    /// <summary>Gets every Story 17 browser specimen route.</summary>
    public static IReadOnlyList<Epic17SpecimenRoute> Routes { get; } =
    [
        Route("Evidence Cockpit", "evidence-cockpit", "MemoriesEvidenceCockpit", "Epic17EvidencePacketFixtures", "mem-evidence-cockpit"),
        Route("Trust Strip", "trust-strip", "MemoriesTrustStrip", "Epic17EvidencePacketFixtures", "mem-trust-strip"),
        Route("Scope Header", "scope-header", "MemoriesScopeHeader", "Epic17EvidencePacketFixtures", "mem-evidence-scope"),
        Route("Source Citation Stack", "source-citation-stack", "MemoriesSourceCitationStack", "Epic17EvidencePacketFixtures", "mem-source-stack"),
        Route("Retrieval Axis Breakdown", "retrieval-axis-breakdown", "MemoriesRetrievalAxisBreakdown", "Epic17EvidencePacketFixtures", "mem-axis-breakdown"),
        Route("Graph Path Summary", "graph-path-summary", "MemoriesGraphPathSummary", "Epic17EvidencePacketFixtures", "mem-graph-summary"),
        Route("Recovery Action Panel", "recovery-action-panel", "MemoriesRecoveryActionPanel", "Epic17RecoveryPacketFixtures", "mem-evidence-recovery"),
        Route("Case Activity Trail", "case-activity-trail", "MemoriesCaseActivityTrail", "Epic17LensPacketFixtures", "mem-activity-trail"),
        Route("Ingestion Lifecycle Tracker", "ingestion-lifecycle-tracker", "MemoriesIngestionLifecycleTracker", "Epic17LensPacketFixtures", "mem-ingestion-tracker"),
        Route("Operator Health Matrix", "operator-health-matrix", "MemoriesOperatorHealthMatrix", "Epic17LensPacketFixtures", "mem-health-matrix"),
        Route("Benchmark Result Comparator", "benchmark-result-comparator", "MemoriesBenchmarkResultComparator", "Epic17LensPacketFixtures", "mem-benchmark-comparator"),
        Route("Agent Packet Inspector", "agent-packet-inspector", "MemoriesAgentPacketInspector", "Epic17LensPacketFixtures", "mem-packet-inspector"),
        Route("Evidence Grid", "evidence-grid", "MemoriesEvidenceGrid", "Epic17EvidencePacketFixtures", "mem-evidence-grid"),
        Route("Command Surface", "command-surface", "MemoriesCommandSurface", "Epic17EvidencePacketFixtures", "mem-command-surface"),
        Route("Action Confirmation", "action-confirmation", "MemoriesActionConfirmation", "Epic17EvidencePacketFixtures", "mem-action-confirmation"),
        Route("Context Navigation", "context-navigation", "MemoriesContextNavigation", "Epic17EvidencePacketFixtures", "mem-context-navigation"),
        Route("Interaction Form", "interaction-form", "MemoriesInteractionForm", "Epic17FormFixtures", "mem-interaction-form"),
        Route("Filter Summary", "filter-summary", "MemoriesFilterSummary", "Epic17EvidencePacketFixtures", "mem-filter-summary"),
        Route("Lens Shell", "lens-shell", "MemoriesLensShell", "Epic17LensPacketFixtures", "mem-lens-shell"),
    ];

    /// <summary>Resolves a specimen route by its slug.</summary>
    /// <param name="slug">The route slug.</param>
    /// <returns>The matching specimen route, or <see langword="null" /> when no route is registered.</returns>
    public static Epic17SpecimenRoute? FindBySlug(string? slug)
        => Routes.FirstOrDefault(route => string.Equals(route.Slug, slug, StringComparison.OrdinalIgnoreCase));

    private static Epic17SpecimenRoute Route(
        string surface,
        string slug,
        string componentName,
        string fixtureFamily,
        string selectorAnchor)
        => new(
            surface,
            slug,
            componentName,
            fixtureFamily,
            selectorAnchor,
            EvidenceSummaryPath);
}
