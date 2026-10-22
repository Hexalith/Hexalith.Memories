// <copyright file="AccessTelemetryDeliveryCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
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
[Collection(Infrastructure.TelemetryTestCollection.Name)]
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
    public async Task Queue_AdmissionAndLoggerMetricsCompleteWhileConsumerScanIsPaused()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        AccessTelemetryRecord first = Reidentify(Sanitize(CreateSearchEvent()) with { EmittedAtUtc = Format(Now.AddSeconds(-20)) });
        AccessTelemetryRecord second = Reidentify(first with { EmittedAtUtc = Format(Now.AddSeconds(-10)) });
        queue.TryEnqueue(first, out _).ShouldBeTrue();
        queue.OldestEmittedAtUtc.ShouldBe(Now.AddSeconds(-20));
        var scanning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var resume = new ManualResetEventSlim();
        Task<int> consumer = Task.Run(() => queue.RemoveWhere(_ =>
        {
            scanning.SetResult();
            resume.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
            return false;
        }));
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, CreateSanitizer());
        ILogger logger = provider.CreateLogger(typeof(AccessTelemetryCategory).FullName!);
        try
        {
            await scanning.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Run(() =>
            {
                queue.TryEnqueue(second, out AccessTelemetryReason reason).ShouldBeTrue();
                reason.ShouldBe(AccessTelemetryReason.None);
                logger.Log(LogLevel.Information, new EventId(7501), CreateSearchEvent(), null, static (_, _) => "ignored");
                queue.Count.ShouldBe(3);
                queue.ByteCount.ShouldBe(3 * AccessTelemetryCanonicalizer.CanonicalizeRecord(first).Length);
                queue.CountByMarkerKey(first.MarkerKeyId).ShouldBe(3);
                queue.OldestEmittedAtUtc.ShouldBe(Now.AddSeconds(-20));
                queue.GetOldestAgeSeconds(Now).ShouldBe(20);
            }).WaitAsync(TimeSpan.FromSeconds(5));
            consumer.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            resume.Set();
            await consumer.WaitAsync(TimeSpan.FromSeconds(5));
        }

        queue.CountByMarkerKey(first.MarkerKeyId).ShouldBe(3);
        queue.PeekBatch(8, 8192)[0].RecordId.ShouldBe(first.RecordId);
        queue.OldestEmittedAtUtc.ShouldBe(Now.AddSeconds(-20));
        queue.Acknowledge(1);
        queue.OldestEmittedAtUtc.ShouldBe(Now.AddSeconds(-10));
        queue.GetOldestAgeSeconds(Now).ShouldBe(10);
        queue.Acknowledge(1);
        queue.OldestEmittedAtUtc.ShouldBe(Now.AddSeconds(-1));
        queue.GetOldestAgeSeconds(Now).ShouldBe(1);
        queue.Acknowledge(1);
        queue.OldestEmittedAtUtc.ShouldBeNull();
        queue.GetOldestAgeSeconds(Now).ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Queue_SimultaneousProducersRespectBothBoundsIncludingPendingDelivery(bool recordBound)
    {
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(template).Length;
        var queue = new BoundedAccessTelemetryQueue(recordBound ? 3 : 16, recordBound ? bytes * 16 : (bytes * 4) - 1);
        queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        queue.PeekBatch(2, bytes * 2).Count.ShouldBe(2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>[] producers = Enumerable.Range(0, 16).Select(async _ =>
        {
            await start.Task;
            bool accepted = queue.TryEnqueue(Reidentify(template), out AccessTelemetryReason reason);
            reason.ShouldBe(accepted ? AccessTelemetryReason.None : AccessTelemetryReason.QueueFull);
            queue.Count.ShouldBeLessThanOrEqualTo(3);
            queue.ByteCount.ShouldBeLessThanOrEqualTo(bytes * 3);
            queue.CountByMarkerKey(template.MarkerKeyId).ShouldBeInRange(0, recordBound ? 3 : 16);
            return accepted;
        }).ToArray();
        start.SetResult();
        bool[] admitted = await Task.WhenAll(producers).WaitAsync(TimeSpan.FromSeconds(5));

        admitted.Count(static accepted => accepted).ShouldBe(1);
        queue.Count.ShouldBe(3);
        queue.ByteCount.ShouldBe(bytes * 3);
        queue.Acknowledge(1);
        queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        queue.RemoveWhere(static _ => true).ShouldBe(3);
        queue.RemoveWhere(static _ => true).ShouldBe(0);
        queue.Count.ShouldBe(0);
        queue.ByteCount.ShouldBe(0);
        queue.OldestEmittedAtUtc.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => queue.Acknowledge(1));
        queue.Count.ShouldBe(0);
        queue.ByteCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Queue_PausedPublicationRetainsMarkerOwnershipAndRollsBackThrowingAdmission(bool throwOnAdmission)
    {
        AccessTelemetryRecord record = Sanitize(CreateSearchEvent());
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length;
        var queue = new BoundedAccessTelemetryQueue(1, bytes);
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var publish = new ManualResetEventSlim();
        Task<bool> producer = Task.Run(() => queue.TryEnqueue(record, out _, () =>
        {
            admitted.SetResult();
            publish.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
            if (throwOnAdmission)
            {
                throw new InvalidOperationException("admission-accounting-failed");
            }
        }));
        try
        {
            await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            queue.Count.ShouldBe(1);
            queue.ByteCount.ShouldBe(bytes);
            queue.CountByMarkerKey(record.MarkerKeyId).ShouldBe(1);
            var saturatedStart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task[] saturatedProducers = Enumerable.Range(0, 16).Select(async _ =>
            {
                await saturatedStart.Task;
                queue.CountByMarkerKey(record.MarkerKeyId).ShouldBeInRange(0, 1);
                queue.TryEnqueue(record, out AccessTelemetryReason full).ShouldBeFalse();
                full.ShouldBe(AccessTelemetryReason.QueueFull);
                queue.CountByMarkerKey(record.MarkerKeyId).ShouldBeInRange(0, 1);
            }).ToArray();
            saturatedStart.SetResult();
            await Task.WhenAll(saturatedProducers).WaitAsync(TimeSpan.FromSeconds(5));
            queue.CountByMarkerKey(record.MarkerKeyId).ShouldBe(1);
            queue.PeekBatch(1, bytes).ShouldBeEmpty();
            queue.TryEnqueue(record, out AccessTelemetryReason reason).ShouldBeFalse();
            reason.ShouldBe(AccessTelemetryReason.QueueFull);
            queue.CountByMarkerKey(record.MarkerKeyId).ShouldBe(1);
            queue.TryEnqueue(Reidentify(record with { MarkerKeyId = "mk-other" }), out _).ShouldBeFalse();
            queue.CountByMarkerKey("mk-other").ShouldBe(0);
            publish.Set();
            if (throwOnAdmission)
            {
                InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
                    () => producer.WaitAsync(TimeSpan.FromSeconds(5)));
                exception.Message.ShouldBe("admission-accounting-failed");
                queue.Count.ShouldBe(0);
                queue.ByteCount.ShouldBe(0);
                queue.CountByMarkerKey(record.MarkerKeyId).ShouldBe(0);
                queue.PeekBatch(1, bytes).ShouldBeEmpty();
                // Rollback restores both bounds, including the same marker's ownership.
                queue.TryEnqueue(record, out _).ShouldBeTrue();
            }
            else
            {
                (await producer.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();
            }

            queue.Count.ShouldBe(1);
            queue.ByteCount.ShouldBe(bytes);
            queue.CountByMarkerKey(record.MarkerKeyId).ShouldBe(1);
            queue.PeekBatch(1, bytes).Single().RecordId.ShouldBe(record.RecordId);
            queue.Acknowledge(1);
            queue.Count.ShouldBe(0);
            queue.ByteCount.ShouldBe(0);
            queue.CountByMarkerKey(record.MarkerKeyId).ShouldBe(0);
        }
        finally
        {
            publish.Set();
            try
            {
                await producer.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (InvalidOperationException) when (throwOnAdmission)
            {
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Queue_ProducersReuseReleasedCapacityWhileExpiryRemovalIsPaused(bool recordBound)
    {
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        AccessTelemetryRecord survivor = Reidentify(template with { UserMarker = null });
        AccessTelemetryRecord expired = Reidentify(template with
        {
            CaseMarker = new string('c', 64),
            ExpiresAtUtc = Format(Now.AddSeconds(-1)),
        });
        AccessTelemetryRecord[] candidates =
        [
            Reidentify(template with { MarkerKeyId = "mk-2026b" }),
            Reidentify(template with { MarkerKeyId = "mk-2026b", CaseMarker = new string('d', 64) }),
        ];
        int survivorBytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(survivor).Length;
        int expiredBytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(expired).Length;
        int[] candidateBytes = candidates.Select(record => AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length).ToArray();
        survivorBytes.ShouldBeLessThan(candidateBytes[0]);
        candidateBytes[0].ShouldBeLessThan(candidateBytes[1]);
        int byteLimit = survivorBytes + expiredBytes + (recordBound ? candidateBytes[0] : 0);
        int recordLimit = recordBound ? 2 : 8;
        var queue = new BoundedAccessTelemetryQueue(recordLimit, byteLimit);
        queue.TryEnqueue(survivor, out _).ShouldBeTrue();
        queue.TryEnqueue(expired, out _).ShouldBeTrue();
        queue.TryEnqueue(candidates[0], out _).ShouldBeFalse();
        queue.CountByMarkerKey("mk-2026b").ShouldBe(0);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var finishRemoval = new ManualResetEventSlim();
        Task<bool>[] producers = candidates.Select(async record =>
        {
            await released.Task;
            bool accepted = queue.TryEnqueue(record, out AccessTelemetryReason reason);
            reason.ShouldBe(accepted ? AccessTelemetryReason.None : AccessTelemetryReason.QueueFull);
            queue.Count.ShouldBeLessThanOrEqualTo(recordLimit);
            queue.ByteCount.ShouldBeLessThanOrEqualTo(byteLimit);
            return accepted;
        }).ToArray();
        var removedIds = new List<string>();
        Task<int> consumer = Task.Run(() => queue.RemoveWhere(
            record => string.Equals(record.RecordId, expired.RecordId, StringComparison.Ordinal),
            record =>
            {
                removedIds.Add(record.RecordId);
                released.SetResult();
                finishRemoval.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
            }));
        try
        {
            bool[] admitted = await Task.WhenAll(producers).WaitAsync(TimeSpan.FromSeconds(5));
            consumer.IsCompleted.ShouldBeFalse();
            admitted.Count(static accepted => accepted).ShouldBe(1);
            int winner = Array.FindIndex(admitted, static accepted => accepted);
            queue.Count.ShouldBe(2);
            queue.ByteCount.ShouldBe(survivorBytes + candidateBytes[winner]);
            queue.CountByMarkerKey(template.MarkerKeyId).ShouldBe(1);
            queue.CountByMarkerKey("mk-2026b").ShouldBe(1);
            finishRemoval.Set();
            (await consumer.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBe(1);
            removedIds.ShouldBe(new[] { expired.RecordId });
            queue.PeekBatch(8, byteLimit).Select(static record => record.RecordId)
                .ShouldBe(new[] { survivor.RecordId, candidates[winner].RecordId });
            queue.Acknowledge(1);
            queue.ByteCount.ShouldBe(candidateBytes[winner]);
            queue.CountByMarkerKey(template.MarkerKeyId).ShouldBe(0);
            queue.Acknowledge(1);
            queue.Count.ShouldBe(0);
            queue.ByteCount.ShouldBe(0);
            queue.CountByMarkerKey("mk-2026b").ShouldBe(0);
            queue.RemoveWhere(static _ => true).ShouldBe(0);
        }
        finally
        {
            finishRemoval.Set();
            await consumer.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task Worker_CancelledWaitAndActiveDeliveryPreservePendingRecordsForSubsequentDrain()
    {
        AccessTelemetryRecord first = Sanitize(CreateSearchEvent());
        AccessTelemetryRecord second = Reidentify(first with { UserMarker = null });
        int firstBytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(first).Length;
        int secondBytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(second).Length;
        var queue = new BoundedAccessTelemetryQueue(2, firstBytes + secondBytes);
        queue.TryEnqueue(first, out _).ShouldBeTrue();
        var sending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldResponse = new TaskCompletionSource<AccessTelemetryWriteBatchResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var batches = new List<string[]>();
        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                IReadOnlyList<AccessTelemetryRecord> batch = call.Arg<IReadOnlyList<AccessTelemetryRecord>>();
                batches.Add(batch.Select(static record => record.RecordId).ToArray());
                if (batches.Count == 1)
                {
                    sending.SetResult();
                    return heldResponse.Task.WaitAsync(call.Arg<CancellationToken>());
                }

                return Task.FromResult(new AccessTelemetryWriteBatchResponse
                {
                    Accepted = batch.Count, Rejected = 0, Reason = AccessTelemetryReason.None,
                });
            });
        var accounting = new AccessTelemetryQualificationAccounting();
        var worker = new AccessTelemetryDeliveryWorker(queue, client, new FakeTimeProvider(Now),
            new AccessTelemetryOptions(), new AccessTelemetryLifecycleStatus(true), accounting);
        using var cancelActive = new CancellationTokenSource();
        using var cancelWaiter = new CancellationTokenSource();
        Task active = worker.DrainOnceAsync(cancelActive.Token);
        await sending.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task waiter = worker.DrainOnceAsync(cancelWaiter.Token);
        try
        {
            waiter.IsCompleted.ShouldBeFalse();
            cancelWaiter.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => waiter.WaitAsync(TimeSpan.FromSeconds(5)));
            active.IsCompleted.ShouldBeFalse();
            heldResponse.Task.IsCompleted.ShouldBeFalse();
            queue.TryEnqueue(second, out _).ShouldBeTrue();
            queue.Count.ShouldBe(2);
            queue.ByteCount.ShouldBe(firstBytes + secondBytes);
            queue.CountByMarkerKey(first.MarkerKeyId).ShouldBe(2);
            cancelActive.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => active.WaitAsync(TimeSpan.FromSeconds(5)));
            heldResponse.Task.IsCompleted.ShouldBeFalse();
            queue.Count.ShouldBe(2);
            queue.ByteCount.ShouldBe(firstBytes + secondBytes);
            accounting.Current.ShouldBe(new AccessTelemetryQualificationAccountingSnapshot(0, 0, 0, 0, 0, 0));
            await worker.DrainOnceAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            batches[0].ShouldBe(new[] { first.RecordId });
            batches[1].ShouldBe(new[] { first.RecordId, second.RecordId });
            batches.Count.ShouldBe(2);
            accounting.Current.ShouldBe(new AccessTelemetryQualificationAccountingSnapshot(0, 0, 2, 0, 0, 0));
            queue.Count.ShouldBe(0);
            queue.ByteCount.ShouldBe(0);
            queue.CountByMarkerKey(first.MarkerKeyId).ShouldBe(0);
        }
        finally
        {
            cancelWaiter.Cancel();
            cancelActive.Cancel();
            heldResponse.TrySetResult(new AccessTelemetryWriteBatchResponse
            {
                Accepted = 1, Rejected = 0, Reason = AccessTelemetryReason.None,
            });
            try
            {
                await Task.WhenAll(active, waiter).WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    [Fact]
    public async Task Worker_ConcurrentDrainWaitsForAcknowledgementWithoutBlockingAdmission()
    {
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(template).Length;
        var queue = new BoundedAccessTelemetryQueue(4, bytes * 4);
        AccessTelemetryRecord first = Reidentify(template);
        AccessTelemetryRecord second = Reidentify(template);
        queue.TryEnqueue(first, out _).ShouldBeTrue();
        queue.TryEnqueue(second, out _).ShouldBeTrue();
        var sending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var acknowledge = new TaskCompletionSource<AccessTelemetryWriteBatchResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var batches = new List<string[]>();
        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                IReadOnlyList<AccessTelemetryRecord> batch = call.Arg<IReadOnlyList<AccessTelemetryRecord>>();
                batches.Add(batch.Select(static record => record.RecordId).ToArray());
                if (batches.Count == 1)
                {
                    sending.SetResult();
                    return acknowledge.Task;
                }

                return Task.FromResult(new AccessTelemetryWriteBatchResponse { Accepted = batch.Count, Rejected = 0, Reason = AccessTelemetryReason.None });
            });
        var accounting = new AccessTelemetryQualificationAccounting();
        var worker = new AccessTelemetryDeliveryWorker(queue, client, new FakeTimeProvider(Now),
            new AccessTelemetryOptions { BatchRecordLimit = 2 }, new AccessTelemetryLifecycleStatus(true), accounting);
        Task firstDrain = worker.DrainOnceAsync(CancellationToken.None);
        await sending.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task secondDrain = worker.DrainOnceAsync(CancellationToken.None);
        try
        {
            secondDrain.IsCompleted.ShouldBeFalse();
            AccessTelemetryRecord third = Reidentify(template);
            queue.TryEnqueue(third, out _).ShouldBeTrue();
            queue.Count.ShouldBe(3);
            queue.ByteCount.ShouldBe(bytes * 3);
            batches.Count.ShouldBe(1);
            acknowledge.SetResult(new AccessTelemetryWriteBatchResponse { Accepted = 2, Rejected = 0, Reason = AccessTelemetryReason.None });
            await Task.WhenAll(firstDrain, secondDrain).WaitAsync(TimeSpan.FromSeconds(5));
            batches[0].ShouldBe(new[] { first.RecordId, second.RecordId });
            batches[1].ShouldBe(new[] { third.RecordId });
            accounting.Current.Persisted.ShouldBe(3);
            queue.Count.ShouldBe(0);
            queue.ByteCount.ShouldBe(0);
        }
        finally
        {
            acknowledge.TrySetResult(new AccessTelemetryWriteBatchResponse { Accepted = 2, Rejected = 0, Reason = AccessTelemetryReason.None });
            await Task.WhenAll(firstDrain, secondDrain).WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Worker_RetryPartialAcknowledgementAndExpiryReleaseEachReservationOnce(bool retryAge)
    {
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        AccessTelemetryRecord first = Reidentify(template);
        AccessTelemetryRecord expired = Reidentify(retryAge
            ? template with { EmittedAtUtc = Format(Now.AddMinutes(-5).AddSeconds(1)) }
            : template with { ExpiresAtUtc = Format(Now.AddSeconds(1)) });
        AccessTelemetryRecord third = Reidentify(template);
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(template).Length;
        var queue = new BoundedAccessTelemetryQueue(3, bytes * 3);
        foreach (AccessTelemetryRecord record in new[] { first, expired, third })
        {
            queue.TryEnqueue(record, out _).ShouldBeTrue();
        }

        var batches = new List<string[]>();
        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                IReadOnlyList<AccessTelemetryRecord> batch = call.Arg<IReadOnlyList<AccessTelemetryRecord>>();
                batches.Add(batch.Select(static record => record.RecordId).ToArray());
                return batches.Count switch
                {
                    1 => Task.FromException<AccessTelemetryWriteBatchResponse>(new HttpRequestException("unavailable")),
                    2 => Task.FromResult(new AccessTelemetryWriteBatchResponse { Accepted = 1, Rejected = 2, Reason = AccessTelemetryReason.DependencyUnavailable }),
                    _ => Task.FromResult(new AccessTelemetryWriteBatchResponse { Accepted = batch.Count, Rejected = 0, Reason = AccessTelemetryReason.None }),
                };
            });
        var time = new FakeTimeProvider(Now);
        var accounting = new AccessTelemetryQualificationAccounting();
        var worker = new AccessTelemetryDeliveryWorker(queue, client, time, new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(true), accounting);
        await worker.DrainOnceAsync(CancellationToken.None);
        queue.Count.ShouldBe(3);
        queue.ByteCount.ShouldBe(bytes * 3);
        queue.TryEnqueue(Reidentify(template), out AccessTelemetryReason full).ShouldBeFalse();
        full.ShouldBe(AccessTelemetryReason.QueueFull);
        accounting.Current.ShouldBe(new AccessTelemetryQualificationAccountingSnapshot(0, 0, 0, 0, 0, 0));

        await worker.DrainOnceAsync(CancellationToken.None);
        queue.Count.ShouldBe(2);
        queue.ByteCount.ShouldBe(bytes * 2);
        AccessTelemetryRecord newest = Reidentify(template);
        queue.TryEnqueue(newest, out _).ShouldBeTrue();
        time.Advance(TimeSpan.FromSeconds(1));
        await worker.DrainOnceAsync(CancellationToken.None);
        await worker.DrainOnceAsync(CancellationToken.None);

        batches.Count.ShouldBe(3);
        batches[0].ShouldBe(new[] { first.RecordId, expired.RecordId, third.RecordId });
        batches[1].ShouldBe(batches[0]);
        batches[2].ShouldBe(new[] { third.RecordId, newest.RecordId });
        accounting.Current.Persisted.ShouldBe(3);
        accounting.Current.Dropped.ShouldBe(1);
        accounting.Current.Rejected.ShouldBe(0);
        queue.Count.ShouldBe(0);
        queue.ByteCount.ShouldBe(0);
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
        var clock = new FakeTimeProvider(Now);
        var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            clock,
            new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(enabled: true));
        List<long> observedQueueRecords = [];
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == ServerAccessTelemetryLifecycleMetrics.MeterName &&
                instrument.Name == AccessTelemetryMetricContract.QueueRecords)
            {
                current.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => observedQueueRecords.Add(value));
        listener.Start();

        await worker.DrainOnceAsync(CancellationToken.None);
        queue.Count.ShouldBe(2);
        listener.RecordObservableInstruments();
        observedQueueRecords.ShouldNotBeEmpty();
        observedQueueRecords[^1].ShouldBe(2);
        await worker.DrainOnceAsync(CancellationToken.None);

        queue.Count.ShouldBe(0);
        await client.Received(2).SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ServerLifecycleInstruments_EmitLiveQueueAndAttestationAges()
    {
        var clock = new FakeTimeProvider(Now);
        List<(string Name, double Value)> doubles = [];
        List<(string Name, long Value)> integers = [];
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == ServerAccessTelemetryLifecycleMetrics.MeterName)
            {
                current.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
            doubles.Add((instrument.Name, value)));
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            integers.Add((instrument.Name, value)));
        listener.Start();

        ServerAccessTelemetryLifecycleMetrics.RecordQueue(3, 123, Now.AddSeconds(-10), clock);
        ServerAccessTelemetryLifecycleMetrics.RecordAttestation(CreateAttestation(Now.AddSeconds(-5)), Now, clock);
        ServerAccessTelemetryLifecycleMetrics.Record(
            AccessTelemetryRecordState.Enqueued,
            AccessTelemetryReason.None);
        ServerAccessTelemetryLifecycleMetrics.RecordDaprLatency(11);
        ServerAccessTelemetryLifecycleMetrics.RecordAttestationLatency(12);
        listener.RecordObservableInstruments();

        integers.Last(measurement => measurement.Name == AccessTelemetryMetricContract.QueueRecords).Value.ShouldBe(3);
        integers.Last(measurement => measurement.Name == AccessTelemetryMetricContract.QueueBytes).Value.ShouldBe(123);
        doubles.Last(measurement => measurement.Name == AccessTelemetryMetricContract.QueueOldestAge).Value.ShouldBe(10);
        doubles.Last(measurement => measurement.Name == AccessTelemetryMetricContract.AttestationAge).Value.ShouldBe(5);
        clock.Advance(TimeSpan.FromSeconds(5));
        listener.RecordObservableInstruments();
        doubles.Last(measurement => measurement.Name == AccessTelemetryMetricContract.QueueOldestAge).Value.ShouldBe(15);
        doubles.Last(measurement => measurement.Name == AccessTelemetryMetricContract.AttestationAge).Value.ShouldBe(10);

        string[] observedNames = doubles.Select(static measurement => measurement.Name)
            .Concat(integers.Select(static measurement => measurement.Name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        foreach (string expected in new[]
        {
            AccessTelemetryMetricContract.Records,
            AccessTelemetryMetricContract.QueueRecords,
            AccessTelemetryMetricContract.QueueBytes,
            AccessTelemetryMetricContract.QueueOldestAge,
            AccessTelemetryMetricContract.DaprDuration,
            AccessTelemetryMetricContract.AttestationDuration,
            AccessTelemetryMetricContract.AttestationAge,
            AccessTelemetryMetricContract.AttestationDelta,
            AccessTelemetryMetricContract.AttestationUncertainty,
        })
        {
            observedNames.ShouldContain(expected);
        }
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
            .Returns(new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = 2, Reason = AccessTelemetryReason.ConfigurationInvalid });
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
        queue.Count.ShouldBe(1);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Worker_RecordIdConflictDoesNotTerminalizeTheWorker()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        foreach (int index in Enumerable.Range(0, 3))
        {
            _ = index;
            queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        }

        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        int sendCount = 0;
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                IReadOnlyList<AccessTelemetryRecord> batch = callInfo.Arg<IReadOnlyList<AccessTelemetryRecord>>();
                sendCount++;
                if (sendCount <= 2)
                {
                    return new AccessTelemetryWriteBatchResponse
                    {
                        Accepted = 0,
                        Rejected = batch.Count,
                        Reason = AccessTelemetryReason.RecordIdConflict,
                    };
                }

                return new AccessTelemetryWriteBatchResponse
                {
                    Accepted = batch.Count,
                    Rejected = 0,
                    Reason = AccessTelemetryReason.None,
                };
            });
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions { BatchRecordLimit = 2, BatchByteLimit = 8192 },
            status);

        await worker.DrainOnceAsync(CancellationToken.None);
        await worker.DrainOnceAsync(CancellationToken.None);

        await client.Received(2).SendAsync(
            Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(),
            Arg.Any<CancellationToken>());
        queue.Count.ShouldBe(0);
        status.Current.Health.ShouldBe(AccessTelemetryHealthState.Unhealthy);
        status.Current.Reason.ShouldBe(AccessTelemetryReason.RecordIdConflict);

        queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        await worker.DrainOnceAsync(CancellationToken.None);

        await client.Received(3).SendAsync(
            Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(),
            Arg.Any<CancellationToken>());
        queue.Count.ShouldBe(0);
        status.Current.Health.ShouldBe(AccessTelemetryHealthState.Unhealthy);
        status.Current.Reason.ShouldBe(AccessTelemetryReason.RecordIdConflict);
    }

    [Theory]
    [InlineData(AccessTelemetryReason.SchemaMismatch)]
    [InlineData(AccessTelemetryReason.Expired)]
    [InlineData(AccessTelemetryReason.ConfigurationInvalid)]
    [InlineData(AccessTelemetryReason.RecordIdConflict)]
    public async Task Worker_TerminalRejectionAcknowledgesTheExactRejectedPrefix(
        AccessTelemetryReason reason)
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
            .Returns(new AccessTelemetryWriteBatchResponse { Accepted = 0, Rejected = 3, Reason = reason });
        var accounting = new AccessTelemetryQualificationAccounting();
        var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(enabled: true),
            accounting);

        await worker.DrainOnceAsync(CancellationToken.None);

        queue.Count.ShouldBe(0);
        accounting.Current.Rejected.ShouldBe(3);
        accounting.Current.Conflicted.ShouldBe(reason == AccessTelemetryReason.RecordIdConflict ? 3 : 0);
    }

    [Fact]
    public async Task Worker_ClockUntrustedLeavesRecordsQueuedForRetry()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        AccessTelemetryRecord template = Sanitize(CreateSearchEvent());
        queue.TryEnqueue(Reidentify(template), out _).ShouldBeTrue();
        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(
                new AccessTelemetryWriteBatchResponse
                {
                    Accepted = 0,
                    Rejected = 1,
                    Reason = AccessTelemetryReason.ClockUntrusted,
                },
                new AccessTelemetryWriteBatchResponse
                {
                    Accepted = 1,
                    Rejected = 0,
                    Reason = AccessTelemetryReason.None,
                });
        var accounting = new AccessTelemetryQualificationAccounting();
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions(),
            status,
            accounting);

        await worker.DrainOnceAsync(CancellationToken.None);

        queue.Count.ShouldBe(1);
        accounting.Current.Rejected.ShouldBe(0);
        accounting.Current.Persisted.ShouldBe(0);
        status.Current.Health.ShouldBe(AccessTelemetryHealthState.Degraded);
        status.Current.Reason.ShouldBe(AccessTelemetryReason.ClockUntrusted);

        await worker.DrainOnceAsync(CancellationToken.None);

        queue.Count.ShouldBe(0);
        accounting.Current.Persisted.ShouldBe(1);
        accounting.Current.Rejected.ShouldBe(0);
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

    private static SignedClockAttestation CreateAttestation(DateTimeOffset? issuedAt = null)
    {
        DateTimeOffset instant = issuedAt ?? Now;
        return new()
        {
            DeploymentId = "deployment-a",
            AppId = "memories",
            ServiceInstanceId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XB",
            ProcessEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XC",
            ComponentProfileHash = new string('a', 64),
            RequestingProcessEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            RequestingServiceInstanceId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XA",
            Nonce = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XD",
            NotBeforeUnixMilliseconds = instant.AddMilliseconds(-10).ToUnixTimeMilliseconds(),
            NotAfterUnixMilliseconds = instant.AddMilliseconds(10).ToUnixTimeMilliseconds(),
            IssuedAtUnixMilliseconds = instant.ToUnixTimeMilliseconds(),
            ExpiresAtUnixMilliseconds = instant.AddSeconds(30).ToUnixTimeMilliseconds(),
            SignerKeyEpoch = "clock-key-1",
            Signature = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        };
    }

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
