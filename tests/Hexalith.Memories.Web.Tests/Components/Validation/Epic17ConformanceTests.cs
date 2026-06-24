// <copyright file="Epic17ConformanceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>
/// Story 17.6 — the reusable FrontComposer / Fluent UI Blazor V5 conformance gate (AC1, AC2, AC3, AC5, AC7).
/// These tests read the <c>Hexalith.Memories.Web</c> <c>.razor</c> and <c>.razor.css</c> source files from
/// disk at test time (the test project references the RCL but does not embed its sources) and fail closed
/// when: a source file is unclassified, a raw HTML control a Fluent/FrontComposer component owns appears,
/// scoped CSS uses a legacy Fluent v4/FAST token or recreates a theme primitive, a raw semantic/container
/// element is kept without a justified allowlist entry, or an allowlist entry has gone stale. Because the
/// scan covers the whole RCL surface, Stories 17.1–17.5 — and any future Epic 17 story — are all gated and
/// cannot add a new raw UI/CSS exception without an explicit <see cref="Epic17ConformanceAllowlist"/> entry.
/// </summary>
public sealed class Epic17ConformanceTests
{
    private static readonly Regex RazorCommentPattern = new(@"@\*.*?\*@", RegexOptions.Singleline);

    [Fact]
    public void Conformance_EveryRazorAndScopedCssFile_IsClassified()
    {
        IReadOnlyList<string> onDisk = Epic17ConformanceAllowlist.SourceFiles();
        HashSet<string> classified = [.. Epic17ConformanceAllowlist.Files.Select(static f => f.RelativePath)];

        onDisk.ShouldNotBeEmpty("The conformance scan resolved no RCL source files — the path resolver is broken.");

        // Fail closed when a new file appears unclassified.
        foreach (string file in onDisk)
        {
            classified.ShouldContain(file, $"Source file '{file}' is not classified in Epic17ConformanceAllowlist.Files.");
        }

        // Fail closed when a classified file no longer exists on disk (stale register).
        HashSet<string> present = [.. onDisk];
        foreach (string file in classified)
        {
            present.ShouldContain(file, $"Classified file '{file}' no longer exists in the RCL source tree.");
        }
    }

