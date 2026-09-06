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

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>Story 27.2 C6 portable runtime, outage, topology, tenant, and privacy evidence.</summary>
[Trait("Category", "Integration")]
public sealed class AccessTelemetryLifecycleIntegrationCheckpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TwoServerWriters_ProduceUniqueRecordsWithoutCrossTenantMarkerMixupOrRawStorage()
    {
        byte[] markerKey = Enumerable.Range(1, 32).Select(static value => checked((byte)value)).ToArray();
        var clockA = new FakeTimeProvider(Now);
        var clockB = new FakeTimeProvider(Now.AddMilliseconds(1));
        var generatorA = new MonotonicRecordIdGenerator();
        var generatorB = new MonotonicRecordIdGenerator();
        var writerA = new AccessTelemetrySanitizer(markerKey, "mk-2026a", clockA, generatorA, TimeSpan.FromHours(24));
        var writerB = new AccessTelemetrySanitizer(markerKey, "mk-2026a", clockB, generatorB, TimeSpan.FromHours(24));
        AccessTelemetryRecord tenantA = Sanitize(writerA, CreateSearchEvent("tenant-a", "alice@example.test"));
        AccessTelemetryRecord tenantAFromWriterB = Sanitize(writerB, CreateSearchEvent("tenant-a", "alice@example.test"));
        AccessTelemetryRecord tenantB = Sanitize(writerB, CreateSearchEvent("tenant-b", "bob@example.test"));
        var queueA = new BoundedAccessTelemetryQueue(1, AccessTelemetryOptions.MaximumRecordBytes);
        var queueB = new BoundedAccessTelemetryQueue(1, AccessTelemetryOptions.MaximumRecordBytes);
        queueA.TryEnqueue(tenantA, out AccessTelemetryReason reasonA).ShouldBeTrue(reasonA.ToString());
        queueB.TryEnqueue(tenantB, out AccessTelemetryReason reasonB).ShouldBeTrue(reasonB.ToString());

        var durableStore = new InMemoryAccessTelemetryStateStore();
        var store = new CoordinatedAccessTelemetryStateStore(durableStore);
        store.ArmConcurrentWriteRendezvous();
        var clientA = new LifecycleProcessBoundaryDeliveryClient(
            "writer-a",
            new AccessTelemetryLifecycleProcessor(store, clockA, new AccessTelemetryOptions { Retention = TimeSpan.FromHours(24) }));
        var clientB = new LifecycleProcessBoundaryDeliveryClient(
            "writer-b",
            new AccessTelemetryLifecycleProcessor(store, clockB, new AccessTelemetryOptions { Retention = TimeSpan.FromHours(24) }));
        var workerA = new AccessTelemetryDeliveryWorker(
            queueA,
            clientA,
            clockA,
            new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(enabled: true));
        var workerB = new AccessTelemetryDeliveryWorker(
            queueB,
            clientB,
            clockB,
            new AccessTelemetryOptions(),
            new AccessTelemetryLifecycleStatus(enabled: true));

        await Task.WhenAll(
            workerA.DrainOnceAsync(TestContext.Current.CancellationToken),
            workerB.DrainOnceAsync(TestContext.Current.CancellationToken));

        store.ConcurrentWriteOverlapObserved.ShouldBeTrue();
        clientA.BoundaryId.ShouldNotBe(clientB.BoundaryId);
        clientA.ReceivedRecords.ShouldHaveSingleItem().RecordId.ShouldBe(tenantA.RecordId);
        clientB.ReceivedRecords.ShouldHaveSingleItem().RecordId.ShouldBe(tenantB.RecordId);
        tenantA.RecordId.ShouldNotBe(tenantB.RecordId);
        tenantA.RecordId.ShouldNotBe(tenantAFromWriterB.RecordId);
        tenantA.TenantMarker.ShouldBe(tenantAFromWriterB.TenantMarker);
        tenantA.UserMarker.ShouldBe(tenantAFromWriterB.UserMarker);
        tenantA.TenantMarker.ShouldNotBe(tenantB.TenantMarker);
        tenantA.UserMarker.ShouldNotBe(tenantB.UserMarker);
        queueA.Count.ShouldBe(0);
        queueB.Count.ShouldBe(0);
        durableStore.RecordCount.ShouldBe(2);
        durableStore.IndexCount.ShouldBe(2);
        durableStore.TransactionOperationCounts.Count.ShouldBe(2);
        durableStore.TransactionOperationCounts.ShouldAllBe(static count => count == 3);

        AccessTelemetryRecord persistedA = durableStore.GetRecord(tenantA.RecordId).ShouldNotBeNull();
        AccessTelemetryRecord persistedB = durableStore.GetRecord(tenantB.RecordId).ShouldNotBeNull();
        persistedA.TenantMarker.ShouldBe(tenantA.TenantMarker);
        persistedB.TenantMarker.ShouldBe(tenantB.TenantMarker);
        persistedA.UserMarker.ShouldBe(tenantA.UserMarker);
        persistedB.UserMarker.ShouldBe(tenantB.UserMarker);
        string canonical = Encoding.UTF8.GetString(
            AccessTelemetryCanonicalizer.CanonicalizeRecord(persistedA)
                .Concat(AccessTelemetryCanonicalizer.CanonicalizeRecord(persistedB))
                .ToArray());
        canonical.ShouldNotContain("tenant-a", Shouldly.Case.Sensitive);
        canonical.ShouldNotContain("tenant-b", Shouldly.Case.Sensitive);
        canonical.ShouldNotContain("alice@example.test", Shouldly.Case.Sensitive);
        canonical.ShouldNotContain("bob@example.test", Shouldly.Case.Sensitive);
        canonical.ShouldNotContain("portable lifecycle raw query", Shouldly.Case.Sensitive);
    }

    [Fact]
    public async Task AdmissionAt250EventsPerSecond_IsByteBoundedAndDropsNewestAtQueueFull()
    {
        var clock = new FakeTimeProvider(Now);
        DateTimeOffset startedAt = clock.GetUtcNow();
        TimeSpan admissionInterval = TimeSpan.FromMilliseconds(4);
        AccessTelemetryRecord record = Sanitize(
            CreateSanitizer(RandomNumberGenerator.GetBytes(32), clock),
            CreateSearchEvent("tenant-a", "writer@example.test"));
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length;
        var queue = new BoundedAccessTelemetryQueue(500, bytes * 250);
        var attemptTimes = new List<DateTimeOffset>(capacity: 500);
        var attemptedRecordIds = new List<string>(capacity: 500);
        int accepted = 0;
        int dropped = 0;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var timer = new PeriodicTimer(admissionInterval, clock);
        using var readyForTick = new SemaphoreSlim(initialCount: 0);
        using var attemptCompleted = new SemaphoreSlim(initialCount: 0);
        using var attemptAcknowledged = new SemaphoreSlim(initialCount: 0);

        Task producer = Task.Run(async () =>
        {
            for (int index = 0; index < 500; index++)
            {
                readyForTick.Release();
                (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)).ShouldBeTrue();
                AccessTelemetryRecord unique = Reidentify(record);
                attemptTimes.Add(clock.GetUtcNow());
                attemptedRecordIds.Add(unique.RecordId);
                if (queue.TryEnqueue(unique, out AccessTelemetryReason reason))
                {
                    accepted++;
                }
                else
                {
                    reason.ShouldBe(AccessTelemetryReason.QueueFull);
                    dropped++;
                }

                attemptCompleted.Release();
                await attemptAcknowledged.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);

        for (int index = 0; index < 500; index++)
        {
            await readyForTick.WaitAsync(cancellationToken);
            clock.Advance(admissionInterval);
            await attemptCompleted.WaitAsync(cancellationToken);
            attemptAcknowledged.Release();
        }

        await producer;

        (clock.GetUtcNow() - startedAt).ShouldBe(TimeSpan.FromSeconds(2));
        attemptTimes.Count(time => time > startedAt && time <= startedAt.AddSeconds(1)).ShouldBe(250);
        attemptTimes.Count(time => time > startedAt.AddSeconds(1) && time <= startedAt.AddSeconds(2)).ShouldBe(250);
        attemptTimes.Select((time, index) => time - startedAt == admissionInterval * (index + 1)).ShouldAllBe(static exact => exact);
        accepted.ShouldBe(250);
        dropped.ShouldBe(250);
        queue.Count.ShouldBe(250);
        queue.ByteCount.ShouldBe(bytes * 250);
        queue.PeekBatch(250, bytes * 250)
            .Select(static queued => queued.RecordId)
            .ShouldBe(attemptedRecordIds.Take(250));
    }

    [Fact]
    public async Task TemporarySixtySecondOutage_RecoversAndFiveMinuteRetryAgeStopsOldWork()
    {
        var clock = new ObservedFakeTimeProvider(Now);
        var queue = new BoundedAccessTelemetryQueue(16, 64 * 1024);
        AccessTelemetryRecord first = Sanitize(
            CreateSanitizer(RandomNumberGenerator.GetBytes(32), clock),
            CreateSearchEvent("tenant-a", "writer@example.test"));
        queue.TryEnqueue(first, out _).ShouldBeTrue();
        var client = new TimedOutageAccessTelemetryDeliveryClient(clock, Now.AddSeconds(60));
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        TimeSpan retryDelay = TimeSpan.FromSeconds(5);
        var options = new AccessTelemetryOptions
        {
            Enabled = true,
            Retention = TimeSpan.FromHours(24),
            RetentionSource = RetentionConfigurationSource.DevelopmentDefault,
            DeploymentId = "lifecycle-checkpoint",
            ConfigurationEpoch = "01J00000000000000000000000",
            ComponentProfileHash = new string('a', 64),
            AttestationVerificationKey = "integration-test-key",
            MarkerKeyReference = "access-telemetry-marker-key",
            MarkerKeyGeneration = "mk-2026a",
            CapacityEvidenceId = "lifecycle-checkpoint-capacity",
            PhysicalReclamationEvidenceId = "pending-story-27-3",
            PhysicalReclamationReporterImageDigest = new string('d', 64),
            RetryInitialDelay = retryDelay,
            RetryMaximumDelay = retryDelay,
        };
        AccessTelemetryOptionsValidationResult validation = AccessTelemetryOptionsValidator.Validate(options, "Development");
        validation.IsValid.ShouldBeTrue(string.Join("; ", validation.Errors));
        validation.AllowsLifecycleWrites.ShouldBeTrue();
        using var worker = new AccessTelemetryDeliveryWorker(
            queue,
            client,
            clock,
            options,
            status);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await worker.StartAsync(cancellationToken);
        await client.WaitForNextAttemptAsync(cancellationToken);
        for (int attemptIndex = 0; attemptIndex < 13; attemptIndex++)
        {
            (DateTimeOffset CreatedAt, TimeSpan DueTime, TimeSpan Period) timerRequest =
                await clock.WaitForTimerCreationAsync(cancellationToken);
            DateTimeOffset expectedAttempt = Now.Add(retryDelay * attemptIndex);
            timerRequest.CreatedAt.ShouldBe(expectedAttempt);
            timerRequest.DueTime.ShouldBe(retryDelay);
            timerRequest.Period.ShouldBe(Timeout.InfiniteTimeSpan);
            client.AttemptTimes[^1].ShouldBe(expectedAttempt);
            client.AttemptRecordIdBatches[^1].ShouldHaveSingleItem().ShouldBe(first.RecordId);
            queue.Count.ShouldBe(expectedAttempt < Now.AddSeconds(60) ? 1 : 0);
            status.Current.Health.ShouldBe(
                expectedAttempt < Now.AddSeconds(60)
                    ? AccessTelemetryHealthState.Degraded
                    : AccessTelemetryHealthState.Healthy);

            if (attemptIndex < 12)
            {
                clock.Advance(retryDelay);
                await client.WaitForNextAttemptAsync(cancellationToken);
            }
        }

        client.AttemptTimes.ShouldBe(Enumerable.Range(0, 13).Select(static index => Now.AddSeconds(index * 5)));
        client.AttemptTimes.Take(12).ShouldAllBe(attemptedAt => attemptedAt < Now.AddSeconds(60));
        client.AttemptTimes[^1].ShouldBe(Now.AddSeconds(60));
        client.AttemptRecordIdBatches.Count.ShouldBe(13);
        client.AttemptRecordIdBatches.ShouldAllBe(batch => batch.Count == 1 && batch[0] == first.RecordId);
        client.FailedBatches.ShouldBe(12);
        client.SuccessfulBatches.ShouldBe(1);
        queue.Count.ShouldBe(0);
        status.Current.Health.ShouldBe(AccessTelemetryHealthState.Healthy);
        status.Current.LastAcceptedOrRejectedUtc.ShouldBe(Now.AddSeconds(60));
        await worker.StopAsync(cancellationToken);

        DateTimeOffset emittedAt = DateTimeOffset.Parse(first.EmittedAtUtc, System.Globalization.CultureInfo.InvariantCulture);
        AccessTelemetryRecord justBeforeAgeCap = Reidentify(first);
        queue.TryEnqueue(justBeforeAgeCap, out _).ShouldBeTrue();
        clock.SetUtcNow(emittedAt.Add(AccessTelemetryOptions.MaximumRetryAge).AddMilliseconds(-1));
        await worker.DrainOnceAsync(cancellationToken);
        queue.Count.ShouldBe(0);
        client.SuccessfulBatches.ShouldBe(2);
        client.AttemptTimes.Count.ShouldBe(14);
        client.AttemptRecordIdBatches[^1].ShouldHaveSingleItem().ShouldBe(justBeforeAgeCap.RecordId);

        AccessTelemetryRecord atAgeCap = Reidentify(first);
        queue.TryEnqueue(atAgeCap, out _).ShouldBeTrue();
        clock.SetUtcNow(emittedAt.Add(AccessTelemetryOptions.MaximumRetryAge));
        await worker.DrainOnceAsync(cancellationToken);
        queue.Count.ShouldBe(0);
        client.SuccessfulBatches.ShouldBe(2);
        client.AttemptTimes.Count.ShouldBe(14);
    }

    [Fact]
    public async Task FiveHundredComponentOperationsWhilePurgeRuns_PreserveNewerRecordsAndAtomicPairs()
    {
        var clock = new FakeTimeProvider(Now.AddMinutes(-10));
        var durableStore = new InMemoryAccessTelemetryStateStore();
        var innerOperationGate = new InnerOperationOverlapStateStore(durableStore);
        var store = new CoordinatedAccessTelemetryStateStore(innerOperationGate);
        var processor = new AccessTelemetryLifecycleProcessor(
            store,
            clock,
            new AccessTelemetryOptions { Retention = TimeSpan.FromMinutes(5) });
        AccessTelemetryRecord due = CreateCanonicalRecord(clock.GetUtcNow().AddSeconds(-1), Now.AddMinutes(-1), "tenant-due");
        (await processor.PersistAsync(due, CancellationToken.None)).Status.ShouldBe(AccessTelemetryPersistenceStatus.Inserted);
        clock.SetUtcNow(Now);
        AccessTelemetryRecord template = CreateCanonicalRecord(Now.AddSeconds(-1), Now.AddHours(1), "tenant-live");
        AccessTelemetryRecord[] liveRecords = Enumerable.Range(0, 500)
            .Select(_ => Reidentify(template))
            .ToArray();
        innerOperationGate.ArmPurgeWriteRendezvous();

        Task<AccessTelemetryPurgeResult> purge = processor.PurgeAsync(TestContext.Current.CancellationToken);
        Task<AccessTelemetryPersistenceResult>[] writes = liveRecords
            .Select(record => processor.PersistAsync(record, TestContext.Current.CancellationToken))
            .ToArray();
        await Task.WhenAll(writes);
        AccessTelemetryPurgeResult purgeResult = await purge;

        innerOperationGate.DueReadEntered.ShouldBeTrue();
        innerOperationGate.WriteEntered.ShouldBeTrue();
        innerOperationGate.OverlapObserved.ShouldBeTrue();
        writes.ShouldAllBe(task => task.Result.Status == AccessTelemetryPersistenceStatus.Inserted);
        purgeResult.Purged.ShouldBe(1);
        durableStore.RecordCount.ShouldBe(500);
        durableStore.IndexCount.ShouldBe(500);
        durableStore.ContainsRecord(due.RecordId).ShouldBeFalse();

        AccessTelemetryRecord[] committedLiveRecords = store.CommittedRecords
            .Where(record => !string.Equals(record.RecordId, due.RecordId, StringComparison.Ordinal))
            .OrderBy(static record => record.RecordId, StringComparer.Ordinal)
            .ToArray();
        committedLiveRecords.Length.ShouldBe(500);
        committedLiveRecords.Select(static record => record.RecordId).ShouldBe(
            liveRecords.Select(static record => record.RecordId).Order(StringComparer.Ordinal));
        foreach (AccessTelemetryRecord committedLive in committedLiveRecords)
        {
            durableStore.GetRecord(committedLive.RecordId).ShouldBe(committedLive);
        }

        AccessTelemetryExpiryEntry[] committedLiveEntries = store.CommittedExpiryEntries
            .Where(entry => !string.Equals(entry.RecordId, due.RecordId, StringComparison.Ordinal))
            .OrderBy(static entry => entry.ExpiresAtUtc, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Shard)
            .ThenBy(static entry => entry.RecordId, StringComparer.Ordinal)
            .ToArray();
        committedLiveEntries.Length.ShouldBe(500);
        (IReadOnlyList<AccessTelemetryExpiryEntry> retainedEntries, bool hasMore) =
            await durableStore.GetDueEntriesAsync(committedLiveEntries[^1].ExpiryMinute, 501, CancellationToken.None);
        hasMore.ShouldBeFalse();
        retainedEntries.ShouldBe(committedLiveEntries);
        // Every atomic write carries record, bucket, and the permanent catalog ETag fence.
        IReadOnlyList<int> committed = durableStore.TransactionOperationCounts;
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

        Should.NotThrow(() => AccessTelemetryYamlLeastPrivilegeValidator.Validate(lifecycleAcl, stateComponent));

        YamlMappingNode reorderedAcl = LoadYaml(lifecycleAcl);
        ReverseSequence(GetPolicies(reorderedAcl));
        foreach (YamlMappingNode policy in GetPolicies(reorderedAcl).Children.Cast<YamlMappingNode>())
        {
            ReverseSequence((YamlSequenceNode)policy.Children[new YamlScalarNode("operations")]);
        }

        Should.NotThrow(() => AccessTelemetryYamlLeastPrivilegeValidator.Validate(
            SerializeYaml(reorderedAcl),
            SerializeYaml(LoadYaml(stateComponent))));

        YamlMappingNode wildcardAcl = LoadYaml(lifecycleAcl);
        ((YamlMappingNode)GetPolicies(wildcardAcl).Children[0]).Children[new YamlScalarNode("appId")] = new YamlScalarNode("*");
        Should.Throw<InvalidDataException>(() => Validate(wildcardAcl, LoadYaml(stateComponent)));

        YamlMappingNode duplicateAcl = LoadYaml(lifecycleAcl);
        GetPolicies(duplicateAcl).Add(GetPolicies(LoadYaml(lifecycleAcl)).Children[0]);
        Should.Throw<InvalidDataException>(() => Validate(duplicateAcl, LoadYaml(stateComponent)));

        YamlMappingNode duplicateGrantAcl = LoadYaml(lifecycleAcl);
        GetOperations(duplicateGrantAcl, policyIndex: 0).Add(
            GetOperations(LoadYaml(lifecycleAcl), policyIndex: 0).Children[0]);
        Should.Throw<InvalidDataException>(() => Validate(duplicateGrantAcl, LoadYaml(stateComponent)));

        YamlMappingNode extraIdentityAcl = LoadYaml(lifecycleAcl);
        YamlMappingNode roguePolicy = (YamlMappingNode)GetPolicies(LoadYaml(lifecycleAcl)).Children[0];
        roguePolicy.Children[new YamlScalarNode("appId")] = new YamlScalarNode("rogue-inspector");
        GetPolicies(extraIdentityAcl).Add(roguePolicy);
        Should.Throw<InvalidDataException>(() => Validate(extraIdentityAcl, LoadYaml(stateComponent)));

        YamlMappingNode missingPolicyAcl = LoadYaml(lifecycleAcl);
        GetPolicies(missingPolicyAcl).Children.RemoveAt(1);
        Should.Throw<InvalidDataException>(() => Validate(missingPolicyAcl, LoadYaml(stateComponent)));

        YamlMappingNode extraGrantAcl = LoadYaml(lifecycleAcl);
        GetOperations(extraGrantAcl, policyIndex: 0).Add(new YamlMappingNode
        {
            { "name", "/v1/access-telemetry/rogue" },
            { "httpVerb", new YamlSequenceNode("POST") },
            { "action", "allow" },
        });
        Should.Throw<InvalidDataException>(() => Validate(extraGrantAcl, LoadYaml(stateComponent)));

        YamlMappingNode missingGrantAcl = LoadYaml(lifecycleAcl);
        GetOperations(missingGrantAcl, policyIndex: 0).Children.RemoveAt(0);
        Should.Throw<InvalidDataException>(() => Validate(missingGrantAcl, LoadYaml(stateComponent)));

        YamlMappingNode extraVerbAcl = LoadYaml(lifecycleAcl);
        ((YamlSequenceNode)((YamlMappingNode)GetOperations(extraVerbAcl, policyIndex: 0).Children[0])
            .Children[new YamlScalarNode("httpVerb")]).Add("PUT");
        Should.Throw<InvalidDataException>(() => Validate(extraVerbAcl, LoadYaml(stateComponent)));

        YamlMappingNode duplicateVerbAcl = LoadYaml(lifecycleAcl);
        ((YamlSequenceNode)((YamlMappingNode)GetOperations(duplicateVerbAcl, policyIndex: 0).Children[0])
            .Children[new YamlScalarNode("httpVerb")]).Add("POST");
        Should.Throw<InvalidDataException>(() => Validate(duplicateVerbAcl, LoadYaml(stateComponent)));

        YamlMappingNode missingVerbAcl = LoadYaml(lifecycleAcl);
        ((YamlMappingNode)GetOperations(missingVerbAcl, policyIndex: 0).Children[0])
            .Children.Remove(new YamlScalarNode("httpVerb")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(missingVerbAcl, LoadYaml(stateComponent)));

        YamlMappingNode wrongActionAcl = LoadYaml(lifecycleAcl);
        ((YamlMappingNode)GetOperations(wrongActionAcl, policyIndex: 0).Children[0])
            .Children[new YamlScalarNode("action")] = new YamlScalarNode("deny");
        Should.Throw<InvalidDataException>(() => Validate(wrongActionAcl, LoadYaml(stateComponent)));

        YamlMappingNode missingActionAcl = LoadYaml(lifecycleAcl);
        ((YamlMappingNode)GetOperations(missingActionAcl, policyIndex: 0).Children[0])
            .Children.Remove(new YamlScalarNode("action")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(missingActionAcl, LoadYaml(stateComponent)));

        YamlMappingNode extraScope = LoadYaml(stateComponent);
        ((YamlSequenceNode)extraScope.Children[new YamlScalarNode("scopes")]).Add("memories");
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), extraScope));

        YamlMappingNode duplicateScope = LoadYaml(stateComponent);
        ((YamlSequenceNode)duplicateScope.Children[new YamlScalarNode("scopes")]).Add("memories-access-telemetry");
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), duplicateScope));

        YamlMappingNode missingScope = LoadYaml(stateComponent);
        ((YamlSequenceNode)missingScope.Children[new YamlScalarNode("scopes")]).Children.RemoveAt(0);
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), missingScope));

        YamlMappingNode wrongConfigurationApiVersion = LoadYaml(lifecycleAcl);
        wrongConfigurationApiVersion.Children[new YamlScalarNode("apiVersion")] = new YamlScalarNode("dapr.io/v1");
        Should.Throw<InvalidDataException>(() => Validate(wrongConfigurationApiVersion, LoadYaml(stateComponent)));

        YamlMappingNode wrongComponentApiVersion = LoadYaml(stateComponent);
        wrongComponentApiVersion.Children[new YamlScalarNode("apiVersion")] = new YamlScalarNode("dapr.io/v1");
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), wrongComponentApiVersion));

        YamlMappingNode wrongConfigurationKind = LoadYaml(lifecycleAcl);
        wrongConfigurationKind.Children[new YamlScalarNode("kind")] = new YamlScalarNode("Component");
        Should.Throw<InvalidDataException>(() => Validate(wrongConfigurationKind, LoadYaml(stateComponent)));

        YamlMappingNode missingConfigurationKind = LoadYaml(lifecycleAcl);
        missingConfigurationKind.Children.Remove(new YamlScalarNode("kind")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(missingConfigurationKind, LoadYaml(stateComponent)));

        YamlMappingNode wrongConfigurationName = LoadYaml(lifecycleAcl);
        GetMetadata(wrongConfigurationName).Children[new YamlScalarNode("name")] = new YamlScalarNode("rogue-config");
        Should.Throw<InvalidDataException>(() => Validate(wrongConfigurationName, LoadYaml(stateComponent)));

        YamlMappingNode missingConfigurationName = LoadYaml(lifecycleAcl);
        GetMetadata(missingConfigurationName).Children.Remove(new YamlScalarNode("name")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(missingConfigurationName, LoadYaml(stateComponent)));

        YamlMappingNode wrongAccessDefault = LoadYaml(lifecycleAcl);
        GetAccessControl(wrongAccessDefault).Children[new YamlScalarNode("defaultAction")] = new YamlScalarNode("allow");
        Should.Throw<InvalidDataException>(() => Validate(wrongAccessDefault, LoadYaml(stateComponent)));

        YamlMappingNode missingAccessDefault = LoadYaml(lifecycleAcl);
        GetAccessControl(missingAccessDefault).Children.Remove(new YamlScalarNode("defaultAction")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(missingAccessDefault, LoadYaml(stateComponent)));

        YamlMappingNode wrongAccessTrustDomain = LoadYaml(lifecycleAcl);
        GetAccessControl(wrongAccessTrustDomain).Children[new YamlScalarNode("trustDomain")] = new YamlScalarNode("rogue");
        Should.Throw<InvalidDataException>(() => Validate(wrongAccessTrustDomain, LoadYaml(stateComponent)));

        YamlMappingNode missingAccessTrustDomain = LoadYaml(lifecycleAcl);
        GetAccessControl(missingAccessTrustDomain).Children.Remove(new YamlScalarNode("trustDomain")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(missingAccessTrustDomain, LoadYaml(stateComponent)));

        foreach (string policyField in new[] { "namespace", "trustDomain", "defaultAction" })
        {
            YamlMappingNode wrongPolicy = LoadYaml(lifecycleAcl);
            ((YamlMappingNode)GetPolicies(wrongPolicy).Children[0]).Children[new YamlScalarNode(policyField)] =
                new YamlScalarNode("rogue");
            Should.Throw<InvalidDataException>(() => Validate(wrongPolicy, LoadYaml(stateComponent)));

            YamlMappingNode missingPolicyField = LoadYaml(lifecycleAcl);
            ((YamlMappingNode)GetPolicies(missingPolicyField).Children[0])
                .Children.Remove(new YamlScalarNode(policyField)).ShouldBeTrue();
            Should.Throw<InvalidDataException>(() => Validate(missingPolicyField, LoadYaml(stateComponent)));
        }

        YamlMappingNode wrongComponentKind = LoadYaml(stateComponent);
        wrongComponentKind.Children[new YamlScalarNode("kind")] = new YamlScalarNode("Configuration");
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), wrongComponentKind));

        YamlMappingNode missingComponentKind = LoadYaml(stateComponent);
        missingComponentKind.Children.Remove(new YamlScalarNode("kind")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), missingComponentKind));

        YamlMappingNode wrongComponentName = LoadYaml(stateComponent);
        GetMetadata(wrongComponentName).Children[new YamlScalarNode("name")] = new YamlScalarNode("rogue-store");
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), wrongComponentName));

        YamlMappingNode missingComponentName = LoadYaml(stateComponent);
        GetMetadata(missingComponentName).Children.Remove(new YamlScalarNode("name")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), missingComponentName));

        foreach ((string componentField, string wrongValue) in new[]
        {
            ("type", "state.redis"),
            ("version", "v1"),
            ("initTimeout", "2m"),
        })
        {
            YamlMappingNode wrongComponentField = LoadYaml(stateComponent);
            GetComponentSpec(wrongComponentField).Children[new YamlScalarNode(componentField)] =
                new YamlScalarNode(wrongValue);
            Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), wrongComponentField));

            YamlMappingNode missingComponentField = LoadYaml(stateComponent);
            GetComponentSpec(missingComponentField).Children.Remove(new YamlScalarNode(componentField)).ShouldBeTrue();
            Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), missingComponentField));
        }

        YamlMappingNode wrongSecretStore = LoadYaml(stateComponent);
        GetAuth(wrongSecretStore).Children[new YamlScalarNode("secretStore")] = new YamlScalarNode("rogue-secrets");
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), wrongSecretStore));

        YamlMappingNode missingSecretStore = LoadYaml(stateComponent);
        GetAuth(missingSecretStore).Children.Remove(new YamlScalarNode("secretStore")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), missingSecretStore));

        YamlMappingNode malformedConfigurationSpec = LoadYaml(lifecycleAcl);
        malformedConfigurationSpec.Children[new YamlScalarNode("spec")] = new YamlSequenceNode();
        Should.Throw<InvalidDataException>(() => Validate(malformedConfigurationSpec, LoadYaml(stateComponent)));

        YamlMappingNode missingComponentMetadata = LoadYaml(stateComponent);
        ((YamlMappingNode)missingComponentMetadata.Children[new YamlScalarNode("spec")])
            .Children.Remove(new YamlScalarNode("metadata")).ShouldBeTrue();
        Should.Throw<InvalidDataException>(() => Validate(LoadYaml(lifecycleAcl), missingComponentMetadata));
        Should.Throw<InvalidDataException>(() => LoadYaml("- not-a-mapping-root"));
        Should.Throw<YamlException>(() => LoadYaml("spec:\n  accessControl: ["));
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

    private static YamlMappingNode LoadYaml(string yaml)
        => AccessTelemetryYamlLeastPrivilegeValidator.LoadSingleMapping(yaml);

    private static YamlMappingNode GetMetadata(YamlMappingNode root)
        => (YamlMappingNode)root.Children[new YamlScalarNode("metadata")];

    private static YamlMappingNode GetAccessControl(YamlMappingNode root)
        => (YamlMappingNode)((YamlMappingNode)root.Children[new YamlScalarNode("spec")])
            .Children[new YamlScalarNode("accessControl")];

    private static YamlMappingNode GetComponentSpec(YamlMappingNode root)
        => (YamlMappingNode)root.Children[new YamlScalarNode("spec")];

    private static YamlMappingNode GetAuth(YamlMappingNode root)
        => (YamlMappingNode)root.Children[new YamlScalarNode("auth")];

    private static YamlSequenceNode GetPolicies(YamlMappingNode root)
        => (YamlSequenceNode)((YamlMappingNode)((YamlMappingNode)root.Children[new YamlScalarNode("spec")])
            .Children[new YamlScalarNode("accessControl")]).Children[new YamlScalarNode("policies")];

    private static YamlSequenceNode GetOperations(YamlMappingNode root, int policyIndex)
        => (YamlSequenceNode)((YamlMappingNode)GetPolicies(root).Children[policyIndex])
            .Children[new YamlScalarNode("operations")];

    private static void ReverseSequence(YamlSequenceNode sequence)
    {
        YamlNode[] reversed = sequence.Children.Reverse().ToArray();
        sequence.Children.Clear();
        foreach (YamlNode child in reversed)
        {
            sequence.Add(child);
        }
    }

    private static string SerializeYaml(YamlMappingNode root)
    {
        var stream = new YamlStream(new YamlDocument(root));
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    private static void Validate(YamlMappingNode lifecycleAcl, YamlMappingNode stateComponent)
        => AccessTelemetryYamlLeastPrivilegeValidator.Validate(lifecycleAcl, stateComponent);
}
