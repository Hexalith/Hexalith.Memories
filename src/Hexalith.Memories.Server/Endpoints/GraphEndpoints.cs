// <copyright file="GraphEndpoints.cs" company="ITANEO">
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
internal static class GraphEndpoints
{
    /// <summary>Maps this resource area's endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapGraphEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/tenants/{tenantId}/traverse", async (
            string tenantId,
            GraphTraversalService traversalService,
            ILogger<global::Program> logger,
            ILogger<AccessTelemetryCategory> auditLogger,
            HttpContext httpContext,
            [FromQuery] string? startNodeId,
            [FromQuery] int depth = 2,
            [FromQuery] string? caseId = null,
            [FromQuery] string? edgeTypes = null,
            [FromQuery] int? tokenBudget = null,
            CancellationToken cancellationToken = default) =>
        {
            using EndpointTelemetryScope scope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                MemoriesActivitySource.TraverseRequest,
                AccessTelemetryLog.OperationTraverse,
                successEventId: 7503,
                errorEventId: 7513,
                tenantId,
                caseId,
                new Dictionary<string, object?>(System.StringComparer.Ordinal)
            {
                ["startNodeId"] = startNodeId,
                ["depth"] = depth,
                ["edgeTypes"] = edgeTypes,
                ["tokenBudget"] = tokenBudget,
            });

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                scope.MarkValidationError("INVALID_TENANT_ID");
                return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId is required.", "Provide a valid tenantId."));
            }

            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                scope.MarkValidationError(tenantValidationError.Code);
                return Results.BadRequest(tenantValidationError);
            }

            if (string.IsNullOrWhiteSpace(startNodeId))
            {
                scope.MarkValidationError("MISSING_START_NODE");
                return Results.BadRequest(new ErrorResponse(
                    "MISSING_START_NODE",
                    "startNodeId query parameter is required.",
                    "Provide startNodeId=<memoryUnitId> to specify the traversal starting point."));
            }

            // Parse edgeTypes: null/empty/whitespace means "use default semantic types".
            IReadOnlyList<EdgeType>? parsedEdgeTypes = null;
            if (!string.IsNullOrWhiteSpace(edgeTypes))
            {
                List<EdgeType> types = [];
                string[] parts = edgeTypes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                string validTypesString = string.Join(", ", Enum.GetValues<EdgeType>().Select(e => char.ToLowerInvariant(e.ToString()[0]) + e.ToString()[1..]));
                foreach (string part in parts)
                {
                    if (!Enum.TryParse<EdgeType>(part, ignoreCase: true, out EdgeType et) || !Enum.IsDefined(et))
                    {
                        scope.MarkValidationError("INVALID_EDGE_TYPE");
                        return Results.BadRequest(new ErrorResponse(
                            "INVALID_EDGE_TYPE",
                            $"Unknown edge type: '{part}'. Valid types: {validTypesString}",
                            "Use comma-separated camelCase edge type names (not underscore format)."));
                    }

                    types.Add(et);
                }

                parsedEdgeTypes = types;
            }

            int clampedDepth = Math.Clamp(depth, 0, 10);
            try
            {
                TraversalResult result = await traversalService.TraverseAsync(
                    tenantId, startNodeId, clampedDepth, caseId, parsedEdgeTypes, cancellationToken);
                result = TraverseResponseMetadataApplier.ApplyTraversal(result, tokenBudget);
                scope.ResultCount = result.Nodes.Count;
                return Results.Ok(result);
            }
            catch (RedisConnectionException ex)
            {
                logger.LogWarning(ex, "Graph traversal degraded for tenant {TenantId} and start node {StartNodeId}", tenantId, startNodeId);
                scope.MarkPartial("GRAPH_UNAVAILABLE");
                return Results.Ok(new TraversalResult(startNodeId, clampedDepth, [], 0)
                {
                    Degraded = true,
                    UnavailableAxes = ["graph"],
                    OmittedReason = OmittedReason.BackendDegraded,
                });
            }
            catch (RedisTimeoutException ex)
            {
                logger.LogWarning(ex, "Graph traversal degraded for tenant {TenantId} and start node {StartNodeId}", tenantId, startNodeId);
                scope.MarkPartial("GRAPH_UNAVAILABLE");
                return Results.Ok(new TraversalResult(startNodeId, clampedDepth, [], 0)
                {
                    Degraded = true,
                    UnavailableAxes = ["graph"],
                    OmittedReason = OmittedReason.BackendDegraded,
                });
            }
            catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
            {
                logger.LogWarning(ex, "Graph traversal degraded for tenant {TenantId} and start node {StartNodeId}", tenantId, startNodeId);
                scope.MarkPartial("GRAPH_UNAVAILABLE");
                return Results.Ok(new TraversalResult(startNodeId, clampedDepth, [], 0)
                {
                    Degraded = true,
                    UnavailableAxes = ["graph"],
                    OmittedReason = OmittedReason.BackendDegraded,
                });
            }
            catch (TimeoutException)
            {
                scope.MarkValidationError("GRAPH_TIMEOUT");
                return SearchEndpointDegradationResponses.BuildGraphTimeoutResponse();
            }
            catch (Exception ex)
            {
                scope.MarkUnhandledException(ex);
                throw;
            }
        });

        app.MapPatch("/api/tenants/{tenantId}/edges/confidence", async (
            string tenantId,
            JsonElement requestBody,
            GraphTraversalService traversalService,
            CancellationToken cancellationToken) =>
        {
            ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
            if (tenantValidationError is not null)
            {
                return Results.BadRequest(tenantValidationError);
            }

            ErrorResponse? requestBodyError = TryReadConfidencePromotionRequest(requestBody, out ConfidencePromotionRequest? request);
            if (requestBodyError is not null)
            {
                return Results.BadRequest(requestBodyError);
            }

            if (string.IsNullOrWhiteSpace(request!.SourceNodeId))
            {
                return Results.BadRequest(new ErrorResponse(
                    "MISSING_SOURCE_NODE",
                    "sourceNodeId is required.",
                    "Provide the source node ID of the edge to promote."));
            }

            if (string.IsNullOrWhiteSpace(request.TargetNodeId))
            {
                return Results.BadRequest(new ErrorResponse(
                    "MISSING_TARGET_NODE",
                    "targetNodeId is required.",
                    "Provide the target node ID of the edge to promote."));
            }

            if (string.IsNullOrWhiteSpace(request.VerifiedBy))
            {
                return Results.BadRequest(new ErrorResponse(
                    "MISSING_VERIFIED_BY",
                    "verifiedBy is required.",
                    "Provide the identity of the person verifying the relationship."));
            }

            if (!float.IsFinite(request.NewConfidence) || request.NewConfidence < 0f || request.NewConfidence > 1f)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_CONFIDENCE",
                    $"Confidence must be between 0.0 and 1.0, got {request.NewConfidence}.",
                    "Provide a confidence value in the range [0.0, 1.0]."));
            }

            ConfidencePromotionResult? result = await traversalService.PromoteEdgeConfidenceAsync(
                tenantId, request, cancellationToken);

            if (result is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "EDGE_NOT_FOUND",
                    $"No {request.EdgeType} edge found from '{request.SourceNodeId}' to '{request.TargetNodeId}'.",
                    "Verify the edge exists by traversing from either node. Note: edges are directed — sourceNodeId must be the relationship origin (e.g., for causedBy, the CausationId is the source)."));
            }

            return Results.Ok(result);
        });

        return app;
    }

    private static ErrorResponse? TryReadConfidencePromotionRequest(
        JsonElement requestBody,
        out ConfidencePromotionRequest? request)
    {
        request = null;

        if (requestBody.ValueKind != JsonValueKind.Object)
        {
            return new ErrorResponse(
                "INVALID_REQUEST_BODY",
                "Request body must be a JSON object.",
                "Provide a valid confidence promotion request payload.");
        }

        if (!requestBody.TryGetProperty("edgeType", out _))
        {
            return new ErrorResponse(
                "MISSING_EDGE_TYPE",
                "edgeType is required.",
                "Provide the relationship type of the edge to promote.");
        }

        if (!requestBody.TryGetProperty("newConfidence", out _))
        {
            return new ErrorResponse(
                "MISSING_NEW_CONFIDENCE",
                "newConfidence is required.",
                "Provide the new confidence value in the range [0.0, 1.0].");
        }

        try
        {
            request = JsonSerializer.Deserialize<ConfidencePromotionRequest>(requestBody.GetRawText(), MemoriesJsonContext.Options);
        }
        catch (JsonException ex)
        {
            return new ErrorResponse(
                "INVALID_REQUEST_BODY",
                ex.Message,
                "Provide a valid confidence promotion request payload.");
        }

        return request is null
            ? new ErrorResponse(
                "INVALID_REQUEST_BODY",
                "Request body could not be deserialized.",
                "Provide a valid confidence promotion request payload.")
            : null;
    }
}
