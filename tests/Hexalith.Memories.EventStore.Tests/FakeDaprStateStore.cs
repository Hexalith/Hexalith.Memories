// <copyright file="FakeDaprStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Globalization;
using System.Text.Json;

using Dapr.Client;

using NSubstitute;

using StateEntry = (byte[] Payload, Type DataType, long Version, bool FirstWrite, DateTimeOffset? ExpiresAt);

/// <summary>In-memory Dapr state-store test double for individual Redis state operations.</summary>
/// <remarks>Values cross a serialization boundary. The test double models ETag checks, the Redis
/// FirstWrite marker, and TTL metadata against a supplied clock. The in-memory store is a single
/// synchronized replica.</remarks>
internal sealed class FakeDaprStateStore(TimeProvider? timeProvider = null)
{
    public const string StoreName = "statestore";

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<string, StateEntry> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _remainingSaveFailures = new(StringComparer.Ordinal);

    /// <summary>Forces the next <paramref name="count"/> saves of <paramref name="key"/> to return false.</summary>
    public void FailNextSaves(string key, int count)
    {
        lock (_entries)
        {
            _remainingSaveFailures[key] = count;
        }
    }

    /// <summary>Creates a substitute client connected to this state store.</summary>
    public DaprClient CreateClient()
    {
        DaprClient client = Substitute.For<DaprClient>();
        SetupType<Dictionary<string, string>>(client);
        SetupType<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>>(client);
        SetupType<List<string>>(client);
        SetupType<string>(client);

        _ = client.DeleteStateAsync(
                StoreName, Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _ = TryDelete(call.ArgAt<string>(1), null);
                return Task.CompletedTask;
            });

        _ = client.TryDeleteStateAsync(
                StoreName, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(call => TryDelete(call.ArgAt<string>(1), call.ArgAt<string>(2)));

        return client;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        lock (_entries)
        {
            if (FindEntry(key) is StateEntry entry && typeof(T).IsAssignableFrom(entry.DataType))
            {
                value = Deserialize<T>(entry);
                return value is not null;
            }

            value = default;
            return false;
        }
    }

    public bool ContainsKey(string key)
    {
        lock (_entries)
        {
            return FindEntry(key) is not null;
        }
    }

    public void Seed<T>(string key, T value)
    {
        lock (_entries)
        {
            WriteEntry(key, value, FindEntry(key), firstWrite: false, expiresAt: null);
        }
    }

    private static string GetEtag(StateEntry entry) => entry.Version.ToString(CultureInfo.InvariantCulture);

    private static T? Deserialize<T>(StateEntry entry) => JsonSerializer.Deserialize<T>(entry.Payload, JsonSerializerOptions.Web);

    private void SetupType<T>(DaprClient client)
        where T : class
    {
        _ = client.GetStateAndETagAsync<T?>(
                StoreName, Arg.Any<string>(), Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(call => Read<T>(call.ArgAt<string>(1)));

        _ = client.GetStateAsync<T?>(
                StoreName, Arg.Any<string>(), Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(call => Read<T>(call.ArgAt<string>(1)).Value);

        _ = client.GetBulkStateAsync<T>(
                StoreName, Arg.Any<IReadOnlyList<string>>(), Arg.Any<int?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(call => ReadBulk<T>(call.ArgAt<IReadOnlyList<string>>(1)));

        _ = client.TrySaveStateAsync(
                StoreName, Arg.Any<string>(), Arg.Any<T>(), Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(call => TrySave(call.ArgAt<string>(1), call.ArgAt<T>(2), call.ArgAt<string>(3),
                call.ArgAt<StateOptions?>(4), call.ArgAt<IReadOnlyDictionary<string, string>?>(5)));
    }

    private (T? Value, string Etag) Read<T>(string key)
    {
        lock (_entries)
        {
            return FindEntry(key) is StateEntry entry
                ? (Deserialize<T>(entry), GetEtag(entry))
                : (default, string.Empty);
        }
    }

    private IReadOnlyList<BulkStateItem<T>> ReadBulk<T>(IReadOnlyList<string> keys)
        where T : class
    {
        lock (_entries)
        {
            List<BulkStateItem<T>> result = [];
            foreach (string key in keys)
            {
                if (FindEntry(key) is StateEntry entry && Deserialize<T>(entry) is T value)
                {
                    result.Add(new BulkStateItem<T>(key, value, GetEtag(entry)));
                }
            }

            return result;
        }
    }

    private bool TrySave<T>(string key, T value, string? etag, StateOptions? options, IReadOnlyDictionary<string, string>? metadata)
    {
        lock (_entries)
        {
            if (_remainingSaveFailures.TryGetValue(key, out int remaining) && remaining > 0)
            {
                _remainingSaveFailures[key] = remaining - 1;
                return false;
            }

            StateEntry? current = FindEntry(key);
            bool compareEtags = options?.Concurrency != ConcurrencyMode.LastWrite && !string.IsNullOrEmpty(etag);
            if (current is StateEntry entry && (compareEtags ? GetEtag(entry) != etag : entry.FirstWrite))
            {
                return false;
            }

            bool firstWrite = options?.Concurrency == ConcurrencyMode.FirstWrite || current?.FirstWrite == true;
            DateTimeOffset? expiresAt = current?.ExpiresAt;
            if (metadata?.TryGetValue("ttlInSeconds", out string? ttlText) == true && !string.IsNullOrEmpty(ttlText))
            {
                long ttl = long.Parse(ttlText, CultureInfo.InvariantCulture);
                expiresAt = ttl > 0 ? _timeProvider.GetUtcNow().AddSeconds(ttl) : null;
            }

            WriteEntry(key, value, current, firstWrite, expiresAt);
            return true;
        }
    }

    private bool TryDelete(string key, string? etag)
    {
        lock (_entries)
        {
            if (FindEntry(key) is StateEntry entry && !string.IsNullOrEmpty(etag) && GetEtag(entry) != etag)
            {
                return false;
            }

            _ = _entries.Remove(key);
            return true;
        }
    }

    private StateEntry? FindEntry(string key)
    {
        if (!_entries.TryGetValue(key, out StateEntry entry))
        {
            return null;
        }

        if (entry.ExpiresAt <= _timeProvider.GetUtcNow())
        {
            _ = _entries.Remove(key);
            return null;
        }

        return entry;
    }

    private void WriteEntry<T>(string key, T value, StateEntry? current, bool firstWrite, DateTimeOffset? expiresAt)
        => _entries[key] = (JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web), typeof(T),
            (current?.Version ?? 0) + 1, firstWrite, expiresAt);
}
