// <copyright file="AccessTelemetryDeliveryCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
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

    public static IEnumerable<object[]> OperationEvents()
    {
        yield return [LogLevel.Information, new EventId(7501), CreateSearchEvent()];
        yield return [LogLevel.Information, new EventId(7502), CreateEvent(7502, "ingest", null, null, new Dictionary<string, object?>
        {
            ["bytes"] = 42L,
            ["contentType"] = "text/plain",
            ["eventOutcome"] = "accepted",
            ["sourceType"] = "file",
        })];
        yield return [LogLevel.Information, new EventId(7503), CreateEvent(7503, "traverse", null, 2, new Dictionary<string, object?>
        {
            ["depth"] = 3,
            ["edgeTypes"] = "references,depends-on",
            ["startNodeId"] = "raw-node-id",
            ["tokenBudget"] = 100,
        })];
        yield return [LogLevel.Information, new EventId(7504), CreateEvent(7504, "case-access", "case-a", 1, new Dictionary<string, object?>
        {
            ["memoryUnitId"] = "raw-memory-unit",
        })];
        yield return [LogLevel.Information, new EventId(7505), CreateEvent(7505, "delete", "case-a", null, new Dictionary<string, object?>
        {
            ["memoryUnitIdPrefix"] = "raw-prefix",
            ["operation"] = "memory-unit-delete",
        })];
        yield return [LogLevel.Information, new EventId(7506), CreateEvent(7506, "tenant-lifecycle", null, null, new Dictionary<string, object?>
        {
            ["operation"] = "tenant-create",
            ["state"] = "pending",
            ["workflowInstanceIdPrefix"] = "raw-prefix",
        })];
        yield return [LogLevel.Information, new EventId(7507), CreateEvent(7507, "tenant-config", null, null, new Dictionary<string, object?>
        {
            ["changedFields"] = new[] { "displayName" },
            ["fieldCount"] = 1,
            ["forceReindex"] = false,
            ["operation"] = "display-name-update",
        })];
        yield return [LogLevel.Information, new EventId(7508), CreateEvent(7508, "case-member", "case-a", null, new Dictionary<string, object?>
        {
            ["memberIdPrefix"] = "raw-prefix",
            ["operation"] = "case-member-add",
        })];
        yield return [LogLevel.Information, new EventId(7509), CreateEvent(7509, "annotation", "case-a", null, new Dictionary<string, object?>
        {
            ["memoryUnitIdPrefix"] = "raw-prefix",
            ["operation"] = "annotation-create",
        })];
    }

    [Theory]
    [MemberData(nameof(OperationEvents))]
    public void Sanitizer_AllNineOperationFamilies_ProduceCanonicalBoundedRecords(
        LogLevel level,
        EventId eventId,
        AccessTelemetryEvent source)
    {
        bool accepted = CreateSanitizer().TrySanitize(level, eventId, source, out AccessTelemetryRecord? record, out AccessTelemetryReason reason);

        accepted.ShouldBeTrue(reason.ToString());
        record.ShouldNotBeNull();
        record.OperationType.ShouldBe(source.OperationType);
        AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length.ShouldBeLessThanOrEqualTo(AccessTelemetryOptions.MaximumRecordBytes);
    }

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
    public void Sanitizer_BlankTenantPreservesRejectedScopeEvidenceWithoutCorrelationMarkers()
    {
        AccessTelemetryEvent source = CreateSearchEvent() with { TenantId = "  ", CaseId = "case-secret" };

        CreateSanitizer().TrySanitize(
            LogLevel.Information,
            new EventId(7501),
            source,
            out AccessTelemetryRecord? record,
            out AccessTelemetryReason reason).ShouldBeTrue(reason.ToString());

        record.ShouldNotBeNull();
        record.TenantMarker.ShouldBe("__rejected__");
        record.CaseMarker.ShouldBeNull();
        record.UserMarker.ShouldBeNull();
        record.QueryParams["caseScope"].ShouldBe("rejected-or-unknown");
    }

    [Fact]
    public void Sanitizer_WrongTypedStateAndUnknownActionSubtype_FailClosed()
    {
        AccessTelemetryEvent wrongType = CreateEvent(7502, "ingest", null, null, new Dictionary<string, object?>
        {
            ["bytes"] = 42,
            ["contentType"] = "text/plain",
            ["sourceType"] = "file",
        });
        AccessTelemetryEvent unknownAction = CreateEvent(7505, "delete", "case-a", null, new Dictionary<string, object?>
        {
            ["operation"] = "invented-delete",
        });

        CreateSanitizer().TrySanitize(LogLevel.Information, new EventId(7502), wrongType, out _, out AccessTelemetryReason wrongTypeReason).ShouldBeFalse();
        CreateSanitizer().TrySanitize(LogLevel.Information, new EventId(7505), unknownAction, out _, out AccessTelemetryReason actionReason).ShouldBeFalse();
        wrongTypeReason.ShouldBe(AccessTelemetryReason.SchemaMismatch);
        actionReason.ShouldBe(AccessTelemetryReason.SchemaMismatch);
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
    public void Provider_RecordsAcceptedOrRejectedActivityAtAdmissionTime()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        var accessor = new AccessTelemetrySanitizerAccessor();
        accessor.Publish(CreateSanitizer());
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        using var provider = new AccessTelemetryLifecycleLoggerProvider(
            queue,
            accessor,
            status,
            new FakeTimeProvider(Now));
        ILogger logger = provider.CreateLogger(typeof(AccessTelemetryCategory).FullName!);

        logger.Log(LogLevel.Information, new EventId(7501), CreateSearchEvent(), null, static (_, _) => "ignored");

        status.Current.LastAcceptedOrRejectedUtc.ShouldBe(Now);
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
    public async Task Queue_AdmissionReturnsImmediatelyWhenAnotherThreadOwnsTheGate()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        object gate = typeof(BoundedAccessTelemetryQueue)
            .GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(queue)!;
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task holder = Task.Run(() =>
        {
            lock (gate)
            {
                entered.Set();
                release.Wait();
            }
        });
        entered.Wait();

        var stopwatch = Stopwatch.StartNew();
        bool accepted = queue.TryEnqueue(Sanitize(CreateSearchEvent()), out AccessTelemetryReason reason);
        stopwatch.Stop();
        release.Set();
        await holder;

        accepted.ShouldBeFalse();
        reason.ShouldBe(AccessTelemetryReason.QueueFull);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromMilliseconds(500));
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
        var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(enabled: true));

        await worker.DrainOnceAsync(CancellationToken.None);

        await client.Received(1).SendAsync(
            Arg.Is<IReadOnlyList<AccessTelemetryRecord>>(records => records!.Count == 256),
            Arg.Any<CancellationToken>());
        queue.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Worker_RemovesNonPrefixExpiryAndThenAcknowledgesTheSurvivingFifoBatch()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        AccessTelemetryRecord first = Reidentify(template with { ExpiresAtUtc = Format(Now.AddHours(1)) });
        AccessTelemetryRecord expired = Reidentify(template with { ExpiresAtUtc = Format(Now.AddMilliseconds(-1)) });
        AccessTelemetryRecord third = Reidentify(template with { ExpiresAtUtc = Format(Now.AddHours(1)) });
        queue.TryEnqueue(first, out _).ShouldBeTrue();
        queue.TryEnqueue(expired, out _).ShouldBeTrue();
        queue.TryEnqueue(third, out _).ShouldBeTrue();
        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(new AccessTelemetryWriteBatchResponse { Accepted = 2, Rejected = 0, Reason = AccessTelemetryReason.None });
        var worker = CreateWorker(queue, client);

        await worker.DrainOnceAsync(CancellationToken.None);

        await client.Received(1).SendAsync(
            Arg.Is<IReadOnlyList<AccessTelemetryRecord>>(records =>
                records != null && records.Count == 2 && records[0].RecordId == first.RecordId && records[1].RecordId == third.RecordId),
            Arg.Any<CancellationToken>());
        queue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Worker_PartialAcknowledgementAdvancesPrefixAndRetriesOnlyTheRemainder()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        foreach (int index in Enumerable.Range(0, 3))
        {
            _ = index;
            queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        }

        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(
                new AccessTelemetryWriteBatchResponse { Accepted = 1, Rejected = 2, Reason = AccessTelemetryReason.DependencyUnavailable },
                new AccessTelemetryWriteBatchResponse { Accepted = 2, Rejected = 0, Reason = AccessTelemetryReason.None });
        var worker = CreateWorker(queue, client);

        await worker.DrainOnceAsync(CancellationToken.None);
        queue.Count.ShouldBe(2);
        await worker.DrainOnceAsync(CancellationToken.None);

        queue.Count.ShouldBe(0);
        await client.Received(2).SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Worker_HonorsConfiguredBatchLimitAndStopsAfterTerminalConflict()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        foreach (int index in Enumerable.Range(0, 3))
        {
            _ = index;
            queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        }

        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = 2, Reason = AccessTelemetryReason.RecordIdConflict });
        var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions { BatchRecordLimit = 2, BatchByteLimit = 8192 },
            new AccessTelemetryLifecycleStatus(enabled: true));

        await worker.DrainOnceAsync(CancellationToken.None);
        await worker.DrainOnceAsync(CancellationToken.None);

        var stopwatch = Stopwatch.StartNew();
        await worker.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        await client.Received(1).SendAsync(
            Arg.Is<IReadOnlyList<AccessTelemetryRecord>>(records => records != null && records.Count == 2),
            Arg.Any<CancellationToken>());
        queue.Count.ShouldBe(3);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DaprDeliveryClient_ReturnsBoundedTerminalErrorBodyBeforeHttpSuccessEnforcement()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new AccessTelemetryWriteBatchResponse
            {
                Accepted = 0,
                Rejected = 1,
                Reason = AccessTelemetryReason.ConfigurationInvalid,
            }),
        }))
        {
            BaseAddress = new Uri("http://dapr.test/"),
        };
        IAccessTelemetryClockEvidenceProvider clock = Substitute.For<IAccessTelemetryClockEvidenceProvider>();
        clock.GetAsync(Arg.Any<CancellationToken>()).Returns(CreateAttestation());
        var client = new DaprAccessTelemetryDeliveryClient(
            httpClient,
            clock,
            new AccessTelemetryOptions
            {
                ConfigurationEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
                ComponentProfileHash = new string('a', 64),
            },
            new AccessTelemetryWriterIdentity(new MonotonicRecordIdGenerator()));

        AccessTelemetryWriteBatchResponse response = await client.SendAsync(
            [Sanitize(CreateSearchEvent())],
            CancellationToken.None);

        response.Reason.ShouldBe(AccessTelemetryReason.ConfigurationInvalid);
        response.Rejected.ShouldBe(1);
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
        client.SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>())
            .Returns(new WriterHeartbeatResponse
            {
                Accepted = true,
                Reason = AccessTelemetryReason.None,
                ActiveGeneration = options.MarkerKeyGeneration,
            });
        var worker = new AccessTelemetryHeartbeatWorker(
            client,
            options,
            identity,
            new BoundedAccessTelemetryQueue(8, 8192),
            new AccessTelemetryLifecycleStatus(enabled: true),
            new FakeTimeProvider(Now));

        await worker.SendOnceAsync(CancellationToken.None);

        await client.Received(1).SendAsync(
            Arg.Is<WriterHeartbeat>(heartbeat =>
                heartbeat!.DeploymentId == "deployment-a" &&
                heartbeat.ServiceInstanceId == identity.ServiceInstanceId &&
                heartbeat.ProcessEpoch == identity.ProcessEpoch &&
                heartbeat.MarkerKeyGeneration == "mk-2026a" &&
                heartbeat.OldKeyQueueCount == 0 &&
                heartbeat.LeaseExpiresAtUnixMilliseconds == Now.AddSeconds(30).ToUnixTimeMilliseconds()),
            Arg.Any<CancellationToken>());
        AccessTelemetryHeartbeatWorker.HeartbeatInterval.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task HeartbeatWorker_TerminalConfigurationResponseCannotBeOverwritten()
    {
        IAccessTelemetryHeartbeatClient client = Substitute.For<IAccessTelemetryHeartbeatClient>();
        client.SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>())
            .Returns(new WriterHeartbeatResponse
            {
                Accepted = false,
                Reason = AccessTelemetryReason.ConfigurationInvalid,
                ActiveGeneration = "mk-2026a",
            });
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var worker = new AccessTelemetryHeartbeatWorker(
            client,
            new AccessTelemetryOptions { DeploymentId = "deployment-a", MarkerKeyGeneration = "mk-2026a" },
            new AccessTelemetryWriterIdentity(new MonotonicRecordIdGenerator()),
            new BoundedAccessTelemetryQueue(8, 8192),
            status,
            new FakeTimeProvider(Now));

        await worker.SendOnceAsync(CancellationToken.None);
        status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);

        status.Current.Health.ShouldBe(AccessTelemetryHealthState.Unhealthy);
        status.Current.Reason.ShouldBe(AccessTelemetryReason.ConfigurationInvalid);
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

    private static AccessTelemetryDeliveryWorker CreateWorker(
        BoundedAccessTelemetryQueue queue,
        IAccessTelemetryDeliveryClient client)
        => new(
            queue,
            client,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(enabled: true));

    private static AccessTelemetryRecord Reidentify(AccessTelemetryRecord record)
    {
        AccessTelemetryRecord identified = record with
        {
            RecordId = new MonotonicRecordIdGenerator().NewId(),
            EnvelopeHash = string.Empty,
        };
        return identified with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(identified) };
    }

    private static string Format(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private static SignedClockAttestation CreateAttestation()
        => new()
        {
            DeploymentId = "deployment-a",
            AppId = "memories",
            ServiceInstanceId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XB",
            ProcessEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XC",
            ComponentProfileHash = new string('a', 64),
            RequestingProcessEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            RequestingServiceInstanceId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XA",
            Nonce = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XD",
            NotBeforeUnixMilliseconds = Now.AddMilliseconds(-10).ToUnixTimeMilliseconds(),
            NotAfterUnixMilliseconds = Now.AddMilliseconds(10).ToUnixTimeMilliseconds(),
            IssuedAtUnixMilliseconds = Now.ToUnixTimeMilliseconds(),
            ExpiresAtUnixMilliseconds = Now.AddSeconds(30).ToUnixTimeMilliseconds(),
            SignerKeyEpoch = "clock-key-1",
            Signature = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responseFactory(request));
        }
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

    private static AccessTelemetryEvent CreateEvent(
        int eventId,
        string operation,
        string? caseId,
        int? resultCount,
        IReadOnlyDictionary<string, object?> queryParams)
        => new()
        {
            EventId = eventId,
            Timestamp = "2026-07-18T09:59:59.000+00:00",
            TenantId = "tenant-a",
            OperationType = operation,
            CaseId = caseId,
            User = "alice@example.test",
            QueryParams = queryParams,
            ResultCount = resultCount,
            DurationMs = 12,
            Outcome = "ok",
            ErrorCode = null,
        };
}
