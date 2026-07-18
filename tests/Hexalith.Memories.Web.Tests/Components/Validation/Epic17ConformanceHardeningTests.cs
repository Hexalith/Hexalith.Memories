// <copyright file="Epic17ConformanceHardeningTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>
/// Story 17.6 — gates that harden the conformance machinery itself and close coverage the source scan in
/// <see cref="Epic17ConformanceTests"/> left open:
/// <list type="bullet">
///   <item>the centrally pinned Fluent UI Blazor package version is the authoritative one (AC6);</item>
///   <item>the forbidden-control and tracked-semantic element sets stay disjoint, and the allowlist holds no
///   duplicate entries, so the register cannot silently classify a control as an allowlistable element;</item>
///   <item>a hand-authored inline <c>style</c> attribute or <c>&lt;style&gt;</c> block in a <c>.razor</c>
///   file cannot smuggle a legacy token, raw hex, or theme-primitive declaration past the scoped-CSS scan
///   (which only reads <c>.razor.css</c> files).</item>
/// </list>
/// </summary>
public sealed class Epic17ConformanceHardeningTests
{
    /// <summary>The Fluent UI Blazor V5 version centrally pinned and validated for Epic 17 (AC6).</summary>
    private const string PinnedFluentVersion = "5.0.0-rc.4-26180.1";

    private const string FluentPackageId = "Microsoft.FluentUI.AspNetCore.Components";

    private static readonly Regex RazorCommentPattern = new(@"@\*.*?\*@", RegexOptions.Singleline);

    private static readonly (string Name, Regex Pattern)[] ForbiddenInlineStyle =
    [
        ("legacy Fluent v4/FAST state token (*-rest / *-hover / *-active)", new Regex(@"--[a-z][\w-]*-(rest|hover|active)\b")),
        ("legacy --type-ramp-* token", new Regex(@"--type-ramp-")),
        ("legacy --accent-* token", new Regex(@"--accent-")),
        ("legacy --neutral-* (v4/FAST) token", new Regex(@"--neutral-")),
        ("legacy --palette-* token", new Regex(@"--palette-")),
        ("raw hex colour", new Regex(@"#[0-9a-fA-F]{3,8}\b")),
        ("font-size theme-primitive declaration", new Regex(@"font-size")),
        ("font-weight theme-primitive declaration", new Regex(@"font-weight")),
        ("line-height theme-primitive declaration", new Regex(@"line-height")),
        ("foreground color declaration", new Regex(@"(?<![-\w])color\s*:")),
    ];

