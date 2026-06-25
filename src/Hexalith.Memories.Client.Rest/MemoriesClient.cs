// <copyright file="MemoriesClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Typed REST client for the Hexalith.Memories Server. Concrete class (no interface) per Architecture D9 —
/// mocking happens at the <see cref="HttpClient"/> / <see cref="IHttpClientFactory"/> boundary.
/// </summary>
public class MemoriesClient
{
    private static readonly TimeSpan HealthProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly HttpClient _httpClient;
    private readonly ILogger<MemoriesClient> _logger;

    /// <summary>Initializes a new instance of the <see cref="MemoriesClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client (supplied by <see cref="IHttpClientFactory"/>).</param>
    /// <param name="options">The client options.</param>
    /// <param name="logger">The logger.</param>
    public MemoriesClient(HttpClient httpClient, IOptions<MemoriesClientOptions> options, ILogger<MemoriesClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;

        MemoriesClientOptions value = options.Value;
        if (_httpClient.BaseAddress is null && value.Endpoint is not null)
        {
            _httpClient.BaseAddress = value.Endpoint;
        }
    }

    /// <summary>Gets the base address the client is configured against.</summary>
    public Uri? BaseAddress => _httpClient.BaseAddress;

    /// <summary>Lists all tenants on the server.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of tenants.</returns>
    public virtual async Task<IReadOnlyList<TenantSummary>> ListTenantsAsync(CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/tenants", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            IReadOnlyList<TenantSummary>? tenants = await response.Content
                .ReadFromJsonAsync<IReadOnlyList<TenantSummary>>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return tenants ?? [];
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with a body that could not be parsed as TenantSummary[].",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
                jsonException);
        }
    }

    /// <summary>Lists the cases for a given tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of cases.</returns>
    public virtual async Task<IReadOnlyList<Case>> ListCasesAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/cases";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        IReadOnlyList<Case>? cases = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<Case>>(MemoriesJsonContext.Options, ct)
            .ConfigureAwait(false);
        return cases ?? [];
    }

    /// <summary>Runs a hybrid (multi-axis) search. Story 7.2 addition.</summary>
    /// <param name="request">The hybrid search request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The fused hybrid search result.</returns>
    public virtual async Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        string path = BuildSearchPath(
            axis: "hybrid",
            tenantId: request.TenantId,
            query: request.Query,
            caseId: request.CaseId,
            sourceType: null,
            metadataQuery: null,
            subject: null,
            maxResults: request.MaxResults,
            offset: 0,
            explain: request.Explain,
            tokenBudget: request.TokenBudget,
            attributeFilters: null);

        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            HybridSearchResult? result = await response.Content
                .ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return result ?? throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with an empty body.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with a body that could not be parsed as HybridSearchResult.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
                jsonException);
        }
    }

    /// <summary>Runs a single-axis search (syntactic, semantic, or graph). Story 7.2 addition.</summary>
    /// <param name="request">The single-axis search request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The single-axis search result.</returns>
    public virtual async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Axis);

        string path = BuildSearchPath(
            axis: request.Axis,
            tenantId: request.TenantId,
            query: request.Query,
            caseId: request.CaseId,
            sourceType: request.SourceType,
            metadataQuery: request.MetadataQuery,
            subject: request.Subject,
            maxResults: request.MaxResults,
            offset: request.Offset,
            explain: request.Explain,
            tokenBudget: request.TokenBudget,
            attributeFilters: request.AttributeFilters);

        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            SearchResult? result = await response.Content
                .ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return result ?? throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with an empty body.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with a body that could not be parsed as SearchResult.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
                jsonException);
        }
    }

    /// <summary>Fetches a single memory unit for <c>search inspect</c>. Story 7.2 addition.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The memory unit with its metadata.</returns>
    public virtual async Task<MemoryUnit> GetMemoryUnitAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}/memory-units/{Uri.EscapeDataString(memoryUnitId)}";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            MemoryUnit? unit = await response.Content
                .ReadFromJsonAsync<MemoryUnit>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return unit ?? throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with an empty body.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with a body that could not be parsed as MemoryUnit.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
                jsonException);
        }
    }

    /// <summary>
    /// Story 18.5 — resolves the canonical <c>MemoryUnitId</c> for a known source URI by exact key via
    /// <c>GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri</c>. This is a deterministic
    /// keyed lookup, NOT a search — the Parties caller uses it so graph mode no longer degrades when the
    /// canonical match falls outside a free-text search's top hits.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="sourceUri">The exact source URI to resolve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The canonical memory-unit id, or <see langword="null"/> when the server returns a structured 404.</returns>
    /// <exception cref="MemoriesRemoteException">Thrown for any non-2xx status other than <c>404</c> (e.g. a 503 backend error — never silently a miss).</exception>
    public virtual async Task<string?> LookupMemoryUnitIdBySourceUriAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUri);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}/memory-units/by-source-uri?sourceUri={Uri.EscapeDataString(sourceUri)}";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);

        // A structured 404 is the deterministic "no committed unit for this URI" signal — surface it as null
        // rather than an exception so callers can branch cheaply. Any other non-2xx (e.g. 503 backend error)
        // is a real failure and must NOT be flattened into a miss.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            MemoryUnitIdLookupResponse? result = await response.Content
                .ReadFromJsonAsync<MemoryUnitIdLookupResponse>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return result?.MemoryUnitId ?? throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with an empty body.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with a body that could not be parsed as MemoryUnitIdLookupResponse.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
                jsonException);
        }
    }

    /// <summary>
    /// Schedules a tenant-provisioning workflow via <c>POST /api/tenants</c>. Fire-and-forget semantics — the
    /// server returns <c>202 Accepted</c> with the workflow instance id before the tenant is fully active.
    /// Callers observe completion via <see cref="GetTenantAsync(string, CancellationToken)"/> — polling for
    /// <see cref="TenantStatus.Active"/> — or by calling the server's
    /// <c>GET /api/tenants/{tenantId}/provision-status/{instanceId}</c> endpoint directly.
    /// </summary>
    /// <param name="tenantId">The desired tenant identifier.</param>
    /// <param name="displayName">The tenant display name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The provisioning workflow instance id.</returns>
    /// <remarks>
    /// EXPERIMENTAL (HXL001 — Story 7.4): Added to unblock the <c>memories quickstart</c> wizard. Signature
    /// may change when the <c>memories tenant create</c> CLI subcommand is wired in Phase 1.5. Suppress with
    /// <c>#pragma warning disable HXL001</c> at opt-in call sites.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental("HXL001")]
    public virtual async Task<string> CreateTenantAsync(string tenantId, string displayName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var input = new TenantProvisioningInput(tenantId, displayName);
        using HttpResponseMessage response = await _httpClient
            .PostAsJsonAsync("api/tenants", input, MemoriesJsonContext.Options, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        return await ReadInstanceIdAsync(response, "workflowInstanceId", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches a tenant by id. Returns <see langword="null"/> when the server responds with
    /// <c>TENANT_NOT_FOUND</c>; throws for any other failure.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tenant info, or <see langword="null"/> when the tenant does not exist.</returns>
    public virtual async Task<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            return await response.Content
                .ReadFromJsonAsync<TenantInfo>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with a body that could not be parsed as TenantInfo.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
                jsonException);
        }
    }

    /// <summary>
    /// Creates a case within a tenant via <c>POST /api/tenants/{tenantId}/cases</c>.
    /// </summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="name">The case display name.</param>
    /// <param name="description">Optional case description.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created case.</returns>
    /// <remarks>
    /// EXPERIMENTAL (HXL001 — Story 7.4): Added to unblock the <c>memories quickstart</c> wizard. Signature
    /// may change when the <c>memories case create</c> CLI subcommand is wired in Phase 1.5.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental("HXL001")]
    public virtual async Task<Case> CreateCaseAsync(string tenantId, string name, string? description, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var input = new CreateCaseInput(tenantId, name, description);
        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/cases";

        using HttpResponseMessage response = await _httpClient
            .PostAsJsonAsync(path, input, MemoriesJsonContext.Options, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            Case? created = await response.Content
                .ReadFromJsonAsync<Case>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return created ?? throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with an empty body.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw new MemoriesRemoteException(
                response.StatusCode,
                new ErrorResponse(
                    Code: "INVALID_RESPONSE",
                    Message: "Server returned a 2xx response with a body that could not be parsed as Case.",
                    Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
                jsonException);
        }
    }

    /// <summary>
    /// Submits a file ingestion via <c>POST /api/ingest</c>. Returns the workflow instance id; the ingestion
    /// runs asynchronously on the server.
    /// </summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="caseId">The case id.</param>
    /// <param name="sourceUri">The logical source URI recorded with the memory unit (callers supply their own scheme — e.g. <c>quickstart://</c>, <c>file://</c>, or a content-addressed URI).</param>
    /// <param name="content">The raw content bytes to ingest.</param>
    /// <param name="contentType">The MIME content-type of <paramref name="content"/>.</param>
    /// <param name="ingestedBy">Identifier of the submitter (user or system).</param>
    /// <param name="metadata">Optional metadata fields (each entry carries its own <see cref="MetadataOrigin"/> and confidence) to attach to the memory unit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow instance id.</returns>
    /// <remarks>
    /// Stable since Story 18.4 — graduated out of <c>HXL001</c>; callers no longer need
    /// <c>#pragma warning disable HXL001</c>. To supply an explicit idempotency token (so two
    /// near-simultaneous ingests of the same source resolve to one memory unit) use the
    /// <see cref="IngestAsync(string, string, string, byte[], string, string, IReadOnlyDictionary{string, MetadataField}, string, CancellationToken)"/>
    /// overload.
    /// </remarks>
    public virtual Task<string> IngestAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        byte[] content,
        string contentType,
        string ingestedBy,
        IReadOnlyDictionary<string, MetadataField>? metadata,
        CancellationToken ct)
        => IngestCoreAsync(tenantId, caseId, sourceUri, content, contentType, ingestedBy, metadata, idempotencyToken: null, ct);

    /// <summary>
    /// Submits a file ingestion via <c>POST /api/ingest</c> with an explicit idempotency token. Returns the
    /// workflow instance id; the ingestion runs asynchronously on the server. Story 18.4 additive overload.
    /// </summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="caseId">The case id.</param>
    /// <param name="sourceUri">The logical source URI recorded with the memory unit (callers supply their own scheme — e.g. <c>quickstart://</c>, <c>file://</c>, or a content-addressed URI).</param>
    /// <param name="content">The raw content bytes to ingest.</param>
    /// <param name="contentType">The MIME content-type of <paramref name="content"/>.</param>
    /// <param name="ingestedBy">Identifier of the submitter (user or system).</param>
    /// <param name="metadata">Optional metadata fields (each entry carries its own <see cref="MetadataOrigin"/> and confidence) to attach to the memory unit.</param>
    /// <param name="idempotencyToken">
    /// Optional explicit idempotency token. When non-blank it takes precedence over <paramref name="sourceUri"/>
    /// as the dedup identity, so concurrent ingests carrying the same token resolve to a single memory unit and
    /// the loser observes the winner's <c>MemoryUnitId</c>. When <see langword="null"/>/blank, dedup falls back
    /// to the <paramref name="sourceUri"/> natural key.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow instance id.</returns>
    public virtual Task<string> IngestAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        byte[] content,
        string contentType,
        string ingestedBy,
        IReadOnlyDictionary<string, MetadataField>? metadata,
        string? idempotencyToken,
        CancellationToken ct)
        => IngestCoreAsync(tenantId, caseId, sourceUri, content, contentType, ingestedBy, metadata, idempotencyToken, ct);

    private async Task<string> IngestCoreAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        byte[] content,
        string contentType,
        string ingestedBy,
        IReadOnlyDictionary<string, MetadataField>? metadata,
        string? idempotencyToken,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUri);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(ingestedBy);

        var metadataMap = new Dictionary<string, MetadataField>(StringComparer.Ordinal);
        if (metadata is not null)
        {
            foreach (KeyValuePair<string, MetadataField> pair in metadata)
            {
                metadataMap[pair.Key] = pair.Value;
            }
        }

        var input = new IngestionInput
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = content,
            ContentType = contentType,
            SourceType = SourceType.File,
            IngestedBy = ingestedBy,
            Metadata = metadataMap,
            IdempotencyToken = string.IsNullOrWhiteSpace(idempotencyToken) ? null : idempotencyToken,
        };

        using HttpResponseMessage response = await _httpClient
            .PostAsJsonAsync("api/ingest", input, MemoriesJsonContext.Options, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        return await ReadInstanceIdAsync(response, "instanceId", ct).ConfigureAwait(false);
    }

    private static async Task<string> ReadInstanceIdAsync(HttpResponseMessage response, string propertyName, CancellationToken ct)
    {
        string payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw CreateInvalidResponseException(response.StatusCode, "Server returned a 2xx response with an empty body.");
        }

        try
        {
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                throw CreateInvalidResponseException(response.StatusCode, "Server returned a 2xx response with a body that was not a JSON object.");
            }

            if (document.RootElement.TryGetProperty(propertyName, out System.Text.Json.JsonElement id)
                && id.ValueKind == System.Text.Json.JsonValueKind.String
                && !string.IsNullOrWhiteSpace(id.GetString()))
            {
                return id.GetString()!;
            }

            throw CreateInvalidResponseException(
                response.StatusCode,
                $"Server returned a 2xx response missing required property '{propertyName}'.");
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as a workflow response.",
                jsonException);
        }
    }

    private static MemoriesRemoteException CreateInvalidResponseException(
        HttpStatusCode statusCode,
        string message,
        Exception? innerException = null)
        => new(
            statusCode,
            new ErrorResponse(
                Code: "INVALID_RESPONSE",
                Message: message,
                Suggestion: "Check that the server version matches the client's Contracts.V1 version."),
            innerException);

    /// <summary>
    /// Probes the <c>/health</c> endpoint with a short 5-second timeout. Returns <see langword="true"/> iff the
    /// server answered with a 2xx status code.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> if the server is reachable and healthy.</returns>
    public virtual async Task<bool> ProbeHealthAsync(CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(HealthProbeTimeout);

        try
        {
            using HttpResponseMessage response = await _httpClient
                .GetAsync("health", linked.Token)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogDebug("Health probe timed out after {Seconds}s.", HealthProbeTimeout.TotalSeconds);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Health probe failed.");
            return false;
        }
    }

    /// <summary>
    /// Server defaults for parameters the CLI omits when unchanged. Keeping this private stops the
    /// values from drifting into callers that might pin them as wire literals.
    /// </summary>
    private const int DefaultServerMaxResults = 10;

    private static string BuildSearchPath(
        string axis,
        string tenantId,
        string? query,
        string? caseId,
        string? sourceType,
        string? metadataQuery,
        string? subject,
        int maxResults,
        int offset,
        bool explain,
        int? tokenBudget,
        IReadOnlyDictionary<string, string>? attributeFilters)
    {
        var builder = new StringBuilder("api/search?tenantId=");
        builder.Append(Uri.EscapeDataString(tenantId));

        if (!string.IsNullOrEmpty(query))
        {
            builder.Append("&query=").Append(Uri.EscapeDataString(query));
        }

        if (!string.IsNullOrEmpty(caseId))
        {
            builder.Append("&caseId=").Append(Uri.EscapeDataString(caseId));
        }

        if (!string.IsNullOrEmpty(sourceType))
        {
            builder.Append("&sourceType=").Append(Uri.EscapeDataString(sourceType));
        }

        if (!string.IsNullOrEmpty(metadataQuery))
        {
            builder.Append("&metadataQuery=").Append(Uri.EscapeDataString(metadataQuery));
        }

        if (!string.IsNullOrEmpty(subject))
        {
            builder.Append("&subject=").Append(Uri.EscapeDataString(subject));
        }

        builder.Append("&axis=").Append(Uri.EscapeDataString(axis));

        if (maxResults != DefaultServerMaxResults)
        {
            builder.Append("&maxResults=").Append(maxResults.ToString(CultureInfo.InvariantCulture));
        }

        if (offset > 0)
        {
            builder.Append("&offset=").Append(offset.ToString(CultureInfo.InvariantCulture));
        }

        if (explain)
        {
            builder.Append("&explain=true");
        }

        if (tokenBudget is not null)
        {
            builder.Append("&tokenBudget=").Append(tokenBudget.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (attributeFilters is { Count: > 0 })
        {
            foreach ((string key, string value) in attributeFilters.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                builder
                    .Append("&attribute.")
                    .Append(Uri.EscapeDataString(key.Trim()))
                    .Append('=')
                    .Append(Uri.EscapeDataString(value.Trim()));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Story 7.5 — fetches the per-tenant telemetry summary from
    /// <c>GET /api/tenants/{tenantId}/telemetry/summary</c>.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The telemetry summary.</returns>
    /// <remarks>
    /// EXPERIMENTAL (HXL001 — Story 7.5): signature may change in Phase 1.5 when the telemetry surface
    /// stabilizes (percentile fields, additional metric axes). Suppress with
    /// <c>#pragma warning disable HXL001</c> at opt-in call sites.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental("HXL001")]
    public virtual async Task<TelemetrySummary> GetTelemetrySummaryAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/telemetry/summary";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            TelemetrySummary? summary = await response.Content
                .ReadFromJsonAsync<TelemetrySummary>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return summary ?? throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with an empty telemetry summary body.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException
            or System.IO.IOException
            or HttpRequestException
            or NotSupportedException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as TelemetrySummary.",
                ex);
        }
    }

    /// <summary>
    /// Story 9.3 — enumerate registered event handlers via <c>GET /api/handlers</c>. Experimental
    /// HXL002 surface. Suppress with <c>#pragma warning disable HXL002</c> at opt-in call sites.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current handler-registry snapshot.</returns>
    [System.Diagnostics.CodeAnalysis.Experimental("HXL002")]
    public virtual async Task<HandlerRegistrationSnapshot> ListHandlersAsync(CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/handlers", ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            HandlerRegistrationSnapshot? snapshot = await response.Content
                .ReadFromJsonAsync<HandlerRegistrationSnapshot>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return snapshot ?? throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with an empty handler snapshot body.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException
            or System.IO.IOException
            or HttpRequestException
            or NotSupportedException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as HandlerRegistrationSnapshot.",
                ex);
        }
    }

    /// <summary>
    /// Story 9.3 — detect handler mismatches for a tenant via
    /// <c>GET /api/tenants/{tenantId}/handlers/mismatches</c>. Experimental HXL002 surface.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mismatch report.</returns>
    [System.Diagnostics.CodeAnalysis.Experimental("HXL002")]
    public virtual async Task<HandlerMismatchReport> GetHandlerMismatchesAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/handlers/mismatches";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            HandlerMismatchReport? report = await response.Content
                .ReadFromJsonAsync<HandlerMismatchReport>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return report ?? throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with an empty handler mismatch report body.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException
            or System.IO.IOException
            or HttpRequestException
            or NotSupportedException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as HandlerMismatchReport.",
                ex);
        }
    }

    /// <summary>
    /// Story 10.1 — fetches a graph traversal from the starting memory unit via
    /// <c>GET /api/tenants/{tenantId}/traverse</c>.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="startNodeId">The memory unit id to start traversal from.</param>
    /// <param name="depth">Maximum traversal depth; the server clamps this to <c>[0, 10]</c>.</param>
    /// <param name="caseId">Optional case identifier scoping the traversal.</param>
    /// <param name="edgeTypes">Optional edge type filter; <see langword="null"/> uses the server defaults.</param>
    /// <param name="tokenBudget">Optional maximum output tokens; null means no server-side budget truncation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The traversal result with ordered nodes, edges, and gap markers.</returns>
    /// <remarks>Stable since Story 10.2.</remarks>
    public virtual async Task<TraversalResult> TraverseAsync(
        string tenantId,
        string startNodeId,
        int depth = 2,
        string? caseId = null,
        IReadOnlyList<EdgeType>? edgeTypes = null,
        CancellationToken ct = default,
        int? tokenBudget = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId);

        var builder = new StringBuilder("api/tenants/");
        builder.Append(Uri.EscapeDataString(tenantId));
        builder.Append("/traverse?startNodeId=");
        builder.Append(Uri.EscapeDataString(startNodeId));
        builder.Append("&depth=");
        builder.Append(depth.ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            builder.Append("&caseId=");
            builder.Append(Uri.EscapeDataString(caseId));
        }

        if (edgeTypes is { Count: > 0 })
        {
            builder.Append("&edgeTypes=");
            string joined = string.Join(
                ',',
                edgeTypes.Select(static et => CamelCase(et.ToString())));
            builder.Append(Uri.EscapeDataString(joined));
        }

        if (tokenBudget is not null)
        {
            builder.Append("&tokenBudget=");
            builder.Append(tokenBudget.Value.ToString(CultureInfo.InvariantCulture));
        }

        using HttpResponseMessage response = await _httpClient
            .GetAsync(builder.ToString(), ct)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            TraversalResult? result = await response.Content
                .ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return result ?? throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with an empty traversal body.");
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as TraversalResult.",
                jsonException);
        }
    }

    /// <summary>
    /// Story 10.1 — fetches case summary for the MCP <c>get_case_info</c> tool via
    /// <c>GET /api/tenants/{tenantId}/cases/{caseId}</c>.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The case summary.</returns>
    /// <remarks>Stable since Story 10.2.</remarks>
    public virtual async Task<Case> GetCaseAsync(string tenantId, string caseId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            Case? caseResult = await response.Content
                .ReadFromJsonAsync<Case>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return caseResult ?? throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with an empty case body.");
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as Case.",
                jsonException);
        }
    }

    private static string CamelCase(string value)
        => value.Length == 0
            ? value
            : string.Create(value.Length, value, static (span, source) =>
            {
                source.AsSpan().CopyTo(span);
                span[0] = char.ToLowerInvariant(span[0]);
            });

    /// <summary>
    /// Story 8.2 — schedules a consistency verification workflow for the tenant. Fire-and-forget;
    /// poll status via <see cref="GetConsistencyVerificationStatusAsync"/>.
    /// </summary>
    /// <param name="tenantId">The tenant to audit.</param>
    /// <param name="request">Optional batch-size override; omit to use server default (500).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow-status URI returned by the server's <c>Location</c> header.</returns>
    public virtual async Task<Uri> StartConsistencyVerificationAsync(
        string tenantId,
        ConsistencyVerificationRequest? request,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/consistency/verify";
        ConsistencyVerificationRequest body = request ?? new ConsistencyVerificationRequest(tenantId);

        using HttpResponseMessage response = await _httpClient
            .PostAsJsonAsync(path, body, MemoriesJsonContext.Options, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        return await ReadWorkflowStatusUriAsync(
            response,
            propertyName: "workflowInstanceId",
            relativePath: $"api/tenants/{Uri.EscapeDataString(tenantId)}/consistency/verify",
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Story 8.2 — fetches the current state of a consistency-verification workflow.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="instanceId">The workflow instance id returned by <see cref="StartConsistencyVerificationAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow state, or <see langword="null"/> if the instance no longer exists.</returns>
    public virtual async Task<ConsistencyVerificationStatus?> GetConsistencyVerificationStatusAsync(
        string tenantId,
        string instanceId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/consistency/verify/{Uri.EscapeDataString(instanceId)}";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            return await response.Content
                .ReadFromJsonAsync<ConsistencyVerificationStatus>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as ConsistencyVerificationStatus.",
                jsonException);
        }
    }

    /// <summary>
    /// Story 8.2 — synchronous per-unit consistency probe. Returns the full inspection result
    /// when the unit is present in at least one backend.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier (must match the ULID pattern).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The inspection result.</returns>
    /// <exception cref="MemoriesRemoteException">
    /// Thrown with a 400 <c>INVALID_MEMORY_UNIT_ID</c> envelope on malformed IDs or
    /// 404 <c>MEMORY_UNIT_NOT_FOUND</c> when the unit is absent from all three backends.
    /// </exception>
    public virtual async Task<ConsistencyInspectionResult> InspectConsistencyAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/consistency/inspect/{Uri.EscapeDataString(memoryUnitId)}";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            ConsistencyInspectionResult? result = await response.Content
                .ReadFromJsonAsync<ConsistencyInspectionResult>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
            return result ?? throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with an empty inspection body.");
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as ConsistencyInspectionResult.",
                jsonException);
        }
    }

    /// <summary>
    /// Story 8.2 — schedules a consistency repair workflow. Fire-and-forget;
    /// poll status via <see cref="GetConsistencyRepairStatusAsync"/>.
    /// </summary>
    /// <param name="tenantId">The tenant to repair.</param>
    /// <param name="request">Optional repair request (batch size, include-unrepairable).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow-status URI returned by the server's <c>Location</c> header.</returns>
    public virtual async Task<Uri> StartConsistencyRepairAsync(
        string tenantId,
        ConsistencyRepairRequest? request,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/consistency/repair";
        ConsistencyRepairRequest body = request ?? new ConsistencyRepairRequest(tenantId);

        using HttpResponseMessage response = await _httpClient
            .PostAsJsonAsync(path, body, MemoriesJsonContext.Options, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        return await ReadWorkflowStatusUriAsync(
            response,
            propertyName: "workflowInstanceId",
            relativePath: $"api/tenants/{Uri.EscapeDataString(tenantId)}/consistency/repair",
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Story 8.2 — fetches the current state of a consistency-repair workflow.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="instanceId">The workflow instance id returned by <see cref="StartConsistencyRepairAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow state, or <see langword="null"/> if the instance no longer exists.</returns>
    public virtual async Task<ConsistencyRepairStatus?> GetConsistencyRepairStatusAsync(
        string tenantId,
        string instanceId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/consistency/repair/{Uri.EscapeDataString(instanceId)}";
        using HttpResponseMessage response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }

        try
        {
            return await response.Content
                .ReadFromJsonAsync<ConsistencyRepairStatus>(MemoriesJsonContext.Options, ct)
                .ConfigureAwait(false);
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            throw CreateInvalidResponseException(
                response.StatusCode,
                "Server returned a 2xx response with a body that could not be parsed as ConsistencyRepairStatus.",
                jsonException);
        }
    }

    /// <summary>
    /// Story 8.3: streams a case export as raw JSON. The returned <see cref="Stream"/> wraps the
    /// response body — the caller MUST dispose it so the underlying <see cref="HttpResponseMessage"/>
    /// and network buffers are released. The request uses
    /// <see cref="HttpCompletionOption.ResponseHeadersRead"/> so the stream is available before the
    /// full body is downloaded.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier to export.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only stream of export JSON bytes.</returns>
    public virtual async Task<Stream> ExportCaseAsync(string tenantId, string caseId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}/export";
        HttpRequestMessage request = new(HttpMethod.Get, path);
        HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
                throw new MemoriesRemoteException(response.StatusCode, error);
            }
            finally
            {
                response.Dispose();
            }
        }

        return await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Story 8.3: streams a tenant export as raw JSON. See <see cref="ExportCaseAsync"/> for
    /// stream-ownership semantics.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only stream of export JSON bytes.</returns>
    public virtual async Task<Stream> ExportTenantAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string path = $"api/tenants/{Uri.EscapeDataString(tenantId)}/export";
        HttpRequestMessage request = new(HttpMethod.Get, path);
        HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
                throw new MemoriesRemoteException(response.StatusCode, error);
            }
            finally
            {
                response.Dispose();
            }
        }

        return await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
    }

    private async Task<Uri> ReadWorkflowStatusUriAsync(
        HttpResponseMessage response,
        string propertyName,
        string relativePath,
        CancellationToken ct)
    {
        if (response.Headers.Location is { } location)
        {
            return location.IsAbsoluteUri || _httpClient.BaseAddress is null
                ? location
                : new Uri(_httpClient.BaseAddress, location);
        }

        string instanceId = await ReadInstanceIdAsync(response, propertyName, ct).ConfigureAwait(false);
        string normalizedPath = relativePath.TrimEnd('/');
        string statusPath = $"{normalizedPath}/{Uri.EscapeDataString(instanceId)}";

        return _httpClient.BaseAddress is { } baseAddress
            ? new Uri(baseAddress, statusPath)
            : new Uri(statusPath, UriKind.Relative);
    }
}
