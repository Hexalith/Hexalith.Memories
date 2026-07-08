// <copyright file="ExportEndpoints.cs" company="ITANEO">
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
internal static class ExportEndpoints
{
    /// <summary>Maps this resource area's endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Story 8.3: streaming data export endpoints. Snapshot + tenant/case existence are validated
        // BEFORE response headers are committed so 400/404 errors return a clean JSON body. Once
        // StartAsync is called, the response is streaming and mid-stream errors only manifest as a
        // truncated body (the client's JSON parse surfaces the failure).
        app.MapGet(MemoriesRoutes.CaseExport, async (
            HttpContext context,
            Hexalith.Memories.Server.Export.TenantExportService exportService,
            string tenantId,
            string caseId) =>
        {
            CancellationToken ct = context.RequestAborted;

            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            Hexalith.Memories.Server.Export.ExportSnapshot snapshot;
            try
            {
                snapshot = await exportService.CaptureSnapshotAsync(tenantId, caseId, ct);
            }
            catch (ArgumentException ex) when (ex.ParamName == "caseId")
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_CASE_ID",
                    ex.Message,
                    "Provide a valid 26-character ULID case identifier."));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_TENANT_ID",
                    ex.Message,
                    "Only alphanumeric characters and hyphens are allowed."));
            }
            catch (KeyNotFoundException ex)
            {
                string code = ex.Message.Contains("Case '", StringComparison.Ordinal) ? "CASE_NOT_FOUND" : "TENANT_NOT_FOUND";
                string recovery = code == "CASE_NOT_FOUND"
                    ? "List available cases with GET /api/tenants/{tenantId}/cases."
                    : "List available tenants with GET /api/tenants.";
                return Results.NotFound(new ErrorResponse(code, ex.Message, recovery));
            }
            catch (Dapr.DaprException ex)
            {
                return ErrorResults.DaprUnavailableResult(
                    $"Export dependency is unavailable: {ex.Message}",
                    "Retry the export after Dapr connectivity is restored.");
            }
            catch (StackExchange.Redis.RedisConnectionException ex)
            {
                return Results.Json(
                    ErrorResults.ExportBackendUnavailable($"Export backend is unavailable: {ex.Message}"),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            string filename = $"{tenantId}-{caseId}-{snapshot.SnapshotAt:yyyyMMdd-HHmmss}.json";
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            context.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            context.Response.Headers["X-Export-Schema-Version"] = "1";
            await context.Response.StartAsync(ct);

            await exportService.WriteCaseExportAsync(tenantId, caseId, snapshot, context.Response.BodyWriter, ct);
            return Results.Empty;
        });

        app.MapGet(MemoriesRoutes.TenantExport, async (
            HttpContext context,
            Hexalith.Memories.Server.Export.TenantExportService exportService,
            string tenantId) =>
        {
            CancellationToken ct = context.RequestAborted;

            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            Hexalith.Memories.Server.Export.ExportSnapshot snapshot;
            try
            {
                snapshot = await exportService.CaptureSnapshotAsync(tenantId, caseId: null, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_TENANT_ID",
                    ex.Message,
                    "Only alphanumeric characters and hyphens are allowed."));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new ErrorResponse(
                    "TENANT_NOT_FOUND",
                    ex.Message,
                    "List available tenants with GET /api/tenants."));
            }
            catch (Dapr.DaprException ex)
            {
                return ErrorResults.DaprUnavailableResult(
                    $"Export dependency is unavailable: {ex.Message}",
                    "Retry the export after Dapr connectivity is restored.");
            }
            catch (StackExchange.Redis.RedisConnectionException ex)
            {
                return Results.Json(
                    ErrorResults.ExportBackendUnavailable($"Export backend is unavailable: {ex.Message}"),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            string filename = $"{tenantId}-tenant-{snapshot.SnapshotAt:yyyyMMdd-HHmmss}.json";
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            context.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            context.Response.Headers["X-Export-Schema-Version"] = "1";
            await context.Response.StartAsync(ct);

            await exportService.WriteTenantExportAsync(tenantId, snapshot, context.Response.BodyWriter, ct);
            return Results.Empty;
        });

        return app;
    }
}
