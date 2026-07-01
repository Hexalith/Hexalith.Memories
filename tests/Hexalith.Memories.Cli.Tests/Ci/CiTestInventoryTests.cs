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

    private static readonly string[] AllowedDeferredStatuses = ["open", "resolved", "accepted", "carried-forward"];

    private const string ReleaseLaneTargetArtifact = "tools/test-release.ps1";

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
    public void ReleaseWorkflow_InstallReleaseTooling_UsesNpmCi()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Install release tooling");

        step.Run.ShouldBe("npm ci");
        step.Uses.ShouldBeNull();
    }

    [Fact]
    public void ReleaseWorkflow_ReleasePreflight_RunsBeforeSemanticRelease()
    {
        string repoRoot = GetRepoRoot();
        ReleaseWorkflowStep[] steps = ParseReleaseWorkflowSteps(File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml")));

        int preflightIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Run release preflight", StringComparison.Ordinal));
        int semanticReleaseIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Run semantic-release", StringComparison.Ordinal));

        preflightIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must run the repository-owned release preflight before semantic-release can prepare or publish packages.");
        semanticReleaseIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must retain the semantic-release step.");
        preflightIndex.ShouldBeLessThan(semanticReleaseIndex, "release preflight must run before semantic-release starts publish-capable work.");

        ReleaseWorkflowStep step = steps[preflightIndex];
        step.Shell.ShouldBe("pwsh");
        step.Run.ShouldBe("./tools/release-preflight.ps1");
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
    public void Workflows_ExternalActions_UseAllowedRefs()
    {
        string repoRoot = GetRepoRoot();
        string workflowsRoot = Path.Combine(repoRoot, ".github", "workflows");

        Regex usesRegex = new(@"^\s*uses:\s*(?<ref>\S+)");
        Regex majorTagRegex = new(@"^v[1-9]\d*$");
        bool foundAny = false;

        foreach (string workflowPath in Directory.GetFiles(workflowsRoot, "*.yml"))
        {
            foreach (string line in File.ReadAllLines(workflowPath))
            {
                Match match = usesRegex.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string reference = match.Groups["ref"].Value;
                if (reference.StartsWith("./", StringComparison.Ordinal) || reference.StartsWith("../", StringComparison.Ordinal))
                {
                    // Local repository action; external action version policy does not apply.
                    continue;
                }

                int atIndex = reference.LastIndexOf('@');
                atIndex.ShouldBeGreaterThan(0, $"third-party action reference '{reference}' must include a '@<ref>' suffix");
                string refSuffix = reference[(atIndex + 1)..];
                bool isHexalithBuildsAction = reference.StartsWith("Hexalith/Hexalith.Builds/", StringComparison.Ordinal);
                bool isAllowedRef = isHexalithBuildsAction ? refSuffix == "main" : majorTagRegex.IsMatch(refSuffix);
                isAllowedRef.ShouldBeTrue($"third-party action '{reference}' must use a major version tag like '@v7'; Hexalith.Builds actions must use '@main' so CI consumes the latest shared build logic.");
                foundAny = true;
            }
        }

        foundAny.ShouldBeTrue("expected workflows to declare at least one third-party action under 'uses:'.");
    }

    [Fact]
    public void ReleaseWorkflow_ReleaseJob_DoesNotUseHeadCommitSkipCondition()
    {
        string? releaseJobIf = GetReleaseWorkflowJobScalar("if");

        releaseJobIf.ShouldBeNull("the release workflow should not carry a second, partial skip-token parser over github.event.head_commit.message; GitHub's native push skip handling is the documented full-message contract.");
    }

    [Fact]
    public void ReleaseWorkflow_NoStep_UsesHeadCommitSkipCondition()
    {
        string repoRoot = GetRepoRoot();
        ReleaseWorkflowStep[] steps = ParseReleaseWorkflowSteps(File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml")));

        foreach (ReleaseWorkflowStep step in steps)
        {
            if (step.If is null)
            {
                continue;
            }

            step.If.Contains("github.event.head_commit.message", StringComparison.Ordinal).ShouldBeFalse(
                $"step '{step.Name}' must not re-introduce the partial skip-token parser at the step level; GitHub's native push skip handling is the documented full-message contract for the release workflow.");
        }
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
    public void ReadOpenDeferredBaselines_StructuredOpenBaselineEntry_IsReturned()
    {
        string[] fixture = OpenStructuredBaselineFixture("S11-FX", "SomeTests.SomeMethod");

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        DeferredBaseline baseline = baselines.ShouldHaveSingleItem();
        baseline.Key.ShouldBe("S11-FX");
        baseline.TestName.ShouldBe("SomeTests.SomeMethod");
        baseline.HasReleaseFilter.ShouldBeTrue();
    }

    [Theory]
    [InlineData("resolved")]
    [InlineData("accepted")]
    [InlineData("carried-forward")]
    public void ReadOpenDeferredBaselines_StructuredEntryWithNonOpenStatus_IsSkipped(string status)
    {
        string[] fixture =
        [
            "- **S11-FX. Test fixture entry.**",
            "  - ID: S11-FX",
            $"  - Status: {status}",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            "  - Test: SomeTests.SomeMethod",
            "  - Evidence: closed by fixture story.",
            "  - Rationale: fixture rationale.",
        ];

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        baselines.ShouldBeEmpty();
    }

    [Fact]
    public void ReadOpenDeferredBaselines_NarrativeMentionsBaseline_NotMisclassified()
    {
        string[] fixture =
        [
            "- **12.4-RV99. Narrative mentions `baseline`, `release lane`, and `tools/test-release.ps1` in prose only.**",
            "  Pre-Story-14.5 deferred entries that talk about `baseline` filters in prose without a structured field block must not be classified as baselines.",
            string.Empty,
            "- **12.4-RV100 — narrative entry with structured fields targeting a non-baseline file.**",
            "  - ID: 12.4-RV100",
            "  - Status: open",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs",
            "  - Re-open trigger: future fixture coverage.",
            "  - Rationale: prose mentions of baseline/release lane must not flip a non-release-lane target into baseline classification.",
        ];

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        baselines.ShouldBeEmpty();
    }

    [Fact]
    public void ReadOpenDeferredBaselines_NoStructuredEntries_ReturnsEmpty()
    {
        string[] fixture =
        [
            "## Deferred from: code review of legacy-story (test-fixture)",
            string.Empty,
            "- **12.4-RV101. Legacy prose entry without structured fields.** Existing prose does not match the structured schema, so the parser must ignore it.",
        ];

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        baselines.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ID")]
    [InlineData("Status")]
    [InlineData("Source story")]
    [InlineData("Target artifact")]
    [InlineData("Re-open trigger")]
    public void ReadOpenDeferredBaselines_StructuredEntryMissingRequiredField_FailsLoudly(string missingField)
    {
        string[] fixture = BuildStructuredFixtureWithoutField(missingField);

        ShouldAssertException assertion = Should.Throw<ShouldAssertException>(() => ReadOpenDeferredBaselines(fixture));
        assertion.Message.ShouldContain($"'{missingField}");
    }

    [Fact]
    public void ReadOpenDeferredBaselines_StructuredEntryMissingEvidenceAndRationale_FailsLoudly()
    {
        string[] fixture =
        [
            "- **S11-FX. Test fixture entry.**",
            "  - ID: S11-FX",
            "  - Status: open",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            "  - Test: SomeTests.SomeMethod",
        ];

        ShouldAssertException assertion = Should.Throw<ShouldAssertException>(() => ReadOpenDeferredBaselines(fixture));
        assertion.Message.ShouldContain("Evidence");
        assertion.Message.ShouldContain("Rationale");
    }

    [Fact]
    public void ReadOpenDeferredBaselines_ResolvedEntryWithoutEvidence_FailsLoudly()
    {
        string[] fixture =
        [
            "- **S11-FX. Test fixture entry.**",
            "  - ID: S11-FX",
            "  - Status: resolved",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            "  - Test: SomeTests.SomeMethod",
            "  - Rationale: fixture rationale.",
        ];

        ShouldAssertException assertion = Should.Throw<ShouldAssertException>(() => ReadOpenDeferredBaselines(fixture));
        assertion.Message.ShouldContain("Evidence");
    }

    [Theory]
    [InlineData("accepted")]
    [InlineData("carried-forward")]
    public void ReadOpenDeferredBaselines_NonResolvedDispositionWithoutRationale_FailsLoudly(string status)
    {
        string[] fixture =
        [
            "- **S11-FX. Test fixture entry.**",
            "  - ID: S11-FX",
            $"  - Status: {status}",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            "  - Test: SomeTests.SomeMethod",
            "  - Evidence: fixture evidence.",
        ];

        ShouldAssertException assertion = Should.Throw<ShouldAssertException>(() => ReadOpenDeferredBaselines(fixture));
        assertion.Message.ShouldContain("Rationale");
    }

    [Theory]
    [InlineData("done")]
    [InlineData("closed")]
    [InlineData("fixed")]
    [InlineData("deferred-again")]
    [InlineData("Open")]
    [InlineData("OPEN")]
    public void ReadOpenDeferredBaselines_StructuredEntryWithInvalidStatus_FailsLoudly(string invalidStatus)
    {
        string[] fixture =
        [
            "- **S11-FX. Test fixture entry.**",
            "  - ID: S11-FX",
            $"  - Status: {invalidStatus}",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            "  - Test: SomeTests.SomeMethod",
            "  - Rationale: fixture rationale.",
        ];

        ShouldAssertException assertion = Should.Throw<ShouldAssertException>(() => ReadOpenDeferredBaselines(fixture));
        assertion.Message.ShouldContain("Status");
    }

    [Theory]
    [InlineData("12x4-RV6")]
    [InlineData("112.4-RV6")]
    [InlineData("12.4-RV60")]
    public void ReadOpenDeferredBaselines_StructuredEntryWithSimilarId_DoesNotCountAsTargetId(string similarId)
    {
        string[] fixture = OpenStructuredBaselineFixture(similarId, "SomeTests.SomeMethod");

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(fixture);

        DeferredBaseline baseline = baselines.ShouldHaveSingleItem();
        baseline.Key.ShouldBe(similarId);
        baseline.Key.ShouldNotBe("12.4-RV6");
    }

    [Fact]
    public void ReadOpenDeferredBaselines_OpenBaselineEntryWithoutTestField_FailsLoudly()
    {
        string[] fixture =
        [
            "- **S11-FX. Test fixture entry.**",
            "  - ID: S11-FX",
            "  - Status: open",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            "  - Rationale: fixture rationale.",
        ];

        ShouldAssertException assertion = Should.Throw<ShouldAssertException>(() => ReadOpenDeferredBaselines(fixture));
        assertion.Message.ShouldContain("Test");
    }

    [Fact]
    public void ReadOpenDeferredBaselines_OpenBaselineEntryWithMultiSegmentTestName_FailsLoudly()
    {
        string[] fixture = OpenStructuredBaselineFixture("S11-FX", "Hexalith.Memories.Server.Tests.SomeTests.SomeMethod");

        ShouldAssertException assertion = Should.Throw<ShouldAssertException>(() => ReadOpenDeferredBaselines(fixture));
        assertion.Message.ShouldContain("Class.Method");
    }

    [Fact]
    public void ReadOpenDeferredBaselines_RealRepo_DoesNotThrowAndReturnsOnlyTargetedEntries()
    {
        string repoRoot = GetRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(repoRoot, "_bmad-output", "implementation-artifacts", "deferred-work.md"));

        DeferredBaseline[] baselines = ReadOpenDeferredBaselines(lines);

        // Story 14.5 close-out: no open structured S11-F* baseline entries exist in the real
        // register; the four migrated entries (12.4-RV6, 12.4-RV19, 12.6-RV2, 13.7-RV5) are all
        // resolved and target non-release-lane artifacts. If a future migration introduces a real
        // open baseline entry, this test catches the change explicitly.
        baselines.ShouldBeEmpty();
    }

    private static string[] OpenStructuredBaselineFixture(string id, string testName)
        =>
        [
            $"- **{id}. Test fixture entry.**",
            $"  - ID: {id}",
            "  - Status: open",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            $"  - Test: {testName}",
            "  - Rationale: fixture rationale.",
        ];

    private static string[] BuildStructuredFixtureWithoutField(string missingField)
    {
        string[] fields =
        [
            "  - ID: S11-FX",
            "  - Status: open",
            "  - Source story: 14-5-fixture",
            "  - Target artifact: tools/test-release.ps1",
            "  - Re-open trigger: fixture trigger",
            "  - Test: SomeTests.SomeMethod",
            "  - Rationale: fixture rationale.",
        ];

        List<string> kept =
        [
            "- **S11-FX. Test fixture entry.**",
        ];
        foreach (string field in fields)
        {
            if (field.Contains($"- {missingField}:", StringComparison.Ordinal))
            {
                continue;
            }

            kept.Add(field);
        }

        return [.. kept];
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

    private static string? GetReleaseWorkflowJobScalar(string scalarName)
    {
        string repoRoot = GetRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml"));
        string prefix = $"    {scalarName}:";
        bool insideReleaseJob = false;

        foreach (string line in lines)
        {
            // TrimEnd tolerates CRLF-checkout line endings (the patch path emits a
            // "LF will be replaced by CRLF" warning); without it the equality check would
            // miss the job header and the helper would fall through with the wrong invariant.
            string trimmedLine = line.TrimEnd();
            if (!insideReleaseJob)
            {
                if (trimmedLine == "  release:")
                {
                    insideReleaseJob = true;
                }

                continue;
            }

            if (line.Length > 0 && line.StartsWith("  ", StringComparison.Ordinal) && !line.StartsWith("    ", StringComparison.Ordinal))
            {
                break;
            }

            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return StripQuotes(line[prefix.Length..].Trim());
            }
        }

        insideReleaseJob.ShouldBeTrue("release.yml does not declare jobs.release.");
        return null;
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
    {
        StructuredDeferredEntry[] entries = ParseStructuredDeferredEntries(lines);
        List<DeferredBaseline> baselines = [];

        foreach (StructuredDeferredEntry entry in entries)
        {
            if (entry.Status != "open")
            {
                continue;
            }

            if (!string.Equals(entry.TargetArtifact, ReleaseLaneTargetArtifact, StringComparison.Ordinal))
            {
                continue;
            }

            entry.Test.ShouldNotBeNullOrWhiteSpace($"open structured deferred entry '{entry.Id}' targets '{ReleaseLaneTargetArtifact}' but is missing the required 'Test:' field.");
            TestNameShape().IsMatch(entry.Test!).ShouldBeTrue($"open structured deferred entry '{entry.Id}' has 'Test: {entry.Test}' which is not a single Class.Method (namespaces, multi-segment names, and wildcards are rejected).");

            baselines.Add(new DeferredBaseline(entry.Id, entry.Test!, HasReleaseFilter: true));
        }

        return [.. baselines];
    }

    private static StructuredDeferredEntry[] ParseStructuredDeferredEntries(string[] lines)
    {
        List<StructuredDeferredEntry> result = [];
        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        int? idLineIndex = null;

        for (int i = 0; i < lines.Length; i++)
        {
            Match match = StructuredFieldRegex().Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            string label = match.Groups["label"].Value;
            string value = match.Groups["value"].Value.Trim();

            if (label == "ID")
            {
                FlushPendingStructuredEntry(result, fields, idLineIndex);
                fields.Clear();
                idLineIndex = i;
            }

            if (idLineIndex is null)
            {
                label.ShouldBe("ID", $"structured deferred field '{label}:' near line {i + 1} appears before an 'ID:' field; structured entries must start with 'ID:'.");
            }

            // review-14-5: a duplicate field within a single entry must fail closed so an editor
            // mistake (e.g. two Status lines disagreeing) cannot silently keep the first or last
            // value depending on hash insertion order.
            fields.ContainsKey(label).ShouldBeFalse($"structured deferred entry has a duplicated '{label}:' field.");
            fields[label] = value;
        }

        FlushPendingStructuredEntry(result, fields, idLineIndex);
        return [.. result];
    }

    private static void FlushPendingStructuredEntry(
        List<StructuredDeferredEntry> result,
        Dictionary<string, string> fields,
        int? idLineIndex)
    {
        if (idLineIndex is null || fields.Count == 0)
        {
            return;
        }

        fields.TryGetValue("ID", out string? id);
        fields.TryGetValue("Status", out string? status);
        fields.TryGetValue("Source story", out string? sourceStory);
        fields.TryGetValue("Target artifact", out string? targetArtifact);
        fields.TryGetValue("Re-open trigger", out string? reopenTrigger);
        fields.TryGetValue("Evidence", out string? evidence);
        fields.TryGetValue("Rationale", out string? rationale);
        fields.TryGetValue("Test", out string? test);

        id.ShouldNotBeNullOrWhiteSpace($"structured deferred entry near line {idLineIndex} is missing the 'ID:' value.");
        StructuredIdShape().IsMatch(id!).ShouldBeTrue($"structured deferred entry has 'ID: {id}' which is not a single non-whitespace token of allowed characters (alphanumeric, dot, dash).");

        status.ShouldNotBeNullOrWhiteSpace($"structured deferred entry '{id}' is missing the required 'Status:' field.");
        sourceStory.ShouldNotBeNullOrWhiteSpace($"structured deferred entry '{id}' is missing the required 'Source story:' field.");
        targetArtifact.ShouldNotBeNullOrWhiteSpace($"structured deferred entry '{id}' is missing the required 'Target artifact:' field.");
        reopenTrigger.ShouldNotBeNullOrWhiteSpace($"structured deferred entry '{id}' is missing the required 'Re-open trigger:' field.");

        AllowedDeferredStatuses.ShouldContain(status!, $"structured deferred entry '{id}' has invalid 'Status: {status}'. Allowed values: {string.Join(", ", AllowedDeferredStatuses)}.");

        if (status == "resolved")
        {
            evidence.ShouldNotBeNullOrWhiteSpace($"structured deferred entry '{id}' has 'Status: resolved' and must include an 'Evidence:' field.");
        }
        else if (status is "accepted" or "carried-forward")
        {
            rationale.ShouldNotBeNullOrWhiteSpace($"structured deferred entry '{id}' has 'Status: {status}' and must include a 'Rationale:' field.");
        }
        else
        {
            bool hasEvidenceOrRationale = !string.IsNullOrWhiteSpace(evidence) || !string.IsNullOrWhiteSpace(rationale);
            hasEvidenceOrRationale.ShouldBeTrue($"structured deferred entry '{id}' must include either 'Evidence:' or 'Rationale:' (or both).");
        }

        result.Add(new StructuredDeferredEntry(id!, status!, sourceStory!, targetArtifact!, reopenTrigger!, evidence, rationale, test));
    }

    [GeneratedRegex(@"(?<key>S11-F[A-Z0-9]+)\.")]
    private static partial Regex DeferredKeyRegex();

    // review-12-4 P6: tighter terminator set — also stops at whitespace so trailing PowerShell
    // tokens cannot bleed into the captured test name.
    [GeneratedRegex(@"FullyQualifiedName!~(?<test>[^\s""&]+)")]
    private static partial Regex ProjectFilterRegex();

    // review-12-4 P4: enforce exactly Class.Method shape. Identifier on each side, exactly one dot.
    [GeneratedRegex(@"^[A-Za-z_]\w*\.[A-Za-z_]\w*$")]
    private static partial Regex TestNameShape();

    // Story 14.5: structured-field reader for deferred-work.md. The label group enumerates the
    // closed schema vocabulary so a typo like 'Statu:' cannot silently start a malformed entry,
    // and the value group stops at end-of-line so trailing prose cannot bleed into a captured
    // value. Bullet markers ('- ', '* ') and indentation are tolerated so the schema can render
    // as nested Markdown sub-bullets.
    [GeneratedRegex(@"^\s*(?:[-*]\s+)?(?<label>ID|Status|Source story|Target artifact|Re-open trigger|Evidence|Rationale|Test):\s+(?<value>.+?)\s*$")]
    private static partial Regex StructuredFieldRegex();

    // Story 14.5: ID values must be a single token of alphanumerics, dot, and dash so that
    // similar-but-different IDs (12x4-RV6, 112.4-RV6) are captured verbatim and fail an exact
    // equality check against 12.4-RV6 instead of being absorbed by a permissive regex.
    [GeneratedRegex(@"^[A-Za-z0-9.\-]+$")]
    private static partial Regex StructuredIdShape();

    private sealed record StructuredDeferredEntry(
        string Id,
        string Status,
        string SourceStory,
        string TargetArtifact,
        string ReopenTrigger,
        string? Evidence,
        string? Rationale,
        string? Test);

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
