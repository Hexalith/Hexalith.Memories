// <copyright file="Epic17ValidationInventory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System.Collections.Generic;

using Hexalith.Memories.Web.Specimens;

/// <summary>
/// Story 17.5 / 17.7 — the fail-closed surface inventory, browser-specimen evidence register, and
/// tooling-gap registry. A surface is only counted as validated when it names an implementation source,
/// a runnable specimen or fixture family, a selector anchor, route metadata, evidence artifacts, and a
/// validation level; every browser or assistive-technology dimension is either evidence-backed or carried
/// forward with a disposition rather than passing by omission.
/// </summary>
internal static class Epic17ValidationInventory
{
    /// <summary>Validation claim levels permitted by Story 17.5 Task 0.</summary>
    public const string ComponentSpecimen = "component-specimen";

    /// <summary>A browser-backed claim against the Story 17.7 non-product specimen host.</summary>
    public const string BrowserSpecimen = "browser-specimen";

    /// <summary>A claim backed by a Story 2.7-aligned contract fixture rendered through a component.</summary>
    public const string ContractFixture = "contract-fixture";

    /// <summary>A dimension that cannot run here and is deferred to a product/architecture decision.</summary>
    public const string Deferred = "deferred";

    /// <summary>One row of the surface inventory.</summary>
    /// <param name="Surface">The Epic 17 trust surface name.</param>
    /// <param name="UpstreamStory">The story that built the surface.</param>
    /// <param name="ImplementationSource">The product component under validation.</param>
    /// <param name="RunnableSpecimen">The runnable bUnit specimen entry point.</param>
    /// <param name="FixtureFamily">The canonical fixture family feeding the specimen.</param>
    /// <param name="SelectorAnchor">The required <c>data-testid</c> / role anchor.</param>
    /// <param name="ValidationLevel">The validation claim level.</param>
    /// <param name="SpecimenRoute">The non-product browser specimen route.</param>
    /// <param name="EvidenceArtifactPath">The bounded browser evidence artifact path.</param>
    /// <param name="BrowserDisposition">The disposition for the browser/assistive-technology dimensions.</param>
    public sealed record SurfaceRow(
        string Surface,
        string UpstreamStory,
        string ImplementationSource,
        string RunnableSpecimen,
        string FixtureFamily,
        string SelectorAnchor,
        string ValidationLevel,
        string SpecimenRoute,
        string EvidenceArtifactPath,
        string BrowserDisposition);

    /// <summary>One browser evidence dimension produced by the Story 17.7 specimen lane.</summary>
    /// <param name="Dimension">The browser or artifact dimension covered.</param>
    /// <param name="EvidenceKind">The evidence kind.</param>
    /// <param name="Scope">The route or route set covered.</param>
    /// <param name="ArtifactPath">The bounded artifact path.</param>
    /// <param name="Severity">Release severity if this evidence is missing.</param>
    /// <param name="Owner">Owner accountable for maintaining the evidence.</param>
    /// <param name="WaiverState">Whether the dimension is waived for this validation pass.</param>
    /// <param name="ReleaseDisposition">What this evidence does and does not claim.</param>
    public sealed record BrowserEvidenceRow(
        string Dimension,
        string EvidenceKind,
        string Scope,
        string ArtifactPath,
        string Severity,
        string Owner,
        string WaiverState,
        string ReleaseDisposition);

    /// <summary>One tracked tooling / assistive-technology gap with an explicit release disposition.</summary>
    /// <param name="Check">The check that cannot run against the host-less RCL.</param>
    /// <param name="Reason">Why it cannot run here.</param>
    /// <param name="Severity">Release severity of the gap.</param>
    /// <param name="Owner">Owner accountable for closing or waiving the gap.</param>
    /// <param name="WaiverState">Whether the gap is waived for this validation pass.</param>
    /// <param name="ReleaseDisposition">What must happen before a full product-surface claim.</param>
    public sealed record ToolingGap(
        string Check,
        string Reason,
        string Severity,
        string Owner,
        string WaiverState,
        string ReleaseDisposition);

