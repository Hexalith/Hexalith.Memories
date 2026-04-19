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
            maxResults: request.MaxResults,
            explain: request.Explain);

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
            maxResults: request.MaxResults,
            explain: request.Explain);

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
    /// EXPERIMENTAL (HXL001 — Story 7.4): Added to unblock the <c>memories quickstart</c> wizard. Signature
    /// may change when the <c>memories ingest</c> CLI subcommand is wired in Phase 1.5. Suppress with
    /// <c>#pragma warning disable HXL001</c> at opt-in call sites.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental("HXL001")]
    public virtual async Task<string> IngestAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        byte[] content,
        string contentType,
        string ingestedBy,
        IReadOnlyDictionary<string, MetadataField>? metadata,
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
        int maxResults,
        bool explain)
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

        builder.Append("&axis=").Append(Uri.EscapeDataString(axis));

        if (maxResults != DefaultServerMaxResults)
        {
            builder.Append("&maxResults=").Append(maxResults.ToString(CultureInfo.InvariantCulture));
        }

        if (explain)
        {
            builder.Append("&explain=true");
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
}