    [Fact]
    public void Conformance_EveryClassification_UsesAKnownKindAndCarriesANote()
    {
        string[] knownKinds =
        [
            Epic17ConformanceAllowlist.FrontComposerComponent,
            Epic17ConformanceAllowlist.FluentComponent,
            Epic17ConformanceAllowlist.FluentWithSemanticMarkup,
            Epic17ConformanceAllowlist.SemanticContainerMarkup,
            Epic17ConformanceAllowlist.LayoutOnlyCss,
            Epic17ConformanceAllowlist.RazorDirectives,
        ];

        foreach (Epic17ConformanceAllowlist.FileClassification row in Epic17ConformanceAllowlist.Files)
        {
            row.RelativePath.ShouldNotBeNullOrWhiteSpace();
            row.Classification.ShouldBeOneOf(knownKinds);
            row.Note.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Conformance_NoRazorFile_UsesARawControlElementAFluentOrFrontComposerComponentOwns()
    {
        // AC2: controls (buttons, inputs, selects, anchors, dialogs, data tables, …) are never allowlisted —
        // a Fluent UI V5 / FrontComposer component owns each of them.
        foreach (string file in RazorSources())
        {
            string markup = MarkupOnly(ReadSource(file));

            foreach (string control in Epic17ConformanceAllowlist.ForbiddenRawControlElements)
            {
                Regex.IsMatch(markup, $@"<{control}(\s|>|/)").ShouldBeFalse(
                    $"'{file}' uses a raw <{control}> element; use the equivalent Fluent UI V5 / FrontComposer component.");
            }
        }
    }

    [Fact]
    public void Conformance_NoScopedCss_UsesLegacyTokensRawHexOrThemePrimitiveRecreation()
    {
        (string Name, Regex Pattern)[] forbidden =
        [
            ("legacy Fluent v4/FAST state token (*-rest / *-hover / *-active)", new Regex(@"--[a-z][\w-]*-(rest|hover|active)\b")),
            ("legacy --type-ramp-* token", new Regex(@"--type-ramp-")),
            ("legacy --accent-* token", new Regex(@"--accent-")),
            ("legacy --neutral-* (v4/FAST) token", new Regex(@"--neutral-")),
            ("legacy --palette-* token", new Regex(@"--palette-")),
            ("raw hex colour", new Regex(@"[\s:(]#[0-9a-fA-F]{3,8}\b")),
            ("font-size theme-primitive declaration", new Regex(@"font-size")),
            ("font-weight theme-primitive declaration", new Regex(@"font-weight")),
            ("line-height theme-primitive declaration", new Regex(@"line-height")),
            ("foreground color declaration", new Regex(@"(?<![-\w])color\s*:")),
        ];

        foreach (string file in CssSources())
        {
            string css = ReadSource(file);

            foreach ((string name, Regex pattern) in forbidden)
            {
                pattern.IsMatch(css).ShouldBeFalse(
                    $"'{file}' contains a {name}; use a Fluent UI V5 component parameter or a Fluent 2 design token instead.");
            }
        }
    }

    [Fact]
    public void Conformance_EveryRawSemanticMarkup_HasAJustifiedAllowlistEntry()
    {
        // AC4 / AC5: a raw semantic/container element that has no Fluent primitive is allowed only with an
        // explicit allowlist entry; a new unallowlisted one fails closed.
        foreach (string file in RazorSources())
        {
            string markup = MarkupOnly(ReadSource(file));

            foreach (string element in Epic17ConformanceAllowlist.TrackedSemanticElements)
            {
                if (!Regex.IsMatch(markup, $@"<{element}(\s|>|/)"))
                {
                    continue;
                }

                Epic17ConformanceAllowlist.Exceptions.ShouldContain(
                    e => e.File == file && e.Pattern == "<" + element,
                    $"'{file}' keeps a raw <{element}> element with no conformance allowlist entry.");
            }
        }

        // The visually-hidden accessibility utility is the one tracked scoped-CSS exception.
        foreach (string file in CssSources())
        {
            if (!ReadSource(file).Contains(Epic17ConformanceAllowlist.VisuallyHiddenSelector, StringComparison.Ordinal))
            {
                continue;
            }

            Epic17ConformanceAllowlist.Exceptions.ShouldContain(
                e => e.File == file && e.Pattern == Epic17ConformanceAllowlist.VisuallyHiddenSelector,
                $"'{file}' keeps the {Epic17ConformanceAllowlist.VisuallyHiddenSelector} utility with no conformance allowlist entry.");
        }
    }

    [Fact]
    public void Conformance_EveryAllowlistEntry_FillsAllSixFieldsAndResolvesToRealSource()
    {
        HashSet<string> classified = [.. Epic17ConformanceAllowlist.Files.Select(static f => f.RelativePath)];

        foreach (Epic17ConformanceAllowlist.AllowlistEntry entry in Epic17ConformanceAllowlist.Exceptions)
        {
            // AC4: all six fields are required.
            entry.File.ShouldNotBeNullOrWhiteSpace();
            entry.Pattern.ShouldNotBeNullOrWhiteSpace();
            entry.Reason.ShouldNotBeNullOrWhiteSpace();
            entry.MissingPrimitive.ShouldNotBeNullOrWhiteSpace();
            entry.OwnerStory.ShouldNotBeNullOrWhiteSpace();
            entry.RemovalCondition.ShouldNotBeNullOrWhiteSpace();

            // No stale entry: the file is classified and the pattern is actually present in the source.
            classified.ShouldContain(entry.File, $"Allowlist entry references unclassified file '{entry.File}'.");
            ReadSource(entry.File).ShouldContain(
                entry.Pattern,
                Shouldly.Case.Sensitive,
                $"Allowlist entry for '{entry.File}' is stale: pattern '{entry.Pattern}' is no longer in the source.");
        }
    }

    [Fact]
    public void Conformance_ClassificationRegister_CoversEveryEpic17PacketSurfaceComponent()
    {
        // AC5: the same conformance register gates every Epic 17 trust surface built by Stories 17.1–17.5.
        foreach (Epic17ValidationInventory.SurfaceRow surface in Epic17ValidationInventory.Surfaces)
        {
            string component = surface.ImplementationSource;
            Epic17ConformanceAllowlist.Files.ShouldContain(
                f => f.RelativePath.EndsWith($"/{component}.razor", StringComparison.Ordinal),
                $"Conformance register is missing the '{component}' component named by the Story 17.5 validation inventory.");
        }
    }

    private static IEnumerable<string> RazorSources()
        => Epic17ConformanceAllowlist.SourceFiles().Where(static f => f.EndsWith(".razor", StringComparison.Ordinal));

    private static IEnumerable<string> CssSources()
        => Epic17ConformanceAllowlist.SourceFiles().Where(static f => f.EndsWith(".razor.css", StringComparison.Ordinal));

    private static string ReadSource(string relativePath)
        => File.ReadAllText(Path.Combine(Epic17ConformanceAllowlist.WebProjectDirectory(), relativePath));

    /// <summary>
    /// Strips Razor comments and C# XML-doc comment lines so <c>@code</c>-block documentation (for example
    /// <c>/// &lt;summary&gt;</c>) is never mistaken for rendered markup by the element scans.
    /// </summary>
    private static string MarkupOnly(string source)
    {
        string withoutRazorComments = RazorCommentPattern.Replace(source, " ");
        IEnumerable<string> lines = withoutRazorComments
            .Split('\n')
            .Where(static line => !line.TrimStart().StartsWith("///", StringComparison.Ordinal));
        return string.Join('\n', lines);
    }
}
