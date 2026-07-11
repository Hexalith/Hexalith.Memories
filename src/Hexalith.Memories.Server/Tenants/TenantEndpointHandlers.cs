// <copyright file="TenantEndpointHandlers.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Testable minimal-API handlers for tenant configuration/listing endpoints introduced in Story 5.5.
/// Extracted from <c>Program.cs</c> so runtime branches can be unit-tested without spinning up a host.
/// </summary>
internal static class TenantEndpointHandlers
{
    /// <summary>Builds the enriched tenant summary used by <c>GET /api/v1/tenants</c>.</summary>
    /// <param name="tenant">The tenant record from the registry.</param>
    /// <param name="metrics">Tenant metrics service.</param>
    /// <param name="embeddingConfigProvider">Cached tenant embedding configuration provider.</param>
    /// <param name="summaryCache">Short-lived tenant summary cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enriched tenant summary.</returns>
    internal static async Task<TenantSummary> BuildTenantSummaryAsync(
        TenantInfo tenant,
        TenantMetricsService metrics,
        ITenantEmbeddingConfigProvider embeddingConfigProvider,
        TenantSummaryCache summaryCache,
        CancellationToken cancellationToken)
        => await summaryCache.GetOrCreateAsync(
            tenant.Id,
            () => BuildTenantSummaryCoreAsync(tenant, metrics, embeddingConfigProvider, cancellationToken)).ConfigureAwait(false);

    /// <summary>Builds tenant summaries with bounded enrichment concurrency.</summary>
    /// <param name="tenants">The tenants to summarize.</param>
    /// <param name="metrics">Tenant metrics service.</param>
    /// <param name="embeddingConfigProvider">Cached tenant embedding configuration provider.</param>
    /// <param name="summaryCache">Short-lived tenant summary cache.</param>
    /// <param name="maxConcurrency">Maximum concurrent tenant summary enrichments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant summaries in input order.</returns>
    internal static async Task<TenantSummary[]> BuildTenantSummariesAsync(
        IReadOnlyList<TenantInfo> tenants,
        TenantMetricsService metrics,
        ITenantEmbeddingConfigProvider embeddingConfigProvider,
        TenantSummaryCache summaryCache,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        int concurrency = Math.Clamp(maxConcurrency, 1, 32);
        using SemaphoreSlim gate = new(concurrency);
        Task<TenantSummary>[] tasks = tenants.Select(async tenant =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await BuildTenantSummaryAsync(
                    tenant,
                    metrics,
                    embeddingConfigProvider,
                    summaryCache,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }).ToArray();

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task<TenantSummary> BuildTenantSummaryCoreAsync(
        TenantInfo tenant,
        TenantMetricsService metrics,
        ITenantEmbeddingConfigProvider embeddingConfigProvider,
        CancellationToken cancellationToken)
    {
        Task<(TenantIndexSizes Sizes, TenantIndexStatus Status)> sizesTask = metrics.GetIndexSizesAsync(tenant.Id, cancellationToken);
        Task<long?> countTask = metrics.GetMemoryUnitCountAsync(tenant.Id, cancellationToken);
        Task<DateTimeOffset?> activityTask = metrics.GetLastActivityAtAsync(tenant.Id, cancellationToken);

        // Metrics have no reliable local write signal for all ingestion/indexing changes yet; the short TTL
        // bounds staleness while preserving degraded/null metric semantics on backend failures.
        bool reindexRequired = false;
        try
        {
            TenantEmbeddingConfig config = await embeddingConfigProvider.GetAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
            reindexRequired = config.ReindexRequired;
        }
        catch (Exception)
        {
            // Tolerate: tenant remains in the list with reindexRequired=false.
        }

        await Task.WhenAll(sizesTask, countTask, activityTask).ConfigureAwait(false);

        return new TenantSummary
        {
            Id = tenant.Id,
            DisplayName = tenant.DisplayName,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt,
            MemoryUnitCount = countTask.Result,
            IndexSizes = sizesTask.Result.Sizes,
            IndexStatus = sizesTask.Result.Status,
            ReindexRequired = reindexRequired,
            LastActivityAt = activityTask.Result,
        };
    }

    /// <summary>Handles <c>GET /api/v1/tenants/{tenantId}/configuration</c>.</summary>
    internal static async Task<IResult> GetTenantConfigurationAsync(
        TenantRegistryService registry,
        TenantStatusGuard tenantGuard,
        TenantMetricsService metrics,
        ITenantEmbeddingConfigProvider embeddingConfigProvider,
        string tenantId,
        CancellationToken cancellationToken)
    {
        ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
        if (tenantValidationError is not null)
        {
            return Results.BadRequest(tenantValidationError);
        }

        ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenantExistsError is not null)
        {
            return TenantStatusGuard.ToHttpResult(tenantExistsError);
        }

        TenantInfo? tenant = await registry.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Results.NotFound(CreateTenantNotFound(tenantId));
        }

