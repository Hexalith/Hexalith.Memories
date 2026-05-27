// <copyright file="QuickstartTenantProvisioner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Idempotent sample-tenant provisioning for wizard step 4. Checks whether the tenant already
/// exists via <see cref="MemoriesClient.GetTenantAsync(string, CancellationToken)"/>; if not, calls
/// the experimental <c>CreateTenantAsync</c> and waits for the tenant to reach
/// <see cref="TenantStatus.Active"/> via short polling.
/// </summary>
public sealed class QuickstartTenantProvisioner
{
    private static readonly TimeSpan ProvisionPollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ProvisionTimeout = TimeSpan.FromSeconds(30);

    private readonly MemoriesClient _client;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="QuickstartTenantProvisioner"/> class.</summary>
    /// <param name="client">The REST client.</param>
    /// <param name="timeProvider">The time provider (inject a fake in tests).</param>
    public QuickstartTenantProvisioner(MemoriesClient client, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _client = client;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Ensures a sample tenant with <paramref name="tenantId"/> exists and is active. Returns a result
    /// indicating whether the tenant was freshly created or already existed (idempotent rerun —
    /// ADR-7.4-004).
    /// </summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The provisioning result.</returns>
    public async Task<QuickstartTenantResult> EnsureSampleTenantAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        TenantInfo? existing = await _client.GetTenantAsync(tenantId, ct).ConfigureAwait(false);
        if (existing is { Status: TenantStatus.Active })
        {
            return new QuickstartTenantResult(
                Created: false,
                AlreadyExisted: true,
                ErrorCode: null,
                Diagnostic: $"Sample tenant '{tenantId}' already exists — continuing.");
        }

        if (existing is { Status: TenantStatus.Provisioning })
        {
            // Prior run may have left the tenant mid-provision; wait for it to land.
            return await WaitForActiveAsync(tenantId, startedFresh: false, ct).ConfigureAwait(false);
        }

        if (existing is { Status: TenantStatus.Deleting })
        {
            return new QuickstartTenantResult(
                Created: false,
                AlreadyExisted: false,
                ErrorCode: "TENANT_DELETING",
                Diagnostic: $"Tenant '{tenantId}' is being deleted.");
        }

        // Tenant missing (null) or in a retryable Failed state — schedule provisioning.
        string tenantDisplay = $"Quickstart sample ({tenantId})";

        try
        {
#pragma warning disable HXL001 // Story 7.4 uses the experimental tenant-create client method by design.
            await _client.CreateTenantAsync(tenantId, tenantDisplay, ct).ConfigureAwait(false);
#pragma warning restore HXL001
        }
        catch (MemoriesRemoteException ex) when (IsAlreadyExists(ex.Error.Code))
        {
            // Concurrent-rerun race: another run created the tenant between our GetTenantAsync
            // null-check and CreateTenantAsync. Fall through to WaitForActiveAsync as if we had
            // observed the existing tenant.
            return await WaitForActiveAsync(tenantId, startedFresh: false, ct).ConfigureAwait(false);
        }

        return await WaitForActiveAsync(tenantId, startedFresh: true, ct).ConfigureAwait(false);
    }

    private static bool IsAlreadyExists(string? code)
        => string.Equals(code, "TENANT_ALREADY_EXISTS", StringComparison.Ordinal)
            || string.Equals(code, "DUPLICATE_TENANT", StringComparison.Ordinal)
            || string.Equals(code, "CONFLICT", StringComparison.Ordinal);

    private async Task<QuickstartTenantResult> WaitForActiveAsync(string tenantId, bool startedFresh, CancellationToken ct)
    {
        long startTimestamp = _timeProvider.GetTimestamp();

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            TenantInfo? tenant = await _client.GetTenantAsync(tenantId, ct).ConfigureAwait(false);
            if (tenant is { Status: TenantStatus.Active })
            {
                string diagnostic = startedFresh
                    ? $"Created tenant '{tenantId}'."
                    : $"Sample tenant '{tenantId}' already existed — waited for Active status.";
                return new QuickstartTenantResult(
                    Created: startedFresh,
                    AlreadyExisted: !startedFresh,
                    ErrorCode: null,
                    Diagnostic: diagnostic);
            }

            if (tenant is { Status: TenantStatus.Failed or TenantStatus.CompensationFailed })
            {
                return new QuickstartTenantResult(
                    Created: false,
                    AlreadyExisted: false,
                    ErrorCode: "TENANT_FAILED",
                    Diagnostic: $"Tenant '{tenantId}' provisioning failed (status: {tenant.Status}).");
            }

            if (tenant is { Status: TenantStatus.Deleting })
            {
                return new QuickstartTenantResult(
                    Created: false,
                    AlreadyExisted: false,
                    ErrorCode: "TENANT_DELETING",
                    Diagnostic: $"Tenant '{tenantId}' is being deleted.");
            }

            if (_timeProvider.GetElapsedTime(startTimestamp) >= ProvisionTimeout)
            {
                return new QuickstartTenantResult(
                    Created: false,
                    AlreadyExisted: false,
                    ErrorCode: "TENANT_PROVISIONING",
                    Diagnostic: $"Tenant '{tenantId}' did not become Active within {ProvisionTimeout.TotalSeconds:F0}s.");
            }

            try
            {
                await Task.Delay(ProvisionPollInterval, _timeProvider, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
        }
    }
}

/// <summary>
/// Outcome of a quickstart tenant-provisioning call.
/// </summary>
/// <param name="Created">True when the wizard freshly created the tenant in this run.</param>
/// <param name="AlreadyExisted">True when the tenant was already active before this run.</param>
/// <param name="ErrorCode">Catalog or synthetic code on failure; null on success.</param>
/// <param name="Diagnostic">Human-readable step outcome message.</param>
public sealed record QuickstartTenantResult(
    bool Created,
    bool AlreadyExisted,
    string? ErrorCode,
    string Diagnostic);
