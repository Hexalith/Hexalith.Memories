// <copyright file="CiTestInventoryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Ci;

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

/// <summary>Guards the CI test project inventory shared by local scripts and GitHub Actions.</summary>
public sealed partial class CiTestInventoryTests
{
    private static readonly string[] ExpectedDockerFreeProjects =
    [
        "tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj",
        "tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj",
        "tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj",
        "tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj",
        "tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj",
        "tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj",
        "tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj",
    ];

    private static readonly string[] AllowedDeferredStatuses = ["open", "resolved", "accepted", "carried-forward"];

    private const string ReleaseLaneTargetArtifact = "tools/test-release.ps1";

    [Fact]
    public void DockerFreeProjectInventory_ShouldMatchExpectedUnitAndContractAssemblies()
    {
        string repoRoot = GetRepoRoot();
        string inventoryPath = Path.Combine(repoRoot, "tools", "test-projects.unit-contract.txt");

        string[] projects = ReadInventory(inventoryPath);

        projects.Distinct(StringComparer.Ordinal).Count().ShouldBe(
            projects.Length,
            "Docker-free project inventory must not contain duplicate runnable-project entries.");
        projects.ShouldAllBe(
            project => File.Exists(Path.Combine(repoRoot, project)),
            "Docker-free project inventory must not contain stale or nonexistent runnable-project entries.");
        projects.ShouldBe(ExpectedDockerFreeProjects);
        projects.ShouldNotContain(static p => p.Contains("Benchmarks", StringComparison.Ordinal));
        projects.ShouldNotContain(static p => p.Contains("IntegrationTests", StringComparison.Ordinal));
        projects.ShouldNotContain(static p => p.Contains("TestHelpers", StringComparison.Ordinal));
    }

    [Fact]
    public void SourceControl_ShouldNotTrackGeneratedProjectCaches()
    {
        string repoRoot = GetRepoRoot();
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(repoRoot);
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("*.csproj.lscache");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git for the generated-cache source-control guard.");
        string trackedCaches = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.ShouldBe(0, standardError);
        string[] trackedCachesPresentInWorktree = trackedCaches
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(relativePath => File.Exists(Path.Combine(repoRoot, relativePath)))
            .ToArray();
        trackedCachesPresentInWorktree.ShouldBeEmpty("Generated *.csproj.lscache files must never be tracked.");
    }

