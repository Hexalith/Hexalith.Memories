// <copyright file="Epic17ConformanceAllowlist.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Story 17.6 — the fail-closed FrontComposer / Fluent UI Blazor V5 conformance register for the
/// <c>Hexalith.Memories.Web</c> Razor Class Library. This is the machine-checked half of the conformance
/// audit (AC1) and the exception allowlist (AC4): every <c>.razor</c> and <c>.razor.css</c> source file is
/// classified, and every unavoidable raw semantic/container element or accessibility utility kept in source
/// is named with all six required fields. <see cref="Epic17ConformanceTests"/> scans the source on disk and
/// fails closed when a new file is unclassified, a raw control or legacy token appears, or an allowlist
/// entry is stale — so Stories 17.1–17.5 (and any future Epic 17 story) cannot drift into a parallel design
/// system or raw HTML/CSS implementation without an explicit, justified entry here.
/// </summary>
internal static class Epic17ConformanceAllowlist
{
    /// <summary>The component composes FrontComposer shell primitives.</summary>
    public const string FrontComposerComponent = "frontcomposer-component";

    /// <summary>The component composes Fluent UI Blazor V5 primitives.</summary>
    public const string FluentComponent = "fluent-v5-component";

    /// <summary>The component composes Fluent/FrontComposer primitives plus allowlisted semantic markup.</summary>
    public const string FluentWithSemanticMarkup = "fluent-with-allowlisted-semantic-markup";

    /// <summary>The file is semantic/container markup over the design system's text/token primitives.</summary>
    public const string SemanticContainerMarkup = "semantic-container-markup";

    /// <summary>Scoped CSS limited to layout the design system does not own (and Fluent 2 tokens).</summary>
    public const string LayoutOnlyCss = "layout-only-css";

    /// <summary>A Razor directive/import file with no rendered markup.</summary>
    public const string RazorDirectives = "razor-directives";

    /// <summary>
    /// Interactive control elements a Fluent UI V5 / FrontComposer component already owns. These are never
    /// allowlisted: a raw occurrence in any <c>.razor</c> source is a hard conformance failure (AC2).
    /// </summary>
    public static IReadOnlyList<string> ForbiddenRawControlElements { get; } =
    [
        "button", "input", "select", "textarea", "option", "form",
        "a", "nav", "dialog", "table", "thead", "tbody", "tr", "td", "th",
    ];

    /// <summary>
    /// Raw semantic/container HTML elements that have no direct Fluent UI V5 / FrontComposer primitive. Each
    /// occurrence must carry an <see cref="AllowlistEntry"/> (AC4); an unallowlisted occurrence fails closed
    /// (AC5), and a stale entry that no longer matches source also fails.
    /// </summary>
    public static IReadOnlyList<string> TrackedSemanticElements { get; } =
    [
        "article", "section", "header", "footer", "main",
        "ol", "ul", "li", "dl", "dt", "dd",
        "details", "summary", "pre",
        "h1", "h2", "h3", "h4", "h5", "h6",
    ];

    /// <summary>The accessibility utility selector tracked in scoped CSS (visually-hidden text).</summary>
    public const string VisuallyHiddenSelector = ".visually-hidden";

    /// <summary>One classified source file in the conformance audit (AC1).</summary>
    /// <param name="RelativePath">Path relative to <c>src/Hexalith.Memories.Web</c>, forward-slashed.</param>
    /// <param name="Classification">How the file conforms (a *Component / *Markup / *Css constant).</param>
    /// <param name="Note">Why the classification holds.</param>
    public sealed record FileClassification(string RelativePath, string Classification, string Note);

    /// <summary>One justified conformance exception kept in source (AC4 — all six fields required).</summary>
    /// <param name="File">Path relative to <c>src/Hexalith.Memories.Web</c>, forward-slashed.</param>
    /// <param name="Pattern">The selector or markup pattern that must be present in the file.</param>
    /// <param name="Reason">Why the exception is unavoidable.</param>
    /// <param name="MissingPrimitive">The FrontComposer/Fluent primitive that would replace it.</param>
    /// <param name="OwnerStory">The story that owns the exception.</param>
    /// <param name="RemovalCondition">The objective condition under which the exception is removed.</param>
    public sealed record AllowlistEntry(
        string File,
        string Pattern,
        string Reason,
        string MissingPrimitive,
        string OwnerStory,
        string RemovalCondition);

