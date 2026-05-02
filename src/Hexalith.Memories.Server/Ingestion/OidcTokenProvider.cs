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
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<OidcTokenCacheKey, CachedOidcToken> _cache = new();
    private readonly ConcurrentDictionary<OidcTokenCacheKey, SemaphoreSlim> _guards = new();
    private readonly HttpClient _httpClient;
    private readonly ILogger<OidcTokenProvider> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="OidcTokenProvider"/> class.</summary>
    /// <param name="httpClient">The configured token endpoint HTTP client.</param>
    /// <param name="timeProvider">The time provider used for cache expiration.</param>
    /// <param name="logger">The logger used for sanitized token acquisition diagnostics.</param>
    public OidcTokenProvider(
        HttpClient httpClient,
        TimeProvider timeProvider,
        ILogger<OidcTokenProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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

        string normalizedEndpoint = uri.GetComponents(
            UriComponents.SchemeAndServer | UriComponents.Path,
            UriFormat.UriEscaped);
        string normalizedScope = string.IsNullOrWhiteSpace(scope) ? string.Empty : scope.Trim();
        return new OidcTokenCacheKey(normalizedEndpoint, clientId.Trim(), normalizedScope);
    }

    private async Task<string> GetOrFetchAsync(
        OidcTokenCacheKey key,
        string clientSecret,
        bool forceRefresh,
        CancellationToken ct)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (!forceRefresh && TryGetCachedToken(key, now, out string? cachedToken))
        {
            LogCacheHit(key);
            return cachedToken!;
        }

        SemaphoreSlim guard = _guards.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await guard.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            now = _timeProvider.GetUtcNow();
            if (!forceRefresh && TryGetCachedToken(key, now, out cachedToken))
            {
                LogCacheHit(key);
                return cachedToken!;
            }

            LogCacheMiss(key, forceRefresh);
            CachedOidcToken token = await FetchTokenAsync(key, clientSecret, ct).ConfigureAwait(false);
            _cache[key] = token;
            return token.AccessToken;
        }
        finally
        {
            _ = guard.Release();
        }
    }

    private bool TryGetCachedToken(
        OidcTokenCacheKey key,
        DateTimeOffset now,
        out string? accessToken)
    {
        if (_cache.TryGetValue(key, out CachedOidcToken? cached) && now < cached.ExpiresAt)
        {
            accessToken = cached.AccessToken;
            return true;
        }

        accessToken = null;
        return false;
    }

    private async Task<CachedOidcToken> FetchTokenAsync(
        OidcTokenCacheKey key,
        string clientSecret,
        CancellationToken ct)
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

        using HttpResponseMessage response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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
            sanitized = sanitized.Replace(clientSecret, "[redacted]", StringComparison.Ordinal);
        }

        sanitized = SensitiveJsonPropertyRegex().Replace(sanitized, "$1\"[redacted]\"");

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

    [GeneratedRegex("(\"(?:access_token|refresh_token|id_token|client_secret)\"\\s*:\\s*)\"(?:\\\\.|[^\"])*\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    private static partial Regex SensitiveJsonPropertyRegex();

    private sealed record OidcTokenCacheKey(string TokenEndpoint, string ClientId, string Scope);

    private sealed record CachedOidcToken(string AccessToken, DateTimeOffset ExpiresAt);
}
