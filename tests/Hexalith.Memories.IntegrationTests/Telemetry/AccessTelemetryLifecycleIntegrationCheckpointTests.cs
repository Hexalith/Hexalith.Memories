// <copyright file="AccessTelemetryLifecycleIntegrationCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>Story 27.2 C6 portable runtime, outage, topology, tenant, and privacy evidence.</summary>
[Trait("Category", "Integration")]
public sealed class AccessTelemetryLifecycleIntegrationCheckpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TwoServerWriters_ProduceUniqueRecordsWithoutCrossTenantMarkerMixupOrRawStorage()
    {
        byte[] markerKey = RandomNumberGenerator.GetBytes(32);
        AccessTelemetrySanitizer writerA = CreateSanitizer(markerKey);
        AccessTelemetrySanitizer writerB = CreateSanitizer(markerKey);

        AccessTelemetryRecord tenantAFromWriterA = Sanitize(writerA, CreateSearchEvent("tenant-a", "alice@example.test"));
        AccessTelemetryRecord tenantAFromWriterB = Sanitize(writerB, CreateSearchEvent("tenant-a", "alice@example.test"));
        AccessTelemetryRecord tenantB = Sanitize(writerA, CreateSearchEvent("tenant-b", "bob@example.test"));
        AccessTelemetryRecord rejected = Sanitize(writerB, CreateSearchEvent("__rejected__", "unknown@example.test"));

        tenantAFromWriterA.RecordId.ShouldNotBe(tenantAFromWriterB.RecordId);
        tenantAFromWriterA.TenantMarker.ShouldBe(tenantAFromWriterB.TenantMarker);
        tenantAFromWriterA.TenantMarker.ShouldNotBe(tenantB.TenantMarker);
        rejected.TenantMarker.ShouldBe("__rejected__");
        rejected.UserMarker.ShouldBeNull();
        rejected.CaseMarker.ShouldBeNull();
        string canonical = Encoding.UTF8.GetString(AccessTelemetryCanonicalizer.CanonicalizeRecord(tenantAFromWriterA));
        canonical.ShouldNotContain("tenant-a", Shouldly.Case.Sensitive);
        canonical.ShouldNotContain("alice@example.test", Shouldly.Case.Sensitive);
        canonical.ShouldNotContain("portable lifecycle raw query", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void AdmissionAt250EventsPerSecond_IsByteBoundedAndDropsNewestAtQueueFull()
    {
        AccessTelemetryRecord record = Sanitize(
            CreateSanitizer(RandomNumberGenerator.GetBytes(32)),
            CreateSearchEvent("tenant-a", "writer@example.test"));
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length;
        var queue = new BoundedAccessTelemetryQueue(250, bytes * 250);
        int accepted = 0;
        int dropped = 0;

        for (int index = 0; index < 500; index++)
        {
            AccessTelemetryRecord unique = Reidentify(record);
            if (queue.TryEnqueue(unique, out AccessTelemetryReason reason))
            {
                accepted++;
            }
            else
            {
                reason.ShouldBe(AccessTelemetryReason.QueueFull);
                dropped++;
            }
        }

        accepted.ShouldBe(250);
        dropped.ShouldBe(250);
        queue.Count.ShouldBe(250);
        queue.ByteCount.ShouldBeLessThanOrEqualTo(bytes * 250);
    }

    [Fact]
    public async Task TemporarySixtySecondOutage_RecoversAndFiveMinuteRetryAgeStopsOldWork()
    {
        var clock = new FakeTimeProvider(Now);
        var queue = new BoundedAccessTelemetryQueue(16, 64 * 1024);
        AccessTelemetryRecord first = Sanitize(
            CreateSanitizer(RandomNumberGenerator.GetBytes(32), clock),
            CreateSearchEvent("tenant-a", "writer@example.test"));
        queue.TryEnqueue(first, out _).ShouldBeTrue();
        var client = new ScriptedDeliveryClient(failuresBeforeSuccess: 1);
        var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            clock,
            new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(enabled: true));

        await worker.DrainOnceAsync(CancellationToken.None);
        queue.Count.ShouldBe(1);
        clock.Advance(TimeSpan.FromSeconds(60));
        await worker.DrainOnceAsync(CancellationToken.None);
        queue.Count.ShouldBe(0);

        AccessTelemetryRecord old = Reidentify(first);
        queue.TryEnqueue(old, out _).ShouldBeTrue();
        clock.SetUtcNow(DateTimeOffset.Parse(old.EmittedAtUtc, System.Globalization.CultureInfo.InvariantCulture).AddMinutes(5));
        await worker.DrainOnceAsync(CancellationToken.None);
        queue.Count.ShouldBe(0);
        client.SuccessfulBatches.ShouldBe(1);
    }

    [Fact]
    public async Task FiveHundredComponentOperationsWhilePurgeRuns_PreserveNewerRecordsAndAtomicPairs()
    {
        var clock = new FakeTimeProvider(Now.AddMinutes(-10));
        var store = new InMemoryAccessTelemetryStateStore();
        var processor = new AccessTelemetryLifecycleProcessor(
            store,
            clock,
            new AccessTelemetryOptions { Retention = TimeSpan.FromMinutes(5) });
        AccessTelemetryRecord due = CreateCanonicalRecord(clock.GetUtcNow().AddSeconds(-1), Now.AddMinutes(-1), "tenant-due");
        (await processor.PersistAsync(due, CancellationToken.None)).Status.ShouldBe(AccessTelemetryPersistenceStatus.Inserted);
        clock.SetUtcNow(Now);
        AccessTelemetryRecord template = CreateCanonicalRecord(Now.AddSeconds(-1), Now.AddHours(1), "tenant-live");

        Task<AccessTelemetryPurgeResult> purge = processor.PurgeAsync(CancellationToken.None);
        Task<AccessTelemetryPersistenceResult>[] writes = Enumerable.Range(0, 500)
            .Select(_ => processor.PersistAsync(Reidentify(template), CancellationToken.None))
            .ToArray();
        await Task.WhenAll(writes);
        AccessTelemetryPurgeResult purgeResult = await purge;

        writes.ShouldAllBe(task => task.Result.Status == AccessTelemetryPersistenceStatus.Inserted);
        purgeResult.Purged.ShouldBe(1);
        store.RecordCount.ShouldBe(500);
        store.IndexCount.ShouldBe(500);
        // Every atomic write carries record, bucket, and the permanent catalog ETag fence.
        IReadOnlyList<int> committed = store.TransactionOperationCounts;
        committed.Count.ShouldBe(501);
        committed.ShouldAllBe(static count => count == 3);
    }

    [Fact]
    public async Task TransientTransactionFailureAndRestartedProcessor_RetryIdempotentlyAndRecoverPurge()
    {
        var clock = new FakeTimeProvider(Now.AddMinutes(-10));
        var durableStore = new InMemoryAccessTelemetryStateStore();
        var transientStore = new FailFirstStateStore(durableStore);
        var testOptions = new AccessTelemetryOptions { Retention = TimeSpan.FromMinutes(5) };
        var firstProcessor = new AccessTelemetryLifecycleProcessor(transientStore, clock, testOptions);
        AccessTelemetryRecord record = CreateCanonicalRecord(clock.GetUtcNow().AddSeconds(-1), Now.AddMinutes(-1), "tenant-a");

        await Should.ThrowAsync<InvalidOperationException>(() => firstProcessor.PersistAsync(record, CancellationToken.None));
        (await firstProcessor.PersistAsync(record, CancellationToken.None)).Status.ShouldBe(AccessTelemetryPersistenceStatus.Inserted);
        (await firstProcessor.PersistAsync(record, CancellationToken.None)).Status.ShouldBe(AccessTelemetryPersistenceStatus.Idempotent);

        clock.SetUtcNow(Now);
        var restartedProcessor = new AccessTelemetryLifecycleProcessor(durableStore, clock, testOptions);
        AccessTelemetryPurgeResult recovered = await restartedProcessor.PurgeAsync(CancellationToken.None);
        recovered.Purged.ShouldBe(1);
        recovered.VerifiedAbsent.ShouldBe(1);
        durableStore.RecordCount.ShouldBe(0);
        durableStore.IndexCount.ShouldBe(0);
    }

    [Fact]
    public void LifecycleBootstrapFailure_DoesNotInterruptExistingConsoleProviderOrBusinessLogging()
    {
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        var accessor = new AccessTelemetrySanitizerAccessor();
        using var lifecycleProvider = new AccessTelemetryLifecycleLoggerProvider(queue, accessor);
        using var existingProvider = new CollectingLoggerProvider();
        using ILoggerFactory factory = LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddProvider(existingProvider);
            logging.AddProvider(lifecycleProvider);
        });
        ILogger logger = factory.CreateLogger(typeof(AccessTelemetryCategory).FullName!);

        Should.NotThrow(() => logger.Log(
            LogLevel.Information,
            new EventId(7501),
            CreateSearchEvent("tenant-a", "writer@example.test"),
            null,
            static (_, _) => "existing-json-console-equivalent"));

        existingProvider.Count.ShouldBe(1);
        queue.Count.ShouldBe(0);
    }

    [Fact]
    public void KubernetesDaprComponentScopes_AreExplicitAndLeastPrivilege()
    {
        string root = FindRepositoryRoot();
        string lifecycleAcl = File.ReadAllText(Path.Combine(root, "deploy", "kubernetes", "base", "dapr", "access-telemetry-lifecycle-config.yaml"));
        string stateComponent = File.ReadAllText(Path.Combine(root, "deploy", "kubernetes", "base", "dapr", "access-telemetry-store.yaml"));

        lifecycleAcl.ShouldContain("appId: memories");
        lifecycleAcl.ShouldContain("appId: memories-access-telemetry-inspector");
        lifecycleAcl.ShouldNotContain("/v1/access-telemetry/inspect\n            httpVerb: [\"POST\"]");
        stateComponent.ShouldContain("- memories-access-telemetry");
        stateComponent.ShouldNotContain("- memories\n");
    }

    [Fact]
    public void ProductionOverlay_RequiresUnprovenProfileToRemainDisabledAndLeavesPhysicalProofPending()
    {
        string root = FindRepositoryRoot();
        string overlay = File.ReadAllText(Path.Combine(root, "deploy", "kubernetes", "overlays", "production", "kustomization.yaml"));
        string patch = File.ReadAllText(Path.Combine(root, "deploy", "kubernetes", "overlays", "production", "access-telemetry-disabled-patch.yaml"));
        string actorState = File.ReadAllText(Path.Combine(root, "src", "Hexalith.Memories.AccessTelemetry", "Lifecycle", "AccessTelemetryLifecycleActorState.cs"));

        overlay.ShouldContain("ACCESS_TELEMETRY_ENABLED=false");
        overlay.ShouldContain("ACCESS_TELEMETRY_COMPONENT_PROFILE_HASH=unproven");
        patch.ShouldContain("required-dapr-configuration: access-telemetry-config/retentionSeconds");
        patch.ShouldContain("disabled-pending-story-27-3");
        actorState.ShouldContain("pending-story-27-3");
    }

    private static AccessTelemetrySanitizer CreateSanitizer(byte[] markerKey, TimeProvider? clock = null)
        => new(
            markerKey,
            "mk-2026a",
            clock ?? new FakeTimeProvider(Now),
            new MonotonicRecordIdGenerator(),
            TimeSpan.FromHours(24));

    private static AccessTelemetryRecord Sanitize(AccessTelemetrySanitizer sanitizer, AccessTelemetryEvent source)
    {
        sanitizer.TrySanitize(
            LogLevel.Information,
            new EventId(7501),
            source,
            out AccessTelemetryRecord? record,
            out AccessTelemetryReason reason).ShouldBeTrue(reason.ToString());
        return record!;
    }

    private static AccessTelemetryEvent CreateSearchEvent(string tenantId, string user)
        => new()
        {
            EventId = 7501,
            Timestamp = Now.AddSeconds(-1).ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            TenantId = tenantId,
            OperationType = "search",
            CaseId = null,
            User = user,
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["query"] = "portable lifecycle raw query",
            },
            ResultCount = 2,
            DurationMs = 10,
            Outcome = "ok",
        };

    private static AccessTelemetryRecord CreateCanonicalRecord(
        DateTimeOffset emittedAt,
        DateTimeOffset expiresAt,
        string tenantSeed)
    {
        AccessTelemetryRecord record = new()
        {
            AcceptedAtUtc = Format(Now),
            CaseMarker = null,
            DurationMs = 1,
            EmittedAtUtc = Format(emittedAt),
            EnvelopeHash = string.Empty,
            ErrorCode = null,
            EventId = 7501,
            ExpiresAtUtc = Format(expiresAt),
            MarkerKeyId = "mk-2026a",
            OperationType = "search",
            Outcome = "ok",
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "all-authorized",
                ["explain"] = false,
                ["queryLengthBucket"] = "33-128",
                ["subjectPresent"] = false,
                ["weightProfile"] = "configured",
            },
            RecordId = new MonotonicRecordIdGenerator().NewId(),
            ResultCount = 1,
            SchemaVersion = 1,
            SpanId = null,
            TenantMarker = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tenantSeed))).ToLowerInvariant(),
            TraceId = null,
            UserMarker = null,
        };
        return Rehash(record);
    }

    private static AccessTelemetryRecord Reidentify(AccessTelemetryRecord record)
    {
        AccessTelemetryRecord identified = record with { RecordId = new MonotonicRecordIdGenerator().NewId(), EnvelopeHash = string.Empty };
        return Rehash(identified);
    }

    private static AccessTelemetryRecord Rehash(AccessTelemetryRecord record)
        => record with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(record) };

    private static string Format(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Hexalith.Memories.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private sealed class ScriptedDeliveryClient(int failuresBeforeSuccess) : IAccessTelemetryDeliveryClient
    {
        private int _remainingFailures = failuresBeforeSuccess;

        public int SuccessfulBatches { get; private set; }

        public Task<AccessTelemetryWriteBatchResponse> SendAsync(
            IReadOnlyList<AccessTelemetryRecord> records,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_remainingFailures-- > 0)
            {
                throw new HttpRequestException("temporary outage");
            }

            SuccessfulBatches++;
            return Task.FromResult(new AccessTelemetryWriteBatchResponse
            {
                Accepted = records.Count,
                Rejected = 0,
                Reason = AccessTelemetryReason.None,
            });
        }
    }

    private sealed class FailFirstStateStore(IAccessTelemetryStateStore inner) : IAccessTelemetryStateStore
    {
        private bool _failed;

        public Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
            AccessTelemetryRecord record,
            AccessTelemetryExpiryEntry expiryEntry,
            int ttlInSeconds,
            CancellationToken cancellationToken)
        {
            if (!_failed)
            {
                _failed = true;
                throw new InvalidOperationException("transient transaction failure");
            }

            return inner.WriteRecordAndIndexAsync(record, expiryEntry, ttlInSeconds, cancellationToken);
        }

        public Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Entries, bool HasMoreDueEntries)> GetDueEntriesAsync(
            long dueMinute,
            int limit,
            CancellationToken cancellationToken)
            => inner.GetDueEntriesAsync(dueMinute, limit, cancellationToken);

        public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
            AccessTelemetryExpiryEntry entry,
            CancellationToken cancellationToken)
            => inner.DeleteAndVerifyAsync(entry, cancellationToken);
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public int Count { get; private set; }

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(this);

        public void Dispose()
        {
        }

        private sealed class CollectingLogger(CollectingLoggerProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => owner.Count++;
        }
    }
}
