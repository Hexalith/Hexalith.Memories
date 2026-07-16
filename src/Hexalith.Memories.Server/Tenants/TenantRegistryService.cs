// <copyright file="TenantRegistryService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.Server.Caching;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Manages the tenant registry using DAPR state store.</summary>
public sealed partial class TenantRegistryService
{
    private const string StoreName = "statestore";
    private const string IndexKey = "tenant-registry-index";
    private const int MaxTenantRegistrationRetries = 3;
    private const int MaxDeletionStartRetries = 3;
    private const int MaxStatusUpdateRetries = 3;
    private static readonly byte[] EmptyTransactionValue = [];

    private readonly DaprClient _daprClient;
    private readonly IMemoriesCommandStore _commandStore;
    private readonly ConcurrentDictionary<string, (TenantRegistryEntry? Entry, DateTimeOffset ExpiresAt)> _statusCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _statusGenerations = new(StringComparer.Ordinal);
    private readonly ILogger<TenantRegistryService> _logger;
    private readonly IOptions<TenantReadCacheOptions> _cacheOptions;
    private readonly TenantSummaryCache? _summaryCache;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="TenantRegistryService"/> class.</summary>
    /// <param name="daprClient">The DAPR client for state management.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandStore">The EventStore command boundary for authoritative tenant lifecycle events.</param>
    public TenantRegistryService(
        DaprClient daprClient,
        ILogger<TenantRegistryService> logger,
        IMemoriesCommandStore? commandStore = null,
        IOptions<TenantReadCacheOptions>? cacheOptions = null,
        TimeProvider? timeProvider = null,
        TenantSummaryCache? summaryCache = null)
    {
        _daprClient = daprClient;
        _commandStore = commandStore ?? new InMemoryMemoriesCommandStore();
        _logger = logger;
        _cacheOptions = cacheOptions ?? Options.Create(new TenantReadCacheOptions());
        _timeProvider = timeProvider ?? TimeProvider.System;
        _summaryCache = summaryCache;
    }

    /// <summary>Registers a new tenant with Provisioning status.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="displayName">The tenant display name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registered tenant info.</returns>
    public async Task<TenantInfo> RegisterTenantAsync(string tenantId, string displayName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        TenantRegistryEntry entry = await RegisterOrGetTenantEntryAsync(tenantId, displayName, workflowInstanceId: null, ct)
            .ConfigureAwait(false);
        return entry.Tenant;
    }

