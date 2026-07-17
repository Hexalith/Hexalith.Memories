// <copyright file="AccessTelemetryRetentionDecisionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.IO;
using System.Linq;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>
/// Story 27.1 structure-aware guards for the proposed access-telemetry lifecycle decision.
/// </summary>
public sealed class AccessTelemetryRetentionDecisionTests
{
    private const string AdrRelativePath = "docs/dev/adr-27.1-001-access-telemetry-lifecycle.md";
    private const string ArchitectureRelativePath = "_bmad-output/planning-artifacts/architecture.md";
    private const string TelemetryRelativePath = "docs/dev/telemetry.md";

    [Fact]
    public void Adr_OptionsTable_ProposesExactlyOneFamilyAndEvaluatesAllThree()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> metadata = adr.GetTableRows("Status and Decision Metadata");
        IReadOnlyList<IReadOnlyList<string>> options = adr.GetTableRows("Options Evaluated");

        GetRow(metadata, "Status").ShouldBe(
            ["Status", "Proposed — review-blocked pending the capacity evidence below"]);
        GetRow(metadata, "Selected family").ShouldBe(
            ["Selected family", "Repository-owned dedicated write-only telemetry store"]);
        GetRow(metadata, "Selected technology").ShouldBe(
        [
            "Selected technology",
            "A separate Redis 7.4 access-telemetry workload using `redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a`",
        ]);
        GetRow(metadata, "Implementation gate")[1].ShouldContain(
            "Stories 27.2 and 27.3 remain blocked until the all-nine-operation capacity recalculation is ratified",
            Case.Sensitive);
        adr.GetTableHeader("Options Evaluated").ShouldBe(
        [
            "Lifecycle field",
            "Deployment-owned OpenTelemetry Collector plus Grafana Loki",
            "Dedicated Redis access-telemetry workload",
            "File or volume storage",
        ]);

        options.Select(static row => row[0]).ShouldBe(
        [
            "Ownership and topology",
            "Two-writer behavior",
            "Durability and recovery",
            "Retention, expiry, purge, and clock",
            "Failure and backpressure",
            "Observability",
            "Privacy and tenant boundary",
            "Capacity and operating cost",
            "Rollback",
            "Hard-gate result",
        ]);
        foreach (IReadOnlyList<string> row in options)
        {
            row.Count.ShouldBe(4);
            foreach (string candidateEvaluation in row.Skip(1))
            {
                candidateEvaluation.ShouldNotBeNullOrWhiteSpace();
            }
        }

