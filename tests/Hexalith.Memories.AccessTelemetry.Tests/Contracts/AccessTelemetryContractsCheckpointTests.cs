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
}