    /// <summary>Registers a tenant if it does not exist, or returns the existing registry entry.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="displayName">The tenant display name.</param>
    /// <param name="workflowInstanceId">The workflow instance that owns provisioning, if any.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current registry entry.</returns>
    public async Task<TenantRegistryEntry> RegisterOrGetTenantEntryAsync(
        string tenantId,
        string displayName,
        string? workflowInstanceId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string stateKey = GetTenantStateKey(tenantId);
        TenantRegistryEntry entry = new(
            new TenantInfo(tenantId, displayName, TenantStatus.Provisioning, now),
            workflowInstanceId,
            now);
        bool commandAccepted = false;

        for (int attempt = 0; attempt < MaxTenantRegistrationRetries; attempt++)
        {
            (TenantRegistryEntry? existing, string entryEtag) = await GetStoredEntryAndETagAsync(stateKey, ct).ConfigureAwait(false);
            (List<string>? existingIndex, string indexEtag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(StoreName, IndexKey, cancellationToken: ct)
                .ConfigureAwait(false);

            List<string> index = NormalizeIndex(existingIndex);
            if (existing is not null)
            {
                if (index.Contains(tenantId, StringComparer.Ordinal))
                {
                    SetStatusCache(tenantId, existing);
                    _summaryCache?.Invalidate(tenantId);
                    return existing;
                }

                index.Add(tenantId);
                try
                {
                    await _daprClient.ExecuteStateTransactionAsync(
                            StoreName,
                            [
                                CreateUpsertRequest(stateKey, PersistenceModelMapper.ToStored(existing), entryEtag),
                                CreateUpsertRequest(IndexKey, index, indexEtag),
                            ],
                            metadata: null!,
                            cancellationToken: ct)
                        .ConfigureAwait(false);
                    SetStatusCache(tenantId, existing);
                    _summaryCache?.Invalidate(tenantId);
                    return existing;
                }
                catch (Dapr.DaprException)
                {
                    continue;
                }
            }

            if (!commandAccepted)
            {
                // Registry rows are Dapr read-model state; tenant lifecycle command acceptance remains EventStore-backed.
                await _commandStore.AcceptAsync(
                    tenantId,
                    new RegisterTenantCommand(tenantId, displayName, now),
                    workflowInstanceId ?? "system",
                    ct).ConfigureAwait(false);
                commandAccepted = true;
            }

            if (!index.Contains(tenantId, StringComparer.Ordinal))
            {
                index.Add(tenantId);
            }

            try
            {
                await _daprClient.ExecuteStateTransactionAsync(
                        StoreName,
                        [
                            CreateUpsertRequest(stateKey, PersistenceModelMapper.ToStored(entry), entryEtag),
                            CreateUpsertRequest(IndexKey, index, indexEtag),
                        ],
                        metadata: null!,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
            }
            catch (Dapr.DaprException)
            {
                continue;
            }

            LogTenantRegistered(_logger, tenantId, displayName);
            SetStatusCache(tenantId, entry);
            _summaryCache?.Invalidate(tenantId);
            return entry;
        }

        TenantRegistryEntry? current = await GetStoredEntryAsync(stateKey, ct).ConfigureAwait(false);
        List<string>? currentIndex = await _daprClient
            .GetStateAsync<List<string>?>(StoreName, IndexKey, cancellationToken: ct)
            .ConfigureAwait(false);

        if (current is not null && currentIndex?.Contains(tenantId, StringComparer.Ordinal) == true)
        {
            SetStatusCache(tenantId, current);
            _summaryCache?.Invalidate(tenantId);
            return current;
        }

        throw new InvalidOperationException(
            $"Failed to register tenant '{tenantId}' after {MaxTenantRegistrationRetries} attempts due to concurrent registry transaction conflicts.");
    }

    /// <summary>Gets the full tenant registry entry by its identifier.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registry entry, or null if not found.</returns>
    public async Task<TenantRegistryEntry?> GetTenantEntryAsync(string tenantId, CancellationToken ct)
    {
        string stateKey = GetTenantStateKey(tenantId);
        TenantRegistryEntry? entry = await GetStoredEntryAsync(stateKey, ct).ConfigureAwait(false);

        if (entry is null)
        {
            LogTenantNotFound(_logger, tenantId);
        }

        return entry;
    }

    /// <summary>Gets a tenant by its identifier.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tenant info, or null if not found.</returns>
    public async Task<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken ct)
    {
        TenantRegistryEntry? entry = await GetTenantEntryAsync(tenantId, ct).ConfigureAwait(false);
        return entry?.Tenant;
    }

    /// <summary>Gets a tenant using the short-lived status-read cache used by hot-path guards.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tenant info, or null if not found.</returns>
    public async Task<TenantInfo?> GetTenantForStatusGuardAsync(string tenantId, CancellationToken ct)
    {
        TenantRegistryEntry? entry = await GetTenantEntryForStatusGuardAsync(tenantId, ct).ConfigureAwait(false);
        return entry?.Tenant;
    }

    /// <summary>Updates the status of an existing tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task UpdateTenantStatusAsync(string tenantId, TenantStatus status, CancellationToken ct, string? workflowInstanceId = null)
    {
        string stateKey = GetTenantStateKey(tenantId);
        bool commandAccepted = false;

        for (int attempt = 0; attempt < MaxStatusUpdateRetries; attempt++)
        {
            (TenantRegistryEntry? entry, string etag) = await GetStoredEntryAndETagAsync(stateKey, ct).ConfigureAwait(false);

            if (entry is null)
            {
                LogTenantNotFound(_logger, tenantId);
                throw new InvalidOperationException($"Tenant '{tenantId}' not found in registry.");
            }

            ThrowIfDeletingClaimWouldBeClobbered(tenantId, status, workflowInstanceId, entry);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!commandAccepted)
            {
                // Registry rows are Dapr read-model state; tenant lifecycle command acceptance remains EventStore-backed.
                await _commandStore.AcceptAsync(
                    tenantId,
                    new UpdateTenantLifecycleStatusCommand(tenantId, status, now),
                    workflowInstanceId ?? "system",
                    ct).ConfigureAwait(false);
                commandAccepted = true;
            }

            TenantRegistryEntry updatedEntry = entry with
            {
                Tenant = entry.Tenant with { Status = status },
                LastUpdated = now,
                WorkflowInstanceId = ResolveWorkflowInstanceId(status, workflowInstanceId, entry.WorkflowInstanceId),
            };
            bool saved = await _daprClient
                .TrySaveStateAsync(StoreName, stateKey, PersistenceModelMapper.ToStored(updatedEntry), etag, cancellationToken: ct)
                .ConfigureAwait(false);

            if (saved)
            {
                LogTenantStatusUpdated(_logger, tenantId, status);
                SetStatusCache(tenantId, updatedEntry);
                _summaryCache?.Invalidate(tenantId);
                return;
            }
        }

        throw new InvalidOperationException(
            $"Failed to update tenant '{tenantId}' status to {status} after {MaxStatusUpdateRetries} attempts due to concurrent registry updates.");
    }

