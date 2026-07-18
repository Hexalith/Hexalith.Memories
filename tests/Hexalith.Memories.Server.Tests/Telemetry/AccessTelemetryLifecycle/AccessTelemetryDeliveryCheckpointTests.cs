// <copyright file="AccessTelemetryDeliveryCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using AccessTelemetryEvent = Hexalith.Memories.Contracts.V1.AccessTelemetryEvent;

/// <summary>Story 27.2 C2 checkpoint for Server admission and delivery.</summary>
public sealed class AccessTelemetryDeliveryCheckpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Sanitizer_TransformsRawFieldsIntoBoundedMarkersAndCatalogs()
    {
        AccessTelemetrySanitizer sanitizer = CreateSanitizer();
        AccessTelemetryEvent source = CreateSearchEvent();

        bool accepted = sanitizer.TrySanitize(LogLevel.Information, new EventId(7501), source, out AccessTelemetryRecord? record, out AccessTelemetryReason reason);

        accepted.ShouldBeTrue();
        reason.ShouldBe(AccessTelemetryReason.None);
        record.ShouldNotBeNull();
        record.TenantMarker.ShouldMatch("^[0-9a-f]{64}$");
        record.UserMarker.ShouldNotBeNull();
        record.UserMarker.ShouldMatch("^[0-9a-f]{64}$");
        record.CaseMarker.ShouldBeNull();
        record.QueryParams["axis"].ShouldBe("hybrid");
        record.QueryParams["caseScope"].ShouldBe("all-authorized");
        record.QueryParams["queryLengthBucket"].ShouldBe("33-128");
        record.QueryParams["subjectPresent"].ShouldBe(true);
        string canonical = Encoding.UTF8.GetString(AccessTelemetryCanonicalizer.CanonicalizeRecord(record));
        canonical.ShouldNotContain("tenant-a", Case.Sensitive);
        canonical.ShouldNotContain("alice@example.test", Case.Sensitive);
        canonical.ShouldNotContain("find the quarterly architecture decisions", Case.Sensitive);
        canonical.ShouldNotContain("customer strategy", Case.Sensitive);
    }

    [Fact]
    public void Sanitizer_RejectedTenantUsesOnlyTenantSentinelAndNullsCorrelationMarkers()
    {
        AccessTelemetryEvent source = CreateSearchEvent() with
        {
            TenantId = "__rejected__",
            CaseId = "case-secret",
            TraceId = new string('a', 32),
            SpanId = new string('b', 16),
        };

        bool accepted = CreateSanitizer().TrySanitize(
            LogLevel.Information,
            new EventId(7501),
            source,
            out AccessTelemetryRecord? record,
            out _);

        accepted.ShouldBeTrue();
        record.ShouldNotBeNull();
        record.TenantMarker.ShouldBe("__rejected__");
        record.UserMarker.ShouldBeNull();
        record.CaseMarker.ShouldBeNull();
        record.TraceId.ShouldBeNull();
        record.SpanId.ShouldBeNull();
        record.QueryParams["caseScope"].ShouldBe("rejected-or-unknown");
    }

    [Fact]
    public void Provider_CapturesTypedStateOnlyForExactCategorySeverityAndTuple()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, CreateSanitizer());
        ILogger accepted = provider.CreateLogger(typeof(AccessTelemetryCategory).FullName!);
        ILogger rejectedCategory = provider.CreateLogger("Other.Category");

        accepted.Log(LogLevel.Information, new EventId(7501), CreateSearchEvent(), null, static (_, _) => "ignored");
        accepted.Log(LogLevel.Debug, new EventId(7501), CreateSearchEvent(), null, static (_, _) => "ignored");
        accepted.Log(LogLevel.Information, new EventId(7502), CreateSearchEvent(), null, static (_, _) => "ignored");
        rejectedCategory.Log(LogLevel.Information, new EventId(7501), CreateSearchEvent(), null, static (_, _) => "ignored");

        queue.Count.ShouldBe(1);
    }

    [Fact]
    public void Provider_ExtractsAccessTelemetryEventFromStructuredLoggerStateByValueType()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, CreateSanitizer());
        ILogger logger = provider.CreateLogger(typeof(AccessTelemetryCategory).FullName!);
        IReadOnlyList<KeyValuePair<string, object?>> state =
        [
            new("Unrelated", "value"),
            new("AuditEvent", CreateSearchEvent()),
            new("{OriginalFormat}", "Search access {@AuditEvent}"),
        ];

        logger.Log(LogLevel.Information, new EventId(7501), state, null, static (_, _) => "must-not-be-parsed");

        queue.Count.ShouldBe(1);
    }

    [Fact]
    public void Queue_DropsNewestAtExactRecordAndByteBounds()
    {
        AccessTelemetryRecord record = Sanitize(CreateSearchEvent());
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length;
        var queue = new BoundedAccessTelemetryQueue(1, bytes);

        queue.TryEnqueue(record, out AccessTelemetryReason firstReason).ShouldBeTrue();
        queue.TryEnqueue(record, out AccessTelemetryReason secondReason).ShouldBeFalse();

        firstReason.ShouldBe(AccessTelemetryReason.None);
        secondReason.ShouldBe(AccessTelemetryReason.QueueFull);
        queue.Count.ShouldBe(1);
        queue.ByteCount.ShouldBe(bytes);
    }

    [Fact]
    public void Provider_InvalidStateNeverEscapesIntoBusinessLogging()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, CreateSanitizer());
        ILogger logger = provider.CreateLogger(typeof(AccessTelemetryCategory).FullName!);
        AccessTelemetryEvent invalid = CreateSearchEvent() with { QueryParams = new Dictionary<string, object?> { ["Query"] = "raw" } };

        Should.NotThrow(() => logger.Log(
            LogLevel.Information,
            new EventId(7501),
            invalid,
            new InvalidOperationException("secret exception"),
            static (_, _) => throw new InvalidOperationException("formatter must not run")));
        queue.Count.ShouldBe(0);
    }

    [Fact]
    public void Provider_FailsClosedUntilMarkerSecretBootstrapPublishesSanitizer()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        var sanitizerAccessor = new AccessTelemetrySanitizerAccessor();
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, sanitizerAccessor);
        ILogger logger = provider.CreateLogger(typeof(AccessTelemetryCategory).FullName!);

        logger.Log(LogLevel.Information, new EventId(7501), CreateSearchEvent(), null, static (_, _) => "ignored");
        queue.Count.ShouldBe(0);

        sanitizerAccessor.Publish(CreateSanitizer());
        logger.Log(LogLevel.Information, new EventId(7501), CreateSearchEvent(), null, static (_, _) => "ignored");
        queue.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Worker_SendsBoundedBatchAndRemovesOnlyAcknowledgedRecords()
    {
        var queue = new BoundedAccessTelemetryQueue(300, 1024 * 1024);
        AccessTelemetryRecord record = Sanitize(CreateSearchEvent());
        for (int index = 0; index < 257; index++)
        {
            AccessTelemetryRecord unique = record with
            {
                RecordId = new MonotonicRecordIdGenerator().NewId(),
            };
            unique = unique with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(unique) };
            queue.TryEnqueue(unique, out _).ShouldBeTrue();
        }

        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(new AccessTelemetryWriteBatchResponse { Accepted = 256, Rejected = 0, Reason = AccessTelemetryReason.None });
        var worker = new AccessTelemetryDeliveryWorker(queue, client, new FakeTimeProvider(Now));

        await worker.DrainOnceAsync(CancellationToken.None);

        await client.Received(1).SendAsync(
            Arg.Is<IReadOnlyList<AccessTelemetryRecord>>(records => records.Count == 256),
            Arg.Any<CancellationToken>());
        queue.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HeartbeatWorker_UsesTenSecondCadenceAndThirtySecondLeaseWithoutMetricIdentity()
    {
        IAccessTelemetryHeartbeatClient client = Substitute.For<IAccessTelemetryHeartbeatClient>();
        var identity = new AccessTelemetryWriterIdentity(new MonotonicRecordIdGenerator());
        var options = new AccessTelemetryOptions
        {
            DeploymentId = "deployment-a",
            MarkerKeyGeneration = "mk-2026a",
        };
        var worker = new AccessTelemetryHeartbeatWorker(client, options, identity, new FakeTimeProvider(Now));

        await worker.SendOnceAsync(CancellationToken.None);

        await client.Received(1).SendAsync(
            Arg.Is<WriterHeartbeat>(heartbeat =>
                heartbeat.DeploymentId == "deployment-a" &&
                heartbeat.ServiceInstanceId == identity.ServiceInstanceId &&
                heartbeat.ProcessEpoch == identity.ProcessEpoch &&
                heartbeat.MarkerKeyGeneration == "mk-2026a" &&
                heartbeat.OldKeyQueueCount == 0 &&
                heartbeat.LeaseExpiresAtUnixMilliseconds == Now.AddSeconds(30).ToUnixTimeMilliseconds()),
            Arg.Any<CancellationToken>());
        AccessTelemetryHeartbeatWorker.HeartbeatInterval.ShouldBe(TimeSpan.FromSeconds(10));
    }

    private static AccessTelemetrySanitizer CreateSanitizer()
        => new(
            RandomNumberGenerator.GetBytes(32),
            "mk-2026a",
            new FakeTimeProvider(Now),
            new MonotonicRecordIdGenerator(),
            TimeSpan.FromHours(24));

    private static AccessTelemetryRecord Sanitize(AccessTelemetryEvent source)
    {
        CreateSanitizer().TrySanitize(
            LogLevel.Information,
            new EventId(7501),
            source,
            out AccessTelemetryRecord? record,
            out AccessTelemetryReason reason).ShouldBeTrue(reason.ToString());
        return record!;
    }

    private static AccessTelemetryEvent CreateSearchEvent()
        => new()
        {
            EventId = 7501,
            Timestamp = "2026-07-18T09:59:59.000+00:00",
            TenantId = "tenant-a",
            OperationType = "search",
            CaseId = null,
            User = "alice@example.test",
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["query"] = "find the quarterly architecture decisions",
                ["subject"] = "customer strategy",
                ["explain"] = true,
            },
            ResultCount = 4,
            DurationMs = 12,
            Outcome = "ok",
            ErrorCode = null,
        };
}
