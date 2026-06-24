// <copyright file="Epic17ValidationInventory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System.Collections.Generic;

/// <summary>
/// Story 17.5 — the fail-closed surface inventory and tooling-gap registry. This is the machine-checked
/// half of Task 0 / Task 6: a surface is only counted as validated when it names an implementation source,
/// a runnable specimen or fixture family, a selector anchor, and a validation level; every browser or
/// assistive-technology dimension that cannot run against a host-less Razor Class Library is recorded as a
/// gap with a disposition rather than passing by omission.
/// </summary>
internal static class Epic17ValidationInventory
{
    /// <summary>Validation claim levels permitted by Story 17.5 Task 0.</summary>
    public const string ComponentSpecimen = "component-specimen";

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
    /// <param name="BrowserDisposition">The disposition for the browser/assistive-technology dimensions.</param>
    public sealed record SurfaceRow(
        string Surface,
        string UpstreamStory,
        string ImplementationSource,
        string RunnableSpecimen,
        string FixtureFamily,
        string SelectorAnchor,
        string ValidationLevel,
        string BrowserDisposition);

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
        new("Evidence Cockpit", "17.1", "MemoriesEvidenceCockpit", "EvidenceCockpit", "EvidencePacketFixtures", "mem-evidence-cockpit", ComponentSpecimen, "Playwright/axe/forced-colors/reduced-motion/screen-reader deferred — host-less RCL"),
        new("Trust Strip", "17.1", "MemoriesTrustStrip", "TrustStrip", "EvidencePacketFixtures", "mem-trust-strip", ComponentSpecimen, "Browser contrast/forced-colors deferred — host-less RCL"),
        new("Scope Header", "17.1", "MemoriesScopeHeader", "EvidenceCockpit", "EvidencePacketFixtures", "mem-evidence-scope", ComponentSpecimen, "Browser dimensions deferred — covered transitively by cockpit"),
        new("Source Citation Stack", "17.1", "MemoriesSourceCitationStack", "EvidenceCockpit", "EvidencePacketFixtures", "mem-source-stack", ComponentSpecimen, "Browser dimensions deferred — covered transitively by cockpit"),
        new("Retrieval Axis Breakdown", "17.1", "MemoriesRetrievalAxisBreakdown", "EvidenceCockpit", "EvidencePacketFixtures", "mem-axis-breakdown", ComponentSpecimen, "Browser dimensions deferred — covered transitively by cockpit"),
        new("Graph Path Summary", "17.1", "MemoriesGraphPathSummary", "EvidenceCockpit", "EvidencePacketFixtures", "mem-graph-summary", ComponentSpecimen, "Browser dimensions deferred — covered transitively by cockpit"),
        new("Recovery Action Panel", "17.2", "MemoriesRecoveryActionPanel", "RecoveryActionPanel", "RecoveryPacketFixtures", "mem-evidence-recovery", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Case Activity Trail", "17.4", "MemoriesCaseActivityTrail", "CaseActivityTrail", "LensPacketFixtures", "mem-activity-trail", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Ingestion Lifecycle Tracker", "17.4", "MemoriesIngestionLifecycleTracker", "IngestionLifecycleTracker", "LensPacketFixtures", "mem-ingestion-tracker", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Operator Health Matrix", "17.4", "MemoriesOperatorHealthMatrix", "OperatorHealthMatrix", "LensPacketFixtures", "mem-health-matrix", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Benchmark Result Comparator", "17.4", "MemoriesBenchmarkResultComparator", "BenchmarkResultComparator", "LensPacketFixtures", "mem-benchmark-comparator", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Agent Packet Inspector", "17.4", "MemoriesAgentPacketInspector", "AgentPacketInspector", "LensPacketFixtures", "mem-packet-inspector", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Evidence Grid (data-heavy)", "17.3", "MemoriesEvidenceGrid", "EvidenceGrid", "EvidencePacketFixtures", "mem-evidence-grid", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Command Surface", "17.3", "MemoriesCommandSurface", "CommandSurface", "EvidencePacketFixtures", "mem-command-surface", ComponentSpecimen, "Browser focus-trap deferred — host-less RCL"),
        new("Action Confirmation", "17.3", "MemoriesActionConfirmation", "MemoriesConfirmationAndNavigationTests", "EvidencePacketFixtures", "fc-destructive-dialog", ComponentSpecimen, "Browser focus-trap/return deferred — FrontComposer overlay"),
        new("Context Navigation", "17.3", "MemoriesContextNavigation", "ContextNavigation", "EvidencePacketFixtures", "mem-context-navigation", ComponentSpecimen, "Browser focus-trap/return deferred — host-less RCL"),
        new("Interaction Form", "17.3", "MemoriesInteractionForm", "MemoriesInteractionFormTests", "FormFixtures", "mem-interaction-form", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Filter Summary", "17.3", "MemoriesFilterSummary", "MemoriesFilterSummaryTests", "EvidencePacketFixtures", "mem-filter-summary", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
        new("Lens Shell", "17.4", "MemoriesLensShell", "CaseActivityTrail", "LensPacketFixtures", "mem-lens-shell", ComponentSpecimen, "Browser dimensions deferred — host-less RCL"),
    ];

    /// <summary>
    /// The browser and assistive-technology checks that cannot run against the host-less RCL. Each is a
    /// deferred release decision paired with manual evidence, never a silent pass.
    /// </summary>
    public static IReadOnlyList<ToolingGap> Gaps { get; } =
    [
        new(
            "Playwright product-route smoke + @axe-core/playwright WCAG 2.2 AA scan",
            "Hexalith.Memories.Web is a host-less Razor Class Library; no runnable Memories web route or e2e specimen is wired into the FrontComposer Playwright workspace (which targets the Counter specimen only).",
            "High",
            "Memories web product owner + QA",
            "Waived for this component-specimen validation pass",
            "Blocked until a runnable Memories web host/specimen exists; do not claim product-route validation from specimens."),
        new(
            "Color-contrast automated check",
            "Contrast is a rendered-pixel property requiring a browser; bUnit renders markup without a layout/paint engine.",
            "Medium",
            "Memories web product owner + UX",
            "Waived for this pass — Fluent UI v5 tokens carry the AA contrast contract",
            "Covered by Fluent UI v5 token usage (no legacy v4/FAST tokens, no hand-rolled color); re-verify in browser when a host exists."),
        new(
            "Forced-colors (high contrast) emulation",
            "Requires a browser context (emulateMedia forced-colors); not available in bUnit.",
            "Medium",
            "Memories web product owner + UX",
            "Waived for this pass — non-color comprehension proven at component level",
            "Manual high-contrast pass required against a host; non-color text equivalents already validated by Epic17AccessibilitySweepTests."),
        new(
            "Reduced-motion emulation",
            "Requires page.emulateMedia({ reducedMotion: 'reduce' }); not available in bUnit.",
            "Low",
            "Memories web product owner + UX",
            "Waived for this pass — no component depends on animation for trust comprehension",
            "Manual reduced-motion pass required against a host; status/progress conveyed as text, validated at component level."),
        new(
            "Zoom / reflow to 400% and 44x44px touch-target sizing",
            "Both are rendered-pixel/layout properties requiring a browser viewport.",
            "Medium",
            "Memories web product owner + UX",
            "Waived for this pass",
            "Manual zoom/reflow + touch-target pass required against a host; Fluent UI v5 controls supply the baseline target size."),
        new(
            "Screen-reader pass (NVDA/JAWS/VoiceOver) and keyboard focus-trap/focus-return in a live DOM",
            "Real focus management, focus order, and AT announcements require a browser + assistive technology; bUnit asserts roles/aria/live-region attributes but not live focus movement.",
            "High",
            "Memories web product owner + QA + accessibility tester",
            "Waived for this pass — focus contracts documented and aria semantics validated",
            "At least one manual screen-reader pass required before release once a host exists; focus contracts recorded in Epic17FocusContractTests."),
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
}
