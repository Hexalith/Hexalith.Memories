// <copyright file="EmbeddingProviderTransport.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Net;
using System.Net.Http.Headers;

using Hexalith.Memories.Contracts.V1;

/// <summary>Shared HTTP transport, auth-retry, rate-limit, and redaction pipeline used by every embedding provider
/// strategy. Providers build requests and parse responses; this type owns the send/read wrapping, the single 401/403
/// credential-refresh retry, 429 rate-limit mapping, Retry-After parsing, and error-body redaction so none of that is
/// duplicated per provider (Story 23.9, AC4).</summary>
internal sealed class EmbeddingProviderTransport
{
    private const string HttpClientName = "EmbeddingClient";

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingProviderTransport"/> class.</summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating the named embedding HTTP client.</param>
    public EmbeddingProviderTransport(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>Executes a single or batch embedding exchange for the supplied provider: authenticate, send, retry once on
    /// 401/403 with refreshed credentials, map 429 to <see cref="EmbeddingRateLimitException"/>, redact error bodies, and
    /// parse the successful response into ordered vectors.</summary>
    /// <param name="provider">The provider strategy.</param>
    /// <param name="texts">The ordered input texts.</param>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="batch">Whether the provider batch request/response shape is used.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The ordered embedding vectors.</returns>
    public async Task<IReadOnlyList<float[]>> ExecuteAsync(
        IEmbeddingProvider provider,
        IReadOnlyList<string> texts,
        string tenantId,
        TenantEmbeddingConfig config,
        bool batch,
        CancellationToken ct)
    {
        List<string?> sensitiveValues = [];
        EmbeddingProviderCredentials credentials = await provider
            .AuthenticateAsync(tenantId, config, ct)
            .ConfigureAwait(false);
        sensitiveValues.AddRange(credentials.SensitiveValues);

        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response = await SendAsync(httpClient, provider, texts, config, credentials, batch, tenantId, ct).ConfigureAwait(false);
        try
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                response.Dispose();
                credentials = await provider.RefreshCredentialsAsync(tenantId, config, ct).ConfigureAwait(false);
                sensitiveValues.AddRange(credentials.SensitiveValues);
                response = await SendAsync(httpClient, provider, texts, config, credentials, batch, tenantId, ct).ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                int retryAfter = ParseRetryAfterSeconds(response.Headers.RetryAfter);
                throw new EmbeddingRateLimitException(tenantId) { RetryAfterSeconds = retryAfter };
            }

            string responseBody = await ReadResponseBodyAsync(response, provider.DisplayName, tenantId, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new EmbeddingApiException(
                    (int)response.StatusCode,
                    EmbeddingResponseSanitizer.Redact(responseBody, sensitiveValues, texts),
                    tenantId);
            }

            return provider.ParseResponse(responseBody, texts.Count, config.Dimensions, tenantId, batch);
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>Parses the <c>Retry-After</c> header per RFC 9110 §10.2.3 — either a delta-seconds value or an HTTP-date.
    /// Returns <c>0</c> when the header is absent, malformed, or points at a past date, so the caller can fall back to its
    /// own default. Positive results are clamped to <c>[1, 3600]</c>.</summary>
    /// <param name="header">The parsed Retry-After header value, or <c>null</c>.</param>
    /// <returns>The retry delay in seconds.</returns>
    internal static int ParseRetryAfterSeconds(RetryConditionHeaderValue? header)
    {
        if (header is null)
        {
            return 0;
        }

        if (header.Delta.HasValue)
        {
            double seconds = header.Delta.Value.TotalSeconds;
            return seconds > 0
                ? (int)Math.Clamp(seconds, 1, 3600)
                : 0;
        }

        if (header.Date.HasValue)
        {
            double seconds = (header.Date.Value - DateTimeOffset.UtcNow).TotalSeconds;
            return seconds > 0 ? (int)Math.Clamp(seconds, 1, 3600) : 0;
        }

        return 0;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient httpClient,
        IEmbeddingProvider provider,
        IReadOnlyList<string> texts,
        TenantEmbeddingConfig config,
        EmbeddingProviderCredentials credentials,
        bool batch,
        string tenantId,
        CancellationToken ct)
    {
        // Provider request construction (including provider-specific argument validation) happens outside the transport
        // try/catch so its exceptions are not misreported as network failures.
        using HttpRequestMessage request = provider.BuildRequest(texts, config, credentials, batch);

        try
        {
            return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller cancellation must surface as OperationCanceledException, not a wrapped transport failure
            // (Story 14.3 Task 5 Implementation Guardrails).
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingApiException(
                $"{provider.DisplayName} embedding provider transport error while sending request.",
                tenantId,
                ex);
        }
        catch (TaskCanceledException ex)
        {
            // Not caused by caller cancellation (handled above) — therefore HttpClient.Timeout.
            throw new EmbeddingApiException(
                $"{provider.DisplayName} embedding provider request timed out.",
                tenantId,
                ex);
        }
        catch (IOException ex)
        {
            throw new EmbeddingApiException(
                $"{provider.DisplayName} embedding provider IO error while sending request.",
                tenantId,
                ex);
        }
    }

    private static async Task<string> ReadResponseBodyAsync(
        HttpResponseMessage response,
        string providerDisplayName,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingApiException(
                $"{providerDisplayName} embedding provider transport error while reading response.",
                tenantId,
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new EmbeddingApiException(
                $"{providerDisplayName} embedding provider response read timed out.",
                tenantId,
                ex);
        }
        catch (IOException ex)
        {
            throw new EmbeddingApiException(
                $"{providerDisplayName} embedding provider IO error while reading response.",
                tenantId,
                ex);
        }
    }
}
