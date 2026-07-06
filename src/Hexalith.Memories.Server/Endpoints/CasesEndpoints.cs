// <copyright file="CasesEndpoints.cs" company="ITANEO">
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
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
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
internal static class CasesEndpoints
{
    /// <summary>Maps this resource area's endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCasesEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/tenants/{tenantId}/cases", async (
            string tenantId,
            CreateCaseInput input,
            CaseService caseService,
            TenantStatusGuard tenantGuard,
            CancellationToken cancellationToken) =>
        {
            var validatedInput = input with { TenantId = tenantId };
            ErrorResponse? error = CaseValidator.ValidateCreateCase(validatedInput);
            if (error is not null)
            {
                return Results.BadRequest(error);
            }

            ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (tenantStatusError is not null)
            {
                return TenantStatusGuard.ToHttpResult(tenantStatusError);
            }

            Case created = await caseService.CreateCaseAsync(validatedInput, cancellationToken);
            return Results.Created($"/api/tenants/{tenantId}/cases/{created.Id}", created);
        });

        app.MapGet("/api/tenants/{tenantId}/cases", async (
            string tenantId,
            int? limit,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            int effectiveLimit = Math.Clamp(limit ?? 100, 1, 500);
            List<Case> cases = await caseService.ListCasesAsync(tenantId, effectiveLimit, cancellationToken);
            return Results.Ok(cases);
        });

        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}", async (
            string tenantId,
            string caseId,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            return caseResult is null
                ? Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."))
                : Results.Ok(caseResult);
        });

        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/status", async (
            string tenantId,
            string caseId,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            CaseStatusDetail? status = await caseService.GetCaseStatusAsync(tenantId, caseId, cancellationToken);
            return status is null
                ? Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."))
                : Results.Ok(status);
        });

        // Story 6.3 FR11: list failed memory units for a case (most-recent first, paged).
        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/failed-units", async (
            string tenantId,
            string caseId,
            int? limit,
            int? offset,
            CaseService caseService,
            FailedUnitsRegistry registry,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            FailedUnitsPage page = await registry.ListAsync(tenantId, caseId, limit ?? 50, offset ?? 0, cancellationToken);
            return Results.Ok(page);
        });

        // Story 6.3 FR11: detail endpoint for a single memory unit. When the indexed-MU hash is missing AND a
        // failed-unit hash exists, synthesize a Failed MemoryUnit projection (content="" since it was never
        // extracted/persisted). Tenant-mismatch guard inside CaseService.
        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}", async (
            string tenantId,
            string caseId,
            string memoryUnitId,
            CaseService caseService,
            FailedUnitsRegistry registry,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity(MemoriesActivitySource.CaseAccess);
            activity?.SetTag(MemoriesActivitySource.TagOperation, AccessTelemetryLog.OperationCaseAccess);
            using var scope = new EndpointTelemetryScope(
                auditLogger,
                activity,
                AccessTelemetryLog.OperationCaseAccess,
                successEventId: 7504,
                errorEventId: 7514,
                tenantIdTag: string.IsNullOrWhiteSpace(tenantId) ? MemoriesMeter.RejectedTenantTag : tenantId);
            scope.User = ResolvePrincipalAuditUser(httpContext, activity);
            scope.CaseId = caseId;
            scope.QueryParams = new Dictionary<string, object?>(System.StringComparer.Ordinal)
            {
                ["memoryUnitId"] = memoryUnitId,
            };
            activity?.SetTag(MemoriesActivitySource.TagTenantId, tenantId);
            activity?.SetTag(MemoriesActivitySource.TagCaseId, caseId);
            activity?.SetTag(MemoriesActivitySource.TagMemoryUnitId, memoryUnitId);

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

                MemoryUnit? indexed = await caseService.GetMemoryUnitAsync(tenantId, memoryUnitId, cancellationToken);
                if (indexed is not null)
                {
                    if (!string.Equals(indexed.CaseId, caseId, StringComparison.Ordinal))
                    {
                        scope.MarkValidationError("MEMORY_UNIT_NOT_FOUND");
                        return Results.NotFound(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", $"Memory unit '{memoryUnitId}' does not exist in case '{caseId}'.", "Verify the case id."));
                    }

                    scope.ResultCount = 1;
                    return Results.Ok(indexed);
                }

                FailedUnitSummary? failed = await registry.GetSummaryAsync(tenantId, memoryUnitId, cancellationToken);
                if (failed is null)
                {
                    scope.MarkValidationError("MEMORY_UNIT_NOT_FOUND");
                    return Results.NotFound(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", $"Memory unit '{memoryUnitId}' was not found.", "Verify the memory unit id."));
                }

                if (!string.Equals(failed.CaseId, caseId, StringComparison.Ordinal))
                {
                    scope.MarkValidationError("CASE_MISMATCH");
                    return Results.BadRequest(new ErrorResponse("CASE_MISMATCH", "Memory unit belongs to a different case.", "Use the case id reported by the failed-units list."));
                }

                MemoryUnit synthesized = new()
                {
                    Id = failed.MemoryUnitId,
                    TenantId = tenantId,
                    CaseId = failed.CaseId,
                    SourceUri = failed.SourceUri,
                    SourceType = failed.SourceType,
                    IngestedBy = string.Empty,
                    IngestedAt = failed.FailedAt,
                    LastUpdated = failed.FailedAt,
                    Content = string.Empty,
                    ContentHash = string.Empty,
                    Status = MemoryUnitStatus.Failed,
                    FailureDetails = new FailureDetails(failed.Stage, failed.ErrorCode, failed.RetryCount, failed.ErrorMessage, failed.LastRetryAt),
                };
                scope.ResultCount = 1;
                return Results.Ok(synthesized);
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        // Story 18.5 — exact source-URI-keyed lookup returning the canonical MemoryUnitId. The literal `by-source-uri`
        // segment is a sibling of the `{memoryUnitId}` template above; ASP.NET Core gives literal segments higher
        // precedence, so this route wins for that path (asserted in MemoryUnitLookupEndpointTests). Reads the permanent
        // dedup record by exact key (no search delegation); structured 404 on miss, 503 on backend failure (AC6).
        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri", MemoryUnitLookupEndpoint.HandleAsync);

        // Story 6.3 FR12: re-ingest a single failed memory unit. Atomic claim via Lua deletes the failed-unit
        // hash, sorted-set entry, AND the dedup key in one round-trip; if the claim fails (already gone),
        // returns 409. The new workflow re-uses the original memory-unit-id via the DAPR workflow `instanceId`
        // parameter — annotations and graph edges survive.
        app.MapPost("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest", async (
            string tenantId,
            string caseId,
            string memoryUnitId,
            ReIngestionCoordinator coordinator,
            TenantStatusGuard tenantGuard,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            ErrorResponse? statusErr = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (statusErr is not null)
            {
                return TenantStatusGuard.ToHttpResult(statusErr);
            }

            ReIngestionAttemptResult attempt = await coordinator.TryScheduleAsync(
                tenantId,
                caseId,
                memoryUnitId,
                cancellationToken);

            return attempt.Outcome switch
            {
                ReIngestionAttemptOutcome.NotFound => Results.NotFound(new ErrorResponse(
                    "MEMORY_UNIT_NOT_FOUND",
                    $"Failed memory unit '{memoryUnitId}' was not found.",
                    "Verify the memory unit id.")),
                ReIngestionAttemptOutcome.CaseMismatch => Results.BadRequest(new ErrorResponse(
                    "CASE_MISMATCH",
                    "Memory unit belongs to a different case.",
                    "Use the case id reported by the failed-units list.")),
                ReIngestionAttemptOutcome.Conflict => Results.Conflict(new ErrorResponse(
                    "RE_INGESTION_IN_PROGRESS",
                    "Another re-ingestion is already in progress for this unit.",
                    "Wait for the current re-ingestion to complete or check status.")),
                ReIngestionAttemptOutcome.UnsupportedSourcePayload => Results.BadRequest(new ErrorResponse(
                    attempt.ErrorCode ?? "NON_URL_REINGESTION_UNAVAILABLE",
                    attempt.Message ?? "Cannot re-ingest this non-URL failed unit because the original source content is unavailable.",
                    attempt.Suggestion ?? "Re-ingest from the original file or event source if available, or ingest the content again.")),
                ReIngestionAttemptOutcome.Scheduled => Results.Accepted(
                    $"/api/ingest/{attempt.WorkflowInstanceId}",
                    new { newWorkflowInstanceId = attempt.WorkflowInstanceId, memoryUnitId }),
                _ => throw new InvalidOperationException($"Unsupported re-ingestion outcome '{attempt.Outcome}'."),
            };
        });

        // Story 6.3 FR12: bulk re-ingestion. Per-unit failures are isolated — one missing or conflicted unit
        // does not abort the batch. Body: { "all": true, "limit": 50 } OR { "memoryUnitIds": ["a","b"] }.
        app.MapPost("/api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest", async (
            string tenantId,
            string caseId,
            ReIngestRequest request,
            CaseService caseService,
            FailedUnitsRegistry registry,
            ReIngestionCoordinator coordinator,
            TenantStatusGuard tenantGuard,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            ErrorResponse? statusErr = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (statusErr is not null)
            {
                return TenantStatusGuard.ToHttpResult(statusErr);
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            int boundedLimit = Math.Clamp(request.Limit, 1, 500);
            List<string> targets;
            if (request.MemoryUnitIds is { Count: > 0 })
            {
                targets = request.MemoryUnitIds.Take(boundedLimit).ToList();
            }
            else if (request.All)
            {
                FailedUnitsPage page = await registry.ListAsync(tenantId, caseId, boundedLimit, 0, cancellationToken);
                targets = page.Units.Select(u => u.MemoryUnitId).ToList();
            }
            else
            {
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "Either 'memoryUnitIds' or 'all=true' must be supplied.", "Provide a list of memory unit ids or set 'all' to true."));
            }

            BulkReIngestionResponse response = await coordinator.TryScheduleManyAsync(
                tenantId,
                caseId,
                targets,
                cancellationToken);

            return Results.Ok(response);
        });

        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/activity", async (
            string tenantId,
            string caseId,
            int? limit,
            CaseService caseService,
            CaseActivityService activityService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            int effectiveLimit = Math.Clamp(limit ?? 50, 1, 500);
            List<CaseActivityEvent> events = await activityService.GetRecentActivityAsync(tenantId, caseId, effectiveLimit, cancellationToken);
            return Results.Ok(events);
        });

        app.MapPut("/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}", async (
            string tenantId,
            string caseId,
            string memberId,
            JsonElement requestBody,
            CaseService caseService,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.case_member");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationCaseMember,
                successEventId: 7508,
                errorEventId: 7518,
                tenantId,
                caseId,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "case-member-add",
                    ["memberIdPrefix"] = PrefixIdentifier(memberId, 32),
                });

            try
            {
            ErrorResponse? bodyError = TryDeserializeAddCaseMemberInput(requestBody, out AddCaseMemberInput? input);
            if (bodyError is not null)
            {
                scope.MarkValidationError(bodyError.Code);
                return Results.BadRequest(bodyError);
            }

            AddCaseMemberInput validatedInput = input! with { MemberId = memberId };
            ErrorResponse? error = CaseValidator.ValidateAddMember(tenantId, caseId, validatedInput);
            if (error is not null)
            {
                scope.MarkValidationError(error.Code);
                return Results.BadRequest(error);
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                scope.MarkValidationError("CASE_NOT_FOUND");
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            try
            {
                (CaseMember member, bool created) = await caseService.AddMemberAsync(tenantId, caseId, validatedInput, cancellationToken);
                return created
                    ? Results.Created($"/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}", member)
                    : Results.Ok(member);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("maximum"))
            {
                scope.MarkValidationError("MEMBER_LIMIT_EXCEEDED");
                return Results.BadRequest(new ErrorResponse("MEMBER_LIMIT_EXCEEDED", ex.Message, "Remove existing members before adding new ones."));
            }
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}", async (
            string tenantId,
            string caseId,
            string memberId,
            CaseService caseService,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.case_member");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationCaseMember,
                successEventId: 7508,
                errorEventId: 7518,
                tenantId,
                caseId,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "case-member-remove",
                    ["memberIdPrefix"] = PrefixIdentifier(memberId, 32),
                });

            try
            {
            ErrorResponse? error = CaseValidator.ValidateRemoveMember(tenantId, caseId, memberId);
            if (error is not null)
            {
                scope.MarkValidationError(error.Code);
                return Results.BadRequest(error);
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                scope.MarkValidationError("CASE_NOT_FOUND");
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            bool removed = await caseService.RemoveMemberAsync(tenantId, caseId, memberId, cancellationToken);
            if (!removed)
            {
                scope.MarkValidationError("MEMBER_NOT_FOUND");
            }

            return removed
                ? Results.NoContent()
                : Results.NotFound(new ErrorResponse("MEMBER_NOT_FOUND", $"Member '{memberId}' is not in case '{caseId}'.", "Run GET /cases/{caseId}/members to see current members."));
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/members", async (
            string tenantId,
            string caseId,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? caseIdError = CaseValidator.ValidateCaseId(caseId);
            if (caseIdError is not null)
            {
                return Results.BadRequest(caseIdError);
            }

            try
            {
                TenantIdGuard.Validate(tenantId);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
            }

            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            List<CaseMember> members = await caseService.ListMembersAsync(tenantId, caseId, cancellationToken);
            return Results.Ok(members);
        });

        app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}", async (
            string tenantId,
            string caseId,
            string memoryUnitId,
            CaseService caseService,
            TenantStatusGuard tenantGuard,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
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
                caseId,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "memory-unit-delete",
                    ["memoryUnitIdPrefix"] = PrefixIdentifier(memoryUnitId, 32),
                });

            try
            {
            ErrorResponse? validationError = CaseValidator.ValidateDeleteMemoryUnit(tenantId, caseId, memoryUnitId);
            if (validationError is not null)
            {
                scope.MarkValidationError(validationError.Code);
                return Results.BadRequest(validationError);
            }

            ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (tenantStatusError is not null)
            {
                scope.MarkTenantRejected(tenantStatusError.Code);
                return TenantStatusGuard.ToHttpResult(tenantStatusError);
            }

            Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (targetCase is null)
            {
                scope.MarkValidationError("CASE_NOT_FOUND");
                return Results.NotFound(new ErrorResponse(
                    "CASE_NOT_FOUND",
                    $"Case '{caseId}' not found in tenant '{tenantId}'.",
                    $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
            }

            if (targetCase.Status == CaseStatus.Deleting)
            {
                scope.MarkValidationError("CASE_DELETING");
                return Results.Conflict(new ErrorResponse(
                    "CASE_DELETING",
                    $"Case '{caseId}' is being deleted.",
                    "Wait for deletion to complete or retry later."));
            }

            bool deleted = await caseService.DeleteMemoryUnitAsync(tenantId, caseId, memoryUnitId, cancellationToken);
            if (!deleted)
            {
                scope.MarkValidationError("MEMORY_UNIT_NOT_FOUND");
            }

            return deleted
                ? Results.NoContent()
                : Results.NotFound(new ErrorResponse(
                    "MEMORY_UNIT_NOT_FOUND",
                    $"Memory unit '{memoryUnitId}' not found in case '{caseId}'.",
                    $"Use GET /api/search?tenantId={tenantId}&caseId={caseId} to find available memory units."));
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}", async (
            string tenantId,
            string caseId,
            CaseService caseService,
            TenantStatusGuard tenantGuard,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
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
                caseId,
                CreateAuditQueryParams("case-delete"));

            try
            {
            ErrorResponse? tenantError = CaseValidator.ValidateTenantId(tenantId);
            if (tenantError is not null)
            {
                scope.MarkValidationError(tenantError.Code);
                return Results.BadRequest(tenantError);
            }

            ErrorResponse? caseError = CaseValidator.ValidateCaseId(caseId);
            if (caseError is not null)
            {
                scope.MarkValidationError(caseError.Code);
                return Results.BadRequest(caseError);
            }

            ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
            if (tenantStatusError is not null)
            {
                scope.MarkTenantRejected(tenantStatusError.Code);
                return TenantStatusGuard.ToHttpResult(tenantStatusError);
            }

            bool deleted = await caseService.DeleteCaseAsync(tenantId, caseId, cancellationToken);
            if (!deleted)
            {
                scope.MarkValidationError("CASE_NOT_FOUND");
            }

            return deleted
                ? Results.NoContent()
                : Results.NotFound(new ErrorResponse(
                    "CASE_NOT_FOUND",
                    $"Case '{caseId}' not found in tenant '{tenantId}'.",
                    $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapPost("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations", async (
            string tenantId,
            string caseId,
            string memoryUnitId,
            CreateAnnotationInput input,
            CaseService caseService,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            using System.Diagnostics.Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.annotation");
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                activity,
                AccessTelemetryLog.OperationAnnotation,
                successEventId: 7509,
                errorEventId: 7519,
                tenantId,
                caseId,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "annotation-create",
                    ["memoryUnitIdPrefix"] = PrefixIdentifier(memoryUnitId, 32),
                });

            try
            {
            var validatedInput = input with { TenantId = tenantId, CaseId = caseId, TargetMemoryUnitId = memoryUnitId };
            ErrorResponse? validationError = CaseValidator.ValidateCreateAnnotation(tenantId, caseId, memoryUnitId, validatedInput);
            if (validationError is not null)
            {
                scope.MarkValidationError(validationError.Code);
                return Results.BadRequest(validationError);
            }

            Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (targetCase is null)
            {
                scope.MarkValidationError("CASE_NOT_FOUND");
                return Results.NotFound(new ErrorResponse(
                    "CASE_NOT_FOUND",
                    $"Case '{caseId}' not found in tenant '{tenantId}'.",
                    $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
            }

            if (targetCase.Status == CaseStatus.Deleting)
            {
                scope.MarkValidationError("CASE_DELETING");
                return Results.Conflict(new ErrorResponse(
                    "CASE_DELETING",
                    $"Case '{caseId}' is being deleted.",
                    "Wait for deletion to complete or retry later."));
            }

            try
            {
                var result = await caseService.CreateAnnotationAsync(validatedInput, cancellationToken);
                if (result is null)
                {
                    scope.MarkValidationError("MEMORY_UNIT_NOT_FOUND");
                    return Results.NotFound(new ErrorResponse(
                        "MEMORY_UNIT_NOT_FOUND",
                        $"Memory unit '{memoryUnitId}' not found in case '{caseId}'.",
                        $"Use GET /api/search?tenantId={tenantId}&caseId={caseId} to find available memory units."));
                }

                return Results.Accepted(
                    $"/api/ingest/{result.Value.WorkflowInstanceId}",
                    new { memoryUnit = result.Value.Annotation, instanceId = result.Value.WorkflowInstanceId });
            }
            catch (InvalidOperationException ex) when (ex.Message == "MEMORY_UNIT_NOT_INDEXED")
            {
                scope.MarkValidationError("MEMORY_UNIT_NOT_INDEXED");
                return Results.BadRequest(new ErrorResponse(
                    "MEMORY_UNIT_NOT_INDEXED",
                    $"Memory unit '{memoryUnitId}' is not yet indexed.",
                    "Wait for ingestion to complete before annotating."));
            }
            catch (InvalidOperationException ex) when (ex.Message == "NESTED_ANNOTATION_NOT_ALLOWED")
            {
                scope.MarkValidationError("NESTED_ANNOTATION_NOT_ALLOWED");
                return Results.BadRequest(new ErrorResponse(
                    "NESTED_ANNOTATION_NOT_ALLOWED",
                    "Cannot annotate an annotation. The target memory unit is itself an annotation.",
                    "Annotate the original memory unit instead."));
            }
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations", async (
            string tenantId,
            string caseId,
            string memoryUnitId,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? validationError = CaseValidator.ValidateDeleteMemoryUnit(tenantId, caseId, memoryUnitId);
            if (validationError is not null)
            {
                return Results.BadRequest(validationError);
            }

            Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (targetCase is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "CASE_NOT_FOUND",
                    $"Case '{caseId}' not found in tenant '{tenantId}'.",
                    $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
            }

            MemoryUnit? targetMemoryUnit = await caseService.GetMemoryUnitAsync(tenantId, memoryUnitId, cancellationToken);
            if (targetMemoryUnit is null || !string.Equals(targetMemoryUnit.CaseId, caseId, StringComparison.Ordinal))
            {
                return Results.NotFound(new ErrorResponse(
                    "MEMORY_UNIT_NOT_FOUND",
                    $"Memory unit '{memoryUnitId}' not found in case '{caseId}'.",
                    $"Use GET /api/search?tenantId={tenantId}&caseId={caseId} to find available memory units."));
            }

            List<MemoryUnit> annotations = await caseService.ListAnnotationsAsync(tenantId, memoryUnitId, cancellationToken);
            return Results.Ok(annotations);
        });

        return app;
    }

    private static ErrorResponse? TryDeserializeAddCaseMemberInput(JsonElement requestBody, out AddCaseMemberInput? input)
    {
        input = null;

        if (requestBody.ValueKind != JsonValueKind.Object)
        {
            return new ErrorResponse(
                "INVALID_MEMBER_INPUT",
                "Request body must be a JSON object.",
                "Provide a JSON object with memberType set to 'user' or 'role'.");
        }

        if (!TryGetJsonPropertyIgnoreCase(requestBody, "memberType", out JsonElement memberTypeElement) ||
            memberTypeElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(memberTypeElement.GetString()))
        {
            return new ErrorResponse(
                "INVALID_MEMBER_TYPE",
                "MemberType is required.",
                "Provide memberType as 'user' or 'role'.");
        }

        try
        {
            input = JsonSerializer.Deserialize<AddCaseMemberInput>(requestBody.GetRawText(), MemoriesJsonContext.Options);
        }
        catch (JsonException ex)
        {
            return new ErrorResponse(
                "INVALID_MEMBER_TYPE",
                ex.Message,
                "Provide memberType as 'user' or 'role'.");
        }

        return input is null
            ? new ErrorResponse(
                "INVALID_MEMBER_INPUT",
                "Request body is required.",
                "Provide a JSON object with memberType set to 'user' or 'role'.")
            : null;
    }

    private static bool TryGetJsonPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
