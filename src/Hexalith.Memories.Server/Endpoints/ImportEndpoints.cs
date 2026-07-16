// <copyright file="ImportEndpoints.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using System.Diagnostics;

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
    /// units is ≈500 MB. The request body is streamed into bounded staging chunks; the ceiling protects both
    /// the service and Redis from an unbounded upload.
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
            IRestoreTargetGuard targetGuard,
            HttpContext context,
            string tenantId) =>
            await HandleImportAsync(context, workflowClient, tenantGuard, stagingStore, targetGuard, tenantId, caseId: null, ExportScope.Tenant, context.RequestAborted))
            .WithMetadata(new RequestSizeLimitAttribute(MaxImportBodyBytes))
            .AddEndpointFilter<TenantAuthorizationEndpointFilter>()
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        app.MapPost(MemoriesRoutes.CaseImport, async (
            DaprWorkflowClient workflowClient,
            TenantStatusGuard tenantGuard,
            IImportStagingStore stagingStore,
            IRestoreTargetGuard targetGuard,
            HttpContext context,
            string tenantId,
            string caseId) =>
            await HandleImportAsync(context, workflowClient, tenantGuard, stagingStore, targetGuard, tenantId, caseId, ExportScope.Case, context.RequestAborted))
            .WithMetadata(new RequestSizeLimitAttribute(MaxImportBodyBytes))
            .AddEndpointFilter<TenantAuthorizationEndpointFilter>()
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        app.MapGet(MemoriesRoutes.RestoreStatus, async (
            DaprWorkflowClient workflowClient,
            string tenantId,
            string instanceId,
            CancellationToken cancellationToken) =>
            await ReadRestoreStatusAsync(workflowClient, tenantId, instanceId, cancellationToken))
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        return app;
    }

    private static async Task<IResult> HandleImportAsync(
        HttpContext context,
        DaprWorkflowClient workflowClient,
        TenantStatusGuard tenantGuard,
        IImportStagingStore stagingStore,
        IRestoreTargetGuard targetGuard,
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

        string instanceId = Guid.NewGuid().ToString();
        string stagingKey;
        try
        {
            stagingKey = await stagingStore.StageAsync(
                tenantId,
                instanceId,
                context.Request.Body,
                MaxImportBodyBytes,
                cancellationToken);
        }
        catch (Exception ex) when (ex is BadHttpRequestException or InvalidDataException)
        {
            return Results.Json(
                new ErrorResponse(
                    "IMPORT_TOO_LARGE",
                    $"Import payload exceeds the configured size limit: {ex.Message}",
                    "Restore case-by-case, or raise the documented import size ceiling."),
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }
        catch (IOException ex)
        {
            // Story 26.2 review (P5): a client disconnect / connection reset mid-upload is a client-side abort,
            // not a server fault — surface a 400 rather than letting it bubble up as an unhandled 500.
            return Results.Json(
                new ErrorResponse(
                    "IMPORT_ABORTED",
                    $"The import upload was interrupted before completion: {ex.Message}",
                    "Retry the import and keep the connection open for the full request body."),
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (StackExchange.Redis.RedisConnectionException ex)
        {
            return Results.Json(
                ErrorResults.BackendUnavailable($"Import staging backend is unavailable: {ex.Message}"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        ExportManifest manifest;
        try
        {
            await using Stream? staged = await stagingStore.OpenReadAsync(stagingKey, cancellationToken);
            if (staged is null || staged.Length == 0)
            {
                await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
                return Results.BadRequest(new ErrorResponse(
                    "IMPORT_EMPTY",
                    "Import payload is empty.",
                    "POST the JSON export envelope produced by the export endpoint."));
            }

            ImportEnvelopeScanResult scan = await ImportEnvelopeStreamProcessor.ProcessAsync(
                staged,
                (importedCase, _) =>
                {
                    ImportEnvelopeValidator.EnsureCaseTarget(importedCase, tenantId, caseId);
                    return Task.CompletedTask;
                },
                (unit, _) =>
                {
                    ImportEnvelopeValidator.EnsureMemoryUnitTarget(unit, tenantId, caseId);
                    return Task.CompletedTask;
                },
                edgeHandler: null,
                cancellationToken).ConfigureAwait(false);
            ImportEnvelopeValidator.EnsureManifestTarget(scan.Manifest, tenantId, caseId);
            manifest = scan.Manifest;
        }
        catch (ImportEnvelopeException ex)
        {
            await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            return Results.BadRequest(new ErrorResponse(
                "IMPORT_MANIFEST_UNREADABLE",
                ex.Message,
                "Ensure the body is the canonical JSON export envelope produced by this service."));
        }
        catch (Exception ex) when (ex is StackExchange.Redis.RedisException or EndOfStreamException or InvalidDataException)
        {
            await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            return Results.Json(
                ErrorResults.BackendUnavailable($"Staged import could not be verified: {ex.Message}"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        string requestedBy = EndpointTelemetryHelpers.ResolvePrincipalAuditUser(context, Activity.Current);

        RestoreLeaseResult lease;
        try
        {
            lease = await stagingStore.AcquireRestoreLeaseAsync(
                stagingKey,
                tenantId,
                caseId,
                instanceId,
                cancellationToken);
        }
        catch (Exception ex) when (ex is StackExchange.Redis.RedisException or InvalidDataException)
        {
            await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            return Results.Json(
                ErrorResults.BackendUnavailable($"Restore lease could not be acquired: {ex.Message}"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!lease.Acquired)
        {
            await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            string existingLocation = MemoriesRoutes.RestoreStatusLocation(tenantId, lease.InstanceId);
            if (lease.SameOperation)
            {
                return Results.Accepted(
                    existingLocation,
                    new RestoreAcceptedResponse(lease.InstanceId, tenantId, caseId, manifest.Scope, existingLocation));
            }

            return Results.Conflict(new ErrorResponse(
                "RESTORE_TARGET_BUSY",
                $"A restore already owns the target scope (workflow '{lease.InstanceId}').",
                "Wait for that restore to finish before importing different content into the same target."));
        }

        try
        {
            await targetGuard.EnsureCleanAsync(tenantId, caseId, cancellationToken).ConfigureAwait(false);
        }
        catch (ImportEnvelopeException ex) when (string.Equals(ex.Code, "RESTORE_TARGET_NOT_CLEAN", StringComparison.Ordinal))
        {
            await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            return Results.Conflict(new ErrorResponse(
                ex.Code,
                ex.Message,
                "Restore into a newly provisioned empty tenant or case target."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            return Results.Json(
                ErrorResults.BackendUnavailable($"Restore target cleanliness could not be verified: {ex.Message}"),
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
            try
            {
                WorkflowState? state = await workflowClient.GetWorkflowStateAsync(
                    instanceId,
                    getInputsAndOutputs: false,
                    cancellationToken);
                if (state is not null && state.Exists)
                {
                    string ambiguousLocation = MemoriesRoutes.RestoreStatusLocation(tenantId, instanceId);
                    return Results.Accepted(
                        ambiguousLocation,
                        new RestoreAcceptedResponse(instanceId, tenantId, caseId, manifest.Scope, ambiguousLocation));
                }

                await stagingStore.DeleteAsync(stagingKey, CancellationToken.None);
            }
            catch (Exception stateException) when (stateException is not OperationCanceledException)
            {
                // Scheduling may have succeeded before the response was lost. Preserve staging + lease until
                // their TTLs expire rather than deleting input a live workflow may still need.
                return Results.Json(
                    ErrorResults.BackendUnavailable(
                        $"Restore scheduling outcome is unknown ({ex.Message}); status confirmation also failed ({stateException.Message}). Staging was retained."),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

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

        WorkflowState state;
        try
        {
            WorkflowState? fetched = await workflowClient.GetWorkflowStateAsync(instanceId, getInputsAndOutputs: true, cancellationToken);
            if (fetched is null || !fetched.Exists)
            {
                return Results.NotFound(new ErrorResponse(
                    "RESTORE_STATUS_NOT_FOUND",
                    "Restore workflow status was not found or has expired.",
                    "Use the instanceId returned by the import scheduling endpoint."));
            }

            state = fetched;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Story 26.2 review (P4): a backend/state-store failure is NOT "not found". Surface 503 so an
            // operator does not read a transient outage as "restore lost" and re-POST (which would spawn a
            // duplicate restore).
            return Results.Json(
                ErrorResults.BackendUnavailable($"Restore status backend is unavailable: {ex.Message}"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        // Tenant isolation: never surface another tenant's restore under this tenant's route.
        RestoreWorkflowInput? restoreInput = TryReadInput(state);
        if (restoreInput is null)
        {
            return Results.NotFound(new ErrorResponse(
                "RESTORE_STATUS_NOT_FOUND",
                "Restore workflow status could not be verified for this tenant.",
                "Use the instanceId returned by this tenant's import endpoint."));
        }

        if (!string.Equals(restoreInput.TenantId, tenantId, StringComparison.Ordinal))
        {
            return Results.NotFound(new ErrorResponse(
                "RESTORE_STATUS_NOT_FOUND",
                "Restore workflow status was not found for this tenant.",
                "Use the instanceId returned by the import scheduling endpoint for this tenant."));
        }

        string status = ResolveReportedStatus(state.RuntimeStatus, TryReadCustomStatus(state));
        RestoreWorkflowResult? result = state.RuntimeStatus == WorkflowRuntimeStatus.Completed
            ? TryReadOutput(state)
            : null;
        if (state.RuntimeStatus == WorkflowRuntimeStatus.Completed && result is null)
        {
            return Results.Json(
                ErrorResults.BackendUnavailable("Restore completed, but its result could not be read safely."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        (string? failureCode, string? failureMessage, string? failureSuggestion) =
            ResolveFailureDiagnostics(state.RuntimeStatus, state.FailureDetails);

        return Results.Ok(new RestoreStatusResponse(
            instanceId,
            tenantId,
            status,
            state.CreatedAt,
            state.LastUpdatedAt,
            result?.RestoredMemoryUnits,
            result?.RestoredCases,
            result?.RestoredEdges,
            result?.SkippedRecords)
        {
            FailureCode = failureCode,
            FailureMessage = failureMessage,
            FailureSuggestion = failureSuggestion,
        });
    }

    /// <summary>Prevents stale custom progress text from masking a terminal workflow state.</summary>
    internal static string ResolveReportedStatus(WorkflowRuntimeStatus runtimeStatus, string? customStatus)
        => runtimeStatus is WorkflowRuntimeStatus.Completed
            or WorkflowRuntimeStatus.Failed
            or WorkflowRuntimeStatus.Canceled
            or WorkflowRuntimeStatus.Terminated
            ? runtimeStatus.ToString()
            : customStatus ?? runtimeStatus.ToString();

    /// <summary>Projects raw workflow failure state into stable, support-safe operator diagnostics.</summary>
    internal static (string? Code, string? Message, string? Suggestion) ResolveFailureDiagnostics(
        WorkflowRuntimeStatus runtimeStatus,
        WorkflowTaskFailureDetails? failureDetails)
    {
        if (runtimeStatus == WorkflowRuntimeStatus.Canceled)
        {
            return (
                "RESTORE_WORKFLOW_CANCELED",
                "The restore workflow was canceled before completion.",
                "Verify the cancellation was intentional; otherwise retry the backup into a clean target.");
        }

        if (runtimeStatus == WorkflowRuntimeStatus.Terminated)
        {
            return (
                "RESTORE_WORKFLOW_TERMINATED",
                "The restore workflow was terminated before completion.",
                "Review the operator action that terminated the workflow, then retry the backup into a clean target.");
        }

        if (runtimeStatus != WorkflowRuntimeStatus.Failed)
        {
            return (null, null, null);
        }

        string rawMessage = failureDetails?.ErrorMessage ?? string.Empty;
        if (rawMessage.Contains("RESTORE_LEASE_LOST", StringComparison.Ordinal))
        {
            return (
                "RESTORE_LEASE_LOST",
                "The restore lost access to its staged backup lease before completion.",
                "Verify Redis staging availability and retention, then retry the backup into a clean target.");
        }

        if (rawMessage.Contains("IMPORT_EMBEDDING_PROVIDER_MISMATCH", StringComparison.Ordinal)
            || rawMessage.Contains("IMPORT_EMBEDDING_MODEL_MISMATCH", StringComparison.Ordinal)
            || rawMessage.Contains("IMPORT_EMBEDDING_DIMENSIONS_MISMATCH", StringComparison.Ordinal))
        {
            return (
                "RESTORE_EMBEDDING_CONFIGURATION_MISMATCH",
                "The restore target embedding configuration does not match the exported data.",
                "Provision a clean target with the export's provider, model, and dimensions, then retry.");
        }

        return (
            "RESTORE_WORKFLOW_FAILED",
            "The restore workflow failed before completion.",
            "Inspect server logs for this restore instance id, correct the backend or configuration issue, then retry into a clean target.");
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