    /// <summary>Claims deletion ownership for a tenant and marks it as deleting.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="workflowInstanceId">The workflow instance that owns the deletion.</param>
    /// <param name="allowRetryFromDeleting">Whether an existing deleting tenant can be re-claimed for a retry.</param>
    /// <param name="expectedWorkflowInstanceId">The workflow instance currently expected to own deletion, if any.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current tenant registry entry after the claim attempt, or null when the tenant does not exist.</returns>
    public async Task<TenantRegistryEntry?> BeginTenantDeletionAsync(
        string tenantId,
        string workflowInstanceId,
        bool allowRetryFromDeleting,
        string? expectedWorkflowInstanceId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowInstanceId);

        string stateKey = GetTenantStateKey(tenantId);
        bool commandAccepted = false;

        for (int attempt = 0; attempt < MaxDeletionStartRetries; attempt++)
        {
            (TenantRegistryEntry? existing, string etag) = await GetStoredEntryAndETagAsync(stateKey, ct).ConfigureAwait(false);

            if (existing is null)
            {
                LogTenantNotFound(_logger, tenantId);
                return null;
            }

            if (existing.Tenant.Status == TenantStatus.Provisioning)
            {
                return existing;
            }

            if (existing.Tenant.Status == TenantStatus.Deleting && !allowRetryFromDeleting)
            {
                return existing;
            }

            if (existing.Tenant.Status == TenantStatus.Deleting
                && allowRetryFromDeleting
                && !string.Equals(existing.WorkflowInstanceId, expectedWorkflowInstanceId, StringComparison.Ordinal))
            {
                return existing;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!commandAccepted)
            {
                await _commandStore.AcceptAsync(
                    tenantId,
                    new UpdateTenantLifecycleStatusCommand(tenantId, TenantStatus.Deleting, now),
                    workflowInstanceId,
                    ct).ConfigureAwait(false);
                commandAccepted = true;
            }

            TenantRegistryEntry updated = existing with
            {
                LastUpdated = now,
                Tenant = existing.Tenant with { Status = TenantStatus.Deleting },
                WorkflowInstanceId = workflowInstanceId,
            };

            bool saved = await _daprClient
                .TrySaveStateAsync(StoreName, stateKey, PersistenceModelMapper.ToStored(updated), etag, cancellationToken: ct)
                .ConfigureAwait(false);

            if (saved)
            {
                LogTenantStatusUpdated(_logger, tenantId, TenantStatus.Deleting);
                SetStatusCache(tenantId, updated);
                _summaryCache?.Invalidate(tenantId);
                return updated;
            }
        }