    private static readonly Regex InlineStyleAttribute = new("style\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);

    private static readonly Regex StyleBlock = new("<style[^>]*>(.*?)</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    [Fact]
    public void PackageLock_DirectoryPackagesProps_PinsTheAuthoritativeFluentUiVersion()
    {
        // AC6: component API choices follow the centrally evaluated Fluent UI V5 RC, not the incompatible
        // Fluent UI MCP documentation target. The consumer wrapper imports the shared authority and never
        // duplicates its PackageVersion items locally.
        EvaluatedPackageVersion(FluentPackageId).ShouldBe(
            PinnedFluentVersion,
            $"The shared catalog must pin {FluentPackageId} to the Epic 17 authoritative version.");
    }

    [Fact]
    public void PackageLock_NoMemoriesProject_OverridesTheFluentUiVersionLocally()
    {
        // AC6 + central package management: a per-project Version attribute would let a project drift off the
        // pinned prerelease and silently copy an incompatible MCP-documented API signature.
        string root = RepositoryRoot();
        IEnumerable<string> projects = new[] { "src", "tests" }
            .Select(d => Path.Combine(root, d))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.csproj", SearchOption.AllDirectories))
            .Where(static p => !p.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal));

        foreach (string project in projects)
        {
            foreach (Match reference in Regex.Matches(
                File.ReadAllText(project),
                $@"<PackageReference\s+Include=""{Regex.Escape(FluentPackageId)}""[^>]*>"))
            {
                reference.Value.ShouldNotContain(
                    "Version",
                    Shouldly.Case.Insensitive,
                    $"'{Path.GetFileName(project)}' pins {FluentPackageId} locally; the version must come only from Directory.Packages.props.");
            }
        }
    }

    [Fact]
    public void Register_ForbiddenControlAndTrackedSemanticElementSets_AreDisjoint()
    {
        // A control a Fluent/FrontComposer component owns (AC2, hard failure) must never also be an
        // allowlistable semantic element (AC4) — that overlap would make a file's classification ambiguous.
        IEnumerable<string> overlap = Epic17ConformanceAllowlist.ForbiddenRawControlElements
            .Intersect(Epic17ConformanceAllowlist.TrackedSemanticElements);

        overlap.ShouldBeEmpty(
            "An element is both a forbidden control and an allowlistable semantic element: " +
            string.Join(", ", overlap));
    }

    [Fact]
    public void Register_AllowlistEntries_HaveNoDuplicateFileAndPatternPairs()
    {
        // A duplicate (File, Pattern) entry would let a stale exception hide behind an identical live one.
        IEnumerable<string> duplicates = Epic17ConformanceAllowlist.Exceptions
            .GroupBy(static e => $"{e.File}|{e.Pattern}")
            .Where(static g => g.Count() > 1)
            .Select(static g => g.Key);

        duplicates.ShouldBeEmpty("Duplicate allowlist (File, Pattern) entries: " + string.Join(", ", duplicates));
    }

    [Fact]
    public void InlineStyle_NoRazorFile_SmugglesALegacyTokenHexOrThemePrimitivePastTheScopedCssScan()
    {
        // The scoped-CSS scan reads only .razor.css. An inline style="…" attribute or a <style> block inside a
        // .razor file is a bypass for the very tokens/primitives AC3 forbids, so it is scanned here too.
        foreach (string file in Epic17ConformanceAllowlist.SourceFiles()
            .Where(static f => f.EndsWith(".razor", StringComparison.Ordinal)))
        {
            string source = RazorCommentPattern.Replace(
                File.ReadAllText(Path.Combine(Epic17ConformanceAllowlist.WebProjectDirectory(), file)),
                " ");

            IEnumerable<string> inlineCss = InlineStyleAttribute.Matches(source).Select(static m => m.Groups[1].Value)
                .Concat(StyleBlock.Matches(source).Select(static m => m.Groups[1].Value));

            foreach (string css in inlineCss)
            {
                foreach ((string name, Regex pattern) in ForbiddenInlineStyle)
                {
                    pattern.IsMatch(css).ShouldBeFalse(
                        $"'{file}' has an inline style containing a {name}; use a Fluent UI V5 component parameter or a Fluent 2 design token instead.");
                }
            }
        }
    }

    /// <summary>Resolves the repository root (the directory holding <c>Directory.Packages.props</c>).</summary>
    private static string RepositoryRoot()
    {
        DirectoryInfo root = Directory.GetParent(Epic17ConformanceAllowlist.WebProjectDirectory())?.Parent
            ?? throw new InvalidOperationException("Could not resolve the repository root above src/Hexalith.Memories.Web.");

        File.Exists(Path.Combine(root.FullName, "Directory.Packages.props")).ShouldBeTrue(
            $"Directory.Packages.props was not found at the resolved repository root '{root.FullName}'.");

        return root.FullName;
    }

    private static string EvaluatedPackageVersion(string packageId)
    {
        string root = RepositoryRoot();
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = root,
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(Path.Combine(root, "Directory.Packages.props"));
        startInfo.ArgumentList.Add("-getItem:PackageVersion");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet msbuild to evaluate Directory.Packages.props.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, $"Directory.Packages.props evaluation failed:{Environment.NewLine}{error}");

        using JsonDocument evaluation = JsonDocument.Parse(output);
        JsonElement item = evaluation.RootElement.GetProperty("Items").GetProperty("PackageVersion")
            .EnumerateArray()
            .Single(element => string.Equals(element.GetProperty("Identity").GetString(), packageId, StringComparison.OrdinalIgnoreCase));

        return item.GetProperty("Version").GetString()
            ?? throw new InvalidOperationException($"The evaluated PackageVersion '{packageId}' has no Version metadata.");
    }
}
