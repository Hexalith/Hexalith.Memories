// <copyright file="HexalithMemoriesAspireSecretStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.IO;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Memories.Aspire;

using Shouldly;

/// <summary>
/// Story 29.2 -- <c>Hexalith.Memories.Aspire</c> must accept an externally-provisioned secret-store
/// component for both hosting extensions instead of hard-coding a <c>secretstores.local.file</c>
/// component, exactly like the existing <c>stateStore</c>/<c>pubSub</c> pattern.
/// <para>
/// The argument-validation tests below exercise the real extension methods against a bare
/// <see cref="IDistributedApplicationBuilder"/> -- <c>secretStore</c> is validated before either extension
/// resolves any cross-repo project, so this executes without a consuming AppHost's
/// <c>references/Hexalith.Memories</c> checkout.
/// </para>
/// <para>
/// Proving the positive wiring (each returned project/sidecar genuinely references and waits for the
/// supplied <c>secretStore</c>, not merely that the source text mentions it) would need a full Aspire model
/// build, mirroring <c>DomainModuleAspireExtensionTests</c> in <c>Hexalith.EventStore.AppHost.Tests</c>.
/// That pattern does not work here: unlike the EventStore extension it mirrors, both
/// <c>AddHexalithMemoriesSearchIndexServer</c> and <c>AddHexalithMemoriesAccessTelemetry</c> call
/// <c>AddProject&lt;T&gt;(name, launchProfileName: "http")</c> with an explicit launch profile for their
/// internal cross-repo <c>MemoriesServerProjectMetadata</c>/<c>MemoriesAccessTelemetry*ProjectMetadata</c>
/// types, which eagerly reads the resolved project's launch settings and throws
/// <see cref="Aspire.Hosting.DistributedApplicationException"/> when the file does not exist --
/// <c>RepositoryProjectPaths.GetReferencedModuleProjectPath("Hexalith.Memories", ...)</c> has no candidate
/// for "this repo's own checkout" (only for a *consumer* repo with <c>Hexalith.Memories</c> under
/// <c>references/</c>), so it never resolves inside this repository's own test run. The structural checks
/// below instead assert on the exact call-syntax occurrence counts of <c>.WithReference(secretStore)</c> and
/// <c>.WaitFor(secretStore)</c> (anchored on the real invocation syntax with parentheses, not a bare class
/// or parameter name that could also appear in prose/XML-doc), so removing any wiring call -- sidecar-level,
/// project-level, or a wait -- drops a count and fails the test.
/// </para>
/// </summary>
public sealed class HexalithMemoriesAspireSecretStoreTests
{
    [Fact]
    public void AddHexalithMemoriesSearchIndexServer_WhenSecretStoreIsNull_ThrowsArgumentNullException()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        IResourceBuilder<IDaprComponentResource> stateStore = builder.AddDaprComponent("statestore", "state.redis");
        IResourceBuilder<IDaprComponentResource> pubSub = builder.AddDaprComponent("pubsub", "pubsub.redis");

