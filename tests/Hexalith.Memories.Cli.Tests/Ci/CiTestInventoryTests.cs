// <copyright file="CiTestInventoryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Ci;

using System.Reflection;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Guards the CI test project inventory shared by local scripts and GitHub Actions.</summary>
public sealed partial class CiTestInventoryTests
{
    private static readonly string[] ExpectedDockerFreeProjects =
    [
        "tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj",
        "tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj",
        "tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj",
        "tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj",
        "tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj",
    ];

    [Fact]
    public void DockerFreeProjectInventory_ShouldMatchExpectedUnitAndContractAssemblies()
    {
        string repoRoot = GetRepoRoot();
        string inventoryPath = Path.Combine(repoRoot, "tools", "test-projects.unit-contract.txt");

        string[] projects = ReadInventory(inventoryPath);

        projects.ShouldBe(ExpectedDockerFreeProjects);
        projects.ShouldNotContain(static p => p.Contains("Benchmarks", StringComparison.Ordinal));
        projects.ShouldNotContain(static p => p.Contains("IntegrationTests", StringComparison.Ordinal));
        projects.ShouldNotContain(static p => p.Contains("TestHelpers", StringComparison.Ordinal));
    }

    [Fact]
    public void IntegrationProjectInventory_ShouldOnlyTargetIntegrationTests()
    {
        string repoRoot = GetRepoRoot();
        string inventoryPath = Path.Combine(repoRoot, "tools", "test-projects.integration-fast.txt");

        string[] projects = ReadInventory(inventoryPath);

        projects.ShouldBe(
        [
            "tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj",
        ]);
    }

