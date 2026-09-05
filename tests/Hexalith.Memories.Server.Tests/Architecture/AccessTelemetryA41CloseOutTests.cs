// <copyright file="AccessTelemetryA41CloseOutTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Linq;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>Story 27.4 anti-corruption guards for the canonical matrix and A41 close-out chain.</summary>
public sealed class AccessTelemetryA41CloseOutTests
{
    private const string EvidenceMatrix = "_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md";

    [Fact]
    public void CanonicalMatrix_ContainsExactlyC0ThroughC6AndKeepsExternalProofPending()
    {
        var document = new MarkdownContractDocument(ReadRepoFile(EvidenceMatrix));
        document.GetTableHeader("Canonical C0-C6 matrix").ShouldBe(
        [
            "Checkpoint",
            "State",
            "Repository validation",
            "Required external evidence",
            "Owner",
            "Blocker / operator action",
        ]);
        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows("Canonical C0-C6 matrix");
        rows.Count.ShouldBe(7);
        rows.Select(row => row[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]).ShouldBe(
            ["C0", "C1", "C2", "C3", "C4", "C5", "C6"]);
        rows[0][1].ShouldBe("`repository-validated`");
        rows.Skip(1).ShouldAllBe(row => row[1] == "`operator-pending`");

        string posture = document.GetSection("Evidence posture");
        posture.ShouldContain("No target was queried", Case.Sensitive);
        posture.ShouldContain("Production lifecycle writes remain disabled", Case.Sensitive);
        posture.ShouldContain("remains carried forward/open", Case.Sensitive);
        document.GetSection("Canonical C0-C6 matrix").ShouldContain(
            "Only authentic external packets in state `passed` can satisfy C2-C6",
            Case.Sensitive);
    }

    [Fact]
    public void ProductionOverlay_RemainsFailClosedWhileCanonicalMatrixIsPending()
    {
        string overlay = ReadRepoFile("deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml");
        overlay.Split("replicas: 0", StringSplitOptions.None).Length.ShouldBe(3);
        overlay.ShouldContain("disabled-pending-story-27-3", Case.Sensitive);

        string matrix = ReadRepoFile(EvidenceMatrix);
        matrix.ShouldNotContain("Production lifecycle writes | `enabled`", Case.Sensitive);
        matrix.ShouldNotContain("A41 status | `closed`", Case.Sensitive);
    }

    [Fact]
    public void Verifier_PinsExactMutableAndProtectedSetsAndAuthenticatesRemotePublish()
    {
        string source = ReadRepoFile("tools/verify_access_telemetry_lifecycle.py");
        string cli = ReadRepoFile("tools/verify-access-telemetry-lifecycle.py");
        string[] mutablePaths =
        [
            "_bmad-output/implementation-artifacts/deferred-work.md",
            "_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md",
            "_bmad-output/project-context.md",
            "docs/dev/telemetry.md",
        ];
        string[] protectedPaths =
        [
            "_bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md",
            "_bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md",
            "_bmad-output/implementation-artifacts/sprint-status.yaml",
            "_bmad-output/planning-artifacts/epics.md",
        ];

        foreach (string path in mutablePaths.Concat(protectedPaths))
        {
            source.ShouldContain($"\"{path}\"", Case.Sensitive);
        }

        source.ShouldContain("git", Case.Sensitive);
        source.ShouldContain("\"fetch\"", Case.Sensitive);
        source.ShouldContain("refs/codex/a41-publish-", Case.Sensitive);
        source.ShouldContain("remote_url_sha256", Case.Sensitive);
        source.ShouldContain("intended remote branch does not contain the close-out commit", Case.Sensitive);
        source.ShouldContain("published-close-out-verified", Case.Sensitive);
        foreach (string mode in new[]
                 {
                     "a41-inventory",
                     "close-out-preflight",
                     "close-out-postflight",
                     "publish-verification",
                 })
        {
            cli.ShouldContain(mode, Case.Sensitive);
        }
    }

    [Fact]
    public void Story27_4Artifacts_ContainNoLeakedToolCallMarkup()
    {
        string[] artifacts =
        [
            EvidenceMatrix,
            "docs/operations/access-telemetry-lifecycle.md",
            "docs/operations/access-telemetry-adapter-production.md",
        ];

        foreach (string artifact in artifacts)
        {
            IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(ReadRepoFile(artifact));
            diagnostics.ShouldBeEmpty($"{artifact} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
        }
    }

    private static string ReadRepoFile(string relativePath)
    {
        string path = Path.Combine(ResolveRepoRoot(), relativePath);
        File.Exists(path).ShouldBeTrue($"Required Story 27.4 artifact not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int index = 0; index < 8; index++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