    /// <summary>Every <c>.razor</c> and <c>.razor.css</c> file in the RCL, classified (AC1).</summary>
    public static IReadOnlyList<FileClassification> Files { get; } =
    [
        new("_Imports.razor", RazorDirectives, "Global usings for the RCL; no rendered markup."),

        new("Components/Evidence/MemoriesEvidenceCockpit.razor", FluentWithSemanticMarkup,
            "Composes one FluentAccordion with V5 Header/Expanded members plus FluentMessageBar/FluentLabel and child components; remaining raw markup is semantic landmarks."),
        new("Components/Evidence/MemoriesGraphPathSummary.razor", FluentWithSemanticMarkup,
            "FluentText copy over the allowlisted semantic description-list fallback and visually-hidden separator."),
        new("Components/Evidence/MemoriesRetrievalAxisBreakdown.razor", FluentWithSemanticMarkup,
            "FluentText copy over an ordered retrieval-axis list with semantic description-list detail."),
        new("Components/Evidence/MemoriesScopeHeader.razor", FluentWithSemanticMarkup,
            "FcStatusBadge isolation chip inside a landmark header; remaining markup is semantic scope captions."),
        new("Components/Evidence/MemoriesSourceCitationStack.razor", FluentWithSemanticMarkup,
            "FluentText copy over an ordered ranked-source list with semantic description-list detail."),
        new("Components/Evidence/MemoriesTrustStrip.razor", FluentWithSemanticMarkup,
            "FcStatusBadge trust chips inside a landmark section; remaining markup is a labelled source-count span."),

        new("Components/Filters/MemoriesFilterSummary.razor", FluentWithSemanticMarkup,
            "FcFilterSummary, FcStatusBadge, FluentLabel and FluentButton chips inside generic grouping containers."),
        new("Components/Forms/MemoriesInteractionForm.razor", FluentWithSemanticMarkup,
            "FluentStack/FluentLabel/FluentButton/FluentCheckbox form over FcStatusBadge severity; landmark sections only."),
        new("Components/Grid/MemoriesEvidenceGrid.razor", FluentComponent,
            "FluentDataGrid with PropertyColumn/TemplateColumn and FcStatusBadge cells; cells are generic spans."),

        new("Components/Interaction/MemoriesActionConfirmation.razor", FrontComposerComponent,
            "FcDestructiveConfirmationDialog (owns focus trap/return) inside a generic wrapper."),
        new("Components/Interaction/MemoriesCommandSurface.razor", FluentComponent,
            "FluentStack of FluentButton commands with FluentLabel disabled reasons."),
        new("Components/Interaction/MemoriesContextNavigation.razor", FluentComponent,
            "FluentStack of FluentLabel context and FluentButton open/return actions."),

        new("Components/Lenses/AgentPacket/MemoriesAgentPacketInspector.razor", FluentWithSemanticMarkup,
            "Fluent labels/badges/button inside the lens shell; raw markup is landmark sections, a schema list, and a native disclosure with a preformatted JSON view."),
        new("Components/Lenses/Benchmark/MemoriesBenchmarkResultComparator.razor", FluentWithSemanticMarkup,
            "FluentProgressBar axis bars with text equivalents; raw markup is an unordered axis list."),
        new("Components/Lenses/CaseActivity/MemoriesCaseActivityTrail.razor", FluentComponent,
            "FluentStack/FluentLabel/FcStatusBadge rows inside the lens shell; generic list containers carry list/listitem roles."),
        new("Components/Lenses/Ingestion/MemoriesIngestionLifecycleTracker.razor", FluentComponent,
            "FluentStack/FluentLabel/FluentButton/FcStatusBadge units inside the lens shell; generic containers only."),
        new("Components/Lenses/MemoriesLensShell.razor", FluentWithSemanticMarkup,
            "Shared lens chrome of FluentStack/FluentLabel/FcStatusBadge/FluentButton; raw markup is landmark section/header/footer."),
        new("Components/Lenses/OperatorHealth/MemoriesOperatorHealthMatrix.razor", FluentComponent,
            "FluentStack/FluentLabel/FluentButton/FcStatusBadge checks inside the lens shell; generic containers only."),

        new("Components/Recovery/MemoriesRecoveryActionPanel.razor", FluentComponent,
            "FluentMessageBar with FluentStack/FluentLabel/FcStatusBadge/FluentButton recovery grammar in a generic live-region container."),

        new("Components/Evidence/MemoriesEvidenceCockpit.razor.css", LayoutOnlyCss,
            "Only the cockpit's own grid layout; status colour, child layout, and typography moved to Fluent components/tokens."),
        new("Components/Evidence/MemoriesGraphPathSummary.razor.css", LayoutOnlyCss,
            "Grid layout plus the visually-hidden accessibility utility; the reference for an acceptable scoped .razor.css."),
        new("Components/Evidence/MemoriesRetrievalAxisBreakdown.razor.css", LayoutOnlyCss,
            "Grid/list layout with a single Fluent 2 --colorNeutralStroke1 border on list items."),
        new("Components/Evidence/MemoriesSourceCitationStack.razor.css", LayoutOnlyCss,
            "Grid/list layout with a single Fluent 2 --colorNeutralStroke1 border on list items."),
    ];