        TenantEmbeddingConfig embeddingConfig;
        try
        {
            embeddingConfig = await embeddingConfigProvider.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (Dapr.DaprException)
        {
            return CreateDaprUnavailable();
        }

        Task<(TenantIndexSizes Sizes, TenantIndexStatus Status)> sizesTask = metrics.GetIndexSizesAsync(tenantId, cancellationToken);
        Task<long?> countTask = metrics.GetMemoryUnitCountAsync(tenantId, cancellationToken);
        Task<DateTimeOffset?> activityTask = metrics.GetLastActivityAtAsync(tenantId, cancellationToken);
        await Task.WhenAll(sizesTask, countTask, activityTask).ConfigureAwait(false);

        TenantConfigurationView view = new()
        {
            Id = tenant.Id,
            DisplayName = tenant.DisplayName,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt,
            LastActivityAt = activityTask.Result,
            MemoryUnitCount = countTask.Result,
            EmbeddingConfig = embeddingConfig,
            IndexStatus = sizesTask.Result.Status,
        };
        return Results.Ok(view);
    }

    /// <summary>Handles <c>PATCH /api/v1/tenants/{tenantId}</c> for display-name updates.</summary>
    internal static async Task<IResult> PatchDisplayNameAsync(
        TenantRegistryService registry,
        TenantStatusGuard tenantGuard,
        TenantMetricsService metrics,
        ITenantEmbeddingConfigProvider embeddingConfigProvider,
        TenantSummaryCache summaryCache,
        HttpContext httpContext,
        string tenantId,
        TenantUpdateInput? body,
        CancellationToken cancellationToken)
    {
        ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
        if (tenantValidationError is not null)
        {
            return Results.BadRequest(tenantValidationError);
        }

        if (body is null)
        {
            return Results.BadRequest(new ErrorResponse(
                "INVALID_INPUT",
                "Request body is required.",
                "Provide a JSON object with a non-empty displayName field."));
        }

        if (string.IsNullOrWhiteSpace(body.DisplayName))
        {
            return Results.BadRequest(new ErrorResponse(
                "INVALID_INPUT",
                "DisplayName must not be empty or whitespace.",
                "Provide a non-empty displayName value."));
        }

        if (body.DisplayName.Length > 100)
        {
            return Results.BadRequest(new ErrorResponse(
                "INVALID_INPUT",
                "DisplayName must be 100 characters or fewer.",
                "Shorten the displayName and retry."));
        }

        foreach (char c in body.DisplayName)
        {
            if (char.IsControl(c))
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_INPUT",
                    "DisplayName must not contain control characters.",
                    "Remove any control characters from the displayName."));
            }
        }

        ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenantStatusError is not null)
        {
            return TenantStatusGuard.ToHttpResult(tenantStatusError);
        }

        string remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string actor = $"operator@{remoteIp}";

        try
        {
            TenantInfo updated = await registry.UpdateTenantDisplayNameAsync(
                tenantId,
                actor,
                body.DisplayName,
                cancellationToken).ConfigureAwait(false);

            summaryCache.Invalidate(tenantId);
            TenantSummary summary = await BuildTenantSummaryAsync(
                updated,
                metrics,
                embeddingConfigProvider,
                summaryCache,
                cancellationToken).ConfigureAwait(false);
            return Results.Ok(summary);
        }
        catch (Dapr.DaprException)
        {
            return CreateDaprUnavailable();
        }
        catch (InvalidOperationException)
        {
            ErrorResponse? currentStateError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (currentStateError is not null)
            {
                return TenantStatusGuard.ToHttpResult(currentStateError);
            }

            return Results.Conflict(new ErrorResponse(
                "TENANT_UPDATE_CONFLICT",
                $"Tenant '{tenantId}' could not be updated because another operation changed it concurrently.",
                "Retry the PATCH request."));
        }
    }

    private static ErrorResponse? ValidateTenantId(string tenantId)
        => EndpointValidationHelpers.ValidateTenantId(tenantId);

    private static ErrorResponse CreateTenantNotFound(string tenantId)
        => ErrorResults.TenantNotFound(tenantId);

    private static IResult CreateDaprUnavailable()
        => ErrorResults.DaprUnavailableResult();
}
