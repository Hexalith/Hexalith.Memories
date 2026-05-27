// <copyright file="DocumentationCompletenessTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.IO;
using System.Linq;

using Shouldly;

/// <summary>Story 9.1 AC #17 — verifies <c>docs/dev/eventstore-integration.md</c> contains the sections
/// and key phrases reviewers flagged as must-have. Intentionally asserts on concrete content (TTL
/// coupling, publisher-spoofing threat model, source-stability contract, alerting, env defaults) rather
/// than only section headers so documentation rot can't silently regress the story's acceptance bar.</summary>
public sealed class DocumentationCompletenessTests
{
    private static string ResolveDocPath()
    {
        // Walk up from the test binary to the repo root, then resolve docs/dev/eventstore-integration.md.
        string candidate = System.AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string marker = Path.Combine(candidate, "Hexalith.Memories.slnx");
            if (File.Exists(marker))
            {
                return Path.Combine(candidate, "docs", "dev", "eventstore-integration.md");
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return Path.Combine(System.AppContext.BaseDirectory, "docs", "dev", "eventstore-integration.md");
    }

    [Fact]
    public void EventStoreIntegrationDoc_HasRequiredSectionsAndKeyContent()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"Documentation file not found at {path}");

        string content = File.ReadAllText(path);

        // Required sections (title fragments).
        string[] requiredSectionFragments =
        [
            "Setup",
            "CloudEvents envelope",
            "At-least-once",
            "Replay semantics",
            "Publisher trust",
            "Source-stability",
            "Alerting",
            "Preflight TTL",
            "Known limitations",
            "Troubleshooting",
            "Worked example",
            "Environment defaults table",
        ];
        foreach (string fragment in requiredSectionFragments)
        {
            content.ShouldContain(fragment, Case.Insensitive, $"Missing required documentation section fragment: {fragment}");
        }

        // Required content checks (not only headers).
        content.ShouldContain("SourceToTenantMap", Case.Sensitive, "Routing config schema must appear inline.");
        content.ShouldContain("MEMORIES_EVENTSTORE_TOPIC", Case.Sensitive, "Topic env var must be documented.");
        content.ShouldContain("AutoCreateCases", Case.Sensitive, "Auto-create case option must be documented.");
        content.ShouldContain("PreflightDedupTtl", Case.Sensitive, "Preflight TTL option must appear in docs.");
        content.ShouldContain("aggregateType", Case.Insensitive, "Aggregate-type extraction rule must be documented.");
        content.ShouldContain("cloudevent.subject", Case.Sensitive, "Exact-match subject filtering must be documented.");
        content.ShouldContain("publishAllowedTopics", Case.Sensitive, "MVP publisher-spoofing mitigation must be documented.");
        content.ShouldContain("max-duration", Case.Sensitive, "TTL ↔ DAPR retry max-duration coupling must be documented.");
        content.ShouldContain("9110", Case.Sensitive, "Unknown-source alert event id must be documented.");
        content.ShouldContain("9120", Case.Sensitive, "Schedule-failed alert event id must be documented.");

        // Environment defaults table must contain both Development and Production columns.
        content.Contains("| Development |", System.StringComparison.Ordinal)
            .ShouldBeTrue("Environment defaults table must document the Development column.");
        content.Contains("| Production |", System.StringComparison.Ordinal)
            .ShouldBeTrue("Environment defaults table must document the Production column.");

        // Worked example must include a publish call + a search call so readers see the end-to-end flow.
        content.ShouldContain("PublishEventAsync", Case.Sensitive, "Worked example must include DAPR publish call.");
        content.ShouldContain("/api/search", Case.Sensitive, "Worked example must end with a search against Memories.");
    }
}
