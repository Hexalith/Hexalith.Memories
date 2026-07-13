// <copyright file="ImportEndpoints.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Import;
using Hexalith.Memories.Server.RateLimiting;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Workflows;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using static Hexalith.Memories.Server.Endpoints.EndpointValidationHelpers;

/// <summary>
/// Story 26.2 — import/restore endpoints. They consume the exact JSON envelope produced by the export
/// endpoints, validate the manifest (schema version + scope + same-tenant targeting) synchronously, stage the
/// payload out-of-band, and schedule the durable <see cref="RestoreWorkflow"/> (202 Accepted + status Location).
/// </summary>
internal static class ImportEndpoints
{
    /// <summary>
    /// The maximum accepted import body. Deliberate, documented ceiling (decision D5): a tenant export of 100K
    /// units is ≈500 MB. The current staging path buffers the body once (bounded here); for corpora beyond this
    /// ceiling, restore case-by-case — a streaming/chunked staging store is the documented follow-up.
    /// </summary>
    private const long MaxImportBodyBytes = 512L * 1024 * 1024;

    /// <summary>Maps the import/restore endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(MemoriesRoutes.TenantImport, async (
            DaprWorkflowClient workflowClient,
            TenantStatusGuard tenantGuard,
            IImportStagingStore stagingStore,
            HttpContext context,
            string tenantId) =>
            await HandleImportAsync(context, workflowClient, tenantGuard, stagingStore, tenantId, caseId: null, ExportScope.Tenant, context.RequestAborted))
            .WithMetadata(new RequestSizeLimitAttribute(MaxImportBodyBytes))
            .AddEndpointFilter<TenantAuthorizationEndpointFilter>()
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        app.MapPost(MemoriesRoutes.CaseImport, async (
            DaprWorkflowClient workflowClient,
            TenantStatusGuard tenantGuard,
            IImportStagingStore stagingStore,
            HttpContext context,
            string tenantId,
            string caseId) =>
            await HandleImportAsync(context, workflowClient, tenantGuard, stagingStore, tenantId, caseId, ExportScope.Case, context.RequestAborted))
            .WithMetadata(new RequestSizeLimitAttribute(MaxImportBodyBytes))
            .AddEndpointFilter<TenantAuthorizationEndpointFilter>()
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        app.MapGet(MemoriesRoutes.RestoreStatus, async (
            DaprWorkflowClient workflowClient,
            string tenantId,
            string instanceId,
            CancellationToken cancellationToken) =>
            await ReadRestoreStatusAsync(workflowClient, tenantId, instanceId, cancellationToken));

        return app;
    }

    private static async Task<IResult> HandleImportAsync(
        HttpContext context,
        DaprWorkflowClient workflowClient,
        TenantStatusGuard tenantGuard,
        IImportStagingStore stagingStore,
        string tenantId,
        string? caseId,
        ExportScope expectedScope,
        CancellationToken cancellationToken)
    {
        ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
        if (tenantValidationError is not null)
        {
            return Results.BadRequest(tenantValidationError);
        }

        if (expectedScope == ExportScope.Case)
        {
            ErrorResponse? caseValidationError = CaseValidator.ValidateCaseId(caseId!);
            if (caseValidationError is not null)
            {
                return Results.BadRequest(caseValidationError);
            }
        }

        ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
        if (tenantStatusError is not null)
        {
            return TenantStatusGuard.ToHttpResult(tenantStatusError);
        }

        byte[] payload;
        try
        {
            using MemoryStream buffer = new();
            await context.Request.Body.CopyToAsync(buffer, cancellationToken);
            payload = buffer.ToArray();
        }
        catch (BadHttpRequestException ex)
        {
            return Results.Json(
                new ErrorResponse(
                    "IMPORT_TOO_LARGE",
                    $"Import payload exceeds the configured size limit: {ex.Message}",
                    "Restore case-by-case, or raise the documented import size ceiling."),
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (payload.Length == 0)
        {
            return Results.BadRequest(new ErrorResponse(
                "IMPORT_EMPTY",
                "Import payload is empty.",
                "POST the JSON export envelope produced by the export endpoint."));
        }

        if (!ImportEnvelopeReader.TryReadManifest(payload, out ExportManifest? manifest, out string? parseError) || manifest is null)
        {
            return Results.BadRequest(new ErrorResponse(
                "IMPORT_MANIFEST_UNREADABLE",
                parseError ?? "The import manifest could not be read.",
                "Ensure the body is the JSON export envelope whose first property is the manifest."));
        }

        ErrorResponse? manifestError = ImportRequestValidator.Validate(manifest, expectedScope, tenantId, caseId);
        if (manifestError is not null)
        {
            return Results.BadRequest(manifestError);
        }

        string instanceId = Guid.NewGuid().ToString();
        string requestedBy = context.User.Identity?.Name ?? "system";

        string stagingKey;
        try
        {
            stagingKey = await stagingStore.StageAsync(tenantId, instanceId, payload, cancellationToken);
        }
        catch (StackExchange.Redis.RedisConnectionException ex)
        {
            return Results.Json(
                ErrorResults.BackendUnavailable($"Import staging backend is unavailable: {ex.Message}"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            _ = await workflowClient.ScheduleNewWorkflowAsync(
                nameof(RestoreWorkflow),
                instanceId,
                new RestoreWorkflowInput(tenantId, caseId, stagingKey, requestedBy));
        }
        catch (Dapr.DaprException ex)
        {
            await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            return ErrorResults.DaprUnavailableResult(
                $"Restore workflow could not be scheduled: {ex.Message}",
                "Retry the import after Dapr connectivity is restored.");
        }

        string location = MemoriesRoutes.RestoreStatusLocation(tenantId, instanceId);
        return Results.Accepted(location, new RestoreAcceptedResponse(instanceId, tenantId, caseId, manifest.Scope, location));
    }

    private static async Task<IResult> ReadRestoreStatusAsync(
        DaprWorkflowClient workflowClient,
        string tenantId,
        string instanceId,
        CancellationToken cancellationToken)
    {
        ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
        if (tenantValidationError is not null)
        {
            return Results.BadRequest(tenantValidationError);
        }

        WorkflowState? state;
        try
        {
            state = await workflowClient.GetWorkflowStateAsync(instanceId, getInputsAndOutputs: true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            state = null;
        }

        if (state is null || !state.Exists)
        {
            return Results.NotFound(new ErrorResponse(
                "RESTORE_STATUS_NOT_FOUND",
                "Restore workflow status was not found or has expired.",
                "Use the instanceId returned by the import scheduling endpoint."));
        }

        // Tenant isolation: never surface another tenant's restore under this tenant's route.
        RestoreWorkflowInput? restoreInput = TryReadInput(state);
        if (restoreInput is not null && !string.Equals(restoreInput.TenantId, tenantId, StringComparison.Ordinal))
        {
            return Results.NotFound(new ErrorResponse(
                "RESTORE_STATUS_NOT_FOUND",
                "Restore workflow status was not found for this tenant.",
                "Use the instanceId returned by the import scheduling endpoint for this tenant."));
        }

        string status = TryReadCustomStatus(state) ?? state.RuntimeStatus.ToString();
        RestoreWorkflowResult? result = state.RuntimeStatus == WorkflowRuntimeStatus.Completed
            ? TryReadOutput(state)
            : null;

        return Results.Ok(new RestoreStatusResponse(
            instanceId,
            tenantId,
            status,
            state.CreatedAt,
            state.LastUpdatedAt,
            result?.RestoredMemoryUnits,
            result?.RestoredCases,
            result?.RestoredEdges));
    }

    private static RestoreWorkflowInput? TryReadInput(WorkflowState state)
    {
        try
        {
            return state.ReadInputAs<RestoreWorkflowInput>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static RestoreWorkflowResult? TryReadOutput(WorkflowState state)
    {
        try
        {
            return state.ReadOutputAs<RestoreWorkflowResult>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? TryReadCustomStatus(WorkflowState state)
    {
        try
        {
            return state.ReadCustomStatusAs<string>();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
