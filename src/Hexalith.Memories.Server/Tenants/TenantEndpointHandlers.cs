// <copyright file="TenantEndpointHandlers.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Actors;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Testable minimal-API handlers for tenant configuration/listing endpoints introduced in Story 5.5.
/// Extracted from <c>Program.cs</c> so runtime branches can be unit-tested without spinning up a host.
/// </summary>
internal static class TenantEndpointHandlers
{
    /// <summary>Builds the enriched tenant summary used by <c>GET /api/tenants</c>.</summary>
    /// <param name="tenant">The tenant record from the registry.</param>
    /// <param name="metrics">Tenant metrics service.</param>
    /// <param name="actorProxyFactory">Actor proxy factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enriched tenant summary.</returns>
    internal static async Task<TenantSummary> BuildTenantSummaryAsync(
        TenantInfo tenant,
        TenantMetricsService metrics,
        IActorProxyFactory actorProxyFactory,
        CancellationToken cancellationToken)
    {
        Task<(TenantIndexSizes Sizes, TenantIndexStatus Status)> sizesTask = metrics.GetIndexSizesAsync(tenant.Id, cancellationToken);
        Task<long?> countTask = metrics.GetMemoryUnitCountAsync(tenant.Id, cancellationToken);
        Task<DateTimeOffset?> activityTask = metrics.GetLastActivityAtAsync(tenant.Id, cancellationToken);

        // Deferred by review decision: keep the actor-proxy fallback until the direct state-store key
        // format is empirically verified. Per-tenant failures still collapse to reindexRequired=false.
        bool reindexRequired = false;
        try
        {
            ITenantConfigurationActor configActor = actorProxyFactory
                .CreateActorProxy<ITenantConfigurationActor>(new ActorId(tenant.Id), nameof(TenantConfigurationActor));
            TenantEmbeddingConfig config = await configActor.GetEmbeddingConfigAsync().ConfigureAwait(false);
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

    /// <summary>Handles <c>GET /api/tenants/{tenantId}/configuration</c>.</summary>
    internal static async Task<IResult> GetTenantConfigurationAsync(
        TenantRegistryService registry,
        TenantStatusGuard tenantGuard,
        TenantMetricsService metrics,
        IActorProxyFactory actorProxyFactory,
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
            ITenantConfigurationActor actor = actorProxyFactory
                .CreateActorProxy<ITenantConfigurationActor>(new ActorId(tenantId), nameof(TenantConfigurationActor));
            embeddingConfig = await actor.GetEmbeddingConfigAsync().ConfigureAwait(false);
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

    /// <summary>Handles <c>PATCH /api/tenants/{tenantId}</c> for display-name updates.</summary>
    internal static async Task<IResult> PatchDisplayNameAsync(
        TenantRegistryService registry,
        TenantStatusGuard tenantGuard,
        TenantMetricsService metrics,
        IActorProxyFactory actorProxyFactory,
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

            TenantSummary summary = await BuildTenantSummaryAsync(updated, metrics, actorProxyFactory, cancellationToken).ConfigureAwait(false);
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
    {
        try
        {
            TenantIdGuard.Validate(tenantId);
            return null;
        }
        catch (ArgumentException ex)
        {
            return new ErrorResponse(
                "INVALID_TENANT_ID",
                ex.Message,
                "Use only alphanumeric characters and hyphens for tenant identifiers.");
        }
    }

    private static ErrorResponse CreateTenantNotFound(string tenantId)
        => new(
            "TENANT_NOT_FOUND",
            $"Tenant '{tenantId}' not found.",
            "Use GET /api/tenants to list available tenants.");

    private static IResult CreateDaprUnavailable()
        => Results.Json(
            new ErrorResponse(
                "DAPR_UNAVAILABLE",
                "DAPR sidecar is not ready.",
                "Check service health via /healthz and retry."),
            statusCode: 503);
}