        IReadOnlyList<string> hardGate = GetRow(options, "Hard-gate result");
        hardGate[1].ShouldStartWith("Rejected:");
        hardGate[2].ShouldStartWith("Provisionally selected:");
        hardGate[3].ShouldStartWith("Rejected:");
        hardGate.Skip(1).Count(static cell => cell.Contains("selected:", StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    public void Adr_CanonicalSections_CoverEveryRequiredLifecycleField()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        string[] requiredSections =
        [
            "Status and Decision Metadata",
            "Verified Current State",
            "Options Evaluated",
            "Selected Design and Rejected Alternatives",
            "Ownership and Topology",
            "Multi-Replica Write and Durability Boundary",
            "Retention, Expiry, Purge, and Clock",
            "Failure, Backpressure, Recovery, and Capacity",
            "Observability",
            "Privacy and Tenant Boundary",
            "Rollback and Transition",
            "Assurance Boundary",
            "Story 27.2 Implementation Handoff",
            "Story 27.3 Verification and Operations Handoff",
        ];

        foreach (string sectionName in requiredSections)
        {
            adr.GetSection(sectionName).ShouldNotBeNullOrWhiteSpace();
        }

        IReadOnlyList<IReadOnlyList<string>> metadata = adr.GetTableRows("Status and Decision Metadata");
        GetRow(metadata, "Approver")[1].ShouldBe("Administrator");
        GetRow(metadata, "Architecture owner")[1].ShouldBe("Hexalith.Memories maintainers");
        GetRow(metadata, "Operational lifecycle owner")[1].ShouldBe("Hexalith Platform Operations");

        string topology = NormalizeWhitespace(adr.GetSection("Ownership and Topology"));
        topology.ShouldContain("two-replica `access-telemetry-redis` StatefulSet", Case.Sensitive);
        topology.ShouldContain("one primary, one replica", Case.Sensitive);
        topology.ShouldContain("three Sentinel Pods", Case.Sensitive);
        topology.ShouldContain("three independently failing nodes", Case.Sensitive);
        topology.ShouldContain("`ReadWriteOnce` persistent volume claim", Case.Sensitive);
        topology.ShouldContain("`access-telemetry-retain` StorageClass", Case.Sensitive);
        topology.ShouldContain("`reclaimPolicy: Retain`", Case.Sensitive);
        topology.ShouldContain("`volumeBindingMode: WaitForFirstConsumer`", Case.Sensitive);
        topology.ShouldContain("independent connection string", Case.Sensitive);
        topology.ShouldContain("Every client, replication, and Sentinel link is TLS-only", Case.Sensitive);
        topology.ShouldContain("default-deny namespace `NetworkPolicy`", Case.Sensitive);
        topology.ShouldContain("CSI-backed encryption at rest", Case.Sensitive);

        string durability = NormalizeWhitespace(adr.GetSection("Multi-Replica Write and Durability Boundary"));
        durability.ShouldContain("`WAITAOF 1 1 1500`", Case.Sensitive);
        durability.ShouldContain(
            "acknowledged loss window is **0 seconds for any single Server Pod, Redis data Pod, node, or one-PVC failure**",
            Case.Sensitive);
        durability.ShouldContain("simultaneous loss or corruption of both data PVCs", Case.Sensitive);
        durability.ShouldContain("within 1 second of an independent UTC reference", Case.Sensitive);
        durability.ShouldContain("within 1 second of every other participating member", Case.Sensitive);
        durability.ShouldContain("`access-telemetry-clock-preflight` Kubernetes Job", Case.Sensitive);
        durability.ShouldContain("each candidate Server writer's UTC clock", Case.Sensitive);
        durability.ShouldContain("repeats the Server-to-Redis comparison every minute", Case.Sensitive);
        durability.ShouldContain("Promotion evidence records the independent reference plus old and new primary `TIME` values", Case.Sensitive);
        durability.ShouldContain("Any difference returns `record_id_conflict`", Case.Sensitive);

        string failure = NormalizeWhitespace(adr.GetSection("Failure, Backpressure, Recovery, and Capacity"));
        failure.ShouldContain("8,192-record limit", Case.Sensitive);
        failure.ShouldContain("64-MiB serialized-byte limit", Case.Sensitive);
        failure.ShouldContain(
            "capped by 5 minutes from event emission and by the record's absolute expiry",
            Case.Sensitive);
        failure.ShouldContain("Shutdown receives 5 seconds to flush", Case.Sensitive);
        failure.ShouldContain(
            "`Unhealthy` with reason `remote_validation_pending`",
            Case.Sensitive);
        failure.ShouldContain("transitions the provider to terminal `configuration_invalid`", Case.Sensitive);
        failure.ShouldContain("Correction requires an explicit Server restart", Case.Sensitive);
        failure.ShouldContain("provider-specific `Information` filter", Case.Sensitive);
        failure.ShouldContain("a global category filter must not suppress success events", Case.Sensitive);

        string rollback = NormalizeWhitespace(adr.GetSection("Rollback and Transition"));
        rollback.ShouldContain("JSON-console emission and optional OTLP export remain enabled and unchanged", Case.Sensitive);
        rollback.ShouldContain(
            "Rollback never deletes the Redis workload, PVCs, credentials, or retained records automatically.",
            Case.Sensitive);
    }

    [Fact]
    public void Adr_RetentionPolicy_IsBoundedAndHasNoSilentUnboundedFallback()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> retention = adr.GetTableRows("Retention, Expiry, Purge, and Clock");

        GetRow(retention, "Production default").ShouldBe(["Production default", "24 hours"]);
        GetRow(retention, "Allowed minimum").ShouldBe(["Allowed minimum", "1 hour"]);
        GetRow(retention, "Allowed maximum").ShouldBe(["Allowed maximum", "7 days"]);
        GetRow(retention, "Authoritative clock").ShouldBe(
            ["Authoritative clock", "Redis primary `TIME`, interpreted as UTC"]);
        GetRow(retention, "Logical expiry").ShouldBe(
            ["Logical expiry", "Absolute millisecond `PXAT` timestamp established atomically with the record write"]);
        GetRow(retention, "Lifecycle sweep").ShouldBe(["Lifecycle sweep", "Every 5 minutes"]);
        GetRow(retention, "Physical-purge grace").ShouldBe(
            ["Physical-purge grace", "No later than 15 minutes after logical expiry while the lifecycle health gate is healthy"]);

        string section = NormalizeWhitespace(adr.GetSection("Retention, Expiry, Purge, and Clock"));
        section.ShouldContain("fail Production startup before serving requests", Case.Sensitive);
        section.ShouldContain("No code path substitutes an unbounded TTL", Case.Sensitive);
        section.ShouldContain("never reset its age", Case.Sensitive);
        section.ShouldContain("selects at most 512 due entries", Case.Sensitive);
        section.ShouldContain("100-millisecond execution budget", Case.Sensitive);
        section.ShouldContain("`lazyfree_pending_objects`", Case.Sensitive);
        section.ShouldContain("namespace purge, not completed physical memory reclamation", Case.Sensitive);
        section.ShouldContain("lazy-free completion must occur no later than 15 minutes", Case.Sensitive);
        section.ShouldContain("separate 24-hour compaction bound", Case.Sensitive);
        section.ShouldNotContain("TBD", Case.Sensitive);
        section.ShouldNotContain("backend default", Case.Sensitive);
    }