    /// <summary>The Epic 17 trust surfaces named in AC1, every one validated at component-specimen level.</summary>
    public static IReadOnlyList<SurfaceRow> Surfaces { get; } =
    [
        Surface("Evidence Cockpit", "17.1", "MemoriesEvidenceCockpit", "EvidenceCockpit", "evidence-cockpit"),
        Surface("Trust Strip", "17.1", "MemoriesTrustStrip", "TrustStrip", "trust-strip"),
        Surface("Scope Header", "17.1", "MemoriesScopeHeader", "EvidenceCockpit", "scope-header"),
        Surface("Source Citation Stack", "17.1", "MemoriesSourceCitationStack", "EvidenceCockpit", "source-citation-stack"),
        Surface("Retrieval Axis Breakdown", "17.1", "MemoriesRetrievalAxisBreakdown", "EvidenceCockpit", "retrieval-axis-breakdown"),
        Surface("Graph Path Summary", "17.1", "MemoriesGraphPathSummary", "EvidenceCockpit", "graph-path-summary"),
        Surface("Recovery Action Panel", "17.2", "MemoriesRecoveryActionPanel", "RecoveryActionPanel", "recovery-action-panel"),
        Surface("Case Activity Trail", "17.4", "MemoriesCaseActivityTrail", "CaseActivityTrail", "case-activity-trail"),
        Surface("Ingestion Lifecycle Tracker", "17.4", "MemoriesIngestionLifecycleTracker", "IngestionLifecycleTracker", "ingestion-lifecycle-tracker"),
        Surface("Operator Health Matrix", "17.4", "MemoriesOperatorHealthMatrix", "OperatorHealthMatrix", "operator-health-matrix"),
        Surface("Benchmark Result Comparator", "17.4", "MemoriesBenchmarkResultComparator", "BenchmarkResultComparator", "benchmark-result-comparator"),
        Surface("Agent Packet Inspector", "17.4", "MemoriesAgentPacketInspector", "AgentPacketInspector", "agent-packet-inspector"),
        Surface("Evidence Grid", "17.3", "MemoriesEvidenceGrid", "EvidenceGrid", "evidence-grid"),
        Surface("Command Surface", "17.3", "MemoriesCommandSurface", "CommandSurface", "command-surface"),
        Surface("Action Confirmation", "17.3", "MemoriesActionConfirmation", "ActionConfirmation", "action-confirmation"),
        Surface("Context Navigation", "17.3", "MemoriesContextNavigation", "ContextNavigation", "context-navigation"),
        Surface("Interaction Form", "17.3", "MemoriesInteractionForm", "MemoriesInteractionFormTests", "interaction-form"),
        Surface("Filter Summary", "17.3", "MemoriesFilterSummary", "MemoriesFilterSummaryTests", "filter-summary"),
        Surface("Lens Shell", "17.4", "MemoriesLensShell", "CaseActivityTrail", "lens-shell"),
    ];

    /// <summary>The browser-backed evidence dimensions produced by the Story 17.7 specimen lane.</summary>
    public static IReadOnlyList<BrowserEvidenceRow> BrowserEvidence { get; } =
    [
        new(
            "Playwright specimen smoke route and selector coverage",
            BrowserSpecimen,
            "All /__memories/specimens routes",
            Epic17SpecimenManifest.EvidenceSummaryPath,
            "High",
            "Memories web QA",
            "Not waived for specimen lane",
            "Resolves browser smoke coverage for non-product specimens only; product-route release claim remains separate."),
        new(
            "AXE blocking WCAG scan and color-contrast-supported rules",
            BrowserSpecimen,
            "All /__memories/specimens routes",
            Epic17SpecimenManifest.EvidenceSummaryPath,
            "High",
            "Memories web QA + UX",
            "Not waived for blocking violations in the specimen lane",
            "Records zero blocking or unknown-impact axe violations for Chromium specimens; axe incomplete findings remain fail-closed below."),
        new(
            "Forced-colors, reduced-motion, zoom/reflow, and touch-target measurements",
            BrowserSpecimen,
            "All /__memories/specimens routes",
            Epic17SpecimenManifest.EvidenceSummaryPath,
            "Medium",
            "Memories web QA + UX",
            "Not waived for supported Chromium media emulation",
            "Records supported Chromium media/layout evidence; undersized touch targets, touch-device, and non-Chromium release claims remain fail-closed."),
        new(
            "Artifact redaction, copied-text summary, screenshot, trace policy, and manual AT checklist",
            BrowserSpecimen,
            "Bounded evidence artifacts",
            Epic17SpecimenManifest.EvidenceSummaryPath,
            "High",
            "Memories web QA + security",
            "Not waived for artifact redaction",
            "Resolves bounded artifact redaction validation; manual screen-reader execution remains fail-closed."),
    ];

