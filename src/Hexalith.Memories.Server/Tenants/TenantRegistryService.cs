// <copyright file="TenantRegistryService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using System.Diagnostics;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

/// <summary>Manages the tenant registry using DAPR state store.</summary>
public sealed partial class TenantRegistryService
{
    private const string StoreName = "statestore";
    private const string IndexKey = "tenant-registry-index";
    private const int MaxIndexRetries = 3;
    private const int MaxTenantRegistrationRetries = 3;
    private const int MaxDeletionStartRetries = 3;

    private readonly DaprClient _daprClient;
    private readonly ILogger<TenantRegistryService> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantRegistryService"/> class.</summary>
    /// <param name="daprClient">The DAPR client for state management.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantRegistryService(DaprClient daprClient, ILogger<TenantRegistryService> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
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

        string stateKey = GetTenantStateKey(tenantId);
        TenantRegistryEntry entry = new(
            new TenantInfo(tenantId, displayName, TenantStatus.Provisioning, DateTimeOffset.UtcNow),
            workflowInstanceId);

        for (int attempt = 0; attempt < MaxTenantRegistrationRetries; attempt++)
        {
            (TenantRegistryEntry? existing, string etag) = await _daprClient
                .GetStateAndETagAsync<TenantRegistryEntry?>(StoreName, stateKey, cancellationToken: ct)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                return existing;
            }

            bool saved = await _daprClient
                .TrySaveStateAsync(StoreName, stateKey, entry, etag, cancellationToken: ct)
                .ConfigureAwait(false);

            if (!saved)
            {
                continue;
            }

            try
            {
                await AddToIndexAsync(tenantId, ct).ConfigureAwait(false);
            }
            catch
            {
                await _daprClient.DeleteStateAsync(StoreName, stateKey, cancellationToken: ct).ConfigureAwait(false);
                throw;
            }

