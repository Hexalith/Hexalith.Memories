// <copyright file="TenantDiagnosticsEndpoints.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Handlers;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tenants;

using StackExchange.Redis;

using static Hexalith.Memories.Server.Endpoints.EndpointValidationHelpers;

/// <summary>Maps tenant verification, telemetry, and handler diagnostics.</summary>
internal static class TenantDiagnosticsEndpoints
{
    /// <summary>Registers read-only diagnostics and tenant isolation verification.</summary>
    internal static void MapTenantDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        // Story 5.3: Tenant isolation verification
        app.MapPost(MemoriesRoutes.TenantVerify, async (
            TenantIsolationVerifier verifier,
            string tenantId,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIsolationVerificationResult result = await verifier.VerifyAsync(tenantId, cancellationToken);
                return Results.Ok(result);
            }
            catch (Dapr.DaprException ex)
            {
                return ErrorResults.DaprUnavailableResult(
                    $"DAPR sidecar unavailable: {ex.Message}",
                    "Check DAPR sidecar connectivity and retry.");
            }
            catch (RedisException ex)
            {
                return ErrorResults.BackendUnavailableResult($"Backend unavailable: {ex.Message}");
            }
        }).AddEndpointFilter(TenantIdValidationEndpointFilter.For())
            // Verification is diagnostic: non-active registered tenants still reach the verifier.
            .AddEndpointFilter(TenantStatusEndpointFilter.ExistsOnly());

        // Story 7.5 — telemetry summary endpoint (AC #6). Operator-facing read-only poke; DOES NOT emit
        // an AccessTelemetryEvent for itself (Task 5.5 — self-referential audit noise).
        app.MapGet(MemoriesRoutes.TenantTelemetrySummary, async (
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
        app.MapGet(MemoriesRoutes.Handlers, async (
            HttpContext http,
            HandlerRegistryService registryService,
            CancellationToken cancellationToken) =>
        {
            http.Response.Headers.Append("X-Memories-API-Experimental", "HXL002");
            HandlerRegistrationSnapshot snapshot = await registryService.GetSnapshotAsync(cancellationToken);
            return Results.Ok(snapshot);
        });

        app.MapGet(MemoriesRoutes.TenantHandlerMismatches, async (
            HttpContext http,
            string tenantId,
            HandlerMismatchDetector detector,
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
                tenantId, HandlerRegistryService.ObservationWindow, cancellationToken);
            return Results.Ok(report);
        });
    }
}
