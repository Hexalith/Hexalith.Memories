// <copyright file="DaprAccessTelemetryStateStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using System.Text.Json;

using Dapr;
using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using NSubstitute;

using Shouldly;

public sealed class DaprAccessTelemetryStateStoreTests
{
    private static readonly DateTimeOffset Expiry = new(2026, 7, 20, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task WriteRecordAndIndexAsync_CommitsRecordBucketAndCatalogInOneTransaction()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);

        AccessTelemetryStoreWriteStatus result = await store.WriteRecordAndIndexAsync(
            record,
            entry,
            3600,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        IReadOnlyList<StateTransactionRequest> transaction = state.Transactions.ShouldHaveSingleItem();
        transaction.Count.ShouldBe(3);
        transaction.Select(static operation => operation.Key).ShouldBe(
        [
            $"records/{entry.Shard:D2}/{record.RecordId}",
            $"expiry-bucket/{entry.ExpiryMinute:D12}/{entry.Shard:D2}",
            "expiry-catalog",
        ]);
        transaction[0].Metadata!["ttlInSeconds"].ShouldBe("3600");
        transaction.ShouldAllBe(static operation => operation.Metadata!["partitionKey"] == "access-telemetry");
        state.Get<AccessTelemetryExpiryBucket>(transaction[1].Key).Entries.ShouldHaveSingleItem().ShouldBe(entry);
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldBe([entry.ExpiryMinute]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_TraversesExplicitBucketsWithoutQueryApi()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord later = CreateRecord("01K0A000000000000000000002", Expiry.AddSeconds(20));
        AccessTelemetryRecord earlier = CreateRecord("01K0A000000000000000000001", Expiry.AddSeconds(5));
        await store.WriteRecordAndIndexAsync(later, CreateEntry(later), 3600, CancellationToken.None);
        await store.WriteRecordAndIndexAsync(earlier, CreateEntry(earlier), 3600, CancellationToken.None);

        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            AccessTelemetryExpiryIndex.GetExpiryMinute(Expiry.AddMinutes(1)),
            10,
            CancellationToken.None);

        due.Select(static entry => entry.RecordId).ShouldBe([earlier.RecordId, later.RecordId]);
        await state.Client.DidNotReceiveWithAnyArgs().QueryStateAsync<AccessTelemetryExpiryEntry>(default!, default!, default!, default);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_RemovesRecordAndBucketEntryAndPrunesEmptyMinute()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(entry, CancellationToken.None);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.Deleted);
        due.ShouldBeEmpty();
        state.Contains($"records/{entry.Shard:D2}/{record.RecordId}").ShouldBeFalse();
        state.Contains($"expiry-bucket/{entry.ExpiryMinute:D12}/{entry.Shard:D2}").ShouldBeFalse();
        state.Contains("expiry-catalog").ShouldBeFalse();
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_RetryIsIdempotentWithoutDuplicatingBucketEntry()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);

        AccessTelemetryStoreWriteStatus first = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);
        AccessTelemetryStoreWriteStatus retry = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        first.ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        retry.ShouldBe(AccessTelemetryStoreWriteStatus.Idempotent);
        state.Transactions.Count.ShouldBe(1);
        state.Get<AccessTelemetryExpiryBucket>($"expiry-bucket/{entry.ExpiryMinute:D12}/{entry.Shard:D2}")
            .Entries.ShouldHaveSingleItem();
    }

    private static AccessTelemetryExpiryEntry CreateEntry(AccessTelemetryRecord record)
        => new(
            record.RecordId,
            AccessTelemetryExpiryIndex.GetExpiryMinute(DateTimeOffset.Parse(record.ExpiresAtUtc, System.Globalization.CultureInfo.InvariantCulture)),
            AccessTelemetryExpiryIndex.GetShard(record.RecordId),
            record.EnvelopeHash,
            record.ExpiresAtUtc);

    private static AccessTelemetryRecord CreateRecord(string recordId, DateTimeOffset? expiry = null)
    {
        AccessTelemetryRecord record = new()
        {
            AcceptedAtUtc = Format(Expiry.AddHours(-1)),
            DurationMs = 42,
            EmittedAtUtc = Format(Expiry.AddHours(-1)),
            EnvelopeHash = string.Empty,
            EventId = 7501,
            ExpiresAtUtc = Format(expiry ?? Expiry),
            MarkerKeyId = "mk-2026a",
            OperationType = "search",
            Outcome = "ok",
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "all-authorized",
                ["explain"] = false,
                ["queryLengthBucket"] = "33-128",
                ["subjectPresent"] = true,
                ["weightProfile"] = "configured",
            },
            RecordId = recordId,
            ResultCount = 1,
            SchemaVersion = 1,
            TenantMarker = new string('a', 64),
        };
        return record with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(record) };
    }

    private static string Format(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private sealed class TransactionalDaprState
    {
        private static readonly JsonSerializerOptions DaprJsonOptions = new(JsonSerializerDefaults.Web);

        private readonly Dictionary<string, (byte[] Value, string ETag)> _entries = new(StringComparer.Ordinal);
        private long _etagSequence;

        public TransactionalDaprState()
        {
            Client = Substitute.For<DaprClient>();
            SetupType<AccessTelemetryRecord>();
            SetupType<AccessTelemetryExpiryBucket>();
            SetupType<AccessTelemetryExpiryCatalog>();
            Client.ExecuteStateTransactionAsync(
                    "access-telemetry-store",
                    Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                    Arg.Any<IReadOnlyDictionary<string, string>?>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    IReadOnlyList<StateTransactionRequest> operations = call.ArgAt<IReadOnlyList<StateTransactionRequest>>(1);
                    Apply(operations);
                    Transactions.Add(operations.ToArray());
                    return Task.CompletedTask;
                });
        }

        public DaprClient Client { get; }

        public List<IReadOnlyList<StateTransactionRequest>> Transactions { get; } = [];

        public bool Contains(string key) => _entries.ContainsKey(key);

        public T Get<T>(string key)
            where T : class
            => JsonSerializer.Deserialize<T>(_entries[key].Value, DaprJsonOptions)!;

        private void Apply(IReadOnlyList<StateTransactionRequest> operations)
        {
            foreach (StateTransactionRequest operation in operations)
            {
                string currentEtag = _entries.TryGetValue(operation.Key, out (byte[] Value, string ETag) current)
                    ? current.ETag
                    : string.Empty;
                if (!string.IsNullOrEmpty(operation.ETag) && !string.Equals(operation.ETag, currentEtag, StringComparison.Ordinal))
                {
                    throw new DaprException("ETag conflict");
                }
            }

            foreach (StateTransactionRequest operation in operations)
            {
                if (operation.OperationType == StateOperationType.Delete)
                {
                    _ = _entries.Remove(operation.Key);
                }
                else
                {
                    _entries[operation.Key] = (operation.Value!, (++_etagSequence).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }

        private void SetupType<T>()
            where T : class
        {
            Client.GetStateAndETagAsync<T>(
                    "access-telemetry-store",
                    Arg.Any<string>(),
                    Arg.Any<ConsistencyMode?>(),
                    Arg.Any<IReadOnlyDictionary<string, string>?>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    string key = call.ArgAt<string>(1);
                    return _entries.TryGetValue(key, out (byte[] Value, string ETag) current)
                        ? (JsonSerializer.Deserialize<T>(current.Value, DaprJsonOptions)!, current.ETag)
                        : (default(T)!, string.Empty);
                });
        }
    }
}
