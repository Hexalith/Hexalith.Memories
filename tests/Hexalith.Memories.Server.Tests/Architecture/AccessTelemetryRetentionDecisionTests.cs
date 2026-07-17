// <copyright file="AccessTelemetryRetentionDecisionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>
/// Story 27.1 structure-aware guards for the accepted access-telemetry lifecycle decision.
/// </summary>
public sealed class AccessTelemetryRetentionDecisionTests
{
    private const string AdrRelativePath = "docs/dev/adr-27.1-001-access-telemetry-lifecycle.md";
    private const string ArchitectureRelativePath = "_bmad-output/planning-artifacts/architecture.md";
    private const string TelemetryRelativePath = "docs/dev/telemetry.md";

    [Fact]
    public void Adr_OptionsTable_RatifiesExactlyOneFamilyAndEvaluatesAllThree()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> metadata = adr.GetTableRows("Status and Decision Metadata");
        IReadOnlyList<IReadOnlyList<string>> options = adr.GetTableRows("Options Evaluated");

        GetRow(metadata, "Status").ShouldBe(["Status", "Accepted"]);
        GetRow(metadata, "Selected family").ShouldBe(
            ["Selected family", "Repository-owned dedicated write-only telemetry store"]);
        GetRow(metadata, "Selected technology").ShouldBe(
        [
            "Selected technology",
            "A separate Redis 7.4 access-telemetry workload using `redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a`",
        ]);
        GetRow(metadata, "Implementation gate")[1].ShouldContain(
            "Stories 27.2 and 27.3 are unblocked to implement and verify this accepted contract",
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
        hardGate[2].ShouldStartWith("Selected:");
        hardGate[3].ShouldStartWith("Rejected:");
        hardGate.Skip(1).Count(static cell => cell.StartsWith("Selected:", StringComparison.Ordinal)).ShouldBe(1);
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
            "Capacity Evidence and Admission Envelope",
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
        topology.ShouldContain("independent connection string", Case.Sensitive);
        topology.ShouldContain("A data-member `PodDisruptionBudget` sets `minAvailable: 1`", Case.Sensitive);
        topology.ShouldContain("Before allowing a second voluntary disruption", Case.Sensitive);
        topology.ShouldContain("two-replica Deployment", Case.Sensitive);
        topology.ShouldContain("active/passive", Case.Sensitive);
        topology.ShouldContain("monotonically increasing fencing epoch", Case.Sensitive);
        topology.ShouldContain("resumes incomplete work idempotently", Case.Sensitive);
        topology.ShouldContain("Every client, replication, and Sentinel link is TLS-only", Case.Sensitive);
        topology.ShouldContain("default-deny namespace `NetworkPolicy`", Case.Sensitive);
        topology.ShouldContain("CSI-backed encryption at rest", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> authorities = adr.GetTableRows("Ownership and Topology");
        authorities.Select(static row => row[0]).ShouldBe(
        [
            "`access-telemetry-writer`",
            "`access-telemetry-lifecycle`",
            "`access-telemetry-compactor`",
            "`access-telemetry-inspector`",
            "Redis administration",
        ]);
        GetRow(authorities, "`access-telemetry-writer`")[2].ShouldContain("call Redis `TIME`", Case.Sensitive);
        GetRow(authorities, "`access-telemetry-writer`")[2].ShouldContain("cannot `GET`, `SCAN`, inspect", Case.Sensitive);
        GetRow(authorities, "`access-telemetry-lifecycle`")[2].ShouldContain("fenced purge/reconciliation", Case.Sensitive);
        GetRow(authorities, "`access-telemetry-compactor`")[2].ShouldContain("`BGREWRITEAOF`, and `BGSAVE`", Case.Sensitive);
        GetRow(authorities, "`access-telemetry-inspector`")[2].ShouldContain("cannot write, extend TTL, delete", Case.Sensitive);

        string durability = NormalizeWhitespace(adr.GetSection("Multi-Replica Write and Durability Boundary"));
        durability.ShouldContain("`WAITAOF 1 1 1500`", Case.Sensitive);
        durability.ShouldContain(
            "acknowledged loss window is **0 seconds for any single Server Pod, Redis data Pod, node, or one-PVC failure**",
            Case.Sensitive);
        durability.ShouldContain("simultaneous loss or corruption of both data PVCs", Case.Sensitive);
        durability.ShouldContain("within 1 second of an independent UTC reference", Case.Sensitive);
        durability.ShouldContain("within 1 second of every other participating member", Case.Sensitive);
        durability.ShouldContain("`access-telemetry-clock-preflight` Kubernetes Job", Case.Sensitive);
        durability.ShouldContain("`/internal/access-telemetry/clock` endpoint", Case.Sensitive);
        durability.ShouldContain("`access-telemetry-clock-attestation` Kubernetes Lease", Case.Sensitive);
        durability.ShouldContain("expires no later than 90 seconds after sampling", Case.Sensitive);
        durability.ShouldContain("does **not** gate business readiness", Case.Sensitive);
        durability.ShouldContain("continuously refreshes the independent comparison every minute", Case.Sensitive);
        durability.ShouldContain("Promotion triggers an immediate fresh attestation", Case.Sensitive);
        durability.ShouldContain("not newly generated acceptance time", Case.Sensitive);
        durability.ShouldContain("marks none of the candidates persisted", Case.Sensitive);
        durability.ShouldContain("destroy and simulate corruption of each current primary PVC", Case.Sensitive);

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
        failure.ShouldContain("requires Redis `redis_version:7.4.0`", Case.Sensitive);
        failure.ShouldContain("Every new physical connection after disconnect", Case.Sensitive);
        failure.ShouldContain("contract epoch", Case.Sensitive);
        failure.ShouldContain("provider-specific `Information` filter", Case.Sensitive);
        failure.ShouldContain("a global category filter must not suppress success events", Case.Sensitive);

        string rollback = NormalizeWhitespace(adr.GetSection("Rollback and Transition"));
        rollback.ShouldContain("JSON-console emission and optional OTLP export remain enabled and unchanged", Case.Sensitive);
        rollback.ShouldContain(
            "Rollback never deletes the Redis workload, PVCs, credentials, or retained records automatically.",
            Case.Sensitive);

        string handoff = NormalizeWhitespace(adr.GetSection("Story 27.2 Implementation Handoff"));
        handoff.ShouldContain("is unblocked by this accepted decision", Case.Sensitive);
        handoff.ShouldContain("exact Redis 7.4.0 version and selected image digest", Case.Sensitive);
        handoff.ShouldContain("per-result batch tracking", Case.Sensitive);
        handoff.ShouldContain("fenced staged, quiesced, all-writer-acknowledged rotation barrier", Case.Sensitive);
        handoff.ShouldContain("complete accepted/rejected/enqueued/persisted/retried/failed/dropped/", Case.Sensitive);
        handoff.ShouldContain("expired/purged counter", Case.Sensitive);
        handoff.ShouldContain("two-replica fenced active/passive lifecycle controller", Case.Sensitive);
        handoff.ShouldContain("continuous signed independent-UTC attestations", Case.Sensitive);
        handoff.ShouldContain("reconnect and contract-epoch revalidation", Case.Sensitive);
        handoff.ShouldContain("named Story 20.2 and Story 24.3 guards", Case.Sensitive);
    }

    [Fact]
    public void Adr_RetentionPolicy_IsBoundedAndHasNoSilentUnboundedFallback()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> retention = adr.GetTableRows("Retention, Expiry, Purge, and Clock");

        GetRow(retention, "Production default").ShouldBe(["Production default", "24 hours"]);
        GetRow(retention, "Allowed minimum").ShouldBe(["Allowed minimum", "1 hour"]);
        GetRow(retention, "Allowed maximum").ShouldBe(["Allowed maximum", "7 days"]);
        GetRow(retention, "Configuration owner").ShouldBe(
            ["Configuration owner", "Kustomize through `AccessTelemetryLifecycle__Retention`"]);
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
        section.ShouldContain("timestamps more than 1 second in the future are rejected", Case.Sensitive);
        section.ShouldContain("there is no separate two-minute exception", Case.Sensitive);
        section.ShouldContain("selects at most 512 due entries", Case.Sensitive);
        section.ShouldContain("100-millisecond execution budget", Case.Sensitive);
        section.ShouldContain("invokes synchronous `DEL`", Case.Sensitive);
        section.ShouldContain("never depends on the workload-global `lazyfree_pending_objects` gauge", Case.Sensitive);
        section.ShouldContain("proves Redis object-memory reclamation for that exact candidate cohort", Case.Sensitive);
        section.ShouldContain("`access:control:v1:purge:<cohortId>`", Case.Sensitive);
        section.ShouldContain("separate 24-hour compaction bound", Case.Sensitive);
        section.ShouldContain("invokes `BGREWRITEAOF` and then `BGSAVE`", Case.Sensitive);
        section.ShouldContain("reconstructs the earliest uncompacted cohort", Case.Sensitive);
        section.ShouldNotContain("invokes `UNLINK`", Case.Sensitive);
        section.ShouldNotContain("TBD", Case.Sensitive);
        section.ShouldNotContain("backend default", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> operationCapacity =
            adr.GetTableRows("Operation Envelope");
        operationCapacity.Select(static row => row[0]).ShouldBe(
        [
            "search",
            "ingest",
            "traverse",
            "case-access",
            "delete",
            "tenant-lifecycle",
            "tenant-config",
            "case-member",
            "annotation",
        ]);

        decimal clusterRate = operationCapacity.Sum(static row => ParseDecimal(row[2]));
        clusterRate.ShouldBe(250.0m);
        foreach (IReadOnlyList<string> row in operationCapacity)
        {
            ParseDecimal(row[1]).ShouldBe(ParseDecimal(row[2]) / 2m);
            (decimal average, int p95) = MeasureSanitizedFixture(row[0]);
            average.ShouldBe(ParseDecimal(row[3]));
            p95.ShouldBe(decimal.ToInt32(ParseDecimal(row[4])));
            p95.ShouldBeLessThanOrEqualTo(893);
        }

        IReadOnlyList<IReadOnlyList<string>> retentionSizing = adr.GetTableRows("Retention Sizing");
        GetRow(retentionSizing, "1 hour").ShouldBe(
            ["1 hour", "900,000", "1.93 GiB", "3.43 GiB", "0.38%", "0.22%"]);
        GetRow(retentionSizing, "24 hours").ShouldBe(
            ["24 hours", "21,600,000", "46.35 GiB", "82.40 GiB", "9.05%", "5.36%"]);
        GetRow(retentionSizing, "7 days").ShouldBe(
            ["7 days", "151,200,000", "324.44 GiB", "576.78 GiB", "63.37%", "37.55%"]);

        string capacity = NormalizeWhitespace(adr.GetSection("Capacity Evidence and Admission Envelope"));
        capacity.ShouldContain("1,536-byte per-record Redis memory budget", Case.Sensitive);
        capacity.ShouldContain("1.50 fragmentation multiplier", Case.Sensitive);
        capacity.ShouldContain("4,096-byte per-record PVC budget", Case.Sensitive);
        capacity.ShouldContain("512 GiB configured Redis memory", Case.Sensitive);
        capacity.ShouldContain("1.5 TiB PVC", Case.Sensitive);
        capacity.ShouldContain("60-second writer-sink outage", Case.Sensitive);
        capacity.ShouldContain("10-minute lifecycle-controller outage", Case.Sensitive);
        capacity.ShouldContain("2,560 records/second", Case.Sensitive);
        capacity.ShouldContain("2,310 records/second", Case.Sensitive);
        capacity.ShouldContain("approved operating-cost envelope", Case.Sensitive);

        string schema = NormalizeWhitespace(adr.GetSection("Persisted Schema Bounds"));
        schema.ShouldContain("RFC 8785", Case.Sensitive);
        schema.ShouldContain("at most 1,024 UTF-8 bytes", Case.Sensitive);
        schema.ShouldContain("`schemaVersion` is integer `1`", Case.Sensitive);
        schema.ShouldContain("error inputs map to exactly one of", Case.Sensitive);
        schema.ShouldContain("at most six lexicographically ordered keys", Case.Sensitive);
        schema.ShouldContain("complete encoded command exceeds 1 MiB", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> queryBounds = adr.GetTableRows("Query Parameter Bounds");
        queryBounds.Select(static row => row[0]).ShouldBe(
        [
            "`search`",
            "`ingest`",
            "`traverse`",
            "`case-access`",
            "`delete`",
            "`tenant-lifecycle`",
            "`tenant-config`",
            "`case-member`",
            "`annotation`",
        ]);
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
        retention.ShouldContain("The ADR is `Accepted`", Case.Sensitive);
        retention.ShouldContain("Stories 27.2 and 27.3 are unblocked", Case.Sensitive);

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
        volume.ShouldContain("all-nine-operation admission envelope", Case.Sensitive);
        volume.ShouldContain("250 events/s cluster ceiling", Case.Sensitive);
        volume.ShouldContain("512 GiB configured Redis memory", Case.Sensitive);
        volume.ShouldContain("1.5 TiB PVC per data member", Case.Sensitive);
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
            "from their Redis-recorded final successful write for the 7-day maximum retention, plus the accepted 1-second future-skew bound, plus the 15-minute active purge grace",
            Case.Sensitive);
        privacy.ShouldContain("at least 7 days, 15 minutes, and 1 second", Case.Sensitive);
        privacy.ShouldContain("fenced two-phase protocol", Case.Sensitive);
        privacy.ShouldContain("Both currently ready writer Pod UIDs", Case.Sensitive);
        privacy.ShouldContain("Redis write function then rejects the retired generation", Case.Sensitive);
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
        observability.ShouldContain("fenced cohort checkpoint proves synchronous key/index object removal", Case.Sensitive);

        string implementationHandoff = NormalizeWhitespace(adr.GetSection("Story 27.2 Implementation Handoff"));
        implementationHandoff.ShouldContain("two concurrent writers", Case.Sensitive);
        implementationHandoff.ShouldContain("private writer-clock endpoint", Case.Sensitive);
        implementationHandoff.ShouldContain("controller-triggered AOF/RDB compaction", Case.Sensitive);
        implementationHandoff.ShouldContain("named Story 20.2 and Story 24.3 guards", Case.Sensitive);

        string verificationHandoff = NormalizeWhitespace(adr.GetSection("Story 27.3 Verification and Operations Handoff"));
        verificationHandoff.ShouldContain("prove unique, sanitized records persist through the `WAITAOF` boundary", Case.Sensitive);
        verificationHandoff.ShouldContain("destroy and corrupt each primary PVC", Case.Sensitive);
        verificationHandoff.ShouldContain("continuous independent-attestation freshness/replay/identity", Case.Sensitive);
        verificationHandoff.ShouldContain("business readiness remain available", Case.Sensitive);
        verificationHandoff.ShouldContain("full lifecycle signal set", Case.Sensitive);
        verificationHandoff.ShouldContain("inspection least privilege", Case.Sensitive);
        verificationHandoff.ShouldContain("Publish the operations runbook", Case.Sensitive);
        verificationHandoff.ShouldContain("A41 deferred entry and action close-out", Case.Sensitive);
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

    private static decimal ParseDecimal(string value)
        => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static (decimal Average, int P95) MeasureSanitizedFixture(string operation)
    {
        SortedDictionary<string, object?> queryParams = CreateBoundedQueryParams(operation);
        bool hasCaseMarker = operation is not "tenant-lifecycle" and not "tenant-config";
        bool hasResultCount = operation is "search" or "traverse" or "case-access";
        int successEventId = operation switch
        {
            "search" => 7501,
            "ingest" => 7502,
            "traverse" => 7503,
            "case-access" => 7504,
            "delete" => 7505,
            "tenant-lifecycle" => 7506,
            "tenant-config" => 7507,
            "case-member" => 7508,
            "annotation" => 7509,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown access operation."),
        };

        int[] sizes = Enumerable.Range(0, 100)
            .Select(index =>
            {
                bool isError = index >= 90;
                var record = new SortedDictionary<string, object?>
                {
                    ["acceptedAtUtc"] = "2026-07-17T12:34:56.812Z",
                    ["caseMarker"] = hasCaseMarker ? new string('c', 64) : null,
                    ["durationMs"] = 9999,
                    ["emittedAtUtc"] = "2026-07-17T12:34:56.789Z",
                    ["envelopeHash"] = new string('f', 64),
                    ["errorCode"] = isError ? "internal_dependency_failure" : null,
                    ["eventId"] = isError ? successEventId + 10 : successEventId,
                    ["expiresAtUtc"] = "2026-07-18T12:34:56.789Z",
                    ["markerKeyId"] = "mk-2026a",
                    ["operationType"] = operation,
                    ["outcome"] = isError ? "error" : "ok",
                    ["queryParams"] = queryParams,
                    ["recordId"] = "01K0ABCDEFGHIJKLMNOPQRSTUV",
                    ["resultCount"] = hasResultCount ? 100 : null,
                    ["schemaVersion"] = 1,
                    ["spanId"] = new string('e', 16),
                    ["tenantMarker"] = new string('a', 64),
                    ["traceId"] = new string('d', 32),
                    ["userMarker"] = new string('b', 64),
                };

                return JsonSerializer.SerializeToUtf8Bytes(record).Length;
            })
            .Order()
            .ToArray();

        return (sizes.Sum() / 100m, sizes[94]);
    }

    private static SortedDictionary<string, object?> CreateBoundedQueryParams(string operation)
        => operation switch
        {
            "search" => new()
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "single",
                ["explain"] = true,
                ["limitBucket"] = "51-100",
                ["queryLengthBucket"] = "257-1024",
                ["weightProfile"] = "balanced",
            },
            "ingest" => new()
            {
                ["batchSizeBucket"] = "1",
                ["contentKind"] = "document",
                ["contentLengthBucket"] = "1-10MiB",
                ["sourceKind"] = "url",
            },
            "traverse" => new()
            {
                ["depthBucket"] = "5",
                ["direction"] = "both",
                ["edgeTypeCount"] = 3,
                ["includeGaps"] = true,
            },
            "case-access" => new()
            {
                ["accessKind"] = "memory-unit",
                ["projection"] = "detail",
            },
            "delete" => new()
            {
                ["cascade"] = true,
                ["targetKind"] = "case",
            },
            "tenant-lifecycle" => new()
            {
                ["action"] = "provision",
                ["resourceCountBucket"] = "4-8",
            },
            "tenant-config" => new()
            {
                ["action"] = "update",
                ["changedFieldCountBucket"] = "4-8",
            },
            "case-member" => new()
            {
                ["action"] = "add",
                ["role"] = "editor",
            },
            "annotation" => new()
            {
                ["action"] = "create",
                ["annotationKind"] = "correction",
                ["subjectPresent"] = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown access operation."),
        };

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