    [Fact]
    public void BackendProjects_ShouldNotUseRedisCompatibilityPackageAsDependencyFacade()
    {
        string repoRoot = GetRepoRoot();
        string[] projectPaths =
        [
            "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj",
            "tests/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.csproj",
        ];

        foreach (string projectPath in projectPaths)
        {
            string project = File.ReadAllText(Path.Combine(repoRoot, projectPath));
            project.ShouldNotContain(
                "Hexalith.Memories.Redis.csproj",
                Case.Sensitive);
        }
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
    public void ReleaseTooling_ManifestAndLockPinSecureSemanticReleaseGraph()
    {
        string repoRoot = GetRepoRoot();
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, "package.json")));
        using JsonDocument lockFile = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, "package-lock.json")));

        Dictionary<string, string> expectedDirectDependencies = new(StringComparer.Ordinal)
        {
            ["@commitlint/cli"] = "^21.1.0",
            ["@commitlint/config-conventional"] = "^21.1.0",
            ["@semantic-release/commit-analyzer"] = "^13.0.1",
            ["@semantic-release/exec"] = "^7.1.0",
            ["@semantic-release/github"] = "^12.0.8",
            ["@semantic-release/release-notes-generator"] = "^14.1.1",
            ["cosmiconfig"] = "9.0.2",
            ["semantic-release"] = "25.0.8",
        };

        Dictionary<string, string> manifestDependencies = ReadStringMapping(manifest.RootElement.GetProperty("devDependencies"));
        manifestDependencies.ShouldBe(expectedDirectDependencies, ignoreOrder: true);
        JsonElement scripts = manifest.RootElement.GetProperty("scripts");
        scripts.GetProperty("verify:semantic-release-config").GetString()
            .ShouldBe("node ./tools/verify-semantic-release-config.mjs && node ./tools/verify-semantic-release-config.mjs --self-test");
        string[] scriptNames = [.. scripts.EnumerateObject().Select(static property => property.Name)];
        foreach (string lifecycleScript in new[] { "preinstall", "install", "postinstall", "prepublish", "prepublishOnly", "preprepare", "prepare", "postprepare" })
        {
            scriptNames.ShouldNotContain(lifecycleScript, "root npm ci must not execute repository-defined install lifecycle code.");
        }

        foreach (string forbiddenSection in new[] { "workspaces", "dependencies", "optionalDependencies", "peerDependencies", "peerDependenciesMeta", "bundleDependencies", "bundledDependencies" })
        {
            manifest.RootElement.TryGetProperty(forbiddenSection, out _).ShouldBeFalse($"root release tooling must not define the '{forbiddenSection}' section.");
        }

        JsonElement overrides = manifest.RootElement.GetProperty("overrides");
        overrides.EnumerateObject().Select(static property => property.Name).ShouldBe(["fast-uri", "js-yaml", "semantic-release"]);
        overrides.GetProperty("fast-uri").GetString().ShouldBe("3.1.5");
        overrides.GetProperty("js-yaml").GetString().ShouldBe("4.3.1");
        JsonElement semanticReleaseOverride = overrides.GetProperty("semantic-release");
        semanticReleaseOverride.EnumerateObject().Select(static property => property.Name).ShouldBe(["@semantic-release/npm"]);
        semanticReleaseOverride.GetProperty("@semantic-release/npm").GetString().ShouldBe("npm:@semantic-release/error@4.0.0");
        overrides.TryGetProperty("npm", out _).ShouldBeFalse("the vulnerable npm CLI bundle must not be restored through a global override.");

        lockFile.RootElement.GetProperty("lockfileVersion").GetInt32().ShouldBe(3);
        JsonElement lockedPackages = lockFile.RootElement.GetProperty("packages");
        ReadStringMapping(lockedPackages.GetProperty(string.Empty).GetProperty("devDependencies"))
            .ShouldBe(expectedDirectDependencies, ignoreOrder: true);

        Dictionary<string, string> expectedLockedVersions = new(StringComparer.Ordinal)
        {
            ["node_modules/@commitlint/cli"] = "21.1.0",
            ["node_modules/@commitlint/config-conventional"] = "21.1.0",
            ["node_modules/@semantic-release/commit-analyzer"] = "13.0.1",
            ["node_modules/@semantic-release/exec"] = "7.1.0",
            ["node_modules/@semantic-release/github"] = "12.0.8",
            ["node_modules/@semantic-release/release-notes-generator"] = "14.1.1",
            ["node_modules/cosmiconfig"] = "9.0.2",
            ["node_modules/fast-uri"] = "3.1.5",
            ["node_modules/js-yaml"] = "4.3.1",
            ["node_modules/semantic-release"] = "25.0.8",
            ["node_modules/undici"] = "7.29.0",
        };
        foreach ((string packagePath, string expectedVersion) in expectedLockedVersions)
        {
            lockedPackages.GetProperty(packagePath).GetProperty("version").GetString().ShouldBe(expectedVersion);
        }

        JsonElement lockedUndici = lockedPackages.GetProperty("node_modules/undici");
        lockedUndici.GetProperty("resolved").GetString()
            .ShouldBe("https://registry.npmjs.org/undici/-/undici-7.29.0.tgz");
        lockedUndici.GetProperty("integrity").GetString()
            .ShouldBe("sha512-IDxfleLmmbSskfWSUATiN1nfn2rDuvnMOqb5CWR92iIfojA0Ud+ulOAAEQ57LPr9rWmsreUyf5lwyao+7GNNVw==");
        lockedPackages.GetProperty("node_modules/@semantic-release/github")
            .GetProperty("dependencies")
            .GetProperty("undici")
            .GetString()
            .ShouldBe("^7.0.0");

        JsonProperty[] npmPluginLockPaths = [.. lockedPackages.EnumerateObject()
            .Where(static package => package.Name.EndsWith("node_modules/@semantic-release/npm", StringComparison.Ordinal))];
        npmPluginLockPaths.Length.ShouldBe(1, "every semantic-release npm plugin lock path must collapse to the single reviewed alias.");
        GetLockedPackageName(npmPluginLockPaths[0]).ShouldBe("@semantic-release/error");
        npmPluginLockPaths[0].Value.GetProperty("version").GetString().ShouldBe("4.0.0");

        string[] installedPackageNames = [.. lockedPackages.EnumerateObject()
            .Select(static package => GetLockedPackageName(package))
            .OfType<string>()];
        installedPackageNames.ShouldNotContain("@semantic-release/npm");
        installedPackageNames.ShouldNotContain("npm");
        installedPackageNames.ShouldNotContain("tar");
        installedPackageNames.ShouldNotContain("brace-expansion");
    }

    [Fact]
    public void ReleaseConfiguration_UsesSingleExplicitAllowlistedPluginSource()
    {
        string repoRoot = GetRepoRoot();
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, "package.json")));
        using JsonDocument releaseConfig = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, ".releaserc.json")));

        manifest.RootElement.TryGetProperty("release", out _).ShouldBeFalse("package.json must not shadow the canonical .releaserc.json configuration.");
        manifest.RootElement.TryGetProperty("cosmiconfig", out _).ShouldBeFalse("package.json must not redirect semantic-release configuration discovery.");

        string[] forbiddenConfigPaths =
        [
            ".releaserc", ".releaserc.yaml", ".releaserc.yml", ".releaserc.js", ".releaserc.ts", ".releaserc.cjs", ".releaserc.mjs",
            ".config/releaserc", ".config/releaserc.json", ".config/releaserc.yaml", ".config/releaserc.yml", ".config/releaserc.js", ".config/releaserc.ts", ".config/releaserc.cjs", ".config/releaserc.mjs",
            "release.config.js", "release.config.ts", "release.config.cjs", "release.config.mjs",
            "package.yaml", ".config/config.json", ".config/config.yaml", ".config/config.yml", ".config/config.js", ".config/config.ts", ".config/config.cjs", ".config/config.mjs",
        ];
        forbiddenConfigPaths.ShouldAllBe(path => !File.Exists(Path.Combine(repoRoot, path)), "alternate cosmiconfig sources must not take precedence over .releaserc.json.");

        JsonElement config = releaseConfig.RootElement;
        config.TryGetProperty("$import", out _).ShouldBeFalse();
        config.TryGetProperty("extends", out _).ShouldBeFalse();
        string[] lifecycleKeys = ["addChannel", "analyzeCommits", "fail", "generateNotes", "prepare", "publish", "success", "verifyConditions", "verifyRelease"];
        string[] configKeys = [.. config.EnumerateObject().Select(static property => property.Name)];
        configKeys.ShouldBe(["branches", "tagFormat", "plugins"]);
        foreach (string lifecycleKey in lifecycleKeys)
        {
            configKeys.ShouldNotContain(lifecycleKey, "top-level lifecycle hooks must not shadow the reviewed plugin graph.");
        }

        config.GetProperty("branches").EnumerateArray().Select(static branch => branch.GetString()).ShouldBe(["main"]);
        config.GetProperty("tagFormat").GetString().ShouldBe("v${version}");
        string[] plugins = [.. config.GetProperty("plugins").EnumerateArray().Select(static plugin => plugin.ValueKind == JsonValueKind.Array ? plugin[0].GetString()! : plugin.GetString()!)];
        plugins.ShouldBe(
        [
            "@semantic-release/commit-analyzer",
            "@semantic-release/release-notes-generator",
            "@semantic-release/exec",
            "@semantic-release/github",
        ]);
        plugins.ShouldNotContain("@semantic-release/npm");

        JsonElement execPlugin = config.GetProperty("plugins")[2];
        execPlugin.GetArrayLength().ShouldBe(2);
        JsonElement execOptions = execPlugin[1];
        execOptions.EnumerateObject().Select(static property => property.Name).ShouldBe(["verifyReleaseCmd", "prepareCmd", "publishCmd"]);
        execOptions.GetProperty("verifyReleaseCmd").GetString().ShouldBe("pwsh -NoLogo -NoProfile -File ./tools/verify-container-registry.ps1");
        execOptions.GetProperty("prepareCmd").GetString()
            .ShouldBe("pwsh -NoLogo -NoProfile -File ./tools/pack-release.ps1 -Version ${nextRelease.version} -OutputDirectory ./artifacts/packages/release");
        execOptions.GetProperty("publishCmd").GetString()
            .ShouldBe("pwsh -NoLogo -NoProfile -File ./tools/publish-release.ps1 -Version ${nextRelease.version}");

        JsonElement githubPlugin = config.GetProperty("plugins")[3];
        githubPlugin.GetArrayLength().ShouldBe(2);
        JsonElement githubOptions = githubPlugin[1];
        githubOptions.EnumerateObject().Select(static property => property.Name).ShouldBe(["assets"]);
        githubOptions.GetProperty("assets").EnumerateArray().Select(static asset => asset.GetString()).ShouldBe(
        [
            "artifacts/packages/release/*.nupkg",
            "artifacts/deployment/hexalith-memories-production.yaml",
        ]);
    }

    [Fact]
    public void CiWorkflow_RequiredUnitContractJob_BlocksOnRootReleaseToolingGates()
    {
        string repoRoot = GetRepoRoot();
        string[] workflowLines = File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "ci.yml"));
        string workflow = string.Join('\n', workflowLines);
        workflow.ShouldContain("pull_request:");
        string[] pullRequestLines = GetWorkflowEventLines(workflowLines, "pull_request");
        pullRequestLines.ShouldNotContain(static line => line.TrimStart().StartsWith("paths:", StringComparison.Ordinal), "the blocking release-tooling gates must run for every pull-request path.");
        pullRequestLines.ShouldNotContain(static line => line.TrimStart().StartsWith("paths-ignore:", StringComparison.Ordinal), "pull-request path exclusions must not bypass release-tooling gates.");
        workflowLines.ShouldNotContain(static line => string.Equals(line.TrimEnd(), "defaults:", StringComparison.Ordinal), "workflow-level default working-directory redirection must not move root release-tooling gates.");

        string[] requiredJobLines = GetWorkflowJobLines(workflowLines, "test-unit-contract");
        requiredJobLines.ShouldNotContain(static line => line.StartsWith("    if:", StringComparison.Ordinal), "the required PR job must not be conditionally skipped.");
        requiredJobLines.ShouldNotContain(static line => line.StartsWith("    continue-on-error:", StringComparison.Ordinal), "the required PR job must fail when a root release-tooling gate fails.");
        requiredJobLines.ShouldNotContain(static line => line.StartsWith("    defaults:", StringComparison.Ordinal), "the required PR job must not redirect root release-tooling commands through a default working directory.");
        ReleaseWorkflowStep[] requiredJobSteps = ParseReleaseWorkflowSteps(requiredJobLines);
        int setupIndex = Array.FindIndex(requiredJobSteps, static step => step.Name == "Initialize root release-tooling Node.js");
        int installIndex = Array.FindIndex(requiredJobSteps, static step => step.Name == "Install root release tooling");
        int auditIndex = Array.FindIndex(requiredJobSteps, static step => step.Name == "Audit root release tooling");
        int verifierIndex = Array.FindIndex(requiredJobSteps, static step => step.Name == "Verify semantic-release configuration offline");

        setupIndex.ShouldBeGreaterThanOrEqualTo(0);
        setupIndex.ShouldBeLessThan(installIndex);
        installIndex.ShouldBeLessThan(auditIndex);
        auditIndex.ShouldBeLessThan(verifierIndex);
        requiredJobSteps[installIndex].Run.ShouldBe("npm ci");
        requiredJobSteps[auditIndex].Run.ShouldBe("npm audit --audit-level=low");
        requiredJobSteps[verifierIndex].Run.ShouldBe("npm run verify:semantic-release-config");
        requiredJobSteps[setupIndex].Uses.ShouldBe("actions/setup-node@v6");
        requiredJobSteps[setupIndex].WorkingDirectory.ShouldBeNull();
        requiredJobSteps[installIndex].WorkingDirectory.ShouldBeNull();
        requiredJobSteps[auditIndex].WorkingDirectory.ShouldBeNull();
        requiredJobSteps[verifierIndex].WorkingDirectory.ShouldBeNull();
        foreach (ReleaseWorkflowStep gate in new[] { requiredJobSteps[installIndex], requiredJobSteps[auditIndex], requiredJobSteps[verifierIndex] })
        {
            gate.If.ShouldBeNull("root release-tooling gates must run unconditionally in the required PR job.");
            gate.ContinueOnError.ShouldBeNull("root release-tooling gate failures must fail the required PR job.");
        }

        ReleaseWorkflowStep[] webE2eSteps = ParseReleaseWorkflowSteps(GetWorkflowJobLines(workflowLines, "web-e2e-specimen"));
        ReleaseWorkflowStep webE2eInstall = webE2eSteps.Single(static step => step.Name == "Install Memories web E2E dependencies");
        webE2eInstall.Run.ShouldBe("npm ci");
        webE2eInstall.WorkingDirectory.ShouldBe("tests/Hexalith.Memories.Web.E2E");
    }

    [Fact]
    public void ReleaseWorkflow_ReleaseToolingGatesAndPreflight_RunBeforeSemanticRelease()
    {
        string repoRoot = GetRepoRoot();
        ReleaseWorkflowStep[] steps = ParseReleaseWorkflowSteps(File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml")));

        int installIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Install release tooling", StringComparison.Ordinal));
        int auditIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Audit root release tooling", StringComparison.Ordinal));
        int verifierIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Verify semantic-release configuration offline", StringComparison.Ordinal));
        int preflightIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Run release preflight", StringComparison.Ordinal));
        int semanticReleaseIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Run semantic-release", StringComparison.Ordinal));

        installIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must install the reviewed root lockfile before release preflight.");
        auditIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must audit the complete root release-tooling graph.");
        verifierIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must run the combined configuration verifier and negative self-tests.");
        preflightIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must run the repository-owned release preflight before semantic-release can prepare or publish packages.");
        semanticReleaseIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must retain the semantic-release step.");
        installIndex.ShouldBeLessThan(auditIndex, "release tooling installation must complete before its audit.");
        auditIndex.ShouldBeLessThan(verifierIndex, "the full low-level audit must pass before configuration verification.");
        verifierIndex.ShouldBeLessThan(preflightIndex, "the combined configuration verifier must pass before release preflight.");
        preflightIndex.ShouldBeLessThan(semanticReleaseIndex, "release preflight must run before semantic-release starts publish-capable work.");

        steps[auditIndex].Run.ShouldBe("npm audit --audit-level=low");
        steps[verifierIndex].Run.ShouldBe("npm run verify:semantic-release-config");
        foreach (ReleaseWorkflowStep gate in new[] { steps[installIndex], steps[auditIndex], steps[verifierIndex] })
        {
            gate.If.ShouldBeNull("release-tooling gates must run unconditionally in the production release job.");
            gate.ContinueOnError.ShouldBeNull("release-tooling gate failures must block production release.");
            gate.WorkingDirectory.ShouldBeNull("production release-tooling gates must operate on the root lockfile.");
        }

        ReleaseWorkflowStep step = steps[preflightIndex];
        step.Shell.ShouldBe("pwsh");
        step.Run.ShouldBe("./tools/release-preflight.ps1");
        step.Uses.ShouldBeNull();
    }

    [Fact]
    public void ReleaseWorkflow_ReleasePreflightFixtures_RunExactlyOnceBeforeLivePreflightAndSemanticRelease()
    {
        string repoRoot = GetRepoRoot();
        ReleaseWorkflowStep[] steps = ParseReleaseWorkflowSteps(File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml")));
        const string FixtureCommand = "python3 -m unittest discover -s tests/tooling/release_preflight -p \"*_test.py\"";

        ReleaseWorkflowStep[] fixtureSteps = [.. steps.Where(step => string.Equals(step.Run, FixtureCommand, StringComparison.Ordinal))];
        fixtureSteps.Length.ShouldBe(1, "release.yml must invoke the canonical release-preflight fixture command exactly once.");

        int fixturesIndex = Array.IndexOf(steps, fixtureSteps[0]);
        int preflightIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Run release preflight", StringComparison.Ordinal));
        int semanticReleaseIndex = Array.FindIndex(steps, static step => string.Equals(step.Name, "Run semantic-release", StringComparison.Ordinal));

        preflightIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must retain the live release preflight.");
        semanticReleaseIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must retain the semantic-release step.");
        fixturesIndex.ShouldBeLessThan(preflightIndex, "release-preflight fixtures must run before the live release preflight.");
        fixturesIndex.ShouldBeLessThan(semanticReleaseIndex, "release-preflight fixtures must run before semantic-release starts publish-capable work.");

        ReleaseWorkflowStep step = fixtureSteps[0];
        step.Name.ShouldBe("Run release preflight fixtures");
        step.If.ShouldBeNull("release-preflight fixtures must run unconditionally.");
        step.ContinueOnError.ShouldBeNull("release-preflight fixture failures must fail the release job.");
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
    public void ReleaseWorkflow_NoOpRelease_SkipsReleaseOnlyArtifactUploads()
    {
        ReleaseWorkflowStep snapshot = GetReleaseWorkflowStep("Snapshot release tags");
        snapshot.Shell.ShouldBe("bash");
        snapshot.Run.ShouldNotBeNull();
        snapshot.Run!.ShouldContain("git tag --points-at HEAD --list 'v*'");
        snapshot.Run.ShouldContain("fingerprint={}");

        ReleaseWorkflowStep detection = GetReleaseWorkflowStep("Detect release publication");
        detection.If.ShouldBe("success()");
        detection.Shell.ShouldBe("bash");
        detection.RunBlock.ShouldContain("published=false");
        detection.RunBlock.ShouldContain("artifacts/deployment/hexalith-memories-production.yaml");
        detection.RunBlock.ShouldContain("artifacts/packages/release/*.nupkg");
        detection.RunBlock.ShouldContain("published=true");

        string releaseOnlyCondition = "success() && steps.release-publication.outputs.published == 'true'";
        GetReleaseWorkflowStep("Upload package artifacts").If.ShouldBe(releaseOnlyCondition);
        GetReleaseWorkflowStep("Upload deployment artifact").If.ShouldBe(releaseOnlyCondition);
    }

    [Fact]
    public void PartialReleaseRecovery_UsesTrustedTagAndCompletesWholeReleaseUnit()
    {
        string repoRoot = GetRepoRoot();
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "recover-partial-release.yml"));

        workflow.ShouldContain("workflow_dispatch:");
        workflow.ShouldContain("group: release");
        workflow.ShouldContain("contents: write");
        workflow.ShouldContain("issues: write");
        workflow.ShouldContain("github.ref == 'refs/heads/main'");
        workflow.ShouldContain("git show-ref --verify --quiet");
        workflow.ShouldContain("git merge-base --is-ancestor");
        workflow.ShouldContain("git checkout --detach");
        workflow.ShouldContain("Validate trusted recovery tooling");
        workflow.ShouldContain("Stage trusted recovery tooling");
        workflow.ShouldContain("verify-container-registry.ps1");
        workflow.ShouldContain("-RepositoryRoot $env:GITHUB_WORKSPACE");
        workflow.ShouldContain("complete-partial-release.ps1");
        workflow.ShouldContain("GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}");
        workflow.ShouldContain("@($summary.packages).Count -ne 9");
        workflow.ShouldContain("@($summary.images).Count -ne 4");
        workflow.ShouldContain("@($summary.releaseAssets).Count -ne 10");
        workflow.ShouldContain("@('pushed', 'already-present')");
        workflow.ShouldContain("if: always()");
        workflow.IndexOf("Stage trusted recovery tooling", StringComparison.Ordinal).ShouldBeLessThan(
            workflow.IndexOf("Checkout tagged release source", StringComparison.Ordinal));
        workflow.IndexOf("Verify container registry write scope", StringComparison.Ordinal).ShouldBeLessThan(
            workflow.IndexOf("Restore tagged source", StringComparison.Ordinal));
        workflow.IndexOf("Reconcile immutable container tags", StringComparison.Ordinal).ShouldBeLessThan(
            workflow.IndexOf("Complete GitHub Release and incident recovery", StringComparison.Ordinal));
        workflow.ShouldNotContain("NUGET_API_KEY");
        workflow.ShouldNotContain("dotnet nuget push");
        workflow.ShouldNotContain("semantic-release");
    }

    [Fact]
    public void ReleaseWorkflow_RegistryAuthentication_IsDeferredToPublishAndUsesSharedZotContract()
    {
        string repoRoot = GetRepoRoot();
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "release.yml"));
        string releasePreflight = File.ReadAllText(Path.Combine(repoRoot, "tools", "release-preflight.ps1"));
        ReleaseWorkflowStep semanticRelease = GetReleaseWorkflowStep("Run semantic-release");

        workflow.ShouldNotContain("docker/login-action");
        workflow.ShouldNotContain("CONTAINER_REGISTRY_USERNAME");
        workflow.ShouldNotContain("CONTAINER_REGISTRY_PASSWORD");
        semanticRelease.Environment["HEXALITH_ZOT_REGISTRY"].ShouldBe("${{ vars.HEXALITH_ZOT_REGISTRY || 'registry.hexalith.com' }}");
        semanticRelease.Environment["HEXALITH_ZOT_USERNAME"].ShouldBe("${{ secrets.HEXALITH_ZOT_USERNAME }}");
        semanticRelease.Environment["HEXALITH_ZOT_API_KEY"].ShouldBe("${{ secrets.HEXALITH_ZOT_API_KEY }}");
        workflow.ShouldNotContain("HEXALITH_RELEASE_CLASSIFICATION_ONLY");
        releasePreflight.ShouldContain("$env:HEXALITH_RELEASE_CLASSIFICATION_ONLY = 'true'");
    }

    [Fact]
    public void ReleaseWorkflow_ReleasePackageValidatorFixtures_RunBeforePublish()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Run release package validator fixtures");

        step.Run.ShouldBe("python3 -m unittest discover -s tests/tooling/release_packages -p \"*_test.py\"");
        step.Uses.ShouldBeNull();
    }

    [Fact]
    public void Workflows_ContainerPublishFixtures_RunBeforeReleasePublish()
    {
        const string FixtureCommand = "python3 -m unittest discover -s tests/tooling/publish_containers -p \"*_test.py\"";
        string repoRoot = GetRepoRoot();
        ReleaseWorkflowStep[] releaseSteps = ParseReleaseWorkflowSteps(File.ReadAllLines(Path.Combine(repoRoot, ".github", "workflows", "release.yml")));
        string ciWorkflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "ci.yml"));

        ReleaseWorkflowStep[] fixtureSteps = [.. releaseSteps.Where(step => string.Equals(step.Run, FixtureCommand, StringComparison.Ordinal))];
        fixtureSteps.Length.ShouldBe(1, "release.yml must execute the container partial-publish and retry fixture exactly once.");

        int fixturesIndex = Array.IndexOf(releaseSteps, fixtureSteps[0]);
        int semanticReleaseIndex = Array.FindIndex(releaseSteps, static step => string.Equals(step.Name, "Run semantic-release", StringComparison.Ordinal));
        semanticReleaseIndex.ShouldBeGreaterThanOrEqualTo(0, "release.yml must retain semantic-release.");
        fixturesIndex.ShouldBeLessThan(semanticReleaseIndex, "container publication fixtures must pass before semantic-release starts publish-capable work.");

        ReleaseWorkflowStep step = fixtureSteps[0];
        step.Name.ShouldBe("Run container publish fixtures");
        step.If.ShouldBeNull("container publication fixtures must run unconditionally in the release lane.");
        step.ContinueOnError.ShouldBeNull("container publication fixture failures must fail the release job.");
        step.Uses.ShouldBeNull();

        ciWorkflow.ShouldContain("name: Run container publish fixtures");
        ciWorkflow.ShouldContain($"run: {FixtureCommand}");
    }

    [Fact]
    public void ReleaseWorkflow_AlertPartialPublishStep_GuardsOnFailureAndReferencesAuditPaths()
    {
        ReleaseWorkflowStep step = GetReleaseWorkflowStep("Alert partial release publish");

        // Anchored to step name so a future workflow refactor that moves the alert outside of the
        // failure() guard (or to a different shell) fails this assertion instead of degrading
        // partial-publish recovery silently.
        step.If.ShouldBe("failure()");
        step.Shell.ShouldBe("pwsh");
        step.RunBlock.ShouldContain("./tools/create-partial-publish-issue.ps1");
        step.RunBlock.ShouldContain("artifacts/release/publish-summary.json");
        step.RunBlock.ShouldNotContain("artifacts/packages/release/publish-summary.json");
        step.RunBlock.ShouldNotContain("artifacts/containers/release/publish-summary.json");
    }

    [Fact]
    public void ReleaseConfiguration_PublishesRetrySafeFourImageUnitAndVersionedDeployment()
    {
        string repoRoot = GetRepoRoot();
        string releaseConfig = File.ReadAllText(Path.Combine(repoRoot, ".releaserc.json"));
        string packRelease = File.ReadAllText(Path.Combine(repoRoot, "tools", "pack-release.ps1"));
        string publishRelease = File.ReadAllText(Path.Combine(repoRoot, "tools", "publish-release.ps1"));
        string publishContainers = File.ReadAllText(Path.Combine(repoRoot, "tools", "publish-containers.ps1"));
        string renderDeployment = File.ReadAllText(Path.Combine(repoRoot, "tools", "render-production-deployment.ps1"));

        releaseConfig.ShouldContain("verifyReleaseCmd");
        releaseConfig.ShouldContain("./tools/verify-container-registry.ps1");
        releaseConfig.IndexOf("verifyReleaseCmd", StringComparison.Ordinal).ShouldBeLessThan(
            releaseConfig.IndexOf("prepareCmd", StringComparison.Ordinal));
        releaseConfig.ShouldContain("./tools/publish-release.ps1 -Version ${nextRelease.version}");
        releaseConfig.ShouldContain("artifacts/deployment/hexalith-memories-production.yaml");
        packRelease.ShouldContain("./tools/publish-containers.ps1");
        packRelease.ShouldContain("artifacts/deployment/hexalith-memories-production.yaml");
        publishRelease.ShouldContain("-Script './tools/publish-containers.ps1'");
        publishRelease.ShouldContain("'-Push'");
        publishRelease.ShouldContain("artifactKind = 'release-artifacts'");
        publishRelease.ShouldContain("artifacts/release");
        publishContainers.ShouldContain("[string]$RepositoryPrefix = 'memories'");
        publishContainers.ShouldContain("repository = $RepositoryPrefix");
        publishContainers.ShouldContain("repository = \"$RepositoryPrefix-mcp\"");
        publishContainers.ShouldContain("repository = \"$RepositoryPrefix-access-telemetry\"");
        publishContainers.ShouldContain("repository = \"$RepositoryPrefix-access-telemetry-clock\"");
        publishContainers.ShouldContain("HEXALITH_ZOT_USERNAME");
        publishContainers.ShouldContain("HEXALITH_ZOT_API_KEY");
        publishContainers.ShouldNotContain("CONTAINER_REGISTRY_USERNAME");
        publishContainers.ShouldNotContain("CONTAINER_REGISTRY_PASSWORD");
        publishContainers.ShouldContain("New-RegistryAuthFile");
        publishContainers.ShouldContain("partial-publish");
        publishContainers.ShouldContain("publish-summary.json");
        publishContainers.ShouldContain("render-production-deployment.ps1");
        publishContainers.ShouldContain("'inspect', '--raw', '--authfile', $AuthFile");
        publishContainers.ShouldContain("'copy', '--authfile', $AuthFile");
        publishContainers.ShouldContain("docker-archive:");
        publishContainers.ShouldContain("[string]$manifest.config.digest");
        publishContainers.ShouldNotContain("docker login");
        publishContainers.ShouldNotContain("& docker");
        publishContainers.ShouldContain("disposition = $Disposition");
        renderDeployment.ShouldContain("All release image references must end with the semantic-release version");
        renderDeployment.ShouldContain("[string]$AccessTelemetryImage");
        renderDeployment.ShouldContain("[string]$AccessTelemetryClockImage");
    }

    [Fact]
    public void CiWorkflow_KeepsEveryRestoreAuditExceptionInSyncWithDirectoryBuildProps()
    {
        // `-p:WarningsNotAsErrors=...` is a *global* MSBuild property, which a <PropertyGroup>
        // cannot override, so Directory.Build.props' own definition is inert during CI restore.
        // The global form is nonetheless required: it is the only way the exception reaches the
        // Hexalith.FrontComposer submodule, which has a separate Directory.Build.props chain.
        //
        // Owner: repository build owner (Hexalith ci-cd-standards.md, "Dependency Auditing").
        // Consequence of drift: a code added to Directory.Build.props works locally and is
        // silently dropped in CI, or one restore step diverges from the other three.
        // Reopen trigger: the submodule adopts the shared Directory.Build.props chain, at which
        // point the -p: injection can be deleted and this guard with it.
        string repoRoot = GetRepoRoot();
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "ci.yml"));
        string buildProps = File.ReadAllText(Path.Combine(repoRoot, "Directory.Build.props"));

        Match declared = Regex.Match(
            buildProps,
            @"<WarningsNotAsErrors>\$\(WarningsNotAsErrors\);(?<codes>[^<]+)</WarningsNotAsErrors>");
        declared.Success.ShouldBeTrue("Directory.Build.props must declare the audit-warning exception list.");
        string[] declaredCodes = [.. declared.Groups["codes"].Value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        declaredCodes.ShouldNotBeEmpty();

        // Match every `dotnet restore` invocation, not only the ones already carrying the flag:
        // matching on the flag made a new restore step added *without* it invisible to this guard,
        // so the count stayed at 4 and the guard stayed green on exactly the drift it exists to catch.
        MatchCollection restoreInvocations = Regex.Matches(workflow, @"dotnet restore [^\r\n]*");
        restoreInvocations.Count.ShouldBe(
            4,
            "Every CI restore invocation must be accounted for by this guard; found a different number of restore steps.");

        Match[] restoreSteps = [.. restoreInvocations
            .Where(static invocation => invocation.Value.Contains("-p:WarningsNotAsErrors=", StringComparison.Ordinal))];
        restoreSteps.Length.ShouldBe(
            restoreInvocations.Count,
            "Every CI restore invocation must inject the audit-warning exception; one carries no -p:WarningsNotAsErrors= at all.");

        foreach (Match invocation in restoreSteps)
        {
            Match step = Regex.Match(invocation.Value, @"-p:WarningsNotAsErrors=(?<codes>[^\s]+)");
            step.Success.ShouldBeTrue();

            // %3B is the URL-encoded semicolon the shell requires inside a -p: value.
            string[] injected = [.. step.Groups["codes"].Value
                .Replace("%3B", ";", StringComparison.Ordinal)
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
            injected.ShouldBe(
                declaredCodes,
                "Each CI restore step must inject exactly the codes Directory.Build.props declares, "
                    + "because the global property makes that declaration inert during CI restore.");
        }
    }

    [Fact]
    public void CiWorkflow_RunsEveryToolingFixtureSuiteThatGuardsAShippedTool()
    {
        // A tooling suite that no workflow runs is not a guard. Story 27.3's four-case (now
        // six-case) C1 inventory ran in no workflow while being cited as mandatory evidence,
        // and the sweep that found it also found `story_scope` and `integration_stub_closure`
        // unwired. Any workflow may host a suite: release_preflight runs from release.yml.
        string repoRoot = GetRepoRoot();
        string workflows = string.Concat(Directory
            .GetFiles(Path.Combine(repoRoot, ".github", "workflows"), "*.yml")
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText));

        string[] suites = [.. Directory
            .GetDirectories(Path.Combine(repoRoot, "tests", "tooling"))
            .Select(static directory => new DirectoryInfo(directory).Name)
            .Where(static name => !string.IsNullOrEmpty(name))
            .Order(StringComparer.Ordinal)];

        // Without this the foreach below runs zero times and the test passes having asserted
        // nothing, which is indistinguishable from every suite being wired.
        suites.ShouldNotBeEmpty("tests/tooling must contain at least one fixture suite for this guard to mean anything.");

        foreach (string suite in suites)
        {
            // Bound to the path as a whole token rather than to a literal trailing space, so
            // harmless workflow-YAML reformatting cannot fail the guard, while `story_scope` still
            // cannot be satisfied by a hypothetical `story_scope_extra` step.
            Regex.IsMatch(workflows, $@"-s\s+tests/tooling/{Regex.Escape(suite)}(?![0-9A-Za-z_-])")
                .ShouldBeTrue(
                    $"tests/tooling/{suite} is executed by no workflow step, so reverting anything it guards ships green.");
        }
    }

    [Fact]
    public void CiWorkflow_PinsDisposableClusterDeploymentVerificationJob()
    {
        string repoRoot = GetRepoRoot();
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "ci.yml"));
        string verifier = File.ReadAllText(Path.Combine(repoRoot, "tools", "verify-production-deployment.ps1"));
        string evidenceValidator = File.ReadAllText(Path.Combine(repoRoot, "tools", "validate-production-deployment-evidence.ps1"));

        // The disposable-cluster rollout is the only executed proof of the runtime, OCI, Secret-RBAC,
        // deny-by-default ACL, and readiness contracts. Pin its job and both script invocations so it
        // cannot be deleted, renamed, or unwired while the rest of the suite stays green.
        workflow.ShouldContain("production-deployment-verification:");
        workflow.ShouldContain("./tools/publish-containers.ps1");
        workflow.ShouldContain("./tools/verify-production-deployment.ps1");
        workflow.ShouldContain("-ServerArchive ./artifacts/containers/release/server.tar.gz");
        workflow.ShouldContain("-McpArchive ./artifacts/containers/release/mcp.tar.gz");
        workflow.ShouldContain("-AccessTelemetryArchive ./artifacts/containers/release/access-telemetry.tar.gz");
        workflow.ShouldContain("-AccessTelemetryClockArchive ./artifacts/containers/release/access-telemetry-clock.tar.gz");
        workflow.ShouldContain("KUBECTL_VERSION: 'v1.35.0'");
        workflow.ShouldContain("KIND_VERSION: 'v0.31.0'");
        workflow.ShouldContain("KIND_NODE_IMAGE: 'kindest/node:v1.35.0'");
        workflow.ShouldContain("-KindNodeImage $env:KIND_NODE_IMAGE");
        workflow.ShouldContain("-EvidenceDirectory ./artifacts/production-deployment-verification");
        workflow.ShouldContain("name: Validate production deployment evidence");
        workflow.ShouldContain("./tools/validate-production-deployment-evidence.ps1 -EvidenceDirectory ./artifacts/production-deployment-verification");
        workflow.ShouldContain("python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p \"*_test.py\"");
        workflow.ShouldContain("artifacts/production-deployment-verification/**");
        verifier.ShouldContain("--image', $KindNodeImage");
        verifier.ShouldContain("test ! -e /app/appsettings.Development.json");
        verifier.ShouldContain("Get-RunningContainerObservation");
        verifier.ShouldContain("Get-RunningPodName");
        verifier.ShouldContain("state.running.startedAt");
        verifier.ShouldContain("lastTransitionTime");
        verifier.ShouldContain("$healthyAt");
        verifier.ShouldContain("$runningContainerInstance");
        verifier.ShouldContain("@('scale', 'deployment/memories', 'deployment/memories-mcp', '-n', $namespace, '--replicas=0')");
        verifier.ShouldContain("@('rollout', 'status', 'statefulset/redis-stack', '-n', $namespace, '--timeout=180s')");
        verifier.ShouldContain("@('rollout', 'status', 'statefulset/falkordb', '-n', $namespace, '--timeout=180s')");
        verifier.ShouldContain("@('scale', 'deployment/memories', 'deployment/memories-mcp', '-n', $namespace, '--replicas=2')");
        verifier.ShouldContain("verification.hexalith.com/dapr-token-stage");
        verifier.ShouldContain("verification-invalid-dapr-api-token");
        verifier.ShouldContain("required-dapr-token-unhealthy");
        verifier.ShouldContain("required-dapr-token-capacity-restored");
        verifier.ShouldContain("RequiredPodAnnotationValue 'faulted'");
        verifier.ShouldContain("RequiredPodAnnotationValue 'restored'");
        verifier.ShouldContain("rollout', 'status', 'deployment/memories'");
        verifier.ShouldContain("Save-MemoriesDeploymentState");
        verifier.ShouldContain("Set-CapacityPreservingMemoriesRollout");
        verifier.ShouldContain("Restore-MemoriesDeploymentState");
        verifier.ShouldContain("\"maxSurge\":0");
        verifier.ShouldContain("\"maxUnavailable\":1");
        verifier.ShouldContain("path = '/spec/replicas'");
        verifier.ShouldContain("path = '/spec/strategy'");
        verifier.ShouldContain("did not return to its exact captured replicas and rollout strategy");
        verifier.ShouldContain("cleanup-server-restored");
        verifier.ShouldContain("cleanup-mcp-restored");
        verifier.ShouldContain("required-server-restored");
        verifier.ShouldContain("required-server-mcp-restored");
        verifier.ShouldContain("expectedHttpStatus = if ($ExpectedStatus -eq 'Unhealthy') { 503 } else { 200 }");
        verifier.ShouldContain("wgetOutput=\"$(wget -S -O- -T 6 --header=\"dapr-api-token: ${APP_API_TOKEN}\"");
        verifier.ShouldContain("wgetExit=$?");
        verifier.ShouldContain("dapr-api-token: %s");
        verifier.ShouldContain("Connection: close\\r\\ndapr-api-token: %s\\r\\n\\r\\n");
        verifier.ShouldContain("$probeCommand = $probeCommand.Replace(\"`r\", '')");
        verifier.ShouldContain("Save-HealthResponseEvidence");

        // Health-response parsing moved to its own dot-sourceable file so it can be exercised
        // with real transcripts instead of pinned by source text, and redaction moved off the
        // health-decision path to the evidence write.
        string health = File.ReadAllText(Path.Combine(repoRoot, "tools", "production-deployment-health.ps1"));
        verifier.ShouldContain("production-deployment-health.ps1");
        verifier.ShouldNotContain("$text = Protect-EvidenceText ($output");
        health.ShouldContain("function Get-HealthJsonBody");
        health.ShouldContain("function Get-HealthStatusCode");
        health.ShouldContain("ConvertFrom-Json -ErrorAction Stop");
        verifier.ShouldContain("Write-ClusterDiagnostics");
        verifier.ShouldContain("describe-pods.txt");
        verifier.ShouldContain("events.txt");
        verifier.ShouldContain("if ($null -eq $container)");
        verifier.ShouldContain("IsNullOrWhiteSpace($containerName)");
        verifier.ShouldContain("--previous");
        verifier.ShouldNotContain("dapr.io/enabled\":\"false");
        evidenceValidator.ShouldContain("verification-result.json");
        evidenceValidator.ShouldContain("health-*.json");
        evidenceValidator.ShouldContain("authenticated HTTP 200 and HTTP 503 health responses");
        evidenceValidator.ShouldContain("Succeeded production deployment evidence must finish at required-server-mcp-restored");
        evidenceValidator.ShouldContain("*-current.log");
        evidenceValidator.ShouldContain("*-previous.log");
        evidenceValidator.ShouldContain("unredacted secret canary");
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

    [Fact]
    public void DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce()
    {
        string repoRoot = GetRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(repoRoot, "_bmad-output", "implementation-artifacts", "deferred-work.md"));

        string[] duplicates = FindDuplicateDeferredIds(lines);

        // Story 27.3: an ID collision has now recurred three times, each time because an entry was
        // renumbered to escape a collision and landed on another live ID (27.3-CR7 -> 27.3-CR17 ->
        // 27.3-CR18). Every existing guard, including ReadOpenDeferredBaselines above, checks for a
        // duplicate field *within* one entry and stays green across entries, so the register could
        // resolve one ID to two unrelated obligations with nothing able to observe it. This binds
        // the register-wide invariant instead of the per-entry one.
        duplicates.ShouldBeEmpty($"deferred-work.md declares duplicate entry IDs: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void FindDuplicateDeferredIds_RepeatedId_IsReportedWithItsCount()
    {
        string[] fixture =
        [
            "  - ID: S11-FX",
            "  - Status: open",
            "  - ID: S11-FY",
            "  - Status: open",
            "  - ID: S11-FX",
            "  - Status: resolved",
        ];

        string[] duplicates = FindDuplicateDeferredIds(fixture);

        duplicates.ShouldBe(["S11-FX (x2)"]);
    }

    [Fact]
    public void FindDuplicateDeferredIds_MatchesTheRegisterConsumersIdLineLexicon()
    {
        // Story 27.3 code review (eighth-invocation review): the guard reused the C# reader's
        // regex, so it disagreed with the register's real consumer in both directions. These
        // three forms are all honoured by read_deferred_entries in
        // tools/verify-integration-stub-closure.py, so each is a genuine second declaration
        // under one key - a fourth recurrence of the collision class this guard exists to stop.
        string[] honouredByTheConsumer =
        [
            "  - ID: 27.3-CRX",
            "  - id: 27.3-CRX",
        ];
        FindDuplicateDeferredIds(honouredByTheConsumer).ShouldBe(["27.3-CRX (x2)"]);

        string[] noSpaceAfterColon =
        [
            "  - ID: 27.3-CRY",
            "  - ID:27.3-CRY",
        ];
        FindDuplicateDeferredIds(noSpaceAfterColon).ShouldBe(["27.3-CRY (x2)"]);

        // The reverse direction: forms the consumer ignores must not turn the CI lane red.
        // It requires a '-' bullet, so a '*' bullet and an unbulleted line are not declarations.
        string[] ignoredByTheConsumer =
        [
            "  - ID: 27.3-CRZ",
            "  * ID: 27.3-CRZ",
            "  ID: 27.3-CRZ",
        ];
        FindDuplicateDeferredIds(ignoredByTheConsumer).ShouldBeEmpty();
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

    private static string[] GetWorkflowEventLines(string[] lines, string eventName)
    {
        string header = $"  {eventName}:";
        int start = Array.FindIndex(lines, line => string.Equals(line.TrimEnd(), header, StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(0, $"workflow must declare on.{eventName}.");

        int end = start + 1;
        while (end < lines.Length)
        {
            string line = lines[end].TrimEnd();
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("    ", StringComparison.Ordinal))
            {
                break;
            }

            end++;
        }

        return lines[start..end];
    }

    private static string[] GetWorkflowJobLines(string[] lines, string jobName)
    {
        string header = $"  {jobName}:";
        int start = Array.FindIndex(lines, line => string.Equals(line.TrimEnd(), header, StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(0, $"workflow must declare jobs.{jobName}.");

        int end = start + 1;
        while (end < lines.Length)
        {
            string line = lines[end].TrimEnd();
            if (line.StartsWith("  ", StringComparison.Ordinal) &&
                !line.StartsWith("    ", StringComparison.Ordinal) &&
                line.EndsWith(':'))
            {
                break;
            }

            end++;
        }

        return lines[start..end];
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
            string? continueOnError = null;
            string? run = null;
            string? uses = null;
            string? workingDirectory = null;
            Dictionary<string, string> environment = new(StringComparer.Ordinal);
            List<string> runBlockLines = [];
            bool inRunBlock = false;
            bool inEnvironment = false;

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

                if (inEnvironment)
                {
                    if (string.IsNullOrWhiteSpace(body))
                    {
                        i++;
                        continue;
                    }

                    if (body.StartsWith(BodyIndent + "  ", StringComparison.Ordinal))
                    {
                        string environmentEntry = body.Trim();
                        int separator = environmentEntry.IndexOf(':');
                        if (separator > 0)
                        {
                            string key = environmentEntry[..separator].Trim();
                            string value = StripQuotes(environmentEntry[(separator + 1)..].Trim());
                            environment[key] = value;
                        }

                        i++;
                        continue;
                    }

                    inEnvironment = false;
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
                else if (trimmed.StartsWith("continue-on-error:", StringComparison.Ordinal))
                {
                    continueOnError = StripQuotes(trimmed["continue-on-error:".Length..].Trim());
                }
                else if (trimmed.StartsWith("uses:", StringComparison.Ordinal))
                {
                    uses = trimmed["uses:".Length..].Trim();
                }
                else if (trimmed.StartsWith("working-directory:", StringComparison.Ordinal))
                {
                    workingDirectory = StripQuotes(trimmed["working-directory:".Length..].Trim());
                }
                else if (string.Equals(trimmed, "env:", StringComparison.Ordinal))
                {
                    inEnvironment = true;
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
                ContinueOnError: continueOnError,
                Run: run,
                Uses: uses,
                WorkingDirectory: workingDirectory,
                Environment: environment,
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

    private static Dictionary<string, string> ReadStringMapping(JsonElement mapping)
    {
        return mapping.EnumerateObject().ToDictionary(
            static property => property.Name,
            static property => property.Value.GetString()!,
            StringComparer.Ordinal);
    }

    private static string? GetLockedPackageName(JsonProperty package)
    {
        if (package.Value.TryGetProperty("name", out JsonElement explicitName) && explicitName.ValueKind == JsonValueKind.String)
        {
            string? name = explicitName.GetString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        const string NodeModulesMarker = "node_modules/";
        int markerIndex = package.Name.LastIndexOf(NodeModulesMarker, StringComparison.Ordinal);
        return markerIndex < 0 ? null : package.Name[(markerIndex + NodeModulesMarker.Length)..];
    }

    private sealed record ReleaseWorkflowStep(
        string Name,
        string? Shell,
        string? If,
        string? ContinueOnError,
        string? Run,
        string? Uses,
        string? WorkingDirectory,
        Dictionary<string, string> Environment,
        string RunBlock);

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

        // The register's closure style writes a dated rationale after the status token
        // (`Status: resolved 2026-07-26 - <why>`). Take the leading token as the status and
        // keep the remainder as prose; the token itself must still be one of the four
        // documented values, so `superseded` or any other invented status still fails.
        status = status!.Split(' ', 2, StringSplitOptions.TrimEntries)[0];
        AllowedDeferredStatuses.ShouldContain(status, $"structured deferred entry '{id}' has invalid 'Status: {status}'. Allowed values: {string.Join(", ", AllowedDeferredStatuses)}.");

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

    /// <summary>Returns every deferred-work ID declared more than once, with its occurrence count.</summary>
    /// <remarks>
    /// Uses <see cref="DeferredConsumerFieldRegex"/>, which mirrors the register's actual consumer
    /// (<c>DEFERRED_FIELD_RE</c> in <c>tools/verify-integration-stub-closure.py</c>) rather than the
    /// C# structured-entry reader. A line that consumer treats as an ID declaration cannot escape
    /// this guard, and a line it ignores cannot make this guard fail. Values are grouped ordinally
    /// because that consumer keys its entry dictionary on the raw ID string.
    /// </remarks>
    private static string[] FindDuplicateDeferredIds(IEnumerable<string> lines)
        => lines
            .Select(static line => DeferredConsumerFieldRegex().Match(line))
            .Where(static match => match.Success
                && string.Equals(match.Groups["label"].Value, "ID", StringComparison.OrdinalIgnoreCase))
            .Select(static match => match.Groups["value"].Value.Trim())
            .GroupBy(static id => id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => $"{group.Key} (x{group.Count()})")
            .OrderBy(static description => description, StringComparer.Ordinal)
            .ToArray();

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

    // Story 27.3 code review (eighth-invocation review): the duplicate-ID guard reused
    // StructuredFieldRegex above and so disagreed with the register's actual consumer,
    // read_deferred_entries in tools/verify-integration-stub-closure.py, in BOTH directions.
    // That consumer's DEFERRED_FIELD_RE is case-insensitive, accepts ':' with no following
    // space, and requires a '-' bullet. So '- id: 27.3-CR18' and '- ID:27.3-CR18' each create
    // a real second entry under one key that the guard could not see, while '* ID: x' and an
    // unbulleted 'ID: x' were counted by the guard although no consumer honours them. This
    // regex mirrors DEFERRED_FIELD_RE exactly so the guard binds the invariant that matters.
    [GeneratedRegex(@"^\s*-\s+(?<label>ID|Status|Source story|Target artifact|Re-open trigger|Rationale):\s*(?<value>.*?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DeferredConsumerFieldRegex();

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
