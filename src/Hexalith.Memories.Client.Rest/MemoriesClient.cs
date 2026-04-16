// <copyright file="MemoriesClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using System.Net;
using System.Net.Http.Json;

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
}
