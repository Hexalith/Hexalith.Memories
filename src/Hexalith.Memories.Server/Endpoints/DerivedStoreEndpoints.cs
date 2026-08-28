// <copyright file="DerivedStoreEndpoints.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;
using Hexalith.Memories.Server.DerivedStores;
using Hexalith.Memories.Server.Tenants;

/// <summary>Maps tenant-scoped diagnostic, finalized-binding, and durable correction endpoints.</summary>
public static class DerivedStoreEndpoints
{
    /// <summary>Maps the Memories-owned derived-store service surface.</summary>
    public static IEndpointRouteBuilder MapDerivedStoreEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut(MemoriesRoutes.DerivedStoreDiagnostic, async (
            RedisDerivedStoreService store,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string storeClass,
            string resourceId,
            DiagnosticStoreEntry entry,
            CancellationToken cancellationToken) =>
        {
            IResult? tenantError = await ValidateTenantAsync(tenantGuard, tenantId, cancellationToken).ConfigureAwait(false);
            if (tenantError is not null)
            {
                return tenantError;
            }

            if (!TryParseClass(storeClass, out DiagnosticStoreClass parsedClass))
            {
                return InvalidClass();
            }

            try
            {
                await store.PutDiagnosticEntryAsync(tenantId, parsedClass, resourceId, entry, cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            }
            catch (Exception exception)
            {
                return ToError(exception);
            }
        });

        app.MapGet(MemoriesRoutes.DerivedStoreDiagnostic, async (
            RedisDerivedStoreService store,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string storeClass,
            string resourceId,
            CancellationToken cancellationToken) =>
        {
            IResult? tenantError = await ValidateTenantAsync(tenantGuard, tenantId, cancellationToken).ConfigureAwait(false);
            if (tenantError is not null)
            {
                return tenantError;
            }

            if (!TryParseClass(storeClass, out DiagnosticStoreClass parsedClass))
            {
                return InvalidClass();
            }

            try
            {
                DiagnosticStoreEntry? entry = await store.GetDiagnosticEntryAsync(tenantId, parsedClass, resourceId, cancellationToken).ConfigureAwait(false);
                return entry is null ? Results.NotFound() : Results.Ok(entry);
            }
            catch (Exception exception)
            {
                return ToError(exception);
            }
        });

        app.MapGet(MemoriesRoutes.DerivedStoreDiagnostics, async (
            RedisDerivedStoreService store,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string storeClass,
            CancellationToken cancellationToken) =>
        {
            IResult? tenantError = await ValidateTenantAsync(tenantGuard, tenantId, cancellationToken).ConfigureAwait(false);
            if (tenantError is not null)
            {
                return tenantError;
            }

            if (!TryParseClass(storeClass, out DiagnosticStoreClass parsedClass))
            {
                return InvalidClass();
            }

            try
            {
                return Results.Ok(await store.ListDiagnosticEntriesAsync(tenantId, parsedClass, cancellationToken).ConfigureAwait(false));
            }
            catch (Exception exception)
            {
                return ToError(exception);
            }
        });

        app.MapDelete(MemoriesRoutes.DerivedStoreDiagnostic, async (
            RedisDerivedStoreService store,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string storeClass,
            string resourceId,
            CancellationToken cancellationToken) =>
        {
            IResult? tenantError = await ValidateTenantAsync(tenantGuard, tenantId, cancellationToken).ConfigureAwait(false);
            if (tenantError is not null)
            {
                return tenantError;
            }

            if (!TryParseClass(storeClass, out DiagnosticStoreClass parsedClass))
            {
                return InvalidClass();
            }

            try
            {
                bool deleted = await store.DeleteDiagnosticEntryAsync(tenantId, parsedClass, resourceId, cancellationToken).ConfigureAwait(false);
                return Results.Ok(new DiagnosticStoreDeleteResult(deleted));
            }
            catch (Exception exception)
            {
                return ToError(exception);
            }
        });

        app.MapPost(MemoriesRoutes.DerivedStoreBindings, async (
            RedisDerivedStoreService store,
            TenantStatusGuard tenantGuard,
            string tenantId,
            FinalizeDerivedStoreBindingRequest request,
            CancellationToken cancellationToken) =>
        {
            IResult? tenantError = await ValidateTenantAsync(tenantGuard, tenantId, cancellationToken).ConfigureAwait(false);
            if (tenantError is not null)
            {
                return tenantError;
            }

            try
            {
                DerivedStoreBinding binding = await store.FinalizeBindingAsync(tenantId, request, cancellationToken).ConfigureAwait(false);
                return Results.Ok(binding);
            }
            catch (Exception exception)
            {
                return ToError(exception);
            }
        });

        app.MapPost(MemoriesRoutes.DerivedStoreCorrections, async (
            RedisDerivedStoreService store,
            DaprWorkflowClient workflowClient,
            TenantStatusGuard tenantGuard,
            string tenantId,
            StartDerivedStoreCorrectionRequest request,
            CancellationToken cancellationToken) =>
        {
            IResult? tenantError = await ValidateTenantAsync(tenantGuard, tenantId, cancellationToken).ConfigureAwait(false);
            if (tenantError is not null)
            {
                return tenantError;
            }

            try
            {
                DerivedStoreCorrectionStartResult start = await store.StartCorrectionAsync(tenantId, request, cancellationToken).ConfigureAwait(false);
                if (start.ShouldSchedule)
                {
                    _ = await workflowClient.ScheduleNewWorkflowAsync(
                        nameof(DerivedStoreCorrectionWorkflow),
                        start.WorkflowInstanceId,
                        new DerivedStoreCorrectionWorkflowInput(tenantId, start.Status.OperationId));
                }

                return start.Status.State == DerivedStoreCorrectionState.NoOp
                    ? Results.Ok(start.Status)
                    : Results.Accepted(MemoriesRoutes.DerivedStoreCorrectionPath(tenantId, start.Status.OperationId), start.Status);
            }
            catch (Exception exception)
            {
                return ToError(exception);
            }
        });

        app.MapGet(MemoriesRoutes.DerivedStoreCorrection, async (
            RedisDerivedStoreService store,
            TenantStatusGuard tenantGuard,
            string tenantId,
            string operationId,
            CancellationToken cancellationToken) =>
        {
            IResult? tenantError = await ValidateTenantAsync(tenantGuard, tenantId, cancellationToken).ConfigureAwait(false);
            if (tenantError is not null)
            {
                return tenantError;
            }

            try
            {
                DerivedStoreCorrectionStatus? status = await store.GetCorrectionStatusAsync(tenantId, operationId, cancellationToken).ConfigureAwait(false);
                return status is null ? Results.NotFound() : Results.Ok(status);
            }
            catch (Exception exception)
            {
                return ToError(exception);
            }
        });

        return app;
    }

