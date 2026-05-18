// <copyright file="SubmoduleGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

/// <summary>Story 15.6 regression guards for root-level submodule validation.</summary>
public sealed partial class SubmoduleGuardTests
{
    [Fact]
    public void DirectoryBuildProps_CheckSubmodulesIncludesEveryRootGitmoduleEntry()
    {
        string repoRoot = LocateRepoRoot();
        string gitmodules = File.ReadAllText(Path.Combine(repoRoot, ".gitmodules"));
        XDocument props = XDocument.Load(Path.Combine(repoRoot, "Directory.Build.props"));

        string[] modulePaths = [.. GitmodulePathRegex()
            .Matches(gitmodules)
            .Select(match => match.Groups["path"].Value.Trim())];
        string[] guardedModules = [.. props
            .Descendants("RequiredRootSubmodule")
            .Select(element => element.Attribute("Include")?.Value)
            .OfType<string>()];

        guardedModules.ShouldBe(
            modulePaths,
            ignoreOrder: true,
            customMessage: "CheckSubmodules must guard every root-level entry in .gitmodules.");
    }

    [Fact]
    public void DirectoryBuildProps_CheckSubmodulesUsesItemDrivenErrorMessage()
    {
        string content = File.ReadAllText(Path.Combine(LocateRepoRoot(), "Directory.Build.props"));

        content.ShouldContain(
            "%(RequiredRootSubmodule.Identity)/.git",
            Case.Sensitive,
            "The guard should iterate RequiredRootSubmodule items instead of hard-coding one Error per module.");
        content.ShouldContain(
            "Git submodule '%(RequiredRootSubmodule.Identity)' is missing",
            Case.Sensitive,
            "Missing-submodule failures should name the exact root-level submodule.");
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, ".gitmodules"))
                && Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the repository root by walking up from '{AppContext.BaseDirectory}'.");
    }

    [GeneratedRegex(@"^\s*path\s*=\s*(?<path>[^\r\n]+)", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex GitmodulePathRegex();
}