        TenantRegistryEntry? current = await GetStoredEntryAsync(stateKey, ct).ConfigureAwait(false);
        SetStatusCache(tenantId, current);
        _summaryCache?.Invalidate(tenantId);
        return current;
    }

    /// <summary>Lists all registered tenants.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of all registered tenants.</returns>
    public async Task<IReadOnlyList<TenantInfo>> ListTenantsAsync(CancellationToken ct)
    {
        // Internal callers legitimately need every tenant; this uses a dedicated unbounded path rather
        // than an int.MaxValue sentinel so the public HTTP path can never bypass the page-size clamp
        // (Story 24.2 review P1).
        TenantListPage page = await ListTenantsPageCoreAsync(0, limit: null, unbounded: true, ct).ConfigureAwait(false);
        return page.Tenants;
    }

    /// <summary>Lists a bounded page of registered tenants.</summary>
    /// <param name="offset">Requested offset.</param>
    /// <param name="limit">Requested limit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bounded tenant-list page.</returns>
    public Task<TenantListPage> ListTenantsPageAsync(int offset, int? limit, CancellationToken ct)
        => ListTenantsPageCoreAsync(offset, limit, unbounded: false, ct);

    private async Task<TenantListPage> ListTenantsPageCoreAsync(int offset, int? limit, bool unbounded, CancellationToken ct)
    {
        List<string>? index = await _daprClient.GetStateAsync<List<string>?>(StoreName, IndexKey, cancellationToken: ct).ConfigureAwait(false);
        List<string> normalizedIndex = NormalizeIndex(index);
        int totalCount = normalizedIndex.Count;
        int clampedOffset = Math.Max(offset, 0);

        // The public path always clamps to the safe maximum; only the internal unbounded path returns all.
        int clampedLimit = unbounded
            ? totalCount
            : Math.Clamp(limit.GetValueOrDefault(_cacheOptions.Value.GetDefaultTenantListLimit()), 1, _cacheOptions.Value.GetMaxTenantListLimit());

        if (totalCount == 0 || clampedOffset >= totalCount)
        {
            return new TenantListPage([], totalCount, clampedOffset, clampedLimit, HasMore: false);
        }

        IEnumerable<string> pageIds = normalizedIndex
            .Skip(clampedOffset)
            .Take(clampedLimit);

        List<TenantInfo> tenants = [];
        foreach (string tenantId in pageIds)
        {
            ct.ThrowIfCancellationRequested();
            TenantRegistryEntry? entry = await GetTenantEntryAsync(tenantId, ct).ConfigureAwait(false);
            if (entry is not null)
            {
                tenants.Add(entry.Tenant);
            }
        }

        // Overflow-free: clampedOffset < totalCount here (guarded above), so the subtraction is positive.
        bool hasMore = totalCount - clampedOffset > clampedLimit;
        return new TenantListPage(tenants, totalCount, clampedOffset, clampedLimit, hasMore);
    }

    /// <summary>
    /// Updates the display name of an existing tenant (Story 5.5 AC3 / FR42).
    /// Uses the ETag CAS retry pattern (mirrors <see cref="RegisterOrGetTenantEntryAsync"/>) and
    /// emits an <see cref="LogLevel.Information"/> operational-log event with the pinned field
    /// names (<c>tenantId</c>, <c>field</c>, <c>oldValue</c>, <c>newValue</c>, <c>actor</c>,
    /// <c>occurredAt</c>, <c>durationMs</c>) so migration to a Phase 2 audit store is a one-for-one
    /// remap (Amendment J).
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="actor">Attribution for the caller (MVP: <c>"operator@{remoteIp}"</c> per Amendment R; Phase 1.5 replaces with authenticated principal).</param>
    /// <param name="displayName">The new display name (already validated at the endpoint boundary).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated tenant info.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tenant does not exist, or ETag CAS fails after the retry budget.</exception>
    public async Task<TenantInfo> UpdateTenantDisplayNameAsync(
        string tenantId,
        string actor,
        string displayName,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        string stateKey = GetTenantStateKey(tenantId);
        long startTimestamp = Stopwatch.GetTimestamp();
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;

        for (int attempt = 0; attempt < MaxTenantRegistrationRetries; attempt++)
        {
            (TenantRegistryEntry? existing, string etag) = await GetStoredEntryAndETagAsync(stateKey, ct).ConfigureAwait(false);

            if (existing is null)
            {
                LogTenantNotFound(_logger, tenantId);
                throw new InvalidOperationException($"Tenant '{tenantId}' not found in registry.");
            }

            if (existing.Tenant.Status != TenantStatus.Active)
            {
                SetStatusCache(tenantId, existing);
                _summaryCache?.Invalidate(tenantId);
                throw new InvalidOperationException($"Tenant '{tenantId}' is not active.");
            }

            string oldValue = existing.Tenant.DisplayName;
            if (string.Equals(oldValue, displayName, StringComparison.Ordinal))
            {
                // No-op at value level, but still emit a log entry (observability over silence).
                LogTenantFieldUpdated(
                    _logger,
                    tenantId,
                    "displayName",
                    oldValue,
                    displayName,
                    actor,
                    occurredAt,
                    (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
                SetStatusCache(tenantId, existing);
                return existing.Tenant;
            }

            TenantRegistryEntry updated = existing with
            {
                Tenant = existing.Tenant with { DisplayName = displayName },
                LastUpdated = occurredAt,
            };
            bool saved = await _daprClient
                .TrySaveStateAsync(StoreName, stateKey, PersistenceModelMapper.ToStored(updated), etag, cancellationToken: ct)
                .ConfigureAwait(false);

            if (saved)
            {
                LogTenantFieldUpdated(
                    _logger,
                    tenantId,
                    "displayName",
                    oldValue,
                    displayName,
                    actor,
                    occurredAt,
                    (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
                SetStatusCache(tenantId, updated);
                _summaryCache?.Invalidate(tenantId);
                return updated.Tenant;
            }
        }

        throw new InvalidOperationException(
            $"Failed to update tenant '{tenantId}' display name after {MaxTenantRegistrationRetries} attempts due to concurrent updates.");
    }

    /// <summary>Checks whether a tenant exists in the registry.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the tenant exists.</returns>
    public async Task<bool> TenantExistsAsync(string tenantId, CancellationToken ct)
    {
        TenantInfo? tenant = await GetTenantAsync(tenantId, ct).ConfigureAwait(false);
        return tenant is not null;
    }

    /// <summary>Removes a tenant from the registry and index.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task RemoveTenantAsync(string tenantId, CancellationToken ct)
    {
        string stateKey = GetTenantStateKey(tenantId);
        for (int attempt = 0; attempt < MaxTenantRegistrationRetries; attempt++)
        {
            (TenantRegistryEntry? existing, string entryEtag) = await GetStoredEntryAndETagAsync(stateKey, ct).ConfigureAwait(false);
            (List<string>? existingIndex, string indexEtag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(StoreName, IndexKey, cancellationToken: ct)
                .ConfigureAwait(false);

            List<string> index = NormalizeIndex(existingIndex);
            bool removedFromIndex = index.Remove(tenantId);
            if (existing is null && !removedFromIndex)
            {
                SetStatusCache(tenantId, null);
                _summaryCache?.Invalidate(tenantId);
                return;
            }

            List<StateTransactionRequest> operations = [];
            if (existing is not null)
            {
                operations.Add(CreateDeleteRequest(stateKey, entryEtag));
            }

            operations.Add(CreateUpsertRequest(IndexKey, index, indexEtag));

            try
            {
                await _daprClient.ExecuteStateTransactionAsync(
                        StoreName,
                        operations,
                        metadata: null!,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
                SetStatusCache(tenantId, null);
                _summaryCache?.Invalidate(tenantId);
                return;
            }
            catch (Dapr.DaprException)
            {
                continue;
            }
        }

        TenantRegistryEntry? current = await GetStoredEntryAsync(stateKey, ct).ConfigureAwait(false);
        List<string>? currentIndex = await _daprClient
            .GetStateAsync<List<string>?>(StoreName, IndexKey, cancellationToken: ct)
            .ConfigureAwait(false);

        if (current is null && currentIndex?.Contains(tenantId, StringComparer.Ordinal) != true)
        {
            SetStatusCache(tenantId, null);
            _summaryCache?.Invalidate(tenantId);
            return;
        }

        throw new InvalidOperationException(
            $"Failed to remove tenant '{tenantId}' from registry after {MaxTenantRegistrationRetries} attempts due to concurrent registry transaction conflicts.");
    }

    private static string GetTenantStateKey(string tenantId)
        => $"tenant-registry-{tenantId}";

    private async Task<TenantRegistryEntry?> GetStoredEntryAsync(string stateKey, CancellationToken cancellationToken)
    {
        StoredTenantRegistryEntry? stored = await _daprClient
            .GetStateAsync<StoredTenantRegistryEntry?>(StoreName, stateKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return stored is null ? null : PersistenceModelMapper.ToContract(stored);
    }

    private async Task<(TenantRegistryEntry? Entry, string Etag)> GetStoredEntryAndETagAsync(
        string stateKey,
        CancellationToken cancellationToken)
    {
        (StoredTenantRegistryEntry? stored, string etag) = await _daprClient
            .GetStateAndETagAsync<StoredTenantRegistryEntry?>(StoreName, stateKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return (stored is null ? null : PersistenceModelMapper.ToContract(stored), etag);
    }

    private static List<string> NormalizeIndex(List<string>? index)
        => index is null ? [] : [.. index.Distinct(StringComparer.Ordinal)];

    private async Task<TenantRegistryEntry?> GetTenantEntryForStatusGuardAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (_statusCache.TryGetValue(tenantId, out (TenantRegistryEntry? Entry, DateTimeOffset ExpiresAt) entry) &&
            entry.ExpiresAt > now)
        {
            return entry.Entry;
        }

        // Capture the invalidation generation before the store read so a mutation (register, status
        // change, deletion, display-name update, removal) that runs while we fetch cannot be clobbered
        // by this read-through populate — which would re-hide a just-created tenant or serve a stale
        // status past the write (Story 24.2 review P2 / AC1).
        long generation = GetStatusGeneration(tenantId);
        TenantRegistryEntry? fresh = await GetTenantEntryAsync(tenantId, ct).ConfigureAwait(false);
        StoreStatusCacheIfCurrent(tenantId, fresh, generation);
        return fresh;
    }

    /// <summary>Authoritative status-cache write from a mutation path.</summary>
    private void SetStatusCache(string tenantId, TenantRegistryEntry? entry)
    {
        // Bump the generation so a concurrent in-flight read cannot re-cache a stale value on top of
        // this fresh, just-written one.
        _statusGenerations.AddOrUpdate(tenantId, 1L, static (_, current) => current + 1L);
        StoreStatusEntry(tenantId, entry);
    }

    /// <summary>Read-through populate that yields to any mutation that raced with the store read.</summary>
    private void StoreStatusCacheIfCurrent(string tenantId, TenantRegistryEntry? entry, long expectedGeneration)
    {
        if (GetStatusGeneration(tenantId) != expectedGeneration)
        {
            return;
        }

        StoreStatusEntry(tenantId, entry);
    }

    private void StoreStatusEntry(string tenantId, TenantRegistryEntry? entry)
    {
        TimeSpan ttl = entry is null
            ? _cacheOptions.Value.GetMissingTenantStatusTtl()
            : _cacheOptions.Value.GetTenantStatusTtl();
        DateTimeOffset now = _timeProvider.GetUtcNow();
        BoundedCache.PruneIfNeeded(_statusCache, _cacheOptions.Value.GetMaxCacheEntries(), now, static e => e.ExpiresAt);
        _statusCache[tenantId] = (entry, now + ttl);
    }

    private long GetStatusGeneration(string tenantId)
        => _statusGenerations.TryGetValue(tenantId, out long generation) ? generation : 0L;

    private static StateTransactionRequest CreateUpsertRequest<T>(string key, T value, string etag)
        => new(
            key,
            JsonSerializer.SerializeToUtf8Bytes(value, MemoriesPersistenceJsonContext.Options),
            StateOperationType.Upsert,
            etag,
            metadata: null!,
            options: null!);

    private static StateTransactionRequest CreateDeleteRequest(string key, string etag)
        => new(
            key,
            EmptyTransactionValue,
            StateOperationType.Delete,
            etag,
            metadata: null!,
            options: null!);

    private static string? ResolveWorkflowInstanceId(
        TenantStatus status,
        string? requestedWorkflowInstanceId,
        string? currentWorkflowInstanceId)
        => status switch
        {
            TenantStatus.Provisioning or TenantStatus.Deleting => requestedWorkflowInstanceId ?? currentWorkflowInstanceId,
            _ => null,
        };

    private static void ThrowIfDeletingClaimWouldBeClobbered(
        string tenantId,
        TenantStatus requestedStatus,
        string? requestedWorkflowInstanceId,
        TenantRegistryEntry current)
    {
        if (current.Tenant.Status != TenantStatus.Deleting || requestedStatus == TenantStatus.Deleting)
        {
            return;
        }

        if (string.Equals(current.WorkflowInstanceId, requestedWorkflowInstanceId, StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Tenant '{tenantId}' is owned by deletion workflow '{current.WorkflowInstanceId}' and cannot be overwritten by workflow '{requestedWorkflowInstanceId ?? "system"}'.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant '{TenantId}' registered with display name '{DisplayName}'")]
    private static partial void LogTenantRegistered(ILogger logger, string tenantId, string displayName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant '{TenantId}' status updated to {Status}")]
    private static partial void LogTenantStatusUpdated(ILogger logger, string tenantId, TenantStatus status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tenant '{TenantId}' not found in registry")]
    private static partial void LogTenantNotFound(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tenant '{TenantId}' already exists in registry")]
    private static partial void LogTenantAlreadyExists(ILogger logger, string tenantId);

    // Story 5.5 AC3 / FR42: operational log for tenant field updates. Field names are pinned
    // to the anticipated Phase 2 audit event contract so migration is a one-to-one remap.
    [LoggerMessage(EventId = 5501, Level = LogLevel.Information,
        Message = "Tenant operational log: {TenantId} field={Field} oldValue={OldValue} newValue={NewValue} actor={Actor} occurredAt={OccurredAt:o} durationMs={DurationMs}")]
    private static partial void LogTenantFieldUpdated(
        ILogger logger,
        string tenantId,
        string field,
        string oldValue,
        string newValue,
        string actor,
        DateTimeOffset occurredAt,
        long durationMs);
}
