// <copyright file="AccessTelemetryContractsCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Contracts;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.AccessTelemetry.Contracts;

using Shouldly;

/// <summary>Story 27.2 Task 2 checkpoint for bounded internal contracts.</summary>
public sealed class AccessTelemetryContractsCheckpointTests
{
    [Fact]
    public void Options_EncodeAcceptedDefaultsAndLimitsOnce()
    {
        AccessTelemetryOptions.DefaultRetention.ShouldBe(TimeSpan.FromHours(24));
        AccessTelemetryOptions.MinimumRetention.ShouldBe(TimeSpan.FromHours(1));
        AccessTelemetryOptions.MaximumRetention.ShouldBe(TimeSpan.FromDays(7));
        AccessTelemetryOptions.MaximumRecordBytes.ShouldBe(1024);
        AccessTelemetryOptions.MaximumBatchRecords.ShouldBe(256);
        AccessTelemetryOptions.MaximumBatchBytes.ShouldBe(1024 * 1024);
        AccessTelemetryOptions.MaximumQueueRecords.ShouldBe(8192);
        AccessTelemetryOptions.MaximumQueueBytes.ShouldBe(64 * 1024 * 1024);
        AccessTelemetryOptions.ShutdownFlushTimeout.ShouldBe(TimeSpan.FromSeconds(5));
        AccessTelemetryOptions.MaximumRetryAge.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(3599)]
    [InlineData(604801)]
    public void Validate_ProductionRetentionOutsideExactDaprBounds_FailsClosed(int? seconds)
    {
        AccessTelemetryOptions options = ValidOptions() with
        {
            Retention = seconds is null ? null : TimeSpan.FromSeconds(seconds.Value),
            RetentionSource = RetentionConfigurationSource.DaprConfiguration,
        };

        AccessTelemetryOptionsValidationResult result = AccessTelemetryOptionsValidator.Validate(options, "Production");

        result.IsValid.ShouldBeFalse();
        result.AllowsLifecycleWrites.ShouldBeFalse();
        result.StopsBusinessReadiness.ShouldBeFalse();
        result.Reason.ShouldBe(AccessTelemetryReason.ConfigurationInvalid);
    }

