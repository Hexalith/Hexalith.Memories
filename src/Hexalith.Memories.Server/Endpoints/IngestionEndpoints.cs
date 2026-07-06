// <copyright file="IngestionEndpoints.cs" company="ITANEO">
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
internal static class IngestionEndpoints
{
    /// <summary>Maps this resource area's endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/ingest", async (
            IIngestionWorkflowScheduler workflowScheduler,
            TenantStatusGuard tenantGuard,
            IngestDedupReservation ingestReservation,
            IOptionsMonitor<Hexalith.Memories.EventStore.TenantEventRoutingOptions> ingestRoutingOptions,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            IngestionInput input) =>
        {
            long scheduledDocumentCount = 1;
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                MemoriesActivitySource.IngestRequest,
                AccessTelemetryLog.OperationIngest,
                successEventId: 7502,
                errorEventId: 7512,
                input.TenantId,
                input.CaseId,
                CreateIngestAuditQueryParams(input.SourceType, input.ContentType, IngestionPayloadClaimCheck.GetDeclaredPayloadLength(input)),
                recordMetricOnDispose: s =>
                {
                    if (s.Outcome == AccessTelemetryLog.OutcomeError)
                    {
                        TelemetryMetricsRecorder.RecordIngestFailure(s.TenantIdTag, s.ErrorCode ?? "UNKNOWN_ERROR");
                    }
                    else
                    {
                        TelemetryMetricsRecorder.RecordIngestSuccess(s.TenantIdTag, scheduledDocumentCount);
                    }
                });
            System.Diagnostics.Activity? activity = scope.Activity;
            activity?.SetTag(MemoriesActivitySource.TagTenantId, input.TenantId);
            activity?.SetTag(MemoriesActivitySource.TagCaseId, input.CaseId);
            activity?.SetTag(MemoriesActivitySource.TagSourceType, input.SourceType.ToString().ToLowerInvariant());

            try
            {
                ErrorResponse? validationError = ValidateIngestionRequest(input);
                if (validationError is not null)
                {
                    scope.MarkValidationError(validationError.Code);
                    return Results.BadRequest(validationError);
                }

                ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(input.TenantId, CancellationToken.None);
                if (tenantStatusError is not null)
                {
                    scope.MarkTenantRejected(tenantStatusError.Code);
                    return TenantStatusGuard.ToHttpResult(tenantStatusError);
                }

                // Story 18.4 (MEM-4) — atomic preflight dedup reservation closes the concurrent-ingest race on the
                // REST path. The candidate id becomes the workflow instance id (and, for SourceType.File, the
                // MemoryUnitId), so a losing concurrent ingest returns the winner's instance and observes the same
                // MemoryUnitId without scheduling a second workflow.
                string candidateInstanceId = Guid.NewGuid().ToString();
                Hexalith.Memories.EventStore.TenantEventRoutingOptions routing = ingestRoutingOptions.CurrentValue;
                bool reservationHeld = false;

                if (routing.PreflightDedupEnabled)
                {
                    IngestReservationResult reservation = await ingestReservation.TryReserveAsync(
                        input.TenantId,
                        input.CaseId,
                        input.SourceUri,
                        input.IdempotencyToken,
                        candidateInstanceId,
                        routing.PreflightDedupTtl,
                        CancellationToken.None);

                    if (reservation.Outcome == IngestReservationOutcome.DuplicateInFlight)
                    {
                        string winnerInstanceId = reservation.ExistingInstanceId!;
                        return Results.Accepted($"/api/ingest/{winnerInstanceId}", new { instanceId = winnerInstanceId });
                    }

                    // Reserved → we own the reservation; FailOpen (Redis down, ADR 9.1-B) → proceed anyway.
                    reservationHeld = reservation.Outcome == IngestReservationOutcome.Reserved;
                }

                try
                {
                    string instanceId = await workflowScheduler
                        .ScheduleAsync(candidateInstanceId, input, CancellationToken.None)
                        .ConfigureAwait(false);
                    return Results.Accepted($"/api/ingest/{instanceId}", new { instanceId });
                }
                catch
                {
                    if (reservationHeld)
                    {
                        await ingestReservation.ReleaseAsync(
                            input.TenantId, input.CaseId, input.SourceUri, input.IdempotencyToken, CancellationToken.None);
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        }).WithMetadata(new RequestSizeLimitAttribute(2 * 1024 * 1024))
            .AddEndpointFilter<TenantAuthorizationEndpointFilter>()
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        app.MapGet("/api/ingest/{instanceId}", async (
            IIngestionWorkflowStateReader workflowStateReader,
            HttpContext httpContext,
            ILogger<TenantAuthorizationEndpointFilter> authorizationLogger,
            string instanceId,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return Results.NotFound(new ErrorResponse(
                    "INGESTION_STATUS_NOT_FOUND",
                    "Ingestion workflow status was not found.",
                    "Use the instanceId returned by the ingestion scheduling endpoint."));
            }

            WorkflowState? state;
            try
            {
                state = await workflowStateReader.GetWorkflowStateAsync(
                    instanceId,
                    includeInputsAndOutputs: true,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                state = null;
            }

            if (state is null || !state.Exists)
            {
                return Results.NotFound(new ErrorResponse(
                    "INGESTION_STATUS_NOT_FOUND",
                    "Ingestion workflow status was not found or has expired.",
                    "Use the instanceId returned by the ingestion scheduling endpoint."));
            }

            if (!IngestionWorkflowStatusMapper.TryReadStoredTenantId(state, out string? storedTenantId))
            {
                _ = TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
                    httpContext,
                    null,
                    "/api/ingest/{instanceId}",
                    authorizationLogger,
                    out IResult? unreadableTenantResult);
                return unreadableTenantResult!;
            }

            if (!TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
                    httpContext,
                    storedTenantId,
                    "/api/ingest/{instanceId}",
                    authorizationLogger,
                    out IResult? authorizationResult))
            {
                return authorizationResult!;
            }

            if (!IngestionWorkflowStatusMapper.TryMap(instanceId, state, out IngestionWorkflowStatus? status))
            {
                return Results.Json(
                    new ErrorResponse(
                        "INGESTION_STATUS_UNREADABLE",
                        "Ingestion workflow status could not be projected safely.",
                        "Retry later or resubmit the ingestion request if the status remains unavailable."),
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(status);
        });

        app.MapPost("/api/ingest/url", async (
            DaprWorkflowClient workflowClient,
            IngestionWorkflowConfigurationCapture workflowConfigurationCapture,
            WorkflowTraceContextCapture workflowTraceContextCapture,
            TenantStatusGuard tenantGuard,
            Microsoft.Extensions.Options.IOptions<UrlFetcherOptions> urlFetcherOptions,
            ILoggerFactory loggerFactory,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            UrlIngestionRequest request,
            CancellationToken cancellationToken) =>
        {
            long scheduledDocumentCount = 1;
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                MemoriesActivitySource.IngestRequest,
                AccessTelemetryLog.OperationIngest,
                successEventId: 7502,
                errorEventId: 7512,
                request.TenantId,
                request.CaseId,
                CreateIngestAuditQueryParams(SourceType.Url, contentType: null, bytes: 0),
                recordMetricOnDispose: s =>
                {
                    if (s.Outcome == AccessTelemetryLog.OutcomeError)
                    {
                        TelemetryMetricsRecorder.RecordIngestFailure(s.TenantIdTag, s.ErrorCode ?? "UNKNOWN_ERROR");
                    }
                    else
                    {
                        TelemetryMetricsRecorder.RecordIngestSuccess(s.TenantIdTag, scheduledDocumentCount);
                    }
                });
            System.Diagnostics.Activity? activity = scope.Activity;
            activity?.SetTag(MemoriesActivitySource.TagTenantId, request.TenantId);
            activity?.SetTag(MemoriesActivitySource.TagCaseId, request.CaseId);
            activity?.SetTag(MemoriesActivitySource.TagSourceType, SourceType.Url.ToString().ToLowerInvariant());

            ILogger urlLogger = loggerFactory.CreateLogger("Hexalith.Memories.Server.Ingestion.Url");

            try
            {
                ErrorResponse? validationError = ValidateUrlIngestionRequest(request, urlFetcherOptions.Value, out Uri? url);
                if (validationError is not null || url is null)
                {
                    scope.MarkValidationError(validationError!.Code);
                    IngestionEndpointLog.LogUrlIngestionRejected(
                        urlLogger,
                        request?.TenantId ?? "(missing)",
                        request?.CaseId ?? "(missing)",
                        IngestionEndpointLog.RedactUrl(request?.Url),
                        validationError.Code);
                    return Results.BadRequest(validationError);
                }

                ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(request.TenantId, cancellationToken);
                if (tenantStatusError is not null)
                {
                    scope.MarkTenantRejected(tenantStatusError.Code);
                    IngestionEndpointLog.LogUrlIngestionRejected(
                        urlLogger,
                        request.TenantId,
                        request.CaseId,
                        IngestionEndpointLog.RedactUrl(request.Url),
                        tenantStatusError.Code);
                    return TenantStatusGuard.ToHttpResult(tenantStatusError);
                }

                IngestionInput input = new()
                {
                    TenantId = request.TenantId,
                    CaseId = request.CaseId,
                    SourceUri = request.Url,
                    ContentBytes = null,
                    ContentType = "application/octet-stream",
                    SourceType = SourceType.Url,
                    IngestedBy = request.IngestedBy,
                    Metadata = request.Metadata,
                    CausationId = request.CausationId,
                    CorrelationId = request.CorrelationId,
                };

                input = workflowTraceContextCapture.Apply(workflowConfigurationCapture.Apply(input));

                string instanceId = await workflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input);

                IngestionEndpointLog.LogUrlIngestionScheduled(
                    urlLogger,
                    request.TenantId,
                    request.CaseId,
                    instanceId,
                    IngestionEndpointLog.RedactUrl(url));

                return Results.Accepted(
                    $"/api/ingest/{instanceId}",
                    new UrlIngestionResponse(instanceId, request.Url));
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        }).AddEndpointFilter<TenantAuthorizationEndpointFilter>()
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        app.MapPost("/api/ingest/directory", async (
            DirectoryIngestionService directoryService,
            TenantStatusGuard tenantGuard,
            ILoggerFactory loggerFactory,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            DirectoryIngestionRequest request,
            CancellationToken cancellationToken) =>
        {
            long scheduledDocumentCount = 0;
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                MemoriesActivitySource.IngestRequest,
                AccessTelemetryLog.OperationIngest,
                successEventId: 7502,
                errorEventId: 7512,
                request.TenantId,
                request.CaseId,
                CreateIngestAuditQueryParams(SourceType.File, contentType: null, bytes: 0),
                recordMetricOnDispose: s =>
                {
                    if (s.Outcome == AccessTelemetryLog.OutcomeError)
                    {
                        TelemetryMetricsRecorder.RecordIngestFailure(s.TenantIdTag, s.ErrorCode ?? "UNKNOWN_ERROR");
                    }
                    else
                    {
                        TelemetryMetricsRecorder.RecordIngestSuccess(s.TenantIdTag, scheduledDocumentCount);
                    }
                });
            System.Diagnostics.Activity? activity = scope.Activity;
            activity?.SetTag(MemoriesActivitySource.TagTenantId, request.TenantId);
            activity?.SetTag(MemoriesActivitySource.TagCaseId, request.CaseId);
            activity?.SetTag(MemoriesActivitySource.TagSourceType, SourceType.File.ToString().ToLowerInvariant());

            ILogger dirLogger = loggerFactory.CreateLogger("Hexalith.Memories.Server.Ingestion.Directory");

            try
            {
                ErrorResponse? shapeError = ValidateDirectoryIngestionRequest(request);
                if (shapeError is not null)
                {
                    scope.MarkValidationError(shapeError.Code);
                    IngestionEndpointLog.LogDirectoryBatchRejected(
                        dirLogger,
                        request?.TenantId ?? "(missing)",
                        request?.CaseId ?? "(missing)",
                        null,
                        shapeError.Code,
                        request?.DirectoryPath ?? string.Empty);
                    return Results.BadRequest(shapeError);
                }

                ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(request.TenantId, cancellationToken);
                if (tenantStatusError is not null)
                {
                    scope.MarkTenantRejected(tenantStatusError.Code);
                    IngestionEndpointLog.LogDirectoryBatchRejected(
                        dirLogger,
                        request.TenantId,
                        request.CaseId,
                        null,
                        tenantStatusError.Code,
                        request.DirectoryPath);
                    return TenantStatusGuard.ToHttpResult(tenantStatusError);
                }

                DirectoryIngestionResult result = await directoryService.IngestAsync(request, cancellationToken);
                if (result.ErrorCode is not null)
                {
                    scope.MarkValidationError(result.ErrorCode);
                    IngestionEndpointLog.LogDirectoryBatchRejected(
                        dirLogger,
                        request.TenantId,
                        request.CaseId,
                        result.BatchId,
                        result.ErrorCode,
                        request.DirectoryPath);

                    return result.ErrorCode switch
                    {
                        "DIRECTORY_INGESTION_DISABLED" => Results.Json(
                            new ErrorResponse(
                                "DIRECTORY_INGESTION_DISABLED",
                                "Directory ingestion is not enabled on this server.",
                                "Configure Ingestion:AllowedDirectoryRoots to enable."),
                            statusCode: StatusCodes.Status403Forbidden),
                        "BATCH_TOO_LARGE" => Results.Json(
                            new ErrorResponse(
                                "BATCH_TOO_LARGE",
                                "Batch exceeds the maximum supported number of files.",
                                "Ingest smaller sub-directories, or call POST /api/ingest per file."),
                            statusCode: StatusCodes.Status400BadRequest),
                        "BATCH_TRACKING_UNAVAILABLE" => Results.Json(
                            new ErrorResponse(
                                "BATCH_TRACKING_UNAVAILABLE",
                                "Directory batch tracking is temporarily unavailable.",
                                "Retry when the DAPR state store is healthy; no successful batch response was returned."),
                            statusCode: StatusCodes.Status503ServiceUnavailable),
                        "DAPR_UNAVAILABLE" => Results.Json(
                            ErrorResults.DaprUnavailable(),
                            statusCode: StatusCodes.Status503ServiceUnavailable),
                        "BATCH_SCHEDULING_FAILED" => Results.Json(
                            new ErrorResponse(
                                "BATCH_SCHEDULING_FAILED",
                                "Directory batch scheduling failed before the batch could be safely accepted.",
                                "Inspect server logs and retry the request."),
                            statusCode: StatusCodes.Status500InternalServerError),
                        _ => Results.Json(
                            new ErrorResponse(
                                "INVALID_DIRECTORY_PATH",
                                "Directory path is not allowed.",
                                "Provide an absolute path under a configured Ingestion:AllowedDirectoryRoots entry."),
                            statusCode: StatusCodes.Status400BadRequest),
                    };
                }

                DirectoryIngestionOutcome outcome = result.Outcome!;
                scheduledDocumentCount = outcome.Enqueued;
                IngestionEndpointLog.LogDirectoryBatchScheduled(
                    dirLogger,
                    request.TenantId,
                    request.CaseId,
                    outcome.BatchId,
                    outcome.Discovered,
                    outcome.Enqueued,
                    outcome.Skipped.Count);

                return Results.Accepted(
                    $"/api/ingest/batches/{outcome.BatchId}",
                    outcome);
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        }).AddEndpointFilter<TenantAuthorizationEndpointFilter>()
            .AddEndpointFilter<InboundRateLimitEndpointFilter>();

        app.MapGet("/api/ingest/batches/{batchId}", async (
            DaprClient daprClient,
            IIngestionWorkflowStateReader workflowStateReader,
            HttpContext httpContext,
            ILogger<TenantAuthorizationEndpointFilter> authorizationLogger,
            string batchId,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                return Results.NotFound();
            }

            DirectoryBatchState? state;
            try
            {
                state = await daprClient.GetStateAsync<DirectoryBatchState>(
                    DirectoryIngestionService.StateStoreName,
                    DirectoryIngestionService.BatchStateKeyPrefix + batchId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                state = null;
            }

            if (state is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "BATCH_NOT_FOUND",
                    $"Batch '{batchId}' was not found or has expired.",
                    "Verify the batchId returned by POST /api/ingest/directory."));
            }

            if (!TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
                httpContext,
                state.TenantId,
                "/api/ingest/batches/{batchId}",
                authorizationLogger,
                out IResult? authorizationResult))
            {
                return authorizationResult!;
            }

            using SemaphoreSlim gate = new(50);
            Task<BatchInstanceStatus>[] statusTasks = state.Files.Select(async file =>
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    WorkflowState? wfState = await workflowStateReader
                        .GetWorkflowStateAsync(file.InstanceId, includeInputsAndOutputs: true, cancellationToken)
                        .ConfigureAwait(false);
                    return DirectoryBatchStatusMapper.MapInstance(file, wfState);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return DirectoryBatchStatusMapper.MapInstance(file, null);
                }
                finally
                {
                    gate.Release();
                }
            }).ToArray();

            BatchInstanceStatus[] instances = await Task.WhenAll(statusTasks);
            BatchStatusCounts counts = DirectoryBatchStatusMapper.BuildCounts(instances);

            BatchStatusResponse response = new(
                state.BatchId,
                state.TenantId,
                state.CaseId,
                Discovered: state.Discovered,
                Enqueued: state.Files.Length,
                Skipped: state.Skipped.Length,
                Counts: counts,
                Instances: instances);

            return Results.Ok(response);
        });

        return app;
    }

    private static ErrorResponse? ValidateUrlIngestionRequest(UrlIngestionRequest request, UrlFetcherOptions options, out Uri? url)
    {
        url = null;
        if (request is null)
        {
            return new ErrorResponse("INVALID_INPUT", "Request body is required.", "Provide a JSON body with tenantId, caseId, url, and ingestedBy.");
        }

        ErrorResponse? tenantError = ValidateTenantId(request.TenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (string.IsNullOrWhiteSpace(request.CaseId))
        {
            return new ErrorResponse("INVALID_INPUT", "CaseId is required.", "Provide a non-empty caseId.");
        }

        if (string.IsNullOrWhiteSpace(request.IngestedBy))
        {
            return new ErrorResponse("INVALID_INPUT", "IngestedBy is required.", "Provide the identity of the ingesting principal.");
        }

        if (string.IsNullOrWhiteSpace(request.Url)
            || !Uri.TryCreate(request.Url, UriKind.Absolute, out Uri? parsed)
            || (parsed.Scheme is not "http" and not "https"))
        {
            return new ErrorResponse(
                "INVALID_URL",
                "URL scheme or host is not allowed.",
                "Use an http(s) URL with a publicly routable host.");
        }

        if (!UrlHostValidator.IsAllowedHost(parsed, options))
        {
            return new ErrorResponse(
                "INVALID_URL",
                "URL scheme or host is not allowed.",
                "Use an http(s) URL with a publicly routable host. Set Ingestion:UrlFetcher:AllowPrivateHosts=true in configuration to allow private hosts (development only).");
        }

        foreach ((string key, MetadataField field) in request.Metadata)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new ErrorResponse("INVALID_INPUT", "Metadata keys must not be empty.", "Remove empty metadata keys.");
            }

            if (float.IsNaN(field.Confidence) || float.IsInfinity(field.Confidence) || field.Confidence < 0f || field.Confidence > 1f)
            {
                return new ErrorResponse(
                    "INVALID_INPUT",
                    $"Metadata field '{key}' confidence must be between 0.0 and 1.0.",
                    "Adjust metadata confidence to a value between 0 and 1.");
            }
        }

        url = parsed;
        return null;
    }

    private static ErrorResponse? ValidateDirectoryIngestionRequest(DirectoryIngestionRequest request)
    {
        if (request is null)
        {
            return new ErrorResponse("INVALID_INPUT", "Request body is required.", "Provide a JSON body with tenantId, caseId, directoryPath, and ingestedBy.");
        }

        ErrorResponse? tenantError = ValidateTenantId(request.TenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (string.IsNullOrWhiteSpace(request.CaseId))
        {
            return new ErrorResponse("INVALID_INPUT", "CaseId is required.", "Provide a non-empty caseId.");
        }

        if (string.IsNullOrWhiteSpace(request.IngestedBy))
        {
            return new ErrorResponse("INVALID_INPUT", "IngestedBy is required.", "Provide the identity of the ingesting principal.");
        }

        if (string.IsNullOrWhiteSpace(request.DirectoryPath))
        {
            return new ErrorResponse("INVALID_INPUT", "DirectoryPath is required.", "Provide an absolute directory path under a configured root.");
        }

        foreach ((string key, MetadataField field) in request.Metadata)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new ErrorResponse("INVALID_INPUT", "Metadata keys must not be empty.", "Remove empty metadata keys.");
            }

            if (float.IsNaN(field.Confidence) || float.IsInfinity(field.Confidence) || field.Confidence < 0f || field.Confidence > 1f)
            {
                return new ErrorResponse(
                    "INVALID_INPUT",
                    $"Metadata field '{key}' confidence must be between 0.0 and 1.0.",
                    "Adjust metadata confidence to a value between 0 and 1.");
            }
        }

        return null;
    }

    private static ErrorResponse? ValidateIngestionRequest(IngestionInput input)
    {
        try
        {
            IngestionInputValidator.Validate(input);
            return null;
        }
        catch (ArgumentException ex)
        {
            return new ErrorResponse(
                "INVALID_INPUT",
                ex.Message,
                "Ensure the ingestion request is valid before scheduling ingestion.");
        }
    }
}
