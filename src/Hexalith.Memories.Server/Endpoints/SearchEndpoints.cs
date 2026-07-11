// <copyright file="SearchEndpoints.cs" company="ITANEO">
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
internal static class SearchEndpoints
{
    /// <summary>Maps this resource area's endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(MemoriesRoutes.Search, async (
            SyntacticSearchService syntacticService,
            SemanticSearchService semanticService,
            NaturalLanguageSemanticSearchService naturalLanguageService,
            GraphScopedSearch graphScopedSearch,
            HybridSearchService hybridSearchService,
            ITenantEmbeddingConfigProvider embeddingConfigProvider,
            CaseService caseService,
            TenantStatusGuard tenantGuard,
            IGraphQueryBuilder graphQueryBuilder,
            [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
            ILogger<global::Program> logger,
            ILogger<AccessTelemetryCategory> auditLogger,
            RollingCounterStore rollingCounterStore,
            HttpContext httpContext,
            [FromQuery] string tenantId,
            [FromQuery] string? query,
            [FromQuery] string? caseId,
            [FromQuery] string? sourceType = null,
            [FromQuery] string? metadataQuery = null,
            [FromQuery] string? subject = null,
            [FromQuery] int maxResults = 10,
            [FromQuery] int offset = 0,
            [FromQuery] string axis = "syntactic",
            [FromQuery] string? axes = null,
            [FromQuery] string? startNodeId = null,
            [FromQuery(Name = "graphStartNodeId")] string? graphStartNodeId = null,
            [FromQuery] int depth = 2,
            [FromQuery] bool explain = false,
            [FromQuery] int? tokenBudget = null,
            [FromQuery] double? syntacticWeight = null,
            [FromQuery] double? semanticWeight = null,
            [FromQuery] double? graphWeight = null,
            [FromQuery] double? nlWeight = null,
            CancellationToken cancellationToken = default) =>
        {
            static string? DetermineSearchAxisMetricTag(string requestedAxis, string? graphScopedStartNodeId)
            {
                bool isGraphScoped = !string.IsNullOrWhiteSpace(graphScopedStartNodeId);

                if (string.Equals(requestedAxis, "semantic", StringComparison.OrdinalIgnoreCase))
                {
                    return isGraphScoped ? "graph-scoped-semantic" : "semantic";
                }

                if (string.Equals(requestedAxis, "graph", StringComparison.OrdinalIgnoreCase))
                {
                    return "graph";
                }

                if (string.Equals(requestedAxis, "nl", StringComparison.OrdinalIgnoreCase))
                {
                    return "nl";
                }

                if (string.Equals(requestedAxis, "hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    return "hybrid";
                }

                if (string.Equals(requestedAxis, "syntactic", StringComparison.OrdinalIgnoreCase))
                {
                    return isGraphScoped ? "graph-scoped-syntactic" : "syntactic";
                }

                return null;
            }

            string? searchAxisTag = DetermineSearchAxisMetricTag(axis, startNodeId);
            IReadOnlyDictionary<string, string>? attributeFilters = ReadAttributeFilters(httpContext.Request.Query);
            Dictionary<string, object?> searchQueryParams = new(System.StringComparer.Ordinal)
            {
                ["query"] = query,
                ["axis"] = axis,
                ["axes"] = axes,
                ["maxResults"] = maxResults,
                ["offset"] = offset,
                ["sourceType"] = sourceType,
                ["subject"] = subject,
                ["metadataFilterCount"] = string.IsNullOrWhiteSpace(metadataQuery) ? 0 : metadataQuery.Split(',').Length,
                ["attributeFilterCount"] = attributeFilters?.Count ?? 0,
                ["explain"] = explain,
                ["tokenBudget"] = tokenBudget,
                ["syntacticWeight"] = syntacticWeight,
                ["semanticWeight"] = semanticWeight,
                ["graphWeight"] = graphWeight,
                ["nlWeight"] = nlWeight,
            };
            using EndpointTelemetryScope searchScope = CreateEndpointAuditScope(
                auditLogger,
                httpContext,
                MemoriesActivitySource.SearchRequest,
                AccessTelemetryLog.OperationSearch,
                successEventId: 7501,
                errorEventId: 7511,
                tenantId,
                caseId,
                searchQueryParams,
                recordMetricOnDispose: s =>
                {
                    if (!string.IsNullOrWhiteSpace(searchAxisTag))
                    {
                        TelemetryMetricsRecorder.RecordSearch(s.TenantIdTag, searchAxisTag, s.ElapsedMs);
                        if (s.Outcome == AccessTelemetryLog.OutcomeError)
                        {
                            rollingCounterStore.RecordSearchError(s.TenantIdTag, searchAxisTag);
                        }
                    }
                });
            System.Diagnostics.Activity? searchActivity = searchScope.Activity;
            if (!string.IsNullOrWhiteSpace(searchAxisTag))
            {
                searchActivity?.SetTag(MemoriesActivitySource.TagAxis, searchAxisTag);
            }

            void SetResolvedSearchAxis(string resolvedAxis)
            {
                searchAxisTag = resolvedAxis;
                searchQueryParams["axis"] = resolvedAxis;
                searchActivity?.SetTag(MemoriesActivitySource.TagAxis, resolvedAxis);
            }

            void CompleteSearchSuccess(string resolvedAxis, int resultCount)
            {
                SetResolvedSearchAxis(resolvedAxis);
                searchScope.ResultCount = resultCount;
            }

            IResult SearchError(string errorCode, IResult result)
            {
                searchScope.MarkValidationError(errorCode);
                return result;
            }

            IResult SearchTenantRejected(string errorCode, IResult result)
            {
                searchScope.MarkTenantRejected(errorCode);
                return result;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    return SearchTenantRejected(
                        "INVALID_INPUT",
                        Results.BadRequest(new ErrorResponse(
                            "INVALID_INPUT",
                            "Parameter 'tenantId' is required.",
                            "Provide tenantId as a query parameter.")));
                }

                searchActivity?.SetTag(MemoriesActivitySource.TagTenantId, tenantId);

                ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
                if (tenantValidationError is not null)
                {
                    return SearchError(tenantValidationError.Code, Results.BadRequest(tenantValidationError));
                }

                ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
                if (tenantStatusError is not null)
                {
                    return SearchTenantRejected(tenantStatusError.Code, TenantStatusGuard.ToHttpResult(tenantStatusError));
                }

                // Validate caseId exists before executing search
                if (!string.IsNullOrWhiteSpace(caseId))
                {
                    Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
                    if (targetCase is null)
                    {
                        return SearchError(
                            "CASE_NOT_FOUND",
                            Results.NotFound(new ErrorResponse(
                                "CASE_NOT_FOUND",
                                $"Case '{caseId}' not found in tenant '{tenantId}'.",
                                $"Use GET /{MemoriesRoutes.CasesPath(tenantId)} to list available cases.")));
                    }
                }

                // Validate sourceType is a known enum value
                if (!string.IsNullOrWhiteSpace(sourceType) && !Enum.TryParse<SourceType>(sourceType, ignoreCase: true, out _))
                {
                    return SearchError(
                        "INVALID_SOURCE_TYPE",
                        Results.BadRequest(new ErrorResponse(
                            "INVALID_SOURCE_TYPE",
                            $"Source type '{sourceType}' is not recognized.",
                            "Valid values: file, url, event, command, projection, discussion, annotation.")));
                }

                // Validate axis BEFORE query — axis determines whether query is required
                if (!string.Equals(axis, "syntactic", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(axis, "nl", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(axis, "graph", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(axis, "hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    return SearchError(
                        "INVALID_AXIS",
                        Results.BadRequest(new ErrorResponse(
                            "INVALID_AXIS",
                            $"Search axis '{axis}' is not supported. Supported axes: syntactic, semantic, nl, graph, hybrid.",
                            "Use axis=syntactic, axis=semantic, axis=nl, axis=graph, or axis=hybrid.")));
                }

                // --- axis=graph: pure traversal (query NOT required) ---
                if (string.Equals(axis, "graph", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(startNodeId))
                    {
                        return SearchError(
                            "MISSING_START_NODE",
                            Results.BadRequest(new ErrorResponse(
                                "MISSING_START_NODE",
                                "Graph-scoped search requires a startNodeId parameter.",
                                "Provide startNodeId=<memoryUnitId> to specify the graph traversal starting point.")));
                    }

                    int clampedDepth = Math.Clamp(depth, 0, 10);
                    int clampedMaxResults = Math.Clamp(maxResults, 1, 100);
                    var searchQuery = new SearchQuery
                    {
                        TenantId = tenantId,
                        Query = query ?? string.Empty,
                        CaseId = caseId,
                        SourceTypeFilter = sourceType,
                        MetadataQuery = metadataQuery,
                        CloudEventSubject = subject,
                        AttributeFilters = attributeFilters,
                        MaxResults = clampedMaxResults,
                        Offset = Math.Max(offset, 0),
                    };

                    SearchResult result;
                    try
                    {
                        result = await graphScopedSearch.SearchAsync(
                            searchQuery, startNodeId, clampedDepth,
                            innerSearch: null, cancellationToken);
                    }
                    catch (SearchPaginationLimitExceededException ex)
                    {
                        return SearchError(
                            "PAGINATION_LIMIT_EXCEEDED",
                            Results.BadRequest(SearchEndpointErrorResponseFactory.CreatePaginationLimitExceeded(ex)));
                    }
                    catch (RedisConnectionException ex)
                    {
                        return SearchError(
                            "GRAPH_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex));
                    }
                    catch (RedisTimeoutException ex)
                    {
                        return SearchError(
                            "GRAPH_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex));
                    }
                    catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
                    {
                        return SearchError(
                            "GRAPH_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex));
                    }
                    catch (TimeoutException)
                    {
                        return SearchError("GRAPH_TIMEOUT", SearchEndpointDegradationResponses.BuildGraphTimeoutResponse());
                    }

                    result = await EnrichResultWithCaseAttributionAsync(result, caseService, tenantId, cancellationToken);
                    result = await EnrichResultWithAnnotationCountsAsync(result, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
                    if (explain)
                    {
                        result = result with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("graph") };
                    }

                    result = SearchResponseMetadataApplier.ApplySearch(result, "graph", tokenBudget);
                    CompleteSearchSuccess("graph", result.Results.Count);
                    return Results.Ok(result);
                }

                // --- axis=hybrid: multi-axis fusion ---
                if (string.Equals(axis, "hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        return SearchError(
                            "INVALID_INPUT",
                            Results.BadRequest(new ErrorResponse(
                                "INVALID_INPUT",
                                "Parameter 'query' is required for hybrid search.",
                                "Provide query as a query parameter.")));
                    }

                    HashSet<string> enabledAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
                    if (!string.IsNullOrWhiteSpace(axes))
                    {
                        enabledAxes = new HashSet<string>(
                            axes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                            StringComparer.OrdinalIgnoreCase);

                        if (enabledAxes.Count == 0)
                        {
                            return SearchError(
                                "INVALID_AXIS",
                                Results.BadRequest(new ErrorResponse(
                                    "INVALID_AXIS",
                                    "Parameter 'axes' must specify at least one search axis. Valid axes: syntactic, semantic, graph, nl.",
                                    "Use a comma-separated list of valid axis names, e.g., axes=syntactic,semantic,nl.")));
                        }
                    }

                    string? invalidAxis = HybridSearchService.FindInvalidAxis(enabledAxes);
                    if (invalidAxis is not null)
                    {
                        return SearchError(
                            "INVALID_AXIS",
                            Results.BadRequest(new ErrorResponse(
                                "INVALID_AXIS",
                                $"Unknown axis '{invalidAxis}' in axes parameter. Valid axes: syntactic, semantic, graph, nl.",
                                "Use a comma-separated list of valid axis names, e.g., axes=syntactic,semantic,nl.")));
                    }

                    var hybridQuery = new SearchQuery
                    {
                        TenantId = tenantId,
                        Query = query,
                        CaseId = caseId,
                        SourceTypeFilter = sourceType,
                        MetadataQuery = metadataQuery,
                        CloudEventSubject = subject,
                        AttributeFilters = attributeFilters,
                        Weights = CreateQueryFusionWeights(syntacticWeight, semanticWeight, graphWeight, nlWeight),
                        MaxResults = Math.Clamp(maxResults, 1, 100),
                        Offset = Math.Max(offset, 0),
                    };

                    FusionWeights? queryWeights;
                    try
                    {
                        queryWeights = hybridQuery.Weights;
                        queryWeights?.Validate();
                    }
                    catch (ArgumentException ex)
                    {
                        return SearchError(
                            "INVALID_FUSION_WEIGHTS",
                            Results.BadRequest(new ErrorResponse(
                                "INVALID_FUSION_WEIGHTS",
                                ex.Message,
                                "Provide finite, non-negative fusion weights with at least one weight greater than zero.")));
                    }

                    FusionWeights weights = queryWeights ?? new FusionWeights();
                    TenantEmbeddingConfig? embeddingConfig = null;
                    List<string> preUnavailableAxes = [];
                    string? effectiveGraphStartNodeId = !string.IsNullOrWhiteSpace(graphStartNodeId)
                        ? graphStartNodeId
                        : startNodeId;
                    Exception? semanticConfigFailure = null;
                    if (queryWeights is null)
                    {
                        try
                        {
                            FusionWeights? tenantWeights = await embeddingConfigProvider.GetFusionWeightsAsync(tenantId, cancellationToken);
                            if (tenantWeights is null)
                            {
                                throw new ArgumentException("Tenant fusion weights are missing.");
                            }

                            tenantWeights.Validate();
                            weights = tenantWeights;
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex) when (IsSemanticConfigUnavailable(ex))
                        {
                            logger.LogWarning(
                                ex,
                                "Tenant fusion weights unavailable for tenant {TenantId}. Falling back to default fusion weights.",
                                tenantId);
                            weights = new FusionWeights();
                        }
                        catch (ArgumentException ex)
                        {
                            logger.LogWarning(
                                ex,
                                "Tenant fusion weights invalid for tenant {TenantId}. Falling back to default fusion weights.",
                                tenantId);
                            weights = new FusionWeights();
                        }
                    }

                    if (enabledAxes.Contains("semantic") || enabledAxes.Contains("nl"))
                    {
                        try
                        {
                            embeddingConfig = await embeddingConfigProvider.GetAsync(tenantId, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex) when (IsSemanticConfigUnavailable(ex))
                        {
                            semanticConfigFailure = ex;
                            if (enabledAxes.Contains("semantic"))
                            {
                                preUnavailableAxes.Add("semantic");
                            }

                            if (enabledAxes.Contains("nl"))
                            {
                                preUnavailableAxes.Add("nl");
                            }
                        }
                    }

                    bool hasHybridFallbackAxis = enabledAxes.Contains("syntactic")
                        || (enabledAxes.Contains("graph") && !string.IsNullOrWhiteSpace(effectiveGraphStartNodeId));

                    if (semanticConfigFailure is not null && !hasHybridFallbackAxis)
                    {
                        string unavailableAxis = enabledAxes.Contains("semantic") ? "semantic" : "nl";
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, unavailableAxis, tenantId, semanticConfigFailure));
                    }

                    int clampedDepth = Math.Clamp(depth, 0, 10);
                    HybridSearchResult hybridResult;
                    try
                    {
                        hybridResult = await hybridSearchService.SearchAsync(
                            hybridQuery,
                            embeddingConfig,
                            effectiveGraphStartNodeId,
                            clampedDepth,
                            weights,
                            enabledAxes,
                            preUnavailableAxes,
                            cancellationToken);
                    }
                    catch (SearchPaginationLimitExceededException ex)
                    {
                        return SearchError(
                            "PAGINATION_LIMIT_EXCEEDED",
                            Results.BadRequest(SearchEndpointErrorResponseFactory.CreatePaginationLimitExceeded(ex)));
                    }

                    if (hybridResult.AllEnabledAxesUnavailable == true)
                    {
                        return SearchError(
                            "ALL_BACKENDS_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildAllBackendsUnavailableResponse(
                                httpContext,
                                logger,
                                tenantId,
                                hybridResult.UnavailableAxes,
                                enabledAxes.ToArray()));
                    }

                    hybridResult = await EnrichHybridResultWithCaseAttributionAsync(hybridResult, caseService, tenantId, cancellationToken);
                    hybridResult = await EnrichHybridResultWithAnnotationCountsAsync(hybridResult, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
                    hybridResult = SearchResponseMetadataApplier.ApplyHybrid(
                        hybridResult,
                        tokenBudget,
                        enabledAxes,
                        embeddingConfig,
                        effectiveGraphStartNodeId);

                    if (explain)
                    {
                        IReadOnlySet<string> explanationAxes = DetermineHybridExplanationAxes(
                            enabledAxes,
                            hybridResult.UnavailableAxes,
                            embeddingConfig is not null,
                            !string.IsNullOrWhiteSpace(effectiveGraphStartNodeId));
                        hybridResult = hybridResult with { Explanation = ExplainMetadataBuilder.BuildForHybrid(explanationAxes, weights) };
                    }

                    CompleteSearchSuccess("hybrid", hybridResult.Results.Count);
                    if (hybridResult.Degraded && hybridResult.UnavailableAxes.Count > 0)
                    {
                        searchScope.MarkPartial("HYBRID_DEGRADED");
                    }

                    return Results.Ok(hybridResult);
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    return SearchError(
                        "INVALID_INPUT",
                        Results.BadRequest(new ErrorResponse(
                            "INVALID_INPUT",
                            "Parameter 'query' is required for syntactic, semantic, and nl search.",
                            "Provide query as a query parameter.")));
                }

                int clampedMax = Math.Clamp(maxResults, 1, 100);
                int clampedOff = Math.Max(offset, 0);
                var mainSearchQuery = new SearchQuery
                {
                    TenantId = tenantId,
                    Query = query,
                    CaseId = caseId,
                    SourceTypeFilter = sourceType,
                    MetadataQuery = metadataQuery,
                    CloudEventSubject = subject,
                    AttributeFilters = attributeFilters,
                    MaxResults = clampedMax,
                    Offset = clampedOff,
                };

                if (string.Equals(axis, "nl", StringComparison.OrdinalIgnoreCase))
                {
                    TenantEmbeddingConfig config;
                    try
                    {
                        config = await embeddingConfigProvider.GetAsync(tenantId, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (IsSemanticConfigUnavailable(ex))
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "nl", tenantId, ex));
                    }

                    SearchResult searchResult;
                    try
                    {
                        searchResult = await naturalLanguageService.SearchAsync(
                            mainSearchQuery, config, cancellationToken);
                    }
                    catch (SearchPaginationLimitExceededException ex)
                    {
                        return SearchError(
                            "PAGINATION_LIMIT_EXCEEDED",
                            Results.BadRequest(SearchEndpointErrorResponseFactory.CreatePaginationLimitExceeded(ex)));
                    }
                    catch (EmbeddingApiException ex)
                    {
                        return SearchError(
                            "EMBEDDING_UNAVAILABLE",
                            Results.Json(SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex), statusCode: 503));
                    }
                    catch (EmbeddingRateLimitException ex)
                    {
                        return SearchError(
                            "EMBEDDING_UNAVAILABLE",
                            Results.Json(SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex), statusCode: 503));
                    }
                    catch (SemanticSearchDimensionMismatchException ex)
                    {
                        return SearchError(
                            "DIMENSION_MISMATCH",
                            Results.Json(SearchEndpointErrorResponseFactory.CreateDimensionMismatch(ex), statusCode: 500));
                    }
                    catch (RedisConnectionException ex)
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "nl", tenantId, ex));
                    }
                    catch (RedisTimeoutException ex)
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "nl", tenantId, ex));
                    }
                    catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "nl", tenantId, ex));
                    }

                    searchResult = await EnrichResultWithCaseAttributionAsync(searchResult, caseService, tenantId, cancellationToken);
                    searchResult = await EnrichResultWithAnnotationCountsAsync(searchResult, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
                    if (explain)
                    {
                        searchResult = searchResult with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("nl") };
                    }

                    searchResult = SearchResponseMetadataApplier.ApplySearch(searchResult, "nl", tokenBudget);
                    CompleteSearchSuccess("nl", searchResult.Results.Count);
                    return Results.Ok(searchResult);
                }

                if (!string.IsNullOrWhiteSpace(startNodeId))
                {
                    int clampedDepth = Math.Clamp(depth, 0, 10);

                    if (string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase))
                    {
                        bool innerSearchStarted = false;

                        TenantEmbeddingConfig config;
                        try
                        {
                            config = await embeddingConfigProvider.GetAsync(tenantId, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex) when (IsSemanticConfigUnavailable(ex))
                        {
                            return SearchError(
                                "BACKEND_UNAVAILABLE",
                                SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex));
                        }

                        SearchResult result;

                        try
                        {
                            result = await graphScopedSearch.SearchAsync(
                                mainSearchQuery, startNodeId, clampedDepth,
                                innerSearch: null,
                                cancellationToken,
                                scopedInnerSearch: (q, graphScopeKeys) =>
                                {
                                    innerSearchStarted = true;
                                    return semanticService.SearchAsync(q, config, graphScopeKeys, cancellationToken);
                                },
                                graphScopeKeyBuilder: IndexSchemaDefinitions.BuildSemanticKey);
                        }
                        catch (SearchPaginationLimitExceededException ex)
                        {
                            return SearchError(
                                "PAGINATION_LIMIT_EXCEEDED",
                                Results.BadRequest(SearchEndpointErrorResponseFactory.CreatePaginationLimitExceeded(ex)));
                        }
                        catch (EmbeddingApiException ex)
                        {
                            return SearchError(
                                "EMBEDDING_UNAVAILABLE",
                                Results.Json(SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex), statusCode: 503));
                        }
                        catch (EmbeddingRateLimitException ex)
                        {
                            return SearchError(
                                "EMBEDDING_UNAVAILABLE",
                                Results.Json(SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex), statusCode: 503));
                        }
                        catch (SemanticSearchDimensionMismatchException ex)
                        {
                            return SearchError(
                                "DIMENSION_MISMATCH",
                                Results.Json(SearchEndpointErrorResponseFactory.CreateDimensionMismatch(ex), statusCode: 500));
                        }
                        catch (RedisConnectionException ex)
                        {
                            return SearchError(
                                innerSearchStarted ? "BACKEND_UNAVAILABLE" : "GRAPH_UNAVAILABLE",
                                SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                                    httpContext,
                                    logger,
                                    "semantic",
                                    tenantId,
                                    startNodeId,
                                    innerSearchStarted,
                                    ex));
                        }
                        catch (RedisTimeoutException ex)
                        {
                            return SearchError(
                                innerSearchStarted ? "BACKEND_UNAVAILABLE" : "GRAPH_UNAVAILABLE",
                                SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                                    httpContext,
                                    logger,
                                    "semantic",
                                    tenantId,
                                    startNodeId,
                                    innerSearchStarted,
                                    ex));
                        }
                        catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
                        {
                            return SearchError(
                                innerSearchStarted ? "BACKEND_UNAVAILABLE" : "GRAPH_UNAVAILABLE",
                                SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                                    httpContext,
                                    logger,
                                    "semantic",
                                    tenantId,
                                    startNodeId,
                                    innerSearchStarted,
                                    ex));
                        }
                        catch (TimeoutException)
                        {
                            return SearchError("GRAPH_TIMEOUT", SearchEndpointDegradationResponses.BuildGraphTimeoutResponse());
                        }

                        result = await EnrichResultWithCaseAttributionAsync(result, caseService, tenantId, cancellationToken);
                        result = await EnrichResultWithAnnotationCountsAsync(result, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
                        if (explain)
                        {
                            result = result with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("semantic") };
                        }

                        result = SearchResponseMetadataApplier.ApplySearch(result, "semantic", tokenBudget);
                        CompleteSearchSuccess("graph-scoped-semantic", result.Results.Count);
                        return Results.Ok(result);
                    }

                    bool innerSyntacticStarted = false;
                    SearchResult syntacticResult;
                    try
                    {
                        syntacticResult = await graphScopedSearch.SearchAsync(
                            mainSearchQuery, startNodeId, clampedDepth,
                            innerSearch: null,
                            cancellationToken,
                            scopedInnerSearch: (q, graphScopeKeys) =>
                            {
                                innerSyntacticStarted = true;
                                return syntacticService.SearchAsync(q, graphScopeKeys);
                            },
                            graphScopeKeyBuilder: IndexSchemaDefinitions.BuildSyntacticKey);
                    }
                    catch (SearchPaginationLimitExceededException ex)
                    {
                        return SearchError(
                            "PAGINATION_LIMIT_EXCEEDED",
                            Results.BadRequest(SearchEndpointErrorResponseFactory.CreatePaginationLimitExceeded(ex)));
                    }
                    catch (RedisConnectionException ex)
                    {
                        return SearchError(
                            innerSyntacticStarted ? "BACKEND_UNAVAILABLE" : "GRAPH_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                                httpContext,
                                logger,
                                "syntactic",
                                tenantId,
                                startNodeId,
                                innerSyntacticStarted,
                                ex));
                    }
                    catch (RedisTimeoutException ex)
                    {
                        return SearchError(
                            innerSyntacticStarted ? "BACKEND_UNAVAILABLE" : "GRAPH_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                                httpContext,
                                logger,
                                "syntactic",
                                tenantId,
                                startNodeId,
                                innerSyntacticStarted,
                                ex));
                    }
                    catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
                    {
                        return SearchError(
                            innerSyntacticStarted ? "BACKEND_UNAVAILABLE" : "GRAPH_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                                httpContext,
                                logger,
                                "syntactic",
                                tenantId,
                                startNodeId,
                                innerSyntacticStarted,
                                ex));
                    }
                    catch (TimeoutException)
                    {
                        return SearchError("GRAPH_TIMEOUT", SearchEndpointDegradationResponses.BuildGraphTimeoutResponse());
                    }

                    syntacticResult = await EnrichResultWithCaseAttributionAsync(syntacticResult, caseService, tenantId, cancellationToken);
                    syntacticResult = await EnrichResultWithAnnotationCountsAsync(syntacticResult, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
                    if (explain)
                    {
                        syntacticResult = syntacticResult with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("syntactic") };
                    }

                    syntacticResult = SearchResponseMetadataApplier.ApplySearch(syntacticResult, "syntactic", tokenBudget);
                    CompleteSearchSuccess("graph-scoped-syntactic", syntacticResult.Results.Count);
                    return Results.Ok(syntacticResult);
                }

                if (string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase))
                {
                    TenantEmbeddingConfig config;
                    try
                    {
                        config = await embeddingConfigProvider.GetAsync(tenantId, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (IsSemanticConfigUnavailable(ex))
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex));
                    }

                    SearchResult searchResult;
                    try
                    {
                        searchResult = await semanticService.SearchAsync(
                            mainSearchQuery, config, cancellationToken);
                    }
                    catch (SearchPaginationLimitExceededException ex)
                    {
                        return SearchError(
                            "PAGINATION_LIMIT_EXCEEDED",
                            Results.BadRequest(SearchEndpointErrorResponseFactory.CreatePaginationLimitExceeded(ex)));
                    }
                    catch (EmbeddingApiException ex)
                    {
                        return SearchError(
                            "EMBEDDING_UNAVAILABLE",
                            Results.Json(SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex), statusCode: 503));
                    }
                    catch (EmbeddingRateLimitException ex)
                    {
                        return SearchError(
                            "EMBEDDING_UNAVAILABLE",
                            Results.Json(SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex), statusCode: 503));
                    }
                    catch (SemanticSearchDimensionMismatchException ex)
                    {
                        return SearchError(
                            "DIMENSION_MISMATCH",
                            Results.Json(SearchEndpointErrorResponseFactory.CreateDimensionMismatch(ex), statusCode: 500));
                    }
                    catch (RedisConnectionException ex)
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex));
                    }
                    catch (RedisTimeoutException ex)
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex));
                    }
                    catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
                    {
                        return SearchError(
                            "BACKEND_UNAVAILABLE",
                            SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex));
                    }

                    searchResult = await EnrichResultWithCaseAttributionAsync(searchResult, caseService, tenantId, cancellationToken);
                    searchResult = await EnrichResultWithAnnotationCountsAsync(searchResult, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
                    if (explain)
                    {
                        searchResult = searchResult with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("semantic") };
                    }

                    searchResult = SearchResponseMetadataApplier.ApplySearch(searchResult, "semantic", tokenBudget);
                    CompleteSearchSuccess("semantic", searchResult.Results.Count);
                    return Results.Ok(searchResult);
                }

                SearchResult syntacticDefault;
                try
                {
                    syntacticDefault = await syntacticService.SearchAsync(mainSearchQuery);
                }
                catch (RedisConnectionException ex)
                {
                    return SearchError(
                        "BACKEND_UNAVAILABLE",
                        SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "syntactic", tenantId, ex));
                }
                catch (RedisTimeoutException ex)
                {
                    return SearchError(
                        "BACKEND_UNAVAILABLE",
                        SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "syntactic", tenantId, ex));
                }
                catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
                {
                    return SearchError(
                        "BACKEND_UNAVAILABLE",
                        SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "syntactic", tenantId, ex));
                }

                syntacticDefault = await EnrichResultWithCaseAttributionAsync(syntacticDefault, caseService, tenantId, cancellationToken);
                syntacticDefault = await EnrichResultWithAnnotationCountsAsync(syntacticDefault, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
                if (explain)
                {
                    syntacticDefault = syntacticDefault with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("syntactic") };
                }

                syntacticDefault = SearchResponseMetadataApplier.ApplySearch(syntacticDefault, "syntactic", tokenBudget);
                CompleteSearchSuccess("syntactic", syntacticDefault.Results.Count);
                return Results.Ok(syntacticDefault);
            }
            catch (Exception ex)
            {
                searchScope.MarkUnhandledException(ex);
                throw;
            }
        });

        return app;
    }

    private static bool IsSemanticConfigUnavailable(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        return ex is Dapr.DaprException
            or TimeoutException
            or System.Net.Http.HttpRequestException;
    }

    private static IReadOnlySet<string> DetermineHybridExplanationAxes(
        IReadOnlySet<string> requestedAxes,
        IReadOnlyCollection<string> unavailableAxes,
        bool hasSemanticConfiguration,
        bool hasGraphStartNode)
    {
        ArgumentNullException.ThrowIfNull(requestedAxes);
        ArgumentNullException.ThrowIfNull(unavailableAxes);

        HashSet<string> explanationAxes = new(requestedAxes, StringComparer.OrdinalIgnoreCase);

        if (!hasSemanticConfiguration)
        {
            _ = explanationAxes.Remove("semantic");
            _ = explanationAxes.Remove("nl");
        }

        if (!hasGraphStartNode)
        {
            _ = explanationAxes.Remove("graph");
        }

        foreach (string unavailableAxis in unavailableAxes)
        {
            _ = explanationAxes.Remove(unavailableAxis);
        }

        return explanationAxes;
    }

    private static FusionWeights? CreateQueryFusionWeights(
        double? syntacticWeight,
        double? semanticWeight,
        double? graphWeight,
        double? nlWeight)
    {
        if (syntacticWeight is null && semanticWeight is null && graphWeight is null && nlWeight is null)
        {
            return null;
        }

        FusionWeights defaults = new();
        return defaults with
        {
            SyntacticWeight = syntacticWeight ?? defaults.SyntacticWeight,
            SemanticWeight = semanticWeight ?? defaults.SemanticWeight,
            GraphWeight = graphWeight ?? defaults.GraphWeight,
            NlWeight = nlWeight ?? defaults.NlWeight,
        };
    }

    private static async Task<SearchResult> EnrichResultWithCaseAttributionAsync(
        SearchResult result,
        CaseService caseService,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> caseIds = result.Results
            .Where(r => r.CaseId is not null)
            .Select(r => r.CaseId!)
            .Distinct()
            .ToList();

        if (caseIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, string> caseNames = await caseService.ResolveNamesAsync(tenantId, caseIds, cancellationToken).ConfigureAwait(false);

        List<ScoredResult> enrichedResults = result.Results
            .Select(r => r.CaseId is not null && caseNames.TryGetValue(r.CaseId, out string? name)
                ? r with { CaseName = name }
                : r)
            .ToList();

        List<CaseGroupSummary> caseGroups = BuildCaseGroups(enrichedResults, caseNames);

        return result with
        {
            Results = enrichedResults,
            CaseGroups = caseGroups.Count > 0 ? caseGroups : null,
        };
    }

    private static async Task<HybridSearchResult> EnrichHybridResultWithCaseAttributionAsync(
        HybridSearchResult result,
        CaseService caseService,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> caseIds = result.Results
            .Where(r => r.CaseId is not null)
            .Select(r => r.CaseId!)
            .Distinct()
            .ToList();

        if (caseIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, string> caseNames = await caseService.ResolveNamesAsync(tenantId, caseIds, cancellationToken).ConfigureAwait(false);

        List<FusedScoredResult> enrichedResults = result.Results
            .Select(r => r.CaseId is not null && caseNames.TryGetValue(r.CaseId, out string? name)
                ? r with { CaseName = name }
                : r)
            .ToList();

        List<CaseGroupSummary> caseGroups = enrichedResults
            .Where(r => r.CaseId is not null)
            .GroupBy(r => r.CaseId!)
            .Select(g => new CaseGroupSummary(g.Key, caseNames.GetValueOrDefault(g.Key, g.Key), g.Count()))
            .OrderByDescending(c => c.ResultCount)
            .ToList();

        return result with
        {
            Results = enrichedResults,
            CaseGroups = caseGroups.Count > 0 ? caseGroups : null,
        };
    }

    private static async Task<SearchResult> EnrichResultWithAnnotationCountsAsync(
        SearchResult result,
        IGraphQueryBuilder graphQueryBuilder,
        IConnectionMultiplexer falkorDb,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> muIds = result.Results.Select(r => r.MemoryUnitId).Distinct().ToList();
        if (muIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, int> counts = await LoadAnnotationCountsAsync(
            graphQueryBuilder,
            falkorDb,
            tenantId,
            muIds,
            cancellationToken).ConfigureAwait(false);

        if (counts.Count == 0)
        {
            return result;
        }

        List<ScoredResult> enriched = result.Results
            .Select(r => counts.TryGetValue(r.MemoryUnitId, out int count) ? r with { AnnotationsCount = count } : r)
            .ToList();

        return result with { Results = enriched };
    }

    private static async Task<HybridSearchResult> EnrichHybridResultWithAnnotationCountsAsync(
        HybridSearchResult result,
        IGraphQueryBuilder graphQueryBuilder,
        IConnectionMultiplexer falkorDb,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> muIds = result.Results.Select(r => r.MemoryUnitId).Distinct().ToList();
        if (muIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, int> counts = await LoadAnnotationCountsAsync(
            graphQueryBuilder,
            falkorDb,
            tenantId,
            muIds,
            cancellationToken).ConfigureAwait(false);

        if (counts.Count == 0)
        {
            return result;
        }

        List<FusedScoredResult> enriched = result.Results
            .Select(r => counts.TryGetValue(r.MemoryUnitId, out int count) ? r with { AnnotationsCount = count } : r)
            .ToList();

        return result with { Results = enriched };
    }

    private static async Task<Dictionary<string, int>> LoadAnnotationCountsAsync(
        IGraphQueryBuilder graphQueryBuilder,
        IConnectionMultiplexer falkorDb,
        string tenantId,
        IReadOnlyList<string> memoryUnitIds,
        CancellationToken cancellationToken)
    {
        if (memoryUnitIds.Count == 0)
        {
            return [];
        }

        try
        {
            (string query, IDictionary<string, object> parameters) = graphQueryBuilder.BuildBatchCountAnnotations(memoryUnitIds);
            NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());
            NFalkorDB.ResultSet countResult = await falkor.SelectGraph(tenantId).QueryAsync(query, parameters)
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                .ConfigureAwait(false);

            Dictionary<string, int> counts = [];
            foreach (NFalkorDB.Record record in countResult)
            {
                if (!TryReadAnnotationCount(record, out string? memoryUnitId, out int count) ||
                    string.IsNullOrWhiteSpace(memoryUnitId) ||
                    count <= 0)
                {
                    continue;
                }

                counts[memoryUnitId] = count;
            }

            return counts;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return [];
        }
    }

    private static bool TryReadAnnotationCount(NFalkorDB.Record record, out string? memoryUnitId, out int count)
    {
        memoryUnitId = null;
        count = 0;

        try
        {
            memoryUnitId = record.GetValue<string>("muId");
            long parsedCount = record.GetValue<long>("count");
            count = checked((int)parsedCount);
            return !string.IsNullOrWhiteSpace(memoryUnitId);
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyDictionary<string, string>? ReadAttributeFilters(IQueryCollection query)
    {
        const string Prefix = "attribute.";

        Dictionary<string, string> filters = new(StringComparer.Ordinal);
        foreach (var pair in query)
        {
            if (!pair.Key.StartsWith(Prefix, StringComparison.Ordinal)
                || pair.Key.Length == Prefix.Length)
            {
                continue;
            }

            string attributeName = pair.Key[Prefix.Length..].Trim();
            string attributeValue = pair.Value.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(attributeName) && !string.IsNullOrWhiteSpace(attributeValue))
            {
                filters[attributeName] = attributeValue;
            }
        }

        return filters.Count == 0 ? null : filters;
    }

    private static List<CaseGroupSummary> BuildCaseGroups(
        IReadOnlyList<ScoredResult> results, Dictionary<string, string> caseNames)
    {
        return results
            .Where(r => r.CaseId is not null)
            .GroupBy(r => r.CaseId!)
            .Select(g => new CaseGroupSummary(g.Key, caseNames.GetValueOrDefault(g.Key, g.Key), g.Count()))
            .OrderByDescending(c => c.ResultCount)
            .ToList();
    }

    // Story 7.5 Rev 1.3 (Task 11.1/11.2): partial Program sentinel enables
    // WebApplicationFactory<Program> to reference the top-level-statement program
}