    /// <summary>Every justified raw semantic/container exception kept in source (AC4).</summary>
    public static IReadOnlyList<AllowlistEntry> Exceptions { get; } = BuildExceptions();

    /// <summary>Resolves the on-disk <c>src/Hexalith.Memories.Web</c> directory from the test assembly.</summary>
    public static string WebProjectDirectory()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Hexalith.Memories.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root (Hexalith.Memories.slnx) above the test assembly.");
        }

        string web = Path.Combine(dir.FullName, "src", "Hexalith.Memories.Web");
        if (!Directory.Exists(web))
        {
            throw new InvalidOperationException($"Web RCL source directory not found at '{web}'.");
        }

        return web;
    }

    /// <summary>The RCL source files (<c>.razor</c> and <c>.razor.css</c>), excluding bin/obj artifacts.</summary>
    public static IReadOnlyList<string> SourceFiles()
    {
        string root = WebProjectDirectory();
        return [.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(static p => !IsGenerated(p))
            .Where(static p => p.EndsWith(".razor", StringComparison.Ordinal)
                || p.EndsWith(".razor.css", StringComparison.Ordinal))
            .Select(p => ToRelative(root, p))
            .OrderBy(static p => p, StringComparer.Ordinal)];
    }

    /// <summary>Normalises a full path to a forward-slashed path relative to the RCL root.</summary>
    public static string ToRelative(string root, string fullPath)
        => Path.GetRelativePath(root, fullPath).Replace('\\', '/');

    private static bool IsGenerated(string path)
    {
        string normalised = path.Replace('\\', '/');
        return normalised.Contains("/obj/", StringComparison.Ordinal)
            || normalised.Contains("/bin/", StringComparison.Ordinal);
    }

    private static AllowlistEntry Landmark(string file, string element, string owner) => new(
        file,
        "<" + element,
        "Semantic landmark element for document structure and assistive-technology navigation.",
        "No Fluent UI V5 / FrontComposer primitive emits HTML landmark elements (FluentStack renders a generic <div>).",
        owner,
        "Remove when FrontComposer or Fluent UI ships a landmark-emitting layout primitive.");

    private static AllowlistEntry ListItem(string file, string element, string owner) => new(
        file,
        "<" + element,
        "List markup conveys ranking/grouping of evidence items to assistive technology.",
        "No Fluent UI V5 / FrontComposer ordered/unordered list primitive.",
        owner,
        "Remove when FrontComposer or Fluent UI ships an ordered/unordered list primitive.");

    private static AllowlistEntry DescriptionList(string file, string element, string owner) => new(
        file,
        "<" + element,
        "Description-list markup pairs field names with sanitised contract values.",
        "No Fluent UI V5 / FrontComposer description-list primitive.",
        owner,
        "Remove when FrontComposer or Fluent UI ships a description-list primitive.");

    private static List<AllowlistEntry> BuildExceptions()
    {
        const string Cockpit = "Components/Evidence/MemoriesEvidenceCockpit.razor";
        const string Graph = "Components/Evidence/MemoriesGraphPathSummary.razor";
        const string Axis = "Components/Evidence/MemoriesRetrievalAxisBreakdown.razor";
        const string Scope = "Components/Evidence/MemoriesScopeHeader.razor";
        const string Source = "Components/Evidence/MemoriesSourceCitationStack.razor";
        const string Trust = "Components/Evidence/MemoriesTrustStrip.razor";
        const string Form = "Components/Forms/MemoriesInteractionForm.razor";
        const string AgentPacket = "Components/Lenses/AgentPacket/MemoriesAgentPacketInspector.razor";
        const string Benchmark = "Components/Lenses/Benchmark/MemoriesBenchmarkResultComparator.razor";
        const string LensShell = "Components/Lenses/MemoriesLensShell.razor";
        const string GraphCss = "Components/Evidence/MemoriesGraphPathSummary.razor.css";

        return
        [
            // Evidence Cockpit (Story 17.1).
            Landmark(Cockpit, "article", "17.1"),
            Landmark(Cockpit, "section", "17.1"),

            // Graph Path Summary (Story 25.7): the pinned Fluent V5 package exposes no description-list primitive.
            Landmark(Graph, "section", "25.7"),
            DescriptionList(Graph, "dl", "25.7"),
            DescriptionList(Graph, "dt", "25.7"),
            DescriptionList(Graph, "dd", "25.7"),

            // Retrieval Axis Breakdown (Story 17.1).
            Landmark(Axis, "section", "17.1"),
            ListItem(Axis, "ol", "17.1"),
            ListItem(Axis, "li", "17.1"),
            DescriptionList(Axis, "dl", "17.1"),
            DescriptionList(Axis, "dt", "17.1"),
            DescriptionList(Axis, "dd", "17.1"),

            // Scope Header (Story 17.1).
            Landmark(Scope, "header", "17.1"),

            // Source Citation Stack (Story 17.1).
            Landmark(Source, "section", "17.1"),
            ListItem(Source, "ol", "17.1"),
            ListItem(Source, "li", "17.1"),
            DescriptionList(Source, "dl", "17.1"),
            DescriptionList(Source, "dt", "17.1"),
            DescriptionList(Source, "dd", "17.1"),

            // Trust Strip (Story 17.1).
            Landmark(Trust, "section", "17.1"),

            // Interaction Form (Story 17.3).
            Landmark(Form, "section", "17.3"),

            // Agent Packet Inspector (Story 17.4).
            Landmark(AgentPacket, "section", "17.4"),
            ListItem(AgentPacket, "ul", "17.4"),
            ListItem(AgentPacket, "li", "17.4"),
            new(
                AgentPacket,
                "<details",
                "Native disclosure for the secondary raw-JSON diagnostic view (a single collapsible region, not sibling titled sections, so the FluentAccordion page-section rule does not apply).",
                "No Fluent UI V5 inline disclosure/expander primitive for a preformatted diagnostic payload.",
                "17.4",
                "Remove when Fluent UI ships an inline disclosure/expander for preformatted content."),
            new(
                AgentPacket,
                "<summary",
                "Disclosure label for the native raw-JSON <details> diagnostic view.",
                "No Fluent UI V5 inline disclosure/expander primitive (see the <details> entry).",
                "17.4",
                "Remove when Fluent UI ships an inline disclosure/expander for preformatted content."),
            new(
                AgentPacket,
                "<pre",
                "Preformatted monospace block for the sanitised raw-JSON copy view, whose text must match the copy payload character-for-character.",
                "No Fluent UI V5 preformatted/code-block primitive.",
                "17.4",
                "Remove when Fluent UI ships a preformatted/code-block primitive."),

            // Benchmark Result Comparator (Story 17.4).
            ListItem(Benchmark, "ul", "17.4"),
            ListItem(Benchmark, "li", "17.4"),

            // Lens Shell (Story 17.4).
            Landmark(LensShell, "section", "17.4"),
            Landmark(LensShell, "header", "17.4"),
            Landmark(LensShell, "footer", "17.4"),

            // Accessibility utility (Story 17.1).
            new(
                GraphCss,
                VisuallyHiddenSelector,
                "Screen-reader-only text utility: visually hidden but exposed to assistive technology (used for the graph path separator).",
                "No Fluent UI V5 visually-hidden helper class or token.",
                "17.1",
                "Remove when Fluent UI ships a visually-hidden text utility."),
        ];
    }
}
