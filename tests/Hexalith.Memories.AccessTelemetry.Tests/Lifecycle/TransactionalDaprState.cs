// <copyright file="TransactionalDaprState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using System.Globalization;
using System.Text.Json;

using Dapr;
using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using NSubstitute;

/// <summary>
/// Deterministic in-process substitute for a strongly consistent, transactional Dapr state store.
/// It models the store contract the PG-ONPREM-1 profile must honour: all-or-nothing transactions,
/// ETag validation, empty-ETag first-write insert semantics, component TTL reaping, and a
/// backend that can acknowledge a delete the strong re-read still contradicts.
/// </summary>
internal sealed class TransactionalDaprState
{
    private static readonly JsonSerializerOptions DaprJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, (byte[] Value, string ETag)> _entries = new(StringComparer.Ordinal);
    private long _etagSequence;

    /// <summary>Initializes a new instance of the <see cref="TransactionalDaprState"/> class.</summary>
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
                BeforeTransaction?.Invoke();
                Apply(operations);
                Transactions.Add(operations.ToArray());
                return Task.CompletedTask;
            });
    }

    /// <summary>Gets the substituted Dapr client bound to this state.</summary>
    public DaprClient Client { get; }

    /// <summary>Gets every transaction the adapter committed, in order.</summary>
    public List<IReadOnlyList<StateTransactionRequest>> Transactions { get; } = [];

    /// <summary>
    /// Gets the keys whose deletion the backend acknowledges but never applies, so a strong
    /// re-read still observes the value. Models the durability defect
    /// <see cref="AccessTelemetryDeleteStatus.VerificationFailed"/> exists to catch.
    /// </summary>
    public HashSet<string> UndeletableKeys { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets a hook invoked immediately before a transaction is validated, so a test can
    /// interleave a concurrent writer between the adapter's ETag read and its commit.
    /// </summary>
    public Action? BeforeTransaction { get; set; }

    /// <summary>Reports whether a key is currently stored.</summary>
    public bool Contains(string key) => _entries.ContainsKey(key);

    /// <summary>Reads and deserializes one stored value.</summary>
    public T Get<T>(string key)
        where T : class
        => JsonSerializer.Deserialize<T>(_entries[key].Value, DaprJsonOptions)!;

    /// <summary>
    /// Removes one key the way native component TTL reaping would, leaving every other key —
    /// including a now-orphaned expiry-bucket entry — untouched.
    /// </summary>
    public void ExpireByTtl(string key) => _entries.Remove(key);

    /// <summary>
    /// Advances one key's ETag without changing its value, modelling a concurrent writer that
    /// commits after the adapter read its ETag.
    /// </summary>
    public void AdvanceETag(string key)
    {
        (byte[] Value, string ETag) current = _entries[key];
        _entries[key] = (current.Value, NextETag());
    }

    private string NextETag() => (++_etagSequence).ToString(CultureInfo.InvariantCulture);

    private void Apply(IReadOnlyList<StateTransactionRequest> operations)
    {
        // Validate every operation before mutating anything: the transaction is all-or-nothing.
        foreach (StateTransactionRequest operation in operations)
        {
            bool exists = _entries.TryGetValue(operation.Key, out (byte[] Value, string ETag) current);
            string currentEtag = exists ? current.ETag : string.Empty;
            if (!string.IsNullOrEmpty(operation.ETag))
            {
                if (!string.Equals(operation.ETag, currentEtag, StringComparison.Ordinal))
                {
                    throw new DaprException("ETag conflict");
                }
            }
            else if (exists &&
                operation.OperationType == StateOperationType.Upsert &&
                operation.Options?.Concurrency == ConcurrencyMode.FirstWrite)
            {
                // An empty ETag under first-write concurrency is an insert, never a clobber.
                throw new DaprException("First-write conflict: an empty ETag cannot overwrite an existing key.");
            }
        }

        foreach (StateTransactionRequest operation in operations)
        {
            if (operation.OperationType == StateOperationType.Delete)
            {
                if (!UndeletableKeys.Contains(operation.Key))
                {
                    _ = _entries.Remove(operation.Key);
                }
            }
            else
            {
                _entries[operation.Key] = (operation.Value!, NextETag());
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