    /// <summary>
    /// The browser and assistive-technology checks still not closed by the non-product Chromium specimen
    /// lane. Each is a release decision paired with manual evidence, never a silent pass.
    /// </summary>
    public static IReadOnlyList<ToolingGap> Gaps { get; } =
    [
        new(
            "AXE incomplete aria-prohibited-attr triage",
            "The Chromium axe lane records zero blocking violations, but axe-core returns known incomplete aria-prohibited-attr results on several Fluent custom-element routes.",
            "Medium",
            "Memories web product owner + UX + FrontComposer/Fluent owner",
            "Not waived for product-route/full WCAG release claim",
            "Source-owned remediation or manual accessibility triage is required before claiming full axe/WCAG clearance."),
        new(
            "Measured 44x44 touch-target failures in Chromium specimens",
            "The browser lane records measurable Fluent button/custom-element controls below 44 CSS pixels in height on several routes; Story 17.7 records the evidence but does not change RCL styling.",
            "Medium",
            "Memories web product owner + UX",
            "Not waived for product-route release claim",
            "Source-owned remediation, target-device manual pass, or explicit release waiver is required before a full touch-target claim."),
        new(
            "Data-heavy horizontal overflow in Chromium specimens",
            "The browser lane records page-level horizontal overflow for data-heavy responsive surfaces while still proving the trust anchor remains visible and not horizontal-only.",
            "Medium",
            "Memories web product owner + UX",
            "Not waived for product-route release claim",
            "Source-owned responsive remediation or explicit release waiver is required before claiming full no-horizontal-overflow behavior."),
        new(
            "Product-route browser validation",
            "Story 17.7 adds a non-product specimen host only; it does not create or validate a production Memories web application route.",
            "High",
            "Memories web product owner",
            "Not waived for product-route release claim",
            "A future production web app route must run its own Playwright/axe/media/AT pass before product-route validation is claimed."),
        new(
            "Non-Chromium browser validation",
            "The automated Story 17.7 lane is intentionally bounded to Chromium for deterministic specimen evidence.",
            "Medium",
            "Memories web product owner + QA",
            "Not waived for broad browser release claim",
            "Firefox/WebKit or target-browser matrix must run before a broad browser-support claim."),
        new(
            "Touch-device 44x44 target confirmation",
            "Chromium bounding-box measurements are recorded, but target-device pointer behavior and Fluent custom-element hit areas still need manual confirmation.",
            "Medium",
            "Memories web product owner + UX",
            "Not waived for product-route release claim",
            "Manual touch-device confirmation required before a full touch-target accessibility claim."),
        new(
            "Screen-reader pass (NVDA/JAWS/VoiceOver) and keyboard focus-trap/focus-return in a live DOM",
            "Real focus management, focus order, and AT announcements require an installed screen reader and target OS/browser pairing; automation records only checklist-method evidence.",
            "High",
            "Memories web product owner + QA + accessibility tester",
            "Not waived for product-route/full AT release claim",
            "At least one manual screen-reader pass required before release; checklist evidence is recorded but does not close this gap."),
    ];

    /// <summary>The Story 17.5 acceptance-criterion → validating test-class map.</summary>
    public static IReadOnlyDictionary<string, string[]> AcceptanceCriteriaToTests { get; } =
        new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["AC1"] = [nameof(Epic17ResponsiveParityTests)],
            ["AC2"] = [nameof(Epic17AccessibilitySweepTests)],
            ["AC3"] = [nameof(Epic17AccessibilitySweepTests), nameof(Epic17FocusContractTests)],
            ["AC4"] = [nameof(Epic17FocusContractTests)],
            ["AC5"] = [nameof(Epic17AccessibilitySweepTests)],
            ["AC6"] = [nameof(Epic17SanitizationCanaryTests)],
        };

    private static SurfaceRow Surface(
        string surface,
        string upstreamStory,
        string implementationSource,
        string runnableSpecimen,
        string slug)
    {
        Epic17SpecimenRoute route = Epic17SpecimenManifest.FindBySlug(slug)
            ?? throw new InvalidOperationException($"Specimen route '{slug}' is not registered.");

        return new SurfaceRow(
            surface,
            upstreamStory,
            implementationSource,
            runnableSpecimen,
            route.FixtureFamily,
            route.SelectorAnchor,
            ComponentSpecimen,
            route.Route,
            route.EvidenceArtifactPath,
            "Browser-backed non-product specimen evidence is summarized in the committed BMAD evidence artifact; product-route, axe incomplete, touch-target, and manual AT claims remain fail-closed where listed.");
    }
}