    [Fact]
    public void Adr_ProductionFactsAndFileOption_KeepAllFileHardGatesExplicit()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        string currentState = NormalizeWhitespace(adr.GetSection("Verified Current State"));

        currentState.ShouldContain("two replicas", Case.Sensitive);
        currentState.ShouldContain("read-only root filesystem", Case.Sensitive);
        currentState.ShouldContain("no OTLP endpoint or access-telemetry backend", Case.Sensitive);
        currentState.ShouldContain("ephemeral `/tmp` `emptyDir`", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> options = adr.GetTableRows("Options Evaluated");
        GetRow(options, "Two-writer behavior")[3].ShouldContain("unresolved locking/rotation", Case.Sensitive);
        GetRow(options, "Durability and recovery")[3].ShouldContain("deleted on Pod removal", Case.Sensitive);
        GetRow(options, "Retention, expiry, purge, and clock")[3].ShouldContain("not record TTL", Case.Sensitive);
        GetRow(options, "Hard-gate result")[3].ShouldContain(
            "fails the current two-replica, read-only-root, rescheduling, rotation, and executable-purge gates",
            Case.Sensitive);
    }

    [Fact]
    public void DecisionDocuments_CrossLinksAreExactAndStaleStructuredFileClaimIsAbsent()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        MarkdownContractDocument architecture = ReadDocument(ArchitectureRelativePath);
        MarkdownContractDocument telemetry = ReadDocument(TelemetryRelativePath);

        NormalizeWhitespace(adr.GetSection("Selected Design and Rejected Alternatives")).ShouldContain(
            "`redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a`",
            Case.Sensitive);

        string security = NormalizeWhitespace(architecture.GetSection("Security Architecture"));
        security.ShouldContain(
            "[ADR 27.1-001](../../docs/dev/adr-27.1-001-access-telemetry-lifecycle.md)",
            Case.Sensitive);
        security.ShouldContain("separate Redis 7.4 write-only access-telemetry workload", Case.Sensitive);
        security.ShouldContain("three independently spread Sentinel members", Case.Sensitive);
        security.ShouldContain("retained encrypted PVCs", Case.Sensitive);
        security.ShouldContain("TLS-only, default-deny network access", Case.Sensitive);
        security.ShouldContain("`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains open", Case.Sensitive);
        security.ShouldContain(
            "not tamper-evident, append-only, legally compliant, or certified audit retention",
            Case.Sensitive);
        security.ShouldNotContain("MVP: structured log file", Case.Sensitive);

        string retention = NormalizeWhitespace(telemetry.GetSection("Retention lifecycle status"));
        retention.ShouldContain("[ADR 27.1-001](adr-27.1-001-access-telemetry-lifecycle.md)", Case.Sensitive);
        retention.ShouldContain("dedicated Redis 7.4 access-telemetry workload", Case.Sensitive);
        retention.ShouldContain("The ADR is `Proposed`, not ratified", Case.Sensitive);
        retention.ShouldContain("Stories 27.2 and 27.3 remain blocked", Case.Sensitive);

        string routing = NormalizeWhitespace(telemetry.GetSection("Audit log routing recipe"));
        routing.ShouldContain("typed `AccessTelemetryEvent` logger state", Case.Sensitive);
        routing.ShouldNotContain("dedicated JSON file sink", Case.Sensitive);
        routing.ShouldNotContain("canonical form", Case.Sensitive);

        string schema = NormalizeWhitespace(telemetry.GetSection("Audit event schema (FR67)"));
        schema.ShouldContain("known privacy deviation", Case.Sensitive);
        schema.ShouldContain("raw `query` and `subject` values", Case.Sensitive);
        schema.ShouldContain("raw `sourceUri`", Case.Sensitive);
        schema.ShouldContain("Do not persist or replay those raw values", Case.Sensitive);
        schema.ShouldContain("That target is not implemented by this decision story", Case.Sensitive);

        string logLevelGate = NormalizeWhitespace(telemetry.GetSection("Log-level config gate"));
        logLevelGate.ShouldContain("provider/category scoped, not tenant scoped", Case.Sensitive);
        logLevelGate.ShouldContain("lifecycle provider must independently keep", Case.Sensitive);
        logLevelGate.ShouldContain("A global category-level `Warning` filter", Case.Sensitive);

