// <copyright file="MemoryUnitLookupEndpoint.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using System.Collections.Generic;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

using static Hexalith.Memories.Server.Endpoints.EndpointTelemetryHelpers;

/// <summary>
/// Story 18.5 — testable minimal-API handler for the exact source-URI-keyed memory-unit lookup
/// (<c>GET /api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri</c>). Extracted from
/// <c>Program.cs</c> (mirroring <see cref="Hexalith.Memories.Server.Tenants.TenantEndpointHandlers"/>) so the
/// success / structured-not-found / invalid-tenant / backend-error branches can be unit-tested without a host.
/// Resolves through <see cref="SourceUriMemoryUnitLookup"/> by exact key — it never delegates to the search
/// engine (AC1) and never introduces a parallel index (AC2).
/// </summary>
internal static class MemoryUnitLookupEndpoint
{
    // Audit event-id pair for the case-access operation channel. In AccessTelemetryLog the payload EventId is
    // operation-keyed (search 7501/7511 … case-access 7504/7514 … delete 7505/7515), and the scope's switch
    // emits this case-scoped read through LogCaseAccess/LogCaseAccessError (7504/7514). So the canonical pair
    // here is 7504/7514 — the same channel GetMemoryUnit uses. (The story's "distinct from 7504/7514" note
    // predated this: 7505/7515 are already the Delete pair pinned by EndpointTelemetryScopeTests, so using them
    // would mislabel a read as a delete in the FR67 audit trail.)
    private const int SuccessEventId = 7504;
    private const int ErrorEventId = 7514;

    /// <summary>Handles <c>GET /api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri</c>.</summary>
    /// <param name="lookup">The exact source-URI lookup seam over the permanent dedup record.</param>
    /// <param name="auditLogger">The access-telemetry audit logger.</param>
    /// <param name="httpContext">The current HTTP context (user resolution).</param>
    /// <param name="tenantId">The tenant identifier (route).</param>
    /// <param name="caseId">The case identifier (route).</param>
    /// <param name="sourceUri">The exact source URI to resolve (query).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// <c>200</c> + <see cref="MemoryUnitIdLookupResponse"/> on a hit; <c>404</c> +
    /// <see cref="ErrorResponse"/> (<c>MEMORY_UNIT_NOT_FOUND</c>) on a genuine miss or transient reservation;
    /// <c>400</c> on an invalid tenant or blank source URI; <c>503</c>
    /// (<c>LOOKUP_BACKEND_UNAVAILABLE</c>) when the Redis read fails (AC6 — never a false <c>404</c>).
    /// </returns>
    public static async Task<IResult> HandleAsync(
        SourceUriMemoryUnitLookup lookup,
        ILogger<AccessTelemetryCategory> auditLogger,
        HttpContext httpContext,
        string tenantId,
        string caseId,
        [FromQuery] string? sourceUri,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(httpContext);

        using EndpointTelemetryScope scope = CreateEndpointAuditScope(
            auditLogger,
            httpContext,
            MemoriesActivitySource.CaseAccess,
            AccessTelemetryLog.OperationCaseAccess,
            successEventId: SuccessEventId,
            errorEventId: ErrorEventId,
            tenantId,
            caseId,
            new Dictionary<string, object?>(System.StringComparer.Ordinal)
        {
            ["sourceUri"] = sourceUri,
        });

        try
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                scope.MarkTenantRejected("INVALID_TENANT_ID");
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            if (string.IsNullOrWhiteSpace(sourceUri))
            {
                scope.MarkValidationError("INVALID_SOURCE_URI");
                return Results.BadRequest(new ErrorResponse("INVALID_SOURCE_URI", "The 'sourceUri' query parameter is required.", "Pass ?sourceUri=<url-encoded source uri>."));
            }

            string? memoryUnitId;
            try
            {
                memoryUnitId = await lookup.ResolveMemoryUnitIdAsync(tenantId, caseId, sourceUri, cancellationToken).ConfigureAwait(false);
            }
            catch (RedisException)
            {
                // AC6: a backend read failure must NOT degrade to a false not-found — a consumer acting on a
                // bogus 404 may re-ingest into a duplicate. Surface a structured backend error instead.
                scope.MarkValidationError("LOOKUP_BACKEND_UNAVAILABLE");
                return ErrorResults.LookupBackendUnavailableResult();
            }

            if (memoryUnitId is null)
            {
                scope.MarkValidationError("MEMORY_UNIT_NOT_FOUND");
                return Results.NotFound(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", $"No memory unit is mapped to the supplied source URI in case '{caseId}'.", "Verify the tenant, case, and source URI; the unit may not be committed yet."));
            }

            scope.ResultCount = 1;
            return Results.Ok(new MemoryUnitIdLookupResponse { MemoryUnitId = memoryUnitId });
        }
        catch (Exception ex)
        {
            scope.MarkUnhandledException(ex);
            throw;
        }
    }
}
