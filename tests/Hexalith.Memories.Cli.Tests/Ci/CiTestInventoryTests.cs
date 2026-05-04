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
    public void ReleaseWorkflow_ValidatePackageInventoryStep_RunsCanonicalScript()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Validate package inventory before release");

        step.Shell.ShouldBe("pwsh", "Validate package inventory step must execute under pwsh so PowerShell 7-only validator features (Test-Json -SchemaFile) are available.");
        step.Run.ShouldBe("./tools/validate-release-packages.ps1");
        step.Uses.ShouldBeNull();
    }

    [Fact]
    public void ReleaseWorkflow_RunSemanticReleaseStep_UsesCanonicalCommand()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Run semantic-release");

        step.Run.ShouldBe("npx semantic-release");
        step.Uses.ShouldBeNull();
    }

    [Fact]
    public void ReleaseWorkflow_ReleasePackageValidatorFixtures_RunBeforePublish()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Run release package validator fixtures");

        step.Run.ShouldBe("python3 -m unittest discover -s tests/tooling/release_packages -p \"*_test.py\"");
        step.Uses.ShouldBeNull();
    }

    [Fact]
    public void ReleaseWorkflow_AlertPartialPublishStep_GuardsOnFailureAndReferencesAuditPaths()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Alert partial NuGet publish");

        // Anchored to step name so a future workflow refactor that moves the alert outside of the
        // failure() guard (or to a different shell) fails this assertion instead of degrading
        // partial-publish recovery silently.
        step.If.ShouldBe("failure()");
        step.Shell.ShouldBe("pwsh");
        step.RunBlock.ShouldContain("./tools/create-partial-publish-issue.ps1");
        step.RunBlock.ShouldContain("artifacts/packages/release/publish-summary.json");
    }

    [Fact]
    public void ReleaseWorkflow_TestReleaseStep_DelegatesToSharedScript()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Test unit and non-Docker suite");

        step.Shell.ShouldBe("pwsh");
        step.Run.ShouldBe("./tools/test-release.ps1");
        step.Uses.ShouldBeNull();
    }

    [Fact]
    public void ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha()
    {
        string repoRoot = GetRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml"));

        // Match 'uses: <ref>' allowing a trailing '# v<x.y.z>' comment after the SHA so the pin
        // can carry a human-readable tag annotation alongside the canonical commit SHA.
        Regex usesRegex = new(@"^\s*uses:\s*(?<ref>\S+)");
        Regex shaRegex = new(@"^[0-9a-f]{40}$");
        bool foundAny = false;

        foreach (string line in lines)
        {
            Match match = usesRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            string reference = match.Groups["ref"].Value;
            if (reference.StartsWith("./", StringComparison.Ordinal) || reference.StartsWith("../", StringComparison.Ordinal))
            {
                // Local repository action; SHA-pinning does not apply.
                continue;
            }

            int atIndex = reference.LastIndexOf('@');
            atIndex.ShouldBeGreaterThan(0, $"third-party action reference '{reference}' must include a '@<ref>' suffix");
            string refSuffix = reference[(atIndex + 1)..];
            shaRegex.IsMatch(refSuffix).ShouldBeTrue($"third-party action '{reference}' must be SHA-pinned (got '@{refSuffix}'); follow https://docs.github.com/actions/learn-github-actions/security-hardening-for-github-actions and use a 40-char commit SHA.");
            foundAny = true;
        }

        foundAny.ShouldBeTrue("expected release.yml to declare at least one third-party action under 'uses:'.");
    }

    [Fact]
    public void ReleaseWorkflow_TopLevelPermissions_AreLeastPrivilege()
    {
        IDictionary<string, string> permissions = GetReleaseWorkflowTopLevelMapping("permissions");

        // Locked to exactly the permissions audited by Story 14.2 (AC1 'preserve least privilege').
        // Adding a write scope must come with a documented reason and an updated test, not a silent
        // workflow edit.
        permissions.Count.ShouldBe(3, $"unexpected permissions block contents: {string.Join(", ", permissions.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        permissions.ShouldContainKeyAndValue("contents", "write");
        permissions.ShouldContainKeyAndValue("issues", "write");
        permissions.ShouldContainKeyAndValue("pull-requests", "write");
    }

    [Fact]
    public void ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal()
    {
        IDictionary<string, string> concurrency = GetReleaseWorkflowTopLevelMapping("concurrency");

        // cancel-in-progress: false is the documented trade-off (W19 + S11-FD): cancelling a
        // release mid-publish would convert a recoverable partial-publish into an indeterminate
        // half-state. If a future change must enable cancellation, it has to land an explicit
        // recovery model, not just flip the flag.
        concurrency["group"].ShouldBe("release");
        concurrency["cancel-in-progress"].ShouldBe("false");
    }

    [Fact]
    public void TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries()
    {
        string repoRoot = GetRepoRoot();

        BaselineFilter[] filters = ReadAcceptedReleaseFilters(File.ReadAllLines(Path.Combine(repoRoot, "tools", "test-release.ps1")));
        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(File.ReadAllLines(Path.Combine(repoRoot, "_bmad-output", "implementation-artifacts", "deferred-work.md")));

        filters.ShouldAllBe(filter => baselines.Any(baseline => baseline.Key == filter.Key && baseline.TestName == filter.TestName));
        baselines
            .Where(static baseline => baseline.HasReleaseFilter)
            .ShouldAllBe(baseline => filters.Any(filter => filter.Key == baseline.Key && filter.TestName == baseline.TestName));

        if (baselines.Length == 0)
        {
            filters.ShouldBeEmpty("release-lane baseline filters must be empty when no open S11-F* baseline entries remain.");
        }
    }

    [Fact]
    public void ReadOpenDeferredBaselines_EntriesUnderClosedBySection_AreSkipped()
    {
        string[] fixture =
        [
            "## Closed by: course correction (test-fixture)",
            string.Empty,
            "- **S11-FX. `OldTests.OldMethod` baseline failure.** Released via `tools/test-release.ps1`; remove the filter.",
            string.Empty,
            "## Deferred from: code review of story-test (test-fixture)",
            string.Empty,
            "- **S11-FY. `NewTests.NewMethod` baseline failure.** Currently excluded via `tools/test-release.ps1`.",
        ];

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        baselines.Length.ShouldBe(1);
        baselines[0].Key.ShouldBe("S11-FY");
    }

    [Fact]
    public void ReadOpenDeferredBaselines_InlineResolvedMarker_IsSkipped()
    {
        string[] fixture =
        [
            "- **S11-FZ [resolved in test].** Old baseline tied to `tools/test-release.ps1` filter referencing `OldTests.OldMethod`.",
        ];

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        baselines.ShouldBeEmpty();
    }

    [Fact]
    public void ReadOpenDeferredBaselines_NoOpenBaselines_ReturnsEmpty()
    {
        string[] fixture =
        [
            "## Closed by: course correction (test-fixture)",
            string.Empty,
            "- **S11-FQ [resolved in test].** Done.",
        ];

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        baselines.ShouldBeEmpty();
    }

    [Fact]
    public void ReadAcceptedReleaseFilters_StaleKeyTooFarFromFilter_FailsLoudly()
    {
        string[] fixture =
        [
            "# entry \"S11-FX. Stale historical comment\".",
            "# unrelated comment 1",
            "# unrelated comment 2",
            "# unrelated comment 3",
            "# unrelated comment 4",
            "$projectFilters = @{",
            "    \"foo.csproj\" = \"FullyQualifiedName!~SomeTests.SomeMethod\"",
            "}",
        ];

        Should.Throw<ShouldAssertException>(() => ReadAcceptedReleaseFilters(fixture));
    }

    [Fact]
    public void ReadAcceptedReleaseFilters_NamespaceShape_FailsLoudly()
    {
        string[] fixture =
        [
            "# entry \"S11-FX. Description\".",
            "$projectFilters = @{",
            "    \"foo.csproj\" = \"FullyQualifiedName!~Hexalith.Memories.Server.Tests\"",
            "}",
        ];

        Should.Throw<ShouldAssertException>(() => ReadAcceptedReleaseFilters(fixture));
    }

    [Fact]
    public void ReadAcceptedReleaseFilters_ValidKeyedFilter_ReturnsFilter()
    {
        string[] fixture =
        [
            "# entry \"S11-FX. Description\".",
            "$projectFilters = @{",
            "    \"foo.csproj\" = \"FullyQualifiedName!~SomeTests.SomeMethod\"",
            "}",
        ];

        BaselineFilter[] filters = ReadAcceptedReleaseFilters(fixture);

        BaselineFilter filter = filters.ShouldHaveSingleItem();
        filter.Key.ShouldBe("S11-FX");
        filter.TestName.ShouldBe("SomeTests.SomeMethod");
    }

    [Fact]
    public void ReadAcceptedReleaseFilters_RealRepo_HasNoAcceptedBaselineFilters()
    {
        string repoRoot = GetRepoRoot();

        BaselineFilter[] filters = ReadAcceptedReleaseFilters(File.ReadAllLines(Path.Combine(repoRoot, "tools", "test-release.ps1")));

        filters.ShouldBeEmpty();
    }

    private static ReleaseWorkflowStep GetReleaseWorkflowStep(string stepName)
    {
        string repoRoot = GetRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml"));

        ReleaseWorkflowStep[] steps = ParseReleaseWorkflowSteps(lines);
        ReleaseWorkflowStep[] matches = [.. steps.Where(step => string.Equals(step.Name, stepName, StringComparison.Ordinal))];

        matches.Length.ShouldBe(1, $"expected exactly one step named '{stepName}' in release.yml; found {matches.Length} (steps present: {string.Join(", ", steps.Select(s => $"\"{s.Name}\""))})");
        return matches[0];
    }

    // Hand-rolled step parser: release.yml has a single 'release' job whose steps are at the
    // 6-space indent level. Each step starts with `      - name: <NAME>` and continues at 8-space
    // body indent until the next sibling step or the end of the steps array. The release.yml
    // shape is stable; if a future restructure breaks this assumption these tests will fail
    // loudly with a "no step found" diagnostic rather than silently passing.
    private static ReleaseWorkflowStep[] ParseReleaseWorkflowSteps(string[] lines)
    {
        const string StepHeaderPrefix = "      - name:";
        const string BodyIndent = "        ";

        List<ReleaseWorkflowStep> steps = [];
        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i];
            if (!line.StartsWith(StepHeaderPrefix, StringComparison.Ordinal))
            {
                i++;
                continue;
            }

            string name = line[StepHeaderPrefix.Length..].Trim();
            string? shell = null;
            string? @if = null;
            string? run = null;
            string? uses = null;
            List<string> runBlockLines = [];
            bool inRunBlock = false;

            i++;
            while (i < lines.Length)
            {
                string body = lines[i];
                if (body.StartsWith(StepHeaderPrefix, StringComparison.Ordinal))
                {
                    break;
                }

                // End of jobs.<job>.steps array: any line at or below 4-space indent that is
                // not blank and not part of the step body.
                if (body.Length > 0 && !body.StartsWith(BodyIndent, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(body))
                {
                    break;
                }

                if (inRunBlock)
                {
                    if (string.IsNullOrWhiteSpace(body) || body.StartsWith(BodyIndent + "  ", StringComparison.Ordinal))
                    {
                        runBlockLines.Add(body);
                        i++;
                        continue;
                    }

                    inRunBlock = false;
                }

                string trimmed = body.TrimStart();
                if (trimmed.StartsWith("shell:", StringComparison.Ordinal))
                {
                    shell = trimmed["shell:".Length..].Trim();
                }
                else if (trimmed.StartsWith("if:", StringComparison.Ordinal))
                {
                    @if = StripQuotes(trimmed["if:".Length..].Trim());
                }
                else if (trimmed.StartsWith("uses:", StringComparison.Ordinal))
                {
                    uses = trimmed["uses:".Length..].Trim();
                }
                else if (trimmed.StartsWith("run:", StringComparison.Ordinal))
                {
                    string runTail = trimmed["run:".Length..].Trim();
                    if (runTail == "|" || runTail == ">" || runTail == "|-" || runTail == ">-")
                    {
                        inRunBlock = true;
                    }
                    else
                    {
                        run = runTail;
                    }
                }

                i++;
            }

            steps.Add(new ReleaseWorkflowStep(
                Name: name,
                Shell: shell,
                If: @if,
                Run: run,
                Uses: uses,
                RunBlock: string.Join('\n', runBlockLines)));
        }

        return [.. steps];
    }

    private static IDictionary<string, string> GetReleaseWorkflowTopLevelMapping(string mappingName)
    {
        string repoRoot = GetRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml"));

        // Top-level mapping starts at column 0 with `<name>:` and members are 2-space-indented
        // scalar entries `  <key>: <value>`. Comments and blank lines inside the block are
        // skipped; the block ends at the first non-blank line that does not start with two
        // spaces (i.e. the next top-level key).
        Regex headerRegex = new($"^{Regex.Escape(mappingName)}:\\s*$", RegexOptions.None);
        Dictionary<string, string> mapping = new(StringComparer.Ordinal);
        bool inside = false;

        foreach (string line in lines)
        {
            if (!inside)
            {
                if (headerRegex.IsMatch(line))
                {
                    inside = true;
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("  ", StringComparison.Ordinal))
            {
                break;
            }

            string trimmed = line.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                continue;
            }

            int colon = trimmed.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            string key = trimmed[..colon].Trim();
            string value = StripQuotes(trimmed[(colon + 1)..].Trim());
            mapping[key] = value;
        }

        inside.ShouldBeTrue($"release.yml does not declare a top-level '{mappingName}:' mapping.");
        return mapping;
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private sealed record ReleaseWorkflowStep(string Name, string? Shell, string? If, string? Run, string? Uses, string RunBlock);

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

    private static BaselineFilter[] ReadAcceptedReleaseFilters(string[] lines)
    {
        const int MaxKeyToFilterDistance = 3;
        List<BaselineFilter> filters = [];
        string? currentKey = null;
        int distance = int.MaxValue;

        foreach (string line in lines)
        {
            Match keyMatch = DeferredKeyRegex().Match(line);
            if (keyMatch.Success)
            {
                currentKey = keyMatch.Groups["key"].Value;
                distance = 0;
            }
            else if (distance != int.MaxValue)
            {
                distance++;
            }

            Match filterMatch = ProjectFilterRegex().Match(line);
            if (!filterMatch.Success)
            {
                continue;
            }

            // Proximity guard (review-12-4 P3): require the S11-F* comment to appear within
            // MaxKeyToFilterDistance lines of the filter so a stale far-above key cannot adopt
            // an unrelated filter.
            currentKey.ShouldNotBeNullOrWhiteSpace($"release baseline filter '{line.Trim()}' must be preceded by a comment naming its S11-F* deferred-work entry.");
            distance.ShouldBeLessThanOrEqualTo(MaxKeyToFilterDistance, $"release baseline filter '{line.Trim()}' must be preceded by its S11-F* comment within {MaxKeyToFilterDistance} lines (stale earlier comments are not enough).");

            // Reject multi-directive lines (review-12-4 P6): a single filter line must contain
            // exactly one FullyQualifiedName!~ directive. Multiple directives make the captured
            // test name silently span across them.
            int directiveCount = ProjectFilterRegex().Matches(line).Count;
            directiveCount.ShouldBe(1, $"release baseline filter line '{line.Trim()}' must contain exactly one FullyQualifiedName!~ directive.");

            string testName = filterMatch.Groups["test"].Value.Trim();
            // Class.Method shape (review-12-4 P4): exactly one dot, identifier on each side.
            // Rejects namespaces (Foo.Bar.Baz, multi-dot), classes alone (no dot), wildcards.
            TestNameShape().IsMatch(testName)
                .ShouldBeTrue($"release baseline filter '{line.Trim()}' must target a single Class.Method (got '{testName}'); namespaces, multi-segment names, and wildcards are not narrow enough.");

            filters.Add(new BaselineFilter(currentKey, testName));

            // Reset after consumption so the next filter requires its own fresh S11-F* pairing.
            currentKey = null;
            distance = int.MaxValue;
        }

        return filters.ToArray();
    }

    private static DeferredBaseline[] ReadOpenDeferredBaselines(string[] lines)
        => ReadDeferredEntries(lines)
            .Select(ParseDeferredBaseline)
            .Where(static baseline => baseline is not null)
            .Cast<DeferredBaseline>()
            .ToArray();

    private static IEnumerable<string> ReadDeferredEntries(string[] lines)
    {
        // review-12-4 P1 + P2: bound entry accumulation by section header / sibling-bullet so the
        // last S11-F* entry no longer absorbs everything to EOF, and skip entries whose containing
        // section is `## Closed by ...` (the canonical resolved-section header).
        List<string> current = [];
        bool inResolvedSection = false;

        foreach (string line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (current.Count > 0)
                {
                    yield return string.Join(Environment.NewLine, current);
                    current.Clear();
                }

                inResolvedSection = line.StartsWith("## Closed by", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inResolvedSection)
            {
                continue;
            }

            if (line.StartsWith("- **S11-F", StringComparison.Ordinal))
            {
                if (current.Count > 0)
                {
                    yield return string.Join(Environment.NewLine, current);
                    current.Clear();
                }

                current.Add(line);
                continue;
            }

            // Sibling top-level bullet (different prefix) closes the current entry without
            // being absorbed.
            if (line.StartsWith("- ", StringComparison.Ordinal) && current.Count > 0)
            {
                yield return string.Join(Environment.NewLine, current);
                current.Clear();
                continue;
            }

            if (current.Count > 0)
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
        // review-12-4 P5: anchor test-name and resolved-marker checks to the entry's first line
        // (the bullet header) — descendant prose is not authoritative for those signals.
        string firstLine = entry.Split('\n', 2)[0];

        Match keyMatch = DeferredKeyRegex().Match(firstLine);
        if (!keyMatch.Success || IsResolvedDeferredEntry(firstLine))
        {
            return null;
        }

        bool baselineRelated = entry.Contains("baseline", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("test-release.ps1", StringComparison.OrdinalIgnoreCase);
        if (!baselineRelated)
        {
            return null;
        }

        Match testMatch = DeferredTestNameRegex().Match(firstLine);
        testMatch.Success.ShouldBeTrue($"deferred baseline entry '{keyMatch.Groups["key"].Value}' must name the filtered test on its bullet header line.");

        bool hasReleaseFilter = entry.Contains("test-release.ps1", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("release lane", StringComparison.OrdinalIgnoreCase);

        return new DeferredBaseline(keyMatch.Groups["key"].Value, testMatch.Groups["test"].Value.Trim(), hasReleaseFilter);
    }

    private static bool IsResolvedDeferredEntry(string firstLine)
        => firstLine.Contains("[resolved", StringComparison.OrdinalIgnoreCase)
            || firstLine.Contains("[closed", StringComparison.OrdinalIgnoreCase)
            || firstLine.Contains("[done]", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"(?<key>S11-F[A-Z0-9]+)\.")]
    private static partial Regex DeferredKeyRegex();

    // review-12-4 P6: tighter terminator set — also stops at whitespace so trailing PowerShell
    // tokens cannot bleed into the captured test name.
    [GeneratedRegex(@"FullyQualifiedName!~(?<test>[^\s""&]+)")]
    private static partial Regex ProjectFilterRegex();

    [GeneratedRegex("`(?<test>[^`]+Tests\\.[^`]+)`")]
    private static partial Regex DeferredTestNameRegex();

    // review-12-4 P4: enforce exactly Class.Method shape. Identifier on each side, exactly one dot.
    [GeneratedRegex(@"^[A-Za-z_]\w*\.[A-Za-z_]\w*$")]
    private static partial Regex TestNameShape();

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
