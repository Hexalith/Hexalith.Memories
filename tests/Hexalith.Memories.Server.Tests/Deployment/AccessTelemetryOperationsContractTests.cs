// <copyright file="AccessTelemetryOperationsContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.IO;
using System.Linq;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>Story 27.4 structure guards for the lifecycle operations contract.</summary>
public sealed class AccessTelemetryOperationsContractTests
{
    private const string AdapterRunbook = "docs/operations/access-telemetry-adapter-production.md";
    private const string LifecycleRunbook = "docs/operations/access-telemetry-lifecycle.md";

    private static readonly string[] LifecycleSections =
    [
        "Purpose and assurance boundary",
        "Ownership and authority separation",
        "Configuration and retention bounds",
        "Rollout and enablement",
        "C2 production replacement verification",
        "C3 retention, purge, and reclamation verification",
        "C4 failure, privacy, and observability verification",
        "Monitoring, alerts, and NoData",
        "Incident response and recovery",
        "Rollback and RPO/RTO limits",
        "Marker key rotation",
        "A41 close-out chain",
        "Verified decommissioning",
    ];

    private static readonly string[] AdapterSections =
    [
        "Scope and immutable profile",
        "Ownership and secret boundary",
        "Retention, capacity, and cost admission",
        "PostgreSQL monitoring and alarms",
        "Purge and physical reclamation proof",
        "Incident recovery",
        "Backup, restore, and RPO/RTO",
        "Upgrade and rollback",
        "Certificate, credential, and marker-key rotation",
        "Verified decommissioning",
    ];

    [Fact]
    public void LifecycleRunbook_BindsEveryOperationalAndEvidenceBoundary()
    {
        string markdown = ReadRepoFile(LifecycleRunbook);
        var document = new MarkdownContractDocument(markdown);
        foreach (string section in LifecycleSections)
        {
            document.GetSection(section).ShouldNotBeNullOrWhiteSpace(section);
        }

        IReadOnlyList<IReadOnlyList<string>> configuration = document.GetTableRows("Configuration and retention bounds");
        configuration.Single(row => row[0] == "Minimum / maximum")[1].ShouldBe("1 hour / 7 days");
        configuration.Single(row => row[0] == "Queue")[1].ShouldContain("8,192 records and 64 MiB", Case.Sensitive);
        configuration.Single(row => row[0] == "Physical reclamation")[1].ShouldContain("24 hours", Case.Sensitive);

        string replacement = NormalizeWhitespace(document.GetSection("C2 production replacement verification"));
        replacement.ShouldContain("both Server writers and their sidecars", Case.Sensitive);
        replacement.ShouldContain("all three Placement members", Case.Sensitive);
        replacement.ShouldContain("all three Scheduler members", Case.Sensitive);
        replacement.ShouldContain("one aggregate replacement result cannot discharge multiple instances", Case.Sensitive);
        replacement.ShouldContain("Production remains disabled throughout", Case.Sensitive);

        string rollout = NormalizeWhitespace(document.GetSection("Rollout and enablement"));
        rollout.ShouldContain("`qualification-target-identity`", Case.Sensitive);
        rollout.ShouldContain("exact non-Production namespace, approved profile hash, and disabled write state", Case.Sensitive);

        string closeOut = document.GetSection("A41 close-out chain");
        closeOut.ShouldContain("--checkpoint a41-inventory", Case.Sensitive);
        closeOut.ShouldContain("--checkpoint close-out-preflight", Case.Sensitive);
        closeOut.ShouldContain("--checkpoint close-out-postflight", Case.Sensitive);
        closeOut.ShouldContain("--checkpoint publish-verification", Case.Sensitive);
        closeOut.ShouldContain("A local commit is not published", Case.Sensitive);
        closeOut.ShouldContain("change `sprint-status.yaml`", Case.Sensitive);

        markdown.ShouldContain("Production lifecycle writes remain disabled", Case.Sensitive);
        markdown.ShouldContain("A41 is open until remote publish verification succeeds", Case.Sensitive);
        ContractDocumentGuard.FindLeakedToolCallMarkup(markdown).ShouldBeEmpty();
    }

    [Fact]
    public void PostgreSqlAppendix_PinsExactProfileAndSeparatesLogicalFromPhysicalProof()
    {
        string markdown = ReadRepoFile(AdapterRunbook);
        var document = new MarkdownContractDocument(markdown);
        foreach (string section in AdapterSections)
        {
            document.GetSection(section).ShouldNotBeNullOrWhiteSpace(section);
        }

        IReadOnlyList<IReadOnlyList<string>> profile = document.GetTableRows("Scope and immutable profile");
        profile.Single(row => row[0] == "PostgreSQL")[1].ShouldStartWith("18.4");
        profile.Single(row => row[0] == "Profile SHA-256")[1].ShouldBe(
            "`dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`");
        profile.Single(row => row[0] == "HA boundary")[1].ShouldContain("no node, disk, zone, control-plane, or site", Case.Sensitive);

        string reclamation = NormalizeWhitespace(document.GetSection("Purge and physical reclamation proof"));
        reclamation.ShouldContain("Dapr Delete, strong Get absence, and index-member removal", Case.Sensitive);
        reclamation.ShouldContain("VACUUM (ANALYZE, INDEX_CLEANUP ON)", Case.Sensitive);
        reclamation.ShouldContain("Do not use `VACUUM FULL`", Case.Sensitive);
        reclamation.ShouldContain("operating-system disk shrink as `not claimed`", Case.Sensitive);

        string recovery = NormalizeWhitespace(document.GetSection("Backup, restore, and RPO/RTO"));
        recovery.ShouldContain("potentially nonzero RPO/RTO", Case.Sensitive);
        recovery.ShouldContain("successful restore command", Case.Sensitive);
        ContractDocumentGuard.FindLeakedToolCallMarkup(markdown).ShouldBeEmpty();
    }

    [Fact]
    public void LifecycleProjectionDocs_LinkTheCanonicalRunbookWithoutClosingA41()
    {
        string[] projections =
        [
            "docs/dev/telemetry.md",
            "docs/operations/deployment-configuration.md",
            "docs/operations/capacity-planning.md",
            "docs/operations/monitoring-alerting-thresholds.md",
            "docs/operations/incident-response.md",
            "docs/operations/failure-recovery.md",
            "docs/operations/disaster-recovery.md",
            "docs/operations/upgrade-migration.md",
            "docs/operations/rate-limiting.md",
        ];

        foreach (string projection in projections)
        {
            string markdown = ReadRepoFile(projection);
            (markdown.Contains("access-telemetry-lifecycle.md", StringComparison.Ordinal) ||
                markdown.Contains("access-telemetry-adapter-production.md", StringComparison.Ordinal))
                .ShouldBeTrue($"{projection} must link one canonical lifecycle runbook.");
        }

        string rateLimiting = ReadRepoFile("docs/operations/rate-limiting.md");
        rateLimiting.ShouldContain("Story 20.5's inbound per-tenant quota behavior is complete and unchanged", Case.Sensitive);
        rateLimiting.ShouldContain("remains open until", Case.Sensitive);
        rateLimiting.ShouldContain("remote publish verification", Case.Sensitive);
    }

    private static string ReadRepoFile(string relativePath)
    {
        string path = Path.Combine(ResolveRepoRoot(), relativePath);
        File.Exists(path).ShouldBeTrue($"Required Story 27.4 artifact not found at {path}");
        return File.ReadAllText(path);
    }

    private static string NormalizeWhitespace(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

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
