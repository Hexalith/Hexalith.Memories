// <copyright file="AccessTelemetryRetentionDecisionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

using AccessTelemetryEvent = Hexalith.Memories.Contracts.V1.AccessTelemetryEvent;

/// <summary>
/// Story 27.1 structure-aware guards for the accepted access-telemetry lifecycle decision.
/// </summary>
public sealed partial class AccessTelemetryRetentionDecisionTests
{
    private const string AdrRelativePath = "docs/dev/adr-27.1-001-access-telemetry-lifecycle.md";
    private const string EvidenceMatrixRelativePath = "_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md";
    private const string AccessEventSourceRelativePath = "src/Hexalith.Memories.Contracts/V1/AccessTelemetryEvent.cs";
    private const string ArchitectureRelativePath = "_bmad-output/planning-artifacts/architecture.md";
    private const string LifecycleMetricContractRelativePath = "src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryMetricContract.cs";
    private const string LifecycleRunbookRelativePath = "docs/operations/access-telemetry-lifecycle.md";
    private const string OperationSourceRelativePath = "src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs";
    private const string TelemetryRelativePath = "docs/dev/telemetry.md";

    private static readonly string[] RequiredSections =
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
        "Stories 27.3 and 27.4 Verification and Operations Handoff",
        "Production Adapter Qualification — PG-ONPREM-1",
    ];

    [Fact]
    public void Adr_OptionsTable_RatifiesExactlyOneFamilyAndEvaluatesAllThree()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> metadata = adr.GetTableRows("Status and Decision Metadata");
        IReadOnlyList<IReadOnlyList<string>> options = adr.GetTableRows("Options Evaluated");

        AssertCellCounts(metadata, 2, "Status and Decision Metadata");
        GetRow(metadata, "Status").ShouldBe(["Status", "Accepted"]);
        GetRow(metadata, "Selected family").ShouldBe(
            ["Selected family", "Repository-owned dedicated write-only telemetry service"]);
        GetRow(metadata, "Selected technology")[1].ShouldContain(
            "Dapr service invocation, state management, actors, reminders, configuration, and secrets",
            Case.Sensitive);
        GetRow(metadata, "Selected technology")[1].ShouldContain(
            "no application dependency on a specific state-store product or container orchestrator",
            Case.Sensitive);
        GetRow(metadata, "Implementation gate")[1].ShouldContain(
            "Story 27.3 qualifies the exact Production adapter",
            Case.Sensitive);

        adr.GetTableHeader("Options Evaluated").ShouldBe(
        [
            "Lifecycle field",
            "Deployment-owned OpenTelemetry backend",
            "Dapr-backed dedicated lifecycle service",
            "File or volume storage",
        ]);
        AssertCellCounts(options, 4, "Options Evaluated");
        options.Select(static row => row[0]).ShouldBe(
        [
            "Ownership and topology",
            "Multi-writer behavior",
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
            row.Skip(1).ShouldAllBe(static value => !string.IsNullOrWhiteSpace(value));
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
        foreach (string sectionName in RequiredSections)
        {
            string section = adr.GetSection(sectionName);
            section.ShouldNotBeNullOrWhiteSpace();
            NormalizeWhitespace(section).ShouldNotContain("TBD", Case.Sensitive);
            NormalizeWhitespace(section).ShouldNotContain("TODO", Case.Sensitive);
        }

        IReadOnlyList<IReadOnlyList<string>> metadata = adr.GetTableRows("Status and Decision Metadata");
        GetRow(metadata, "Approver")[1].ShouldBe("Administrator");
        GetRow(metadata, "Architecture owner")[1].ShouldBe("Hexalith.Memories maintainers");
        GetRow(metadata, "Operational lifecycle owner")[1].ShouldBe("Hexalith Platform Operations");
        GetRow(metadata, "Affected deployment")[1].ShouldContain("Any container service", Case.Sensitive);

        string selected = NormalizeWhitespace(adr.GetSection("Selected Design and Rejected Alternatives"));
        selected.ShouldContain("Memories code calls Dapr APIs only", Case.Sensitive);
        selected.ShouldContain("does not link a backend SDK", Case.Sensitive);
        selected.ShouldContain("require Kubernetes or any other orchestrator", Case.Sensitive);
        selected.ShouldContain("Alpha status is not a waiver", Case.Sensitive);
        selected.ShouldContain("`allowAlphaComponent: true`", Case.Sensitive);

        string topology = NormalizeWhitespace(adr.GetSection("Ownership and Topology"));
        topology.ShouldContain("`memories-access-telemetry`", Case.Sensitive);
        topology.ShouldContain("`memories-access-telemetry-clock`", Case.Sensitive);
        topology.ShouldContain("`AccessTelemetryLifecycleActor/global`", Case.Sensitive);
        topology.ShouldContain("`access-telemetry-store`", Case.Sensitive);
        topology.ShouldContain("No Pod UID, StatefulSet ordinal, Lease, PVC, or orchestrator API", Case.Sensitive);
        topology.ShouldContain("durable actor state and reminders", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> capabilities = adr.GetTableRows("Component Capability Gates");
        AssertCellCounts(capabilities, 2, "Component Capability Gates");
        string[] requiredCapabilityRows =
        [
            "Dapr-only boundary",
            "State semantics",
            "Actor semantics",
            "TTL semantics",
            "Request bounds",
            "Throughput",
            "Durability",
            "Isolation and encryption",
            "Capacity and reclamation",
        ];
        foreach (string rowName in requiredCapabilityRows)
        {
            GetRow(capabilities, rowName)[1].ShouldNotBeNullOrWhiteSpace();
        }

        IReadOnlyList<IReadOnlyList<string>> authorities = adr.GetTableRows("Authorities");
        AssertCellCounts(authorities, 3, "Authorities");
        string[] requiredAuthorityRows =
        [
            "`access-telemetry-writer`",
            "`access-telemetry-service`",
            "`access-telemetry-clock`",
            "`access-telemetry-inspector`",
            "`access-telemetry-adapter`",
        ];
        foreach (string rowName in requiredAuthorityRows)
        {
            GetRow(authorities, rowName)[2].ShouldNotBeNullOrWhiteSpace();
        }

        string durability = NormalizeWhitespace(adr.GetSection("Multi-Replica Write and Durability Boundary"));
        durability.ShouldContain("`AccessTelemetryLifecycleActor/global`", Case.Sensitive);
        durability.ShouldContain("one Dapr state transaction", Case.Sensitive);
        durability.ShouldContain("64 deterministic shards", Case.Sensitive);
        durability.ShouldContain("`record_id_conflict`", Case.Sensitive);
        durability.ShouldContain(
            "0 seconds for any single Server or lifecycle-service process, container, or host loss",
            Case.Sensitive);
        durability.ShouldContain("through Dapr every 10 seconds", Case.Sensitive);
        durability.ShouldContain("expires after 30 seconds", Case.Sensitive);
        durability.ShouldContain("greater than 1 second", Case.Sensitive);
        durability.ShouldContain("does **not** gate business readiness", Case.Sensitive);

        string failure = NormalizeWhitespace(adr.GetSection("Failure, Backpressure, Recovery, and Capacity"));
        failure.ShouldContain("8,192-record limit", Case.Sensitive);
        failure.ShouldContain("64-MiB serialized-byte limit", Case.Sensitive);
        failure.ShouldContain("capped by 5 minutes from event emission", Case.Sensitive);
        failure.ShouldContain("Shutdown receives 5 seconds to flush", Case.Sensitive);
        failure.ShouldContain("`remote_validation_pending`", Case.Sensitive);
        failure.ShouldContain("terminal `configuration_invalid`", Case.Sensitive);
        failure.ShouldContain("business readiness stays available", Case.Sensitive);

        string rollback = NormalizeWhitespace(adr.GetSection("Rollback and Transition"));
        rollback.ShouldContain("JSON-console emission and optional OTLP export remain enabled and unchanged", Case.Sensitive);
        rollback.ShouldContain("Rollback never deletes a Dapr component", Case.Sensitive);
    }

    [Fact]
    public void Adr_RetentionPolicy_IsBoundedAndHasNoSilentUnboundedFallback()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> retention = adr.GetTableRows("Retention, Expiry, Purge, and Clock");

        AssertCellCounts(retention, 2, "Retention, Expiry, Purge, and Clock");
        GetRow(retention, "Production default").ShouldBe(["Production default", "24 hours"]);
        GetRow(retention, "Allowed minimum").ShouldBe(["Allowed minimum", "1 hour"]);
        GetRow(retention, "Allowed maximum").ShouldBe(["Allowed maximum", "7 days"]);
        GetRow(retention, "Configuration owner")[1].ShouldContain("Dapr configuration", Case.Sensitive);
        GetRow(retention, "Authoritative clock")[1].ShouldContain("Signed independent UTC", Case.Sensitive);
        GetRow(retention, "Logical expiry")[1].ShouldContain("Absolute millisecond", Case.Sensitive);
        GetRow(retention, "Defense-in-depth TTL")[1].ShouldContain("Dapr state TTL", Case.Sensitive);
        GetRow(retention, "Lifecycle sweep").ShouldBe(["Lifecycle sweep", "Durable actor reminder every 5 minutes"]);
        GetRow(retention, "Active-purge grace")[1].ShouldContain("15 minutes", Case.Sensitive);
        GetRow(retention, "Physical-reclamation bound")[1].ShouldContain("per component", Case.Sensitive);

        string section = NormalizeWhitespace(adr.GetSection("Retention, Expiry, Purge, and Clock"));
        section.ShouldContain("No code path substitutes an unbounded TTL", Case.Sensitive);
        section.ShouldContain("never reset age or extend expiry", Case.Sensitive);
        section.ShouldContain("more than 1 second ahead", Case.Sensitive);
        section.ShouldContain("Lowering retention applies to new records only", Case.Sensitive);
        section.ShouldContain("at most 512 records per actor turn", Case.Sensitive);
        section.ShouldContain("100-millisecond observed execution budget", Case.Sensitive);
        section.ShouldContain("does **not** claim", Case.Sensitive);
        section.ShouldContain("outside the application API", Case.Sensitive);
        section.ShouldContain("never greater than 24 hours", Case.Sensitive);
        section.ShouldNotContain("backend default", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> operationCapacity = adr.GetTableRows("Operation Envelope");
        AssertCellCounts(operationCapacity, 5, "Operation Envelope");
        string[] operationRows = operationCapacity.Select(static row => row[0]).ToArray();
        operationRows.ShouldBe(
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
        ReadSourceOperationTypes().ShouldBe(operationRows, ignoreOrder: true);

        decimal clusterRate = operationCapacity.Sum(static row => ParseDecimal(row[2]));
        clusterRate.ShouldBe(250.0m);
        foreach (IReadOnlyList<string> row in operationCapacity)
        {
            ParseDecimal(row[1]).ShouldBe(ParseDecimal(row[2]) / 2m);
            (decimal average, int p95) = MeasureSanitizedFixture(row[0]);
            average.ShouldBe(ParseDecimal(row[3]));
            p95.ShouldBe(ParseInt(row[4]));
            p95.ShouldBeLessThanOrEqualTo(900);
        }

        IReadOnlyList<IReadOnlyList<string>> retentionSizing = adr.GetTableRows("Retention Sizing");
        AssertCellCounts(retentionSizing, 3, "Retention Sizing");
        GetRow(retentionSizing, "1 hour").ShouldBe(["1 hour", "900,000", "0.86 GiB"]);
        GetRow(retentionSizing, "24 hours").ShouldBe(["24 hours", "21,600,000", "20.60 GiB"]);
        GetRow(retentionSizing, "7 days").ShouldBe(["7 days", "151,200,000", "144.20 GiB"]);

        string capacity = NormalizeWhitespace(adr.GetSection("Capacity Evidence and Admission Envelope"));
        capacity.ShouldContain("authoritative capacity ceiling is the 1,024-byte", Case.Sensitive);
        capacity.ShouldContain("physical bytes per record and index entry", Case.Sensitive);
        capacity.ShouldContain("at most 70%", Case.Sensitive);
        capacity.ShouldContain("at most 80%", Case.Sensitive);
        capacity.ShouldContain("at least 500 events/s", Case.Sensitive);
        capacity.ShouldContain("accepted full-rate invocation outage is 60 seconds", Case.Sensitive);
        capacity.ShouldContain("10-minute actor/reminder outage", Case.Sensitive);

        string schema = NormalizeWhitespace(adr.GetSection("Persisted Schema Bounds"));
        schema.ShouldContain("RFC 8785 ordering, escaping, and number rules", Case.Sensitive);
        schema.ShouldContain("at most 1,024 UTF-8 bytes", Case.Sensitive);
        schema.ShouldContain("`schemaVersion` is integer `1`", Case.Sensitive);
        schema.ShouldContain("at most six ordinally, lexicographically ordered keys", Case.Sensitive);
        schema.ShouldContain("complete encoded Dapr request exceeds 1 MiB", Case.Sensitive);
        schema.ShouldContain("Case, Result, and Nullable Mapping", Case.Sensitive);
        schema.ShouldContain("Only `tenantMarker` may carry `__rejected__`", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> queryBounds = adr.GetTableRows("Query Parameter Bounds");
        AssertCellCounts(queryBounds, 2, "Query Parameter Bounds");
        queryBounds.Select(static row => row[0]).ShouldBe(operationRows.Select(static value => $"`{value}`").ToArray());
    }

    [Fact]
    public void Adr_C1SourceEventMapping_CoversEveryCurrentLoggerFamilyAndOutcome()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> decisions = adr.GetTableRows("Ratification Decision");
        IReadOnlyList<IReadOnlyList<string>> mappings = adr.GetTableRows("Source Event Mapping");

        AssertCellCounts(decisions, 2, "Ratification Decision");
        GetRow(decisions, "Administrator decision").ShouldBe(
            ["Administrator decision", "ratified 2026-07-18 by Administrator"]);
        GetRow(decisions, "Architecture owner decision").ShouldBe(
            ["Architecture owner decision", "ratified 2026-07-18 by Administrator on behalf of Hexalith.Memories maintainers"]);
        GetRow(decisions, "Runtime persistence gate").ShouldBe(
            ["Runtime persistence gate", "open — both ratifications recorded and structure guards green"]);

        AssertCellCounts(mappings, 6, "Source Event Mapping");
        (string Operation, int SuccessId, int ErrorId)[] expected =
        [
            ("search", 7501, 7511),
            ("ingest", 7502, 7512),
            ("traverse", 7503, 7513),
            ("case-access", 7504, 7514),
            ("delete", 7505, 7515),
            ("tenant-lifecycle", 7506, 7516),
            ("tenant-config", 7507, 7517),
            ("case-member", 7508, 7518),
            ("annotation", 7509, 7519),
        ];
        mappings.Select(static row => row[0]).ShouldBe(expected.Select(static item => $"`{item.Operation}`").ToArray());

        string loggerSource = ReadRepoFile(OperationSourceRelativePath);
        foreach ((string operation, int successId, int errorId) in expected)
        {
            IReadOnlyList<string> row = GetRow(mappings, $"`{operation}`");
            ParseInt(row[1]).ShouldBe(successId);
            row[2].ShouldBe("`Information`");
            ParseInt(row[3]).ShouldBe(errorId);
            row[4].ShouldBe("`Warning`");
            loggerSource.ShouldContain($"EventId = {successId}, Level = LogLevel.Information", Case.Sensitive);
            loggerSource.ShouldContain($"EventId = {errorId}, Level = LogLevel.Warning", Case.Sensitive);
        }

        GetRow(mappings, "`search`")[5].ShouldContain("`partial` -> `partial`", Case.Sensitive);
        mappings.Skip(1).ShouldAllBe(static row => !row[5].Contains("`partial`", StringComparison.Ordinal));
        NormalizeWhitespace(adr.GetSection("Persisted Schema Bounds")).ShouldContain(
            "`outcome` is `ok`, `partial`, or `error`",
            Case.Sensitive);
    }

    [Fact]
    public void Adr_C1TypedStateAndNullableMapping_CoversFrozenLoggerState()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> fields = adr.GetTableRows("Typed State Mapping");
        IReadOnlyList<IReadOnlyList<string>> nullable = adr.GetTableRows("Case, Result, and Nullable Mapping");

        AssertCellCounts(fields, 3, "Typed State Mapping");
        string[] sourceFields = typeof(AccessTelemetryEvent)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToArray();
        sourceFields.ShouldBe(
            fields.Select(static row => row[0].Trim('`')).ToArray(),
            ignoreOrder: true);
        sourceFields.Length.ShouldBe(14);
        fields.ShouldAllBe(static row => !string.IsNullOrWhiteSpace(row[1]) && !string.IsNullOrWhiteSpace(row[2]));

        AssertCellCounts(nullable, 4, "Case, Result, and Nullable Mapping");
        nullable.Select(static row => row[0]).ShouldBe(
            ReadSourceOperationTypes().Select(static operation => $"`{operation}`").ToArray(),
            ignoreOrder: true);
        GetRow(nullable, "`search`")[2].ShouldContain("`caseScope=all-authorized`", Case.Sensitive);
        GetRow(nullable, "`ingest`")[1].ShouldContain("EventStore ingestion adapter", Case.Sensitive);
        GetRow(nullable, "`delete`")[2].ShouldContain("`tenant` permits null", Case.Sensitive);

        string schema = NormalizeWhitespace(adr.GetSection("Persisted Schema Bounds"));
        schema.ShouldContain("Case, Result, and Nullable Mapping", Case.Sensitive);
        schema.ShouldContain("Only `tenantMarker` may carry `__rejected__`", Case.Sensitive);
        schema.ShouldNotContain("`caseMarker` is non-null for every operation except", Case.Sensitive);
        ReadRepoFile(AccessEventSourceRelativePath).ShouldContain("public sealed record AccessTelemetryEvent", Case.Sensitive);
    }

    [Fact]
    public void Adr_C1QueryAndErrorMappings_AreTotalBoundedAndPrivacySafe()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> errors = adr.GetTableRows("Error Code Mapping");
        IReadOnlyList<IReadOnlyList<string>> sources = adr.GetTableRows("Query Parameter Source Mapping");
        IReadOnlyList<IReadOnlyList<string>> bounds = adr.GetTableRows("Query Parameter Bounds");

        AssertCellCounts(errors, 2, "Error Code Mapping");
        errors.Select(static row => row[0]).ShouldBe(
        [
            "`invalid_input`",
            "`not_found`",
            "`forbidden`",
            "`conflict`",
            "`cancelled`",
            "`dependency_unavailable`",
            "`rate_limited`",
            "`internal_dependency_failure`",
            "`internal_failure`",
            "`unknown`",
        ]);
        GetRow(errors, "`unknown`")[1].ShouldContain("any unmatched source code", Case.Sensitive);
        GetRow(errors, "`unknown`")[1].ShouldContain("longer than 128 characters", Case.Sensitive);

        AssertCellCounts(sources, 4, "Query Parameter Source Mapping");
        AssertCellCounts(bounds, 2, "Query Parameter Bounds");
        sources.Select(static row => row[0]).ShouldBe(bounds.Select(static row => row[0]).ToArray());
        sources.ShouldAllBe(static row => row[2].Split(',', StringSplitOptions.RemoveEmptyEntries).Length <= 6);
        string sourceMapping = NormalizeWhitespace(adr.GetSection("Query Parameter Source Mapping"));
        sourceMapping.ShouldContain("Drop the memory-unit ID and URI", Case.Sensitive);
        sourceMapping.ShouldContain("Drop CloudEvent and aggregate identifiers", Case.Sensitive);
        sourceMapping.ShouldContain("drop the `changedFields` array", Case.Sensitive);

        string schema = NormalizeWhitespace(adr.GetSection("Persisted Schema Bounds"));
        schema.ShouldContain("partial and error inputs use Error Code Mapping", Case.Sensitive);
        bounds.Select(static row => row[0]).ShouldBe(
            ReadSourceOperationTypes().Select(static operation => $"`{operation}`").ToArray(),
            ignoreOrder: true);
    }

    [Fact]
    public void Adr_ProductionFactsAndFileOption_KeepAllFileHardGatesExplicit()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        string currentState = NormalizeWhitespace(adr.GetSection("Verified Current State"));

        currentState.ShouldContain("two replicas", Case.Sensitive);
        currentState.ShouldContain("read-only root filesystem", Case.Sensitive);
        currentState.ShouldContain("no OTLP endpoint or access-telemetry backend", Case.Sensitive);
        currentState.ShouldContain("ephemeral temporary storage", Case.Sensitive);
        currentState.ShouldContain("not requirements on future container services", Case.Sensitive);

        IReadOnlyList<IReadOnlyList<string>> options = adr.GetTableRows("Options Evaluated");
        GetRow(options, "Multi-writer behavior")[3].ShouldContain("locking and rotation coordination", Case.Sensitive);
        GetRow(options, "Durability and recovery")[3].ShouldContain("lost on replacement", Case.Sensitive);
        GetRow(options, "Retention, expiry, purge, and clock")[3].ShouldContain("not record TTL", Case.Sensitive);
        GetRow(options, "Hard-gate result")[3].ShouldContain(
            "multi-replica, read-only-root, rescheduling, rotation, and executable-purge gates",
            Case.Sensitive);
    }

    [Fact]
    public void DecisionDocuments_CrossLinksAreExactAndStaleStructuredFileClaimIsAbsent()
    {
        MarkdownContractDocument architecture = ReadDocument(ArchitectureRelativePath);
        MarkdownContractDocument telemetry = ReadDocument(TelemetryRelativePath);

        string security = NormalizeWhitespace(architecture.GetSection("Security Architecture"));
        security.ShouldContain(
            "[ADR 27.1-001](../../docs/dev/adr-27.1-001-access-telemetry-lifecycle.md)",
            Case.Sensitive);
        security.ShouldContain("Dapr-only access-telemetry lifecycle service", Case.Sensitive);
        security.ShouldContain("fixed-ID Dapr actor", Case.Sensitive);
        security.ShouldContain("container-service neutral", Case.Sensitive);
        security.ShouldContain("component-specific physical-reclamation evidence", Case.Sensitive);
        security.ShouldContain("`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains open", Case.Sensitive);
        security.ShouldContain(
            "not tamper-evident, append-only, legally compliant, or certified audit retention",
            Case.Sensitive);
        security.ShouldNotContain("MVP: structured log file", Case.Sensitive);

        // The approved 2026-08-01 ownership correction retained Story 27.3's
        // independent qualification work but removed its ownership of the exact
        // running-target C1 qualification. Guard both sides of that correction so
        // the architecture cannot silently revert to the superseded assignment.
        security.ShouldContain(
            "Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification",
            Case.Sensitive);
        security.ShouldContain(
            "Exact running-target C1 qualification is held without a registered story owner",
            Case.Sensitive);
        security.ShouldNotContain("Story 27.3 now qualifies `PG-ONPREM-1`", Case.Sensitive);
        security.ShouldContain(
            "in-profile zero-loss fault is PostgreSQL pod/process replacement",
            Case.Sensitive);
        security.ShouldContain(
            "node, volume, control-plane, and site loss remain outside profile with no HA claim",
            Case.Sensitive);
        security.ShouldContain("require approved backup/restore RPO/RTO", Case.Sensitive);

        // Verification was transferred to Story 27.4 by the approved 2026-07-20
        // course correction; the architecture record must not reassign it to 27.3.
        security.ShouldContain("Story 27.4 owns deployment-shaped lifecycle verification", Case.Sensitive);
        security.ShouldNotContain(
            "Stories 27.2 and 27.3 are unblocked to implement and verify",
            Case.Sensitive);

        string retention = NormalizeWhitespace(telemetry.GetSection("Retention lifecycle status"));
        retention.ShouldContain("[ADR 27.1-001](adr-27.1-001-access-telemetry-lifecycle.md)", Case.Sensitive);
        retention.ShouldContain("is `Accepted`", Case.Sensitive);
        retention.ShouldContain("Story 27.2 implements its separate portable slice", Case.Sensitive);
        retention.ShouldContain("fixed `AccessTelemetryLifecycleActor/global`", Case.Sensitive);
        retention.ShouldContain("Production overlay keeps the Server provider and lifecycle writes disabled", Case.Sensitive);
        retention.ShouldContain("Story 27.3 must select and pin the exact Production adapter", Case.Sensitive);
        retention.ShouldContain("`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward", Case.Sensitive);

        string routing = NormalizeWhitespace(telemetry.GetSection("Audit log routing recipe"));
        routing.ShouldContain("typed `AccessTelemetryEvent` logger state", Case.Sensitive);
        routing.ShouldNotContain("dedicated JSON file sink", Case.Sensitive);

        string schema = NormalizeWhitespace(telemetry.GetSection("Audit event schema (FR67)"));
        schema.ShouldContain("public logger contract", Case.Sensitive);
        schema.ShouldContain("reads typed `AccessTelemetryEvent` state", Case.Sensitive);
        schema.ShouldContain("only then attempts nonblocking enqueue", Case.Sensitive);
        schema.ShouldContain("not admitted to lifecycle state", Case.Sensitive);

        string logLevelGate = NormalizeWhitespace(telemetry.GetSection("Log-level config gate"));
        logLevelGate.ShouldContain("provider/category scoped, not tenant scoped", Case.Sensitive);
        logLevelGate.ShouldContain("When the lifecycle provider is disabled", Case.Sensitive);
        logLevelGate.ShouldContain("retain `Information`", Case.Sensitive);

        string volume = NormalizeWhitespace(telemetry.GetSection("Access telemetry volume estimates"));
        volume.ShouldContain("all-nine-operation admission envelope", Case.Sensitive);
        volume.ShouldContain("250 events/s cluster ceiling", Case.Sensitive);
        volume.ShouldContain("151,200,000 records", Case.Sensitive);
        volume.ShouldContain("144.20 GiB", Case.Sensitive);
        volume.ShouldContain("selected Dapr component adapter", Case.Sensitive);
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
        privacy.ShouldContain("The Server has no state read or arbitrary-delete authority", Case.Sensitive);
        privacy.ShouldContain("There is no tenant-facing read API", Case.Sensitive);
        privacy.ShouldContain("Raw tenant, user, case, query, subject, source URI", Case.Sensitive);
        privacy.ShouldContain("dynamic writer membership", Case.Sensitive);
        privacy.ShouldContain("membership leases expire after 30 seconds", Case.Sensitive);
        privacy.ShouldContain("cannot create old-key work", Case.Sensitive);
        privacy.ShouldContain("maximum five-minute retry age expires", Case.Sensitive);
        privacy.ShouldContain("at least 7 days, 15 minutes, and 1 second", Case.Sensitive);
        privacy.ShouldContain("durable reminders", Case.Sensitive);
        privacy.ShouldContain("Story 20.2 tenant-denial guards", Case.Sensitive);
        privacy.ShouldContain("Story 24.3 verifier guards", Case.Sensitive);

        string observability = NormalizeWhitespace(adr.GetSection("Observability"));
        string[] states = ["accepted", "rejected", "enqueued", "persisted", "retried", "failed", "dropped", "expired", "purged"];
        foreach (string state in states)
        {
            observability.ShouldContain($"**{state}**", Case.Sensitive);
        }

        observability.ShouldContain("`NoData`", Case.Sensitive);
        observability.ShouldContain("`Unhealthy` takes precedence over `Degraded`", Case.Sensitive);
        observability.ShouldContain("unavailable or unvalidated lifecycle path", Case.Sensitive);
        observability.ShouldContain("physical_reclamation", Case.Sensitive);
        observability.ShouldContain(
            "Metric labels must never contain tenant, user, case, memory-unit, query, subject, source, trace, span, record, service-instance, process, or component backend identifiers.",
            Case.Sensitive);

        string implementationHandoff = NormalizeWhitespace(adr.GetSection("Story 27.2 Implementation Handoff"));
        implementationHandoff.ShouldContain("Do not add a backend SDK or orchestrator API", Case.Sensitive);
        implementationHandoff.ShouldContain("`AccessTelemetryLifecycleActor/global`", Case.Sensitive);
        implementationHandoff.ShouldContain("three-source/uncertainty rule", Case.Sensitive);
        implementationHandoff.ShouldContain("Alpha components are allowed", Case.Sensitive);
        implementationHandoff.ShouldContain("named Story 20.2/24.3 privacy negatives", Case.Sensitive);

        string verificationHandoff = NormalizeWhitespace(adr.GetSection("Stories 27.3 and 27.4 Verification and Operations Handoff"));
        verificationHandoff.ShouldContain("Story 27.3 owns only exact Production-adapter qualification", Case.Sensitive);
        verificationHandoff.ShouldContain("Story 27.4 remains backlog", Case.Sensitive);
        verificationHandoff.ShouldContain("no direct backend dependency", Case.Sensitive);
        verificationHandoff.ShouldContain("physical reclamation", Case.Sensitive);
        verificationHandoff.ShouldContain("business readiness must stay available", Case.Sensitive);
        verificationHandoff.ShouldContain("container-service-neutral lifecycle runbook", Case.Sensitive);
        verificationHandoff.ShouldContain("A41 deferred entry and action close-out", Case.Sensitive);

        NormalizeWhitespace(telemetry.GetSection("Retention lifecycle status")).ShouldContain(
            "`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward and its action remains open",
            Case.Sensitive);

        ContractDocumentGuard.FindLeakedToolCallMarkup(adrMarkdown).ShouldBeEmpty();
        ContractDocumentGuard.FindLeakedToolCallMarkup(architectureMarkdown).ShouldBeEmpty();
        ContractDocumentGuard.FindLeakedToolCallMarkup(telemetryMarkdown).ShouldBeEmpty();
    }

    [Fact]
    public void Adr_ProductionAdapterQualification_PinsExactProfileAndFailClosedApprovalGate()
    {
        MarkdownContractDocument adr = ReadDocument(AdrRelativePath);
        IReadOnlyList<IReadOnlyList<string>> profile = adr.GetTableRows("Exact qualification profile");
        IReadOnlyList<IReadOnlyList<string>> images = adr.GetTableRows("Required immutable image set");

        AssertCellCounts(profile, 2, "Exact qualification profile");
        GetRow(profile, "Profile ID")[1].ShouldBe(
            "`postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1`");
        GetRow(profile, "Dapr component")[1].ShouldContain("`type: state.postgresql`", Case.Sensitive);
        GetRow(profile, "Backend")[1].ShouldContain("PostgreSQL 18.4", Case.Sensitive);
        GetRow(profile, "Kubernetes target")[1].ShouldContain("`jpiquot@local`", Case.Sensitive);
        GetRow(profile, "Availability")[1].ShouldContain("no node, disk, zone, control-plane, or site HA claim", Case.Sensitive);
        GetRow(profile, "Storage")[1].ShouldContain("429,496,729,600 bytes", Case.Sensitive);
        GetRow(profile, "Dapr control plane")[1].ShouldContain("co-located on `node1`", Case.Sensitive);
        GetRow(profile, "Physical reclamation")[1].ShouldContain("within 24 hours", Case.Sensitive);

        AssertCellCounts(images, 2, "Required immutable image set");
        GetRow(images, "Dapr sidecar")[1].ShouldBe(
            "`ghcr.io/dapr/daprd@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8`");
        GetRow(images, "PostgreSQL")[1].ShouldContain(
            "sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a",
            Case.Sensitive);
        GetRow(images, "Lifecycle service")[1].ShouldContain("**Missing and blocking:**", Case.Sensitive);
        GetRow(images, "Clock service")[1].ShouldContain("**Missing and blocking:**", Case.Sensitive);

        string qualification = NormalizeWhitespace(adr.GetSection("Production Adapter Qualification — PG-ONPREM-1"));
        qualification.ShouldContain("Maximum steady-state admission (70%) | 300,647,710,720", Case.Sensitive);
        qualification.ShouldContain("Reclamation critical boundary (80%) | 343,597,383,680", Case.Sensitive);
        qualification.ShouldContain("Lifecycle Unhealthy boundary (90%) | 386,547,056,640", Case.Sensitive);
        qualification.ShouldContain("forced loss of the PostgreSQL container/process", Case.Sensitive);
        qualification.ShouldContain("must never be described as node-, disk-, zone-, or site-redundant", Case.Sensitive);
        qualification.ShouldContain("named backup destination", Case.Sensitive);
        qualification.ShouldContain("`queryIndexes` is intentionally absent", Case.Sensitive);
        qualification.ShouldContain("neither may be inferred from the other", Case.Sensitive);
        qualification.ShouldContain("is rejected for C1", Case.Sensitive);
        qualification.ShouldContain("remain Dapr-only", Case.Sensitive);
    }

    [Fact]
    public void Adr_Story27_4Handoff_ProjectsToPendingEvidenceAndRuntimeMetricContracts()
    {
        string handoff = NormalizeWhitespace(
            ReadDocument(AdrRelativePath).GetSection("Stories 27.3 and 27.4 Verification and Operations Handoff"));
        handoff.ShouldContain("container-service-neutral lifecycle runbook", Case.Sensitive);
        handoff.ShouldContain("physical reclamation", Case.Sensitive);
        handoff.ShouldContain("business readiness must stay available", Case.Sensitive);

        string runbook = ReadRepoFile(LifecycleRunbookRelativePath);
        runbook.ShouldContain("A41 is open until remote publish verification succeeds", Case.Sensitive);
        runbook.ShouldContain("Production lifecycle writes remain disabled", Case.Sensitive);

        string evidence = ReadRepoFile(EvidenceMatrixRelativePath);
        evidence.ShouldContain("## Canonical C0-C6 matrix", Case.Sensitive);
        evidence.ShouldContain("`operator-pending`", Case.Sensitive);
        evidence.ShouldContain("Only authentic external packets in state `passed` can satisfy C2-C6", Case.Sensitive);

        string metricContract = ReadRepoFile(LifecycleMetricContractRelativePath);
        foreach (string label in new[] { "state", "reason", "outcome" })
        {
            metricContract.ShouldContain($"\"{label}\"", Case.Sensitive);
        }

        foreach (string forbidden in new[] { "tenant_id", "user", "case_id", "memory_unit_id" })
        {
            metricContract.ShouldNotContain($"\"{forbidden}\"", Case.Sensitive);
        }
    }

    private static void AssertCellCounts(
        IReadOnlyList<IReadOnlyList<string>> rows,
        int expectedCount,
        string tableName)
        => rows.ShouldAllBe(row => row.Count == expectedCount, $"Every {tableName} row must contain {expectedCount} cells.");

    private static IReadOnlyList<string> GetRow(
        IReadOnlyList<IReadOnlyList<string>> rows,
        string firstCell)
    {
        IReadOnlyList<IReadOnlyList<string>> matches = rows
            .Where(row => row.Count > 0 && string.Equals(row[0], firstCell, StringComparison.Ordinal))
            .ToArray();
        matches.Count.ShouldBe(1, $"Expected exactly one row named '{firstCell}'.");
        return matches[0];
    }

    private static decimal ParseDecimal(string value)
        => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static int ParseInt(string value)
        => int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);

    private static IReadOnlyList<string> ReadSourceOperationTypes()
        => OperationConstantRegex()
            .Matches(ReadRepoFile(OperationSourceRelativePath))
            .Select(static match => match.Groups[1].Value)
            .ToArray();

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
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        int[] sizes = Enumerable.Range(0, 100)
            .Select(index =>
            {
                bool isError = index >= 90;
                var record = new SortedDictionary<string, object?>(StringComparer.Ordinal)
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

                return JsonSerializer.SerializeToUtf8Bytes(record, options).Length;
            })
            .Order()
            .ToArray();

        return (sizes.Sum() / 100m, sizes[94]);
    }

    private static SortedDictionary<string, object?> CreateBoundedQueryParams(string operation)
        => operation switch
        {
            "search" => new(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "single",
                ["explain"] = true,
                ["queryLengthBucket"] = "257-1024",
                ["subjectPresent"] = true,
                ["weightProfile"] = "request-override",
            },
            "ingest" => new(StringComparer.Ordinal)
            {
                ["caseScope"] = "case",
                ["contentKind"] = "document",
                ["contentLengthBucket"] = "1-10MiB",
                ["eventOutcome"] = "accepted",
                ["sourceKind"] = "url",
            },
            "traverse" => new(StringComparer.Ordinal)
            {
                ["caseScope"] = "single",
                ["depthBucket"] = "5",
                ["direction"] = "out",
                ["edgeTypeCount"] = 3,
                ["includeGaps"] = false,
            },
            "case-access" => new(StringComparer.Ordinal)
            {
                ["accessKind"] = "source-uri",
                ["projection"] = "detail",
                ["sourceKind"] = "url",
            },
            "delete" => new(StringComparer.Ordinal)
            {
                ["cascade"] = true,
                ["targetKind"] = "case",
            },
            "tenant-lifecycle" => new(StringComparer.Ordinal)
            {
                ["action"] = "deletion-status",
                ["workflowState"] = "completed",
            },
            "tenant-config" => new(StringComparer.Ordinal)
            {
                ["action"] = "update",
                ["changedFieldCountBucket"] = "4-8",
                ["configKind"] = "embedding",
                ["forceReindex"] = true,
            },
            "case-member" => new(StringComparer.Ordinal)
            {
                ["action"] = "add",
                ["role"] = "unknown",
            },
            "annotation" => new(StringComparer.Ordinal)
            {
                ["action"] = "create",
                ["annotationKind"] = "unknown",
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

        throw new DirectoryNotFoundException(
            $"Could not find Hexalith.Memories.slnx within eight parent directories of {AppContext.BaseDirectory}.");
    }

    [GeneratedRegex("""public const string Operation\w+ = "([^"]+)";""", RegexOptions.CultureInvariant)]
    private static partial Regex OperationConstantRegex();
}