            LogTenantRegistered(_logger, tenantId, displayName);
            return entry;
        }

        TenantRegistryEntry? current = await _daprClient
            .GetStateAsync<TenantRegistryEntry?>(StoreName, stateKey, cancellationToken: ct)
            .ConfigureAwait(false);

        return current
            ?? throw new InvalidOperationException(
                $"Failed to register tenant '{tenantId}' after {MaxTenantRegistrationRetries} attempts due to concurrent updates.");
    }

    /// <summary>Gets the full tenant registry entry by its identifier.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registry entry, or null if not found.</returns>
    public async Task<TenantRegistryEntry?> GetTenantEntryAsync(string tenantId, CancellationToken ct)
    {
        string stateKey = GetTenantStateKey(tenantId);
        TenantRegistryEntry? entry = await _daprClient
            .GetStateAsync<TenantRegistryEntry?>(StoreName, stateKey, cancellationToken: ct)
            .ConfigureAwait(false);

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

    /// <summary>Updates the status of an existing tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task UpdateTenantStatusAsync(string tenantId, TenantStatus status, CancellationToken ct, string? workflowInstanceId = null)
    {
        string stateKey = GetTenantStateKey(tenantId);
        TenantRegistryEntry? entry = await _daprClient
            .GetStateAsync<TenantRegistryEntry?>(StoreName, stateKey, cancellationToken: ct)
            .ConfigureAwait(false);

        if (entry is null)
        {
            LogTenantNotFound(_logger, tenantId);
            throw new InvalidOperationException($"Tenant '{tenantId}' not found in registry.");
        }

        TenantInfo updated = entry.Tenant with { Status = status };
        TenantRegistryEntry updatedEntry = entry with
        {
            Tenant = updated,
            WorkflowInstanceId = status == TenantStatus.Provisioning ? workflowInstanceId ?? entry.WorkflowInstanceId : null,
        };
        await _daprClient.SaveStateAsync(StoreName, stateKey, updatedEntry, cancellationToken: ct).ConfigureAwait(false);

        LogTenantStatusUpdated(_logger, tenantId, status);
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

        for (int attempt = 0; attempt < MaxDeletionStartRetries; attempt++)
        {
            (TenantRegistryEntry? existing, string etag) = await _daprClient
                .GetStateAndETagAsync<TenantRegistryEntry?>(StoreName, stateKey, cancellationToken: ct)
                .ConfigureAwait(false);

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

            TenantRegistryEntry updated = existing with
            {
                Tenant = existing.Tenant with { Status = TenantStatus.Deleting },
                WorkflowInstanceId = workflowInstanceId,
            };

            bool saved = await _daprClient
                .TrySaveStateAsync(StoreName, stateKey, updated, etag, cancellationToken: ct)
                .ConfigureAwait(false);

            if (saved)
            {
                LogTenantStatusUpdated(_logger, tenantId, TenantStatus.Deleting);
                return updated;
            }
        }

        return await _daprClient
            .GetStateAsync<TenantRegistryEntry?>(StoreName, stateKey, cancellationToken: ct)
            .ConfigureAwait(false);
    }

    /// <summary>Lists all registered tenants.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of all registered tenants.</returns>
    public async Task<IReadOnlyList<TenantInfo>> ListTenantsAsync(CancellationToken ct)
    {
        List<string>? index = await _daprClient.GetStateAsync<List<string>?>(StoreName, IndexKey, cancellationToken: ct).ConfigureAwait(false);

        if (index is null || index.Count == 0)
        {
            return [];
        }

        List<TenantInfo> tenants = [];
        foreach (string tenantId in index)
        {
            TenantRegistryEntry? entry = await _daprClient
                .GetStateAsync<TenantRegistryEntry?>(StoreName, GetTenantStateKey(tenantId), cancellationToken: ct)
                .ConfigureAwait(false);
            if (entry is not null)
            {
                tenants.Add(entry.Tenant);
            }
        }

        return tenants;
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
            (TenantRegistryEntry? existing, string etag) = await _daprClient
                .GetStateAndETagAsync<TenantRegistryEntry?>(StoreName, stateKey, cancellationToken: ct)
                .ConfigureAwait(false);

            if (existing is null)
            {
                LogTenantNotFound(_logger, tenantId);
                throw new InvalidOperationException($"Tenant '{tenantId}' not found in registry.");
            }

            if (existing.Tenant.Status != TenantStatus.Active)
            {
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
                return existing.Tenant;
            }

            TenantRegistryEntry updated = existing with { Tenant = existing.Tenant with { DisplayName = displayName } };
            bool saved = await _daprClient
                .TrySaveStateAsync(StoreName, stateKey, updated, etag, cancellationToken: ct)
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
        await _daprClient.DeleteStateAsync(StoreName, stateKey, cancellationToken: ct).ConfigureAwait(false);
        await RemoveFromIndexAsync(tenantId, ct).ConfigureAwait(false);
    }

    private static string GetTenantStateKey(string tenantId)
        => $"tenant-registry-{tenantId}";

    private async Task AddToIndexAsync(string tenantId, CancellationToken ct)
    {
        for (int attempt = 0; attempt < MaxIndexRetries; attempt++)
        {
            (List<string> index, string etag) = await _daprClient.GetStateAndETagAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct).ConfigureAwait(false);
            index ??= [];

            if (!index.Contains(tenantId))
            {
                index.Add(tenantId);
            }

            bool saved = await _daprClient.TrySaveStateAsync(StoreName, IndexKey, index, etag, cancellationToken: ct).ConfigureAwait(false);
            if (saved)
            {
                return;
            }
        }

        throw new InvalidOperationException($"Failed to add tenant '{tenantId}' to registry index after {MaxIndexRetries} attempts due to concurrent updates.");
    }

    private async Task RemoveFromIndexAsync(string tenantId, CancellationToken ct)
    {
        for (int attempt = 0; attempt < MaxIndexRetries; attempt++)
        {
            (List<string> index, string etag) = await _daprClient.GetStateAndETagAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct).ConfigureAwait(false);
            if (index is null)
            {
                return;
            }

            _ = index.Remove(tenantId);

            bool saved = await _daprClient.TrySaveStateAsync(StoreName, IndexKey, index, etag, cancellationToken: ct).ConfigureAwait(false);
            if (saved)
            {
                return;
            }
        }

        throw new InvalidOperationException($"Failed to remove tenant '{tenantId}' from registry index after {MaxIndexRetries} attempts due to concurrent updates.");
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
