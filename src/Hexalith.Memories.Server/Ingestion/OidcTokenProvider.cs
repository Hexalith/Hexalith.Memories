// <copyright file="OidcTokenProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

/// <summary>Singleton-safe OIDC client credentials token provider with per-client caching.</summary>
public sealed partial class OidcTokenProvider : IOidcTokenProvider
{
    /// <summary>The configured HttpClient name used by dependency injection.</summary>
    internal const string HttpClientName = nameof(OidcTokenProvider);

    private const int ResponsePreviewLimit = 1024;
    private const string RedactedPlaceholder = "[redacted]";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<OidcTokenCacheKey, CachedOidcToken> _cache = new();
    private readonly ConcurrentDictionary<InflightTokenFetchKey, Task<CachedOidcToken>> _inflight = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OidcTokenProvider> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="OidcTokenProvider"/> class.</summary>
    /// <param name="httpClientFactory">The HTTP client factory used to obtain a short-lived HttpClient per fetch.</param>
    /// <param name="timeProvider">The time provider used for cache expiration.</param>
    /// <param name="logger">The logger used for sanitized token acquisition diagnostics.</param>
    public OidcTokenProvider(
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider,
        ILogger<OidcTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<string> GetAccessTokenAsync(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope,
        CancellationToken ct)
    {
        OidcTokenCacheKey key = ValidateAndCreateKey(tokenEndpoint, clientId, clientSecret, scope);
        return GetOrFetchAsync(key, clientSecret, forceRefresh: false, ct);
    }

    /// <inheritdoc />
    public Task<string> InvalidateAndRefreshAsync(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope,
        CancellationToken ct)
    {
        OidcTokenCacheKey key = ValidateAndCreateKey(tokenEndpoint, clientId, clientSecret, scope);
        _cache.TryRemove(key, out _);
        return GetOrFetchAsync(key, clientSecret, forceRefresh: true, ct);
    }

    private static OidcTokenCacheKey ValidateAndCreateKey(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        if (!Uri.TryCreate(tokenEndpoint, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme is not "https" and not "http"))
        {
            throw new ArgumentException("Token endpoint must be an absolute HTTP or HTTPS URI.", nameof(tokenEndpoint));
        }

        // AC3: reject embedded credentials, query strings, and fragments. Error text must not
        // echo any embedded user-info, query value, or fragment so secrets cannot leak through
        // exception logs.
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                "Token endpoint must not contain embedded credentials (user-info component).",
                nameof(tokenEndpoint));
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException(
                "Token endpoint must not contain a query string.",
                nameof(tokenEndpoint));
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                "Token endpoint must not contain a fragment.",
                nameof(tokenEndpoint));
        }

        ValidateTokenEndpointTransport(uri, nameof(tokenEndpoint));

        string normalizedEndpoint = uri.GetComponents(
            UriComponents.SchemeAndServer | UriComponents.Path,
            UriFormat.UriEscaped);
        string normalizedScope = string.IsNullOrWhiteSpace(scope) ? string.Empty : scope.Trim();
        return new OidcTokenCacheKey(normalizedEndpoint, clientId.Trim(), normalizedScope);
    }

    /// <summary>Validates the production HTTPS policy and local loopback HTTP exception for OIDC token endpoints.</summary>
    /// <param name="uri">The absolute token endpoint URI.</param>
    /// <param name="parameterName">The field or argument name used by the thrown exception.</param>
    /// <exception cref="ArgumentException">Thrown when an HTTP token endpoint is not one of the literal loopback exceptions.</exception>
    internal static void ValidateTokenEndpointTransport(Uri uri, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             IsLocalHttpTokenEndpoint(uri)))
        {
            return;
        }

        throw new ArgumentException(
            "Production OIDC token endpoints require HTTPS; HTTP is allowed only for local loopback hosts (localhost, 127.0.0.1, [::1]).",
            parameterName);
    }

    private static bool IsLocalHttpTokenEndpoint(Uri uri)
        => string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal) ||
           string.Equals(uri.Host, "::1", StringComparison.Ordinal) ||
           string.Equals(uri.Host, "[::1]", StringComparison.Ordinal);

    private async Task<string> GetOrFetchAsync(
        OidcTokenCacheKey key,
        string clientSecret,
        bool forceRefresh,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!forceRefresh)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (TryGetCachedToken(key, now, out CachedOidcToken cachedToken))
            {
                LogCacheHit(key);
                return cachedToken.AccessToken;
            }
        }

        Task<CachedOidcToken> inflight = GetOrCreateInflightFetch(key, clientSecret, forceRefresh);
        CachedOidcToken token = await inflight.WaitAsync(ct).ConfigureAwait(false);
        return token.AccessToken;
    }

    private Task<CachedOidcToken> GetOrCreateInflightFetch(
        OidcTokenCacheKey key,
        string clientSecret,
        bool forceRefresh)
    {
        InflightTokenFetchKey inflightKey = new(key, forceRefresh);

        // Storm-collapse within the same fetch mode. Forced refreshes intentionally do not join
        // a normal cache-miss fetch, because a 401/403 retry may be carrying a freshly rotated
        // client_secret and must not reuse a request started with stale credentials.
        while (true)
        {
            if (_inflight.TryGetValue(inflightKey, out Task<CachedOidcToken>? existing))
            {
                return existing;
            }

            TaskCompletionSource<CachedOidcToken> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            if (_inflight.TryAdd(inflightKey, tcs.Task))
            {
                _ = RunDetachedFetchAsync(inflightKey, clientSecret, tcs);
                return tcs.Task;
            }
        }
    }

    private async Task RunDetachedFetchAsync(
        InflightTokenFetchKey inflightKey,
        string clientSecret,
        TaskCompletionSource<CachedOidcToken> tcs)
    {
        OidcTokenCacheKey key = inflightKey.TokenKey;
        try
        {
            // Re-check the cache once we own the inflight slot so a token published by an
            // earlier leader between the fast-path miss and TryAdd is reused instead of refetched.
            if (!inflightKey.ForceRefresh)
            {
                DateTimeOffset now = _timeProvider.GetUtcNow();
                if (TryGetCachedToken(key, now, out CachedOidcToken cachedToken))
                {
                    LogCacheHit(key);
                    tcs.SetResult(cachedToken);
                    return;
                }
            }

            LogCacheMiss(key, inflightKey.ForceRefresh);

            // The HTTP fetch runs detached from any caller's CancellationToken (AC1). Per-caller
            // cancellation flows through Task.WaitAsync(ct) at the public surface; the underlying
            // request is bounded by HttpClient.Timeout configured at registration.
            CachedOidcToken token = await FetchTokenAsync(key, clientSecret).ConfigureAwait(false);
            _cache[key] = token;
            tcs.SetResult(token);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
        finally
        {
            // Atomically remove only our own slot so a competing forced-refresh that already
            // started a fresh fetch is not displaced.
            ((ICollection<KeyValuePair<InflightTokenFetchKey, Task<CachedOidcToken>>>)_inflight)
                .Remove(new KeyValuePair<InflightTokenFetchKey, Task<CachedOidcToken>>(inflightKey, tcs.Task));
        }
    }

    private bool TryGetCachedToken(
        OidcTokenCacheKey key,
        DateTimeOffset now,
        out CachedOidcToken token)
    {
        if (_cache.TryGetValue(key, out CachedOidcToken? cached) && now < cached.ExpiresAt)
        {
            token = cached;
            return true;
        }

        token = null!;
        return false;
    }

    private async Task<CachedOidcToken> FetchTokenAsync(
        OidcTokenCacheKey key,
        string clientSecret)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, key.TokenEndpoint);
        List<KeyValuePair<string, string>> form =
        [
            new("grant_type", "client_credentials"),
            new("client_id", key.ClientId),
            new("client_secret", clientSecret),
        ];

        if (!string.IsNullOrWhiteSpace(key.Scope))
        {
            form.Add(new KeyValuePair<string, string>("scope", key.Scope));
        }

        request.Content = new FormUrlEncodedContent(form);

        // Resolve a fresh HttpClient from the factory per fetch so handler rotation (DNS, TLS
        // session pooling, PooledConnectionLifetime) is honored (AC2).
        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, CancellationToken.None).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw CreateTransportException(key, "transport error during token request", ex);
        }
        catch (TaskCanceledException ex)
        {
            // The fetch is detached, so any TaskCanceledException here is a Timeout from the
            // configured HttpClient.Timeout, never a caller cancellation.
            throw CreateTransportException(key, "token endpoint request timed out", ex);
        }
        catch (IOException ex)
        {
            throw CreateTransportException(key, "io error during token request", ex);
        }

        try
        {
            string responseBody = await ReadTokenResponseBodyAsync(response, key).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string correlationId = NewCorrelationId();
                string preview = SanitizePreview(responseBody, clientSecret);
                LogAcquisitionFailed(key, response.StatusCode, correlationId);
                throw new OidcTokenAcquisitionException(
                    response.StatusCode,
                    preview,
                    key.TokenEndpoint,
                    key.ClientId,
                    correlationId,
                    "token endpoint returned a non-success status code");
            }

            return ParseSuccessfulResponse(key, responseBody, clientSecret);
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task<string> ReadTokenResponseBodyAsync(
        HttpResponseMessage response,
        OidcTokenCacheKey key)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw CreateTransportException(key, "transport error while reading token response", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw CreateTransportException(key, "token endpoint response read timed out", ex);
        }
        catch (IOException ex)
        {
            throw CreateTransportException(key, "io error while reading token response", ex);
        }
    }

    private OidcTokenAcquisitionException CreateTransportException(
        OidcTokenCacheKey key,
        string reason,
        Exception innerException)
    {
        string correlationId = NewCorrelationId();
        LogTransportFailure(key, reason, correlationId);
        return new OidcTokenAcquisitionException(
            statusCode: null,
            responseBodyPreview: string.Empty,
            tokenEndpoint: key.TokenEndpoint,
            clientId: key.ClientId,
            correlationId: correlationId,
            reason: reason,
            innerException: innerException);
    }

    private CachedOidcToken ParseSuccessfulResponse(
        OidcTokenCacheKey key,
        string responseBody,
        string clientSecret)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement root = document.RootElement;
            string accessToken = ReadRequiredString(root, "access_token");
            int expiresIn = ReadRequiredPositiveInt32(root, "expires_in");
            string tokenType = ReadRequiredString(root, "token_type").Trim();

            if (!string.Equals(tokenType, "Bearer", StringComparison.OrdinalIgnoreCase))
            {
                throw CreateMalformedException(
                    key,
                    responseBody,
                    clientSecret,
                    $"unsupported token_type '{tokenType}'");
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateTimeOffset expiresAt = expiresIn <= RefreshSkew.TotalSeconds
                ? now
                : now.AddSeconds(expiresIn).Subtract(RefreshSkew);

            LogAcquisitionSucceeded(key, expiresAt);
            return new CachedOidcToken(accessToken, expiresAt);
        }
        catch (JsonException ex)
        {
            throw CreateMalformedException(key, responseBody, clientSecret, "invalid JSON token response", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw CreateMalformedException(key, responseBody, clientSecret, ex.Message, ex);
        }
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidOperationException($"missing or blank '{propertyName}'");
        }

        return value.GetString()!;
    }

    private static int ReadRequiredPositiveInt32(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value)
            || !value.TryGetInt32(out int parsed)
            || parsed <= 0)
        {
            throw new InvalidOperationException($"missing or non-positive '{propertyName}'");
        }

        return parsed;
    }

    private OidcTokenAcquisitionException CreateMalformedException(
        OidcTokenCacheKey key,
        string responseBody,
        string clientSecret,
        string reason,
        Exception? innerException = null)
    {
        string correlationId = NewCorrelationId();
        LogMalformedResponse(key, correlationId);
        return new OidcTokenAcquisitionException(
            statusCode: null,
            responseBodyPreview: SanitizePreview(responseBody, clientSecret),
            tokenEndpoint: key.TokenEndpoint,
            clientId: key.ClientId,
            correlationId: correlationId,
            reason: reason,
            innerException: innerException);
    }

    private static string SanitizePreview(string responseBody, string clientSecret)
    {
        // Sanitize before truncating so a secret straddling the boundary is fully redacted.
        string sanitized = responseBody;
        if (!string.IsNullOrEmpty(clientSecret))
        {
            sanitized = sanitized.Replace(clientSecret, RedactedPlaceholder, StringComparison.Ordinal);
        }

        sanitized = SensitiveJsonPropertyRegex().Replace(sanitized, $"$1\"{RedactedPlaceholder}\"");
        sanitized = BearerTokenRegex().Replace(sanitized, $"Bearer {RedactedPlaceholder}");

        return sanitized.Length <= ResponsePreviewLimit
            ? sanitized
            : sanitized[..ResponsePreviewLimit];
    }

    private static string NewCorrelationId() => Guid.NewGuid().ToString("N");

    private static string SafeEndpointForLog(string tokenEndpoint)
    {
        Uri uri = new(tokenEndpoint, UriKind.Absolute);
        return string.Concat(uri.Host, uri.AbsolutePath);
    }

    private void LogCacheHit(OidcTokenCacheKey key)
        => _logger.LogDebug(
            "OIDC token cache hit for endpoint {TokenEndpoint} and client {ClientId}.",
            SafeEndpointForLog(key.TokenEndpoint),
            key.ClientId);

    private void LogCacheMiss(OidcTokenCacheKey key, bool forced)
        => _logger.LogDebug(
            "OIDC token cache {CacheState} for endpoint {TokenEndpoint} and client {ClientId}.",
            forced ? "forced-refresh" : "miss",
            SafeEndpointForLog(key.TokenEndpoint),
            key.ClientId);

    private void LogAcquisitionSucceeded(OidcTokenCacheKey key, DateTimeOffset expiresAt)
        => _logger.LogInformation(
            "OIDC token acquired for endpoint {TokenEndpoint} and client {ClientId}; cache expires at {ExpiresAt}.",
            SafeEndpointForLog(key.TokenEndpoint),
            key.ClientId,
            expiresAt);

    private void LogAcquisitionFailed(
        OidcTokenCacheKey key,
        HttpStatusCode statusCode,
        string correlationId)
        => _logger.LogWarning(
            "OIDC token acquisition failed for endpoint {TokenEndpoint}, client {ClientId}, status {StatusCode}, correlation {CorrelationId}.",
            SafeEndpointForLog(key.TokenEndpoint),
            key.ClientId,
            (int)statusCode,
            correlationId);

    private void LogMalformedResponse(OidcTokenCacheKey key, string correlationId)
        => _logger.LogWarning(
            "OIDC token response was malformed for endpoint {TokenEndpoint}, client {ClientId}, correlation {CorrelationId}.",
            SafeEndpointForLog(key.TokenEndpoint),
            key.ClientId,
            correlationId);

    private void LogTransportFailure(OidcTokenCacheKey key, string reason, string correlationId)
        => _logger.LogWarning(
            "OIDC token transport failure for endpoint {TokenEndpoint}, client {ClientId}, reason {Reason}, correlation {CorrelationId}.",
            SafeEndpointForLog(key.TokenEndpoint),
            key.ClientId,
            reason,
            correlationId);

    [GeneratedRegex("(\"(?:access_token|refresh_token|id_token|client_secret)\"\\s*:\\s*)\"(?:\\\\.|[^\"])*\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    private static partial Regex SensitiveJsonPropertyRegex();

    [GeneratedRegex("\\bBearer\\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    private static partial Regex BearerTokenRegex();

    private sealed record OidcTokenCacheKey(string TokenEndpoint, string ClientId, string Scope);

    private sealed record InflightTokenFetchKey(OidcTokenCacheKey TokenKey, bool ForceRefresh);

    private sealed record CachedOidcToken(string AccessToken, DateTimeOffset ExpiresAt);
}