    private static async Task<IResult?> ValidateTenantAsync(
        TenantStatusGuard tenantGuard,
        string tenantId,
        CancellationToken cancellationToken)
    {
        ErrorResponse? tenantError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return tenantError is null ? null : TenantStatusGuard.ToHttpResult(tenantError);
    }

    private static bool TryParseClass(string value, out DiagnosticStoreClass storeClass)
        => Enum.TryParse(value, ignoreCase: true, out storeClass) && Enum.IsDefined(storeClass);

    private static IResult InvalidClass()
        => Results.BadRequest(new ErrorResponse(
            "DIAGNOSTIC_CLASS_INVALID",
            "The diagnostic class is unknown.",
            "Use vectorIndex, embeddingStore, promptContextCache, or candidateRankingCache."));

    private static IResult ToError(Exception exception)
    {
        if (exception is ArgumentException)
        {
            return Results.BadRequest(new ErrorResponse(
                "DERIVED_STORE_REQUEST_INVALID",
                "The derived-store request is invalid.",
                "Correct the metadata-only identifiers and manifest, then retry."));
        }

        if (exception is DerivedStoreStateException stateException)
        {
            return Results.Json(
                new ErrorResponse(
                    stateException.Code,
                    stateException.Message,
                    "Correct the governed binding or retry after the referenced Memories state is available."),
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Json(
            new ErrorResponse(
                "DERIVED_STORE_BACKEND_UNAVAILABLE",
                "The derived-store backend is unavailable.",
                "Verify Redis Vector, FalkorDB, and Dapr readiness, then retry."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
