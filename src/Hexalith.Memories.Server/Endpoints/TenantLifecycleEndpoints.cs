// <copyright file="TenantLifecycleEndpoints.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using System.Globalization;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.RateLimiting;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Workflows;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

using static Hexalith.Memories.Server.Endpoints.EndpointTelemetryHelpers;
using static Hexalith.Memories.Server.Endpoints.EndpointValidationHelpers;

/// <summary>Maps the Memories Server endpoints for this resource area.</summary>
internal static class TenantLifecycleEndpoints
{
    /// <summary>Maps this resource area's endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapTenantLifecycleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/tenants/{tenantId}/embedding-config", async (
            ITenantEmbeddingConfigProvider embeddingConfigProvider,
            TenantStatusGuard tenantGuard,
            string tenantId,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (tenantStatusError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantStatusError);
            }

            TenantEmbeddingConfig config = await embeddingConfigProvider.GetAsync(tenantId, cancellationToken);
            return Results.Ok(config);
        });

        app.MapPut("/api/tenants/{tenantId}/embedding-config",
            async (
                IActorProxyFactory actorProxyFactory,
                ITenantEmbeddingConfigProvider embeddingConfigProvider,
                TenantSummaryCache summaryCache,
                TenantStatusGuard tenantGuard,
                ILogger<AccessTelemetryCategory> auditLogger,
                HttpContext httpContext,
                string tenantId,
                TenantEmbeddingConfig config,
                CancellationToken cancellationToken,
                bool forceReindex = false) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.tenant_config");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationTenantConfig,
                successEventId: 7507,
                errorEventId: 7517,
                tenantId,
                caseId: null,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "embedding-config-update",
                    ["forceReindex"] = forceReindex,
                    ["fieldCount"] = 7,
                    ["changedFields"] = new[] { "provider", "model", "dimensions", "rateLimitPerMinute", "authMode", "baseUrlConfigured", "oidcConfigured" },
                });

            try
            {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                scope.MarkValidationError(tenantValidationError.Code);
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (tenantStatusError is not null)
            {
                scope.MarkTenantRejected(tenantStatusError.Code);
                return TenantStatusGuard.ToHttpResult(tenantStatusError);
            }

            try
            {
                EmbeddingProviderDefaults.Validate(config);
            }
            catch (ArgumentException ex)
            {
                scope.MarkValidationError("INVALID_CONFIG");
                return Results.BadRequest(new ErrorResponse("INVALID_CONFIG", ex.Message, "Fix the configuration values and retry."));
            }

            ITenantConfigurationActor actor = actorProxyFactory
                .CreateActorProxy<ITenantConfigurationActor>(new ActorId(tenantId), nameof(TenantConfigurationActor));

            try
            {
                await actor.SetEmbeddingConfigAsync(config, forceReindex);
                embeddingConfigProvider.Invalidate(tenantId);
                summaryCache.Invalidate(tenantId);
                TenantEmbeddingConfig updatedConfig = await embeddingConfigProvider.GetAsync(tenantId, cancellationToken);
                return Results.Ok(updatedConfig);
            }
            catch (EmbeddingConfigChangeException ex)
            {
                scope.MarkValidationError("EMBEDDING_CONFIG_CONFLICT");
                return Results.Conflict(CreateEmbeddingConfigConflictResponse(
                    ex.TenantId,
                    ex.CurrentConfig ?? EmbeddingProviderDefaults.Google(),
                    ex.ProposedConfig ?? config,
                    ex.AffectedFields));
            }
            catch (ActorMethodInvocationException) when (!forceReindex)
            {
                TenantEmbeddingConfig currentConfig = await actor.GetEmbeddingConfigAsync();
                string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(currentConfig, config);
                if (affectedFields.Length > 0)
                {
                    scope.MarkValidationError("EMBEDDING_CONFIG_CONFLICT");
                    return Results.Conflict(CreateEmbeddingConfigConflictResponse(
                        tenantId,
                        currentConfig,
                        config,
                        affectedFields));
                }

                throw;
            }
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        // Story 5.1: Tenant provisioning endpoints
        app.MapPost("/api/tenants", async (
            DaprWorkflowClient workflowClient,
            TenantProvisioningInput input,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            ILogger<global::Program> logger) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.tenant_lifecycle");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationTenantLifecycle,
                successEventId: 7506,
                errorEventId: 7516,
                input?.TenantId,
                caseId: null,
                CreateAuditQueryParams("tenant-create"));

            try
            {
            if (input is null)
            {
                scope.MarkValidationError("INVALID_INPUT");
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_INPUT",
                    "Request body is required.",
                    "Provide tenant provisioning details."));
            }

            ErrorResponse? tenantValidationError = ValidateTenantId(input.TenantId);
            if (tenantValidationError is not null)
            {
                scope.MarkValidationError(tenantValidationError.Code);
                return Results.BadRequest(tenantValidationError);
            }

            if (string.IsNullOrWhiteSpace(input.DisplayName))
            {
                scope.MarkValidationError("INVALID_INPUT");
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_INPUT",
                    "DisplayName is required.",
                    "Provide a non-empty display name for the tenant."));
            }

            if (input.VectorDimensions < 1 || input.VectorDimensions > 4096)
            {
                scope.MarkValidationError("INVALID_DIMENSIONS");
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_DIMENSIONS",
                    $"Vector dimensions {input.VectorDimensions} must be between 1 and 4096.",
                    "Provide a tenant provisioning vector dimension between 1 and 4096."));
            }

            string instanceId = $"provision-{input.TenantId}-{Guid.NewGuid():N}";
            try
            {
                await workflowClient.ScheduleNewWorkflowAsync(
                    nameof(TenantProvisioningWorkflow), instanceId, input);
            }
            catch (Dapr.DaprException)
            {
                scope.MarkValidationError("DAPR_UNAVAILABLE");
                return Results.Json(
                    new ErrorResponse(
                        "DAPR_UNAVAILABLE",
                        "DAPR sidecar is not ready.",
                        "Check service health via /healthz and retry."),
                    statusCode: 503);
            }

            return Results.Accepted($"/api/tenants/{input.TenantId}/provision-status/{instanceId}",
                new { workflowInstanceId = instanceId });
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapGet("/api/tenants/{tenantId}/provision-status/{instanceId}", async (
            DaprWorkflowClient workflowClient,
            TenantStatusGuard tenantGuard,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            string tenantId,
            string instanceId,
            CancellationToken cancellationToken) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.tenant_lifecycle");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationTenantLifecycle,
                successEventId: 7506,
                errorEventId: 7516,
                tenantId,
                caseId: null,
                CreateWorkflowStatusAuditQueryParams("tenant-provision-status", instanceId));

            try
            {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                scope.MarkValidationError(tenantValidationError.Code);
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                scope.MarkTenantRejected(tenantExistsError.Code);
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            if (!instanceId.StartsWith($"provision-{tenantId}-", StringComparison.Ordinal))
            {
                scope.MarkValidationError("PROVISIONING_STATUS_NOT_FOUND");
                return Results.NotFound(new ErrorResponse(
                    "PROVISIONING_STATUS_NOT_FOUND",
                    $"Provisioning workflow '{instanceId}' was not found for tenant '{tenantId}'.",
                    "Use the workflowInstanceId returned by POST /api/tenants for the same tenant."));
            }

            WorkflowState? state = await workflowClient.GetWorkflowStateAsync(instanceId);
            scope.QueryParams = new Dictionary<string, object?>(scope.QueryParams, StringComparer.Ordinal)
            {
                ["state"] = state?.RuntimeStatus.ToString(),
            };
            return state is null ? Results.NotFound() : Results.Ok(state);
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        // Story 5.5 AC1 / FR41: enriched tenant listing — per-tenant counts + index health + activity.
        // Contract change (pre-Gate-2): now returns TenantSummary[] instead of TenantInfo[].
        app.MapGet("/api/tenants", async (
            TenantRegistryService registry,
            TenantMetricsService metrics,
            ITenantEmbeddingConfigProvider embeddingConfigProvider,
            TenantSummaryCache summaryCache,
            IOptions<TenantReadCacheOptions> tenantReadCacheOptions,
            HttpContext httpContext,
            [FromQuery] int offset = 0,
            [FromQuery] int? limit = null,
            CancellationToken cancellationToken = default) =>
        {
            TenantListPage page = await registry.ListTenantsPageAsync(offset, limit, cancellationToken);
            httpContext.Response.Headers["X-Hexalith-Total-Count"] = page.TotalCount.ToString(CultureInfo.InvariantCulture);
            httpContext.Response.Headers["X-Hexalith-Offset"] = page.Offset.ToString(CultureInfo.InvariantCulture);
            httpContext.Response.Headers["X-Hexalith-Limit"] = page.Limit.ToString(CultureInfo.InvariantCulture);
            httpContext.Response.Headers["X-Hexalith-Has-More"] = page.HasMore ? "true" : "false";
            TenantSummary[] summaries = await TenantEndpointHandlers.BuildTenantSummariesAsync(
                page.Tenants,
                metrics,
                embeddingConfigProvider,
                summaryCache,
                tenantReadCacheOptions.Value.GetMaxTenantListConcurrency(),
                cancellationToken);
            return Results.Ok(summaries);
        });

        app.MapGet("/api/tenants/{tenantId}", async (TenantRegistryService registry, string tenantId) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            TenantInfo? tenant = await registry.GetTenantAsync(tenantId, CancellationToken.None);
            return tenant is null
                ? Results.NotFound(new ErrorResponse(
                    "TENANT_NOT_FOUND",
                    $"Tenant '{tenantId}' not found.",
                    "Use GET /api/tenants to list available tenants."))
                : Results.Ok(tenant);
        });

        // Story 5.5 AC2 / FR45: composed configuration view (embedding + metrics + health).
        app.MapGet("/api/tenants/{tenantId}/configuration", TenantEndpointHandlers.GetTenantConfigurationAsync);

        // Story 5.5 AC3 / FR42: PATCH display name (rate-limit updates go through PUT /embedding-config).
        app.MapPatch("/api/tenants/{tenantId}", async (
            TenantRegistryService registry,
            TenantStatusGuard tenantGuard,
            TenantMetricsService metrics,
            ITenantEmbeddingConfigProvider embeddingConfigProvider,
            TenantSummaryCache summaryCache,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            string tenantId,
            TenantUpdateInput? body,
            CancellationToken cancellationToken) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.tenant_config");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationTenantConfig,
                successEventId: 7507,
                errorEventId: 7517,
                tenantId,
                caseId: null,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "display-name-update",
                    ["fieldCount"] = 1,
                    ["changedFields"] = new[] { "displayName" },
                });

            try
            {
                IResult result = await TenantEndpointHandlers.PatchDisplayNameAsync(
                    registry,
                    tenantGuard,
                    metrics,
                    embeddingConfigProvider,
                    summaryCache,
                    httpContext,
                    tenantId,
                    body,
                    cancellationToken);
                MarkAuditFromHttpResult(scope, result);
                return result;
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        // Story 5.2: Tenant deletion endpoints
        app.MapDelete("/api/tenants/{tenantId}", async (
            DaprWorkflowClient workflowClient,
            TenantRegistryService registry,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            string tenantId) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity(MemoriesActivitySource.DeleteRequest);
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationDelete,
                successEventId: 7505,
                errorEventId: 7515,
                tenantId,
                caseId: null,
                CreateAuditQueryParams("tenant-delete"));

            try
            {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                scope.MarkValidationError(tenantValidationError.Code);
                return Results.BadRequest(tenantValidationError);
            }

            TenantRegistryEntry? tenantEntry = await registry.GetTenantEntryAsync(tenantId, CancellationToken.None);
            if (tenantEntry is null)
            {
                scope.MarkTenantRejected("TENANT_NOT_FOUND");
                return Results.NotFound(new ErrorResponse(
                    "TENANT_NOT_FOUND",
                    $"Tenant '{tenantId}' not found.",
                    "Use GET /api/tenants to list available tenants."));
            }

            if (tenantEntry.Tenant.Status == TenantStatus.Provisioning)
            {
                scope.MarkValidationError("TENANT_PROVISIONING");
                return Results.Conflict(new ErrorResponse(
                    "TENANT_PROVISIONING",
                    $"Tenant '{tenantId}' is still provisioning.",
                    "Wait for provisioning to complete."));
            }

            if (tenantEntry.Tenant.Status == TenantStatus.Deleting &&
                !string.IsNullOrWhiteSpace(tenantEntry.WorkflowInstanceId))
            {
                try
                {
                    WorkflowState? existingState = await workflowClient.GetWorkflowStateAsync(tenantEntry.WorkflowInstanceId);
                    if (existingState?.Exists == true && !existingState.IsWorkflowCompleted)
                    {
                        return Results.Accepted(
                            $"/api/tenants/{tenantId}/deletion-status/{tenantEntry.WorkflowInstanceId}",
                            new
                            {
                                workflowInstanceId = tenantEntry.WorkflowInstanceId,
                                message = "Deletion already in progress.",
                            });
                    }
                }
                catch (Dapr.DaprException)
                {
                    scope.MarkValidationError("DAPR_UNAVAILABLE");
                    return Results.Json(
                        new ErrorResponse(
                            "DAPR_UNAVAILABLE",
                            "DAPR sidecar is not ready.",
                            "Check service health via /healthz and retry."),
                        statusCode: 503);
                }
            }

            string instanceId = $"delete-{tenantId}-{Guid.NewGuid():N}";
            TenantRegistryEntry? deletionClaim = await registry.BeginTenantDeletionAsync(
                tenantId,
                instanceId,
                allowRetryFromDeleting: tenantEntry.Tenant.Status == TenantStatus.Deleting,
                tenantEntry.WorkflowInstanceId,
                CancellationToken.None);

            if (deletionClaim is null)
            {
                scope.MarkTenantRejected("TENANT_NOT_FOUND");
                return Results.NotFound(new ErrorResponse(
                    "TENANT_NOT_FOUND",
                    $"Tenant '{tenantId}' not found.",
                    "Use GET /api/tenants to list available tenants."));
            }

            if (deletionClaim.Tenant.Status == TenantStatus.Provisioning)
            {
                scope.MarkValidationError("TENANT_PROVISIONING");
                return Results.Conflict(new ErrorResponse(
                    "TENANT_PROVISIONING",
                    $"Tenant '{tenantId}' is still provisioning.",
                    "Wait for provisioning to complete."));
            }

            if (!string.Equals(deletionClaim.WorkflowInstanceId, instanceId, StringComparison.Ordinal))
            {
                return Results.Accepted(
                    $"/api/tenants/{tenantId}/deletion-status/{deletionClaim.WorkflowInstanceId}",
                    new
                    {
                        workflowInstanceId = deletionClaim.WorkflowInstanceId,
                        message = "Deletion already in progress.",
                    });
            }

            try
            {
                await workflowClient.ScheduleNewWorkflowAsync(
                    nameof(TenantDeletionWorkflow), instanceId, new TenantDeletionInput(tenantId));
            }
            catch (Dapr.DaprException)
            {
                scope.MarkValidationError("DAPR_UNAVAILABLE");
                if (tenantEntry.Tenant.Status != TenantStatus.Deleting)
                {
                    try
                    {
                        await registry.UpdateTenantStatusAsync(
                            tenantId,
                            tenantEntry.Tenant.Status,
                            CancellationToken.None,
                            instanceId);
                    }
                    catch (InvalidOperationException)
                    {
                        // Best effort rollback only — the original Dapr error is more actionable to callers.
                    }
                }

                return Results.Json(
                    new ErrorResponse(
                        "DAPR_UNAVAILABLE",
                        "DAPR sidecar is not ready.",
                        "Check service health via /healthz and retry."),
                    statusCode: 503);
            }

            return Results.Accepted($"/api/tenants/{tenantId}/deletion-status/{instanceId}",
                new { workflowInstanceId = instanceId });
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapGet("/api/tenants/{tenantId}/deletion-status/{instanceId}", async (
            DaprWorkflowClient workflowClient,
            TenantStatusGuard tenantGuard,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            string tenantId,
            string instanceId,
            CancellationToken cancellationToken) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.tenant_lifecycle");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationTenantLifecycle,
                successEventId: 7506,
                errorEventId: 7516,
                tenantId,
                caseId: null,
                CreateWorkflowStatusAuditQueryParams("tenant-deletion-status", instanceId));

            try
            {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                scope.MarkValidationError(tenantValidationError.Code);
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                scope.MarkTenantRejected(tenantExistsError.Code);
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            if (!instanceId.StartsWith($"delete-{tenantId}-", StringComparison.Ordinal))
            {
                scope.MarkValidationError("DELETION_STATUS_NOT_FOUND");
                return Results.NotFound(new ErrorResponse(
                    "DELETION_STATUS_NOT_FOUND",
                    $"Deletion workflow '{instanceId}' was not found for tenant '{tenantId}'.",
                    "Use the workflowInstanceId returned by DELETE /api/tenants/{tenantId} for the same tenant."));
            }

            WorkflowState? state = await workflowClient.GetWorkflowStateAsync(instanceId);
            scope.QueryParams = new Dictionary<string, object?>(scope.QueryParams, StringComparer.Ordinal)
            {
                ["state"] = state?.RuntimeStatus.ToString(),
            };
            return state is null ? Results.NotFound() : Results.Ok(state);
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        // Story 5.3: Tenant isolation verification
        app.MapPost("/api/tenants/{tenantId}/verify", async (
            TenantIsolationVerifier verifier,
            TenantStatusGuard tenantGuard,
            string tenantId,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            // Verification is diagnostic — allow it on non-Active tenants (Provisioning, Deleting, Failed)
            // as long as the tenant exists in the registry. Existence-only check is correct here.
            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            try
            {
                TenantIsolationVerificationResult result = await verifier.VerifyAsync(tenantId, cancellationToken);
                return Results.Ok(result);
            }
            catch (Dapr.DaprException ex)
            {
                return Results.Json(
                    new ErrorResponse("DAPR_UNAVAILABLE", $"DAPR sidecar unavailable: {ex.Message}", "Check DAPR sidecar connectivity and retry."),
                    statusCode: 503);
            }
            catch (RedisException ex)
            {
                return Results.Json(
                    new ErrorResponse("BACKEND_UNAVAILABLE", $"Backend unavailable: {ex.Message}", "Check Redis/FalkorDB connectivity and retry."),
                    statusCode: 503);
            }
        });

        // Story 7.5 — telemetry summary endpoint (AC #6). Operator-facing read-only poke; DOES NOT emit
        // an AccessTelemetryEvent for itself (Task 5.5 — self-referential audit noise).
        app.MapGet("/api/tenants/{tenantId}/telemetry/summary", async (
            string tenantId,
            TelemetrySummaryService summaryService,
            TenantStatusGuard tenantGuard,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (tenantStatusError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantStatusError);
            }

            TelemetrySummary summary = await summaryService.GetSummaryAsync(tenantId, cancellationToken);
            return Results.Ok(summary);
        });

        // Story 9.3 — handler registry + mismatch detector endpoints. Experimental HXL002 surface.
        // Story 20.1: Server fallback authorization now requires bearer authentication for these API routes.
        app.MapGet("/api/handlers", async (
            HttpContext http,
            Hexalith.Memories.Server.Handlers.HandlerRegistryService registryService,
            CancellationToken cancellationToken) =>
        {
            http.Response.Headers.Append("X-Memories-API-Experimental", "HXL002");
            HandlerRegistrationSnapshot snapshot = await registryService.GetSnapshotAsync(cancellationToken);
            return Results.Ok(snapshot);
        });

        app.MapGet("/api/tenants/{tenantId}/handlers/mismatches", async (
            HttpContext http,
            string tenantId,
            Hexalith.Memories.Server.Handlers.HandlerMismatchDetector detector,
            TenantStatusGuard tenantGuard,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (tenantStatusError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantStatusError);
            }

            http.Response.Headers.Append("X-Memories-API-Experimental", "HXL002");
            HandlerMismatchReport report = await detector.DetectAsync(
                tenantId, Hexalith.Memories.Server.Handlers.HandlerRegistryService.ObservationWindow, cancellationToken);
            return Results.Ok(report);
        });

        return app;
    }

    private static object CreateEmbeddingConfigConflictResponse(
        string tenantId,
        TenantEmbeddingConfig currentConfig,
        TenantEmbeddingConfig proposedConfig,
        string[] affectedFields)
    {
        EmbeddingConfigChangeException exception = new(
            tenantId,
            currentConfig,
            proposedConfig,
            affectedFields);

        return new
        {
            error = "EmbeddingConfigChangeRequired",
            message = exception.Message,
            currentConfig,
            proposedConfig,
            affectedFields,
        };
    }
}