        _ = Should.Throw<ArgumentNullException>(() => builder.AddHexalithMemoriesSearchIndexServer(
            stateStore,
            pubSub,
            secretStore: null!,
            llmComponentPath: "llm.yaml"));
    }

    [Fact]
    public void AddHexalithMemoriesAccessTelemetry_WhenSecretStoreIsNull_ThrowsArgumentNullException()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        IResourceBuilder<ProjectResource> server = builder.AddProject<TestProjectMetadata>("test-server");

        _ = Should.Throw<ArgumentNullException>(() => builder.AddHexalithMemoriesAccessTelemetry(
            server,
            stateStoreComponentPath: "statestore.yaml",
            secretStore: null!,
            configurationStoreComponentPath: "config.yaml"));
    }

    [Fact]
    public void ServerExtension_NeverCreatesALocalFileSecretStoreComponentAndValidatesSecretStoreLikeItsSiblings()
    {
        string source = ReadRepoFile("src", "Hexalith.Memories.Aspire", "HexalithMemoriesServerExtensions.cs");

        // Quoted form only -- the type name may still appear in XML-doc prose explaining it is no longer used.
        source.ShouldNotContain("\"secretstores.local.file\"", Case.Sensitive);
        source.ShouldContain("IResourceBuilder<IDaprComponentResource> secretStore,", Case.Sensitive);
        source.ShouldContain("ArgumentNullException.ThrowIfNull(secretStore);", Case.Sensitive);

        // The old string-path parameter must be gone, not merely renamed.
        source.ShouldNotContain("string secretStoreComponentPath", Case.Sensitive);
    }

    [Fact]
    public void AccessTelemetryExtension_NeverCreatesALocalFileSecretStoreComponentAndValidatesSecretStoreLikeItsSiblings()
    {
        string source = ReadRepoFile("src", "Hexalith.Memories.Aspire", "HexalithMemoriesAccessTelemetryExtensions.cs");

        // Quoted form only -- the type name may still appear in XML-doc prose explaining it is no longer used.
        source.ShouldNotContain("\"secretstores.local.file\"", Case.Sensitive);
        source.ShouldContain("IResourceBuilder<IDaprComponentResource> secretStore,", Case.Sensitive);
        source.ShouldContain("ArgumentNullException.ThrowIfNull(secretStore);", Case.Sensitive);
        source.ShouldNotContain("string secretStoreComponentPath", Case.Sensitive);
    }

    [Fact]
    public void ServerExtension_WiresTheSuppliedSecretStoreToTheServerSidecarAndProjectAndWaitsForIt()
    {
        // AC1: the server sidecar and the CS0618 project-level workaround must both reference `secretStore`,
        // and the server must wait for it before starting. Two `.WithReference(secretStore)` call sites
        // (sidecar-level, project-level) and one `.WaitFor(secretStore)` are the complete expected wiring;
        // removing any of them drops the corresponding count below its expected value. The aggregate counts
        // alone would not catch a regression that drops one wiring call while adding a compensating duplicate
        // elsewhere, so each site below is also pinned to its exact attachment point via unique surrounding
        // context ([Review][Patch] hardening).
        string source = ReadRepoFile("src", "Hexalith.Memories.Aspire", "HexalithMemoriesServerExtensions.cs");

        CountOccurrences(source, ".WithReference(secretStore)").ShouldBe(2);
        CountOccurrences(source, ".WaitFor(secretStore)").ShouldBe(1);

        // Sidecar-level: secretStore chained inside .WithDaprSidecar(...), alongside stateStore/pubSub/llm.
        source.ShouldContain(
            ".WithReference(stateStore)\n                .WithReference(pubSub)\n                .WithReference(secretStore)\n                .WithReference(llm))",
            Case.Sensitive);

        // Project-level CS0618 workaround: the same four references reattached directly on `server`.
        source.ShouldContain(
            "server = server\n            .WithReference(stateStore)\n            .WithReference(pubSub)\n            .WithReference(secretStore)\n            .WithReference(llm);",
            Case.Sensitive);

        // The server waits for the secret store before starting.
        source.ShouldContain(".WaitFor(secretStore)\n            .WaitFor(llm);", Case.Sensitive);
    }

    [Fact]
    public void AccessTelemetryExtension_WiresTheSuppliedSecretStoreToServerLifecycleAndClockAndWaitsForIt()
    {
        // AC1 names Server, lifecycle, AND clock as required secret-store consumers. Clock previously never
        // referenced the secret store at all (a pre-existing gap predating Story 29.2, confirmed by diffing
        // this extension's history), even though Hexalith.Memories.AccessTelemetry.Clock's Program.cs
        // genuinely resolves its signing key through DaprClient.GetSecretAsync against it.
        //
        // Expected `.WithReference(secretStore)` call sites (5): clock sidecar-level, clock project-level
        // (`clock = clock.WithReference(secretStore);`), lifecycle sidecar-level, lifecycle project-level,
        // server project-level. Expected `.WaitFor(secretStore)` call sites (2): clock, lifecycle -- `server`
        // never waits for it in the current design. Removing any wiring call drops a count below its
        // expected value.
        string source = ReadRepoFile("src", "Hexalith.Memories.Aspire", "HexalithMemoriesAccessTelemetryExtensions.cs");

        CountOccurrences(source, ".WithReference(secretStore)").ShouldBe(5);
        CountOccurrences(source, ".WaitFor(secretStore)").ShouldBe(2);
        source.ShouldContain("clock = clock.WithReference(secretStore);", Case.Sensitive);

        // The aggregate counts alone would not catch a regression that misattaches a reference while keeping
        // the total unchanged, so each site is also pinned to its exact attachment point below ([Review][Patch]
        // hardening).

        // Clock sidecar-level reference + wait, pinned to the clock's own CreateSidecarOptions call.
        source.ShouldContain(
            "\"memories-access-telemetry-clock\",\n                    3800,\n                    50301,\n                    daprConfigurationPath,\n                    daprPlacementHostAddress,\n                    daprSchedulerHostAddress))\n                .WithReference(secretStore))\n            .WaitFor(secretStore);",
            Case.Sensitive);

        // Lifecycle sidecar-level references, pinned to the lifecycle's own CreateSidecarOptions call.
        source.ShouldContain(
            "\"memories-access-telemetry\",\n                    3700,\n                    50201,\n                    daprConfigurationPath,\n                    daprPlacementHostAddress,\n                    daprSchedulerHostAddress))\n                .WithReference(stateStore)\n                .WithReference(secretStore)\n                .WithReference(configurationStore))",
            Case.Sensitive);

        // Lifecycle waits for all three of its sidecar-referenced components.
        source.ShouldContain(
            ".WaitFor(stateStore)\n            .WaitFor(secretStore)\n            .WaitFor(configurationStore);",
            Case.Sensitive);

        // Project-level CS0618 workaround: lifecycle and server both reattach secretStore directly.
        source.ShouldContain(
            "lifecycle = lifecycle\n            .WithReference(stateStore)\n            .WithReference(secretStore)\n            .WithReference(configurationStore);",
            Case.Sensitive);
        source.ShouldContain(
            "server = server\n            .WithReference(secretStore)\n            .WithReference(configurationStore)",
            Case.Sensitive);
    }

    private static int CountOccurrences(string text, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }

    /// <summary>Minimal <see cref="IProjectMetadata"/> pointing at this test project's own real, on-disk
    /// <c>.csproj</c> so <c>AddProject</c> can resolve launch-profile metadata without depending on a
    /// consumer repository's cross-repo checkout.</summary>
    private sealed class TestProjectMetadata : IProjectMetadata
    {
        public string ProjectPath => Path.Combine(
            ResolveRepoRoot(),
            "tests",
            "Hexalith.Memories.Server.Tests",
            "Hexalith.Memories.Server.Tests.csproj");

        public bool SuppressBuild => true;
    }

    private static string ReadRepoFile(params string[] segments)
    {
        string[] parts = new string[segments.Length + 1];
        parts[0] = ResolveRepoRoot();
        Array.Copy(segments, 0, parts, 1, segments.Length);
        string path = Path.Combine(parts);
        File.Exists(path).ShouldBeTrue($"Source file not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root (Hexalith.Memories.slnx) within 8 levels above {AppContext.BaseDirectory}.");
    }
}
