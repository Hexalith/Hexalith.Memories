// <copyright file="FakeDaprStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dapr.Client;

using NSubstitute;

/// <summary>In-memory Dapr state-store test double that models ETag optimistic concurrency so the
/// migrated stores' compare-and-set read-modify-write logic (idempotency, late/out-of-order writes,
/// first-writer-wins, cardinality cap) can be exercised without a real Dapr sidecar. Backs an NSubstitute
/// <see cref="DaprClient"/> whose Get/Save/Delete for each configured state type delegate here.</summary>
internal sealed class FakeDaprStateStore
{
    public const string StoreName = "statestore";

    private readonly Dictionary<string, (object? Value, string Etag)> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _remainingSaveFailures = new(StringComparer.Ordinal);
    private long _etagSequence;

    /// <summary>Forces the next <paramref name="count"/> <c>TrySaveStateAsync</c> calls for
    /// <paramref name="key"/> to return <c>false</c> (ETag mismatch), enabling CAS-exhaustion tests.</summary>
    public void FailNextSaves(string key, int count)
        => _remainingSaveFailures[key] = count;

    /// <summary>Creates a substitute <see cref="DaprClient"/> wired to this backing store for the given
    /// state value types. Each type used by a store under test must be registered.</summary>
    public DaprClient CreateClient()
    {
        DaprClient client = Substitute.For<DaprClient>();
        SetupType<Dictionary<string, string>>(client);
        SetupType<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>>(client);
        SetupType<List<string>>(client);
        SetupType<string>(client);

        _ = client.DeleteStateAsync(
                StoreName, Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _ = _entries.Remove(ci.ArgAt<string>(1));
                return Task.CompletedTask;
            });

        _ = client.TryDeleteStateAsync(
                StoreName, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                string key = ci.ArgAt<string>(1);
                string etag = ci.ArgAt<string>(2);
                string current = _entries.TryGetValue(key, out (object? Value, string Etag) entry) ? entry.Etag : string.Empty;
                if (!string.Equals(etag, current, StringComparison.Ordinal))
                {
                    return Task.FromResult(false);
                }

                _ = _entries.Remove(key);
                return Task.FromResult(true);
            });

        return client;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        if (_entries.TryGetValue(key, out (object? Value, string Etag) entry) && entry.Value is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public void Seed<T>(string key, T value) => _entries[key] = (value, NextEtag());

    private string NextEtag() => (++_etagSequence).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private void SetupType<T>(DaprClient client)
        where T : class
    {
        _ = client.GetStateAndETagAsync<T?>(
                StoreName, Arg.Any<string>(), Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                string key = ci.ArgAt<string>(1);
                return _entries.TryGetValue(key, out (object? Value, string Etag) entry)
                    ? ((T?)entry.Value, entry.Etag)
                    : (default, string.Empty);
            });

        _ = client.GetStateAsync<T?>(
                StoreName, Arg.Any<string>(), Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                string key = ci.ArgAt<string>(1);
                return _entries.TryGetValue(key, out (object? Value, string Etag) entry) ? (T?)entry.Value : default;
            });

        _ = client.TrySaveStateAsync(
                StoreName, Arg.Any<string>(), Arg.Any<T>(), Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                string key = ci.ArgAt<string>(1);
                T value = ci.ArgAt<T>(2);
                string etag = ci.ArgAt<string>(3);

                if (_remainingSaveFailures.TryGetValue(key, out int remaining) && remaining > 0)
                {
                    _remainingSaveFailures[key] = remaining - 1;
                    return Task.FromResult(false);
                }

                string current = _entries.TryGetValue(key, out (object? Value, string Etag) entry) ? entry.Etag : string.Empty;
                if (!string.Equals(etag, current, StringComparison.Ordinal))
                {
                    return Task.FromResult(false);
                }

                _entries[key] = (value, NextEtag());
                return Task.FromResult(true);
            });
    }
}
