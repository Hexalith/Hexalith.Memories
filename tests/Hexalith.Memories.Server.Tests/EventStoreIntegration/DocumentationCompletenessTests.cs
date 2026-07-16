// <copyright file="DocumentationCompletenessTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.IO;
using System.Linq;

using Hexalith.Memories.TestHelpers.Documentation;

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
    public void EventStoreIntegrationDoc_HasEachRequiredExactHeadingOnce()
    {
        var document = ReadDocument();
        string[] requiredHeadings =
        [
            "1. Setup",
            "1.5 Environment defaults table",
            "1.6 Route surface for Hexalith modules",
            "2. CloudEvents envelope requirements",
            "2.1 Aggregate-type extraction",
            "2.2 Exact-match subject filtering",
            "3. At-least-once + dead-letter + replay semantics",
            "3.1 Replay semantics",
            "4. Publisher trust & spoofing — deploy-time mitigations",
            "5. Source-stability publisher contract",
            "6. Alerting recommendations",
            "7. Preflight TTL ↔ DAPR retry-policy alignment",
            "8. Known limitations",
            "9. Troubleshooting — \"Why didn't my event appear?\"",
            "10. Worked example — from publish to searchable memory",
        ];

        foreach (string heading in requiredHeadings)
        {
            document.GetSection(heading).ShouldNotBeNull();
        }
    }

    [Fact]
    public void EventStoreIntegrationDoc_HasNormalizedContractRows()
    {
        var document = ReadDocument();
        IReadOnlyList<IReadOnlyList<string>> environment = document.GetTableRows("1.5 Environment defaults table");
        IReadOnlyList<IReadOnlyList<string>> envelope = document.GetTableRows("2. CloudEvents envelope requirements");
        IReadOnlyList<IReadOnlyList<string>> outcomes = document.GetTableRows("3. At-least-once + dead-letter + replay semantics");
        IReadOnlyList<IReadOnlyList<string>> alerts = document.GetTableRows("6. Alerting recommendations");

        document.GetTableHeader("1.5 Environment defaults table").ShouldBe(["Option", "Development", "Production", "Rationale"]);
        document.GetTableHeader("2. CloudEvents envelope requirements").ShouldBe(["Field", "Required", "Notes"]);
        document.GetTableHeader("3. At-least-once + dead-letter + replay semantics").ShouldBe(["Situation", "HTTP", "DAPR behavior"]);
        document.GetTableHeader("6. Alerting recommendations").ShouldBe(["Signal", "Source", "Recommended alert"]);

        environment.Count.ShouldBe(4);
        environment[0].ShouldBe(["`AutoCreateCases`", "`true`", "`false`", "Development optimizes for zero-config DX (PRD §534). Production requires explicit tenant/case provisioning so a mis-routed publisher can't silently create cases. ADR 9.1-C."]);
        environment[1].ShouldBe(["`MaxAutoCreatedCasesPerTenant`", "`100`", "`100`", "Hard cap is a safety backstop regardless of environment."]);
        environment[2].ShouldBe(["`PreflightDedupEnabled`", "`true`", "`true`", "Saves 1-3 s of embedding compute per at-least-once redelivery. Fails open on Redis outage. ADR 9.1-B."]);
        environment[3].ShouldBe(["`PreflightDedupTtl`", "`24h`", "**Must be ≥ DAPR resiliency max-duration + 10% buffer**", "See §7 TTL coupling."]);

        envelope.Count.ShouldBe(7);
        envelope[0].ShouldBe(["`id`", "**Yes**", "Drives idempotency — the existing `DedupKeyBuilder` hashes this as `sourceUri`. Must be globally unique per at-least-once semantics."]);
        envelope[1].ShouldBe(["`source`", "**Yes**", "Publisher-supplied URI-reference. Matched longest-prefix (case-insensitive) against `SourceToTenantMap`."]);
        envelope[2].ShouldBe(["`type`", "**Yes**", "Aggregate type extracted from the **second dotted segment** (e.g. `MyApp.Claims.ClaimSubmittedV2` → `Claims`). Falls back to the full type when no second segment exists."]);
        envelope[3].ShouldBe(["`subject`", "Optional", "Aggregate identifier. Absent values persist as `(unset)`. Exact-match filterable via the `cloudevent.subject` metadata field (AC #2, #4)."]);
        envelope[4].ShouldBe(["`time`", "Optional", "ISO-8601 publisher-supplied timestamp. Preserved verbatim; **never replaced with server time** (clock-skew risk)."]);
        envelope[5].ShouldBe(["`datacontenttype`", "Optional", "Defaults to `application/json` when absent."]);
        envelope[6].ShouldBe(["`data`", "**Yes**", "Event payload. Missing or null `data` returns `400 INVALID_CLOUDEVENT`."]);

        outcomes.Count.ShouldBe(10);
        outcomes[0].ShouldBe(["Accepted — workflow scheduled", "200 + `accepted`", "No retry."]);
        outcomes[1].ShouldBe(["Duplicate — preflight, workflow-level dedup, or deterministic workflow instance already exists", "200 + `duplicate`", "No retry."]);
        outcomes[2].ShouldBe(["Unknown source", "200 + `unknown-source` + Warning log", "No retry (publisher never mapped)."]);
        outcomes[3].ShouldBe(["Tenant not found", "500 + `tenant-not-found` + Warning log", "Retry; reaches DLT only if operators configure DAPR retry + dead-letter topics."]);
        outcomes[4].ShouldBe(["Tenant deleting or unavailable", "500 + `tenant-deleting` + Warning log", "Retry; reaches DLT only if operators configure DAPR retry + dead-letter topics."]);
        outcomes[5].ShouldBe(["Auto-create disabled", "200 + `auto-create-disabled`", "No retry (operator opted out)."]);
        outcomes[6].ShouldBe(["Case cap exceeded", "200 + `case-cap-exceeded` + Warning log", "No retry."]);
        outcomes[7].ShouldBe(["Tenant provisioning", "500", "Retries until tenant becomes active or retry budget exhausts."]);
        outcomes[8].ShouldBe(["Malformed envelope (`id`/`source`/`type`/`data` missing)", "400 + `INVALID_CLOUDEVENT`", "Dead-letter topic (if configured); else dropped."]);
        outcomes[9].ShouldBe(["Transient scheduling failure", "500 (preflight reservation released)", "DAPR retries on clean key."]);

        alerts.Count.ShouldBe(6);
        alerts[0].ShouldBe(["`memories_eventstore_unknownsource_total{source=...}`", "EventId 9110", "Rate of increase > 0 for 5 min pages the subscriber team. Indicates publisher drift or a misconfigured `SourceToTenantMap`."]);
        alerts[1].ShouldBe(["EventId 9111 / 9112 (tenant deleting/unavailable or missing)", "Warning", "Rate > 0 for 5 min warns operators to fix tenant rollout/registry state or inspect DAPR retry/DLT."]);
        alerts[2].ShouldBe(["EventId 9121 (invalid-envelope)", "Error", "Rate > 5/min pages. Indicates a publisher is emitting malformed CloudEvents."]);
        alerts[3].ShouldBe(["EventId 9120 (schedule-failed)", "Error", "Rate > 1/min pages. Transient DAPR sidecar / workflow runtime problem."]);
        alerts[4].ShouldBe(["EventId 9105 (routing-config-unknown-tenant)", "Critical, startup", "Fail-fast crash — do not restart the pod without fixing config."]);
        alerts[5].ShouldBe(["EventId 9114 (case-cap-exceeded)", "Warning", "Warn operator; likely aggregate-type cardinality misconfiguration (e.g. `type` encoded into an id)."]);
    }

    [Fact]
    public void EventStoreIntegrationDoc_NarrativeClaimsStayInOwningSections()
    {
        var document = ReadDocument();

        string setup = document.GetSection("1. Setup");
        setup.ShouldContain("SourceToTenantMap", Case.Sensitive);
        setup.ShouldContain("MEMORIES_EVENTSTORE_TOPIC", Case.Sensitive);
        setup.ShouldContain("AutoCreateCases", Case.Sensitive);
        setup.ShouldContain("PreflightDedupTtl", Case.Sensitive);

        string route = document.GetSection("1.6 Route surface for Hexalith modules");
        route.ShouldContain("Hexalith modules", Case.Sensitive);
        route.ShouldContain("hexalith/tenants", Case.Sensitive);
        route.ShouldContain("hexalith/parties", Case.Sensitive);
        route.ShouldContain("/dapr/subscribe", Case.Sensitive);
        route.ShouldContain("POST /events/ingest", Case.Sensitive);
        route.ShouldContain("`/process` is not part of the Memories event-ingest surface", Case.Sensitive);
        route.ShouldContain("shared-topic pattern", Case.Sensitive);
        route.ShouldContain("separate Memories", Case.Sensitive);
        route.ShouldContain("deployments per topic", Case.Sensitive);

        document.GetSection("2.1 Aggregate-type extraction").ShouldContain("second dotted", Case.Insensitive);
        document.GetSection("2.2 Exact-match subject filtering").ShouldContain("cloudevent.subject", Case.Sensitive);
        document.GetSection("4. Publisher trust & spoofing — deploy-time mitigations").ShouldContain("publishAllowedTopics", Case.Sensitive);
        document.GetSection("5. Source-stability publisher contract").ShouldContain("opaque identifier", Case.Sensitive);
        document.GetSection("7. Preflight TTL ↔ DAPR retry-policy alignment").ShouldContain("max-duration", Case.Sensitive);

        string workedExample = document.GetSection("10. Worked example — from publish to searchable memory");
        workedExample.ShouldContain("PublishEventAsync", Case.Sensitive);
        workedExample.ShouldContain("/api/v1/search", Case.Sensitive);
    }

    [Fact]
    public void EventStoreIntegrationDoc_ContainsNoLeakedToolCallMarkup()
    {
        string content = File.ReadAllText(ResolveDocPath());
        IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(content);

        diagnostics.ShouldBeEmpty($"docs/dev/eventstore-integration.md contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
    }

    private static MarkdownContractDocument ReadDocument()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"Documentation file not found at {path}");
        return new MarkdownContractDocument(File.ReadAllText(path));
    }
}
