// <copyright file="DaprWorkflowPayloadStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Globalization;
using System.Security.Cryptography;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

using Microsoft.Extensions.Options;

/// <summary>Dapr state-store implementation of <see cref="IWorkflowPayloadStore"/>.</summary>
public sealed class DaprWorkflowPayloadStore : IWorkflowPayloadStore
{
    private readonly DaprClient _daprClient;
    private readonly TimeProvider _timeProvider;
    private readonly WorkflowPayloadStoreOptions _options;

    /// <summary>Initializes a new instance of the <see cref="DaprWorkflowPayloadStore"/> class.</summary>
    public DaprWorkflowPayloadStore(
        DaprClient daprClient,
        IOptions<WorkflowPayloadStoreOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _daprClient = daprClient;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<WorkflowPayloadReference> SaveAsync(
        string tenantId,
        string memoryUnitId,
        WorkflowPayloadKind kind,
        ReadOnlyMemory<byte> payload,
        string? idSuffix = null,
        CancellationToken cancellationToken = default)
    {
        TenantIdGuard.Validate(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        if (kind == WorkflowPayloadKind.Unknown || !Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), "Workflow payload kind must be supported.");
        }

        if (payload.Length == 0)
        {
            throw new ArgumentException("Workflow payload must not be empty.", nameof(payload));
        }

        byte[] bytes = payload.ToArray();
        string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        string safeSuffix = string.IsNullOrWhiteSpace(idSuffix)
            ? string.Empty
            : ":" + Uri.EscapeDataString(idSuffix);
        string id = $"{memoryUnitId}:{kind.ToString().ToLowerInvariant()}:{hash}{safeSuffix}";
        WorkflowPayloadReference reference = new(
            id,
            hash,
            bytes.LongLength,
            kind,
            tenantId,
            memoryUnitId);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int ttlSeconds = Math.Max(1, _options.TtlHours) * 3600;
        WorkflowPayloadStoreEntry entry = new(reference, bytes, now, now.AddSeconds(ttlSeconds));

        await _daprClient.SaveStateAsync(
            _options.StateStoreName,
            BuildStateKey(reference),
            entry,
            metadata: new Dictionary<string, string>
            {
                ["ttlInSeconds"] = ttlSeconds.ToString(CultureInfo.InvariantCulture),
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return reference;
    }

    /// <inheritdoc/>
    public async Task<byte[]> ReadAsync(
        WorkflowPayloadReference reference,
        string tenantId,
        string memoryUnitId,
        WorkflowPayloadKind expectedKind,
        CancellationToken cancellationToken = default)
    {
        ValidateReferenceScope(reference, tenantId, memoryUnitId, expectedKind);
        WorkflowPayloadStoreEntry? entry = await _daprClient
            .GetStateAsync<WorkflowPayloadStoreEntry?>(
                _options.StateStoreName,
                BuildStateKey(reference),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            throw new WorkflowPayloadException("PAYLOAD_NOT_FOUND", reference.Id);
        }

        ValidateReferenceScope(entry.Reference, tenantId, memoryUnitId, expectedKind);
        if (!entry.Reference.Equals(reference))
        {
            throw new WorkflowPayloadException("PAYLOAD_REFERENCE_MISMATCH", reference.Id);
        }

        byte[] payload = entry.Payload ?? [];
        if (payload.LongLength != reference.ByteLength)
        {
            throw new WorkflowPayloadException("PAYLOAD_LENGTH_MISMATCH", reference.Id);
        }

        string actualHash = Convert.ToHexStringLower(SHA256.HashData(payload));
        if (!string.Equals(actualHash, reference.Sha256Hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkflowPayloadException("PAYLOAD_HASH_MISMATCH", reference.Id);
        }

        return payload;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(WorkflowPayloadReference reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return _daprClient.DeleteStateAsync(
            _options.StateStoreName,
            BuildStateKey(reference),
            cancellationToken: cancellationToken);
    }

    private static string BuildStateKey(WorkflowPayloadReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        TenantIdGuard.Validate(reference.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference.MemoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference.Sha256Hash);
        return $"{reference.TenantId}:workflow-payload:{reference.Id}";
    }

    private static void ValidateReferenceScope(
        WorkflowPayloadReference reference,
        string tenantId,
        string memoryUnitId,
        WorkflowPayloadKind expectedKind)
    {
        ArgumentNullException.ThrowIfNull(reference);
        TenantIdGuard.Validate(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        if (!string.Equals(reference.TenantId, tenantId, StringComparison.Ordinal))
        {
            throw new WorkflowPayloadException("PAYLOAD_TENANT_MISMATCH", reference.Id);
        }

        if (!string.Equals(reference.MemoryUnitId, memoryUnitId, StringComparison.Ordinal))
        {
            throw new WorkflowPayloadException("PAYLOAD_MEMORY_UNIT_MISMATCH", reference.Id);
        }

        if (reference.ContentKind != expectedKind)
        {
            throw new WorkflowPayloadException("PAYLOAD_KIND_MISMATCH", reference.Id);
        }

        if (reference.ByteLength <= 0)
        {
            throw new WorkflowPayloadException("PAYLOAD_LENGTH_INVALID", reference.Id);
        }
    }
}