    [Fact]
    public void Validate_DevelopmentMissingRetention_UsesBoundedDefault()
    {
        AccessTelemetryOptions options = ValidOptions() with
        {
            Retention = null,
            RetentionSource = RetentionConfigurationSource.DevelopmentDefault,
        };

        AccessTelemetryOptionsValidationResult result = AccessTelemetryOptionsValidator.Validate(options, "Development");

        result.IsValid.ShouldBeTrue();
        result.EffectiveRetention.ShouldBe(TimeSpan.FromHours(24));
        result.AllowsLifecycleWrites.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ProductionRequiresDaprSourceAndExactIdentities()
    {
        AccessTelemetryOptions options = ValidOptions() with
        {
            RetentionSource = RetentionConfigurationSource.DevelopmentDefault,
            LifecycleAppId = "wrong",
        };

        AccessTelemetryOptionsValidationResult result = AccessTelemetryOptionsValidator.Validate(options, "Production");

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(static value => value.Contains("Dapr configuration", StringComparison.Ordinal));
        result.Errors.ShouldContain(static value => value.Contains("memories-access-telemetry", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_UndefinedRetentionSource_FailsClosed()
    {
        AccessTelemetryOptionsValidationResult result = AccessTelemetryOptionsValidator.Validate(
            ValidOptions() with { RetentionSource = (RetentionConfigurationSource)999 },
            "Development");

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(AccessTelemetryReason.ConfigurationInvalid);
        result.AllowsLifecycleWrites.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("reporter:latest")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Validate_ReporterImageDigestMustBeExactLowercaseSha256(string digest)
    {
        AccessTelemetryOptionsValidationResult result = AccessTelemetryOptionsValidator.Validate(
            ValidOptions() with { PhysicalReclamationReporterImageDigest = digest },
            "Development");

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(static value => value.Contains(
            nameof(AccessTelemetryOptions.PhysicalReclamationReporterImageDigest),
            StringComparison.Ordinal));
    }

    [Fact]
    public void Canonicalizer_ProducesDeterministicExplicitNullJsonAndEnvelopeHash()
    {
        AccessTelemetryRecord record = CreateRecord();

        byte[] first = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
        byte[] second = AccessTelemetryCanonicalizer.CanonicalizeRecord(record with
        {
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["weightProfile"] = "configured",
                ["axis"] = "hybrid",
                ["caseScope"] = "all-authorized",
                ["explain"] = false,
                ["queryLengthBucket"] = "33-128",
                ["subjectPresent"] = true,
            },
        });

        first.ShouldBe(second);
        string json = Encoding.UTF8.GetString(first);
        json.ShouldContain("\"caseMarker\":null", Case.Sensitive);
        json.ShouldContain("\"errorCode\":null", Case.Sensitive);
        json.IndexOf("\"axis\"", StringComparison.Ordinal).ShouldBeLessThan(
            json.IndexOf("\"weightProfile\"", StringComparison.Ordinal));
        first.Length.ShouldBeLessThanOrEqualTo(AccessTelemetryOptions.MaximumRecordBytes);

        byte[] envelope = AccessTelemetryCanonicalizer.CanonicalizeEnvelope(record);
        Convert.ToHexString(SHA256.HashData(envelope)).ToLowerInvariant().ShouldBe(record.EnvelopeHash);
        AccessTelemetryRecord parsed = AccessTelemetryCanonicalizer.ParseCanonicalRecord(first);
        AccessTelemetryCanonicalizer.CanonicalizeRecord(parsed).ShouldBe(first);
        parsed.QueryParams.ShouldBe(record.QueryParams);
    }

    [Theory]
    [InlineData("search", 7501, "ok", null, 10, false, null)]
    [InlineData("search", 7501, "partial", "dependency_unavailable", 5, false, null)]
    [InlineData("search", 7511, "error", "unknown", null, false, null)]
    [InlineData("ingest", 7502, "ok", null, null, false, null)]
    [InlineData("ingest", 7512, "error", "unknown", null, false, null)]
    [InlineData("traverse", 7503, "ok", null, 3, false, null)]
    [InlineData("traverse", 7513, "error", "unknown", null, false, null)]
    [InlineData("case-access", 7504, "ok", null, 1, true, null)]
    [InlineData("case-access", 7514, "error", "unknown", null, true, null)]
    [InlineData("delete", 7505, "ok", null, null, true, "memory-unit")]
    [InlineData("delete", 7515, "error", "unknown", null, false, "tenant")]
    [InlineData("tenant-lifecycle", 7506, "ok", null, null, false, null)]
    [InlineData("tenant-lifecycle", 7516, "error", "unknown", null, false, null)]
    [InlineData("tenant-config", 7507, "ok", null, null, false, null)]
    [InlineData("tenant-config", 7517, "error", "unknown", null, false, null)]
    [InlineData("case-member", 7508, "ok", null, null, true, null)]
    [InlineData("case-member", 7518, "error", "unknown", null, true, null)]
    [InlineData("annotation", 7509, "ok", null, null, true, null)]
    [InlineData("annotation", 7519, "error", "unknown", null, true, null)]
    public void Canonicalizer_SupportedOperationTuple_RoundTrips(
        string operationType,
        int eventId,
        string outcome,
        string? errorCode,
        int? resultCount,
        bool hasCaseMarker,
        string? deleteTargetKind)
    {
        AccessTelemetryRecord unsigned = CreateRecord() with
        {
            EventId = eventId,
            OperationType = operationType,
            Outcome = outcome,
            ErrorCode = errorCode,
            ResultCount = resultCount,
            CaseMarker = hasCaseMarker ? new string('e', 64) : null,
            QueryParams = CreateQueryParams(operationType, deleteTargetKind),
            EnvelopeHash = string.Empty,
        };
        AccessTelemetryRecord record = unsigned with
        {
            EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(unsigned),
        };

        byte[] canonical = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
        AccessTelemetryRecord parsed = AccessTelemetryCanonicalizer.ParseCanonicalRecord(canonical);

        AccessTelemetryCanonicalizer.CanonicalizeRecord(parsed).ShouldBe(canonical);
        parsed.OperationType.ShouldBe(operationType);
        parsed.EventId.ShouldBe(eventId);
        parsed.Outcome.ShouldBe(outcome);
        parsed.ErrorCode.ShouldBe(errorCode);
        parsed.ResultCount.ShouldBe(resultCount);
        parsed.CaseMarker.ShouldBe(record.CaseMarker);
        parsed.QueryParams.ShouldBe(record.QueryParams);
    }

    [Fact]
    public void Canonicalizer_RejectsUnknownDuplicateWrongCaseAndNoncanonicalFields()
    {
        byte[] valid = AccessTelemetryCanonicalizer.CanonicalizeRecord(CreateRecord());
        string json = Encoding.UTF8.GetString(valid);

        Should.Throw<AccessTelemetryContractException>(() =>
            AccessTelemetryCanonicalizer.ParseCanonicalRecord(Encoding.UTF8.GetBytes(json.Replace(
                "\"schemaVersion\":1",
                "\"SchemaVersion\":1",
                StringComparison.Ordinal))));
        Should.Throw<AccessTelemetryContractException>(() =>
            AccessTelemetryCanonicalizer.ParseCanonicalRecord(Encoding.UTF8.GetBytes(json.Replace(
                "\"schemaVersion\":1",
                "\"schemaVersion\":1,\"unknown\":true",
                StringComparison.Ordinal))));
        Should.Throw<AccessTelemetryContractException>(() =>
            AccessTelemetryCanonicalizer.ParseCanonicalRecord(Encoding.UTF8.GetBytes(json.Replace(
                "\"schemaVersion\":1",
                "\"schemaVersion\":1,\"schemaVersion\":1",
                StringComparison.Ordinal))));
        Should.Throw<AccessTelemetryContractException>(() =>
            AccessTelemetryCanonicalizer.ParseCanonicalRecord(Encoding.UTF8.GetBytes(json.Replace(
                "{\"acceptedAtUtc\"",
                "{ \"acceptedAtUtc\"",
                StringComparison.Ordinal))));
    }

    [Fact]
    public void Canonicalizer_EnforcesExactTupleCatalogCorrelationAndNullabilityRules()
    {
        AccessTelemetryRecord valid = CreateRecord();

        Should.Throw<AccessTelemetryContractException>(() => AccessTelemetryCanonicalizer.CanonicalizeRecord(
            valid with { EventId = 7502 }));
        Should.Throw<AccessTelemetryContractException>(() => AccessTelemetryCanonicalizer.CanonicalizeRecord(
            valid with { Outcome = "partial", ErrorCode = "unknown" }));
        Should.Throw<AccessTelemetryContractException>(() => AccessTelemetryCanonicalizer.CanonicalizeRecord(
            valid with { TraceId = null }));
        Should.Throw<AccessTelemetryContractException>(() => AccessTelemetryCanonicalizer.CanonicalizeRecord(
            valid with { ResultCount = null }));
        Should.Throw<AccessTelemetryContractException>(() => AccessTelemetryCanonicalizer.CanonicalizeRecord(
            valid with
            {
                QueryParams = new Dictionary<string, object?>(valid.QueryParams, StringComparer.Ordinal)
                {
                    ["axis"] = "invented-axis",
                },
            }));
        Should.Throw<AccessTelemetryContractException>(() => AccessTelemetryCanonicalizer.CanonicalizeRecord(
            valid with
            {
                TenantMarker = "__rejected__",
                UserMarker = new string('b', 64),
            }));
    }

    [Fact]
    public void RecordIdGenerator_IsUniqueUppercaseAndMonotonic()
    {
        var generator = new MonotonicRecordIdGenerator();

        string[] ids = Enumerable.Range(0, 100).Select(_ => generator.NewId()).ToArray();

        ids.Distinct(StringComparer.Ordinal).Count().ShouldBe(ids.Length);
        ids.ShouldBe(ids.Order(StringComparer.Ordinal).ToArray());
        ids.ShouldAllBe(static value => value.Length == 26 && value == value.ToUpperInvariant());
    }

    private static AccessTelemetryOptions ValidOptions()
        => new()
        {
            Enabled = true,
            Retention = TimeSpan.FromHours(24),
            RetentionSource = RetentionConfigurationSource.DaprConfiguration,
            DeploymentId = "development",
            ComponentProfileHash = new string('a', 64),
            ConfigurationEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            MarkerKeyReference = "access-telemetry-marker",
            MarkerKeyGeneration = "mk-2026a",
            AttestationVerificationKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CapacityEvidenceId = "development-capacity",
            PhysicalReclamationEvidenceId = "pending-story-27-3",
            PhysicalReclamationReporterImageDigest = new string('d', 64),
        };

    private static AccessTelemetryRecord CreateRecord()
    {
        AccessTelemetryRecord record = new()
        {
            SchemaVersion = 1,
            RecordId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            EventId = 7501,
            EmittedAtUtc = "2026-07-18T08:00:00.000Z",
            AcceptedAtUtc = "2026-07-18T08:00:00.100Z",
            ExpiresAtUtc = "2026-07-19T08:00:00.000Z",
            TenantMarker = new string('a', 64),
            UserMarker = new string('b', 64),
            CaseMarker = null,
            MarkerKeyId = "mk-2026a",
            OperationType = "search",
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "all-authorized",
                ["explain"] = false,
                ["queryLengthBucket"] = "33-128",
                ["subjectPresent"] = true,
                ["weightProfile"] = "configured",
            },
            ResultCount = 10,
            DurationMs = 42,
            Outcome = "ok",
            ErrorCode = null,
            TraceId = new string('c', 32),
            SpanId = new string('d', 16),
            EnvelopeHash = string.Empty,
        };
        return record with
        {
            EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(record),
        };
    }

    private static IReadOnlyDictionary<string, object?> CreateQueryParams(
        string operationType,
        string? deleteTargetKind)
        => operationType switch
        {
            "search" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "all-authorized",
                ["explain"] = false,
                ["queryLengthBucket"] = "33-128",
                ["subjectPresent"] = true,
                ["weightProfile"] = "configured",
            },
            "ingest" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["caseScope"] = "case",
                ["contentKind"] = "document",
                ["contentLengthBucket"] = "1-64KiB",
                ["eventOutcome"] = "accepted",
                ["sourceKind"] = "file",
            },
            "traverse" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["caseScope"] = "single",
                ["depthBucket"] = "2",
                ["direction"] = "out",
                ["edgeTypeCount"] = 2,
                ["includeGaps"] = false,
            },
            "case-access" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["accessKind"] = "memory-unit-id",
                ["projection"] = "detail",
                ["sourceKind"] = "file",
            },
            "delete" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cascade"] = true,
                ["targetKind"] = deleteTargetKind,
            },
            "tenant-lifecycle" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["action"] = "provision",
                ["workflowState"] = "completed",
            },
            "tenant-config" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["action"] = "update",
                ["changedFieldCountBucket"] = "2-3",
                ["configKind"] = "embedding",
                ["forceReindex"] = false,
            },
            "case-member" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["action"] = "add",
                ["role"] = "unknown",
            },
            "annotation" => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["action"] = "create",
                ["annotationKind"] = "unknown",
            },
            _ => throw new InvalidOperationException($"Unsupported operation type '{operationType}'."),
        };
}