    [Fact]
    public void TestScriptsAndCiWorkflow_ShouldUseSharedProjectInventories()
    {
        string repoRoot = GetRepoRoot();
        string powershellScript = File.ReadAllText(Path.Combine(repoRoot, "tools", "test.ps1"));
        string bashScript = File.ReadAllText(Path.Combine(repoRoot, "tools", "test.sh"));
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "ci.yml"));

        powershellScript.ShouldContain("test-projects.unit-contract.txt");
        powershellScript.ShouldContain("test-projects.integration-fast.txt");
        powershellScript.ShouldContain("Category!=Integration&Category!=Benchmark");
        bashScript.ShouldContain("test-projects.unit-contract.txt");
        bashScript.ShouldContain("test-projects.integration-fast.txt");
        bashScript.ShouldContain("Category!=Integration&Category!=Benchmark");
        workflow.ShouldContain("tools/test.sh --filter \"Category!=Integration\"");
        workflow.ShouldContain("tools/test.sh --filter \"Category=Integration&Category!=IntegrationSlow&Category!=Performance\"");
    }

    [Fact]
    public void TestReleaseScript_ShouldDriveFromSharedUnitContractInventory()
    {
        string repoRoot = GetRepoRoot();
        string releaseScript = File.ReadAllText(Path.Combine(repoRoot, "tools", "test-release.ps1"));

        // Story 11.1 Task 0.7 single-source-of-truth: the release lane must read
        // tools/test-projects.unit-contract.txt rather than carry its own hand-maintained list.
        releaseScript.ShouldContain("test-projects.unit-contract.txt");

        // Benchmarks lane is opt-in (Category=Benchmark); release lane must exclude it.
        releaseScript.ShouldContain("Category!=Benchmark");

        // The release lane must not reintroduce a hardcoded array of test-project paths.
        // Each shared inventory entry should appear at most once in the script (as part of an
        // override map keyed by project path), never as the primary execution loop.
        foreach (string project in ExpectedDockerFreeProjects)
        {
            int occurrences = CountOccurrences(releaseScript, project);
            occurrences.ShouldBeLessThanOrEqualTo(1, $"test-release.ps1 hardcodes '{project}' more than once; drive from the shared inventory instead.");
        }
    }

    [Fact]
    public void TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries()
    {
        string repoRoot = GetRepoRoot();

        BaselineFilter[] filters = ReadAcceptedReleaseFilters(Path.Combine(repoRoot, "tools", "test-release.ps1"));
        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(Path.Combine(repoRoot, "_bmad-output", "implementation-artifacts", "deferred-work.md"));

        filters.ShouldAllBe(filter => baselines.Any(baseline => baseline.Key == filter.Key && baseline.TestName == filter.TestName));
        baselines
            .Where(static baseline => baseline.HasReleaseFilter)
            .ShouldAllBe(baseline => filters.Any(filter => filter.Key == baseline.Key && filter.TestName == baseline.TestName));

        if (baselines.Length == 0)
        {
            filters.ShouldBeEmpty("release-lane baseline filters must be empty when no open S11-F* baseline entries remain.");
        }
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static BaselineFilter[] ReadAcceptedReleaseFilters(string path)
    {
        string[] lines = File.ReadAllLines(path);
        List<BaselineFilter> filters = [];
        string? currentKey = null;

        foreach (string line in lines)
        {
            Match keyMatch = DeferredKeyRegex().Match(line);
            if (keyMatch.Success)
            {
                currentKey = keyMatch.Groups["key"].Value;
            }

            Match filterMatch = ProjectFilterRegex().Match(line);
            if (!filterMatch.Success)
            {
                continue;
            }

            currentKey.ShouldNotBeNullOrWhiteSpace($"release baseline filter '{line.Trim()}' must be preceded by a comment naming its S11-F* deferred-work entry.");

            string testName = filterMatch.Groups["test"].Value.Trim();
            testName.Contains('.', StringComparison.Ordinal)
                .ShouldBeTrue($"release baseline filter '{line.Trim()}' must target a single test method, not a class, namespace, or category.");
            filters.Add(new BaselineFilter(currentKey, testName));
        }

        return filters.ToArray();
    }

    private static DeferredBaseline[] ReadOpenDeferredBaselines(string path)
        => ReadDeferredEntries(path)
            .Select(ParseDeferredBaseline)
            .Where(static baseline => baseline is not null)
            .Cast<DeferredBaseline>()
            .ToArray();

    private static IEnumerable<string> ReadDeferredEntries(string path)
    {
        string[] lines = File.ReadAllLines(path);
        List<string> current = [];

        foreach (string line in lines)
        {
            if (line.StartsWith("- **S11-F", StringComparison.Ordinal) && current.Count > 0)
            {
                yield return string.Join(Environment.NewLine, current);
                current.Clear();
            }

            if (line.StartsWith("- **S11-F", StringComparison.Ordinal) || current.Count > 0)
            {
                current.Add(line);
            }
        }

        if (current.Count > 0)
        {
            yield return string.Join(Environment.NewLine, current);
        }
    }

    private static DeferredBaseline? ParseDeferredBaseline(string entry)
    {
        Match keyMatch = DeferredKeyRegex().Match(entry);
        if (!keyMatch.Success || IsResolvedDeferredEntry(entry))
        {
            return null;
        }

        bool baselineRelated = entry.Contains("baseline", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("test-release.ps1", StringComparison.OrdinalIgnoreCase);
        if (!baselineRelated)
        {
            return null;
        }

        Match testMatch = DeferredTestNameRegex().Match(entry);
        testMatch.Success.ShouldBeTrue($"deferred baseline entry '{keyMatch.Groups["key"].Value}' must name the filtered test.");

        bool hasReleaseFilter = entry.Contains("test-release.ps1", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("release lane", StringComparison.OrdinalIgnoreCase);

        return new DeferredBaseline(keyMatch.Groups["key"].Value, testMatch.Groups["test"].Value.Trim(), hasReleaseFilter);
    }

    private static bool IsResolvedDeferredEntry(string entry)
        => entry.Contains("[resolved", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("closed for", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("resolved in", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"(?<key>S11-F[A-Z0-9]+)\.")]
    private static partial Regex DeferredKeyRegex();

    [GeneratedRegex("FullyQualifiedName!~(?<test>[^\"&]+)")]
    private static partial Regex ProjectFilterRegex();

    [GeneratedRegex("`(?<test>[^`]+Tests\\.[^`]+)`")]
    private static partial Regex DeferredTestNameRegex();

    private sealed record BaselineFilter(string Key, string TestName);

    private sealed record DeferredBaseline(string Key, string TestName, bool HasReleaseFilter);

    private static string[] ReadInventory(string path)
        => File.ReadAllLines(path)
            .Select(static line => line.Trim())
            .Where(static line => line.Length > 0 && !line.StartsWith('#'))
            .ToArray();

    private static string GetRepoRoot()
    {
        Assembly assembly = typeof(CiTestInventoryTests).Assembly;
        string? repoRoot = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(static a => string.Equals(a.Key, "RepoRoot", StringComparison.Ordinal))
            ?.Value;

        repoRoot.ShouldNotBeNullOrWhiteSpace();
        return Path.GetFullPath(repoRoot);
    }
}
