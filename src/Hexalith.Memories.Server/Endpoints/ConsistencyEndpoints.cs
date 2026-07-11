// <copyright file="ConsistencyEndpoints.cs" company="ITANEO">
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
internal static class ConsistencyEndpoints
{
    /// <summary>Maps this resource area's endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapConsistencyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Story 8.2: consistency verification & repair endpoints
        app.MapPost(MemoriesRoutes.ConsistencyVerify, async (
            IConsistencyWorkflowService workflowService,
            TenantStatusGuard tenantGuard,
            string tenantId,
            ConsistencyVerificationRequest? request,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            int batchSize = request?.BatchSize ?? 500;
            if (batchSize < ConsistencyVerificationWorkflow.MinBatchSize ||
                batchSize > ConsistencyVerificationWorkflow.MaxBatchSize)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_BATCH_SIZE",
                    $"BatchSize {batchSize} is out of range.",
                    $"Use a value between {ConsistencyVerificationWorkflow.MinBatchSize} and {ConsistencyVerificationWorkflow.MaxBatchSize}."));
            }

            // Consistency endpoints are diagnostic — allow on non-Active tenants.
            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            string instanceId = $"verify-consistency-{tenantId}-{Guid.NewGuid():N}";

            try
            {
                await workflowService.ScheduleVerificationAsync(
                    instanceId,
                    new ConsistencyVerificationInput(tenantId, batchSize),
                    cancellationToken);
            }
            catch (Dapr.DaprException ex)
            {
                return ErrorResults.DaprUnavailableResult(
                    $"DAPR sidecar unavailable: {ex.Message}",
                    "Check DAPR sidecar connectivity and retry.");
            }

            return Results.Accepted(
                MemoriesRoutes.ConsistencyVerifyStatusLocation(tenantId, instanceId),
                new { workflowInstanceId = instanceId });
        });

        app.MapGet(MemoriesRoutes.ConsistencyVerifyStatus, async (
            IConsistencyWorkflowService workflowService,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string instanceId,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            if (!instanceId.StartsWith($"verify-consistency-{tenantId}-", StringComparison.Ordinal))
            {
                return Results.NotFound(new ErrorResponse(
                    "CONSISTENCY_VERIFY_NOT_FOUND",
                    $"Verification workflow '{instanceId}' was not found for tenant '{tenantId}'.",
                    $"Use the workflowInstanceId returned by POST {MemoriesRoutes.ConsistencyVerify} for the same tenant."));
            }

            ConsistencyVerificationStatus? status = await workflowService.GetVerificationStatusAsync(instanceId, cancellationToken);
            return status is null ? Results.NotFound() : Results.Ok(status);
        });

        app.MapGet(MemoriesRoutes.ConsistencyInspect, async (
            IConsistencyInspectionService inspectionService,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string memoryUnitId,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            try
            {
                ConsistencyInspectionResult result = await inspectionService.InspectAsync(
                    tenantId, memoryUnitId, cancellationToken);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_MEMORY_UNIT_ID",
                    ex.Message,
                    "Memory unit IDs must be 26-character Crockford-base32 ULIDs or GUIDs (hyphenated or 32-hex)."));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new ErrorResponse(
                    "MEMORY_UNIT_NOT_FOUND",
                    ex.Message,
                    "Run 'memories consistency verify' to audit the tenant or verify the ID via the ingest system."));
            }
            catch (RedisException ex)
            {
                return ErrorResults.BackendUnavailableResult($"Backend unavailable: {ex.Message}");
            }
        });

        app.MapPost(MemoriesRoutes.ConsistencyRepair, async (
            IConsistencyWorkflowService workflowService,
            TenantStatusGuard tenantGuard,
            string tenantId,
            ConsistencyRepairRequest? request,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            int batchSize = request?.BatchSize ?? 500;
            if (batchSize < ConsistencyVerificationWorkflow.MinBatchSize ||
                batchSize > ConsistencyVerificationWorkflow.MaxBatchSize)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_BATCH_SIZE",
                    $"BatchSize {batchSize} is out of range.",
                    $"Use a value between {ConsistencyVerificationWorkflow.MinBatchSize} and {ConsistencyVerificationWorkflow.MaxBatchSize}."));
            }

            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            string instanceId = $"repair-consistency-{tenantId}-{Guid.NewGuid():N}";

            try
            {
                await workflowService.ScheduleRepairAsync(
                    instanceId,
                    new ConsistencyRepairInput(tenantId, batchSize, request?.IncludeUnrepairable ?? false),
                    cancellationToken);
            }
            catch (Dapr.DaprException ex)
            {
                return ErrorResults.DaprUnavailableResult(
                    $"DAPR sidecar unavailable: {ex.Message}",
                    "Check DAPR sidecar connectivity and retry.");
            }

            return Results.Accepted(
                MemoriesRoutes.ConsistencyRepairStatusLocation(tenantId, instanceId),
                new { workflowInstanceId = instanceId });
        });

        app.MapGet(MemoriesRoutes.ConsistencyRepairStatus, async (
            IConsistencyWorkflowService workflowService,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string instanceId,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
            if (tenantExistsError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantExistsError);
            }

            if (!instanceId.StartsWith($"repair-consistency-{tenantId}-", StringComparison.Ordinal))
            {
                return Results.NotFound(new ErrorResponse(
                    "CONSISTENCY_REPAIR_NOT_FOUND",
                    $"Repair workflow '{instanceId}' was not found for tenant '{tenantId}'.",
                    $"Use the workflowInstanceId returned by POST {MemoriesRoutes.ConsistencyRepair} for the same tenant."));
            }

            ConsistencyRepairStatus? status = await workflowService.GetRepairStatusAsync(instanceId, cancellationToken);
            return status is null ? Results.NotFound() : Results.Ok(status);
        });

        return app;
    }
}