        string volume = NormalizeWhitespace(telemetry.GetSection("Access telemetry volume estimates"));
        volume.ShouldContain("two-replica Production shape", Case.Sensitive);
        volume.ShouldContain("It is not an all-nine-operation capacity calculation", Case.Sensitive);
        volume.ShouldContain("must not size memory, PVCs, retention, or cost", Case.Sensitive);
    }

    [Fact]
    public void DecisionBoundaries_AssuranceA41PrivacySignalsAndDownstreamMapsRemainExplicit()
    {
        string adrMarkdown = ReadRepoFile(AdrRelativePath);
        string architectureMarkdown = ReadRepoFile(ArchitectureRelativePath);
        string telemetryMarkdown = ReadRepoFile(TelemetryRelativePath);
        var adr = new MarkdownContractDocument(adrMarkdown);
        var telemetry = new MarkdownContractDocument(telemetryMarkdown);

        NormalizeWhitespace(adr.GetSection("Assurance Boundary")).ShouldContain(
            "Bounded infrastructure telemetry only; no tamper evidence, append-only integrity, legal compliance, or certified audit retention.",
            Case.Sensitive);

        string privacy = NormalizeWhitespace(adr.GetSection("Privacy and Tenant Boundary"));
        privacy.ShouldContain("The Server has no read or arbitrary-delete authority", Case.Sensitive);
        privacy.ShouldContain("There is no tenant-facing read API", Case.Sensitive);
        privacy.ShouldContain("Raw tenant, user, case, query, subject, source URI", Case.Sensitive);
        privacy.ShouldContain("Every persisted marker carries `markerKeyId`", Case.Sensitive);
        privacy.ShouldContain(
            "from the final successful record written with each old key for the 7-day maximum retention, plus the accepted 2-minute future-skew window, plus the 15-minute active purge grace",
            Case.Sensitive);
        privacy.ShouldContain("at least 7 days 17 minutes", Case.Sensitive);
        privacy.ShouldContain("Story 20.2 guards", Case.Sensitive);
        privacy.ShouldContain("Story 24.3 verifier guards", Case.Sensitive);

        string observability = NormalizeWhitespace(adr.GetSection("Observability"));
        string[] states = ["accepted", "rejected", "enqueued", "persisted", "retried", "failed", "dropped", "expired", "purged"];
        foreach (string state in states)
        {
            observability.ShouldContain($"**{state}**", Case.Sensitive);
        }

        observability.ShouldContain("`NoData`", Case.Sensitive);
        observability.ShouldContain(
            "Metric labels must never contain tenant, user, case, memory-unit, query, subject, source, trace, span, or record identifiers.",
            Case.Sensitive);
        observability.ShouldContain(
            "Health endpoints expose only bounded state, reason, capacity percentages, and ages",
            Case.Sensitive);
        observability.ShouldContain("`Unhealthy` takes precedence over `Degraded`", Case.Sensitive);
        observability.ShouldContain("A disconnected or unvalidated sink with no events is therefore `Unhealthy`, never `NoData`", Case.Sensitive);
        observability.ShouldContain("`lazyfree_pending_objects`", Case.Sensitive);

        NormalizeWhitespace(adr.GetSection("Story 27.2 Implementation Handoff")).ShouldContain("two concurrent writers", Case.Sensitive);
        NormalizeWhitespace(adr.GetSection("Story 27.3 Verification and Operations Handoff")).ShouldContain(
            "A41 deferred entry and action close-out",
            Case.Sensitive);
        NormalizeWhitespace(telemetry.GetSection("Retention lifecycle status")).ShouldContain(
            "`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward and its action remains open",
            Case.Sensitive);

        ContractDocumentGuard.FindLeakedToolCallMarkup(adrMarkdown).ShouldBeEmpty();
        ContractDocumentGuard.FindLeakedToolCallMarkup(architectureMarkdown).ShouldBeEmpty();
        ContractDocumentGuard.FindLeakedToolCallMarkup(telemetryMarkdown).ShouldBeEmpty();
    }

    private static IReadOnlyList<string> GetRow(
        IReadOnlyList<IReadOnlyList<string>> rows,
        string firstCell)
        => rows.Single(row => string.Equals(row[0], firstCell, StringComparison.Ordinal));

    private static MarkdownContractDocument ReadDocument(string relativePath)
        => new(ReadRepoFile(relativePath));

    private static string NormalizeWhitespace(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string ReadRepoFile(string relativePath)
    {
        string path = Path.Combine(ResolveRepoRoot(), relativePath);
        File.Exists(path).ShouldBeTrue($"Required Story 27.1 artifact not found at {path}");
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

        return AppContext.BaseDirectory;
    }
}
